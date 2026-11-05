using System;
using System.Collections.Generic;

namespace diameter;

/// <summary>
/// Describes the capabilities of a transport implementation.
/// </summary>
public class DiameterTransportMetadata
{
    /// <summary>The transport kind.</summary>
    public DiameterTransportKind Kind;

    /// <summary>Indicates that active outbound connections are supported.</summary>
    public bool SupportsActiveConnections;

    /// <summary>Indicates that passive listeners are supported.</summary>
    public bool SupportsListeners;

    /// <summary>Indicates that IPv4 endpoints are supported.</summary>
    public bool SupportsIpv4;

    /// <summary>Indicates that IPv6 endpoints are supported.</summary>
    public bool SupportsIpv6;

    /// <summary>Indicates that the transport preserves message boundaries.</summary>
    public bool MessageOriented;

    /// <summary>The maximum supported inbound stream count.</summary>
    public int MaximumInboundStreams = 1;

    /// <summary>The maximum supported outbound stream count.</summary>
    public int MaximumOutboundStreams = 1;

    /// <summary>The transport payload protocol identifier, or zero when not applicable.</summary>
    public uint PayloadProtocolIdentifier;
}

/// <summary>
/// Describes the negotiated security of a transport connection.
/// </summary>
public class DiameterSecurityMetadata
{
    /// <summary>The configured security mode.</summary>
    public DiameterSecurityMode Mode;

    /// <summary>Indicates that the remote endpoint was authenticated.</summary>
    public bool Authenticated;

    /// <summary>Indicates that application data is encrypted.</summary>
    public bool Encrypted;

    /// <summary>Indicates that both endpoints were authenticated.</summary>
    public bool MutuallyAuthenticated;

    /// <summary>The negotiated security protocol name.</summary>
    public string Protocol = string.Empty;

    /// <summary>The negotiated cipher suite name.</summary>
    public string CipherSuite = string.Empty;

    /// <summary>The local certificate subject, or an empty string when absent.</summary>
    public string LocalCertificateSubject = string.Empty;

    /// <summary>The peer certificate subject, or an empty string when absent.</summary>
    public string PeerCertificateSubject = string.Empty;

    /// <summary>The peer certificate issuer, or an empty string when absent.</summary>
    public string PeerCertificateIssuer = string.Empty;

    /// <summary>The peer certificate thumbprint, or an empty string when absent.</summary>
    public string PeerCertificateThumbprint = string.Empty;

    /// <summary>The start of the peer certificate validity period.</summary>
    public DateTimeOffset PeerCertificateNotBefore;

    /// <summary>The end of the peer certificate validity period.</summary>
    public DateTimeOffset PeerCertificateNotAfter;

    /// <summary>The DNS identities carried by the peer certificate.</summary>
    public List<string> PeerCertificateDnsNames = new List<string>();
}

/// <summary>
/// Describes a passive transport listener.
/// </summary>
public class DiameterListenerMetadata
{
    /// <summary>The endpoint on which connections are accepted.</summary>
    public DiameterEndpoint LocalEndpoint = new DiameterEndpoint();

    /// <summary>The listener transport capabilities.</summary>
    public DiameterTransportMetadata Transport = new DiameterTransportMetadata();

    /// <summary>The security mode applied to accepted connections.</summary>
    public DiameterSecurityMode SecurityMode;

    /// <summary>Indicates that the listener is accepting connections.</summary>
    public bool Open;
}

/// <summary>
/// Describes an established transport connection.
/// </summary>
public class DiameterConnectionMetadata
{
    /// <summary>The implementation-assigned connection identifier.</summary>
    public string ConnectionId = string.Empty;

    /// <summary>The local transport endpoint.</summary>
    public DiameterEndpoint LocalEndpoint = new DiameterEndpoint();

    /// <summary>The remote transport endpoint.</summary>
    public DiameterEndpoint RemoteEndpoint = new DiameterEndpoint();

    /// <summary>The side that initiated the connection.</summary>
    public DiameterConnectionDirection Direction;

    /// <summary>The transport capabilities used by the connection.</summary>
    public DiameterTransportMetadata Transport = new DiameterTransportMetadata();

    /// <summary>The negotiated connection security.</summary>
    public DiameterSecurityMetadata Security = new DiameterSecurityMetadata();

    /// <summary>The selected inbound transport stream.</summary>
    public int InboundStream;

    /// <summary>The selected outbound transport stream.</summary>
    public int OutboundStream;

    /// <summary>The UTC time at which the connection was established.</summary>
    public DateTimeOffset EstablishedAt;

    /// <summary>Indicates that the connection can exchange data.</summary>
    public bool Open;
}

/// <summary>
/// Contains the outcome of probing transport support.
/// </summary>
public class DiameterTransportSupportResult
{
    /// <summary>The probe status.</summary>
    public DiameterTransportStatus Status;

    /// <summary>The supported transport capabilities.</summary>
    public DiameterTransportMetadata Metadata = new DiameterTransportMetadata();

    /// <summary>The stable machine-readable outcome code.</summary>
    public string Code = string.Empty;

    /// <summary>The human-readable outcome description.</summary>
    public string Message = string.Empty;
}

/// <summary>
/// Contains the outcome of creating a transport listener.
/// </summary>
public class DiameterListenerResult
{
    /// <summary>The listener creation status.</summary>
    public DiameterTransportStatus Status;

    /// <summary>The created listener, or <c>null</c> when creation failed.</summary>
    public IDiameterListener? Listener;

    /// <summary>The listener metadata.</summary>
    public DiameterListenerMetadata Metadata = new DiameterListenerMetadata();

    /// <summary>The stable machine-readable outcome code.</summary>
    public string Code = string.Empty;

    /// <summary>The human-readable outcome description.</summary>
    public string Message = string.Empty;
}

/// <summary>
/// Contains the outcome of connecting or accepting a transport connection.
/// </summary>
public class DiameterConnectionResult
{
    /// <summary>The connection operation status.</summary>
    public DiameterTransportStatus Status;

    /// <summary>The connected transport, or <c>null</c> when no connection was produced.</summary>
    public IDiameterConnection? Connection;

    /// <summary>The connection metadata.</summary>
    public DiameterConnectionMetadata Metadata = new DiameterConnectionMetadata();

    /// <summary>The stable machine-readable outcome code.</summary>
    public string Code = string.Empty;

    /// <summary>The human-readable outcome description.</summary>
    public string Message = string.Empty;
}

/// <summary>
/// Contains the outcome of one blocking transport input/output operation.
/// </summary>
public class DiameterTransportIoResult
{
    /// <summary>The input/output status.</summary>
    public DiameterTransportStatus Status;

    /// <summary>The number of octets transferred.</summary>
    public int TransferredCount;

    /// <summary>Indicates that the remote endpoint ended its input stream.</summary>
    public bool EndOfStream;

    /// <summary>The stable machine-readable outcome code.</summary>
    public string Code = string.Empty;

    /// <summary>The human-readable outcome description.</summary>
    public string Message = string.Empty;
}

/// <summary>
/// Contains the outcome of a transport lifecycle operation.
/// </summary>
public class DiameterTransportOperationResult
{
    /// <summary>The lifecycle operation status.</summary>
    public DiameterTransportStatus Status;

    /// <summary>The stable machine-readable outcome code.</summary>
    public string Code = string.Empty;

    /// <summary>The human-readable outcome description.</summary>
    public string Message = string.Empty;
}
