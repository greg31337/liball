using System;

namespace websocket;

/// <summary>
/// Defines WebSocket Close payload parsing, measurement, and writing operations.
/// </summary>
public interface IWebSocketCloseCodec
{
    /// <summary>
    /// Reads a complete Close payload.
    /// </summary>
    /// <param name="source">The complete Close payload bytes.</param>
    /// <returns>The decoded close result.</returns>
    WebSocketCloseReadResult ReadClosePayload(ReadOnlySpan<byte> source);

    /// <summary>
    /// Measures the encoded length of close information.
    /// </summary>
    /// <param name="close">The close information to encode.</param>
    /// <returns>The required payload length in bytes.</returns>
    /// <exception cref="WebSocketProtocolException">Thrown when the close information cannot form a valid wire payload.</exception>
    /// <exception cref="WebSocketLimitException">Thrown when the encoded reason exceeds the control-frame limit.</exception>
    int MeasureClosePayload(WebSocketCloseInfo close);

    /// <summary>
    /// Writes close information to a caller-provided destination.
    /// </summary>
    /// <param name="close">The close information to encode.</param>
    /// <param name="destination">The destination bytes.</param>
    /// <returns>The number of bytes written.</returns>
    /// <exception cref="WebSocketProtocolException">Thrown when the close information cannot form a valid wire payload.</exception>
    /// <exception cref="WebSocketLimitException">Thrown when the payload exceeds the limit or the destination is too small.</exception>
    int WriteClosePayload(WebSocketCloseInfo close, Span<byte> destination);
}
