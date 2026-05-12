using System;
using System.Threading;
using System.Threading.Tasks;
using Messager.EskizUz.Models;

namespace Messager.EskizUz;

/// <summary>
/// Public contract for the Eskiz SMS messenger. Implementations must be
/// thread-safe and may cache the Eskiz auth token internally.
/// </summary>
public interface IMessagerAgent
{
    /// <summary>Send an OTP SMS to <paramref name="phoneNumber"/>.</summary>
    Task<SendResultSMS> SendOtpAsync(string phoneNumber, CancellationToken ct = default);

    /// <summary>Send a free-form SMS.</summary>
    Task<SendResultSMS> SendSMSAsync(string phoneNumber, string text, CancellationToken ct = default);
}
