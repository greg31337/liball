namespace json.Tests;

public class JsonDocumentSerializerBehaviorTests
{
    [Test]
    public void EveryDomKindCanBeSerializedAsTheRoot()
    {
        JsonValueFactory factory = new JsonValueFactory();
        JsonValue objectValue = factory.CreateObject(new List<JsonMember>
        {
            Member("a", factory.CreateNull())
        });
        JsonDocument[] documents = new JsonDocument[]
        {
            Document(factory.CreateNull()),
            Document(factory.CreateBoolean(true)),
            Document(factory.CreateNumber("-0.50E+2")),
            Document(factory.CreateString("a\n€")),
            Document(factory.CreateArray(new List<JsonValue>
            {
                factory.CreateNull(),
                factory.CreateNumber("1")
            })),
            Document(objectValue)
        };
        string[] expected = new string[]
        {
            "null",
            "true",
            "-0.50E+2",
            "\"a\\n€\"",
            "[null,1]",
            "{\"a\":null}"
        };
        IJsonDocumentSerializer serializer = new JsonDocumentSerializer();

        int index = 0;
        while (index < documents.Length)
        {
            Assert.That(
                serializer.SerializeString(
                    documents[index],
                    new JsonWriterOptions()),
                Is.EqualTo(expected[index]),
                expected[index]);
            index++;
        }
    }

    [Test]
    public void EveryDestinationProducesIdenticalUtf8AndRemainsCallerOwned()
    {
        JsonDocument document = Document(
            new JsonValueFactory().CreateString("😀"));
        JsonWriterOptions options = new JsonWriterOptions();
        IJsonDocumentSerializer serializer = new JsonDocumentSerializer();
        byte[] owned = serializer.SerializeUtf8(document, options);
        string text = serializer.SerializeString(document, options);

        TrackingStream stream = new TrackingStream();
        byte[] prefix = Bytes("prefix:");
        stream.Write(prefix, 0, prefix.Length);
        JsonWriterTarget streamTarget = new JsonWriterTarget();
        streamTarget.Stream = stream;
        JsonOperationResult streamResult = serializer.SerializeTarget(
            document,
            options,
            streamTarget);

        System.Buffers.ArrayBufferWriter<byte> buffer =
            new System.Buffers.ArrayBufferWriter<byte>();
        WriteToBuffer(buffer, prefix);
        JsonWriterTarget bufferTarget = new JsonWriterTarget();
        bufferTarget.BufferWriter = buffer;
        JsonOperationResult bufferResult = serializer.SerializeTarget(
            document,
            options,
            bufferTarget);
        byte[] expectedWithPrefix = Bytes("prefix:\"😀\"");

        Assert.Multiple(() =>
        {
            Assert.That(System.Text.Encoding.UTF8.GetString(owned),
                Is.EqualTo(text));
            Assert.That(stream.ToArray(), Is.EqualTo(expectedWithPrefix));
            Assert.That(buffer.WrittenSpan.ToArray(),
                Is.EqualTo(expectedWithPrefix));
            Assert.That(streamResult.BytesWritten, Is.EqualTo(owned.Length));
            Assert.That(bufferResult.BytesWritten, Is.EqualTo(owned.Length));
            Assert.That(stream.FlushCount, Is.EqualTo(1));
            Assert.That(stream.WasDisposed, Is.False);
            Assert.That(stream.CanWrite, Is.True);
        });

        stream.WriteByte(0x21);
        WriteToBuffer(buffer, new byte[] { 0x21 });
        Assert.Multiple(() =>
        {
            Assert.That(
                stream.ToArray()[(int)stream.Length - 1],
                Is.EqualTo(0x21));
            Assert.That(buffer.WrittenSpan[buffer.WrittenCount - 1],
                Is.EqualTo(0x21));
        });
    }

