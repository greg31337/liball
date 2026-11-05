namespace websocket;

/// <summary>
/// Defines pull-based access to WebSocket connection state and events.
/// </summary>
public interface IWebSocketConnection
{
    /// <summary>
    /// Queues one complete application message for sending.
    /// </summary>
    /// <param name="request">The complete message send request.</param>
    /// <returns>The send status, including output-capacity pressure or state rejection.</returns>
    WebSocketParseStatus SendWholeMessage(WebSocketSendRequest request);

    /// <summary>
    /// Queues one chunk of a streaming application message for sending.
    /// </summary>
    /// <param name="chunk">The streaming message chunk.</param>
    /// <returns>The send status, including output-capacity pressure or state rejection.</returns>
    WebSocketParseStatus SendMessageChunk(WebSocketSendChunk chunk);

    /// <summary>
    /// Reads the next queued connection event when one is available.
    /// </summary>
    /// <returns>The next event, or <c>null</c> when the event queue is empty.</returns>
    WebSocketEvent? ReadNextEvent();

    /// <summary>
    /// Gets an independent snapshot of current connection information.
    /// </summary>
    /// <returns>The connection information snapshot.</returns>
    WebSocketConnectionInfo GetConnectionInfo();
}
