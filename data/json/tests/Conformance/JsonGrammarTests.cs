namespace json.Tests;

public class JsonGrammarTests
{
    [TestCase("{}", JsonTokenKind.StartObject, 2)]
    [TestCase("[]", JsonTokenKind.StartArray, 2)]
    [TestCase("\"root\"", JsonTokenKind.String, 1)]
    [TestCase("-0.5e+2", JsonTokenKind.Number, 1)]
    [TestCase("true", JsonTokenKind.Boolean, 1)]
    [TestCase("false", JsonTokenKind.Boolean, 1)]
    [TestCase("null", JsonTokenKind.Null, 1)]
    public void EveryJsonValueKindIsAcceptedAtTheRoot(
        string jsonText,
        JsonTokenKind expectedFirstKind,
        int expectedTokenCount)
    {
        List<JsonToken> tokens = ParseTokens(" \t\r\n" + jsonText + "\n ");

        Assert.Multiple(() =>
        {
            Assert.That(tokens, Has.Count.EqualTo(expectedTokenCount));
            Assert.That(tokens[0].Kind, Is.EqualTo(expectedFirstKind));
            Assert.That(tokens.All(token => token.Pointer?.OriginalRepresentation == ""), Is.True);
            Assert.That(tokens.All(token => token.Depth == 0), Is.True);
        });

        if (expectedFirstKind == JsonTokenKind.Boolean)
        {
            Assert.That(tokens[0].BooleanValue, Is.EqualTo(jsonText == "true"));
        }
    }

    [Test]
    public void EmptyAndNestedContainersPreserveGrammarDepthAndPaths()
    {
        List<JsonToken> tokens = ParseTokens("{\"a\":[{},[],{\"b\":[]}],\"c\":{}}");

        Assert.Multiple(() =>
        {
            Assert.That(tokens.Select(token => token.Kind), Is.EqualTo(new[]
            {
                JsonTokenKind.StartObject,
                JsonTokenKind.PropertyName,
                JsonTokenKind.StartArray,
                JsonTokenKind.StartObject,
                JsonTokenKind.EndObject,
                JsonTokenKind.StartArray,
                JsonTokenKind.EndArray,
                JsonTokenKind.StartObject,
                JsonTokenKind.PropertyName,
                JsonTokenKind.StartArray,
                JsonTokenKind.EndArray,
                JsonTokenKind.EndObject,
                JsonTokenKind.EndArray,
                JsonTokenKind.PropertyName,
                JsonTokenKind.StartObject,
                JsonTokenKind.EndObject,
                JsonTokenKind.EndObject
            }));
            Assert.That(tokens.Max(token => token.Depth), Is.EqualTo(3));
            Assert.That(tokens[3].Pointer?.OriginalRepresentation, Is.EqualTo("/a/0"));
            Assert.That(tokens[5].Pointer?.OriginalRepresentation, Is.EqualTo("/a/1"));
            Assert.That(tokens[7].Pointer?.OriginalRepresentation, Is.EqualTo("/a/2"));
            Assert.That(tokens[9].Pointer?.OriginalRepresentation, Is.EqualTo("/a/2/b"));
            Assert.That(tokens[14].Pointer?.OriginalRepresentation, Is.EqualTo("/c"));
            Assert.That(tokens[16].Pointer?.OriginalRepresentation, Is.EqualTo(""));
        });
    }

    [Test]
    public void ValidArrayAndObjectSeparatorsAcceptAllValueKinds()
    {
        AssertSuccessfulParse("[null,false,true,0,\"\",[],{}]");
        AssertSuccessfulParse("{\"\":0,\"array\":[],\"object\":{},\"value\":null}");
        AssertSuccessfulParse("{\"a\":[1,2,3],\"b\":{\"c\":true}}");
    }

    [TestCase("[,]", JsonDiagnosticCode.UnexpectedToken)]
    [TestCase("[0,]", JsonDiagnosticCode.TrailingComma)]
    [TestCase("[0,,1]", JsonDiagnosticCode.UnexpectedToken)]
    [TestCase("[0 1]", JsonDiagnosticCode.MissingCommaOrEnd)]
    [TestCase("{:1}", JsonDiagnosticCode.MissingPropertyName)]
    [TestCase("{true:1}", JsonDiagnosticCode.MissingPropertyName)]
    [TestCase("{\"a\" 1}", JsonDiagnosticCode.MissingColon)]
    [TestCase("{\"a\",1}", JsonDiagnosticCode.MissingColon)]
    [TestCase("{\"a\"::1}", JsonDiagnosticCode.UnexpectedToken)]
    [TestCase("{\"a\":}", JsonDiagnosticCode.UnexpectedToken)]
    [TestCase("{\"a\":1 \"b\":2}", JsonDiagnosticCode.MissingCommaOrEnd)]
    [TestCase("{\"a\":1,}", JsonDiagnosticCode.TrailingComma)]
    [TestCase("{\"a\":1,,\"b\":2}", JsonDiagnosticCode.MissingPropertyName)]
    [TestCase("[}", JsonDiagnosticCode.UnexpectedToken)]
    [TestCase("{]", JsonDiagnosticCode.UnexpectedToken)]
    public void InvalidSeparatorNameAndColonTransitionsHaveSpecificDiagnostics(
        string jsonText,
        JsonDiagnosticCode expectedCode)
    {
        AssertGrammarFailure(jsonText, expectedCode);
    }

