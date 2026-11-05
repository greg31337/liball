using System.Text;

namespace pop3.Tests;

public class Pop3SecretRedactorTests
{
    [Test]
    public void SafeCommandsAreDispatchedSynchronouslyInCallOrder()
    {
        RecordingLogger logger = new RecordingLogger();
        Pop3SecretRedactor redactor = new Pop3SecretRedactor(logger);
        Pop3CommandParser parser = new Pop3CommandParser();

        redactor.LogCommand(
            Pop3LogDirection.ClientToServer,
            parser.ParseCommand(Encoding.ASCII.GetBytes("user alice")),
            12,
            Pop3SessionState.Authorization);
        redactor.LogCommand(
            Pop3LogDirection.ClientToServer,
            parser.ParseCommand(Encoding.ASCII.GetBytes("STAT")),
            6,
            Pop3SessionState.Transaction);

        Assert.That(logger.Entries, Has.Count.EqualTo(2));
        AssertEntry(
            logger.Entries[0],
            Pop3LogDirection.ClientToServer,
            "USER alice",
            12,
            Pop3SessionState.Authorization,
            false);
        AssertEntry(
            logger.Entries[1],
            Pop3LogDirection.ClientToServer,
            "STAT",
            6,
            Pop3SessionState.Transaction,
            false);
    }

    [Test]
    public void AuthenticationCommandsExposeOnlyNonsecretMetadata()
    {
        RecordingLogger logger = new RecordingLogger();
        Pop3SecretRedactor redactor = new Pop3SecretRedactor(logger);
        Pop3CommandParser parser = new Pop3CommandParser();
        Pop3Command extension = new Pop3Command();
        extension.Keyword = "XID";
        extension.Arguments = new[] { "extension-secret" };
        extension.ContainsSensitiveData = true;

        redactor.LogCommand(Pop3LogDirection.ClientToServer, parser.ParseCommand(Encoding.ASCII.GetBytes("PASS swordfish")), 16, Pop3SessionState.Authorization);
        redactor.LogCommand(Pop3LogDirection.ClientToServer, parser.ParseCommand(Encoding.ASCII.GetBytes("APOP alice d41d8cd98f")), 22, Pop3SessionState.Authorization);
        redactor.LogCommand(Pop3LogDirection.ClientToServer, parser.ParseCommand(Encoding.ASCII.GetBytes("AUTH PLAIN AHVzZXIAc2VjcmV0")), 29, Pop3SessionState.Authorization);
        redactor.LogCommand(Pop3LogDirection.ClientToServer, parser.ParseCommand(Encoding.ASCII.GetBytes("AUTH SCRAM-SHA-256")), 20, Pop3SessionState.Authorization);
        redactor.LogCommand(Pop3LogDirection.ClientToServer, extension, 22, Pop3SessionState.Authorization);

        Assert.That(logger.Texts(), Is.EqualTo(new[]
        {
            "PASS [redacted]",
            "APOP [redacted]",
            "AUTH PLAIN [redacted]",
            "AUTH SCRAM-SHA-256",
            "XID [redacted]"
        }));
        Assert.That(logger.Entries.ConvertAll(entry => entry.IsRedacted), Is.EqualTo(new[] { true, true, true, false, true }));
        string combined = string.Join("|", logger.Texts());
        Assert.That(combined, Does.Not.Contain("swordfish"));
        Assert.That(combined, Does.Not.Contain("alice"));
        Assert.That(combined, Does.Not.Contain("d41d8cd98f"));
        Assert.That(combined, Does.Not.Contain("AHVzZXIAc2VjcmV0"));
        Assert.That(combined, Does.Not.Contain("extension-secret"));
    }

    [Test]
    public void SaslChallengesAndPayloadsAreAlwaysRedacted()
    {
        RecordingLogger logger = new RecordingLogger();
        Pop3SecretRedactor redactor = new Pop3SecretRedactor(logger);
        Pop3Response challenge = new Pop3Response();
        challenge.Kind = Pop3ResponseKind.SaslContinuation;
        challenge.Text = "c2VydmVyLWNoYWxsZW5nZQ==";

        redactor.LogResponse(
            Pop3LogDirection.ServerToClient,
            challenge,
            31,
            Pop3SessionState.Authorization);
        redactor.LogSaslPayload(
            Pop3LogDirection.ClientToServer,
            35,
            Pop3SessionState.Authorization);

        AssertEntry(
            logger.Entries[0],
            Pop3LogDirection.ServerToClient,
            "+ [redacted]",
            31,
            Pop3SessionState.Authorization,
            true);
        AssertEntry(
            logger.Entries[1],
            Pop3LogDirection.ClientToServer,
            "SASL [redacted]",
            35,
            Pop3SessionState.Authorization,
            true);
        Assert.That(string.Join("|", logger.Texts()), Does.Not.Contain(challenge.Text));
    }

