using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Messager.EskizUz.Models;

namespace Messager.EskizUz;

/// <summary>
/// Concrete implementation of <see cref="IMessagerAgent"/> that talks to
/// the Eskiz.uz SMS API.
///
/// <para>
/// Two construction modes are supported:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       <b>Legacy</b>: <c>new MessagerAgent(email, secretKey)</c> — uses a
///       process-wide <see cref="HttpClient"/>. Safe to keep around for the
///       lifetime of the process. Avoid creating many instances per request.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>DI / recommended</b>: <c>services.AddEskizMessenger(configuration)</c>
///       — uses <see cref="IHttpClientFactory"/> for proper socket reuse.
///     </description>
///   </item>
/// </list>
/// </summary>
public class MessagerAgent : IMessagerAgent, IDisposable
{
    /// <summary>Name used when registering the named <see cref="HttpClient"/>.</summary>
    public const string HttpClientName = "Messager.EskizUz";

    // Static client shared across all legacy-constructed instances to avoid
    // socket exhaustion. Never disposed for the lifetime of the process.
    private static readonly HttpClient _sharedHttp = new HttpClient();

    private readonly EskizOptions _options;
    private readonly IHttpClientFactory? _httpFactory;

    private string? _token;
    private DateTimeOffset _tokenObtainedAt = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _tokenLock = new SemaphoreSlim(1, 1);

    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    // ---------------- Constructors ----------------

    /// <summary>
    /// Legacy constructor kept for backwards compatibility. Uses a process-wide
    /// shared <see cref="HttpClient"/> and the default sender id ("4546").
    /// </summary>
    public MessagerAgent(string email, string secretKey)
        : this(new EskizOptions { Email = email, SecretKey = secretKey }, httpClientFactory: null)
    {
    }

    /// <summary>
    /// New constructor accepting full options (including sender id) for the
    /// non-DI scenario.
    /// </summary>
    public MessagerAgent(EskizOptions options)
        : this(options, httpClientFactory: null)
    {
    }

