namespace websocket;

/// <summary>
/// Holds the result of parsing one WebSocket Close payload.
/// </summary>
public class WebSocketCloseReadResult
{
    /// <summary>
    /// Indicates that the entire Close payload is valid.
    /// </summary>
    public bool Valid;

    /// <summary>
    /// Holds decoded close information when <see cref="Valid"/> is true.
    /// </summary>
    public WebSocketCloseInfo Close = new WebSocketCloseInfo();

    /// <summary>
    /// Indicates whether <see cref="Error"/> describes an invalid payload.
    /// </summary>
    public bool HasError;

    /// <summary>
    /// Holds structured error information when <see cref="HasError"/> is true.
    /// </summary>
    public WebSocketError Error = new WebSocketError();
}
