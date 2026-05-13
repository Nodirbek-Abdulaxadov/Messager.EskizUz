using System;
using System.Text.Json.Serialization;

namespace Messager.EskizUz.Models;

public class JWT_Token
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public Data Data { get; set; } = new Data();

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    // ---------- Backwards-compat shims ----------

    [JsonIgnore]
    [Obsolete("Use Message instead.")]
    public string message
    {
        get => Message;
        set => Message = value;
    }

    [JsonIgnore]
    [Obsolete("Use Data instead.")]
    public Data data
    {
        get => Data;
        set => Data = value;
    }

    [JsonIgnore]
    [Obsolete("Use TokenType instead.")]
    public string token_type
    {
        get => TokenType;
        set => TokenType = value;
    }
}

public class Data
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonIgnore]
    [Obsolete("Use Token instead.")]
    public string token
    {
        get => Token;
        set => Token = value;
    }
}
