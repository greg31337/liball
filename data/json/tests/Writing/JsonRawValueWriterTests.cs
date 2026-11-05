namespace json.Tests;

public class JsonRawValueWriterTests
{
    [Test]
    public void EveryRawJsonKindIsPreservedByteForByte()
    {
        WriterFixture fixture = new WriterFixture(new JsonWriterOptions());
        byte[][] values = new byte[][]
        {
            Bytes(" null "),
            Bytes("true"),
            Bytes("\"\\u0061\""),
            Bytes("-0.00E+02"),
            Bytes("[1, {\"b\":2,\"a\":3}]"),
            Bytes("{\"x\":1,\"x\":2}")
        };

        AssertSuccess(fixture.Writer.WriteStartArray());
        foreach (byte[] value in values)
        {
            JsonOperationResult result = fixture.Writer.WriteRawValue(value);
            AssertSuccess(result);
            Assert.That(result.BytesConsumed, Is.EqualTo(value.Length));
        }

        AssertSuccess(fixture.Writer.WriteEndArray());
        AssertSuccess(fixture.Writer.CompleteWriter());

        Assert.That(
            fixture.Text(),
            Is.EqualTo(
                "[ null ,true,\"\\u0061\",-0.00E+02," +
                "[1, {\"b\":2,\"a\":3}],{\"x\":1,\"x\":2}]"));
    }

    [Test]
    public void LeadingTrailingWhitespaceAndEncodedSpellingRemainExact()
    {
        JsonWriterOptions options = new JsonWriterOptions();
        options.Formatting = JsonFormatting.Pretty;
        options.PropertyOrder = JsonPropertyOrder.OrdinalUtf16;
        options.StringEscapeMode = JsonStringEscapeMode.AsciiOnly;
        WriterFixture fixture = new WriterFixture(options);
        byte[] raw = Bytes(
            " \t\r\n{\"z\":\"é\",\"a\":-0.00e+2}\n ");

        JsonOperationResult result = fixture.Writer.WriteRawValue(raw);
        AssertSuccess(result);
        AssertSuccess(fixture.Writer.CompleteWriter());

        Assert.Multiple(() =>
        {
            Assert.That(result.BytesConsumed, Is.EqualTo(raw.Length));
            Assert.That(result.BytesWritten, Is.EqualTo(raw.Length));
            Assert.That(fixture.Bytes(), Is.EqualTo(raw));
        });
    }

    [Test]
    public void RawInputRequiresExactlyOneValue()
    {
        byte[][] invalidValues = new byte[][]
        {
            Array.Empty<byte>(),
            Bytes(" \t\r\n"),
            Bytes("0 1"),
            Bytes("0\n[]"),
            Bytes("{}[]"),
            Bytes("true false"),
            Bytes("null x"),
            new byte[] { 0xEF, 0xBB, 0xBF, 0x6E, 0x75, 0x6C, 0x6C }
        };
        AssertInvalidInputsAndRecover(invalidValues);
    }

    [Test]
    public void PartialAndTrailingRawInputsAreRejected()
    {
        byte[][] invalidValues = new byte[][]
        {
            Bytes("n"),
            Bytes("tru"),
            Bytes("\""),
            Bytes("\"\\"),
            Bytes("\"\\u12"),
            Bytes("-"),
            Bytes("1."),
            Bytes("1e"),
            Bytes("["),
            Bytes("[1"),
            Bytes("[1,"),
            Bytes("{"),
            Bytes("{\"a\""),
            Bytes("{\"a\":"),
            Bytes("{\"a\":1"),
            Bytes("nullx"),
            Bytes("[]x"),
            Bytes("\"a\"x"),
            Bytes("0,")
        };
        AssertInvalidInputsAndRecover(invalidValues);
    }

