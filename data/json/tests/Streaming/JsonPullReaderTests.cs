namespace json.Tests;

public class JsonPullReaderTests
{
    [Test]
    public void ReaderPublishesEveryTokenKindAndPayloadThroughItsInterface()
    {
        string jsonText =
            "{\"s\":\"x\\u20AC\",\"n\":-0.5e2,\"t\":true,\"f\":false," +
            "\"z\":null,\"a\":[],\"o\":{}}";
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes(jsonText);
        JsonReaderOptions options = new JsonReaderOptions();
        options.CaptureRawTokens = true;
        options.TrackLineInformation = true;
        options.TrackUnicodeScalarColumn = true;
        IJsonPullReader reader = new JsonPullReader(options);

        JsonFeedResult feed = reader.FeedUtf8(utf8);
        JsonFeedResult final = reader.CompleteInput();
        List<JsonToken> tokens = new List<JsonToken>();
        List<JsonReadStatus> statuses = new List<JsonReadStatus>();
        while (true)
        {
            JsonTokenResult read = reader.ReadToken();
            statuses.Add(read.Status);
            if (read.Token != null)
            {
                tokens.Add(read.Token);
            }

            if (read.Status == JsonReadStatus.StreamComplete)
            {
                break;
            }
        }

        List<JsonToken> properties = tokens
            .Where(token => token.Kind == JsonTokenKind.PropertyName)
            .ToList();
        List<JsonToken> booleans = tokens
            .Where(token => token.Kind == JsonTokenKind.Boolean)
            .ToList();
        JsonToken number = tokens.Single(token => token.Kind == JsonTokenKind.Number);
        JsonToken text = tokens.Single(token => token.Kind == JsonTokenKind.String);

        Assert.Multiple(() =>
        {
            Assert.That(feed.BytesAccepted, Is.EqualTo(utf8.Length));
            Assert.That(feed.ReadStatus, Is.EqualTo(JsonReadStatus.TokenAvailable));
            Assert.That(final.ReadStatus, Is.EqualTo(JsonReadStatus.TokenAvailable));
            Assert.That(tokens.Select(token => token.Kind), Is.EqualTo(new[]
            {
                JsonTokenKind.StartObject,
                JsonTokenKind.PropertyName,
                JsonTokenKind.String,
                JsonTokenKind.PropertyName,
                JsonTokenKind.Number,
                JsonTokenKind.PropertyName,
                JsonTokenKind.Boolean,
                JsonTokenKind.PropertyName,
                JsonTokenKind.Boolean,
                JsonTokenKind.PropertyName,
                JsonTokenKind.Null,
                JsonTokenKind.PropertyName,
                JsonTokenKind.StartArray,
                JsonTokenKind.EndArray,
                JsonTokenKind.PropertyName,
                JsonTokenKind.StartObject,
                JsonTokenKind.EndObject,
                JsonTokenKind.EndObject
            }));
            Assert.That(properties.Select(token => token.StringValue), Is.EqualTo(new[]
            {
                "s", "n", "t", "f", "z", "a", "o"
            }));
            Assert.That(properties.Select(token => token.Pointer?.OriginalRepresentation), Is.EqualTo(new[]
            {
                "/s", "/n", "/t", "/f", "/z", "/a", "/o"
            }));
            Assert.That(properties.All(token => token.Depth == 1), Is.True);
            Assert.That(text.StringValue, Is.EqualTo("x€"));
            Assert.That(text.RawUtf8, Is.EqualTo(System.Text.Encoding.ASCII.GetBytes("\"x\\u20AC\"")));
            Assert.That(text.Pointer?.OriginalRepresentation, Is.EqualTo("/s"));
            Assert.That(number.Number?.Lexeme, Is.EqualTo("-0.5e2"));
            Assert.That(number.Number?.IsNegative, Is.True);
            Assert.That(number.Number?.HasFraction, Is.True);
            Assert.That(number.Number?.HasExponent, Is.True);
            Assert.That(number.Number?.IsNegativeZero, Is.False);
            Assert.That(number.RawUtf8, Is.EqualTo(System.Text.Encoding.ASCII.GetBytes("-0.5e2")));
            Assert.That(booleans.Select(token => token.BooleanValue), Is.EqualTo(new[] { true, false }));
            Assert.That(tokens.All(token => token.RawUtf8 != null), Is.True);
            Assert.That(tokens.All(token => token.Range?.Start != null && token.Range?.End != null), Is.True);
            Assert.That(tokens[0].Depth, Is.Zero);
            Assert.That(tokens[0].Pointer?.OriginalRepresentation, Is.EqualTo(""));
            Assert.That(tokens[^1].Depth, Is.Zero);
            Assert.That(tokens[^1].Pointer?.OriginalRepresentation, Is.EqualTo(""));
            Assert.That(statuses.Count(status => status == JsonReadStatus.DocumentComplete), Is.EqualTo(1));
            Assert.That(statuses[^1], Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.That(reader.GetBytesAccepted(), Is.EqualTo(utf8.Length));
            Assert.That(reader.GetBytesConsumed(), Is.EqualTo(utf8.Length));
        });
    }

    [Test]
    public void ReaderTransitionsThroughNeedTokenDocumentAndStreamStatuses()
    {
        IJsonPullReader reader = new JsonPullReader(new JsonReaderOptions());

        JsonFeedResult prefix = reader.FeedUtf8(
            System.Text.Encoding.ASCII.GetBytes("nu"));
        JsonTokenResult waiting = reader.PeekToken();
        JsonFeedResult suffix = reader.FeedUtf8(
            System.Text.Encoding.ASCII.GetBytes("ll "));
        JsonTokenResult peek = reader.PeekToken();
        JsonTokenResult token = reader.ReadToken();
        JsonTokenResult document = reader.ReadToken();
        JsonTokenResult more = reader.ReadToken();
        JsonFeedResult completed = reader.CompleteInput();
        JsonTokenResult stream = reader.ReadToken();

        Assert.Multiple(() =>
        {
            Assert.That(prefix.ReadStatus, Is.EqualTo(JsonReadStatus.NeedMoreData));
            Assert.That(waiting.Status, Is.EqualTo(JsonReadStatus.NeedMoreData));
            Assert.That(suffix.ReadStatus, Is.EqualTo(JsonReadStatus.TokenAvailable));
            Assert.That(peek.Status, Is.EqualTo(JsonReadStatus.TokenAvailable));
            Assert.That(token.Status, Is.EqualTo(JsonReadStatus.TokenAvailable));
            Assert.That(token.Token?.Kind, Is.EqualTo(JsonTokenKind.Null));
            Assert.That(token.Token, Is.SameAs(peek.Token));
            Assert.That(document.Status, Is.EqualTo(JsonReadStatus.DocumentComplete));
            Assert.That(document.Token, Is.Null);
            Assert.That(more.Status, Is.EqualTo(JsonReadStatus.NeedMoreData));
            Assert.That(completed.ReadStatus, Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.That(stream.Status, Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.That(stream.Token, Is.Null);
        });
    }

    [Test]
    public void FeedResultsAndCumulativeCountersRespectUnreadBackpressure()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.InputMode = JsonInputMode.WhitespaceSeparated;
        IJsonPullReader reader = new JsonPullReader(options);

        JsonFeedResult first = reader.FeedUtf8(
            System.Text.Encoding.ASCII.GetBytes("0 "));
        JsonFeedResult second = reader.FeedUtf8(
            System.Text.Encoding.ASCII.GetBytes("1 "));

        Assert.Multiple(() =>
        {
            Assert.That(first.BytesAccepted, Is.EqualTo(2));
            Assert.That(first.BytesConsumed, Is.EqualTo(2));
            Assert.That(first.BytesWritten, Is.Zero);
            Assert.That(first.ReadStatus, Is.EqualTo(JsonReadStatus.TokenAvailable));
            Assert.That(first.Diagnostic, Is.Null);
            Assert.That(second.BytesAccepted, Is.EqualTo(2));
            Assert.That(second.BytesConsumed, Is.Zero);
            Assert.That(second.ReadStatus, Is.EqualTo(JsonReadStatus.TokenAvailable));
            Assert.That(reader.GetBytesAccepted(), Is.EqualTo(4));
            Assert.That(reader.GetBytesConsumed(), Is.EqualTo(2));
        });

        Assert.That(reader.ReadToken().Token?.Number?.Lexeme, Is.EqualTo("0"));
        Assert.That(reader.ReadToken().Status, Is.EqualTo(JsonReadStatus.DocumentComplete));
        Assert.That(reader.ReadToken().Token?.Number?.Lexeme, Is.EqualTo("1"));
        Assert.That(reader.ReadToken().Status, Is.EqualTo(JsonReadStatus.DocumentComplete));

        Assert.Multiple(() =>
        {
            Assert.That(reader.GetBytesAccepted(), Is.EqualTo(4));
            Assert.That(reader.GetBytesConsumed(), Is.EqualTo(4));
            Assert.That(reader.CompleteInput().ReadStatus, Is.EqualTo(JsonReadStatus.StreamComplete));
        });
    }

    [Test]
    public void ZeroLengthFeedsAreNoOpsInWaitingAndTokenStates()
    {
        IJsonPullReader reader = new JsonPullReader(new JsonReaderOptions());

        JsonFeedResult emptyWaiting = reader.FeedUtf8(global::System.ReadOnlySpan<byte>.Empty);
        JsonFeedResult value = reader.FeedUtf8(
            System.Text.Encoding.ASCII.GetBytes("[]"));
        JsonTokenResult peek = reader.PeekToken();
        JsonFeedResult emptyToken = reader.FeedUtf8(global::System.ReadOnlySpan<byte>.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(emptyWaiting.BytesAccepted, Is.Zero);
            Assert.That(emptyWaiting.BytesConsumed, Is.Zero);
            Assert.That(emptyWaiting.ReadStatus, Is.EqualTo(JsonReadStatus.NeedMoreData));
            Assert.That(value.ReadStatus, Is.EqualTo(JsonReadStatus.TokenAvailable));
            Assert.That(emptyToken.BytesAccepted, Is.Zero);
            Assert.That(emptyToken.BytesConsumed, Is.Zero);
            Assert.That(emptyToken.ReadStatus, Is.EqualTo(JsonReadStatus.TokenAvailable));
            Assert.That(reader.PeekToken().Token, Is.SameAs(peek.Token));
            Assert.That(reader.GetBytesAccepted(), Is.EqualTo(2));
            Assert.That(reader.GetBytesConsumed(), Is.EqualTo(1));
        });
    }

    [Test]
    public void FeedTakesOwnershipOfUnreadBytesAndConfiguredOptions()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.CaptureRawTokens = true;
        IJsonPullReader reader = new JsonPullReader(options);
        options.CaptureRawTokens = false;
        byte[] callerBuffer = System.Text.Encoding.ASCII.GetBytes("[1]");

        JsonFeedResult feed = reader.FeedUtf8(callerBuffer);
        callerBuffer[0] = 0x78;
        callerBuffer[1] = 0x78;
        callerBuffer[2] = 0x78;
        reader.CompleteInput();

        JsonTokenResult start = reader.ReadToken();
        JsonTokenResult number = reader.ReadToken();
        JsonTokenResult end = reader.ReadToken();

        Assert.Multiple(() =>
        {
            Assert.That(feed.BytesAccepted, Is.EqualTo(3));
            Assert.That(start.Token?.Kind, Is.EqualTo(JsonTokenKind.StartArray));
            Assert.That(start.Token?.RawUtf8, Is.EqualTo(new byte[] { JsonSyntaxBytes.BeginArray }));
            Assert.That(number.Token?.Number?.Lexeme, Is.EqualTo("1"));
            Assert.That(number.Token?.RawUtf8, Is.EqualTo(new byte[] { 0x31 }));
            Assert.That(end.Token?.Kind, Is.EqualTo(JsonTokenKind.EndArray));
            Assert.That(end.Token?.RawUtf8, Is.EqualTo(new byte[] { JsonSyntaxBytes.EndArray }));
        });
    }

