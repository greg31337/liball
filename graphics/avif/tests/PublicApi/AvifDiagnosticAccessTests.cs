namespace avif.Tests;

public class AvifDiagnosticAccessTests
{
    [Test]
    public void DiagnosticReportsPreserveEveryContextFieldAndOwnershipBoundary()
    {
        AvifDiagnostic original = new AvifDiagnostic(
            AvifResultCode.InvalidAv1Bitstream,
            AvifDiagnosticSeverity.Error,
            AvifDiagnosticCategory.Av1,
            "invalid tile syntax",
            4096,
            3,
            "/moov/trak/mdia",
            17,
            4,
            8,
            2,
            "frame_obu/tile_group/tile_size_minus_1",
            "AV1 section 6.10.1");
        AvifDiagnosticReport report = new AvifDiagnosticReport(
            new AvifDiagnostic[] { original });
        original.Message = "changed by caller";
        AvifDiagnostic? first = report.GetDiagnostic(0);

        Assert.Multiple(() =>
        {
            Assert.That(report.GetDiagnosticCount(), Is.EqualTo(1));
            Assert.That(report.GetDiagnostic(-1), Is.Null);
            Assert.That(report.GetDiagnostic(1), Is.Null);
            Assert.That(first, Is.Not.Null);
            Assert.That(
                (first ?? throw new InvalidOperationException("Invalid internal state.")).Code,
                Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(first.Severity, Is.EqualTo(AvifDiagnosticSeverity.Error));
            Assert.That(first.Category, Is.EqualTo(AvifDiagnosticCategory.Av1));
            Assert.That(first.Message, Is.EqualTo("invalid tile syntax"));
            Assert.That(first.ByteOffset, Is.EqualTo(4096));
            Assert.That(first.BitOffset, Is.EqualTo(3));
            Assert.That(first.BoxPath, Is.EqualTo("/moov/trak/mdia"));
            Assert.That(first.ItemId, Is.EqualTo(17));
            Assert.That(first.TrackId, Is.EqualTo(4));
            Assert.That(first.FrameIndex, Is.EqualTo(8));
            Assert.That(first.TileIndex, Is.EqualTo(2));
            Assert.That(
                first.Av1SyntaxPath,
                Is.EqualTo("frame_obu/tile_group/tile_size_minus_1"));
            Assert.That(
                first.StandardReference,
                Is.EqualTo("AV1 section 6.10.1"));
        });

        first.Message = "changed report copy";
        Assert.That(
            (report.GetDiagnostic(0)
                ?? throw new InvalidOperationException("Invalid internal state.")).Message,
            Is.EqualTo("invalid tile syntax"));
        IAvifDiagnosticReport readOnly = report;
        Assert.That(readOnly.GetDiagnosticCount(), Is.EqualTo(1));
    }

    [Test]
    public void InspectionAndLowLevelAv1ExposeOriginalParserContext()
    {
        byte[] truncatedFileType = new byte[]
        {
            0, 0, 0, 20,
            (byte)'f', (byte)'t', (byte)'y', (byte)'p',
            (byte)'a', (byte)'v', (byte)'i', (byte)'f'
        };
        AvifReadResult read = Avif.Read(truncatedFileType);
        AvifDecodeResult decode = Avif.Decode(truncatedFileType);
        AvifReader reader = new AvifReader(
            truncatedFileType,
            new AvifDecoderOptions());
        reader.Parse();
        AvifDiagnostic? readDiagnostic = read.GetDiagnostics().GetDiagnostic(0);

        byte[] truncatedObu = new byte[] { 0x0A, 2, 0x80 };
        Av1ObuReader obuReader = new Av1ObuReader(
            truncatedObu,
            new AvifSecurityLimits());
        Av1Decoder decoder = new Av1Decoder();
        decoder.Decode(truncatedObu);
        AvifDiagnostic? obuDiagnostic = obuReader.GetDiagnostics()
            .GetDiagnostic(0);
        AvifDiagnostic? decoderDiagnostic = decoder.GetDiagnostics()
            .GetDiagnostic(0);

        Assert.Multiple(() =>
        {
            Assert.That(read.GetCode(), Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(readDiagnostic, Is.Not.Null);
            Assert.That(
                (readDiagnostic ?? throw new InvalidOperationException("Invalid internal state.")).ByteOffset,
                Is.GreaterThanOrEqualTo(0));
            Assert.That(readDiagnostic.BoxPath, Is.Not.Empty);
            Assert.That(
                ((IAvifDiagnosticSource)decode).GetDiagnostics().GetDiagnosticCount(),
                Is.GreaterThan(0));
            Assert.That(reader.GetDiagnostics().GetDiagnosticCount(), Is.GreaterThan(0));
            Assert.That(
                (read.GetDocument() ?? throw new InvalidOperationException("Invalid internal state."))
                    .GetDiagnostics().GetDiagnosticCount(),
                Is.GreaterThan(0));
            Assert.That(obuReader.GetCode(), Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(obuDiagnostic, Is.Not.Null);
            Assert.That(
                (obuDiagnostic ?? throw new InvalidOperationException("Invalid internal state.")).Category,
                Is.EqualTo(AvifDiagnosticCategory.Av1));
            Assert.That(obuDiagnostic.ByteOffset, Is.GreaterThanOrEqualTo(0));
            Assert.That(obuDiagnostic.BitOffset, Is.GreaterThanOrEqualTo(0));
            Assert.That(obuDiagnostic.StandardReference, Is.Not.Empty);
            Assert.That(decoderDiagnostic, Is.Not.Null);
            Assert.That(
                (decoderDiagnostic ?? throw new InvalidOperationException("Invalid internal state.")).ByteOffset,
                Is.EqualTo(obuDiagnostic.ByteOffset));
            Assert.That(
                decoderDiagnostic.StandardReference,
                Is.EqualTo(obuDiagnostic.StandardReference));
        });

        reader.Close();
        obuReader.Close();
        decoder.Close();
    }

    [Test]
    public void EveryOperationFamilyProvidesCategorizedFailureDiagnostics()
    {
        AssertFailure(
            new AvifProbeResult(AvifResultCode.InvalidArgument),
            AvifDiagnosticCategory.Container);
        AssertFailure(
            new AvifReadResult(AvifResultCode.InvalidArgument, null),
            AvifDiagnosticCategory.Validation);
        AssertFailure(
            new AvifDecodeResult(AvifResultCode.DecodeFailed, null, null),
            AvifDiagnosticCategory.Rendering);
        AssertFailure(
            new AvifEncodeResult(AvifResultCode.EncodeFailed, null),
            AvifDiagnosticCategory.Encoding);

        Av1Encoder av1Encoder = new Av1Encoder();
        av1Encoder.AddFrame(null!, 0);
        AssertFailure(av1Encoder, AvifDiagnosticCategory.Encoding);
        Av1CodecConfigurationCodec configuration =
            new Av1CodecConfigurationCodec();
        configuration.ParseCodecString(null);
        AssertFailure(configuration, AvifDiagnosticCategory.Av1);

        AvifWriter writer = new AvifWriter(null!);
        AssertFailure(writer, AvifDiagnosticCategory.Encoding);
        AvifRewriter rewriter = new AvifRewriter(null!);
        AssertFailure(rewriter, AvifDiagnosticCategory.Rewrite);
        AvifIncrementalReader incremental = new AvifIncrementalReader(null);
        AssertFailure(incremental, AvifDiagnosticCategory.Input);
        AvifColorConverter converter = new AvifColorConverter(null);
        AssertFailure(converter, AvifDiagnosticCategory.Color);

        AvifExif exif = new AvifExif(
            System.Array.Empty<byte>(),
            new AvifSecurityLimits());
        AssertFailure(exif, AvifDiagnosticCategory.Metadata);
        AvifMetadata metadata = new AvifMetadata(
            string.Empty,
            string.Empty,
            null);
        AssertFailure(metadata, AvifDiagnosticCategory.Metadata);
        AvifGainMapMetadata gainMapMetadata = new AvifGainMapMetadata(
            System.Array.Empty<byte>());
        AssertFailure(gainMapMetadata, AvifDiagnosticCategory.Metadata);

        AvifPixelBuffer pixels = new AvifPixelBuffer(
            0,
            0,
            AvifPixelFormat.Rgba,
            8,
            false,
            1,
            new AvifSecurityLimits());
        AssertFailure(pixels, AvifDiagnosticCategory.Rendering);
        AvifGrid grid = new AvifGrid(
            0,
            1,
            1,
            1,
            new AvifSecurityLimits());
        AssertFailure(grid, AvifDiagnosticCategory.Rendering);
        AvifSampleTransform transform = new AvifSampleTransform(
            System.Array.Empty<byte>(),
            System.Array.Empty<AvifImage>(),
            8);
        AssertFailure(transform, AvifDiagnosticCategory.Rendering);

        av1Encoder.Close();
        writer.Close();
        rewriter.Close();
        incremental.Close();
        pixels.Close();
    }

    static void AssertFailure(
        IAvifDiagnosticSource source,
        AvifDiagnosticCategory category)
    {
        AvifDiagnosticReport report = source.GetDiagnostics();
        AvifDiagnostic? diagnostic = report.GetDiagnostic(0);
        Assert.Multiple(() =>
        {
            Assert.That(report.GetDiagnosticCount(), Is.EqualTo(1));
            Assert.That(diagnostic, Is.Not.Null);
            Assert.That(
                (diagnostic ?? throw new InvalidOperationException("Invalid internal state.")).Code,
                Is.Not.EqualTo(AvifResultCode.Success));
            Assert.That(diagnostic.Category, Is.EqualTo(category));
            Assert.That(diagnostic.Severity, Is.EqualTo(AvifDiagnosticSeverity.Error));
            Assert.That(diagnostic.Message, Is.Not.Empty);
        });
    }
}
