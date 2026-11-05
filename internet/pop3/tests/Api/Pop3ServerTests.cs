using System.Text;

namespace pop3.Tests;

public class Pop3ServerTests
{
    [Test]
    public void PublicServerRunsCompleteUserPasswordMailboxWorkflow()
    {
        string message = "Subject: test\r\n\r\nbody\r\n";
        ServerStream stream = new ServerStream(
            "USER alice\r\n" +
            "PASS secret\r\n" +
            "STAT\r\n" +
            "LIST\r\n" +
            "LIST 1\r\n" +
            "UIDL\r\n" +
            "UIDL 1\r\n" +
            "RETR 1\r\n" +
            "TOP 1 0\r\n" +
            "DELE 1\r\n" +
            "RSET\r\n" +
            "NOOP\r\n" +
            "QUIT\r\n");
        AuthenticationProvider authentication =
            new AuthenticationProvider();
        MailboxSession session = new MailboxSession(message);
        MailboxProvider mailbox = new MailboxProvider(session);
        Pop3ServerOptions options = Options();
        options.AllowUnprotectedCredentials = true;
        options.AuthenticationProvider = authentication;
        options.MailboxProvider = mailbox;
        Pop3ServerConnection connection = Connection(stream, true);
        IPop3Server server = new Pop3Server();

        server.ServeConnection(connection, options, CancellationToken.None);

        Assert.That(authentication.Username, Is.EqualTo("alice"));
        Assert.That(authentication.Password, Is.EqualTo("secret"));
        Assert.That(mailbox.Request, Is.Not.Null);
        Assert.That(mailbox.Request!.AuthenticatedIdentity,
            Is.EqualTo("identity-alice"));
        Assert.That(mailbox.Request.MaildropKey, Is.EqualTo("mail-alice"));
        Assert.That(session.MessageWrites, Is.EqualTo(1));
        Assert.That(session.TopWrites, Is.EqualTo(1));
        Assert.That(session.MarkedNumbers, Is.EqualTo(new[] { 1 }));
        Assert.That(session.ResetCount, Is.EqualTo(1));
        Assert.That(session.CommitCount, Is.EqualTo(1));
        Assert.That(session.RollbackCount, Is.Zero);
        Assert.That(session.ReleaseCount, Is.EqualTo(1));
        Assert.That(stream.Disposed, Is.False);
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n" +
            "+OK Send password.\r\n" +
            "+OK Authentication successful.\r\n" +
            "+OK 1 23 Maildrop status.\r\n" +
            "+OK Scan listing follows.\r\n" +
            "1 23\r\n" +
            ".\r\n" +
            "+OK 1 23 Message scan listing.\r\n" +
            "+OK Unique-id listing follows.\r\n" +
            "1 uid-one\r\n" +
            ".\r\n" +
            "+OK 1 uid-one Unique-id listing.\r\n" +
            "+OK 23 Message follows.\r\n" +
            message +
            ".\r\n" +
            "+OK Top of message follows.\r\n" +
            "Subject: test\r\n" +
            "\r\n" +
            ".\r\n" +
            "+OK Message marked for deletion.\r\n" +
            "+OK 1 23 Maildrop restored.\r\n" +
            "+OK No operation.\r\n" +
            "+OK Update committed; server signing off.\r\n"));

        stream.Dispose();
    }

    [Test]
    public void PipelinedNegativesPreserveStateAndQuitRejectsTrailingInput()
    {
        ServerStream stream = new ServerStream(
            "USER alice\r\n" +
            "PASS secret\r\n" +
            "DELE 9\r\n" +
            "STAT\r\n" +
            "DELE 1\r\n" +
            "DELE 1\r\n" +
            "STAT\r\n" +
            "RSET\r\n" +
            "NOOP\r\n" +
            "QUIT\r\n" +
            "DELE 1\r\n");
        MailboxSession session = new MailboxSession("body\r\n");
        Pop3ServerOptions options = Options();
        options.EnablePipelining = true;
        options.AllowUnprotectedCredentials = true;
        options.AuthenticationProvider = new AuthenticationProvider();
        options.MailboxProvider = new MailboxProvider(session);

        new Pop3Server().ServeConnection(
            Connection(stream, true),
            options,
            CancellationToken.None);

        Assert.That(session.MarkedNumbers, Is.EqualTo(new[] { 1 }));
        Assert.That(session.ResetCount, Is.EqualTo(1));
        Assert.That(session.CommitCount, Is.EqualTo(1));
        Assert.That(session.RollbackCount, Is.Zero);
        Assert.That(session.ReleaseCount, Is.EqualTo(1));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n" +
            "+OK Send password.\r\n" +
            "+OK Authentication successful.\r\n" +
            "-ERR No such message.\r\n" +
            "+OK 1 6 Maildrop status.\r\n" +
            "+OK Message marked for deletion.\r\n" +
            "-ERR Message is already marked for deletion.\r\n" +
            "+OK 0 0 Maildrop status.\r\n" +
            "+OK 1 6 Maildrop restored.\r\n" +
            "+OK No operation.\r\n" +
            "+OK Update committed; server signing off.\r\n"));

        stream.Dispose();
    }

    [Test]
    public void PublicServerRunsAuthorizationCapabilitiesUtf8AndLanguageCommands()
    {
        ServerStream stream = new ServerStream(
            "CAPA\r\n" +
            "UTF8\r\n" +
            "LANG\r\n" +
            "LANG fr\r\n" +
            "LANG *\r\n" +
            "QUIT\r\n");
        LanguageProvider language = new LanguageProvider();
        Pop3ServerOptions options = Options();
        options.EnableUtf8 = true;
        options.LanguageProvider = language;
        Pop3ServerConnection connection = Connection(stream, true);

        new Pop3Server().ServeConnection(
            connection,
            options,
            CancellationToken.None);

        Assert.That(language.SelectedTags, Is.EqualTo(new[] { "fr" }));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n" +
            "+OK Capability list follows\r\n" +
            "RESP-CODES\r\n" +
            "PIPELINING\r\n" +
            "UTF8\r\n" +
            "LANG\r\n" +
            ".\r\n" +
            "+OK UTF8 mode enabled.\r\n" +
            "+OK Language listing follows.\r\n" +
            "en English\r\n" +
            "fr Français\r\n" +
            ".\r\n" +
            "+OK fr langue choisie\r\n" +
            "+OK Server default language selected.\r\n" +
            "+OK Server signing off.\r\n"));

        stream.Dispose();
    }

    [Test]
    public void RejectedAuthenticationReturnsToAuthorizationUntilQuit()
    {
        ServerStream stream = new ServerStream(
            "USER alice\r\nPASS wrong\r\nQUIT\r\n");
        AuthenticationProvider authentication =
            new AuthenticationProvider();
        authentication.RejectPassword = true;
        MailboxSession session = new MailboxSession("");
        MailboxProvider mailbox = new MailboxProvider(session);
        Pop3ServerOptions options = Options();
        options.AllowUnprotectedCredentials = true;
        options.AuthenticationProvider = authentication;
        options.MailboxProvider = mailbox;

        new Pop3Server().ServeConnection(
            Connection(stream, false),
            options,
            CancellationToken.None);

        Assert.That(mailbox.OpenCount, Is.Zero);
        Assert.That(session.CommitCount, Is.Zero);
        Assert.That(session.ReleaseCount, Is.Zero);
        Assert.That(stream.Disposed, Is.True);
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n" +
            "+OK Send password.\r\n" +
            "-ERR [AUTH] Authentication failed.\r\n" +
            "+OK Server signing off.\r\n"));
    }

    [Test]
    public void UnexpectedEofRollsBackOpenMailboxAndPropagatesFailure()
    {
        ServerStream stream = new ServerStream(
            "USER alice\r\nPASS secret\r\nDELE 1\r\n");
        MailboxSession session = new MailboxSession("body\r\n");
        Pop3ServerOptions options = Options();
        options.AllowUnprotectedCredentials = true;
        options.AuthenticationProvider = new AuthenticationProvider();
        options.MailboxProvider = new MailboxProvider(session);

        Assert.Throws<EndOfStreamException>(() =>
            new Pop3Server().ServeConnection(
                Connection(stream, true),
                options,
                CancellationToken.None));

        Assert.That(session.MarkedNumbers, Is.EqualTo(new[] { 1 }));
        Assert.That(session.CommitCount, Is.Zero);
        Assert.That(session.RollbackCount, Is.EqualTo(1));
        Assert.That(session.ReleaseCount, Is.EqualTo(1));
        Assert.That(stream.Disposed, Is.False);
        stream.Dispose();
    }

    [Test]
    public void PublicServerDispatchesCustomCloseCommand()
    {
        ServerStream stream = new ServerStream("XBYE\r\n");
        ClosingExtension extension = new ClosingExtension();
        Pop3ServerOptions options = Options();
        options.Extensions = new IPop3ServerExtension[] { extension };

        new Pop3Server().ServeConnection(
            Connection(stream, true),
            options,
            CancellationToken.None);

        Assert.That(extension.ExecutionCount, Is.EqualTo(1));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n+OK extension closing\r\n"));
        Assert.That(stream.Disposed, Is.False);
        stream.Dispose();
    }

    static Pop3ServerOptions Options()
    {
        Pop3ServerOptions options = new Pop3ServerOptions();
        options.GreetingText = "ready";
        options.SecurityMode = Pop3SecurityMode.Plaintext;
        options.BufferSize = 64;
        return options;
    }

    static Pop3ServerConnection Connection(
        ServerStream stream,
        bool leaveOpen)
    {
        Pop3ServerConnection connection = new Pop3ServerConnection();
        connection.Stream = stream;
        connection.LeaveOpen = leaveOpen;
        connection.LocalEndPoint = "local";
        connection.RemoteEndPoint = "remote";
        return connection;
    }

    sealed class AuthenticationProvider : IPop3AuthenticationProvider
    {
        internal bool RejectPassword;
        internal string? Username;
        internal string? Password;

        public Pop3AuthenticationResult CheckLoginDelay(
            string username,
            Pop3ServerConnection connection,
            CancellationToken cancellationToken)
        {
            return Success(username);
        }

        public Pop3AuthenticationResult AuthenticateUserPassword(
            Pop3Credentials credentials,
            Pop3ServerConnection connection,
            CancellationToken cancellationToken)
        {
            Username = credentials.Username;
            Password = credentials.Password;
            if (RejectPassword)
            {
                Pop3AuthenticationResult rejected =
                    new Pop3AuthenticationResult();
                rejected.Status = Pop3AuthenticationStatus.Rejected;
                return rejected;
            }
            return Success(credentials.Username);
        }

        public Pop3AuthenticationResult AuthenticateApop(
            Pop3ApopRequest request,
            Pop3ServerConnection connection,
            CancellationToken cancellationToken)
        {
            return Success(request.Username);
        }

        public Pop3AuthenticationResult AuthenticateSaslPlain(
            Pop3Credentials credentials,
            Pop3ServerConnection connection,
            CancellationToken cancellationToken)
        {
            return Success(credentials.Username);
        }

        Pop3AuthenticationResult Success(string username)
        {
            Pop3AuthenticationResult result = new Pop3AuthenticationResult();
            result.Status = Pop3AuthenticationStatus.Success;
            result.AuthenticatedIdentity = "identity-" + username;
            result.MaildropKey = "mail-" + username;
            return result;
        }
    }

    sealed class MailboxProvider : IPop3MailboxProvider
    {
        MailboxSession Session;
        internal int OpenCount;
        internal Pop3MailboxOpenRequest? Request;

        internal MailboxProvider(MailboxSession session)
        {
            Session = session;
        }

        public Pop3MailboxOpenResult OpenMailbox(
            Pop3MailboxOpenRequest request,
            CancellationToken cancellationToken)
        {
            OpenCount++;
            Request = request;
            Pop3MailboxOpenResult result = new Pop3MailboxOpenResult();
            result.Status = Pop3AuthenticationStatus.Success;
            result.Session = Session;
            return result;
        }
    }

    sealed class MailboxSession : IPop3MailboxSession
    {
        byte[] Message;
        Pop3MailboxStatus Status = new Pop3MailboxStatus();
        Pop3MessageInfo[] Messages;
        Pop3UniqueId[] UniqueIds;
        internal List<int> MarkedNumbers = new List<int>();
        internal int MessageWrites;
        internal int TopWrites;
        internal int ResetCount;
        internal int CommitCount;
        internal int RollbackCount;
        internal int ReleaseCount;

        internal MailboxSession(string message)
        {
            Message = Encoding.ASCII.GetBytes(message);
            Status.MessageCount = 1;
            Status.TotalOctets = Message.Length;
            Pop3MessageInfo info = new Pop3MessageInfo();
            info.MessageNumber = 1;
            info.OctetCount = Message.Length;
            Messages = new[] { info };
            Pop3UniqueId uniqueId = new Pop3UniqueId();
            uniqueId.MessageNumber = 1;
            uniqueId.Value = "uid-one";
            UniqueIds = new[] { uniqueId };
        }

        public Pop3MailboxStatus GetStatus()
        {
            return Status;
        }

        public Pop3MessageInfo[] GetMessages()
        {
            return Messages;
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
            MessageWrites++;
            destination.Write(Message, 0, Message.Length);
        }

        public void WriteTop(
            int messageNumber,
            int bodyLineCount,
            bool useUtf8Representation,
            Stream destination,
            CancellationToken cancellationToken)
        {
            TopWrites++;
            destination.Write(Message, 0, Message.Length);
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
        internal List<string> SelectedTags = new List<string>();

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
            if (languageTag == "fr")
            {
                SelectedTags.Add(languageTag);
                return true;
            }
            return false;
        }

        public string GetResponseText(Pop3LanguageTextRequest request)
        {
            if (request.ResponseTextKey == "lang-select-success")
            {
                return "langue choisie";
            }
            return "localisé";
        }
    }

    sealed class ClosingExtension : IPop3ServerExtension
    {
        internal int ExecutionCount;

        public Pop3Capability[] GetCapabilities(Pop3ExtensionContext context)
        {
            return Array.Empty<Pop3Capability>();
        }

        public string[] GetCommandNames()
        {
            return new[] { "XBYE" };
        }

        public Pop3SessionState[] GetAllowedStates(string commandName)
        {
            return new[] { Pop3SessionState.Authorization };
        }

        public Pop3ExtensionResult ExecuteCommand(
            Pop3ExtensionContext context,
            Pop3Command command,
            CancellationToken cancellationToken)
        {
            ExecutionCount++;
            Pop3ExtensionResult result = new Pop3ExtensionResult();
            result.Response.Kind = Pop3ResponseKind.Positive;
            result.Response.Text = "extension closing";
            result.CloseSession = true;
            return result;
        }
    }

    sealed class ServerStream : Stream
    {
        byte[] Input;
        int InputOffset;
        MemoryStream Output = new MemoryStream();
        internal bool Disposed;

        internal ServerStream(string input)
        {
            Input = Encoding.ASCII.GetBytes(input);
        }

        internal string WrittenText()
        {
            return Encoding.UTF8.GetString(Output.ToArray());
        }

        public override bool CanRead => !Disposed;
        public override bool CanSeek => false;
        public override bool CanWrite => !Disposed;
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
            int available = Input.Length - InputOffset;
            int copied = Math.Min(count, available);
            if (copied > 0)
            {
                Array.Copy(Input, InputOffset, buffer, offset, copied);
                InputOffset += copied;
            }
            return copied;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Output.Write(buffer, offset, count);
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
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
