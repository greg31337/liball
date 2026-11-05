using System.Text;

namespace pop3.Tests;

public class Pop3ClientEngineStreamTests
{
    [Test]
    public void ConnectionLifecycleUpdatesEveryObservableSnapshotField()
    {
        ScriptedDuplexStream stream = new ScriptedDuplexStream(
            Encoding.ASCII.GetBytes(
                "+OK ready <1@example.test>\r\n" +
                "+OK capabilities\r\n" +
                "TOP\r\n" +
                "USER\r\n" +
                ".\r\n"),
            3,
            true,
            true,
            false);
        Pop3ClientEngine engine = new Pop3ClientEngine();

        AssertDisconnectedSnapshot(engine.GetSnapshot());

        engine.ConnectStream(stream, CreateOptions(true), CancellationToken.None);

        Pop3ClientSnapshot connected = engine.GetSnapshot();
        Assert.That(connected.State, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(connected.IsConnected, Is.True);
        Assert.That(connected.IsSecure, Is.False);
        Assert.That(connected.IsAuthenticated, Is.False);
        Assert.That(connected.AuthenticatedIdentity, Is.Null);
        Assert.That(connected.Greeting, Is.EqualTo("ready <1@example.test>"));
        Assert.That(connected.LastResponse!.Kind, Is.EqualTo(Pop3ResponseKind.Positive));
        Assert.That(connected.LastResponse.Text, Is.EqualTo("capabilities"));
        Assert.That(connected.Capabilities.SupportsTop, Is.True);
        Assert.That(connected.Capabilities.SupportsUser, Is.True);
        Assert.That(connected.MailboxStatus, Is.Null);
        Assert.That(connected.IsUtf8Enabled, Is.False);
        Assert.That(connected.IsUtf8UserEnabled, Is.False);
        Assert.That(connected.SelectedLanguage, Is.Null);
        Assert.That(connected.Messages, Is.Empty);
        Assert.That(connected.UniqueIds, Is.Empty);

        engine.CloseConnection();

        Pop3ClientSnapshot closed = engine.GetSnapshot();
        Assert.That(closed.State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(closed.IsConnected, Is.False);
        Assert.That(closed.IsSecure, Is.False);
        Assert.That(closed.IsAuthenticated, Is.False);
        Assert.That(closed.AuthenticatedIdentity, Is.Null);
        Assert.That(closed.Greeting, Is.EqualTo("ready <1@example.test>"));
        Assert.That(closed.LastResponse!.Text, Is.EqualTo("capabilities"));
        Assert.That(closed.Capabilities.Entries, Is.Empty);
        Assert.That(closed.MailboxStatus, Is.Null);
        Assert.That(closed.IsUtf8Enabled, Is.False);
        Assert.That(closed.IsUtf8UserEnabled, Is.False);
        Assert.That(closed.SelectedLanguage, Is.Null);
        Assert.That(closed.Messages, Is.Empty);
        Assert.That(closed.UniqueIds, Is.Empty);
        Assert.That(stream.IsDisposed, Is.False);
        stream.Dispose();
    }

    [Test]
    public void AttachesArbitraryDuplexStreamAndPreservesBufferedReadAhead()
    {
        ScriptedDuplexStream stream = new ScriptedDuplexStream(
            Encoding.ASCII.GetBytes("+OK ready\r\n-ERR CAPA unavailable\r\n+OK next\r\n"),
            64,
            true,
            true,
            false);
        Pop3ClientOptions options = CreateOptions(true);
        options.BufferSize = 64;
        Pop3ClientEngine engine = new Pop3ClientEngine();

        engine.ConnectStream(stream, options, CancellationToken.None);
        int readsAfterGreeting = stream.ReadCallCount;
        Pop3Response next = engine.GetWireReader().ReadResponse(
            Pop3Defaults.MaximumResponseLineOctets,
            CancellationToken.None);
        Pop3Command command = new Pop3Command();
        command.Keyword = Pop3Commands.Noop;
        engine.GetWireWriter().WriteCommand(
            command,
            Pop3Defaults.MaximumCommandLineOctets,
            CancellationToken.None);

        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(engine.GetSnapshot().Greeting, Is.EqualTo("ready"));
        Assert.That(next.Text, Is.EqualTo("next"));
        Assert.That(stream.ReadCallCount, Is.EqualTo(readsAfterGreeting));
        Assert.That(Encoding.ASCII.GetString(stream.WrittenBytes()), Is.EqualTo("CAPA\r\nNOOP\r\n"));

        engine.CloseConnection();
        Assert.That(stream.IsDisposed, Is.False);
        Assert.That(stream.CanRead, Is.True);
        stream.Dispose();
    }

    [Test]
    public void AttachedStreamUsesConfiguredTimeoutsWhenSupported()
    {
        ScriptedDuplexStream stream = new ScriptedDuplexStream(
            Encoding.ASCII.GetBytes("+OK ready\r\n-ERR CAPA unavailable\r\n"),
            64,
            true,
            true,
            true);
        Pop3ClientOptions options = CreateOptions(true);
        options.ReadTimeoutMilliseconds = 345;
        options.WriteTimeoutMilliseconds = 456;
        Pop3ClientEngine engine = new Pop3ClientEngine();

        engine.ConnectStream(stream, options, CancellationToken.None);

        Assert.That(stream.ReadTimeoutValue, Is.EqualTo(345));
        Assert.That(stream.WriteTimeoutValue, Is.EqualTo(456));
        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void EmptyPositiveGreetingIsAccepted()
    {
        ScriptedDuplexStream stream = CreateGreetingStream("+OK\r\n");
        Pop3ClientEngine engine = new Pop3ClientEngine();

        engine.ConnectStream(stream, CreateOptions(true), CancellationToken.None);

        Assert.That(engine.GetSnapshot().Greeting, Is.Empty);
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Authorization));
        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void LibraryOwnedAttachedStreamIsDisposedOnClose()
    {
        ScriptedDuplexStream stream = CreateGreetingStream("+OK ready\r\n");
        Pop3ClientEngine engine = new Pop3ClientEngine();

        engine.ConnectStream(stream, CreateOptions(false), CancellationToken.None);
        engine.CloseConnection();

        Assert.That(stream.IsDisposed, Is.True);
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void NegativeGreetingHonorsCallerOwnership(bool leaveOpen, bool expectedDisposed)
    {
        ScriptedDuplexStream stream = CreateGreetingStream("-ERR unavailable\r\n");
        Pop3ClientEngine engine = new Pop3ClientEngine();

        Pop3ConnectionException exception = Assert.Throws<Pop3ConnectionException>(() =>
            engine.ConnectStream(stream, CreateOptions(leaveOpen), CancellationToken.None))!;

        Assert.That(exception.Operation, Is.EqualTo("ConnectStream"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(engine.GetSnapshot().LastResponse!.Text, Is.EqualTo("unavailable"));
        Assert.That(stream.IsDisposed, Is.EqualTo(expectedDisposed));
        stream.Dispose();
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void MalformedGreetingHonorsCallerOwnership(bool leaveOpen, bool expectedDisposed)
    {
        ScriptedDuplexStream stream = CreateGreetingStream("+OK malformed\n");
        Pop3ClientEngine engine = new Pop3ClientEngine();

        Assert.Throws<Pop3ProtocolException>(() =>
            engine.ConnectStream(stream, CreateOptions(leaveOpen), CancellationToken.None));

        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(stream.IsDisposed, Is.EqualTo(expectedDisposed));
        stream.Dispose();
    }

    [TestCase(true, false, "")]
    [TestCase(false, true, "")]
    [TestCase(true, false, "+OK partial")]
    [TestCase(false, true, "+OK partial")]
    public void GreetingEndOfStreamHonorsOwnership(
        bool leaveOpen,
        bool expectedDisposed,
        string partialGreeting)
    {
        ScriptedDuplexStream stream = new ScriptedDuplexStream(
            Encoding.ASCII.GetBytes(partialGreeting),
            2,
            true,
            true,
            false);
        Pop3ClientEngine engine = new Pop3ClientEngine();

        Pop3ConnectionException exception = Assert.Throws<Pop3ConnectionException>(() =>
            engine.ConnectStream(stream, CreateOptions(leaveOpen), CancellationToken.None))!;

        Assert.That(exception.Operation, Is.EqualTo("ReadGreeting"));
        Assert.That(exception.IsEndOfStream, Is.True);
        Assert.That(exception.IsTimeout, Is.False);
        Assert.That(exception.InnerException, Is.TypeOf<EndOfStreamException>());
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(stream.IsDisposed, Is.EqualTo(expectedDisposed));
        stream.Dispose();
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void GreetingTimeoutHonorsCallerOwnership(bool leaveOpen, bool expectedDisposed)
    {
        BlockingDuplexStream stream = new BlockingDuplexStream();
        Pop3ClientOptions options = CreateOptions(leaveOpen);
        options.ReadTimeoutMilliseconds = 50;
        Pop3ClientEngine engine = new Pop3ClientEngine();

        Pop3ConnectionException exception = Assert.Throws<Pop3ConnectionException>(() =>
            engine.ConnectStream(stream, options, CancellationToken.None))!;

        Assert.That(exception.IsTimeout, Is.True);
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(stream.IsDisposed, Is.EqualTo(expectedDisposed));
        stream.Dispose();
    }

    [Test]
    public void PreCanceledAttachmentDoesNotReadConfigureOrDisposeStream()
    {
        ScriptedDuplexStream stream = CreateGreetingStream("+OK ready\r\n");
        Pop3ClientOptions options = CreateOptions(false);
        Pop3ClientEngine engine = new Pop3ClientEngine();
        using (CancellationTokenSource cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();

            Assert.Throws<OperationCanceledException>(() =>
                engine.ConnectStream(stream, options, cancellation.Token));

            Assert.That(stream.ReadCallCount, Is.Zero);
            Assert.That(stream.IsDisposed, Is.False);
            Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Disconnected));
        }
        stream.Dispose();
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void CancellationDuringGreetingHonorsOwnership(
        bool leaveOpen,
        bool expectedDisposed)
    {
        BlockingDuplexStream stream = new BlockingDuplexStream();
        Pop3ClientOptions options = CreateOptions(leaveOpen);
        Pop3ClientEngine engine = new Pop3ClientEngine();
        using (CancellationTokenSource cancellation = new CancellationTokenSource())
        {
            cancellation.CancelAfter(50);

            Assert.Throws<OperationCanceledException>(() =>
                engine.ConnectStream(stream, options, cancellation.Token));

            Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
            Assert.That(stream.IsDisposed, Is.EqualTo(expectedDisposed));
        }
        stream.Dispose();
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void InitialCapaEndOfStreamClosesAndHonorsOwnership(
        bool leaveOpen,
        bool expectedDisposed)
    {
        ScriptedDuplexStream stream = new ScriptedDuplexStream(
            Encoding.ASCII.GetBytes("+OK ready\r\n"),
            2,
            true,
            true,
            false);
        Pop3ClientEngine engine = new Pop3ClientEngine();

        Pop3ConnectionException exception = Assert.Throws<Pop3ConnectionException>(() =>
            engine.ConnectStream(stream, CreateOptions(leaveOpen), CancellationToken.None))!;

        Pop3ClientSnapshot snapshot = engine.GetSnapshot();
        Assert.That(exception.Operation, Is.EqualTo(Pop3Commands.Capa));
        Assert.That(exception.SessionState, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(exception.IsEndOfStream, Is.True);
        Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(snapshot.Greeting, Is.EqualTo("ready"));
        Assert.That(snapshot.LastResponse!.Text, Is.EqualTo("ready"));
        Assert.That(stream.IsDisposed, Is.EqualTo(expectedDisposed));
        stream.Dispose();
    }

    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(true, false, true)]
    [TestCase(false, true, true)]
    public void InitialCapaReadAndWriteTimeoutsCloseAndHonorOwnership(
        bool leaveOpen,
        bool expectedDisposed,
        bool blockWrites)
    {
        BlockingDuplexStream stream = new BlockingDuplexStream(
            "+OK ready\r\n",
            blockWrites);
        Pop3ClientOptions options = CreateOptions(leaveOpen);
        options.ReadTimeoutMilliseconds = 50;
        options.WriteTimeoutMilliseconds = 50;
        Pop3ClientEngine engine = new Pop3ClientEngine();

        Pop3ConnectionException exception = Assert.Throws<Pop3ConnectionException>(() =>
            engine.ConnectStream(stream, options, CancellationToken.None))!;

        Assert.That(exception.Operation, Is.EqualTo(Pop3Commands.Capa));
        Assert.That(exception.SessionState, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(exception.IsEndOfStream, Is.False);
        Assert.That(exception.IsTimeout, Is.True);
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(stream.IsDisposed, Is.EqualTo(expectedDisposed));
        stream.Dispose();
    }

    [Test]
    public void AttachmentValidatesDuplexCapabilityBeforeReading()
    {
        Pop3ClientEngine engine = new Pop3ClientEngine();
        Pop3ClientOptions options = CreateOptions(true);
        ScriptedDuplexStream readOnly = new ScriptedDuplexStream(new byte[0], 1, true, false, false);
        ScriptedDuplexStream writeOnly = new ScriptedDuplexStream(new byte[0], 1, false, true, false);

        Assert.Throws<ArgumentNullException>(() => engine.ConnectStream(null!, options, CancellationToken.None));
        Assert.Throws<ArgumentException>(() => engine.ConnectStream(readOnly, options, CancellationToken.None));
        Assert.Throws<ArgumentException>(() => engine.ConnectStream(writeOnly, options, CancellationToken.None));
        Assert.That(readOnly.ReadCallCount, Is.Zero);
        Assert.That(writeOnly.ReadCallCount, Is.Zero);
        Assert.That(readOnly.IsDisposed, Is.False);
        Assert.That(writeOnly.IsDisposed, Is.False);
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Disconnected));
        readOnly.Dispose();
        writeOnly.Dispose();
    }

    [Test]
    public void SecondAttachmentIsRejectedWithoutTouchingItsStream()
    {
        ScriptedDuplexStream first = CreateGreetingStream("+OK ready\r\n");
        ScriptedDuplexStream second = CreateGreetingStream("+OK other\r\n");
        Pop3ClientEngine engine = new Pop3ClientEngine();
        engine.ConnectStream(first, CreateOptions(true), CancellationToken.None);

        Pop3StateException exception = Assert.Throws<Pop3StateException>(() =>
            engine.ConnectStream(second, CreateOptions(true), CancellationToken.None))!;

        Assert.That(exception.Operation, Is.EqualTo("ConnectStream"));
        Assert.That(exception.SessionState, Is.EqualTo(Pop3SessionState.Authorization));
        Assert.That(second.ReadCallCount, Is.Zero);
        Assert.That(second.IsDisposed, Is.False);
        engine.CloseConnection();
        first.Dispose();
        second.Dispose();
    }

    private static Pop3ClientOptions CreateOptions(bool leaveOpen)
    {
        Pop3ClientOptions options = new Pop3ClientOptions();
        options.ConnectTimeoutMilliseconds = 2000;
        options.ReadTimeoutMilliseconds = 2000;
        options.WriteTimeoutMilliseconds = 2000;
        options.BufferSize = 8;
        options.LeaveStreamOpen = leaveOpen;
        return options;
    }

    private static void AssertDisconnectedSnapshot(Pop3ClientSnapshot snapshot)
    {
        Assert.That(snapshot.State, Is.EqualTo(Pop3SessionState.Disconnected));
        Assert.That(snapshot.IsConnected, Is.False);
        Assert.That(snapshot.IsSecure, Is.False);
        Assert.That(snapshot.IsAuthenticated, Is.False);
        Assert.That(snapshot.AuthenticatedIdentity, Is.Null);
        Assert.That(snapshot.Greeting, Is.Null);
        Assert.That(snapshot.LastResponse, Is.Null);
        Assert.That(snapshot.Capabilities.Entries, Is.Empty);
        Assert.That(snapshot.MailboxStatus, Is.Null);
        Assert.That(snapshot.IsUtf8Enabled, Is.False);
        Assert.That(snapshot.IsUtf8UserEnabled, Is.False);
        Assert.That(snapshot.SelectedLanguage, Is.Null);
        Assert.That(snapshot.Messages, Is.Empty);
        Assert.That(snapshot.UniqueIds, Is.Empty);
    }

    private static ScriptedDuplexStream CreateGreetingStream(string greeting)
    {
        return new ScriptedDuplexStream(
            Encoding.ASCII.GetBytes(greeting + "-ERR CAPA unavailable\r\n"),
            2,
            true,
            true,
            false);
    }

    private class ScriptedDuplexStream : Stream
    {
        byte[] ReadData;
        int MaximumReadSize;
        int ReadPosition;
        bool Readable;
        bool Writable;
        bool SupportsTimeout;
        MemoryStream Written = new MemoryStream();
        public bool IsDisposed;
        public int ReadCallCount;
        public int ReadTimeoutValue;
        public int WriteTimeoutValue;

        public ScriptedDuplexStream(
            byte[] readData,
            int maximumReadSize,
            bool readable,
            bool writable,
            bool supportsTimeout)
        {
            ReadData = readData;
            MaximumReadSize = maximumReadSize;
            Readable = readable;
            Writable = writable;
            SupportsTimeout = supportsTimeout;
        }

        public override bool CanRead
        {
            get { return Readable && !IsDisposed; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanTimeout
        {
            get { return SupportsTimeout; }
        }

        public override bool CanWrite
        {
            get { return Writable && !IsDisposed; }
        }

        public override long Length
        {
            get { return ReadData.Length; }
        }

        public override long Position
        {
            get { return ReadPosition; }
            set { throw new NotSupportedException(); }
        }

        public override int ReadTimeout
        {
            get { return ReadTimeoutValue; }
            set { ReadTimeoutValue = value; }
        }

        public override int WriteTimeout
        {
            get { return WriteTimeoutValue; }
            set { WriteTimeoutValue = value; }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadCallCount++;
            int remaining = ReadData.Length - ReadPosition;
            int readCount = Math.Min(count, Math.Min(MaximumReadSize, remaining));
            Array.Copy(ReadData, ReadPosition, buffer, offset, readCount);
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

        public byte[] WrittenBytes()
        {
            return Written.ToArray();
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            if (disposing)
            {
                Written.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    private class BlockingDuplexStream : Stream
    {
        byte[] ReadPrefix;
        int ReadPosition;
        bool BlockWrites;
        public bool IsDisposed;

        public BlockingDuplexStream()
            : this("", false)
        {
        }

        public BlockingDuplexStream(string readPrefix, bool blockWrites)
        {
            ReadPrefix = Encoding.ASCII.GetBytes(readPrefix);
            BlockWrites = blockWrites;
        }

        public override bool CanRead
        {
            get { return !IsDisposed; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return !IsDisposed; }
        }

        public override long Length
        {
            get { return ReadPrefix.Length; }
        }

        public override long Position
        {
            get { return 0; }
            set { throw new NotSupportedException(); }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override async Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            int remaining = ReadPrefix.Length - ReadPosition;
            if (remaining > 0)
            {
                int readCount = Math.Min(count, remaining);
                Array.Copy(ReadPrefix, ReadPosition, buffer, offset, readCount);
                ReadPosition += readCount;
                return readCount;
            }
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return 0;
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
        }

        public override async Task WriteAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            if (BlockWrites)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            cancellationToken.ThrowIfCancellationRequested();
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}
