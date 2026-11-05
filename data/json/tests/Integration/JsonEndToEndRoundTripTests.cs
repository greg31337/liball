namespace json.Tests;

public class JsonEndToEndRoundTripTests
{
    [TestCase("null", JsonValueKind.Null)]
    [TestCase("false", JsonValueKind.Boolean)]
    [TestCase("-0.50E+2", JsonValueKind.Number)]
    [TestCase("\"text \\u20ac \\ud83d\\ude00\"", JsonValueKind.String)]
    [TestCase("[null,true,-0.50E+2,\"x\",[],{}]", JsonValueKind.Array)]
    [TestCase(
        "{\"n\":null,\"b\":true,\"x\":-0.50E+2," +
        "\"s\":\"é\",\"a\":[0],\"o\":{\"k\":\"v\"}}",
        JsonValueKind.Object)]
    public void EveryValueFamilyRoundTripsAcrossEveryPipeline(
        string input,
        JsonValueKind expectedRootKind)
    {
        byte[] utf8 = Bytes(input);
        JsonReaderOptions readerOptions = new JsonReaderOptions();
        JsonWriterOptions writerOptions = new JsonWriterOptions();
        IJsonDocumentParser parser = new JsonDocumentParser();
        JsonDocument source = parser.ParseUtf8(utf8, readerOptions);
        JsonValue root = source.Root
            ?? throw new InvalidOperationException("Expected a root value.");
        RecordingVisitor visitor = new RecordingVisitor();
        IJsonTraversal traversal = new JsonDepthFirstTraversal();

        JsonVisitAction visitResult = traversal.TraverseValues(root, visitor);
        RoundTripOutputs outputs = WriteEveryPath(
            source,
            utf8,
            readerOptions,
            writerOptions);

        Assert.Multiple(() =>
        {
            Assert.That(root.Kind, Is.EqualTo(expectedRootKind));
            Assert.That(visitResult, Is.EqualTo(JsonVisitAction.Continue));
            Assert.That(visitor.Kinds.Count, Is.EqualTo(CountValues(root)));
            Assert.That(visitor.Kinds[0], Is.EqualTo(expectedRootKind));
            Assert.That(outputs.Traversal, Is.EqualTo(outputs.Serializer));
            Assert.That(outputs.Pull, Is.EqualTo(outputs.Serializer));
            Assert.That(outputs.Push, Is.EqualTo(outputs.Serializer));
        });

        AssertReparsesSemantically(source, outputs.Serializer, readerOptions);
        AssertReparsesSemantically(source, outputs.Traversal, readerOptions);
        AssertReparsesSemantically(source, outputs.Pull, readerOptions);
        AssertReparsesSemantically(source, outputs.Push, readerOptions);
    }

    [Test]
    public void ReaderAndWriterOptionFamiliesRoundTripTogether()
    {
        List<RoundTripOptionCase> cases = CreateOptionCases();
        int index = 0;
        while (index < cases.Count)
        {
            RoundTripOptionCase current = cases[index];
            byte[] input = FrameInput(
                current.Json,
                current.Reader.InputMode,
                current.IncludeBom);
            IJsonDocumentParser parser = new JsonDocumentParser();
            JsonDocument source = parser.ParseUtf8(input, current.Reader);
            RoundTripOutputs outputs = WriteEveryPath(
                source,
                input,
                current.Reader,
                current.Writer);

            AssertReparsesSemantically(
                source,
                outputs.Serializer,
                current.Reader,
                current.Name + " serializer");
            AssertReparsesSemantically(
                source,
                outputs.Traversal,
                current.Reader,
                current.Name + " traversal");
            AssertReparsesSemantically(
                source,
                outputs.Pull,
                current.Reader,
                current.Name + " pull");
            AssertReparsesSemantically(
                source,
                outputs.Push,
                current.Reader,
                current.Name + " push");
            index++;
        }
    }

