using System.Text;

namespace pop3.Tests;

public class Pop3ServerExtensionTests
{
    [Test]
    public void AuthorizationExtensionReceivesSafeContextAndLogsRedactedCommand()
    {
        ServerExtension extension = Extension(
            "XONE",
            new[] { Pop3SessionState.Authorization });
        extension.Capabilities = new[] { Capability("X-CAP") };
        extension.MarkCommandSensitive = true;
        extension.Result.Response.Kind = Pop3ResponseKind.Negative;
        extension.Result.Response.Text = "later";
        extension.Result.Response.ResponseCodeLevels =
            new[] { "SYS", "TEMP" };
        RecordingLogger logger = new RecordingLogger();
        Fixture fixture = Start(
            "xone secret\r\n",
            new IPop3ServerExtension[] { extension },
            false,
            logger,
            null,
            64);

        Pop3ExtensionResult result = fixture.Engine.HandleExtensionCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(result.Response.Kind, Is.EqualTo(Pop3ResponseKind.Negative));
        Assert.That(extension.ExecuteCount, Is.EqualTo(1));
        Assert.That(extension.Context, Is.Not.Null);
        Assert.That(extension.Context!.State,
            Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(extension.Context.IsAuthenticated, Is.False);
        Assert.That(extension.Context.Capabilities.Entries.Any(
            capability => capability.Name == "X-CAP"), Is.True);
        Assert.That(extension.Command, Is.Not.Null);
        Assert.That(extension.Command!.Keyword, Is.EqualTo("XONE"));
        Assert.That(extension.Command.Arguments, Is.EqualTo(new[] { "secret" }));
        Assert.That(logger.Entries.Count, Is.EqualTo(2));
        Assert.That(logger.Entries[0].Text, Is.EqualTo("XONE [redacted]"));
        Assert.That(logger.Entries[0].IsRedacted, Is.True);
        Assert.That(logger.Entries[0].ByteCount, Is.EqualTo(13));
        Assert.That(logger.Entries[1].Text,
            Is.EqualTo("-ERR [SYS/TEMP] later"));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n-ERR [SYS/TEMP] later\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void DisabledResponseCodesSuppressExtensionClassification()
    {
        ServerExtension extension = Extension(
            "XONE",
            new[] { Pop3SessionState.Authorization });
        extension.Result.Response.Kind = Pop3ResponseKind.Negative;
        extension.Result.Response.Text = "later";
        extension.Result.Response.ResponseCodeLevels =
            new[] { "SYS", "TEMP" };
        Fixture fixture = Start(
            "XONE\r\n",
            new IPop3ServerExtension[] { extension },
            false,
            null,
            null,
            64);
        fixture.Options.EnableResponseCodes = false;

        Pop3ExtensionResult result = fixture.Engine.HandleExtensionCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(result.Response.ResponseCodeLevels, Is.Empty);
        Assert.That(extension.Result.Response.ResponseCodeLevels,
            Is.EqualTo(new[] { "SYS", "TEMP" }));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n-ERR later\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void TransactionExtensionStreamsDotStuffedDataWithProgressAndLogging()
    {
        ServerExtension extension = Extension(
            "XDAT",
            new[] { Pop3SessionState.Transaction });
        TrackingStream data = new TrackingStream(
            Encoding.ASCII.GetBytes("one\n.two"));
        extension.Result.Response.Kind = Pop3ResponseKind.Positive;
        extension.Result.Response.Text = "data follows";
        extension.Result.MultilineData = data;
        RecordingLogger logger = new RecordingLogger();
        RecordingObserver observer = new RecordingObserver();
        Fixture fixture = Start(
            "xdat\r\n",
            new IPop3ServerExtension[] { extension },
            true,
            logger,
            observer,
            64);

        Pop3ExtensionResult result = fixture.Engine.HandleExtensionCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(result.Response.IsMultiline, Is.True);
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n" +
            "+OK data follows\r\n" +
            "one\r\n" +
            "..two\r\n" +
            ".\r\n"));
        Assert.That(observer.Reports.Count, Is.EqualTo(3));
        AssertProgress(observer.Reports[0], 5, false);
        AssertProgress(observer.Reports[1], 11, false);
        AssertProgress(observer.Reports[2], 11, true);
        Assert.That(logger.Entries.Count, Is.EqualTo(3));
        Assert.That(logger.Entries[2].Text, Is.EqualTo("extension data"));
        Assert.That(logger.Entries[2].ByteCount, Is.EqualTo(11));
        Assert.That(logger.Entries[2].IsRedacted, Is.False);
        Assert.That(data.Disposed, Is.False);

        fixture.Dispose();
        data.Dispose();
    }

    [Test]
    public void ExtensionStateDeclarationRejectsCommandAndKeepsInputSynchronized()
    {
        ServerExtension extension = Extension(
            "XTXN",
            new[] { Pop3SessionState.Transaction });
        Fixture fixture = Start(
            "xtxn\r\nCAPA\r\n",
            new IPop3ServerExtension[] { extension },
            false,
            null,
            null,
            64);

        Pop3Command command =
            fixture.Engine.ReadNextCommand(CancellationToken.None);

        Assert.That(command.Keyword, Is.EqualTo(Pop3Commands.Capa));
        Assert.That(extension.ExecuteCount, Is.Zero);
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n" +
            "-ERR Command is not valid in the current state.\r\n"));

