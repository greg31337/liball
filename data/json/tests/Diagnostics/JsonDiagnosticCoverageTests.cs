namespace json.Tests;

public class JsonDiagnosticCoverageTests
{
    [Test]
    public void EveryDiagnosticCodeIsProvokedWithStableBoundedContext()
    {
        List<DiagnosticCase> cases = CreateCases();
        HashSet<JsonDiagnosticCode> observed = new HashSet<JsonDiagnosticCode>();

        foreach (DiagnosticCase current in cases)
        {
            JsonDiagnostic first = current.Create();
            JsonDiagnostic second = current.Create();
            AssertStableDiagnostic(current, first, second);
            observed.Add(first.Code);
        }

        Assert.That(
            observed.OrderBy(code => (int)code),
            Is.EqualTo(Enum.GetValues<JsonDiagnosticCode>()));
    }

    static List<DiagnosticCase> CreateCases()
    {
        List<DiagnosticCase> cases = new List<DiagnosticCase>();
        cases.Add(Case(JsonDiagnosticCode.None, () => new JsonDiagnostic()));
        cases.Add(ParseCase(JsonDiagnosticCode.UnexpectedCharacter, "/"));
        cases.Add(ParseCase(JsonDiagnosticCode.InvalidWhitespace, "\v"));
        cases.Add(ParseCase(JsonDiagnosticCode.InvalidLiteral, "truX"));
        cases.Add(ParseCase(JsonDiagnosticCode.InvalidNumber, "01"));
        cases.Add(ParseCase(JsonDiagnosticCode.InvalidStringCharacter, "\"\u0001\""));
        cases.Add(ParseCase(JsonDiagnosticCode.InvalidEscape, "\"\\q\""));
        cases.Add(ParseCase(JsonDiagnosticCode.InvalidUnicodeEscape, "\"\\u12X4\""));
        cases.Add(Case(JsonDiagnosticCode.InvalidSurrogatePair, () =>
            ParseFailure("\"\\ud800\"", options =>
                options.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar)));
        cases.Add(Case(JsonDiagnosticCode.InvalidUtf8, () => ParseFailure(
            new byte[] { 0x22, 0xC0, 0x22 }, null)));
        cases.Add(ParseCase(JsonDiagnosticCode.UnexpectedEndOfInput, "["));
        cases.Add(ParseCase(JsonDiagnosticCode.UnexpectedToken, "[}"));
        cases.Add(ParseCase(JsonDiagnosticCode.MissingPropertyName, "{:0}"));
        cases.Add(ParseCase(JsonDiagnosticCode.MissingColon, "{\"a\" 0}"));
        cases.Add(ParseCase(JsonDiagnosticCode.MissingCommaOrEnd, "[0 1]"));
        cases.Add(ParseCase(JsonDiagnosticCode.TrailingComma, "[0,]"));
        cases.Add(ParseCase(JsonDiagnosticCode.TrailingContent, "0 1"));
        cases.Add(Case(JsonDiagnosticCode.DuplicatePropertyName, () =>
            ParseFailure("{\"a\":0,\"a\":1}", options =>
                options.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.Reject)));
        cases.Add(Case(JsonDiagnosticCode.BomNotAllowed, () => ParseFailure(
            new byte[] { 0xEF, 0xBB, 0xBF, 0x6E, 0x75, 0x6C, 0x6C },
            null)));
        cases.Add(Case(JsonDiagnosticCode.UnexpectedBom, () => ParseFailure(
            new byte[] { 0x20, 0xEF, 0xBB, 0xBF, 0x6E, 0x75, 0x6C, 0x6C },
            options => options.BomPolicy = JsonBomPolicy.AllowInitial)));
        cases.Add(Case(JsonDiagnosticCode.MissingRecordSeparator, () =>
            ParseFailure("{}", SequenceOptions)));
        cases.Add(Case(JsonDiagnosticCode.EmptySequenceRecord, () => ParseFailure(
            new byte[] { 0x1E, 0x20, 0x1E },
            SequenceOptions)));
        cases.Add(Case(JsonDiagnosticCode.IncompleteSequenceRecord, () =>
            ParseFailure(new byte[] { 0x1E, 0x5B }, SequenceOptions)));

        cases.Add(LimitCase(
            JsonDiagnosticCode.DocumentByteLimitExceeded,
            "[0]",
            limits => limits.MaximumDocumentBytes = 2));
        cases.Add(LimitCase(
            JsonDiagnosticCode.InputBufferLimitExceeded,
            "[0]",
            limits => limits.MaximumInputBufferBytes = 2));
        cases.Add(LimitCase(
            JsonDiagnosticCode.TokenByteLimitExceeded,
            "\"ab\"",
            limits => limits.MaximumTokenBytes = 3));
        cases.Add(LimitCase(
            JsonDiagnosticCode.DecodedStringByteLimitExceeded,
            "\"€\"",
            limits => limits.MaximumDecodedStringBytes = 2));
        cases.Add(LimitCase(
            JsonDiagnosticCode.DecodedStringCodeUnitLimitExceeded,
            "\"ab\"",
            limits => limits.MaximumDecodedStringCodeUnits = 1));
        cases.Add(LimitCase(
            JsonDiagnosticCode.NumberCharacterLimitExceeded,
            "1234",
            limits => limits.MaximumNumberCharacters = 3));
        cases.Add(LimitCase(
            JsonDiagnosticCode.NestingDepthLimitExceeded,
            "[[]]",
            limits => limits.MaximumNestingDepth = 1));
        cases.Add(LimitCase(
            JsonDiagnosticCode.ArrayItemLimitExceeded,
            "[0,1]",
            limits => limits.MaximumArrayItems = 1));
        cases.Add(LimitCase(
            JsonDiagnosticCode.ObjectMemberLimitExceeded,
            "{\"a\":0,\"b\":1}",
            limits => limits.MaximumObjectMembers = 1));
        cases.Add(LimitCase(
            JsonDiagnosticCode.DomNodeLimitExceeded,
            "{\"a\":0}",
            limits => limits.MaximumDomNodes = 2));
        cases.Add(Case(JsonDiagnosticCode.OutputByteLimitExceeded,
            OutputLimitFailure));

        cases.Add(Case(JsonDiagnosticCode.InvalidPointerSyntax, () =>
            PointerCodecFailure("not-a-pointer", "syntax")));
        cases.Add(Case(JsonDiagnosticCode.InvalidPointerEscape, () =>
            PointerCodecFailure("/~2", "escape")));
        cases.Add(Case(JsonDiagnosticCode.InvalidPointerPercentEncoding, () =>
            PointerCodecFailure("#/%GG", "percent")));
        cases.Add(Case(JsonDiagnosticCode.InvalidPointerIndex, () =>
            PointerNavigationFailure("01")));
        cases.Add(Case(JsonDiagnosticCode.PointerIndexOutOfRange, () =>
            PointerNavigationFailure("2147483648")));
        cases.Add(Case(JsonDiagnosticCode.PointerTargetNotFound, () =>
            PointerNavigationFailure("2")));
        cases.Add(Case(JsonDiagnosticCode.PointerTypeMismatch,
            PointerTypeFailure));

        cases.Add(Case(JsonDiagnosticCode.InvalidModel, InvalidModelFailure));
        cases.Add(Case(JsonDiagnosticCode.ModelCycle, ModelCycleFailure));
        cases.Add(Case(JsonDiagnosticCode.InvalidWriterState,
            InvalidWriterStateFailure));
        cases.Add(Case(JsonDiagnosticCode.InvalidRawValue,
            InvalidRawValueFailure));
        cases.Add(Case(JsonDiagnosticCode.NonFiniteNumber,
            NonFiniteNumberFailure));
        cases.Add(Case(JsonDiagnosticCode.NumericConversionFailed,
            NumericConversionFailure));
        cases.Add(Case(JsonDiagnosticCode.InvalidOptions, InvalidOptionsFailure));
        cases.Add(Case(JsonDiagnosticCode.IoFailure, IoFailure));
        return cases;
    }

