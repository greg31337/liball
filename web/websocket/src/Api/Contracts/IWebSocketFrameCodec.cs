using System;

namespace websocket;

/// <summary>
/// Defines incremental WebSocket frame codec operations.
/// </summary>
public interface IWebSocketFrameCodec
{
    /// <summary>
    /// Reads frame data from the supplied source bytes.
    /// </summary>
    /// <param name="source">The next contiguous input bytes.</param>
    /// <returns>The incremental frame read result.</returns>
    WebSocketFrameReadResult ReadFrame(ReadOnlySpan<byte> source);

    /// <summary>
    /// Measures the complete encoded size of a frame write request.
    /// </summary>
    /// <param name="request">The frame write request.</param>
    /// <returns>The complete encoded frame size in bytes.</returns>
    /// <exception cref="WebSocketProtocolException">Thrown when the frame header is invalid.</exception>
    /// <exception cref="WebSocketLimitException">Thrown when the frame exceeds the configured or signed size limit.</exception>
    /// <exception cref="WebSocketStateException">Thrown when the write request is inconsistent.</exception>
    int MeasureFrame(WebSocketFrameWriteRequest request);

    /// <summary>
    /// Writes the next encoded frame bytes and advances the consumed request payload range.
    /// </summary>
    /// <param name="request">The current frame payload chunk and continuation offset.</param>
    /// <param name="destination">The bounded output destination.</param>
    /// <returns>The number of encoded bytes produced.</returns>
    /// <exception cref="WebSocketProtocolException">Thrown when the frame header is invalid.</exception>
    /// <exception cref="WebSocketLimitException">Thrown when the frame exceeds the configured or signed size limit.</exception>
    /// <exception cref="WebSocketStateException">Thrown when the write request is inconsistent.</exception>
    int WriteFrame(WebSocketFrameWriteRequest request, Span<byte> destination);

    /// <summary>
    /// Resets incremental input state before reading another frame.
    /// </summary>
    void ResetFrameRead();
}
