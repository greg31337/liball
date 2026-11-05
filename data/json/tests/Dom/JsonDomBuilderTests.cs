namespace json.Tests;

public class JsonDomBuilderTests
{
    [Test]
    public void BuilderCreatesOrderedExactDocumentWithAllValueKinds()
    {
        string jsonText =
            "{\"z\":-0.50e+2,\"a\":[null,true,false,\"€\"],\"o\":{}}";
        byte[] input = System.Text.Encoding.UTF8.GetBytes(jsonText);
        JsonReaderOptions options = new JsonReaderOptions();

        JsonDocument document = BuildDocument(input, options);
        JsonValue root = document.Root
            ?? throw new InvalidOperationException("Expected a document root.");
        List<JsonMember> members = root.ObjectMembers
            ?? throw new InvalidOperationException("Expected object members.");
        List<JsonValue> array = members[1].Value?.ArrayItems
            ?? throw new InvalidOperationException("Expected array items.");

        Assert.Multiple(() =>
        {
            Assert.That(root.Kind, Is.EqualTo(JsonValueKind.Object));
            Assert.That(members.Select(member => member.Name), Is.EqualTo(new[]
            {
                "z", "a", "o"
            }));
            Assert.That(members.Select(member => member.SourceOrdinal), Is.EqualTo(new[]
            {
                0, 1, 2
            }));
            Assert.That(members[0].Value?.Number?.Lexeme, Is.EqualTo("-0.50e+2"));
            Assert.That(members[0].Value?.Number?.Kind, Is.EqualTo(JsonNumberKind.FractionWithExponent));
            Assert.That(members[0].Value?.Number?.IsNegative, Is.True);
            Assert.That(members[0].Value?.Number?.HasFraction, Is.True);
            Assert.That(members[0].Value?.Number?.HasExponent, Is.True);
            Assert.That(array.Select(value => value.Kind), Is.EqualTo(new[]
            {
                JsonValueKind.Null,
                JsonValueKind.Boolean,
                JsonValueKind.Boolean,
                JsonValueKind.String
            }));
            Assert.That(array[1].BooleanValue, Is.True);
            Assert.That(array[2].BooleanValue, Is.False);
            Assert.That(array[3].StringValue, Is.EqualTo("€"));
            Assert.That(members[2].Value?.ObjectMembers, Is.Empty);
            Assert.That(document.SourceByteCount, Is.EqualTo(input.Length));
            Assert.That(document.ConformanceProfile, Is.EqualTo(options.ConformanceProfile));
            Assert.That(document.DuplicateKeyPolicy, Is.EqualTo(options.DuplicateKeyPolicy));
            Assert.That(document.UnicodePolicy, Is.EqualTo(options.UnicodePolicy));
            Assert.That(document.InputMode, Is.EqualTo(options.InputMode));
            Assert.That(document.BomPolicy, Is.EqualTo(options.BomPolicy));
        });
    }

    [TestCase(JsonDuplicateKeyPolicy.Preserve, 3, "0,1,2", "0,1,2")]
    [TestCase(JsonDuplicateKeyPolicy.KeepFirst, 2, "0,2", "0,2")]
    [TestCase(JsonDuplicateKeyPolicy.KeepLast, 2, "1,2", "1,2")]
    public void BuilderAppliesEveryRetainingDuplicatePolicy(
        JsonDuplicateKeyPolicy policy,
        int expectedCount,
        string expectedValues,
        string expectedOrdinals)
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.DuplicateKeyPolicy = policy;

        JsonDocument document = BuildDocument(
            System.Text.Encoding.ASCII.GetBytes(
                "{\"a\":0,\"\\u0061\":1,\"b\":2}"),
            options);
        List<JsonMember> members = document.Root?.ObjectMembers
            ?? throw new InvalidOperationException("Expected object members.");

