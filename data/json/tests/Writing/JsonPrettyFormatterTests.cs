namespace json.Tests;

public class JsonPrettyFormatterTests
{
    [Test]
    public void DefaultPrettyFormattingHandlesNestedAndEmptyContainers()
    {
        System.Buffers.ArrayBufferWriter<byte> destination =
            new System.Buffers.ArrayBufferWriter<byte>();
        JsonWriterOptions options = new JsonWriterOptions();
        options.Formatting = JsonFormatting.Pretty;
        JsonWriterCore core = CreateCore(destination, options);

        Assert.That(core.WriteObjectStart().Success, Is.True);
        Assert.That(core.WriteEncodedPropertyName(Bytes("\"a\"")).Success, Is.True);
        Assert.That(core.WriteArrayStart().Success, Is.True);
        Assert.That(core.WriteEncodedScalarValue(Bytes("1")).Success, Is.True);
        Assert.That(core.WriteObjectStart().Success, Is.True);
        Assert.That(core.WriteEncodedPropertyName(Bytes("\"b\"")).Success, Is.True);
        Assert.That(core.WriteArrayStart().Success, Is.True);
        Assert.That(core.WriteArrayEnd().Success, Is.True);
        Assert.That(core.WriteObjectEnd().Success, Is.True);
        Assert.That(core.WriteArrayEnd().Success, Is.True);
        Assert.That(core.WriteEncodedPropertyName(Bytes("\"empty\"")).Success, Is.True);
        Assert.That(core.WriteObjectStart().Success, Is.True);
        Assert.That(core.WriteObjectEnd().Success, Is.True);
        Assert.That(core.WriteObjectEnd().Success, Is.True);
        Assert.That(core.CompleteOutput().Success, Is.True);

        Assert.That(
            Text(destination),
            Is.EqualTo(
                "{\n" +
                "  \"a\": [\n" +
                "    1,\n" +
                "    {\n" +
                "      \"b\": []\n" +
                "    }\n" +
                "  ],\n" +
                "  \"empty\": {}\n" +
                "}"));
    }

    [Test]
    public void CustomIndentationCrLfAndColonSpacingApplyAtEveryDepth()
    {
        System.Buffers.ArrayBufferWriter<byte> destination =
            new System.Buffers.ArrayBufferWriter<byte>();
        JsonWriterOptions options = new JsonWriterOptions();
        options.Formatting = JsonFormatting.Pretty;
        options.Indentation = "\t";
        options.NewLine = "\r\n";
        options.OmitSpaceAfterColon = true;
        JsonWriterCore core = CreateCore(destination, options);

        Assert.That(core.WriteArrayStart().Success, Is.True);
        Assert.That(core.WriteObjectStart().Success, Is.True);
        Assert.That(core.WriteEncodedPropertyName(Bytes("\"x\"")).Success, Is.True);
        Assert.That(core.WriteArrayStart().Success, Is.True);
        Assert.That(core.WriteEncodedScalarValue(Bytes("true")).Success, Is.True);
        Assert.That(core.WriteArrayEnd().Success, Is.True);
        Assert.That(core.WriteObjectEnd().Success, Is.True);
        Assert.That(core.WriteArrayStart().Success, Is.True);
        Assert.That(core.WriteArrayEnd().Success, Is.True);
        Assert.That(core.WriteArrayEnd().Success, Is.True);
        Assert.That(core.CompleteOutput().Success, Is.True);

        Assert.That(
            Text(destination),
            Is.EqualTo(
                "[\r\n" +
                "\t{\r\n" +
                "\t\t\"x\":[\r\n" +
                "\t\t\ttrue\r\n" +
                "\t\t]\r\n" +
                "\t},\r\n" +
                "\t[]\r\n" +
                "]"));
    }

