namespace pop3.Tests;

public class Pop3ProviderContractTests
{
    [Test]
    public void AuthenticationProviderReturnsClassifiedResultsForEveryPath()
    {
        AuthenticationProbe probe = new AuthenticationProbe();
        IPop3AuthenticationProvider provider = probe;
        Pop3ServerConnection connection = new Pop3ServerConnection();
        Pop3Credentials credentials = new Pop3Credentials();
        Pop3ApopRequest apop = new Pop3ApopRequest();
        CancellationTokenSource source = new CancellationTokenSource();

        Assert.That(provider.CheckLoginDelay("alice", connection, source.Token), Is.SameAs(probe.Result));
        Assert.That(probe.LastOperation, Is.EqualTo("delay"));
        Assert.That(provider.AuthenticateUserPassword(credentials, connection, source.Token), Is.SameAs(probe.Result));
        Assert.That(probe.LastOperation, Is.EqualTo("user-password"));
        Assert.That(provider.AuthenticateApop(apop, connection, source.Token), Is.SameAs(probe.Result));
        Assert.That(probe.LastOperation, Is.EqualTo("apop"));
        Assert.That(provider.AuthenticateSaslPlain(credentials, connection, source.Token), Is.SameAs(probe.Result));
        Assert.That(probe.LastOperation, Is.EqualTo("sasl-plain"));
        Assert.That(probe.Connection, Is.SameAs(connection));
        Assert.That(probe.LastCancellationToken, Is.EqualTo(source.Token));
    }

    [Test]
    public void MailboxContractsSupportTransactionLifecycleAndCallerOwnedOutput()
    {
        MailboxSessionProbe session = new MailboxSessionProbe();
        MailboxProviderProbe providerProbe = new MailboxProviderProbe(session);
        IPop3MailboxProvider provider = providerProbe;
        Pop3MailboxOpenRequest request = new Pop3MailboxOpenRequest();
        CancellationTokenSource source = new CancellationTokenSource();
        Pop3MailboxOpenResult openResult = provider.OpenMailbox(request, source.Token);
        IPop3MailboxSession mailbox = openResult.Session!;
        MemoryStream destination = new MemoryStream();

        Assert.That(openResult.Session, Is.SameAs(session));
        Assert.That(providerProbe.Request, Is.SameAs(request));
        Assert.That(providerProbe.CancellationToken, Is.EqualTo(source.Token));
        Assert.That(mailbox.GetStatus(), Is.SameAs(session.Status));
        Assert.That(mailbox.GetMessages(), Is.SameAs(session.Messages));
        Assert.That(mailbox.GetUniqueIds(), Is.SameAs(session.UniqueIds));
        mailbox.WriteMessage(7, true, destination, source.Token);
        mailbox.WriteTop(7, 3, true, destination, source.Token);
        mailbox.MarkDeleted(7);
        Assert.That(mailbox.ResetDeletions(), Is.SameAs(session.Status));
        mailbox.CommitUpdate(source.Token);
        mailbox.RollbackChanges();
        mailbox.ReleaseSession();

        Assert.That(destination.ToArray(), Is.EqualTo(new byte[] { 1, 2 }));
        Assert.That(session.MessageNumber, Is.EqualTo(7));
        Assert.That(session.BodyLineCount, Is.EqualTo(3));
        Assert.That(session.UseUtf8Representation, Is.True);
        Assert.That(session.WasMarkedDeleted, Is.True);
        Assert.That(session.WasCommitted, Is.True);
        Assert.That(session.WasRolledBack, Is.True);
        Assert.That(session.WasReleased, Is.True);
    }

    [Test]
    public void SaslServerContractsSupportFactoryExchangeAndSecurityLayer()
    {
        SaslServerSessionProbe session = new SaslServerSessionProbe();
        SaslServerFactoryProbe factoryProbe = new SaslServerFactoryProbe(session);
        IPop3SaslServerFactory factory = factoryProbe;
        Pop3ServerConnection connection = new Pop3ServerConnection();
        IPop3SaslServerSession created = factory.CreateSession(connection);
        MemoryStream stream = new MemoryStream();

        Assert.That(factory.GetMechanism(), Is.EqualTo("TEST"));
        Assert.That(created, Is.SameAs(session));
        Assert.That(factoryProbe.Connection, Is.SameAs(connection));
        Assert.That(created.BeginAuthentication(new byte[] { 1 }, true).Data, Is.EqualTo(new byte[] { 1 }));
        Assert.That(created.ContinueAuthentication(new byte[] { 2 }).Data, Is.EqualTo(new byte[] { 2 }));
        Assert.That(created.GetAuthenticationResult(), Is.SameAs(session.Result));
        Assert.That(created.ApplySecurityLayer(stream), Is.SameAs(stream));
        created.CancelAuthentication();
        Assert.That(session.WasCancelled, Is.True);
    }

