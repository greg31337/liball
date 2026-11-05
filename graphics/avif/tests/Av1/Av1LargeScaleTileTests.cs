namespace avif.Tests;

public class Av1LargeScaleTileTests
{
    [Test]
    public void TileListParsingRetainsBoundedEntriesAndReleasesResources()
    {
        ParsedAv1SequenceHeader sequence = CreateSequence(8, false);
        ParsedAv1FrameHeader header = CreateHeader();
        ResourceGuard resources = new ResourceGuard(new AvifSecurityLimits());
        Av1LargeScaleTileListParseResult result = Av1LargeScaleTiles.Parse(
            CreateTileListPayload(),
            sequence,
            header,
            resources,
            CreateSelection(true));
        Av1LargeScaleTileEntry retainedEntry = (result.List ?? throw new InvalidOperationException("Invalid internal state.")).Entries[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(result.List.OutputTileColumns, Is.EqualTo(2));
            Assert.That(result.List.OutputTileRows, Is.EqualTo(2));
            Assert.That(result.List.Entries, Has.Length.EqualTo(3));
            Assert.That(result.List.Entries[0].AnchorFrameIndex, Is.Zero);
            Assert.That(result.List.Entries[1].AnchorFrameIndex, Is.EqualTo(127));
            Assert.That(result.List.Entries[1].AnchorTileRow, Is.EqualTo(1));
            Assert.That(result.List.Entries[1].AnchorTileColumn, Is.EqualTo(1));
            Assert.That(result.List.Entries[0].GetData().ToArray(),
                Is.EqualTo(new byte[] { 0xAA,0xBB }));
            Assert.That(result.List.Entries[2].GetData().ToArray(),
                Is.EqualTo(new byte[] { 0xDD,0xEE,0xFF }));
            Assert.That(resources.TileCount.Current, Is.EqualTo(3));
            Assert.That(resources.TemporaryBytes.Current,
                Is.EqualTo(CreateTileListPayload().Length));
        });

