namespace json.Tests;

public class JsonResourceLimitBoundaryTests
{
    [Test]
    public void ParserLimitsUseInclusiveBoundariesAcrossAllInputPaths()
    {
        List<ParserLimitCase> cases = CreateParserLimitCases();
        int caseIndex = 0;
        while (caseIndex < cases.Count)
        {
            ParserLimitCase current = cases[caseIndex];
            int adjustment = -1;
            while (adjustment <= 1)
            {
                int configured = current.Actual + adjustment;
                JsonReaderOptions options = OptionsFor(
                    current,
                    configured);
                byte[] input = Bytes(current.Json);
                bool below = adjustment < 0;

                IJsonUtf8Validator validator = new JsonUtf8Validator();
                JsonOperationResult validation = validator.ValidateUtf8(
                    input,
                    options);
                AssertBoundary(
                    validation.Success,
                    validation.Diagnostic,
                    below && current.AffectsTokenValidation,
                    current,
                    "one-shot validator",
                    adjustment);

                IJsonDocumentParser documentParser =
                    new JsonDocumentParser();
                JsonDocumentResult document =
                    documentParser.TryParseUtf8(input, options);
                AssertBoundary(
                    document.Success,
                    document.Diagnostic,
                    below,
                    current,
                    "one-shot DOM parser",
                    adjustment);

                JsonValueResult pull = CapturePull(input, options);
                AssertBoundary(
                    pull.Success,
                    pull.Diagnostic,
                    below,
                    current,
                    "pull capture",
                    adjustment);

                JsonOperationResult push = ParsePush(input, options);
                AssertBoundary(
                    push.Success,
                    push.Diagnostic,
                    below && current.AffectsTokenValidation,
                    current,
                    "push parser",
                    adjustment);

                IJsonCanonicalizer canonicalizer = new JsonCanonicalizer();
                JsonOperationResult canonical =
                    canonicalizer.VerifyCanonicalUtf8(input, options);
                AssertBoundary(
                    canonical.Success,
                    canonical.Diagnostic,
                    below,
                    current,
                    "canonicalizer",
                    adjustment);

                JsonOperationResult raw = WriteRaw(input, current, configured);
                AssertBoundary(
                    raw.Success,
                    raw.Diagnostic,
                    below && current.AffectsTokenValidation,
                    current,
                    "raw writer",
                    adjustment);
                adjustment++;
            }

            caseIndex++;
        }
    }

    [Test]
    public void InputBufferBoundaryReflectsRetainedChunkOwnership()
    {
        byte[] input = Bytes("null");
        int configured = input.Length - 1;
        while (configured <= input.Length + 1)
        {
            JsonLimits limits = new JsonLimits();
            limits.MaximumInputBufferBytes = configured;
            JsonReaderOptions options = ReaderOptions(limits);
            bool below = configured < input.Length;

            JsonOperationResult validation =
                new JsonUtf8Validator().ValidateUtf8(input, options);
            JsonDocumentResult document =
                new JsonDocumentParser().TryParseUtf8(input, options);
            JsonOperationResult canonical =
                new JsonCanonicalizer().VerifyCanonicalUtf8(input, options);
            JsonValueResult pull = CapturePull(input, options);
            JsonOperationResult push = ParsePush(input, options);
            ParserLimitCase rawCase = ParserCase(
                "input-buffer",
                "null",
                input.Length,
                JsonDiagnosticCode.InputBufferLimitExceeded,
                true,
                (value, maximum) =>
                    value.MaximumInputBufferBytes = maximum);
            JsonOperationResult raw = WriteRaw(input, rawCase, configured);

            Assert.Multiple(() =>
            {
                Assert.That(validation.Success, Is.EqualTo(!below),
                    "one-shot validator " + configured);
                Assert.That(document.Success, Is.EqualTo(!below),
                    "one-shot parser " + configured);
                Assert.That(canonical.Success, Is.EqualTo(!below),
                    "canonicalizer " + configured);
                Assert.That(pull.Success, Is.EqualTo(!below),
                    "pull " + configured);
                Assert.That(push.Success, Is.EqualTo(!below),
                    "push " + configured);
                Assert.That(raw.Success, Is.EqualTo(!below),
                    "raw writer " + configured);
                if (below)
                {
                    Assert.That(validation.Diagnostic?.Code,
                        Is.EqualTo(JsonDiagnosticCode.InputBufferLimitExceeded));
                    Assert.That(document.Diagnostic?.Code,
                        Is.EqualTo(JsonDiagnosticCode.InputBufferLimitExceeded));
                    Assert.That(canonical.Diagnostic?.Code,
                        Is.EqualTo(JsonDiagnosticCode.InputBufferLimitExceeded));
                    Assert.That(pull.Diagnostic?.Code,
                        Is.EqualTo(JsonDiagnosticCode.InputBufferLimitExceeded));
                    Assert.That(push.Diagnostic?.Code,
                        Is.EqualTo(JsonDiagnosticCode.InputBufferLimitExceeded));
                    Assert.That(raw.Diagnostic?.Code,
                        Is.EqualTo(JsonDiagnosticCode.InputBufferLimitExceeded));
                }
            });
            configured++;
        }
    }

