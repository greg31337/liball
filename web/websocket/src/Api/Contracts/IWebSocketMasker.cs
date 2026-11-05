using System;

namespace websocket;

/// <summary>
/// Defines in-place WebSocket payload masking and unmasking.
/// </summary>
public interface IWebSocketMasker
{
    /// <summary>
    /// Applies the four-byte XOR masking cycle to a payload span.
    /// </summary>
    /// <param name="payload">The payload bytes modified in place.</param>
    /// <param name="maskingKey">The masking key interpreted in network byte order.</param>
    /// <param name="streamOffset">The nonnegative payload stream offset of the first byte.</param>
    /// <exception cref="WebSocketLimitException">Thrown when the stream offset is negative.</exception>
    void ApplyMask(Span<byte> payload, uint maskingKey, int streamOffset);
}
