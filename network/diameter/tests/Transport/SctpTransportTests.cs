using System;
using System.Net;
using System.Net.Sockets;

namespace diameter.Tests;

public class SctpTransportTests
{
    [Test]
    public void ProbeReportsSctpPolicyMetadataOrStablePlatformUnsupportedResult()
    {
        DiameterSctpTransportFactory factory = new DiameterSctpTransportFactory();
        DiameterTransportConfiguration transport = CreateSctpTransport(3868, false);
        transport.SctpStreamCount = 4;
        DiameterTransportSupportResult support = factory.ProbeDiameterTransportSupport(
            transport,
            CreateUnsecuredSecurity());

        Assert.Multiple(() =>
        {
            Assert.That(support.Status,
                Is.AnyOf(DiameterTransportStatus.Success, DiameterTransportStatus.Unsupported));
            Assert.That(support.Metadata.Kind, Is.EqualTo(DiameterTransportKind.Sctp));
            Assert.That(support.Metadata.SupportsActiveConnections, Is.True);
            Assert.That(support.Metadata.SupportsListeners, Is.True);
            Assert.That(support.Metadata.MessageOriented, Is.True);
            Assert.That(support.Metadata.MaximumInboundStreams, Is.EqualTo(4));
            Assert.That(support.Metadata.MaximumOutboundStreams, Is.EqualTo(4));
            Assert.That(support.Metadata.PayloadProtocolIdentifier,
                Is.EqualTo(DiameterProtocolConstants.SctpPayloadProtocolIdentifier));
            if (support.Status == DiameterTransportStatus.Unsupported)
            {
                Assert.That(support.Code, Is.EqualTo("sctp-platform-unsupported"));
                Assert.That(support.Metadata.SupportsIpv4, Is.False);
                Assert.That(support.Metadata.SupportsIpv6, Is.False);
            }
        });

        if (!OperatingSystem.IsLinux())
        {
            Assert.That(support.Status, Is.EqualTo(DiameterTransportStatus.Unsupported));
        }
    }

    [Test]
    public void ForcedUnsupportedPlatformReturnsOneStableContractForEveryOperation()
    {
        DeterministicUnsupportedSctpProbe probe =
            new DeterministicUnsupportedSctpProbe();
        DiameterSctpTransportFactory factory = new DiameterSctpTransportFactory();
        factory.CapabilityProbe = probe;
        DiameterTransportConfiguration active = CreateSctpTransport(3868, false);
        active.SctpStreamCount = 3;
        DiameterTransportConfiguration passive = CreateSctpTransport(3868, true);
        passive.SctpStreamCount = 3;
        DiameterSecurityConfiguration security = CreateUnsecuredSecurity();

        DiameterTransportSupportResult support = factory.ProbeDiameterTransportSupport(
            active,
            security);
        DiameterListenerResult listener = factory.CreateDiameterListener(
            passive,
            security);
        DiameterConnectionResult connection = factory.ConnectDiameterEndpoint(
            CreateSctpEndpoint(3868),
            active,
            security,
            1000);

        Assert.Multiple(() =>
        {
            Assert.That(support.Status,
                Is.EqualTo(DiameterTransportStatus.Unsupported));
            Assert.That(support.Code, Is.EqualTo("sctp-platform-unsupported"));
            Assert.That(support.Message,
                Is.EqualTo("SCTP was disabled by the deterministic test probe."));
            Assert.That(support.Metadata.Kind, Is.EqualTo(DiameterTransportKind.Sctp));
            Assert.That(support.Metadata.SupportsIpv4, Is.False);
            Assert.That(support.Metadata.SupportsIpv6, Is.False);
            Assert.That(support.Metadata.MaximumInboundStreams, Is.EqualTo(3));
            Assert.That(support.Metadata.MaximumOutboundStreams, Is.EqualTo(3));
            Assert.That(support.Metadata.MessageOriented, Is.True);
            Assert.That(support.Metadata.PayloadProtocolIdentifier,
                Is.EqualTo(DiameterProtocolConstants.SctpPayloadProtocolIdentifier));
            Assert.That(listener.Status,
                Is.EqualTo(DiameterTransportStatus.Unsupported));
            Assert.That(listener.Listener, Is.Null);
            Assert.That(listener.Code, Is.EqualTo(support.Code));
            Assert.That(listener.Message, Is.EqualTo(support.Message));
            Assert.That(connection.Status,
                Is.EqualTo(DiameterTransportStatus.Unsupported));
            Assert.That(connection.Connection, Is.Null);
            Assert.That(connection.Code, Is.EqualTo(support.Code));
            Assert.That(connection.Message, Is.EqualTo(support.Message));
            Assert.That(probe.CallCount, Is.EqualTo(3));
            Assert.That(probe.LastFamily, Is.EqualTo(AddressFamily.InterNetwork));
            Assert.That(probe.LastStreamCount, Is.EqualTo(3));
        });
    }

