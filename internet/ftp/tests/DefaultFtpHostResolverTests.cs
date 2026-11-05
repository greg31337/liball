using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace ftp.Tests;

public class DefaultFtpHostResolverTests
{
    [Test]
    public void Resolver_normalizes_names_returns_snapshots_and_reports_invalid_input()
    {
        FtpVirtualHostOptions named = CreateHost(
            "FTP.Example", "Named greeting", "/named", 2);
        FtpFeature feature = new FtpFeature();
        feature.Name = "UTF8";
        feature.RawLine = "UTF8";
        feature.Support = FtpFeatureSupport.Supported;
        named.Features.Features = new[] { feature };
        FtpVirtualHostOptions address = CreateHost(
            "[2001:0db8:0:0:0:0:0:1]", "Address greeting", "/address", 2);
        DefaultFtpHostResolver resolver = new DefaultFtpHostResolver(
            new[] { named, address });

        named.Greeting = "mutated";
        named.Features.Features[0].Name = "MUTATED";
        FtpVirtualHostResult first = resolver.ResolveVirtualHost(
            CreateRequest("ftp.example", null));
        FtpVirtualHostResult second = resolver.ResolveVirtualHost(
            CreateRequest("", "FTP.EXAMPLE"));
        FtpVirtualHostResult ipv6 = resolver.ResolveVirtualHost(
            CreateRequest("[2001:db8::1]", null));
        FtpVirtualHostResult missing = resolver.ResolveVirtualHost(
            CreateRequest("missing.example", null));
        FtpVirtualHostResult invalid = resolver.ResolveVirtualHost(
            CreateRequest("bad host", null));

        Assert.Multiple(() =>
        {
            Assert.That(first.Handled, Is.True);
            Assert.That(first.Found, Is.True);
            Assert.That(first.Host, Is.Not.Null);
            Assert.That(first.Host!.Greeting, Is.EqualTo("Named greeting"));
            Assert.That(first.Host.Features.Features[0].Name, Is.EqualTo("UTF8"));
            Assert.That(second.Found, Is.True);
            Assert.That(second.Host, Is.Not.SameAs(first.Host));
            Assert.That(second.Host!.Features, Is.Not.SameAs(first.Host.Features));
            Assert.That(ipv6.Found, Is.True);
            Assert.That(ipv6.Host!.RootPath, Is.EqualTo("/address"));
            Assert.That(missing.Handled, Is.True);
            Assert.That(missing.Found, Is.False);
            Assert.That(missing.FailureMessage, Is.Not.Empty);
            Assert.That(invalid.Handled, Is.True);
            Assert.That(invalid.Found, Is.False);
            Assert.That(invalid.FailureMessage, Is.Not.Empty);
        });
    }

