using System;

namespace diameter;

/// <summary>
/// Integrates and validates a transport factory supplied by the host application.
/// </summary>
public class DiameterExternalTransportFactory : IDiameterTransportFactory
{
    internal IDiameterTransportFactory OwnedFactory;

    /// <summary>Initializes an adapter that owns the supplied transport factory results.</summary>
    /// <param name="factory">The host-provided transport factory.</param>
    public DiameterExternalTransportFactory(IDiameterTransportFactory factory)
    {
        if (factory == null)
        {
            throw new ArgumentException("An external transport factory is required.");
        }

        OwnedFactory = factory;
    }

    /// <summary>Probes and validates the capabilities reported by the host transport.</summary>
    /// <param name="transport">The requested transport configuration.</param>
    /// <param name="security">The requested transport security configuration.</param>
    /// <returns>The validated host transport support outcome.</returns>
    public DiameterTransportSupportResult ProbeDiameterTransportSupport(
        DiameterTransportConfiguration? transport,
        DiameterSecurityConfiguration? security)
    {
        DiameterTransportSupportResult? configurationFailure =
            ValidateExternalConfiguration(transport, security);
        if (configurationFailure != null)
        {
            return configurationFailure;
        }

        DiameterTransportSupportResult? supplied =
            OwnedFactory.ProbeDiameterTransportSupport(transport, security);
        if (supplied == null)
        {
            return CreateSupportFailure(
                DiameterTransportStatus.Failed,
                "external-probe-result-invalid",
                "The host transport returned no probe result.");
        }

        if (supplied.Status != DiameterTransportStatus.Success)
        {
            return CloneSupportResult(supplied);
        }

        if (transport == null || security == null || supplied.Metadata == null ||
            !ValidateTransportMetadata(supplied.Metadata, transport, security))
        {
            return CreateSupportFailure(
                DiameterTransportStatus.Failed,
                "external-capability-invalid",
                "The host transport reported capabilities inconsistent with the configuration.");
        }

        if ((transport.EnableActiveConnections &&
            !supplied.Metadata.SupportsActiveConnections) ||
            (transport.EnableListener && !supplied.Metadata.SupportsListeners))
        {
            DiameterTransportSupportResult result = CreateSupportFailure(
                DiameterTransportStatus.Unsupported,
                "external-capability-unsupported",
                "The host transport does not support an enabled connection role.");
            result.Metadata = QueuedDiameterConnection.CloneTransportMetadata(
                supplied.Metadata);
            return result;
        }

        return CloneSupportResult(supplied);
    }

    /// <summary>Creates a validating listener around a host-provided listener.</summary>
    /// <param name="transport">The requested listener transport configuration.</param>
    /// <param name="security">The security required for accepted connections.</param>
    /// <returns>The validated listener creation outcome.</returns>
    public DiameterListenerResult CreateDiameterListener(
        DiameterTransportConfiguration? transport,
        DiameterSecurityConfiguration? security)
    {
        DiameterTransportSupportResult support = ProbeDiameterTransportSupport(
            transport,
            security);
        if (support.Status != DiameterTransportStatus.Success)
        {
            return CreateListenerFailure(support.Status, support.Code, support.Message);
        }

        if (transport == null || security == null || !transport.EnableListener)
        {
            return CreateListenerFailure(
                DiameterTransportStatus.InvalidConfiguration,
                "listener-configuration-invalid",
                "An enabled external listener configuration is required.");
        }

        DiameterListenerResult? supplied = OwnedFactory.CreateDiameterListener(
            transport,
            security);
        if (supplied == null)
        {
            return CreateListenerFailure(
                DiameterTransportStatus.Failed,
                "external-listener-result-invalid",
                "The host transport returned no listener result.");
        }

        if (supplied.Status != DiameterTransportStatus.Success)
        {
            if (supplied.Listener != null)
            {
                supplied.Listener.CloseDiameterListener();
            }

            return CloneListenerResult(supplied);
        }

        if (supplied.Listener == null)
        {
            return CreateListenerFailure(
                DiameterTransportStatus.Failed,
                "external-listener-result-invalid",
                "The host transport reported success without a listener.");
        }

        DiameterListenerMetadata? metadata =
            supplied.Listener.ReadDiameterListenerMetadata();
        if (metadata == null || !metadata.Open || metadata.Transport == null ||
            metadata.LocalEndpoint == null || metadata.SecurityMode != security.Mode ||
            !ValidateTransportMetadata(metadata.Transport, transport, security))
        {
            supplied.Listener.CloseDiameterListener();
            return CreateListenerFailure(
                DiameterTransportStatus.Failed,
                "external-listener-metadata-invalid",
                "The host listener metadata is inconsistent with the configuration.");
        }

        ExternalDiameterListener listener = new ExternalDiameterListener(
            this,
            supplied.Listener,
            CloneTransportConfiguration(transport),
            CloneSecurityConfiguration(security),
            metadata);
        DiameterListenerResult result = new DiameterListenerResult();
        result.Listener = listener;
        result.Metadata = listener.ReadDiameterListenerMetadata();
        return result;
    }

