namespace json.Tests;

public class JsonStreamInputConformanceTests
{
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(7)]
    public void ShortReadsMatchByteInputAcrossValidationPullAndPush(int maximumReadBytes)
    {
        string jsonText =
            "\r\n{\"😀\":\"\\uD834\\uDD1E\",\"n\":-12.5E+3," +
            "\"b\":false,\"a\":[null,true]}\r\n";
        byte[] input = System.Text.Encoding.UTF8.GetBytes(jsonText);
        JsonReaderOptions baselineOptions = CreateTrackedOptions();
        List<string> expected = ReadByteEventSignatures(input, baselineOptions);

        PatternReadStream validationStream = new PatternReadStream(
            input,
            maximumReadBytes,
            true,
            new HashSet<int>());
        JsonReaderOptions validationOptions = CreateTrackedOptions();
        JsonOperationResult validation = new JsonUtf8Validator().ValidateStream(
            validationStream,
            validationOptions);

        PatternReadStream pullStream = new PatternReadStream(
            input,
            maximumReadBytes,
            true,
            new HashSet<int>());
        List<string> pullEvents = ReadPullStreamEventSignatures(
            pullStream,
            CreateTrackedOptions());

        PatternReadStream pushStream = new PatternReadStream(
            input,
            maximumReadBytes,
            true,
            new HashSet<int>());
        List<string> pushEvents = ReadPushStreamEventSignatures(
            pushStream,
            CreateTrackedOptions());

        Assert.Multiple(() =>
        {
            Assert.That(validation.Success, Is.True);
            Assert.That(validation.BytesConsumed, Is.EqualTo(input.Length));
            Assert.That(pullEvents, Is.EqualTo(expected));
            Assert.That(pushEvents, Is.EqualTo(expected));
            Assert.That(validationStream.ReadCallCount, Is.GreaterThan(1));
            Assert.That(pullStream.ReadCallCount, Is.GreaterThan(1));
            Assert.That(pushStream.ReadCallCount, Is.GreaterThan(1));
            Assert.That(validationStream.WasDisposed, Is.False);
            Assert.That(pullStream.WasDisposed, Is.False);
            Assert.That(pushStream.WasDisposed, Is.False);
        });
    }

    [TestCase(0)]
    [TestCase(2)]
    [TestCase(5)]
    public void SeekableIntermediateZeroReadsAreRetried(int zeroReadCall)
    {
        byte[] input = System.Text.Encoding.ASCII.GetBytes(
            "{\"a\":[0,1],\"b\":true}");
        HashSet<int> zeroReads = new HashSet<int>();
        zeroReads.Add(zeroReadCall);
        PatternReadStream stream = new PatternReadStream(
            input,
            3,
            true,
            zeroReads);
        JsonReaderOptions options = new JsonReaderOptions();
        options.StreamBufferBytes = 8;

        JsonOperationResult result = new JsonUtf8Validator().ValidateStream(
            stream,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, "Zero read call " + zeroReadCall);
            Assert.That(result.BytesConsumed, Is.EqualTo(input.Length));
            Assert.That(stream.Position, Is.EqualTo(input.Length));
            Assert.That(stream.WasDisposed, Is.False);
        });
    }

    [Test]
    public void TokenMuchLargerThanTheStreamBufferSucceedsInEveryFacade()
    {
        string payload = new string('x', 50_000) + "€";
        byte[] input = System.Text.Encoding.UTF8.GetBytes("\"" + payload + "\"");
        JsonReaderOptions validationOptions = new JsonReaderOptions();
        validationOptions.StreamBufferBytes = 17;
        PatternReadStream validationStream = new PatternReadStream(
            input,
            11,
            false,
            new HashSet<int>());
        JsonOperationResult validation = new JsonUtf8Validator().ValidateStream(
            validationStream,
            validationOptions);

        JsonReaderOptions pullOptions = new JsonReaderOptions();
        pullOptions.StreamBufferBytes = 17;
        PatternReadStream pullStream = new PatternReadStream(
            input,
            11,
            false,
            new HashSet<int>());
        IJsonPullReader pull = new JsonPullReader(pullOptions);
        JsonFeedResult pullFeed = pull.FeedStream(pullStream);
        JsonTokenResult pullToken = pull.ReadToken();
        JsonTokenResult pullBoundary = pull.ReadToken();
        JsonTokenResult pullComplete = pull.ReadToken();

        JsonReaderOptions pushOptions = new JsonReaderOptions();
        pushOptions.StreamBufferBytes = 17;
        PatternReadStream pushStream = new PatternReadStream(
            input,
            11,
            false,
            new HashSet<int>());
        CollectingHandler handler = new CollectingHandler();
        IJsonPushParser push = new JsonPushParser(pushOptions, handler);
        JsonFeedResult pushResult = push.FeedPushStream(pushStream);

        Assert.Multiple(() =>
        {
            Assert.That(validation.Success, Is.True);
            Assert.That(pullFeed.ReadStatus, Is.EqualTo(JsonReadStatus.TokenAvailable));
            Assert.That(pullToken.Token?.Kind, Is.EqualTo(JsonTokenKind.String));
            Assert.That(pullToken.Token?.StringValue, Is.EqualTo(payload));
            Assert.That(pullBoundary.Status, Is.EqualTo(JsonReadStatus.DocumentComplete));
            Assert.That(pullComplete.Status, Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.That(pushResult.ReadStatus, Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.That(handler.Tokens[0].StringValue, Is.EqualTo(payload));
            Assert.That(handler.Tokens[1].Kind, Is.EqualTo(JsonTokenKind.DocumentBoundary));
            Assert.That(validationStream.WasDisposed, Is.False);
            Assert.That(pullStream.WasDisposed, Is.False);
            Assert.That(pushStream.WasDisposed, Is.False);
        });
    }

    [Test]
    public void TokenLimitFailureIsConsistentAcrossStreamRefills()
    {
        byte[] input = System.Text.Encoding.ASCII.GetBytes(
            "\"" + new string('x', 100) + "\"");
        JsonLimits limits = new JsonLimits();
        limits.MaximumTokenBytes = 64;
        JsonReaderOptions options = new JsonReaderOptions();
        options.StreamBufferBytes = 3;
        options.Limits = limits;

        PatternReadStream validationStream = new PatternReadStream(
            input,
            2,
            false,
            new HashSet<int>());
        JsonOperationResult validation = new JsonUtf8Validator().ValidateStream(
            validationStream,
            options);

        PatternReadStream pullStream = new PatternReadStream(
            input,
            2,
            false,
            new HashSet<int>());
        IJsonPullReader pull = new JsonPullReader(options);
        JsonFeedResult pullFailure = pull.FeedStream(pullStream);

        PatternReadStream pushStream = new PatternReadStream(
            input,
            2,
            false,
            new HashSet<int>());
        CollectingHandler handler = new CollectingHandler();
        IJsonPushParser push = new JsonPushParser(options, handler);
        JsonFeedResult pushFailure = push.FeedPushStream(pushStream);

        Assert.Multiple(() =>
        {
            Assert.That(validation.Success, Is.False);
            Assert.That(validation.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.TokenByteLimitExceeded));
            Assert.That(pullFailure.ReadStatus, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(pullFailure.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.TokenByteLimitExceeded));
            Assert.That(pushFailure.ReadStatus, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(pushFailure.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.TokenByteLimitExceeded));
            Assert.That(handler.Tokens, Is.Empty);
            Assert.That(validationStream.WasDisposed, Is.False);
            Assert.That(pullStream.WasDisposed, Is.False);
            Assert.That(pushStream.WasDisposed, Is.False);
        });
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(7)]
    [TestCase(11)]
    public void RefillBoundariesPreserveLocationsPayloadsAndFraming(int bufferBytes)
    {
        string jsonText =
            "\r\nnull [\"😀\",\"\\uD834\\uDD1E\",-12.5E+3,true] " +
            "{\"x\":false}\r\n";
        byte[] input = System.Text.Encoding.UTF8.GetBytes(jsonText);
        JsonReaderOptions baselineOptions = CreateTrackedOptions();
        baselineOptions.InputMode = JsonInputMode.WhitespaceSeparated;
        List<string> expected = ReadByteEventSignatures(input, baselineOptions);

        JsonReaderOptions pullOptions = CreateTrackedOptions();
        pullOptions.InputMode = JsonInputMode.WhitespaceSeparated;
        pullOptions.StreamBufferBytes = bufferBytes;
        PatternReadStream pullStream = new PatternReadStream(
            input,
            int.MaxValue,
            true,
            new HashSet<int>());
        List<string> pullEvents = ReadPullStreamEventSignatures(
            pullStream,
            pullOptions);

        JsonReaderOptions pushOptions = CreateTrackedOptions();
        pushOptions.InputMode = JsonInputMode.WhitespaceSeparated;
        pushOptions.StreamBufferBytes = bufferBytes;
        PatternReadStream pushStream = new PatternReadStream(
            input,
            int.MaxValue,
            true,
            new HashSet<int>());
        List<string> pushEvents = ReadPushStreamEventSignatures(
            pushStream,
            pushOptions);

        Assert.Multiple(() =>
        {
            Assert.That(pullEvents, Is.EqualTo(expected), "Pull buffer " + bufferBytes);
            Assert.That(pushEvents, Is.EqualTo(expected), "Push buffer " + bufferBytes);
            Assert.That(pullStream.Position, Is.EqualTo(input.Length));
            Assert.That(pushStream.Position, Is.EqualTo(input.Length));
        });
    }

    [TestCase(0)]
    [TestCase(3)]
    [TestCase(7)]
    public void ValidatorIoFailuresReportConsumedPrefixAndPreserveOwnership(int failureOffset)
    {
        byte[] input = System.Text.Encoding.ASCII.GetBytes("[0,1,2]");
        FaultingReadStream stream = new FaultingReadStream(input, failureOffset, 3);
        JsonReaderOptions options = new JsonReaderOptions();
        options.StreamBufferBytes = 4;

        JsonOperationResult result = new JsonUtf8Validator().ValidateStream(
            stream,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.IoFailure));
            Assert.That(result.BytesConsumed, Is.EqualTo(failureOffset));
            Assert.That(stream.WasDisposed, Is.False);
        });
    }

    [Test]
    public void PushIoFailureLatchesUntilResetWithoutClosingEitherStream()
    {
        byte[] input = System.Text.Encoding.ASCII.GetBytes("[0,1]");
        FaultingReadStream failedStream = new FaultingReadStream(input, 3, 2);
        JsonReaderOptions options = new JsonReaderOptions();
        options.StreamBufferBytes = 2;
        CollectingHandler handler = new CollectingHandler();
        IJsonPushParser parser = new JsonPushParser(options, handler);

        JsonFeedResult failure = parser.FeedPushStream(failedStream);
        JsonFeedResult repeated = parser.CompletePushInput();

        parser.ResetPushParser();
        handler.Tokens.Clear();
        TrackingMemoryStream validStream = new TrackingMemoryStream(
            System.Text.Encoding.ASCII.GetBytes("{}"));
        JsonFeedResult valid = parser.FeedPushStream(validStream);

        Assert.Multiple(() =>
        {
            Assert.That(failure.ReadStatus, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(failure.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.IoFailure));
            Assert.That(repeated.ReadStatus, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(repeated.Diagnostic, Is.SameAs(failure.Diagnostic));
            Assert.That(failedStream.WasDisposed, Is.False);
            Assert.That(valid.ReadStatus, Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.That(handler.Tokens.Select(token => token.Kind), Is.EqualTo(new[]
            {
                JsonTokenKind.StartObject,
                JsonTokenKind.EndObject,
                JsonTokenKind.DocumentBoundary
            }));
            Assert.That(validStream.WasDisposed, Is.False);
        });
    }

    [Test]
    public void StopAndResetNeverCloseOrContinueReadingTheCallerStream()
    {
        byte[] input = System.Text.Encoding.ASCII.GetBytes("[0,1,2,3]");
        PatternReadStream stream = new PatternReadStream(
            input,
            2,
            false,
            new HashSet<int>());
        JsonReaderOptions options = new JsonReaderOptions();
        options.StreamBufferBytes = 2;
        CollectingHandler handler = new CollectingHandler();
        handler.StopAtInvocation = 0;
        IJsonPushParser parser = new JsonPushParser(options, handler);

        JsonFeedResult stopped = parser.FeedPushStream(stream);
        long positionAtStop = stream.Position;
        parser.ResetPushParser();

        Assert.Multiple(() =>
        {
            Assert.That(stopped.ReadStatus, Is.EqualTo(JsonReadStatus.Stopped));
            Assert.That(positionAtStop, Is.LessThan(input.Length));
            Assert.That(stream.Position, Is.EqualTo(positionAtStop));
            Assert.That(stream.WasDisposed, Is.False);
            Assert.That(stream.CanRead, Is.True);
        });
    }

    static JsonReaderOptions CreateTrackedOptions()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.CaptureRawTokens = true;
        options.TrackLineInformation = true;
        options.TrackUnicodeScalarColumn = true;
        options.StreamBufferBytes = 13;
        return options;
    }

    static List<string> ReadByteEventSignatures(
        byte[] input,
        JsonReaderOptions options)
    {
        IJsonPullReader reader = new JsonPullReader(options);
        reader.FeedUtf8(input);
        reader.CompleteInput();
        return DrainPullEvents(reader);
    }

    static List<string> ReadPullStreamEventSignatures(
        Stream stream,
        JsonReaderOptions options)
    {
        IJsonPullReader reader = new JsonPullReader(options);
        reader.FeedStream(stream);
        return DrainPullEvents(reader);
    }

    static List<string> DrainPullEvents(IJsonPullReader reader)
    {
        List<string> events = new List<string>();
        while (true)
        {
            JsonTokenResult read = reader.ReadToken();
            if (read.Status == JsonReadStatus.TokenAvailable)
            {
                JsonToken token = read.Token
                    ?? throw new InvalidOperationException("Expected a token.");
                events.Add(CreateTokenSignature(token));
                continue;
            }

            if (read.Status == JsonReadStatus.DocumentComplete)
            {
                events.Add("D");
                continue;
            }

            if (read.Status == JsonReadStatus.StreamComplete)
            {
                return events;
            }

            throw new InvalidOperationException(
                "Unexpected pull status: " + read.Status);
        }
    }

    static List<string> ReadPushStreamEventSignatures(
        Stream stream,
        JsonReaderOptions options)
    {
        CollectingHandler handler = new CollectingHandler();
        IJsonPushParser parser = new JsonPushParser(options, handler);
        JsonFeedResult result = parser.FeedPushStream(stream);
        Assert.That(result.ReadStatus, Is.EqualTo(JsonReadStatus.StreamComplete));
        return handler.Tokens.Select(token =>
            token.Kind == JsonTokenKind.DocumentBoundary
                ? "D"
                : CreateTokenSignature(token)).ToList();
    }

    static string CreateTokenSignature(JsonToken token)
    {
        string raw = token.RawUtf8 == null
            ? "<null>"
            : Convert.ToHexString(token.RawUtf8);
        return token.Kind + "|" +
            (token.StringValue ?? "<null>") + "|" +
            (token.Number?.Lexeme ?? "<null>") + "|" +
            token.BooleanValue + "|" +
            token.Depth + "|" +
            (token.Pointer?.OriginalRepresentation ?? "<null>") + "|" +
            raw + "|" +
            FormatPosition(token.Range?.Start) + "|" +
            FormatPosition(token.Range?.End);
    }

    static string FormatPosition(JsonSourcePosition? position)
    {
        if (position == null)
        {
            return "<null>";
        }

        return position.ByteOffset + ":" +
            position.Line + ":" +
            position.ByteColumn + ":" +
            position.ScalarColumn;
    }

    class PatternReadStream : Stream
    {
        byte[] bytes;
        int maximumReadBytes;
        bool seekable;
        HashSet<int> zeroReadCalls;
        int position;

        internal bool WasDisposed;
        internal int ReadCallCount;

        internal PatternReadStream(
            byte[] bytes,
            int maximumReadBytes,
            bool seekable,
            HashSet<int> zeroReadCalls)
        {
            this.bytes = bytes;
            this.maximumReadBytes = maximumReadBytes;
            this.seekable = seekable;
            this.zeroReadCalls = zeroReadCalls;
        }

        public override bool CanRead => true;

        public override bool CanSeek => seekable;

        public override bool CanWrite => false;

        public override long Length => bytes.Length;

        public override long Position
        {
            get => position;
            set
            {
                if (!seekable)
                {
                    throw new NotSupportedException();
                }

                position = (int)value;
            }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int call = ReadCallCount;
            ReadCallCount++;
            if (zeroReadCalls.Contains(call))
            {
                return 0;
            }

            int remaining = bytes.Length - position;
            if (remaining == 0)
            {
                return 0;
            }

            int length = count;
            if (length > maximumReadBytes)
            {
                length = maximumReadBytes;
            }

            if (length > remaining)
            {
                length = remaining;
            }

            Array.Copy(bytes, position, buffer, offset, length);
            position += length;
            return length;
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
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }

    class FaultingReadStream : Stream
    {
        byte[] bytes;
        int failureOffset;
        int maximumReadBytes;
        int position;

        internal bool WasDisposed;

        internal FaultingReadStream(
            byte[] bytes,
            int failureOffset,
            int maximumReadBytes)
        {
            this.bytes = bytes;
            this.failureOffset = failureOffset;
            this.maximumReadBytes = maximumReadBytes;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => bytes.Length;

        public override long Position
        {
            get => position;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (position >= failureOffset)
            {
                throw new IOException("Injected stream failure.");
            }

            int remainingBeforeFailure = failureOffset - position;
            int length = count;
            if (length > maximumReadBytes)
            {
                length = maximumReadBytes;
            }

            if (length > remainingBeforeFailure)
            {
                length = remainingBeforeFailure;
            }

            Array.Copy(bytes, position, buffer, offset, length);
            position += length;
            return length;
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
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }

    class TrackingMemoryStream : MemoryStream
    {
        internal bool WasDisposed;

        internal TrackingMemoryStream(byte[] bytes) : base(bytes)
        {
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }

    class CollectingHandler : IJsonTokenHandler
    {
        internal List<JsonToken> Tokens = new List<JsonToken>();
        internal int StopAtInvocation = -1;

        public JsonHandlerAction HandleToken(JsonToken token)
        {
            int invocation = Tokens.Count;
            Tokens.Add(token);
            if (invocation == StopAtInvocation)
            {
                return JsonHandlerAction.Stop;
            }

            return JsonHandlerAction.Continue;
        }
    }
}
