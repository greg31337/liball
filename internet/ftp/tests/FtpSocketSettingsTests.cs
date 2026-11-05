using System.Net;
using System.Net.Sockets;

namespace ftp.Tests;

public class FtpSocketSettingsTests
{
    [Test]
    public void Public_socket_options_retain_OS_defaults_and_are_independently_owned()
    {
        FtpSocketOptions socket = new FtpSocketOptions();
        FtpClientOptions firstClient = new FtpClientOptions();
        FtpClientOptions secondClient = new FtpClientOptions();
        FtpListenerOptions listener = new FtpListenerOptions();
        FtpDataConnectionOptions data = new FtpDataConnectionOptions();

        Assert.Multiple(() =>
        {
            Assert.That(socket.NoDelay, Is.False);
            Assert.That(socket.KeepAlive, Is.False);
            Assert.That(socket.ReceiveBufferBytes, Is.Zero);
            Assert.That(socket.SendBufferBytes, Is.Zero);
            Assert.That(socket.ReceiveTimeoutMilliseconds, Is.Zero);
            Assert.That(socket.SendTimeoutMilliseconds, Is.Zero);
            Assert.That(socket.LingerEnabled, Is.False);
            Assert.That(socket.LingerSeconds, Is.Zero);
            Assert.That(firstClient.ControlSocket, Is.Not.Null);
            Assert.That(firstClient.ControlSocket,
                Is.Not.SameAs(secondClient.ControlSocket));
            Assert.That(listener.ControlSocket, Is.Not.Null);
            Assert.That(data.Socket, Is.Not.Null);
        });
    }

