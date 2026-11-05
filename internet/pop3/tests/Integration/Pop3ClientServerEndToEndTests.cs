using System.Text;

namespace pop3.Tests;

public class Pop3ClientServerEndToEndTests
{
    [Test]
    public void PublicClientAndServerRunTheCompleteMailboxWorkflow()
    {
        using DuplexPair pair = new DuplexPair();
        AuthenticationProvider authentication = new AuthenticationProvider();
        MailboxSession mailbox = new MailboxSession();
        Pop3ServerOptions serverOptions = CreateServerOptions(
            authentication,
            mailbox,
            Pop3SecurityMode.Plaintext,
            null);
        serverOptions.EnableUtf8 = true;
        serverOptions.EnableUtf8User = true;
        serverOptions.LanguageProvider = new LanguageProvider();
        Task serverTask = StartServer(pair.Server, serverOptions);
        Pop3Client client = new Pop3Client();
        Pop3ClientOptions clientOptions = CreateClientOptions(
            Pop3SecurityMode.Plaintext,
            null);
        clientOptions.Authentication.AllowUnprotectedCredentials = true;
        clientOptions.EnablePipelining = true;

        client.ConnectStream(
            pair.Client,
            clientOptions,
            CancellationToken.None);
        client.EnableUtf8(CancellationToken.None);
        Pop3Language[] languages = client.GetLanguages(
            CancellationToken.None);
        client.SelectLanguage("fr", CancellationToken.None);
        Pop3AuthenticationResult result = client.AuthenticateUserPassword(
            Credentials(),
            CancellationToken.None);
        Pop3MailboxStatus status = client.GetMailboxStatus(
            CancellationToken.None);
        Pop3MessageInfo firstInfo = client.GetMessageInfo(
            1,
            CancellationToken.None);
        Pop3MessageInfo[] allInfo = client.GetAllMessageInfo(
            CancellationToken.None);
        Pop3UniqueId firstId = client.GetUniqueId(
            1,
            CancellationToken.None);
        Pop3UniqueId[] allIds = client.GetAllUniqueIds(
            CancellationToken.None);
        byte[][] messages = client.RetrieveMessagesToMemory(
            new[] { 1, 2 },
            CancellationToken.None);
        byte[][] tops = client.RetrieveTopBatchToMemory(
            new[] { 1, 2 },
            new[] { 1, 0 },
            CancellationToken.None);
        client.DeleteMessages(new[] { 1, 2 }, CancellationToken.None);
        Pop3MailboxStatus deletedStatus = client.GetMailboxStatus(
            CancellationToken.None);
        Pop3MailboxStatus restored = client.ResetDeletions(
            CancellationToken.None);
        client.NoOperation(CancellationToken.None);
        client.DeleteMessage(2, CancellationToken.None);
        client.QuitAndCommit(CancellationToken.None);
        AwaitServer(serverTask);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(languages.Select(language => language.Tag),
            Is.EqualTo(new[] { "en", "fr" }));
        Assert.That(status.MessageCount, Is.EqualTo(2));
        Assert.That(status.TotalOctets,
            Is.EqualTo(mailbox.Messages.Sum(message => message.LongLength)));
        Assert.That(firstInfo.MessageNumber, Is.EqualTo(1));
        Assert.That(allInfo.Select(info => info.MessageNumber),
            Is.EqualTo(new[] { 1, 2 }));
        Assert.That(firstId.Value, Is.EqualTo("uid-one"));
        Assert.That(allIds.Select(id => id.Value),
            Is.EqualTo(new[] { "uid-one", "uid-two" }));
        Assert.That(messages[0], Is.EqualTo(mailbox.Messages[0]));
        Assert.That(messages[1], Is.EqualTo(mailbox.Messages[1]));
        Assert.That(Encoding.UTF8.GetString(tops[0]), Is.EqualTo(
            "Subject: one\r\n\r\n.line one\r\n"));
        Assert.That(Encoding.UTF8.GetString(tops[1]), Is.EqualTo(
            "Subject: café\r\n\r\n"));
        Assert.That(deletedStatus.MessageCount, Is.Zero);
        Assert.That(deletedStatus.TotalOctets, Is.Zero);
        Assert.That(restored.MessageCount, Is.EqualTo(2));
        Assert.That(mailbox.MarkedNumbers, Is.EqualTo(new[] { 1, 2, 2 }));
        Assert.That(mailbox.ResetCount, Is.EqualTo(1));
        Assert.That(mailbox.CommitCount, Is.EqualTo(1));
        Assert.That(mailbox.RollbackCount, Is.Zero);
        Assert.That(mailbox.ReleaseCount, Is.EqualTo(1));
        Assert.That(mailbox.Utf8Requests, Does.Contain(true));
        Assert.That(client.GetSnapshot().State,
            Is.EqualTo(Pop3SessionState.Closed));
    }

