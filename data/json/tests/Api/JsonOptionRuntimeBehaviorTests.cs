namespace json.Tests;

public class JsonOptionRuntimeBehaviorTests
{
    [Test]
    public void EveryConformanceProfileSelectsItsDocumentRules()
    {
        JsonDocumentResult syntax = Parse(
            "\"\\ud800\"",
            ProfileOptions(JsonConformanceProfile.SyntaxCompatible));
        JsonDocumentResult portable = Parse(
            "\"\\ufdd0\"",
            ProfileOptions(JsonConformanceProfile.Portable));
        JsonDocumentResult iJson = Parse(
            "\"\\ufdd0\"",
            ProfileOptions(JsonConformanceProfile.IJson));
        JsonDocumentResult jcs = Parse(
            "\"\\ufdd0\"",
            ProfileOptions(JsonConformanceProfile.Jcs));

        Assert.Multiple(() =>
        {
            Assert.That(syntax.Success, Is.True);
            Assert.That(syntax.Document?.Root?.StringValue, Is.EqualTo("\ud800"));
            Assert.That(portable.Success, Is.True);
            Assert.That(portable.Document?.Root?.StringValue, Is.EqualTo("\ufdd0"));
            Assert.That(iJson.Success, Is.False);
            Assert.That(iJson.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidStringCharacter));
            Assert.That(jcs.Success, Is.False);
            Assert.That(jcs.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidStringCharacter));
        });
    }

    [TestCase(JsonDuplicateKeyPolicy.Preserve, true, "0,1")]
    [TestCase(JsonDuplicateKeyPolicy.Reject, false, "")]
    [TestCase(JsonDuplicateKeyPolicy.KeepFirst, true, "0")]
    [TestCase(JsonDuplicateKeyPolicy.KeepLast, true, "1")]
    public void EveryReaderDuplicatePolicyChangesMaterialization(
        JsonDuplicateKeyPolicy policy,
        bool expectedSuccess,
        string expectedValues)
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.DuplicateKeyPolicy = policy;

        JsonDocumentResult result = Parse("{\"a\":0,\"a\":1}", options);
        string values = result.Document?.Root?.ObjectMembers == null
            ? ""
            : string.Join(",", result.Document.Root.ObjectMembers
                .Select(member => member.Value?.Number?.Lexeme));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.EqualTo(expectedSuccess));
            Assert.That(values, Is.EqualTo(expectedValues));
            if (expectedSuccess)
            {
                Assert.That(result.Diagnostic, Is.Null);
            }
            else
            {
                Assert.That(result.Diagnostic?.Code,
                    Is.EqualTo(JsonDiagnosticCode.DuplicatePropertyName));
            }
        });
    }

    [TestCase(JsonInputMode.SingleValue, "0 1", false)]
    [TestCase(JsonInputMode.WhitespaceSeparated, "0 1", true)]
    [TestCase(JsonInputMode.JsonTextSequence, "\u001e0\n\u001e1\n", true)]
    public void EveryInputModeSelectsItsFraming(
        JsonInputMode mode,
        string text,
        bool expectedSuccess)
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.InputMode = mode;

        JsonOperationResult result = new JsonUtf8Validator().ValidateUtf8(
            Bytes(text),
            options);

        Assert.That(result.Success, Is.EqualTo(expectedSuccess));
    }

    [Test]
    public void SequenceErrorPolicySelectsStopOrRecovery()
    {
        byte[] input = new byte[]
        {
            0x1E, 0x31, 0x1E, 0x7B, 0x7D, 0x0A
        };
        JsonReaderOptions stopOptions = new JsonReaderOptions();
        stopOptions.InputMode = JsonInputMode.JsonTextSequence;
        IJsonPullReader stopped = new JsonPullReader(stopOptions);
        JsonFeedResult stopResult = stopped.FeedUtf8(input);

        JsonReaderOptions recoverOptions = new JsonReaderOptions();
        recoverOptions.InputMode = JsonInputMode.JsonTextSequence;
        recoverOptions.SequenceErrorPolicy = JsonSequenceErrorPolicy.Resynchronize;
        IJsonPullReader recovered = new JsonPullReader(recoverOptions);
        JsonFeedResult recoverError = recovered.FeedUtf8(input);
        JsonTokenResult consumedError = recovered.ReadToken();
        JsonTokenResult objectStart = recovered.ReadToken();
        JsonTokenResult objectEnd = recovered.ReadToken();
        JsonTokenResult boundary = recovered.ReadToken();

        Assert.Multiple(() =>
        {
            Assert.That(stopResult.ReadStatus, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(stopped.ReadToken().Status, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(recoverError.ReadStatus, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(consumedError.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.IncompleteSequenceRecord));
            Assert.That(objectStart.Token?.Kind,
                Is.EqualTo(JsonTokenKind.StartObject));
            Assert.That(objectEnd.Token?.Kind,
                Is.EqualTo(JsonTokenKind.EndObject));
            Assert.That(boundary.Status,
                Is.EqualTo(JsonReadStatus.DocumentComplete));
            Assert.That(recovered.CompleteInput().ReadStatus,
                Is.EqualTo(JsonReadStatus.StreamComplete));
        });
    }

    [Test]
    public void UnicodeAndBomPoliciesChangeAcceptance()
    {
        JsonReaderOptions compatible = new JsonReaderOptions();
        JsonReaderOptions scalar = new JsonReaderOptions();
        scalar.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
        JsonReaderOptions rejectBom = new JsonReaderOptions();
        JsonReaderOptions allowBom = new JsonReaderOptions();
        allowBom.BomPolicy = JsonBomPolicy.AllowInitial;
        byte[] bomNull = new byte[] { 0xEF, 0xBB, 0xBF, 0x6E, 0x75, 0x6C, 0x6C };

        JsonOperationResult compatibleResult = Validate("\"\\ud800\"", compatible);
        JsonOperationResult scalarResult = Validate("\"\\ud800\"", scalar);
        JsonOperationResult rejected = new JsonUtf8Validator().ValidateUtf8(
            bomNull,
            rejectBom);
        JsonOperationResult allowed = new JsonUtf8Validator().ValidateUtf8(
            bomNull,
            allowBom);

        Assert.Multiple(() =>
        {
            Assert.That(compatibleResult.Success, Is.True);
            Assert.That(scalarResult.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidSurrogatePair));
            Assert.That(rejected.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.BomNotAllowed));
            Assert.That(allowed.Success, Is.True);
        });
    }

    [Test]
    public void ReaderCaptureAndLocationFlagsChangeTokenAndDomData()
    {
        JsonReaderOptions plainOptions = new JsonReaderOptions();
        JsonToken plain = ReadFirstToken("\"é\"", plainOptions);
        JsonDocument plainDocument = new JsonDocumentParser().ParseString(
            "[0]",
            plainOptions);

        JsonReaderOptions detailedOptions = new JsonReaderOptions();
        detailedOptions.TrackLineInformation = true;
        detailedOptions.TrackUnicodeScalarColumn = true;
        detailedOptions.CaptureRawTokens = true;
        detailedOptions.CaptureSourceRanges = true;
        JsonToken detailed = ReadFirstToken("\"é\"", detailedOptions);
        JsonDocument detailedDocument = new JsonDocumentParser().ParseString(
            "[0]",
            detailedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(plain.RawUtf8, Is.Null);
            Assert.That(plain.Range?.Start?.Line, Is.Zero);
            Assert.That(plain.Range?.End?.ByteColumn, Is.Zero);
            Assert.That(plain.Range?.End?.ScalarColumn, Is.Zero);
            Assert.That(plainDocument.Root?.Range, Is.Null);
            Assert.That(detailed.RawUtf8, Is.EqualTo(Bytes("\"é\"")));
            Assert.That(detailed.Range?.Start?.Line, Is.EqualTo(1));
            Assert.That(detailed.Range?.End?.ByteColumn, Is.EqualTo(5));
            Assert.That(detailed.Range?.End?.ScalarColumn, Is.EqualTo(4));
            Assert.That(detailedDocument.Root?.Range, Is.Not.Null);
            Assert.That(detailedDocument.Root?.ArrayItems?[0].Range, Is.Not.Null);
        });
    }

    [Test]
    public void StreamBufferBytesChangesStreamReadGranularity()
    {
        byte[] input = Bytes("[0,1,2,3]");
        RecordingReadStream oneByteStream = new RecordingReadStream(input);
        JsonReaderOptions oneByteOptions = new JsonReaderOptions();
        oneByteOptions.StreamBufferBytes = 1;
        JsonOperationResult oneByte = new JsonUtf8Validator().ValidateStream(
            oneByteStream,
            oneByteOptions);

        RecordingReadStream wholeStream = new RecordingReadStream(input);
        JsonReaderOptions wholeOptions = new JsonReaderOptions();
        wholeOptions.StreamBufferBytes = input.Length;
        JsonOperationResult whole = new JsonUtf8Validator().ValidateStream(
            wholeStream,
            wholeOptions);

        Assert.Multiple(() =>
        {
            Assert.That(oneByte.Success, Is.True);
            Assert.That(whole.Success, Is.True);
            Assert.That(oneByteStream.MaximumRequestedBytes, Is.EqualTo(1));
            Assert.That(wholeStream.MaximumRequestedBytes,
                Is.EqualTo(input.Length));
            Assert.That(oneByteStream.ReadCount, Is.GreaterThan(wholeStream.ReadCount));
        });
    }

    [TestCase(nameof(JsonLimits.MaximumDocumentBytes), JsonDiagnosticCode.DocumentByteLimitExceeded)]
    [TestCase(nameof(JsonLimits.MaximumInputBufferBytes), JsonDiagnosticCode.InputBufferLimitExceeded)]
    [TestCase(nameof(JsonLimits.MaximumTokenBytes), JsonDiagnosticCode.TokenByteLimitExceeded)]
    [TestCase(nameof(JsonLimits.MaximumDecodedStringBytes), JsonDiagnosticCode.DecodedStringByteLimitExceeded)]
    [TestCase(nameof(JsonLimits.MaximumDecodedStringCodeUnits), JsonDiagnosticCode.DecodedStringCodeUnitLimitExceeded)]
    [TestCase(nameof(JsonLimits.MaximumNumberCharacters), JsonDiagnosticCode.NumberCharacterLimitExceeded)]
    [TestCase(nameof(JsonLimits.MaximumNestingDepth), JsonDiagnosticCode.NestingDepthLimitExceeded)]
    [TestCase(nameof(JsonLimits.MaximumArrayItems), JsonDiagnosticCode.ArrayItemLimitExceeded)]
    [TestCase(nameof(JsonLimits.MaximumObjectMembers), JsonDiagnosticCode.ObjectMemberLimitExceeded)]
    [TestCase(nameof(JsonLimits.MaximumDomNodes), JsonDiagnosticCode.DomNodeLimitExceeded)]
    [TestCase(nameof(JsonLimits.MaximumOutputBytes), JsonDiagnosticCode.OutputByteLimitExceeded)]
    [TestCase(nameof(JsonLimits.MaximumDiagnosticExcerptBytes), JsonDiagnosticCode.InvalidNumber)]
    public void EveryLimitFieldChangesItsRuntimeBoundary(
        string field,
        JsonDiagnosticCode expectedCode)
    {
        JsonDiagnostic diagnostic = ExerciseLimit(field);

        Assert.That(diagnostic.Code, Is.EqualTo(expectedCode), field);
        if (field == nameof(JsonLimits.MaximumDiagnosticExcerptBytes))
        {
            Assert.That(diagnostic.SourceExcerpt, Has.Length.EqualTo(2));
        }
    }

    [Test]
    public void WriterFormattingFieldsChangeLayout()
    {
        JsonDocument document = Document(Object(
            Member("a", Array(Number("0")))));
        string compact = Serialize(document, new JsonWriterOptions());

        JsonWriterOptions prettyOptions = new JsonWriterOptions();
        prettyOptions.Formatting = JsonFormatting.Pretty;
        prettyOptions.Indentation = "\t";
        prettyOptions.NewLine = "\r\n";
        prettyOptions.OmitSpaceAfterColon = true;
        string pretty = Serialize(document, prettyOptions);

        Assert.Multiple(() =>
        {
            Assert.That(compact, Is.EqualTo("{\"a\":[0]}"));
            Assert.That(pretty, Is.EqualTo(
                "{\r\n\t\"a\":[\r\n\t\t0\r\n\t]\r\n}"));
        });
    }

    [Test]
    public void WriterEscapeFieldsChangeEncodedStrings()
    {
        JsonWriterOptions minimalOptions = new JsonWriterOptions();
        JsonWriterOptions asciiOptions = new JsonWriterOptions();
        asciiOptions.StringEscapeMode = JsonStringEscapeMode.AsciiOnly;
        JsonWriterOptions htmlOptions = new JsonWriterOptions();
        htmlOptions.StringEscapeMode = JsonStringEscapeMode.HtmlSafe;
        JsonWriterOptions slashOptions = new JsonWriterOptions();
        slashOptions.EscapeSolidus = true;
        JsonWriterOptions uppercaseOptions = new JsonWriterOptions();
        uppercaseOptions.StringEscapeMode = JsonStringEscapeMode.AsciiOnly;
        uppercaseOptions.UseUppercaseHexadecimalEscapes = true;
        JsonDocument text = Document(String("<é>/"));

        Assert.Multiple(() =>
        {
            Assert.That(Serialize(text, minimalOptions), Is.EqualTo("\"<é>/\""));
            Assert.That(Serialize(text, asciiOptions),
                Is.EqualTo("\"<\\u00e9>/\""));
            Assert.That(Serialize(text, htmlOptions),
                Is.EqualTo("\"\\u003cé\\u003e/\""));
            Assert.That(Serialize(text, slashOptions), Is.EqualTo("\"<é>\\/\""));
            Assert.That(Serialize(text, uppercaseOptions),
                Is.EqualTo("\"<\\u00E9>/\""));
        });
    }

    [Test]
    public void EveryPropertyOrderSelectsItsComparison()
    {
        string supplementary = char.ConvertFromUtf32(0x10000);
        string privateUse = "\ue000";
        JsonDocument document = Document(Object(
            Member(privateUse, Number("0")),
            Member(supplementary, Number("1"))));

        string preserve = Serialize(document,
            WriterOptions(JsonPropertyOrder.Preserve));
        string utf16 = Serialize(document,
            WriterOptions(JsonPropertyOrder.OrdinalUtf16));
        string scalar = Serialize(document,
            WriterOptions(JsonPropertyOrder.OrdinalUnicodeScalar));
        string jcs = Serialize(document,
            WriterOptions(JsonPropertyOrder.Jcs));

        Assert.Multiple(() =>
        {
            Assert.That(preserve,
                Is.EqualTo("{\"" + privateUse + "\":0,\"" + supplementary + "\":1}"));
            Assert.That(utf16,
                Is.EqualTo("{\"" + supplementary + "\":1,\"" + privateUse + "\":0}"));
            Assert.That(scalar, Is.EqualTo(preserve));
            Assert.That(jcs, Is.EqualTo(utf16));
        });
    }

    [TestCase(JsonDuplicateKeyPolicy.Preserve, true, "{\"a\":0,\"a\":1}")]
    [TestCase(JsonDuplicateKeyPolicy.Reject, false, "")]
    [TestCase(JsonDuplicateKeyPolicy.KeepFirst, true, "{\"a\":0}")]
    [TestCase(JsonDuplicateKeyPolicy.KeepLast, true, "{\"a\":1}")]
    public void EveryWriterDuplicatePolicyChangesOutput(
        JsonDuplicateKeyPolicy policy,
        bool expectedSuccess,
        string expectedText)
    {
        JsonDocument document = Document(Object(
            Member("a", Number("0")),
            Member("a", Number("1"))));
        JsonWriterOptions options = new JsonWriterOptions();
        options.DuplicateKeyPolicy = policy;
        System.Buffers.ArrayBufferWriter<byte> destination =
            new System.Buffers.ArrayBufferWriter<byte>();
        JsonOperationResult result = new JsonDocumentSerializer().SerializeTarget(
            document,
            options,
            BufferTarget(destination));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.EqualTo(expectedSuccess));
            Assert.That(Text(destination.WrittenSpan.ToArray()),
                Is.EqualTo(expectedText));
            if (expectedSuccess)
            {
                Assert.That(result.Diagnostic, Is.Null);
            }
            else
            {
                Assert.That(result.Diagnostic?.Code,
                    Is.EqualTo(JsonDiagnosticCode.DuplicatePropertyName));
            }
        });
    }

    [Test]
    public void WriterUnicodePolicyChangesLoneSurrogateHandling()
    {
        JsonWriterOptions compatibleOptions = new JsonWriterOptions();
        JsonWriterOptions scalarOptions = new JsonWriterOptions();
        scalarOptions.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
        System.Buffers.ArrayBufferWriter<byte> compatibleDestination =
            new System.Buffers.ArrayBufferWriter<byte>();
        IJsonTokenWriter compatibleWriter = new JsonUtf8Writer(
            compatibleOptions,
            BufferTarget(compatibleDestination));
        JsonOperationResult compatible = compatibleWriter.WriteString("\ud800");
        Assert.That(compatibleWriter.CompleteWriter().Success, Is.True);

        System.Buffers.ArrayBufferWriter<byte> scalarDestination =
            new System.Buffers.ArrayBufferWriter<byte>();
        IJsonTokenWriter scalarWriter = new JsonUtf8Writer(
            scalarOptions,
            BufferTarget(scalarDestination));
        JsonOperationResult scalar = scalarWriter.WriteString("\ud800");

        Assert.Multiple(() =>
        {
            Assert.That(compatible.Success, Is.True);
            Assert.That(Text(compatibleDestination.WrittenSpan.ToArray()),
                Is.EqualTo("\"\\ud800\""));
            Assert.That(scalar.Success, Is.False);
            Assert.That(scalar.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidSurrogatePair));
        });
    }

    [Test]
    public void EveryOutputModeSelectsItsFraming()
    {
        Assert.Multiple(() =>
        {
            Assert.That(WriteTwoValues(JsonInputMode.SingleValue),
                Is.EqualTo("null"));
            Assert.That(WriteTwoValues(JsonInputMode.WhitespaceSeparated),
                Is.EqualTo("null\ntrue"));
            Assert.That(WriteTwoValues(JsonInputMode.JsonTextSequence),
                Is.EqualTo("\u001enull\n\u001etrue\n"));
        });
    }

    [Test]
    public void SkipModelValidationChangesDocumentMetadataValidation()
    {
        JsonDocument document = Document(Null());
        document.SourceByteCount = -1;
        System.Buffers.ArrayBufferWriter<byte> rejectedDestination =
            new System.Buffers.ArrayBufferWriter<byte>();
        JsonOperationResult rejected = new JsonDocumentSerializer().SerializeTarget(
            document,
            new JsonWriterOptions(),
            BufferTarget(rejectedDestination));

        JsonWriterOptions skippedOptions = new JsonWriterOptions();
        skippedOptions.SkipModelValidation = true;
        string skipped = Serialize(document, skippedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
            Assert.That(skipped, Is.EqualTo("null"));
        });
    }

    [Test]
    public void WriterLimitsAndBothTargetFieldsChangeTheDestinationAndBoundary()
    {
        JsonWriterOptions limitedOptions = new JsonWriterOptions();
        limitedOptions.Limits = new JsonLimits();
        limitedOptions.Limits.MaximumOutputBytes = 3;
        System.Buffers.ArrayBufferWriter<byte> limitedDestination =
            new System.Buffers.ArrayBufferWriter<byte>();
        JsonOperationResult limited = new JsonDocumentSerializer().SerializeTarget(
            Document(Null()),
            limitedOptions,
            BufferTarget(limitedDestination));

        MemoryStream stream = new MemoryStream();
        JsonWriterTarget streamTarget = new JsonWriterTarget();
        streamTarget.Stream = stream;
        JsonOperationResult streamResult = new JsonDocumentSerializer().SerializeTarget(
            Document(Boolean(true)),
            new JsonWriterOptions(),
            streamTarget);
        System.Buffers.ArrayBufferWriter<byte> buffer =
            new System.Buffers.ArrayBufferWriter<byte>();
        JsonOperationResult bufferResult = new JsonDocumentSerializer().SerializeTarget(
            Document(Boolean(false)),
            new JsonWriterOptions(),
            BufferTarget(buffer));

        Assert.Multiple(() =>
        {
            Assert.That(limited.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.OutputByteLimitExceeded));
            Assert.That(streamResult.Success, Is.True);
            Assert.That(Text(stream.ToArray()), Is.EqualTo("true"));
            Assert.That(bufferResult.Success, Is.True);
            Assert.That(Text(buffer.WrittenSpan.ToArray()), Is.EqualTo("false"));
        });
    }

    static JsonDiagnostic ExerciseLimit(string field)
    {
        JsonLimits limits = new JsonLimits();
        string text = "null";
        if (field == nameof(JsonLimits.MaximumDocumentBytes))
        {
            limits.MaximumDocumentBytes = 3;
        }
        else if (field == nameof(JsonLimits.MaximumInputBufferBytes))
        {
            limits.MaximumInputBufferBytes = 3;
        }
        else if (field == nameof(JsonLimits.MaximumTokenBytes))
        {
            limits.MaximumTokenBytes = 4;
            text = "\"abc\"";
        }
        else if (field == nameof(JsonLimits.MaximumDecodedStringBytes))
        {
            limits.MaximumDecodedStringBytes = 1;
            text = "\"é\"";
        }
        else if (field == nameof(JsonLimits.MaximumDecodedStringCodeUnits))
        {
            limits.MaximumDecodedStringCodeUnits = 1;
            text = "\"😀\"";
        }
        else if (field == nameof(JsonLimits.MaximumNumberCharacters))
        {
            limits.MaximumNumberCharacters = 3;
            text = "-123";
        }
        else if (field == nameof(JsonLimits.MaximumNestingDepth))
        {
            limits.MaximumNestingDepth = 1;
            text = "[[]]";
        }
        else if (field == nameof(JsonLimits.MaximumArrayItems))
        {
            limits.MaximumArrayItems = 1;
            text = "[0,1]";
        }
        else if (field == nameof(JsonLimits.MaximumObjectMembers))
        {
            limits.MaximumObjectMembers = 1;
            text = "{\"a\":0,\"b\":1}";
        }
        else if (field == nameof(JsonLimits.MaximumDomNodes))
        {
            limits.MaximumDomNodes = 2;
            text = "{\"a\":0}";
        }
        else if (field == nameof(JsonLimits.MaximumOutputBytes))
        {
            limits.MaximumOutputBytes = 3;
            JsonWriterOptions writerOptions = new JsonWriterOptions();
            writerOptions.Limits = limits;
            System.Buffers.ArrayBufferWriter<byte> destination =
                new System.Buffers.ArrayBufferWriter<byte>();
            JsonOperationResult output = new JsonDocumentSerializer().SerializeTarget(
                Document(Null()),
                writerOptions,
                BufferTarget(destination));
            return output.Diagnostic
                ?? throw new InvalidOperationException("Expected output limit diagnostic.");
        }
        else
        {
            limits.MaximumDiagnosticExcerptBytes = 2;
            text = "12345x";
        }

        JsonReaderOptions options = new JsonReaderOptions();
        options.Limits = limits;
        JsonDocumentResult result = new JsonDocumentParser().TryParseUtf8(
            Bytes(text),
            options);
        return result.Diagnostic
            ?? throw new InvalidOperationException("Expected input limit diagnostic.");
    }

    static JsonReaderOptions ProfileOptions(JsonConformanceProfile profile)
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.ConformanceProfile = profile;
        if (profile != JsonConformanceProfile.SyntaxCompatible)
        {
            options.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.Reject;
            options.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
        }

        return options;
    }

    static JsonDocumentResult Parse(string text, JsonReaderOptions options)
    {
        return new JsonDocumentParser().TryParseUtf8(Bytes(text), options);
    }

    static JsonOperationResult Validate(string text, JsonReaderOptions options)
    {
        return new JsonUtf8Validator().ValidateUtf8(Bytes(text), options);
    }

    static JsonToken ReadFirstToken(string text, JsonReaderOptions options)
    {
        IJsonPullReader reader = new JsonPullReader(options);
        reader.FeedUtf8(Bytes(text));
        reader.CompleteInput();
        return reader.ReadToken().Token
            ?? throw new InvalidOperationException("Expected token.");
    }

    static JsonWriterOptions WriterOptions(JsonPropertyOrder order)
    {
        JsonWriterOptions options = new JsonWriterOptions();
        options.PropertyOrder = order;
        return options;
    }

    static string WriteTwoValues(JsonInputMode mode)
    {
        System.Buffers.ArrayBufferWriter<byte> destination =
            new System.Buffers.ArrayBufferWriter<byte>();
        JsonWriterOptions options = new JsonWriterOptions();
        options.OutputMode = mode;
        IJsonTokenWriter writer = new JsonUtf8Writer(
            options,
            BufferTarget(destination));
        Assert.That(writer.WriteNull().Success, Is.True);
        if (mode == JsonInputMode.SingleValue)
        {
            Assert.That(writer.FlushWriter().Success, Is.True);
        }

        JsonOperationResult second = writer.WriteBoolean(true);
        if (mode == JsonInputMode.SingleValue)
        {
            Assert.That(second.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidWriterState));
        }
        else
        {
            Assert.That(second.Success, Is.True);
            Assert.That(writer.CompleteWriter().Success, Is.True);
        }

        return Text(destination.WrittenSpan.ToArray());
    }

    static string Serialize(JsonDocument document, JsonWriterOptions options)
    {
        return new JsonDocumentSerializer().SerializeString(document, options);
    }

    static JsonWriterTarget BufferTarget(
        System.Buffers.ArrayBufferWriter<byte> destination)
    {
        JsonWriterTarget target = new JsonWriterTarget();
        target.BufferWriter = destination;
        return target;
    }

    static JsonDocument Document(JsonValue value)
    {
        JsonDocument document = new JsonDocument();
        document.Root = value;
        return document;
    }

    static JsonValue Null()
    {
        return new JsonValueFactory().CreateNull();
    }

    static JsonValue Boolean(bool value)
    {
        return new JsonValueFactory().CreateBoolean(value);
    }

    static JsonValue Number(string value)
    {
        return new JsonValueFactory().CreateNumber(value);
    }

    static JsonValue String(string value)
    {
        return new JsonValueFactory().CreateString(value);
    }

    static JsonValue Array(params JsonValue[] values)
    {
        return new JsonValueFactory().CreateArray(values.ToList());
    }

    static JsonValue Object(params JsonMember[] members)
    {
        return new JsonValueFactory().CreateObject(members.ToList());
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

    static string Text(byte[] value)
    {
        return System.Text.Encoding.UTF8.GetString(value);
    }

    class RecordingReadStream : Stream
    {
        readonly byte[] bytes;
        int position;

        internal int MaximumRequestedBytes;
        internal int ReadCount;

        internal RecordingReadStream(byte[] bytes)
        {
            this.bytes = bytes;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => bytes.Length;
        public override long Position
        {
            get => position;
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadCount++;
            MaximumRequestedBytes = Math.Max(MaximumRequestedBytes, count);
            int available = bytes.Length - position;
            int copied = Math.Min(available, count);
            global::System.Array.Copy(bytes, position, buffer, offset, copied);
            position += copied;
            return copied;
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