    [Test]
    public void ModelLimitsUseInclusiveBoundariesOnlyWhereApplicable()
    {
        List<ParserLimitCase> cases = CreateParserLimitCases();
        IJsonValueValidator validator = new JsonValueValidator();
        int caseIndex = 0;
        while (caseIndex < cases.Count)
        {
            ParserLimitCase current = cases[caseIndex];
            JsonDocument document = new JsonDocumentParser().ParseString(
                current.Json,
                new JsonReaderOptions());
            int adjustment = -1;
            while (adjustment <= 1)
            {
                JsonLimits limits = new JsonLimits();
                current.Configure(limits, current.Actual + adjustment);
                JsonOperationResult result = validator.ValidateDocument(
                    document,
                    limits);
                bool shouldFail = adjustment < 0 && current.AffectsModel;

                AssertBoundary(
                    result.Success,
                    result.Diagnostic,
                    shouldFail,
                    current,
                    "model validator",
                    adjustment);
                adjustment++;
            }

            caseIndex++;
        }
    }

    [Test]
    public void WriterLimitsUseInclusiveBoundariesForModelAndOutput()
    {
        List<WriterLimitCase> cases = CreateWriterLimitCases();
        int caseIndex = 0;
        while (caseIndex < cases.Count)
        {
            WriterLimitCase current = cases[caseIndex];
            int adjustment = -1;
            while (adjustment <= 1)
            {
                int configured = current.Actual + adjustment;
                JsonLimits limits = new JsonLimits();
                current.Configure(limits, configured);
                JsonWriterOptions options = new JsonWriterOptions();
                options.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
                options.Limits = limits;
                System.Buffers.ArrayBufferWriter<byte> destination =
                    new System.Buffers.ArrayBufferWriter<byte>();
                JsonWriterTarget target = new JsonWriterTarget();
                target.BufferWriter = destination;
                IJsonTokenWriter writer = new JsonUtf8Writer(options, target);

                JsonOperationResult write = writer.WriteJsonValue(
                    current.CreateValue());
                bool below = adjustment < 0;
                Assert.Multiple(() =>
                {
                    Assert.That(write.Success, Is.EqualTo(!below),
                        current.Name + " adjustment " + adjustment);
                    if (below)
                    {
                        Assert.That(write.Diagnostic?.Code,
                            Is.EqualTo(current.Code),
                            current.Name);
                    }
                });
                if (write.Success)
                {
                    JsonOperationResult completion = writer.CompleteWriter();
                    Assert.That(completion.Success, Is.True,
                        current.Name + " completion");
                }

                adjustment++;
            }

            caseIndex++;
        }
    }

