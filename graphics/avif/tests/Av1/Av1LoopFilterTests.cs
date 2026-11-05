namespace avif.Tests;

public class Av1LoopFilterTests
{
    [Test]
    public void LevelSelectionCombinesBlockSegmentReferenceAndModeDeltas()
    {
        Av1LoopFilterRequest request = CreateRequest(8, 4, 8);
        ((request.Header ?? throw new InvalidOperationException("Invalid internal state.")).LoopFilter ?? throw new InvalidOperationException("Invalid internal state.")).Levels = new int[] { 20,21,22,23 };
        (request.Header.LoopFilter ?? throw new InvalidOperationException("Invalid internal state.")).DeltaEnabled = true;
        (request.Header.LoopFilter.ReferenceDeltas ?? throw new InvalidOperationException("Invalid internal state."))[(int)Av1ReferenceFrame.Last] = -2;
        (request.Header.LoopFilter.ModeDeltas ?? throw new InvalidOperationException("Invalid internal state."))[1] = 3;
        (request.Header.Quantization ?? throw new InvalidOperationException("Invalid internal state.")).DeltaLoopFilterMulti = true;
        EnableSegmentFeature(
            request.Header, 2, Av1SegmentFeature.AlternateLoopFilterYVertical, 3);
        EnableSegmentFeature(
            request.Header, 2, Av1SegmentFeature.AlternateLoopFilterU, 4);
        Av1ModeInfo mode = request.Modes[0];
        mode.SegmentId = 2;
        mode.IsInter = true;
        mode.ReferenceFrames[0] = Av1ReferenceFrame.Last;
        mode.YMode = Av1PredictionMode.NewMv;
        mode.RawDeltaLoopFilters = new int[] { 5,-5,7,-7 };

        int lumaVertical = Av1LoopFilter.GetLevel(request, mode, 0, 0);
        int chromaU = Av1LoopFilter.GetLevel(request, mode, 1, 0);

        Assert.Multiple(() =>
        {
            Assert.That(lumaVertical, Is.EqualTo(29));
            Assert.That(chromaU, Is.EqualTo(35));
        });
        Close(request);
    }