    [Test]
    public void OrdinaryResponsesRetainSafeStatusCodesAndUnicodeText()
    {
        RecordingLogger logger = new RecordingLogger();
        Pop3SecretRedactor redactor = new Pop3SecretRedactor(logger);
        Pop3Response response = new Pop3Response();
        response.Kind = Pop3ResponseKind.Negative;
        response.Text = "réessayez";
        response.ResponseCodeLevels = new[] { "SYS", "TEMP" };

        redactor.LogResponse(
            Pop3LogDirection.ServerToClient,
            response,
            26,
            Pop3SessionState.Authorization);

        AssertEntry(
            logger.Entries[0],
            Pop3LogDirection.ServerToClient,
            "-ERR [SYS/TEMP] réessayez",
            26,
            Pop3SessionState.Authorization,
            false);
    }

    [Test]
    public void MessageTransfersAreRepresentedOnlyByRedactedMetadata()
    {
        RecordingLogger logger = new RecordingLogger();
        Pop3SecretRedactor redactor = new Pop3SecretRedactor(logger);

        redactor.LogMultilineData(Pop3LogDirection.ServerToClient, Pop3TransferKind.Retrieve, 7, 1048576, Pop3SessionState.Transaction);
        redactor.LogMultilineData(Pop3LogDirection.ServerToClient, Pop3TransferKind.Top, 8, 512, Pop3SessionState.Transaction);
        redactor.LogMultilineData(Pop3LogDirection.ServerToClient, Pop3TransferKind.List, 0, 40, Pop3SessionState.Transaction);
        redactor.LogMultilineData(Pop3LogDirection.ServerToClient, Pop3TransferKind.Uidl, 0, 80, Pop3SessionState.Transaction);
        redactor.LogMultilineData(Pop3LogDirection.ServerToClient, Pop3TransferKind.ExtensionData, 0, 10, Pop3SessionState.Transaction);

        Assert.That(logger.Texts(), Is.EqualTo(new[]
        {
            "RETR message body [redacted] for message 7",
            "TOP message data [redacted] for message 8",
            "LIST data",
            "UIDL data",
            "extension data"
        }));
        Assert.That(logger.Entries.ConvertAll(entry => entry.IsRedacted), Is.EqualTo(new[] { true, true, false, false, false }));
        Assert.That(logger.Entries.ConvertAll(entry => entry.ByteCount), Is.EqualTo(new long[] { 1048576, 512, 40, 80, 10 }));
    }

    [Test]
    public void LocalStateEntriesContainNoWireBytes()
    {
        RecordingLogger logger = new RecordingLogger();
        Pop3SecretRedactor redactor = new Pop3SecretRedactor(logger);

        redactor.LogState("maildrop acquired", Pop3SessionState.Transaction);

        AssertEntry(
            logger.Entries[0],
            Pop3LogDirection.LocalState,
            "maildrop acquired",
            0,
            Pop3SessionState.Transaction,
            false);
    }

    [Test]
    public void InvalidMetadataIsRejectedBeforeTheLoggerIsCalled()
    {
        RecordingLogger logger = new RecordingLogger();
        Pop3SecretRedactor redactor = new Pop3SecretRedactor(logger);
        Pop3Command command = new Pop3Command();
        command.Keyword = Pop3Commands.Noop;

        Assert.Throws<ArgumentOutOfRangeException>(() => redactor.LogCommand(Pop3LogDirection.LocalState, command, 6, Pop3SessionState.Authorization));
        Assert.Throws<ArgumentOutOfRangeException>(() => redactor.LogSaslPayload(Pop3LogDirection.ClientToServer, -1, Pop3SessionState.Authorization));
        Assert.Throws<ArgumentOutOfRangeException>(() => redactor.LogMultilineData(Pop3LogDirection.ServerToClient, (Pop3TransferKind)99, 0, 0, Pop3SessionState.Transaction));
        Assert.Throws<ArgumentOutOfRangeException>(() => redactor.LogMultilineData(Pop3LogDirection.ServerToClient, Pop3TransferKind.Retrieve, -1, 0, Pop3SessionState.Transaction));
        Assert.Throws<ArgumentOutOfRangeException>(() => redactor.LogState("state", (Pop3SessionState)99));
        Assert.That(logger.Entries, Is.Empty);
    }