    [TestCase(Pop3AuthenticationMethod.UserPassword)]
    [TestCase(Pop3AuthenticationMethod.Apop)]
    [TestCase(Pop3AuthenticationMethod.SaslPlain)]
    public void PublicAuthenticationMethodsInteroperate(
        Pop3AuthenticationMethod method)
    {
        using DuplexPair pair = new DuplexPair();
        AuthenticationProvider authentication = new AuthenticationProvider();
        MailboxSession mailbox = new MailboxSession();
        Pop3ServerOptions serverOptions = CreateServerOptions(
            authentication,
            mailbox,
            Pop3SecurityMode.Plaintext,
            null);
        serverOptions.EnableApop = true;
        Task serverTask = StartServer(pair.Server, serverOptions);
        Pop3Client client = new Pop3Client();
        Pop3ClientOptions clientOptions = CreateClientOptions(
            Pop3SecurityMode.Plaintext,
            null);
        clientOptions.Authentication.AllowUnprotectedCredentials = true;
        clientOptions.Authentication.AllowApop = true;
        client.ConnectStream(
            pair.Client,
            clientOptions,
            CancellationToken.None);

        Pop3AuthenticationResult result;
        if (method == Pop3AuthenticationMethod.UserPassword)
        {
            result = client.AuthenticateUserPassword(
                Credentials(),
                CancellationToken.None);
        }
        else if (method == Pop3AuthenticationMethod.Apop)
        {
            result = client.AuthenticateApop(
                Credentials(),
                CancellationToken.None);
        }
        else
        {
            Pop3PlainSaslClient sasl = new Pop3PlainSaslClient(
                Credentials(),
                false,
                true);
            result = client.AuthenticateSasl(
                sasl,
                CancellationToken.None);
        }
        Pop3MailboxStatus status = client.GetMailboxStatus(
            CancellationToken.None);
        client.QuitAndCommit(CancellationToken.None);
        AwaitServer(serverTask);

        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(status.MessageCount, Is.EqualTo(2));
        Assert.That(authentication.UserPasswordCount,
            Is.EqualTo(method == Pop3AuthenticationMethod.UserPassword ? 1 : 0));
        Assert.That(authentication.ApopCount,
            Is.EqualTo(method == Pop3AuthenticationMethod.Apop ? 1 : 0));
        Assert.That(authentication.SaslPlainCount,
            Is.EqualTo(method == Pop3AuthenticationMethod.SaslPlain ? 1 : 0));
        Assert.That(mailbox.CommitCount, Is.EqualTo(1));
        Assert.That(mailbox.ReleaseCount, Is.EqualTo(1));
    }

    [TestCase(Pop3SecurityMode.StlsRequired)]
    [TestCase(Pop3SecurityMode.ImplicitTls)]
    public void PublicTlsModesInteroperateAcrossTheDuplexBoundary(
        Pop3SecurityMode securityMode)
    {
        using DuplexPair pair = new DuplexPair();
        AuthenticationProvider authentication = new AuthenticationProvider();
        MailboxSession mailbox = new MailboxSession();
        PassThroughServerTlsProvider serverTls =
            new PassThroughServerTlsProvider();
        Pop3ServerOptions serverOptions = CreateServerOptions(
            authentication,
            mailbox,
            securityMode,
            serverTls);
        Task serverTask = StartServer(pair.Server, serverOptions);
        PassThroughClientTlsProvider clientTls =
            new PassThroughClientTlsProvider();
        Pop3ClientOptions clientOptions = CreateClientOptions(
            securityMode,
            clientTls);
        Pop3Client client = new Pop3Client();

        client.ConnectStream(
            pair.Client,
            clientOptions,
            CancellationToken.None);
        Pop3ClientSnapshot connected = client.GetSnapshot();
        client.AuthenticateUserPassword(
            Credentials(),
            CancellationToken.None);
        client.QuitAndCommit(CancellationToken.None);
        AwaitServer(serverTask);

        Assert.That(connected.IsSecure, Is.True);
        Assert.That(clientTls.CallCount, Is.EqualTo(1));
        Assert.That(serverTls.CallCount, Is.EqualTo(1));
        Assert.That(authentication.UserPasswordCount, Is.EqualTo(1));
        Assert.That(mailbox.CommitCount, Is.EqualTo(1));
        Assert.That(mailbox.ReleaseCount, Is.EqualTo(1));
    }

