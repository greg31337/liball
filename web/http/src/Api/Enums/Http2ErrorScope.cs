namespace http;

/// <summary>
/// Identifies the protocol scope of an HTTP/2 error.
/// </summary>
public enum Http2ErrorScope
{
    /// <summary>
    /// No protocol error is present.
    /// </summary>
    None = 0,

    /// <summary>
    /// The error applies to one stream.
    /// </summary>
    Stream = 1,

    /// <summary>
    /// The error applies to the connection.
    /// </summary>
    Connection = 2
}
