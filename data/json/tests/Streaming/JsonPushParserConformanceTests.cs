namespace json.Tests;

public class JsonPushParserConformanceTests
{
    [TestCase(
        JsonInputMode.SingleValue,
        "\r\n{\"s\":\"x\\u20ac\",\"n\":-0.5e2,\"b\":false,\"a\":[null]}\r\n")]
    [TestCase(
        JsonInputMode.WhitespaceSeparated,
        "null [1,true] {\"x\":\"€\"} ")]
    [TestCase(
        JsonInputMode.JsonTextSequence,
        "\u001e{}\n\u001e[true]\n\u001e\"€\"\n")]
    public void PushEventsExactlyMatchPullEventsInEveryFramingMode(
        JsonInputMode inputMode,
        string jsonText)
    {
        JsonReaderOptions options = CreateTrackedOptions(inputMode);
        byte[] input = System.Text.Encoding.UTF8.GetBytes(jsonText);

        List<string> pullEvents = ReadPullEventSignatures(input, options);
        List<string> pushEvents = ReadPushEventSignatures(input, options);

        Assert.That(pushEvents, Is.EqualTo(pullEvents));
    }

    [Test]
    public void PausingAtEveryEventResumesWithoutReplayingOrOmittingAnEvent()
    {
        byte[] input = System.Text.Encoding.ASCII.GetBytes(
            "{\"a\":[0,true],\"b\":{}}");
        List<string> expected = ReadPushEventSignatures(
            input,
            new JsonReaderOptions());

        for (int pauseIndex = 0; pauseIndex < expected.Count; pauseIndex++)
        {
            MatrixTokenHandler handler = new MatrixTokenHandler();
            handler.PauseAtInvocation = pauseIndex;
            IJsonPushParser parser = new JsonPushParser(
                new JsonReaderOptions(),
                handler);

            JsonFeedResult paused = parser.FeedPushUtf8(input);
            Assert.Multiple(() =>
            {
                Assert.That(
                    paused.ReadStatus,
                    Is.EqualTo(JsonReadStatus.Paused),
                    "Pause index " + pauseIndex);
                Assert.That(
                    handler.Tokens,
                    Has.Count.EqualTo(pauseIndex + 1),
                    "Pause index " + pauseIndex);
            });

            JsonFeedResult resumed = parser.ResumePushParser();
            JsonFeedResult completed = parser.CompletePushInput();
            Assert.Multiple(() =>
            {
                Assert.That(
                    resumed.ReadStatus,
                    Is.EqualTo(JsonReadStatus.NeedMoreData),
                    "Pause index " + pauseIndex);
                Assert.That(
                    completed.ReadStatus,
                    Is.EqualTo(JsonReadStatus.StreamComplete),
                    "Pause index " + pauseIndex);
                Assert.That(
                    handler.Tokens.Select(CreateTokenSignature),
                    Is.EqualTo(expected),
                    "Pause index " + pauseIndex);
            });
        }
    }