    [Test]
    public void NarrowFilterMatchesTheFourTapProcess()
    {
        Av1LoopFilterRequest request = CreateRequest(8, 4, 8);
        (((request.Header ?? throw new InvalidOperationException("Invalid internal state.")).LoopFilter ?? throw new InvalidOperationException("Invalid internal state.")).Levels ?? throw new InvalidOperationException("Invalid internal state."))[0] = 20;
        FillVerticalStep(request.Planes[0], 4, 100, 104);

        AvifResultCode code = Av1LoopFilter.Filter(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadRow(request.Planes[0], 0),
                Is.EqualTo(new int[] { 100,100,101,101,102,103,104,104 }));
            Assert.That(ReadRow(request.Planes[0], 3),
                Is.EqualTo(new int[] { 100,100,101,101,102,103,104,104 }));
        });
        Close(request);
    }

    [Test]
    public void HighEdgeVarianceOnlyChangesTheSamplesNearestTheEdge()
    {
        Av1LoopFilterRequest request = CreateRequest(8, 4, 8);
        (((request.Header ?? throw new InvalidOperationException("Invalid internal state.")).LoopFilter ?? throw new InvalidOperationException("Invalid internal state.")).Levels ?? throw new InvalidOperationException("Invalid internal state."))[0] = 40;
        int row = 0;
        while (row < 4)
        {
            WriteSample(request.Planes[0], 2, row, 95);
            WriteSample(request.Planes[0], 3, row, 100);
            WriteSample(request.Planes[0], 4, row, 108);
            WriteSample(request.Planes[0], 5, row, 113);
            row++;
        }

        AvifResultCode code = Av1LoopFilter.Filter(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadSample(request.Planes[0], 2, 0), Is.EqualTo(95));
            Assert.That(ReadSample(request.Planes[0], 3, 0), Is.EqualTo(101));
            Assert.That(ReadSample(request.Planes[0], 4, 0), Is.EqualTo(107));
            Assert.That(ReadSample(request.Planes[0], 5, 0), Is.EqualTo(113));
        });
        Close(request);
    }

    [Test]
    public void EightTapWideFilterUsesTheFlatRegionKernel()
    {
        Av1LoopFilterRequest request = CreateRequest(16, 4, 8);
        request.Modes = CreateModes(
            request.MiRows, request.MiColumns,
            Av1BlockSize.Block8x4, Av1TransformSize.Transform8x4);
        (((request.Header ?? throw new InvalidOperationException("Invalid internal state.")).LoopFilter ?? throw new InvalidOperationException("Invalid internal state.")).Levels ?? throw new InvalidOperationException("Invalid internal state."))[0] = 40;
        FillVerticalStep(request.Planes[0], 8, 100, 104);

        AvifResultCode code = Av1LoopFilter.Filter(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadRange(request.Planes[0], 0, 5, 6),
                Is.EqualTo(new int[] { 101,101,102,103,103,104 }));
        });
        Close(request);
    }

    [Test]
    public void SixteenTapWideFilterSmoothsSevenSamplesOnEachSide()
    {
        Av1LoopFilterRequest request = CreateRequest(32, 4, 8);
        request.Modes = CreateModes(
            request.MiRows, request.MiColumns,
            Av1BlockSize.Block16x4, Av1TransformSize.Transform16x4);
        (((request.Header ?? throw new InvalidOperationException("Invalid internal state.")).LoopFilter ?? throw new InvalidOperationException("Invalid internal state.")).Levels ?? throw new InvalidOperationException("Invalid internal state."))[0] = 40;
        FillVerticalStep(request.Planes[0], 16, 100, 104);

        AvifResultCode code = Av1LoopFilter.Filter(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadRange(request.Planes[0], 0, 10, 12),
                Is.EqualTo(new int[]
                {
                    100,101,101,101,101,102,102,103,103,103,104,104
                }));
        });
        Close(request);
    }

    [Test]
    public void HorizontalEdgesUseTheIndependentLumaLevel()
    {
        Av1LoopFilterRequest request = CreateRequest(4, 8, 8);
        (((request.Header ?? throw new InvalidOperationException("Invalid internal state.")).LoopFilter ?? throw new InvalidOperationException("Invalid internal state.")).Levels ?? throw new InvalidOperationException("Invalid internal state."))[1] = 20;
        FillHorizontalStep(request.Planes[0], 4, 100, 104);

        AvifResultCode code = Av1LoopFilter.Filter(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadColumn(request.Planes[0], 0),
                Is.EqualTo(new int[] { 100,100,101,101,102,103,104,104 }));
        });
        Close(request);
    }

    [Test]
    public void SkippedInterBlocksIgnoreInternalTransformEdges()
    {
        Av1LoopFilterRequest skipped = CreateRequest(8, 4, 8);
        skipped.Modes = CreateModes(
            skipped.MiRows, skipped.MiColumns,
            Av1BlockSize.Block8x4, Av1TransformSize.Transform4x4);
        (((skipped.Header ?? throw new InvalidOperationException("Invalid internal state.")).LoopFilter ?? throw new InvalidOperationException("Invalid internal state.")).Levels ?? throw new InvalidOperationException("Invalid internal state."))[0] = 20;
        FillVerticalStep(skipped.Planes[0], 4, 100, 104);
        int index = 0;
        while (index < skipped.Modes.Length)
        {
            skipped.Modes[index].Skip = true;
            skipped.Modes[index].IsInter = true;
            skipped.Modes[index].ReferenceFrames[0] = Av1ReferenceFrame.Last;
            index++;
        }

        AvifResultCode code = Av1LoopFilter.Filter(skipped);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadRow(skipped.Planes[0], 0),
                Is.EqualTo(new int[] { 100,100,100,100,104,104,104,104 }));
        });
        Close(skipped);
    }

    [Test]
    public void ZeroCurrentLevelFallsBackToThePreviousBlockLevel()
    {
        Av1LoopFilterRequest request = CreateRequest(8, 4, 8);
        (((request.Header ?? throw new InvalidOperationException("Invalid internal state.")).LoopFilter ?? throw new InvalidOperationException("Invalid internal state.")).Levels ?? throw new InvalidOperationException("Invalid internal state."))[0] = 20;
        request.Modes[1].RawDeltaLoopFilters[0] = -20;
        FillVerticalStep(request.Planes[0], 4, 100, 104);

        AvifResultCode code = Av1LoopFilter.Filter(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadSample(request.Planes[0], 3, 0), Is.EqualTo(101));
            Assert.That(ReadSample(request.Planes[0], 4, 0), Is.EqualTo(102));
        });
        Close(request);
    }

    [Test]
    public void ChromaFilteringHonorsSubsamplingAndPlaneLevels()
    {
        Av1LoopFilterRequest request = CreateColorRequest(16, 8, 8, true, true);
        request.Modes = CreateModes(
            request.MiRows, request.MiColumns,
            Av1BlockSize.Block8x8, Av1TransformSize.Transform8x8);
        (((request.Header ?? throw new InvalidOperationException("Invalid internal state.")).LoopFilter ?? throw new InvalidOperationException("Invalid internal state.")).Levels ?? throw new InvalidOperationException("Invalid internal state."))[2] = 20;
        FillPlane(request.Planes[0], 50);
        FillVerticalStep(request.Planes[1], 4, 100, 104);
        FillVerticalStep(request.Planes[2], 4, 110, 114);

        AvifResultCode code = Av1LoopFilter.Filter(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadRow(request.Planes[0], 0), Is.All.EqualTo(50));
            Assert.That(ReadRow(request.Planes[1], 0),
                Is.EqualTo(new int[] { 100,100,101,101,102,103,104,104 }));
            Assert.That(ReadRow(request.Planes[2], 0),
                Is.EqualTo(new int[] { 110,110,110,110,114,114,114,114 }));
        });
        Close(request);
    }

    [TestCase(10, 400, 416)]
    [TestCase(12, 1600, 1664)]
    public void ThresholdsAndFilteringScaleForHighBitDepth(
        int bitDepth,
        int lower,
        int upper)
    {
        Av1LoopFilterRequest request = CreateRequest(8, 4, bitDepth);
        (((request.Header ?? throw new InvalidOperationException("Invalid internal state.")).LoopFilter ?? throw new InvalidOperationException("Invalid internal state.")).Levels ?? throw new InvalidOperationException("Invalid internal state."))[0] = 20;
        FillVerticalStep(request.Planes[0], 4, lower, upper);

        AvifResultCode code = Av1LoopFilter.Filter(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadSample(request.Planes[0], 3, 0), Is.GreaterThan(lower));
            Assert.That(ReadSample(request.Planes[0], 4, 0), Is.LessThan(upper));
            Assert.That(ReadSample(request.Planes[0], 3, 0),
                Is.InRange(0, (1 << bitDepth) - 1));
        });
        Close(request);
    }

    [Test]
    public void InvalidRequestLeavesEveryPlaneUntouched()
    {
        Av1LoopFilterRequest request = CreateRequest(8, 4, 8);
        (((request.Header ?? throw new InvalidOperationException("Invalid internal state.")).LoopFilter ?? throw new InvalidOperationException("Invalid internal state.")).Levels ?? throw new InvalidOperationException("Invalid internal state."))[0] = 20;
        FillVerticalStep(request.Planes[0], 4, 100, 104);
        request.Modes = new Av1ModeInfo[1];
        int[] before = ReadRow(request.Planes[0], 0);

        AvifResultCode code = Av1LoopFilter.Filter(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(ReadRow(request.Planes[0], 0), Is.EqualTo(before));
        });
        Close(request);
    }

    [Test]
    public void BlockModeDecoderPreservesRawAndSegmentAdjustedDeltas()
    {
        ParsedAv1FrameHeader header = CreateHeader(8, 4);
        (header.Segmentation ?? throw new InvalidOperationException("Invalid internal state.")).Enabled = true;
        header.Segmentation.UpdateMap = true;
        EnableSegmentFeature(
            header, 1, Av1SegmentFeature.AlternateLoopFilterYVertical, 6);
        Av1BlockModeDecoder decoder = Av1BlockModeDecoder.Create(
            header, 0, new AvifSecurityLimits(), null, 1);
        Av1PartitionEvent block = new Av1PartitionEvent
        {
            Kind = Av1PartitionEventKind.Block,
            Partition = Av1PartitionType.None,
            BlockSize = Av1BlockSize.Block4x4,
            MiRow = 0,
            MiColumn = 0,
            VisibleMiWidth = 1,
            VisibleMiHeight = 1,
            SuperblockIndex = 0
        };
        Av1BlockModeSyntax syntax = new Av1BlockModeSyntax();
        syntax.SegmentId = 1;

        Av1BlockModeDecodeResult result = decoder.Decode(block, syntax);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((result.Mode ?? throw new InvalidOperationException("Invalid internal state.")).RawDeltaLoopFilters[0], Is.Zero);
            Assert.That(result.Mode.DeltaLoopFilters[0], Is.EqualTo(6));
        });
        (decoder.Grid ?? throw new InvalidOperationException("Invalid internal state.")).Close();
    }

    static Av1LoopFilterRequest CreateRequest(int width, int height, int bitDepth)
    {
        Av1LoopFilterRequest request = new Av1LoopFilterRequest();
        request.Header = CreateHeader(width, height);
        request.MiRows = (height + 3) >> 2;
        request.MiColumns = (width + 3) >> 2;
        request.BitDepth = bitDepth;
        request.Planes = new PlaneBuffer[] { CreatePlane(width, height, bitDepth) };
        request.Modes = CreateModes(
            request.MiRows, request.MiColumns,
            Av1BlockSize.Block4x4, Av1TransformSize.Transform4x4);
        return request;
    }

    static Av1LoopFilterRequest CreateColorRequest(
        int width,
        int height,
        int bitDepth,
        bool subsamplingX,
        bool subsamplingY)
    {
        Av1LoopFilterRequest request = CreateRequest(width, height, bitDepth);
        request.SubsamplingX = subsamplingX;
        request.SubsamplingY = subsamplingY;
        int chromaWidth = (width + (subsamplingX ? 1 : 0))
            >> (subsamplingX ? 1 : 0);
        int chromaHeight = (height + (subsamplingY ? 1 : 0))
            >> (subsamplingY ? 1 : 0);
        request.Planes = new PlaneBuffer[]
        {
            request.Planes[0],
            CreatePlane(chromaWidth, chromaHeight, bitDepth),
            CreatePlane(chromaWidth, chromaHeight, bitDepth)
        };
        return request;
    }

    static ParsedAv1FrameHeader CreateHeader(int width, int height)
    {
        ParsedAv1FrameHeader header = new ParsedAv1FrameHeader();
        header.Size = new Av1FrameSize();
        header.Size.FrameWidth = width;
        header.Size.FrameHeight = height;
        header.Size.MiColumns = (width + 3) >> 2;
        header.Size.MiRows = (height + 3) >> 2;
        header.Quantization = new Av1QuantizationParameters();
        header.LoopFilter = new Av1LoopFilterParameters();
        header.LoopFilter.Levels = new int[4];
        header.LoopFilter.ReferenceDeltas = new int[8];
        header.LoopFilter.ModeDeltas = new int[2];
        header.Segmentation = new Av1SegmentationParameters();
        header.Segmentation.FeatureEnabled = new bool[8][];
        header.Segmentation.FeatureData = new int[8][];
        int segment = 0;
        while (segment < 8)
        {
            header.Segmentation.FeatureEnabled[segment] = new bool[8];
            header.Segmentation.FeatureData[segment] = new int[8];
            segment++;
        }
        header.Tiles = new Av1TileInfo();
        header.Tiles.TileColumns = 1;
        header.Tiles.TileRows = 1;
        header.Tiles.MiColumnStarts = new int[] { 0, header.Size.MiColumns };
        header.Tiles.MiRowStarts = new int[] { 0, header.Size.MiRows };
        return header;
    }

    static Av1ModeInfo[] CreateModes(
        int miRows,
        int miColumns,
        Av1BlockSize blockSize,
        Av1TransformSize transformSize)
    {
        Av1ModeInfo[] modes = new Av1ModeInfo[miRows * miColumns];
        int index = 0;
        while (index < modes.Length)
        {
            Av1ModeInfo mode = new Av1ModeInfo();
            mode.BlockSize = blockSize;
            mode.TransformSize = transformSize;
            mode.ReferenceFrames[0] = Av1ReferenceFrame.Intra;
            modes[index] = mode;
            index++;
        }
        return modes;
    }

    static void EnableSegmentFeature(
        ParsedAv1FrameHeader header,
        int segment,
        Av1SegmentFeature feature,
        int value)
    {
        (header.Segmentation ?? throw new InvalidOperationException("Invalid internal state.")).Enabled = true;
        if (segment > header.Segmentation.LastActiveSegmentId)
        {
            header.Segmentation.LastActiveSegmentId = segment;
        }
        (header.Segmentation.FeatureEnabled ?? throw new InvalidOperationException("Invalid internal state."))[segment][(int)feature] = true;
        (header.Segmentation.FeatureData ?? throw new InvalidOperationException("Invalid internal state."))[segment][(int)feature] = value;
    }

    static PlaneBuffer CreatePlane(int width, int height, int bitDepth)
    {
        return new PlaneBuffer(
            width, height, bitDepth > 8 ? 2 : 1, 1, 65536, true);
    }

    static void FillVerticalStep(
        PlaneBuffer plane,
        int boundary,
        int lower,
        int upper)
    {
        int y = 0;
        while (y < plane.Height)
        {
            int x = 0;
            while (x < plane.Width)
            {
                WriteSample(plane, x, y, x < boundary ? lower : upper);
                x++;
            }
            y++;
        }
    }

    static void FillHorizontalStep(
        PlaneBuffer plane,
        int boundary,
        int lower,
        int upper)
    {
        int y = 0;
        while (y < plane.Height)
        {
            int x = 0;
            while (x < plane.Width)
            {
                WriteSample(plane, x, y, y < boundary ? lower : upper);
                x++;
            }
            y++;
        }
    }

    static void FillPlane(PlaneBuffer plane, int value)
    {
        int y = 0;
        while (y < plane.Height)
        {
            int x = 0;
            while (x < plane.Width)
            {
                WriteSample(plane, x, y, value);
                x++;
            }
            y++;
        }
    }

    static int[] ReadRow(PlaneBuffer plane, int y)
    {
        return ReadRange(plane, y, 0, plane.Width);
    }

    static int[] ReadRange(PlaneBuffer plane, int y, int start, int count)
    {
        int[] values = new int[count];
        int index = 0;
        while (index < count)
        {
            values[index] = ReadSample(plane, start + index, y);
            index++;
        }
        return values;
    }

    static int[] ReadColumn(PlaneBuffer plane, int x)
    {
        int[] values = new int[plane.Height];
        int y = 0;
        while (y < plane.Height)
        {
            values[y] = ReadSample(plane, x, y);
            y++;
        }
        return values;
    }

    static int ReadSample(PlaneBuffer plane, int x, int y)
    {
        Span<byte> row = plane.GetRowSpan(y);
        int offset = x * plane.BytesPerSample;
        return plane.BytesPerSample == 1
            ? row[offset]
            : row[offset] | row[offset + 1] << 8;
    }

    static void WriteSample(PlaneBuffer plane, int x, int y, int value)
    {
        Span<byte> row = plane.GetRowSpan(y);
        int offset = x * plane.BytesPerSample;
        row[offset] = (byte)value;
        if (plane.BytesPerSample == 2)
        {
            row[offset + 1] = (byte)(value >> 8);
        }
    }

    static void Close(Av1LoopFilterRequest request)
    {
        int plane = 0;
        while (plane < request.Planes.Length)
        {
            request.Planes[plane].Close();
            plane++;
        }
    }
}