    static RoundTripOutputs WriteEveryPath(
        JsonDocument document,
        byte[] input,
        JsonReaderOptions readerOptions,
        JsonWriterOptions writerOptions)
    {
        IJsonDocumentSerializer serializer = new JsonDocumentSerializer();
        RoundTripOutputs outputs = new RoundTripOutputs();
        outputs.Serializer = serializer.SerializeUtf8(document, writerOptions);
        JsonValue root = document.Root
            ?? throw new InvalidOperationException("Expected a root value.");
        outputs.Traversal = WriteTraversal(root, writerOptions);
        outputs.Pull = WritePull(input, readerOptions, writerOptions);
        outputs.Push = WritePush(input, readerOptions, writerOptions);
        return outputs;
    }

    static byte[] WriteTraversal(
        JsonValue root,
        JsonWriterOptions writerOptions)
    {
        WriterSession session = new WriterSession(writerOptions);
        WriterTokenHandler handler = new WriterTokenHandler(session.Writer);
        IJsonTraversal traversal = new JsonDepthFirstTraversal();

        JsonOperationResult publication = traversal.PublishValueTokens(
            root,
            handler);
        Assert.That(publication.Success, Is.True,
            publication.Diagnostic?.Message);
        Assert.That(handler.Failure, Is.Null,
            handler.Failure?.Diagnostic?.Message);
        AssertSuccess(session.Writer.CompleteWriter());
        return session.Destination.WrittenSpan.ToArray();
    }

    static byte[] WritePull(
        byte[] input,
        JsonReaderOptions readerOptions,
        JsonWriterOptions writerOptions)
    {
        WriterSession session = new WriterSession(writerOptions);
        WriterTokenHandler handler = new WriterTokenHandler(session.Writer);
        IJsonPullReader reader = new JsonPullReader(readerOptions);
        JsonFeedResult feed = reader.FeedUtf8(input);
        Assert.That(feed.ReadStatus, Is.Not.EqualTo(JsonReadStatus.Error),
            feed.Diagnostic?.Message);
        JsonFeedResult completion = reader.CompleteInput();
        Assert.That(completion.ReadStatus,
            Is.Not.EqualTo(JsonReadStatus.Error),
            completion.Diagnostic?.Message);

        int iterations = 0;
        while (iterations < 1000)
        {
            JsonTokenResult read = reader.ReadToken();
            Assert.That(read.Status, Is.Not.EqualTo(JsonReadStatus.Error),
                read.Diagnostic?.Message);
            if (read.Token != null)
            {
                JsonHandlerAction action = handler.HandleToken(read.Token);
                Assert.That(action, Is.EqualTo(JsonHandlerAction.Continue),
                    handler.Failure?.Diagnostic?.Message);
            }

            if (read.Status == JsonReadStatus.StreamComplete)
            {
                break;
            }

            iterations++;
        }

        Assert.That(iterations, Is.LessThan(1000));
        Assert.That(handler.Failure, Is.Null,
            handler.Failure?.Diagnostic?.Message);
        AssertSuccess(session.Writer.CompleteWriter());
        return session.Destination.WrittenSpan.ToArray();
    }

    static byte[] WritePush(
        byte[] input,
        JsonReaderOptions readerOptions,
        JsonWriterOptions writerOptions)
    {
        WriterSession session = new WriterSession(writerOptions);
        WriterTokenHandler handler = new WriterTokenHandler(session.Writer);
        IJsonPushParser parser = new JsonPushParser(readerOptions, handler);

        JsonFeedResult feed = parser.FeedPushUtf8(input);
        Assert.That(feed.ReadStatus, Is.Not.EqualTo(JsonReadStatus.Error),
            feed.Diagnostic?.Message);
        Assert.That(feed.ReadStatus, Is.Not.EqualTo(JsonReadStatus.Stopped),
            handler.Failure?.Diagnostic?.Message);
        JsonFeedResult completion = parser.CompletePushInput();
        Assert.That(completion.ReadStatus,
            Is.Not.EqualTo(JsonReadStatus.Error),
            completion.Diagnostic?.Message);
        Assert.That(completion.ReadStatus,
            Is.Not.EqualTo(JsonReadStatus.Stopped),
            handler.Failure?.Diagnostic?.Message);
        Assert.That(handler.Failure, Is.Null,
            handler.Failure?.Diagnostic?.Message);
        AssertSuccess(session.Writer.CompleteWriter());
        return session.Destination.WrittenSpan.ToArray();
    }

