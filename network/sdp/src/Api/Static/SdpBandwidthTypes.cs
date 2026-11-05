namespace sdp;

/// <summary>
/// Defines registered SDP bandwidth-type tokens.
/// </summary>
public static class SdpBandwidthTypes
{
    /// <summary>The conference-total bandwidth token.</summary>
    public const string ConferenceTotal = "CT";

    /// <summary>The application-specific bandwidth token.</summary>
    public const string ApplicationSpecific = "AS";

    /// <summary>The RTCP sender bandwidth token.</summary>
    public const string RtcpSender = "RS";

    /// <summary>The RTCP receiver bandwidth token.</summary>
    public const string RtcpReceiver = "RR";

    /// <summary>The transport-independent application-specific maximum token.</summary>
    public const string TransportIndependentApplicationSpecific = "TIAS";
}