        Assert.Multiple(() =>
        {
            Assert.That(members, Has.Count.EqualTo(expectedCount));
            Assert.That(
                string.Join(",", members.Select(member => member.Value?.Number?.Lexeme)),
                Is.EqualTo(expectedValues));
            Assert.That(
                string.Join(",", members.Select(member => member.SourceOrdinal)),
                Is.EqualTo(expectedOrdinals));
            Assert.That(members.Select(member => member.Name), Is.EqualTo(
                policy == JsonDuplicateKeyPolicy.Preserve
                    ? new[] { "a", "a", "b" }
                    : new[] { "a", "b" }));
        });
    }

    [Test]
    public void BuilderUsesParserDuplicateRejection()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.Reject;

        JsonParseException? failure = Assert.Throws<JsonParseException>(() =>
            BuildDocument(
                System.Text.Encoding.ASCII.GetBytes(
                    "{\"a\":0,\"\\u0061\":1}"),
                options));

        Assert.That(
            failure?.Diagnostic.Code,
            Is.EqualTo(JsonDiagnosticCode.DuplicatePropertyName));
    }

    [Test]
    public void BuilderCapturesOwnedValueAndMemberRangesOnlyWhenEnabled()
    {
        byte[] input = System.Text.Encoding.ASCII.GetBytes("\n{\"a\":[0]}");
        JsonReaderOptions withoutOptions = new JsonReaderOptions();
        JsonDocument withoutRanges = BuildDocument(input, withoutOptions);

        JsonReaderOptions withOptions = new JsonReaderOptions();
        withOptions.CaptureSourceRanges = true;
        withOptions.TrackLineInformation = true;
        JsonDocument withRanges = BuildDocument(input, withOptions);
        JsonValue root = withRanges.Root
            ?? throw new InvalidOperationException("Expected a document root.");
        JsonMember member = root.ObjectMembers?[0]
            ?? throw new InvalidOperationException("Expected an object member.");
        JsonValue array = member.Value
            ?? throw new InvalidOperationException("Expected an array value.");
        JsonValue number = array.ArrayItems?[0]
            ?? throw new InvalidOperationException("Expected a number value.");

        Assert.Multiple(() =>
        {
            Assert.That(withoutRanges.Root?.Range, Is.Null);
            Assert.That(withoutRanges.Root?.ObjectMembers?[0].NameRange, Is.Null);
            Assert.That(withoutRanges.Root?.ObjectMembers?[0].MemberRange, Is.Null);
            Assert.That(root.Range?.Start?.ByteOffset, Is.EqualTo(1));
            Assert.That(root.Range?.End?.ByteOffset, Is.EqualTo(input.Length));
            Assert.That(root.Range?.Start?.Line, Is.EqualTo(2));
            Assert.That(member.NameRange?.Start?.ByteOffset, Is.EqualTo(2));
            Assert.That(member.NameRange?.End?.ByteOffset, Is.EqualTo(5));
            Assert.That(member.MemberRange?.Start?.ByteOffset, Is.EqualTo(2));
            Assert.That(member.MemberRange?.End?.ByteOffset, Is.EqualTo(9));
            Assert.That(array.Range?.Start?.ByteOffset, Is.EqualTo(6));
            Assert.That(array.Range?.End?.ByteOffset, Is.EqualTo(9));
            Assert.That(number.Range?.Start?.ByteOffset, Is.EqualTo(7));
            Assert.That(number.Range?.End?.ByteOffset, Is.EqualTo(8));
        });
    }

    [Test]
    public void BuilderAcceptsTheExactDomNodeLimitAndFailsBeforeTheNextNode()
    {
        byte[] input = System.Text.Encoding.ASCII.GetBytes("{\"a\":[0]}");
        JsonLimits exactLimits = new JsonLimits();
        exactLimits.MaximumDomNodes = 4;
        JsonReaderOptions exactOptions = new JsonReaderOptions();
        exactOptions.Limits = exactLimits;
        JsonDocument exact = BuildDocument(input, exactOptions);

        JsonLimits failingLimits = new JsonLimits();
        failingLimits.MaximumDomNodes = 3;
        JsonReaderOptions failingOptions = new JsonReaderOptions();
        failingOptions.CaptureSourceRanges = true;
        failingOptions.Limits = failingLimits;
        JsonLimitException? failure = Assert.Throws<JsonLimitException>(() =>
            BuildDocument(input, failingOptions));

        Assert.Multiple(() =>
        {
            Assert.That(exact.Root?.ObjectMembers?[0].Value?.ArrayItems, Has.Count.EqualTo(1));
            Assert.That(failure?.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.DomNodeLimitExceeded));
            Assert.That(failure?.Diagnostic.Pointer, Is.EqualTo("/a/0"));
            Assert.That(failure?.Diagnostic.ActualToken, Is.EqualTo(JsonTokenKind.Number.ToString()));
            Assert.That(failure?.Diagnostic.Range?.Start?.ByteOffset, Is.EqualTo(6));
        });
    }

    [TestCase("depth", "[[0]]", JsonDiagnosticCode.NestingDepthLimitExceeded)]
    [TestCase("array", "[0,1]", JsonDiagnosticCode.ArrayItemLimitExceeded)]
    [TestCase("object", "{\"a\":0,\"b\":1}", JsonDiagnosticCode.ObjectMemberLimitExceeded)]
    public void BuilderPipelineEnforcesStructuralDomLimits(
        string limitName,
        string jsonText,
        JsonDiagnosticCode expectedCode)
    {
        JsonReaderOptions options = CreateStructuralLimitOptions(limitName);

        JsonLimitException? failure = Assert.Throws<JsonLimitException>(() =>
            BuildDocument(
                System.Text.Encoding.ASCII.GetBytes(jsonText),
                options));

        Assert.That(failure?.Diagnostic.Code, Is.EqualTo(expectedCode));
    }

    [Test]
    public void BuilderMaterializesDeepContainersIteratively()
    {
        string jsonText = "0";
        for (int depth = 0; depth < 80; depth++)
        {
            jsonText = "[" + jsonText + "]";
        }

        JsonLimits limits = new JsonLimits();
        limits.MaximumNestingDepth = 100;
        limits.MaximumDomNodes = 100;
        JsonReaderOptions options = new JsonReaderOptions();
        options.Limits = limits;

        JsonDocument document = BuildDocument(
            System.Text.Encoding.ASCII.GetBytes(jsonText),
            options);
        JsonValue current = document.Root
            ?? throw new InvalidOperationException("Expected a document root.");
        for (int depth = 0; depth < 80; depth++)
        {
            current = current.ArrayItems?[0]
                ?? throw new InvalidOperationException("Expected a nested array item.");
        }

        Assert.Multiple(() =>
        {
            Assert.That(current.Kind, Is.EqualTo(JsonValueKind.Number));
            Assert.That(current.Number?.Lexeme, Is.EqualTo("0"));
        });
    }

    [Test]
    public void BuilderResetCreatesAnIndependentDocument()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        JsonDomBuilder builder = new JsonDomBuilder(options);
        JsonDocument first = BuildDocumentWithBuilder(
            System.Text.Encoding.ASCII.GetBytes("[0]"),
            options,
            builder);

        builder.ResetDomBuilder();
        JsonDocument second = BuildDocumentWithBuilder(
            System.Text.Encoding.ASCII.GetBytes("{\"a\":true}"),
            options,
            builder);

        Assert.Multiple(() =>
        {
            Assert.That(first.Root?.Kind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(first.Root?.ArrayItems?[0].Number?.Lexeme, Is.EqualTo("0"));
            Assert.That(second.Root?.Kind, Is.EqualTo(JsonValueKind.Object));
            Assert.That(second.Root?.ObjectMembers?[0].Name, Is.EqualTo("a"));
            Assert.That(second.Root?.ObjectMembers?[0].Value?.BooleanValue, Is.True);
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second.Root, Is.Not.SameAs(first.Root));
        });
    }

    static JsonDocument BuildDocument(byte[] input, JsonReaderOptions options)
    {
        JsonDomBuilder builder = new JsonDomBuilder(options);
        return BuildDocumentWithBuilder(input, options, builder);
    }

    static JsonDocument BuildDocumentWithBuilder(
        byte[] input,
        JsonReaderOptions options,
        JsonDomBuilder builder)
    {
        JsonParserCore parser = new JsonParserCore(options);
        parser.FeedBytes(input);
        while (true)
        {
            JsonReadStatus status = parser.ReadNextEvent(true);
            if (status == JsonReadStatus.TokenAvailable)
            {
                JsonToken token = parser.GetCurrentToken()
                    ?? throw new InvalidOperationException("Expected a parser token.");
                builder.AcceptDomToken(
                    token,
                    parser.GetCurrentDuplicateDecision());
                continue;
            }

            if (status == JsonReadStatus.Error)
            {
                JsonDiagnostic diagnostic = parser.GetCurrentDiagnostic()
                    ?? throw new InvalidOperationException("Expected a parser diagnostic.");
                throw new JsonParseException(diagnostic);
            }

            if (status == JsonReadStatus.StreamComplete)
            {
                return builder.GetDomDocument(parser.GetConsumedByteCount());
            }
        }
    }

    static JsonReaderOptions CreateStructuralLimitOptions(string limitName)
    {
        JsonLimits limits = new JsonLimits();
        if (limitName == "depth")
        {
            limits.MaximumNestingDepth = 1;
        }
        else if (limitName == "array")
        {
            limits.MaximumArrayItems = 1;
        }
        else
        {
            limits.MaximumObjectMembers = 1;
        }

        JsonReaderOptions options = new JsonReaderOptions();
        options.Limits = limits;
        return options;
    }
}
