using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace ftp.Tests;

public class FtpServerSessionTests
{
    [Test]
    public void Session_uses_virtual_host_greeting_dispatches_commands_and_records_lifecycle()
    {
        int port = ReserveAvailablePort();
        RecordingHostResolver resolver = new RecordingHostResolver();
        resolver.Host.HostName = "ftp.example";
        resolver.Host.Greeting = "Welcome to example FTP";
        resolver.Host.RootPath = "/srv/example";
        RecordingServerEventSink events = new RecordingServerEventSink();
        FtpServer server = CreateServer(resolver, events);
        FtpServerOptions options = CreateOptions(port);
        options.DefaultHostName = "ftp.example";
        List<FtpServerCommandContext> dispatched =
            new List<FtpServerCommandContext>();
        object dispatchLock = new object();
        server.Runtime.CommandOperation = context =>
        {
            lock (dispatchLock)
            {
                dispatched.Add(context);
            }
            if (context.Command.Kind == FtpCommandKind.Quit)
            {
                return CreateResult(
                    context.Command,
                    FtpReplyCodes.ServiceClosingControlConnection,
                    "Goodbye",
                    true);
            }
            return CreateResult(
                context.Command,
                FtpReplyCodes.CommandNotImplemented,
                "Not implemented",
                false);
        };

        try
        {
            server.StartServer(options);
            using TcpClient client = Connect(port);
            using NetworkStream stream = client.GetStream();
            stream.ReadTimeout = 3000;
            using StreamReader reader = new StreamReader(
                stream, Encoding.ASCII, false, 1024, true);

            string? greeting = reader.ReadLine();
            WriteCommands(stream, "PASS top-secret\r\nQUIT\r\n");
            string? passwordReply = reader.ReadLine();
            string? quitReply = reader.ReadLine();
            Assert.That(SpinWait.SpinUntil(() =>
                GetDispatcher(server).GetCompletedSessionCount() == 1, 3000), Is.True);

            FtpEvent[] recorded = events.GetEvents();
            FtpEvent? passwordEvent = recorded.FirstOrDefault(eventData =>
                eventData.Kind == FtpEventKind.Command &&
                eventData.CommandKind == FtpCommandKind.Pass);
            Assert.Multiple(() =>
            {
                Assert.That(greeting, Is.EqualTo("220 Welcome to example FTP"));
                Assert.That(passwordReply, Is.EqualTo("502 Not implemented"));
                Assert.That(quitReply, Is.EqualTo("221 Goodbye"));
                Assert.That(resolver.Requests, Has.Count.EqualTo(1));
                Assert.That(resolver.Requests[0].HostName, Is.EqualTo("ftp.example"));
                Assert.That(resolver.Requests[0].LocalEndpoint, Is.Not.Null);
                Assert.That(resolver.Requests[0].RemoteEndpoint, Is.Not.Null);
                Assert.That(dispatched, Has.Count.EqualTo(2));
                Assert.That(dispatched[0].Host, Is.SameAs(resolver.Host));
                Assert.That(dispatched[1].State.State.SessionState,
                    Is.EqualTo(FtpSessionState.Closed));
                Assert.That(passwordEvent, Is.Not.Null);
                Assert.That(passwordEvent!.Command!.Argument,
                    Is.EqualTo(FtpTranscriptRedactor.RedactedText));
                Assert.That(passwordEvent.Message, Does.Not.Contain("top-secret"));
                Assert.That(recorded.Count(eventData =>
                    eventData.Kind == FtpEventKind.Lifecycle), Is.EqualTo(3));
                Assert.That(recorded.Any(eventData =>
                    eventData.Kind == FtpEventKind.Reply &&
                    eventData.Reply!.Code == FtpReplyCodes.ServiceClosingControlConnection),
                    Is.True);
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Session_enforces_malformed_command_rate_and_idle_disconnects()
    {
        int port = ReserveAvailablePort();
        RecordingHostResolver resolver = new RecordingHostResolver();
        RecordingServerEventSink events = new RecordingServerEventSink();
        FtpServer server = CreateServer(resolver, events);
        FtpServerOptions options = CreateOptions(port);
        options.MaximumMalformedLines = 1;
        options.MaximumCommandsPerMinute = 1;
        options.Timeouts.IdleTimeoutMilliseconds = 100;
        server.Runtime.CommandOperation = context =>
        {
            return CreateResult(
                context.Command,
                FtpReplyCodes.CommandOkay,
                "Okay",
                false);
        };

        try
        {
            server.StartServer(options);

            using (TcpClient malformedClient = Connect(port))
            using (NetworkStream malformedStream = malformedClient.GetStream())
            using (StreamReader malformedReader = CreateReader(malformedStream))
            {
                Assert.That(malformedReader.ReadLine(),
                    Is.EqualTo("220 FTP service ready"));
                WriteCommands(malformedStream, "\r\n\r\n");
                Assert.That(malformedReader.ReadLine(),
                    Is.EqualTo("500 Malformed FTP command line."));
                Assert.That(malformedReader.ReadLine(),
                    Is.EqualTo("421 Too many malformed FTP command lines."));
            }

            using (TcpClient rateClient = Connect(port))
            using (NetworkStream rateStream = rateClient.GetStream())
            using (StreamReader rateReader = CreateReader(rateStream))
            {
                Assert.That(rateReader.ReadLine(),
                    Is.EqualTo("220 FTP service ready"));
                WriteCommands(rateStream, "NOOP\r\nNOOP\r\n");
                Assert.That(rateReader.ReadLine(), Is.EqualTo("200 Okay"));
                Assert.That(rateReader.ReadLine(),
                    Is.EqualTo("421 FTP command rate limit exceeded."));
            }

            using (TcpClient idleClient = Connect(port))
            using (NetworkStream idleStream = idleClient.GetStream())
            using (StreamReader idleReader = CreateReader(idleStream))
            {
                Assert.That(idleReader.ReadLine(),
                    Is.EqualTo("220 FTP service ready"));
                Assert.That(idleReader.ReadLine(),
                    Is.EqualTo("421 FTP session idle timeout."));
            }

            Assert.That(SpinWait.SpinUntil(() =>
                GetDispatcher(server).GetCompletedSessionCount() == 3, 3000), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(events.GetEvents().Count(eventData =>
                    eventData.Kind == FtpEventKind.PolicyRejection),
                    Is.EqualTo(3));
                Assert.That(server.GetServerState(), Is.EqualTo(FtpServerState.Running));
                Assert.That(GetLimits(server).GetSessionCount(), Is.Zero);
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Session_rejects_injection_and_invalid_utf8_without_collapsing_unicode_names()
    {
        int port = ReserveAvailablePort();
        RecordingHostResolver resolver = new RecordingHostResolver();
        RecordingServerEventSink events = new RecordingServerEventSink();
        FtpServer server = CreateServer(resolver, events);
        FtpServerOptions options = CreateOptions(port);
        options.MaximumMalformedLines = 3;
        List<FtpCommand> dispatched = new List<FtpCommand>();
        string composed = "caf\u00e9";
        string decomposed = "cafe\u0301";
        server.Runtime.CommandOperation = context =>
        {
            dispatched.Add(context.Command);
            bool close = context.Command.Kind == FtpCommandKind.Quit;
            return CreateResult(
                context.Command,
                close ? FtpReplyCodes.ServiceClosingControlConnection :
                    FtpReplyCodes.CommandOkay,
                close ? "Goodbye" : "Okay",
                close);
        };

        try
        {
            server.StartServer(options);
            using TcpClient client = Connect(port);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = CreateReader(stream);
            Assert.That(reader.ReadLine(), Is.EqualTo("220 FTP service ready"));
            byte[] nulInjection = Encoding.ASCII.GetBytes(
                "NOOP\0QUIT\r\n");
            byte[] invalidUtf8 = new byte[]
            {
                (byte)'C', (byte)'W', (byte)'D', (byte)' ',
                0xc0, 0xaf, 13, 10,
            };
            byte[] validCommands = Encoding.UTF8.GetBytes(
                "CWD " + composed + "\r\n" +
                "CWD " + decomposed + "\r\n" +
                "QUIT\r\n");
            stream.Write(nulInjection, 0, nulInjection.Length);
            stream.Write(invalidUtf8, 0, invalidUtf8.Length);
            stream.Write(validCommands, 0, validCommands.Length);
            stream.Flush();

            string? injection = reader.ReadLine();
            string? malformedEncoding = reader.ReadLine();
            string? composedReply = reader.ReadLine();
            string? decomposedReply = reader.ReadLine();
            string? quit = reader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(injection,
                    Is.EqualTo("500 Malformed FTP command line."));
                Assert.That(malformedEncoding,
                    Is.EqualTo("500 Malformed FTP command line."));
                Assert.That(composedReply, Is.EqualTo("200 Okay"));
                Assert.That(decomposedReply, Is.EqualTo("200 Okay"));
                Assert.That(quit, Is.EqualTo("221 Goodbye"));
                Assert.That(dispatched.Select(command => command.Kind),
                    Is.EqualTo(new[]
                    {
                        FtpCommandKind.Cwd,
                        FtpCommandKind.Cwd,
                        FtpCommandKind.Quit,
                    }));
                Assert.That(dispatched[0].Argument, Is.EqualTo(composed));
                Assert.That(dispatched[1].Argument, Is.EqualTo(decomposed));
                Assert.That(dispatched[0].Argument,
                    Is.Not.EqualTo(dispatched[1].Argument));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Session_closes_after_overlong_line_without_dispatching_its_suffix()
    {
        int port = ReserveAvailablePort();
        RecordingHostResolver resolver = new RecordingHostResolver();
        RecordingServerEventSink events = new RecordingServerEventSink();
        FtpServer server = CreateServer(resolver, events);
        FtpServerOptions options = CreateOptions(port);
        options.MaximumMalformedLines = 3;
        List<FtpCommand> dispatched = new List<FtpCommand>();
        server.Runtime.CommandOperation = context =>
        {
            dispatched.Add(context.Command);
            return CreateResult(
                context.Command,
                FtpReplyCodes.CommandOkay,
                "Okay",
                false);
        };

        try
        {
            server.StartServer(options);
            using TcpClient client = Connect(port);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = CreateReader(stream);
            Assert.That(reader.ReadLine(), Is.EqualTo("220 FTP service ready"));
            byte[] attack = Encoding.ASCII.GetBytes(
                new string('A', FtpDefaults.MaximumControlLineBytes - 1) +
                "NOOP\r\n");
            stream.Write(attack, 0, attack.Length);
            stream.Flush();

            string? rejection = reader.ReadLine();
            bool connectionClosed = ReadConnectionClosed(reader);

            Assert.Multiple(() =>
            {
                Assert.That(rejection,
                    Is.EqualTo("500 Malformed FTP command line."));
                Assert.That(connectionClosed, Is.True);
                Assert.That(dispatched, Is.Empty);
                Assert.That(events.GetEvents().Any(eventData =>
                    eventData.Kind == FtpEventKind.PolicyRejection &&
                    eventData.Message ==
                        "The FTP control stream lost command framing synchronization."),
                    Is.True);
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    private static FtpCommandResult CreateResult(
        FtpCommand command,
        int code,
        string text,
        bool close)
    {
        FtpCommandResult result = new FtpCommandResult();
        result.Command = command;
        result.FinalReply = FtpServerSession.CreateReply(code, text);
        result.ConnectionClosed = close;
        result.Succeeded = code >= 200 && code < 300;
        return result;
    }

    private static FtpServer CreateServer(
        IFtpHostResolver hostResolver,
        IFtpEventSink eventSink)
    {
        return new FtpServer(
            CreateProxy<IFtpFileSystem>(),
            CreateProxy<IFtpAuthenticator>(),
            CreateProxy<IFtpAuthorizer>(),
            hostResolver,
            CreateProxy<IFtpCertificateProvider>(),
            eventSink);
    }

    private static FtpServerOptions CreateOptions(int port)
    {
        FtpServerOptions options = new FtpServerOptions();
        FtpListenerOptions listener = new FtpListenerOptions();
        listener.Endpoint.Host = "127.0.0.1";
        listener.Endpoint.Port = port;
        listener.Endpoint.AddressFamily = FtpAddressFamily.IPv4;
        listener.SecurityMode = FtpSecurityMode.Plain;
        listener.DualStack = false;
        listener.Backlog = 8;
        options.Listeners = new[] { listener };
        return options;
    }

    private static TcpClient Connect(int port)
    {
        TcpClient client = new TcpClient(AddressFamily.InterNetwork);
        client.ReceiveTimeout = 3000;
        client.SendTimeout = 3000;
        client.Connect(IPAddress.Loopback, port);
        return client;
    }

    private static StreamReader CreateReader(NetworkStream stream)
    {
        stream.ReadTimeout = 3000;
        return new StreamReader(stream, Encoding.ASCII, false, 1024, true);
    }

    private static void WriteCommands(NetworkStream stream, string commands)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(commands);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
    }

    private static bool ReadConnectionClosed(StreamReader reader)
    {
        try
        {
            return reader.ReadLine() == null;
        }
        catch (IOException)
        {
            return true;
        }
    }

    private static int ReserveAvailablePort()
    {
        using TcpListener reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.ExclusiveAddressUse = true;
        reservation.Start();
        return ((IPEndPoint)reservation.LocalEndpoint).Port;
    }

    private static FtpServerSessionDispatcher GetDispatcher(FtpServer server)
    {
        FtpServerSessionDispatcher? dispatcher = server.Runtime.SessionDispatcher;
        Assert.That(dispatcher, Is.Not.Null);
        return dispatcher!;
    }

    private static FtpServerLimitManager GetLimits(FtpServer server)
    {
        FtpServerLimitManager? limits = server.Runtime.Limits;
        Assert.That(limits, Is.Not.Null);
        return limits!;
    }

    private static T CreateProxy<T>() where T : class
    {
        return DispatchProxy.Create<T, FtpServerSessionProviderProxy>();
    }
}

public class RecordingHostResolver : IFtpHostResolver
{
    public FtpVirtualHostOptions Host;
    public List<FtpVirtualHostRequest> Requests;

    public RecordingHostResolver()
    {
        Host = new FtpVirtualHostOptions();
        Host.HostName = "unused.example";
        Host.RootPath = "/srv/unused";
        Requests = new List<FtpVirtualHostRequest>();
    }

    public FtpVirtualHostResult ResolveVirtualHost(FtpVirtualHostRequest request)
    {
        Requests.Add(request);
        FtpVirtualHostResult result = new FtpVirtualHostResult();
        result.Handled = true;
        result.Found = true;
        result.Host = Host;
        return result;
    }
}

public class RecordingServerEventSink : IFtpEventSink
{
    public object EventLock;
    public List<FtpEvent> Events;

    public RecordingServerEventSink()
    {
        EventLock = new object();
        Events = new List<FtpEvent>();
    }

    public void RecordEvent(FtpEvent eventData)
    {
        lock (EventLock)
        {
            Events.Add(eventData);
        }
    }

    public FtpEvent[] GetEvents()
    {
        lock (EventLock)
        {
            return Events.ToArray();
        }
    }
}

public class FtpServerSessionProviderProxy : DispatchProxy
{
    protected override object? Invoke(
        MethodInfo? targetMethod,
        object?[]? arguments)
    {
        throw new NotSupportedException(
            "Server session tests do not invoke unrelated providers.");
    }
}