    [Test]
    public void WriterOptionsAreAppliedTogetherByEveryOwnedMethod()
    {
        JsonValueFactory factory = new JsonValueFactory();
        JsonDocument document = Document(factory.CreateObject(
            new List<JsonMember>
            {
                Member("z", factory.CreateString("<é/>")),
                Member("a", factory.CreateArray(new List<JsonValue>
                {
                    factory.CreateBoolean(true),
                    factory.CreateArray(new List<JsonValue>())
                }))
            }));
        JsonWriterOptions options = new JsonWriterOptions();
        options.Formatting = JsonFormatting.Pretty;
        options.Indentation = "\t";
        options.NewLine = "\r\n";
        options.OmitSpaceAfterColon = true;
        options.PropertyOrder = JsonPropertyOrder.OrdinalUtf16;
        options.StringEscapeMode = JsonStringEscapeMode.AsciiOnly;
        options.EscapeSolidus = true;
        options.UseUppercaseHexadecimalEscapes = true;
        IJsonDocumentSerializer serializer = new JsonDocumentSerializer();
        string expected =
            "{\r\n" +
            "\t\"a\":[\r\n" +
            "\t\ttrue,\r\n" +
            "\t\t[]\r\n" +
            "\t],\r\n" +
            "\t\"z\":\"<\\u00E9\\/>\"\r\n" +
            "}";

        byte[] utf8 = serializer.SerializeUtf8(document, options);
        string text = serializer.SerializeString(document, options);

        Assert.Multiple(() =>
        {
            Assert.That(System.Text.Encoding.UTF8.GetString(utf8),
                Is.EqualTo(expected));
            Assert.That(text, Is.EqualTo(expected));
        });
    }

    [Test]
    public void JsonTextSequenceOutputModeFramesTheSerializedDocument()
    {
        JsonWriterOptions options = new JsonWriterOptions();
        options.OutputMode = JsonInputMode.JsonTextSequence;
        IJsonDocumentSerializer serializer = new JsonDocumentSerializer();

        byte[] result = serializer.SerializeUtf8(
            Document(new JsonValueFactory().CreateNumber("1")),
            options);

        Assert.That(result, Is.EqualTo(new byte[]
        {
            JsonSyntaxBytes.RecordSeparator,
            0x31,
            JsonSyntaxBytes.LineFeed
        }));
    }