    [Test]
    public void FinalInputPreservesUnreadTokensAndDiagnosesIncompleteInputAfterward()
    {
        IJsonPullReader reader = new JsonPullReader(new JsonReaderOptions());
        JsonFeedResult feed = reader.FeedUtf8(
            new byte[] { JsonSyntaxBytes.BeginArray });
        JsonFeedResult completed = reader.CompleteInput();
        JsonTokenResult start = reader.ReadToken();
        JsonTokenResult failure = reader.ReadToken();

        Assert.Multiple(() =>
        {
            Assert.That(feed.ReadStatus, Is.EqualTo(JsonReadStatus.TokenAvailable));
            Assert.That(completed.ReadStatus, Is.EqualTo(JsonReadStatus.TokenAvailable));
            Assert.That(start.Token?.Kind, Is.EqualTo(JsonTokenKind.StartArray));
            Assert.That(failure.Status, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(failure.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.UnexpectedEndOfInput));
            Assert.That(reader.ReadToken().Diagnostic, Is.SameAs(failure.Diagnostic));
            Assert.That(reader.CompleteInput().Diagnostic, Is.SameAs(failure.Diagnostic));
            Assert.Throws<InvalidOperationException>(() => reader.FeedUtf8(global::System.ReadOnlySpan<byte>.Empty));
        });

        IJsonPullReader empty = new JsonPullReader(new JsonReaderOptions());
        JsonFeedResult emptyFinal = empty.CompleteInput();
        Assert.Multiple(() =>
        {
            Assert.That(emptyFinal.ReadStatus, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(emptyFinal.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.UnexpectedEndOfInput));
            Assert.That(empty.GetBytesAccepted(), Is.Zero);
            Assert.That(empty.GetBytesConsumed(), Is.Zero);
        });
    }