        fixture.Dispose();
    }

    [Test]
    public void ExtensionCommandIsAnIntakeBarrier()
    {
        ServerExtension extension = Extension(
            "XONE",
            new[] { Pop3SessionState.Authorization });
        extension.Result.Response.Kind = Pop3ResponseKind.Positive;
        extension.Result.Response.Text = "done";
        Fixture fixture = Start(
            "XONE\r\nCAPA\r\n",
            new IPop3ServerExtension[] { extension },
            false,
            null,
            null,
            64);
        Pop3Command command =
            fixture.Engine.ReadNextCommand(CancellationToken.None);

        Assert.Throws<Pop3StateException>(() =>
            fixture.Engine.ReadNextCommand(CancellationToken.None));
        fixture.Engine.HandleExtensionCommand(
            command,
            CancellationToken.None);
        Assert.That(fixture.Engine.ReadNextCommand(
            CancellationToken.None).Keyword, Is.EqualTo(Pop3Commands.Capa));

        fixture.Dispose();
    }

    [Test]
    public void InvalidExtensionRegistrationsFailBeforeGreeting()
    {
        AssertRegistrationFailure(
            Extension("STAT", new[] { Pop3SessionState.Transaction }));
        AssertRegistrationFailure(
            Extension("XBAD", new[] { Pop3SessionState.Closed }));
        AssertRegistrationFailure(
            Extension("XX", new[] { Pop3SessionState.Authorization }));
        AssertRegistrationFailure(
            Extension("XONE", Array.Empty<Pop3SessionState>()));
        AssertRegistrationFailure(
            Extension("XONE", new[]
            {
                Pop3SessionState.Authorization,
                Pop3SessionState.Authorization
            }));
        AssertRegistrationFailure(
            Extension("XONE", new[] { Pop3SessionState.Authorization }),
            Extension("xone", new[] { Pop3SessionState.Transaction }));
    }

    [Test]
    public void InvalidExtensionResultClosesSessionBeforeWritingResponse()
    {
        ServerExtension extension = Extension(
            "XONE",
            new[] { Pop3SessionState.Authorization });
        extension.Result.Response.Kind = Pop3ResponseKind.SaslContinuation;
        extension.Result.Response.Text = "challenge";
        Fixture fixture = Start(
            "XONE\r\n",
            new IPop3ServerExtension[] { extension },
            false,
            null,
            null,
            64);

        Assert.Throws<InvalidDataException>(() =>
            fixture.Engine.HandleExtensionCommand(
                fixture.Engine.ReadNextCommand(CancellationToken.None),
                CancellationToken.None));

        Assert.That(fixture.Engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo("+OK ready\r\n"));
        Assert.That(fixture.Stream.CanRead, Is.True);
        fixture.Stream.Dispose();
    }

    [Test]
    public void MultilineLimitFailureClosesSessionWithoutOwningProviderStream()
    {
        ServerExtension extension = Extension(
            "XDAT",
            new[] { Pop3SessionState.Transaction });
        TrackingStream data = new TrackingStream(
            Encoding.ASCII.GetBytes("four"));
        extension.Result.Response.Kind = Pop3ResponseKind.Positive;
        extension.Result.Response.Text = "data follows";
        extension.Result.MultilineData = data;
        Fixture fixture = Start(
            "XDAT\r\n",
            new IPop3ServerExtension[] { extension },
            true,
            null,
            null,
            5);

        Assert.Throws<InvalidDataException>(() =>
            fixture.Engine.HandleExtensionCommand(
                fixture.Engine.ReadNextCommand(CancellationToken.None),
                CancellationToken.None));

        Assert.That(fixture.Engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n+OK data follows\r\n"));
        Assert.That(data.Disposed, Is.False);
        fixture.Stream.Dispose();
        data.Dispose();
    }

    [Test]
    public void ExtensionCapabilityFailureIsContainedDuringCapa()
    {
        ServerExtension extension = Extension(
            "XONE",
            new[] { Pop3SessionState.Authorization });
        extension.Capabilities = new[] { Capability(Pop3Capabilities.Top) };
        Fixture fixture = Start(
            "CAPA\r\n",
            new IPop3ServerExtension[] { extension },
            false,
            null,
            null,
            64);
        fixture.Engine.ReadNextCommand(CancellationToken.None);

        Assert.Throws<InvalidDataException>(() =>
            fixture.Engine.WriteCapabilitiesResponse(CancellationToken.None));

        Assert.That(fixture.Engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo("+OK ready\r\n"));
        fixture.Stream.Dispose();
    }

    [Test]
    public void CloseResultWritesCompleteResponseBeforeClosingSession()
    {
        ServerExtension extension = Extension(
            "XBYE",
            new[] { Pop3SessionState.Authorization });
        extension.Result.Response.Kind = Pop3ResponseKind.Positive;
        extension.Result.Response.Text = "closing";
        extension.Result.CloseSession = true;
        Fixture fixture = Start(
            "XBYE\r\n",
            new IPop3ServerExtension[] { extension },
            false,
            null,
            null,
            64);

        fixture.Engine.HandleExtensionCommand(
            fixture.Engine.ReadNextCommand(CancellationToken.None),
            CancellationToken.None);

        Assert.That(fixture.Engine.GetStateMachine().GetCurrentState(),
            Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(fixture.Stream.WrittenText(), Is.EqualTo(
            "+OK ready\r\n+OK closing\r\n"));
        fixture.Stream.Dispose();
    }

    static void AssertProgress(
        Pop3TransferProgress progress,
        long bytes,
        bool complete)
    {
        Assert.That(progress.Kind, Is.EqualTo(Pop3TransferKind.ExtensionData));
        Assert.That(progress.MessageNumber, Is.Zero);
        Assert.That(progress.BytesTransferred, Is.EqualTo(bytes));
        Assert.That(progress.ExpectedBytes, Is.EqualTo(-1));
        Assert.That(progress.IsComplete, Is.EqualTo(complete));
    }

    static void AssertRegistrationFailure(
        params ServerExtension[] extensions)
    {
        DuplexStream stream = new DuplexStream("");
        Pop3ServerOptions options = Options(
            extensions,
            null,
            null,
            64);
        Pop3ServerConnection connection = new Pop3ServerConnection();
        connection.Stream = stream;
        connection.LeaveOpen = true;
        Pop3ServerEngine engine = new Pop3ServerEngine();

        Assert.Throws<InvalidDataException>(() =>
            engine.StartSession(
                connection,
                options,
                CancellationToken.None));
        Assert.That(stream.WrittenText(), Is.Empty);
        stream.Dispose();
    }

    static ServerExtension Extension(
        string name,
        Pop3SessionState[] states)
    {
        ServerExtension extension = new ServerExtension();
        extension.CommandNames = new[] { name };
        extension.AllowedStates = states;
        return extension;
    }

    static Pop3Capability Capability(string name)
    {
        Pop3Capability capability = new Pop3Capability();
        capability.Name = name;
        return capability;
    }

    static Fixture Start(
        string input,
        IPop3ServerExtension[] extensions,
        bool transaction,
        IPop3ProtocolLogger? logger,
        IPop3TransferObserver? observer,
        int maximumLineOctets)
    {
        DuplexStream stream = new DuplexStream(input);
        Pop3ServerOptions options = Options(
            extensions,
            logger,
            observer,
            maximumLineOctets);
        Pop3ServerConnection connection = new Pop3ServerConnection();
        connection.Stream = stream;
        connection.LeaveOpen = true;
        Pop3ServerEngine engine = new Pop3ServerEngine();
        engine.StartSession(connection, options, CancellationToken.None);
        if (transaction)
        {
            engine.GetStateMachine().EnterTransaction();
        }
        return new Fixture(engine, stream, options);
    }

    static Pop3ServerOptions Options(
        IPop3ServerExtension[] extensions,
        IPop3ProtocolLogger? logger,
        IPop3TransferObserver? observer,
        int maximumLineOctets)
    {
        Pop3ServerOptions options = new Pop3ServerOptions();
        options.GreetingText = "ready";
        options.SecurityMode = Pop3SecurityMode.Plaintext;
        options.EnablePipelining = true;
        options.Extensions = extensions;
        options.Logger = logger;
        options.TransferObserver = observer;
        options.MaximumMessageLineOctets = maximumLineOctets;
        options.BufferSize = 64;
        return options;
    }

    sealed class Fixture
    {
        internal Pop3ServerEngine Engine;
        internal DuplexStream Stream;
        internal Pop3ServerOptions Options;

        internal Fixture(
            Pop3ServerEngine engine,
            DuplexStream stream,
            Pop3ServerOptions options)
        {
            Engine = engine;
            Stream = stream;
            Options = options;
        }

        internal void Dispose()
        {
            Engine.CloseSession();
            Stream.Dispose();
        }
    }

    sealed class ServerExtension : IPop3ServerExtension
    {
        internal string[] CommandNames = Array.Empty<string>();
        internal Pop3SessionState[] AllowedStates =
            Array.Empty<Pop3SessionState>();
        internal Pop3Capability[] Capabilities = Array.Empty<Pop3Capability>();
        internal Pop3ExtensionResult Result = new Pop3ExtensionResult();
        internal bool MarkCommandSensitive;
        internal int ExecuteCount;
        internal Pop3ExtensionContext? Context;
        internal Pop3Command? Command;

        public Pop3Capability[] GetCapabilities(Pop3ExtensionContext context)
        {
            return Capabilities;
        }

        public string[] GetCommandNames()
        {
            return CommandNames;
        }

        public Pop3SessionState[] GetAllowedStates(string commandName)
        {
            return AllowedStates;
        }

        public Pop3ExtensionResult ExecuteCommand(
            Pop3ExtensionContext context,
            Pop3Command command,
            CancellationToken cancellationToken)
        {
            ExecuteCount++;
            Context = context;
            Command = command;
            if (MarkCommandSensitive)
            {
                command.ContainsSensitiveData = true;
            }
            return Result;
        }
    }

    sealed class RecordingLogger : IPop3ProtocolLogger
    {
        internal List<Pop3LogEntry> Entries = new List<Pop3LogEntry>();

        public void Log(Pop3LogEntry entry)
        {
            Entries.Add(entry);
        }
    }

    sealed class RecordingObserver : IPop3TransferObserver
    {
        internal List<Pop3TransferProgress> Reports =
            new List<Pop3TransferProgress>();

        public void Report(Pop3TransferProgress progress)
        {
            Reports.Add(progress);
        }
    }

    sealed class TrackingStream : MemoryStream
    {
        internal bool Disposed;

        internal TrackingStream(byte[] data) : base(data)
        {
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    sealed class DuplexStream : Stream
    {
        byte[] Input;
        int InputOffset;
        MemoryStream Output = new MemoryStream();

        internal DuplexStream(string input)
        {
            Input = Encoding.ASCII.GetBytes(input);
        }

        internal string WrittenText()
        {
            return Encoding.UTF8.GetString(Output.ToArray());
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