    static DiagnosticCase ParseCase(JsonDiagnosticCode code, string text)
    {
        return Case(code, () => ParseFailure(text, null));
    }

    static DiagnosticCase LimitCase(
        JsonDiagnosticCode code,
        string text,
        System.Action<JsonLimits> configure)
    {
        return Case(code, () => ParseFailure(text, options =>
        {
            JsonLimits limits = new JsonLimits();
            configure(limits);
            options.Limits = limits;
        }));
    }

    static DiagnosticCase Case(
        JsonDiagnosticCode code,
        System.Func<JsonDiagnostic> create)
    {
        DiagnosticCase result = new DiagnosticCase();
        result.Code = code;
        result.Create = create;
        return result;
    }

    static JsonDiagnostic ParseFailure(
        string text,
        System.Action<JsonReaderOptions>? configure)
    {
        return ParseFailure(System.Text.Encoding.UTF8.GetBytes(text), configure);
    }

    static JsonDiagnostic ParseFailure(
        byte[] input,
        System.Action<JsonReaderOptions>? configure)
    {
        JsonReaderOptions options = new JsonReaderOptions();
        if (configure != null)
        {
            configure(options);
        }

        JsonLimits limits = options.Limits ?? new JsonLimits();
        limits.MaximumDiagnosticExcerptBytes = 8;
        options.Limits = limits;
        JsonDocumentResult result = new JsonDocumentParser().TryParseUtf8(
            input,
            options);
        return result.Diagnostic
            ?? throw new InvalidOperationException("Expected parse diagnostic.");
    }

