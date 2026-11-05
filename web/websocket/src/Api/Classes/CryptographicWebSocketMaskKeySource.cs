namespace websocket;

/// <summary>
/// Creates WebSocket frame masking keys with the built-in cryptographic random generator.
/// </summary>
public class CryptographicWebSocketMaskKeySource : IWebSocketMaskKeySource
{
    /// <summary>
    /// Initializes a cryptographic masking key source.
    /// </summary>
    public CryptographicWebSocketMaskKeySource()
    {
    }

    /// <summary>
    /// Creates a fresh unpredictable four-byte masking key.
    /// </summary>
    /// <returns>The masking key interpreted in network byte order.</returns>
    public uint CreateMaskingKey()
    {
        byte[] randomBytes = new byte[WebSocketProtocolConstants.MaskingKeyLength];
        System.Security.Cryptography.RandomNumberGenerator.Fill(randomBytes);
        uint key = (uint)randomBytes[0] << 24;
        key = key | ((uint)randomBytes[1] << 16);
        key = key | ((uint)randomBytes[2] << 8);
        key = key | (uint)randomBytes[3];
        return key;
    }
}
