using raw;

namespace raw.Tests;

public class PublicEntryPointCoverageTests
{
    [Test]
    public void ProcessingAliasesExecuteTheSameValidatedPublicPipeline()
    {
        IRawCodec codec = new RawCodecService();
        RawImageInfo info = BayerInfo();
        RawStoredImage stored = new RawStoredImage(info, new uint[]
        {
            256, 512, 768, 1024,
            384, 640, 896, 1152,
            1280, 1536, 1792, 2048,
            1408, 1664, 1920, 2176
        }, null, null, 4, true, null);
        RawNormalizationOptions normalization = new RawNormalizationOptions();
        normalization.WhiteLevels = new double[] { 4095 };

        RawSensorImage preprocessed = codec.Preprocess(stored, normalization);
        RawSensorImage repaired = codec.RepairDefects(preprocessed, new RawDefectOptions());
        RawSensorImage corrected = codec.CorrectLens(repaired, new RawLensCorrectionOptions());
        RawRgbImage preview = codec.CreateHalfSizePreview(corrected, new RawPreviewOptions());
        RawAnalysisResult analysis = codec.Analyze(corrected, new RawAnalysisOptions());
        RawHistogramResult histogram = codec.Histogram(corrected, new RawHistogramOptions());

        Assert.Multiple(() =>
        {
            Assert.That(preprocessed.Validate(new RawLimits()).IsValid(), Is.True);
            Assert.That(repaired.Validate(new RawLimits()).IsValid(), Is.True);
            Assert.That(corrected.Validate(new RawLimits()).IsValid(), Is.True);
            Assert.That(preview.Width, Is.EqualTo(2));
            Assert.That(preview.Height, Is.EqualTo(2));
            Assert.That(analysis.Statistics, Has.Length.EqualTo(4));
            Assert.That(histogram.Channels, Has.Length.EqualTo(1));
            Assert.That(histogram.Channels.Sum(channel => channel.SampleCount), Is.EqualTo(16));
        });
    }

    [Test]
    public void MemoryMetadataPreviewDescriptorAndSidecarAliasesReturnPublicModels()
    {
        IRawCodec codec = new RawCodecService();
        NefFixture fixture = NefParserTests.BuildNef();
        RawDocument metadata = codec.OpenMetadata(fixture.Bytes, null, new RawMetadataOptions());
        RawDocument previews = codec.OpenPreviews(fixture.Bytes, null, new RawOpenOptions());
        RawDescriptorHints hints = new RawDescriptorHints();
        hints.Width = 4;
        hints.Height = 2;
        hints.BitDepth = 8;
        hints.StoredWordBits = 8;
        hints.SampleKindKnown = true;
        hints.SampleKind = RawSampleKind.UnsignedInteger;
        hints.PackingKnown = true;
        hints.Packing = RawPacking.ByteAligned;
        hints.ByteOrderKnown = true;
        hints.ByteOrder = RawByteOrder.LittleEndian;
        hints.LayoutKnown = true;
        hints.Layout = RawSampleLayout.Interleaved;
        hints.ChannelCount = 1;
        hints.PhotometricKnown = true;
        hints.Photometric = RawPhotometricInterpretation.BlackIsZero;
        RawDescriptorSuggestionResult suggestion = codec.SuggestGenericDescriptor(8, hints, new RawLimits(), CancellationToken.None);
        RawSidecar sidecar = new RawSidecar(1, suggestion.Candidates.Single().Descriptor, null);
        byte[] json = codec.WriteSidecarBytes(sidecar, new RawLimits(), CancellationToken.None);
        RawSidecar parsed = codec.ReadSidecar(json, new RawLimits());

        Assert.Multiple(() =>
        {
            Assert.That(metadata.MetadataLoaded, Is.True);
            Assert.That(metadata.RawPayloadAvailable, Is.False);
            Assert.That(previews.PreviewsLoaded, Is.True);
            Assert.That(previews.RawPayloadAvailable, Is.False);
            Assert.That(previews.Thumbnails, Has.Length.EqualTo(2));
            Assert.That(suggestion.Candidates, Has.Length.EqualTo(1));
            Assert.That(parsed.Descriptor.Width, Is.EqualTo(4));
            Assert.That(parsed.Descriptor.Height, Is.EqualTo(2));
        });
    }

