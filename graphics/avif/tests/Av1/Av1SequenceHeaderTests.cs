namespace avif.Tests;

public class Av1SequenceHeaderTests
{
    [Test]
    public void GeneralHeaderRetainsTimingModelsOperatingPointsAndTools()
    {
        byte[] payload = BuildComplexGeneralHeader();

        Av1SequenceHeaderParseResult result = Av1SequenceHeaderParser.ParsePayload(payload);
        ParsedAv1SequenceHeader? header = result.Header;
        Av1TimingInfo? timing = (header ?? throw new InvalidOperationException("Invalid internal state.")).GetTimingInfo();
        Av1DecoderModelInfo? model = header.GetDecoderModelInfo();
        Av1OperatingPoint? first = header.GetOperatingPoint(0);
        Av1OperatingPoint? second = header.GetOperatingPoint(1);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(header.GetSequenceProfile(), Is.EqualTo(2));
            Assert.That(header.IsStillPicture(), Is.False);
            Assert.That(header.HasReducedStillPictureHeader(), Is.False);
            Assert.That(header.HasTimingInfo(), Is.True);
            Assert.That((timing ?? throw new InvalidOperationException("Invalid internal state.")).NumUnitsInDisplayTick, Is.EqualTo(1001));
            Assert.That(timing.TimeScale, Is.EqualTo(60000));
            Assert.That(timing.EqualPictureInterval, Is.True);
            Assert.That(timing.NumTicksPerPictureMinusOne, Is.EqualTo(1));
            Assert.That(header.HasDecoderModelInfo(), Is.True);
            Assert.That((model ?? throw new InvalidOperationException("Invalid internal state.")).BufferDelayLength, Is.EqualTo(8));
            Assert.That(model.NumUnitsInDecodingTick, Is.EqualTo(100));
            Assert.That(model.BufferRemovalTimeLength, Is.EqualTo(5));
            Assert.That(model.FramePresentationTimeLength, Is.EqualTo(6));
            Assert.That(header.HasInitialDisplayDelay(), Is.True);
            Assert.That(header.GetOperatingPointCount(), Is.EqualTo(2));
            Assert.That((first ?? throw new InvalidOperationException("Invalid internal state.")).Idc, Is.EqualTo(0x101));
            Assert.That(first.Level, Is.EqualTo(7));
            Assert.That(first.Tier, Is.Zero);
            Assert.That(first.DecoderModelPresent, Is.True);
            Assert.That(first.DecoderBufferDelay, Is.EqualTo(11));
            Assert.That(first.EncoderBufferDelay, Is.EqualTo(22));
            Assert.That(first.LowDelayMode, Is.True);
            Assert.That(first.InitialDisplayDelayPresent, Is.True);
            Assert.That(first.InitialDisplayDelayMinusOne, Is.EqualTo(3));
            Assert.That((second ?? throw new InvalidOperationException("Invalid internal state.")).Idc, Is.EqualTo(0x302));
            Assert.That(second.Level, Is.EqualTo(12));
            Assert.That(second.Tier, Is.EqualTo(1));
            Assert.That(second.DecoderModelPresent, Is.False);
            Assert.That(second.InitialDisplayDelayPresent, Is.False);
            Assert.That(header.GetOperatingPoint(2), Is.Null);
        });

        Assert.Multiple(() =>
        {
            Assert.That(header.GetMaximumWidth(), Is.EqualTo(1920));
            Assert.That(header.GetMaximumHeight(), Is.EqualTo(1080));
            Assert.That(header.GetFrameWidthBits(), Is.EqualTo(11));
            Assert.That(header.GetFrameHeightBits(), Is.EqualTo(11));
            Assert.That(header.HasFrameIdNumbers(), Is.True);
            Assert.That(header.GetDeltaFrameIdLength(), Is.EqualTo(6));
            Assert.That(header.GetAdditionalFrameIdLength(), Is.EqualTo(4));
            Assert.That(header.GetFrameIdLength(), Is.EqualTo(10));
            Assert.That(header.Uses128x128Superblocks(), Is.True);
            Assert.That(header.IsFilterIntraEnabled(), Is.True);
            Assert.That(header.IsIntraEdgeFilterEnabled(), Is.False);
            Assert.That(header.IsInterIntraCompoundEnabled(), Is.True);
            Assert.That(header.IsMaskedCompoundEnabled(), Is.False);
            Assert.That(header.IsWarpedMotionEnabled(), Is.True);
            Assert.That(header.IsDualFilterEnabled(), Is.True);
            Assert.That(header.IsOrderHintEnabled(), Is.True);
            Assert.That(header.IsJointCompoundEnabled(), Is.True);
            Assert.That(header.AreReferenceFrameMotionVectorsEnabled(), Is.False);
            Assert.That(header.ChoosesScreenContentTools(), Is.False);
            Assert.That(header.GetScreenContentToolsMode(), Is.EqualTo(1));
            Assert.That(header.ChoosesIntegerMotionVectors(), Is.False);
            Assert.That(header.GetIntegerMotionVectorMode(), Is.EqualTo(1));
            Assert.That(header.GetOrderHintBits(), Is.EqualTo(7));
            Assert.That(header.IsSuperresEnabled(), Is.True);
            Assert.That(header.IsCdefEnabled(), Is.True);
            Assert.That(header.IsRestorationEnabled(), Is.False);
        });

        Assert.Multiple(() =>
        {
            Assert.That(header.GetBitDepth(), Is.EqualTo(12));
            Assert.That(header.IsMonochrome(), Is.False);
            Assert.That(header.HasColorDescription(), Is.True);
            Assert.That(header.GetColorPrimaries(), Is.EqualTo(9));
            Assert.That(header.GetTransferCharacteristics(), Is.EqualTo(16));
            Assert.That(header.GetMatrixCoefficients(), Is.EqualTo(9));
            Assert.That(header.IsFullRange(), Is.False);
            Assert.That(header.HasChromaSubsamplingX(), Is.True);
            Assert.That(header.HasChromaSubsamplingY(), Is.True);
            Assert.That(header.GetChromaSamplePosition(), Is.EqualTo(2));
            Assert.That(header.HasSeparateUvDeltaQuantizer(), Is.True);
            Assert.That(header.HasFilmGrainParameters(), Is.True);
            Assert.That(header.PayloadEquals(payload), Is.True);
        });
    }

    [Test]
    public void ReducedHeaderInfersNormativeDefaults()
    {
        byte[] payload = BuildReducedHeader(
            0,
            4,
            false,
            false,
            true,
            true,
            true,
            0,
            false);

        Av1SequenceHeaderParseResult result = Av1SequenceHeaderParser.ParsePayload(payload);
        ParsedAv1SequenceHeader? header = result.Header;

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((header ?? throw new InvalidOperationException("Invalid internal state.")).IsStillPicture(), Is.True);
            Assert.That(header.HasReducedStillPictureHeader(), Is.True);
            Assert.That(header.HasTimingInfo(), Is.False);
            Assert.That(header.HasDecoderModelInfo(), Is.False);
            Assert.That(header.HasInitialDisplayDelay(), Is.False);
            Assert.That(header.GetOperatingPointCount(), Is.EqualTo(1));
            Assert.That((header.GetOperatingPoint(0) ?? throw new InvalidOperationException("Invalid internal state.")).Idc, Is.Zero);
            Assert.That((header.GetOperatingPoint(0) ?? throw new InvalidOperationException("Invalid internal state.")).Level, Is.EqualTo(4));
            Assert.That(header.HasFrameIdNumbers(), Is.False);
            Assert.That(header.IsInterIntraCompoundEnabled(), Is.False);
            Assert.That(header.IsMaskedCompoundEnabled(), Is.False);
            Assert.That(header.IsWarpedMotionEnabled(), Is.False);
            Assert.That(header.IsDualFilterEnabled(), Is.False);
            Assert.That(header.IsOrderHintEnabled(), Is.False);
            Assert.That(header.GetScreenContentToolsMode(), Is.EqualTo(2));
            Assert.That(header.GetIntegerMotionVectorMode(), Is.EqualTo(2));
            Assert.That(header.GetOrderHintBits(), Is.Zero);
            Assert.That(header.GetBitDepth(), Is.EqualTo(8));
            Assert.That(header.IsMonochrome(), Is.True);
            Assert.That(header.GetPlaneCount(), Is.EqualTo(1));
            Assert.That(header.HasChromaSubsamplingX(), Is.True);
            Assert.That(header.HasChromaSubsamplingY(), Is.True);
            Assert.That(header.HasSeparateUvDeltaQuantizer(), Is.False);
        });
    }

    [TestCase(0, false, false, true, true, true, 0, 8)]
    [TestCase(0, false, false, false, true, true, 1, 8)]
    [TestCase(0, true, false, true, true, true, 0, 10)]
    [TestCase(0, true, false, false, true, true, 2, 10)]
    [TestCase(1, false, false, false, false, false, 0, 8)]
    [TestCase(1, true, false, false, false, false, 0, 10)]
    [TestCase(2, false, false, true, true, true, 0, 8)]
    [TestCase(2, false, false, false, true, false, 0, 8)]
    [TestCase(2, true, false, true, true, true, 0, 10)]
    [TestCase(2, true, false, false, true, false, 0, 10)]
    [TestCase(2, true, true, true, true, true, 0, 12)]
    [TestCase(2, true, true, false, false, false, 0, 12)]
    [TestCase(2, true, true, false, true, false, 0, 12)]
    [TestCase(2, true, true, false, true, true, 2, 12)]
    public void ProfileColorBranchesProduceExpectedDepthAndSubsampling(
        int profile,
        bool highBitDepth,
        bool twelveBit,
        bool monochrome,
        bool subsamplingX,
        bool subsamplingY,
        int chromaSamplePosition,
        int expectedBitDepth)
    {
        byte[] payload = BuildReducedHeader(
            profile,
            4,
            highBitDepth,
            twelveBit,
            monochrome,
            subsamplingX,
            subsamplingY,
            chromaSamplePosition,
            true);

        Av1SequenceHeaderParseResult result = Av1SequenceHeaderParser.ParsePayload(payload);
        Av1SemanticValidationResult validation =
            Av1SemanticValidator.ValidateSequence(result.Header);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(validation.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((result.Header ?? throw new InvalidOperationException("Invalid internal state.")).GetSequenceProfile(), Is.EqualTo(profile));
            Assert.That(result.Header.GetBitDepth(), Is.EqualTo(expectedBitDepth));
            Assert.That(result.Header.IsMonochrome(), Is.EqualTo(monochrome));
            Assert.That(result.Header.HasChromaSubsamplingX(), Is.EqualTo(subsamplingX));
            Assert.That(result.Header.HasChromaSubsamplingY(), Is.EqualTo(subsamplingY));
            Assert.That(result.Header.GetChromaSamplePosition(),
                Is.EqualTo(chromaSamplePosition));
            Assert.That(result.Header.HasSeparateUvDeltaQuantizer(),
                Is.EqualTo(!monochrome));
        });
    }

    [Test]
    public void InvalidLocalSemanticsTruncationAndTrailingBitsAreRejected()
    {
        byte[] reducedWithoutStill = new byte[2];
        BitWriter reducedWriter = BitWriter.Create(reducedWithoutStill.Length);
        reducedWriter.WriteBits(reducedWithoutStill, 0, 3);
        reducedWriter.WriteBit(reducedWithoutStill, 0);
        reducedWriter.WriteBit(reducedWithoutStill, 1);

        byte[] invalidLevel = BuildReducedHeader(
            0, 24, false, false, false, true, true, 0, false);
        byte[] zeroDisplayTick = BuildInvalidTimingHeader(false);
        byte[] zeroDecodingTick = BuildInvalidTimingHeader(true);
        byte[] valid = BuildComplexGeneralHeader();
        byte[] truncated = valid[..^1];
        byte[] badTrailing = new byte[valid.Length + 1];
        valid.CopyTo(badTrailing, 0);
        badTrailing[^1] = 1;

        Av1SequenceHeaderParseResult reduced =
            Av1SequenceHeaderParser.ParsePayload(reducedWithoutStill);
        Av1SequenceHeaderParseResult level =
            Av1SequenceHeaderParser.ParsePayload(invalidLevel);
        Av1SequenceHeaderParseResult display =
            Av1SequenceHeaderParser.ParsePayload(zeroDisplayTick);
        Av1SequenceHeaderParseResult decoding =
            Av1SequenceHeaderParser.ParsePayload(zeroDecodingTick);
        Av1SequenceHeaderParseResult shortResult =
            Av1SequenceHeaderParser.ParsePayload(truncated);
        Av1SequenceHeaderParseResult trailing =
            Av1SequenceHeaderParser.ParsePayload(badTrailing);

        Assert.Multiple(() =>
        {
            Assert.That(reduced.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(level.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(display.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(decoding.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(shortResult.Code, Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(trailing.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
        });
    }

    static byte[] BuildComplexGeneralHeader()
    {
        byte[] payload = new byte[128];
        BitWriter writer = BitWriter.Create(payload.Length);
        writer.WriteBits(payload, 2, 3);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 1001, 32);
        writer.WriteBits(payload, 60000, 32);
        writer.WriteBit(payload, 1);
        writer.WriteUvlc(payload, 1);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 7, 5);
        writer.WriteBits(payload, 100, 32);
        writer.WriteBits(payload, 4, 5);
        writer.WriteBits(payload, 5, 5);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 1, 5);

        writer.WriteBits(payload, 0x101, 12);
        writer.WriteBits(payload, 7, 5);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 11, 8);
        writer.WriteBits(payload, 22, 8);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 3, 4);

        writer.WriteBits(payload, 0x302, 12);
        writer.WriteBits(payload, 12, 5);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 0);

        writer.WriteBits(payload, 10, 4);
        writer.WriteBits(payload, 10, 4);
        writer.WriteBits(payload, 1919, 11);
        writer.WriteBits(payload, 1079, 11);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 4, 4);
        writer.WriteBits(payload, 3, 3);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 6, 3);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 0);

        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 9, 8);
        writer.WriteBits(payload, 16, 8);
        writer.WriteBits(payload, 9, 8);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 2, 2);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 1);
        return FinishPayload(payload, writer);
    }

    static byte[] BuildReducedHeader(
        int profile,
        int level,
        bool highBitDepth,
        bool twelveBit,
        bool monochrome,
        bool subsamplingX,
        bool subsamplingY,
        int chromaSamplePosition,
        bool separateUvDeltaQuantizer)
    {
        byte[] payload = new byte[64];
        BitWriter writer = BitWriter.Create(payload.Length);
        writer.WriteBits(payload, (uint)profile, 3);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, (uint)level, 5);
        writer.WriteBits(payload, 5, 4);
        writer.WriteBits(payload, 4, 4);
        writer.WriteBits(payload, 63, 6);
        writer.WriteBits(payload, 31, 5);
        writer.WriteBits(payload, 0, 3);
        writer.WriteBits(payload, 0, 3);

        writer.WriteBit(payload, highBitDepth ? 1 : 0);
        if (profile == 2 && highBitDepth)
        {
            writer.WriteBit(payload, twelveBit ? 1 : 0);
        }

        if (profile != 1)
        {
            writer.WriteBit(payload, monochrome ? 1 : 0);
        }

        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 1);
        if (!monochrome)
        {
            if (profile == 2 && highBitDepth && twelveBit)
            {
                writer.WriteBit(payload, subsamplingX ? 1 : 0);
                if (subsamplingX)
                {
                    writer.WriteBit(payload, subsamplingY ? 1 : 0);
                }
            }

            if (subsamplingX && subsamplingY)
            {
                writer.WriteBits(payload, (uint)chromaSamplePosition, 2);
            }

            writer.WriteBit(payload, separateUvDeltaQuantizer ? 1 : 0);
        }

        writer.WriteBit(payload, 0);
        return FinishPayload(payload, writer);
    }

    static byte[] BuildInvalidTimingHeader(bool decoderTickOnly)
    {
        byte[] payload = new byte[32];
        BitWriter writer = BitWriter.Create(payload.Length);
        writer.WriteBits(payload, 0, 3);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, decoderTickOnly ? 1U : 0U, 32);
        writer.WriteBits(payload, 1, 32);
        writer.WriteBit(payload, 0);
        if (decoderTickOnly)
        {
            writer.WriteBit(payload, 1);
            writer.WriteBits(payload, 0, 5);
            writer.WriteBits(payload, 0, 32);
            writer.WriteBits(payload, 0, 5);
            writer.WriteBits(payload, 0, 5);
        }

        return payload;
    }

    static byte[] FinishPayload(byte[] payload, BitWriter writer)
    {
        writer.WriteTrailingBits(payload);
        Assert.That(writer.Code, Is.EqualTo(AvifResultCode.Success));
        return payload[..(int)(writer.BitPosition / 8)];
    }
}
