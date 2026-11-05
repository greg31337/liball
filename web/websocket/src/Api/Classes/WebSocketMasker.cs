using System;

namespace websocket;

/// <summary>
/// Applies the RFC 6455 XOR masking operation directly to payload bytes.
/// </summary>
public class WebSocketMasker : IWebSocketMasker
{
    /// <summary>
    /// Initializes a stateless WebSocket payload masker.
    /// </summary>
    public WebSocketMasker()
    {
    }

    /// <summary>
    /// Applies the four-byte XOR masking cycle to a payload span.
    /// </summary>
    /// <param name="payload">The payload bytes modified in place.</param>
    /// <param name="maskingKey">The masking key interpreted in network byte order.</param>
    /// <param name="streamOffset">The nonnegative payload stream offset of the first byte.</param>
    /// <exception cref="WebSocketLimitException">Thrown when the stream offset is negative.</exception>
    public void ApplyMask(Span<byte> payload, uint maskingKey, int streamOffset)
    {
        if (streamOffset < 0)
        {
            throw new WebSocketLimitException();
        }

        int keyIndex = streamOffset % WebSocketProtocolConstants.MaskingKeyLength;
        int payloadIndex = 0;
        while (payloadIndex < payload.Length)
        {
            byte maskByte;
            switch (keyIndex)
            {
                case 0:
                    maskByte = (byte)((maskingKey >> 24) & 255U);
                    break;
                case 1:
                    maskByte = (byte)((maskingKey >> 16) & 255U);
                    break;
                case 2:
                    maskByte = (byte)((maskingKey >> 8) & 255U);
                    break;
                default:
                    maskByte = (byte)(maskingKey & 255U);
                    break;
            }

            payload[payloadIndex] = (byte)(payload[payloadIndex] ^ maskByte);
            payloadIndex = payloadIndex + 1;
            keyIndex = keyIndex + 1;
            if (keyIndex == WebSocketProtocolConstants.MaskingKeyLength)
            {
                keyIndex = 0;
            }
        }
    }
}