    [Test]
    public void StoppingAtEveryEventPublishesOnlyTheRequestedPrefix()
    {
        byte[] input = System.Text.Encoding.ASCII.GetBytes(
            "{\"a\":[0,true],\"b\":{}}");
        List<string> expected = ReadPushEventSignatures(
            input,
            new JsonReaderOptions());

        for (int stopIndex = 0; stopIndex < expected.Count; stopIndex++)
        {
            MatrixTokenHandler handler = new MatrixTokenHandler();
            handler.StopAtInvocation = stopIndex;
            IJsonPushParser parser = new JsonPushParser(
                new JsonReaderOptions(),
                handler);

            JsonFeedResult stopped = parser.FeedPushUtf8(input);
            long accepted = parser.GetPushBytesAccepted();
            long consumed = parser.GetPushBytesConsumed();
            JsonFeedResult ignored = parser.FeedPushUtf8(
                global::System.ReadOnlySpan<byte>.Empty);
            JsonFeedResult completed = parser.CompletePushInput();
            JsonFeedResult resumed = parser.ResumePushParser();

            Assert.Multiple(() =>
            {
                Assert.That(
                    stopped.ReadStatus,
                    Is.EqualTo(JsonReadStatus.Stopped),
                    "Stop index " + stopIndex);
                Assert.That(
                    handler.Tokens.Select(CreateTokenSignature),
                    Is.EqualTo(expected.Take(stopIndex + 1)),
                    "Stop index " + stopIndex);
                Assert.That(ignored.ReadStatus, Is.EqualTo(JsonReadStatus.Stopped));
                Assert.That(ignored.BytesAccepted, Is.Zero);
                Assert.That(ignored.BytesConsumed, Is.Zero);
                Assert.That(completed.ReadStatus, Is.EqualTo(JsonReadStatus.Stopped));
                Assert.That(resumed.ReadStatus, Is.EqualTo(JsonReadStatus.Stopped));
                Assert.That(parser.GetPushBytesAccepted(), Is.EqualTo(accepted));
                Assert.That(parser.GetPushBytesConsumed(), Is.EqualTo(consumed));
            });
        }
    }

    [Test]
    public void HandlerFailureAtEveryEventContinuesAtTheFollowingEvent()
    {
        byte[] input = System.Text.Encoding.ASCII.GetBytes(
            "{\"a\":[0,true],\"b\":{}}");
        List<string> expected = ReadPushEventSignatures(
            input,
            new JsonReaderOptions());

        for (int failureIndex = 0; failureIndex < expected.Count; failureIndex++)
        {
            MatrixTokenHandler handler = new MatrixTokenHandler();
            InvalidOperationException expectedFailure = new InvalidOperationException(
                "Failure at event " + failureIndex);
            handler.ThrowAtInvocation = failureIndex;
            handler.HandlerFailure = expectedFailure;
            IJsonPushParser parser = new JsonPushParser(
                new JsonReaderOptions(),
                handler);

            InvalidOperationException? failure = Assert.Throws<InvalidOperationException>(
                () => parser.FeedPushUtf8(input));
            handler.ThrowAtInvocation = -1;
            JsonFeedResult continued = parser.FeedPushUtf8(
                global::System.ReadOnlySpan<byte>.Empty);
            JsonFeedResult completed = parser.CompletePushInput();

            Assert.Multiple(() =>
            {
                Assert.That(failure, Is.SameAs(expectedFailure));
                Assert.That(
                    handler.Tokens.Select(CreateTokenSignature),
                    Is.EqualTo(expected),
                    "Failure index " + failureIndex);
                Assert.That(continued.ReadStatus, Is.EqualTo(JsonReadStatus.NeedMoreData));
                Assert.That(completed.ReadStatus, Is.EqualTo(JsonReadStatus.StreamComplete));
                Assert.That(parser.GetPushBytesAccepted(), Is.EqualTo(input.Length));
                Assert.That(parser.GetPushBytesConsumed(), Is.EqualTo(input.Length));
            });
        }
    }

