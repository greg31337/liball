using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace diameter;

/// <summary>Creates mutually authenticated TLS-over-TCP listeners and connections.</summary>
public class DiameterTlsTransportFactory : IDiameterTransportFactory
{
    internal const int CertificateLoadTimeoutMilliseconds = 10000;
    internal DiameterTcpTransportFactory TcpFactory = new DiameterTcpTransportFactory();

    /// <summary>Initializes a TLS-over-TCP transport factory.</summary>
    public DiameterTlsTransportFactory()
    {
    }

    /// <summary>Probes TLS-over-TCP support and validates certificate configuration.</summary>
    /// <param name="transport">The TCP transport configuration.</param>
    /// <param name="security">The mutual TLS security configuration.</param>
    /// <returns>The support outcome and TCP capability metadata.</returns>
    public DiameterTransportSupportResult ProbeDiameterTransportSupport(
        DiameterTransportConfiguration? transport,
        DiameterSecurityConfiguration? security)
    {
        DiameterTransportSupportResult result = new DiameterTransportSupportResult();
        result.Metadata.Kind = DiameterTransportKind.Tcp;
        result.Metadata.SupportsActiveConnections = true;
        result.Metadata.SupportsListeners = true;
        result.Metadata.SupportsIpv4 = Socket.OSSupportsIPv4;
        result.Metadata.SupportsIpv6 = Socket.OSSupportsIPv6;
        if (transport == null || security == null)
        {
            result.Status = DiameterTransportStatus.InvalidConfiguration;
            result.Code = "configuration-required";
            result.Message = "TCP transport and TLS security configurations are required.";
            return result;
        }

        if (transport.Kind != DiameterTransportKind.Tcp)
        {
            result.Status = DiameterTransportStatus.Unsupported;
            result.Code = "transport-unsupported";
            result.Message = "The TLS factory only supports TCP transport configuration.";
            return result;
        }

        if (security.Mode != DiameterSecurityMode.Tls)
        {
            result.Status = DiameterTransportStatus.Unsupported;
            result.Code = "security-unsupported";
            result.Message = "The TLS factory requires TLS security mode.";
            return result;
        }

        if (security.AllowUnsecuredTransport || !security.RequirePeerCertificate ||
            string.IsNullOrWhiteSpace(security.LocalCertificatePath))
        {
            result.Status = DiameterTransportStatus.InvalidConfiguration;
            result.Code = "tls-configuration-invalid";
            result.Message = "TLS requires a local certificate and mutual authentication.";
            return result;
        }

        if (security.ExpectedPeerIdentity == null || security.ExpectedPeerRealm == null ||
            (!string.IsNullOrWhiteSpace(security.ExpectedPeerIdentity.Value) &&
            ValueCodecs.NormalizeDiameterIdentity(security.ExpectedPeerIdentity).Status !=
                DiameterCodecStatus.Success) ||
            (!string.IsNullOrWhiteSpace(security.ExpectedPeerRealm.Value) &&
            ValueCodecs.NormalizeDiameterRealm(security.ExpectedPeerRealm).Status !=
                DiameterCodecStatus.Success))
        {
            result.Status = DiameterTransportStatus.InvalidConfiguration;
            result.Code = "peer-identity-configuration-invalid";
            result.Message = "The configured Diameter peer identity or realm is invalid.";
            return result;
        }

        if (!File.Exists(security.LocalCertificatePath))
        {
            result.Status = DiameterTransportStatus.InvalidConfiguration;
            result.Code = "local-certificate-not-found";
            result.Message = "The configured local TLS certificate does not exist.";
            return result;
        }

        if (!string.IsNullOrWhiteSpace(security.TrustAnchorPath) &&
            !File.Exists(security.TrustAnchorPath))
        {
            result.Status = DiameterTransportStatus.InvalidConfiguration;
            result.Code = "trust-anchor-not-found";
            result.Message = "The configured TLS trust anchor does not exist.";
            return result;
        }

        DiameterTransportSupportResult tcpSupport = TcpFactory.ProbeDiameterTransportSupport(
            transport,
            CreateUnderlyingTcpSecurity());
        result.Metadata = tcpSupport.Metadata;
        if (tcpSupport.Status != DiameterTransportStatus.Success)
        {
            result.Status = tcpSupport.Status;
            result.Code = tcpSupport.Code;
            result.Message = tcpSupport.Message;
        }

        return result;
    }