    [Test]
    public void MalformedUtf8AndScalarViolationsAreRejected()
    {
        JsonWriterOptions scalarOptions = new JsonWriterOptions();
        scalarOptions.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
        WriterFixture scalar = new WriterFixture(scalarOptions);
        byte[][] invalidValues = new byte[][]
        {
            new byte[] { 0x22, 0xC0, 0xAF, 0x22 },
            new byte[] { 0x22, 0xE2, 0x82, 0x22 },
            Bytes("\"\\ud800\""),
            Bytes("\"\\udc00\"")
        };

        foreach (byte[] invalid in invalidValues)
        {
            JsonOperationResult result = scalar.Writer.WriteRawValue(invalid);
            AssertInvalidRaw(result, invalid);
            Assert.That(scalar.Writer.GetBytesPending(), Is.Zero);
        }

        AssertSuccess(scalar.Writer.WriteRawValue(Bytes("\"😀\"")));
        AssertSuccess(scalar.Writer.CompleteWriter());

        JsonWriterOptions compatibleOptions = new JsonWriterOptions();
        compatibleOptions.UnicodePolicy = JsonUnicodePolicy.SyntaxCompatible;
        WriterFixture compatible = new WriterFixture(compatibleOptions);
        byte[] loneSurrogate = Bytes("\"\\ud800\"");
        AssertSuccess(compatible.Writer.WriteRawValue(loneSurrogate));
        AssertSuccess(compatible.Writer.CompleteWriter());

        Assert.Multiple(() =>
        {
            Assert.That(scalar.Text(), Is.EqualTo("\"😀\""));
            Assert.That(compatible.Bytes(), Is.EqualTo(loneSurrogate));
        });
    }

    [Test]
    public void InvalidRawValuesDoNotAdvanceObjectOrArrayState()
    {
        WriterFixture objectFixture = new WriterFixture(
            new JsonWriterOptions());
        AssertSuccess(objectFixture.Writer.WriteStartObject());
        AssertSuccess(objectFixture.Writer.WritePropertyName("a"));
        long objectPending = objectFixture.Writer.GetBytesPending();
        JsonOperationResult partial =
            objectFixture.Writer.WriteRawValue(Bytes("[1"));
        AssertInvalidRaw(partial, Bytes("[1"));
        Assert.That(objectFixture.Writer.GetBytesPending(),
            Is.EqualTo(objectPending));
        AssertSuccess(objectFixture.Writer.WriteRawValue(Bytes(" [1] ")));

        JsonOperationResult unnamed =
            objectFixture.Writer.WriteRawValue(Bytes("0"));
        Assert.Multiple(() =>
        {
            Assert.That(unnamed.Success, Is.False);
            Assert.That(unnamed.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidWriterState));
        });

        AssertSuccess(objectFixture.Writer.WritePropertyName("b"));
        JsonOperationResult multiple =
            objectFixture.Writer.WriteRawValue(Bytes("1 2"));
        AssertInvalidRaw(multiple, Bytes("1 2"));
        AssertSuccess(objectFixture.Writer.WriteRawValue(Bytes("{\"x\":2}")));
        AssertSuccess(objectFixture.Writer.WriteEndObject());
        AssertSuccess(objectFixture.Writer.CompleteWriter());

        WriterFixture arrayFixture = new WriterFixture(new JsonWriterOptions());
        AssertSuccess(arrayFixture.Writer.WriteStartArray());
        JsonOperationResult invalid =
            arrayFixture.Writer.WriteRawValue(Bytes("false true"));
        AssertInvalidRaw(invalid, Bytes("false true"));
        AssertSuccess(arrayFixture.Writer.WriteRawValue(Bytes("0")));
        AssertSuccess(arrayFixture.Writer.WriteRawValue(Bytes("1")));
        AssertSuccess(arrayFixture.Writer.WriteEndArray());
        AssertSuccess(arrayFixture.Writer.CompleteWriter());

