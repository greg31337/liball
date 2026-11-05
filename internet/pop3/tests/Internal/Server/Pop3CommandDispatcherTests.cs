using System.Text;

namespace pop3.Tests;

public class Pop3CommandDispatcherTests
{
    [Test]
    public void EngineReturnsCanonicalCommandAndWritesRejectedCommandsInOrder()
    {
        string oversized = new string('X', Pop3Defaults.MaximumCommandLineOctets - 1);
        string input = "\r\n" +
            "USER  alice\r\n" +
            "XYZZ\r\n" +
            "STAT\r\n" +
            "LIST +1\r\n" +
            oversized + "\r\n" +
            "cApA\r\n";
        DispatcherStream stream = new DispatcherStream(input);
        Pop3ServerEngine engine = StartEngine(stream);

        Pop3Command command = engine.ReadNextCommand(CancellationToken.None);

        Assert.That(command.Keyword, Is.EqualTo(Pop3Commands.Capa));
        Assert.That(command.Arguments, Is.Empty);
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n" +
            "-ERR A command cannot be empty.\r\n" +
            "-ERR Command arguments cannot be empty.\r\n" +
            "-ERR Unknown command.\r\n" +
            "-ERR Command is not valid in the current state.\r\n" +
            "-ERR Invalid command arguments.\r\n" +
            "-ERR The command exceeds 255 octets including CRLF.\r\n"));
        Assert.That(stream.BytesRead, Is.EqualTo(Encoding.ASCII.GetByteCount(input)));

        engine.CloseSession();
    }

    [Test]
    public void CaseInsensitiveCommandPreservesArguments()
    {
        DispatcherStream stream = new DispatcherStream("uSeR Alice-123\r\n");
        Pop3ServerEngine engine = StartEngine(stream);

        Pop3Command command = engine.ReadNextCommand(CancellationToken.None);

        Assert.That(command.Keyword, Is.EqualTo(Pop3Commands.User));
        Assert.That(command.Arguments, Is.EqualTo(new[] { "Alice-123" }));
        Assert.That(command.ContainsSensitiveData, Is.False);

        engine.CloseSession();
    }

    [TestCase("USER")]
    [TestCase("USER alice extra")]
    [TestCase("PASS")]
    [TestCase("PASS secret extra")]
    [TestCase("APOP alice")]
    [TestCase("APOP alice digest extra")]
    [TestCase("STAT 1")]
    [TestCase("LIST 0")]
    [TestCase("LIST +1")]
    [TestCase("LIST 2147483648")]
    [TestCase("RETR 0")]
    [TestCase("DELE -1")]
    [TestCase("NOOP extra")]
    [TestCase("RSET extra")]
    [TestCase("QUIT extra")]
    [TestCase("TOP 0 1")]
    [TestCase("TOP 1 -1")]
    [TestCase("TOP 1 2147483648")]
    [TestCase("UIDL message")]
    [TestCase("CAPA extra")]
    [TestCase("STLS extra")]
    [TestCase("AUTH")]
    [TestCase("AUTH PLAIN response extra")]
    [TestCase("UTF8 extra")]
    [TestCase("LANG en extra")]
    public void InvalidStandardCommandArgumentsAreRejected(string commandLine)
    {
        DispatcherStream stream = new DispatcherStream(
            commandLine + "\r\nCAPA\r\n");
        Pop3ServerEngine engine = StartEngine(stream);

        Pop3Command command = engine.ReadNextCommand(CancellationToken.None);

        Assert.That(command.Keyword, Is.EqualTo(Pop3Commands.Capa));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n-ERR Invalid command arguments.\r\n"));
        engine.CloseSession();
    }

    [TestCase("STAT")]
    [TestCase("LIST")]
    [TestCase("RETR 1")]
    [TestCase("DELE 1")]
    [TestCase("NOOP")]
    [TestCase("RSET")]
    [TestCase("TOP 1 0")]
    [TestCase("UIDL")]
    public void AuthorizationRejectsEveryTransactionOnlyCommand(string commandLine)
    {
        DispatcherStream stream = new DispatcherStream(
            commandLine + "\r\nCAPA\r\n");
        Pop3ServerEngine engine = StartEngine(stream);

        Pop3Command command = engine.ReadNextCommand(CancellationToken.None);

        Assert.That(command.Keyword, Is.EqualTo(Pop3Commands.Capa));
        Assert.That(engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n" +
            "-ERR Command is not valid in the current state.\r\n"));
        engine.CloseSession();
    }

    [TestCase("USER alice")]
    [TestCase("PASS secret")]
    [TestCase("APOP alice digest")]
    [TestCase("AUTH PLAIN")]
    [TestCase("STLS")]
    [TestCase("UTF8")]
    public void TransactionRejectsEveryAuthorizationOnlyCommand(string commandLine)
    {
        DispatcherStream stream = new DispatcherStream(
            commandLine + "\r\nNOOP\r\n");
        Pop3ServerEngine engine = StartEngine(stream);
        engine.GetStateMachine().EnterTransaction();

        Pop3Command command = engine.ReadNextCommand(CancellationToken.None);

        Assert.That(command.Keyword, Is.EqualTo(Pop3Commands.Noop));
        Assert.That(engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Transaction));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n" +
            "-ERR Command is not valid in the current state.\r\n"));
        engine.CloseSession();
    }

    [Test]
    public void CommandAtTheExactLineLimitIsAcceptedWithoutTruncation()
    {
        string userName = new string('a', 248);
        string input = "uSeR " + userName + "\r\n";
        DispatcherStream stream = new DispatcherStream(input);
        Pop3ServerEngine engine = StartEngine(stream);

        Pop3Command command = engine.ReadNextCommand(CancellationToken.None);

        Assert.That(Encoding.ASCII.GetByteCount(input),
            Is.EqualTo(Pop3Defaults.MaximumCommandLineOctets));
        Assert.That(command.Keyword, Is.EqualTo(Pop3Commands.User));
        Assert.That(command.Arguments, Is.EqualTo(new[] { userName }));
        Assert.That(stream.WrittenText(), Is.EqualTo("+OK ready\r\n"));
        engine.CloseSession();
    }

    [Test]
    public void ExplicitResponsesRemainInCommandOrder()
    {
        DispatcherStream stream = new DispatcherStream(
            "USER alice\r\nCAPA\r\n");
        Pop3ServerEngine engine = StartEngine(stream);
        Pop3Response first = CreateResponse(Pop3ResponseKind.Positive, "first");
        Pop3Response second = CreateResponse(Pop3ResponseKind.Negative, "second");

        Assert.That(engine.ReadNextCommand(CancellationToken.None).Keyword,
            Is.EqualTo(Pop3Commands.User));
        engine.WriteNextResponse(first, CancellationToken.None);
        Assert.That(engine.ReadNextCommand(CancellationToken.None).Keyword,
            Is.EqualTo(Pop3Commands.Capa));
        engine.WriteNextResponse(second, CancellationToken.None);

        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n+OK first\r\n-ERR second\r\n"));
        engine.CloseSession();
    }

    [Test]
    public void DisabledPipeliningRequiresAResponseBeforeTheNextCommand()
    {
        DispatcherStream stream = new DispatcherStream(
            "USER alice\r\nCAPA\r\n");
        Pop3ServerEngine engine = StartEngineWithoutPipelining(stream);

        Assert.Throws<Pop3StateException>(() =>
            engine.WriteNextResponse(
                CreateResponse(Pop3ResponseKind.Positive, "early"),
                CancellationToken.None));
        Assert.That(engine.ReadNextCommand(CancellationToken.None).Keyword,
            Is.EqualTo(Pop3Commands.User));
        Assert.Throws<Pop3StateException>(() =>
            engine.ReadNextCommand(CancellationToken.None));

        engine.WriteNextResponse(
            CreateResponse(Pop3ResponseKind.Positive, "accepted"),
            CancellationToken.None);
        Assert.That(engine.ReadNextCommand(CancellationToken.None).Keyword,
            Is.EqualTo(Pop3Commands.Capa));

        engine.CloseSession();
    }

    [Test]
    public void EnabledPipeliningQueuesCommandsAndRequiresResponseOrder()
    {
        DispatcherStream stream = new DispatcherStream(
            "CAPA\r\nUSER alice\r\n");
        Pop3ServerEngine engine = StartEngine(stream);

        Pop3Command capa = engine.ReadNextCommand(CancellationToken.None);
        Pop3Command user = engine.ReadNextCommand(CancellationToken.None);

        Assert.Throws<Pop3StateException>(() =>
            engine.HandleUserCommand(user, CancellationToken.None));
        engine.WriteCapabilitiesResponse(CancellationToken.None);
        engine.WriteNextResponse(
            CreateResponse(Pop3ResponseKind.Positive, "user follows"),
            CancellationToken.None);

        string output = stream.WrittenText();
        Assert.That(capa.Keyword, Is.EqualTo(Pop3Commands.Capa));
        Assert.That(user.Keyword, Is.EqualTo(Pop3Commands.User));
        Assert.That(output.IndexOf("+OK Capability list follows\r\n"),
            Is.LessThan(output.IndexOf("+OK user follows\r\n")));
        Assert.That(output, Does.Contain(".\r\n+OK user follows\r\n"));
        engine.CloseSession();
    }

    [Test]
    public void DeferredPipelineErrorsKeepTheirInputOrder()
    {
        DispatcherStream stream = new DispatcherStream(
            "CAPA\r\nBAD!\r\nUSER alice\r\n");
        Pop3ServerEngine engine = StartEngine(stream);
        Pop3Response first = CreateResponse(Pop3ResponseKind.Positive, "first");
        Pop3Response third = CreateResponse(Pop3ResponseKind.Positive, "third");

        Pop3Command capa = engine.ReadNextCommand(CancellationToken.None);
        Pop3Command user = engine.ReadNextCommand(CancellationToken.None);

        Assert.That(stream.WrittenText(), Is.EqualTo("+OK ready\r\n"));
        engine.WriteNextResponse(first, CancellationToken.None);
        engine.WriteNextResponse(third, CancellationToken.None);

        Assert.That(capa.Keyword, Is.EqualTo(Pop3Commands.Capa));
        Assert.That(user.Keyword, Is.EqualTo(Pop3Commands.User));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n" +
            "+OK first\r\n" +
            "-ERR Unknown command.\r\n" +
            "+OK third\r\n"));
        engine.CloseSession();
    }

    [TestCase("STLS")]
    [TestCase("AUTH PLAIN")]
    [TestCase("UTF8")]
    public void SecurityCommandsCannotJoinAnExistingPipeline(
        string commandLine)
    {
        DispatcherStream stream = new DispatcherStream(
            "CAPA\r\n" + commandLine + "\r\nUSER alice\r\n");
        Pop3ServerEngine engine = StartEngine(stream);

        Assert.That(engine.ReadNextCommand(CancellationToken.None).Keyword,
            Is.EqualTo(Pop3Commands.Capa));
        Assert.That(engine.ReadNextCommand(CancellationToken.None).Keyword,
            Is.EqualTo(Pop3Commands.User));
        engine.WriteNextResponse(
            CreateResponse(Pop3ResponseKind.Positive, "capability response"),
            CancellationToken.None);
        engine.WriteNextResponse(
            CreateResponse(Pop3ResponseKind.Positive, "user response"),
            CancellationToken.None);

        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n" +
            "+OK capability response\r\n" +
            "-ERR The command cannot be used in a pipeline.\r\n" +
            "+OK user response\r\n"));
        engine.CloseSession();
    }

    [TestCase("STLS")]
    [TestCase("AUTH PLAIN")]
    [TestCase("UTF8")]
    [TestCase("QUIT")]
    public void AcceptedBarrierStopsFurtherCommandIntake(string commandLine)
    {
        DispatcherStream stream = new DispatcherStream(
            commandLine + "\r\nCAPA\r\n");
        Pop3ServerEngine engine = StartEngine(stream);

        engine.ReadNextCommand(CancellationToken.None);

        Assert.Throws<Pop3StateException>(() =>
            engine.ReadNextCommand(CancellationToken.None));
        Assert.That(stream.WrittenText(), Is.EqualTo("+OK ready\r\n"));
        engine.CloseSession();
    }

    [Test]
    public void Utf8CommandEnablesModeAndOrdersBeforeStls()
    {
        DispatcherStream stream = new DispatcherStream("UTF8\r\nSTLS\r\n");
        Pop3ServerEngine engine = StartEngineWithUtf8(stream, true, false);

        Pop3Response enabled = engine.HandleUtf8Command(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);
        Pop3CapabilitySet? upgraded = engine.UpgradeTransportToTls(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(enabled.Kind, Is.EqualTo(Pop3ResponseKind.Positive));
        Assert.That(engine.GetStateMachine().GetIsUtf8Enabled(), Is.True);
        Assert.That(upgraded, Is.Null);
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n" +
            "+OK UTF8 mode enabled.\r\n" +
            "-ERR TLS upgrade is not available.\r\n"));
        engine.CloseSession();
    }

    [Test]
    public void DisabledUtf8CommandLeavesAsciiModeActive()
    {
        DispatcherStream stream = new DispatcherStream("UTF8\r\n");
        Pop3ServerEngine engine = StartEngineWithUtf8(stream, false, false);

        Pop3Response response = engine.HandleUtf8Command(
            engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(response.Kind, Is.EqualTo(Pop3ResponseKind.Negative));
        Assert.That(engine.GetStateMachine().GetIsUtf8Enabled(), Is.False);
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n-ERR UTF8 mode is not available.\r\n"));
        engine.CloseSession();
    }

    [Test]
    public void Utf8UserAcceptsStrictCredentialsBeforeUtf8Mode()
    {
        DispatcherStream stream = new DispatcherStream(
            Encoding.UTF8.GetBytes("USER álîçé😀\r\n"));
        Pop3ServerEngine engine = StartEngineWithUtf8(stream, true, true);

        Pop3Command command = engine.ReadNextCommand(CancellationToken.None);

        Assert.That(command.Arguments, Is.EqualTo(new[] { "álîçé😀" }));
        Assert.That(engine.GetStateMachine().GetIsUtf8Enabled(), Is.False);
        engine.CloseSession();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void UnavailableOrMalformedUtf8CredentialsAreRejected(bool malformed)
    {
        byte[] credential;
        if (malformed)
        {
            credential = new byte[] { 195, 40 };
        }
        else
        {
            credential = Encoding.UTF8.GetBytes("álice");
        }
        byte[] prefix = Encoding.ASCII.GetBytes("USER ");
        byte[] suffix = Encoding.ASCII.GetBytes("\r\nCAPA\r\n");
        byte[] input = new byte[prefix.Length + credential.Length + suffix.Length];
        Array.Copy(prefix, input, prefix.Length);
        Array.Copy(credential, 0, input, prefix.Length, credential.Length);
        Array.Copy(
            suffix,
            0,
            input,
            prefix.Length + credential.Length,
            suffix.Length);
        DispatcherStream stream = new DispatcherStream(input);
        Pop3ServerEngine engine = StartEngineWithUtf8(
            stream,
            true,
            malformed);

        Pop3Command command = engine.ReadNextCommand(CancellationToken.None);

        Assert.That(command.Keyword, Is.EqualTo(Pop3Commands.Capa));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n" +
            "-ERR The command contains invalid UTF-8 syntax.\r\n"));
        engine.CloseSession();
    }

    [Test]
    public void MultilineResponseIsAProviderOutputBarrierUntilCompleted()
    {
        DispatcherStream stream = new DispatcherStream(
            "CAPA\r\nUSER alice\r\n");
        Pop3ServerOptions options = CreateOptions();
        Pop3ServerStateMachine stateMachine = new Pop3ServerStateMachine();
        Pop3ServerConnection connection = new Pop3ServerConnection();
        connection.Stream = stream;
        connection.LeaveOpen = true;
        stateMachine.OpenConnection(connection, options, 1);
        Pop3CommandDispatcher dispatcher = new Pop3CommandDispatcher(
            new Pop3WireReader(stream, options.BufferSize),
            new Pop3WireWriter(stream, options.BufferSize),
            stateMachine,
            true,
            false);
        Pop3Response response = CreateResponse(
            Pop3ResponseKind.Positive,
            "multiline follows");
        response.IsMultiline = true;

        Assert.That(dispatcher.ReadDispatchableCommand(
            CancellationToken.None).Keyword, Is.EqualTo(Pop3Commands.Capa));
        dispatcher.WriteDispatchedResponse(response, CancellationToken.None);

        Assert.Throws<Pop3StateException>(() =>
            dispatcher.ReadDispatchableCommand(CancellationToken.None));
        dispatcher.CompleteMultilineResponse(CancellationToken.None);
        Assert.That(dispatcher.ReadDispatchableCommand(
            CancellationToken.None).Keyword, Is.EqualTo(Pop3Commands.User));

        stateMachine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void EveryCompleteClientLineResetsInactivityTracking()
    {
        DispatcherStream stream = new DispatcherStream("BAD!\r\nCAPA\r\n");
        Pop3ServerOptions options = CreateOptions();
        Pop3ServerStateMachine stateMachine = new Pop3ServerStateMachine();
        Pop3ServerConnection connection = new Pop3ServerConnection();
        connection.Stream = stream;
        connection.LeaveOpen = true;
        stateMachine.OpenConnection(connection, options, 1);
        Pop3CommandDispatcher dispatcher = new Pop3CommandDispatcher(
            new Pop3WireReader(stream, options.BufferSize),
            new Pop3WireWriter(stream, options.BufferSize),
            stateMachine,
            options.EnablePipelining,
            false);

        Pop3Command command = dispatcher.ReadDispatchableCommand(
            CancellationToken.None);
        long observedAt = Environment.TickCount64;

        Assert.That(command.Keyword, Is.EqualTo(Pop3Commands.Capa));
        Assert.That(stateMachine.GetRemainingInactivityMilliseconds(observedAt),
            Is.GreaterThan(
                Pop3Defaults.MinimumServerInactivityTimeoutMilliseconds - 1000));
        stateMachine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void RejectedCompleteLineResetsInactivityBeforeEndOfStream()
    {
        DispatcherStream stream = new DispatcherStream("BAD!\r\n");
        Pop3ServerOptions options = CreateOptions();
        Pop3ServerStateMachine stateMachine = new Pop3ServerStateMachine();
        Pop3ServerConnection connection = new Pop3ServerConnection();
        connection.Stream = stream;
        connection.LeaveOpen = true;
        stateMachine.OpenConnection(connection, options, 1);
        Pop3CommandDispatcher dispatcher = new Pop3CommandDispatcher(
            new Pop3WireReader(stream, options.BufferSize),
            new Pop3WireWriter(stream, options.BufferSize),
            stateMachine,
            options.EnablePipelining,
            false);

        Assert.Throws<EndOfStreamException>(() =>
            dispatcher.ReadDispatchableCommand(CancellationToken.None));
        long observedAt = Environment.TickCount64;

        Assert.That(stream.WrittenText(), Is.EqualTo("-ERR Unknown command.\r\n"));
        Assert.That(stateMachine.GetRemainingInactivityMilliseconds(observedAt),
            Is.GreaterThan(
                Pop3Defaults.MinimumServerInactivityTimeoutMilliseconds - 1000));
        stateMachine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void CommandStateMatrixMatchesPop3SessionStates()
    {
        Pop3CommandDispatcher dispatcher = CreateDispatcher();
        string[] authorization = new[]
        {
            Pop3Commands.User, Pop3Commands.Pass, Pop3Commands.Apop,
            Pop3Commands.Auth, Pop3Commands.Quit, Pop3Commands.Capa,
            Pop3Commands.Stls, Pop3Commands.Utf8, Pop3Commands.Lang
        };
        string[] transaction = new[]
        {
            Pop3Commands.Stat, Pop3Commands.List, Pop3Commands.Retr,
            Pop3Commands.Dele, Pop3Commands.Noop, Pop3Commands.Rset,
            Pop3Commands.Quit, Pop3Commands.Top, Pop3Commands.Uidl,
            Pop3Commands.Capa, Pop3Commands.Lang
        };
        string[] all = new[]
        {
            Pop3Commands.User, Pop3Commands.Pass, Pop3Commands.Apop,
            Pop3Commands.Stat, Pop3Commands.List, Pop3Commands.Retr,
            Pop3Commands.Dele, Pop3Commands.Noop, Pop3Commands.Rset,
            Pop3Commands.Quit, Pop3Commands.Top, Pop3Commands.Uidl,
            Pop3Commands.Capa, Pop3Commands.Stls, Pop3Commands.Auth,
            Pop3Commands.Utf8, Pop3Commands.Lang
        };

        foreach (string keyword in all)
        {
            Assert.That(dispatcher.IsCommandAllowed(
                keyword.ToLowerInvariant(),
                Pop3SessionState.Authorization),
                Is.EqualTo(Array.IndexOf(authorization, keyword) >= 0),
                keyword + " in AUTHORIZATION");
            Assert.That(dispatcher.IsCommandAllowed(
                keyword.ToLowerInvariant(),
                Pop3SessionState.Transaction),
                Is.EqualTo(Array.IndexOf(transaction, keyword) >= 0),
                keyword + " in TRANSACTION");
            Assert.That(dispatcher.IsCommandAllowed(
                keyword,
                Pop3SessionState.Update), Is.False,
                keyword + " in UPDATE");
            Assert.That(dispatcher.IsCommandAllowed(
                keyword,
                Pop3SessionState.Closed), Is.False,
                keyword + " in CLOSED");
        }
    }

    [Test]
    public void BareLineEndingFailsFramingWithoutWritingAResponse()
    {
        DispatcherStream stream = new DispatcherStream("USER alice\nCAPA\r\n");
        Pop3ServerEngine engine = StartEngine(stream);

        Assert.Throws<InvalidDataException>(() =>
            engine.ReadNextCommand(CancellationToken.None));

        Assert.That(stream.WrittenText(), Is.EqualTo("+OK ready\r\n"));
        engine.CloseSession();
    }

    [Test]
    public void EndOfStreamBeforeAnotherCommandIsReported()
    {
        DispatcherStream stream = new DispatcherStream("");
        Pop3ServerEngine engine = StartEngine(stream);

        Assert.Throws<EndOfStreamException>(() =>
            engine.ReadNextCommand(CancellationToken.None));

        engine.CloseSession();
    }

    static Pop3ServerEngine StartEngine(DispatcherStream stream)
    {
        Pop3ServerConnection connection = new Pop3ServerConnection();
        connection.Stream = stream;
        connection.LeaveOpen = true;
        Pop3ServerEngine engine = new Pop3ServerEngine();
        engine.StartSession(connection, CreateOptions(), CancellationToken.None);
        return engine;
    }

    static Pop3ServerEngine StartEngineWithoutPipelining(
        DispatcherStream stream)
    {
        Pop3ServerConnection connection = new Pop3ServerConnection();
        connection.Stream = stream;
        connection.LeaveOpen = true;
        Pop3ServerOptions options = CreateOptions();
        options.EnablePipelining = false;
        Pop3ServerEngine engine = new Pop3ServerEngine();
        engine.StartSession(connection, options, CancellationToken.None);
        return engine;
    }

    static Pop3ServerEngine StartEngineWithUtf8(
        DispatcherStream stream,
        bool enableUtf8,
        bool enableUtf8User)
    {
        Pop3ServerConnection connection = new Pop3ServerConnection();
        connection.Stream = stream;
        connection.LeaveOpen = true;
        Pop3ServerOptions options = CreateOptions();
        options.EnableUtf8 = enableUtf8;
        options.EnableUtf8User = enableUtf8User;
        Pop3ServerEngine engine = new Pop3ServerEngine();
        engine.StartSession(connection, options, CancellationToken.None);
        return engine;
    }

    static Pop3ServerOptions CreateOptions()
    {
        Pop3ServerOptions options = new Pop3ServerOptions();
        options.GreetingText = "ready";
        options.SecurityMode = Pop3SecurityMode.Plaintext;
        options.InactivityTimeoutMilliseconds =
            Pop3Defaults.MinimumServerInactivityTimeoutMilliseconds;
        return options;
    }

    static Pop3Response CreateResponse(Pop3ResponseKind kind, string text)
    {
        Pop3Response response = new Pop3Response();
        response.Kind = kind;
        response.Text = text;
        return response;
    }

    static Pop3CommandDispatcher CreateDispatcher()
    {
        DispatcherStream stream = new DispatcherStream("");
        return new Pop3CommandDispatcher(
            new Pop3WireReader(stream, 32),
            new Pop3WireWriter(stream, 32),
            new Pop3ServerStateMachine(),
            false,
            false);
    }

    sealed class DispatcherStream : Stream
    {
        byte[] Input;
        int InputOffset;
        MemoryStream Output = new MemoryStream();

        internal DispatcherStream(string input)
        {
            Input = Encoding.ASCII.GetBytes(input);
        }

        internal DispatcherStream(byte[] input)
        {
            Input = input;
        }

        internal int BytesRead;

        internal string WrittenText()
        {
            return Encoding.ASCII.GetString(Output.ToArray());
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
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
                BytesRead += copied;
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
    }
}
