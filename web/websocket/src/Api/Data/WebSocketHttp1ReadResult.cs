namespace websocket;

/// <summary>
/// Holds the result of an incremental HTTP/1 opening-handshake read.
/// </summary>
public class WebSocketHttp1ReadResult
{
    /// <summary>
    /// Identifies whether parsing completed, needs input, ended, or failed.
    /// </summary>
    public WebSocketParseStatus Status;

    /// <summary>
    /// Holds the number of source bytes consumed by the read operation.
    /// </summary>
    public int ConsumedCount;

    /// <summary>
    /// Indicates whether <see cref="Request"/> contains a completed request.
    /// </summary>
    public bool HasRequest;

    /// <summary>
    /// Holds a completed HTTP/1 request when <see cref="HasRequest"/> is true.
    /// </summary>
    public WebSocketHttpRequest Request = new WebSocketHttpRequest();

    /// <summary>
    /// Indicates whether <see cref="Response"/> contains a completed response.
    /// </summary>
    public bool HasResponse;

    /// <summary>
    /// Holds a completed HTTP/1 response when <see cref="HasResponse"/> is true.
    /// </summary>
    public WebSocketHttpResponse Response = new WebSocketHttpResponse();

    /// <summary>
    /// Indicates whether <see cref="Error"/> describes a parsing failure.
    /// </summary>
    public bool HasError;

    /// <summary>
    /// Holds structured error information when <see cref="HasError"/> is true.
    /// </summary>
    public WebSocketError Error = new WebSocketError();
}
