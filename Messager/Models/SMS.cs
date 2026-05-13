using System;
using System.Text.Json.Serialization;

namespace Messager.EskizUz.Models;

public class SMS
{
    [JsonPropertyName("mobile_phone")]
    public string MobilePhone { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    [JsonPropertyName("callback_url")]
    public string CallbackUrl { get; set; } = string.Empty;

    // ---------- Backwards-compat shims ----------

    [JsonIgnore]
    [Obsolete("Use MobilePhone instead. This property forwards to MobilePhone.")]
    public string mobile_phone
    {
        get => MobilePhone;
        set => MobilePhone = value;
    }

    [JsonIgnore]
    [Obsolete("Use Message instead. This property forwards to Message.")]
    public string message
    {
        get => Message;
        set => Message = value;
    }

    [JsonIgnore]
    [Obsolete("Use From instead. This property forwards to From.")]
    public string from
    {
        get => From;
        set => From = value;
    }

    [JsonIgnore]
    [Obsolete("Use CallbackUrl instead. This property forwards to CallbackUrl.")]
    public string callback_url
    {
        get => CallbackUrl;
        set => CallbackUrl = value;
    }
}
