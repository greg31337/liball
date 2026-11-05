namespace sip;

/// <summary>
/// Provides media types used by SIP messages and standard SIP event packages.
/// </summary>
public static class SipMediaTypes
{
    /// <summary>The media type for a complete SIP message.</summary>
    public const string Sip = "message/sip";

    /// <summary>The media type for a SIP message fragment.</summary>
    public const string SipFragment = "message/sipfrag";

    /// <summary>The media type for Session Description Protocol data.</summary>
    public const string Sdp = "application/sdp";

    /// <summary>The Trickle ICE SDP fragment media type.</summary>
    public const string TrickleIceSdpFragment =
        "application/trickle-ice-sdpfrag";

    /// <summary>The mixed multipart media type.</summary>
    public const string MultipartMixed = "multipart/mixed";

    /// <summary>The alternative multipart media type.</summary>
    public const string MultipartAlternative = "multipart/alternative";

    /// <summary>The related multipart media type.</summary>
    public const string MultipartRelated = "multipart/related";

    /// <summary>The signed multipart media type.</summary>
    public const string MultipartSigned = "multipart/signed";

    /// <summary>The encrypted multipart media type.</summary>
    public const string MultipartEncrypted = "multipart/encrypted";

    /// <summary>The Presence Information Data Format media type.</summary>
    public const string Pidf = "application/pidf+xml";

    /// <summary>The partial PIDF update media type.</summary>
    public const string PidfDiff = "application/pidf-diff+xml";

    /// <summary>The watcher information event body media type.</summary>
    public const string WatcherInfo = "application/watcherinfo+xml";

    /// <summary>The dialog information event body media type.</summary>
    public const string DialogInfo = "application/dialog-info+xml";

    /// <summary>The registration information event body media type.</summary>
    public const string RegistrationInfo = "application/reginfo+xml";

    /// <summary>The message-summary event body media type.</summary>
    public const string MessageSummary = "application/simple-message-summary";

    /// <summary>The message composition indication media type.</summary>
    public const string IsComposing = "application/im-iscomposing+xml";

    /// <summary>The resource lists media type.</summary>
    public const string ResourceLists = "application/resource-lists+xml";

    /// <summary>The resource list meta-information media type.</summary>
    public const string ResourceListMetaInformation = "application/rlmi+xml";

    /// <summary>The conference information event body media type.</summary>
    public const string ConferenceInfo = "application/conference-info+xml";

    /// <summary>The media-control XML media type.</summary>
    public const string MediaControl = "application/media_control+xml";

    /// <summary>The SIP recording metadata media type.</summary>
    public const string RecordingMetadata = "application/rs-metadata+xml";
}
