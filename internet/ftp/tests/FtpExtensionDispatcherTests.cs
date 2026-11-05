using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace ftp.Tests;

public class FtpExtensionDispatcherTests
{
    [Test]
    public void Dispatcher_uses_ordered_fallthrough_and_snapshots_handled_result()
    {
        RecordingExtensionHandler first = new RecordingExtensionHandler();
        RecordingExtensionHandler second = new RecordingExtensionHandler();
        second.Result.Disposition = FtpExtensionDisposition.Handled;
        second.Result.Replies = new[] { CreateReply(200, "Extension accepted") };
        second.Result.CloseConnection = true;
        FtpExtensionContext context = new FtpExtensionContext();
        context.Command = FtpCommandParser.ParseDecodedCommand(
            "X-TRACE alpha");
        FtpExtensionDispatcher dispatcher = new FtpExtensionDispatcher(
            new IFtpExtensionHandler[] { first, second });

        FtpExtensionResult result = dispatcher.DispatchExtension(context);
        second.Result.Replies[0].Text = "changed after dispatch";
        first.Contexts[0].Command.Argument = "changed by first";

        Assert.Multiple(() =>
        {
            Assert.That(first.Contexts, Has.Count.EqualTo(1));
            Assert.That(second.Contexts, Has.Count.EqualTo(1));
            Assert.That(first.Contexts[0], Is.Not.SameAs(second.Contexts[0]));
            Assert.That(second.Contexts[0].Command.Argument,
                Is.EqualTo("alpha"));
            Assert.That(result.Disposition,
                Is.EqualTo(FtpExtensionDisposition.Handled));
            Assert.That(result.CloseConnection, Is.True);
            Assert.That(result.Replies[0].Text,
                Is.EqualTo("Extension accepted"));
            Assert.That(result.Replies[0],
                Is.Not.SameAs(second.Result.Replies[0]));
        });
    }

    [Test]
    public void Dispatcher_preserves_a_handler_rejection_and_stops_fallthrough()
    {
        RecordingExtensionHandler rejecting =
            new RecordingExtensionHandler();
        rejecting.Result.Disposition = FtpExtensionDisposition.Rejected;
        rejecting.Result.Replies = new[]
        {
            CreateReply(550, "Extension policy rejected the command")
        };
        rejecting.Result.FailureMessage = "Extension policy rejected the command.";
        RecordingExtensionHandler later = new RecordingExtensionHandler();
        later.Result.Disposition = FtpExtensionDisposition.Handled;
        later.Result.Replies = new[] { CreateReply(200, "Unexpected") };
        FtpExtensionDispatcher dispatcher = new FtpExtensionDispatcher(
            new IFtpExtensionHandler[] { rejecting, later });
        FtpExtensionContext context = new FtpExtensionContext();
        context.Command = FtpCommandParser.ParseDecodedCommand(
            "X-POLICY value");

        FtpExtensionResult result = dispatcher.DispatchExtension(context);
        rejecting.Result.Replies[0].Text = "mutated";

        Assert.Multiple(() =>
        {
            Assert.That(result.Disposition,
                Is.EqualTo(FtpExtensionDisposition.Rejected));
            Assert.That(result.Replies, Has.Length.EqualTo(1));
            Assert.That(result.Replies[0].Code, Is.EqualTo(550));
            Assert.That(result.Replies[0].Text,
                Is.EqualTo("Extension policy rejected the command"));
            Assert.That(result.FailureMessage,
                Is.EqualTo("Extension policy rejected the command."));
            Assert.That(rejecting.Contexts, Has.Count.EqualTo(1));
            Assert.That(later.Contexts, Is.Empty);
        });
    }

    [Test]
    public void Dispatcher_converts_fault_null_and_invalid_results_to_rejection()
    {
        DelegateExtensionHandler faulting = new DelegateExtensionHandler(
            context => throw new InvalidOperationException("provider secret"));
        DelegateExtensionHandler missing = new DelegateExtensionHandler(
            context => null!);
        DelegateExtensionHandler invalid = new DelegateExtensionHandler(
            context =>
            {
                FtpExtensionResult result = new FtpExtensionResult();
                result.Disposition = FtpExtensionDisposition.Handled;
                return result;
            });

        FtpExtensionResult fault = DispatchSingle(faulting);
        FtpExtensionResult nullResult = DispatchSingle(missing);
        FtpExtensionResult malformed = DispatchSingle(invalid);

        Assert.Multiple(() =>
        {
            AssertRejectedFailure(fault);
            AssertRejectedFailure(nullResult);
            AssertRejectedFailure(malformed);
            Assert.That(fault.Replies[0].Text,
                Does.Not.Contain("provider secret"));
        });
    }

