namespace pop3.Tests;

public class Pop3ProviderDataTests
{
    [Test]
    public void MailboxOpenDataDefaultsAndFieldsRoundTrip()
    {
        Pop3MailboxOpenRequest request = new Pop3MailboxOpenRequest();
        Pop3MailboxOpenResult result = new Pop3MailboxOpenResult();
        Pop3ServerConnection connection = new Pop3ServerConnection();

        Assert.That(request.AuthenticatedIdentity, Is.Empty);
        Assert.That(request.MaildropKey, Is.Empty);
        Assert.That(request.Connection, Is.Null);
        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.Success));
        Assert.That(result.Session, Is.Null);
        Assert.That(result.RetryAfterSeconds, Is.EqualTo(-1));
        Assert.That(result.FailureText, Is.Null);

        request.AuthenticatedIdentity = "alice";
        request.MaildropKey = "maildrop-1";
        request.Connection = connection;
        FakeMailboxSession session = new FakeMailboxSession();
        result.Status = Pop3AuthenticationStatus.InUse;
        result.Session = session;
        result.RetryAfterSeconds = 120;
        result.FailureText = "maildrop locked";

        Assert.That(request.AuthenticatedIdentity, Is.EqualTo("alice"));
        Assert.That(request.MaildropKey, Is.EqualTo("maildrop-1"));
        Assert.That(request.Connection, Is.SameAs(connection));
        Assert.That(result.Status, Is.EqualTo(Pop3AuthenticationStatus.InUse));
        Assert.That(result.Session, Is.SameAs(session));
        Assert.That(result.RetryAfterSeconds, Is.EqualTo(120));
        Assert.That(result.FailureText, Is.EqualTo("maildrop locked"));
    }

    [Test]
    public void LanguageDataDefaultsAndFieldsRoundTrip()
    {
        Pop3Language language = new Pop3Language();
        Pop3LanguageTextRequest request = new Pop3LanguageTextRequest();

        Assert.That(language.Tag, Is.Empty);
        Assert.That(language.Description, Is.Empty);
        Assert.That(request.SelectedLanguage, Is.Null);
        Assert.That(request.ResponseTextKey, Is.Empty);

        language.Tag = "en";
        language.Description = "English";
        request.SelectedLanguage = "en";
        request.ResponseTextKey = "authentication-failed";

        Assert.That(language.Tag, Is.EqualTo("en"));
        Assert.That(language.Description, Is.EqualTo("English"));
        Assert.That(request.SelectedLanguage, Is.EqualTo("en"));
        Assert.That(request.ResponseTextKey, Is.EqualTo("authentication-failed"));
    }

    [Test]
    public void LoggingAndTransferDataSupportLargeCounters()
    {
        Pop3LogEntry log = new Pop3LogEntry();
        Pop3TransferProgress progress = new Pop3TransferProgress();
        long largeCount = 12345678901;

        Assert.That(log.Text, Is.Empty);
        Assert.That(log.Direction, Is.EqualTo(Pop3LogDirection.ClientToServer));
        Assert.That(log.ByteCount, Is.Zero);
        Assert.That(log.SessionState, Is.EqualTo(Pop3SessionState.Disconnected));
        Assert.That(log.IsRedacted, Is.False);
        Assert.That(progress.MessageNumber, Is.Zero);
        Assert.That(progress.Kind, Is.EqualTo(Pop3TransferKind.List));
        Assert.That(progress.BytesTransferred, Is.Zero);
        Assert.That(progress.ExpectedBytes, Is.EqualTo(-1));
        Assert.That(progress.IsComplete, Is.False);

        log.Direction = Pop3LogDirection.LocalState;
        log.Text = "PASS [redacted]";
        log.ByteCount = largeCount;
        log.SessionState = Pop3SessionState.Authorization;
        log.IsRedacted = true;
        progress.Kind = Pop3TransferKind.Retrieve;
        progress.MessageNumber = 7;
        progress.BytesTransferred = largeCount;
        progress.ExpectedBytes = largeCount + 1;
        progress.IsComplete = true;

        Assert.That(log.Direction, Is.EqualTo(Pop3LogDirection.LocalState));
        Assert.That(log.Text, Is.EqualTo("PASS [redacted]"));
        Assert.That(log.ByteCount, Is.EqualTo(largeCount));
        Assert.That(log.SessionState, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(log.IsRedacted, Is.True);
        Assert.That(progress.Kind, Is.EqualTo(Pop3TransferKind.Retrieve));
        Assert.That(progress.MessageNumber, Is.EqualTo(7));
        Assert.That(progress.BytesTransferred, Is.EqualTo(largeCount));
        Assert.That(progress.ExpectedBytes, Is.EqualTo(largeCount + 1));
        Assert.That(progress.IsComplete, Is.True);
    }

    [Test]
    public void ExtensionAndConnectionDataDefaultsAreIndependent()
    {
        Pop3ExtensionContext firstContext = new Pop3ExtensionContext();
        Pop3ExtensionContext secondContext = new Pop3ExtensionContext();
        Pop3ExtensionResult firstResult = new Pop3ExtensionResult();
        Pop3ExtensionResult secondResult = new Pop3ExtensionResult();
        Pop3ServerConnection connection = new Pop3ServerConnection();

        Assert.That(firstContext.State, Is.EqualTo(Pop3SessionState.Disconnected));
        Assert.That(firstContext.Capabilities.Entries, Is.Empty);
        Assert.That(firstContext.Capabilities, Is.Not.SameAs(secondContext.Capabilities));
        Assert.That(firstContext.IsSecure, Is.False);
        Assert.That(firstContext.IsAuthenticated, Is.False);
        Assert.That(firstContext.IsUtf8Enabled, Is.False);
        Assert.That(firstContext.SelectedLanguage, Is.Null);
        Assert.That(firstResult.Response, Is.Not.SameAs(secondResult.Response));
        Assert.That(firstResult.MultilineData, Is.Null);
        Assert.That(firstResult.CloseSession, Is.False);
        Assert.That(connection.Stream, Is.Null);
        Assert.That(connection.IsSecure, Is.False);
        Assert.That(connection.LocalEndPoint, Is.Null);
        Assert.That(connection.RemoteEndPoint, Is.Null);
        Assert.That(connection.LeaveOpen, Is.False);
    }

    [Test]
    public void ExtensionAndConnectionFieldsRoundTrip()
    {
        Pop3ExtensionContext context = new Pop3ExtensionContext();
        Pop3ExtensionResult result = new Pop3ExtensionResult();
        Pop3ServerConnection connection = new Pop3ServerConnection();
        MemoryStream stream = new MemoryStream();
        Pop3CapabilitySet capabilities = new Pop3CapabilitySet();
        Pop3Response response = new Pop3Response();

        context.State = Pop3SessionState.Transaction;
        context.Capabilities = capabilities;
        context.IsSecure = true;
        context.IsAuthenticated = true;
        context.IsUtf8Enabled = true;
        context.SelectedLanguage = "en";
        result.Response = response;
        result.MultilineData = stream;
        result.CloseSession = true;
        connection.Stream = stream;
        connection.IsSecure = true;
        connection.LocalEndPoint = "127.0.0.1:110";
        connection.RemoteEndPoint = "127.0.0.1:40000";
        connection.LeaveOpen = true;

        Assert.That(context.State, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(context.Capabilities, Is.SameAs(capabilities));
        Assert.That(context.IsSecure, Is.True);
        Assert.That(context.IsAuthenticated, Is.True);
        Assert.That(context.IsUtf8Enabled, Is.True);
        Assert.That(context.SelectedLanguage, Is.EqualTo("en"));
        Assert.That(result.Response, Is.SameAs(response));
        Assert.That(result.MultilineData, Is.SameAs(stream));
        Assert.That(result.CloseSession, Is.True);
        Assert.That(connection.Stream, Is.SameAs(stream));
        Assert.That(connection.IsSecure, Is.True);
        Assert.That(connection.LocalEndPoint, Is.EqualTo("127.0.0.1:110"));
        Assert.That(connection.RemoteEndPoint, Is.EqualTo("127.0.0.1:40000"));
        Assert.That(connection.LeaveOpen, Is.True);
    }
}