    /// <summary>Creates a passive mutual TLS listener and starts its authentication worker.</summary>
    /// <param name="transport">The listener TCP transport configuration.</param>
    /// <param name="security">The listener mutual TLS configuration.</param>
    /// <returns>The listener creation outcome.</returns>
    public DiameterListenerResult CreateDiameterListener(
        DiameterTransportConfiguration? transport,
        DiameterSecurityConfiguration? security)
    {
        DiameterTransportSupportResult support = ProbeDiameterTransportSupport(
            transport,
            security);
        if (support.Status != DiameterTransportStatus.Success)
        {
            return CreateTlsListenerFailure(support.Status, support.Code, support.Message);
        }

        if (transport == null || security == null || !transport.EnableListener)
        {
            return CreateTlsListenerFailure(
                DiameterTransportStatus.InvalidConfiguration,
                "listener-configuration-invalid",
                "An enabled TLS listener configuration is required.");
        }

        TlsCertificateLoadResult certificates =
            TlsCertificateSupport.LoadTlsCertificates(
                security,
                CertificateLoadTimeoutMilliseconds);
        X509Certificate2? localCertificate = certificates.LocalCertificate;
        if (certificates.Status != DiameterTransportStatus.Success ||
            localCertificate == null)
        {
            return CreateTlsListenerFailure(
                certificates.Status,
                certificates.Code,
                certificates.Message);
        }

        DiameterListenerResult tcpListener = TcpFactory.CreateDiameterListener(
            transport,
            CreateUnderlyingTcpSecurity());
        if (tcpListener.Status != DiameterTransportStatus.Success ||
            tcpListener.Listener == null)
        {
            DisposeLoadedTlsCertificates(certificates);
            return CreateTlsListenerFailure(
                tcpListener.Status,
                tcpListener.Code,
                tcpListener.Message);
        }

        DiameterListenerMetadata metadata =
            TcpSocketDiameterListener.CloneListenerMetadata(tcpListener.Metadata);
        metadata.SecurityMode = DiameterSecurityMode.Tls;
        metadata.LocalEndpoint.Security = DiameterSecurityMode.Tls;
        TlsDiameterListener listener = new TlsDiameterListener(
            tcpListener.Listener,
            CloneTlsSecurityConfiguration(security),
            localCertificate,
            certificates.TrustAnchor,
            metadata,
            transport.ListenBacklog);
        DiameterListenerResult result = new DiameterListenerResult();
        result.Listener = listener;
        result.Metadata = TcpSocketDiameterListener.CloneListenerMetadata(metadata);
        return result;
    }

    /// <summary>Connects TCP and completes mutual TLS authentication before returning.</summary>
    /// <param name="endpoint">The resolved or named remote endpoint.</param>
    /// <param name="transport">The local TCP transport configuration.</param>
    /// <param name="security">The client mutual TLS configuration.</param>
    /// <param name="timeoutMilliseconds">The positive total connection timeout.</param>
    /// <returns>The authenticated connection outcome.</returns>
    public DiameterConnectionResult ConnectDiameterEndpoint(
        DiameterEndpoint? endpoint,
        DiameterTransportConfiguration? transport,
        DiameterSecurityConfiguration? security,
        int timeoutMilliseconds)
    {
        DiameterTransportSupportResult support = ProbeDiameterTransportSupport(
            transport,
            security);
        if (support.Status != DiameterTransportStatus.Success)
        {
            return CreateTlsConnectionFailure(support.Status, support.Code, support.Message);
        }

        if (endpoint == null || transport == null || security == null ||
            timeoutMilliseconds <= 0)
        {
            return CreateTlsConnectionFailure(
                DiameterTransportStatus.InvalidConfiguration,
                "connection-input-invalid",
                "A remote endpoint and positive TLS connection timeout are required.");
        }

        long deadline = Environment.TickCount64 + timeoutMilliseconds;
        int remaining = QueuedDiameterConnection.ReadRemainingMilliseconds(deadline);
        TlsCertificateLoadResult certificates =
            TlsCertificateSupport.LoadTlsCertificates(security, remaining);
        X509Certificate2? localCertificate = certificates.LocalCertificate;
        if (certificates.Status != DiameterTransportStatus.Success ||
            localCertificate == null)
        {
            return CreateTlsConnectionFailure(
                certificates.Status,
                certificates.Code,
                certificates.Message);
        }

        remaining = QueuedDiameterConnection.ReadRemainingMilliseconds(deadline);
        DiameterConnectionResult tcpConnection = TcpFactory.ConnectDiameterEndpoint(
            endpoint,
            transport,
            CreateUnderlyingTcpSecurity(),
            remaining);
        if (tcpConnection.Status != DiameterTransportStatus.Success ||
            tcpConnection.Connection == null)
        {
            DisposeLoadedTlsCertificates(certificates);
            return CreateTlsConnectionFailure(
                tcpConnection.Status,
                tcpConnection.Code,
                tcpConnection.Message);
        }

        string expectedPeerName = ReadExpectedTlsPeerName(endpoint, security);
        if (string.IsNullOrWhiteSpace(expectedPeerName))
        {
            tcpConnection.Connection.CloseDiameterConnection();
            DisposeLoadedTlsCertificates(certificates);
            return CreateTlsConnectionFailure(
                DiameterTransportStatus.InvalidConfiguration,
                "expected-peer-name-required",
                "TLS requires an expected peer DNS name.");
        }

        remaining = QueuedDiameterConnection.ReadRemainingMilliseconds(deadline);
        DiameterIdentity peerIdentity = ReadExpectedDiameterPeerIdentity(
            endpoint,
            security,
            expectedPeerName);
        DiameterIdentity peerRealm = ReadExpectedDiameterPeerRealm(
            peerIdentity,
            security);
        DiameterConnectionResult result = TlsCertificateSupport.AuthenticateTlsClient(
            tcpConnection.Connection,
            tcpConnection.Metadata,
            security,
            localCertificate,
            certificates.TrustAnchor,
            expectedPeerName,
            peerIdentity,
            peerRealm,
            remaining);
        DisposeLoadedTlsCertificates(certificates);
        return result;
    }