    /// <summary>
    /// DI-friendly constructor. Use the
    /// <see cref="ServiceCollectionExtensions.AddEskizMessenger(Microsoft.Extensions.DependencyInjection.IServiceCollection, Microsoft.Extensions.Configuration.IConfiguration, string)"/>
    /// extension instead of calling this directly.
    /// </summary>
    public MessagerAgent(EskizOptions options, IHttpClientFactory? httpClientFactory)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(_options.Email))
            throw new ArgumentException("EskizOptions.Email is required.", nameof(options));
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
            throw new ArgumentException("EskizOptions.SecretKey is required.", nameof(options));

        _httpFactory = httpClientFactory;
    }

    // ---------------- Public API ----------------

    /// <inheritdoc/>
    public async Task<SendResultSMS> SendOtpAsync(string phoneNumber, CancellationToken ct = default)
    {
        int code = GetRandomCode();
        var result = await SendInternalAsync(phoneNumber, CreateSMS(code), ct).ConfigureAwait(false);
        result.Code = code;
        return result;
    }

    /// <summary>Legacy parameterless-CT overload — forwards to the CancellationToken-aware version.</summary>
    [Obsolete("Use the CancellationToken overload: SendOtpAsync(phoneNumber, ct).")]
    public Task<SendResultSMS> SendOtpAsync(string phoneNumber)
        => SendOtpAsync(phoneNumber, CancellationToken.None);

    /// <inheritdoc/>
    public Task<SendResultSMS> SendSMSAsync(string phoneNumber, string text, CancellationToken ct = default)
        => SendInternalAsync(phoneNumber, text, ct);

    /// <summary>
    /// Legacy <see cref="bool"/>-returning overload kept for backwards compatibility.
    /// Returns <c>true</c> if Eskiz responded with a 2xx status.
    /// </summary>
    [Obsolete("Use SendSMSAsync(phoneNumber, text, ct) returning SendResultSMS for richer info.")]
    public async Task<bool> SendSMSAsync(string phoneNumber, string text)
    {
        var result = await SendSMSAsync(phoneNumber, text, CancellationToken.None).ConfigureAwait(false);
        return result.IsSuccess;
    }

    // ---------------- Core send logic ----------------

    private async Task<SendResultSMS> SendInternalAsync(string phoneNumber, string text, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Phone number is required.", nameof(phoneNumber));
        if (text is null)
            throw new ArgumentNullException(nameof(text));

        var sms = new SMS
        {
            MobilePhone = phoneNumber.Replace("+", string.Empty),
            From = _options.SenderId,
            Message = text,
            CallbackUrl = _options.CallbackUrl,
        };

        var token = await GetTokenAsync(ct).ConfigureAwait(false);
        var response = await SendOnceAsync(sms, token, ct).ConfigureAwait(false);

        // One-shot retry on 401 with a forcibly refreshed token.
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            response.Dispose();
            await InvalidateTokenAsync(ct).ConfigureAwait(false);
            token = await GetTokenAsync(ct).ConfigureAwait(false);
            response = await SendOnceAsync(sms, token, ct).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var body401 = await SafeReadAsync(response, ct).ConfigureAwait(false);
                response.Dispose();
                throw new EskizAuthException(
                    "Eskiz auth still failing after token refresh.", body401);
            }
        }

        var body = await SafeReadAsync(response, ct).ConfigureAwait(false);
        var status = response.StatusCode;
        response.Dispose();

        var result = new SendResultSMS(status == HttpStatusCode.OK
            ? "Successfully sent!"
            : $"Eskiz returned {(int)status} {status}")
        {
            Success = (int)status >= 200 && (int)status < 300,
            StatusCode = status,
            ResponseBody = body,
            MessageId = TryExtractMessageId(body),
        };
        return result;
    }

    private async Task<HttpResponseMessage> SendOnceAsync(SMS sms, string token, CancellationToken ct)
    {
        var httpClient = GetHttpClient();
        var json = JsonSerializer.Serialize(sms, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, Constants.Send_SMS_URL)
        {
            Content = content,
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await httpClient.SendAsync(request, ct).ConfigureAwait(false);
    }

    // ---------------- Token management ----------------

    private async Task<string> GetTokenAsync(CancellationToken ct)
    {
        if (IsTokenFresh()) return _token!;

        await _tokenLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (IsTokenFresh()) return _token!;

            var fresh = await LoginAsync(ct).ConfigureAwait(false);
            _token = fresh;
            _tokenObtainedAt = DateTimeOffset.UtcNow;
            return _token;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private bool IsTokenFresh()
    {
        if (string.IsNullOrEmpty(_token)) return false;
        return DateTimeOffset.UtcNow - _tokenObtainedAt < _options.TokenLifetime;
    }

    private async Task InvalidateTokenAsync(CancellationToken ct)
    {
        await _tokenLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            _token = null;
            _tokenObtainedAt = DateTimeOffset.MinValue;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task<string> LoginAsync(CancellationToken ct)
    {
        var httpClient = GetHttpClient();

        var payload = new { email = _options.Email, password = _options.SecretKey };
        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await httpClient.PostAsync(Constants.LOGIN_URL, content, ct).ConfigureAwait(false);
        var body = await SafeReadAsync(response, ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new EskizAuthException("Eskiz login failed (401).", body);
            throw new EskizApiException(response.StatusCode, body);
        }

        JWT_Token? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<JWT_Token>(body, _jsonOptions);
        }
        catch (JsonException)
        {
            throw new EskizApiException(response.StatusCode, body);
        }

        var token = parsed?.Data?.Token;
        if (string.IsNullOrWhiteSpace(token))
            throw new EskizAuthException("Eskiz login response did not contain a token.", body);
        return token!;
    }

    // ---------------- Helpers ----------------

    private HttpClient GetHttpClient()
        => _httpFactory is not null ? _httpFactory.CreateClient(HttpClientName) : _sharedHttp;

    private static int GetRandomCode()
        => RandomNumberGenerator.GetInt32(10000, 100000);

    private static string CreateSMS(int code)
        => $"Sizning tasdiqlash kodingiz:\n{code}\n\nВаш проверочный код:\n{code}";

    private static async Task<string> SafeReadAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
#if NET6_0
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#else
            return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
#endif
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string? TryExtractMessageId(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;
            if (doc.RootElement.TryGetProperty("id", out var id))
                return id.ToString();
            if (doc.RootElement.TryGetProperty("data", out var data) &&
                data.ValueKind == JsonValueKind.Object &&
                data.TryGetProperty("id", out var did))
                return did.ToString();
            return null;
        }
        catch
        {
            return null;
        }
    }

    // ---------------- IDisposable (legacy, no-op for compat) ----------------

    /// <summary>
    /// Historically a no-op. The shared <see cref="HttpClient"/> is owned by
    /// the process and is never disposed; injected clients are owned by the
    /// DI container. Kept for backwards-compatibility only.
    /// </summary>
    [Obsolete("Dispose is a no-op. MessagerAgent does not own its HttpClient.")]
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
