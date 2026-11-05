namespace json.Tests;

public class JsonUtf8WriterTests
{
    [Test]
    public void InterfaceWritesEveryStructuralAndScalarEntryPoint()
    {
        System.Buffers.ArrayBufferWriter<byte> destination =
            new System.Buffers.ArrayBufferWriter<byte>();
        JsonWriterOptions options = new JsonWriterOptions();
        options.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
        IJsonTokenWriter writer = CreateWriter(
            destination,
            options);

        AssertSuccess(writer.WriteStartArray());
        AssertSuccess(writer.WriteNull());
        AssertSuccess(writer.WriteBoolean(true));
        AssertSuccess(writer.WriteBoolean(false));
        AssertSuccess(writer.WriteString("a\n"));
        AssertSuccess(writer.WriteNumberLexeme("-0.00E+2"));
        AssertSuccess(writer.WriteInt64(long.MinValue));
        AssertSuccess(writer.WriteUInt64(ulong.MaxValue));
        AssertSuccess(writer.WriteDecimal(1.2300m));
        AssertSuccess(writer.WriteDouble(-0.0));
        AssertSuccess(writer.WriteStartObject());
        AssertSuccess(writer.WritePropertyName("p"));
        AssertSuccess(writer.WriteBoolean(true));
        AssertSuccess(writer.WriteEndObject());
        AssertSuccess(writer.WriteEndArray());

        Assert.That(writer.GetBytesPending(), Is.GreaterThan(0));
        JsonOperationResult completion = writer.CompleteWriter();

        Assert.Multiple(() =>
        {
            Assert.That(completion.Success, Is.True);
            Assert.That(writer.GetBytesPending(), Is.Zero);
            Assert.That(writer.GetBytesCommitted(),
                Is.EqualTo(destination.WrittenCount));
            Assert.That(
                Text(destination),
                Is.EqualTo(
                    "[null,true,false,\"a\\n\",-0.00E+2," +
                    "-9223372036854775808,18446744073709551615," +
                    "1.2300,-0,{\"p\":true}]"));
        });
    }