    [Test]
    public void ClientAbortCausesServerRollbackAndRelease()
    {
        using DuplexPair pair = new DuplexPair();
        AuthenticationProvider authentication = new AuthenticationProvider();
        MailboxSession mailbox = new MailboxSession();
        Task serverTask = StartServer(
            pair.Server,
            CreateServerOptions(
                authentication,
                mailbox,
                Pop3SecurityMode.Plaintext,
                null));
        Pop3ClientOptions clientOptions = CreateClientOptions(
            Pop3SecurityMode.Plaintext,
            null);
        clientOptions.Authentication.AllowUnprotectedCredentials = true;
        Pop3Client client = new Pop3Client();
        client.ConnectStream(
            pair.Client,
            clientOptions,
            CancellationToken.None);
        client.AuthenticateUserPassword(
            Credentials(),
            CancellationToken.None);
        client.DeleteMessage(1, CancellationToken.None);

        client.Abort();
        Task completed = Task.WhenAny(
            serverTask,
            Task.Delay(5000)).GetAwaiter().GetResult();

        Assert.That(completed, Is.SameAs(serverTask));
        Assert.Throws<EndOfStreamException>(() =>
            serverTask.GetAwaiter().GetResult());
        Assert.That(mailbox.MarkedNumbers, Is.EqualTo(new[] { 1 }));
        Assert.That(mailbox.CommitCount, Is.Zero);
        Assert.That(mailbox.RollbackCount, Is.EqualTo(1));
        Assert.That(mailbox.ReleaseCount, Is.EqualTo(1));
        Assert.That(client.GetSnapshot().State,
            Is.EqualTo(Pop3SessionState.Closed));
    }

    static Task StartServer(
        Stream stream,
        Pop3ServerOptions options)
    {
        Pop3ServerConnection connection = new Pop3ServerConnection();
        connection.Stream = stream;
        connection.LeaveOpen = false;
        return Task.Run(() => new Pop3Server().ServeConnection(
            connection,
            options,
            CancellationToken.None));
    }

    static void AwaitServer(Task serverTask)
    {
        Task completed = Task.WhenAny(
            serverTask,
            Task.Delay(5000)).GetAwaiter().GetResult();
        Assert.That(completed, Is.SameAs(serverTask));
        serverTask.GetAwaiter().GetResult();
    }

    static Pop3ServerOptions CreateServerOptions(
        AuthenticationProvider authentication,
        MailboxSession mailbox,
        Pop3SecurityMode securityMode,
        IPop3ServerTlsProvider? tlsProvider)
    {
        Pop3ServerOptions options = new Pop3ServerOptions();
        options.GreetingText = "ready";
        options.SecurityMode = securityMode;
        options.TlsProvider = tlsProvider;
        options.AuthenticationProvider = authentication;
        options.MailboxProvider = new MailboxProvider(mailbox);
        options.AllowUnprotectedCredentials = true;
        options.EnablePipelining = true;
        options.ReadTimeoutMilliseconds = 5000;
        options.WriteTimeoutMilliseconds = 5000;
        options.AuthenticationTimeoutMilliseconds = 5000;
        options.BufferSize = 64;
        return options;
    }

    static Pop3ClientOptions CreateClientOptions(
        Pop3SecurityMode securityMode,
        IPop3ClientTlsProvider? tlsProvider)
    {
        Pop3ClientOptions options = new Pop3ClientOptions();
        options.Host = "mail.test";
        options.SecurityMode = securityMode;
        options.TlsProvider = tlsProvider;
        options.Tls.TargetHost = "mail.test";
        options.ConnectTimeoutMilliseconds = 5000;
        options.ReadTimeoutMilliseconds = 5000;
        options.WriteTimeoutMilliseconds = 5000;
        options.AuthenticationTimeoutMilliseconds = 5000;
        options.BufferSize = 64;
        options.MessageMemoryCeilingBytes = 4096;
        options.LeaveStreamOpen = false;
        return options;
    }

    static Pop3Credentials Credentials()
    {
        Pop3Credentials credentials = new Pop3Credentials();
        credentials.Username = "alice";
        credentials.Password = "secret";
        return credentials;
    }

    sealed class AuthenticationProvider : IPop3AuthenticationProvider
    {
        internal int UserPasswordCount;
        internal int ApopCount;
        internal int SaslPlainCount;

        public Pop3AuthenticationResult CheckLoginDelay(
            string username,
            Pop3ServerConnection connection,
            CancellationToken cancellationToken)
        {
            Pop3AuthenticationResult result = new Pop3AuthenticationResult();
            result.Status = Pop3AuthenticationStatus.Success;
            return result;
        }

