using System.Text.Json;

namespace jmap;

/// <summary>
/// Represents a JMAP method-level error response.
/// </summary>
public class JmapMethodException : System.Exception
{
    /// <summary>The method name associated with the error.</summary>
    public string? MethodName;

    /// <summary>The method call identifier associated with the error.</summary>
    public string? CallId;

    /// <summary>The exact method error type token returned by the server.</summary>
    public string? ErrorToken;

    /// <summary>The decoded method error, or null when decoding was not possible.</summary>
    public JmapMethodError? Error;

    /// <summary>The complete structured method error data returned by the server.</summary>
    public JsonElement StructuredData;

    /// <summary>Whether retrying the method call can be considered by the caller.</summary>
    public bool IsRetryable;

    /// <summary>Creates a method exception with call identity, error, structured, and retry context.</summary>
    /// <param name="message">The diagnostic failure message.</param>
    /// <param name="methodName">The method name associated with the error.</param>
    /// <param name="callId">The method call identifier associated with the error.</param>
    /// <param name="errorToken">The exact method error type token.</param>
    /// <param name="error">The decoded method error, or null.</param>
    /// <param name="data">The complete structured method error data.</param>
    /// <param name="isRetryable">Whether retrying can be considered by the caller.</param>
    public JmapMethodException(string message, string? methodName, string? callId, string? errorToken, JmapMethodError? error, JsonElement data, bool isRetryable) : base(message)
    {
        MethodName = methodName;
        CallId = callId;
        ErrorToken = errorToken;
        Error = error;
        StructuredData = data;
        IsRetryable = isRetryable;
    }
}
