using System.Text;

namespace pop3.Tests;

public class Pop3ClientUtf8Tests
{
    [TestCase("UTF8", false)]
    [TestCase("UTF8 USER", true)]
    public void SuccessfulEnableTracksMessageAndCredentialModesIndependently(
        string capability,
        bool expectedUserMode)
    {
        Utf8SequenceStream stream = CreateAuthorizationStream(
            capability,
            "+OK UTF8 enabled\r\n");
        Pop3ClientEngine engine = Connect(stream);

        engine.EnableUtf8(CancellationToken.None);

        Pop3ClientSnapshot snapshot = engine.GetSnapshot();
        Assert.That(snapshot.IsUtf8Enabled, Is.True);
        Assert.That(snapshot.IsUtf8UserEnabled, Is.EqualTo(expectedUserMode));
        Assert.That(snapshot.Capabilities.SupportsUtf8, Is.True);
        Assert.That(snapshot.Capabilities.SupportsUtf8User, Is.EqualTo(expectedUserMode));
        Assert.That(snapshot.LastResponse!.Text, Is.EqualTo("UTF8 enabled"));
        Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nUTF8\r\n"));

        engine.Abort();
        stream.Dispose();
    }

    [Test]
    public void AdvertisedUtf8UserDoesNotEnableCredentialsUntilCommandSucceeds()
    {
        Utf8SequenceStream stream = CreateAuthorizationStream(
            "USER\r\nUTF8 USER",
            "-ERR temporarily unavailable\r\n" +
            "+OK UTF8 enabled\r\n");
        Pop3ClientEngine engine = Connect(stream);

        Assert.That(engine.GetSnapshot().Capabilities.SupportsUtf8User, Is.True);
        Assert.That(engine.GetSnapshot().IsUtf8UserEnabled, Is.False);
        Pop3CommandException exception = Assert.Throws<Pop3CommandException>(() =>
            engine.EnableUtf8(CancellationToken.None))!;
        Assert.That(engine.GetSnapshot().IsUtf8Enabled, Is.False);
        Assert.That(engine.GetSnapshot().IsUtf8UserEnabled, Is.False);

        engine.EnableUtf8(CancellationToken.None);

        Assert.That(exception.Command.Keyword, Is.EqualTo(Pop3Commands.Utf8));
        Assert.That(exception.Response.Text, Is.EqualTo("temporarily unavailable"));
        Assert.That(engine.GetSnapshot().IsUtf8Enabled, Is.True);
        Assert.That(engine.GetSnapshot().IsUtf8UserEnabled, Is.True);
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nUTF8\r\nUTF8\r\n"));

        engine.Abort();
        stream.Dispose();
    }

    [Test]
    public void MissingCapabilityAndPreCancellationPreventUtf8Writes()
    {
        Utf8SequenceStream missingStream = CreateAuthorizationStream("USER", "");
        Pop3ClientEngine missingEngine = Connect(missingStream);

        Assert.Throws<InvalidOperationException>(() =>
            missingEngine.EnableUtf8(CancellationToken.None));
        Assert.That(missingStream.WrittenText(), Is.EqualTo("CAPA\r\n"));
        Assert.That(missingEngine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Authorization));

        Utf8SequenceStream cancelledStream = CreateAuthorizationStream(
            "UTF8 USER",
            "+OK unused\r\n");
        Pop3ClientEngine cancelledEngine = Connect(cancelledStream);
        using (CancellationTokenSource source = new CancellationTokenSource())
        {
            source.Cancel();

            Assert.Throws<OperationCanceledException>(() =>
                cancelledEngine.EnableUtf8(source.Token));
        }
        Assert.That(cancelledStream.WrittenText(), Is.EqualTo("CAPA\r\n"));
        Assert.That(cancelledEngine.GetSnapshot().IsUtf8Enabled, Is.False);

