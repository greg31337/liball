using System.Net;
using System.Net.Sockets;

namespace ftp.Tests;

public class FtpPassiveConnectorTests
{
    [Test]
    public void EPSV_command_uses_control_protocol_or_matching_explicit_protocol()
    {
        FtpPassiveConnector ipv4 = new FtpPassiveConnector(
            IPAddress.Loopback, CreateOptions(false));
        FtpPassiveConnector ipv6 = new FtpPassiveConnector(
            IPAddress.IPv6Loopback, CreateOptions(false));

        FtpCommand controlProtocol = ipv4.CreateExtendedPassiveCommand(
            FtpAddressFamily.Unspecified);
        FtpCommand explicitIpv4 = ipv4.CreateExtendedPassiveCommand(FtpAddressFamily.IPv4);
        FtpCommand explicitIpv6 = ipv6.CreateExtendedPassiveCommand(FtpAddressFamily.IPv6);

        Assert.Multiple(() =>
        {
            Assert.That(controlProtocol.Kind, Is.EqualTo(FtpCommandKind.Epsv));
            Assert.That(controlProtocol.Verb, Is.EqualTo("EPSV"));
            Assert.That(controlProtocol.Argument, Is.Null);
            Assert.That(explicitIpv4.Argument, Is.EqualTo("1"));
            Assert.That(explicitIpv6.Argument, Is.EqualTo("2"));
            Assert.Throws<FtpConfigurationException>(() =>
                ipv4.CreateExtendedPassiveCommand(FtpAddressFamily.IPv6));
            Assert.Throws<FtpConfigurationException>(() =>
                ipv6.CreateExtendedPassiveCommand(FtpAddressFamily.IPv4));
        });
    }

    [Test]
    public void EPSV_ALL_is_sent_once_and_acceptance_enables_extended_passive_only_state()
    {
        FtpPassiveConnector connector = new FtpPassiveConnector(
            IPAddress.Loopback, CreateOptions(true));

        FtpCommand? all = connector.CreateExtendedPassiveAllCommand();

        Assert.Multiple(() =>
        {
            Assert.That(all, Is.Not.Null);
            Assert.That(all!.Kind, Is.EqualTo(FtpCommandKind.Epsv));
            Assert.That(all.Argument, Is.EqualTo("ALL"));
            Assert.That(connector.CreateExtendedPassiveAllCommand(), Is.Null);
            Assert.Throws<FtpProtocolException>(() =>
                connector.CreateExtendedPassiveCommand(FtpAddressFamily.Unspecified));
        });

        connector.ApplyExtendedPassiveAllReply(CreateReply(200, "EPSV only"));
        FtpCommand setup = connector.CreateExtendedPassiveCommand(FtpAddressFamily.Unspecified);

        Assert.Multiple(() =>
        {
            Assert.That(connector.ExtendedPassiveAllAttempted, Is.True);
            Assert.That(connector.ExtendedPassiveAllReplyApplied, Is.True);
            Assert.That(connector.ExtendedPassiveOnly, Is.True);
            Assert.That(setup.Argument, Is.Null);
            Assert.Throws<FtpProtocolException>(() =>
                connector.ApplyExtendedPassiveAllReply(CreateReply(200, "duplicate")));
        });
    }

    [Test]
    public void Rejected_EPSV_ALL_does_not_enable_restricted_state_but_allows_EPSV()
    {
        FtpPassiveConnector connector = new FtpPassiveConnector(
            IPAddress.Loopback, CreateOptions(true));
        connector.CreateExtendedPassiveAllCommand();

        connector.ApplyExtendedPassiveAllReply(CreateReply(500, "not supported"));
        FtpCommand setup = connector.CreateExtendedPassiveCommand(FtpAddressFamily.IPv4);

        Assert.Multiple(() =>
        {
            Assert.That(connector.ExtendedPassiveOnly, Is.False);
            Assert.That(setup.Argument, Is.EqualTo("1"));
        });
    }

