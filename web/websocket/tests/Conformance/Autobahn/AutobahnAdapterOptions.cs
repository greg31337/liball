using websocket;

namespace websocket.Tests.Autobahn;

internal static class AutobahnAdapterOptions
{
    internal const int MaximumPayloadLength = 134217728;

    internal static PerMessageDeflateOptions CreateCompressionOptions()
    {
        return new PerMessageDeflateOptions
        {
            OfferServerNoContextTakeover = true,
            OfferClientNoContextTakeover = true,
            OfferServerMaxWindowBits = true,
            HasServerMaxWindowBits = true,
            ServerMaxWindowBits = 15,
            OfferClientMaxWindowBits = true,
            CompressionThreshold = 0,
            MaximumDecompressedMessagePayloadLength = MaximumPayloadLength
        };
    }

    internal static WebSocketConnectionOptions CreateConnectionTemplate(
        IWebSocketExtension extension,
        PerMessageDeflateOptions compression,
        bool client,
        bool compressionNegotiated)
    {
        WebSocketConnectionOptions options = new WebSocketConnectionOptions
        {
            DeliveryMode = WebSocketMessageDeliveryMode.Reassembled,
            Extensions = compressionNegotiated ? [extension] : [],
            HasPerMessageDeflate = compressionNegotiated,
            PerMessageDeflate = compression,
            MaximumFramePayloadLength = MaximumPayloadLength,
            MaximumMessagePayloadLength = MaximumPayloadLength,
            MaximumDecompressedMessagePayloadLength = MaximumPayloadLength,
            MaximumFragmentCount = 1000000,
            MaximumBufferedInputBytes = MaximumPayloadLength,
            MaximumBufferedOutputBytes = MaximumPayloadLength,
            MaximumQueuedEvents = 1024,
            AutomaticFramePayloadLength = 65536,
            AutomaticPong = true,
            AutomaticCloseResponse = true,
            CloseTimeoutMilliseconds = 30000
        };
        if (client)
        {
            options.HasMaskKeySource = true;
            options.MaskKeySource = new CryptographicWebSocketMaskKeySource();
        }

        return options;
    }
}

internal class AllowAllAutobahnOrigins : IWebSocketOriginPolicy
{
    public bool IsOriginAllowed(WebSocketHttpRequest request)
    {
        return request != null;
    }
}
