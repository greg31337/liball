using System.Text;

namespace pop3.Tests;

public class Pop3ClientUidlTests
{
    [Test]
    public void SingleUidlPreservesTheExactStableIdentifierInTheSnapshot()
    {
        UidlSequenceStream stream = CreateAdvertisedStream(
            "+OK [X-UIDL] 2 QhdPYR:00WBw1Ph7x7\r\n" +
            "+OK 2 QhdPYR:00WBw1Ph7x7\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream, CreateOptions(false));

        Pop3UniqueId first = engine.GetUniqueId(2, CancellationToken.None);
        Pop3UniqueId second = engine.GetUniqueId(2, CancellationToken.None);
        first.Value = "caller-change";
        second.Value = "caller-change";
        Pop3ClientSnapshot snapshot = engine.GetSnapshot();

        Assert.That(snapshot.UniqueIds, Has.Length.EqualTo(1));
        Assert.That(snapshot.UniqueIds[0].MessageNumber, Is.EqualTo(2));
        Assert.That(snapshot.UniqueIds[0].Value, Is.EqualTo("QhdPYR:00WBw1Ph7x7"));
        Assert.That(snapshot.LastResponse!.Text, Is.EqualTo("2 QhdPYR:00WBw1Ph7x7"));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nCAPA\r\nUIDL 2\r\nUIDL 2\r\n"));

        engine.CloseConnection();
        stream.Dispose();
    }

    [TestCase("+OK 1 wrong-message\r\n")]
    [TestCase("+OK 2 \r\n")]
    [TestCase("+OK 2 AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA\r\n")]
    public void SingleUidlRejectsMismatchedOrMalformedIdentifiers(string response)
    {
        UidlSequenceStream stream = CreateAdvertisedStream(response);
        Pop3ClientEngine engine = CreateTransactionEngine(stream, CreateOptions(false));

        Pop3ProtocolException exception = Assert.Throws<Pop3ProtocolException>(() =>
            engine.GetUniqueId(2, CancellationToken.None))!;

        Assert.That(exception.Context, Is.EqualTo("UIDL response"));
        Assert.That(exception.InnerException, Is.TypeOf<InvalidDataException>());
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(engine.GetSnapshot().UniqueIds, Is.Empty);
        Assert.That(stream.IsDisposed, Is.False);
        stream.Dispose();
    }

    [Test]
    public void SingleUidlAcceptsPrintableResponseTextAfterTheIdentifier()
    {
        UidlSequenceStream stream = CreateAdvertisedStream(
            "+OK 2 stable-id Unique-id listing.\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream, CreateOptions(false));

        Pop3UniqueId uniqueId = engine.GetUniqueId(2, CancellationToken.None);

        Assert.That(uniqueId.MessageNumber, Is.EqualTo(2));
        Assert.That(uniqueId.Value, Is.EqualTo("stable-id"));
        Assert.That(engine.GetSnapshot().LastResponse!.Text,
            Is.EqualTo("2 stable-id Unique-id listing."));

        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void SingleUidlRejectsChangedOrDuplicatedIdentifiersDuringATransaction()
    {
        UidlSequenceStream changedStream = CreateAdvertisedStream(
            "+OK 1 Stable-ID\r\n" +
            "+OK 1 stable-id\r\n");
        Pop3ClientEngine changedEngine = CreateTransactionEngine(
            changedStream,
            CreateOptions(false));
        changedEngine.GetUniqueId(1, CancellationToken.None);

        Assert.Throws<Pop3ProtocolException>(() =>
            changedEngine.GetUniqueId(1, CancellationToken.None));
        Assert.That(changedEngine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        changedStream.Dispose();

        UidlSequenceStream duplicateStream = CreateAdvertisedStream(
            "+OK 1 shared-id\r\n" +
            "+OK 2 shared-id\r\n");
        Pop3ClientEngine duplicateEngine = CreateTransactionEngine(
            duplicateStream,
            CreateOptions(false));
        duplicateEngine.GetUniqueId(1, CancellationToken.None);

        Assert.Throws<Pop3ProtocolException>(() =>
            duplicateEngine.GetUniqueId(2, CancellationToken.None));
        Assert.That(duplicateEngine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        duplicateStream.Dispose();
    }

    [Test]
    public void SingleUidlTreatsMissingOrDeletedMessageAsRetryableCommandFailure()
    {
        UidlSequenceStream stream = CreateAdvertisedStream(
            "-ERR no such message\r\n" +
            "+OK 4 retry-id\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream, CreateOptions(false));

        Pop3CommandException exception = Assert.Throws<Pop3CommandException>(() =>
            engine.GetUniqueId(4, CancellationToken.None))!;
        Pop3UniqueId retried = engine.GetUniqueId(4, CancellationToken.None);

        Assert.That(exception.Command.Keyword, Is.EqualTo(Pop3Commands.Uidl));
        Assert.That(exception.Command.Arguments, Is.EqualTo(new[] { "4" }));
        Assert.That(exception.Response.Text, Is.EqualTo("no such message"));
        Assert.That(retried.Value, Is.EqualTo("retry-id"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));

        engine.CloseConnection();
        stream.Dispose();
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(int.MinValue)]
    public void SingleUidlRejectsNonPositiveNumbersBeforeWriting(int messageNumber)
    {
        UidlSequenceStream stream = CreateAdvertisedStream("+OK 1 unused\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream, CreateOptions(false));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            engine.GetUniqueId(messageNumber, CancellationToken.None));

        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nCAPA\r\n"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));
        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void MultilineUidlMatchesKnownListAndPreservesServerOrder()
    {
        UidlSequenceStream stream = CreateAdvertisedStream(
            "+OK listing\r\n" +
            "3 30\r\n" +
            "1 10\r\n" +
            ".\r\n" +
            "+OK unique identifiers\r\n" +
            "3 Z-ID:03\r\n" +
            "1 a-id:01\r\n" +
            ".\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream, CreateOptions(false));
        engine.GetAllMessageInfo(CancellationToken.None);

        Pop3UniqueId[] uniqueIds = engine.GetAllUniqueIds(CancellationToken.None);
        uniqueIds[0].Value = "caller-change";
        Pop3ClientSnapshot snapshot = engine.GetSnapshot();

        Assert.That(snapshot.UniqueIds.Select(value => value.MessageNumber), Is.EqualTo(new[] { 3, 1 }));
        Assert.That(snapshot.UniqueIds.Select(value => value.Value), Is.EqualTo(new[] { "Z-ID:03", "a-id:01" }));
        Assert.That(snapshot.LastResponse!.IsMultiline, Is.True);
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nCAPA\r\nLIST\r\nUIDL\r\n"));

        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void MultilineUidlAcceptsAnEmptyKnownMaildrop()
    {
        UidlSequenceStream stream = CreateAdvertisedStream(
            "+OK empty list\r\n.\r\n" +
            "+OK empty uidl\r\n.\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream, CreateOptions(false));
        engine.GetAllMessageInfo(CancellationToken.None);

        Pop3UniqueId[] uniqueIds = engine.GetAllUniqueIds(CancellationToken.None);

        Assert.That(uniqueIds, Is.Empty);
        Assert.That(engine.GetSnapshot().UniqueIds, Is.Empty);
        Assert.That(engine.GetSnapshot().MailboxStatus!.MessageCount, Is.Zero);

        engine.CloseConnection();
        stream.Dispose();
    }

    [TestCase("1 first\r\n1 second\r\n.\r\n")]
    [TestCase("1 shared\r\n2 shared\r\n.\r\n")]
    public void MultilineUidlRejectsDuplicateNumbersOrValues(string uidlData)
    {
        UidlSequenceStream stream = CreateAdvertisedStream(
            "+OK identifiers\r\n" + uidlData);
        Pop3ClientEngine engine = CreateTransactionEngine(stream, CreateOptions(false));

        Pop3ProtocolException exception = Assert.Throws<Pop3ProtocolException>(() =>
            engine.GetAllUniqueIds(CancellationToken.None))!;

        Assert.That(exception.Context, Is.EqualTo("UIDL response"));
        Assert.That(exception.InnerException, Is.TypeOf<InvalidDataException>());
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        stream.Dispose();
    }

    [TestCase("0 identifier\r\n")]
    [TestCase("1 \r\n")]
    [TestCase("1 value with spaces\r\n")]
    [TestCase("1 AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA\r\n")]
    public void MultilineUidlRejectsMalformedEntryData(string uidlEntry)
    {
        UidlSequenceStream stream = CreateAdvertisedStream(
            "+OK identifiers\r\n" +
            uidlEntry +
            ".\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream, CreateOptions(false));

        Pop3ProtocolException exception = Assert.Throws<Pop3ProtocolException>(() =>
            engine.GetAllUniqueIds(CancellationToken.None))!;

        Assert.That(exception.Context, Is.EqualTo("UIDL response"));
        Assert.That(exception.InnerException, Is.TypeOf<InvalidDataException>());
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        Assert.That(engine.GetSnapshot().UniqueIds, Is.Empty);
        stream.Dispose();
    }

    [Test]
    public void MultilineUidlExcludesDeletedMessagesAndKeepsStableNumbers()
    {
        UidlSequenceStream stream = CreateAdvertisedStream(
            "+OK listing\r\n" +
            "1 10\r\n" +
            "2 20\r\n" +
            ".\r\n" +
            "+OK initial identifiers\r\n" +
            "1 first-id\r\n" +
            "2 second-id\r\n" +
            ".\r\n" +
            "+OK marked\r\n" +
            "+OK remaining identifiers\r\n" +
            "2 second-id\r\n" +
            ".\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream, CreateOptions(false));
        engine.GetAllMessageInfo(CancellationToken.None);
        engine.GetAllUniqueIds(CancellationToken.None);
        engine.DeleteMessage(1, CancellationToken.None);

        Pop3UniqueId[] uniqueIds = engine.GetAllUniqueIds(CancellationToken.None);
        Pop3ClientSnapshot snapshot = engine.GetSnapshot();

        Assert.That(uniqueIds.Select(value => value.MessageNumber), Is.EqualTo(new[] { 2 }));
        Assert.That(uniqueIds.Select(value => value.Value), Is.EqualTo(new[] { "second-id" }));
        Assert.That(snapshot.UniqueIds.Select(value => value.MessageNumber), Is.EqualTo(new[] { 2 }));
        Assert.That(snapshot.MailboxStatus!.MessageCount, Is.EqualTo(1));
        Assert.That(stream.WrittenText(), Is.EqualTo(
            "CAPA\r\nCAPA\r\nLIST\r\nUIDL\r\nDELE 1\r\nUIDL\r\n"));

        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void MultilineUidlRejectsADeletedMessageInPlaceOfAVisibleMessage()
    {
        UidlSequenceStream stream = CreateAdvertisedStream(
            "+OK listing\r\n" +
            "1 10\r\n" +
            "2 20\r\n" +
            ".\r\n" +
            "+OK marked\r\n" +
            "+OK identifiers\r\n" +
            "1 first-id\r\n" +
            ".\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream, CreateOptions(false));
        engine.GetAllMessageInfo(CancellationToken.None);
        engine.DeleteMessage(1, CancellationToken.None);

        Pop3ProtocolException exception = Assert.Throws<Pop3ProtocolException>(() =>
            engine.GetAllUniqueIds(CancellationToken.None))!;

        Assert.That(exception.Context, Is.EqualTo("UIDL response"));
        Assert.That(exception.InnerException, Is.TypeOf<InvalidDataException>());
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        stream.Dispose();
    }

    [Test]
    public void MultilineUidlRejectsCountAndMessageSetContradictions()
    {
        UidlSequenceStream countStream = CreateAdvertisedStream(
            "+OK 2 30\r\n" +
            "+OK identifiers\r\n" +
            "1 first\r\n" +
            ".\r\n");
        Pop3ClientEngine countEngine = CreateTransactionEngine(countStream, CreateOptions(false));
        countEngine.GetMailboxStatus(CancellationToken.None);

        Assert.Throws<Pop3ProtocolException>(() =>
            countEngine.GetAllUniqueIds(CancellationToken.None));
        Assert.That(countEngine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        countStream.Dispose();

        UidlSequenceStream setStream = CreateAdvertisedStream(
            "+OK listing\r\n" +
            "1 10\r\n" +
            "2 20\r\n" +
            ".\r\n" +
            "+OK identifiers\r\n" +
            "1 first\r\n" +
            "3 third\r\n" +
            ".\r\n");
        Pop3ClientEngine setEngine = CreateTransactionEngine(setStream, CreateOptions(false));
        setEngine.GetAllMessageInfo(CancellationToken.None);

        Assert.Throws<Pop3ProtocolException>(() =>
            setEngine.GetAllUniqueIds(CancellationToken.None));
        Assert.That(setEngine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Closed));
        setStream.Dispose();
    }

    [Test]
    public void UidlIsRejectedLocallyWhenItIsNotAdvertisedOrProbeable()
    {
        UidlSequenceStream stream = CreateUnadvertisedStream("");
        Pop3ClientOptions options = CreateOptions(true);
        Pop3ClientEngine engine = CreateTransactionEngine(stream, options);

        Assert.Throws<InvalidOperationException>(() =>
            engine.GetUniqueId(1, CancellationToken.None));
        Assert.Throws<InvalidOperationException>(() =>
            engine.GetAllUniqueIds(CancellationToken.None));

        Assert.That(stream.WrittenText(), Is.EqualTo("CAPA\r\nCAPA\r\n"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));
        engine.CloseConnection();
        stream.Dispose();
    }

    [Test]
    public void UidlProbeRequiresExplicitOptInWhenCapaIsUnavailable()
    {
        UidlSequenceStream disabledStream = CreateUnavailableStream("+OK 1 ignored\r\n");
        Pop3ClientEngine disabledEngine = CreateTransactionEngine(
            disabledStream,
            CreateOptions(false));

        Assert.Throws<InvalidOperationException>(() =>
            disabledEngine.GetUniqueId(1, CancellationToken.None));
        Assert.That(disabledStream.WrittenText(), Is.EqualTo("CAPA\r\nCAPA\r\n"));
        disabledEngine.CloseConnection();
        disabledStream.Dispose();

        UidlSequenceStream enabledStream = CreateUnavailableStream("+OK 1 exact-ID\r\n");
        Pop3ClientEngine enabledEngine = CreateTransactionEngine(
            enabledStream,
            CreateOptions(true));

        Pop3UniqueId uniqueId = enabledEngine.GetUniqueId(1, CancellationToken.None);

        Assert.That(uniqueId.Value, Is.EqualTo("exact-ID"));
        Assert.That(enabledStream.WrittenText(), Is.EqualTo("CAPA\r\nCAPA\r\nUIDL 1\r\n"));
        enabledEngine.CloseConnection();
        enabledStream.Dispose();
    }

    [Test]
    public void MultilineUidlNegativeResponseDoesNotConsumeFollowingData()
    {
        UidlSequenceStream stream = CreateAdvertisedStream(
            "-ERR identifiers unavailable\r\n" +
            "+OK 1 exact-id\r\n");
        Pop3ClientEngine engine = CreateTransactionEngine(stream, CreateOptions(false));

        Pop3CommandException exception = Assert.Throws<Pop3CommandException>(() =>
            engine.GetAllUniqueIds(CancellationToken.None))!;
        Pop3UniqueId uniqueId = engine.GetUniqueId(1, CancellationToken.None);

        Assert.That(exception.Command.ExpectsMultilineResponse, Is.True);
        Assert.That(uniqueId.Value, Is.EqualTo("exact-id"));
        Assert.That(engine.GetSnapshot().State, Is.EqualTo(Pop3SessionState.Transaction));

        engine.CloseConnection();
        stream.Dispose();
    }

    private static Pop3ClientEngine CreateTransactionEngine(
        UidlSequenceStream stream,
        Pop3ClientOptions options)
    {
        Pop3ClientEngine engine = new Pop3ClientEngine();
        engine.ConnectStream(stream, options, CancellationToken.None);
        engine.CompleteAuthentication("alice", CancellationToken.None);
        return engine;
    }

    private static UidlSequenceStream CreateAdvertisedStream(string uidlResponses)
    {
        return new UidlSequenceStream(
            "+OK ready\r\n" +
            "+OK capabilities\r\nUIDL\r\n.\r\n" +
            "+OK capabilities\r\nUIDL\r\n.\r\n" +
            uidlResponses);
    }

    private static UidlSequenceStream CreateUnadvertisedStream(string uidlResponses)
    {
        return new UidlSequenceStream(
            "+OK ready\r\n" +
            "+OK capabilities\r\n.\r\n" +
            "+OK capabilities\r\n.\r\n" +
            uidlResponses);
    }

    private static UidlSequenceStream CreateUnavailableStream(string uidlResponses)
    {
        return new UidlSequenceStream(
            "+OK ready\r\n" +
            "-ERR CAPA unavailable\r\n" +
            "-ERR CAPA unavailable\r\n" +
            uidlResponses);
    }

    private static Pop3ClientOptions CreateOptions(bool probeUidl)
    {
        Pop3ClientOptions options = new Pop3ClientOptions();
        options.ConnectTimeoutMilliseconds = 2000;
        options.ReadTimeoutMilliseconds = 2000;
        options.WriteTimeoutMilliseconds = 2000;
        options.AuthenticationTimeoutMilliseconds = 2000;
        options.BufferSize = 16;
        options.ProbeUidlWhenCapaUnavailable = probeUidl;
        options.LeaveStreamOpen = true;
        return options;
    }

    private class UidlSequenceStream : Stream
    {
        byte[] Responses;
        int ReadPosition;
        MemoryStream Written = new MemoryStream();
        public bool IsDisposed;

        public UidlSequenceStream(string responses)
        {
            Responses = Encoding.UTF8.GetBytes(responses);
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
            get { return Responses.Length; }
        }

        public override long Position
        {
            get { return ReadPosition; }
            set { throw new NotSupportedException(); }
        }

        public string WrittenText()
        {
            return Encoding.ASCII.GetString(Written.ToArray());
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
}
