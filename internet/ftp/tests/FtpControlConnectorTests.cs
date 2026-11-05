using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ftp.Tests;

public class FtpControlConnectorTests
{
    [Test]
    public void Public_client_resolves_DNS_binds_locally_and_accepts_ready_greeting()
    {
        using TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        Task server = SendGreeting(listener, "220 Ready for FTP\r\n");
        FtpClientOptions options = CreatePlainOptions("localhost", port);
        options.Endpoint.AddressFamily = FtpAddressFamily.IPv4;
        options.LocalBindEndpoint = new FtpEndpoint();
        options.LocalBindEndpoint.Host = IPAddress.Loopback.ToString();
        options.LocalBindEndpoint.Port = 0;
        options.LocalBindEndpoint.AddressFamily = FtpAddressFamily.IPv4;
        FtpClient client = new FtpClient(null);

        client.Connect(options);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Connected));
            Assert.That(client.State.Connection, Is.Not.Null);
            Assert.That(client.State.Connection!.Greeting!.Code, Is.EqualTo(FtpReplyCodes.ServiceReady));
            Assert.That(client.State.Connection.Greeting.Text, Is.EqualTo("Ready for FTP"));
            Assert.That(client.State.Connection.DelayedGreeting, Is.False);
            Assert.That(client.State.Connection.LocalEndpoint.Host, Is.EqualTo(IPAddress.Loopback.ToString()));
            Assert.That(client.State.Connection.LocalEndpoint.Port, Is.GreaterThan(0));
            Assert.That(client.State.Connection.RemoteEndpoint.Port, Is.EqualTo(port));
            Assert.That(client.State.Options!.LocalBindEndpoint, Is.Not.SameAs(options.LocalBindEndpoint));
        });

        client.AbortConnection();
        Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Closed));
    }

    [Test]
    public void Delayed_greeting_waits_for_one_terminal_ready_reply()
    {
        using TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        Task server = SendGreeting(
            listener,
            "120 Service ready in one minute\r\n220 Service ready now\r\n");
        FtpClient client = new FtpClient(null);

        client.Connect(CreatePlainOptions(IPAddress.Loopback.ToString(), port));
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(client.State.Connection!.DelayedGreeting, Is.True);
            Assert.That(client.State.Connection.InitialGreeting!.Code, Is.EqualTo(FtpReplyCodes.ServiceReadySoon));
            Assert.That(client.State.Connection.Greeting!.Code, Is.EqualTo(FtpReplyCodes.ServiceReady));
            Assert.That(client.State.Connection.Greeting.Text, Is.EqualTo("Service ready now"));
        });

        client.Disconnect();
    }

    [Test]
    public void Server_rejection_maps_to_connection_failure_and_cleans_up()
    {
        using TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        Task server = SendGreeting(listener, "421 Service unavailable\r\n");
        FtpClient client = new FtpClient(null);

        FtpConnectionException failure = Assert.Throws<FtpConnectionException>(() =>
            client.Connect(CreatePlainOptions(IPAddress.Loopback.ToString(), port)))!;
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(failure.Message, Does.Contain("421"));
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Disconnected));
            Assert.That(client.State.Connection, Is.Null);
        });
    }

    [Test]
    public void Malformed_greeting_maps_to_protocol_failure_and_cleans_up()
    {
        using TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        Task server = SendGreeting(listener, "220 malformed\n");
        FtpClient client = new FtpClient(null);

        Assert.Throws<FtpProtocolException>(() =>
            client.Connect(CreatePlainOptions(IPAddress.Loopback.ToString(), port)));
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Disconnected));
            Assert.That(client.State.Connection, Is.Null);
        });
    }

    [Test]
    public void Cancellation_during_greeting_closes_the_control_socket()
    {
        using TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        using ManualResetEventSlim accepted = new ManualResetEventSlim(false);
        using ManualResetEventSlim release = new ManualResetEventSlim(false);
        Task server = HoldConnection(listener, accepted, release);
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.CancelAfter(100);
        FtpClientOptions options = CreatePlainOptions(IPAddress.Loopback.ToString(), port);
        options.Timeouts.CancellationToken = cancellation.Token;
        options.Timeouts.ControlReadTimeoutMilliseconds = 5000;
        FtpClient client = new FtpClient(null);

        Assert.Throws<OperationCanceledException>(() => client.Connect(options));
        Assert.That(accepted.IsSet, Is.True);
        release.Set();
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Disconnected));
            Assert.That(client.State.Connection, Is.Null);
        });
    }

    [Test]
    public void Greeting_timeout_is_reported_and_closes_the_control_socket()
    {
        using TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        using ManualResetEventSlim accepted = new ManualResetEventSlim(false);
        using ManualResetEventSlim release = new ManualResetEventSlim(false);
        Task server = HoldConnection(listener, accepted, release);
        FtpClientOptions options = CreatePlainOptions(IPAddress.Loopback.ToString(), port);
        options.Timeouts.ControlReadTimeoutMilliseconds = 100;
        FtpClient client = new FtpClient(null);

        Assert.Throws<TimeoutException>(() => client.Connect(options));
        Assert.That(accepted.IsSet, Is.True);
        release.Set();
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Disconnected));
            Assert.That(client.State.Connection, Is.Null);
        });
    }

    [Test]
    public void Refused_address_attempt_maps_to_connection_failure()
    {
        int port;
        using (TcpListener reservation = new TcpListener(IPAddress.Loopback, 0))
        {
            reservation.Start();
            port = ((IPEndPoint)reservation.LocalEndpoint).Port;
        }

        FtpClient client = new FtpClient(null);
        FtpClientOptions options = CreatePlainOptions(IPAddress.Loopback.ToString(), port);
        options.Timeouts.ConnectTimeoutMilliseconds = 1000;

        FtpConnectionException failure = Assert.Throws<FtpConnectionException>(
            () => client.Connect(options))!;

        Assert.Multiple(() =>
        {
            Assert.That(failure.Message, Does.Contain("resolved FTP endpoint"));
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Disconnected));
            Assert.That(client.State.Connection, Is.Null);
        });
    }

    [Test]
    public void IPv6_control_connection_uses_the_requested_address_family()
    {
        if (!Socket.OSSupportsIPv6)
        {
            Assert.Ignore("IPv6 is not supported on this host.");
        }

        using TcpListener listener = new TcpListener(IPAddress.IPv6Loopback, 0);
        listener.Server.DualMode = false;
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        Task server = SendGreeting(listener, "220 IPv6 ready\r\n");
        FtpClientOptions options = CreatePlainOptions(IPAddress.IPv6Loopback.ToString(), port);
        options.Endpoint.AddressFamily = FtpAddressFamily.IPv6;
        FtpClient client = new FtpClient(null);

        client.Connect(options);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(client.State.Connection!.RemoteEndpoint.AddressFamily, Is.EqualTo(FtpAddressFamily.IPv6));
            Assert.That(client.State.Connection.Greeting!.Text, Is.EqualTo("IPv6 ready"));
        });

        client.AbortConnection();
    }

    [Test]
    public void Implicit_TLS_connection_failure_never_changes_to_plaintext()
    {
        FtpClientOptions options = new FtpClientOptions();
        options.Endpoint.Host = IPAddress.Loopback.ToString();
        options.SecurityMode = FtpSecurityMode.ImplicitTls;
        FtpClient client = new FtpClient(new StrictFtpCertificateValidator());

        FtpConnectionException failure = Assert.Throws<FtpConnectionException>(
            () => client.Connect(options))!;

        Assert.Multiple(() =>
        {
            Assert.That(failure.Message, Does.Not.Contain("plaintext fallback"));
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Disconnected));
            Assert.That(client.State.Connection, Is.Null);
        });
    }

    private static FtpClientOptions CreatePlainOptions(string host, int port)
    {
        FtpClientOptions options = new FtpClientOptions();
        options.Endpoint.Host = host;
        options.Endpoint.Port = port;
        options.SecurityMode = FtpSecurityMode.Plain;
        options.NegotiateFeatures = false;
        options.Timeouts.ConnectTimeoutMilliseconds = 5000;
        options.Timeouts.ControlReadTimeoutMilliseconds = 5000;
        return options;
    }

    private static Task SendGreeting(TcpListener listener, string greeting)
    {
        return Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            byte[] bytes = Encoding.UTF8.GetBytes(greeting);
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush();
        });
    }

    private static Task HoldConnection(
        TcpListener listener,
        ManualResetEventSlim acceptedSignal,
        ManualResetEventSlim releaseSignal)
    {
        return Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            acceptedSignal.Set();
            releaseSignal.Wait(5000);
        });
    }
}
