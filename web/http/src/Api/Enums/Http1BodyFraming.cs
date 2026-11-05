namespace http;

/// <summary>
/// Identifies the framing that delimits an HTTP/1 message body.
/// </summary>
public enum Http1BodyFraming
{
    /// <summary>
    /// The message has no body.
    /// </summary>
    None = 0,

    /// <summary>
    /// A Content-Length field delimits the body.
    /// </summary>
    ContentLength = 1,

    /// <summary>
    /// The chunked transfer coding delimits the body.
    /// </summary>
    Chunked = 2,

    /// <summary>
    /// Closing the connection delimits the body.
    /// </summary>
    CloseDelimited = 3,

    /// <summary>
    /// The connection carries opaque tunnel data.
    /// </summary>
    Tunnel = 4,

    /// <summary>
    /// The available framing metadata is invalid or ambiguous.
    /// </summary>
    Invalid = 5
}