    [Test]
    public void LanguageAndExtensionContractsCarryTypedContext()
    {
        LanguageProbe languageProbe = new LanguageProbe();
        IPop3LanguageProvider languageProvider = languageProbe;
        ExtensionProbe extensionProbe = new ExtensionProbe();
        IPop3ServerExtension extension = extensionProbe;
        Pop3ExtensionContext context = new Pop3ExtensionContext();
        Pop3Command command = new Pop3Command();
        CancellationTokenSource source = new CancellationTokenSource();

        Assert.That(languageProvider.GetLanguages(), Is.SameAs(languageProbe.Languages));
        Assert.That(languageProvider.SupportsLanguage("en"), Is.True);
        Assert.That(languageProvider.SupportsLanguage("fr"), Is.False);
        Assert.That(languageProvider.GetResponseText(new Pop3LanguageTextRequest()), Is.EqualTo("localized"));
        Assert.That(extension.GetCapabilities(context), Is.SameAs(extensionProbe.Capabilities));
        Assert.That(extension.GetCommandNames(), Is.EqualTo(new[] { "XTEST" }));
        Assert.That(extension.GetAllowedStates("XTEST"), Is.EqualTo(new[] { Pop3SessionState.Transaction }));
        Assert.That(extension.ExecuteCommand(context, command, source.Token), Is.SameAs(extensionProbe.Result));
        Assert.That(extensionProbe.Context, Is.SameAs(context));
        Assert.That(extensionProbe.Command, Is.SameAs(command));
        Assert.That(extensionProbe.CancellationToken, Is.EqualTo(source.Token));
    }

    [Test]
    public void ServerContractReceivesConnectionOptionsAndCancellation()
    {
        ServerProbe probe = new ServerProbe();
        IPop3Server server = probe;
        Pop3ServerConnection connection = new Pop3ServerConnection();
        Pop3ServerOptions options = new Pop3ServerOptions();
        CancellationTokenSource source = new CancellationTokenSource();

        server.ServeConnection(connection, options, source.Token);

        Assert.That(probe.Connection, Is.SameAs(connection));
        Assert.That(probe.Options, Is.SameAs(options));
        Assert.That(probe.CancellationToken, Is.EqualTo(source.Token));
    }

    private class AuthenticationProbe : IPop3AuthenticationProvider
    {
        public Pop3AuthenticationResult Result = new Pop3AuthenticationResult();
        public string LastOperation = "";
        public Pop3ServerConnection? Connection;
        public CancellationToken LastCancellationToken;

        public Pop3AuthenticationResult CheckLoginDelay(string username, Pop3ServerConnection connection, CancellationToken cancellationToken)
        {
            LastOperation = "delay";
            Connection = connection;
            LastCancellationToken = cancellationToken;
            return Result;
        }

        public Pop3AuthenticationResult AuthenticateUserPassword(Pop3Credentials credentials, Pop3ServerConnection connection, CancellationToken cancellationToken)
        {
            LastOperation = "user-password";
            Connection = connection;
            LastCancellationToken = cancellationToken;
            return Result;
        }

        public Pop3AuthenticationResult AuthenticateApop(Pop3ApopRequest request, Pop3ServerConnection connection, CancellationToken cancellationToken)
        {
            LastOperation = "apop";
            Connection = connection;
            LastCancellationToken = cancellationToken;
            return Result;
        }

        public Pop3AuthenticationResult AuthenticateSaslPlain(Pop3Credentials credentials, Pop3ServerConnection connection, CancellationToken cancellationToken)
        {
            LastOperation = "sasl-plain";
            Connection = connection;
            LastCancellationToken = cancellationToken;
            return Result;
        }
    }

    private class MailboxProviderProbe : IPop3MailboxProvider
    {
        public Pop3MailboxOpenRequest? Request;
        public CancellationToken CancellationToken;
        private IPop3MailboxSession Session;

        public MailboxProviderProbe(IPop3MailboxSession session)
        {
            Session = session;
        }

        public Pop3MailboxOpenResult OpenMailbox(Pop3MailboxOpenRequest request, CancellationToken cancellationToken)
        {
            Pop3MailboxOpenResult result = new Pop3MailboxOpenResult();
            Request = request;
            CancellationToken = cancellationToken;
            result.Session = Session;
            return result;
        }
    }

