namespace json.Tests;

public class JsonReuseOwnershipTests
{
    [Test]
    public void PullResetClearsBufferedInputPathLocationAndDiagnostic()
    {
        JsonReaderOptions options = CreateTrackedOptions();
        IJsonPullReader reader = new JsonPullReader(options);
        byte[] invalid = System.Text.Encoding.UTF8.GetBytes(
            "{\n\"old\":[0,]} trailing");

        reader.FeedUtf8(invalid);
        JsonTokenResult failure = ReadPullFailure(reader);

        Assert.Multiple(() =>
        {
            Assert.That(failure.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.TrailingComma));
            Assert.That(reader.GetBytesAccepted(), Is.EqualTo(invalid.Length));
            Assert.That(reader.GetBytesConsumed(), Is.LessThan(invalid.Length));
        });

        reader.ResetReader();
        JsonTokenResult waiting = reader.PeekToken();
        byte[] valid = System.Text.Encoding.UTF8.GetBytes(
            "[\r\n{\"new\":1}]");
        reader.FeedUtf8(valid);
        reader.CompleteInput();
        List<JsonToken> tokens = ReadPullCompletion(reader);
        JsonToken number = tokens.Single(token => token.Kind == JsonTokenKind.Number);

        Assert.Multiple(() =>
        {
            Assert.That(waiting.Status, Is.EqualTo(JsonReadStatus.NeedMoreData));
            Assert.That(waiting.Diagnostic, Is.Null);
            Assert.That(number.Number?.Lexeme, Is.EqualTo("1"));
            Assert.That(number.Pointer?.OriginalRepresentation, Is.EqualTo("/0/new"));
            Assert.That(number.Depth, Is.EqualTo(2));
            Assert.That(number.Range?.Start?.ByteOffset, Is.EqualTo(10));
            Assert.That(number.Range?.Start?.Line, Is.EqualTo(2));
            Assert.That(number.Range?.Start?.ByteColumn, Is.EqualTo(8));
            Assert.That(reader.GetBytesAccepted(), Is.EqualTo(valid.Length));
            Assert.That(reader.GetBytesConsumed(), Is.EqualTo(valid.Length));
        });
    }

    [Test]
    public void PushResetClearsBufferedInputPathLocationAndDiagnostic()
    {
        JsonReaderOptions options = CreateTrackedOptions();
        ReuseTokenHandler handler = new ReuseTokenHandler();
        IJsonPushParser parser = new JsonPushParser(options, handler);
        byte[] invalid = System.Text.Encoding.UTF8.GetBytes(
            "{\n\"old\":[0,]} trailing");

        JsonFeedResult failure = parser.FeedPushUtf8(invalid);

        Assert.Multiple(() =>
        {
            Assert.That(failure.ReadStatus, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(failure.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.TrailingComma));
            Assert.That(parser.GetPushBytesAccepted(), Is.EqualTo(invalid.Length));
            Assert.That(parser.GetPushBytesConsumed(), Is.LessThan(invalid.Length));
        });

        parser.ResetPushParser();
        handler.Tokens.Clear();
        byte[] valid = System.Text.Encoding.UTF8.GetBytes(
            "[\r\n{\"new\":1}]");
        JsonFeedResult feed = parser.FeedPushUtf8(valid);
        JsonFeedResult completed = parser.CompletePushInput();
        JsonToken number = handler.Tokens.Single(
            token => token.Kind == JsonTokenKind.Number);

        Assert.Multiple(() =>
        {
            Assert.That(feed.Diagnostic, Is.Null);
            Assert.That(completed.ReadStatus, Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.That(completed.Diagnostic, Is.Null);
            Assert.That(number.Number?.Lexeme, Is.EqualTo("1"));
            Assert.That(number.Pointer?.OriginalRepresentation, Is.EqualTo("/0/new"));
            Assert.That(number.Depth, Is.EqualTo(2));
            Assert.That(number.Range?.Start?.ByteOffset, Is.EqualTo(10));
            Assert.That(number.Range?.Start?.Line, Is.EqualTo(2));
            Assert.That(parser.GetPushBytesAccepted(), Is.EqualTo(valid.Length));
            Assert.That(parser.GetPushBytesConsumed(), Is.EqualTo(valid.Length));
        });
    }

    [Test]
    public void ParserResetDetachesPartiallyReadStreamsWithoutClosingThem()
    {
        JsonReaderOptions options = CreateTrackedOptions();
        options.StreamBufferBytes = 1;
        TrackingMemoryStream abandonedPullStream = new TrackingMemoryStream(
            System.Text.Encoding.UTF8.GetBytes("[0,1]"));
        IJsonPullReader reader = new JsonPullReader(options);

        JsonFeedResult pullStopped = reader.FeedStream(abandonedPullStream);
        long pullPosition = abandonedPullStream.Position;
        reader.ResetReader();
        TrackingMemoryStream validPullStream = new TrackingMemoryStream(
            System.Text.Encoding.UTF8.GetBytes("null"));
        JsonFeedResult pullReused = reader.FeedStream(validPullStream);
        List<JsonToken> pullTokens = ReadPullCompletion(reader);

        ReuseTokenHandler handler = new ReuseTokenHandler();
        handler.StopAtInvocation = 0;
        TrackingMemoryStream abandonedPushStream = new TrackingMemoryStream(
            System.Text.Encoding.UTF8.GetBytes("[0,1]"));
        IJsonPushParser parser = new JsonPushParser(options, handler);
        JsonFeedResult pushStopped = parser.FeedPushStream(abandonedPushStream);
        long pushPosition = abandonedPushStream.Position;
        parser.ResetPushParser();
        handler.Tokens.Clear();
        handler.StopAtInvocation = -1;
        TrackingMemoryStream validPushStream = new TrackingMemoryStream(
            System.Text.Encoding.UTF8.GetBytes("true"));
        JsonFeedResult pushReused = parser.FeedPushStream(validPushStream);

        Assert.Multiple(() =>
        {
            Assert.That(pullStopped.ReadStatus,
                Is.EqualTo(JsonReadStatus.TokenAvailable));
            Assert.That(pullPosition, Is.LessThan(abandonedPullStream.Length));
            Assert.That(abandonedPullStream.Position, Is.EqualTo(pullPosition));
            Assert.That(abandonedPullStream.WasDisposed, Is.False);
            Assert.That(abandonedPullStream.CanRead, Is.True);
            Assert.That(pullReused.Diagnostic, Is.Null);
            Assert.That(pullTokens.Select(token => token.Kind),
                Is.EqualTo(new[] { JsonTokenKind.Null }));
            Assert.That(validPullStream.WasDisposed, Is.False);
            Assert.That(pushStopped.ReadStatus, Is.EqualTo(JsonReadStatus.Stopped));
            Assert.That(pushPosition, Is.LessThan(abandonedPushStream.Length));
            Assert.That(abandonedPushStream.Position, Is.EqualTo(pushPosition));
            Assert.That(abandonedPushStream.WasDisposed, Is.False);
            Assert.That(abandonedPushStream.CanRead, Is.True);
            Assert.That(pushReused.ReadStatus, Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.That(handler.Tokens.Select(token => token.Kind), Is.EqualTo(new[]
            {
                JsonTokenKind.Boolean,
                JsonTokenKind.DocumentBoundary
            }));
            Assert.That(validPushStream.WasDisposed, Is.False);
        });
    }

    [Test]
    public void WriterResetClearsFailureStateAndPendingBytesWithoutClosingStream()
    {
        FailOnceWriteStream stream = new FailOnceWriteStream();
        JsonWriterTarget target = new JsonWriterTarget();
        target.Stream = stream;
        IJsonTokenWriter writer = new JsonUtf8Writer(
            new JsonWriterOptions(),
            target);

        AssertSuccess(writer.WriteStartObject());
        AssertSuccess(writer.WritePropertyName("old"));
        AssertSuccess(writer.WriteStartArray());
        JsonOperationResult failure = writer.FlushWriter();
        JsonOperationResult repeated = writer.FlushWriter();

        Assert.Multiple(() =>
        {
            Assert.That(failure.Success, Is.False);
            Assert.That(failure.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.IoFailure));
            Assert.That(repeated.Diagnostic, Is.SameAs(failure.Diagnostic));
            Assert.That(writer.GetBytesPending(), Is.GreaterThan(0));
            Assert.That(writer.GetBytesCommitted(), Is.Zero);
            Assert.That(stream.WasDisposed, Is.False);
        });

        writer.ResetWriter();
        Assert.Multiple(() =>
        {
            Assert.That(writer.GetBytesPending(), Is.Zero);
            Assert.That(writer.GetBytesCommitted(), Is.Zero);
        });

        AssertSuccess(writer.WriteStartObject());
        AssertSuccess(writer.WritePropertyName("new"));
        JsonOperationResult value = writer.WriteBoolean(true);
        AssertSuccess(value);
        AssertSuccess(writer.WriteEndObject());
        JsonOperationResult completed = writer.CompleteWriter();

        Assert.Multiple(() =>
        {
            Assert.That(value.Diagnostic, Is.Null);
            Assert.That(completed.Success, Is.True);
            Assert.That(completed.Diagnostic, Is.Null);
            Assert.That(System.Text.Encoding.UTF8.GetString(stream.ToArray()),
                Is.EqualTo("{\"new\":true}"));
            Assert.That(writer.GetBytesPending(), Is.Zero);
            Assert.That(writer.GetBytesCommitted(), Is.EqualTo(stream.Length));
            Assert.That(stream.WasDisposed, Is.False);
            Assert.That(stream.CanWrite, Is.True);
        });

        writer.ResetWriter();
        Assert.That(stream.WasDisposed, Is.False);
    }

    static JsonReaderOptions CreateTrackedOptions()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.CaptureRawTokens = true;
        options.TrackLineInformation = true;
        options.TrackUnicodeScalarColumn = true;
        return options;
    }

    static JsonTokenResult ReadPullFailure(IJsonPullReader reader)
    {
        int attempts = 0;
        while (true)
        {
            attempts++;
            Assert.That(attempts, Is.LessThan(100));
            JsonTokenResult read = reader.ReadToken();
            if (read.Status == JsonReadStatus.Error)
            {
                return read;
            }

            Assert.That(read.Status, Is.EqualTo(JsonReadStatus.TokenAvailable));
        }
    }

    static List<JsonToken> ReadPullCompletion(IJsonPullReader reader)
    {
        List<JsonToken> tokens = new List<JsonToken>();
        int attempts = 0;
        while (true)
        {
            attempts++;
            Assert.That(attempts, Is.LessThan(100));
            JsonTokenResult read = reader.ReadToken();
            if (read.Status == JsonReadStatus.TokenAvailable)
            {
                tokens.Add(read.Token
                    ?? throw new InvalidOperationException("Expected token."));
                continue;
            }

            if (read.Status == JsonReadStatus.DocumentComplete)
            {
                continue;
            }

            if (read.Status == JsonReadStatus.StreamComplete)
            {
                return tokens;
            }

            Assert.Fail("Unexpected pull status: " + read.Status);
        }
    }

    static void AssertSuccess(JsonOperationResult result)
    {
        Assert.That(result.Success, Is.True, result.Diagnostic?.Message);
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

    class FailOnceWriteStream : MemoryStream
    {
        bool failNextWrite = true;

        internal bool WasDisposed;

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (failNextWrite)
            {
                failNextWrite = false;
                throw new IOException("Injected write failure.");
            }

            base.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }

    class ReuseTokenHandler : IJsonTokenHandler
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