    [Test]
    public void ProbeRejectsWrongTransportSecurityAndStreamConfigurations()
    {
        DiameterSctpTransportFactory factory = new DiameterSctpTransportFactory();
        DiameterTransportConfiguration tcp = CreateSctpTransport(3868, false);
        tcp.Kind = DiameterTransportKind.Tcp;
        DiameterTransportSupportResult wrongTransport =
            factory.ProbeDiameterTransportSupport(tcp, CreateUnsecuredSecurity());
        DiameterSecurityConfiguration tls = new DiameterSecurityConfiguration();
        tls.Mode = DiameterSecurityMode.Tls;
        tls.LocalCertificatePath = "local.pfx";
        tls.RequirePeerCertificate = true;
        DiameterTransportSupportResult wrongSecurity =
            factory.ProbeDiameterTransportSupport(
                CreateSctpTransport(3868, false),
                tls);
        DiameterSecurityConfiguration implicitInsecure =
            new DiameterSecurityConfiguration();
        DiameterTransportSupportResult insecure =
            factory.ProbeDiameterTransportSupport(
                CreateSctpTransport(3868, false),
                implicitInsecure);
        DiameterTransportConfiguration excessiveStreams =
            CreateSctpTransport(3868, false);
        excessiveStreams.SctpStreamCount = ushort.MaxValue + 1;
        DiameterTransportSupportResult streamFailure =
            factory.ProbeDiameterTransportSupport(
                excessiveStreams,
                CreateUnsecuredSecurity());
        DiameterSecurityConfiguration certificateSettings = CreateUnsecuredSecurity();
        certificateSettings.LocalCertificatePath = "local.pfx";
        DiameterTransportSupportResult certificateFailure =
            factory.ProbeDiameterTransportSupport(
                CreateSctpTransport(3868, false),
                certificateSettings);

        Assert.Multiple(() =>
        {
            Assert.That(wrongTransport.Status,
                Is.EqualTo(DiameterTransportStatus.Unsupported));
            Assert.That(wrongTransport.Code, Is.EqualTo("transport-unsupported"));
            Assert.That(wrongSecurity.Status,
                Is.EqualTo(DiameterTransportStatus.Unsupported));
            Assert.That(wrongSecurity.Code, Is.EqualTo("security-unsupported"));
            Assert.That(insecure.Status,
                Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
            Assert.That(insecure.Code, Is.EqualTo("security-configuration-invalid"));
            Assert.That(streamFailure.Status,
                Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
            Assert.That(streamFailure.Code, Is.EqualTo("transport-configuration-invalid"));
            Assert.That(certificateFailure.Status,
                Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
            Assert.That(certificateFailure.Code,
                Is.EqualTo("certificate-configuration-invalid"));
        });
    }

    [Test]
    public void ProbeAndConnectionRejectEveryRemainingConfigurationBoundary()
    {
        DiameterSctpTransportFactory factory = new DiameterSctpTransportFactory();
        factory.CapabilityProbe = new DeterministicSupportedSctpProbe();
        DiameterTransportConfiguration valid = CreateSctpTransport(3868, false);
        DiameterSecurityConfiguration unsecured = CreateUnsecuredSecurity();
        Assert.That(factory.ProbeDiameterTransportSupport(null, unsecured).Status,
            Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
        Assert.That(factory.ProbeDiameterTransportSupport(valid, null).Status,
            Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));

        DiameterSecurityConfiguration ipsec = new DiameterSecurityConfiguration();
        ipsec.Mode = DiameterSecurityMode.Ipsec;
        ipsec.AllowUnsecuredTransport = true;
        Assert.That(factory.ProbeDiameterTransportSupport(valid, ipsec).Status,
            Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));

        Action<DiameterSecurityConfiguration>[] certificateSettings =
            new Action<DiameterSecurityConfiguration>[]
        {
            value => value.LocalCertificatePath = "certificate.pfx",
            value => value.LocalCertificatePassword = "password",
            value => value.TrustAnchorPath = "root.pem",
            value => value.ExpectedPeerName = "peer.example",
            value => value.RequirePeerCertificate = true,
            value => value.CheckCertificateRevocation = true,
            value => value.CertificatePeerPolicy =
                new RecordingCertificatePeerPolicy()
        };
        int securityIndex = 0;
        while (securityIndex < certificateSettings.Length)
        {
            DiameterSecurityConfiguration changed = CreateUnsecuredSecurity();
            certificateSettings[securityIndex](changed);
            Assert.That(factory.ProbeDiameterTransportSupport(valid, changed).Status,
                Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
            securityIndex = securityIndex + 1;
        }

        Action<DiameterTransportConfiguration>[] invalidTransport =
            new Action<DiameterTransportConfiguration>[]
        {
            value => value.Port = 0,
            value => value.Port = 65536,
            value => { value.EnableIpv4 = false; value.EnableIpv6 = false; },
            value => value.ReceiveBufferSize = 1,
            value => value.SendBufferSize = 1,
            value => value.ListenBacklog = 0,
            value => value.SctpStreamCount = 0,
            value => value.SctpStreamCount = ushort.MaxValue + 1
        };
        int transportIndex = 0;
        while (transportIndex < invalidTransport.Length)
        {
            DiameterTransportConfiguration changed =
                CreateSctpTransport(3868, false);
            invalidTransport[transportIndex](changed);
            Assert.That(factory.ProbeDiameterTransportSupport(
                changed, unsecured).Status,
                Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
            transportIndex = transportIndex + 1;
        }

        DiameterTransportConfiguration active =
            CreateSctpTransport(3868, false);
        active.EnableActiveConnections = true;
        DiameterEndpoint endpoint = CreateSctpEndpoint(3868);
        Assert.That(factory.ConnectDiameterEndpoint(
            null, active, unsecured, 1).Status,
            Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
        active.EnableActiveConnections = false;
        Assert.That(factory.ConnectDiameterEndpoint(
            endpoint, active, unsecured, 1).Status,
            Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
        active.EnableActiveConnections = true;
        endpoint.Port = 0;
        Assert.That(factory.ConnectDiameterEndpoint(
            endpoint, active, unsecured, 1).Status,
            Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
        endpoint.Port = 65536;
        Assert.That(factory.ConnectDiameterEndpoint(
            endpoint, active, unsecured, 1).Status,
            Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
        endpoint.Port = 3868;
        endpoint.Transport = DiameterTransportKind.Tcp;
        Assert.That(factory.ConnectDiameterEndpoint(
            endpoint, active, unsecured, 1).Status,
            Is.EqualTo(DiameterTransportStatus.InvalidConfiguration));
    }

    [Test]
    public void NativeOptionPayloadsEncodeStreamsOrderedDeliveryAndDiameterPpid()
    {
        byte[] initialization = SctpSocketSupport.CreateSctpInitializationBytes(7);
        byte[] sendParameters = SctpSocketSupport.CreateSctpSendParameterBytes(
            3,
            DiameterProtocolConstants.SctpPayloadProtocolIdentifier);
        int firstStreamCount;
        int secondStreamCount;
        int stream;
        if (BitConverter.IsLittleEndian)
        {
            firstStreamCount = initialization[0] | (initialization[1] << 8);
            secondStreamCount = initialization[2] | (initialization[3] << 8);
            stream = sendParameters[0] | (sendParameters[1] << 8);
        }
        else
        {
            firstStreamCount = (initialization[0] << 8) | initialization[1];
            secondStreamCount = (initialization[2] << 8) | initialization[3];
            stream = (sendParameters[0] << 8) | sendParameters[1];
        }

        Assert.Multiple(() =>
        {
            Assert.That(initialization, Has.Length.EqualTo(8));
            Assert.That(firstStreamCount, Is.EqualTo(7));
            Assert.That(secondStreamCount, Is.EqualTo(7));
            Assert.That(initialization.AsSpan(4).ToArray(),
                Is.EqualTo(new byte[] { 0, 0, 0, 0 }));
            Assert.That(sendParameters, Has.Length.EqualTo(32));
            Assert.That(stream, Is.EqualTo(3));
            Assert.That(sendParameters[4], Is.Zero);
            Assert.That(sendParameters[5], Is.Zero);
            Assert.That(sendParameters.AsSpan(8, 4).ToArray(),
                Is.EqualTo(new byte[] { 0, 0, 0, 46 }));
        });
    }

    [Test]
    public void ListenerAndConnectionReturnUnsupportedOrExchangeOrderedMessages()
    {
        DiameterSctpTransportFactory factory = new DiameterSctpTransportFactory();
        DiameterTransportConfiguration probeConfiguration = CreateSctpTransport(3868, false);
        probeConfiguration.SctpStreamCount = 2;
        DiameterTransportSupportResult support = factory.ProbeDiameterTransportSupport(
            probeConfiguration,
            CreateUnsecuredSecurity());
        if (support.Status == DiameterTransportStatus.Unsupported)
        {
            DiameterTransportConfiguration listenerConfiguration =
                CreateSctpTransport(3868, true);
            listenerConfiguration.SctpStreamCount = 2;
            DiameterListenerResult listener = factory.CreateDiameterListener(
                listenerConfiguration,
                CreateUnsecuredSecurity());
            DiameterConnectionResult connection = factory.ConnectDiameterEndpoint(
                CreateSctpEndpoint(3868),
                probeConfiguration,
                CreateUnsecuredSecurity(),
                1000);
            Assert.Multiple(() =>
            {
                Assert.That(listener.Status,
                    Is.EqualTo(DiameterTransportStatus.Unsupported));
                Assert.That(listener.Listener, Is.Null);
                Assert.That(listener.Code, Is.EqualTo("sctp-platform-unsupported"));
                Assert.That(connection.Status,
                    Is.EqualTo(DiameterTransportStatus.Unsupported));
                Assert.That(connection.Connection, Is.Null);
                Assert.That(connection.Code, Is.EqualTo("sctp-platform-unsupported"));
            });
            return;
        }

        int port = ReservePort();
        DiameterTransportConfiguration listenerTransport =
            CreateSctpTransport(port, true);
        listenerTransport.SctpStreamCount = 2;
        DiameterListenerResult created = factory.CreateDiameterListener(
            listenerTransport,
            CreateUnsecuredSecurity());
        Assert.That(created.Status, Is.EqualTo(DiameterTransportStatus.Success),
            created.Code + ": " + created.Message);
        DiameterTransportConfiguration activeTransport =
            CreateSctpTransport(port, false);
        activeTransport.SctpStreamCount = 2;
        DiameterConnectionResult active = factory.ConnectDiameterEndpoint(
            CreateSctpEndpoint(port),
            activeTransport,
            CreateUnsecuredSecurity(),
            5000);
        DiameterConnectionResult accepted = created.Listener!
            .AcceptDiameterConnection(5000);
        Assert.That(active.Status, Is.EqualTo(DiameterTransportStatus.Success));
        Assert.That(accepted.Status, Is.EqualTo(DiameterTransportStatus.Success));
        byte[] first = CreateFrame(1U);
        byte[] second = CreateFrame(2U);
        DiameterTransportIoResult firstSend = active.Connection!
            .SendDiameterConnectionBytes(first, 2000);
        byte[] firstDestination = new byte[64];
        DiameterTransportIoResult firstReceive = accepted.Connection!
            .ReceiveDiameterConnectionBytes(firstDestination, 2000);
        DiameterTransportIoResult secondSend = active.Connection!
            .SendDiameterConnectionBytes(second, 2000);
        byte[] secondDestination = new byte[64];
        DiameterTransportIoResult secondReceive = accepted.Connection!
            .ReceiveDiameterConnectionBytes(secondDestination, 2000);
        DiameterConnectionMetadata metadata = active.Connection!
            .ReadDiameterConnectionMetadata();
        accepted.Connection!.CloseDiameterConnection();
        active.Connection!.CloseDiameterConnection();
        created.Listener!.CloseDiameterListener();

        Assert.Multiple(() =>
        {
            Assert.That(firstSend.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(secondSend.Status, Is.EqualTo(DiameterTransportStatus.Success));
            Assert.That(firstDestination.AsSpan(0, firstReceive.TransferredCount).ToArray(),
                Is.EqualTo(first));
            Assert.That(secondDestination.AsSpan(0, secondReceive.TransferredCount).ToArray(),
                Is.EqualTo(second));
            Assert.That(metadata.OutboundStream, Is.EqualTo(1));
            Assert.That(metadata.Transport.MessageOriented, Is.True);
            Assert.That(metadata.Transport.MaximumOutboundStreams, Is.EqualTo(2));
            Assert.That(metadata.Transport.PayloadProtocolIdentifier,
                Is.EqualTo(DiameterProtocolConstants.SctpPayloadProtocolIdentifier));
        });
    }

    private static DiameterTransportConfiguration CreateSctpTransport(
        int port,
        bool listener)
    {
        DiameterTransportConfiguration result = new DiameterTransportConfiguration();
        result.Kind = DiameterTransportKind.Sctp;
        result.Host = "127.0.0.1";
        result.Port = port;
        result.EnableIpv4 = true;
        result.EnableIpv6 = false;
        result.EnableListener = listener;
        result.EnableActiveConnections = !listener;
        return result;
    }

    private static DiameterSecurityConfiguration CreateUnsecuredSecurity()
    {
        DiameterSecurityConfiguration result = new DiameterSecurityConfiguration();
        result.AllowUnsecuredTransport = true;
        return result;
    }

    private static DiameterEndpoint CreateSctpEndpoint(int port)
    {
        DiameterEndpoint result = new DiameterEndpoint();
        result.Identity.Value = "peer.example";
        result.Host = "127.0.0.1";
        result.Address = IPAddress.Loopback;
        result.Port = port;
        result.Transport = DiameterTransportKind.Sctp;
        return result;
    }

    private static byte[] CreateFrame(uint identifier)
    {
        byte[] result = new byte[20];
        result[0] = DiameterProtocolConstants.Version;
        result[3] = 20;
        result[12] = (byte)(identifier >> 24);
        result[13] = (byte)(identifier >> 16);
        result[14] = (byte)(identifier >> 8);
        result[15] = (byte)identifier;
        return result;
    }

    private static int ReservePort()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}

internal class DeterministicUnsupportedSctpProbe : ISctpSocketCapabilityProbe
{
    internal int CallCount;
    internal AddressFamily LastFamily;
    internal int LastStreamCount;

    public SctpProbeResult ProbeSctpAddressFamily(
        AddressFamily family,
        int streamCount)
    {
        CallCount = CallCount + 1;
        LastFamily = family;
        LastStreamCount = streamCount;
        SctpProbeResult result = new SctpProbeResult();
        result.Message = "SCTP was disabled by the deterministic test probe.";
        return result;
    }
}

internal class DeterministicSupportedSctpProbe : ISctpSocketCapabilityProbe
{
    public SctpProbeResult ProbeSctpAddressFamily(
        AddressFamily family,
        int streamCount)
    {
        SctpProbeResult result = new SctpProbeResult();
        result.Supported = true;
        return result;
    }
}