    static void AssertReparsesSemantically(
        JsonDocument expected,
        byte[] output,
        JsonReaderOptions readerOptions)
    {
        AssertReparsesSemantically(expected, output, readerOptions, "output");
    }

    static void AssertReparsesSemantically(
        JsonDocument expected,
        byte[] output,
        JsonReaderOptions readerOptions,
        string context)
    {
        IJsonDocumentParser parser = new JsonDocumentParser();
        JsonDocument reparsed = parser.ParseUtf8(output, readerOptions);
        JsonValue expectedRoot = expected.Root
            ?? throw new InvalidOperationException("Expected a root value.");
        JsonValue actualRoot = reparsed.Root
            ?? throw new InvalidOperationException("Expected a reparsed root.");
        IJsonValueComparer comparer = new JsonValueComparer();

        Assert.That(
            comparer.CompareValues(
                expectedRoot,
                actualRoot,
                JsonComparisonMode.JsonSemantic),
            Is.True,
            context);
    }

    static int CountValues(JsonValue root)
    {
        int count = 0;
        List<JsonValue> pending = new List<JsonValue>();
        pending.Add(root);
        while (pending.Count > 0)
        {
            JsonValue value = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            count++;
            if (value.ArrayItems != null)
            {
                pending.AddRange(value.ArrayItems);
            }

            if (value.ObjectMembers != null)
            {
                int index = 0;
                while (index < value.ObjectMembers.Count)
                {
                    JsonValue child = value.ObjectMembers[index].Value
                        ?? throw new InvalidOperationException(
                            "Expected an object value.");
                    pending.Add(child);
                    index++;
                }
            }
        }

        return count;
    }

    static List<RoundTripOptionCase> CreateOptionCases()
    {
        const string duplicateJson =
            "{\"z\":[null,true,false,-0.50E+2,\"<é/>&\"," +
            "{\"b\":2,\"a\":1}],\"dup\":1,\"dup\":2," +
            "\"é\":\"é😀\"}";
        const string uniqueJson =
            "{\"z\":[null,true,false,-0.50E+2,\"<é/>&\"," +
            "{\"b\":2,\"a\":1}],\"first\":1,\"last\":2," +
            "\"é\":\"é😀\"}";
        List<RoundTripOptionCase> cases = new List<RoundTripOptionCase>();

        JsonReaderOptions preserveReader = BaseReader();
        preserveReader.BomPolicy = JsonBomPolicy.AllowInitial;
        preserveReader.TrackLineInformation = true;
        preserveReader.TrackUnicodeScalarColumn = true;
        preserveReader.CaptureRawTokens = true;
        preserveReader.CaptureSourceRanges = true;
        JsonWriterOptions preserveWriter = BaseWriter();
        preserveWriter.Formatting = JsonFormatting.Pretty;
        preserveWriter.Indentation = " ";
        preserveWriter.NewLine = "\n";
        cases.Add(OptionCase(
            "syntax-preserve-single",
            duplicateJson,
            preserveReader,
            preserveWriter,
            true));

        JsonReaderOptions firstReader = BaseReader();
        firstReader.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.KeepFirst;
        firstReader.InputMode = JsonInputMode.WhitespaceSeparated;
        firstReader.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
        JsonWriterOptions firstWriter = BaseWriter();
        firstWriter.StringEscapeMode = JsonStringEscapeMode.AsciiOnly;
        firstWriter.PropertyOrder = JsonPropertyOrder.OrdinalUtf16;
        firstWriter.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.KeepFirst;
        firstWriter.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
        firstWriter.OutputMode = JsonInputMode.WhitespaceSeparated;
        firstWriter.EscapeSolidus = true;
        firstWriter.UseUppercaseHexadecimalEscapes = true;
        cases.Add(OptionCase(
            "syntax-first-whitespace",
            duplicateJson,
            firstReader,
            firstWriter,
            false));

        JsonReaderOptions lastReader = BaseReader();
        lastReader.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.KeepLast;
        lastReader.InputMode = JsonInputMode.JsonTextSequence;
        lastReader.SequenceErrorPolicy =
            JsonSequenceErrorPolicy.Resynchronize;
        lastReader.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
        JsonWriterOptions lastWriter = BaseWriter();
        lastWriter.Formatting = JsonFormatting.Pretty;
        lastWriter.StringEscapeMode = JsonStringEscapeMode.HtmlSafe;
        lastWriter.PropertyOrder = JsonPropertyOrder.OrdinalUnicodeScalar;
        lastWriter.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.KeepLast;
        lastWriter.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
        lastWriter.OutputMode = JsonInputMode.JsonTextSequence;
        lastWriter.OmitSpaceAfterColon = true;
        lastWriter.Indentation = "\t";
        lastWriter.NewLine = "\r\n";
        cases.Add(OptionCase(
            "syntax-last-sequence",
            duplicateJson,
            lastReader,
            lastWriter,
            false));

        cases.Add(ProfileCase(
            "portable",
            JsonConformanceProfile.Portable,
            JsonInputMode.SingleValue,
            JsonPropertyOrder.Jcs,
            JsonStringEscapeMode.Minimal,
            uniqueJson));
        cases.Add(ProfileCase(
            "ijson",
            JsonConformanceProfile.IJson,
            JsonInputMode.WhitespaceSeparated,
            JsonPropertyOrder.OrdinalUtf16,
            JsonStringEscapeMode.AsciiOnly,
            uniqueJson));
        cases.Add(ProfileCase(
            "jcs",
            JsonConformanceProfile.Jcs,
            JsonInputMode.SingleValue,
            JsonPropertyOrder.Jcs,
            JsonStringEscapeMode.HtmlSafe,
            uniqueJson));
        return cases;
    }

