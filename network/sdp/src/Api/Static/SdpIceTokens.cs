namespace sdp;

/// <summary>
/// Defines token values used by SDP ICE attributes.
/// </summary>
public static class SdpIceTokens
{
    /// <summary>The host candidate-type token.</summary>
    public const string HostCandidate = "host";

    /// <summary>The server-reflexive candidate-type token.</summary>
    public const string ServerReflexiveCandidate = "srflx";

    /// <summary>The peer-reflexive candidate-type token.</summary>
    public const string PeerReflexiveCandidate = "prflx";

    /// <summary>The relayed candidate-type token.</summary>
    public const string RelayedCandidate = "relay";

    /// <summary>The UDP candidate-transport token.</summary>
    public const string UdpTransport = "UDP";

    /// <summary>The TCP candidate-transport token.</summary>
    public const string TcpTransport = "TCP";

    /// <summary>The active TCP candidate-type token.</summary>
    public const string ActiveTcp = "active";

    /// <summary>The passive TCP candidate-type token.</summary>
    public const string PassiveTcp = "passive";

    /// <summary>The simultaneous-open TCP candidate-type token.</summary>
    public const string SimultaneousOpenTcp = "so";

    /// <summary>The candidate-type field token.</summary>
    public const string CandidateType = "typ";

    /// <summary>The related-address candidate extension token.</summary>
    public const string RelatedAddress = "raddr";

    /// <summary>The related-port candidate extension token.</summary>
    public const string RelatedPort = "rport";

    /// <summary>The TCP candidate-type extension token.</summary>
    public const string TcpType = "tcptype";

    /// <summary>The trickle ICE option token.</summary>
    public const string Trickle = "trickle";

    /// <summary>The ICE version 2 option token.</summary>
    public const string Ice2 = "ice2";

    /// <summary>The ICE renomination option token.</summary>
    public const string Renomination = "renomination";
}
