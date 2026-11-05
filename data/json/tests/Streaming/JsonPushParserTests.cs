namespace json.Tests;

public class JsonPushParserTests
{
    [Test]
    public void ContinuePublishesEveryTokenAndDocumentBoundary()
    {
        RecordingTokenHandler handler = new RecordingTokenHandler();
        IJsonPushParser parser = new JsonPushParser(new JsonReaderOptions(), handler);
        byte[] input = System.Text.Encoding.ASCII.GetBytes("{\"a\":[true,null]}");

        JsonFeedResult feed = parser.FeedPushUtf8(input);
        JsonFeedResult completed = parser.CompletePushInput();
        JsonToken boundary = handler.Tokens[^1];

        Assert.Multiple(() =>
        {
            Assert.That(feed.BytesAccepted, Is.EqualTo(input.Length));
            Assert.That(feed.BytesConsumed, Is.EqualTo(input.Length));
            Assert.That(feed.ReadStatus, Is.EqualTo(JsonReadStatus.NeedMoreData));
            Assert.That(completed.ReadStatus, Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.That(handler.Tokens.Select(token => token.Kind), Is.EqualTo(new[]
            {
                JsonTokenKind.StartObject,
                JsonTokenKind.PropertyName,
                JsonTokenKind.StartArray,
                JsonTokenKind.Boolean,
                JsonTokenKind.Null,
                JsonTokenKind.EndArray,
                JsonTokenKind.EndObject,
                JsonTokenKind.DocumentBoundary
            }));
            Assert.That(handler.Tokens[1].StringValue, Is.EqualTo("a"));
            Assert.That(handler.Tokens[1].Pointer?.OriginalRepresentation, Is.EqualTo("/a"));
            Assert.That(handler.Tokens[3].BooleanValue, Is.True);
            Assert.That(boundary.Depth, Is.Zero);
            Assert.That(boundary.Pointer?.OriginalRepresentation, Is.EqualTo(""));
            Assert.That(boundary.Pointer?.ReferenceTokens, Is.Empty);
            Assert.That(parser.GetPushBytesAccepted(), Is.EqualTo(input.Length));
            Assert.That(parser.GetPushBytesConsumed(), Is.EqualTo(input.Length));
        });
    }

    [Test]
    public void PauseAndResumeContinueAfterTheDeliveredTokenWithoutReplay()
    {
        RecordingTokenHandler handler = new RecordingTokenHandler();
        handler.PauseAtInvocation = 1;
        IJsonPushParser parser = new JsonPushParser(new JsonReaderOptions(), handler);
        byte[] input = System.Text.Encoding.ASCII.GetBytes("[0,1]");

        JsonFeedResult paused = parser.FeedPushUtf8(input);
        int consumedWhilePaused = paused.BytesConsumed;
        JsonFeedResult resumed = parser.ResumePushParser();

        Assert.Multiple(() =>
        {
            Assert.That(paused.ReadStatus, Is.EqualTo(JsonReadStatus.Paused));
            Assert.That(paused.BytesAccepted, Is.EqualTo(input.Length));
            Assert.That(consumedWhilePaused, Is.GreaterThan(0));
            Assert.That(consumedWhilePaused, Is.LessThan(input.Length));
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
            Assert.That(resumed.BytesAccepted, Is.Zero);
            Assert.That(resumed.BytesConsumed, Is.EqualTo(input.Length - consumedWhilePaused));
            Assert.That(resumed.ReadStatus, Is.EqualTo(JsonReadStatus.NeedMoreData));
            Assert.Throws<InvalidOperationException>(() => parser.ResumePushParser());
        });

        Assert.That(parser.CompletePushInput().ReadStatus, Is.EqualTo(JsonReadStatus.StreamComplete));
    }

    [Test]
    public void FeedsAndCompletionCanQueueInputWhilePaused()
    {
        RecordingTokenHandler handler = new RecordingTokenHandler();
        handler.PauseAtInvocation = 0;
        IJsonPushParser parser = new JsonPushParser(new JsonReaderOptions(), handler);

        JsonFeedResult paused = parser.FeedPushUtf8(
            new byte[] { JsonSyntaxBytes.BeginArray });
        JsonFeedResult queued = parser.FeedPushUtf8(
            System.Text.Encoding.ASCII.GetBytes("0]"));
        JsonFeedResult completedWhilePaused = parser.CompletePushInput();
        JsonFeedResult resumed = parser.ResumePushParser();

        Assert.Multiple(() =>
        {
            Assert.That(paused.ReadStatus, Is.EqualTo(JsonReadStatus.Paused));
            Assert.That(paused.BytesAccepted, Is.EqualTo(1));
            Assert.That(paused.BytesConsumed, Is.EqualTo(1));
            Assert.That(queued.ReadStatus, Is.EqualTo(JsonReadStatus.Paused));
            Assert.That(queued.BytesAccepted, Is.EqualTo(2));
            Assert.That(queued.BytesConsumed, Is.Zero);
            Assert.That(completedWhilePaused.ReadStatus, Is.EqualTo(JsonReadStatus.Paused));
            Assert.That(resumed.ReadStatus, Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.That(resumed.BytesConsumed, Is.EqualTo(2));
            Assert.That(handler.Tokens.Select(token => token.Kind), Is.EqualTo(new[]
            {
                JsonTokenKind.StartArray,
                JsonTokenKind.Number,
                JsonTokenKind.EndArray,
                JsonTokenKind.DocumentBoundary
            }));
            Assert.That(parser.GetPushBytesAccepted(), Is.EqualTo(3));
            Assert.That(parser.GetPushBytesConsumed(), Is.EqualTo(3));
        });
    }

    [Test]
    public void StopIsTerminalUntilResetAndDoesNotAcceptMoreInput()
    {
        RecordingTokenHandler handler = new RecordingTokenHandler();
        handler.StopAtInvocation = 0;
        IJsonPushParser parser = new JsonPushParser(new JsonReaderOptions(), handler);

        JsonFeedResult stopped = parser.FeedPushUtf8(
            System.Text.Encoding.ASCII.GetBytes("[0]"));
        JsonFeedResult ignored = parser.FeedPushUtf8(new byte[] { 0x20 });
        JsonFeedResult completed = parser.CompletePushInput();
        JsonFeedResult resumed = parser.ResumePushParser();

        Assert.Multiple(() =>
        {
            Assert.That(stopped.ReadStatus, Is.EqualTo(JsonReadStatus.Stopped));
            Assert.That(handler.Tokens.Select(token => token.Kind), Is.EqualTo(new[]
            {
                JsonTokenKind.StartArray
            }));
            Assert.That(ignored.ReadStatus, Is.EqualTo(JsonReadStatus.Stopped));
            Assert.That(ignored.BytesAccepted, Is.Zero);
            Assert.That(ignored.BytesConsumed, Is.Zero);
            Assert.That(completed.ReadStatus, Is.EqualTo(JsonReadStatus.Stopped));
            Assert.That(resumed.ReadStatus, Is.EqualTo(JsonReadStatus.Stopped));
        });

        parser.ResetPushParser();
        handler.Tokens.Clear();
        handler.StopAtInvocation = -1;
        JsonFeedResult reused = parser.FeedPushUtf8(
            System.Text.Encoding.ASCII.GetBytes("null "));
        JsonFeedResult reuseCompleted = parser.CompletePushInput();

        Assert.Multiple(() =>
        {
            Assert.That(reused.ReadStatus, Is.EqualTo(JsonReadStatus.NeedMoreData));
            Assert.That(reuseCompleted.ReadStatus, Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.That(handler.Tokens.Select(token => token.Kind), Is.EqualTo(new[]
            {
                JsonTokenKind.Null,
                JsonTokenKind.DocumentBoundary
            }));
            Assert.That(parser.GetPushBytesAccepted(), Is.EqualTo(5));
            Assert.That(parser.GetPushBytesConsumed(), Is.EqualTo(5));
        });
    }

    [Test]
    public void IncrementalScalarIsPublishedOnlyWhenFinalInputCompletesIt()
    {
        RecordingTokenHandler handler = new RecordingTokenHandler();
        IJsonPushParser parser = new JsonPushParser(new JsonReaderOptions(), handler);

        JsonFeedResult prefix = parser.FeedPushUtf8(
            System.Text.Encoding.ASCII.GetBytes("nu"));
        JsonFeedResult suffix = parser.FeedPushUtf8(
            System.Text.Encoding.ASCII.GetBytes("ll"));
        JsonFeedResult completed = parser.CompletePushInput();

        Assert.Multiple(() =>
        {
            Assert.That(prefix.ReadStatus, Is.EqualTo(JsonReadStatus.NeedMoreData));
            Assert.That(prefix.BytesAccepted, Is.EqualTo(2));
            Assert.That(prefix.BytesConsumed, Is.EqualTo(2));
            Assert.That(suffix.ReadStatus, Is.EqualTo(JsonReadStatus.NeedMoreData));
            Assert.That(suffix.BytesAccepted, Is.EqualTo(2));
            Assert.That(suffix.BytesConsumed, Is.EqualTo(2));
            Assert.That(completed.ReadStatus, Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.That(handler.Tokens.Select(token => token.Kind), Is.EqualTo(new[]
            {
                JsonTokenKind.Null,
                JsonTokenKind.DocumentBoundary
            }));
            Assert.That(parser.GetPushBytesAccepted(), Is.EqualTo(4));
            Assert.That(parser.GetPushBytesConsumed(), Is.EqualTo(4));
        });
    }

    [Test]
    public void ParserFailuresLatchTheirDiagnosticUntilReset()
    {
        RecordingTokenHandler handler = new RecordingTokenHandler();
        IJsonPushParser parser = new JsonPushParser(new JsonReaderOptions(), handler);

        JsonFeedResult failure = parser.FeedPushUtf8(
            System.Text.Encoding.ASCII.GetBytes("[0,]"));
        JsonFeedResult repeated = parser.CompletePushInput();

        Assert.Multiple(() =>
        {
            Assert.That(failure.ReadStatus, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(failure.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.TrailingComma));
            Assert.That(repeated.ReadStatus, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(repeated.Diagnostic, Is.SameAs(failure.Diagnostic));
            Assert.That(handler.Tokens.Select(token => token.Kind), Is.EqualTo(new[]
            {
                JsonTokenKind.StartArray,
                JsonTokenKind.Number
            }));
        });

        parser.ResetPushParser();
        handler.Tokens.Clear();
        JsonFeedResult valid = parser.FeedPushUtf8(
            System.Text.Encoding.ASCII.GetBytes("[]"));
        JsonFeedResult completed = parser.CompletePushInput();

        Assert.Multiple(() =>
        {
            Assert.That(valid.ReadStatus, Is.EqualTo(JsonReadStatus.NeedMoreData));
            Assert.That(completed.ReadStatus, Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.That(handler.Tokens.Select(token => token.Kind), Is.EqualTo(new[]
            {
                JsonTokenKind.StartArray,
                JsonTokenKind.EndArray,
                JsonTokenKind.DocumentBoundary
            }));
            Assert.That(parser.GetPushBytesAccepted(), Is.EqualTo(2));
            Assert.That(parser.GetPushBytesConsumed(), Is.EqualTo(2));
        });
    }

    [Test]
    public void HandlerExceptionsPropagateAfterTheDeliveredTokenWithoutReplay()
    {
        RecordingTokenHandler handler = new RecordingTokenHandler();
        handler.ThrowAtInvocation = 1;
        IJsonPushParser parser = new JsonPushParser(new JsonReaderOptions(), handler);
        byte[] input = System.Text.Encoding.ASCII.GetBytes("[0,1]");

        Assert.Throws<InvalidOperationException>(() => parser.FeedPushUtf8(input));
        handler.ThrowAtInvocation = -1;
        JsonFeedResult continued = parser.FeedPushUtf8(
            global::System.ReadOnlySpan<byte>.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(continued.ReadStatus, Is.EqualTo(JsonReadStatus.NeedMoreData));
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
        });

        Assert.That(parser.CompletePushInput().ReadStatus, Is.EqualTo(JsonReadStatus.StreamComplete));
    }

    class RecordingTokenHandler : IJsonTokenHandler
    {
        internal List<JsonToken> Tokens = new List<JsonToken>();
        internal int PauseAtInvocation = -1;
        internal int StopAtInvocation = -1;
        internal int ThrowAtInvocation = -1;

        public JsonHandlerAction HandleToken(JsonToken token)
        {
            int invocation = Tokens.Count;
            Tokens.Add(token);
            if (invocation == ThrowAtInvocation)
            {
                throw new InvalidOperationException("Handler failure.");
            }

            if (invocation == PauseAtInvocation)
            {
                return JsonHandlerAction.Pause;
            }

            if (invocation == StopAtInvocation)
            {
                return JsonHandlerAction.Stop;
            }

            return JsonHandlerAction.Continue;
        }
    }
}