    static RoundTripOptionCase ProfileCase(
        string name,
        JsonConformanceProfile profile,
        JsonInputMode mode,
        JsonPropertyOrder order,
        JsonStringEscapeMode escapeMode,
        string json)
    {
        JsonReaderOptions reader = BaseReader();
        reader.ConformanceProfile = profile;
        reader.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.Reject;
        reader.InputMode = mode;
        reader.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
        reader.BomPolicy = JsonBomPolicy.Reject;
        JsonWriterOptions writer = BaseWriter();
        writer.Formatting = name == "ijson"
            ? JsonFormatting.Pretty
            : JsonFormatting.Compact;
        writer.StringEscapeMode = escapeMode;
        writer.PropertyOrder = order;
        writer.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.Reject;
        writer.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
        writer.OutputMode = mode;
        writer.SkipModelValidation = name == "portable";
        return OptionCase(name, json, reader, writer, false);
    }

    static JsonReaderOptions BaseReader()
    {
        return new JsonReaderOptions();
    }

    static JsonWriterOptions BaseWriter()
    {
        return new JsonWriterOptions();
    }

    static RoundTripOptionCase OptionCase(
        string name,
        string json,
        JsonReaderOptions reader,
        JsonWriterOptions writer,
        bool includeBom)
    {
        RoundTripOptionCase result = new RoundTripOptionCase();
        result.Name = name;
        result.Json = json;
        result.Reader = reader;
        result.Writer = writer;
        result.IncludeBom = includeBom;
        return result;
    }

    static byte[] FrameInput(
        string json,
        JsonInputMode mode,
        bool includeBom)
    {
        string framed = json;
        if (mode == JsonInputMode.WhitespaceSeparated)
        {
            framed = " \n" + json + "\t";
        }

        if (mode == JsonInputMode.JsonTextSequence)
        {
            framed = "\u001e" + json + "\n";
        }

        byte[] content = Bytes(framed);
        if (!includeBom)
        {
            return content;
        }

        byte[] withBom = new byte[content.Length + 3];
        withBom[0] = 0xEF;
        withBom[1] = 0xBB;
        withBom[2] = 0xBF;
        global::System.Buffer.BlockCopy(
            content,
            0,
            withBom,
            3,
            content.Length);
        return withBom;
    }

