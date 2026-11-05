namespace json.Tests;

public class JsonStreamInputTests
{
    [Test]
    public void ValidatorReadsBufferedStreamWithoutClosingIt()
    {
        byte[] input = System.Text.Encoding.UTF8.GetBytes(
            "{\"text\":\"€ and a token longer than the buffer\"}");
        TrackingMemoryStream stream = new TrackingMemoryStream(input);
        JsonReaderOptions options = new JsonReaderOptions();
        options.StreamBufferBytes = 3;
        IJsonUtf8Validator validator = new JsonUtf8Validator();

        JsonOperationResult result = validator.ValidateStream(stream, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.BytesConsumed, Is.EqualTo(input.Length));
            Assert.That(stream.Position, Is.EqualTo(input.Length));
            Assert.That(stream.WasDisposed, Is.False);
            Assert.That(stream.CanRead, Is.True);
        });
    }

    [Test]
    public void ValidatorReturnsParserDiagnosticsForInvalidStreamContent()
    {
        byte[] input = System.Text.Encoding.ASCII.GetBytes("[0,]");
        TrackingMemoryStream stream = new TrackingMemoryStream(input);
        JsonReaderOptions options = new JsonReaderOptions();
        options.StreamBufferBytes = 1;
        IJsonUtf8Validator validator = new JsonUtf8Validator();

        JsonOperationResult result = validator.ValidateStream(stream, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.TrailingComma));
            Assert.That(result.BytesConsumed, Is.EqualTo(input.Length));
            Assert.That(stream.WasDisposed, Is.False);
        });
    }

    [Test]
    public void PullReaderRefillsStreamAcrossTokensAndCompletesAtEndOfStream()
    {
        byte[] input = System.Text.Encoding.UTF8.GetBytes(
            "[\"€\",-12.5e2,true,null]");
        TrackingMemoryStream stream = new TrackingMemoryStream(input);
        JsonReaderOptions options = new JsonReaderOptions();
        options.StreamBufferBytes = 2;
        IJsonPullReader reader = new JsonPullReader(options);

        JsonFeedResult attached = reader.FeedStream(stream);
        List<JsonToken> tokens = new List<JsonToken>();
        JsonReadStatus finalStatus = JsonReadStatus.NeedMoreData;
        while (finalStatus != JsonReadStatus.StreamComplete)
        {
            JsonTokenResult read = reader.ReadToken();
            finalStatus = read.Status;
            if (read.Token != null)
            {
                tokens.Add(read.Token);
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(attached.ReadStatus, Is.EqualTo(JsonReadStatus.TokenAvailable));
            Assert.That(tokens.Select(token => token.Kind), Is.EqualTo(new[]
            {
                JsonTokenKind.StartArray,
                JsonTokenKind.String,
                JsonTokenKind.Number,
                JsonTokenKind.Boolean,
                JsonTokenKind.Null,
                JsonTokenKind.EndArray
            }));
            Assert.That(tokens[1].StringValue, Is.EqualTo("€"));
            Assert.That(tokens[2].Number?.Lexeme, Is.EqualTo("-12.5e2"));
            Assert.That(reader.GetBytesAccepted(), Is.EqualTo(input.Length));
            Assert.That(reader.GetBytesConsumed(), Is.EqualTo(input.Length));
            Assert.That(stream.WasDisposed, Is.False);
        });
    }

    [Test]
    public void PullCaptureRefillsACompleteSubtreeAndPreservesContinuation()
    {
        byte[] input = System.Text.Encoding.ASCII.GetBytes(
            "[{\"a\":[1,2]},3]");
        TrackingMemoryStream stream = new TrackingMemoryStream(input);
        JsonReaderOptions options = new JsonReaderOptions();
        options.StreamBufferBytes = 2;
        IJsonPullReader reader = new JsonPullReader(options);

        reader.FeedStream(stream);
        Assert.That(reader.ReadToken().Token?.Kind, Is.EqualTo(JsonTokenKind.StartArray));
        JsonValueResult captured = reader.CaptureValue();
        JsonTokenResult next = reader.ReadToken();

        Assert.Multiple(() =>
        {
            Assert.That(captured.Success, Is.True);
            Assert.That(captured.Value?.Kind, Is.EqualTo(JsonValueKind.Object));
            Assert.That(captured.Value?.ObjectMembers?[0].Name, Is.EqualTo("a"));
            Assert.That(captured.Value?.ObjectMembers?[0].Value?.ArrayItems, Has.Count.EqualTo(2));
            Assert.That(next.Token?.Number?.Lexeme, Is.EqualTo("3"));
            Assert.That(next.Token?.Pointer?.OriginalRepresentation, Is.EqualTo("/1"));
            Assert.That(stream.WasDisposed, Is.False);
        });
    }

    [Test]
    public void PullStreamPublishesARecoverableSequenceFailureOnlyOnce()
    {
        byte[] input = new byte[]
        {
            JsonSyntaxBytes.RecordSeparator,
            JsonSyntaxBytes.BeginArray,
            JsonSyntaxBytes.RecordSeparator,
            JsonSyntaxBytes.BeginObject,
            JsonSyntaxBytes.EndObject,
            JsonSyntaxBytes.LineFeed
        };
        TrackingMemoryStream stream = new TrackingMemoryStream(input);
        JsonReaderOptions options = new JsonReaderOptions();
        options.InputMode = JsonInputMode.JsonTextSequence;
        options.SequenceErrorPolicy = JsonSequenceErrorPolicy.Resynchronize;
        options.StreamBufferBytes = 1;
        IJsonPullReader reader = new JsonPullReader(options);

        reader.FeedStream(stream);
        JsonTokenResult startArray = reader.ReadToken();
        JsonTokenResult failure = reader.ReadToken();
        JsonTokenResult startObject = reader.ReadToken();
        JsonTokenResult endObject = reader.ReadToken();
        JsonTokenResult boundary = reader.ReadToken();
        JsonTokenResult complete = reader.ReadToken();

        Assert.Multiple(() =>
        {
            Assert.That(startArray.Token?.Kind, Is.EqualTo(JsonTokenKind.StartArray));
            Assert.That(failure.Status, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(failure.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.IncompleteSequenceRecord));
            Assert.That(startObject.Token?.Kind, Is.EqualTo(JsonTokenKind.StartObject));
            Assert.That(endObject.Token?.Kind, Is.EqualTo(JsonTokenKind.EndObject));
            Assert.That(boundary.Status, Is.EqualTo(JsonReadStatus.DocumentComplete));
            Assert.That(complete.Status, Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.That(stream.WasDisposed, Is.False);
        });
    }

    [Test]
    public void PushParserReadsStreamToCompletionAndPublishesBoundaries()
    {
        byte[] input = System.Text.Encoding.ASCII.GetBytes("[0,true]");
        TrackingMemoryStream stream = new TrackingMemoryStream(input);
        JsonLimits limits = new JsonLimits();
        limits.MaximumInputBufferBytes = 2;
        JsonReaderOptions options = new JsonReaderOptions();
        options.StreamBufferBytes = 64;
        options.Limits = limits;
        StreamTokenHandler handler = new StreamTokenHandler();
        IJsonPushParser parser = new JsonPushParser(options, handler);

        JsonFeedResult result = parser.FeedPushStream(stream);

        Assert.Multiple(() =>
        {
            Assert.That(result.ReadStatus, Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.That(result.BytesAccepted, Is.EqualTo(input.Length));
            Assert.That(result.BytesConsumed, Is.EqualTo(input.Length));
            Assert.That(handler.Tokens.Select(token => token.Kind), Is.EqualTo(new[]
            {
                JsonTokenKind.StartArray,
                JsonTokenKind.Number,
                JsonTokenKind.Boolean,
                JsonTokenKind.EndArray,
                JsonTokenKind.DocumentBoundary
            }));
            Assert.That(parser.GetPushBytesAccepted(), Is.EqualTo(input.Length));
            Assert.That(parser.GetPushBytesConsumed(), Is.EqualTo(input.Length));
            Assert.That(stream.WasDisposed, Is.False);
        });
    }

    [Test]
    public void PushResumeContinuesReadingTheAttachedStreamAfterAPause()
    {
        byte[] input = System.Text.Encoding.ASCII.GetBytes("[0,1]");
        TrackingMemoryStream stream = new TrackingMemoryStream(input);
        JsonReaderOptions options = new JsonReaderOptions();
        options.StreamBufferBytes = 2;
        StreamTokenHandler handler = new StreamTokenHandler();
        handler.PauseAtInvocation = 0;
        IJsonPushParser parser = new JsonPushParser(options, handler);

        JsonFeedResult paused = parser.FeedPushStream(stream);
        JsonFeedResult resumed = parser.ResumePushParser();

        Assert.Multiple(() =>
        {
            Assert.That(paused.ReadStatus, Is.EqualTo(JsonReadStatus.Paused));
            Assert.That(resumed.ReadStatus, Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.That(
                resumed.BytesConsumed,
                Is.EqualTo(input.Length - paused.BytesConsumed));
            Assert.That(handler.Tokens.Select(token => token.Kind), Is.EqualTo(new[]
            {
                JsonTokenKind.StartArray,
                JsonTokenKind.Number,
                JsonTokenKind.Number,
                JsonTokenKind.EndArray,
                JsonTokenKind.DocumentBoundary
            }));
            Assert.That(handler.Tokens.Where(token => token.Kind == JsonTokenKind.Number)
                .Select(token => token.Number?.Lexeme), Is.EqualTo(new[] { "0", "1" }));
            Assert.That(parser.GetPushBytesAccepted(), Is.EqualTo(input.Length));
            Assert.That(parser.GetPushBytesConsumed(), Is.EqualTo(input.Length));
            Assert.That(stream.WasDisposed, Is.False);
        });
    }

    [Test]
    public void PullStreamIoFailureIsDiagnosticAndResetRestoresManualInput()
    {
        byte[] input = System.Text.Encoding.ASCII.GetBytes("[0]");
        FailingReadStream stream = new FailingReadStream(input, 1);
        JsonReaderOptions options = new JsonReaderOptions();
        options.StreamBufferBytes = 1;
        IJsonPullReader reader = new JsonPullReader(options);

        JsonFeedResult attached = reader.FeedStream(stream);
        JsonTokenResult start = reader.ReadToken();
        JsonTokenResult failure = reader.ReadToken();
        JsonTokenResult repeated = reader.PeekToken();

        Assert.Multiple(() =>
        {
            Assert.That(attached.ReadStatus, Is.EqualTo(JsonReadStatus.TokenAvailable));
            Assert.That(start.Token?.Kind, Is.EqualTo(JsonTokenKind.StartArray));
            Assert.That(failure.Status, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(failure.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.IoFailure));
            Assert.That(repeated.Diagnostic, Is.SameAs(failure.Diagnostic));
            Assert.That(stream.WasDisposed, Is.False);
        });

        reader.ResetReader();
        JsonFeedResult valid = reader.FeedUtf8(
            System.Text.Encoding.ASCII.GetBytes("null "));
        Assert.Multiple(() =>
        {
            Assert.That(valid.ReadStatus, Is.EqualTo(JsonReadStatus.TokenAvailable));
            Assert.That(reader.GetBytesAccepted(), Is.EqualTo(5));
            Assert.That(reader.ReadToken().Token?.Kind, Is.EqualTo(JsonTokenKind.Null));
        });
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

    class FailingReadStream : Stream
    {
        byte[] bytes;
        int failureOffset;
        int position;

        internal bool WasDisposed;

        internal FailingReadStream(byte[] bytes, int failureOffset)
        {
            this.bytes = bytes;
            this.failureOffset = failureOffset;
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
                throw new IOException("Injected read failure.");
            }

            int available = failureOffset - position;
            int length = count < available ? count : available;
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

    class StreamTokenHandler : IJsonTokenHandler
    {
        internal List<JsonToken> Tokens = new List<JsonToken>();
        internal int PauseAtInvocation = -1;

        public JsonHandlerAction HandleToken(JsonToken token)
        {
            int invocation = Tokens.Count;
            Tokens.Add(token);
            if (invocation == PauseAtInvocation)
            {
                return JsonHandlerAction.Pause;
            }

            return JsonHandlerAction.Continue;
        }
    }
}