    /// <summary>Creates and validates an active host-provided transport connection.</summary>
    /// <param name="endpoint">The remote transport endpoint.</param>
    /// <param name="transport">The requested local transport configuration.</param>
    /// <param name="security">The security required for the connection.</param>
    /// <param name="timeoutMilliseconds">The positive connection timeout.</param>
    /// <returns>The validated active connection outcome.</returns>
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
            return CreateConnectionFailure(support.Status, support.Code, support.Message);
        }

        if (endpoint == null || endpoint.Identity == null || transport == null ||
            security == null || timeoutMilliseconds <= 0 ||
            !transport.EnableActiveConnections || endpoint.Port < 0 ||
            endpoint.Port > 65535 ||
            (transport.Kind != DiameterTransportKind.External && endpoint.Port == 0))
        {
            return CreateConnectionFailure(
                DiameterTransportStatus.InvalidConfiguration,
                "connection-configuration-invalid",
                "A valid endpoint, enabled transport, and positive timeout are required.");
        }

        DiameterConnectionResult? supplied = OwnedFactory.ConnectDiameterEndpoint(
            endpoint,
            transport,
            security,
            timeoutMilliseconds);
        return IntegrateConnectionResult(
            supplied,
            transport,
            security,
            endpoint,
            DiameterConnectionDirection.Outbound);
    }

    internal DiameterConnectionResult IntegrateConnectionResult(
        DiameterConnectionResult? supplied,
        DiameterTransportConfiguration transport,
        DiameterSecurityConfiguration security,
        DiameterEndpoint? endpoint,
        DiameterConnectionDirection direction)
    {
        if (supplied == null)
        {
            return CreateConnectionFailure(
                DiameterTransportStatus.Failed,
                "external-connection-result-invalid",
                "The host transport returned no connection result.");
        }

        if (supplied.Status != DiameterTransportStatus.Success)
        {
            if (supplied.Connection != null)
            {
                supplied.Connection.CloseDiameterConnection();
            }

            return CloneConnectionResult(supplied);
        }

        if (supplied.Connection == null)
        {
            return CreateConnectionFailure(
                DiameterTransportStatus.Failed,
                "external-connection-result-invalid",
                "The host transport reported success without a connection.");
        }

        DiameterConnectionMetadata? metadata =
            supplied.Connection.ReadDiameterConnectionMetadata();
        if (metadata == null || !ValidateConnectionMetadata(
            metadata,
            transport,
            security,
            direction))
        {
            supplied.Connection.CloseDiameterConnection();
            return CreateConnectionFailure(
                DiameterTransportStatus.Failed,
                "external-connection-metadata-invalid",
                "The host connection metadata is inconsistent with the configuration.");
        }

        DiameterPeerAdmissionResult admission = AuthorizeExternalPeer(
            metadata,
            security,
            endpoint);
        if (!admission.Admitted)
        {
            supplied.Connection.CloseDiameterConnection();
            DiameterConnectionResult rejected = CreateConnectionFailure(
                DiameterTransportStatus.AuthenticationFailed,
                admission.Code,
                admission.Message);
            rejected.Metadata = QueuedDiameterConnection.CloneConnectionMetadata(metadata);
            rejected.Metadata.Open = false;
            return rejected;
        }

        ExternalDiameterConnection connection = new ExternalDiameterConnection(
            supplied.Connection,
            metadata);
        DiameterConnectionResult result = new DiameterConnectionResult();
        result.Connection = connection;
        result.Metadata = connection.ReadDiameterConnectionMetadata();
        return result;
    }

    internal DiameterPeerAdmissionResult AuthorizeExternalPeer(
        DiameterConnectionMetadata metadata,
        DiameterSecurityConfiguration security,
        DiameterEndpoint? endpoint)
    {
        DiameterIdentity identity = ReadExternalPeerIdentity(metadata, security, endpoint);
        DiameterIdentity realm = ReadExternalPeerRealm(identity, security);
        if (security.Mode == DiameterSecurityMode.Tls ||
            security.Mode == DiameterSecurityMode.Dtls)
        {
            return TlsCertificateSupport.AuthorizeTlsPeer(
                metadata,
                security,
                identity,
                realm);
        }

        if (security.PeerAdmissionPolicy == null)
        {
            return TlsCertificateSupport.CreatePeerAdmissionDecision(
                true,
                string.Empty,
                string.Empty);
        }

        DiameterPeerAdmissionResult? supplied =
            security.PeerAdmissionPolicy.EvaluatePeerAdmission(
                TlsCertificateSupport.CreatePeerAdmissionRequest(
                    metadata,
                    identity,
                    realm));
        return TlsCertificateSupport.NormalizePeerAdmissionDecision(
            supplied,
            "peer-admission-rejected",
            "The peer admission policy rejected the external connection.");
    }

    internal DiameterIdentity ReadExternalPeerIdentity(
        DiameterConnectionMetadata metadata,
        DiameterSecurityConfiguration security,
        DiameterEndpoint? endpoint)
    {
        DiameterIdentity result = new DiameterIdentity();
        if (!string.IsNullOrWhiteSpace(security.ExpectedPeerIdentity.Value))
        {
            result.Value = security.ExpectedPeerIdentity.Value;
        }
        else if (endpoint != null && endpoint.Identity != null &&
            !string.IsNullOrWhiteSpace(endpoint.Identity.Value))
        {
            result.Value = endpoint.Identity.Value;
        }
        else if (!string.IsNullOrWhiteSpace(security.ExpectedPeerName))
        {
            result.Value = security.ExpectedPeerName;
        }
        else if (metadata.RemoteEndpoint != null &&
            metadata.RemoteEndpoint.Identity != null &&
            !string.IsNullOrWhiteSpace(metadata.RemoteEndpoint.Identity.Value))
        {
            result.Value = metadata.RemoteEndpoint.Identity.Value;
        }
        else if (metadata.RemoteEndpoint != null)
        {
            result.Value = metadata.RemoteEndpoint.Host;
        }

        DiameterValueResult normalized = ValueCodecs.NormalizeDiameterIdentity(result);
        if (normalized.Status == DiameterCodecStatus.Success)
        {
            result.Value = normalized.Text;
        }

        return result;
    }

    internal DiameterIdentity ReadExternalPeerRealm(
        DiameterIdentity identity,
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

        return TlsCertificateSupport.DeriveDiameterRealm(identity);
    }

    internal bool ValidateConnectionMetadata(
        DiameterConnectionMetadata metadata,
        DiameterTransportConfiguration transport,
        DiameterSecurityConfiguration security,
        DiameterConnectionDirection direction)
    {
        if (!metadata.Open || string.IsNullOrWhiteSpace(metadata.ConnectionId) ||
            metadata.LocalEndpoint == null || metadata.RemoteEndpoint == null ||
            metadata.LocalEndpoint.Identity == null ||
            metadata.RemoteEndpoint.Identity == null || metadata.Transport == null ||
            metadata.Security == null || metadata.Direction != direction ||
            !ValidateTransportMetadata(metadata.Transport, transport, security) ||
            metadata.Security.Mode != security.Mode ||
            metadata.LocalEndpoint.Transport != metadata.Transport.Kind ||
            metadata.RemoteEndpoint.Transport != metadata.Transport.Kind ||
            metadata.LocalEndpoint.Security != security.Mode ||
            metadata.RemoteEndpoint.Security != security.Mode ||
            metadata.InboundStream < 0 || metadata.OutboundStream < 0 ||
            metadata.InboundStream >= metadata.Transport.MaximumInboundStreams ||
            metadata.OutboundStream >= metadata.Transport.MaximumOutboundStreams)
        {
            return false;
        }

        if (security.Mode == DiameterSecurityMode.None)
        {
            return !metadata.Security.Authenticated && !metadata.Security.Encrypted &&
                !metadata.Security.MutuallyAuthenticated;
        }

        if (!metadata.Security.Authenticated || !metadata.Security.Encrypted ||
            string.IsNullOrWhiteSpace(metadata.Security.Protocol))
        {
            return false;
        }

        if (security.Mode == DiameterSecurityMode.Tls ||
            security.Mode == DiameterSecurityMode.Dtls)
        {
            return metadata.Security.MutuallyAuthenticated &&
                !string.IsNullOrWhiteSpace(metadata.Security.PeerCertificateThumbprint) &&
                metadata.Security.PeerCertificateDnsNames != null &&
                metadata.Security.PeerCertificateDnsNames.Count > 0;
        }

        return true;
    }

    internal bool ValidateTransportMetadata(
        DiameterTransportMetadata metadata,
        DiameterTransportConfiguration transport,
        DiameterSecurityConfiguration security)
    {
        if (metadata.MaximumInboundStreams <= 0 ||
            metadata.MaximumOutboundStreams <= 0 ||
            metadata.Kind != transport.Kind)
        {
            return false;
        }

        if (metadata.Kind == DiameterTransportKind.Tcp)
        {
            return !metadata.MessageOriented &&
                metadata.MaximumInboundStreams == 1 &&
                metadata.MaximumOutboundStreams == 1 &&
                metadata.PayloadProtocolIdentifier == 0;
        }

        if (metadata.Kind == DiameterTransportKind.Sctp)
        {
            uint expectedIdentifier =
                DiameterProtocolConstants.SctpPayloadProtocolIdentifier;
            if (security.Mode == DiameterSecurityMode.Dtls)
            {
                expectedIdentifier =
                    DiameterProtocolConstants.DtlsSctpPayloadProtocolIdentifier;
            }

            return metadata.MessageOriented &&
                metadata.MaximumInboundStreams >= transport.SctpStreamCount &&
                metadata.MaximumOutboundStreams >= transport.SctpStreamCount &&
                metadata.PayloadProtocolIdentifier == expectedIdentifier;
        }

        return metadata.Kind == DiameterTransportKind.External;
    }

    internal DiameterTransportSupportResult? ValidateExternalConfiguration(
        DiameterTransportConfiguration? transport,
        DiameterSecurityConfiguration? security)
    {
        if (transport == null || security == null)
        {
            return CreateSupportFailure(
                DiameterTransportStatus.InvalidConfiguration,
                "configuration-required",
                "External transport and security configurations are required.");
        }

        bool validKind = transport.Kind == DiameterTransportKind.Tcp ||
            transport.Kind == DiameterTransportKind.Sctp ||
            transport.Kind == DiameterTransportKind.External;
        bool validMode = security.Mode == DiameterSecurityMode.None ||
            security.Mode == DiameterSecurityMode.Tls ||
            security.Mode == DiameterSecurityMode.Dtls ||
            security.Mode == DiameterSecurityMode.Ipsec ||
            security.Mode == DiameterSecurityMode.External;
        bool validPort = transport.Port >= 0 && transport.Port <= 65535 &&
            (transport.Kind == DiameterTransportKind.External || transport.Port > 0);
        bool validFamilies = transport.Kind == DiameterTransportKind.External ||
            transport.EnableIpv4 || transport.EnableIpv6;
        bool validSizes =
            transport.ReceiveBufferSize >= DiameterProtocolConstants.MessageHeaderSize &&
            transport.SendBufferSize >= DiameterProtocolConstants.MessageHeaderSize &&
            transport.ListenBacklog > 0 && transport.SctpStreamCount > 0 &&
            (transport.Kind == DiameterTransportKind.Sctp ||
            transport.SctpStreamCount == 1) &&
            (transport.Kind != DiameterTransportKind.Sctp ||
            transport.SctpStreamCount <= ushort.MaxValue);
        if (!validKind || !validMode || !validPort || !validFamilies || !validSizes)
        {
            return CreateSupportFailure(
                DiameterTransportStatus.InvalidConfiguration,
                "external-configuration-invalid",
                "The external transport configuration is invalid.");
        }

        bool certificateMode = security.Mode == DiameterSecurityMode.Tls ||
            security.Mode == DiameterSecurityMode.Dtls;
        bool hasCertificateSettings =
            !string.IsNullOrWhiteSpace(security.LocalCertificatePath) ||
            !string.IsNullOrWhiteSpace(security.LocalCertificatePassword) ||
            !string.IsNullOrWhiteSpace(security.TrustAnchorPath) ||
            !string.IsNullOrWhiteSpace(security.ExpectedPeerName) ||
            security.RequirePeerCertificate || security.CheckCertificateRevocation ||
            security.CertificatePeerPolicy != null;
        bool validSecurity =
            ((security.Mode == DiameterSecurityMode.None &&
            security.AllowUnsecuredTransport) ||
            (security.Mode != DiameterSecurityMode.None &&
            !security.AllowUnsecuredTransport)) &&
            ((!certificateMode && !hasCertificateSettings) ||
            (certificateMode &&
            !string.IsNullOrWhiteSpace(security.LocalCertificatePath) &&
            security.RequirePeerCertificate)) &&
            (security.Mode != DiameterSecurityMode.Tls ||
            transport.Kind == DiameterTransportKind.Tcp) &&
            (security.Mode != DiameterSecurityMode.Dtls ||
            transport.Kind == DiameterTransportKind.Sctp) &&
            (security.Mode != DiameterSecurityMode.External ||
            transport.Kind == DiameterTransportKind.External);
        if (!validSecurity)
        {
            return CreateSupportFailure(
                DiameterTransportStatus.InvalidConfiguration,
                "external-security-invalid",
                "The external security configuration is invalid.");
        }

        if (security.ExpectedPeerIdentity == null ||
            security.ExpectedPeerRealm == null)
        {
            return CreateSupportFailure(
                DiameterTransportStatus.InvalidConfiguration,
                "external-peer-identity-invalid",
                "External peer identity objects are required.");
        }

        if ((!string.IsNullOrWhiteSpace(security.ExpectedPeerIdentity.Value) &&
            ValueCodecs.NormalizeDiameterIdentity(security.ExpectedPeerIdentity).Status !=
                DiameterCodecStatus.Success) ||
            (!string.IsNullOrWhiteSpace(security.ExpectedPeerRealm.Value) &&
            ValueCodecs.NormalizeDiameterRealm(security.ExpectedPeerRealm).Status !=
                DiameterCodecStatus.Success))
        {
            return CreateSupportFailure(
                DiameterTransportStatus.InvalidConfiguration,
                "external-peer-identity-invalid",
                "The external peer identity configuration is invalid.");
        }

        return null;
    }

    internal DiameterTransportConfiguration CloneTransportConfiguration(
        DiameterTransportConfiguration source)
    {
        DiameterTransportConfiguration result = new DiameterTransportConfiguration();
        result.Kind = source.Kind;
        result.Host = source.Host;
        result.Port = source.Port;
        result.EnableIpv4 = source.EnableIpv4;
        result.EnableIpv6 = source.EnableIpv6;
        result.EnableListener = source.EnableListener;
        result.EnableActiveConnections = source.EnableActiveConnections;
        result.NoDelay = source.NoDelay;
        result.KeepAlive = source.KeepAlive;
        result.ReceiveBufferSize = source.ReceiveBufferSize;
        result.SendBufferSize = source.SendBufferSize;
        result.ListenBacklog = source.ListenBacklog;
        result.SctpStreamCount = source.SctpStreamCount;
        return result;
    }

    internal DiameterSecurityConfiguration CloneSecurityConfiguration(
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

    internal DiameterTransportSupportResult CreateSupportFailure(
        DiameterTransportStatus status,
        string code,
        string message)
    {
        DiameterTransportSupportResult result = new DiameterTransportSupportResult();
        result.Status = status;
        result.Code = code;
        result.Message = message;
        return result;
    }

    internal DiameterListenerResult CreateListenerFailure(
        DiameterTransportStatus status,
        string code,
        string message)
    {
        DiameterListenerResult result = new DiameterListenerResult();
        result.Status = status;
        result.Code = code;
        result.Message = message;
        return result;
    }

    internal DiameterConnectionResult CreateConnectionFailure(
        DiameterTransportStatus status,
        string code,
        string message)
    {
        DiameterConnectionResult result = new DiameterConnectionResult();
        result.Status = status;
        result.Code = code;
        result.Message = message;
        return result;
    }

    internal DiameterTransportSupportResult CloneSupportResult(
        DiameterTransportSupportResult source)
    {
        DiameterTransportSupportResult result = new DiameterTransportSupportResult();
        result.Status = source.Status;
        if (source.Metadata != null)
        {
            result.Metadata = QueuedDiameterConnection.CloneTransportMetadata(
                source.Metadata);
        }

        result.Code = source.Code;
        result.Message = source.Message;
        return result;
    }

    internal DiameterListenerResult CloneListenerResult(DiameterListenerResult source)
    {
        DiameterListenerResult result = new DiameterListenerResult();
        result.Status = source.Status;
        result.Code = source.Code;
        result.Message = source.Message;
        if (source.Metadata != null && source.Metadata.LocalEndpoint != null &&
            source.Metadata.Transport != null)
        {
            result.Metadata = TcpSocketDiameterListener.CloneListenerMetadata(
                source.Metadata);
        }

        return result;
    }

    internal DiameterConnectionResult CloneConnectionResult(
        DiameterConnectionResult source)
    {
        DiameterConnectionResult result = new DiameterConnectionResult();
        result.Status = source.Status;
        result.Code = source.Code;
        result.Message = source.Message;
        if (source.Metadata != null && source.Metadata.LocalEndpoint != null &&
            source.Metadata.RemoteEndpoint != null && source.Metadata.Transport != null &&
            source.Metadata.Security != null)
        {
            result.Metadata = QueuedDiameterConnection.CloneConnectionMetadata(
                source.Metadata);
        }

        return result;
    }
}
