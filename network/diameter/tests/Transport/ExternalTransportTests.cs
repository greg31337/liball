using System;

namespace diameter.Tests;

public class ExternalTransportTests
{
    [Test]
    public void HostProvidedDtlsSctpConnectionIsValidatedAndOwnedByAdapter()
    {
        ExternalTestFactory host = new ExternalTestFactory();
        host.Support = CreateSctpTransportMetadata(true, false);
        host.Connection = new ExternalTestConnection(
            CreateConnectionMetadata(
                DiameterTransportKind.Sctp,
                DiameterSecurityMode.Dtls,
                DiameterConnectionDirection.Outbound));
        IDiameterTransportFactory factory =
            new DiameterExternalTransportFactory(host);
        DiameterTransportConfiguration transport = CreateSctpTransportConfiguration();
        DiameterSecurityConfiguration security = CreateDtlsSecurityConfiguration();
        DiameterEndpoint endpoint = new DiameterEndpoint();
        endpoint.Identity.Value = "peer.example";
        endpoint.Host = "peer.example";
        endpoint.Port = DiameterProtocolConstants.SecurePort;
        endpoint.Transport = DiameterTransportKind.Sctp;
        endpoint.Security = DiameterSecurityMode.Dtls;

        DiameterTransportSupportResult support =
            factory.ProbeDiameterTransportSupport(transport, security);
        DiameterConnectionResult connected = factory.ConnectDiameterEndpoint(
            endpoint,
            transport,
            security,
            1000);

        Assert.That(support.Status, Is.EqualTo(DiameterTransportStatus.Success));
        Assert.That(support.Metadata.PayloadProtocolIdentifier,
            Is.EqualTo(DiameterProtocolConstants.DtlsSctpPayloadProtocolIdentifier));
        Assert.That(connected.Status, Is.EqualTo(DiameterTransportStatus.Success));
        Assert.That(connected.Connection, Is.Not.Null);
        Assert.That(connected.Connection, Is.Not.SameAs(host.Connection));
        Assert.That(connected.Metadata.Security.Mode,
            Is.EqualTo(DiameterSecurityMode.Dtls));
        Assert.That(connected.Metadata.Security.MutuallyAuthenticated, Is.True);

        byte[] outbound = new byte[] { 1, 2, 3 };
        DiameterTransportIoResult sent = connected.Connection!
            .SendDiameterConnectionBytes(outbound, 1000);
        byte[] inbound = new byte[4];
        DiameterTransportIoResult received = connected.Connection
            .ReceiveDiameterConnectionBytes(inbound, 1000);
        DiameterTransportOperationResult firstClose = connected.Connection
            .CloseDiameterConnection();
        DiameterTransportOperationResult secondClose = connected.Connection
            .CloseDiameterConnection();

        Assert.Multiple(() =>
        {
            Assert.That(sent.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(sent.TransferredCount, Is.EqualTo(3));
            Assert.That(host.Connection!.SentBytes, Is.EqualTo(outbound));
            Assert.That(received.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(received.TransferredCount, Is.EqualTo(4));
            Assert.That(inbound, Is.EqualTo(new byte[] { 9, 8, 7, 6 }));
            Assert.That(firstClose.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(secondClose.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(host.Connection.CloseCount, Is.EqualTo(1));
            Assert.That(connected.Connection.ReadDiameterConnectionMetadata().Open,
                Is.False);
        });
    }

    [Test]
    public void ExternalListenerValidatesAcceptedConnectionAndOwnsListenerClose()
    {
        DiameterTransportConfiguration transport =
            CreateExternalTransportConfiguration();
        DiameterSecurityConfiguration security =
            CreateExternalSecurityConfiguration();
        ExternalTestConnection hostConnection = new ExternalTestConnection(
            CreateConnectionMetadata(
                DiameterTransportKind.External,
                DiameterSecurityMode.External,
                DiameterConnectionDirection.Inbound));
        ExternalTestListener hostListener = new ExternalTestListener();
        hostListener.Metadata = CreateListenerMetadata(
            DiameterTransportKind.External,
            DiameterSecurityMode.External);
        hostListener.Connection = hostConnection;
        ExternalTestFactory host = new ExternalTestFactory();
        host.Support = CreateExternalTransportMetadata(false, true);
        host.Listener = hostListener;
        IDiameterTransportFactory factory =
            new DiameterExternalTransportFactory(host);

        DiameterListenerResult created = factory.CreateDiameterListener(
            transport,
            security);
        security.Mode = DiameterSecurityMode.None;
        DiameterConnectionResult accepted = created.Listener!
            .AcceptDiameterConnection(1000);
        DiameterTransportOperationResult firstClose = created.Listener
            .CloseDiameterListener();
        DiameterTransportOperationResult secondClose = created.Listener
            .CloseDiameterListener();

        Assert.Multiple(() =>
        {
            Assert.That(created.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(created.Listener, Is.Not.SameAs(hostListener));
            Assert.That(accepted.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(accepted.Connection, Is.Not.Null);
            Assert.That(accepted.Connection, Is.Not.SameAs(hostConnection));
            Assert.That(accepted.Metadata.Direction,
                Is.EqualTo(DiameterConnectionDirection.Inbound));
            Assert.That(firstClose.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(secondClose.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(hostListener.CloseCount, Is.EqualTo(1));
            Assert.That(created.Listener.ReadDiameterListenerMetadata().Open, Is.False);
        });

        accepted.Connection!.CloseDiameterConnection();
        Assert.That(hostConnection.CloseCount, Is.EqualTo(1));
    }

    [Test]
    public void IpsecPeerAdmissionRejectionClosesHostConnectionBeforeHandoff()
    {
        ExternalTestFactory host = new ExternalTestFactory();
        host.Support = CreateExternalTransportMetadata(true, false);
        host.Connection = new ExternalTestConnection(
            CreateConnectionMetadata(
                DiameterTransportKind.External,
                DiameterSecurityMode.Ipsec,
                DiameterConnectionDirection.Outbound));
        DiameterTransportConfiguration transport =
            CreateExternalTransportConfiguration();
        transport.EnableListener = false;
        transport.EnableActiveConnections = true;
        DiameterSecurityConfiguration security = new DiameterSecurityConfiguration();
        security.Mode = DiameterSecurityMode.Ipsec;
        ExternalRejectingAdmissionPolicy policy =
            new ExternalRejectingAdmissionPolicy();
        security.PeerAdmissionPolicy = policy;
        DiameterEndpoint endpoint = new DiameterEndpoint();
        endpoint.Identity.Value = "blocked.example";
        endpoint.Host = "blocked.example";
        endpoint.Transport = DiameterTransportKind.External;
        endpoint.Security = DiameterSecurityMode.Ipsec;
        IDiameterTransportFactory factory =
            new DiameterExternalTransportFactory(host);

        DiameterConnectionResult result = factory.ConnectDiameterEndpoint(
            endpoint,
            transport,
            security,
            1000);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status,
                Is.EqualTo(DiameterTransportStatus.AuthenticationFailed));
            Assert.That(result.Code, Is.EqualTo("host-peer-blocked"));
            Assert.That(result.Connection, Is.Null);
            Assert.That(host.Connection!.CloseCount, Is.EqualTo(1));
            Assert.That(policy.Request, Is.Not.Null);
            Assert.That(policy.Request!.PeerIdentity.Value,
                Is.EqualTo("blocked.example"));
            Assert.That(policy.Request.PeerRealm.Value, Is.EqualTo("example"));
            Assert.That(policy.Request.Connection.Security.Protocol,
                Is.EqualTo("IPsec"));
        });
    }

    [Test]
    public void InvalidHostCapabilitiesAndIoResultsAreRejectedDeterministically()
    {
        ExternalTestFactory invalidHost = new ExternalTestFactory();
        invalidHost.Support = CreateSctpTransportMetadata(true, false);
        invalidHost.Support.PayloadProtocolIdentifier =
            DiameterProtocolConstants.SctpPayloadProtocolIdentifier;
        IDiameterTransportFactory invalidFactory =
            new DiameterExternalTransportFactory(invalidHost);
        DiameterTransportConfiguration sctp = CreateSctpTransportConfiguration();
        DiameterSecurityConfiguration dtls = CreateDtlsSecurityConfiguration();

        DiameterTransportSupportResult invalidSupport =
            invalidFactory.ProbeDiameterTransportSupport(sctp, dtls);

        ExternalTestFactory ioHost = new ExternalTestFactory();
        ioHost.Support = CreateExternalTransportMetadata(true, false);
        ioHost.Connection = new ExternalTestConnection(
            CreateConnectionMetadata(
                DiameterTransportKind.External,
                DiameterSecurityMode.External,
                DiameterConnectionDirection.Outbound));
        ioHost.Connection.InvalidSendCount = true;
        IDiameterTransportFactory ioFactory =
            new DiameterExternalTransportFactory(ioHost);
        DiameterTransportConfiguration external =
            CreateExternalTransportConfiguration();
        external.EnableListener = false;
        external.EnableActiveConnections = true;
        DiameterSecurityConfiguration externalSecurity =
            CreateExternalSecurityConfiguration();
        DiameterEndpoint endpoint = new DiameterEndpoint();
        endpoint.Identity.Value = "peer.example";
        endpoint.Host = "peer.example";
        endpoint.Transport = DiameterTransportKind.External;
        endpoint.Security = DiameterSecurityMode.External;
        DiameterConnectionResult connected = ioFactory.ConnectDiameterEndpoint(
            endpoint,
            external,
            externalSecurity,
            1000);
        DiameterTransportIoResult send = connected.Connection!
            .SendDiameterConnectionBytes(new byte[] { 1, 2 }, 1000);

        Assert.Multiple(() =>
        {
            Assert.That(invalidSupport.Status,
                Is.EqualTo(DiameterTransportStatus.Failed));
            Assert.That(invalidSupport.Code, Is.EqualTo("external-capability-invalid"));
            Assert.That(connected.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(send.Status, Is.EqualTo(DiameterTransportStatus.Failed));
            Assert.That(send.Code, Is.EqualTo("external-io-result-invalid"));
            Assert.That(ioHost.Connection.CloseCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void ExternalConfigurationValidationCoversEveryTransportAndSecurityBoundary()
    {
        DiameterExternalTransportFactory factory =
            new DiameterExternalTransportFactory(new ExternalTestFactory());
        DiameterTransportConfiguration external =
            CreateExternalTransportConfiguration();
        DiameterSecurityConfiguration externalSecurity =
            CreateExternalSecurityConfiguration();
        Assert.Throws<ArgumentException>(() =>
            new DiameterExternalTransportFactory(null!));
        Assert.That(factory.ValidateExternalConfiguration(null, externalSecurity),
            Is.Not.Null);
        Assert.That(factory.ValidateExternalConfiguration(external, null),
            Is.Not.Null);

        AssertInvalidExternalConfiguration(factory, external, externalSecurity,
            value => value.Kind = (DiameterTransportKind)99, null);
        AssertInvalidExternalConfiguration(factory, external, externalSecurity,
            null, value => value.Mode = (DiameterSecurityMode)99);
        AssertInvalidExternalConfiguration(factory, external, externalSecurity,
            value => value.Port = -1, null);
        AssertInvalidExternalConfiguration(factory, external, externalSecurity,
            value => value.Port = 65536, null);
        AssertInvalidExternalConfiguration(factory, CreateSctpTransportConfiguration(),
            CreateDtlsSecurityConfiguration(), value => value.Port = 0, null);
        AssertInvalidExternalConfiguration(factory, CreateSctpTransportConfiguration(),
            CreateDtlsSecurityConfiguration(), value =>
            {
                value.EnableIpv4 = false;
                value.EnableIpv6 = false;
            }, null);
        AssertInvalidExternalConfiguration(factory, external, externalSecurity,
            value => value.ReceiveBufferSize = 1, null);
        AssertInvalidExternalConfiguration(factory, external, externalSecurity,
            value => value.SendBufferSize = 1, null);
        AssertInvalidExternalConfiguration(factory, external, externalSecurity,
            value => value.ListenBacklog = 0, null);
        AssertInvalidExternalConfiguration(factory, external, externalSecurity,
            value => value.SctpStreamCount = 0, null);
        AssertInvalidExternalConfiguration(factory, external, externalSecurity,
            value => value.SctpStreamCount = 2, null);
        AssertInvalidExternalConfiguration(factory, CreateSctpTransportConfiguration(),
            CreateDtlsSecurityConfiguration(),
            value => value.SctpStreamCount = ushort.MaxValue + 1, null);

        DiameterTransportConfiguration unsecuredTransport =
            CreateExternalTransportConfiguration();
        DiameterSecurityConfiguration unsecured =
            new DiameterSecurityConfiguration();
        unsecured.Mode = DiameterSecurityMode.None;
        AssertInvalidExternalConfiguration(factory, unsecuredTransport, unsecured,
            null, null);
        unsecured.AllowUnsecuredTransport = true;
        Assert.That(factory.ValidateExternalConfiguration(
            unsecuredTransport, unsecured), Is.Null);
        AssertInvalidExternalConfiguration(factory, external, externalSecurity,
            null, value => value.AllowUnsecuredTransport = true);
        AssertInvalidExternalConfiguration(factory, external, externalSecurity,
            null, value => value.LocalCertificatePath = "unexpected.pfx");

        DiameterTransportConfiguration tlsTransport =
            CreateExternalTransportConfiguration();
        tlsTransport.Kind = DiameterTransportKind.Tcp;
        tlsTransport.Port = DiameterProtocolConstants.SecurePort;
        tlsTransport.EnableIpv4 = true;
        DiameterSecurityConfiguration tls = CreateDtlsSecurityConfiguration();
        tls.Mode = DiameterSecurityMode.Tls;
        AssertInvalidExternalConfiguration(factory, tlsTransport, tls,
            null, value => value.LocalCertificatePath = "");
        AssertInvalidExternalConfiguration(factory, tlsTransport, tls,
            null, value => value.RequirePeerCertificate = false);
        AssertInvalidExternalConfiguration(factory, CreateSctpTransportConfiguration(),
            tls, null, null);
        AssertInvalidExternalConfiguration(factory, tlsTransport,
            CreateDtlsSecurityConfiguration(), null, null);
        AssertInvalidExternalConfiguration(factory, tlsTransport,
            externalSecurity, null, null);
        AssertInvalidExternalConfiguration(factory, external, externalSecurity,
            null, value => value.ExpectedPeerIdentity = null!);
        AssertInvalidExternalConfiguration(factory, external, externalSecurity,
            null, value => value.ExpectedPeerRealm = null!);
        AssertInvalidExternalConfiguration(factory, external, externalSecurity,
            null, value => value.ExpectedPeerIdentity.Value = "bad identity");
        AssertInvalidExternalConfiguration(factory, external, externalSecurity,
            null, value => value.ExpectedPeerRealm.Value = "bad realm");
    }

    [Test]
    public void ExternalMetadataValidationRejectsEachIncompleteConnectionShape()
    {
        DiameterExternalTransportFactory factory =
            new DiameterExternalTransportFactory(new ExternalTestFactory());
        DiameterTransportConfiguration transport =
            CreateExternalTransportConfiguration();
        transport.EnableActiveConnections = true;
        transport.EnableListener = false;
        DiameterSecurityConfiguration security =
            CreateExternalSecurityConfiguration();

        AssertInvalidExternalConnection(factory, transport, security,
            value => value.Open = false);
        AssertInvalidExternalConnection(factory, transport, security,
            value => value.ConnectionId = "");
        AssertInvalidExternalConnection(factory, transport, security,
            value => value.LocalEndpoint = null!);
        AssertInvalidExternalConnection(factory, transport, security,
            value => value.RemoteEndpoint = null!);
        AssertInvalidExternalConnection(factory, transport, security,
            value => value.LocalEndpoint.Identity = null!);
        AssertInvalidExternalConnection(factory, transport, security,
            value => value.RemoteEndpoint.Identity = null!);
        AssertInvalidExternalConnection(factory, transport, security,
            value => value.Transport = null!);
        AssertInvalidExternalConnection(factory, transport, security,
            value => value.Security = null!);
        AssertInvalidExternalConnection(factory, transport, security,
            value => value.Direction = DiameterConnectionDirection.Inbound);
        AssertInvalidExternalConnection(factory, transport, security,
            value => value.Security.Mode = DiameterSecurityMode.Ipsec);
        AssertInvalidExternalConnection(factory, transport, security,
            value => value.LocalEndpoint.Transport = DiameterTransportKind.Tcp);
        AssertInvalidExternalConnection(factory, transport, security,
            value => value.RemoteEndpoint.Transport = DiameterTransportKind.Tcp);
        AssertInvalidExternalConnection(factory, transport, security,
            value => value.LocalEndpoint.Security = DiameterSecurityMode.Ipsec);
        AssertInvalidExternalConnection(factory, transport, security,
            value => value.RemoteEndpoint.Security = DiameterSecurityMode.Ipsec);
        AssertInvalidExternalConnection(factory, transport, security,
            value => value.InboundStream = -1);
        AssertInvalidExternalConnection(factory, transport, security,
            value => value.OutboundStream = -1);
        AssertInvalidExternalConnection(factory, transport, security,
            value => value.InboundStream = 1);
        AssertInvalidExternalConnection(factory, transport, security,
            value => value.OutboundStream = 1);

        DiameterSecurityConfiguration none =
            new DiameterSecurityConfiguration();
        none.Mode = DiameterSecurityMode.None;
        none.AllowUnsecuredTransport = true;
        DiameterConnectionMetadata unsecured = CreateConnectionMetadata(
            DiameterTransportKind.External,
            DiameterSecurityMode.None,
            DiameterConnectionDirection.Outbound);
        Assert.That(factory.ValidateConnectionMetadata(
            unsecured, transport, none, DiameterConnectionDirection.Outbound),
            Is.True);
        unsecured.Security.Authenticated = true;
        Assert.That(factory.ValidateConnectionMetadata(
            unsecured, transport, none, DiameterConnectionDirection.Outbound),
            Is.False);

        DiameterConnectionMetadata ipsec = CreateConnectionMetadata(
            DiameterTransportKind.External,
            DiameterSecurityMode.Ipsec,
            DiameterConnectionDirection.Outbound);
        DiameterSecurityConfiguration ipsecSecurity =
            new DiameterSecurityConfiguration();
        ipsecSecurity.Mode = DiameterSecurityMode.Ipsec;
        ipsec.Security.Authenticated = false;
        Assert.That(factory.ValidateConnectionMetadata(
            ipsec, transport, ipsecSecurity, DiameterConnectionDirection.Outbound),
            Is.False);
        ipsec = CreateConnectionMetadata(
            DiameterTransportKind.External,
            DiameterSecurityMode.Ipsec,
            DiameterConnectionDirection.Outbound);
        ipsec.Security.Encrypted = false;
        Assert.That(factory.ValidateConnectionMetadata(
            ipsec, transport, ipsecSecurity, DiameterConnectionDirection.Outbound),
            Is.False);
        ipsec = CreateConnectionMetadata(
            DiameterTransportKind.External,
            DiameterSecurityMode.Ipsec,
            DiameterConnectionDirection.Outbound);
        ipsec.Security.Protocol = "";
        Assert.That(factory.ValidateConnectionMetadata(
            ipsec, transport, ipsecSecurity, DiameterConnectionDirection.Outbound),
            Is.False);
    }

    [Test]
    public void TransportMetadataValidationCoversTcpSctpAndExternalShapes()
    {
        DiameterExternalTransportFactory factory =
            new DiameterExternalTransportFactory(new ExternalTestFactory());
        DiameterTransportConfiguration tcp =
            new DiameterTransportConfiguration();
        tcp.Kind = DiameterTransportKind.Tcp;
        tcp.Port = DiameterProtocolConstants.Port;
        DiameterSecurityConfiguration none =
            new DiameterSecurityConfiguration();
        none.AllowUnsecuredTransport = true;
        DiameterTransportMetadata tcpMetadata = new DiameterTransportMetadata();
        tcpMetadata.Kind = DiameterTransportKind.Tcp;
        tcpMetadata.MaximumInboundStreams = 1;
        tcpMetadata.MaximumOutboundStreams = 1;
        Assert.That(factory.ValidateTransportMetadata(tcpMetadata, tcp, none),
            Is.True);
        tcpMetadata.MessageOriented = true;
        Assert.That(factory.ValidateTransportMetadata(tcpMetadata, tcp, none),
            Is.False);
        tcpMetadata.MessageOriented = false;
        tcpMetadata.MaximumInboundStreams = 2;
        Assert.That(factory.ValidateTransportMetadata(tcpMetadata, tcp, none),
            Is.False);
        tcpMetadata.MaximumInboundStreams = 1;
        tcpMetadata.MaximumOutboundStreams = 2;
        Assert.That(factory.ValidateTransportMetadata(tcpMetadata, tcp, none),
            Is.False);
        tcpMetadata.MaximumOutboundStreams = 1;
        tcpMetadata.PayloadProtocolIdentifier = 1U;
        Assert.That(factory.ValidateTransportMetadata(tcpMetadata, tcp, none),
            Is.False);

        DiameterTransportConfiguration sctp =
            CreateSctpTransportConfiguration();
        DiameterSecurityConfiguration dtls = CreateDtlsSecurityConfiguration();
        DiameterTransportMetadata sctpMetadata =
            CreateSctpTransportMetadata(true, true);
        Assert.That(factory.ValidateTransportMetadata(sctpMetadata, sctp, dtls),
            Is.True);
        sctpMetadata.MessageOriented = false;
        Assert.That(factory.ValidateTransportMetadata(sctpMetadata, sctp, dtls),
            Is.False);
        sctpMetadata = CreateSctpTransportMetadata(true, true);
        sctpMetadata.MaximumInboundStreams = 1;
        Assert.That(factory.ValidateTransportMetadata(sctpMetadata, sctp, dtls),
            Is.False);
        sctpMetadata = CreateSctpTransportMetadata(true, true);
        sctpMetadata.MaximumOutboundStreams = 1;
        Assert.That(factory.ValidateTransportMetadata(sctpMetadata, sctp, dtls),
            Is.False);
        sctpMetadata = CreateSctpTransportMetadata(true, true);
        sctpMetadata.PayloadProtocolIdentifier = 1U;
        Assert.That(factory.ValidateTransportMetadata(sctpMetadata, sctp, dtls),
            Is.False);

        DiameterTransportMetadata external =
            CreateExternalTransportMetadata(true, true);
        Assert.That(factory.ValidateTransportMetadata(
            external,
            CreateExternalTransportConfiguration(),
            CreateExternalSecurityConfiguration()), Is.True);
        external.MaximumInboundStreams = 0;
        Assert.That(factory.ValidateTransportMetadata(
            external,
            CreateExternalTransportConfiguration(),
            CreateExternalSecurityConfiguration()), Is.False);
    }

    [Test]
    public void ExternalAdapterNormalizesNullFailureAndMissingCapabilityResults()
    {
        ExternalTestFactory host = new ExternalTestFactory();
        DiameterExternalTransportFactory factory =
            new DiameterExternalTransportFactory(host);
        DiameterTransportConfiguration transport =
            CreateExternalTransportConfiguration();
        DiameterSecurityConfiguration security =
            CreateExternalSecurityConfiguration();

        Assert.That(factory.ProbeDiameterTransportSupport(null, security).Status,
            Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
        host.NullProbe = true;
        Assert.That(factory.ProbeDiameterTransportSupport(
            transport, security).Code,
            Is.EqualTo("external-probe-result-invalid"));
        host.NullProbe = false;
        host.ProbeStatus = DiameterTransportStatus.Failed;
        Assert.That(factory.ProbeDiameterTransportSupport(
            transport, security).Status,
            Is.EqualTo(DiameterTransportStatus.Failed));
        host.ProbeStatus = DiameterTransportStatus.Success;
        host.Support = null!;
        Assert.That(factory.ProbeDiameterTransportSupport(
            transport, security).Code,
            Is.EqualTo("external-capability-invalid"));
        host.Support = CreateExternalTransportMetadata(false, false);
        Assert.That(factory.ProbeDiameterTransportSupport(
            transport, security).Status,
            Is.EqualTo(DiameterTransportStatus.Unsupported));
        transport.EnableActiveConnections = true;
        transport.EnableListener = false;
        Assert.That(factory.ProbeDiameterTransportSupport(
            transport, security).Status,
            Is.EqualTo(DiameterTransportStatus.Unsupported));

        transport = CreateExternalTransportConfiguration();
        host.Support = CreateExternalTransportMetadata(false, true);
        host.NullListenerResult = true;
        Assert.That(factory.CreateDiameterListener(
            transport, security).Code,
            Is.EqualTo("external-listener-result-invalid"));
        host.NullListenerResult = false;
        host.ListenerStatus = DiameterTransportStatus.Failed;
        host.Listener = new ExternalTestListener();
        DiameterListenerResult listenerFailure = factory.CreateDiameterListener(
            transport, security);
        Assert.That(listenerFailure.Status,
            Is.EqualTo(DiameterTransportStatus.Failed));
        Assert.That(host.Listener.CloseCount, Is.EqualTo(1));
        host.ListenerStatus = DiameterTransportStatus.Success;
        host.Listener = null;
        Assert.That(factory.CreateDiameterListener(
            transport, security).Code,
            Is.EqualTo("external-listener-result-invalid"));

        transport.EnableListener = false;
        transport.EnableActiveConnections = true;
        host.Support = CreateExternalTransportMetadata(true, false);
        DiameterEndpoint endpoint = new DiameterEndpoint();
        endpoint.Identity.Value = "peer.example";
        endpoint.Host = "peer.example";
        endpoint.Transport = DiameterTransportKind.External;
        endpoint.Security = DiameterSecurityMode.External;
        host.NullConnectionResult = true;
        Assert.That(factory.ConnectDiameterEndpoint(
            endpoint, transport, security, 1).Code,
            Is.EqualTo("external-connection-result-invalid"));
        host.NullConnectionResult = false;
        host.ConnectionStatus = DiameterTransportStatus.Failed;
        host.Connection = new ExternalTestConnection(CreateConnectionMetadata(
            DiameterTransportKind.External,
            DiameterSecurityMode.External,
            DiameterConnectionDirection.Outbound));
        DiameterConnectionResult connectionFailure =
            factory.ConnectDiameterEndpoint(endpoint, transport, security, 1);
        Assert.That(connectionFailure.Status,
            Is.EqualTo(DiameterTransportStatus.Failed));
        Assert.That(host.Connection.CloseCount, Is.EqualTo(1));
        host.ConnectionStatus = DiameterTransportStatus.Success;
        host.Connection = null;
        Assert.That(factory.ConnectDiameterEndpoint(
            endpoint, transport, security, 1).Code,
            Is.EqualTo("external-connection-result-invalid"));
    }

    [Test]
    public void ExternalConnectInputAndPeerIdentityFallbacksCoverEveryShape()
    {
        ExternalTestFactory host = new ExternalTestFactory();
        host.Support = CreateExternalTransportMetadata(true, false);
        DiameterExternalTransportFactory factory =
            new DiameterExternalTransportFactory(host);
        DiameterTransportConfiguration transport =
            CreateExternalTransportConfiguration();
        transport.EnableListener = false;
        transport.EnableActiveConnections = true;
        DiameterSecurityConfiguration security =
            CreateExternalSecurityConfiguration();
        DiameterEndpoint endpoint = new DiameterEndpoint();
        endpoint.Identity.Value = "peer.example";
        endpoint.Host = "peer.example";
        endpoint.Transport = DiameterTransportKind.External;
        endpoint.Security = DiameterSecurityMode.External;

        Assert.That(factory.ConnectDiameterEndpoint(
            null, transport, security, 1).Status,
            Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
        DiameterEndpoint noIdentity = new DiameterEndpoint();
        noIdentity.Identity = null!;
        noIdentity.Transport = DiameterTransportKind.External;
        Assert.That(factory.ConnectDiameterEndpoint(
            noIdentity, transport, security, 1).Status,
            Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
        Assert.That(factory.ConnectDiameterEndpoint(
            endpoint, transport, security, 0).Status,
            Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
        transport.EnableActiveConnections = false;
        Assert.That(factory.ConnectDiameterEndpoint(
            endpoint, transport, security, 1).Status,
            Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
        transport.EnableActiveConnections = true;
        endpoint.Port = -1;
        Assert.That(factory.ConnectDiameterEndpoint(
            endpoint, transport, security, 1).Status,
            Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
        endpoint.Port = 65536;
        Assert.That(factory.ConnectDiameterEndpoint(
            endpoint, transport, security, 1).Status,
            Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));

        DiameterConnectionMetadata metadata = CreateConnectionMetadata(
            DiameterTransportKind.External,
            DiameterSecurityMode.External,
            DiameterConnectionDirection.Outbound);
        DiameterSecurityConfiguration expectedName =
            CreateExternalSecurityConfiguration();
        expectedName.ExpectedPeerName = "named.example";
        DiameterIdentity fromName = factory.ReadExternalPeerIdentity(
            metadata, expectedName, null);
        DiameterSecurityConfiguration remoteIdentity =
            CreateExternalSecurityConfiguration();
        DiameterIdentity fromRemote = factory.ReadExternalPeerIdentity(
            metadata, remoteIdentity, null);
        metadata.RemoteEndpoint.Identity.Value = "";
        DiameterIdentity fromHost = factory.ReadExternalPeerIdentity(
            metadata, remoteIdentity, null);
        Assert.Multiple(() =>
        {
            Assert.That(fromName.Value, Is.EqualTo("named.example"));
            Assert.That(fromRemote.Value, Is.EqualTo("peer.example"));
            Assert.That(fromHost.Value, Is.EqualTo("peer.example"));
        });
    }

    [Test]
    public void ExternalIntegrationCoversSupportMetadataTlsAndCloneBoundaries()
    {
        ExternalTestFactory host = new ExternalTestFactory();
        DiameterExternalTransportFactory factory =
            new DiameterExternalTransportFactory(host);
        DiameterTransportConfiguration listenerTransport =
            CreateExternalTransportConfiguration();
        DiameterSecurityConfiguration externalSecurity =
            CreateExternalSecurityConfiguration();
        host.Support = CreateExternalTransportMetadata(false, true);
        host.ProbeStatus = DiameterTransportStatus.Failed;
        Assert.That(factory.CreateDiameterListener(
            listenerTransport, externalSecurity).Status,
            Is.EqualTo(DiameterTransportStatus.Failed));
        host.ProbeStatus = DiameterTransportStatus.Success;
        listenerTransport.EnableListener = false;
        Assert.That(factory.CreateDiameterListener(
            listenerTransport, externalSecurity).Status,
            Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
        listenerTransport.EnableListener = true;

        Action<DiameterListenerMetadata>[] listenerChanges =
            new Action<DiameterListenerMetadata>[]
        {
            value => value.Open = false,
            value => value.Transport = null!,
            value => value.LocalEndpoint = null!,
            value => value.SecurityMode = DiameterSecurityMode.Ipsec,
            value => value.Transport.Kind = DiameterTransportKind.Tcp
        };
        int listenerIndex = 0;
        while (listenerIndex < listenerChanges.Length)
        {
            ExternalTestListener listener = new ExternalTestListener();
            listener.Metadata = CreateListenerMetadata(
                DiameterTransportKind.External,
                DiameterSecurityMode.External);
            listenerChanges[listenerIndex](listener.Metadata);
            host.Listener = listener;
            DiameterListenerResult result = factory.CreateDiameterListener(
                listenerTransport,
                externalSecurity);
            Assert.That(result.Status, Is.EqualTo(DiameterTransportStatus.Failed));
            listenerIndex = listenerIndex + 1;
        }

        DiameterTransportConfiguration active =
            CreateExternalTransportConfiguration();
        active.EnableListener = false;
        active.EnableActiveConnections = true;
        host.Support = CreateExternalTransportMetadata(true, false);
        host.ProbeStatus = DiameterTransportStatus.Failed;
        Assert.That(factory.ConnectDiameterEndpoint(
            new DiameterEndpoint(), active, externalSecurity, 1).Status,
            Is.EqualTo(DiameterTransportStatus.Failed));
        host.ProbeStatus = DiameterTransportStatus.Success;
        ExternalTestConnection nullMetadata = new ExternalTestConnection(
            new DiameterConnectionMetadata());
        nullMetadata.ReturnNullMetadata = true;
        DiameterConnectionResult suppliedNullMetadata =
            new DiameterConnectionResult();
        suppliedNullMetadata.Connection = nullMetadata;
        Assert.That(factory.IntegrateConnectionResult(
            suppliedNullMetadata,
            active,
            externalSecurity,
            CreateExternalEndpoint(),
            DiameterConnectionDirection.Outbound).Status,
            Is.EqualTo(DiameterTransportStatus.Failed));
        ExternalTestConnection invalidMetadata = new ExternalTestConnection(
            CreateConnectionMetadata(
                DiameterTransportKind.External,
                DiameterSecurityMode.External,
                DiameterConnectionDirection.Inbound));
        host.Connection = invalidMetadata;
        Assert.That(factory.ConnectDiameterEndpoint(
            CreateExternalEndpoint(), active, externalSecurity, 1).Status,
            Is.EqualTo(DiameterTransportStatus.Failed));

        DiameterConnectionMetadata tlsMetadata = CreateConnectionMetadata(
            DiameterTransportKind.Sctp,
            DiameterSecurityMode.Dtls,
            DiameterConnectionDirection.Outbound);
        DiameterSecurityConfiguration tlsSecurity =
            CreateDtlsSecurityConfiguration();
        tlsSecurity.Mode = DiameterSecurityMode.Tls;
        Assert.That(factory.AuthorizeExternalPeer(
            tlsMetadata,
            tlsSecurity,
            CreateExternalEndpoint()), Is.Not.Null);

        DiameterTransportConfiguration sctp =
            CreateSctpTransportConfiguration();
        DiameterSecurityConfiguration dtls = CreateDtlsSecurityConfiguration();
        DiameterConnectionMetadata secure = CreateConnectionMetadata(
            DiameterTransportKind.Sctp,
            DiameterSecurityMode.Dtls,
            DiameterConnectionDirection.Outbound);
        secure.Security.MutuallyAuthenticated = false;
        Assert.That(factory.ValidateConnectionMetadata(
            secure, sctp, dtls, DiameterConnectionDirection.Outbound), Is.False);
        secure = CreateConnectionMetadata(
            DiameterTransportKind.Sctp,
            DiameterSecurityMode.Dtls,
            DiameterConnectionDirection.Outbound);
        secure.Security.PeerCertificateThumbprint = "";
        Assert.That(factory.ValidateConnectionMetadata(
            secure, sctp, dtls, DiameterConnectionDirection.Outbound), Is.False);
        secure = CreateConnectionMetadata(
            DiameterTransportKind.Sctp,
            DiameterSecurityMode.Dtls,
            DiameterConnectionDirection.Outbound);
        secure.Security.PeerCertificateDnsNames = null!;
        Assert.That(factory.ValidateConnectionMetadata(
            secure, sctp, dtls, DiameterConnectionDirection.Outbound), Is.False);
        secure = CreateConnectionMetadata(
            DiameterTransportKind.Sctp,
            DiameterSecurityMode.Dtls,
            DiameterConnectionDirection.Outbound);
        secure.Security.PeerCertificateDnsNames.Clear();
        Assert.That(factory.ValidateConnectionMetadata(
            secure, sctp, dtls, DiameterConnectionDirection.Outbound), Is.False);

        DiameterListenerResult sparseListener = new DiameterListenerResult();
        sparseListener.Metadata = new DiameterListenerMetadata();
        factory.CloneListenerResult(sparseListener);
        DiameterConnectionResult sparseConnection = new DiameterConnectionResult();
        sparseConnection.Metadata = new DiameterConnectionMetadata();
        sparseConnection.Metadata.LocalEndpoint = null!;
        factory.CloneConnectionResult(sparseConnection);
        sparseConnection.Metadata = null!;
        factory.CloneConnectionResult(sparseConnection);
        Assert.Pass();
    }

    private static DiameterEndpoint CreateExternalEndpoint()
    {
        DiameterEndpoint result = new DiameterEndpoint();
        result.Identity.Value = "peer.example";
        result.Host = "peer.example";
        result.Transport = DiameterTransportKind.External;
        result.Security = DiameterSecurityMode.External;
        return result;
    }

    private static void AssertInvalidExternalConfiguration(
        DiameterExternalTransportFactory factory,
        DiameterTransportConfiguration transport,
        DiameterSecurityConfiguration security,
        Action<DiameterTransportConfiguration>? changeTransport,
        Action<DiameterSecurityConfiguration>? changeSecurity)
    {
        DiameterTransportConfiguration owned =
            factory.CloneTransportConfiguration(transport);
        DiameterSecurityConfiguration ownedSecurity =
            factory.CloneSecurityConfiguration(security);
        if (changeTransport != null)
        {
            changeTransport(owned);
        }

        if (changeSecurity != null)
        {
            changeSecurity(ownedSecurity);
        }

        Assert.That(factory.ValidateExternalConfiguration(
            owned, ownedSecurity), Is.Not.Null);
    }

    private static void AssertInvalidExternalConnection(
        DiameterExternalTransportFactory factory,
        DiameterTransportConfiguration transport,
        DiameterSecurityConfiguration security,
        Action<DiameterConnectionMetadata> change)
    {
        DiameterConnectionMetadata metadata = CreateConnectionMetadata(
            DiameterTransportKind.External,
            DiameterSecurityMode.External,
            DiameterConnectionDirection.Outbound);
        change(metadata);
        Assert.That(factory.ValidateConnectionMetadata(
            metadata,
            transport,
            security,
            DiameterConnectionDirection.Outbound), Is.False);
    }

    private static DiameterTransportConfiguration CreateSctpTransportConfiguration()
    {
        DiameterTransportConfiguration result = new DiameterTransportConfiguration();
        result.Kind = DiameterTransportKind.Sctp;
        result.Port = DiameterProtocolConstants.SecurePort;
        result.SctpStreamCount = 4;
        result.EnableActiveConnections = true;
        result.EnableListener = false;
        return result;
    }

    private static DiameterTransportConfiguration CreateExternalTransportConfiguration()
    {
        DiameterTransportConfiguration result = new DiameterTransportConfiguration();
        result.Kind = DiameterTransportKind.External;
        result.Port = 0;
        result.EnableIpv4 = false;
        result.EnableIpv6 = false;
        result.EnableActiveConnections = false;
        result.EnableListener = true;
        return result;
    }

    private static DiameterSecurityConfiguration CreateDtlsSecurityConfiguration()
    {
        DiameterSecurityConfiguration result = new DiameterSecurityConfiguration();
        result.Mode = DiameterSecurityMode.Dtls;
        result.LocalCertificatePath = "managed-by-host.pfx";
        result.ExpectedPeerName = "peer.example";
        result.ExpectedPeerIdentity.Value = "peer.example";
        result.ExpectedPeerRealm.Value = "example";
        result.RequirePeerCertificate = true;
        return result;
    }

    private static DiameterSecurityConfiguration CreateExternalSecurityConfiguration()
    {
        DiameterSecurityConfiguration result = new DiameterSecurityConfiguration();
        result.Mode = DiameterSecurityMode.External;
        return result;
    }

    private static DiameterTransportMetadata CreateSctpTransportMetadata(
        bool active,
        bool listener)
    {
        DiameterTransportMetadata result = new DiameterTransportMetadata();
        result.Kind = DiameterTransportKind.Sctp;
        result.SupportsActiveConnections = active;
        result.SupportsListeners = listener;
        result.SupportsIpv4 = true;
        result.SupportsIpv6 = true;
        result.MessageOriented = true;
        result.MaximumInboundStreams = 8;
        result.MaximumOutboundStreams = 8;
        result.PayloadProtocolIdentifier =
            DiameterProtocolConstants.DtlsSctpPayloadProtocolIdentifier;
        return result;
    }

    private static DiameterTransportMetadata CreateExternalTransportMetadata(
        bool active,
        bool listener)
    {
        DiameterTransportMetadata result = new DiameterTransportMetadata();
        result.Kind = DiameterTransportKind.External;
        result.SupportsActiveConnections = active;
        result.SupportsListeners = listener;
        result.MessageOriented = true;
        return result;
    }

    private static DiameterListenerMetadata CreateListenerMetadata(
        DiameterTransportKind kind,
        DiameterSecurityMode security)
    {
        DiameterListenerMetadata result = new DiameterListenerMetadata();
        result.LocalEndpoint.Transport = kind;
        result.LocalEndpoint.Security = security;
        result.Transport = CreateExternalTransportMetadata(false, true);
        result.Transport.Kind = kind;
        result.SecurityMode = security;
        result.Open = true;
        return result;
    }

    private static DiameterConnectionMetadata CreateConnectionMetadata(
        DiameterTransportKind kind,
        DiameterSecurityMode security,
        DiameterConnectionDirection direction)
    {
        DiameterConnectionMetadata result = new DiameterConnectionMetadata();
        result.ConnectionId = "host-connection";
        result.LocalEndpoint.Identity.Value = "local.example";
        result.LocalEndpoint.Host = "local.example";
        result.LocalEndpoint.Transport = kind;
        result.LocalEndpoint.Security = security;
        result.RemoteEndpoint.Identity.Value = "peer.example";
        result.RemoteEndpoint.Host = "peer.example";
        result.RemoteEndpoint.Transport = kind;
        result.RemoteEndpoint.Security = security;
        result.Direction = direction;
        if (kind == DiameterTransportKind.Sctp)
        {
            result.Transport = CreateSctpTransportMetadata(true, true);
        }
        else
        {
            result.Transport = CreateExternalTransportMetadata(true, true);
        }

        result.Security.Mode = security;
        if (security != DiameterSecurityMode.None)
        {
            result.Security.Authenticated = true;
            result.Security.Encrypted = true;
            result.Security.Protocol = security == DiameterSecurityMode.Ipsec
                ? "IPsec"
                : "host-security";
        }

        if (security == DiameterSecurityMode.Dtls)
        {
            result.Security.MutuallyAuthenticated = true;
            result.Security.Protocol = "DTLS 1.3";
            result.Security.PeerCertificateThumbprint = "001122";
            result.Security.PeerCertificateDnsNames.Add("peer.example");
        }

        result.EstablishedAt = DateTimeOffset.UtcNow;
        result.Open = true;
        return result;
    }
}

public class ExternalTestFactory : IDiameterTransportFactory
{
    public DiameterTransportMetadata Support = new DiameterTransportMetadata();
    public ExternalTestListener? Listener;
    public ExternalTestConnection? Connection;
    public bool NullProbe;
    public bool NullListenerResult;
    public bool NullConnectionResult;
    public DiameterTransportStatus ProbeStatus;
    public DiameterTransportStatus ListenerStatus;
    public DiameterTransportStatus ConnectionStatus;

    public DiameterTransportSupportResult ProbeDiameterTransportSupport(
        DiameterTransportConfiguration? transport,
        DiameterSecurityConfiguration? security)
    {
        if (NullProbe)
        {
            return null!;
        }

        DiameterTransportSupportResult result = new DiameterTransportSupportResult();
        result.Status = ProbeStatus;
        result.Metadata = Support;
        return result;
    }

    public DiameterListenerResult CreateDiameterListener(
        DiameterTransportConfiguration? transport,
        DiameterSecurityConfiguration? security)
    {
        if (NullListenerResult)
        {
            return null!;
        }

        DiameterListenerResult result = new DiameterListenerResult();
        result.Status = ListenerStatus;
        result.Listener = Listener;
        if (Listener != null)
        {
            result.Metadata = Listener.ReadDiameterListenerMetadata();
        }

        return result;
    }

    public DiameterConnectionResult ConnectDiameterEndpoint(
        DiameterEndpoint? endpoint,
        DiameterTransportConfiguration? transport,
        DiameterSecurityConfiguration? security,
        int timeoutMilliseconds)
    {
        if (NullConnectionResult)
        {
            return null!;
        }

        DiameterConnectionResult result = new DiameterConnectionResult();
        result.Status = ConnectionStatus;
        result.Connection = Connection;
        if (Connection != null)
        {
            result.Metadata = Connection.ReadDiameterConnectionMetadata();
        }

        return result;
    }
}

public class ExternalTestListener : IDiameterListener
{
    public DiameterListenerMetadata Metadata = new DiameterListenerMetadata();
    public ExternalTestConnection? Connection;
    public int CloseCount;

    public DiameterConnectionResult AcceptDiameterConnection(int timeoutMilliseconds)
    {
        DiameterConnectionResult result = new DiameterConnectionResult();
        result.Connection = Connection;
        if (Connection != null)
        {
            result.Metadata = Connection.ReadDiameterConnectionMetadata();
        }

        return result;
    }

    public DiameterListenerMetadata ReadDiameterListenerMetadata()
    {
        return Metadata;
    }

    public DiameterTransportOperationResult CloseDiameterListener()
    {
        CloseCount = CloseCount + 1;
        Metadata.Open = false;
        return new DiameterTransportOperationResult();
    }
}

public class ExternalTestConnection : IDiameterConnection
{
    public DiameterConnectionMetadata Metadata;
    public byte[] SentBytes = new byte[0];
    public byte[] ReceivedBytes = new byte[] { 9, 8, 7, 6 };
    public int CloseCount;
    public bool InvalidSendCount;
    public bool ReturnNullMetadata;

    public ExternalTestConnection(DiameterConnectionMetadata metadata)
    {
        Metadata = metadata;
    }

    public DiameterTransportIoResult SendDiameterConnectionBytes(
        ReadOnlySpan<byte> source,
        int timeoutMilliseconds)
    {
        SentBytes = source.ToArray();
        DiameterTransportIoResult result = new DiameterTransportIoResult();
        result.TransferredCount = InvalidSendCount ? source.Length + 1 : source.Length;
        return result;
    }

    public DiameterTransportIoResult ReceiveDiameterConnectionBytes(
        Span<byte> destination,
        int timeoutMilliseconds)
    {
        int count = Math.Min(destination.Length, ReceivedBytes.Length);
        ReceivedBytes.AsSpan(0, count).CopyTo(destination);
        DiameterTransportIoResult result = new DiameterTransportIoResult();
        result.TransferredCount = count;
        return result;
    }

    public DiameterConnectionMetadata ReadDiameterConnectionMetadata()
    {
        if (ReturnNullMetadata)
        {
            return null!;
        }

        Metadata.Open = CloseCount == 0;
        return Metadata;
    }

    public DiameterTransportOperationResult CloseDiameterConnection()
    {
        CloseCount = CloseCount + 1;
        if (Metadata != null)
        {
            Metadata.Open = false;
        }
        return new DiameterTransportOperationResult();
    }
}

public class ExternalRejectingAdmissionPolicy : IPeerAdmissionPolicy
{
    public DiameterPeerAdmissionRequest? Request;

    public DiameterPeerAdmissionResult EvaluatePeerAdmission(
        DiameterPeerAdmissionRequest request)
    {
        Request = request;
        DiameterPeerAdmissionResult result = new DiameterPeerAdmissionResult();
        result.Code = "host-peer-blocked";
        result.Message = "The host rejected this peer.";
        return result;
    }
}
