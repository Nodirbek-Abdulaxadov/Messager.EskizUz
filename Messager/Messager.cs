using Messager.Models;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace Messager;
public class Messager : IDisposable
{
	private string TOKEN = string.Empty;
	private readonly string _email;
	private readonly string _secretKey;

	/// <summary>
	/// Initialize intance and token
	/// </summary>
	public Messager(string email, string secretKey)
	{
		_email = email;
		_secretKey = secretKey;
		GetToken();
	}

    /// <summary>
    /// Send otp sms with phone number
    /// </summary>
    /// <param name="phoneNumber"></param>
    /// <returns>SendResultSMS</returns>
    public async Task<SendResultSMS> SendOtpAsync(string phoneNumber)
    {
        int code = GetRandomCode();
        var sms = new SMS()
		{
			mobile_phone = phoneNumber.Replace("+",""),
			from = "4546",
			message = CreateSMS(code),
			callback_url = "https://software-engineer.uz"
		};
		using var httpClient = new HttpClient();
        var httpContent = new StringContent(JsonConvert.SerializeObject(sms),
            Encoding.UTF8, "application/json");

		var htm = new HttpRequestMessage(HttpMethod.Post, Constants.Send_SMS_URL);
		htm.Content = httpContent;
		htm.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN);

        var httpResponse = await httpClient.SendAsync(htm);

		
		if (httpResponse.IsSuccessStatusCode)
		{
			var result = new SendResultSMS("Successfully sent!");
			result.Success = true;
			result.Code = code;
			return result;
        }
		else
		{
            var result = new SendResultSMS("Something went wrong!");
            result.Success = false;
            return result;
        }
    }

    /// <summary>
    /// Send SMS via phoneNumber with custom text
    /// </summary>
    /// <param name="phoneNumber"></param>
    /// <param name="text"></param>
    /// <returns></returns>
    public async Task<bool> SendSMSAsync(string phoneNumber, string text)
    {
        var sms = new SMS()
        {
            mobile_phone = phoneNumber.Replace("+", ""),
            from = "4546",
            message = text,
            callback_url = "https://software-engineer.uz"
        };
        using var httpClient = new HttpClient();
        var httpContent = new StringContent(JsonConvert.SerializeObject(sms),
            Encoding.UTF8, "application/json");

        var htm = new HttpRequestMessage(HttpMethod.Post, Constants.Send_SMS_URL);
        htm.Content = httpContent;
        htm.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TOKEN);

        var httpResponse = await httpClient.SendAsync(htm);


        if (httpResponse.IsSuccessStatusCode)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Login to sms api service
    /// </summary>
    /// <returns>token</returns>
    private async Task<string> LoginAsync()
	{
		using var httpClient = new HttpClient();
		var data = new
		{
            email = _email,
			password = _secretKey
        };
        var httpContent = new StringContent(JsonConvert.SerializeObject(data), 
			Encoding.UTF8, "application/json");
        var httpResponse = await httpClient.PostAsync(Constants.LOGIN_URL, httpContent);

		if (httpResponse.IsSuccessStatusCode)
		{
            var json = await httpResponse.Content.ReadAsStringAsync();
			return JsonConvert.DeserializeObject<JWT_Token>(json).data.token;
        }
		else
		{
			return httpResponse.StatusCode.ToString();
        }
    }

	/// <summary>
	/// If token null or empty gets new token
	/// </summary>
	private void GetToken()
	{
		if (string.IsNullOrEmpty(TOKEN))
		{
            TOKEN = LoginAsync().Result;
        }
	}

	/// <summary>
	/// Creates new random code with 5 digits
	/// </summary>
	/// <returns>new code</returns>
	private int GetRandomCode()
	{
		Random random= new Random();
		return random.Next(10000, 99999);
	}

	/// <summary>
	/// Creates sms template with code
	/// </summary>
	/// <param name="code"></param>
	/// <returns>SMS</returns>
    private string CreateSMS(int code)
        => $"""
            Sizning tasdiqlash kodingiz:
            {code}

            Ваш проверочный код:
            {code}
            """;

    public void Dispose()
		=> GC.SuppressFinalize(this);
}