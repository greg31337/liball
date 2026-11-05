namespace sip;

/// <summary>
/// Provides SIP transport tokens, ports, and protocol identifiers.
/// </summary>
public static class SipTransportNames
{
    /// <summary>The SIP protocol version token.</summary>
    public const string ProtocolVersion = "SIP/2.0";

    /// <summary>The UDP transport token.</summary>
    public const string Udp = "UDP";

    /// <summary>The TCP transport token.</summary>
    public const string Tcp = "TCP";

    /// <summary>The TLS-over-TCP transport token.</summary>
    public const string Tls = "TLS";

    /// <summary>The SCTP transport token.</summary>
    public const string Sctp = "SCTP";

    /// <summary>The TLS-over-SCTP transport token.</summary>
    public const string TlsSctp = "TLS-SCTP";

    /// <summary>The WebSocket transport token.</summary>
    public const string WebSocket = "WS";

    /// <summary>The secure WebSocket transport token.</summary>
    public const string SecureWebSocket = "WSS";

    /// <summary>The datagram TLS transport token.</summary>
    public const string DatagramTls = "DTLS";

    /// <summary>The default port for SIP over UDP or TCP.</summary>
    public const int SipPort = 5060;

    /// <summary>The default port for SIPS over TLS.</summary>
    public const int SipsPort = 5061;

    /// <summary>The WebSocket subprotocol token for SIP.</summary>
    public const string WebSocketSubprotocol = "sip";
}
