namespace sdp;

/// <summary>
/// Defines transport-protocol tokens used by common SDP profiles.
/// </summary>
public static class SdpTransportProtocols
{
    /// <summary>The User Datagram Protocol token.</summary>
    public const string Udp = "udp";

    /// <summary>The Transmission Control Protocol token.</summary>
    public const string Tcp = "TCP";

    /// <summary>The RTP audio/video profile token.</summary>
    public const string RtpAvp = "RTP/AVP";

    /// <summary>The RTP audio/video profile with feedback token.</summary>
    public const string RtpAvpf = "RTP/AVPF";

    /// <summary>The secure RTP audio/video profile token.</summary>
    public const string RtpSavp = "RTP/SAVP";

    /// <summary>The secure RTP audio/video profile with feedback token.</summary>
    public const string RtpSavpf = "RTP/SAVPF";

    /// <summary>The TCP-framed RTP audio/video profile token.</summary>
    public const string TcpRtpAvp = "TCP/RTP/AVP";

    /// <summary>The TCP-framed RTP audio/video profile with feedback token.</summary>
    public const string TcpRtpAvpf = "TCP/RTP/AVPF";

    /// <summary>The UDP, TLS, and secure RTP profile token.</summary>
    public const string UdpTlsRtpSavp = "UDP/TLS/RTP/SAVP";

    /// <summary>The UDP, TLS, and secure RTP feedback profile token.</summary>
    public const string UdpTlsRtpSavpf = "UDP/TLS/RTP/SAVPF";

    /// <summary>The TCP, DTLS, and secure RTP profile token.</summary>
    public const string TcpDtlsRtpSavp = "TCP/DTLS/RTP/SAVP";

    /// <summary>The TCP, DTLS, and secure RTP feedback profile token.</summary>
    public const string TcpDtlsRtpSavpf = "TCP/DTLS/RTP/SAVPF";

    /// <summary>The UDP, DTLS, and SCTP data-channel token.</summary>
    public const string UdpDtlsSctp = "UDP/DTLS/SCTP";

    /// <summary>The TCP, DTLS, and SCTP data-channel token.</summary>
    public const string TcpDtlsSctp = "TCP/DTLS/SCTP";

    /// <summary>The TCP BFCP token.</summary>
    public const string TcpBfcp = "TCP/BFCP";

    /// <summary>The DTLS-protected TCP BFCP token.</summary>
    public const string TcpDtlsBfcp = "TCP/DTLS/BFCP";

    /// <summary>The TLS-protected TCP BFCP token.</summary>
    public const string TcpTlsBfcp = "TCP/TLS/BFCP";

    /// <summary>The UDP BFCP token.</summary>
    public const string UdpBfcp = "UDP/BFCP";

    /// <summary>The DTLS-protected UDP BFCP token.</summary>
    public const string UdpTlsBfcp = "UDP/TLS/BFCP";
}