    [Test]
    public void EveryDuplicatePolicyIsAppliedBeforeStablePropertyOrdering()
    {
        JsonValueFactory factory = new JsonValueFactory();
        JsonDocument document = Document(factory.CreateObject(
            new List<JsonMember>
            {
                Member("b", factory.CreateNumber("0")),
                Member("a", factory.CreateNumber("1")),
                Member("b", factory.CreateNumber("2")),
                Member("a", factory.CreateNumber("3"))
            }));
        IJsonDocumentSerializer serializer = new JsonDocumentSerializer();

        Assert.That(
            SerializeDuplicates(
                serializer,
                document,
                JsonDuplicateKeyPolicy.Preserve),
            Is.EqualTo("{\"a\":1,\"a\":3,\"b\":0,\"b\":2}"));
        Assert.That(
            SerializeDuplicates(
                serializer,
                document,
                JsonDuplicateKeyPolicy.KeepFirst),
            Is.EqualTo("{\"a\":1,\"b\":0}"));
        Assert.That(
            SerializeDuplicates(
                serializer,
                document,
                JsonDuplicateKeyPolicy.KeepLast),
            Is.EqualTo("{\"a\":3,\"b\":2}"));

        JsonWriterOptions rejectOptions = DuplicateOptions(
            JsonDuplicateKeyPolicy.Reject);
        System.Buffers.ArrayBufferWriter<byte> destination =
            new System.Buffers.ArrayBufferWriter<byte>();
        JsonOperationResult rejected = serializer.SerializeTarget(
            document,
            rejectOptions,
            Target(destination));
        JsonWriteException failure = Assert.Throws<JsonWriteException>(() =>
            serializer.SerializeUtf8(document, rejectOptions))
            ?? throw new InvalidOperationException(
                "Expected JsonWriteException.");

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.DuplicatePropertyName));
            Assert.That(failure.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.DuplicatePropertyName));
            Assert.That(destination.WrittenCount, Is.Zero);
        });
    }

    [Test]
    public void InvalidMutableValueShapesAndCyclesNeverReachTheTarget()
    {
        JsonValueFactory factory = new JsonValueFactory();
        JsonValue invalidNumber = factory.CreateNumber("1");
        if (invalidNumber.Number != null)
        {
            invalidNumber.Number.Lexeme = "01";
        }

        JsonValue mismatched = factory.CreateNull();
        mismatched.StringValue = "unexpected";
        JsonValue loneSurrogate = factory.CreateString(
            new string(new char[] { '\uD800' }));
        JsonValue nullChild = factory.CreateArray(new List<JsonValue>
        {
            null!
        });
        JsonValue nullMember = factory.CreateObject(new List<JsonMember>
        {
            null!
        });
        JsonMember missingName = new JsonMember();
        missingName.Value = factory.CreateNull();
        JsonValue unnamed = factory.CreateObject(new List<JsonMember>
        {
            missingName
        });
        JsonMember missingValue = new JsonMember();
        missingValue.Name = "a";
        JsonValue valueless = factory.CreateObject(new List<JsonMember>
        {
            missingValue
        });
        JsonValue cycle = factory.CreateArray(new List<JsonValue>());
        cycle.ArrayItems?.Add(cycle);
        JsonValue[] invalidValues = new JsonValue[]
        {
            new JsonValue(),
            new JsonValue { Kind = JsonValueKind.Number },
            new JsonValue { Kind = JsonValueKind.String },
            new JsonValue { Kind = JsonValueKind.Array },
            new JsonValue { Kind = JsonValueKind.Object },
            invalidNumber,
            mismatched,
            loneSurrogate,
            nullChild,
            nullMember,
            unnamed,
            valueless,
            cycle
        };
        IJsonDocumentSerializer serializer = new JsonDocumentSerializer();

        foreach (JsonValue invalid in invalidValues)
        {
            System.Buffers.ArrayBufferWriter<byte> destination =
                new System.Buffers.ArrayBufferWriter<byte>();
            JsonOperationResult result = serializer.SerializeTarget(
                Document(invalid),
                new JsonWriterOptions(),
                Target(destination));
            JsonDiagnosticCode expectedCode = invalid == cycle
                ? JsonDiagnosticCode.ModelCycle
                : JsonDiagnosticCode.InvalidModel;
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False, invalid.Kind.ToString());
                Assert.That(result.Diagnostic?.Code,
                    Is.EqualTo(expectedCode),
                    invalid.Kind.ToString());
                Assert.That(result.BytesWritten, Is.Zero);
                Assert.That(destination.WrittenCount, Is.Zero);
            });
        }
    }

    [Test]
    public void InvalidDocumentMetadataIsValidatedUnlessExplicitlySkipped()
    {
        JsonDocument document = Document(
            new JsonValueFactory().CreateNull());
        document.SourceByteCount = -1;
        IJsonDocumentSerializer serializer = new JsonDocumentSerializer();
        System.Buffers.ArrayBufferWriter<byte> rejectedDestination =
            new System.Buffers.ArrayBufferWriter<byte>();

        JsonOperationResult rejected = serializer.SerializeTarget(
            document,
            new JsonWriterOptions(),
            Target(rejectedDestination));

        JsonWriterOptions skippedOptions = new JsonWriterOptions();
        skippedOptions.SkipModelValidation = true;
        string skipped = serializer.SerializeString(document, skippedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
            Assert.That(rejectedDestination.WrittenCount, Is.Zero);
            Assert.That(skipped, Is.EqualTo("null"));
        });
    }

    [Test]
    public void OutputLimitSucceedsAtTheBoundaryAndFailsAtomicallyBelowIt()
    {
        JsonDocument document = Document(
            new JsonValueFactory().CreateArray(new List<JsonValue>
            {
                new JsonValueFactory().CreateNumber("0")
            }));
        IJsonDocumentSerializer serializer = new JsonDocumentSerializer();
        JsonWriterOptions exactOptions = OutputLimitOptions(3);
        System.Buffers.ArrayBufferWriter<byte> exactDestination =
            new System.Buffers.ArrayBufferWriter<byte>();
        JsonOperationResult exact = serializer.SerializeTarget(
            document,
            exactOptions,
            Target(exactDestination));

        JsonWriterOptions failingOptions = OutputLimitOptions(2);
        System.Buffers.ArrayBufferWriter<byte> failingDestination =
            new System.Buffers.ArrayBufferWriter<byte>();
        JsonOperationResult failing = serializer.SerializeTarget(
            document,
            failingOptions,
            Target(failingDestination));
        JsonLimitException failure = Assert.Throws<JsonLimitException>(() =>
            serializer.SerializeString(document, failingOptions))
            ?? throw new InvalidOperationException(
                "Expected JsonLimitException.");

        Assert.Multiple(() =>
        {
            Assert.That(exact.Success, Is.True);
            Assert.That(exact.BytesWritten, Is.EqualTo(3));
            Assert.That(exactDestination.WrittenSpan.ToArray(),
                Is.EqualTo(Bytes("[0]")));
            Assert.That(failing.Success, Is.False);
            Assert.That(failing.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.OutputByteLimitExceeded));
            Assert.That(failing.BytesWritten, Is.Zero);
            Assert.That(failingDestination.WrittenCount, Is.Zero);
            Assert.That(failure.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.OutputByteLimitExceeded));
        });
    }

    [Test]
    public void FlushFailureReturnsIoDiagnosticWithoutClosingCallerStream()
    {
        FlushFailingStream stream = new FlushFailingStream();
        JsonWriterTarget target = new JsonWriterTarget();
        target.Stream = stream;
        IJsonDocumentSerializer serializer = new JsonDocumentSerializer();

        JsonOperationResult result = serializer.SerializeTarget(
            Document(new JsonValueFactory().CreateBoolean(true)),
            new JsonWriterOptions(),
            target);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.IoFailure));
            Assert.That(result.BytesWritten, Is.EqualTo(4));
            Assert.That(stream.ToArray(), Is.EqualTo(Bytes("true")));
            Assert.That(stream.WasDisposed, Is.False);
        });
    }

    static string SerializeDuplicates(
        IJsonDocumentSerializer serializer,
        JsonDocument document,
        JsonDuplicateKeyPolicy policy)
    {
        return serializer.SerializeString(
            document,
            DuplicateOptions(policy));
    }

    static JsonWriterOptions DuplicateOptions(
        JsonDuplicateKeyPolicy policy)
    {
        JsonWriterOptions options = new JsonWriterOptions();
        options.DuplicateKeyPolicy = policy;
        options.PropertyOrder = JsonPropertyOrder.OrdinalUtf16;
        return options;
    }

    static JsonWriterOptions OutputLimitOptions(int maximumOutputBytes)
    {
        JsonLimits limits = new JsonLimits();
        limits.MaximumOutputBytes = maximumOutputBytes;
        JsonWriterOptions options = new JsonWriterOptions();
        options.Limits = limits;
        return options;
    }

    static JsonDocument Document(JsonValue root)
    {
        JsonDocument document = new JsonDocument();
        document.Root = root;
        return document;
    }

    static JsonMember Member(string name, JsonValue value)
    {
        JsonMember member = new JsonMember();
        member.Name = name;
        member.Value = value;
        return member;
    }

    static JsonWriterTarget Target(
        System.Buffers.ArrayBufferWriter<byte> destination)
    {
        JsonWriterTarget target = new JsonWriterTarget();
        target.BufferWriter = destination;
        return target;
    }

    static void WriteToBuffer(
        System.Buffers.ArrayBufferWriter<byte> destination,
        byte[] bytes)
    {
        System.Span<byte> span = destination.GetSpan(bytes.Length);
        bytes.CopyTo(span);
        destination.Advance(bytes.Length);
    }

    static byte[] Bytes(string value)
    {
        return System.Text.Encoding.UTF8.GetBytes(value);
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

    class FlushFailingStream : MemoryStream
    {
        internal bool WasDisposed;

        public override void Flush()
        {
            throw new IOException("Injected flush failure.");
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }
    }
}
