namespace avif.Tests;

public class Av1BlockModeInfoTests
{
    [Test]
    public void SegmentationFeaturesControlSkipReferenceQuantizerAndLoopFilter()
    {
        ParsedAv1FrameHeader header = CreateHeader(32, 32);
        EnableFeature(header, 2, Av1SegmentFeature.AlternateQuantizer, -20);
        EnableFeature(header, 2, Av1SegmentFeature.AlternateLoopFilterYVertical, 3);
        EnableFeature(header, 2, Av1SegmentFeature.ReferenceFrame, (int)Av1ReferenceFrame.Golden);
        EnableFeature(header, 2, Av1SegmentFeature.Skip, 0);
        Av1BlockModeDecoder decoder = CreateDecoder(header, null);
        Av1BlockModeSyntax syntax = new Av1BlockModeSyntax();
        syntax.SegmentId = 2;
        syntax.Skip = false;
        syntax.IsInter = false;
        syntax.TransformDepth = 1;

        Av1BlockModeDecodeResult result = decoder.Decode(
            CreateBlock(0, 0, Av1BlockSize.Block8x8, 0),
            syntax);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((result.Mode ?? throw new InvalidOperationException("Invalid internal state.")).SegmentId, Is.EqualTo(2));
            Assert.That(result.Mode.Skip, Is.True);
            Assert.That(result.Mode.SkipMode, Is.False);
            Assert.That(result.Mode.IsInter, Is.True);
            Assert.That(result.Mode.ReferenceFrames[0], Is.EqualTo(Av1ReferenceFrame.Golden));
            Assert.That(result.Mode.ReferenceFrames[1], Is.EqualTo(Av1ReferenceFrame.None));
            Assert.That(result.Mode.CurrentQIndex, Is.EqualTo(100));
            Assert.That(result.Mode.EffectiveQIndex, Is.EqualTo(80));
            Assert.That(result.Mode.DeltaLoopFilters[0], Is.EqualTo(3));
            Assert.That(result.Mode.TransformSize, Is.EqualTo(Av1TransformSize.Transform8x8));
        });
    }

    [Test]
    public void SkipModeRequiresEligibleGeometryAndUsesFrameReferences()
    {
        ParsedAv1FrameHeader header = CreateHeader(32, 32);
        header.SkipModePresent = true;
        header.SkipModeFrames[0] = (int)Av1ReferenceFrame.Last;
        header.SkipModeFrames[1] = (int)Av1ReferenceFrame.Golden;
        Av1BlockModeDecoder decoder = CreateDecoder(header, null);
        Av1BlockModeSyntax syntax = new Av1BlockModeSyntax();
        syntax.SkipMode = true;

        Av1BlockModeDecodeResult small = decoder.Decode(
            CreateBlock(0, 0, Av1BlockSize.Block4x4, 0),
            syntax);
        Av1BlockModeDecodeResult eligible = decoder.Decode(
            CreateBlock(0, 1, Av1BlockSize.Block8x8, 0),
            syntax);

        Assert.Multiple(() =>
        {
            Assert.That(small.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((small.Mode ?? throw new InvalidOperationException("Invalid internal state.")).SkipMode, Is.False);
            Assert.That(small.Mode.Skip, Is.False);
            Assert.That(eligible.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((eligible.Mode ?? throw new InvalidOperationException("Invalid internal state.")).SkipMode, Is.True);
            Assert.That(eligible.Mode.Skip, Is.True);
            Assert.That(eligible.Mode.IsInter, Is.True);
            Assert.That(eligible.Mode.ReferenceFrames, Is.EqualTo(new Av1ReferenceFrame[]
            {
                Av1ReferenceFrame.Last,
                Av1ReferenceFrame.Golden
            }));
        });
    }

    [Test]
    public void DeltaStateAppliesOncePerSuperblockAndClipsNormativeRanges()
    {
        ParsedAv1FrameHeader header = CreateHeader(128, 64);
        (header.Quantization ?? throw new InvalidOperationException("Invalid internal state.")).DeltaQPresent = true;
        header.Quantization.DeltaLoopFilterPresent = true;
        header.Quantization.DeltaLoopFilterMulti = true;
        Av1BlockModeDecoder decoder = CreateDecoder(header, null);
        Av1BlockModeSyntax increase = CreateDeltaSyntax(200, new int[] { 70, -80, 5, -5 });
        Av1BlockModeSyntax ignored = CreateDeltaSyntax(-250, new int[] { -60, 60, -60, 60 });
        Av1BlockModeSyntax decrease = CreateDeltaSyntax(-300, new int[] { -200, 200, -10, 10 });

        Av1BlockModeDecodeResult first = decoder.Decode(
            CreateBlock(0, 0, Av1BlockSize.Block8x8, 0),
            increase);
        Av1BlockModeDecodeResult second = decoder.Decode(
            CreateBlock(0, 2, Av1BlockSize.Block8x8, 0),
            ignored);
        Av1BlockModeDecodeResult third = decoder.Decode(
            CreateBlock(0, 16, Av1BlockSize.Block8x8, 1),
            decrease);

        Assert.Multiple(() =>
        {
            Assert.That((first.Mode ?? throw new InvalidOperationException("Invalid internal state.")).CurrentQIndex, Is.EqualTo(255));
            Assert.That(first.Mode.DeltaLoopFilters, Is.EqualTo(new int[] { 63, -63, 5, -5 }));
            Assert.That((second.Mode ?? throw new InvalidOperationException("Invalid internal state.")).CurrentQIndex, Is.EqualTo(255));
            Assert.That(second.Mode.DeltaLoopFilters, Is.EqualTo(first.Mode.DeltaLoopFilters));
            Assert.That((third.Mode ?? throw new InvalidOperationException("Invalid internal state.")).CurrentQIndex, Is.EqualTo(1));
            Assert.That(third.Mode.DeltaLoopFilters, Is.EqualTo(new int[] { -63, 63, -5, 5 }));
        });
    }

    [Test]
    public void WholeSkippedSuperblockDefersDeltaUntilAFollowingBlock()
    {
        ParsedAv1FrameHeader header = CreateHeader(64, 64);
        (header.Quantization ?? throw new InvalidOperationException("Invalid internal state.")).DeltaQPresent = true;
        Av1BlockModeDecoder decoder = CreateDecoder(header, null);
        Av1BlockModeSyntax skipped = CreateDeltaSyntax(25, new int[4]);
        skipped.Skip = true;
        skipped.DeltaLoopFilterPresent = false;
        Av1BlockModeSyntax following = CreateDeltaSyntax(7, new int[4]);
        following.DeltaLoopFilterPresent = false;

        Av1BlockModeDecodeResult whole = decoder.Decode(
            CreateBlock(0, 0, Av1BlockSize.Block64x64, 0),
            skipped);

        Assert.Multiple(() =>
        {
            Assert.That(whole.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((whole.Mode ?? throw new InvalidOperationException("Invalid internal state.")).CurrentQIndex, Is.EqualTo(100));
            Assert.That(decoder.ReadDeltas, Is.True);
        });

        decoder = CreateDecoder(header, null);
        Av1BlockModeDecodeResult small = decoder.Decode(
            CreateBlock(0, 0, Av1BlockSize.Block8x8, 0),
            following);
        Assert.That((small.Mode ?? throw new InvalidOperationException("Invalid internal state.")).CurrentQIndex, Is.EqualTo(107));
    }

    [Test]
    public void NeighborContextsUseStoredAboveLeftAndUpperLeftModes()
    {
        ParsedAv1FrameHeader header = CreateHeader(16, 16);
        Av1BlockModeDecoder decoder = CreateDecoder(header, null);
        DecodeMode(decoder, 0, 0, 1, false, false, false);
        DecodeMode(decoder, 0, 1, 1, true, false, true);
        DecodeMode(decoder, 1, 0, 2, false, true, false);

        Av1BlockNeighborContexts contexts = decoder.GetNeighborContexts(
            CreateBlock(1, 1, Av1BlockSize.Block4x4, 0));

        Assert.Multiple(() =>
        {
            Assert.That(contexts.AboveAvailable, Is.True);
            Assert.That(contexts.LeftAvailable, Is.True);
            Assert.That((contexts.Above ?? throw new InvalidOperationException("Invalid internal state.")).MiRow, Is.EqualTo(0));
            Assert.That((contexts.Left ?? throw new InvalidOperationException("Invalid internal state.")).MiColumn, Is.EqualTo(0));
            Assert.That(contexts.SkipContext, Is.EqualTo(1));
            Assert.That(contexts.SkipModeContext, Is.EqualTo(0));
            Assert.That(contexts.SegmentPredictedContext, Is.EqualTo(1));
            Assert.That(contexts.SegmentIdContext, Is.EqualTo(1));
            Assert.That(contexts.IsInterContext, Is.EqualTo(1));
            Assert.That(contexts.PredictedSegmentId, Is.EqualTo(1));
            Assert.That(contexts.TransformSizeContext, Is.EqualTo(0));
        });
    }

    [Test]
    public void PreviousSegmentationMapUsesMinimumCoveredIdWhenMapIsNotUpdated()
    {
        ParsedAv1FrameHeader header = CreateHeader(16, 16);
        (header.Segmentation ?? throw new InvalidOperationException("Invalid internal state.")).UpdateMap = false;
        int[] previous = new int[(header.Size ?? throw new InvalidOperationException("Invalid internal state.")).MiRows * header.Size.MiColumns];
        System.Array.Fill(previous, 6);
        previous[1] = 4;
        previous[header.Size.MiColumns] = 2;
        Av1BlockModeDecoder decoder = CreateDecoder(header, previous);
        Av1BlockModeSyntax syntax = new Av1BlockModeSyntax();
        syntax.SegmentId = 7;

        Av1BlockModeDecodeResult result = decoder.Decode(
            CreateBlock(0, 0, Av1BlockSize.Block8x8, 0),
            syntax);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(decoder.GetPreviousSegmentId(
                CreateBlock(0, 0, Av1BlockSize.Block8x8, 0)), Is.EqualTo(2));
            Assert.That((result.Mode ?? throw new InvalidOperationException("Invalid internal state.")).SegmentId, Is.EqualTo(2));
            Assert.That(Av1BlockModeDecoder.NegDeinterleave(1, 2, 8), Is.EqualTo(1));
            Assert.That(Av1BlockModeDecoder.NegDeinterleave(2, 2, 8), Is.EqualTo(3));
            Assert.That(Av1BlockModeDecoder.NegDeinterleave(5, 5, 8), Is.EqualTo(2));
        });
    }

    [Test]
    public void TransformModeSelectSplitsByDepthAndRejectsExcessDepth()
    {
        ParsedAv1FrameHeader header = CreateHeader(64, 64);
        Av1BlockModeDecoder decoder = CreateDecoder(header, null);
        Av1BlockModeSyntax split = new Av1BlockModeSyntax();
        split.TransformDepth = 1;
        Av1BlockModeSyntax invalid = new Av1BlockModeSyntax();
        invalid.TransformDepth = 20;

        Av1BlockModeDecodeResult selected = decoder.Decode(
            CreateBlock(0, 0, Av1BlockSize.Block32x16, 0),
            split);
        Av1BlockModeDecodeResult rejected = decoder.Decode(
            CreateBlock(0, 8, Av1BlockSize.Block32x16, 0),
            invalid);

        Assert.Multiple(() =>
        {
            Assert.That(selected.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((selected.Mode ?? throw new InvalidOperationException("Invalid internal state.")).TransformSize, Is.EqualTo(Av1TransformSize.Transform16x16));
            Assert.That(rejected.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(rejected.Mode, Is.Null);
        });
    }

    [Test]
    public void GridRejectsOverlapsHonorsDecodedByteLimitAndCloses()
    {
        ParsedAv1FrameHeader header = CreateHeader(16, 16);
        Av1BlockModeDecoder decoder = CreateDecoder(header, null);
        Av1PartitionEvent block = CreateBlock(0, 0, Av1BlockSize.Block8x8, 0);
        Av1BlockModeDecodeResult first = decoder.Decode(block, new Av1BlockModeSyntax());
        Av1BlockModeDecodeResult overlap = decoder.Decode(block, new Av1BlockModeSyntax());
        AvifResultCode close = decoder.Close();

        AvifSecurityLimits limits = new AvifSecurityLimits();
        limits.MaxDecodedBytes = 1;
        Av1BlockModeDecoder limited = Av1BlockModeDecoder.Create(header, 0, limits, null);

        Assert.Multiple(() =>
        {
            Assert.That(first.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(overlap.Code, Is.EqualTo(AvifResultCode.InvalidState));
            Assert.That(close, Is.EqualTo(AvifResultCode.Success));
            Assert.That((decoder.Grid ?? throw new InvalidOperationException("Invalid internal state.")).Get(0, 0), Is.Null);
            Assert.That(limited.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
        });
    }

    [Test]
    public void SymbolPathReadsSkipInterAndIntraModesFromTileCdfs()
    {
        ParsedAv1FrameHeader header = CreateHeader(16, 16);
        (header.Segmentation ?? throw new InvalidOperationException("Invalid internal state.")).Enabled = false;
        header.TransformMode = Av1TransformMode.Largest;
        Av1FrameCdfContext? frameCdf = Av1FrameCdfContext.CreateDefault(100);
        Av1TileCdfContext tileCdf = (frameCdf ?? throw new InvalidOperationException("Invalid internal state.")).CreateTileContext(1, 0);
        ForceFirstSymbol(tileCdf.GetCdf("Default_Skip_Cdf", 0));
        ForceFirstSymbol(tileCdf.GetCdf("Default_Is_Inter_Cdf", 0));
        ForceFirstSymbol(tileCdf.GetCdf(
            "Default_Y_Mode_Cdf",
            Av1ConversionTables.GetSizeGroup(Av1BlockSize.Block8x8)));
        ForceFirstSymbol(tileCdf.GetCdf("Default_Uv_Mode_Cfl_Not_Allowed_Cdf", 0));
        Av1SymbolDecoder symbols = new Av1SymbolDecoder(
            new byte[] { 0x00, 0x00 },
            0,
            2,
            true);
        Av1BlockModeDecoder decoder = CreateDecoder(header, null);

        Av1BlockModeDecodeResult result = decoder.DecodeSymbols(
            CreateBlock(0, 0, Av1BlockSize.Block8x8, 0),
            symbols,
            tileCdf);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((result.Mode ?? throw new InvalidOperationException("Invalid internal state.")).Skip, Is.False);
            Assert.That(result.Mode.IsInter, Is.False);
            Assert.That(result.Mode.YMode, Is.EqualTo(Av1PredictionMode.Dc));
            Assert.That(result.Mode.UvMode, Is.EqualTo(Av1PredictionMode.Dc));
            Assert.That(result.Mode.ReferenceFrames[0], Is.EqualTo(Av1ReferenceFrame.Intra));
            Assert.That(symbols.Code, Is.EqualTo(AvifResultCode.Success));
        });
    }

    static void DecodeMode(
        Av1BlockModeDecoder decoder,
        int row,
        int column,
        int segmentId,
        bool skip,
        bool inter,
        bool predicted)
    {
        Av1BlockModeSyntax syntax = new Av1BlockModeSyntax();
        syntax.SegmentId = segmentId;
        syntax.Skip = skip;
        syntax.IsInter = inter;
        syntax.SegmentIdPredicted = predicted;
        Av1BlockModeDecodeResult result = decoder.Decode(
            CreateBlock(row, column, Av1BlockSize.Block4x4, 0),
            syntax);
        Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
    }

    static Av1BlockModeSyntax CreateDeltaSyntax(int quantizer, int[] loopFilters)
    {
        Av1BlockModeSyntax syntax = new Av1BlockModeSyntax();
        syntax.DeltaQuantizerPresent = true;
        syntax.DeltaQuantizer = quantizer;
        syntax.DeltaLoopFilterPresent = true;
        syntax.DeltaLoopFilters = loopFilters;
        return syntax;
    }

    static Av1BlockModeDecoder CreateDecoder(
        ParsedAv1FrameHeader header,
        int[]? previousSegmentIds)
    {
        return Av1BlockModeDecoder.Create(
            header,
            0,
            new AvifSecurityLimits(),
            previousSegmentIds);
    }

    static Av1PartitionEvent CreateBlock(
        int row,
        int column,
        Av1BlockSize size,
        int superblockIndex)
    {
        Av1PartitionEvent block = new Av1PartitionEvent();
        block.Kind = Av1PartitionEventKind.Block;
        block.MiRow = row;
        block.MiColumn = column;
        block.BlockSize = size;
        block.VisibleMiWidth = Av1ConversionTables.GetBlockMiWidth(size);
        block.VisibleMiHeight = Av1ConversionTables.GetBlockMiHeight(size);
        block.SuperblockIndex = superblockIndex;
        return block;
    }

    static ParsedAv1FrameHeader CreateHeader(int width, int height)
    {
        ParsedAv1FrameHeader header = new ParsedAv1FrameHeader();
        header.FrameType = Av1FrameType.Inter;
        header.FrameIsIntra = false;
        header.TransformMode = Av1TransformMode.Select;
        header.Size = new Av1FrameSize();
        header.Size.FrameWidth = width;
        header.Size.FrameHeight = height;
        header.Size.MiColumns = 2 * ((width + 7) >> 3);
        header.Size.MiRows = 2 * ((height + 7) >> 3);
        header.Tiles = new Av1TileInfo();
        header.Tiles.TileColumns = 1;
        header.Tiles.TileRows = 1;
        header.Tiles.MiColumnStarts = new int[] { 0, header.Size.MiColumns };
        header.Tiles.MiRowStarts = new int[] { 0, header.Size.MiRows };
        header.Quantization = new Av1QuantizationParameters();
        header.Quantization.BaseQIndex = 100;
        header.Segmentation = new Av1SegmentationParameters();
        header.Segmentation.Enabled = true;
        header.Segmentation.UpdateMap = true;
        header.Segmentation.LastActiveSegmentId = 7;
        header.Segmentation.FeatureEnabled = new bool[Av1SyntaxConstants.MaxSegments][];
        header.Segmentation.FeatureData = new int[Av1SyntaxConstants.MaxSegments][];
        int index = 0;
        while (index < Av1SyntaxConstants.MaxSegments)
        {
            header.Segmentation.FeatureEnabled[index] = new bool[8];
            header.Segmentation.FeatureData[index] = new int[8];
            index++;
        }

        return header;
    }

    static void EnableFeature(
        ParsedAv1FrameHeader header,
        int segmentId,
        Av1SegmentFeature feature,
        int data)
    {
        ((header.Segmentation ?? throw new InvalidOperationException("Invalid internal state.")).FeatureEnabled ?? throw new InvalidOperationException("Invalid internal state."))[segmentId][(int)feature] = true;
        (header.Segmentation.FeatureData ?? throw new InvalidOperationException("Invalid internal state."))[segmentId][(int)feature] = data;
    }

    static void ForceFirstSymbol(Av1Cdf? cdf)
    {
        int index = 0;
        while (index < (cdf ?? throw new InvalidOperationException("Invalid internal state.")).SymbolCount - 1)
        {
            cdf.SetValue(index, 32767);
            index++;
        }

        cdf.SetValue(cdf.SymbolCount - 1, 32768);
    }
}