        Assert.Multiple(() =>
        {
            Assert.That(
                objectFixture.Text(),
                Is.EqualTo("{\"a\": [1] ,\"b\":{\"x\":2}}"));
            Assert.That(arrayFixture.Text(), Is.EqualTo("[0,1]"));
        });
    }

    [Test]
    public void ReaderAndWriterResourceLimitsApplyToRawValues()
    {
        AssertLimit(
            Bytes("null"),
            limits => limits.MaximumDocumentBytes = 3,
            JsonDiagnosticCode.DocumentByteLimitExceeded);
        AssertLimit(
            Bytes("null"),
            limits => limits.MaximumInputBufferBytes = 3,
            JsonDiagnosticCode.InputBufferLimitExceeded);
        AssertLimit(
            Bytes("\"ab\""),
            limits => limits.MaximumTokenBytes = 3,
            JsonDiagnosticCode.TokenByteLimitExceeded);
        AssertLimit(
            Bytes("\"é\""),
            limits => limits.MaximumDecodedStringBytes = 1,
            JsonDiagnosticCode.DecodedStringByteLimitExceeded);
        AssertLimit(
            Bytes("\"😀\""),
            limits => limits.MaximumDecodedStringCodeUnits = 1,
            JsonDiagnosticCode.DecodedStringCodeUnitLimitExceeded);
        AssertLimit(
            Bytes("100"),
            limits => limits.MaximumNumberCharacters = 2,
            JsonDiagnosticCode.NumberCharacterLimitExceeded);
        AssertLimit(
            Bytes("[[]]"),
            limits => limits.MaximumNestingDepth = 1,
            JsonDiagnosticCode.NestingDepthLimitExceeded);
        AssertLimit(
            Bytes("[0,1]"),
            limits => limits.MaximumArrayItems = 1,
            JsonDiagnosticCode.ArrayItemLimitExceeded);
        AssertLimit(
            Bytes("{\"a\":0,\"b\":1}"),
            limits => limits.MaximumObjectMembers = 1,
            JsonDiagnosticCode.ObjectMemberLimitExceeded);
        AssertLimit(
            Bytes("null"),
            limits => limits.MaximumOutputBytes = 3,
            JsonDiagnosticCode.OutputByteLimitExceeded);
    }

    static void AssertInvalidInputsAndRecover(byte[][] invalidValues)
    {
        WriterFixture fixture = new WriterFixture(new JsonWriterOptions());
        AssertSuccess(fixture.Writer.WriteStartArray());
        foreach (byte[] invalid in invalidValues)
        {
            JsonOperationResult result = fixture.Writer.WriteRawValue(invalid);
            AssertInvalidRaw(result, invalid);
            Assert.That(fixture.Writer.GetBytesPending(), Is.EqualTo(1));
        }

        AssertSuccess(fixture.Writer.WriteRawValue(Bytes("{\"ok\":true}")));
        AssertSuccess(fixture.Writer.WriteEndArray());
        AssertSuccess(fixture.Writer.CompleteWriter());
        Assert.That(fixture.Text(), Is.EqualTo("[{\"ok\":true}]"));
    }

    static void AssertLimit(
        byte[] raw,
        System.Action<JsonLimits> configure,
        JsonDiagnosticCode expectedCode)
    {
        JsonLimits limits = new JsonLimits();
        configure(limits);
        JsonWriterOptions options = new JsonWriterOptions();
        options.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
        options.Limits = limits;
        WriterFixture fixture = new WriterFixture(options);

        JsonOperationResult result = fixture.Writer.WriteRawValue(raw);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False, expectedCode.ToString());
            Assert.That(result.Diagnostic?.Code,
                Is.EqualTo(expectedCode));
            Assert.That(result.BytesWritten, Is.Zero);
            Assert.That(fixture.Writer.GetBytesPending(), Is.Zero);
            Assert.That(fixture.Writer.GetBytesCommitted(), Is.Zero);
            Assert.That(fixture.Destination.WrittenCount, Is.Zero);
        });
    }

    static void AssertInvalidRaw(JsonOperationResult result, byte[] input)
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                result.Success,
                Is.False,
                Convert.ToHexString(input));
            Assert.That(
                result.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidRawValue),
                Convert.ToHexString(input));
            Assert.That(result.BytesWritten, Is.Zero);
        });
    }

    static byte[] Bytes(string value)
    {
        return System.Text.Encoding.UTF8.GetBytes(value);
    }

    static void AssertSuccess(JsonOperationResult result)
    {
        Assert.That(result.Success, Is.True, result.Diagnostic?.Message);
    }

    sealed class WriterFixture
    {
        internal System.Buffers.ArrayBufferWriter<byte> Destination;
        internal IJsonTokenWriter Writer;

        internal WriterFixture(JsonWriterOptions options)
        {
            Destination = new System.Buffers.ArrayBufferWriter<byte>();
            JsonWriterTarget target = new JsonWriterTarget();
            target.BufferWriter = Destination;
            Writer = new JsonUtf8Writer(options, target);
        }

        internal byte[] Bytes()
        {
            return Destination.WrittenSpan.ToArray();
        }

        internal string Text()
        {
            return System.Text.Encoding.UTF8.GetString(
                Destination.WrittenSpan);
        }
    }
}
