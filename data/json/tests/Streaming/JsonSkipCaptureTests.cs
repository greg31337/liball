namespace json.Tests;

public class JsonSkipCaptureTests
{
    [TestCase("null")]
    [TestCase("true")]
    [TestCase("false")]
    [TestCase("-0")]
    [TestCase("1.25e-2")]
    [TestCase("\"text\"")]
    [TestCase("\"\\u20ac\"")]
    public void SkipConsumesEveryScalarFormAndLeavesTheNextItemUnread(string scalar)
    {
        IJsonPullReader reader = CreateReader("[" + scalar + ",99]", null);
        Assert.That(reader.ReadToken().Token?.Kind, Is.EqualTo(JsonTokenKind.StartArray));

        JsonOperationResult skipped = reader.SkipValue();
        JsonTokenResult next = reader.ReadToken();

        Assert.Multiple(() =>
        {
            Assert.That(skipped.Success, Is.True);
            Assert.That(skipped.Diagnostic, Is.Null);
            Assert.That(next.Token?.Kind, Is.EqualTo(JsonTokenKind.Number));
            Assert.That(next.Token?.Number?.Lexeme, Is.EqualTo("99"));
            Assert.That(next.Token?.Pointer?.OriginalRepresentation, Is.EqualTo("/1"));
        });
    }