    [Test]
    public void EveryTwoChunkSplitProducesTheSameEventsAndCounters()
    {
        string jsonText =
            "\r\n{\"€\":\"\\uD834\\uDD1E\",\"n\":-12.5E+3," +
            "\"b\":false,\"a\":[null,true]}\r\n";
        byte[] input = System.Text.Encoding.UTF8.GetBytes(jsonText);
        JsonReaderOptions baselineOptions = CreateTrackedOptions(
            JsonInputMode.SingleValue);
        List<string> expected = ReadPushEventSignatures(input, baselineOptions);

        for (int split = 0; split <= input.Length; split++)
        {
            MatrixTokenHandler handler = new MatrixTokenHandler();
            JsonReaderOptions options = CreateTrackedOptions(
                JsonInputMode.SingleValue);
            IJsonPushParser parser = new JsonPushParser(options, handler);

            JsonFeedResult prefix = parser.FeedPushUtf8(
                input.AsSpan(0, split));
            JsonFeedResult suffix = parser.FeedPushUtf8(
                input.AsSpan(split));
            JsonFeedResult completed = parser.CompletePushInput();

            Assert.Multiple(() =>
            {
                Assert.That(
                    prefix.ReadStatus,
                    Is.Not.EqualTo(JsonReadStatus.Error),
                    "Split " + split);
                Assert.That(
                    suffix.ReadStatus,
                    Is.Not.EqualTo(JsonReadStatus.Error),
                    "Split " + split);
                Assert.That(
                    completed.ReadStatus,
                    Is.EqualTo(JsonReadStatus.StreamComplete),
                    "Split " + split);
                Assert.That(
                    handler.Tokens.Select(CreateTokenSignature),
                    Is.EqualTo(expected),
                    "Split " + split);
                Assert.That(parser.GetPushBytesAccepted(), Is.EqualTo(input.Length));
                Assert.That(parser.GetPushBytesConsumed(), Is.EqualTo(input.Length));
            });
        }
    }

