namespace websocket;

/// <summary>
/// Holds transport-neutral HTTP request metadata for an opening handshake.
/// </summary>
/// <remarks>
/// Field order and duplicates are preserved. Callers own assigned arrays; an operation that retains this request must copy them.
/// </remarks>
public class WebSocketHttpRequest
{
    /// <summary>
    /// Identifies the HTTP handshake transport.
    /// </summary>
    public WebSocketHandshakeTransport Transport;

    /// <summary>
    /// Holds the request method or <c>:method</c> value.
    /// </summary>
    public string Method = "";

    /// <summary>
    /// Indicates whether <see cref="RequestTarget"/> contains an HTTP/1 request target.
    /// </summary>
    public bool HasRequestTarget;

    /// <summary>
    /// Holds the HTTP/1 request target when <see cref="HasRequestTarget"/> is true.
    /// </summary>
    public string RequestTarget = "";

    /// <summary>
    /// Indicates whether <see cref="HttpVersion"/> contains an HTTP/1 version.
    /// </summary>
    public bool HasHttpVersion;

    /// <summary>
    /// Holds the HTTP/1 version when <see cref="HasHttpVersion"/> is true.
    /// </summary>
    public string HttpVersion = "";

    /// <summary>
    /// Indicates whether <see cref="Scheme"/> contains a <c>:scheme</c> value.
    /// </summary>
    public bool HasScheme;

    /// <summary>
    /// Holds the Extended CONNECT <c>:scheme</c> value when <see cref="HasScheme"/> is true.
    /// </summary>
    public string Scheme = "";

    /// <summary>
    /// Indicates whether <see cref="Authority"/> contains a <c>:authority</c> value.
    /// </summary>
    public bool HasAuthority;

    /// <summary>
    /// Holds the Extended CONNECT <c>:authority</c> value when <see cref="HasAuthority"/> is true.
    /// </summary>
    public string Authority = "";

    /// <summary>
    /// Indicates whether <see cref="Path"/> contains a <c>:path</c> value.
    /// </summary>
    public bool HasPath;

    /// <summary>
    /// Holds the Extended CONNECT <c>:path</c> value when <see cref="HasPath"/> is true.
    /// </summary>
    public string Path = "";

    /// <summary>
    /// Indicates whether <see cref="Protocol"/> contains a <c>:protocol</c> value.
    /// </summary>
    public bool HasProtocol;

    /// <summary>
    /// Holds the Extended CONNECT <c>:protocol</c> value when <see cref="HasProtocol"/> is true.
    /// </summary>
    public string Protocol = "";

    /// <summary>
    /// Holds regular HTTP fields in received or serialization order.
    /// </summary>
    public WebSocketHeaderField[] Fields = [];
}
