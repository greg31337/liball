namespace websocket;

/// <summary>
/// Holds the public classification of a WebSocket error.
/// </summary>
public class WebSocketError
{
    /// <summary>
    /// Identifies the error category.
    /// </summary>
    public WebSocketErrorKind Kind;

    /// <summary>
    /// Indicates whether <see cref="CloseCode"/> and <see cref="KnownCloseCode"/> describe a close code.
    /// </summary>
    public bool HasCloseCode;

    /// <summary>
    /// Holds the raw close code, including future registered and private-use values.
    /// </summary>
    public int CloseCode;

    /// <summary>
    /// Indicates whether <see cref="KnownCloseCode"/> identifies the known meaning of <see cref="CloseCode"/>.
    /// </summary>
    public bool HasKnownCloseCode;

    /// <summary>
    /// Identifies the known meaning of <see cref="CloseCode"/> when <see cref="HasKnownCloseCode"/> is true.
    /// </summary>
    public WebSocketCloseCode KnownCloseCode;

    /// <summary>
    /// Indicates whether <see cref="HttpStatusCode"/> contains a related HTTP status.
    /// </summary>
    public bool HasHttpStatusCode;

    /// <summary>
    /// Holds the related HTTP status when <see cref="HasHttpStatusCode"/> is true.
    /// </summary>
    public int HttpStatusCode;

    /// <summary>
    /// Holds a stable machine-readable error identifier.
    /// </summary>
    public string Identifier = "";

    /// <summary>
    /// Holds a safe diagnostic message that excludes peer-controlled line breaks.
    /// </summary>
    public string Diagnostic = "";
}