    [Test]
    public void CompactModeIgnoresPrettyLayoutSettings()
    {
        System.Buffers.ArrayBufferWriter<byte> destination =
            new System.Buffers.ArrayBufferWriter<byte>();
        JsonWriterOptions options = new JsonWriterOptions();
        options.Formatting = JsonFormatting.Compact;
        options.Indentation = "\t\t";
        options.NewLine = "\r\n";
        JsonWriterCore core = CreateCore(destination, options);

        Assert.That(core.WriteObjectStart().Success, Is.True);
        Assert.That(core.WriteEncodedPropertyName(Bytes("\"a\"")).Success, Is.True);
        Assert.That(core.WriteArrayStart().Success, Is.True);
        Assert.That(core.WriteEncodedScalarValue(Bytes("0")).Success, Is.True);
        Assert.That(core.WriteArrayEnd().Success, Is.True);
        Assert.That(core.WriteObjectEnd().Success, Is.True);
        Assert.That(core.CompleteOutput().Success, Is.True);

        Assert.That(Text(destination), Is.EqualTo("{\"a\":[0]}"));
    }

    [Test]
    public void EmptyIndentationKeepsPrettyLineStructureWithoutPadding()
    {
        System.Buffers.ArrayBufferWriter<byte> destination =
            new System.Buffers.ArrayBufferWriter<byte>();
        JsonWriterOptions options = new JsonWriterOptions();
        options.Formatting = JsonFormatting.Pretty;
        options.Indentation = "";
        JsonWriterCore core = CreateCore(destination, options);

        Assert.That(core.WriteArrayStart().Success, Is.True);
        Assert.That(core.WriteEncodedScalarValue(Bytes("1")).Success, Is.True);
        Assert.That(core.WriteArrayEnd().Success, Is.True);
        Assert.That(core.CompleteOutput().Success, Is.True);

        Assert.That(Text(destination), Is.EqualTo("[\n1\n]"));
    }

    [Test]
    public void InvalidIndentationAndLineEndingsAreRejectedAsOptions()
    {
        foreach (string indentation in new string[]
        {
            "x",
            "\n",
            "\r",
            "\u00a0"
        })
        {
            JsonWriterOptions options = new JsonWriterOptions();
            options.Indentation = indentation;
            JsonWriteException failure = Assert.Throws<JsonWriteException>(() =>
                JsonOptionNormalizer.NormalizeWriterOptions(options))
                ?? throw new InvalidOperationException(
                    "Expected JsonWriteException.");
            Assert.That(failure.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidOptions),
                Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(indentation)));
        }

        foreach (string newLine in new string[]
        {
            "",
            "\r",
            "\n\r",
            "\t",
            " \n"
        })
        {
            JsonWriterOptions options = new JsonWriterOptions();
            options.NewLine = newLine;
            JsonWriteException failure = Assert.Throws<JsonWriteException>(() =>
                JsonOptionNormalizer.NormalizeWriterOptions(options))
                ?? throw new InvalidOperationException(
                    "Expected JsonWriteException.");
            Assert.That(failure.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidOptions),
                Convert.ToHexString(System.Text.Encoding.UTF8.GetBytes(newLine)));
        }
    }

    [Test]
    public void FormattingLimitFailureLeavesContainerEmptyAndRecoverable()
    {
        System.Buffers.ArrayBufferWriter<byte> destination =
            new System.Buffers.ArrayBufferWriter<byte>();
        JsonLimits limits = new JsonLimits();
        limits.MaximumOutputBytes = 3;
        JsonWriterOptions options = new JsonWriterOptions();
        options.Formatting = JsonFormatting.Pretty;
        options.Limits = limits;
        JsonWriterCore core = CreateCore(destination, options);

        Assert.That(core.WriteArrayStart().Success, Is.True);
        JsonOperationResult limited =
            core.WriteEncodedScalarValue(Bytes("0"));
        Assert.That(core.WriteArrayEnd().Success, Is.True);
        Assert.That(core.CompleteOutput().Success, Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(limited.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.OutputByteLimitExceeded));
            Assert.That(Text(destination), Is.EqualTo("[]"));
        });
    }

    static JsonWriterCore CreateCore(
        System.Buffers.ArrayBufferWriter<byte> destination,
        JsonWriterOptions options)
    {
        JsonWriterTarget target = new JsonWriterTarget();
        target.BufferWriter = destination;
        return new JsonWriterCore(options, target);
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
