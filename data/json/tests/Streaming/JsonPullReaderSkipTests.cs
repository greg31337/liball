namespace json.Tests;

public class JsonPullReaderSkipTests
{
    [Test]
    public void ScalarSkipConsumesExactlyOneUnreadValue()
    {
        IJsonPullReader reader = CreateReader("[0,1]", null);
        Assert.That(reader.ReadToken().Token?.Kind, Is.EqualTo(JsonTokenKind.StartArray));

        JsonOperationResult skipped = reader.SkipValue();
        JsonTokenResult next = reader.ReadToken();

        Assert.Multiple(() =>
        {
            Assert.That(skipped.Success, Is.True);
            Assert.That(skipped.Diagnostic, Is.Null);
            Assert.That(next.Token?.Kind, Is.EqualTo(JsonTokenKind.Number));
            Assert.That(next.Token?.Number?.Lexeme, Is.EqualTo("1"));
            Assert.That(next.Token?.Pointer?.OriginalRepresentation, Is.EqualTo("/1"));
        });
    }

    [Test]
    public void NestedSubtreeSkipLeavesTheFollowingArrayItemUnread()
    {
        IJsonPullReader reader = CreateReader("[{\"a\":[1,{\"b\":2}]},3]", null);
        Assert.That(reader.ReadToken().Token?.Kind, Is.EqualTo(JsonTokenKind.StartArray));

        JsonOperationResult skipped = reader.SkipValue();
        JsonTokenResult next = reader.PeekToken();

        Assert.Multiple(() =>
        {
            Assert.That(skipped.Success, Is.True);
            Assert.That(next.Token?.Kind, Is.EqualTo(JsonTokenKind.Number));
            Assert.That(next.Token?.Number?.Lexeme, Is.EqualTo("3"));
            Assert.That(next.Token?.Pointer?.OriginalRepresentation, Is.EqualTo("/1"));
            Assert.That(reader.ReadToken().Token, Is.SameAs(next.Token));
            Assert.That(reader.ReadToken().Token?.Kind, Is.EqualTo(JsonTokenKind.EndArray));
        });
    }

    [Test]
    public void ObjectValueSkipPreservesTheNextPropertyPosition()
    {
        IJsonPullReader reader = CreateReader(
            "{\"skip\":{\"x\":[1,2]},\"keep\":3}",
            null);
        Assert.That(reader.ReadToken().Token?.Kind, Is.EqualTo(JsonTokenKind.StartObject));
        Assert.That(reader.ReadToken().Token?.StringValue, Is.EqualTo("skip"));

        JsonOperationResult skipped = reader.SkipValue();
        JsonTokenResult property = reader.ReadToken();
        JsonTokenResult value = reader.ReadToken();

        Assert.Multiple(() =>
        {
            Assert.That(skipped.Success, Is.True);
            Assert.That(property.Token?.Kind, Is.EqualTo(JsonTokenKind.PropertyName));
            Assert.That(property.Token?.StringValue, Is.EqualTo("keep"));
            Assert.That(property.Token?.Pointer?.OriginalRepresentation, Is.EqualTo("/keep"));
            Assert.That(value.Token?.Number?.Lexeme, Is.EqualTo("3"));
            Assert.That(value.Token?.Pointer?.OriginalRepresentation, Is.EqualTo("/keep"));
        });
    }

    [Test]
    public void PartialSubtreeSkipResumesAfterMoreInput()
    {
        IJsonPullReader reader = new JsonPullReader(new JsonReaderOptions());
        reader.FeedUtf8(System.Text.Encoding.ASCII.GetBytes("[{\"a\":[1"));
        Assert.That(reader.ReadToken().Token?.Kind, Is.EqualTo(JsonTokenKind.StartArray));

        JsonOperationResult partial = reader.SkipValue();
        Assert.Multiple(() =>
        {
            Assert.That(partial.Success, Is.False);
            Assert.That(partial.Diagnostic, Is.Null);
            Assert.Throws<InvalidOperationException>(() => reader.ReadToken());
        });

        reader.FeedUtf8(System.Text.Encoding.ASCII.GetBytes(",2]},3]"));
        JsonOperationResult completed = reader.SkipValue();
        JsonTokenResult next = reader.ReadToken();

        Assert.Multiple(() =>
        {
            Assert.That(completed.Success, Is.True);
            Assert.That(completed.Diagnostic, Is.Null);
            Assert.That(next.Token?.Number?.Lexeme, Is.EqualTo("3"));
            Assert.That(next.Token?.Pointer?.OriginalRepresentation, Is.EqualTo("/1"));
        });
    }