    [Test]
    public void Host_command_routes_context_preserves_failed_selection_and_enforces_host_limit()
    {
        int port = ReserveAvailablePort();
        IFtpFileSystem globalFileSystem = CreateProxy<IFtpFileSystem>();
        IFtpAuthenticator globalAuthenticator = CreateProxy<IFtpAuthenticator>();
        IFtpAuthorizer globalAuthorizer = CreateProxy<IFtpAuthorizer>();
        IFtpCertificateProvider globalCertificate =
            CreateProxy<IFtpCertificateProvider>();
        IFtpFileSystem alphaFileSystem = CreateProxy<IFtpFileSystem>();
        IFtpAuthenticator alphaAuthenticator = CreateProxy<IFtpAuthenticator>();
        IFtpAuthorizer alphaAuthorizer = CreateProxy<IFtpAuthorizer>();
        IFtpCertificateProvider alphaCertificate =
            CreateProxy<IFtpCertificateProvider>();
        IFtpFileSystem betaFileSystem = CreateProxy<IFtpFileSystem>();

        FtpVirtualHostOptions alpha = CreateHost(
            "ftp.alpha", "Alpha ready", "/alpha", 1);
        alpha.FileSystem = alphaFileSystem;
        alpha.Authenticator = alphaAuthenticator;
        alpha.Authorizer = alphaAuthorizer;
        alpha.CertificateProvider = alphaCertificate;
        FtpFeature alphaFeature = new FtpFeature();
        alphaFeature.Name = "ALPHA";
        alphaFeature.RawLine = "ALPHA";
        alphaFeature.Support = FtpFeatureSupport.Supported;
        alpha.Features.Features = new[] { alphaFeature };

        FtpVirtualHostOptions beta = CreateHost(
            "ftp.beta", "Beta ready", "/beta", 2);
        beta.FileSystem = betaFileSystem;
        FtpFeature betaFeature = new FtpFeature();
        betaFeature.Name = "BETA";
        betaFeature.RawLine = "BETA";
        betaFeature.Support = FtpFeatureSupport.Supported;
        beta.Features.Features = new[] { betaFeature };

        FtpServerOptions options = CreateOptions(port, new[] { alpha, beta });
        DefaultFtpHostResolver resolver =
            new DefaultFtpHostResolver(options.VirtualHosts);
        RecordingServerEventSink events = new RecordingServerEventSink();
        FtpServer server = new FtpServer(
            globalFileSystem,
            globalAuthenticator,
            globalAuthorizer,
            resolver,
            globalCertificate,
            events);
        List<FtpServerCommandContext> dispatched =
            new List<FtpServerCommandContext>();
        List<bool> authenticationResults = new List<bool>();
        object dispatchLock = new object();
        server.Runtime.CommandOperation = context =>
        {
            lock (dispatchLock)
            {
                dispatched.Add(context);
                if (context.Command.Kind == FtpCommandKind.Noop &&
                    context.RootPath == "/beta")
                {
                    authenticationResults.Add(
                        context.Connection.TryAuthenticateUser("shared-user"));
                }
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
                context.Command, FtpReplyCodes.CommandOkay, "Okay", false);
        };

        try
        {
            server.StartServer(options);
            using TcpClient firstClient = Connect(port);
            using NetworkStream firstStream = firstClient.GetStream();
            using StreamReader firstReader = CreateReader(firstStream);
            Assert.That(firstReader.ReadLine(), Is.EqualTo("220 FTP service ready"));
            WriteCommands(firstStream, "HOST FTP.ALPHA\r\nNOOP\r\n");
            Assert.That(firstReader.ReadLine(), Is.EqualTo("220 Alpha ready"));
            Assert.That(firstReader.ReadLine(), Is.EqualTo("200 Okay"));

            using (TcpClient secondClient = Connect(port))
            using (NetworkStream secondStream = secondClient.GetStream())
            using (StreamReader secondReader = CreateReader(secondStream))
            {
                Assert.That(secondReader.ReadLine(),
                    Is.EqualTo("220 FTP service ready"));
                WriteCommands(secondStream, "HOST ftp.alpha\r\n");
                Assert.That(secondReader.ReadLine(), Is.EqualTo(
                    "421 The requested FTP virtual host has no available sessions."));
            }

            WriteCommands(firstStream,
                "HOST missing.example\r\nNOOP\r\nHOST ftp.beta\r\nNOOP\r\n");
            Assert.That(firstReader.ReadLine(), Is.EqualTo(
                "504 The requested FTP virtual host is unavailable."));
            Assert.That(firstReader.ReadLine(), Is.EqualTo("200 Okay"));
            Assert.That(firstReader.ReadLine(), Is.EqualTo("220 Beta ready"));
            Assert.That(firstReader.ReadLine(), Is.EqualTo("200 Okay"));

            using (TcpClient thirdClient = Connect(port))
            using (NetworkStream thirdStream = thirdClient.GetStream())
            using (StreamReader thirdReader = CreateReader(thirdStream))
            {
                Assert.That(thirdReader.ReadLine(),
                    Is.EqualTo("220 FTP service ready"));
                WriteCommands(thirdStream, "HOST ftp.beta\r\nNOOP\r\nQUIT\r\n");
                Assert.That(thirdReader.ReadLine(), Is.EqualTo("220 Beta ready"));
                Assert.That(thirdReader.ReadLine(), Is.EqualTo("200 Okay"));
                Assert.That(thirdReader.ReadLine(), Is.EqualTo("221 Goodbye"));
            }

            WriteCommands(firstStream, "QUIT\r\n");
            Assert.That(firstReader.ReadLine(), Is.EqualTo("221 Goodbye"));

            Assert.That(SpinWait.SpinUntil(() =>
                GetDispatcher(server).GetCompletedSessionCount() == 3, 3000), Is.True);
            FtpServerCommandContext[] contexts;
            lock (dispatchLock)
            {
                contexts = dispatched.ToArray();
            }
            Assert.Multiple(() =>
            {
                Assert.That(contexts, Has.Length.EqualTo(6));
                Assert.That(authenticationResults,
                    Is.EqualTo(new[] { true, false }));
                Assert.That(contexts[0].RootPath, Is.EqualTo("/alpha"));
                Assert.That(contexts[0].FileSystem, Is.SameAs(alphaFileSystem));
                Assert.That(contexts[0].Authenticator,
                    Is.SameAs(alphaAuthenticator));
                Assert.That(contexts[0].Authorizer, Is.SameAs(alphaAuthorizer));
                Assert.That(contexts[0].AuthorizationGate.Provider,
                    Is.SameAs(alphaAuthorizer));
                Assert.That(contexts[0].AuthorizationGate.Host!.HostName,
                    Is.EqualTo("ftp.alpha"));
                Assert.That(contexts[0].CertificateProvider,
                    Is.SameAs(alphaCertificate));
                Assert.That(contexts[0].Features.Features.Select(feature =>
                    feature.Name), Does.Contain("ALPHA"));
                Assert.That(contexts[1].RootPath, Is.EqualTo("/alpha"));
                Assert.That(contexts[2].RootPath, Is.EqualTo("/beta"));
                Assert.That(contexts[2].FileSystem, Is.SameAs(betaFileSystem));
                Assert.That(contexts[2].Authenticator,
                    Is.SameAs(globalAuthenticator));
                Assert.That(contexts[2].Authorizer, Is.SameAs(globalAuthorizer));
                Assert.That(contexts[2].CertificateProvider,
                    Is.SameAs(globalCertificate));
                Assert.That(contexts[2].Features.Features.Select(feature =>
                    feature.Name), Does.Contain("BETA"));
                Assert.That(contexts[3].RootPath, Is.EqualTo("/beta"));
                Assert.That(GetLimits(server).GetHostSessionCount("ftp.alpha"),
                    Is.Zero);
                Assert.That(GetLimits(server).GetHostSessionCount("ftp.beta"),
                    Is.Zero);
                Assert.That(GetLimits(server).GetHostUserSessionCount(
                    "ftp.beta", "shared-user"), Is.Zero);
                Assert.That(events.GetEvents().Any(eventData =>
                    eventData.Kind == FtpEventKind.PolicyRejection), Is.True);
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    private static FtpVirtualHostOptions CreateHost(
        string name,
        string greeting,
        string rootPath,
        int maximumSessions)
    {
        FtpVirtualHostOptions host = new FtpVirtualHostOptions();
        host.HostName = name;
        host.Greeting = greeting;
        host.RootPath = rootPath;
        host.MaximumSessions = maximumSessions;
        host.MaximumSessionsPerUser = 1;
        return host;
    }

    private static FtpVirtualHostRequest CreateRequest(
        string hostName,
        string? serverName)
    {
        FtpVirtualHostRequest request = new FtpVirtualHostRequest();
        request.HostName = hostName;
        request.ServerName = serverName;
        return request;
    }

    private static FtpServerOptions CreateOptions(
        int port,
        FtpVirtualHostOptions[] hosts)
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
        options.VirtualHosts = hosts;
        options.MaximumSessions = 4;
        options.MaximumSessionsPerAddress = 4;
        options.MaximumSessionsPerUser = 4;
        options.MaximumPendingConnections = 4;
        options.MaximumSimultaneousTransfers = 4;
        return options;
    }

    private static FtpCommandResult CreateResult(
        FtpCommand command,
        int code,
        string text,
        bool close)
    {
        return FtpServerSession.CreateCommandResult(
            command, code, text, close);
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
        return DispatchProxy.Create<T, DefaultHostProviderProxy>();
    }
}

public class DefaultHostProviderProxy : DispatchProxy
{
    protected override object? Invoke(
        MethodInfo? targetMethod,
        object?[]? arguments)
    {
        throw new NotSupportedException(
            "Virtual-host routing tests do not invoke policy providers.");
    }
}
