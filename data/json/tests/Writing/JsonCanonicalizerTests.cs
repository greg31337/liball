namespace json.Tests;

public class JsonCanonicalizerTests
{
    [Test]
    public void Rfc8785PropertyOrderExampleUsesUtf16CodeUnits()
    {
        byte[] input = Bytes(
            "{" +
            "\"\\u20ac\":\"Euro Sign\"," +
            "\"\\r\":\"Carriage Return\"," +
            "\"\\ufb33\":\"Hebrew Letter Dalet With Dagesh\"," +
            "\"1\":\"One\"," +
            "\"\\ud83d\\ude00\":\"Emoji: Grinning Face\"," +
            "\"\\u0080\":\"Control\"," +
            "\"\\u00f6\":\"Latin Small Letter O With Diaeresis\"" +
            "}");
        string expected =
            "{\"\\r\":\"Carriage Return\",\"1\":\"One\"," +
            "\"\u0080\":\"Control\",\"ö\":" +
            "\"Latin Small Letter O With Diaeresis\"," +
            "\"€\":\"Euro Sign\",\"😀\":\"Emoji: Grinning Face\"," +
            "\"דּ\":\"Hebrew Letter Dalet With Dagesh\"}";
        IJsonCanonicalizer canonicalizer = new JsonCanonicalizer();

        string actual = Text(canonicalizer.CanonicalizeUtf8(
            input,
            new JsonReaderOptions()));

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void Utf8CanonicalizationMatchesTheRfc8785Example()
    {
        byte[] input = Bytes(
            "{\n" +
            "  \"numbers\": [333333333.33333329, 1E30, 4.50," +
            " 2e-3, 0.000000000000000000000000001],\n" +
            "  \"string\": \"\\u20ac$\\u000F\\u000aA'" +
            "\\u0042\\u0022\\u005c\\\\\\\"\\/\",\n" +
            "  \"literals\": [null, true, false]\n" +
            "}");
        IJsonCanonicalizer canonicalizer = new JsonCanonicalizer();

        byte[] output = canonicalizer.CanonicalizeUtf8(
            input,
            new JsonReaderOptions());

        Assert.That(
            Text(output),
            Is.EqualTo(
                "{\"literals\":[null,true,false]," +
                "\"numbers\":[333333333.3333333,1e+30,4.5,0.002,1e-27]," +
                "\"string\":\"€$\\u000f\\nA'B\\\"\\\\\\\\\\\"/\"}"));
    }

    [Test]
    public void DocumentCanonicalizationSortsObjectsRecursivelyButNotArrays()
    {
        JsonValueFactory factory = new JsonValueFactory();
        JsonValue objectInArray = factory.CreateObject(new List<JsonMember>
        {
            Member("z", factory.CreateNumber("2.0")),
            Member("a", factory.CreateNumber("1.00"))
        });
        JsonValue array = factory.CreateArray(new List<JsonValue>
        {
            factory.CreateNumber("3"),
            objectInArray,
            factory.CreateArray(new List<JsonValue>
            {
                factory.CreateNumber("2"),
                factory.CreateNumber("1")
            })
        });
        JsonDocument document = Document(factory.CreateObject(
            new List<JsonMember>
            {
                Member("z", factory.CreateString("last")),
                Member("a", array)
            }));
        IJsonCanonicalizer canonicalizer = new JsonCanonicalizer();

        string output = Text(canonicalizer.CanonicalizeDocument(document));

        Assert.Multiple(() =>
        {
            Assert.That(
                output,
                Is.EqualTo(
                    "{\"a\":[3,{\"a\":1,\"z\":2},[2,1]]," +
                    "\"z\":\"last\"}"));
            Assert.That(
                document.Root?.ObjectMembers?.Select(member => member.Name),
                Is.EqualTo(new[] { "z", "a" }));
            Assert.That(array.ArrayItems,
                Is.EqualTo(new[]
                {
                    array.ArrayItems?[0],
                    objectInArray,
                    array.ArrayItems?[2]
                }));
        });
    }

    [Test]
    public void CanonicalTargetFlushesWithoutTakingStreamOwnership()
    {
        JsonValueFactory factory = new JsonValueFactory();
        JsonDocument document = Document(factory.CreateObject(
            new List<JsonMember>
            {
                Member("b", factory.CreateBoolean(true)),
                Member("a", factory.CreateNull())
            }));
        TrackingStream stream = new TrackingStream();
        JsonWriterTarget target = new JsonWriterTarget();
        target.Stream = stream;
        IJsonCanonicalizer canonicalizer = new JsonCanonicalizer();

        JsonOperationResult result = canonicalizer.WriteCanonicalTarget(
            document,
            target);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.BytesWritten, Is.EqualTo(stream.Length));
            Assert.That(Text(stream.ToArray()),
                Is.EqualTo("{\"a\":null,\"b\":true}"));
            Assert.That(stream.FlushCount, Is.EqualTo(1));
            Assert.That(stream.WasDisposed, Is.False);
            Assert.That(stream.CanWrite, Is.True);
        });
    }

    [Test]
    public void DuplicateUnicodeAndNumberViolationsFailBeforeTargetOutput()
    {
        JsonValueFactory factory = new JsonValueFactory();
        JsonDocument duplicate = Document(factory.CreateObject(
            new List<JsonMember>
            {
                Member("a", factory.CreateNull()),
                Member("a", factory.CreateBoolean(true))
            }));
        JsonDocument loneSurrogate = Document(factory.CreateString(
            new string(new char[] { '\uD800' })));
        JsonDocument noncharacter = Document(factory.CreateString("\uFDD0"));
        JsonDocument hugeNumber = Document(factory.CreateNumber("1e999"));
        JsonDocument[] documents = new JsonDocument[]
        {
            duplicate,
            loneSurrogate,
            noncharacter,
            hugeNumber
        };
        JsonDiagnosticCode[] codes = new JsonDiagnosticCode[]
        {
            JsonDiagnosticCode.DuplicatePropertyName,
            JsonDiagnosticCode.InvalidModel,
            JsonDiagnosticCode.InvalidModel,
            JsonDiagnosticCode.NumericConversionFailed
        };
        IJsonCanonicalizer canonicalizer = new JsonCanonicalizer();

        int index = 0;
        while (index < documents.Length)
        {
            System.Buffers.ArrayBufferWriter<byte> destination =
                new System.Buffers.ArrayBufferWriter<byte>();
            JsonWriterTarget target = new JsonWriterTarget();
            target.BufferWriter = destination;
            JsonOperationResult result = canonicalizer.WriteCanonicalTarget(
                documents[index],
                target);

            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False, codes[index].ToString());
                Assert.That(result.Diagnostic?.Code,
                    Is.EqualTo(codes[index]));
                Assert.That(result.BytesWritten, Is.Zero);
                Assert.That(destination.WrittenCount, Is.Zero);
            });
            index++;
        }
    }

    [Test]
    public void Utf8InputEnforcesJcsDuplicatesUnicodeAndFiniteNumbers()
    {
        IJsonCanonicalizer canonicalizer = new JsonCanonicalizer();
        JsonParseException duplicate = Assert.Throws<JsonParseException>(() =>
            canonicalizer.CanonicalizeUtf8(
                Bytes("{\"a\":0,\"a\":1}"),
                new JsonReaderOptions()))
            ?? throw new InvalidOperationException(
                "Expected JsonParseException.");
        JsonParseException surrogate = Assert.Throws<JsonParseException>(() =>
            canonicalizer.CanonicalizeUtf8(
                Bytes("\"\\ud800\""),
                new JsonReaderOptions()))
            ?? throw new InvalidOperationException(
                "Expected JsonParseException.");
        JsonWriteException number = Assert.Throws<JsonWriteException>(() =>
            canonicalizer.CanonicalizeUtf8(
                Bytes("1e999"),
                new JsonReaderOptions()))
            ?? throw new InvalidOperationException(
                "Expected JsonWriteException.");

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.DuplicatePropertyName));
            Assert.That(surrogate.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidSurrogatePair));
            Assert.That(number.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.NumericConversionFailed));
        });
    }

    [Test]
    public void VerificationDistinguishesCanonicalNoncanonicalAndInvalidInput()
    {
        IJsonCanonicalizer canonicalizer = new JsonCanonicalizer();
        byte[] canonical = Bytes("{\"a\":1,\"b\":[true,null]}");
        byte[] whitespace = Bytes("{ \"a\" : 1, \"b\" : [true, null] }");
        byte[] number = Bytes("1.0");
        byte[] duplicate = Bytes("{\"a\":0,\"a\":1}");

        JsonOperationResult canonicalResult =
            canonicalizer.VerifyCanonicalUtf8(
                canonical,
                new JsonReaderOptions());
        JsonOperationResult whitespaceResult =
            canonicalizer.VerifyCanonicalUtf8(
                whitespace,
                new JsonReaderOptions());
        JsonOperationResult numberResult =
            canonicalizer.VerifyCanonicalUtf8(
                number,
                new JsonReaderOptions());
        JsonOperationResult duplicateResult =
            canonicalizer.VerifyCanonicalUtf8(
                duplicate,
                new JsonReaderOptions());

        Assert.Multiple(() =>
        {
            Assert.That(canonicalResult.Success, Is.True);
            Assert.That(canonicalResult.BytesConsumed,
                Is.EqualTo(canonical.Length));
            Assert.That(whitespaceResult.Success, Is.False);
            Assert.That(whitespaceResult.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidRawValue));
            Assert.That(numberResult.Success, Is.False);
            Assert.That(numberResult.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidRawValue));
            Assert.That(duplicateResult.Success, Is.False);
            Assert.That(duplicateResult.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.DuplicatePropertyName));
        });
    }

    [Test]
    public void Utf8CanonicalizationAppliesInputAndOutputLimits()
    {
        IJsonCanonicalizer canonicalizer = new JsonCanonicalizer();
        JsonLimits inputLimits = new JsonLimits();
        inputLimits.MaximumDocumentBytes = 2;
        JsonReaderOptions inputOptions = new JsonReaderOptions();
        inputOptions.Limits = inputLimits;
        JsonLimits outputLimits = new JsonLimits();
        outputLimits.MaximumOutputBytes = 2;
        JsonReaderOptions outputOptions = new JsonReaderOptions();
        outputOptions.Limits = outputLimits;

        JsonLimitException inputFailure = Assert.Throws<JsonLimitException>(() =>
            canonicalizer.CanonicalizeUtf8(Bytes("null"), inputOptions))
            ?? throw new InvalidOperationException(
                "Expected JsonLimitException.");
        JsonLimitException outputFailure = Assert.Throws<JsonLimitException>(() =>
            canonicalizer.CanonicalizeUtf8(Bytes("[0]"), outputOptions))
            ?? throw new InvalidOperationException(
                "Expected JsonLimitException.");

        Assert.Multiple(() =>
        {
            Assert.That(inputFailure.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.DocumentByteLimitExceeded));
            Assert.That(outputFailure.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.OutputByteLimitExceeded));
        });
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

    static byte[] Bytes(string value)
    {
        return System.Text.Encoding.UTF8.GetBytes(value);
    }

    static string Text(byte[] value)
    {
        return System.Text.Encoding.UTF8.GetString(value);
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