    [Test]
    public void CaptureConsumesEveryScalarFormWithExactValues()
    {
        IJsonPullReader reader = CreateReader(
            "[null,true,false,-0,1.25e-2,\"text\",\"\\u20ac\",99]",
            null);
        Assert.That(reader.ReadToken().Token?.Kind, Is.EqualTo(JsonTokenKind.StartArray));

        JsonValueResult nullValue = reader.CaptureValue();
        JsonValueResult trueValue = reader.CaptureValue();
        JsonValueResult falseValue = reader.CaptureValue();
        JsonValueResult negativeZero = reader.CaptureValue();
        JsonValueResult exponent = reader.CaptureValue();
        JsonValueResult text = reader.CaptureValue();
        JsonValueResult escaped = reader.CaptureValue();
        JsonTokenResult next = reader.ReadToken();

        Assert.Multiple(() =>
        {
            Assert.That(nullValue.Value?.Kind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(trueValue.Value?.Kind, Is.EqualTo(JsonValueKind.Boolean));
            Assert.That(trueValue.Value?.BooleanValue, Is.True);
            Assert.That(falseValue.Value?.Kind, Is.EqualTo(JsonValueKind.Boolean));
            Assert.That(falseValue.Value?.BooleanValue, Is.False);
            Assert.That(negativeZero.Value?.Number?.Lexeme, Is.EqualTo("-0"));
            Assert.That(negativeZero.Value?.Number?.IsNegativeZero, Is.True);
            Assert.That(exponent.Value?.Number?.Lexeme, Is.EqualTo("1.25e-2"));
            Assert.That(text.Value?.StringValue, Is.EqualTo("text"));
            Assert.That(escaped.Value?.StringValue, Is.EqualTo("€"));
            Assert.That(next.Token?.Number?.Lexeme, Is.EqualTo("99"));
            Assert.That(next.Token?.Pointer?.OriginalRepresentation, Is.EqualTo("/7"));
        });
    }

    [Test]
    public void EmptyArraysAndObjectsCanBeSkippedAndCapturedInEitherRole()
    {
        IJsonPullReader reader = CreateReader("[[],{},{},[],7]", null);
        reader.ReadToken();

        JsonOperationResult skippedArray = reader.SkipValue();
        JsonValueResult capturedObject = reader.CaptureValue();
        JsonOperationResult skippedObject = reader.SkipValue();
        JsonValueResult capturedArray = reader.CaptureValue();
        JsonTokenResult next = reader.ReadToken();

        Assert.Multiple(() =>
        {
            Assert.That(skippedArray.Success, Is.True);
            Assert.That(capturedObject.Value?.Kind, Is.EqualTo(JsonValueKind.Object));
            Assert.That(capturedObject.Value?.ObjectMembers, Is.Empty);
            Assert.That(skippedObject.Success, Is.True);
            Assert.That(capturedArray.Value?.Kind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(capturedArray.Value?.ArrayItems, Is.Empty);
            Assert.That(next.Token?.Number?.Lexeme, Is.EqualTo("7"));
            Assert.That(next.Token?.Pointer?.OriginalRepresentation, Is.EqualTo("/4"));
        });
    }

    [Test]
    public void DeepContainersAreSkippedAndCapturedWithoutLosingContinuation()
    {
        string nested = "0";
        for (int depth = 0; depth < 48; depth++)
        {
            nested = "[" + nested + "]";
        }

        IJsonPullReader skipping = CreateReader("[" + nested + ",1]", null);
        skipping.ReadToken();
        JsonOperationResult skipped = skipping.SkipValue();
        JsonTokenResult afterSkip = skipping.ReadToken();

        IJsonPullReader capturing = CreateReader("[" + nested + ",1]", null);
        capturing.ReadToken();
        JsonValueResult captured = capturing.CaptureValue();
        JsonValue current = captured.Value
            ?? throw new InvalidOperationException("Expected a captured value.");
        for (int depth = 0; depth < 48; depth++)
        {
            current = current.ArrayItems?[0]
                ?? throw new InvalidOperationException("Expected a nested array item.");
        }
        JsonTokenResult afterCapture = capturing.ReadToken();

        Assert.Multiple(() =>
        {
            Assert.That(skipped.Success, Is.True);
            Assert.That(afterSkip.Token?.Number?.Lexeme, Is.EqualTo("1"));
            Assert.That(afterSkip.Token?.Pointer?.OriginalRepresentation, Is.EqualTo("/1"));
            Assert.That(captured.Success, Is.True);
            Assert.That(current.Kind, Is.EqualTo(JsonValueKind.Number));
            Assert.That(current.Number?.Lexeme, Is.EqualTo("0"));
            Assert.That(afterCapture.Token?.Number?.Lexeme, Is.EqualTo("1"));
            Assert.That(afterCapture.Token?.Pointer?.OriginalRepresentation, Is.EqualTo("/1"));
        });
    }

    [TestCase("[{\"a\":\"\\q\"},1]", JsonDiagnosticCode.InvalidEscape)]
    [TestCase("[{\"a\":1e},1]", JsonDiagnosticCode.InvalidNumber)]
    [TestCase("[{\"a\" 0},1]", JsonDiagnosticCode.MissingColon)]
    [TestCase("[{\"a\":[0 1]},1]", JsonDiagnosticCode.MissingCommaOrEnd)]
    public void SkipFullyValidatesMalformedContent(
        string jsonText,
        JsonDiagnosticCode expectedCode)
    {
        IJsonPullReader reader = CreateReader(jsonText, null);
        reader.ReadToken();

        JsonOperationResult skipped = reader.SkipValue();

        Assert.Multiple(() =>
        {
            Assert.That(skipped.Success, Is.False);
            Assert.That(skipped.Diagnostic?.Code, Is.EqualTo(expectedCode));
            Assert.That(reader.PeekToken().Status, Is.EqualTo(JsonReadStatus.Error));
        });
    }

    [Test]
    public void SkipFullyValidatesMalformedUtf8InsideTheSubtree()
    {
        IJsonPullReader reader = new JsonPullReader(new JsonReaderOptions());
        reader.FeedUtf8(new byte[]
        {
            JsonSyntaxBytes.BeginArray,
            JsonSyntaxBytes.BeginObject,
            JsonSyntaxBytes.StringDelimiter,
            0x61,
            JsonSyntaxBytes.StringDelimiter,
            JsonSyntaxBytes.NameSeparator,
            JsonSyntaxBytes.StringDelimiter,
            0xc3,
            0x28,
            JsonSyntaxBytes.StringDelimiter,
            JsonSyntaxBytes.EndObject,
            JsonSyntaxBytes.ValueSeparator,
            0x31,
            JsonSyntaxBytes.EndArray
        });
        reader.ReadToken();

        JsonOperationResult skipped = reader.SkipValue();

        Assert.Multiple(() =>
        {
            Assert.That(skipped.Success, Is.False);
            Assert.That(skipped.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.InvalidUtf8));
            Assert.That(reader.PeekToken().Status, Is.EqualTo(JsonReadStatus.Error));
        });
    }

    [TestCase(JsonDuplicateKeyPolicy.Preserve)]
    [TestCase(JsonDuplicateKeyPolicy.KeepFirst)]
    [TestCase(JsonDuplicateKeyPolicy.KeepLast)]
    public void SkipAcceptsEveryRetainingDuplicatePolicy(JsonDuplicateKeyPolicy policy)
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.DuplicateKeyPolicy = policy;
        IJsonPullReader reader = CreateReader(
            "[{\"a\":0,\"\\u0061\":1},2]",
            options);
        reader.ReadToken();

        JsonOperationResult skipped = reader.SkipValue();
        JsonTokenResult next = reader.ReadToken();

        Assert.Multiple(() =>
        {
            Assert.That(skipped.Success, Is.True);
            Assert.That(skipped.Diagnostic, Is.Null);
            Assert.That(next.Token?.Number?.Lexeme, Is.EqualTo("2"));
            Assert.That(next.Token?.Pointer?.OriginalRepresentation, Is.EqualTo("/1"));
        });
    }