        Assert.That(result.List.Close(), Is.EqualTo(AvifResultCode.Success));
        Assert.Multiple(() =>
        {
            Assert.That(resources.TileCount.Current, Is.Zero);
            Assert.That(resources.TemporaryBytes.Current, Is.Zero);
            Assert.That(retainedEntry.GetData().IsEmpty, Is.True);
        });
    }

    [Test]
    public void TileListModeIsOptInAndRejectsMalformedOrLimitedLists()
    {
        ParsedAv1SequenceHeader sequence = CreateSequence(8, false);
        ParsedAv1FrameHeader header = CreateHeader();
        byte[] invalidAnchor = CreateTileListPayload();
        invalidAnchor[11] = 128;
        byte[] excessiveCount = new byte[]
        {
            0,0,0,1,
            0,0,0,0,0,0x80,
            0,0,0,0,0,0x80
        };
        byte[] truncated = CreateTileListPayload();
        System.Array.Resize(ref truncated, truncated.Length - 1);
        AvifSecurityLimits limited = new AvifSecurityLimits();
        limited.MaxTileCount = 2;

        Av1LargeScaleTileListParseResult disabled = Av1LargeScaleTiles.Parse(
            CreateTileListPayload(), sequence, header,
            new ResourceGuard(new AvifSecurityLimits()),
            CreateSelection(false));
        Av1LargeScaleTileListParseResult anchor = Av1LargeScaleTiles.Parse(
            invalidAnchor, sequence, header,
            new ResourceGuard(new AvifSecurityLimits()),
            CreateSelection(true));
        Av1LargeScaleTileListParseResult capacity = Av1LargeScaleTiles.Parse(
            excessiveCount, sequence, header,
            new ResourceGuard(new AvifSecurityLimits()),
            CreateSelection(true));
        Av1LargeScaleTileListParseResult shortList = Av1LargeScaleTiles.Parse(
            truncated, sequence, header,
            new ResourceGuard(new AvifSecurityLimits()),
            CreateSelection(true));
        Av1LargeScaleTileListParseResult overLimit = Av1LargeScaleTiles.Parse(
            CreateTileListPayload(), sequence, header,
            new ResourceGuard(limited),
            CreateSelection(true));

        Assert.Multiple(() =>
        {
            Assert.That(disabled.Code,
                Is.EqualTo(AvifResultCode.UnsupportedFeature));
            Assert.That(anchor.Code,
                Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(capacity.Code,
                Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(shortList.Code,
                Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(overLimit.Code,
                Is.EqualTo(AvifResultCode.LimitExceeded));
        });
    }

    [Test]
    public void ConfigurationValidationEnforcesCameraTileRestrictions()
    {
        ParsedAv1FrameHeader header = CreateHeader();
        ParsedAv1FrameHeader wrongFrame = CreateHeader();
        wrongFrame.RefreshFrameFlags = 1;
        ParsedAv1FrameHeader unevenTiles = CreateHeader();
        (unevenTiles.Tiles ?? throw new InvalidOperationException("Invalid internal state.")).MiColumnStarts = new int[] { 0,16,31 };

        Assert.Multiple(() =>
        {
            Assert.That(Av1LargeScaleTiles.ValidateConfiguration(
                CreateSequence(8, false), header),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(Av1LargeScaleTiles.ValidateConfiguration(
                CreateSequence(8, true), header),
                Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(Av1LargeScaleTiles.ValidateConfiguration(
                CreateSequence(8, false), wrongFrame),
                Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(Av1LargeScaleTiles.ValidateConfiguration(
                CreateSequence(8, false), unevenTiles),
                Is.EqualTo(AvifResultCode.NonConformingData));
        });
    }

    [Test]
    public void DecodedTilesAreComposedInListOrderAndLeaveHolesUntouched()
    {
        ParsedAv1SequenceHeader sequence = CreateSequence(10, false);
        ParsedAv1FrameHeader header = CreateHeader();
        ResourceGuard resources = new ResourceGuard(new AvifSecurityLimits());
        Av1LargeScaleTileListParseResult parsed = Av1LargeScaleTiles.Parse(
            CreateTileListPayload(), sequence, header, resources,
            CreateSelection(true));
        Av1LargeScaleTileOutputRequest request = CreateOutputRequest(
            sequence, header, parsed.List);

        AvifResultCode code = Av1LargeScaleTiles.Compose(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadSample(request.DestinationPlanes[0], 4, 4),
                Is.EqualTo(101));
            Assert.That(ReadSample(request.DestinationPlanes[0], 68, 4),
                Is.EqualTo(201));
            Assert.That(ReadSample(request.DestinationPlanes[0], 4, 68),
                Is.EqualTo(301));
            Assert.That(ReadSample(request.DestinationPlanes[0], 68, 68),
                Is.EqualTo(9));
            Assert.That(ReadSample(request.DestinationPlanes[1], 2, 2),
                Is.EqualTo(102));
            Assert.That(ReadSample(request.DestinationPlanes[2], 34, 2),
                Is.EqualTo(203));
            Assert.That(ReadSample(request.DestinationPlanes[2], 34, 34),
                Is.EqualTo(9));
        });
        CloseOutput(request);
        (parsed.List ?? throw new InvalidOperationException("Invalid internal state.")).Close();
    }

    [Test]
    public void InvalidOutputIsAtomic()
    {
        ParsedAv1SequenceHeader sequence = CreateSequence(8, false);
        ParsedAv1FrameHeader header = CreateHeader();
        ResourceGuard resources = new ResourceGuard(new AvifSecurityLimits());
        Av1LargeScaleTileListParseResult parsed = Av1LargeScaleTiles.Parse(
            CreateTileListPayload(), sequence, header, resources,
            CreateSelection(true));
        Av1LargeScaleTileOutputRequest request = CreateOutputRequest(
            sequence, header, parsed.List);
        Fill(request.DestinationPlanes[0], 77);
        request.DecodedTiles[2][0].Close();

        AvifResultCode code = Av1LargeScaleTiles.Compose(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Closed));
            Assert.That(ReadSample(request.DestinationPlanes[0], 0, 0),
                Is.EqualTo(77));
            Assert.That(ReadSample(request.DestinationPlanes[0], 127, 127),
                Is.EqualTo(77));
        });
        CloseOutput(request);
        (parsed.List ?? throw new InvalidOperationException("Invalid internal state.")).Close();
    }

    static Av1LayerSelection CreateSelection(bool enabled)
    {
        Av1LayerSelection selection = new Av1LayerSelection(
            AvifResultCode.Success);
        selection.LargeScaleTiles = enabled;
        return selection;
    }

    static byte[] CreateTileListPayload()
    {
        return new byte[]
        {
            1,1,0,2,
            0,0,0,0,1,0xAA,0xBB,
            127,1,1,0,0,0xCC,
            2,0,1,0,2,0xDD,0xEE,0xFF
        };
    }

    static Av1LargeScaleTileOutputRequest CreateOutputRequest(
        ParsedAv1SequenceHeader sequence,
        ParsedAv1FrameHeader header,
        Av1LargeScaleTileList? list)
    {
        Av1LargeScaleTileOutputRequest request =
            new Av1LargeScaleTileOutputRequest();
        request.Sequence = sequence;
        request.Header = header;
        request.List = list;
        request.DecodedTiles = new PlaneBuffer[(list ?? throw new InvalidOperationException("Invalid internal state.")).Entries.Length][];
        int tile = 0;
        while (tile < request.DecodedTiles.Length)
        {
            request.DecodedTiles[tile] = new PlaneBuffer[3];
            int plane = 0;
            while (plane < 3)
            {
                int width = plane == 0 ? 64 : 32;
                int height = plane == 0 ? 64 : 32;
                request.DecodedTiles[tile][plane] = CreatePlane(
                    width, height, sequence.GetBitDepth());
                Fill(request.DecodedTiles[tile][plane],
                    (tile + 1) * 100 + plane + 1);
                plane++;
            }
            tile++;
        }
        request.DestinationPlanes = new PlaneBuffer[]
        {
            CreatePlane(128, 128, sequence.GetBitDepth()),
            CreatePlane(64, 64, sequence.GetBitDepth()),
            CreatePlane(64, 64, sequence.GetBitDepth())
        };
        Fill(request.DestinationPlanes[0], 9);
        Fill(request.DestinationPlanes[1], 9);
        Fill(request.DestinationPlanes[2], 9);
        return request;
    }

    static ParsedAv1FrameHeader CreateHeader()
    {
        ParsedAv1FrameHeader header = new ParsedAv1FrameHeader();
        header.FrameType = Av1FrameType.Inter;
        header.ShowFrame = true;
        header.DisableCdfUpdate = true;
        header.DisableFrameEndUpdateCdf = true;
        header.Size = new Av1FrameSize();
        header.Size.FrameWidth = 128;
        header.Size.FrameHeight = 128;
        header.Size.UpscaledWidth = 128;
        header.Size.RenderWidth = 128;
        header.Size.RenderHeight = 128;
        header.Size.MiColumns = 32;
        header.Size.MiRows = 32;
        header.Tiles = new Av1TileInfo();
        header.Tiles.TileColumns = 2;
        header.Tiles.TileRows = 2;
        header.Tiles.TileColumnsLog2 = 1;
        header.Tiles.TileRowsLog2 = 1;
        header.Tiles.MiColumnStarts = new int[] { 0,16,32 };
        header.Tiles.MiRowStarts = new int[] { 0,16,32 };
        header.Tiles.TileSizeBytes = 1;
        header.Quantization = new Av1QuantizationParameters();
        header.Segmentation = new Av1SegmentationParameters();
        header.LoopFilter = new Av1LoopFilterParameters();
        header.LoopFilter.Levels = new int[4];
        return header;
    }

    static ParsedAv1SequenceHeader CreateSequence(
        int bitDepth,
        bool enableCdef)
    {
        Av1SequenceFeatures features = new Av1SequenceFeatures();
        features.EnableCdef = enableCdef;
        return new ParsedAv1SequenceHeader(
            0,
            false,
            false,
            null,
            null,
            false,
            new Av1OperatingPoint[] { new Av1OperatingPoint(0, 4, 0) },
            128,
            128,
            features,
            bitDepth > 8,
            bitDepth == 12,
            false,
            true,
            true,
            0,
            1,
            13,
            1,
            false,
            true,
            false,
            false,
            System.Array.Empty<byte>());
    }

    static PlaneBuffer CreatePlane(int width, int height, int bitDepth)
    {
        return new PlaneBuffer(
            width, height, bitDepth > 8 ? 2 : 1, 1, 1 << 22, true);
    }

    static void Fill(PlaneBuffer plane, int value)
    {
        if (plane.Closed)
        {
            return;
        }
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

    static void CloseOutput(Av1LargeScaleTileOutputRequest request)
    {
        int tile = 0;
        while (tile < request.DecodedTiles.Length)
        {
            int plane = 0;
            while (plane < request.DecodedTiles[tile].Length)
            {
                request.DecodedTiles[tile][plane].Close();
                plane++;
            }
            tile++;
        }
        int destination = 0;
        while (destination < request.DestinationPlanes.Length)
        {
            request.DestinationPlanes[destination].Close();
            destination++;
        }
    }
}
