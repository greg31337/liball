using System.Collections.Generic;
using System.Net;

namespace diameter;

/// <summary>
/// Contains a Diameter message header and its ordered AVPs.
/// </summary>
public class DiameterMessage
{
    /// <summary>The Diameter protocol version.</summary>
    public byte Version = DiameterProtocolConstants.Version;

    /// <summary>The message header flags.</summary>
    public DiameterMessageFlags Flags;

    /// <summary>The 24-bit command code.</summary>
    public uint CommandCode;

    /// <summary>The application identifier from the message header.</summary>
    public uint ApplicationId;

    /// <summary>The Hop-by-Hop Identifier.</summary>
    public uint HopByHopIdentifier;

    /// <summary>The End-to-End Identifier.</summary>
    public uint EndToEndIdentifier;

    /// <summary>The ordered AVPs carried by the message.</summary>
    public List<DiameterAvp> Avps = new List<DiameterAvp>();
}

/// <summary>
/// Contains a Diameter AVP header and its owned payload bytes.
/// </summary>
public class DiameterAvp
{
    /// <summary>The AVP code.</summary>
    public uint Code;

    /// <summary>The AVP header flags.</summary>
    public DiameterAvpFlags Flags;

    /// <summary>The Vendor-ID when the vendor-specific flag is set.</summary>
    public uint VendorId;

    /// <summary>The owned, unpadded AVP payload bytes.</summary>
    public byte[] Data = new byte[0];
}

/// <summary>
/// Identifies a standard or vendor-specific authentication or accounting application.
/// </summary>
public class DiameterApplicationId
{
    /// <summary>The application identifier.</summary>
    public uint Id;

    /// <summary>The Vendor-ID, or zero for an IETF application.</summary>
    public uint VendorId;

    /// <summary>Indicates that the identifier represents an accounting application.</summary>
    public bool IsAccounting;
}

/// <summary>
/// Contains a Diameter node or realm identity.
/// </summary>
public class DiameterIdentity
{
    /// <summary>The identity text as supplied or decoded.</summary>
    public string Value = string.Empty;
}

/// <summary>
/// Contains the components of a DiameterURI value.
/// </summary>
public class DiameterUri
{
    /// <summary>The <c>aaa</c> or <c>aaas</c> URI scheme.</summary>
    public string Scheme = "aaa";

    /// <summary>The fully qualified host identity.</summary>
    public DiameterIdentity Host = new DiameterIdentity();

    /// <summary>The listener port.</summary>
    public int Port = DiameterProtocolConstants.Port;

    /// <summary>The URI transport parameter.</summary>
    public string Transport = "tcp";

    /// <summary>The URI AAA protocol parameter.</summary>
    public string Protocol = "diameter";
}

/// <summary>
/// Contains a resolved or configured Diameter transport endpoint.
/// </summary>
public class DiameterEndpoint
{
    /// <summary>The Diameter identity associated with the endpoint.</summary>
    public DiameterIdentity Identity = new DiameterIdentity();

    /// <summary>The configured DNS host name.</summary>
    public string Host = string.Empty;

    /// <summary>The resolved IP address, or <c>null</c> when unresolved.</summary>
    public IPAddress? Address;

    /// <summary>The transport port.</summary>
    public int Port = DiameterProtocolConstants.Port;

    /// <summary>The endpoint transport kind.</summary>
    public DiameterTransportKind Transport;

    /// <summary>The endpoint security mode.</summary>
    public DiameterSecurityMode Security;
}
