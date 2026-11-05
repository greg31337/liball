namespace json.Tests;

public class JsonPullReaderStateTests
{
    [Test]
    public void FeedPeekAndReadPreserveOneStableTokenEvent()
    {
        IJsonPullReader reader = new JsonPullReader(new JsonReaderOptions());
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("{}");

        JsonFeedResult feed = reader.FeedUtf8(utf8);
        JsonTokenResult firstPeek = reader.PeekToken();
        JsonTokenResult secondPeek = reader.PeekToken();
        JsonTokenResult firstRead = reader.ReadToken();

        Assert.Multiple(() =>
        {
            Assert.That(feed.BytesAccepted, Is.EqualTo(2));
            Assert.That(feed.BytesConsumed, Is.EqualTo(1));
            Assert.That(feed.ReadStatus, Is.EqualTo(JsonReadStatus.TokenAvailable));
            Assert.That(firstPeek.Status, Is.EqualTo(JsonReadStatus.TokenAvailable));
            Assert.That(firstPeek.Token?.Kind, Is.EqualTo(JsonTokenKind.StartObject));
            Assert.That(secondPeek.Token, Is.SameAs(firstPeek.Token));
            Assert.That(firstRead.Token, Is.SameAs(firstPeek.Token));
            Assert.That(reader.GetBytesAccepted(), Is.EqualTo(2));
            Assert.That(reader.GetBytesConsumed(), Is.EqualTo(1));
        });

        JsonTokenResult end = reader.ReadToken();
        JsonTokenResult boundary = reader.ReadToken();
        JsonTokenResult waiting = reader.ReadToken();
        JsonFeedResult completed = reader.CompleteInput();

        Assert.Multiple(() =>
        {
            Assert.That(end.Status, Is.EqualTo(JsonReadStatus.TokenAvailable));
            Assert.That(end.Token?.Kind, Is.EqualTo(JsonTokenKind.EndObject));
            Assert.That(boundary.Status, Is.EqualTo(JsonReadStatus.DocumentComplete));
            Assert.That(boundary.Token, Is.Null);
            Assert.That(waiting.Status, Is.EqualTo(JsonReadStatus.NeedMoreData));
            Assert.That(completed.ReadStatus, Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.That(reader.ReadToken().Status, Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.That(reader.GetBytesConsumed(), Is.EqualTo(2));
        });
    }

    [Test]
    public void PartialTokensNeedMoreDataUntilCompletionFinalizesThem()
    {
        IJsonPullReader reader = new JsonPullReader(new JsonReaderOptions());

        JsonFeedResult first = reader.FeedUtf8(System.Text.Encoding.ASCII.GetBytes("nu"));
        JsonFeedResult second = reader.FeedUtf8(System.Text.Encoding.ASCII.GetBytes("ll"));
        JsonFeedResult completed = reader.CompleteInput();
        JsonFeedResult repeatedCompletion = reader.CompleteInput();

        Assert.Multiple(() =>
        {
            Assert.That(first.BytesAccepted, Is.EqualTo(2));
            Assert.That(first.BytesConsumed, Is.EqualTo(2));
            Assert.That(first.ReadStatus, Is.EqualTo(JsonReadStatus.NeedMoreData));
            Assert.That(second.BytesAccepted, Is.EqualTo(2));
            Assert.That(second.BytesConsumed, Is.EqualTo(2));
            Assert.That(second.ReadStatus, Is.EqualTo(JsonReadStatus.NeedMoreData));
            Assert.That(completed.ReadStatus, Is.EqualTo(JsonReadStatus.TokenAvailable));
            Assert.That(repeatedCompletion.ReadStatus, Is.EqualTo(JsonReadStatus.TokenAvailable));
            Assert.That(reader.GetBytesAccepted(), Is.EqualTo(4));
            Assert.That(reader.GetBytesConsumed(), Is.EqualTo(4));
        });

        JsonTokenResult token = reader.ReadToken();
        JsonTokenResult boundary = reader.ReadToken();
        JsonTokenResult stream = reader.ReadToken();

        Assert.Multiple(() =>
        {
            Assert.That(token.Token?.Kind, Is.EqualTo(JsonTokenKind.Null));
            Assert.That(boundary.Status, Is.EqualTo(JsonReadStatus.DocumentComplete));
            Assert.That(stream.Status, Is.EqualTo(JsonReadStatus.StreamComplete));
        });
    }

    [Test]
    public void AdditionalFeedsDoNotAdvanceAnUnreadToken()
    {
        IJsonPullReader reader = new JsonPullReader(new JsonReaderOptions());

        JsonFeedResult first = reader.FeedUtf8(new byte[] { JsonSyntaxBytes.BeginArray });
        JsonFeedResult second = reader.FeedUtf8(
            System.Text.Encoding.ASCII.GetBytes("0]"));

        Assert.Multiple(() =>
        {
            Assert.That(first.ReadStatus, Is.EqualTo(JsonReadStatus.TokenAvailable));
            Assert.That(second.BytesAccepted, Is.EqualTo(2));
            Assert.That(second.BytesConsumed, Is.Zero);
            Assert.That(second.ReadStatus, Is.EqualTo(JsonReadStatus.TokenAvailable));
            Assert.That(reader.GetBytesAccepted(), Is.EqualTo(3));
            Assert.That(reader.GetBytesConsumed(), Is.EqualTo(1));
        });

        Assert.That(reader.ReadToken().Token?.Kind, Is.EqualTo(JsonTokenKind.StartArray));
        Assert.That(reader.ReadToken().Token?.Kind, Is.EqualTo(JsonTokenKind.Number));
        Assert.That(reader.ReadToken().Token?.Kind, Is.EqualTo(JsonTokenKind.EndArray));
    }

    [Test]
    public void ParseFailuresLatchUntilResetAndResetClearsCounters()
    {
        IJsonPullReader reader = new JsonPullReader(new JsonReaderOptions());
        reader.FeedUtf8(System.Text.Encoding.ASCII.GetBytes("[0,]"));

        Assert.That(reader.ReadToken().Token?.Kind, Is.EqualTo(JsonTokenKind.StartArray));
        Assert.That(reader.ReadToken().Token?.Kind, Is.EqualTo(JsonTokenKind.Number));
        JsonTokenResult failure = reader.ReadToken();
        JsonTokenResult repeated = reader.ReadToken();
        JsonFeedResult completedFailure = reader.CompleteInput();

        Assert.Multiple(() =>
        {
            Assert.That(failure.Status, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(failure.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.TrailingComma));
            Assert.That(repeated.Status, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(repeated.Diagnostic, Is.SameAs(failure.Diagnostic));
            Assert.That(completedFailure.ReadStatus, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(completedFailure.Diagnostic, Is.SameAs(failure.Diagnostic));
        });

        reader.ResetReader();
        Assert.Multiple(() =>
        {
            Assert.That(reader.GetBytesAccepted(), Is.Zero);
            Assert.That(reader.GetBytesConsumed(), Is.Zero);
            Assert.That(reader.PeekToken().Status, Is.EqualTo(JsonReadStatus.NeedMoreData));
        });

        reader.FeedUtf8(System.Text.Encoding.ASCII.GetBytes("null"));
        reader.CompleteInput();
        Assert.That(reader.ReadToken().Token?.Kind, Is.EqualTo(JsonTokenKind.Null));
    }

    [Test]
    public void InputBufferFailuresAreExceptionlessAndAtomic()
    {
        JsonLimits limits = new JsonLimits();
        limits.MaximumInputBufferBytes = 2;
        JsonReaderOptions options = new JsonReaderOptions();
        options.Limits = limits;
        IJsonPullReader reader = new JsonPullReader(options);

        JsonFeedResult failure = reader.FeedUtf8(
            System.Text.Encoding.ASCII.GetBytes("[0]"));

        Assert.Multiple(() =>
        {
            Assert.That(failure.BytesAccepted, Is.Zero);
            Assert.That(failure.BytesConsumed, Is.Zero);
            Assert.That(failure.ReadStatus, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(failure.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.InputBufferLimitExceeded));
            Assert.That(reader.GetBytesAccepted(), Is.Zero);
            Assert.That(reader.GetBytesConsumed(), Is.Zero);
        });

        reader.ResetReader();
        JsonFeedResult valid = reader.FeedUtf8(
            System.Text.Encoding.ASCII.GetBytes("0 "));
        Assert.That(valid.ReadStatus, Is.EqualTo(JsonReadStatus.TokenAvailable));
    }

    [Test]
    public void RecoverableSequenceErrorsAreConsumableReaderEvents()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.InputMode = JsonInputMode.JsonTextSequence;
        options.SequenceErrorPolicy = JsonSequenceErrorPolicy.Resynchronize;
        IJsonPullReader reader = new JsonPullReader(options);
        byte[] utf8 = new byte[]
        {
            JsonSyntaxBytes.RecordSeparator,
            0x31,
            JsonSyntaxBytes.RecordSeparator,
            JsonSyntaxBytes.BeginObject,
            JsonSyntaxBytes.EndObject,
            JsonSyntaxBytes.LineFeed
        };

        JsonFeedResult feed = reader.FeedUtf8(utf8);
        JsonTokenResult errorPeek = reader.PeekToken();
        JsonTokenResult errorRead = reader.ReadToken();
        JsonTokenResult start = reader.ReadToken();
        JsonTokenResult end = reader.ReadToken();
        JsonTokenResult boundary = reader.ReadToken();

        Assert.Multiple(() =>
        {
            Assert.That(feed.ReadStatus, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(errorPeek.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.IncompleteSequenceRecord));
            Assert.That(errorRead.Diagnostic, Is.SameAs(errorPeek.Diagnostic));
            Assert.That(start.Token?.Kind, Is.EqualTo(JsonTokenKind.StartObject));
            Assert.That(end.Token?.Kind, Is.EqualTo(JsonTokenKind.EndObject));
            Assert.That(boundary.Status, Is.EqualTo(JsonReadStatus.DocumentComplete));
        });

        Assert.That(reader.CompleteInput().ReadStatus, Is.EqualTo(JsonReadStatus.StreamComplete));
    }

    [Test]
    public void FinalInputDisallowsFurtherFeedsAndCompletionIsIdempotent()
    {
        IJsonPullReader reader = new JsonPullReader(new JsonReaderOptions());
        reader.FeedUtf8(System.Text.Encoding.ASCII.GetBytes("null"));
        JsonFeedResult firstCompletion = reader.CompleteInput();
        JsonFeedResult secondCompletion = reader.CompleteInput();

        Assert.Multiple(() =>
        {
            Assert.That(firstCompletion.ReadStatus, Is.EqualTo(JsonReadStatus.TokenAvailable));
            Assert.That(secondCompletion.ReadStatus, Is.EqualTo(JsonReadStatus.TokenAvailable));
            Assert.Throws<InvalidOperationException>(() => reader.FeedUtf8(new byte[] { 0x20 }));
        });
    }
}
