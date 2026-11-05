namespace json.Tests;

public class JsonParserDiagnosticTests
{
    [TestCase("/", JsonDiagnosticCode.UnexpectedCharacter)]
    [TestCase("\v", JsonDiagnosticCode.InvalidWhitespace)]
    [TestCase("truX", JsonDiagnosticCode.InvalidLiteral)]
    [TestCase("01", JsonDiagnosticCode.InvalidNumber)]
    [TestCase("\"\u0001\"", JsonDiagnosticCode.InvalidStringCharacter)]
    [TestCase("\"\\q\"", JsonDiagnosticCode.InvalidEscape)]
    [TestCase("\"\\u12X4\"", JsonDiagnosticCode.InvalidUnicodeEscape)]
    public void ParserProvokesEveryLexicalDiagnostic(
        string jsonText,
        JsonDiagnosticCode expectedCode)
    {
        AssertParseFailure(System.Text.Encoding.UTF8.GetBytes(jsonText), null, expectedCode);
    }

    [Test]
    public void ParserProvokesEveryUnicodeDiagnostic()
    {
        JsonReaderOptions scalarOptions = new JsonReaderOptions();
        scalarOptions.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
        AssertParseFailure(
            System.Text.Encoding.ASCII.GetBytes("\"\\uD800\""),
            scalarOptions,
            JsonDiagnosticCode.InvalidSurrogatePair);

        AssertParseFailure(
            new byte[] { JsonSyntaxBytes.StringDelimiter, 0xC0, JsonSyntaxBytes.StringDelimiter },
            null,
            JsonDiagnosticCode.InvalidUtf8);

        AssertParseFailure(
            new byte[] { 0xEF, 0xBB, 0xBF, 0x6E, 0x75, 0x6C, 0x6C },
            null,
            JsonDiagnosticCode.BomNotAllowed);

        JsonReaderOptions allowInitialBom = new JsonReaderOptions();
        allowInitialBom.BomPolicy = JsonBomPolicy.AllowInitial;
        AssertParseFailure(
            new byte[] { JsonSyntaxBytes.Space, 0xEF, 0xBB, 0xBF, 0x6E, 0x75, 0x6C, 0x6C },
            allowInitialBom,
            JsonDiagnosticCode.UnexpectedBom);
    }

    [TestCase("[", JsonDiagnosticCode.UnexpectedEndOfInput)]
    [TestCase("[}", JsonDiagnosticCode.UnexpectedToken)]
    [TestCase("{:0}", JsonDiagnosticCode.MissingPropertyName)]
    [TestCase("{\"a\" 0}", JsonDiagnosticCode.MissingColon)]
    [TestCase("[0 1]", JsonDiagnosticCode.MissingCommaOrEnd)]
    [TestCase("[0,]", JsonDiagnosticCode.TrailingComma)]
    [TestCase("0 1", JsonDiagnosticCode.TrailingContent)]
    public void ParserProvokesEveryStructuralDiagnostic(
        string jsonText,
        JsonDiagnosticCode expectedCode)
    {
        AssertParseFailure(System.Text.Encoding.ASCII.GetBytes(jsonText), null, expectedCode);
    }

    [Test]
    public void ParserProvokesTheDuplicateDiagnosticFromDecodedNames()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.Reject;

        JsonDiagnostic diagnostic = AssertParseFailure(
            System.Text.Encoding.ASCII.GetBytes("{\"a\":0,\"\\u0061\":1}"),
            options,
            JsonDiagnosticCode.DuplicatePropertyName);