        public Pop3AuthenticationResult AuthenticateUserPassword(
            Pop3Credentials credentials,
            Pop3ServerConnection connection,
            CancellationToken cancellationToken)
        {
            UserPasswordCount++;
            return Success(credentials.Username);
        }

        public Pop3AuthenticationResult AuthenticateApop(
            Pop3ApopRequest request,
            Pop3ServerConnection connection,
            CancellationToken cancellationToken)
        {
            ApopCount++;
            return Success(request.Username);
        }

        public Pop3AuthenticationResult AuthenticateSaslPlain(
            Pop3Credentials credentials,
            Pop3ServerConnection connection,
            CancellationToken cancellationToken)
        {
            SaslPlainCount++;
            return Success(credentials.Username);
        }

        Pop3AuthenticationResult Success(string identity)
        {
            Pop3AuthenticationResult result = new Pop3AuthenticationResult();
            result.Status = Pop3AuthenticationStatus.Success;
            result.AuthenticatedIdentity = identity;
            result.MaildropKey = "mail-" + identity;
            return result;
        }
    }

    sealed class MailboxProvider : IPop3MailboxProvider
    {
        MailboxSession Session;

        internal MailboxProvider(MailboxSession session)
        {
            Session = session;
        }

        public Pop3MailboxOpenResult OpenMailbox(
            Pop3MailboxOpenRequest request,
            CancellationToken cancellationToken)
        {
            Pop3MailboxOpenResult result = new Pop3MailboxOpenResult();
            result.Status = Pop3AuthenticationStatus.Success;
            result.Session = Session;
            return result;
        }
    }

    sealed class MailboxSession : IPop3MailboxSession
    {
        internal byte[][] Messages;
        Pop3MailboxStatus Status = new Pop3MailboxStatus();
        Pop3MessageInfo[] MessageInfo;
        Pop3UniqueId[] UniqueIds;
        internal List<int> MarkedNumbers = new List<int>();
        internal List<bool> Utf8Requests = new List<bool>();
        internal int ResetCount;
        internal int CommitCount;
        internal int RollbackCount;
        internal int ReleaseCount;

        internal MailboxSession()
        {
            Messages = new[]
            {
                Encoding.ASCII.GetBytes(
                    "Subject: one\r\n\r\n.line one\r\nline two\r\n"),
                Encoding.UTF8.GetBytes("Subject: café\r\n\r\ncorps\r\n")
            };
            Status.MessageCount = Messages.Length;
            MessageInfo = new Pop3MessageInfo[Messages.Length];
            UniqueIds = new Pop3UniqueId[Messages.Length];
            int index = 0;
            while (index < Messages.Length)
            {
                Pop3MessageInfo info = new Pop3MessageInfo();
                info.MessageNumber = index + 1;
                info.OctetCount = Messages[index].LongLength;
                info.IsUtf8Representation = index == 1;
                MessageInfo[index] = info;
                Status.TotalOctets += info.OctetCount;
                Pop3UniqueId uniqueId = new Pop3UniqueId();
                uniqueId.MessageNumber = index + 1;
                uniqueId.Value = index == 0 ? "uid-one" : "uid-two";
                UniqueIds[index] = uniqueId;
                index++;
            }
        }

        public Pop3MailboxStatus GetStatus()
        {
            return Status;
        }

        public Pop3MessageInfo[] GetMessages()
        {
            return MessageInfo;
        }

        public Pop3UniqueId[] GetUniqueIds()
        {
            return UniqueIds;
        }

        public void WriteMessage(
            int messageNumber,
            bool useUtf8Representation,
            Stream destination,
            CancellationToken cancellationToken)
        {
            Utf8Requests.Add(useUtf8Representation);
            byte[] message = Messages[messageNumber - 1];
            destination.Write(message, 0, message.Length);
        }

        public void WriteTop(
            int messageNumber,
            int bodyLineCount,
            bool useUtf8Representation,
            Stream destination,
            CancellationToken cancellationToken)
        {
            Utf8Requests.Add(useUtf8Representation);
            string content;
            if (messageNumber == 1)
            {
                content = "Subject: one\r\n\r\n";
                if (bodyLineCount > 0)
                {
                    content += ".line one\r\n";
                }
            }
            else
            {
                content = "Subject: café\r\n\r\n";
                if (bodyLineCount > 0)
                {
                    content += "corps\r\n";
                }
            }
            byte[] data = Encoding.UTF8.GetBytes(content);
            destination.Write(data, 0, data.Length);
        }

