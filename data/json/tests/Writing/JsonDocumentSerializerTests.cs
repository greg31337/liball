namespace json.Tests;

public class JsonDocumentSerializerTests
{
    [Test]
    public void OwnedUtf8AndStringMethodsUseTheSameWriterOutput()
    {
        JsonDocument document = CreateDocument();
        JsonWriterOptions options = new JsonWriterOptions();
        IJsonDocumentSerializer serializer = new JsonDocumentSerializer();

        byte[] first = serializer.SerializeUtf8(document, options);
        byte[] second = serializer.SerializeUtf8(document, options);
        string text = serializer.SerializeString(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(
                System.Text.Encoding.UTF8.GetString(first),
                Is.EqualTo("{\"z\":\"é\",\"a\":[null,-0.00,true]}"));
            Assert.That(second, Is.EqualTo(first));
            Assert.That(text,
                Is.EqualTo("{\"z\":\"é\",\"a\":[null,-0.00,true]}"));
        });

        first[0] = JsonSyntaxBytes.BeginArray;
        Assert.That(
            System.Text.Encoding.UTF8.GetString(second),
            Is.EqualTo("{\"z\":\"é\",\"a\":[null,-0.00,true]}"));
    }

    [Test]
    public void OwnedMethodsApplyAllWriterOptionsThroughTheWriterCore()
    {
        JsonDocument document = CreateDocument();
        JsonWriterOptions options = new JsonWriterOptions();
        options.Formatting = JsonFormatting.Pretty;
        options.PropertyOrder = JsonPropertyOrder.OrdinalUtf16;
        options.StringEscapeMode = JsonStringEscapeMode.AsciiOnly;
        IJsonDocumentSerializer serializer = new JsonDocumentSerializer();

        string text = serializer.SerializeString(document, options);

        Assert.That(
            text,
            Is.EqualTo(
                "{\n" +
                "  \"a\": [\n" +
                "    null,\n" +
                "    -0.00,\n" +
                "    true\n" +
                "  ],\n" +
                "  \"z\": \"\\u00e9\"\n" +
                "}"));
    }

    [Test]
    public void TargetMethodWritesAndFlushesStreamAndBufferWriter()
    {
        JsonDocument document = CreateDocument();
        JsonWriterOptions options = new JsonWriterOptions();
        IJsonDocumentSerializer serializer = new JsonDocumentSerializer();
        TrackingStream stream = new TrackingStream();
        JsonWriterTarget streamTarget = new JsonWriterTarget();
        streamTarget.Stream = stream;
        System.Buffers.ArrayBufferWriter<byte> buffer =
            new System.Buffers.ArrayBufferWriter<byte>();
        JsonWriterTarget bufferTarget = new JsonWriterTarget();
        bufferTarget.BufferWriter = buffer;

        JsonOperationResult streamResult = serializer.SerializeTarget(
            document,
            options,
            streamTarget);
        JsonOperationResult bufferResult = serializer.SerializeTarget(
            document,
            options,
            bufferTarget);
        byte[] streamBytes = stream.ToArray();
        byte[] bufferBytes = buffer.WrittenSpan.ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(streamResult.Success, Is.True);
            Assert.That(bufferResult.Success, Is.True);
            Assert.That(streamResult.BytesWritten,
                Is.EqualTo(streamBytes.Length));
            Assert.That(bufferResult.BytesWritten,
                Is.EqualTo(bufferBytes.Length));
            Assert.That(streamBytes, Is.EqualTo(bufferBytes));
            Assert.That(stream.FlushCount, Is.EqualTo(1));
            Assert.That(stream.WasDisposed, Is.False);
        });
    }

    [Test]
    public void InvalidDocumentReturnsResultOrThrowsWriteException()
    {
        JsonDocument invalid = new JsonDocument();
        JsonWriterOptions options = new JsonWriterOptions();
        IJsonDocumentSerializer serializer = new JsonDocumentSerializer();
        System.Buffers.ArrayBufferWriter<byte> destination =
            new System.Buffers.ArrayBufferWriter<byte>();
        JsonWriterTarget target = new JsonWriterTarget();
        target.BufferWriter = destination;

        JsonOperationResult result = serializer.SerializeTarget(
            invalid,
            options,
            target);
        JsonWriteException failure = Assert.Throws<JsonWriteException>(() =>
            serializer.SerializeUtf8(invalid, options))
            ?? throw new InvalidOperationException(
                "Expected JsonWriteException.");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
            Assert.That(result.BytesWritten, Is.Zero);
            Assert.That(failure.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
            Assert.That(destination.WrittenCount, Is.Zero);
        });
    }

    [Test]
    public void OutputLimitReturnsResultOrThrowsLimitException()
    {
        JsonValueFactory factory = new JsonValueFactory();
        JsonDocument document = new JsonDocument();
        document.Root = factory.CreateBoolean(true);
        JsonLimits limits = new JsonLimits();
        limits.MaximumOutputBytes = 3;
        JsonWriterOptions options = new JsonWriterOptions();
        options.Limits = limits;
        IJsonDocumentSerializer serializer = new JsonDocumentSerializer();
        System.Buffers.ArrayBufferWriter<byte> destination =
            new System.Buffers.ArrayBufferWriter<byte>();
        JsonWriterTarget target = new JsonWriterTarget();
        target.BufferWriter = destination;

        JsonOperationResult result = serializer.SerializeTarget(
            document,
            options,
            target);
        JsonLimitException failure = Assert.Throws<JsonLimitException>(() =>
            serializer.SerializeString(document, options))
            ?? throw new InvalidOperationException(
                "Expected JsonLimitException.");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.OutputByteLimitExceeded));
            Assert.That(result.BytesWritten, Is.Zero);
            Assert.That(failure.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.OutputByteLimitExceeded));
            Assert.That(destination.WrittenCount, Is.Zero);
        });
    }

    static JsonDocument CreateDocument()
    {
        JsonValueFactory factory = new JsonValueFactory();
        JsonValue array = factory.CreateArray(new List<JsonValue>
        {
            factory.CreateNull(),
            factory.CreateNumber("-0.00"),
            factory.CreateBoolean(true)
        });
        JsonMember z = new JsonMember();
        z.Name = "z";
        z.Value = factory.CreateString("é");
        JsonMember a = new JsonMember();
        a.Name = "a";
        a.Value = array;
        JsonDocument document = new JsonDocument();
        document.Root = factory.CreateObject(new List<JsonMember> { z, a });
        return document;
    }

    class TrackingStream : MemoryStream
    {
        internal int FlushCount;
        internal bool WasDisposed;

        public override void Flush()
        {
            FlushCount++;
            base.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }
}