    [Test]
    public void Eligibility_covers_custom_security_and_compatibility_commands()
    {
        FtpSequenceState clearState = new FtpSequenceState();
        FtpSequenceState customSecurityState = new FtpSequenceState();
        customSecurityState.SecurityExchangeComplete = true;
        customSecurityState.SecurityExchangeIsTls = false;
        FtpSequenceState tlsState = new FtpSequenceState();
        tlsState.SecurityExchangeComplete = true;
        tlsState.SecurityExchangeIsTls = true;

        Assert.Multiple(() =>
        {
            Assert.That(Handles("X-VENDOR value", clearState), Is.True);
            Assert.That(Handles("ALGS STATUS64", clearState), Is.True);
            Assert.That(Handles("OPTS X-MODE FAST", clearState), Is.True);
            Assert.That(Handles("SITE X-CUSTOM value", clearState), Is.True);
            Assert.That(Handles("AUTH CUSTOM", clearState), Is.True);
            Assert.That(Handles("AUTH TLS", clearState), Is.False);
            Assert.That(Handles("AUTH TLS-C", clearState), Is.False);
            Assert.That(Handles("ADAT data", clearState), Is.True);
            Assert.That(Handles("MIC data", clearState), Is.True);
            Assert.That(Handles("CONF data", clearState), Is.True);
            Assert.That(Handles("ENC data", clearState), Is.True);
            Assert.That(Handles("PBSZ 10", customSecurityState), Is.True);
            Assert.That(Handles("PROT S", customSecurityState), Is.True);
            Assert.That(Handles("CCC", customSecurityState), Is.True);
            Assert.That(Handles("PBSZ 0", tlsState), Is.False);
            Assert.That(Handles("XCWD /archive", clearState), Is.True);
            Assert.That(Handles("XCUP", clearState), Is.True);
            Assert.That(Handles("XPWD", clearState), Is.True);
            Assert.That(Handles("XMKD /archive", clearState), Is.True);
            Assert.That(Handles("XRMD /archive", clearState), Is.True);
            Assert.That(Handles("PRET RETR /file.bin", clearState), Is.True);
            Assert.That(Handles("CLNT client/1.0", clearState), Is.True);
            Assert.That(Handles(
                "MFMT 20240203040506 /file.bin", clearState), Is.True);
            Assert.That(Handles("HASH /file.bin", clearState), Is.True);
            Assert.That(Handles("MD5 /file.bin", clearState), Is.True);
            Assert.That(Handles("XMD5 /file.bin", clearState), Is.True);
            Assert.That(Handles("XSHA1 /file.bin", clearState), Is.True);
            Assert.That(Handles("XSHA256 /file.bin", clearState), Is.True);
            Assert.That(Handles("XSHA512 /file.bin", clearState), Is.True);
            Assert.That(Handles("XCRC /file.bin", clearState), Is.True);
            Assert.That(Handles("RMDA /archive", clearState), Is.True);
            Assert.That(Handles("MODE Z", clearState), Is.True);
            Assert.That(Handles("MODE S", clearState), Is.False);
            Assert.That(Handles("NOOP", clearState), Is.False);
        });
    }

