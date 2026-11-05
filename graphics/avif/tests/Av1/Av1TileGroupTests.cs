namespace avif.Tests;

public class Av1TileGroupTests
{
    [Test]
    public void ParserSplitsLittleEndianTileSizesAndOwnsPayload()
    {
        byte[] payload =
        [
            0x98,
            1, 0, 0xA1, 0xA2,
            0, 0, 0xB1,
            2, 0, 0xC1, 0xC2, 0xC3,
            0xD1, 0xD2
        ];
        ResourceGuard resources = new ResourceGuard(new AvifSecurityLimits());
        resources.BeginTileSet();

        Av1TileGroupParseResult result = Av1TileGroupParser.Parse(
            payload,
            0,
            CreateHeader(2, 2, 2, 3),
            resources);
        Av1TileGroup? group = result.Group;
        payload[3] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((group ?? throw new InvalidOperationException("Invalid internal state.")).StartTile, Is.Zero);
            Assert.That(group.EndTile, Is.EqualTo(3));
            Assert.That(group.Tiles, Has.Length.EqualTo(4));
            Assert.That(group.Tiles[0].Offset, Is.EqualTo(3));
            Assert.That(group.Tiles[0].Size, Is.EqualTo(2));
            Assert.That(group.Tiles[0].GetData().ToArray(),
                Is.EqualTo(new byte[] { 0xA1, 0xA2 }));
            Assert.That(group.Tiles[1].Offset, Is.EqualTo(7));
            Assert.That(group.Tiles[1].Size, Is.EqualTo(1));
            Assert.That(group.Tiles[2].Offset, Is.EqualTo(10));
            Assert.That(group.Tiles[2].Size, Is.EqualTo(3));
            Assert.That(group.Tiles[3].Offset, Is.EqualTo(13));
            Assert.That(group.Tiles[3].Size, Is.EqualTo(2));
            Assert.That(group.Tiles[3].TileRow, Is.EqualTo(1));
            Assert.That(group.Tiles[3].TileColumn, Is.EqualTo(1));
            Assert.That(resources.TileCount.Current, Is.EqualTo(4));
            Assert.That(resources.TemporaryBytes.Current, Is.EqualTo(payload.Length));
        });

        Assert.That((group ?? throw new InvalidOperationException("Invalid internal state.")).Close(), Is.EqualTo(AvifResultCode.Success));
        Assert.Multiple(() =>
        {
            Assert.That(resources.TemporaryBytes.Current, Is.Zero);
            Assert.That(group.Tiles[0].GetData().IsEmpty, Is.True);
        });
    }

    [Test]
    public void ParserSupportsFrameObuBitOffsetsAndSingleTilePayloads()
    {
        byte[] payload = new byte[] { 0xA0, 0x11, 0x22, 0x33 };
        ResourceGuard resources = new ResourceGuard(new AvifSecurityLimits());
        resources.BeginTileSet();

        Av1TileGroupParseResult result = Av1TileGroupParser.Parse(
            payload,
            3,
            CreateHeader(1, 1, 0, 0),
            resources);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((result.Group ?? throw new InvalidOperationException("Invalid internal state.")).Tiles, Has.Length.EqualTo(1));
            Assert.That(result.Group.Tiles[0].Offset, Is.EqualTo(1));
            Assert.That(result.Group.Tiles[0].GetData().ToArray(),
                Is.EqualTo(new byte[] { 0x11, 0x22, 0x33 }));
        });

        (result.Group ?? throw new InvalidOperationException("Invalid internal state.")).Close();
    }

    [Test]
    public void ParserRejectsRangesAlignmentSizesAndResourceOverruns()
    {
        ParsedAv1FrameHeader header = CreateHeader(2, 2, 2, 3);
        Av1TileGroupParseResult reversed = Parse(
            new byte[] { 0xC8, 1 }, header, new AvifSecurityLimits());
        Av1TileGroupParseResult alignment = Parse(
            new byte[] { 0x99, 1 }, header, new AvifSecurityLimits());
        Av1TileGroupParseResult truncated = Parse(
            new byte[] { 0x98, 0xFF, 0xFF }, header, new AvifSecurityLimits());
        Av1TileGroupParseResult emptyLast = Parse(
            new byte[] { 0xD0 }, header, new AvifSecurityLimits());
        ParsedAv1FrameHeader badLogHeader = CreateHeader(2, 2, 2, 0);
        (badLogHeader.Tiles ?? throw new InvalidOperationException("Invalid internal state.")).TileColumnsLog2 = 0;
        Av1TileGroupParseResult badLog = Parse(
            CreateFullGroupPayload(), badLogHeader, new AvifSecurityLimits());
        AvifSecurityLimits tileLimits = new AvifSecurityLimits();
        tileLimits.MaxTileCount = 3;
        Av1TileGroupParseResult tileLimited = Parse(
            CreateFullGroupPayload(), header, tileLimits);
        AvifSecurityLimits byteLimits = new AvifSecurityLimits();
        byteLimits.MaxTemporaryBytes = 5;
        Av1TileGroupParseResult byteLimited = Parse(
            CreateFullGroupPayload(), header, byteLimits);

        Assert.Multiple(() =>
        {
            Assert.That(reversed.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(alignment.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(truncated.Code, Is.EqualTo(AvifResultCode.TruncatedData));
            Assert.That(emptyLast.Code, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(badLog.Code, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(tileLimited.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(byteLimited.Code, Is.EqualTo(AvifResultCode.LimitExceeded));
        });
    }

    [Test]
    public void SchedulerLimitsInFlightTilesAndCompletesOutOfOrder()
    {
        Av1CdfContextManager contexts = new Av1CdfContextManager();
        Av1TileScheduler scheduler = new Av1TileScheduler(
            CreateLimits(4, 2),
            contexts);
        ParsedAv1FrameHeader header = CreateHeader(2, 2, 2, 3);
        header.RefreshFrameFlags = 1;

        AvifResultCode begin = scheduler.BeginFrame(header, 2);
        AvifResultCode add = scheduler.AddTileGroup(CreateFullGroupPayload(), 0);
        AvifResultCode groups = scheduler.CompleteGroups();
        Av1TileWorkItem? first = scheduler.TakeNextTile();
        Av1TileWorkItem? second = scheduler.TakeNextTile();
        Av1TileWorkItem? blocked = scheduler.TakeNextTile();
        Av1Cdf? firstCdf = ((first ?? throw new InvalidOperationException("Invalid internal state.")).Context ?? throw new InvalidOperationException("Invalid internal state.")).GetCdf("Default_Mv_Joint_Cdf", 0);
        Av1Cdf? secondCdf = ((second ?? throw new InvalidOperationException("Invalid internal state.")).Context ?? throw new InvalidOperationException("Invalid internal state.")).GetCdf("Default_Mv_Joint_Cdf", 0);
        (firstCdf ?? throw new InvalidOperationException("Invalid internal state.")).SetValue(0, 3000);

        AvifResultCode secondDone = scheduler.CompleteTile(second);
        Av1TileWorkItem? third = scheduler.TakeNextTile();
        AvifResultCode firstDone = scheduler.CompleteTile(first);
        Av1TileWorkItem? fourth = scheduler.TakeNextTile();
        AvifResultCode thirdDone = scheduler.CompleteTile(third);
        AvifResultCode fourthDone = scheduler.CompleteTile(fourth);

        Assert.Multiple(() =>
        {
            Assert.That(begin, Is.EqualTo(AvifResultCode.Success));
            Assert.That(add, Is.EqualTo(AvifResultCode.Success));
            Assert.That(groups, Is.EqualTo(AvifResultCode.Success));
            Assert.That(first.Tile.TileIndex, Is.Zero);
            Assert.That(second.Tile.TileIndex, Is.EqualTo(1));
            Assert.That(blocked, Is.Null);
            Assert.That((secondCdf ?? throw new InvalidOperationException("Invalid internal state.")).GetValue(0), Is.EqualTo(4096));
            Assert.That(secondDone, Is.EqualTo(AvifResultCode.Success));
            Assert.That((third ?? throw new InvalidOperationException("Invalid internal state.")).Tile.TileIndex, Is.EqualTo(2));
            Assert.That(firstDone, Is.EqualTo(AvifResultCode.Success));
            Assert.That((fourth ?? throw new InvalidOperationException("Invalid internal state.")).Tile.TileIndex, Is.EqualTo(3));
            Assert.That(thirdDone, Is.EqualTo(AvifResultCode.Success));
            Assert.That(fourthDone, Is.EqualTo(AvifResultCode.Success));
            Assert.That(scheduler.FrameActive, Is.False);
            Assert.That(scheduler.Resources.WorkerCount.Current, Is.Zero);
            Assert.That(scheduler.Resources.TemporaryBytes.Current, Is.Zero);
            Assert.That(contexts.FrameActive, Is.False);
            Assert.That(contexts.GetReferenceContext(0), Is.Not.Null);
            Assert.That(scheduler.CompleteTile(fourth),
                Is.EqualTo(AvifResultCode.InvalidArgument));
        });

        scheduler.Close();
    }

    [Test]
    public void SchedulerAcceptsContiguousSplitGroups()
    {
        Av1TileScheduler scheduler = new Av1TileScheduler(
            CreateLimits(4, 1),
            new Av1CdfContextManager());
        ParsedAv1FrameHeader header = CreateHeader(2, 2, 2, 0);

        AvifResultCode begin = scheduler.BeginFrame(header, 1);
        AvifResultCode first = scheduler.AddTileGroup(
            new byte[] { 0x88, 1, 0, 0x10, 0x11, 0x12, 0x13 }, 0);
        AvifResultCode second = scheduler.AddTileGroup(
            new byte[] { 0xD8, 0, 0, 0x20, 0x21 }, 0);
        AvifResultCode groups = scheduler.CompleteGroups();

        int index = 0;
        while (index < 4)
        {
            Av1TileWorkItem? work = scheduler.TakeNextTile();
            Assert.That((work ?? throw new InvalidOperationException("Invalid internal state.")).Tile.TileIndex, Is.EqualTo(index));
            Assert.That(scheduler.CompleteTile(work), Is.EqualTo(AvifResultCode.Success));
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(begin, Is.EqualTo(AvifResultCode.Success));
            Assert.That(first, Is.EqualTo(AvifResultCode.Success));
            Assert.That(second, Is.EqualTo(AvifResultCode.Success));
            Assert.That(groups, Is.EqualTo(AvifResultCode.Success));
            Assert.That(scheduler.FrameActive, Is.False);
        });

        scheduler.Close();
    }

    [Test]
    public void SchedulerAbortsGappedGroupsAndReleasesEveryReservation()
    {
        Av1CdfContextManager contexts = new Av1CdfContextManager();
        Av1TileScheduler scheduler = new Av1TileScheduler(
            CreateLimits(4, 2),
            contexts);
        scheduler.BeginFrame(CreateHeader(2, 2, 2, 0), 2);

        AvifResultCode result = scheduler.AddTileGroup(
            new byte[] { 0xD8, 0, 0, 0x20, 0x21 }, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(scheduler.FrameActive, Is.False);
            Assert.That(contexts.FrameActive, Is.False);
            Assert.That(scheduler.Resources.WorkerCount.Current, Is.Zero);
            Assert.That(scheduler.Resources.TemporaryBytes.Current, Is.Zero);
        });

        scheduler.Close();
    }

    static Av1TileGroupParseResult Parse(
        byte[] payload,
        ParsedAv1FrameHeader header,
        AvifSecurityLimits limits)
    {
        ResourceGuard resources = new ResourceGuard(limits);
        resources.BeginTileSet();
        return Av1TileGroupParser.Parse(payload, 0, header, resources);
    }

    static byte[] CreateFullGroupPayload()
    {
        return new byte[]
        {
            0x98,
            0, 0, 0x10,
            0, 0, 0x20,
            0, 0, 0x30,
            0x40
        };
    }

    static AvifSecurityLimits CreateLimits(int maxTiles, int maxWorkers)
    {
        AvifSecurityLimits limits = new AvifSecurityLimits();
        limits.MaxTileCount = maxTiles;
        limits.MaxWorkerCount = maxWorkers;
        return limits;
    }

    static ParsedAv1FrameHeader CreateHeader(
        int columns,
        int rows,
        int tileSizeBytes,
        int contextUpdateTile)
    {
        ParsedAv1FrameHeader header = new ParsedAv1FrameHeader();
        header.PrimaryReferenceFrame = 7;
        header.Quantization = new Av1QuantizationParameters();
        header.Quantization.BaseQIndex = 20;
        header.Tiles = new Av1TileInfo();
        header.Tiles.TileColumns = columns;
        header.Tiles.TileRows = rows;
        header.Tiles.TileColumnsLog2 = CeilLog2(columns);
        header.Tiles.TileRowsLog2 = CeilLog2(rows);
        header.Tiles.TileSizeBytes = tileSizeBytes;
        header.Tiles.ContextUpdateTileId = contextUpdateTile;
        return header;
    }

    static int CeilLog2(int value)
    {
        int result = 0;
        int power = 1;
        while (power < value)
        {
            power <<= 1;
            result++;
        }

        return result;
    }
}
