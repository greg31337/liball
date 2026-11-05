namespace websocket;

/// <summary>
/// Holds transport-neutral HTTP response metadata for an opening handshake.
/// </summary>
/// <remarks>
/// Field order and duplicates are preserved. Callers own assigned arrays; an operation that retains this response must copy them.
/// </remarks>
public class WebSocketHttpResponse
{
    /// <summary>
    /// Identifies the HTTP handshake transport.
    /// </summary>
    public WebSocketHandshakeTransport Transport;

    /// <summary>
    /// Holds the three-digit HTTP status code.
    /// </summary>
    public int StatusCode;

    /// <summary>
    /// Indicates whether <see cref="ReasonPhrase"/> contains an HTTP/1 reason phrase.
    /// </summary>
    public bool HasReasonPhrase;

    /// <summary>
    /// Holds the HTTP/1 reason phrase when <see cref="HasReasonPhrase"/> is true.
    /// </summary>
    public string ReasonPhrase = "";

    /// <summary>
    /// Holds regular HTTP fields in received or serialization order.
    /// </summary>
    public WebSocketHeaderField[] Fields = [];

    /// <summary>
    /// Indicates whether <see cref="Body"/> contains rejection body metadata.
    /// </summary>
    public bool HasBody;

    /// <summary>
    /// Holds rejection body bytes when <see cref="HasBody"/> is true.
    /// </summary>
    /// <remarks>
    /// Callers own the assigned array; an operation that retains the response must copy it.
    /// </remarks>
    public byte[] Body = [];
}
