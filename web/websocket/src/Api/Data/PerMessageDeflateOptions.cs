namespace websocket;

/// <summary>
/// Holds offer, requirement, and resource settings for <c>permessage-deflate</c>.
/// </summary>
public class PerMessageDeflateOptions
{
    /// <summary>
    /// Indicates that <c>server_no_context_takeover</c> is offered.
    /// </summary>
    public bool OfferServerNoContextTakeover;

    /// <summary>
    /// Indicates that negotiation must include <c>server_no_context_takeover</c>.
    /// </summary>
    public bool RequireServerNoContextTakeover;

    /// <summary>
    /// Indicates that <c>client_no_context_takeover</c> is offered.
    /// </summary>
    public bool OfferClientNoContextTakeover;

    /// <summary>
    /// Indicates that negotiation must include <c>client_no_context_takeover</c>.
    /// </summary>
    public bool RequireClientNoContextTakeover;

    /// <summary>
    /// Indicates that <c>server_max_window_bits</c> is offered.
    /// </summary>
    public bool OfferServerMaxWindowBits;

    /// <summary>
    /// Indicates whether <see cref="ServerMaxWindowBits"/> supplies a requested value.
    /// </summary>
    public bool HasServerMaxWindowBits;

    /// <summary>
    /// Holds the requested server window bits when <see cref="HasServerMaxWindowBits"/> is true.
    /// </summary>
    public int ServerMaxWindowBits = 15;

    /// <summary>
    /// Indicates that <c>client_max_window_bits</c> is offered.
    /// </summary>
    public bool OfferClientMaxWindowBits;

    /// <summary>
    /// Indicates whether <see cref="ClientMaxWindowBits"/> supplies a requested value.
    /// </summary>
    public bool HasClientMaxWindowBits;

    /// <summary>
    /// Holds the requested client window bits when <see cref="HasClientMaxWindowBits"/> is true.
    /// </summary>
    public int ClientMaxWindowBits = 15;

    /// <summary>
    /// Holds the minimum message length eligible for compression in bytes.
    /// </summary>
    public int CompressionThreshold = 1024;

    /// <summary>
    /// Holds the maximum permitted decompressed message length in bytes.
    /// </summary>
    public int MaximumDecompressedMessagePayloadLength = WebSocketDefaults.MaximumDecompressedMessagePayloadLength;
}