    internal DiameterSecurityConfiguration CreateUnderlyingTcpSecurity()
    {
        DiameterSecurityConfiguration result = new DiameterSecurityConfiguration();
        result.AllowUnsecuredTransport = true;
        return result;
    }

    internal DiameterSecurityConfiguration CloneTlsSecurityConfiguration(
        DiameterSecurityConfiguration source)
    {
        DiameterSecurityConfiguration result = new DiameterSecurityConfiguration();
        result.Mode = source.Mode;
        result.LocalCertificatePath = source.LocalCertificatePath;
        result.LocalCertificatePassword = source.LocalCertificatePassword;
        result.TrustAnchorPath = source.TrustAnchorPath;
        result.ExpectedPeerName = source.ExpectedPeerName;
        result.ExpectedPeerIdentity.Value = source.ExpectedPeerIdentity.Value;
        result.ExpectedPeerRealm.Value = source.ExpectedPeerRealm.Value;
        result.PeerAdmissionPolicy = source.PeerAdmissionPolicy;
        result.CertificatePeerPolicy = source.CertificatePeerPolicy;
        result.RequirePeerCertificate = source.RequirePeerCertificate;
        result.CheckCertificateRevocation = source.CheckCertificateRevocation;
        result.AllowUnsecuredTransport = source.AllowUnsecuredTransport;
        return result;
    }

    internal void DisposeLoadedTlsCertificates(TlsCertificateLoadResult certificates)
    {
        if (certificates.LocalCertificate != null)
        {
            certificates.LocalCertificate.Dispose();
        }

        if (certificates.TrustAnchor != null)
        {
            certificates.TrustAnchor.Dispose();
        }
    }

    internal string ReadExpectedTlsPeerName(
        DiameterEndpoint endpoint,
        DiameterSecurityConfiguration security)
    {
        if (!string.IsNullOrWhiteSpace(security.ExpectedPeerName))
        {
            return security.ExpectedPeerName;
        }

        if (!string.IsNullOrWhiteSpace(endpoint.Host))
        {
            return endpoint.Host;
        }

        if (!string.IsNullOrWhiteSpace(endpoint.Identity.Value))
        {
            return endpoint.Identity.Value;
        }

        if (endpoint.Address != null)
        {
            return endpoint.Address.ToString();
        }

        return string.Empty;
    }

    internal DiameterIdentity ReadExpectedDiameterPeerIdentity(
        DiameterEndpoint endpoint,
        DiameterSecurityConfiguration security,
        string expectedPeerName)
    {
        DiameterIdentity result = new DiameterIdentity();
        if (!string.IsNullOrWhiteSpace(security.ExpectedPeerIdentity.Value))
        {
            result.Value = security.ExpectedPeerIdentity.Value;
        }
        else if (!string.IsNullOrWhiteSpace(endpoint.Identity.Value))
        {
            result.Value = endpoint.Identity.Value;
        }
        else
        {
            result.Value = expectedPeerName;
        }

        DiameterValueResult normalized = ValueCodecs.NormalizeDiameterIdentity(result);
        if (normalized.Status == DiameterCodecStatus.Success)
        {
            result.Value = normalized.Text;
        }

        return result;
    }

    internal DiameterIdentity ReadExpectedDiameterPeerRealm(
        DiameterIdentity peerIdentity,
        DiameterSecurityConfiguration security)
    {
        if (!string.IsNullOrWhiteSpace(security.ExpectedPeerRealm.Value))
        {
            DiameterIdentity result = new DiameterIdentity();
            result.Value = security.ExpectedPeerRealm.Value;
            DiameterValueResult normalized = ValueCodecs.NormalizeDiameterRealm(result);
            if (normalized.Status == DiameterCodecStatus.Success)
            {
                result.Value = normalized.Text;
            }

            return result;
        }

        return TlsCertificateSupport.DeriveDiameterRealm(peerIdentity);
    }

    internal DiameterListenerResult CreateTlsListenerFailure(
        DiameterTransportStatus status,
        string code,
        string message)
    {
        DiameterListenerResult result = new DiameterListenerResult();
        result.Status = status;
        result.Code = code;
        result.Message = message;
        result.Metadata.Transport.Kind = DiameterTransportKind.Tcp;
        result.Metadata.SecurityMode = DiameterSecurityMode.Tls;
        return result;
    }

    internal DiameterConnectionResult CreateTlsConnectionFailure(
        DiameterTransportStatus status,
        string code,
        string message)
    {
        DiameterConnectionResult result = new DiameterConnectionResult();
        result.Status = status;
        result.Code = code;
        result.Message = message;
        result.Metadata.Transport.Kind = DiameterTransportKind.Tcp;
        result.Metadata.Security.Mode = DiameterSecurityMode.Tls;
        return result;
    }
}
