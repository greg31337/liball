namespace jmap;

/// <summary>
/// Represents an HTTP, streaming, or connectivity failure while accessing a JMAP endpoint.
/// </summary>
public class JmapTransportException : System.Exception
{
    /// <summary>The endpoint involved in the failure, or null when unavailable.</summary>
    public string? Endpoint;

    /// <summary>The HTTP status code, when present.</summary>
    public int HttpStatus;

    /// <summary>Whether an HTTP status code is present.</summary>
    public bool HasHttpStatus;

    /// <summary>Whether retrying the transport operation can be considered by the caller.</summary>
    public bool IsRetryable;

    /// <summary>The unmodified HTTP response body, or null when no response body was received.</summary>
    public byte[]? ResponseBody;

    /// <summary>Creates a transport exception with endpoint, HTTP, response, and retry context.</summary>
    /// <param name="message">The diagnostic failure message.</param>
    /// <param name="endpoint">The endpoint involved in the failure, or null.</param>
    /// <param name="httpStatus">The HTTP status code, or zero when no status is present.</param>
    /// <param name="hasHttpStatus">Whether an HTTP status code is present.</param>
    /// <param name="isRetryable">Whether retrying can be considered by the caller.</param>
    /// <param name="responseBody">The unmodified response body, or null.</param>
    /// <param name="innerException">The underlying transport exception, or null.</param>
    public JmapTransportException(string message, string? endpoint, int httpStatus, bool hasHttpStatus, bool isRetryable, byte[]? responseBody, System.Exception? innerException) : base(message, innerException)
    {
        Endpoint = endpoint;
        HttpStatus = httpStatus;
        HasHttpStatus = hasHttpStatus;
        IsRetryable = isRetryable;
        ResponseBody = responseBody;
    }
}