    [Test]
    public void Context_is_detached_and_redacts_principal_secrets()
    {
        FtpSequenceState state = new FtpSequenceState();
        state.SessionState = FtpSessionState.Authenticated;
        state.CurrentDirectory = "/pub";
        state.ControlProtected = true;
        state.TransferInProgress = true;
        state.Principal = new FtpPrincipal();
        state.Principal.Name = "alice";
        state.Principal.Authenticated = true;
        state.Principal.Account = "billing-secret";
        state.Principal.CertificateIdentity = "certificate-secret";
        state.Principal.HomeDirectory = "/secret-home";
        state.Principal.Roles = new[] { "writer" };
        FtpFeature feature = new FtpFeature();
        feature.Name = "X-CUSTOM";
        feature.RawLine = "X-CUSTOM enabled";
        FtpFeatureSet features = new FtpFeatureSet();
        features.Features = new[] { feature };
        FtpCommand command = FtpCommandParser.ParseDecodedCommand(
            "ADAT private-security-data");

        FtpExtensionContext context = FtpExtensionDispatcher.CreateContext(
            command, state, features);
        command.Argument = "changed";
        state.Principal.Roles[0] = "changed";
        feature.Name = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(context.Command.Argument,
                Is.EqualTo("private-security-data"));
            Assert.That(context.Command, Is.Not.SameAs(command));
            Assert.That(context.Principal!.Name, Is.EqualTo("alice"));
            Assert.That(context.Principal.Account, Is.Null);
            Assert.That(context.Principal.CertificateIdentity, Is.Null);
            Assert.That(context.Principal.HomeDirectory, Is.Null);
            Assert.That(context.Principal.Roles, Is.EqualTo(new[] { "writer" }));
            Assert.That(context.Features.Features[0].Name,
                Is.EqualTo("X-CUSTOM"));
            Assert.That(context.CurrentDirectory, Is.EqualTo("/pub"));
            Assert.That(context.ControlProtected, Is.True);
            Assert.That(context.TransferInProgress, Is.True);
        });
    }

    [Test]
    public void Live_session_dispatches_unknown_and_opts_extensions()
    {
        int port = ReserveAvailablePort();
        RecordingExtensionHandler handler = new RecordingExtensionHandler();
        handler.Result.Disposition = FtpExtensionDisposition.Handled;
        handler.Result.Replies = new[] { CreateReply(200, "Extension accepted") };
        RecordingExtensionEventSink events = new RecordingExtensionEventSink();
        FtpServer server = CreateServer(events);
        FtpServerOptions options = CreateOptions(port);
        options.ExtensionHandlers = new IFtpExtensionHandler[] { handler };
        server.Runtime.CommandOperation = context =>
        {
            if (context.Command.Kind == FtpCommandKind.User)
            {
                FtpPrincipal principal = new FtpPrincipal();
                principal.Name = "alice";
                principal.Authenticated = true;
                context.State.State.Principal = principal;
                return CreateCommandResult(
                    context.Command, FtpReplyCodes.UserLoggedIn, "Logged in", false);
            }
            return CreateCommandResult(
                context.Command,
                FtpReplyCodes.ServiceClosingControlConnection,
                "Goodbye",
                true);
        };

        try
        {
            server.StartServer(options);
            options.ExtensionHandlers[0] = new RecordingExtensionHandler();
            using TcpClient client = Connect(port);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = new StreamReader(
                stream, Encoding.ASCII, false, 1024, true);
            Assert.That(reader.ReadLine(), Is.EqualTo("220 FTP service ready"));

            WriteCommands(stream,
                "X-VENDOR alpha\r\nOPTS X-MODE FAST\r\nQUIT\r\n");

            Assert.Multiple(() =>
            {
                Assert.That(reader.ReadLine(),
                    Is.EqualTo("200 Extension accepted"));
                Assert.That(reader.ReadLine(),
                    Is.EqualTo("200 Extension accepted"));
                Assert.That(reader.ReadLine(), Is.EqualTo("221 Goodbye"));
            });
            Assert.That(SpinWait.SpinUntil(() =>
                handler.GetContextCount() == 2, 3000), Is.True);

            FtpExtensionContext[] contexts = handler.GetContexts();
            FtpEvent[] recordedEvents = events.GetEvents();
            Assert.Multiple(() =>
            {
                Assert.That(contexts.Select(item => item.Command.Verb),
                    Is.EqualTo(new[] { "X-VENDOR", "OPTS" }));
                Assert.That(recordedEvents.Count(item =>
                    item.HasExtensionDisposition &&
                    item.ExtensionDisposition == FtpExtensionDisposition.Handled),
                    Is.EqualTo(2));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Live_session_routes_custom_security_without_intercepting_tls()
    {
        int port = ReserveAvailablePort();
        SecurityExtensionHandler handler = new SecurityExtensionHandler();
        RecordingExtensionEventSink events = new RecordingExtensionEventSink();
        FtpServer server = CreateServer(events);
        FtpServerOptions options = CreateOptions(port);
        options.ExtensionHandlers = new IFtpExtensionHandler[] { handler };
        server.Runtime.CommandOperation = context =>
        {
            if (context.Command.Kind == FtpCommandKind.Auth)
            {
                return CreateCommandResult(
                    context.Command,
                    FtpReplyCodes.CommandNotImplemented,
                    "Built-in TLS path",
                    false);
            }
            return CreateCommandResult(
                context.Command,
                FtpReplyCodes.ServiceClosingControlConnection,
                "Goodbye",
                true);
        };

        try
        {
            server.StartServer(options);
            using TcpClient client = Connect(port);
            using NetworkStream stream = client.GetStream();
            using StreamReader reader = new StreamReader(
                stream, Encoding.ASCII, false, 1024, true);
            Assert.That(reader.ReadLine(), Is.EqualTo("220 FTP service ready"));
            WriteCommands(stream,
                "AUTH CUSTOM\r\nADAT private-security-data\r\n" +
                "AUTH TLS\r\nQUIT\r\n");

            Assert.Multiple(() =>
            {
                Assert.That(reader.ReadLine(),
                    Is.EqualTo("334 Continue custom security"));
                Assert.That(reader.ReadLine(),
                    Is.EqualTo("235 Custom security complete"));
                Assert.That(reader.ReadLine(),
                    Is.EqualTo("502 Built-in TLS path"));
                Assert.That(reader.ReadLine(), Is.EqualTo("221 Goodbye"));
            });

            FtpExtensionContext[] contexts = handler.GetContexts();
            FtpEvent[] recordedEvents = events.GetEvents();
            Assert.Multiple(() =>
            {
                Assert.That(contexts.Select(item => item.Command.Kind),
                    Is.EqualTo(new[]
                    {
                        FtpCommandKind.Auth,
                        FtpCommandKind.Adat
                    }));
                Assert.That(contexts[1].Command.Argument,
                    Is.EqualTo("private-security-data"));
                Assert.That(recordedEvents.Where(item =>
                    item.Command != null &&
                    item.Command.Kind == FtpCommandKind.Adat).All(item =>
                        item.Command!.Argument ==
                            FtpTranscriptRedactor.RedactedText),
                    Is.True);
                Assert.That(recordedEvents.All(item =>
                    !item.Message.Contains("private-security-data")),
                    Is.True);
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Session_authorizes_site_before_dispatching_the_extension()
    {
        RecordingExtensionHandler handler = new RecordingExtensionHandler();
        handler.Result.Disposition = FtpExtensionDisposition.Handled;
        handler.Result.Replies = new[] { CreateReply(200, "SITE accepted") };
        FtpServerOptions settings = new FtpServerOptions();
        settings.ExtensionHandlers = new IFtpExtensionHandler[] { handler };
        FtpListenerOptions listener = new FtpListenerOptions();
        using Socket socket = new Socket(
            AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        FtpServerLimitManager limits = new FtpServerLimitManager(settings);
        FtpServerSessionContext connection = new FtpServerSessionContext(
            socket, listener, limits);
        FtpServerSession session = new FtpServerSession(
            connection,
            settings,
            CreateProxy<IFtpFileSystem>(),
            CreateProxy<IFtpAuthenticator>(),
            new AllowAllFtpAuthorizer(),
            CreateProxy<IFtpHostResolver>(),
            CreateProxy<IFtpCertificateProvider>(),
            new NullFtpEventSink(),
            context => CreateCommandResult(
                context.Command,
                FtpReplyCodes.CommandNotImplemented,
                "Not implemented",
                false));
        session.State.OpenConnection();
        session.State.State.SessionState = FtpSessionState.Authenticated;
        session.State.State.Principal = new FtpPrincipal();
        session.State.State.Principal.Name = "alice";
        session.State.State.Principal.Authenticated = true;
        FtpCommand command = FtpCommandParser.ParseDecodedCommand(
            "SITE X-CUSTOM value");

        FtpCommandResult? result = session.DispatchExtensionCommand(command);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.FinalReply!.Code,
                Is.EqualTo(FtpReplyCodes.CommandOkay));
            Assert.That(handler.GetContextCount(), Is.EqualTo(1));
            Assert.That(handler.GetContexts()[0].Principal!.Name,
                Is.EqualTo("alice"));
        });
    }

    private static FtpExtensionResult DispatchSingle(
        IFtpExtensionHandler handler)
    {
        FtpExtensionDispatcher dispatcher = new FtpExtensionDispatcher(
            new[] { handler });
        return dispatcher.DispatchExtension(new FtpExtensionContext());
    }

    private static void AssertRejectedFailure(FtpExtensionResult result)
    {
        Assert.That(result.Disposition,
            Is.EqualTo(FtpExtensionDisposition.Rejected));
        Assert.That(result.Replies, Has.Length.EqualTo(1));
        Assert.That(result.Replies[0].Code,
            Is.EqualTo(FtpReplyCodes.RequestedActionAbortedLocalError));
        Assert.That(result.CloseConnection, Is.False);
    }

    private static bool Handles(string text, FtpSequenceState state)
    {
        return FtpExtensionDispatcher.HandlesCommand(
            FtpCommandParser.ParseDecodedCommand(text), state);
    }

    private static FtpReply CreateReply(int code, string text)
    {
        return FtpServerSession.CreateReply(code, text);
    }

    private static FtpCommandResult CreateCommandResult(
        FtpCommand command,
        int code,
        string text,
        bool close)
    {
        FtpCommandResult result = new FtpCommandResult();
        result.Command = command;
        result.FinalReply = CreateReply(code, text);
        result.ConnectionClosed = close;
        result.Succeeded = code >= 200 && code < 300;
        return result;
    }

    private static FtpServer CreateServer(IFtpEventSink eventSink)
    {
        return new FtpServer(
            CreateProxy<IFtpFileSystem>(),
            CreateProxy<IFtpAuthenticator>(),
            new AllowAllFtpAuthorizer(),
            CreateProxy<IFtpHostResolver>(),
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

    private static void WriteCommands(NetworkStream stream, string commands)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(commands);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
    }

    private static int ReserveAvailablePort()
    {
        using TcpListener reservation = new TcpListener(
            IPAddress.Loopback, 0);
        reservation.ExclusiveAddressUse = true;
        reservation.Start();
        return ((IPEndPoint)reservation.LocalEndpoint).Port;
    }

    private static T CreateProxy<T>() where T : class
    {
        return DispatchProxy.Create<T, ExtensionProviderProxy>();
    }
}

public class RecordingExtensionHandler : IFtpExtensionHandler
{
    public List<FtpExtensionContext> Contexts;
    public FtpExtensionResult Result;
    private object ContextLock;

    public RecordingExtensionHandler()
    {
        Contexts = new List<FtpExtensionContext>();
        Result = new FtpExtensionResult();
        ContextLock = new object();
    }

    public FtpExtensionResult HandleExtensionCommand(
        FtpExtensionContext context)
    {
        lock (ContextLock)
        {
            Contexts.Add(context);
        }
        return Result;
    }

    public int GetContextCount()
    {
        lock (ContextLock)
        {
            return Contexts.Count;
        }
    }

    public FtpExtensionContext[] GetContexts()
    {
        lock (ContextLock)
        {
            return Contexts.ToArray();
        }
    }
}

public class DelegateExtensionHandler : IFtpExtensionHandler
{
    public Func<FtpExtensionContext, FtpExtensionResult> Operation;

    public DelegateExtensionHandler(
        Func<FtpExtensionContext, FtpExtensionResult> operation)
    {
        Operation = operation;
    }

    public FtpExtensionResult HandleExtensionCommand(
        FtpExtensionContext context)
    {
        return Operation(context);
    }
}

public class SecurityExtensionHandler : IFtpExtensionHandler
{
    private List<FtpExtensionContext> Contexts;
    private object ContextLock;

    public SecurityExtensionHandler()
    {
        Contexts = new List<FtpExtensionContext>();
        ContextLock = new object();
    }

    public FtpExtensionResult HandleExtensionCommand(
        FtpExtensionContext context)
    {
        lock (ContextLock)
        {
            Contexts.Add(context);
        }
        FtpExtensionResult result = new FtpExtensionResult();
        result.Disposition = FtpExtensionDisposition.Handled;
        result.Replies = new FtpReply[]
        {
            context.Command.Kind == FtpCommandKind.Auth ?
                FtpServerSession.CreateReply(
                    FtpReplyCodes.SecurityMechanismOkay,
                    "Continue custom security") :
                FtpServerSession.CreateReply(
                    FtpReplyCodes.SecurityExchangeCompleteWithData,
                    "Custom security complete")
        };
        return result;
    }

    public FtpExtensionContext[] GetContexts()
    {
        lock (ContextLock)
        {
            return Contexts.ToArray();
        }
    }
}

public class RecordingExtensionEventSink : IFtpEventSink
{
    private List<FtpEvent> Events;
    private object EventLock;

    public RecordingExtensionEventSink()
    {
        Events = new List<FtpEvent>();
        EventLock = new object();
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

public class ExtensionProviderProxy : DispatchProxy
{
    protected override object? Invoke(
        MethodInfo? targetMethod,
        object?[]? arguments)
    {
        throw new NotSupportedException(
            "The extension dispatcher test did not expect this provider call.");
    }
}
