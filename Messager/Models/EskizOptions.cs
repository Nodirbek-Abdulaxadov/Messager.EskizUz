namespace Messager.EskizUz.Models;

/// <summary>
/// Options used to configure the Eskiz messenger. Bind from configuration
/// (e.g. an "Eskiz" section in appsettings.json).
/// </summary>
public class EskizOptions
{
    /// <summary>Eskiz account email.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Eskiz account secret (password).</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>SMS sender id. Defaults to the public test sender "4546".</summary>
    public string SenderId { get; set; } = "4546";

    /// <summary>Optional callback URL passed to Eskiz.</summary>
    public string CallbackUrl { get; set; } = "https://eskiz.uz/sms";

    /// <summary>
    /// Token cache TTL. Eskiz tokens live ~30 days; we refresh proactively
    /// every 25 days by default to leave a safety margin.
    /// </summary>
    public System.TimeSpan TokenLifetime { get; set; } = System.TimeSpan.FromDays(25);
}
