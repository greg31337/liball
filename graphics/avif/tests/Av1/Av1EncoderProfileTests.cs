namespace avif.Tests;

public class Av1EncoderProfileTests
{
    [TestCase(8, AvifChromaFormat.Monochrome, 0)]
    [TestCase(8, AvifChromaFormat.Yuv420, 0)]
    [TestCase(8, AvifChromaFormat.Yuv422, 2)]
    [TestCase(8, AvifChromaFormat.Yuv444, 1)]
    [TestCase(10, AvifChromaFormat.Monochrome, 0)]
    [TestCase(10, AvifChromaFormat.Yuv420, 0)]
    [TestCase(10, AvifChromaFormat.Yuv422, 2)]
    [TestCase(10, AvifChromaFormat.Yuv444, 1)]
    [TestCase(12, AvifChromaFormat.Monochrome, 2)]
    [TestCase(12, AvifChromaFormat.Yuv420, 2)]
    [TestCase(12, AvifChromaFormat.Yuv422, 2)]
    [TestCase(12, AvifChromaFormat.Yuv444, 2)]
    public void AutomaticSelectionWritesEveryLegalDepthAndChromaCombination(
        int bitDepth,
        AvifChromaFormat chromaFormat,
        int expectedProfile)
    {
        Av1EncoderProfileRequest request = new Av1EncoderProfileRequest();
        request.BitDepth = bitDepth;
        request.ChromaFormat = chromaFormat;
        request.ChromaSamplePosition = AvifChromaSamplePosition.Colocated;

        Av1EncoderProfileDecision decision =
            Av1EncoderProfileSelector.Choose(request);
        Av1WriteResult write = Av1SequenceHeaderWriter.WritePayload(
            CreateHeader(decision, expectedProfile));
        Av1SequenceHeaderParseResult parsed =
            Av1SequenceHeaderParser.ParsePayload(write.Data);

        Assert.Multiple(() =>
        {
            Assert.That(decision.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((int)decision.Profile, Is.EqualTo(expectedProfile));
            Assert.That(decision.BitDepth, Is.EqualTo(bitDepth));
            Assert.That(decision.Monochrome,
                Is.EqualTo(chromaFormat == AvifChromaFormat.Monochrome));
            Assert.That(write.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((parsed.Header ?? throw new InvalidOperationException("Invalid internal state.")).GetSequenceProfile(),
                Is.EqualTo(expectedProfile));
            Assert.That(parsed.Header.GetBitDepth(), Is.EqualTo(bitDepth));
            Assert.That(parsed.Header.IsMonochrome(),
                Is.EqualTo(chromaFormat == AvifChromaFormat.Monochrome));
            Assert.That(parsed.Header.HasChromaSubsamplingX(),
                Is.EqualTo(chromaFormat == AvifChromaFormat.Monochrome
                    || chromaFormat == AvifChromaFormat.Yuv420
                    || chromaFormat == AvifChromaFormat.Yuv422));
            Assert.That(parsed.Header.HasChromaSubsamplingY(),
                Is.EqualTo(chromaFormat == AvifChromaFormat.Monochrome
                    || chromaFormat == AvifChromaFormat.Yuv420));
        });
    }

    [Test]
    public void ExplicitProfilesMustSupportTheRequestedPixelFormat()
    {
        Av1EncoderProfileDecision high420 = Select(
            10, AvifChromaFormat.Yuv420, 1);
        Av1EncoderProfileDecision main444 = Select(
            10, AvifChromaFormat.Yuv444, 0);
        Av1EncoderProfileDecision high422 = Select(
            8, AvifChromaFormat.Yuv422, 1);
        Av1EncoderProfileDecision high12 = Select(
            12, AvifChromaFormat.Yuv444, 1);
        Av1EncoderProfileDecision professionalMono = Select(
            10, AvifChromaFormat.Monochrome, 2);

        Assert.Multiple(() =>
        {
            Assert.That(high420.Code,
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(main444.Code,
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(high422.Code,
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(high12.Code,
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(professionalMono.Code,
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(professionalMono.Profile,
                Is.EqualTo(Av1EncoderProfile.Professional));
        });
    }

    [Test]
    public void ImageSelectionRejectsUnsupportedDepthAndClosedImages()
    {
        AvifImage high = AvifImage.Create(
            4, 4, 8, AvifChromaFormat.Yuv444, false);
        AvifImage sixteenBit = AvifImage.Create(
            4, 4, 16, AvifChromaFormat.Yuv444, false);
        Av1EncoderProfileDecision selected =
            Av1EncoderProfileSelector.Choose(high, -1);
        Av1EncoderProfileDecision unsupported =
            Av1EncoderProfileSelector.Choose(sixteenBit, -1);
        high.Close();
        Av1EncoderProfileDecision closed =
            Av1EncoderProfileSelector.Choose(high, -1);

        Assert.Multiple(() =>
        {
            Assert.That(selected.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(selected.Profile, Is.EqualTo(Av1EncoderProfile.High));
            Assert.That(unsupported.Code,
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(closed.Code,
                Is.EqualTo(AvifResultCode.InvalidArgument));
        });
        sixteenBit.Close();
    }

    [Test]
    public void SequenceWriterRejectsAProfileThatCannotRepresentTheFormat()
    {
        Av1EncoderProfileDecision twelveBit444 = Select(
            12, AvifChromaFormat.Yuv444, -1);
        ParsedAv1SequenceHeader wrongProfile = CreateHeader(
            twelveBit444,
            (int)Av1EncoderProfile.Main);

        Av1WriteResult result =
            Av1SequenceHeaderWriter.WritePayload(wrongProfile);

        Assert.That(result.Code, Is.EqualTo(AvifResultCode.InvalidArgument));
    }

    static Av1EncoderProfileDecision Select(
        int bitDepth,
        AvifChromaFormat chromaFormat,
        int requestedProfile)
    {
        Av1EncoderProfileRequest request = new Av1EncoderProfileRequest();
        request.BitDepth = bitDepth;
        request.ChromaFormat = chromaFormat;
        request.RequestedProfile = requestedProfile;
        return Av1EncoderProfileSelector.Choose(request);
    }

    static ParsedAv1SequenceHeader CreateHeader(
        Av1EncoderProfileDecision decision,
        int profile)
    {
        Av1SequenceFeatures features = new Av1SequenceFeatures();
        features.FrameWidthBits = 6;
        features.FrameHeightBits = 6;
        return new ParsedAv1SequenceHeader(
            profile,
            true,
            true,
            null,
            null,
            false,
            new Av1OperatingPoint[] { new Av1OperatingPoint(0, 0, 0) },
            64,
            64,
            features,
            decision.HighBitDepth,
            decision.TwelveBit,
            decision.Monochrome,
            decision.ChromaSubsamplingX,
            decision.ChromaSubsamplingY,
            decision.ChromaSamplePosition,
            2,
            2,
            2,
            true,
            false,
            false,
            false,
            System.Array.Empty<byte>());
    }
}