    [Test]
    public void DomWritingValidatesFiltersSortsAndTraversesIteratively()
    {
        JsonValueFactory factory = new JsonValueFactory();
        JsonMember b = CreateMember(
            "b",
            factory.CreateArray(new List<JsonValue>
            {
                factory.CreateString("x")
            }));
        JsonMember firstA = CreateMember("a", factory.CreateNumber("1"));
        JsonMember lastA = CreateMember("a", factory.CreateNumber("2"));
        JsonValue root = factory.CreateObject(new List<JsonMember>
        {
            b,
            firstA,
            lastA
        });
        JsonWriterOptions options = new JsonWriterOptions();
        options.PropertyOrder = JsonPropertyOrder.OrdinalUtf16;
        options.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.KeepLast;
        System.Buffers.ArrayBufferWriter<byte> destination =
            new System.Buffers.ArrayBufferWriter<byte>();
        IJsonTokenWriter writer = CreateWriter(destination, options);

        JsonOperationResult result = writer.WriteJsonValue(root);
        JsonOperationResult completion = writer.CompleteWriter();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.BytesWritten,
                Is.EqualTo(System.Text.Encoding.UTF8.GetByteCount(
                    "{\"a\":2,\"b\":[\"x\"]}")));
            Assert.That(completion.Success, Is.True);
            Assert.That(Text(destination),
                Is.EqualTo("{\"a\":2,\"b\":[\"x\"]}"));
            Assert.That(root.ObjectMembers,
                Is.EqualTo(new[] { b, firstA, lastA }));
        });
    }

    [Test]
    public void RejectedDomDuplicatesAndInvalidModelsEmitNothing()
    {
        JsonValueFactory factory = new JsonValueFactory();
        JsonValue duplicate = factory.CreateObject(new List<JsonMember>
        {
            CreateMember("a", factory.CreateNull()),
            CreateMember("a", factory.CreateBoolean(true))
        });
        JsonWriterOptions options = new JsonWriterOptions();
        options.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.Reject;
        System.Buffers.ArrayBufferWriter<byte> destination =
            new System.Buffers.ArrayBufferWriter<byte>();
        IJsonTokenWriter writer = CreateWriter(destination, options);

        JsonOperationResult duplicateResult = writer.WriteJsonValue(duplicate);
        JsonOperationResult missingRoot = writer.WriteJsonValue(null!);
        JsonValue invalid = new JsonValue();
        JsonOperationResult invalidResult = writer.WriteJsonValue(invalid);

        Assert.Multiple(() =>
        {
            Assert.That(duplicateResult.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.DuplicatePropertyName));
            Assert.That(missingRoot.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
            Assert.That(invalidResult.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
            Assert.That(writer.GetBytesPending(), Is.Zero);
            Assert.That(destination.WrittenCount, Is.Zero);
        });
    }

    [Test]
    public void RawValuesAreValidatedPreservedAndDoNotCorruptStateOnFailure()
    {
        System.Buffers.ArrayBufferWriter<byte> destination =
            new System.Buffers.ArrayBufferWriter<byte>();
        IJsonTokenWriter writer = CreateWriter(
            destination,
            new JsonWriterOptions());
        byte[] invalid = Bytes("[1,]");
        byte[] multiple = Bytes("0 1");
        byte[] valid = Bytes(" {\"x\":[1,true]} ");

        JsonOperationResult invalidResult = writer.WriteRawValue(invalid);
        JsonOperationResult multipleResult = writer.WriteRawValue(multiple);
        JsonOperationResult validResult = writer.WriteRawValue(valid);
        JsonOperationResult completion = writer.CompleteWriter();

        Assert.Multiple(() =>
        {
            Assert.That(invalidResult.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidRawValue));
            Assert.That(multipleResult.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidRawValue));
            Assert.That(validResult.Success, Is.True);
            Assert.That(validResult.BytesConsumed, Is.EqualTo(valid.Length));
            Assert.That(completion.Success, Is.True);
            Assert.That(destination.WrittenSpan.ToArray(), Is.EqualTo(valid));
        });
    }

    [Test]
    public void EncodingFailuresReturnDiagnosticsWithoutAdvancingTheWriter()
    {
        System.Buffers.ArrayBufferWriter<byte> destination =
            new System.Buffers.ArrayBufferWriter<byte>();
        JsonWriterOptions options = new JsonWriterOptions();
        options.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
        IJsonTokenWriter writer = CreateWriter(
            destination,
            options);
        string lone = new string(new char[] { '\uD800' });

        AssertSuccess(writer.WriteStartArray());
        JsonOperationResult number = writer.WriteNumberLexeme("01");
        JsonOperationResult finite = writer.WriteDouble(double.NaN);
        JsonOperationResult text = writer.WriteString(lone);
        AssertSuccess(writer.WriteNull());
        AssertSuccess(writer.WriteEndArray());
        AssertSuccess(writer.CompleteWriter());

        Assert.Multiple(() =>
        {
            Assert.That(number.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidNumber));
            Assert.That(finite.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.NonFiniteNumber));
            Assert.That(text.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidSurrogatePair));
            Assert.That(Text(destination), Is.EqualTo("[null]"));
        });
    }

    [Test]
    public void FlushCountersAndResetSupportIndependentReuse()
    {
        System.Buffers.ArrayBufferWriter<byte> destination =
            new System.Buffers.ArrayBufferWriter<byte>();
        IJsonTokenWriter writer = CreateWriter(
            destination,
            new JsonWriterOptions());

        AssertSuccess(writer.WriteString("x"));
        Assert.That(writer.GetBytesPending(), Is.EqualTo(3));
        JsonOperationResult flush = writer.FlushWriter();

        Assert.Multiple(() =>
        {
            Assert.That(flush.Success, Is.True);
            Assert.That(flush.BytesWritten, Is.EqualTo(3));
            Assert.That(writer.GetBytesPending(), Is.Zero);
            Assert.That(writer.GetBytesCommitted(), Is.EqualTo(3));
        });

        writer.ResetWriter();
        AssertSuccess(writer.WriteBoolean(true));
        AssertSuccess(writer.CompleteWriter());

        Assert.Multiple(() =>
        {
            Assert.That(writer.GetBytesCommitted(), Is.EqualTo(4));
            Assert.That(Text(destination), Is.EqualTo("\"x\"true"));
        });
    }

    [Test]
    public void MultipleValueModesEmitTheirRequiredFraming()
    {
        JsonWriterOptions whitespaceOptions = new JsonWriterOptions();
        whitespaceOptions.OutputMode = JsonInputMode.WhitespaceSeparated;
        System.Buffers.ArrayBufferWriter<byte> whitespaceDestination =
            new System.Buffers.ArrayBufferWriter<byte>();
        IJsonTokenWriter whitespace = CreateWriter(
            whitespaceDestination,
            whitespaceOptions);
        AssertSuccess(whitespace.WriteNull());
        AssertSuccess(whitespace.WriteBoolean(true));
        AssertSuccess(whitespace.CompleteWriter());

        JsonWriterOptions sequenceOptions = new JsonWriterOptions();
        sequenceOptions.OutputMode = JsonInputMode.JsonTextSequence;
        System.Buffers.ArrayBufferWriter<byte> sequenceDestination =
            new System.Buffers.ArrayBufferWriter<byte>();
        IJsonTokenWriter sequence = CreateWriter(
            sequenceDestination,
            sequenceOptions);
        AssertSuccess(sequence.WriteNull());
        AssertSuccess(sequence.WriteStartArray());
        AssertSuccess(sequence.WriteEndArray());
        AssertSuccess(sequence.CompleteWriter());

        Assert.Multiple(() =>
        {
            Assert.That(Text(whitespaceDestination), Is.EqualTo("null\ntrue"));
            Assert.That(
                sequenceDestination.WrittenSpan.ToArray(),
                Is.EqualTo(new byte[]
                {
                    JsonSyntaxBytes.RecordSeparator,
                    0x6E,
                    0x75,
                    0x6C,
                    0x6C,
                    JsonSyntaxBytes.LineFeed,
                    JsonSyntaxBytes.RecordSeparator,
                    JsonSyntaxBytes.BeginArray,
                    JsonSyntaxBytes.EndArray,
                    JsonSyntaxBytes.LineFeed
                }));
        });
    }

    static IJsonTokenWriter CreateWriter(
        System.Buffers.ArrayBufferWriter<byte> destination,
        JsonWriterOptions options)
    {
        JsonWriterTarget target = new JsonWriterTarget();
        target.BufferWriter = destination;
        return new JsonUtf8Writer(options, target);
    }

    static JsonMember CreateMember(string name, JsonValue value)
    {
        JsonMember member = new JsonMember();
        member.Name = name;
        member.Value = value;
        return member;
    }

    static void AssertSuccess(JsonOperationResult result)
    {
        Assert.That(result.Success, Is.True, result.Diagnostic?.Message);
    }

    static byte[] Bytes(string value)
    {
        return System.Text.Encoding.UTF8.GetBytes(value);
    }

    static string Text(System.Buffers.ArrayBufferWriter<byte> destination)
    {
        return System.Text.Encoding.UTF8.GetString(destination.WrittenSpan);
    }
}