    [TestCase("null true")]
    [TestCase("{}[]")]
    [TestCase("\"x\"0")]
    [TestCase("[0]false")]
    [TestCase("[]]")]
    public void SingleValueModeRejectsEveryFormOfTrailingContent(string jsonText)
    {
        AssertGrammarFailure(jsonText, JsonDiagnosticCode.TrailingContent);
    }

    [TestCase("")]
    [TestCase(" \t\r\n")]
    [TestCase("[")]
    [TestCase("[0")]
    [TestCase("[0,")]
    [TestCase("{")]
    [TestCase("{\"a\"")]
    [TestCase("{\"a\":")]
    [TestCase("{\"a\":1")]
    [TestCase("{\"a\":1,")]
    [TestCase("{\"a\":[0")]
    [TestCase("\"unterminated")]
    public void PrematureEndOfInputIsRejectedInEveryStructuralPhase(string jsonText)
    {
        AssertGrammarFailure(jsonText, JsonDiagnosticCode.UnexpectedEndOfInput);
    }

    [Test]
    public void MaximumDepthAcceptsTheExactBoundaryAndRejectsTheNextContainer()
    {
        JsonLimits limits = new JsonLimits();
        limits.MaximumNestingDepth = 2;
        JsonReaderOptions options = new JsonReaderOptions();
        options.Limits = limits;

        AssertSuccessfulParse("[[0]]", options);
        AssertSuccessfulParse("[{\"a\":0}]", options);
        AssertSuccessfulParse("{\"a\":{\"b\":0}}", options);

        AssertLimitFailure("[[[0]]]", options, JsonDiagnosticCode.NestingDepthLimitExceeded);
        AssertLimitFailure(
            "{\"a\":{\"b\":[]}}",
            options,
            JsonDiagnosticCode.NestingDepthLimitExceeded);
    }

    static List<JsonToken> ParseTokens(string jsonText)
    {
        JsonParserCore parser = CreateParser(jsonText, null);
        List<JsonToken> tokens = new List<JsonToken>();
        int documentCount = 0;
        while (true)
        {
            JsonReadStatus status = parser.ReadNextEvent(true);
            if (status == JsonReadStatus.TokenAvailable)
            {
                tokens.Add(parser.GetCurrentToken()
                    ?? throw new InvalidOperationException("Expected token."));
                continue;
            }

            if (status == JsonReadStatus.DocumentComplete)
            {
                documentCount++;
                continue;
            }

            Assert.That(status, Is.EqualTo(JsonReadStatus.StreamComplete));
            Assert.That(documentCount, Is.EqualTo(1));
            return tokens;
        }
    }

    static void AssertSuccessfulParse(string jsonText)
    {
        AssertSuccessfulParse(jsonText, null);
    }

    static void AssertSuccessfulParse(string jsonText, JsonReaderOptions? options)
    {
        JsonParserCore parser = CreateParser(jsonText, options);
        int documentCount = 0;
        while (true)
        {
            JsonReadStatus status = parser.ReadNextEvent(true);
            Assert.That(status, Is.Not.EqualTo(JsonReadStatus.Error));
            Assert.That(status, Is.Not.EqualTo(JsonReadStatus.NeedMoreData));
            if (status == JsonReadStatus.DocumentComplete)
            {
                documentCount++;
            }

            if (status == JsonReadStatus.StreamComplete)
            {
                Assert.That(documentCount, Is.EqualTo(1));
                return;
            }
        }
    }

    static void AssertGrammarFailure(string jsonText, JsonDiagnosticCode expectedCode)
    {
        JsonParserCore parser = CreateParser(jsonText, null);
        JsonParseException exception = Assert.Throws<JsonParseException>(() => ReadToCompletion(parser))
            ?? throw new InvalidOperationException("Expected JsonParseException.");
        Assert.That(exception.Diagnostic.Code, Is.EqualTo(expectedCode), jsonText);
    }

    static void AssertLimitFailure(
        string jsonText,
        JsonReaderOptions options,
        JsonDiagnosticCode expectedCode)
    {
        JsonParserCore parser = CreateParser(jsonText, options);
        JsonLimitException exception = Assert.Throws<JsonLimitException>(() => ReadToCompletion(parser))
            ?? throw new InvalidOperationException("Expected JsonLimitException.");
        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(expectedCode));
            Assert.That(exception.Diagnostic.Depth, Is.EqualTo(2));
        });
    }

    static JsonParserCore CreateParser(string jsonText, JsonReaderOptions? options)
    {
        JsonParserCore parser = new JsonParserCore(options);
        parser.FeedBytes(System.Text.Encoding.UTF8.GetBytes(jsonText));
        return parser;
    }

    static void ReadToCompletion(JsonParserCore parser)
    {
        int eventCount = 0;
        while (true)
        {
            JsonReadStatus status = parser.ReadNextEvent(true);
            eventCount++;
            Assert.That(eventCount, Is.LessThan(1000));
            if (status == JsonReadStatus.StreamComplete)
            {
                return;
            }

            if (status == JsonReadStatus.Error)
            {
                throw new InvalidOperationException("Unexpected recoverable parser error.");
            }
        }
    }
}