        Assert.That(diagnostic.ActualToken, Is.EqualTo("a"));
    }

    [Test]
    public void ParserProvokesEveryStopModeFramingDiagnostic()
    {
        JsonReaderOptions sequenceOptions = new JsonReaderOptions();
        sequenceOptions.InputMode = JsonInputMode.JsonTextSequence;

        AssertParseFailure(
            System.Text.Encoding.ASCII.GetBytes("{}"),
            sequenceOptions,
            JsonDiagnosticCode.MissingRecordSeparator);
        AssertParseFailure(
            new byte[]
            {
                JsonSyntaxBytes.RecordSeparator,
                JsonSyntaxBytes.Space,
                JsonSyntaxBytes.RecordSeparator
            },
            sequenceOptions,
            JsonDiagnosticCode.EmptySequenceRecord);
        AssertParseFailure(
            new byte[] { JsonSyntaxBytes.RecordSeparator, JsonSyntaxBytes.BeginArray },
            sequenceOptions,
            JsonDiagnosticCode.IncompleteSequenceRecord);
    }

    [Test]
    public void RecoverableFramingDiagnosticIsPublishedForExactlyOneEvent()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.InputMode = JsonInputMode.JsonTextSequence;
        options.SequenceErrorPolicy = JsonSequenceErrorPolicy.Resynchronize;
        JsonParserCore parser = CreateParser(
            new byte[] { JsonSyntaxBytes.RecordSeparator, 0x31 },
            options);

        Assert.That(parser.ReadNextEvent(true), Is.EqualTo(JsonReadStatus.TokenAvailable));
        Assert.That(parser.ReadNextEvent(true), Is.EqualTo(JsonReadStatus.Error));
        JsonDiagnostic diagnostic = parser.GetCurrentDiagnostic()
            ?? throw new InvalidOperationException("Expected diagnostic.");

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.IncompleteSequenceRecord));
            Assert.That(diagnostic.Message, Is.Not.Null.And.Not.Empty);
            Assert.That(diagnostic.Pointer, Is.EqualTo(""));
            Assert.That(diagnostic.Depth, Is.Zero);
            Assert.That(diagnostic.Range?.Start?.ByteOffset, Is.EqualTo(2));
        });

        Assert.That(parser.ReadNextEvent(true), Is.EqualTo(JsonReadStatus.StreamComplete));
        Assert.That(parser.GetCurrentDiagnostic(), Is.Null);
    }

    [TestCase("document", "[0]", JsonDiagnosticCode.DocumentByteLimitExceeded)]
    [TestCase("buffer", "[0]", JsonDiagnosticCode.InputBufferLimitExceeded)]
    [TestCase("token", "\"ab\"", JsonDiagnosticCode.TokenByteLimitExceeded)]
    [TestCase("decodedBytes", "\"€\"", JsonDiagnosticCode.DecodedStringByteLimitExceeded)]
    [TestCase("decodedUnits", "\"ab\"", JsonDiagnosticCode.DecodedStringCodeUnitLimitExceeded)]
    [TestCase("number", "1234", JsonDiagnosticCode.NumberCharacterLimitExceeded)]
    [TestCase("nesting", "[[]]", JsonDiagnosticCode.NestingDepthLimitExceeded)]
    [TestCase("array", "[0,1]", JsonDiagnosticCode.ArrayItemLimitExceeded)]
    [TestCase("object", "{\"a\":0,\"b\":1}", JsonDiagnosticCode.ObjectMemberLimitExceeded)]
    public void ParserProvokesEveryInputLimitDiagnostic(
        string limitName,
        string jsonText,
        JsonDiagnosticCode expectedCode)
    {
        JsonLimits limits = CreateLimits(limitName);
        JsonReaderOptions options = new JsonReaderOptions();
        options.Limits = limits;
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(jsonText);

        JsonLimitException exception = Assert.Throws<JsonLimitException>(() =>
        {
            JsonParserCore parser = CreateParser(bytes, options);
            ReadToCompletion(parser);
        }) ?? throw new InvalidOperationException("Expected JsonLimitException.");

        AssertDiagnostic(exception.Diagnostic, expectedCode);
    }

    [Test]
    public void InvalidParserConfigurationHasTheInvalidOptionsDiagnostic()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.TrackUnicodeScalarColumn = true;

        JsonParseException exception = Assert.Throws<JsonParseException>(() => new JsonParserCore(options))
            ?? throw new InvalidOperationException("Expected JsonParseException.");

        AssertDiagnostic(exception.Diagnostic, JsonDiagnosticCode.InvalidOptions);
    }

    [Test]
    public void ParserLifecycleStateFailureDoesNotReuseACompletedEventDiagnostic()
    {
        JsonParserCore parser = CreateParser(
            System.Text.Encoding.ASCII.GetBytes("null"),
            new JsonReaderOptions());
        ReadToCompletion(parser);

        Assert.Throws<InvalidOperationException>(() => parser.FeedBytes(new byte[] { 0x30 }));
        Assert.Multiple(() =>
        {
            Assert.That(parser.GetCurrentDiagnostic(), Is.Null);
            Assert.That(parser.ReadNextEvent(true), Is.EqualTo(JsonReadStatus.StreamComplete));
        });
    }

    static JsonLimits CreateLimits(string limitName)
    {
        JsonLimits limits = new JsonLimits();
        switch (limitName)
        {
            case "document":
                limits.MaximumDocumentBytes = 2;
                break;
            case "buffer":
                limits.MaximumInputBufferBytes = 2;
                break;
            case "token":
                limits.MaximumTokenBytes = 3;
                break;
            case "decodedBytes":
                limits.MaximumDecodedStringBytes = 2;
                break;
            case "decodedUnits":
                limits.MaximumDecodedStringCodeUnits = 1;
                break;
            case "number":
                limits.MaximumNumberCharacters = 3;
                break;
            case "nesting":
                limits.MaximumNestingDepth = 1;
                break;
            case "array":
                limits.MaximumArrayItems = 1;
                break;
            case "object":
                limits.MaximumObjectMembers = 1;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(limitName));
        }

        return limits;
    }

    static JsonDiagnostic AssertParseFailure(
        byte[] bytes,
        JsonReaderOptions? options,
        JsonDiagnosticCode expectedCode)
    {
        JsonParseException exception = Assert.Throws<JsonParseException>(() =>
        {
            JsonParserCore parser = CreateParser(bytes, options);
            ReadToCompletion(parser);
        }) ?? throw new InvalidOperationException("Expected JsonParseException.");

        AssertDiagnostic(exception.Diagnostic, expectedCode);
        return exception.Diagnostic;
    }

    static void AssertDiagnostic(JsonDiagnostic diagnostic, JsonDiagnosticCode expectedCode)
    {
        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Code, Is.EqualTo(expectedCode));
            Assert.That(diagnostic.Message, Is.Not.Null.And.Not.Empty);
        });
    }

    static JsonParserCore CreateParser(byte[] bytes, JsonReaderOptions? options)
    {
        JsonParserCore parser = new JsonParserCore(options);
        parser.FeedBytes(bytes);
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
