using System.Net;
using System.Net.Sockets;
using System.Text;

namespace pop3.Tests;

[NonParallelizable]
public class Pop3RfcTranscriptEndToEndTests
{
    [Test]
    public void PublicClientAndServerRunTheCombinedRfcTranscripts()
    {
        FakeAuthenticationProvider authentication = new FakeAuthenticationProvider();
        FakeMailboxSession mailbox = CreateRfc1939Mailbox();
        FakeLanguageProvider language = CreateRfc6856Languages();
        FakeTlsProvider serverTls = new FakeTlsProvider();
        FakeTlsProvider clientTls = new FakeTlsProvider();
        FakeProtocolLogger logger = new FakeProtocolLogger();
        Pop3ServerOptions serverOptions = CreateServerOptions(
            authentication,
            mailbox);
        serverOptions.SecurityMode = Pop3SecurityMode.StlsRequired;
        serverOptions.TlsProvider = serverTls;
        serverOptions.EnableUtf8 = true;
        serverOptions.EnableUtf8User = true;
        serverOptions.LanguageProvider = language;
        serverOptions.Logger = logger;
        using ServerConnection server = new ServerConnection(serverOptions);
        Pop3ClientOptions clientOptions = CreateClientOptions(server.Port);
        clientOptions.SecurityMode = Pop3SecurityMode.StlsRequired;
        clientOptions.TlsProvider = clientTls;
        Pop3Client client = new Pop3Client();

        client.ConnectHost(clientOptions, CancellationToken.None);
        Pop3CapabilitySet capabilities = client.GetSnapshot().Capabilities;
        client.EnableUtf8(CancellationToken.None);
        Pop3Language[] languages = client.GetLanguages(CancellationToken.None);
        client.SelectLanguage("es", CancellationToken.None);
        client.ResetLanguage(CancellationToken.None);
        Pop3Credentials credentials = new Pop3Credentials();
        credentials.AuthorizationIdentity = "test";
        credentials.Username = "test";
        credentials.Password = "test";
        Pop3AuthenticationResult authenticationResult = client.AuthenticateSasl(
            new Pop3PlainSaslClient(credentials, true, false),
            CancellationToken.None);
        Pop3MailboxStatus status = client.GetMailboxStatus(CancellationToken.None);
        Pop3MessageInfo[] messages = client.GetAllMessageInfo(CancellationToken.None);
        Pop3UniqueId[] uniqueIds = client.GetAllUniqueIds(CancellationToken.None);
        byte[] retrieved = client.RetrieveMessageToMemory(1, CancellationToken.None);
        byte[] top = client.RetrieveTopToMemory(1, 10, CancellationToken.None);
        client.DeleteMessage(1, CancellationToken.None);
        Pop3MailboxStatus restored = client.ResetDeletions(CancellationToken.None);
        client.NoOperation(CancellationToken.None);
        client.QuitAndCommit(CancellationToken.None);
        server.AwaitCompletion();

        string[] sent = logger.Entries
            .Where(entry => entry.Direction == Pop3LogDirection.ClientToServer)
            .Select(entry => entry.Text)
            .ToArray();
        Assert.That(serverTls.ServerCallCount, Is.EqualTo(1));
        Assert.That(clientTls.ClientCallCount, Is.EqualTo(1));
        Assert.That(capabilities.SupportsStls, Is.False);
        Assert.That(capabilities.SupportsUser, Is.True);
        Assert.That(capabilities.SaslMechanisms, Does.Contain("PLAIN"));
        Assert.That(capabilities.SupportsPipelining, Is.True);
        Assert.That(capabilities.SupportsUidl, Is.True);
        Assert.That(capabilities.SupportsUtf8, Is.True);
        Assert.That(capabilities.SupportsUtf8User, Is.True);
        Assert.That(capabilities.SupportsLang, Is.True);
        Assert.That(languages.Select(item => item.Tag), Is.EqualTo(new[]
        {
            "en", "en-boont", "de", "it", "es", "sv"
        }));
        Assert.That(language.SupportedLanguageRequests, Does.Contain("es"));
        Assert.That(authenticationResult.Status,
            Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(authentication.SaslPlainCallCount, Is.EqualTo(1));
        Assert.That(authentication.LastSaslPlainCredentials!.AuthorizationIdentity,
            Is.EqualTo("test"));
        Assert.That(authentication.LastSaslPlainCredentials.Username,
            Is.EqualTo("test"));
        Assert.That(authentication.LastSaslPlainCredentials.Password,
            Is.EqualTo("test"));
        Assert.That(status.MessageCount, Is.EqualTo(2));
        Assert.That(status.TotalOctets, Is.EqualTo(320));
        Assert.That(messages.Select(message => message.OctetCount),
            Is.EqualTo(new long[] { 120, 200 }));
        Assert.That(uniqueIds.Select(uniqueId => uniqueId.Value), Is.EqualTo(new[]
        {
            "whqtswO00WBw418f9t5JxYwZ",
            "QhdPYR:00WBw1Ph7x7"
        }));
        Assert.That(retrieved, Is.EqualTo(mailbox.MessageData[1]));
        Assert.That(top, Is.EqualTo(mailbox.TopData[1]));
        Assert.That(restored.MessageCount, Is.EqualTo(2));
        Assert.That(restored.TotalOctets, Is.EqualTo(320));
        Assert.That(mailbox.Calls, Does.Contain("mark-deleted:1"));
        Assert.That(mailbox.ResetCallCount, Is.EqualTo(1));
        Assert.That(mailbox.CommitCallCount, Is.EqualTo(1));
        Assert.That(mailbox.ReleaseCallCount, Is.EqualTo(1));
        Assert.That(sent.Count(command => command == "CAPA"),
            Is.GreaterThanOrEqualTo(2));
        Assert.That(sent, Does.Contain("STLS"));
        Assert.That(sent, Does.Contain("UTF8"));
        Assert.That(sent, Does.Contain("LANG"));
        Assert.That(sent, Does.Contain("LANG es"));
        Assert.That(sent, Does.Contain("LANG *"));
        Assert.That(sent, Does.Contain("AUTH PLAIN [redacted]"));
        Assert.That(sent, Does.Contain("STAT"));
        Assert.That(sent, Does.Contain("LIST"));
        Assert.That(sent, Does.Contain("UIDL"));
        Assert.That(sent, Does.Contain("RETR 1"));
        Assert.That(sent, Does.Contain("TOP 1 10"));
        Assert.That(sent, Does.Contain("DELE 1"));
        Assert.That(sent, Does.Contain("RSET"));
        Assert.That(sent, Does.Contain("NOOP"));
        Assert.That(sent, Does.Contain("QUIT"));
    }

    [Test]
    public void PublicClientClassifiesRfc3206TemporaryFailureAndRetries()
    {
        FakeAuthenticationProvider authentication = new FakeAuthenticationProvider();
        authentication.UserPasswordHandler = (credentials, connection, cancellationToken) =>
        {
            if (authentication.UserPasswordCallCount == 1)
            {
                Pop3AuthenticationResult temporary = new Pop3AuthenticationResult();
                temporary.Status = Pop3AuthenticationStatus.TemporaryFailure;
                return temporary;
            }
            return authentication.UserPasswordResult;
        };
        FakeMailboxSession mailbox = CreateRfc1939Mailbox();
        Pop3ServerOptions serverOptions = CreateServerOptions(
            authentication,
            mailbox);
        serverOptions.AllowUnprotectedCredentials = true;
        using ServerConnection server = new ServerConnection(serverOptions);
        Pop3ClientOptions clientOptions = CreateClientOptions(server.Port);
        clientOptions.Authentication.AllowUnprotectedCredentials = true;
        Pop3Client client = new Pop3Client();
        Pop3Credentials credentials = new Pop3Credentials();
        credentials.Username = "mrose";
        credentials.Password = "secret";
        client.ConnectHost(clientOptions, CancellationToken.None);

        Pop3AuthenticationException failure = Assert.Throws<Pop3AuthenticationException>(() =>
            client.AuthenticateUserPassword(
                credentials,
                CancellationToken.None))!;
        Pop3AuthenticationResult retried = client.AuthenticateUserPassword(
            credentials,
            CancellationToken.None);
        client.QuitAndCommit(CancellationToken.None);
        server.AwaitCompletion();

        Assert.That(failure.Result.Status,
            Is.EqualTo(Pop3AuthenticationStatus.TemporaryFailure));
        Assert.That(failure.Response.ResponseCodeLevels,
            Is.EqualTo(new[] { "SYS", "TEMP" }));
        Assert.That(retried.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(authentication.UserPasswordCallCount, Is.EqualTo(2));
        Assert.That(mailbox.CommitCallCount, Is.EqualTo(1));
        Assert.That(mailbox.ReleaseCallCount, Is.EqualTo(1));
    }

    static Pop3ServerOptions CreateServerOptions(
        FakeAuthenticationProvider authentication,
        FakeMailboxSession mailbox)
    {
        Pop3ServerOptions options = new Pop3ServerOptions();
        options.GreetingText = "POP3 server ready";
        options.ImplementationText = "RFC transcript server";
        options.AuthenticationProvider = authentication;
        options.MailboxProvider = new FakeMailboxProvider(mailbox);
        options.EnablePipelining = true;
        options.EnableTop = true;
        options.EnableUidl = true;
        options.EnableResponseCodes = true;
        options.ReadTimeoutMilliseconds = 5000;
        options.WriteTimeoutMilliseconds = 5000;
        options.AuthenticationTimeoutMilliseconds = 5000;
        options.BufferSize = 64;
        return options;
    }

    static Pop3ClientOptions CreateClientOptions(int port)
    {
        Pop3ClientOptions options = new Pop3ClientOptions();
        options.Host = IPAddress.Loopback.ToString();
        options.Port = port;
        options.ConnectTimeoutMilliseconds = 5000;
        options.ReadTimeoutMilliseconds = 5000;
        options.WriteTimeoutMilliseconds = 5000;
        options.AuthenticationTimeoutMilliseconds = 5000;
        options.BufferSize = 64;
        options.MessageMemoryCeilingBytes = 4096;
        options.EnablePipelining = true;
        options.Tls.TargetHost = "pop.example.com";
        return options;
    }

    static FakeMailboxSession CreateRfc1939Mailbox()
    {
        FakeMailboxSession mailbox = new FakeMailboxSession();
        mailbox.Status = Status(2, 320);
        mailbox.ResetStatus = Status(2, 320);
        mailbox.Messages = new[]
        {
            Message(1, 120),
            Message(2, 200)
        };
        mailbox.UniqueIds = new[]
        {
            UniqueId(1, "whqtswO00WBw418f9t5JxYwZ"),
            UniqueId(2, "QhdPYR:00WBw1Ph7x7")
        };
        byte[] first = Encoding.ASCII.GetBytes(
            "Subject: first\r\n\r\n" + new string('a', 100) + "\r\n");
        byte[] second = Encoding.ASCII.GetBytes(
            "Subject: second\r\n\r\n" + new string('b', 179) + "\r\n");
        mailbox.MessageData.Add(1, first);
        mailbox.MessageData.Add(2, second);
        mailbox.TopData.Add(1, first);
        mailbox.TopData.Add(2, second);
        return mailbox;
    }

    static FakeLanguageProvider CreateRfc6856Languages()
    {
        FakeLanguageProvider provider = new FakeLanguageProvider();
        provider.Languages = new[]
        {
            Language("en", "English"),
            Language("en-boont", "English Boontling dialect"),
            Language("de", "Deutsch"),
            Language("it", "Italiano"),
            Language("es", "Espanol"),
            Language("sv", "Svenska")
        };
        provider.SupportedLanguages.Add("es");
        provider.ResponseTexts.Add("lang-select-success", "es Idioma cambiado");
        provider.ResponseTexts.Add("lang-reset-success", "es Idioma cambiado");
        return provider;
    }

    static Pop3MailboxStatus Status(int count, long octets)
    {
        Pop3MailboxStatus status = new Pop3MailboxStatus();
        status.MessageCount = count;
        status.TotalOctets = octets;
        return status;
    }

    static Pop3MessageInfo Message(int number, long octets)
    {
        Pop3MessageInfo message = new Pop3MessageInfo();
        message.MessageNumber = number;
        message.OctetCount = octets;
        return message;
    }

    static Pop3UniqueId UniqueId(int number, string value)
    {
        Pop3UniqueId uniqueId = new Pop3UniqueId();
        uniqueId.MessageNumber = number;
        uniqueId.Value = value;
        return uniqueId;
    }

    static Pop3Language Language(string tag, string description)
    {
        Pop3Language language = new Pop3Language();
        language.Tag = tag;
        language.Description = description;
        return language;
    }

    sealed class ServerConnection : IDisposable
    {
        readonly TcpListener Listener;
        readonly Task ServerTask;

        internal int Port;

        internal ServerConnection(Pop3ServerOptions options)
        {
            Listener = new TcpListener(IPAddress.Loopback, 0);
            Listener.Start();
            Port = ((IPEndPoint)Listener.LocalEndpoint).Port;
            ServerTask = Task.Run(() => Serve(options));
        }

        public void Dispose()
        {
            Listener.Stop();
            if (!ServerTask.IsCompleted)
            {
                Task.WhenAny(ServerTask, Task.Delay(5000)).GetAwaiter().GetResult();
            }
        }

        internal void AwaitCompletion()
        {
            Task completed = Task.WhenAny(
                ServerTask,
                Task.Delay(5000)).GetAwaiter().GetResult();
            Assert.That(completed, Is.SameAs(ServerTask));
            ServerTask.GetAwaiter().GetResult();
        }

        void Serve(Pop3ServerOptions options)
        {
            using TcpClient accepted = Listener.AcceptTcpClient();
            Pop3ServerConnection connection = new Pop3ServerConnection();
            connection.Stream = accepted.GetStream();
            connection.LocalEndPoint = accepted.Client.LocalEndPoint?.ToString();
            connection.RemoteEndPoint = accepted.Client.RemoteEndPoint?.ToString();
            connection.LeaveOpen = true;
            new Pop3Server().ServeConnection(
                connection,
                options,
                CancellationToken.None);
        }
    }
}