    [Test]
    public void NestedLimitDiagnosticsAndPointerResolutionMeetTheBoundary()
    {
        const string json = "{\"a\":[0,1,2]}";
        byte[] input = Bytes(json);
        int configured = 2;
        while (configured <= 4)
        {
            JsonLimits limits = new JsonLimits();
            limits.MaximumArrayItems = configured;
            JsonReaderOptions options = ReaderOptions(limits);
            JsonDocumentResult parsed =
                new JsonDocumentParser().TryParseUtf8(input, options);
            JsonValueResult pull = CapturePull(input, options);
            JsonOperationResult push = ParsePush(input, options);

            if (configured < 3)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(parsed.Diagnostic?.Code,
                        Is.EqualTo(JsonDiagnosticCode.ArrayItemLimitExceeded));
                    Assert.That(parsed.Diagnostic?.Pointer, Is.EqualTo("/a/2"));
                    Assert.That(pull.Diagnostic?.Pointer, Is.EqualTo("/a/2"));
                    Assert.That(push.Diagnostic?.Pointer, Is.EqualTo("/a/2"));
                });
            }
            else
            {
                JsonDocument document = parsed.Document
                    ?? throw new InvalidOperationException(
                        "Expected a parsed document.");
                JsonValue root = document.Root
                    ?? throw new InvalidOperationException(
                        "Expected a root value.");
                IJsonPointerCodec codec = new JsonPointerCodec();
                IJsonPointerNavigator navigator = new JsonPointerNavigator();
                JsonValue resolved = navigator.ResolvePointer(
                    root,
                    codec.ParseStringPointer("/a/2"),
                    JsonDuplicateKeyPolicy.Preserve);

                Assert.Multiple(() =>
                {
                    Assert.That(pull.Success, Is.True);
                    Assert.That(push.Success, Is.True);
                    Assert.That(resolved.Number?.Lexeme, Is.EqualTo("2"));
                });
            }

            configured++;
        }
    }

    [Test]
    public void OutputAndDiagnosticExcerptLimitsMeetAllThreeBoundaries()
    {
        byte[] canonicalInput = Bytes("[0]");
        int outputLimit = canonicalInput.Length - 1;
        while (outputLimit <= canonicalInput.Length + 1)
        {
            JsonLimits limits = new JsonLimits();
            limits.MaximumOutputBytes = outputLimit;
            JsonReaderOptions options = ReaderOptions(limits);
            bool below = outputLimit < canonicalInput.Length;

            JsonOperationResult canonical =
                new JsonCanonicalizer().VerifyCanonicalUtf8(
                    canonicalInput,
                    options);

            Assert.That(canonical.Success, Is.EqualTo(!below),
                "canonical output " + outputLimit);
            if (below)
            {
                Assert.That(canonical.Diagnostic?.Code,
                    Is.EqualTo(JsonDiagnosticCode.OutputByteLimitExceeded));
            }

            outputLimit++;
        }

        byte[] invalid = Bytes("12345x");
        int excerptLimit = invalid.Length - 1;
        while (excerptLimit <= invalid.Length + 1)
        {
            JsonLimits limits = new JsonLimits();
            limits.MaximumDiagnosticExcerptBytes = excerptLimit;
            JsonReaderOptions options = ReaderOptions(limits);
            int expectedLength = excerptLimit < invalid.Length
                ? excerptLimit
                : invalid.Length;

            JsonOperationResult validation =
                new JsonUtf8Validator().ValidateUtf8(invalid, options);
            JsonDocumentResult document =
                new JsonDocumentParser().TryParseUtf8(invalid, options);
            JsonValueResult pull = CapturePull(invalid, options);
            JsonOperationResult push = ParsePush(invalid, options);
            JsonOperationResult canonical =
                new JsonCanonicalizer().VerifyCanonicalUtf8(
                    invalid,
                    options);

            Assert.Multiple(() =>
            {
                Assert.That(validation.Diagnostic?.SourceExcerpt,
                    Has.Length.EqualTo(expectedLength));
                Assert.That(document.Diagnostic?.SourceExcerpt,
                    Has.Length.EqualTo(expectedLength));
                Assert.That(pull.Diagnostic?.SourceExcerpt,
                    Has.Length.EqualTo(expectedLength));
                Assert.That(push.Diagnostic?.SourceExcerpt,
                    Has.Length.EqualTo(expectedLength));
                Assert.That(canonical.Diagnostic?.SourceExcerpt,
                    Has.Length.EqualTo(expectedLength));
            });
            excerptLimit++;
        }
    }

    [Test]
    public void EveryLexerDiagnosticSourceHonorsTheExcerptLimit()
    {
        List<ExcerptCase> cases = new List<ExcerptCase>();
        cases.Add(ExcerptCaseOf("literal", Bytes("trux"),
            options =>
            {
            }, 3));
        cases.Add(ExcerptCaseOf("number", Bytes("123x"),
            options =>
            {
            }, 3));
        cases.Add(ExcerptCaseOf("string", Bytes("\"abc\\q\""),
            options =>
            {
            }, 3));
        cases.Add(ExcerptCaseOf("escape", Bytes("\"\\ud800x\""),
            options => options.UnicodePolicy =
                JsonUnicodePolicy.UnicodeScalar, 3));
        cases.Add(ExcerptCaseOf("token-limit", Bytes("\"abcdef\""),
            options =>
            {
                JsonLimits limits = options.Limits
                    ?? throw new InvalidOperationException(
                        "Expected configured limits.");
                limits.MaximumTokenBytes = 5;
            }, 3));
        cases.Add(ExcerptCaseOf("noncharacter", Bytes("\"ab\\ufdd0\""),
            options =>
            {
                options.ConformanceProfile = JsonConformanceProfile.Jcs;
                options.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.Reject;
                options.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
            }, 3));
        cases.Add(ExcerptCaseOf("bom", new byte[] { 0xEF, 0xBB, 0xBF, 0x30 },
            options =>
            {
            }, 2));

        int index = 0;
        while (index < cases.Count)
        {
            ExcerptCase current = cases[index];
            JsonLimits limits = new JsonLimits();
            limits.MaximumDiagnosticExcerptBytes = current.Maximum;
            JsonReaderOptions options = ReaderOptions(limits);
            current.Configure(options);

            JsonOperationResult result =
                new JsonUtf8Validator().ValidateUtf8(
                    current.Input,
                    options);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False, current.Name);
                Assert.That(result.Diagnostic?.SourceExcerpt,
                    Has.Length.EqualTo(current.Maximum),
                    current.Name);
            });
            index++;
        }
    }

    static List<ParserLimitCase> CreateParserLimitCases()
    {
        List<ParserLimitCase> cases = new List<ParserLimitCase>();
        cases.Add(ParserCase(
            "document-bytes",
            "null",
            4,
            JsonDiagnosticCode.DocumentByteLimitExceeded,
            true,
            (value, maximum) => value.MaximumDocumentBytes = maximum));
        cases.Add(ParserCase(
            "token-bytes",
            "\"abc\"",
            5,
            JsonDiagnosticCode.TokenByteLimitExceeded,
            false,
            (value, maximum) => value.MaximumTokenBytes = maximum));
        cases.Add(ParserCase(
            "decoded-string-bytes",
            "\"é\"",
            2,
            JsonDiagnosticCode.DecodedStringByteLimitExceeded,
            true,
            (value, maximum) =>
                value.MaximumDecodedStringBytes = maximum));
        cases.Add(ParserCase(
            "decoded-string-code-units",
            "\"😀\"",
            2,
            JsonDiagnosticCode.DecodedStringCodeUnitLimitExceeded,
            true,
            (value, maximum) =>
                value.MaximumDecodedStringCodeUnits = maximum));
        cases.Add(ParserCase(
            "number-characters",
            "-123",
            4,
            JsonDiagnosticCode.NumberCharacterLimitExceeded,
            true,
            (value, maximum) =>
                value.MaximumNumberCharacters = maximum));
        cases.Add(ParserCase(
            "nesting-depth",
            "[[0]]",
            2,
            JsonDiagnosticCode.NestingDepthLimitExceeded,
            true,
            (value, maximum) => value.MaximumNestingDepth = maximum));
        cases.Add(ParserCase(
            "array-items",
            "[0,1,2]",
            3,
            JsonDiagnosticCode.ArrayItemLimitExceeded,
            true,
            (value, maximum) => value.MaximumArrayItems = maximum));
        cases.Add(ParserCase(
            "object-members",
            "{\"a\":0,\"b\":1}",
            2,
            JsonDiagnosticCode.ObjectMemberLimitExceeded,
            true,
            (value, maximum) => value.MaximumObjectMembers = maximum));
        ParserLimitCase dom = ParserCase(
            "dom-nodes",
            "{\"a\":0,\"b\":1}",
            5,
            JsonDiagnosticCode.DomNodeLimitExceeded,
            true,
            (value, maximum) => value.MaximumDomNodes = maximum);
        dom.AffectsTokenValidation = false;
        cases.Add(dom);
        return cases;
    }

    static List<WriterLimitCase> CreateWriterLimitCases()
    {
        JsonValueFactory factory = new JsonValueFactory();
        List<WriterLimitCase> cases = new List<WriterLimitCase>();
        cases.Add(WriterCase(
            "decoded-string-bytes",
            2,
            JsonDiagnosticCode.DecodedStringByteLimitExceeded,
            () => factory.CreateString("é"),
            (value, maximum) =>
                value.MaximumDecodedStringBytes = maximum));
        cases.Add(WriterCase(
            "decoded-string-code-units",
            2,
            JsonDiagnosticCode.DecodedStringCodeUnitLimitExceeded,
            () => factory.CreateString("😀"),
            (value, maximum) =>
                value.MaximumDecodedStringCodeUnits = maximum));
        cases.Add(WriterCase(
            "number-characters",
            4,
            JsonDiagnosticCode.NumberCharacterLimitExceeded,
            () => factory.CreateNumber("-123"),
            (value, maximum) =>
                value.MaximumNumberCharacters = maximum));
        cases.Add(WriterCase(
            "nesting-depth",
            2,
            JsonDiagnosticCode.NestingDepthLimitExceeded,
            () => factory.CreateArray(new List<JsonValue>
            {
                factory.CreateArray(new List<JsonValue>
                {
                    factory.CreateNumber("0")
                })
            }),
            (value, maximum) => value.MaximumNestingDepth = maximum));
        cases.Add(WriterCase(
            "array-items",
            3,
            JsonDiagnosticCode.ArrayItemLimitExceeded,
            () => factory.CreateArray(new List<JsonValue>
            {
                factory.CreateNumber("0"),
                factory.CreateNumber("1"),
                factory.CreateNumber("2")
            }),
            (value, maximum) => value.MaximumArrayItems = maximum));
        cases.Add(WriterCase(
            "object-members",
            2,
            JsonDiagnosticCode.ObjectMemberLimitExceeded,
            () => factory.CreateObject(new List<JsonMember>
            {
                Member("a", factory.CreateNumber("0")),
                Member("b", factory.CreateNumber("1"))
            }),
            (value, maximum) => value.MaximumObjectMembers = maximum));
        cases.Add(WriterCase(
            "dom-nodes",
            5,
            JsonDiagnosticCode.DomNodeLimitExceeded,
            () => factory.CreateObject(new List<JsonMember>
            {
                Member("a", factory.CreateNumber("0")),
                Member("b", factory.CreateNumber("1"))
            }),
            (value, maximum) => value.MaximumDomNodes = maximum));
        cases.Add(WriterCase(
            "output-bytes",
            4,
            JsonDiagnosticCode.OutputByteLimitExceeded,
            () => factory.CreateString("é"),
            (value, maximum) => value.MaximumOutputBytes = maximum));
        return cases;
    }

    static ParserLimitCase ParserCase(
        string name,
        string json,
        int actual,
        JsonDiagnosticCode code,
        bool affectsModel,
        System.Action<JsonLimits, int> configure)
    {
        ParserLimitCase result = new ParserLimitCase();
        result.Name = name;
        result.Json = json;
        result.Actual = actual;
        result.Code = code;
        result.AffectsModel = affectsModel;
        result.Configure = configure;
        return result;
    }

    static WriterLimitCase WriterCase(
        string name,
        int actual,
        JsonDiagnosticCode code,
        System.Func<JsonValue> createValue,
        System.Action<JsonLimits, int> configure)
    {
        WriterLimitCase result = new WriterLimitCase();
        result.Name = name;
        result.Actual = actual;
        result.Code = code;
        result.CreateValue = createValue;
        result.Configure = configure;
        return result;
    }

    static ExcerptCase ExcerptCaseOf(
        string name,
        byte[] input,
        System.Action<JsonReaderOptions> configure,
        int maximum)
    {
        ExcerptCase result = new ExcerptCase();
        result.Name = name;
        result.Input = input;
        result.Configure = configure;
        result.Maximum = maximum;
        return result;
    }

    static JsonReaderOptions OptionsFor(
        ParserLimitCase current,
        int configured)
    {
        JsonLimits limits = new JsonLimits();
        current.Configure(limits, configured);
        return ReaderOptions(limits);
    }

    static JsonReaderOptions ReaderOptions(JsonLimits limits)
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.Limits = limits;
        return options;
    }

    static JsonValueResult CapturePull(
        byte[] input,
        JsonReaderOptions options)
    {
        IJsonPullReader reader = new JsonPullReader(options);
        JsonFeedResult feed = reader.FeedUtf8(input);
        if (feed.Diagnostic != null)
        {
            return FailedValue(feed.Diagnostic);
        }

        JsonFeedResult completion = reader.CompleteInput();
        if (completion.Diagnostic != null)
        {
            return FailedValue(completion.Diagnostic);
        }

        return reader.CaptureValue();
    }

    static JsonOperationResult ParsePush(
        byte[] input,
        JsonReaderOptions options)
    {
        IJsonPushParser parser = new JsonPushParser(
            options,
            new ContinuingHandler());
        JsonFeedResult feed = parser.FeedPushUtf8(input);
        if (feed.Diagnostic != null)
        {
            return FailedOperation(feed.Diagnostic);
        }

        JsonFeedResult completion = parser.CompletePushInput();
        if (completion.Diagnostic != null)
        {
            return FailedOperation(completion.Diagnostic);
        }

        JsonOperationResult result = new JsonOperationResult();
        result.Success = completion.ReadStatus == JsonReadStatus.StreamComplete;
        return result;
    }

    static JsonOperationResult WriteRaw(
        byte[] input,
        ParserLimitCase current,
        int configured)
    {
        JsonLimits limits = new JsonLimits();
        current.Configure(limits, configured);
        JsonWriterOptions options = new JsonWriterOptions();
        options.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
        options.Limits = limits;
        System.Buffers.ArrayBufferWriter<byte> destination =
            new System.Buffers.ArrayBufferWriter<byte>();
        JsonWriterTarget target = new JsonWriterTarget();
        target.BufferWriter = destination;
        IJsonTokenWriter writer = new JsonUtf8Writer(options, target);
        return writer.WriteRawValue(input);
    }

    static void AssertBoundary(
        bool success,
        JsonDiagnostic? diagnostic,
        bool shouldFail,
        ParserLimitCase current,
        string path,
        int adjustment)
    {
        string context = current.Name + " " + path +
            " adjustment " + adjustment;
        Assert.That(success, Is.EqualTo(!shouldFail), context);
        if (shouldFail)
        {
            Assert.That(diagnostic?.Code, Is.EqualTo(current.Code), context);
        }
        else
        {
            Assert.That(diagnostic, Is.Null, context);
        }
    }

    static JsonValueResult FailedValue(JsonDiagnostic diagnostic)
    {
        JsonValueResult result = new JsonValueResult();
        result.Success = false;
        result.Status = JsonReadStatus.Error;
        result.Diagnostic = diagnostic;
        return result;
    }

    static JsonOperationResult FailedOperation(JsonDiagnostic diagnostic)
    {
        JsonOperationResult result = new JsonOperationResult();
        result.Success = false;
        result.Diagnostic = diagnostic;
        return result;
    }

    static JsonMember Member(string name, JsonValue value)
    {
        JsonMember member = new JsonMember();
        member.Name = name;
        member.Value = value;
        return member;
    }

    static byte[] Bytes(string value)
    {
        return System.Text.Encoding.UTF8.GetBytes(value);
    }

    class ParserLimitCase
    {
        internal string Name = "";
        internal string Json = "";
        internal int Actual;
        internal JsonDiagnosticCode Code;
        internal bool AffectsTokenValidation = true;
        internal bool AffectsModel;
        internal System.Action<JsonLimits, int> Configure =
            (value, maximum) =>
                value.MaximumDocumentBytes = maximum;
    }

    class WriterLimitCase
    {
        internal string Name = "";
        internal int Actual;
        internal JsonDiagnosticCode Code;
        internal System.Func<JsonValue> CreateValue =
            () => new JsonValueFactory().CreateNull();
        internal System.Action<JsonLimits, int> Configure =
            (value, maximum) =>
                value.MaximumOutputBytes = maximum;
    }

    class ExcerptCase
    {
        internal string Name = "";
        internal byte[] Input = global::System.Array.Empty<byte>();
        internal System.Action<JsonReaderOptions> Configure = options =>
            options.CaptureRawTokens = false;
        internal int Maximum;
    }

    class ContinuingHandler : IJsonTokenHandler
    {
        public JsonHandlerAction HandleToken(JsonToken token)
        {
            return JsonHandlerAction.Continue;
        }
    }
}
