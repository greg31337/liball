namespace json.Tests;

public class JsonPullReaderCaptureTests
{
    [Test]
    public void CaptureBuildsEveryCanonicalValueKindWithExactPayloads()
    {
        IJsonPullReader reader = CreateReader(
            "[null,true,-0.5e2,\"text\",[],{\"a\":1}]",
            null);

        JsonValueResult result = reader.CaptureValue();
        JsonValue root = result.Value
            ?? throw new InvalidOperationException("Expected captured value.");
        List<JsonValue> items = root.ArrayItems
            ?? throw new InvalidOperationException("Expected array items.");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Status, Is.EqualTo(JsonReadStatus.TokenAvailable));
            Assert.That(result.Diagnostic, Is.Null);
            Assert.That(root.Kind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(items.Select(item => item.Kind), Is.EqualTo(new[]
            {
                JsonValueKind.Null,
                JsonValueKind.Boolean,
                JsonValueKind.Number,
                JsonValueKind.String,
                JsonValueKind.Array,
                JsonValueKind.Object
            }));
            Assert.That(items[1].BooleanValue, Is.True);
            Assert.That(items[2].Number?.Lexeme, Is.EqualTo("-0.5e2"));
            Assert.That(items[2].Number?.IsNegative, Is.True);
            Assert.That(items[2].Number?.HasFraction, Is.True);
            Assert.That(items[2].Number?.HasExponent, Is.True);
            Assert.That(items[3].StringValue, Is.EqualTo("text"));
            Assert.That(items[4].ArrayItems, Is.Empty);
            Assert.That(items[5].ObjectMembers?[0].Name, Is.EqualTo("a"));
            Assert.That(items[5].ObjectMembers?[0].Value?.Number?.Lexeme, Is.EqualTo("1"));
            Assert.That(root.Range, Is.Null);
        });
    }

    [Test]
    public void CapturedSubtreeLeavesTheFollowingArrayItemStreaming()
    {
        IJsonPullReader reader = CreateReader("[{\"x\":[1,2]},3]", null);
        Assert.That(reader.ReadToken().Token?.Kind, Is.EqualTo(JsonTokenKind.StartArray));

        JsonValueResult captured = reader.CaptureValue();
        JsonTokenResult next = reader.PeekToken();

        Assert.Multiple(() =>
        {
            Assert.That(captured.Success, Is.True);
            Assert.That(captured.Value?.Kind, Is.EqualTo(JsonValueKind.Object));
            Assert.That(captured.Value?.ObjectMembers?[0].Name, Is.EqualTo("x"));
            Assert.That(captured.Value?.ObjectMembers?[0].Value?.ArrayItems, Has.Count.EqualTo(2));
            Assert.That(next.Token?.Kind, Is.EqualTo(JsonTokenKind.Number));
            Assert.That(next.Token?.Number?.Lexeme, Is.EqualTo("3"));
            Assert.That(next.Token?.Pointer?.OriginalRepresentation, Is.EqualTo("/1"));
            Assert.That(reader.ReadToken().Token, Is.SameAs(next.Token));
        });
    }

    [Test]
    public void PartialCaptureResumesAcrossFeedsAndBlocksOtherValueOperations()
    {
        IJsonPullReader reader = new JsonPullReader(new JsonReaderOptions());
        reader.FeedUtf8(System.Text.Encoding.ASCII.GetBytes("[{\"a\":[1"));
        reader.ReadToken();

        JsonValueResult partial = reader.CaptureValue();
        Assert.Multiple(() =>
        {
            Assert.That(partial.Success, Is.False);
            Assert.That(partial.Status, Is.EqualTo(JsonReadStatus.NeedMoreData));
            Assert.That(partial.Value, Is.Null);
            Assert.That(partial.Diagnostic, Is.Null);
            Assert.Throws<InvalidOperationException>(() => reader.PeekToken());
            Assert.Throws<InvalidOperationException>(() => reader.SkipValue());
        });

        reader.FeedUtf8(System.Text.Encoding.ASCII.GetBytes(",2]},3]"));
        JsonValueResult completed = reader.CaptureValue();
        JsonTokenResult next = reader.ReadToken();

        Assert.Multiple(() =>
        {
            Assert.That(completed.Success, Is.True);
            Assert.That(completed.Value?.ObjectMembers?[0].Value?.ArrayItems, Has.Count.EqualTo(2));
            Assert.That(next.Token?.Number?.Lexeme, Is.EqualTo("3"));
            Assert.That(next.Token?.Pointer?.OriginalRepresentation, Is.EqualTo("/1"));
        });
    }

    [TestCase(JsonDuplicateKeyPolicy.Preserve, 2, "1", 1)]
    [TestCase(JsonDuplicateKeyPolicy.KeepFirst, 1, "0", 0)]
    [TestCase(JsonDuplicateKeyPolicy.KeepLast, 1, "1", 1)]
    public void CaptureAppliesEveryRetainingDuplicatePolicy(
        JsonDuplicateKeyPolicy policy,
        int expectedMemberCount,
        string expectedLexeme,
        int expectedSourceOrdinal)
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.DuplicateKeyPolicy = policy;
        IJsonPullReader reader = CreateReader(
            "{\"a\":0,\"\\u0061\":1}",
            options);

        JsonValueResult result = reader.CaptureValue();
        List<JsonMember> members = result.Value?.ObjectMembers
            ?? throw new InvalidOperationException("Expected object members.");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(members, Has.Count.EqualTo(expectedMemberCount));
            Assert.That(members[^1].Name, Is.EqualTo("a"));
            Assert.That(members[^1].Value?.Number?.Lexeme, Is.EqualTo(expectedLexeme));
            Assert.That(members[^1].SourceOrdinal, Is.EqualTo(expectedSourceOrdinal));
        });
    }

    [Test]
    public void CaptureOwnsNumbersAndOptionalSourceRanges()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.CaptureSourceRanges = true;
        options.TrackLineInformation = true;
        IJsonPullReader reader = CreateReader("\n{\"a\":[0]}", options);
        JsonTokenResult start = reader.PeekToken();

        JsonValueResult result = reader.CaptureValue();
        JsonValue root = result.Value
            ?? throw new InvalidOperationException("Expected captured value.");
        JsonMember member = root.ObjectMembers?[0]
            ?? throw new InvalidOperationException("Expected captured member.");
        JsonValue number = member.Value?.ArrayItems?[0]
            ?? throw new InvalidOperationException("Expected captured number.");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(root.Range?.Start?.ByteOffset, Is.EqualTo(1));
            Assert.That(root.Range?.End?.ByteOffset, Is.EqualTo(10));
            Assert.That(root.Range?.Start?.Line, Is.EqualTo(2));
            Assert.That(member.NameRange?.Start?.ByteOffset, Is.EqualTo(2));
            Assert.That(member.NameRange?.End?.ByteOffset, Is.EqualTo(5));
            Assert.That(member.MemberRange?.Start?.ByteOffset, Is.EqualTo(2));
            Assert.That(member.MemberRange?.End?.ByteOffset, Is.EqualTo(9));
            Assert.That(member.Value?.Range?.Start?.ByteOffset, Is.EqualTo(6));
            Assert.That(member.Value?.Range?.End?.ByteOffset, Is.EqualTo(9));
            Assert.That(number.Range?.Start?.ByteOffset, Is.EqualTo(7));
            Assert.That(number.Range?.End?.ByteOffset, Is.EqualTo(8));
            Assert.That(root.Range, Is.Not.SameAs(start.Token?.Range));
        });

        IJsonPullReader numberReader = CreateReader("-0 ", null);
        JsonTokenResult numberToken = numberReader.PeekToken();
        JsonValueResult numberResult = numberReader.CaptureValue();
        Assert.Multiple(() =>
        {
            Assert.That(numberResult.Value?.Number?.Lexeme, Is.EqualTo("-0"));
            Assert.That(numberResult.Value?.Number?.IsNegativeZero, Is.True);
            Assert.That(numberResult.Value?.Number, Is.Not.SameAs(numberToken.Token?.Number));
        });
    }

    [Test]
    public void CaptureEnforcesDomLimitsAndParserValidation()
    {
        JsonLimits limits = new JsonLimits();
        limits.MaximumDomNodes = 2;
        JsonReaderOptions limitedOptions = new JsonReaderOptions();
        limitedOptions.Limits = limits;
        IJsonPullReader limited = CreateReader("{\"a\":0}", limitedOptions);
        JsonValueResult limitedResult = limited.CaptureValue();

        JsonReaderOptions rejectOptions = new JsonReaderOptions();
        rejectOptions.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.Reject;
        IJsonPullReader invalid = CreateReader(
            "{\"a\":0,\"\\u0061\":1}",
            rejectOptions);
        JsonValueResult invalidResult = invalid.CaptureValue();

        Assert.Multiple(() =>
        {
            Assert.That(limitedResult.Success, Is.False);
            Assert.That(limitedResult.Status, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(limitedResult.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.DomNodeLimitExceeded));
            Assert.That(limited.PeekToken().Status, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(invalidResult.Success, Is.False);
            Assert.That(invalidResult.Status, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(invalidResult.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.DuplicatePropertyName));
        });
    }

    [Test]
    public void RecoverableCaptureFailureLeavesTheNextRecordAvailable()
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

        JsonValueResult failed = reader.CaptureValue();
        JsonValueResult recovered = reader.CaptureValue();

        Assert.Multiple(() =>
        {
            Assert.That(failed.Success, Is.False);
            Assert.That(failed.Status, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(failed.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.IncompleteSequenceRecord));
            Assert.That(recovered.Success, Is.True);
            Assert.That(recovered.Value?.Kind, Is.EqualTo(JsonValueKind.Object));
            Assert.That(recovered.Value?.ObjectMembers, Is.Empty);
        });
    }

    [Test]
    public void CaptureRejectsPropertyAndBoundaryEventsWithoutConsumingThem()
    {
        IJsonPullReader reader = CreateReader("{\"a\":0}", null);
        reader.ReadToken();

        Assert.Throws<InvalidOperationException>(() => reader.CaptureValue());
        Assert.That(reader.ReadToken().Token?.Kind, Is.EqualTo(JsonTokenKind.PropertyName));
        Assert.That(reader.CaptureValue().Value?.Number?.Lexeme, Is.EqualTo("0"));
        Assert.That(reader.ReadToken().Token?.Kind, Is.EqualTo(JsonTokenKind.EndObject));
        Assert.That(reader.PeekToken().Status, Is.EqualTo(JsonReadStatus.DocumentComplete));
        Assert.Throws<InvalidOperationException>(() => reader.CaptureValue());
        Assert.That(reader.ReadToken().Status, Is.EqualTo(JsonReadStatus.DocumentComplete));
    }

    static IJsonPullReader CreateReader(string jsonText, JsonReaderOptions? options)
    {
        IJsonPullReader reader = new JsonPullReader(options ?? new JsonReaderOptions());
        reader.FeedUtf8(System.Text.Encoding.UTF8.GetBytes(jsonText));
        return reader;
    }
}