    [TestCase("array", "[[0,1],2]", JsonDiagnosticCode.ArrayItemLimitExceeded)]
    [TestCase("object", "[{\"a\":0,\"b\":1},2]", JsonDiagnosticCode.ObjectMemberLimitExceeded)]
    [TestCase("depth", "[[[0]],2]", JsonDiagnosticCode.NestingDepthLimitExceeded)]
    [TestCase("token", "[\"abcd\",2]", JsonDiagnosticCode.TokenByteLimitExceeded)]
    [TestCase("decoded", "[\"€\",2]", JsonDiagnosticCode.DecodedStringByteLimitExceeded)]
    public void SkipEnforcesEveryRelevantParserLimit(
        string limitName,
        string jsonText,
        JsonDiagnosticCode expectedCode)
    {
        JsonReaderOptions options = CreateLimitedOptions(limitName);
        IJsonPullReader reader = CreateReader(jsonText, options);
        reader.ReadToken();

        JsonOperationResult skipped = reader.SkipValue();

        Assert.Multiple(() =>
        {
            Assert.That(skipped.Success, Is.False);
            Assert.That(skipped.Diagnostic?.Code, Is.EqualTo(expectedCode));
            Assert.That(reader.PeekToken().Status, Is.EqualTo(JsonReadStatus.Error));
        });
    }

    [Test]
    public void SkipDoesNotApplyTheDomNodeLimit()
    {
        JsonLimits limits = new JsonLimits();
        limits.MaximumDomNodes = 1;
        JsonReaderOptions options = new JsonReaderOptions();
        options.Limits = limits;
        IJsonPullReader reader = CreateReader("[[0,1,{}],2]", options);
        reader.ReadToken();

        JsonOperationResult skipped = reader.SkipValue();
        JsonTokenResult next = reader.ReadToken();

        Assert.Multiple(() =>
        {
            Assert.That(skipped.Success, Is.True);
            Assert.That(next.Token?.Number?.Lexeme, Is.EqualTo("2"));
            Assert.That(next.Token?.Pointer?.OriginalRepresentation, Is.EqualTo("/1"));
        });
    }