    [Test]
    public void SkippedSubtreesAreFullyValidated()
    {
        IJsonPullReader malformed = CreateReader("[{\"a\":[0,]},1]", null);
        malformed.ReadToken();
        JsonOperationResult malformedResult = malformed.SkipValue();

        JsonReaderOptions duplicateOptions = new JsonReaderOptions();
        duplicateOptions.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.Reject;
        IJsonPullReader duplicate = CreateReader(
            "[{\"a\":0,\"\\u0061\":1},2]",
            duplicateOptions);
        duplicate.ReadToken();
        JsonOperationResult duplicateResult = duplicate.SkipValue();

        Assert.Multiple(() =>
        {
            Assert.That(malformedResult.Success, Is.False);
            Assert.That(malformedResult.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.TrailingComma));
            Assert.That(malformed.PeekToken().Status, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(duplicateResult.Success, Is.False);
            Assert.That(duplicateResult.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.DuplicatePropertyName));
            Assert.That(duplicate.PeekToken().Status, Is.EqualTo(JsonReadStatus.Error));
        });
    }

    [Test]
    public void RecoverableRecordFailureEndsTheSkipAndPositionsAtTheNextRecord()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.InputMode = JsonInputMode.JsonTextSequence;
        options.SequenceErrorPolicy = JsonSequenceErrorPolicy.Resynchronize;
        IJsonPullReader reader = new JsonPullReader(options);
        reader.FeedUtf8(new byte[]
        {
            JsonSyntaxBytes.RecordSeparator,
            JsonSyntaxBytes.BeginArray,
            JsonSyntaxBytes.RecordSeparator,
            JsonSyntaxBytes.BeginObject,
            JsonSyntaxBytes.EndObject,
            JsonSyntaxBytes.LineFeed
        });

        JsonOperationResult skipped = reader.SkipValue();
        JsonTokenResult next = reader.ReadToken();

        Assert.Multiple(() =>
        {
            Assert.That(skipped.Success, Is.False);
            Assert.That(skipped.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.IncompleteSequenceRecord));
            Assert.That(next.Token?.Kind, Is.EqualTo(JsonTokenKind.StartObject));
            Assert.That(next.Token?.Pointer?.OriginalRepresentation, Is.EqualTo(""));
        });
    }

    [Test]
    public void SkipCanWaitForItsFirstValueAndRejectsNonValueEvents()
    {
        IJsonPullReader waiting = new JsonPullReader(new JsonReaderOptions());
        JsonOperationResult needMore = waiting.SkipValue();
        waiting.FeedUtf8(System.Text.Encoding.ASCII.GetBytes("null "));
        JsonOperationResult skipped = waiting.SkipValue();

        IJsonPullReader objectReader = CreateReader("{\"a\":0}", null);
        objectReader.ReadToken();

        Assert.Multiple(() =>
        {
            Assert.That(needMore.Success, Is.False);
            Assert.That(needMore.Diagnostic, Is.Null);
            Assert.That(skipped.Success, Is.True);
            Assert.Throws<InvalidOperationException>(() => objectReader.SkipValue());
        });

        Assert.That(objectReader.ReadToken().Token?.Kind, Is.EqualTo(JsonTokenKind.PropertyName));
        Assert.That(objectReader.SkipValue().Success, Is.True);
    }

    [Test]
    public void FinalInputFullyValidatesAPartialSkipAndPublishesItsFailure()
    {
        IJsonPullReader reader = new JsonPullReader(new JsonReaderOptions());
        reader.FeedUtf8(System.Text.Encoding.ASCII.GetBytes("[{\"a\":"));
        reader.ReadToken();
        JsonOperationResult partial = reader.SkipValue();

        JsonFeedResult completed = reader.CompleteInput();
        JsonTokenResult failure = reader.PeekToken();
        JsonOperationResult repeated = reader.SkipValue();

        Assert.Multiple(() =>
        {
            Assert.That(partial.Success, Is.False);
            Assert.That(partial.Diagnostic, Is.Null);
            Assert.That(completed.ReadStatus, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(completed.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.UnexpectedEndOfInput));
            Assert.That(failure.Status, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(failure.Diagnostic, Is.SameAs(completed.Diagnostic));
            Assert.That(repeated.Success, Is.False);
            Assert.That(repeated.Diagnostic, Is.SameAs(completed.Diagnostic));
        });
    }

    static IJsonPullReader CreateReader(string jsonText, JsonReaderOptions? options)
    {
        IJsonPullReader reader = new JsonPullReader(options ?? new JsonReaderOptions());
        reader.FeedUtf8(System.Text.Encoding.ASCII.GetBytes(jsonText));
        return reader;
    }
}
