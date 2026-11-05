namespace sip;

/// <summary>Identifies a SIP transport.</summary>
public enum SipTransportKind
{
    /// <summary>No transport has been selected.</summary>
    Unknown = 0,
    /// <summary>User Datagram Protocol.</summary>
    Udp = 1,
    /// <summary>Transmission Control Protocol.</summary>
    Tcp = 2,
    /// <summary>Transport Layer Security over TCP.</summary>
    Tls = 3,
    /// <summary>SIP over WebSocket.</summary>
    WebSocket = 4,
    /// <summary>SIP over secure WebSocket.</summary>
    SecureWebSocket = 5,
    /// <summary>Stream Control Transmission Protocol.</summary>
    Sctp = 6,
    /// <summary>Datagram Transport Layer Security.</summary>
    Dtls = 7,
    /// <summary>An application-provided transport.</summary>
    Custom = 8
}