    [Test]
    public void ByteAtATimeFeedsPreserveMultibyteEscapeAndNumberEvents()
    {
        string jsonText =
            "{\"€\":\"\\uD834\\uDD1E\",\"n\":-12.5E+3," +
            "\"b\":false,\"a\":[null,true]}";
        byte[] input = System.Text.Encoding.UTF8.GetBytes(jsonText);
        JsonReaderOptions baselineOptions = CreateTrackedOptions(
            JsonInputMode.SingleValue);
        List<string> expected = ReadPushEventSignatures(input, baselineOptions);
        MatrixTokenHandler handler = new MatrixTokenHandler();
        IJsonPushParser parser = new JsonPushParser(
            CreateTrackedOptions(JsonInputMode.SingleValue),
            handler);

        int index = 0;
        while (index < input.Length)
        {
            JsonFeedResult feed = parser.FeedPushUtf8(
                new byte[] { input[index] });
            Assert.That(feed.ReadStatus, Is.Not.EqualTo(JsonReadStatus.Error));
            index++;
        }

        JsonFeedResult completed = parser.CompletePushInput();
        Assert.Multiple(() =>
        {
            Assert.That(completed.ReadStatus, Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.That(handler.Tokens.Select(CreateTokenSignature), Is.EqualTo(expected));
            Assert.That(parser.GetPushBytesAccepted(), Is.EqualTo(input.Length));
            Assert.That(parser.GetPushBytesConsumed(), Is.EqualTo(input.Length));
        });
    }

    [Test]
    public void ResetFromPausedAndCompletedStatesSupportsIndependentReuse()
    {
        MatrixTokenHandler handler = new MatrixTokenHandler();
        handler.PauseAtInvocation = 1;
        IJsonPushParser parser = new JsonPushParser(
            new JsonReaderOptions(),
            handler);
        byte[] abandoned = System.Text.Encoding.ASCII.GetBytes("[0,1]");

        Assert.That(
            parser.FeedPushUtf8(abandoned).ReadStatus,
            Is.EqualTo(JsonReadStatus.Paused));
        parser.ResetPushParser();
        handler.Tokens.Clear();
        handler.PauseAtInvocation = -1;

        JsonFeedResult firstReuse = parser.FeedPushUtf8(
            System.Text.Encoding.ASCII.GetBytes("true "));
        JsonFeedResult firstCompletion = parser.CompletePushInput();
        List<JsonTokenKind> firstKinds = handler.Tokens
            .Select(token => token.Kind)
            .ToList();

        parser.ResetPushParser();
        handler.Tokens.Clear();
        JsonFeedResult secondReuse = parser.FeedPushUtf8(
            System.Text.Encoding.ASCII.GetBytes("{}"));
        JsonFeedResult secondCompletion = parser.CompletePushInput();

        Assert.Multiple(() =>
        {
            Assert.That(firstReuse.ReadStatus, Is.EqualTo(JsonReadStatus.NeedMoreData));
            Assert.That(firstCompletion.ReadStatus, Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.That(firstKinds, Is.EqualTo(new[]
            {
                JsonTokenKind.Boolean,
                JsonTokenKind.DocumentBoundary
            }));
            Assert.That(secondReuse.ReadStatus, Is.EqualTo(JsonReadStatus.NeedMoreData));
            Assert.That(secondCompletion.ReadStatus, Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.That(handler.Tokens.Select(token => token.Kind), Is.EqualTo(new[]
            {
                JsonTokenKind.StartObject,
                JsonTokenKind.EndObject,
                JsonTokenKind.DocumentBoundary
            }));
            Assert.That(parser.GetPushBytesAccepted(), Is.EqualTo(2));
            Assert.That(parser.GetPushBytesConsumed(), Is.EqualTo(2));
            Assert.That(parser.ResumePushParser().ReadStatus, Is.EqualTo(JsonReadStatus.StreamComplete));
        });
    }

    static JsonReaderOptions CreateTrackedOptions(JsonInputMode inputMode)
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.InputMode = inputMode;
        options.CaptureRawTokens = true;
        options.TrackLineInformation = true;
        options.TrackUnicodeScalarColumn = true;
        return options;
    }

    static List<string> ReadPullEventSignatures(
        byte[] input,
        JsonReaderOptions options)
    {
        IJsonPullReader reader = new JsonPullReader(options);
        reader.FeedUtf8(input);
        reader.CompleteInput();
        List<string> events = new List<string>();

        while (true)
        {
            JsonTokenResult read = reader.ReadToken();
            if (read.Status == JsonReadStatus.TokenAvailable)
            {
                JsonToken token = read.Token
                    ?? throw new InvalidOperationException("Expected a pull token.");
                events.Add(CreateTokenSignature(token));
                continue;
            }

            if (read.Status == JsonReadStatus.DocumentComplete)
            {
                events.Add(CreateTokenSignature(CreateBoundaryToken()));
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

    static List<string> ReadPushEventSignatures(
        byte[] input,
        JsonReaderOptions options)
    {
        MatrixTokenHandler handler = new MatrixTokenHandler();
        IJsonPushParser parser = new JsonPushParser(options, handler);
        JsonFeedResult feed = parser.FeedPushUtf8(input);
        JsonFeedResult completed = parser.CompletePushInput();

        Assert.Multiple(() =>
        {
            Assert.That(feed.ReadStatus, Is.Not.EqualTo(JsonReadStatus.Error));
            Assert.That(completed.ReadStatus, Is.EqualTo(JsonReadStatus.StreamComplete));
        });
        return handler.Tokens.Select(CreateTokenSignature).ToList();
    }

    static JsonToken CreateBoundaryToken()
    {
        JsonPointer pointer = new JsonPointer();
        pointer.Encoding = JsonPointerEncoding.String;
        pointer.OriginalRepresentation = "";
        pointer.ReferenceTokens = new List<string>();
        JsonToken token = new JsonToken();
        token.Kind = JsonTokenKind.DocumentBoundary;
        token.Depth = 0;
        token.Pointer = pointer;
        return token;
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

    class MatrixTokenHandler : IJsonTokenHandler
    {
        internal List<JsonToken> Tokens = new List<JsonToken>();
        internal int PauseAtInvocation = -1;
        internal int StopAtInvocation = -1;
        internal int ThrowAtInvocation = -1;
        internal global::System.Exception? HandlerFailure;

        public JsonHandlerAction HandleToken(JsonToken token)
        {
            int invocation = Tokens.Count;
            Tokens.Add(token);
            if (invocation == ThrowAtInvocation)
            {
                throw HandlerFailure
                    ?? new InvalidOperationException("Handler failure.");
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
