using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;

namespace ftp.Tests;

public class FtpServerRfc959CommandMatrixTests
{
    [Test]
    public void Every_fixed_argument_shape_returns_the_exact_syntax_reply()
    {
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer();

        try
        {
            server.StartServer(CreateOptions(port));
            FtpCommandDefinition[] definitions = GetRfc959Definitions();
            List<string> expected = new List<string>();
            List<string?> actual = new List<string?>();
            List<string?> noOperationReplies = new List<string?>();
            int index = 0;
            while (index < definitions.Length)
            {
                FtpCommandDefinition definition = definitions[index];
                if (definition.ArgumentRequired ||
                    !definition.ArgumentAllowed)
                {
                    using TcpClient client = Connect(port);
                    using NetworkStream stream = client.GetStream();
                    using StreamReader reader = CreateReader(stream);
                    Assert.That(reader.ReadLine(),
                        Is.EqualTo("220 FTP service ready"));
                    string command = definition.ArgumentRequired ?
                        definition.Verb : definition.Verb + " unexpected";
                    WriteText(stream, command + "\r\n");
                    actual.Add(reader.ReadLine());
                    bool parserFailure = definition.ArgumentRequired &&
                        ParserValidatesArgument(definition.Kind);
                    if (parserFailure)
                    {
                        expected.Add("500 Malformed FTP command line.");
                    }
                    else if (definition.ArgumentRequired)
                    {
                        expected.Add(
                            "501 The FTP command requires an argument.");
                    }
                    else
                    {
                        expected.Add(
                            "501 The FTP command does not accept an argument.");
                    }
                    if (!parserFailure)
                    {
                        WriteText(stream, "NOOP\r\nQUIT\r\n");
                        noOperationReplies.Add(reader.ReadLine());
                        Assert.That(reader.ReadLine(), Is.EqualTo(
                            "221 FTP service closing control connection."),
                            definition.Kind.ToString());
                    }
                }
                index++;
            }

            Assert.Multiple(() =>
            {
                Assert.That(actual, Is.EqualTo(expected));
                Assert.That(actual, Has.Count.EqualTo(29));
                Assert.That(noOperationReplies, Has.All.EqualTo(
                    "200 NOOP command successful."));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    [Test]
    public void Every_authenticated_only_command_returns_the_exact_530_reply()
    {
        int port = ReserveAvailablePort();
        FtpServer server = CreateServer();

        try
        {
            server.StartServer(CreateOptions(port));
            FtpCommandDefinition[] definitions = GetRfc959Definitions()
                .Where(definition => definition.RequiresAuthentication)
                .ToArray();
            List<string?> replies = new List<string?>();
            List<string?> stateProbes = new List<string?>();
            int index = 0;
            while (index < definitions.Length)
            {
                FtpCommandDefinition definition = definitions[index];
                using TcpClient client = Connect(port);
                using NetworkStream stream = client.GetStream();
                using StreamReader reader = CreateReader(stream);
                Assert.That(reader.ReadLine(),
                    Is.EqualTo("220 FTP service ready"));
                string command = definition.Verb;
                if (definition.ArgumentRequired)
                {
                    command += " " + BuildValidArgument(definition.Kind);
                }
                WriteText(stream, command + "\r\n");
                replies.Add(reader.ReadLine());
                WriteText(stream, "PWD\r\nQUIT\r\n");
                stateProbes.Add(reader.ReadLine());
                Assert.That(reader.ReadLine(), Is.EqualTo(
                    "221 FTP service closing control connection."));
                index++;
            }

            using TcpClient sequenceClient = Connect(port);
            using NetworkStream sequenceStream = sequenceClient.GetStream();
            using StreamReader sequenceReader = CreateReader(sequenceStream);
            Assert.That(sequenceReader.ReadLine(),
                Is.EqualTo("220 FTP service ready"));
            WriteText(sequenceStream,
                "PASS secret\r\nACCT billing\r\nQUIT\r\n");
            string? password = sequenceReader.ReadLine();
            string? account = sequenceReader.ReadLine();
            string? quit = sequenceReader.ReadLine();

            Assert.Multiple(() =>
            {
                Assert.That(definitions, Has.Length.EqualTo(25));
                Assert.That(replies, Has.All.EqualTo(
                    "530 The FTP command requires an authenticated session."));
                Assert.That(stateProbes, Has.All.EqualTo(
                    "530 The FTP command requires an authenticated session."));
                Assert.That(password, Is.EqualTo(
                    "503 PASS must follow USER when a password is requested."));
                Assert.That(account, Is.EqualTo(
                    "503 ACCT must follow a reply that requests account information."));
                Assert.That(quit,
                    Is.EqualTo("221 FTP service closing control connection."));
            });
        }
        finally
        {
            server.StopServer();
        }
    }

    private static FtpCommandDefinition[] GetRfc959Definitions()
    {
        return FtpCommandCatalog.GetDefinitions().Where(definition =>
            definition.Kind >= FtpCommandKind.Abor &&
            definition.Kind <= FtpCommandKind.Pwd).ToArray();
    }

    private static bool ParserValidatesArgument(FtpCommandKind kind)
    {
        return kind == FtpCommandKind.Allo ||
            kind == FtpCommandKind.Mode ||
            kind == FtpCommandKind.Port ||
            kind == FtpCommandKind.Rest ||
            kind == FtpCommandKind.Stru ||
            kind == FtpCommandKind.Type;
    }

    private static string BuildValidArgument(FtpCommandKind kind)
    {
        switch (kind)
        {
            case FtpCommandKind.Allo:
                return "1";
            case FtpCommandKind.Mode:
                return "S";
            case FtpCommandKind.Port:
                return "127,0,0,1,200,1";
            case FtpCommandKind.Rest:
                return "0";
            case FtpCommandKind.Stru:
                return "F";
            case FtpCommandKind.Type:
                return "I";
            default:
                return "value";
        }
    }

    private static FtpServer CreateServer()
    {
        return new FtpServer(
            CreateProxy<IFtpFileSystem>(),
            CreateProxy<IFtpAuthenticator>(),
            CreateProxy<IFtpAuthorizer>(),
            CreateProxy<IFtpHostResolver>(),
            CreateProxy<IFtpCertificateProvider>(),
            CreateProxy<IFtpEventSink>());
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
        options.Listeners = new[] { listener };
        options.MaximumSessionsPerAddress = 128;
        options.MaximumSessionsPerUser = 128;
        options.MaximumCommandsPerMinute = 1000;
        options.MaximumMalformedLines = 100;
        return options;
    }

    private static TcpClient Connect(int port)
    {
        TcpClient client = new TcpClient(AddressFamily.InterNetwork);
        client.Connect(IPAddress.Loopback, port);
        return client;
    }

    private static StreamReader CreateReader(Stream stream)
    {
        return new StreamReader(
            stream, Encoding.ASCII, false, 1024, true);
    }

    private static void WriteText(Stream stream, string text)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
    }

    private static int ReserveAvailablePort()
    {
        using TcpListener reservation =
            new TcpListener(IPAddress.Loopback, 0);
        reservation.ExclusiveAddressUse = true;
        reservation.Start();
        return ((IPEndPoint)reservation.LocalEndpoint).Port;
    }

    private static T CreateProxy<T>() where T : class
    {
        return DispatchProxy.Create<T, Rfc959CommandProviderProxy>();
    }
}

public class Rfc959CommandProviderProxy : DispatchProxy
{
    protected override object? Invoke(
        MethodInfo? targetMethod,
        object?[]? arguments)
    {
        throw new NotSupportedException(
            "RFC 959 precondition tests do not invoke providers.");
    }
}
