using System;
using System.Net;

namespace Messager.EskizUz;

/// <summary>Base exception type for the Eskiz messenger package.</summary>
public class EskizException : Exception
{
    public EskizException(string message) : base(message) { }
    public EskizException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>Authentication failed (e.g. 401 even after refresh).</summary>
public class EskizAuthException : EskizException
{
    public string? ResponseBody { get; }
    public EskizAuthException(string message, string? responseBody = null) : base(message)
    {
        ResponseBody = responseBody;
    }
}

/// <summary>Eskiz returned a non-success HTTP status that is not auth-related.</summary>
public class EskizApiException : EskizException
{
    public HttpStatusCode StatusCode { get; }
    public string ResponseBody { get; }

    public EskizApiException(HttpStatusCode statusCode, string responseBody)
        : base($"Eskiz API returned {(int)statusCode} {statusCode}: {responseBody}")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
