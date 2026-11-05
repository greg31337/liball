using System.Net;
using System.Net.Sockets;

namespace ftp.Tests;

public class FtpServerActiveConnectorTests
{
    [Test]
    public void PORT_accepts_a_numeric_same_peer_endpoint_and_returns_200()
    {
        FtpServerActiveConnector connector = CreateConnector(
            IPAddress.Parse("192.0.2.40"), true, false, true);

        FtpReply reply = connector.ConfigurePort("192,0,2,40,195,80");

        Assert.Multiple(() =>
        {
            Assert.That(reply.Code, Is.EqualTo(FtpReplyCodes.CommandOkay));
            Assert.That(reply.Classification.Succeeded, Is.True);
            Assert.That(connector.Configured, Is.True);
            Assert.That(connector.Extended, Is.False);
            Assert.That(connector.TargetEndpoint.Host, Is.EqualTo("192.0.2.40"));
            Assert.That(connector.TargetEndpoint.Port, Is.EqualTo(50000));
            Assert.That(connector.TargetEndpoint.AddressFamily,
                Is.EqualTo(FtpAddressFamily.IPv4));
        });
    }

    [Test]
    public void EPRT_accepts_IPv6_and_returns_522_for_an_unknown_protocol()
    {
        FtpServerActiveConnector connector = CreateConnector(
            IPAddress.Parse("2001:db8::40"), true, false, true);

        FtpReply accepted = connector.ConfigureExtendedPort(
            "|2|2001:db8::40|50000|");
        FtpReply unsupported = connector.ConfigureExtendedPort(
            "!300!future-address!50000!");
        FtpNetworkProtocolReply protocols =
            FtpEndpointCodec.ParseNetworkProtocolReply(unsupported);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Code, Is.EqualTo(FtpReplyCodes.CommandOkay));
            Assert.That(unsupported.Code,
                Is.EqualTo(FtpReplyCodes.NetworkProtocolNotSupported));
            Assert.That(protocols.SupportedProtocols, Is.EqualTo(new[] { 2 }));
            Assert.That(connector.Configured, Is.False);
        });
    }

    [Test]
    public void PORT_and_EPRT_reject_non_numeric_or_mismatched_endpoint_syntax()
    {
        FtpServerActiveConnector connector = CreateConnector(
            IPAddress.Parse("192.0.2.40"), true, false, true);

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() =>
                connector.ConfigurePort("ftp,example,test,40,195,80"));
            Assert.Throws<FtpProtocolException>(() =>
                connector.ConfigureExtendedPort("|1|ftp.example.test|50000|"));
            Assert.Throws<FtpProtocolException>(() =>
                connector.ConfigureExtendedPort("|1|2001:db8::40|50000|"));
            Assert.That(connector.Configured, Is.False);
        });
    }

    [Test]
    public void Foreign_targets_require_explicit_FXP_opt_in_even_without_peer_matching()
    {
        FtpServerActiveConnector matching = CreateConnector(
            IPAddress.Parse("192.0.2.40"), true, false, true);
        FtpServerActiveConnector noImplicitFxp = CreateConnector(
            IPAddress.Parse("192.0.2.40"), false, false, true);
        FtpServerActiveConnector fxp = CreateConnector(
            IPAddress.Parse("192.0.2.40"), false, true, true);

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() =>
                matching.ConfigureExtendedPort("|1|198.51.100.70|50000|"));
            Assert.Throws<FtpProtocolException>(() =>
                noImplicitFxp.ConfigureExtendedPort("|1|198.51.100.70|50000|"));
        });

        FtpReply reply = fxp.ConfigureExtendedPort(
            "|1|198.51.100.70|50000|");

        Assert.Multiple(() =>
        {
            Assert.That(reply.Code, Is.EqualTo(FtpReplyCodes.CommandOkay));
            Assert.That(fxp.Configured, Is.True);
            Assert.That(fxp.TargetEndpoint.Host, Is.EqualTo("198.51.100.70"));
        });
    }

    [Test]
    public void Active_setup_rejects_privileged_ports_and_prohibited_addresses()
    {
        FtpServerActiveConnector privileged = CreateConnector(
            IPAddress.Loopback, true, false, true);
        FtpServerActiveConnector foreign = CreateConnector(
            IPAddress.Loopback, false, true, false);

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpProtocolException>(() =>
                privileged.ConfigurePort("127,0,0,1,0,21"));
            Assert.Throws<FtpProtocolException>(() =>
                foreign.ConfigurePort("0,1,2,3,195,80"));
            Assert.Throws<FtpProtocolException>(() =>
                foreign.ConfigurePort("224,0,0,1,195,80"));
            Assert.Throws<FtpProtocolException>(() =>
                foreign.ConfigurePort("255,255,255,255,195,80"));
            Assert.Throws<FtpProtocolException>(() =>
                foreign.ConfigureExtendedPort("|2|ff02::1|50000|"));
        });
    }

    [Test]
    public void Privileged_port_policy_can_be_explicitly_disabled()
    {
        FtpServerActiveConnector connector = CreateConnector(
            IPAddress.Loopback, true, false, false);

        FtpReply reply = connector.ConfigurePort("127,0,0,1,0,21");

        Assert.Multiple(() =>
        {
            Assert.That(reply.Code, Is.EqualTo(FtpReplyCodes.CommandOkay));
            Assert.That(connector.TargetEndpoint.Port, Is.EqualTo(21));
        });
    }

    [TestCase(1, true)]
    [TestCase(21, true)]
    [TestCase(1023, true)]
    [TestCase(1024, false)]
    [TestCase(65535, false)]
    public void Active_port_blocklist_stops_at_the_privileged_port_boundary(
        int port,
        bool blocked)
    {
        FtpServerActiveConnector connector = CreateConnector(
            IPAddress.Loopback, true, false, true);
        FtpEndpoint endpoint = new FtpEndpoint();
        endpoint.Host = IPAddress.Loopback.ToString();
        endpoint.Port = port;
        endpoint.AddressFamily = FtpAddressFamily.IPv4;
        string argument = FtpEndpointCodec.FormatPortArgument(endpoint);

        if (blocked)
        {
            Assert.Throws<FtpProtocolException>(() =>
                connector.ConfigurePort(argument));
            Assert.That(connector.Configured, Is.False);
            return;
        }

        Assert.DoesNotThrow(() => connector.ConfigurePort(argument));
        Assert.That(connector.TargetEndpoint.Port, Is.EqualTo(port));
    }

    [Test]
    public void EPSV_ALL_clears_active_setup_blocks_PORT_and_EPRT_and_resets_with_session()
    {
        FtpServerActiveConnector connector = CreateConnector(
            IPAddress.Loopback, true, false, true);
        connector.ConfigurePort("127,0,0,1,195,80");

        connector.EnableExtendedPassiveOnly();

        Assert.Multiple(() =>
        {
            Assert.That(connector.Configured, Is.False);
            Assert.That(connector.ExtendedPassiveOnly, Is.True);
            Assert.Throws<FtpProtocolException>(() =>
                connector.ConfigurePort("127,0,0,1,195,80"));
            Assert.Throws<FtpProtocolException>(() =>
                connector.ConfigureExtendedPort("|1|127.0.0.1|50000|"));
        });

        connector.ResetSession();
        FtpReply reply = connector.ConfigurePort("127,0,0,1,195,80");

        Assert.Multiple(() =>
        {
            Assert.That(reply.Code, Is.EqualTo(FtpReplyCodes.CommandOkay));
            Assert.That(connector.ExtendedPassiveOnly, Is.False);
            Assert.That(connector.Configured, Is.True);
        });
    }

    [Test]
    public void Outbound_connection_is_bounded_and_consumes_the_setup_once()
    {
        using TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        FtpServerActiveConnector connector = CreateConnector(
            IPAddress.Loopback, true, false, true);
        FtpEndpoint endpoint = new FtpEndpoint();
        endpoint.Host = IPAddress.Loopback.ToString();
        endpoint.Port = port;
        endpoint.AddressFamily = FtpAddressFamily.IPv4;
        connector.ConfigurePort(FtpEndpointCodec.FormatPortArgument(endpoint));

        using Socket outbound = connector.OpenOutboundConnection(
            5000, default(CancellationToken));
        using Socket accepted = listener.AcceptSocket();

        Assert.Multiple(() =>
        {
            Assert.That(outbound.Connected, Is.True);
            Assert.That(accepted.Connected, Is.True);
            Assert.That(connector.Configured, Is.False);
            Assert.That(connector.TargetAddress, Is.Null);
            Assert.Throws<FtpConfigurationException>(() =>
                connector.OpenOutboundConnection(5000, default(CancellationToken)));
        });
    }

    [Test]
    public void Invalid_server_active_configuration_is_rejected()
    {
        FtpDataConnectionOptions passive = CreateOptions(true, false, true);
        passive.Mode = FtpDataConnectionMode.Passive;

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() =>
                new FtpServerActiveConnector(null, CreateOptions(true, false, true)));
            Assert.Throws<FtpConfigurationException>(() =>
                new FtpServerActiveConnector(IPAddress.Any,
                    CreateOptions(true, false, true)));
            Assert.Throws<FtpConfigurationException>(() =>
                new FtpServerActiveConnector(IPAddress.Loopback, passive));
        });
    }

    private static FtpServerActiveConnector CreateConnector(
        IPAddress controlPeer,
        bool requirePeerMatch,
        bool allowForeign,
        bool rejectPrivilegedPorts)
    {
        return new FtpServerActiveConnector(
            controlPeer,
            CreateOptions(requirePeerMatch, allowForeign, rejectPrivilegedPorts));
    }

    private static FtpDataConnectionOptions CreateOptions(
        bool requirePeerMatch,
        bool allowForeign,
        bool rejectPrivilegedPorts)
    {
        FtpDataConnectionOptions options = new FtpDataConnectionOptions();
        options.Mode = FtpDataConnectionMode.Active;
        options.RequireDataPeerMatch = requirePeerMatch;
        options.AllowForeignDataConnections = allowForeign;
        options.RejectPrivilegedActivePorts = rejectPrivilegedPorts;
        return options;
    }
}