    [Test]
    public void Validation_rejects_unbounded_and_inconsistent_socket_settings()
    {
        FtpSocketOptions negativeReceiveBuffer = new FtpSocketOptions();
        negativeReceiveBuffer.ReceiveBufferBytes = -1;
        FtpSocketOptions largeSendBuffer = new FtpSocketOptions();
        largeSendBuffer.SendBufferBytes = FtpSocketSettings.MaximumBufferBytes + 1;
        FtpSocketOptions negativeReceiveTimeout = new FtpSocketOptions();
        negativeReceiveTimeout.ReceiveTimeoutMilliseconds = -1;
        FtpSocketOptions largeSendTimeout = new FtpSocketOptions();
        largeSendTimeout.SendTimeoutMilliseconds =
            FtpSocketSettings.MaximumTimeoutMilliseconds + 1;
        FtpSocketOptions negativeLinger = new FtpSocketOptions();
        negativeLinger.LingerEnabled = true;
        negativeLinger.LingerSeconds = -1;
        FtpSocketOptions largeLinger = new FtpSocketOptions();
        largeLinger.LingerEnabled = true;
        largeLinger.LingerSeconds = 65536;
        FtpSocketOptions disabledLinger = new FtpSocketOptions();
        disabledLinger.LingerSeconds = 1;

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() =>
                FtpSocketSettings.Validate(null));
            Assert.Throws<FtpConfigurationException>(() =>
                FtpSocketSettings.Validate(negativeReceiveBuffer));
            Assert.Throws<FtpConfigurationException>(() =>
                FtpSocketSettings.Validate(largeSendBuffer));
            Assert.Throws<FtpConfigurationException>(() =>
                FtpSocketSettings.Validate(negativeReceiveTimeout));
            Assert.Throws<FtpConfigurationException>(() =>
                FtpSocketSettings.Validate(largeSendTimeout));
            Assert.Throws<FtpConfigurationException>(() =>
                FtpSocketSettings.Validate(negativeLinger));
            Assert.Throws<FtpConfigurationException>(() =>
                FtpSocketSettings.Validate(largeLinger));
            Assert.Throws<FtpConfigurationException>(() =>
                FtpSocketSettings.Validate(disabledLinger));
        });
    }

    [Test]
    public void Configuration_validation_rejects_missing_control_and_data_socket_options()
    {
        FtpDataConnectionOptions data = new FtpDataConnectionOptions();
        data.Socket = null!;
        FtpListenerOptions listener = new FtpListenerOptions();
        listener.ControlSocket = null!;
        FtpClientOptions client = new FtpClientOptions();
        client.Endpoint.Host = "ftp.example.test";
        client.ControlSocket = null!;

        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() =>
                FtpConfigurationValidator.ValidateDataConnectionOptions(data));
            Assert.Throws<FtpConfigurationException>(() =>
                FtpConfigurationValidator.ValidateListenerOptions(listener));
            Assert.Throws<FtpConfigurationException>(() =>
                FtpConfigurationValidator.ValidateClientOptions(client));
        });
    }

    [Test]
    public void Snapshot_is_validated_and_independent_of_later_caller_mutation()
    {
        FtpSocketOptions options = CreateConfiguredOptions();

        FtpSocketOptions snapshot = FtpSocketSettings.CreateSnapshot(options);
        options.NoDelay = false;
        options.ReceiveBufferBytes = 0;
        options.LingerSeconds = 8;

        Assert.Multiple(() =>
        {
            Assert.That(snapshot, Is.Not.SameAs(options));
            Assert.That(snapshot.NoDelay, Is.True);
            Assert.That(snapshot.ReceiveBufferBytes, Is.EqualTo(32768));
            Assert.That(snapshot.LingerEnabled, Is.True);
            Assert.That(snapshot.LingerSeconds, Is.EqualTo(2));
        });
    }

    [Test]
    public void Built_in_TCP_settings_are_applied_and_non_TCP_sockets_are_rejected()
    {
        FtpSocketOptions options = CreateConfiguredOptions();
        using Socket tcp = new Socket(
            AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        using Socket udp = new Socket(
            AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        FtpSocketSettings.ApplyConnectedSocket(tcp, options);
        LingerOption? linger = tcp.LingerState;

        Assert.Multiple(() =>
        {
            Assert.That(tcp.NoDelay, Is.True);
            Assert.That(tcp.GetSocketOption(
                SocketOptionLevel.Socket, SocketOptionName.KeepAlive), Is.Not.EqualTo(0));
            Assert.That(tcp.ReceiveBufferSize, Is.GreaterThanOrEqualTo(32768));
            Assert.That(tcp.SendBufferSize, Is.GreaterThanOrEqualTo(32768));
            Assert.That(tcp.ReceiveTimeout, Is.EqualTo(1200));
            Assert.That(tcp.SendTimeout, Is.EqualTo(1300));
            Assert.That(linger!.Enabled, Is.True);
            Assert.That(linger.LingerTime, Is.EqualTo(2));
            Assert.Throws<FtpConfigurationException>(() =>
                FtpSocketSettings.ApplyConnectedSocket(udp, options));
        });
    }

    [Test]
    public void Configured_connect_applies_settings_before_opening_the_connection()
    {
        using TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        FtpSocketOptions options = CreateConfiguredOptions();

        using Socket connection = FtpSocketOperation.ConnectConfigured(
            IPAddress.Loopback,
            port,
            5000,
            default(CancellationToken),
            options);
        using Socket accepted = listener.AcceptSocket();

        Assert.Multiple(() =>
        {
            Assert.That(connection.Connected, Is.True);
            Assert.That(connection.NoDelay, Is.True);
            Assert.That(connection.ReceiveTimeout, Is.EqualTo(1200));
            Assert.That(connection.SendTimeout, Is.EqualTo(1300));
        });
    }

    [Test]
    public void Passive_outbound_connector_uses_its_defensive_socket_snapshot()
    {
        using TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        FtpDataConnectionOptions options = new FtpDataConnectionOptions();
        options.Socket = CreateConfiguredOptions();
        FtpPassiveConnector connector = new FtpPassiveConnector(
            IPAddress.Loopback, options);
        options.Socket.NoDelay = false;
        FtpReply reply = FtpEndpointCodec.CreateExtendedPassiveReply(port, '|');

        FtpPassiveConnection result = connector.OpenExtendedPassiveConnection(
            reply,
            FtpAddressFamily.IPv4,
            5000,
            default(CancellationToken));
        using Socket connection = result.Connection!;
        using Socket accepted = listener.AcceptSocket();

        Assert.Multiple(() =>
        {
            Assert.That(connection.NoDelay, Is.True);
            Assert.That(connection.ReceiveTimeout, Is.EqualTo(1200));
            Assert.That(connector.SocketOptions.NoDelay, Is.True);
        });
    }

    [Test]
    public void Active_accept_applies_configured_data_socket_settings()
    {
        FtpDataConnectionOptions options = new FtpDataConnectionOptions();
        options.Mode = FtpDataConnectionMode.Active;
        options.Socket = CreateConfiguredOptions();
        FtpActiveConnector connector = new FtpActiveConnector(
            IPAddress.Loopback, IPAddress.Loopback, options);
        FtpActiveListener listener = connector.OpenListener();
        using Socket client = new Socket(
            AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        client.Connect(IPAddress.Loopback, listener.BindEndpoint.Port);

        using Socket accepted = connector.AcceptConnection(
            listener, 5000, default(CancellationToken));

        Assert.Multiple(() =>
        {
            Assert.That(accepted.NoDelay, Is.True);
            Assert.That(accepted.ReceiveTimeout, Is.EqualTo(1200));
            Assert.That(accepted.SendTimeout, Is.EqualTo(1300));
        });
    }

    private static FtpSocketOptions CreateConfiguredOptions()
    {
        FtpSocketOptions options = new FtpSocketOptions();
        options.NoDelay = true;
        options.KeepAlive = true;
        options.ReceiveBufferBytes = 32768;
        options.SendBufferBytes = 32768;
        options.ReceiveTimeoutMilliseconds = 1200;
        options.SendTimeoutMilliseconds = 1300;
        options.LingerEnabled = true;
        options.LingerSeconds = 2;
        return options;
    }
}