        public void MarkDeleted(int messageNumber)
        {
            MarkedNumbers.Add(messageNumber);
        }

        public Pop3MailboxStatus ResetDeletions()
        {
            ResetCount++;
            return Status;
        }

        public void CommitUpdate(CancellationToken cancellationToken)
        {
            CommitCount++;
        }

        public void RollbackChanges()
        {
            RollbackCount++;
        }

        public void ReleaseSession()
        {
            ReleaseCount++;
        }
    }

    sealed class LanguageProvider : IPop3LanguageProvider
    {
        public Pop3Language[] GetLanguages()
        {
            Pop3Language english = new Pop3Language();
            english.Tag = "en";
            english.Description = "English";
            Pop3Language french = new Pop3Language();
            french.Tag = "fr";
            french.Description = "Français";
            return new[] { english, french };
        }

        public bool SupportsLanguage(string languageTag)
        {
            return languageTag == "en" || languageTag == "fr";
        }

        public string GetResponseText(Pop3LanguageTextRequest request)
        {
            return "localized";
        }
    }

    sealed class PassThroughClientTlsProvider : IPop3ClientTlsProvider
    {
        internal int CallCount;

        public Stream SecureClientStream(
            Stream stream,
            Pop3TlsClientOptions options,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return stream;
        }
    }

    sealed class PassThroughServerTlsProvider : IPop3ServerTlsProvider
    {
        internal int CallCount;

        public Stream SecureServerStream(
            Stream stream,
            Pop3TlsServerOptions options,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return stream;
        }
    }

    sealed class DuplexPair : IDisposable
    {
        internal DuplexEndpoint Client;
        internal DuplexEndpoint Server;

        internal DuplexPair()
        {
            ByteChannel clientToServer = new ByteChannel();
            ByteChannel serverToClient = new ByteChannel();
            Client = new DuplexEndpoint(serverToClient, clientToServer);
            Server = new DuplexEndpoint(clientToServer, serverToClient);
        }

        public void Dispose()
        {
            Client.Dispose();
            Server.Dispose();
        }
    }

    sealed class ByteChannel
    {
        object SyncRoot = new object();
        Queue<byte[]> Chunks = new Queue<byte[]>();
        byte[]? Current;
        int CurrentOffset;
        bool IsComplete;

        internal void Write(byte[] buffer, int offset, int count)
        {
            byte[] copy = new byte[count];
            Array.Copy(buffer, offset, copy, 0, count);
            lock (SyncRoot)
            {
                if (IsComplete)
                {
                    throw new ObjectDisposedException("DuplexEndpoint");
                }
                Chunks.Enqueue(copy);
                Monitor.PulseAll(SyncRoot);
            }
        }

        internal int Read(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            lock (SyncRoot)
            {
                while (Current == null && Chunks.Count == 0 && !IsComplete)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Monitor.Wait(SyncRoot, 20);
                }
                cancellationToken.ThrowIfCancellationRequested();
                if (Current == null && Chunks.Count > 0)
                {
                    Current = Chunks.Dequeue();
                    CurrentOffset = 0;
                }
                if (Current == null)
                {
                    return 0;
                }
                int copied = Math.Min(count, Current.Length - CurrentOffset);
                Array.Copy(Current, CurrentOffset, buffer, offset, copied);
                CurrentOffset += copied;
                if (CurrentOffset == Current.Length)
                {
                    Current = null;
                    CurrentOffset = 0;
                }
                return copied;
            }
        }

        internal void Complete()
        {
            lock (SyncRoot)
            {
                IsComplete = true;
                Monitor.PulseAll(SyncRoot);
            }
        }
    }

    sealed class DuplexEndpoint : Stream
    {
        ByteChannel Incoming;
        ByteChannel Outgoing;
        bool IsDisposed;

        internal DuplexEndpoint(ByteChannel incoming, ByteChannel outgoing)
        {
            Incoming = incoming;
            Outgoing = outgoing;
        }

        public override bool CanRead => !IsDisposed;
        public override bool CanSeek => false;
        public override bool CanWrite => !IsDisposed;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Incoming.Read(
                buffer,
                offset,
                count,
                CancellationToken.None);
        }

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Incoming.Read(
                buffer,
                offset,
                count,
                cancellationToken));
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Outgoing.Write(buffer, offset, count);
        }

        public override Task WriteAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Outgoing.Write(buffer, offset, count);
            return Task.CompletedTask;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                Outgoing.Complete();
            }
            base.Dispose(disposing);
        }
    }
}