    static void SequenceOptions(JsonReaderOptions options)
    {
        options.InputMode = JsonInputMode.JsonTextSequence;
    }

    static JsonDiagnostic OutputLimitFailure()
    {
        JsonLimits limits = new JsonLimits();
        limits.MaximumOutputBytes = 3;
        JsonWriterOptions options = new JsonWriterOptions();
        options.Limits = limits;
        IJsonTokenWriter writer = CreateWriter(options);
        return writer.WriteNull().Diagnostic
            ?? throw new InvalidOperationException("Expected output diagnostic.");
    }

    static JsonDiagnostic PointerCodecFailure(string value, string kind)
    {
        IJsonPointerCodec codec = new JsonPointerCodec();
        try
        {
            if (kind == "percent")
            {
                codec.ParseUriFragment(value);
            }
            else
            {
                codec.ParseStringPointer(value);
            }
        }
        catch (JsonPointerException exception)
        {
            return exception.Diagnostic;
        }

        throw new InvalidOperationException("Expected pointer diagnostic.");
    }

    static JsonDiagnostic PointerNavigationFailure(string token)
    {
        JsonValue root = new JsonValueFactory().CreateArray(new List<JsonValue>
        {
            new JsonValueFactory().CreateNull()
        });
        JsonPointer pointer = new JsonPointerCodec().ParseStringPointer("/" + token);
        JsonPointerResult result = new JsonPointerNavigator().TryResolvePointer(
            root,
            pointer,
            JsonDuplicateKeyPolicy.Preserve);
        return result.Diagnostic
            ?? throw new InvalidOperationException("Expected navigation diagnostic.");
    }

    static JsonDiagnostic PointerTypeFailure()
    {
        JsonValue root = new JsonValueFactory().CreateString("scalar");
        JsonPointer pointer = new JsonPointerCodec().ParseStringPointer("/child");
        JsonPointerResult result = new JsonPointerNavigator().TryResolvePointer(
            root,
            pointer,
            JsonDuplicateKeyPolicy.Preserve);
        return result.Diagnostic
            ?? throw new InvalidOperationException("Expected type diagnostic.");
    }

    static JsonDiagnostic InvalidModelFailure()
    {
        JsonOperationResult result = new JsonValueValidator().ValidateValue(
            new JsonValue(),
            new JsonLimits());
        return result.Diagnostic
            ?? throw new InvalidOperationException("Expected model diagnostic.");
    }

    static JsonDiagnostic ModelCycleFailure()
    {
        JsonValue cyclic = new JsonValue();
        cyclic.Kind = JsonValueKind.Array;
        cyclic.ArrayItems = new List<JsonValue> { cyclic };
        JsonOperationResult result = new JsonValueValidator().ValidateValue(
            cyclic,
            new JsonLimits());
        return result.Diagnostic
            ?? throw new InvalidOperationException("Expected cycle diagnostic.");
    }

    static JsonDiagnostic InvalidWriterStateFailure()
    {
        return CreateWriter(new JsonWriterOptions()).WriteEndArray().Diagnostic
            ?? throw new InvalidOperationException("Expected writer-state diagnostic.");
    }

    static JsonDiagnostic InvalidRawValueFailure()
    {
        return CreateWriter(new JsonWriterOptions())
            .WriteRawValue(System.Text.Encoding.ASCII.GetBytes("0 1"))
            .Diagnostic
            ?? throw new InvalidOperationException("Expected raw-value diagnostic.");
    }

    static JsonDiagnostic NonFiniteNumberFailure()
    {
        return CreateWriter(new JsonWriterOptions()).WriteDouble(double.NaN).Diagnostic
            ?? throw new InvalidOperationException("Expected finite-number diagnostic.");
    }

    static JsonDiagnostic NumericConversionFailure()
    {
        JsonNumber number = new JsonValueFactory().CreateNumber("1.5").Number
            ?? throw new InvalidOperationException("Expected number.");
        return new JsonNumberConverter().TryInt64(number).Diagnostic
            ?? throw new InvalidOperationException("Expected conversion diagnostic.");
    }