    [Test]
    public void CaptureAcceptsTheExactDomNodeLimitAndEnforcesParserLimits()
    {
        JsonLimits exactLimits = new JsonLimits();
        exactLimits.MaximumDomNodes = 2;
        JsonReaderOptions exactOptions = new JsonReaderOptions();
        exactOptions.Limits = exactLimits;
        JsonValueResult exact = CreateReader("[0]", exactOptions).CaptureValue();

        JsonLimits parserLimits = new JsonLimits();
        parserLimits.MaximumArrayItems = 1;
        JsonReaderOptions parserOptions = new JsonReaderOptions();
        parserOptions.Limits = parserLimits;
        IJsonPullReader parserLimited = CreateReader("[0,1]", parserOptions);
        JsonValueResult parserFailure = parserLimited.CaptureValue();

        Assert.Multiple(() =>
        {
            Assert.That(exact.Success, Is.True);
            Assert.That(exact.Value?.ArrayItems, Has.Count.EqualTo(1));
            Assert.That(parserFailure.Success, Is.False);
            Assert.That(parserFailure.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.ArrayItemLimitExceeded));
            Assert.That(parserLimited.PeekToken().Status, Is.EqualTo(JsonReadStatus.Error));
        });
    }

    [Test]
    public void MixedSkipAndCaptureContinueAcrossContainersAndFramedDocuments()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.InputMode = JsonInputMode.WhitespaceSeparated;
        IJsonPullReader reader = CreateReader("{} [0,{\"x\":1},[],2] true ", options);

        JsonOperationResult first = reader.SkipValue();
        JsonTokenResult firstBoundary = reader.ReadToken();
        JsonTokenResult arrayStart = reader.ReadToken();
        JsonOperationResult scalar = reader.SkipValue();
        JsonValueResult objectValue = reader.CaptureValue();
        JsonOperationResult emptyArray = reader.SkipValue();
        JsonValueResult finalArrayItem = reader.CaptureValue();
        JsonTokenResult arrayEnd = reader.ReadToken();
        JsonTokenResult secondBoundary = reader.ReadToken();
        JsonValueResult finalDocument = reader.CaptureValue();
        JsonTokenResult thirdBoundary = reader.ReadToken();
        JsonFeedResult completed = reader.CompleteInput();

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(firstBoundary.Status, Is.EqualTo(JsonReadStatus.DocumentComplete));
            Assert.That(arrayStart.Token?.Kind, Is.EqualTo(JsonTokenKind.StartArray));
            Assert.That(scalar.Success, Is.True);
            Assert.That(objectValue.Value?.ObjectMembers?[0].Name, Is.EqualTo("x"));
            Assert.That(objectValue.Value?.ObjectMembers?[0].Value?.Number?.Lexeme, Is.EqualTo("1"));
            Assert.That(emptyArray.Success, Is.True);
            Assert.That(finalArrayItem.Value?.Number?.Lexeme, Is.EqualTo("2"));
            Assert.That(arrayEnd.Token?.Kind, Is.EqualTo(JsonTokenKind.EndArray));
            Assert.That(secondBoundary.Status, Is.EqualTo(JsonReadStatus.DocumentComplete));
            Assert.That(finalDocument.Value?.Kind, Is.EqualTo(JsonValueKind.Boolean));
            Assert.That(finalDocument.Value?.BooleanValue, Is.True);
            Assert.That(thirdBoundary.Status, Is.EqualTo(JsonReadStatus.DocumentComplete));
            Assert.That(completed.ReadStatus, Is.EqualTo(JsonReadStatus.StreamComplete));
        });
    }

    static JsonReaderOptions CreateLimitedOptions(string limitName)
    {
        JsonLimits limits = new JsonLimits();
        if (limitName == "array")
        {
            limits.MaximumArrayItems = 1;
        }
        else if (limitName == "object")
        {
            limits.MaximumObjectMembers = 1;
        }
        else if (limitName == "depth")
        {
            limits.MaximumNestingDepth = 2;
        }
        else if (limitName == "token")
        {
            limits.MaximumTokenBytes = 5;
        }
        else
        {
            limits.MaximumDecodedStringBytes = 2;
        }

        JsonReaderOptions options = new JsonReaderOptions();
        options.Limits = limits;
        return options;
    }

    static IJsonPullReader CreateReader(string jsonText, JsonReaderOptions? options)
    {
        IJsonPullReader reader = new JsonPullReader(options ?? new JsonReaderOptions());
        reader.FeedUtf8(System.Text.Encoding.UTF8.GetBytes(jsonText));
        return reader;
    }
}
