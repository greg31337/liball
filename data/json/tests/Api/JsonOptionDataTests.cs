namespace json.Tests;

public class JsonOptionDataTests
{
    [Test]
    public void LimitsDefaultToZeroAndRetainConfiguredValues()
    {
        JsonLimits limits = new JsonLimits();

        Assert.That(
            new[]
            {
                limits.MaximumDocumentBytes,
                limits.MaximumInputBufferBytes,
                limits.MaximumTokenBytes,
                limits.MaximumDecodedStringBytes,
                limits.MaximumDecodedStringCodeUnits,
                limits.MaximumNumberCharacters,
                limits.MaximumNestingDepth,
                limits.MaximumArrayItems,
                limits.MaximumObjectMembers,
                limits.MaximumDomNodes,
                limits.MaximumOutputBytes,
                limits.MaximumDiagnosticExcerptBytes
            },
            Is.All.Zero);

        limits.MaximumDocumentBytes = 101;
        limits.MaximumInputBufferBytes = 102;
        limits.MaximumTokenBytes = 103;
        limits.MaximumDecodedStringBytes = 104;
        limits.MaximumDecodedStringCodeUnits = 105;
        limits.MaximumNumberCharacters = 106;
        limits.MaximumNestingDepth = 107;
        limits.MaximumArrayItems = 108;
        limits.MaximumObjectMembers = 109;
        limits.MaximumDomNodes = 110;
        limits.MaximumOutputBytes = 111;
        limits.MaximumDiagnosticExcerptBytes = 112;

        Assert.That(
            new[]
            {
                limits.MaximumDocumentBytes,
                limits.MaximumInputBufferBytes,
                limits.MaximumTokenBytes,
                limits.MaximumDecodedStringBytes,
                limits.MaximumDecodedStringCodeUnits,
                limits.MaximumNumberCharacters,
                limits.MaximumNestingDepth,
                limits.MaximumArrayItems,
                limits.MaximumObjectMembers,
                limits.MaximumDomNodes,
                limits.MaximumOutputBytes,
                limits.MaximumDiagnosticExcerptBytes
            },
            Is.EqualTo(new[] { 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112 }));
    }

    [Test]
    public void ReaderOptionsUseZeroValueDefaultsAndRetainSettings()
    {
        JsonReaderOptions options = new JsonReaderOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.ConformanceProfile, Is.EqualTo(JsonConformanceProfile.SyntaxCompatible));
            Assert.That(options.DuplicateKeyPolicy, Is.EqualTo(JsonDuplicateKeyPolicy.Preserve));
            Assert.That(options.InputMode, Is.EqualTo(JsonInputMode.SingleValue));
            Assert.That(options.SequenceErrorPolicy, Is.EqualTo(JsonSequenceErrorPolicy.Stop));
            Assert.That(options.UnicodePolicy, Is.EqualTo(JsonUnicodePolicy.SyntaxCompatible));
            Assert.That(options.BomPolicy, Is.EqualTo(JsonBomPolicy.Reject));
            Assert.That(options.TrackLineInformation, Is.False);
            Assert.That(options.TrackUnicodeScalarColumn, Is.False);
            Assert.That(options.CaptureRawTokens, Is.False);
            Assert.That(options.CaptureSourceRanges, Is.False);
            Assert.That(options.StreamBufferBytes, Is.Zero);
            Assert.That(options.Limits, Is.Null);
        });

        JsonLimits limits = new JsonLimits();
        options.TrackLineInformation = true;
        options.TrackUnicodeScalarColumn = true;
        options.CaptureRawTokens = true;
        options.CaptureSourceRanges = true;
        options.StreamBufferBytes = 4096;
        options.Limits = limits;

        Assert.Multiple(() =>
        {
            Assert.That(options.TrackLineInformation, Is.True);
            Assert.That(options.TrackUnicodeScalarColumn, Is.True);
            Assert.That(options.CaptureRawTokens, Is.True);
            Assert.That(options.CaptureSourceRanges, Is.True);
            Assert.That(options.StreamBufferBytes, Is.EqualTo(4096));
            Assert.That(options.Limits, Is.SameAs(limits));
        });
    }

    [Test]
    public void WriterOptionsUseSafeZeroValueDefaultsAndRetainSettings()
    {
        JsonWriterOptions options = new JsonWriterOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.Formatting, Is.EqualTo(JsonFormatting.Compact));
            Assert.That(options.StringEscapeMode, Is.EqualTo(JsonStringEscapeMode.Minimal));
            Assert.That(options.PropertyOrder, Is.EqualTo(JsonPropertyOrder.Preserve));
            Assert.That(options.DuplicateKeyPolicy, Is.EqualTo(JsonDuplicateKeyPolicy.Preserve));
            Assert.That(options.UnicodePolicy, Is.EqualTo(JsonUnicodePolicy.SyntaxCompatible));
            Assert.That(options.OutputMode, Is.EqualTo(JsonInputMode.SingleValue));
            Assert.That(options.EscapeSolidus, Is.False);
            Assert.That(options.UseUppercaseHexadecimalEscapes, Is.False);
            Assert.That(options.SkipModelValidation, Is.False);
            Assert.That(options.OmitSpaceAfterColon, Is.False);
            Assert.That(options.Indentation, Is.Null);
            Assert.That(options.NewLine, Is.Null);
            Assert.That(options.Limits, Is.Null);
        });

        JsonLimits limits = new JsonLimits();
        options.EscapeSolidus = true;
        options.UseUppercaseHexadecimalEscapes = true;
        options.SkipModelValidation = true;
        options.OmitSpaceAfterColon = true;
        options.Indentation = "\t";
        options.NewLine = "\r\n";
        options.Limits = limits;

        Assert.Multiple(() =>
        {
            Assert.That(options.EscapeSolidus, Is.True);
            Assert.That(options.UseUppercaseHexadecimalEscapes, Is.True);
            Assert.That(options.SkipModelValidation, Is.True);
            Assert.That(options.OmitSpaceAfterColon, Is.True);
            Assert.That(options.Indentation, Is.EqualTo("\t"));
            Assert.That(options.NewLine, Is.EqualTo("\r\n"));
            Assert.That(options.Limits, Is.SameAs(limits));
        });
    }

    [Test]
    public void WriterTargetStoresEitherCallerOwnedDestination()
    {
        using MemoryStream stream = new MemoryStream();
        JsonWriterTarget target = new JsonWriterTarget();
        target.Stream = stream;

        Assert.Multiple(() =>
        {
            Assert.That(target.Stream, Is.SameAs(stream));
            Assert.That(target.BufferWriter, Is.Null);
        });

        global::System.Buffers.ArrayBufferWriter<byte> bufferWriter = new global::System.Buffers.ArrayBufferWriter<byte>();
        target.Stream = null;
        target.BufferWriter = bufferWriter;

        Assert.Multiple(() =>
        {
            Assert.That(target.Stream, Is.Null);
            Assert.That(target.BufferWriter, Is.SameAs(bufferWriter));
        });
    }
}
