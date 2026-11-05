using System.Net;
using System.Net.Sockets;
using System.Text;

namespace pop3.Tests;

public class Pop3ClientEngineConnectionTests
{
    [Test]
    public void ConnectsToExplicitIpv4PortAndOwnsTheSocket()
    {
        using (LoopbackServer server = new LoopbackServer(IPAddress.Loopback, "+OK ready <1@example>\r\n", false))
        {
            Pop3ClientEngine engine = new Pop3ClientEngine();
            Pop3ClientOptions options = CreateOptions("127.0.0.1", server.Port);
            options.ReadTimeoutMilliseconds = 1234;
            options.WriteTimeoutMilliseconds = 2345;
            options.LeaveStreamOpen = true;

            engine.ConnectHost(options, CancellationToken.None);

            Pop3ClientSnapshot snapshot = engine.GetSnapshot();
            Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Authorization));
            Assert.That(snapshot.IsConnected, Is.True);
            Assert.That(snapshot.IsSecure, Is.False);
            Assert.That(snapshot.Greeting, Is.EqualTo("ready <1@example>"));
            Assert.That(snapshot.LastResponse!.Kind, Is.EqualTo(Pop3ResponseKind.Negative));
            Assert.That(snapshot.LastResponse.Text, Is.EqualTo("CAPA unavailable"));
            Assert.That(engine.GetTransportStream().ReadTimeout, Is.EqualTo(1234));
            Assert.That(engine.GetTransportStream().WriteTimeout, Is.EqualTo(2345));

            Pop3Command command = new Pop3Command();
            command.Keyword = Pop3Commands.Noop;
            engine.GetWireWriter().WriteCommand(command, Pop3Defaults.MaximumCommandLineOctets, CancellationToken.None);
            engine.CloseConnection();