    [Test]
    public void EPSV_connection_reuses_the_numeric_control_peer_and_server_port()
    {
        using TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        FtpReply reply = FtpEndpointCodec.CreateExtendedPassiveReply(port, '!');
        FtpPassiveConnector connector = new FtpPassiveConnector(
            IPAddress.Loopback, CreateOptions(false));

        FtpPassiveConnection result = connector.OpenExtendedPassiveConnection(
            reply,
            FtpAddressFamily.Unspecified,
            5000,
            default(CancellationToken));
        using Socket accepted = listener.AcceptSocket();
        using Socket connection = result.Connection!;
        IPEndPoint remote = (IPEndPoint)connection.RemoteEndPoint!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Extended, Is.True);
            Assert.That(result.UsedControlPeerAddress, Is.True);
            Assert.That(result.Endpoint.Host, Is.EqualTo(IPAddress.Loopback.ToString()));
            Assert.That(result.Endpoint.Port, Is.EqualTo(port));
            Assert.That(result.Endpoint.AddressFamily, Is.EqualTo(FtpAddressFamily.IPv4));
            Assert.That(remote.Address, Is.EqualTo(IPAddress.Loopback));
            Assert.That(remote.Port, Is.EqualTo(port));
            Assert.That(connection.Connected, Is.True);
            Assert.That(accepted.Connected, Is.True);
        });
    }

    [Test]
    public void Canceled_and_expired_EPSV_connections_stop_before_socket_setup()
    {
        FtpPassiveConnector connector = new FtpPassiveConnector(
            IPAddress.Loopback, CreateOptions(false));
        FtpReply reply = FtpEndpointCodec.CreateExtendedPassiveReply(65000, '|');
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Multiple(() =>
        {
            Assert.Throws<OperationCanceledException>(() =>
                connector.OpenExtendedPassiveConnection(
                    reply, FtpAddressFamily.IPv4, 5000, cancellation.Token));
            Assert.Throws<TimeoutException>(() =>
                connector.OpenExtendedPassiveConnection(
                    reply, FtpAddressFamily.IPv4, 0, default(CancellationToken)));
        });
    }

    [Test]
    public void Refused_EPSV_socket_connection_is_mapped_to_connection_failure()
    {
        int closedPort;
        using (TcpListener listener = new TcpListener(IPAddress.Loopback, 0))
        {
            listener.Start();
            closedPort = ((IPEndPoint)listener.LocalEndpoint).Port;
        }

        FtpPassiveConnector connector = new FtpPassiveConnector(
            IPAddress.Loopback, CreateOptions(false));
        FtpReply reply = FtpEndpointCodec.CreateExtendedPassiveReply(closedPort, '|');

        FtpConnectionException? error = Assert.Throws<FtpConnectionException>(() =>
            connector.OpenExtendedPassiveConnection(
                reply, FtpAddressFamily.IPv4, 5000, default(CancellationToken)));

        Assert.That(error!.InnerException, Is.TypeOf<SocketException>());
    }

    [Test]
    public void Malformed_EPSV_replies_and_invalid_connector_configuration_are_rejected()
    {
        FtpDataConnectionOptions active = CreateOptions(false);
        active.Mode = FtpDataConnectionMode.Active;
        FtpDataConnectionOptions noEpsv = CreateOptions(false);
        noEpsv.UseExtendedPassive = false;
        FtpPassiveConnector pasvOnly = new FtpPassiveConnector(IPAddress.Loopback, noEpsv);
        FtpPassiveConnector connector = new FtpPassiveConnector(
            IPAddress.Loopback, CreateOptions(false));
        FtpReply malformed = CreateReply(229, "Entering Extended Passive Mode (|||0|)");

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() =>
                new FtpPassiveConnector(null, CreateOptions(false)));
            Assert.Throws<FtpConfigurationException>(() =>
                new FtpPassiveConnector(IPAddress.Loopback, active));
            Assert.That(pasvOnly.CreatePassiveCommand().Kind, Is.EqualTo(FtpCommandKind.Pasv));
            Assert.Throws<FtpConfigurationException>(() =>
                pasvOnly.CreateExtendedPassiveCommand(FtpAddressFamily.IPv4));
            Assert.Throws<FtpProtocolException>(() =>
                connector.OpenExtendedPassiveConnection(
                    malformed,
                    FtpAddressFamily.IPv4,
                    5000,
                    default(CancellationToken)));
            Assert.Throws<FtpConfigurationException>(() =>
                FtpSocketOperation.Connect(IPAddress.Loopback, 0, 1000,
                    default(CancellationToken)));
        });
    }

    private static FtpDataConnectionOptions CreateOptions(bool sendAll)
    {
        FtpDataConnectionOptions options = new FtpDataConnectionOptions();
        options.SendExtendedPassiveAll = sendAll;
        if (sendAll)
        {
            options.AllowPassiveFallback = false;
        }

        return options;
    }

    private static FtpReply CreateReply(int code, string text)
    {
        FtpReply reply = new FtpReply();
        reply.Code = code;
        reply.Text = text;
        reply.Classification = FtpReplyClassifier.ClassifyCode(code);
        return reply;
    }
}