    private class MailboxSessionProbe : IPop3MailboxSession
    {
        public Pop3MailboxStatus Status = new Pop3MailboxStatus();
        public Pop3MessageInfo[] Messages = new Pop3MessageInfo[0];
        public Pop3UniqueId[] UniqueIds = new Pop3UniqueId[0];
        public int MessageNumber;
        public int BodyLineCount;
        public bool UseUtf8Representation;
        public bool WasMarkedDeleted;
        public bool WasCommitted;
        public bool WasRolledBack;
        public bool WasReleased;

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

        public void WriteMessage(int messageNumber, bool useUtf8Representation, Stream destination, CancellationToken cancellationToken)
        {
            MessageNumber = messageNumber;
            UseUtf8Representation = useUtf8Representation;
            destination.WriteByte(1);
        }

        public void WriteTop(int messageNumber, int bodyLineCount, bool useUtf8Representation, Stream destination, CancellationToken cancellationToken)
        {
            MessageNumber = messageNumber;
            BodyLineCount = bodyLineCount;
            UseUtf8Representation = useUtf8Representation;
            destination.WriteByte(2);
        }

        public void MarkDeleted(int messageNumber)
        {
            MessageNumber = messageNumber;
            WasMarkedDeleted = true;
        }

        public Pop3MailboxStatus ResetDeletions()
        {
            return Status;
        }

        public void CommitUpdate(CancellationToken cancellationToken)
        {
            WasCommitted = true;
        }

        public void RollbackChanges()
        {
            WasRolledBack = true;
        }

        public void ReleaseSession()
        {
            WasReleased = true;
        }
    }

    private class SaslServerFactoryProbe : IPop3SaslServerFactory
    {
        public Pop3ServerConnection? Connection;
        private IPop3SaslServerSession Session;

        public SaslServerFactoryProbe(IPop3SaslServerSession session)
        {
            Session = session;
        }

        public string GetMechanism()
        {
            return "TEST";
        }

        public IPop3SaslServerSession CreateSession(Pop3ServerConnection connection)
        {
            Connection = connection;
            return Session;
        }
    }

    private class SaslServerSessionProbe : IPop3SaslServerSession
    {
        public Pop3AuthenticationResult Result = new Pop3AuthenticationResult();
        public bool WasCancelled;

        public Pop3SaslStep BeginAuthentication(byte[] initialResponse, bool hasInitialResponse)
        {
            Pop3SaslStep step = new Pop3SaslStep();
            step.Data = initialResponse;
            return step;
        }

        public Pop3SaslStep ContinueAuthentication(byte[] response)
        {
            Pop3SaslStep step = new Pop3SaslStep();
            step.Data = response;
            return step;
        }

        public void CancelAuthentication()
        {
            WasCancelled = true;
        }

        public Pop3AuthenticationResult GetAuthenticationResult()
        {
            return Result;
        }

        public Stream ApplySecurityLayer(Stream stream)
        {
            return stream;
        }
    }

    private class LanguageProbe : IPop3LanguageProvider
    {
        public Pop3Language[] Languages = new[] { new Pop3Language() };

        public Pop3Language[] GetLanguages()
        {
            return Languages;
        }

        public bool SupportsLanguage(string languageTag)
        {
            return languageTag == "en";
        }

        public string GetResponseText(Pop3LanguageTextRequest request)
        {
            return "localized";
        }
    }

    private class ExtensionProbe : IPop3ServerExtension
    {
        public Pop3Capability[] Capabilities = new[] { new Pop3Capability() };
        public Pop3ExtensionResult Result = new Pop3ExtensionResult();
        public Pop3ExtensionContext? Context;
        public Pop3Command? Command;
        public CancellationToken CancellationToken;

        public Pop3Capability[] GetCapabilities(Pop3ExtensionContext context)
        {
            return Capabilities;
        }

        public string[] GetCommandNames()
        {
            return new[] { "XTEST" };
        }

        public Pop3SessionState[] GetAllowedStates(string commandName)
        {
            return new[] { Pop3SessionState.Transaction };
        }

        public Pop3ExtensionResult ExecuteCommand(Pop3ExtensionContext context, Pop3Command command, CancellationToken cancellationToken)
        {
            Context = context;
            Command = command;
            CancellationToken = cancellationToken;
            return Result;
        }
    }

    private class ServerProbe : IPop3Server
    {
        public Pop3ServerConnection? Connection;
        public Pop3ServerOptions? Options;
        public CancellationToken CancellationToken;

        public void ServeConnection(Pop3ServerConnection connection, Pop3ServerOptions options, CancellationToken cancellationToken)
        {
            Connection = connection;
            Options = options;
            CancellationToken = cancellationToken;
        }
    }
}