    [Test]
    public void ResetAfterSuccessAndFailureStartsIndependentStreams()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.CaptureRawTokens = true;
        options.TrackLineInformation = true;
        IJsonPullReader successful = new JsonPullReader(options);
        successful.FeedUtf8(System.Text.Encoding.ASCII.GetBytes("[]"));
        successful.CompleteInput();
        DrainToStream(successful);

        successful.ResetReader();
        Assert.Multiple(() =>
        {
            Assert.That(successful.GetBytesAccepted(), Is.Zero);
            Assert.That(successful.GetBytesConsumed(), Is.Zero);
            Assert.That(successful.PeekToken().Status, Is.EqualTo(JsonReadStatus.NeedMoreData));
        });

        successful.FeedUtf8(System.Text.Encoding.ASCII.GetBytes("true "));
        JsonTokenResult reused = successful.ReadToken();
        Assert.Multiple(() =>
        {
            Assert.That(reused.Token?.Kind, Is.EqualTo(JsonTokenKind.Boolean));
            Assert.That(reused.Token?.BooleanValue, Is.True);
            Assert.That(reused.Token?.RawUtf8, Is.EqualTo(System.Text.Encoding.ASCII.GetBytes("true")));
            Assert.That(reused.Token?.Range?.Start?.ByteOffset, Is.Zero);
            Assert.That(reused.Token?.Range?.Start?.Line, Is.EqualTo(1));
        });

        IJsonPullReader failed = new JsonPullReader(new JsonReaderOptions());
        JsonFeedResult invalid = failed.FeedUtf8(
            System.Text.Encoding.ASCII.GetBytes("01"));
        Assert.That(invalid.ReadStatus, Is.EqualTo(JsonReadStatus.Error));
        failed.ResetReader();
        JsonFeedResult valid = failed.FeedUtf8(
            System.Text.Encoding.ASCII.GetBytes("null "));

        Assert.Multiple(() =>
        {
            Assert.That(valid.ReadStatus, Is.EqualTo(JsonReadStatus.TokenAvailable));
            Assert.That(failed.ReadToken().Token?.Kind, Is.EqualTo(JsonTokenKind.Null));
            Assert.That(failed.GetBytesAccepted(), Is.EqualTo(5));
            Assert.That(failed.GetBytesConsumed(), Is.EqualTo(5));
        });
    }

    static void DrainToStream(IJsonPullReader reader)
    {
        while (reader.ReadToken().Status != JsonReadStatus.StreamComplete)
        {
        }
    }
}
