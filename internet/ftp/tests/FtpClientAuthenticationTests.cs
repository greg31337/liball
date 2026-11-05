using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ftp.Tests;

public class FtpClientAuthenticationTests
{
    [Test]
    public void Configured_login_routes_HOST_before_USER_PASS_and_ACCT()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = RunScriptedServer(
            listener,
            new[]
            {
                "220 Virtual host ready\r\n",
                "331 Password required\r\n",
                "332 Account required\r\n",
                "230 Logged in\r\n",
            },
            commands);
        FtpClientOptions options = CreatePlainOptions(listener);
        options.VirtualHost = "tenant.example";
        options.Credentials.UserName = "alice";
        options.Credentials.Password = "secret";
        options.Credentials.Account = "project";
        FtpClient client = new FtpClient(null);

        client.Connect(options);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "HOST tenant.example",
                "USER alice",
                "PASS secret",
                "ACCT project",
            }));
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Authenticated));
            Assert.That(client.State.Session.State.SelectedHost, Is.EqualTo("tenant.example"));
            Assert.That(client.State.LastAuthentication!.Status, Is.EqualTo(FtpAuthenticationStatus.Success));
            Assert.That(client.State.LastAuthentication.Principal!.Name, Is.EqualTo("alice"));
            Assert.That(client.State.LastAuthentication.Principal.Account, Is.EqualTo("project"));
        });

        client.AbortConnection();
    }

    [Test]
    public void USER_can_complete_authentication_without_sending_PASS()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = RunScriptedServer(
            listener,
            new[] { "230 Logged in without a password\r\n" },
            commands);
        FtpClient client = new FtpClient(null);
        client.Connect(CreatePlainOptions(listener));
        FtpCredentials credentials = new FtpCredentials();
        credentials.UserName = "certificate-user";

        FtpAuthenticationResult result = client.AuthenticateUser(credentials);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[] { "USER certificate-user" }));
            Assert.That(result.Status, Is.EqualTo(FtpAuthenticationStatus.Success));
            Assert.That(result.Principal!.Authenticated, Is.True);
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Authenticated));
        });

        client.AbortConnection();
    }

    [Test]
    public void Missing_requested_password_returns_an_intermediate_result()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = RunScriptedServer(
            listener,
            new[] { "331 Send password\r\n" },
            commands);
        FtpClient client = new FtpClient(null);
        client.Connect(CreatePlainOptions(listener));
        FtpCredentials credentials = new FtpCredentials();
        credentials.UserName = "alice";

        FtpAuthenticationResult result = client.AuthenticateUser(credentials);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[] { "USER alice" }));
            Assert.That(result.Status, Is.EqualTo(FtpAuthenticationStatus.AdditionalInformationRequired));
            Assert.That(result.Challenge, Is.EqualTo("Send password"));
            Assert.That(result.Principal, Is.Null);
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Authenticating));
        });

        client.AbortConnection();
    }

    [Test]
    public void Challenge_password_and_superfluous_completion_are_accepted_exactly()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = RunScriptedServer(
            listener,
            new[]
            {
                "336 Challenge password required\r\n",
                "202 Password accepted by prior security exchange\r\n",
            },
            commands);
        FtpClient client = new FtpClient(null);
        client.Connect(CreatePlainOptions(listener));
        FtpCredentials credentials = new FtpCredentials();
        credentials.UserName = "challenge-user";
        credentials.Password = "response";

        FtpAuthenticationResult result = client.AuthenticateUser(credentials);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER challenge-user",
                "PASS response",
            }));
            Assert.That(result.Status, Is.EqualTo(FtpAuthenticationStatus.Success));
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Authenticated));
        });

        client.AbortConnection();
    }

    [Test]
    public void USER_can_request_ACCT_directly()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = RunScriptedServer(
            listener,
            new[]
            {
                "332 Account required\r\n",
                "230 Account accepted\r\n",
            },
            commands);
        FtpClient client = new FtpClient(null);
        client.Connect(CreatePlainOptions(listener));
        FtpCredentials credentials = new FtpCredentials();
        credentials.UserName = "account-user";
        credentials.Account = "account-7";

        FtpAuthenticationResult result = client.AuthenticateUser(credentials);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER account-user",
                "ACCT account-7",
            }));
            Assert.That(result.Status, Is.EqualTo(FtpAuthenticationStatus.Success));
            Assert.That(result.Principal!.Account, Is.EqualTo("account-7"));
        });

        client.AbortConnection();
    }

    [Test]
    public void Anonymous_credentials_preserve_anonymous_identity_semantics()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = RunScriptedServer(
            listener,
            new[]
            {
                "331 Anonymous password required\r\n",
                "230 Anonymous login accepted\r\n",
            },
            commands);
        FtpClient client = new FtpClient(null);
        client.Connect(CreatePlainOptions(listener));
        FtpCredentials credentials = new FtpCredentials();
        credentials.UserName = "anonymous";
        credentials.Password = "guest@example.test";
        credentials.Anonymous = true;

        FtpAuthenticationResult result = client.AuthenticateUser(credentials);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[]
            {
                "USER anonymous",
                "PASS guest@example.test",
            }));
            Assert.That(result.Status, Is.EqualTo(FtpAuthenticationStatus.Success));
            Assert.That(result.Principal!.Anonymous, Is.True);
            Assert.That(result.Principal.Name, Is.EqualTo("anonymous"));
        });

        client.AbortConnection();
    }

    [Test]
    public void Authentication_rejection_is_returned_without_closing_a_usable_session()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = RunScriptedServer(
            listener,
            new[] { "530 Login incorrect\r\n" },
            commands);
        FtpClient client = new FtpClient(null);
        client.Connect(CreatePlainOptions(listener));
        FtpCredentials credentials = new FtpCredentials();
        credentials.UserName = "unknown";

        FtpAuthenticationResult result = client.AuthenticateUser(credentials);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(FtpAuthenticationStatus.Failure));
            Assert.That(result.FailureMessage, Does.Contain("530"));
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Connected));
            Assert.That(client.State.Connection, Is.Not.Null);
        });

        client.AbortConnection();
    }

    [Test]
    public void Service_unavailable_authentication_reply_closes_the_session()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = RunScriptedServer(
            listener,
            new[] { "421 Service closing\r\n" },
            commands);
        FtpClient client = new FtpClient(null);
        client.Connect(CreatePlainOptions(listener));
        FtpCredentials credentials = new FtpCredentials();
        credentials.UserName = "alice";

        FtpAuthenticationResult result = client.AuthenticateUser(credentials);
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(FtpAuthenticationStatus.Failure));
            Assert.That(result.FailureMessage, Does.Contain("421"));
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Closed));
            Assert.That(client.State.Connection, Is.Null);
        });
    }

    [Test]
    public void HOST_rejection_aborts_configured_connection_before_USER()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = RunScriptedServer(
            listener,
            new[] { "504 HOST unavailable\r\n" },
            commands);
        FtpClientOptions options = CreatePlainOptions(listener);
        options.VirtualHost = "missing.example";
        options.Credentials.UserName = "must-not-be-sent";
        FtpClient client = new FtpClient(null);

        Assert.Throws<FtpCommandException>(() => client.Connect(options));
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[] { "HOST missing.example" }));
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Closed));
            Assert.That(client.State.Connection, Is.Null);
        });
    }

    [Test]
    public void Plaintext_policy_blocks_public_credentials_before_USER_is_written()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = RunScriptedServer(listener, new string[0], commands);
        FtpClientOptions options = CreatePlainOptions(listener);
        options.AllowPlaintextCredentials = false;
        FtpClient client = new FtpClient(null);
        client.Connect(options);
        FtpCredentials credentials = new FtpCredentials();
        credentials.UserName = "blocked";

        Assert.Throws<FtpConfigurationException>(() =>
            client.AuthenticateUser(credentials));
        client.AbortConnection();
        server.GetAwaiter().GetResult();

        Assert.That(commands, Is.Empty);
    }

    [Test]
    public void Unexpected_authentication_intermediate_reply_is_a_protocol_failure()
    {
        using TcpListener listener = StartListener();
        List<string> commands = new List<string>();
        Task server = RunScriptedServer(
            listener,
            new[] { "350 Unexpected continuation\r\n" },
            commands);
        FtpClient client = new FtpClient(null);
        client.Connect(CreatePlainOptions(listener));
        FtpCredentials credentials = new FtpCredentials();
        credentials.UserName = "alice";

        Assert.Throws<FtpProtocolException>(() =>
            client.AuthenticateUser(credentials));
        server.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(commands, Is.EqualTo(new[] { "USER alice" }));
            Assert.That(client.GetSessionState(), Is.EqualTo(FtpSessionState.Connected));
        });

        client.AbortConnection();
    }

    private static TcpListener StartListener()
    {
        TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return listener;
    }

    private static FtpClientOptions CreatePlainOptions(TcpListener listener)
    {
        FtpClientOptions options = new FtpClientOptions();
        options.Endpoint.Host = IPAddress.Loopback.ToString();
        options.Endpoint.Port = ((IPEndPoint)listener.LocalEndpoint).Port;
        options.Endpoint.AddressFamily = FtpAddressFamily.IPv4;
        options.SecurityMode = FtpSecurityMode.Plain;
        options.NegotiateFeatures = false;
        options.AllowPlaintextCredentials = true;
        options.Timeouts.ConnectTimeoutMilliseconds = 5000;
        options.Timeouts.ControlReadTimeoutMilliseconds = 5000;
        options.Timeouts.ControlWriteTimeoutMilliseconds = 5000;
        return options;
    }

    private static Task RunScriptedServer(
        TcpListener listener,
        string[] replies,
        List<string> commands)
    {
        return Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            WriteText(stream, "220 Server ready\r\n");
            int index = 0;
            while (index < replies.Length)
            {
                commands.Add(ReadCommand(stream));
                WriteText(stream, replies[index]);
                index++;
            }
            if (replies.Length == 0)
            {
                string? command = ReadOptionalCommand(stream);
                if (command != null)
                {
                    commands.Add(command);
                }
            }
        });
    }

    private static void WriteText(Stream stream, string text)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(text);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();
    }

    private static string ReadCommand(Stream stream)
    {
        string? command = ReadOptionalCommand(stream);
        if (command == null)
        {
            throw new EndOfStreamException(
                "The client closed before sending the scripted command.");
        }
        return command;
    }

    private static string? ReadOptionalCommand(Stream stream)
    {
        using MemoryStream bytes = new MemoryStream();
        bool carriageReturn = false;
        while (true)
        {
            int value = stream.ReadByte();
            if (value < 0)
            {
                return null;
            }
            if (carriageReturn)
            {
                if (value != 10)
                {
                    throw new InvalidDataException(
                        "The client command did not use CRLF.");
                }
                return Encoding.ASCII.GetString(bytes.ToArray());
            }
            if (value == 13)
            {
                carriageReturn = true;
            }
            else
            {
                bytes.WriteByte((byte)value);
            }
        }
    }
}
