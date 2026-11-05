namespace diameter;

/// <summary>
/// Defines the flag bits in a Diameter message header.
/// </summary>
public enum DiameterMessageFlags
{
    /// <summary>Indicates that no message flags are set.</summary>
    None = 0,

    /// <summary>Identifies a request message.</summary>
    Request = 128,

    /// <summary>Permits the message to be proxied, relayed, or redirected.</summary>
    Proxyable = 64,

    /// <summary>Identifies an answer containing a protocol error.</summary>
    Error = 32,

    /// <summary>Indicates a potentially retransmitted request.</summary>
    PotentiallyRetransmitted = 16
}

/// <summary>
/// Defines the flag bits in a Diameter AVP header.
/// </summary>
public enum DiameterAvpFlags
{
    /// <summary>Indicates that no AVP flags are set.</summary>
    None = 0,

    /// <summary>Indicates that the AVP header contains a Vendor-ID.</summary>
    VendorSpecific = 128,

    /// <summary>Requires unsupported AVPs to be rejected.</summary>
    Mandatory = 64,

    /// <summary>Indicates a need for end-to-end security.</summary>
    Protected = 32
}

/// <summary>
/// Identifies a Diameter AVP base data type.
/// </summary>
public enum DiameterAvpBaseType
{
    /// <summary>Represents arbitrary bytes.</summary>
    OctetString = 0,

    /// <summary>Represents a signed 32-bit integer.</summary>
    Integer32 = 1,

    /// <summary>Represents a signed 64-bit integer.</summary>
    Integer64 = 2,

    /// <summary>Represents an unsigned 32-bit integer.</summary>
    Unsigned32 = 3,

    /// <summary>Represents an unsigned 64-bit integer.</summary>
    Unsigned64 = 4,

    /// <summary>Represents an IEEE 754 single-precision value.</summary>
    Float32 = 5,

    /// <summary>Represents an IEEE 754 double-precision value.</summary>
    Float64 = 6,

    /// <summary>Represents an ordered collection of AVPs.</summary>
    Grouped = 7
}

/// <summary>
/// Identifies the address family encoded in a Diameter Address value.
/// </summary>
public enum DiameterAddressFamily
{
    /// <summary>Identifies an IPv4 address.</summary>
    IPv4 = 1,

    /// <summary>Identifies an IPv6 address.</summary>
    IPv6 = 2
}

/// <summary>
/// Identifies the transport used by a Diameter connection.
/// </summary>
public enum DiameterTransportKind
{
    /// <summary>Uses Transmission Control Protocol.</summary>
    Tcp = 0,

    /// <summary>Uses Stream Control Transmission Protocol.</summary>
    Sctp = 1,

    /// <summary>Uses a transport supplied by the host application.</summary>
    External = 2
}

/// <summary>
/// Identifies the security applied to a Diameter transport.
/// </summary>
public enum DiameterSecurityMode
{
    /// <summary>Applies no transport security.</summary>
    None = 0,

    /// <summary>Applies Transport Layer Security.</summary>
    Tls = 1,

    /// <summary>Applies Datagram Transport Layer Security.</summary>
    Dtls = 2,

    /// <summary>Uses an externally established IPsec association.</summary>
    Ipsec = 3,

    /// <summary>Uses security supplied by the host application.</summary>
    External = 4
}

/// <summary>
/// Defines independently enabled roles for a Diameter node.
/// </summary>
public enum DiameterNodeRole
{
    /// <summary>Enables no role.</summary>
    None = 0,

    /// <summary>Enables request origination.</summary>
    Client = 1,

    /// <summary>Enables local request handling.</summary>
    Server = 2,

    /// <summary>Enables relay-agent behavior.</summary>
    Relay = 4,

    /// <summary>Enables proxy-agent behavior.</summary>
    Proxy = 8,

    /// <summary>Enables redirect-agent behavior.</summary>
    Redirect = 16,

    /// <summary>Enables translation-agent behavior.</summary>
    Translation = 32
}