    [Test]
    public void DefaultWriterEntryPointsExposeBuiltInGatesAndRewritePlans()
    {
        IRawWriter writer = new RawWriterService();
        RawCr2WriteInput cr2Input = Cr2EncoderTests.Input(8, 4, 8);
        RawWriteOptions cr2Options = Cr2EncoderTests.Options();
        cr2Options.ProfileName = RawBuiltInProfileCatalogue.Eos6DMarkIiWriteProfile;
        cr2Options.AllowExperimentalProfiles = false;
        RawNefWriteInput nefInput = NefEncoderTests.Input(8, 4, 8);
        RawWriteOptions nefOptions = NefEncoderTests.Options();
        nefOptions.ProfileName = RawBuiltInProfileCatalogue.NikonZ7UncompressedWriteProfile;
        nefOptions.AllowExperimentalProfiles = false;
        RawDocument cr2Document = Open(Cr2Encoder.Encode(Cr2EncoderTests.Input(8, 4, 8), Cr2EncoderTests.SmallProfile(), Cr2EncoderTests.Options()));
        RawDocument nefDocument = Open(NefParserTests.BuildNef().Bytes);

        RawWritePlan cr2Rewrite = writer.PlanRewriteCr2(cr2Document, new RawWriteOptions());
        RawWritePlan nefRewrite = writer.PlanRewriteNef(nefDocument, new RawWriteOptions());

        Assert.Multiple(() =>
        {
            Assert.Throws<RawUnsupportedException>(() => writer.PlanCr2(cr2Input, cr2Options));
            Assert.Throws<RawUnsupportedException>(() => writer.EncodeCr2(cr2Input, cr2Options));
            Assert.Throws<RawUnsupportedException>(() => writer.PlanNef(nefInput, nefOptions));
            Assert.Throws<RawUnsupportedException>(() => writer.EncodeNef(nefInput, nefOptions));
            Assert.That(cr2Rewrite.CanWrite, Is.True);
            Assert.That(nefRewrite.CanWrite, Is.True);
            Assert.That(cr2Rewrite.PixelValidation.Kind, Is.EqualTo(RawPixelValidationKind.None));
            Assert.That(nefRewrite.PixelValidation.Kind, Is.EqualTo(RawPixelValidationKind.None));
            Assert.That(cr2Rewrite.PixelValidation.Required, Is.False);
            Assert.That(nefRewrite.PixelValidation.Required, Is.False);
        });
    }

    static RawImageInfo BayerInfo()
    {
        RawChannelDescription[] channels = new RawChannelDescription[] { new RawChannelDescription("CFA", RawChannelKind.Custom) };
        RawPlaneInfo[] planes = new RawPlaneInfo[] { new RawPlaneInfo(0, "CFA", RawChannelKind.Custom, 4, 4, 12, 4) };
        return new RawImageInfo(0, RawImageKind.Raw, 4, 4, 12, RawSampleKind.UnsignedInteger, RawPhotometricInterpretation.ColorFilterArray, RawCompression.None, RawOrientation.Normal, channels, planes, RawCfaPattern.CreateBayer(RawCfaLayout.Rggb), new RawArea(RawAreaKind.StoredImage, 0, 0, 4, 4), null, null, null);
    }

    static RawDocument Open(byte[] source)
    {
        IRawCodec codec = new RawCodecService();
        RawOpenOptions options = new RawOpenOptions();
        options.AllowExperimentalProfiles = true;
        options.ReadPreviews = true;
        return codec.Open(source, null, options);
    }
}
