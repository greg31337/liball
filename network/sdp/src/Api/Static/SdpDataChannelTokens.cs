namespace sdp;

/// <summary>
/// Defines tokens used by SCTP data-channel SDP attributes.
/// </summary>
public static class SdpDataChannelTokens
{
    /// <summary>The WebRTC data-channel format token.</summary>
    public const string WebRtcDataChannel = "webrtc-datachannel";

    /// <summary>The default maximum received SCTP message size in bytes.</summary>
    public const long DefaultMaximumMessageSize = 65536;

    /// <summary>The data-channel subprotocol parameter token.</summary>
    public const string Subprotocol = "subprotocol";

    /// <summary>The data-channel label parameter token.</summary>
    public const string Label = "label";

    /// <summary>The maximum retransmission-count parameter token.</summary>
    public const string MaximumRetransmissions = "max-retr";

    /// <summary>The maximum retransmission-time parameter token.</summary>
    public const string MaximumRetransmissionTime = "max-time";

    /// <summary>The ordered-delivery parameter token.</summary>
    public const string Ordered = "ordered";

    /// <summary>The data-channel priority parameter token.</summary>
    public const string Priority = "priority";
}
