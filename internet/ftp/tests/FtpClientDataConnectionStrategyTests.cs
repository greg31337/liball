using System.Net;
using System.Net.Sockets;

namespace ftp.Tests;

public class FtpClientDataConnectionStrategyTests
{
    [Test]
    public void Passive_mode_uses_EPSV_and_preopens_the_data_connection()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            string command = ReadCommand(stream);
            commands.Add(command);
            WriteReply(stream, "229 Entering Extended Passive Mode (|||" +
                GetPort(dataListener).ToString() + "|)\r\n");
            using TcpClient data = dataListener.AcceptTcpClient();
        });
        FtpClient client = ConnectAuthenticated(
            controlListener, FtpDataConnectionMode.Passive);

        FtpClientDataConnectionStrategy strategy = client.PrepareDataConnection();
        FtpReply preliminary = strategy.BeginTransfer();
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "EPSV",
            }));
            Assert.That(strategy.SelectedMode, Is.EqualTo(FtpDataConnectionMode.Passive));
            Assert.That(strategy.Coordinator.Preopened, Is.True);
            Assert.That(preliminary.Code,
                Is.EqualTo(FtpReplyCodes.DataConnectionAlreadyOpen));
        });

        strategy.AbortDataConnection();
        client.AbortConnection();
    }

    [Test]
    public void Passive_mode_falls_back_from_unsupported_EPSV_to_IPV4_PASV()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "500 EPSV is not implemented\r\n");
            int port = GetPort(dataListener);
            Receive(stream, commands, "227 Entering Passive Mode (127,0,0,1," +
                (port / 256).ToString() + "," + (port % 256).ToString() + ")\r\n");
            using TcpClient data = dataListener.AcceptTcpClient();
        });
        FtpClient client = ConnectAuthenticated(
            controlListener, FtpDataConnectionMode.Passive);

        FtpClientDataConnectionStrategy strategy = client.PrepareDataConnection();
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "EPSV", "PASV",
            }));
            Assert.That(strategy.SetupCommands.Select(command => command.Kind),
                Is.EqualTo(new[] { FtpCommandKind.Epsv, FtpCommandKind.Pasv }));
            Assert.That(strategy.Coordinator.Preopened, Is.True);
            Assert.That(client.State.Session.State.ExtendedDataConnection, Is.False);
        });

        strategy.AbortDataConnection();
        client.AbortConnection();
    }

    [Test]
    public void Passive_mode_sends_EPSV_ALL_before_opening_an_extended_connection()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 EPSV ALL accepted\r\n");
            string command = ReadCommand(stream);
            commands.Add(command);
            WriteReply(stream, "229 Entering Extended Passive Mode (|||" +
                GetPort(dataListener).ToString() + "|)\r\n");
            using TcpClient data = dataListener.AcceptTcpClient();
        });
        FtpClientOptions options = CreateOptions(
            controlListener, FtpDataConnectionMode.Passive, FtpAddressFamily.IPv4);
        options.DataConnection.SendExtendedPassiveAll = true;
        options.DataConnection.AllowPassiveFallback = false;
        FtpClient client = ConnectAuthenticatedWithOptions(options);

        FtpClientDataConnectionStrategy strategy = client.PrepareDataConnection();
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "EPSV ALL", "EPSV",
            }));
            Assert.That(strategy.SetupCommands.Select(command => command.Kind),
                Is.EqualTo(new[] { FtpCommandKind.Epsv, FtpCommandKind.Epsv }));
            Assert.That(client.State.Session.State.ExtendedPassiveOnly, Is.True);
            Assert.That(strategy.Coordinator.Preopened, Is.True);
        });

        strategy.AbortDataConnection();
        client.AbortConnection();
    }

    [Test]
    public void Passive_IPV6_mode_uses_EPSV_and_the_control_peer_address()
    {
        if (!Socket.OSSupportsIPv6)
        {
            Assert.Ignore("IPv6 is not supported on this host.");
        }

        using TcpListener controlListener = StartIpv6Listener();
        using TcpListener dataListener = StartIpv6Listener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            string command = ReadCommand(stream);
            commands.Add(command);
            WriteReply(stream, "229 Entering Extended Passive Mode (|||" +
                GetPort(dataListener).ToString() + "|)\r\n");
            using TcpClient data = dataListener.AcceptTcpClient();
        });
        FtpClientOptions options = CreateOptions(
            controlListener, FtpDataConnectionMode.Passive, FtpAddressFamily.IPv6);
        FtpClient client = ConnectAuthenticatedWithOptions(options);

        FtpClientDataConnectionStrategy strategy = client.PrepareDataConnection();
        server.GetAwaiter().GetResult();
        IPEndPoint remote = (IPEndPoint)strategy.Coordinator.Connection!.RemoteEndPoint!;

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "EPSV",
            }));
            Assert.That(remote.Address, Is.EqualTo(IPAddress.IPv6Loopback));
            Assert.That(client.State.Connection!.RemoteEndpoint.AddressFamily,
                Is.EqualTo(FtpAddressFamily.IPv6));
            Assert.That(strategy.SelectedMode, Is.EqualTo(FtpDataConnectionMode.Passive));
        });

        strategy.AbortDataConnection();
        client.AbortConnection();
    }

    [Test]
    public void PASV_NAT_reply_uses_the_control_peer_address_by_default()
    {
        using TcpListener controlListener = StartListener();
        using TcpListener dataListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            int port = GetPort(dataListener);
            Receive(stream, commands, "227 Entering Passive Mode (10,0,0,5," +
                (port / 256).ToString() + "," + (port % 256).ToString() + ")\r\n");
            using TcpClient data = dataListener.AcceptTcpClient();
        });
        FtpClientOptions options = CreateOptions(
            controlListener, FtpDataConnectionMode.Passive, FtpAddressFamily.IPv4);
        options.DataConnection.UseExtendedPassive = false;
        FtpClient client = ConnectAuthenticatedWithOptions(options);

        FtpClientDataConnectionStrategy strategy = client.PrepareDataConnection();
        server.GetAwaiter().GetResult();
        IPEndPoint remote = (IPEndPoint)strategy.Coordinator.Connection!.RemoteEndPoint!;

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "PASV",
            }));
            Assert.That(remote.Address, Is.EqualTo(IPAddress.Loopback));
            Assert.That(remote.Port, Is.EqualTo(GetPort(dataListener)));
            Assert.That(strategy.Coordinator.Preopened, Is.True);
        });

        strategy.AbortDataConnection();
        client.AbortConnection();
    }

    [Test]
    public void Honored_PASV_NAT_mismatch_fails_setup_and_preserves_the_control_connection()
    {
        using TcpListener controlListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands,
                "227 Entering Passive Mode (10,0,0,5,195,80)\r\n");
            Receive(stream, commands, "200 NOOP okay\r\n");
        });
        FtpClientOptions options = CreateOptions(
            controlListener, FtpDataConnectionMode.Passive, FtpAddressFamily.IPv4);
        options.DataConnection.UseExtendedPassive = false;
        options.DataConnection.IgnorePassiveAdvertisedAddress = false;
        FtpClient client = ConnectAuthenticatedWithOptions(options);

        FtpConnectionException? failure = Assert.Throws<FtpConnectionException>(() =>
            client.PrepareDataConnection());
        FtpCommandResult noop = client.SendNoOperation();
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(failure!.Message, Does.Contain("does not match the control peer"));
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER alice", "PASS secret", "PASV", "NOOP",
            }));
            Assert.That(client.State.DataStrategy, Is.Null);
            Assert.That(noop.Succeeded, Is.True);
        });

        client.AbortConnection();
    }

    [Test]
    public void Active_mode_falls_back_from_unsupported_EPRT_to_PORT_and_accepts_once()
    {
        using TcpListener controlListener = StartListener();
        List<string> commands = new List<string>();
        FtpEndpoint advertised = new FtpEndpoint();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "522 Network protocol not supported, use (1)\r\n");
            string portCommand = ReadCommand(stream);
            commands.Add(portCommand);
            advertised = FtpEndpointCodec.ParsePortArgument(
                portCommand.Substring("PORT ".Length));
            WriteReply(stream, "200 PORT accepted\r\n");
        });
        FtpClient client = ConnectAuthenticated(
            controlListener, FtpDataConnectionMode.Active);

        FtpClientDataConnectionStrategy strategy = client.PrepareDataConnection();
        FtpReply preliminary = strategy.BeginTransfer();
        server.GetAwaiter().GetResult();
        using TcpClient dataPeer = new TcpClient(AddressFamily.InterNetwork);
        dataPeer.Connect(IPAddress.Parse(advertised.Host), advertised.Port);
        using Socket accepted = strategy.Coordinator.OpenDataConnection(
            5000, default(CancellationToken));

        Assert.Multiple(() =>
        {
            Assert.That(commands[2], Does.StartWith("EPRT |1|127.0.0.1|"));
            Assert.That(commands[3], Does.StartWith("PORT 127,0,0,1,"));
            Assert.That(strategy.SetupCommands.Select(command => command.Kind),
                Is.EqualTo(new[] { FtpCommandKind.Eprt, FtpCommandKind.Port }));
            Assert.That(strategy.SelectedMode, Is.EqualTo(FtpDataConnectionMode.Active));
            Assert.That(preliminary.Code,
                Is.EqualTo(FtpReplyCodes.FileStatusOkayOpeningDataConnection));
            Assert.That(accepted.Connected, Is.True);
            Assert.That(strategy.ActiveListener!.Open, Is.False);
        });

        strategy.AbortDataConnection();
        client.AbortConnection();
    }

    [Test]
    public void Active_IPV6_mode_uses_EPRT_and_accepts_the_control_peer()
    {
        if (!Socket.OSSupportsIPv6)
        {
            Assert.Ignore("IPv6 is not supported on this host.");
        }

        using TcpListener controlListener = StartIpv6Listener();
        List<string> commands = new List<string>();
        FtpEndpoint advertised = new FtpEndpoint();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            string command = ReadCommand(stream);
            commands.Add(command);
            advertised = FtpEndpointCodec.ParseExtendedPortArgument(
                command.Substring("EPRT ".Length));
            WriteReply(stream, "200 EPRT accepted\r\n");
            using TcpClient data = new TcpClient(AddressFamily.InterNetworkV6);
            data.Connect(IPAddress.Parse(advertised.Host), advertised.Port);
        });
        FtpClientOptions options = CreateOptions(
            controlListener, FtpDataConnectionMode.Active, FtpAddressFamily.IPv6);
        FtpClient client = ConnectAuthenticatedWithOptions(options);

        FtpClientDataConnectionStrategy strategy = client.PrepareDataConnection();
        strategy.BeginTransfer();
        using Socket accepted = strategy.Coordinator.OpenDataConnection(
            5000, default(CancellationToken));
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands[2], Does.StartWith("EPRT |2|::1|"));
            Assert.That(advertised.AddressFamily, Is.EqualTo(FtpAddressFamily.IPv6));
            Assert.That(((IPEndPoint)accepted.RemoteEndPoint!).Address,
                Is.EqualTo(IPAddress.IPv6Loopback));
            Assert.That(strategy.SetupCommands.Select(command => command.Kind),
                Is.EqualTo(new[] { FtpCommandKind.Eprt }));
            Assert.That(strategy.ActiveListener!.Open, Is.False);
        });

        strategy.AbortDataConnection();
        client.AbortConnection();
    }

    [Test]
    public void Active_accept_timeout_closes_the_one_shot_listener()
    {
        using TcpListener controlListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "200 EPRT accepted\r\n");
        });
        FtpClient client = ConnectAuthenticated(
            controlListener, FtpDataConnectionMode.Active);

        FtpClientDataConnectionStrategy strategy = client.PrepareDataConnection();
        strategy.BeginTransfer();
        Socket listeningSocket = strategy.ActiveListener!.Listener!;
        TimeoutException? failure = Assert.Throws<TimeoutException>(() =>
            strategy.Coordinator.OpenDataConnection(0, default(CancellationToken)));
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(failure!.Message, Does.Contain("timed out"));
            Assert.That(strategy.ActiveListener.Open, Is.False);
            Assert.That(strategy.ActiveListener.Listener, Is.Null);
            Assert.That(listeningSocket.SafeHandle.IsClosed, Is.True);
        });

        strategy.AbortDataConnection();
        client.AbortConnection();
    }

    [Test]
    public void Active_mode_rejects_a_data_peer_that_differs_from_the_control_peer()
    {
        using TcpListener controlListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            string command = ReadCommand(stream);
            commands.Add(command);
            FtpEndpoint endpoint = FtpEndpointCodec.ParseExtendedPortArgument(
                command.Substring("EPRT ".Length));
            WriteReply(stream, "200 EPRT accepted\r\n");
            using TcpClient data = new TcpClient(AddressFamily.InterNetwork);
            data.Connect(IPAddress.Parse(endpoint.Host), endpoint.Port);
        });
        FtpClient client = ConnectAuthenticated(
            controlListener, FtpDataConnectionMode.Active);
        client.State.Connection!.RemoteEndpoint.Host = "127.0.0.2";

        FtpClientDataConnectionStrategy strategy = client.PrepareDataConnection();
        strategy.BeginTransfer();
        server.GetAwaiter().GetResult();
        FtpConnectionException? failure = Assert.Throws<FtpConnectionException>(() =>
            strategy.Coordinator.OpenDataConnection(5000, default(CancellationToken)));

        Assert.Multiple(() =>
        {
            Assert.That(failure!.InnerException, Is.TypeOf<FtpConnectionException>());
            Assert.That(failure.InnerException!.InnerException,
                Is.TypeOf<FtpConnectionException>());
            Assert.That(failure.InnerException.InnerException!.Message,
                Does.Contain("does not match"));
            Assert.That(strategy.ActiveListener!.Open, Is.False);
            Assert.That(strategy.Coordinator.Connection, Is.Null);
        });

        strategy.AbortDataConnection();
        client.AbortConnection();
    }

    [Test]
    public void Rejected_active_setup_closes_its_listener_and_preserves_the_control_connection()
    {
        using TcpListener controlListener = StartListener();
        List<string> commands = new List<string>();
        FtpEndpoint advertised = new FtpEndpoint();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            string command = ReadCommand(stream);
            commands.Add(command);
            advertised = FtpEndpointCodec.ParseExtendedPortArgument(
                command.Substring("EPRT ".Length));
            WriteReply(stream, "425 Active setup unavailable\r\n");
            Receive(stream, commands, "200 NOOP okay\r\n");
        });
        FtpClient client = ConnectAuthenticated(
            controlListener, FtpDataConnectionMode.Active);

        FtpConnectionException? failure = Assert.Throws<FtpConnectionException>(() =>
            client.PrepareDataConnection());
        FtpCommandResult noop = client.SendNoOperation();
        server.GetAwaiter().GetResult();
        using TcpClient rejectedPeer = new TcpClient(AddressFamily.InterNetwork);
        Assert.Throws<SocketException>(() =>
            rejectedPeer.Connect(IPAddress.Parse(advertised.Host), advertised.Port));

        Assert.Multiple(() =>
        {
            Assert.That(failure!.Message, Does.Contain("reply 425"));
            Assert.That(commands[2], Does.StartWith("EPRT |1|127.0.0.1|"));
            Assert.That(commands[3], Is.EqualTo("NOOP"));
            Assert.That(client.State.DataStrategy, Is.Null);
            Assert.That(noop.Succeeded, Is.True);
        });

        client.AbortConnection();
    }

    [Test]
    public void Automatic_mode_changes_strategy_before_transfer_and_never_retries_after_start()
    {
        using TcpListener controlListener = StartListener();
        List<string> commands = new List<string>();
        Task server = Task.Run(() =>
        {
            using TcpClient control = controlListener.AcceptTcpClient();
            using NetworkStream stream = control.GetStream();
            Authenticate(stream, commands);
            Receive(stream, commands, "425 Passive setup unavailable\r\n");
            Receive(stream, commands, "200 EPRT accepted\r\n");
            Receive(stream, commands, "200 NOOP okay\r\n");
        });
        FtpClient client = ConnectAuthenticated(
            controlListener, FtpDataConnectionMode.Automatic);

        FtpClientDataConnectionStrategy strategy = client.PrepareDataConnection();
        strategy.BeginTransfer();
        FtpProtocolException? replayFailure = Assert.Throws<FtpProtocolException>(() =>
            strategy.PrepareDataConnection());
        strategy.AbortDataConnection();
        FtpCommandResult noop = client.SendNoOperation();
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands[2], Is.EqualTo("EPSV"));
            Assert.That(commands[3], Does.StartWith("EPRT |1|127.0.0.1|"));
            Assert.That(commands[4], Is.EqualTo("NOOP"));
            Assert.That(commands, Has.Count.EqualTo(5));
            Assert.That(strategy.SelectedMode, Is.EqualTo(FtpDataConnectionMode.Active));
            Assert.That(replayFailure!.Message, Does.Contain("cannot be retried"));
            Assert.That(noop.Succeeded, Is.True);
        });

        client.AbortConnection();
    }

    private static TcpListener StartListener()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return listener;
    }

    private static TcpListener StartIpv6Listener()
    {
        TcpListener listener = new TcpListener(IPAddress.IPv6Loopback, 0);
        listener.Server.DualMode = false;
        listener.Start();
        return listener;
    }

    private static int GetPort(TcpListener listener)
    {
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static FtpClient ConnectAuthenticated(
        TcpListener listener,
        FtpDataConnectionMode mode)
    {
        FtpClientOptions options = CreateOptions(
            listener, mode, FtpAddressFamily.IPv4);
        return ConnectAuthenticatedWithOptions(options);
    }

    private static FtpClientOptions CreateOptions(
        TcpListener listener,
        FtpDataConnectionMode mode,
        FtpAddressFamily addressFamily)
    {
        FtpClientOptions options = new FtpClientOptions();
        options.Endpoint.Host = addressFamily == FtpAddressFamily.IPv6 ?
            IPAddress.IPv6Loopback.ToString() : IPAddress.Loopback.ToString();
        options.Endpoint.Port = GetPort(listener);
        options.Endpoint.AddressFamily = addressFamily;
        options.SecurityMode = FtpSecurityMode.Plain;
        options.AllowPlaintextCredentials = true;
        options.NegotiateFeatures = false;
        options.DataConnection.Mode = mode;
        return options;
    }

    private static FtpClient ConnectAuthenticatedWithOptions(FtpClientOptions options)
    {
        FtpClient client = new FtpClient(null);
        client.Connect(options);
        FtpCredentials credentials = new FtpCredentials();
        credentials.UserName = "alice";
        credentials.Password = "secret";
        FtpAuthenticationResult authentication = client.AuthenticateUser(credentials);
        Assert.That(authentication.Status, Is.EqualTo(FtpAuthenticationStatus.Success));
        return client;
    }

    private static void Authenticate(NetworkStream stream, List<string> commands)
    {
        WriteReply(stream, "220 Ready\r\n");
        Receive(stream, commands, "331 Password required\r\n");
        Receive(stream, commands, "230 Logged in\r\n");
    }

    private static void Receive(
        NetworkStream stream,
        List<string> commands,
        string reply)
    {
        commands.Add(ReadCommand(stream));
        WriteReply(stream, reply);
    }

    private static string ReadCommand(NetworkStream stream)
    {
        List<byte> bytes = new List<byte>();
        while (true)
        {
            int value = stream.ReadByte();
            if (value < 0)
            {
                throw new EndOfStreamException();
            }
            if (value == '\n')
            {
                break;
            }
            if (value != '\r')
            {
                bytes.Add((byte)value);
            }
        }
        return System.Text.Encoding.ASCII.GetString(bytes.ToArray());
    }

    private static void WriteReply(NetworkStream stream, string reply)
    {
        byte[] bytes = System.Text.Encoding.ASCII.GetBytes(reply);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
    }
}
