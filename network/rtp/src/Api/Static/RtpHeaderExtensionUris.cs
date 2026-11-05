namespace rtp;

/// <summary>
/// Defines standardized RTP header-extension URIs supported by typed helpers.
/// </summary>
public static class RtpHeaderExtensionUris
{
    /// <summary>The URI for the RFC 6051 64-bit NTP timestamp extension.</summary>
    public const string Ntp64 = "urn:ietf:params:rtp-hdrext:ntp-64";

    /// <summary>The URI for the RFC 6464 client-to-mixer audio level extension.</summary>
    public const string SsrcAudioLevel = "urn:ietf:params:rtp-hdrext:ssrc-audio-level";

    /// <summary>The URI for the RFC 6465 mixer-to-client audio level extension.</summary>
    public const string CsrcAudioLevel = "urn:ietf:params:rtp-hdrext:csrc-audio-level";

    /// <summary>The URI for the media identification extension.</summary>
    public const string MediaIdentifier = "urn:ietf:params:rtp-hdrext:sdes:mid";

    /// <summary>The URI for the RTP stream identifier extension.</summary>
    public const string RtpStreamIdentifier = "urn:ietf:params:rtp-hdrext:sdes:rtp-stream-id";

    /// <summary>The URI for the repaired RTP stream identifier extension.</summary>
    public const string RepairedRtpStreamIdentifier = "urn:ietf:params:rtp-hdrext:sdes:repaired-rtp-stream-id";
}