    static void AssertSuccess(JsonOperationResult result)
    {
        Assert.That(result.Success, Is.True, result.Diagnostic?.Message);
    }

    static byte[] Bytes(string value)
    {
        return System.Text.Encoding.UTF8.GetBytes(value);
    }

    class WriterSession
    {
        internal System.Buffers.ArrayBufferWriter<byte> Destination;
        internal IJsonTokenWriter Writer;

        internal WriterSession(JsonWriterOptions options)
        {
            Destination = new System.Buffers.ArrayBufferWriter<byte>();
            JsonWriterTarget target = new JsonWriterTarget();
            target.BufferWriter = Destination;
            Writer = new JsonUtf8Writer(options, target);
        }
    }

    class WriterTokenHandler : IJsonTokenHandler
    {
        IJsonTokenWriter writer;
        internal JsonOperationResult? Failure;

        internal WriterTokenHandler(IJsonTokenWriter writer)
        {
            this.writer = writer;
        }

        public JsonHandlerAction HandleToken(JsonToken token)
        {
            if (token.Kind == JsonTokenKind.DocumentBoundary)
            {
                return JsonHandlerAction.Continue;
            }

            JsonOperationResult result = WriteToken(token);
            if (!result.Success)
            {
                Failure = result;
                return JsonHandlerAction.Stop;
            }

            return JsonHandlerAction.Continue;
        }

        JsonOperationResult WriteToken(JsonToken token)
        {
            if (token.Kind == JsonTokenKind.StartObject)
            {
                return writer.WriteStartObject();
            }

            if (token.Kind == JsonTokenKind.EndObject)
            {
                return writer.WriteEndObject();
            }

            if (token.Kind == JsonTokenKind.StartArray)
            {
                return writer.WriteStartArray();
            }

            if (token.Kind == JsonTokenKind.EndArray)
            {
                return writer.WriteEndArray();
            }

            if (token.Kind == JsonTokenKind.PropertyName)
            {
                return writer.WritePropertyName(token.StringValue
                    ?? throw new InvalidOperationException(
                        "Expected a property name."));
            }

            if (token.Kind == JsonTokenKind.Null)
            {
                return writer.WriteNull();
            }

            if (token.Kind == JsonTokenKind.Boolean)
            {
                return writer.WriteBoolean(token.BooleanValue);
            }

            if (token.Kind == JsonTokenKind.Number)
            {
                return writer.WriteNumberLexeme(token.Number?.Lexeme
                    ?? throw new InvalidOperationException(
                        "Expected a number lexeme."));
            }

            if (token.Kind == JsonTokenKind.String)
            {
                return writer.WriteString(token.StringValue
                    ?? throw new InvalidOperationException(
                        "Expected a string value."));
            }

            throw new InvalidOperationException("Unexpected token kind.");
        }
    }

    class RecordingVisitor : IJsonValueVisitor
    {
        internal List<JsonValueKind> Kinds = new List<JsonValueKind>();

        public JsonVisitAction VisitValue(JsonVisit visit)
        {
            JsonValue value = visit.Value
                ?? throw new InvalidOperationException(
                    "Expected a visited value.");
            Kinds.Add(value.Kind);
            return JsonVisitAction.Continue;
        }
    }

    class RoundTripOutputs
    {
        internal byte[] Serializer = Array.Empty<byte>();
        internal byte[] Traversal = Array.Empty<byte>();
        internal byte[] Pull = Array.Empty<byte>();
        internal byte[] Push = Array.Empty<byte>();
    }

    class RoundTripOptionCase
    {
        internal string Name = "";
        internal string Json = "";
        internal JsonReaderOptions Reader = new JsonReaderOptions();
        internal JsonWriterOptions Writer = new JsonWriterOptions();
        internal bool IncludeBom;
    }
}