    [Test]
    public void DisabledLoggingDoesNotInspectSecretBearingObjects()
    {
        Pop3SecretRedactor redactor = new Pop3SecretRedactor(null);
        Pop3Command command = new Pop3Command();
        Pop3Response response = new Pop3Response();
        command.Keyword = null!;
        command.Arguments = null!;
        response.Kind = Pop3ResponseKind.SaslContinuation;
        response.Text = null!;

        Assert.DoesNotThrow(() => redactor.LogCommand(Pop3LogDirection.ClientToServer, command, 1, Pop3SessionState.Authorization));
        Assert.DoesNotThrow(() => redactor.LogResponse(Pop3LogDirection.ServerToClient, response, 1, Pop3SessionState.Authorization));
    }

    [Test]
    public void EnabledLoggingRejectsNullAndMalformedObjectsBeforeDispatch()
    {
        RecordingLogger logger = new RecordingLogger();
        Pop3SecretRedactor redactor = new Pop3SecretRedactor(logger);
        Pop3Command missingKeyword = new Pop3Command();
        missingKeyword.Keyword = null!;
        Pop3Command missingArguments = new Pop3Command();
        missingArguments.Keyword = Pop3Commands.Noop;
        missingArguments.Arguments = null!;
        Pop3Command nullArgument = new Pop3Command();
        nullArgument.Keyword = "X-TEST";
        nullArgument.Arguments = new string[] { null! };

        Assert.Throws<ArgumentNullException>(() => redactor.LogCommand(
            Pop3LogDirection.ClientToServer,
            null!,
            0,
            Pop3SessionState.Authorization));
        Assert.Throws<ArgumentNullException>(() => redactor.LogResponse(
            Pop3LogDirection.ServerToClient,
            null!,
            0,
            Pop3SessionState.Authorization));
        Assert.Throws<ArgumentNullException>(() => redactor.LogState(
            null!, Pop3SessionState.Authorization));
        Assert.Throws<InvalidDataException>(() => redactor.LogCommand(
            Pop3LogDirection.ClientToServer,
            missingKeyword,
            0,
            Pop3SessionState.Authorization));
        Assert.Throws<InvalidDataException>(() => redactor.LogCommand(
            Pop3LogDirection.ClientToServer,
            missingArguments,
            0,
            Pop3SessionState.Authorization));
        Assert.Throws<InvalidDataException>(() => redactor.LogCommand(
            Pop3LogDirection.ClientToServer,
            nullArgument,
            0,
            Pop3SessionState.Authorization));
        Assert.That(logger.Entries, Is.Empty);
    }

    [Test]
    public void AuthWithoutArgumentsLogsOnlyTheCommandName()
    {
        RecordingLogger logger = new RecordingLogger();
        Pop3SecretRedactor redactor = new Pop3SecretRedactor(logger);
        Pop3Command command = new Pop3Command();
        command.Keyword = Pop3Commands.Auth;

        redactor.LogCommand(
            Pop3LogDirection.ClientToServer,
            command,
            6,
            Pop3SessionState.Authorization);

        Assert.That(logger.Texts(), Is.EqualTo(new[] { Pop3Commands.Auth }));
        Assert.That(logger.Entries[0].IsRedacted, Is.False);
    }

    [Test]
    public void DisabledStateLoggingReturnsWithoutDispatch()
    {
        Pop3SecretRedactor redactor = new Pop3SecretRedactor(null);

        Assert.DoesNotThrow(() => redactor.LogState(
            "closed", Pop3SessionState.Closed));
    }

    private static void AssertEntry(
        Pop3LogEntry entry,
        Pop3LogDirection direction,
        string text,
        long byteCount,
        Pop3SessionState state,
        bool isRedacted)
    {
        Assert.That(entry.Direction, Is.EqualTo(direction));
        Assert.That(entry.Text, Is.EqualTo(text));
        Assert.That(entry.ByteCount, Is.EqualTo(byteCount));
        Assert.That(entry.SessionState, Is.EqualTo(state));
        Assert.That(entry.IsRedacted, Is.EqualTo(isRedacted));
    }

    private class RecordingLogger : IPop3ProtocolLogger
    {
        public List<Pop3LogEntry> Entries = new List<Pop3LogEntry>();

        public void Log(Pop3LogEntry entry)
        {
            Entries.Add(entry);
        }

        public List<string> Texts()
        {
            return Entries.ConvertAll(entry => entry.Text);
        }
    }
}