        missingEngine.Abort();
        cancelledEngine.Abort();
        missingStream.Dispose();
        cancelledStream.Dispose();
    }

    [Test]
    public void UnexpectedUtf8ContinuationInvalidatesTheSession()
    {
        Utf8SequenceStream stream = CreateAuthorizationStream(
            "UTF8 USER",
            "+ unexpected\r\n");
        Pop3ClientEngine engine = Connect(stream);

        Pop3ProtocolException exception = Assert.Throws<Pop3ProtocolException>(() =>
            engine.EnableUtf8(CancellationToken.None))!;

        Assert.That(exception.SessionState, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(exception.Context, Is.EqualTo("UTF8 response"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(engine.GetSnapshot().IsUtf8Enabled, Is.False);
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nUTF8\r\n"));
        stream.Dispose();
    }

    [Test]
    public void Utf8IsAuthorizationOnlyAndBlocksLaterStls()
    {
        Utf8SequenceStream transactionStream = new Utf8SequenceStream(
            "+OK ready\r\n" +
            CapabilityResponse("UTF8") +
            CapabilityResponse("UIDL"));
        Pop3ClientEngine transactionEngine = Connect(transactionStream);
        transactionEngine.CompleteAuthentication("alice", CancellationToken.None);

        Pop3StateException transactionFailure = Assert.Throws<Pop3StateException>(() =>
            transactionEngine.EnableUtf8(CancellationToken.None))!;
        Assert.That(transactionFailure.Operation, Is.EqualTo(Pop3Commands.Utf8));
        Assert.That(transactionFailure.SessionState, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(transactionStream.WrittenText(), Is.EqualTo("CAPA\r\nCAPA\r\n"));

        Utf8SequenceStream stlsStream = CreateAuthorizationStream(
            "STLS\r\nUTF8 USER",
            "+OK UTF8 enabled\r\n");
        Pop3ClientEngine stlsEngine = Connect(stlsStream);
        stlsEngine.EnableUtf8(CancellationToken.None);

        Pop3StateException stlsFailure = Assert.Throws<Pop3StateException>(() =>
            stlsEngine.UpgradeToTls(CancellationToken.None))!;
        Assert.That(stlsFailure.Operation, Is.EqualTo(Pop3Commands.Stls));
        Assert.That(stlsFailure.SessionState, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(stlsStream.WrittenText(), Is.EqualTo("CAPA\r\nUTF8\r\n"));
        Assert.That(stlsEngine.GetSnapshot().IsUtf8Enabled, Is.True);

        transactionEngine.Abort();
        stlsEngine.Abort();
        transactionStream.Dispose();
        stlsStream.Dispose();
    }

    [Test]
    public void NegotiatedUtf8ModesSurviveAuthenticationCapabilityRefresh()
    {
        Utf8SequenceStream stream = new Utf8SequenceStream(
            "+OK ready\r\n" +
            CapabilityResponse("UTF8 USER") +
            "+OK UTF8 enabled\r\n" +
            CapabilityResponse("UIDL") +
            "+OK 1 42\r\n");
        Pop3ClientEngine engine = Connect(stream);

        engine.EnableUtf8(CancellationToken.None);
        engine.CompleteAuthentication("álîçé", CancellationToken.None);
        Pop3MessageInfo message = engine.GetMessageInfo(1, CancellationToken.None);

        Pop3ClientSnapshot snapshot = engine.GetSnapshot();
        Assert.That(snapshot.Capabilities.SupportsUtf8, Is.False);
        Assert.That(snapshot.Capabilities.SupportsUtf8User, Is.False);
        Assert.That(snapshot.IsUtf8Enabled, Is.True);
        Assert.That(snapshot.IsUtf8UserEnabled, Is.True);
        Assert.That(message.IsUtf8Representation, Is.True);
        Assert.That(snapshot.Messages[0].IsUtf8Representation, Is.True);
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nUTF8\r\nCAPA\r\nLIST 1\r\n"));

        engine.Abort();
        stream.Dispose();
    }

    [Test]
    public void Utf8ResponseCodeProvidesReconnectAndRetryGuidance()
    {
        Utf8SequenceStream stream = new Utf8SequenceStream(
            "+OK ready\r\n" +
            CapabilityResponse("USER") +
            CapabilityResponse("TOP") +
            "-ERR [utf8] international message\r\n" +
            "-ERR [UTF8/vendor] international message\r\n" +
            "-ERR [UtF8] international message\r\n");
        Pop3ClientEngine engine = Connect(stream);
        engine.CompleteAuthentication("alice", CancellationToken.None);

        Pop3CommandException listFailure = Assert.Throws<Pop3CommandException>(() =>
            engine.GetMessageInfo(1, CancellationToken.None))!;
        Pop3CommandException retrieveFailure = Assert.Throws<Pop3CommandException>(() =>
            engine.RetrieveMessageToMemory(1, CancellationToken.None))!;
        Pop3CommandException topFailure = Assert.Throws<Pop3CommandException>(() =>
            engine.RetrieveTopToMemory(1, 0, CancellationToken.None))!;

        AssertUtf8Guidance(listFailure, Pop3Commands.List, new[] { "utf8" });
        AssertUtf8Guidance(
            retrieveFailure,
            Pop3Commands.Retr,
            new[] { "UTF8", "vendor" });
        AssertUtf8Guidance(topFailure, Pop3Commands.Top, new[] { "UtF8" });
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nCAPA\r\nLIST 1\r\nRETR 1\r\nTOP 1 0\r\n"));

        engine.Abort();
        stream.Dispose();
    }

    [Test]
    public void Utf8ResponseCodeOnMultilineListProvidesReconnectAndRetryGuidance()
    {
        Utf8SequenceStream stream = new Utf8SequenceStream(
            "+OK ready\r\n" +
            CapabilityResponse("USER") +
            CapabilityResponse("UIDL") +
            "-ERR [UTF8] international maildrop\r\n");
        Pop3ClientEngine engine = Connect(stream);
        engine.CompleteAuthentication("alice", CancellationToken.None);

        Pop3CommandException exception = Assert.Throws<Pop3CommandException>(() =>
            engine.GetAllMessageInfo(CancellationToken.None))!;

        AssertUtf8Guidance(exception, Pop3Commands.List, new[] { "UTF8" });
        Assert.That(exception.Command.Arguments, Is.Empty);
        Assert.That(exception.Command.ExpectsMultilineResponse, Is.True);
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nCAPA\r\nLIST\r\n"));

        engine.Abort();
        stream.Dispose();
    }

    static void AssertUtf8Guidance(
        Pop3CommandException exception,
        string command,
        string[] responseCodeLevels)
    {
        Assert.That(exception.Command.Keyword, Is.EqualTo(command));
        Assert.That(exception.Response.ResponseCodeLevels, Is.EqualTo(responseCodeLevels));
        Assert.That(exception.Message, Does.Contain("Reconnect"));
        Assert.That(exception.Message, Does.Contain("EnableUtf8"));
        Assert.That(exception.Message, Does.Contain("retry"));
    }

    static Pop3ClientEngine Connect(Utf8SequenceStream stream)
    {
        Pop3ClientEngine engine = new Pop3ClientEngine();
        engine.ConnectStream(stream, CreateOptions(), CancellationToken.None);
        return engine;
    }

    static Utf8SequenceStream CreateAuthorizationStream(
        string capabilities,
        string responses)
    {
        return new Utf8SequenceStream(
            "+OK ready\r\n" +
            CapabilityResponse(capabilities) +
            responses);
    }

    static string CapabilityResponse(string capabilities)
    {
        return "+OK capabilities\r\n" + capabilities + "\r\n.\r\n";
    }

    static Pop3ClientOptions CreateOptions()
    {
        Pop3ClientOptions options = new Pop3ClientOptions();
        options.ConnectTimeoutMilliseconds = 2000;
        options.ReadTimeoutMilliseconds = 2000;
        options.WriteTimeoutMilliseconds = 2000;
        options.AuthenticationTimeoutMilliseconds = 2000;
        options.BufferSize = 32;
        options.LeaveStreamOpen = true;
        return options;
    }

    class Utf8SequenceStream : Stream
    {
        byte[] Responses;
        int ReadPosition;
        MemoryStream Written = new MemoryStream();

        public Utf8SequenceStream(string responses)
        {
            Responses = Encoding.UTF8.GetBytes(responses);
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get { return Responses.Length; }
        }

        public override long Position
        {
            get { return ReadPosition; }
            set { throw new NotSupportedException(); }
        }

        public string WrittenText()
        {
            return Encoding.UTF8.GetString(Written.ToArray());
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int remaining = Responses.Length - ReadPosition;
            int readCount = Math.Min(count, remaining);
            Array.Copy(Responses, ReadPosition, buffer, offset, readCount);
            ReadPosition += readCount;
            return readCount;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Written.Write(buffer, offset, count);
        }
    }
}
