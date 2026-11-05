namespace http;

/// <summary>
/// Identifies an HTTP protocol version represented by the common message model.
/// </summary>
public enum HttpProtocolVersion
{
    /// <summary>
    /// HTTP version 1.0.
    /// </summary>
    Http10 = 0,

    /// <summary>
    /// HTTP version 1.1.
    /// </summary>
    Http11 = 1,

    /// <summary>
    /// HTTP version 2.
    /// </summary>
    Http2 = 2
}