            server.WaitForCompletion();
            Assert.That(Encoding.ASCII.GetString(server.ReceivedBytes()), Is.EqualTo("CAPA\r\nNOOP\r\n"));
            Assert.That(server.SawEndOfStream, Is.True);
            Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
            Assert.Throws<Pop3StateException>(() => engine.GetTransportStream());
        }
    }

    [Test]
    public void ConnectsToExplicitIpv6PortWhenIpv6IsAvailable()
    {
        if (!Socket.OSSupportsIPv6)
        {
            Assert.Ignore("IPv6 is not available on this platform.");
        }

        using (LoopbackServer server = new LoopbackServer(IPAddress.IPv6Loopback, "+OK IPv6 ready\r\n", false))
        {
            Pop3ClientEngine engine = new Pop3ClientEngine();
            Pop3ClientOptions options = CreateOptions("::1", server.Port);

            engine.ConnectHost(options, CancellationToken.None);

            Assert.That(engine.GetSnapshot().Greeting, Is.EqualTo("IPv6 ready"));
            engine.CloseConnection();
            server.WaitForCompletion();
            Assert.That(server.SawEndOfStream, Is.True);
        }
    }

    [Test]
    public void HostnameResolutionCanSelectLoopbackTransport()
    {
        using (LoopbackServer server = new LoopbackServer(IPAddress.Loopback, "+OK hostname ready\r\n", false))
        {
            Pop3ClientEngine engine = new Pop3ClientEngine();
            Pop3ClientOptions options = CreateOptions("localhost", server.Port);

            engine.ConnectHost(options, CancellationToken.None);

            Assert.That(engine.GetSnapshot().Greeting, Is.EqualTo("hostname ready"));
            engine.CloseConnection();
            server.WaitForCompletion();
        }
    }

    [Test]
    public void NegativeGreetingClosesOwnedSocketAndRetainsTheResponse()
    {
        using (LoopbackServer server = new LoopbackServer(IPAddress.Loopback, "-ERR [SYS/TEMP] unavailable\r\n", false))
        {
            Pop3ClientEngine engine = new Pop3ClientEngine();
            Pop3ClientOptions options = CreateOptions("127.0.0.1", server.Port);

            Pop3ConnectionException exception = Assert.Throws<Pop3ConnectionException>(() =>
                engine.ConnectHost(options, CancellationToken.None))!;

            server.WaitForCompletion();
            Pop3ClientSnapshot snapshot = engine.GetSnapshot();
            Assert.That(exception.Operation, Is.EqualTo("ConnectHost"));
            Assert.That(exception.IsEndOfStream, Is.False);
            Assert.That(exception.IsTimeout, Is.False);
            Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Closed));
            Assert.That(snapshot.IsConnected, Is.False);
            Assert.That(snapshot.LastResponse!.Kind, Is.EqualTo(Pop3ResponseKind.Negative));
            Assert.That(snapshot.LastResponse.ResponseCodeLevels, Is.EqualTo(new[] { "SYS", "TEMP" }));
            Assert.That(snapshot.LastResponse.Text, Is.EqualTo("[SYS/TEMP] unavailable"));
            Assert.That(server.SawEndOfStream, Is.True);
        }
    }

    [Test]
    public void MalformedGreetingClosesSocketAndInvalidatesSession()
    {
        using (LoopbackServer server = new LoopbackServer(IPAddress.Loopback, "+OK malformed\n", false))
        {
            Pop3ClientEngine engine = new Pop3ClientEngine();
            Pop3ClientOptions options = CreateOptions("127.0.0.1", server.Port);

            Pop3ProtocolException exception = Assert.Throws<Pop3ProtocolException>(() =>
                engine.ConnectHost(options, CancellationToken.None))!;

            server.WaitForCompletion();
            Assert.That(exception.Context, Is.EqualTo("greeting"));
            Assert.That(exception.InnerException, Is.TypeOf<InvalidDataException>());
            Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
            Assert.That(server.SawEndOfStream, Is.True);
        }
    }

    [Test]
    public void EofBeforeGreetingIsClassifiedAndClosesSession()
    {
        using (LoopbackServer server = new LoopbackServer(IPAddress.Loopback, null, true))
        {
            Pop3ClientEngine engine = new Pop3ClientEngine();
            Pop3ClientOptions options = CreateOptions("127.0.0.1", server.Port);

            Pop3ConnectionException exception = Assert.Throws<Pop3ConnectionException>(() =>
                engine.ConnectHost(options, CancellationToken.None))!;

            server.WaitForCompletion();
            Assert.That(exception.Operation, Is.EqualTo("ReadGreeting"));
            Assert.That(exception.IsEndOfStream, Is.True);
            Assert.That(exception.IsTimeout, Is.False);
            Assert.That(exception.InnerException, Is.TypeOf<EndOfStreamException>());
            Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        }
    }

    [Test]
    public void GreetingReadTimeoutClosesSocketAndIsClassified()
    {
        using (LoopbackServer server = new LoopbackServer(IPAddress.Loopback, null, false))
        {
            Pop3ClientEngine engine = new Pop3ClientEngine();
            Pop3ClientOptions options = CreateOptions("127.0.0.1", server.Port);
            options.ReadTimeoutMilliseconds = 50;

            Pop3ConnectionException exception = Assert.Throws<Pop3ConnectionException>(() =>
                engine.ConnectHost(options, CancellationToken.None))!;

            server.WaitForCompletion();
            Assert.That(exception.Operation, Is.EqualTo("ReadGreeting"));
            Assert.That(exception.IsEndOfStream, Is.False);
            Assert.That(exception.IsTimeout, Is.True);
            Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
            Assert.That(server.SawEndOfStream, Is.True);
        }
    }

    [Test]
    public void CancellationWhileReadingGreetingClosesSocket()
    {
        using (LoopbackServer server = new LoopbackServer(IPAddress.Loopback, null, false))
        using (CancellationTokenSource cancellation = new CancellationTokenSource())
        {
            Pop3ClientEngine engine = new Pop3ClientEngine();
            Pop3ClientOptions options = CreateOptions("127.0.0.1", server.Port);
            cancellation.CancelAfter(50);

            Assert.Throws<OperationCanceledException>(() => engine.ConnectHost(options, cancellation.Token));

            server.WaitForCompletion();
            Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
            Assert.That(server.SawEndOfStream, Is.True);
        }
    }

    [Test]
    public void PreCanceledConnectionDoesNotLeaveDisconnectedState()
    {
        Pop3ClientEngine engine = new Pop3ClientEngine();
        Pop3ClientOptions options = CreateOptions("127.0.0.1", 1);
        using (CancellationTokenSource cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();

            Assert.Throws<OperationCanceledException>(() => engine.ConnectHost(options, cancellation.Token));

            Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Disconnected));
        }
    }

    [Test]
    public void RefusedConnectionIsWrappedAndInvalidatesSession()
    {
        int port;
        TcpListener reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.Start();
        port = ((IPEndPoint)reservation.LocalEndpoint).Port;
        reservation.Stop();
        Pop3ClientEngine engine = new Pop3ClientEngine();
        Pop3ClientOptions options = CreateOptions("127.0.0.1", port);

        Pop3ConnectionException exception = Assert.Throws<Pop3ConnectionException>(() =>
            engine.ConnectHost(options, CancellationToken.None))!;

        Assert.That(exception.Operation, Is.EqualTo("ConnectHost"));
        Assert.That(exception.IsTimeout, Is.False);
        Assert.That(exception.InnerException, Is.Not.Null);
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
    }

    [Test]
    public void InvalidOptionsAreRejectedBeforeConnectionStateChanges()
    {
        AssertInvalidOptions(options => options.Host = "");
        AssertInvalidOptions(options => options.Host = "   ");
        AssertInvalidOptions(options => options.Port = 0);
        AssertInvalidOptions(options => options.Port = 65536);
        AssertInvalidOptions(options => options.ConnectTimeoutMilliseconds = 0);
        AssertInvalidOptions(options => options.ReadTimeoutMilliseconds = 0);
        AssertInvalidOptions(options => options.WriteTimeoutMilliseconds = 0);
        AssertInvalidOptions(options => options.BufferSize = 0);
        AssertInvalidOptions(options => options.SecurityMode = (Pop3SecurityMode)99);

        Pop3ClientEngine engine = new Pop3ClientEngine();
        Assert.Throws<ArgumentNullException>(() => engine.ConnectHost(null!, CancellationToken.None));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Disconnected));
    }

    private static Pop3ClientOptions CreateOptions(string host, int port)
    {
        Pop3ClientOptions options = new Pop3ClientOptions();
        options.Host = host;
        options.Port = port;
        options.ConnectTimeoutMilliseconds = 2000;
        options.ReadTimeoutMilliseconds = 2000;
        options.WriteTimeoutMilliseconds = 2000;
        options.BufferSize = 8;
        return options;
    }

    private static void AssertInvalidOptions(Action<Pop3ClientOptions> change)
    {
        Pop3ClientEngine engine = new Pop3ClientEngine();
        Pop3ClientOptions options = CreateOptions("127.0.0.1", 110);
        change(options);

        Assert.Catch<ArgumentException>(() => engine.ConnectHost(options, CancellationToken.None));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Disconnected));
    }

    private class LoopbackServer : IDisposable
    {
        TcpListener Listener;
        string? Greeting;
        bool CloseImmediately;
        Task ServerTask;
        MemoryStream Received = new MemoryStream();
        public int Port;
        public bool SawEndOfStream;

        public LoopbackServer(IPAddress address, string? greeting, bool closeImmediately)
        {
            Greeting = greeting;
            CloseImmediately = closeImmediately;
            Listener = new TcpListener(address, 0);
            Listener.Start();
            Port = ((IPEndPoint)Listener.LocalEndpoint).Port;
            ServerTask = Task.Run(Serve);
        }

        public byte[] ReceivedBytes()
        {
            return Received.ToArray();
        }

        public void WaitForCompletion()
        {
            ServerTask.GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            Listener.Stop();
            ServerTask.GetAwaiter().GetResult();
            Received.Dispose();
        }

        void Serve()
        {
            using (TcpClient client = Listener.AcceptTcpClient())
            {
                if (CloseImmediately)
                {
                    return;
                }

                NetworkStream stream = client.GetStream();
                stream.ReadTimeout = 5000;
                if (Greeting != null)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(Greeting);
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush();
                }

                byte[] buffer = new byte[64];
                bool capaAnswered = false;
                int count = stream.Read(buffer, 0, buffer.Length);
                while (count > 0)
                {
                    Received.Write(buffer, 0, count);
                    if (!capaAnswered && StartsWithCapa(Received.ToArray()))
                    {
                        byte[] response = Encoding.ASCII.GetBytes("-ERR CAPA unavailable\r\n");
                        stream.Write(response, 0, response.Length);
                        stream.Flush();
                        capaAnswered = true;
                    }
                    count = stream.Read(buffer, 0, buffer.Length);
                }
                SawEndOfStream = true;
            }
        }

        bool StartsWithCapa(byte[] value)
        {
            return value.Length >= 6 &&
                value[0] == 67 &&
                value[1] == 65 &&
                value[2] == 80 &&
                value[3] == 65 &&
                value[4] == 13 &&
                value[5] == 10;
        }
    }
}
