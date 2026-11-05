namespace sdp;

/// <summary>
/// Defines standardized SDP attribute names supported by typed projections.
/// </summary>
public static class SdpAttributeNames
{
    /// <summary>The send-and-receive direction attribute.</summary>
    public const string SendReceive = "sendrecv";

    /// <summary>The send-only direction attribute.</summary>
    public const string SendOnly = "sendonly";

    /// <summary>The receive-only direction attribute.</summary>
    public const string ReceiveOnly = "recvonly";

    /// <summary>The inactive direction attribute.</summary>
    public const string Inactive = "inactive";

    /// <summary>The orientation attribute.</summary>
    public const string Orientation = "orient";

    /// <summary>The conference type attribute.</summary>
    public const string ConferenceType = "type";

    /// <summary>The character-set attribute.</summary>
    public const string CharacterSet = "charset";

    /// <summary>The session-description language attribute.</summary>
    public const string SessionDescriptionLanguage = "sdplang";

    /// <summary>The media language attribute.</summary>
    public const string Language = "lang";

    /// <summary>The frame-rate attribute.</summary>
    public const string FrameRate = "framerate";

    /// <summary>The quality attribute.</summary>
    public const string Quality = "quality";

    /// <summary>The generating-tool attribute.</summary>
    public const string Tool = "tool";

    /// <summary>The obsolete category attribute.</summary>
    public const string Category = "cat";

    /// <summary>The obsolete keywords attribute.</summary>
    public const string Keywords = "keywds";

    /// <summary>The RTSP control attribute.</summary>
    public const string Control = "control";

    /// <summary>The media-content attribute.</summary>
    public const string Content = "content";

    /// <summary>The media label attribute.</summary>
    public const string Label = "label";

    /// <summary>The recommended packet-time attribute.</summary>
    public const string PacketTime = "ptime";

    /// <summary>The maximum packet-time attribute.</summary>
    public const string MaximumPacketTime = "maxptime";

    /// <summary>The RTP payload mapping attribute.</summary>
    public const string RtpMap = "rtpmap";

    /// <summary>The media-format parameters attribute.</summary>
    public const string FormatParameters = "fmtp";

    /// <summary>The RTCP endpoint attribute.</summary>
    public const string Rtcp = "rtcp";

    /// <summary>The RTP and RTCP multiplexing attribute.</summary>
    public const string RtcpMux = "rtcp-mux";

    /// <summary>The RTP and RTCP multiplexing-only attribute.</summary>
    public const string RtcpMuxOnly = "rtcp-mux-only";

    /// <summary>The reduced-size RTCP attribute.</summary>
    public const string RtcpReducedSize = "rtcp-rsize";

    /// <summary>The RTCP feedback attribute.</summary>
    public const string RtcpFeedback = "rtcp-fb";

    /// <summary>The RTCP extended-reports attribute.</summary>
    public const string RtcpExtendedReports = "rtcp-xr";

    /// <summary>The synchronization-source attribute.</summary>
    public const string SynchronizationSource = "ssrc";

    /// <summary>The synchronization-source group attribute.</summary>
    public const string SynchronizationSourceGroup = "ssrc-group";

    /// <summary>The media-stream identification attribute.</summary>
    public const string MediaStreamId = "msid";

    /// <summary>The media-stream identification semantic attribute.</summary>
    public const string MediaStreamIdSemantic = "msid-semantic";

    /// <summary>The multicast source-filter attribute.</summary>
    public const string SourceFilter = "source-filter";

    /// <summary>The media grouping attribute.</summary>
    public const string Group = "group";

    /// <summary>The media identification attribute.</summary>
    public const string MediaId = "mid";

    /// <summary>The BUNDLE-only attribute.</summary>
    public const string BundleOnly = "bundle-only";

    /// <summary>The ICE username-fragment attribute.</summary>
    public const string IceUsernameFragment = "ice-ufrag";

    /// <summary>The ICE password attribute.</summary>
    public const string IcePassword = "ice-pwd";

    /// <summary>The ICE options attribute.</summary>
    public const string IceOptions = "ice-options";

    /// <summary>The ICE-lite attribute.</summary>
    public const string IceLite = "ice-lite";

    /// <summary>The ICE mismatch attribute.</summary>
    public const string IceMismatch = "ice-mismatch";

    /// <summary>The ICE candidate attribute.</summary>
    public const string Candidate = "candidate";

    /// <summary>The ICE remote-candidates attribute.</summary>
    public const string RemoteCandidates = "remote-candidates";

    /// <summary>The ICE end-of-candidates attribute.</summary>
    public const string EndOfCandidates = "end-of-candidates";

    /// <summary>The certificate fingerprint attribute.</summary>
    public const string Fingerprint = "fingerprint";

    /// <summary>The TLS or DTLS setup-role attribute.</summary>
    public const string Setup = "setup";

    /// <summary>The connection-reuse attribute.</summary>
    public const string Connection = "connection";

    /// <summary>The TLS connection identity attribute.</summary>
    public const string TlsId = "tls-id";

    /// <summary>The legacy security-description crypto attribute.</summary>
    public const string Crypto = "crypto";

    /// <summary>The RTP header-extension mapping attribute.</summary>
    public const string ExtMap = "extmap";

    /// <summary>The mixed RTP header-extension mapping attribute.</summary>
    public const string ExtMapAllowMixed = "extmap-allow-mixed";

    /// <summary>The RTP stream restriction identifier attribute.</summary>
    public const string Rid = "rid";

    /// <summary>The simulcast attribute.</summary>
    public const string Simulcast = "simulcast";

    /// <summary>The image constraints attribute.</summary>
    public const string ImageAttribute = "imageattr";

    /// <summary>The timestamp reference-clock attribute.</summary>
    public const string TimestampReferenceClock = "ts-refclk";

    /// <summary>The media-clock attribute.</summary>
    public const string MediaClock = "mediaclk";

    /// <summary>The SCTP port attribute.</summary>
    public const string SctpPort = "sctp-port";

    /// <summary>The maximum SCTP message-size attribute.</summary>
    public const string MaximumMessageSize = "max-message-size";

    /// <summary>The legacy SCTP mapping attribute.</summary>
    public const string SctpMap = "sctpmap";

    /// <summary>The data-channel mapping attribute.</summary>
    public const string DataChannelMap = "dcmap";

    /// <summary>The data-channel subprotocol attribute.</summary>
    public const string DataChannelSubprotocol = "dcsa";

    /// <summary>The BFCP floor-control role attribute.</summary>
    public const string FloorControl = "floorctrl";

    /// <summary>The BFCP conference identifier attribute.</summary>
    public const string ConferenceId = "confid";

    /// <summary>The BFCP user identifier attribute.</summary>
    public const string UserId = "userid";

    /// <summary>The BFCP floor identifier attribute.</summary>
    public const string FloorId = "floorid";

    /// <summary>The BFCP version attribute.</summary>
    public const string BfcpVersion = "bfcpver";
}
