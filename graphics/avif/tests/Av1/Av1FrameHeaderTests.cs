namespace avif.Tests;

public class Av1FrameHeaderTests
{
    [Test]
    public void ReducedKeyFrameParsesLosslessLayoutAndFilmGrain()
    {
        ParsedAv1SequenceHeader sequence = CreateSequence(
            true, 64, 32, false, true, true, false, false, true);
        byte[] payload = new byte[64];
        BitWriter writer = BitWriter.Create(payload.Length);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 0, 8);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 0x2468, 16);
        writer.WriteBits(payload, 1, 4);
        writer.WriteBits(payload, 12, 8);
        writer.WriteBits(payload, 34, 8);
        writer.WriteBits(payload, 1, 2);
        writer.WriteBits(payload, 0, 2);
        writer.WriteBits(payload, 2, 2);
        writer.WriteBits(payload, 1, 2);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 0);
        payload = FinishPayload(payload, writer);

        Av1FrameHeaderParseResult result = Av1FrameHeaderParser.ParsePayload(
            payload, sequence, new Av1FrameHeaderState(), 2, 1, true);
        ParsedAv1FrameHeader? header = result.Header;

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((header ?? throw new InvalidOperationException("Invalid internal state.")).TemporalId, Is.EqualTo(2));
            Assert.That(header.SpatialId, Is.EqualTo(1));
            Assert.That(header.FrameType, Is.EqualTo(Av1FrameType.Key));
            Assert.That(header.FrameIsIntra, Is.True);
            Assert.That(header.ShowFrame, Is.True);
            Assert.That(header.ErrorResilientMode, Is.True);
            Assert.That(header.RefreshFrameFlags, Is.EqualTo(255));
            Assert.That((header.Size ?? throw new InvalidOperationException("Invalid internal state.")).UpscaledWidth, Is.EqualTo(64));
            Assert.That(header.Size.FrameWidth, Is.EqualTo(64));
            Assert.That(header.Size.FrameHeight, Is.EqualTo(32));
            Assert.That(header.Size.RenderWidth, Is.EqualTo(64));
            Assert.That(header.Size.RenderHeight, Is.EqualTo(32));
            Assert.That((header.Tiles ?? throw new InvalidOperationException("Invalid internal state.")).TileColumns, Is.EqualTo(1));
            Assert.That(header.Tiles.TileRows, Is.EqualTo(1));
            Assert.That((header.Quantization ?? throw new InvalidOperationException("Invalid internal state.")).BaseQIndex, Is.Zero);
            Assert.That(header.CodedLossless, Is.True);
            Assert.That(header.AllLossless, Is.True);
            Assert.That(header.TransformMode, Is.EqualTo(Av1TransformMode.Only4x4));
            Assert.That(header.ReducedTransformSet, Is.True);
            Assert.That(((header.GlobalMotion ?? throw new InvalidOperationException("Invalid internal state.")).Types ?? throw new InvalidOperationException("Invalid internal state."))[1], Is.EqualTo(Av1WarpModelType.Identity));
        });

        Assert.Multiple(() =>
        {
            Assert.That(((header ?? throw new InvalidOperationException("Invalid internal state.")).FilmGrain ?? throw new InvalidOperationException("Invalid internal state.")).ApplyGrain, Is.True);
            Assert.That((header.FilmGrain ?? throw new InvalidOperationException("Invalid internal state.")).GrainSeed, Is.EqualTo(0x2468));
            Assert.That(header.FilmGrain.UpdateGrain, Is.True);
            Assert.That(header.FilmGrain.YPoints, Has.Length.EqualTo(1));
            Assert.That(header.FilmGrain.YPoints[0], Is.EqualTo(new int[] { 12, 34 }));
            Assert.That(header.FilmGrain.ScalingShift, Is.EqualTo(9));
            Assert.That(header.FilmGrain.ArCoefficientLag, Is.Zero);
            Assert.That(header.FilmGrain.ArCoefficientShift, Is.EqualTo(8));
            Assert.That(header.FilmGrain.GrainScaleShift, Is.EqualTo(1));
            Assert.That(header.FilmGrain.Overlap, Is.True);
            Assert.That(header.FilmGrain.ClipToRestrictedRange, Is.False);
        });
    }

    [Test]
    public void InterFrameParsesReferencesMotionAndPredictedFilmGrain()
    {
        ParsedAv1SequenceHeader sequence = CreateSequence(
            false, 64, 64, false, false, false, true, false, true);
        Av1FrameHeaderState state = new Av1FrameHeaderState();
        Av1FilmGrainParameters storedGrain = new Av1FilmGrainParameters();
        storedGrain.ApplyGrain = true;
        storedGrain.ScalingShift = 10;
        storedGrain.YPoints = new int[][] { new int[] { 8, 24 } };
        state.SetReferenceFilmGrain(0, storedGrain);

        byte[] payload = new byte[64];
        BitWriter writer = BitWriter.Create(payload.Length);
        writer.WriteBit(payload, 0);
        writer.WriteBits(payload, 1, 2);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 0);
        writer.WriteBits(payload, 0, 3);
        writer.WriteBits(payload, 1, 8);
        int reference = 0;
        while (reference < Av1SyntaxConstants.RefsPerFrame)
        {
            writer.WriteBits(payload, (uint)reference, 3);
            reference++;
        }

        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 0, 8);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 0, 4);
        writer.WriteBits(payload, 0, 4);
        reference = 2;
        while (reference < Av1SyntaxConstants.TotalRefsPerFrame)
        {
            writer.WriteBit(payload, 0);
            reference++;
        }

        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 0x1234, 16);
        writer.WriteBit(payload, 0);
        writer.WriteBits(payload, 0, 3);
        payload = FinishPayload(payload, writer);

        Av1FrameHeaderParseResult result = Av1FrameHeaderParser.ParsePayload(
            payload, sequence, state, 0, 0, true);
        ParsedAv1FrameHeader? header = result.Header;

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((header ?? throw new InvalidOperationException("Invalid internal state.")).FrameType, Is.EqualTo(Av1FrameType.Inter));
            Assert.That(header.ReferenceFrameIndexes,
                Is.EqualTo(new int[] { 0, 1, 2, 3, 4, 5, 6 }));
            Assert.That(header.AllowHighPrecisionMotionVectors, Is.True);
            Assert.That(header.InterpolationFilter,
                Is.EqualTo(Av1InterpolationFilter.Switchable));
            Assert.That(header.MotionModeSwitchable, Is.True);
            Assert.That(header.ReferenceSelect, Is.True);
            Assert.That(header.AllowWarpedMotion, Is.True);
            Assert.That(((header.GlobalMotion ?? throw new InvalidOperationException("Invalid internal state.")).Types ?? throw new InvalidOperationException("Invalid internal state."))[1],
                Is.EqualTo(Av1WarpModelType.Translation));
            Assert.That((header.GlobalMotion.Parameters ?? throw new InvalidOperationException("Invalid internal state."))[1][0], Is.Zero);
            Assert.That(header.GlobalMotion.Parameters[1][1], Is.Zero);
            Assert.That((header.GlobalMotion.Types ?? throw new InvalidOperationException("Invalid internal state."))[7], Is.EqualTo(Av1WarpModelType.Identity));
            Assert.That((header.FilmGrain ?? throw new InvalidOperationException("Invalid internal state.")).GrainSeed, Is.EqualTo(0x1234));
            Assert.That(header.FilmGrain.UpdateGrain, Is.False);
            Assert.That(header.FilmGrain.ReferenceIndex, Is.Zero);
            Assert.That(header.FilmGrain.ScalingShift, Is.EqualTo(10));
            Assert.That(header.FilmGrain.YPoints[0], Is.EqualTo(new int[] { 8, 24 }));
        });
    }

    [Test]
    public void NonLosslessFrameParsesTilesSegmentationAndFilters()
    {
        ParsedAv1SequenceHeader sequence = CreateSequence(
            true, 128, 64, false, true, true, false, false, false);
        byte[] payload = new byte[128];
        BitWriter writer = BitWriter.Create(payload.Length);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 1, 1);
        writer.WriteBits(payload, 2, 2);
        writer.WriteBits(payload, 20, 8);
        writer.WriteBit(payload, 1);
        writer.WriteSignedBits(payload, -2, 7);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 3, 4);
        writer.WriteBits(payload, 4, 4);
        writer.WriteBit(payload, 1);
        int segment = 0;
        while (segment < Av1SyntaxConstants.MaxSegments)
        {
            int feature = 0;
            while (feature < Av1SyntaxConstants.SegmentFeatureCount)
            {
                bool enabled = segment == 0 && feature == 0;
                writer.WriteBit(payload, enabled ? 1 : 0);
                if (enabled)
                {
                    writer.WriteSignedBits(payload, -5, 9);
                }

                feature++;
            }

            segment++;
        }

        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 2, 2);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 1, 2);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 10, 6);
        writer.WriteBits(payload, 11, 6);
        writer.WriteBits(payload, 3, 3);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 1);
        int index = 0;
        while (index < 8)
        {
            bool update = index == 0;
            writer.WriteBit(payload, update ? 1 : 0);
            if (update)
            {
                writer.WriteSignedBits(payload, -3, 7);
            }

            index++;
        }

        writer.WriteBit(payload, 1);
        writer.WriteSignedBits(payload, 2, 7);
        writer.WriteBit(payload, 0);
        writer.WriteBits(payload, 1, 2);
        writer.WriteBits(payload, 1, 2);
        writer.WriteBits(payload, 5, 4);
        writer.WriteBits(payload, 3, 2);
        writer.WriteBits(payload, 7, 4);
        writer.WriteBits(payload, 2, 2);
        writer.WriteBits(payload, 2, 2);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 1);
        payload = FinishPayload(payload, writer);

        Av1FrameHeaderParseResult result = Av1FrameHeaderParser.ParsePayload(
            payload, sequence, new Av1FrameHeaderState(), 0, 0, true);
        ParsedAv1FrameHeader? header = result.Header;

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(((header ?? throw new InvalidOperationException("Invalid internal state.")).Tiles ?? throw new InvalidOperationException("Invalid internal state.")).TileColumns, Is.EqualTo(2));
            Assert.That((header.Tiles ?? throw new InvalidOperationException("Invalid internal state.")).TileRows, Is.EqualTo(1));
            Assert.That(header.Tiles.ContextUpdateTileId, Is.EqualTo(1));
            Assert.That(header.Tiles.TileSizeBytes, Is.EqualTo(3));
            Assert.That((header.Quantization ?? throw new InvalidOperationException("Invalid internal state.")).BaseQIndex, Is.EqualTo(20));
            Assert.That(header.Quantization.DeltaYDc, Is.EqualTo(-2));
            Assert.That(header.Quantization.UsesQuantizerMatrix, Is.True);
            Assert.That(header.Quantization.MatrixLevelY, Is.EqualTo(3));
            Assert.That(header.Quantization.MatrixLevelU, Is.EqualTo(4));
            Assert.That(header.Quantization.DeltaQPresent, Is.True);
            Assert.That(header.Quantization.DeltaQResolution, Is.EqualTo(2));
            Assert.That(header.Quantization.DeltaLoopFilterPresent, Is.True);
            Assert.That(header.Quantization.DeltaLoopFilterResolution, Is.EqualTo(1));
            Assert.That(header.Quantization.DeltaLoopFilterMulti, Is.True);
            Assert.That((header.Segmentation ?? throw new InvalidOperationException("Invalid internal state.")).Enabled, Is.True);
            Assert.That((header.Segmentation.FeatureEnabled ?? throw new InvalidOperationException("Invalid internal state."))[0][0], Is.True);
            Assert.That((header.Segmentation.FeatureData ?? throw new InvalidOperationException("Invalid internal state."))[0][0], Is.EqualTo(-5));
            Assert.That(header.CodedLossless, Is.False);
        });

        Assert.Multiple(() =>
        {
            Assert.That((((header ?? throw new InvalidOperationException("Invalid internal state.")).LoopFilter ?? throw new InvalidOperationException("Invalid internal state.")).Levels ?? throw new InvalidOperationException("Invalid internal state."))[0], Is.EqualTo(10));
            Assert.That(((header.LoopFilter ?? throw new InvalidOperationException("Invalid internal state.")).Levels ?? throw new InvalidOperationException("Invalid internal state."))[1], Is.EqualTo(11));
            Assert.That(header.LoopFilter.Sharpness, Is.EqualTo(3));
            Assert.That((header.LoopFilter.ReferenceDeltas ?? throw new InvalidOperationException("Invalid internal state."))[0], Is.EqualTo(-3));
            Assert.That((header.LoopFilter.ModeDeltas ?? throw new InvalidOperationException("Invalid internal state."))[0], Is.EqualTo(2));
            Assert.That((header.Cdef ?? throw new InvalidOperationException("Invalid internal state.")).Damping, Is.EqualTo(4));
            Assert.That(header.Cdef.Bits, Is.EqualTo(1));
            Assert.That(header.Cdef.YPrimaryStrengths, Is.EqualTo(new int[] { 5, 7 }));
            Assert.That(header.Cdef.YSecondaryStrengths, Is.EqualTo(new int[] { 4, 2 }));
            Assert.That((header.LoopRestoration ?? throw new InvalidOperationException("Invalid internal state.")).UsesRestoration, Is.True);
            Assert.That((header.LoopRestoration.Types ?? throw new InvalidOperationException("Invalid internal state."))[0], Is.EqualTo(Av1RestorationType.Wiener));
            Assert.That((header.LoopRestoration.UnitSizes ?? throw new InvalidOperationException("Invalid internal state."))[0], Is.EqualTo(128));
            Assert.That(header.TransformMode, Is.EqualTo(Av1TransformMode.Select));
            Assert.That(header.ReducedTransformSet, Is.True);
        });
    }

    [Test]
    public void ShowExistingFrameRetainsPresentationTimeAndRejectsBadInput()
    {
        Av1TimingInfo timing = new Av1TimingInfo(1, 30, false, 0);
        Av1DecoderModelInfo model = new Av1DecoderModelInfo(4, 1, 4, 5);
        ParsedAv1SequenceHeader sequence = CreateSequence(
            false, 64, 64, false, false, false, false, false, false, timing, model);
        Av1FrameHeaderState state = new Av1FrameHeaderState();
        state.SetReference(2, Av1FrameType.Key, 0, 0, 64, 64, 64, 64, 64);
        byte[] payload = new byte[4];
        BitWriter writer = BitWriter.Create(payload.Length);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 2, 3);
        writer.WriteBits(payload, 17, 5);
        payload = FinishPayload(payload, writer);

        Av1FrameHeaderParseResult result = Av1FrameHeaderParser.ParsePayload(
            payload, sequence, state, 0, 0, true);
        Av1FrameHeaderParseResult missing = Av1FrameHeaderParser.ParsePayload(
            payload, sequence, new Av1FrameHeaderState(), 0, 0, true);
        Av1FrameHeaderParseResult truncated = Av1FrameHeaderParser.ParsePayload(
            payload[..^1], sequence, state, 0, 0, true);
        byte[] badTrailing = (byte[])payload.Clone();
        badTrailing[^1] |= 1;
        Av1FrameHeaderParseResult trailing = Av1FrameHeaderParser.ParsePayload(
            badTrailing, sequence, state, 0, 0, true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((result.Header ?? throw new InvalidOperationException("Invalid internal state.")).ShowExistingFrame, Is.True);
            Assert.That(result.Header.FrameToShowMapIndex, Is.EqualTo(2));
            Assert.That(result.Header.FrameType, Is.EqualTo(Av1FrameType.Key));
            Assert.That(result.Header.FramePresentationTimePresent, Is.True);
            Assert.That(result.Header.FramePresentationTime, Is.EqualTo(17));
            Assert.That(result.Header.RefreshFrameFlags, Is.EqualTo(255));
            Assert.That(missing.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(truncated.Code, Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(trailing.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
        });
    }

    [Test]
    public void ShortSignalingSelectsReferencesAndLoadsReferenceDimensions()
    {
        ParsedAv1SequenceHeader sequence = CreateSequence(
            false, 128, 64, true, false, false, true, true, false);
        Av1FrameHeaderState state = new Av1FrameHeaderState();
        int[] hints = new int[] { 7, 6, 5, 4, 9, 11, 13, 10 };
        int index = 0;
        while (index < hints.Length)
        {
            state.SetReference(
                index,
                Av1FrameType.Inter,
                hints[index],
                index,
                index == 0 ? 120 : 128,
                index == 0 ? 96 : 128,
                index == 0 ? 40 : 64,
                index == 0 ? 118 : 128,
                index == 0 ? 38 : 64);
            index++;
        }

        byte[] payload = new byte[64];
        BitWriter writer = BitWriter.Create(payload.Length);
        writer.WriteBit(payload, 0);
        writer.WriteBits(payload, 1, 2);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 8, 4);
        writer.WriteBits(payload, 0, 3);
        writer.WriteBits(payload, 1, 8);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 0, 3);
        writer.WriteBits(payload, 3, 3);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 1);
        writer.WriteBits(payload, 1, 3);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 0);
        writer.WriteBits(payload, 0, 8);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 1);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 0);
        writer.WriteBit(payload, 0);
        index = 0;
        while (index < Av1SyntaxConstants.RefsPerFrame)
        {
            writer.WriteBit(payload, 0);
            index++;
        }

        payload = FinishPayload(payload, writer);

        Av1FrameHeaderParseResult result = Av1FrameHeaderParser.ParsePayload(
            payload, sequence, state, 0, 0, true);
        ParsedAv1FrameHeader? header = result.Header;

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((header ?? throw new InvalidOperationException("Invalid internal state.")).FrameReferencesShortSignaling, Is.True);
            Assert.That(header.ReferenceFrameIndexes,
                Is.EqualTo(new int[] { 0, 1, 2, 3, 4, 7, 6 }));
            Assert.That((header.Size ?? throw new InvalidOperationException("Invalid internal state.")).UpscaledWidth, Is.EqualTo(120));
            Assert.That(header.Size.FrameWidth, Is.EqualTo(96));
            Assert.That(header.Size.FrameHeight, Is.EqualTo(40));
            Assert.That(header.Size.RenderWidth, Is.EqualTo(118));
            Assert.That(header.Size.RenderHeight, Is.EqualTo(38));
            Assert.That(header.Size.UsesSuperres, Is.True);
            Assert.That(header.Size.SuperresDenominator, Is.EqualTo(10));
            Assert.That(header.SkipModeFrames, Is.EqualTo(new int[] { 1, 5 }));
            Assert.That(header.SkipModePresent, Is.False);
        });
    }

    static ParsedAv1SequenceHeader CreateSequence(
        bool reduced,
        uint width,
        uint height,
        bool superres,
        bool cdef,
        bool restoration,
        bool warpedMotion,
        bool orderHint,
        bool filmGrain)
    {
        return CreateSequence(
            reduced,
            width,
            height,
            superres,
            cdef,
            restoration,
            warpedMotion,
            orderHint,
            filmGrain,
            null,
            null);
    }

    static ParsedAv1SequenceHeader CreateSequence(
        bool reduced,
        uint width,
        uint height,
        bool superres,
        bool cdef,
        bool restoration,
        bool warpedMotion,
        bool orderHint,
        bool filmGrain,
        Av1TimingInfo? timing,
        Av1DecoderModelInfo? model)
    {
        Av1SequenceFeatures features = new Av1SequenceFeatures();
        features.FrameWidthBits = BitsRequired(width - 1);
        features.FrameHeightBits = BitsRequired(height - 1);
        features.ScreenContentToolsMode = reduced
            ? Av1SyntaxConstants.SelectScreenContentTools
            : 0;
        features.IntegerMotionVectorMode = Av1SyntaxConstants.SelectIntegerMotionVector;
        features.EnableSuperres = superres;
        features.EnableCdef = cdef;
        features.EnableRestoration = restoration;
        features.EnableWarpedMotion = warpedMotion;
        features.EnableOrderHint = orderHint;
        features.OrderHintBits = orderHint ? 4 : 0;
        return new ParsedAv1SequenceHeader(
            0,
            reduced,
            reduced,
            timing,
            model,
            false,
            new Av1OperatingPoint[] { new Av1OperatingPoint(0, 4, 0) },
            width,
            height,
            features,
            false,
            false,
            true,
            true,
            true,
            0,
            2,
            2,
            2,
            true,
            false,
            false,
            filmGrain,
            System.Array.Empty<byte>());
    }

    static int BitsRequired(uint value)
    {
        int bits = 1;
        while ((1UL << bits) <= value)
        {
            bits++;
        }

        return bits;
    }

    static byte[] FinishPayload(byte[] payload, BitWriter writer)
    {
        writer.WriteTrailingBits(payload);
        Assert.That(writer.Code, Is.EqualTo(AvifResultCode.Success));
        return payload[..(int)(writer.BitPosition / 8)];
    }
}