    static JsonDiagnostic InvalidOptionsFailure()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.TrackUnicodeScalarColumn = true;
        JsonOperationResult result = new JsonUtf8Validator().ValidateUtf8(
            System.Text.Encoding.ASCII.GetBytes("null"),
            options);
        return result.Diagnostic
            ?? throw new InvalidOperationException("Expected options diagnostic.");
    }

    static JsonDiagnostic IoFailure()
    {
        JsonOperationResult result = new JsonUtf8Validator().ValidateStream(
            new ThrowingReadStream(),
            new JsonReaderOptions());
        return result.Diagnostic
            ?? throw new InvalidOperationException("Expected I/O diagnostic.");
    }

    static IJsonTokenWriter CreateWriter(JsonWriterOptions options)
    {
        JsonWriterTarget target = new JsonWriterTarget();
        target.BufferWriter = new System.Buffers.ArrayBufferWriter<byte>();
        return new JsonUtf8Writer(options, target);
    }

    static void AssertStableDiagnostic(
        DiagnosticCase current,
        JsonDiagnostic first,
        JsonDiagnostic second)
    {
        Assert.Multiple(() =>
        {
            Assert.That(first.Code, Is.EqualTo(current.Code), current.Code.ToString());
            Assert.That(second.Code, Is.EqualTo(first.Code), current.Code.ToString());
            Assert.That(second.Message, Is.EqualTo(first.Message), current.Code.ToString());
            Assert.That(second.Pointer, Is.EqualTo(first.Pointer), current.Code.ToString());
            Assert.That(second.Depth, Is.EqualTo(first.Depth), current.Code.ToString());
            Assert.That(second.ExpectedTokens,
                Is.EqualTo(first.ExpectedTokens), current.Code.ToString());
            Assert.That(second.ActualToken,
                Is.EqualTo(first.ActualToken), current.Code.ToString());
            Assert.That(second.SourceExcerpt,
                Is.EqualTo(first.SourceExcerpt), current.Code.ToString());
            AssertRangeEqual(first.Range, second.Range, current.Code);
            Assert.That(first.Depth, Is.GreaterThanOrEqualTo(0), current.Code.ToString());
            Assert.That(first.ExpectedTokens == null ||
                first.ExpectedTokens.Length <= 256, Is.True, current.Code.ToString());
            Assert.That(first.ActualToken == null ||
                first.ActualToken.Length <= 256, Is.True, current.Code.ToString());
            Assert.That(first.SourceExcerpt == null ||
                first.SourceExcerpt.Length <= JsonDefaults.MaximumDiagnosticExcerptBytes,
                Is.True,
                current.Code.ToString());
            Assert.That(first.Pointer == null || first.Pointer == "" ||
                first.Pointer.StartsWith("/", StringComparison.Ordinal),
                Is.True,
                current.Code.ToString());
            if (current.Code == JsonDiagnosticCode.None)
            {
                Assert.That(first.Message, Is.Null);
            }
            else
            {
                Assert.That(first.Message, Is.Not.Null.And.Not.Empty,
                    current.Code.ToString());
            }
        });
    }

    static void AssertRangeEqual(
        JsonSourceRange? first,
        JsonSourceRange? second,
        JsonDiagnosticCode code)
    {
        Assert.That(second == null, Is.EqualTo(first == null), code.ToString());
        if (first == null || second == null)
        {
            return;
        }

        JsonSourcePosition firstStart = first.Start
            ?? throw new InvalidOperationException("Expected range start.");
        JsonSourcePosition firstEnd = first.End
            ?? throw new InvalidOperationException("Expected range end.");
        JsonSourcePosition secondStart = second.Start
            ?? throw new InvalidOperationException("Expected range start.");
        JsonSourcePosition secondEnd = second.End
            ?? throw new InvalidOperationException("Expected range end.");
        Assert.Multiple(() =>
        {
            Assert.That(secondStart.ByteOffset,
                Is.EqualTo(firstStart.ByteOffset), code.ToString());
            Assert.That(secondEnd.ByteOffset,
                Is.EqualTo(firstEnd.ByteOffset), code.ToString());
            Assert.That(secondStart.Line, Is.EqualTo(firstStart.Line), code.ToString());
            Assert.That(secondEnd.Line, Is.EqualTo(firstEnd.Line), code.ToString());
            Assert.That(firstStart.ByteOffset,
                Is.GreaterThanOrEqualTo(0), code.ToString());
            Assert.That(firstEnd.ByteOffset,
                Is.GreaterThanOrEqualTo(firstStart.ByteOffset), code.ToString());
        });
    }

    class DiagnosticCase
    {
        internal JsonDiagnosticCode Code;
        internal System.Func<JsonDiagnostic> Create = () => new JsonDiagnostic();
    }

    class ThrowingReadStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => 0;
        public override long Position
        {
            get => 0;
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new IOException("read failed");
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
