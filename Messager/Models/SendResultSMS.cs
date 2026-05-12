using System;
using System.Net;

namespace Messager.EskizUz.Models;

/// <summary>
/// Result returned by SMS send operations. Contains transport-level status,
/// raw response body and (when present) the OTP code that was generated.
/// </summary>
public class SendResultSMS
{
    /// <summary>OTP code (only set by <see cref="MessagerAgent.SendOtpAsync(string, System.Threading.CancellationToken)"/>).</summary>
    public int Code { get; set; }

    /// <summary>Whether the HTTP call succeeded (2xx).</summary>
    public bool Success { get; set; }

    /// <summary>Status message (success summary or error excerpt).</summary>
    public string Message { get; set; }

    /// <summary>HTTP status code returned by Eskiz.</summary>
    public HttpStatusCode StatusCode { get; set; }

    /// <summary>Raw response body as returned by Eskiz.</summary>
    public string ResponseBody { get; set; } = string.Empty;

    /// <summary>Optional Eskiz message id extracted from the response body, if any.</summary>
    public string? MessageId { get; set; }

    /// <summary>Convenience alias for <see cref="Success"/>.</summary>
    public bool IsSuccess => Success;

    public SendResultSMS(string message)
    {
        Message = message;
    }

    public SendResultSMS() : this(string.Empty) { }
}
