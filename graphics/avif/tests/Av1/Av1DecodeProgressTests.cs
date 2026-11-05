namespace avif.Tests;

public class Av1DecodeProgressTests
{
    [Test]
    public void IncrementalProgressExposesDecodedTileRowsAndContiguousRows()
    {
        Av1DecodeProgress progress = new Av1DecodeProgress(
            true, new AvifCancellation());

        Assert.That(progress.BeginFrame(CreateHeader()),
            Is.EqualTo(AvifResultCode.Success));
        Assert.That(progress.ReportDecodedRows(0, 20),
            Is.EqualTo(AvifResultCode.Success));
        Assert.That(progress.ReportDecodedRows(1, 12),
            Is.EqualTo(AvifResultCode.Success));
        Av1DecodeProgressSnapshot first = progress.GetSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(first.FrameWidth, Is.EqualTo(120));
            Assert.That(first.FrameHeight, Is.EqualTo(100));
            Assert.That(first.TotalTileCount, Is.EqualTo(4));
            Assert.That(first.CompletedTileCount, Is.Zero);
            Assert.That(first.ContiguousDecodedRows, Is.EqualTo(12));
            Assert.That(first.VisibleTiles, Has.Length.EqualTo(2));
            Assert.That(first.VisibleTiles[0].TileIndex, Is.Zero);
            Assert.That(first.VisibleTiles[0].Width, Is.EqualTo(64));
            Assert.That(first.VisibleTiles[0].Height, Is.EqualTo(20));
            Assert.That(first.VisibleTiles[1].TileColumn, Is.EqualTo(1));
            Assert.That(first.VisibleTiles[1].X, Is.EqualTo(64));
            Assert.That(first.VisibleTiles[1].Width, Is.EqualTo(56));
            Assert.That(first.VisibleTiles[1].Height, Is.EqualTo(12));
        });

        Assert.That(progress.ReportTileCompleted(0),
            Is.EqualTo(AvifResultCode.Success));
        Assert.That(progress.ReportTileCompleted(1),
            Is.EqualTo(AvifResultCode.Success));
        Assert.That(progress.ReportDecodedRows(2, 36),
            Is.EqualTo(AvifResultCode.Success));
        Assert.That(progress.ReportDecodedRows(3, 10),
            Is.EqualTo(AvifResultCode.Success));
        Av1DecodeProgressSnapshot partial = progress.GetSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(partial.CompletedTileCount, Is.EqualTo(2));
            Assert.That(partial.ContiguousDecodedRows, Is.EqualTo(74));
            Assert.That(partial.VisibleTiles, Has.Length.EqualTo(4));
            Assert.That(partial.VisibleTiles[3].TileRow, Is.EqualTo(1));
            Assert.That(partial.VisibleTiles[3].X, Is.EqualTo(64));
            Assert.That(partial.VisibleTiles[3].Y, Is.EqualTo(64));
            Assert.That(partial.VisibleTiles[3].Width, Is.EqualTo(56));
            Assert.That(partial.VisibleTiles[3].Height, Is.EqualTo(10));
            Assert.That(partial.VisibleTiles[3].Complete, Is.False);
        });

        Assert.That(progress.ReportTileCompleted(2),
            Is.EqualTo(AvifResultCode.Success));
        Assert.That(progress.ReportTileCompleted(3),
            Is.EqualTo(AvifResultCode.Success));
        Assert.That(progress.CompleteFrame(),
            Is.EqualTo(AvifResultCode.Success));
        Av1DecodeProgressSnapshot complete = progress.GetSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(complete.FrameComplete, Is.True);
            Assert.That(complete.CompletedTileCount, Is.EqualTo(4));
            Assert.That(complete.ContiguousDecodedRows, Is.EqualTo(100));
            Assert.That(complete.VisibleTiles[3].Height, Is.EqualTo(36));
            Assert.That(complete.VisibleTiles[3].Complete, Is.True);
        });
    }

    [Test]
    public void NonIncrementalProgressHidesPartialGridUntilFrameCompletion()
    {
        Av1DecodeProgress progress = new Av1DecodeProgress(
            false, new AvifCancellation());
        progress.BeginFrame(CreateHeader());
        progress.ReportDecodedRows(0, 20);
        progress.ReportTileCompleted(0);

        Av1DecodeProgressSnapshot partial = progress.GetSnapshot();
        progress.ReportTileCompleted(1);
        progress.ReportTileCompleted(2);
        progress.ReportTileCompleted(3);
        progress.CompleteFrame();
        Av1DecodeProgressSnapshot complete = progress.GetSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(partial.VisibleTiles, Is.Empty);
            Assert.That(partial.ContiguousDecodedRows, Is.Zero);
            Assert.That(complete.VisibleTiles, Has.Length.EqualTo(4));
            Assert.That(complete.ContiguousDecodedRows, Is.EqualTo(100));
        });
    }

    [Test]
    public void RequiredRangesMergeSplitAndIncludeIncrementalReadGaps()
    {
        Av1DecodeProgress progress = new Av1DecodeProgress(
            true, new AvifCancellation());
        progress.BeginFrame(CreateHeader());

        Assert.That(progress.RequireRange(100, 20),
            Is.EqualTo(AvifResultCode.NeedMoreData));
        Assert.That(progress.RequireRange(115, 20),
            Is.EqualTo(AvifResultCode.NeedMoreData));
        Assert.That(progress.SatisfyRange(110, 10),
            Is.EqualTo(AvifResultCode.Success));

        IncrementalInput input = new IncrementalInput(0, 32, 32);
        input.Append(new byte[] { 1, 2, 3 });
        InputReadResult read = input.ReadExact(2, new byte[4]);
        Assert.That(progress.ObserveRead(read),
            Is.EqualTo(AvifResultCode.NeedMoreData));
        Av1DecodeProgressSnapshot waiting = progress.GetSnapshot();
        waiting.RequiredRanges[0].Length = 99;
        Av1DecodeProgressSnapshot unchanged = progress.GetSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(waiting.Code, Is.EqualTo(AvifResultCode.NeedMoreData));
            Assert.That(waiting.RequiredRanges, Has.Length.EqualTo(3));
            Assert.That(unchanged.RequiredRanges[0].Offset, Is.EqualTo(3));
            Assert.That(unchanged.RequiredRanges[0].Length, Is.EqualTo(3));
            Assert.That(unchanged.RequiredRanges[1].Offset, Is.EqualTo(100));
            Assert.That(unchanged.RequiredRanges[1].Length, Is.EqualTo(10));
            Assert.That(unchanged.RequiredRanges[2].Offset, Is.EqualTo(120));
            Assert.That(unchanged.RequiredRanges[2].Length, Is.EqualTo(15));
        });

        Assert.That(progress.SatisfyRange(0, 200),
            Is.EqualTo(AvifResultCode.Success));
        Assert.That(progress.GetSnapshot().RequiredRanges, Is.Empty);
        input.Close();
    }

    [Test]
    public void CancellationStopsProgressAtTheNextCheckpoint()
    {
        AvifCancellation cancellation = new AvifCancellation();
        Av1DecodeProgress progress = new Av1DecodeProgress(
            true, cancellation);
        progress.BeginFrame(CreateHeader());
        progress.ReportDecodedRows(0, 8);

        cancellation.Cancel();
        AvifResultCode result = progress.ReportDecodedRows(0, 9);
        Av1DecodeProgressSnapshot snapshot = progress.GetSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(cancellation.IsCancellationRequested(), Is.True);
            Assert.That(result, Is.EqualTo(AvifResultCode.Cancelled));
            Assert.That(snapshot.Code, Is.EqualTo(AvifResultCode.Cancelled));
            Assert.That(snapshot.DecodedRowsPerTile[0], Is.EqualTo(8));
            Assert.That(progress.CompleteFrame(),
                Is.EqualTo(AvifResultCode.Cancelled));
        });
    }

    [Test]
    public void SchedulerReportsRowsAndReleasesResourcesWhenCancelled()
    {
        AvifCancellation cancellation = new AvifCancellation();
        Av1DecodeProgress progress = new Av1DecodeProgress(
            true, cancellation);
        Av1CdfContextManager contexts = new Av1CdfContextManager();
        Av1TileScheduler scheduler = new Av1TileScheduler(
            CreateLimits(), contexts);

        Assert.That(scheduler.BeginFrameWithProgress(
                CreateHeader(), 2, progress),
            Is.EqualTo(AvifResultCode.Success));
        Assert.That(scheduler.AddTileGroup(CreateFullGroupPayload(), 0),
            Is.EqualTo(AvifResultCode.Success));
        Assert.That(scheduler.CompleteGroups(),
            Is.EqualTo(AvifResultCode.Success));
        Av1TileWorkItem? work = scheduler.TakeNextTile();
        Assert.That(scheduler.ReportDecodedRows(work, 17),
            Is.EqualTo(AvifResultCode.Success));
        Av1DecodeProgressSnapshot partial = progress.GetSnapshot();

        cancellation.Cancel();
        Av1TileWorkItem? cancelled = scheduler.TakeNextTile();

        Assert.Multiple(() =>
        {
            Assert.That(partial.VisibleTiles, Has.Length.EqualTo(1));
            Assert.That(partial.VisibleTiles[0].Height, Is.EqualTo(17));
            Assert.That(cancelled, Is.Null);
            Assert.That(scheduler.Code, Is.EqualTo(AvifResultCode.Cancelled));
            Assert.That(scheduler.FrameActive, Is.False);
            Assert.That(scheduler.Resources.WorkerCount.Current, Is.Zero);
            Assert.That(scheduler.Resources.TemporaryBytes.Current, Is.Zero);
            Assert.That(contexts.FrameActive, Is.False);
            Assert.That((work ?? throw new InvalidOperationException("Invalid internal state.")).State, Is.EqualTo(2));
            Assert.That(progress.GetSnapshot().Code,
                Is.EqualTo(AvifResultCode.Cancelled));
        });

        scheduler.Close();
    }

    static ParsedAv1FrameHeader CreateHeader()
    {
        ParsedAv1FrameHeader header = new ParsedAv1FrameHeader();
        header.PrimaryReferenceFrame = 7;
        header.Quantization = new Av1QuantizationParameters();
        header.Quantization.BaseQIndex = 20;
        header.Size = new Av1FrameSize();
        header.Size.FrameWidth = 120;
        header.Size.FrameHeight = 100;
        header.Size.MiColumns = 30;
        header.Size.MiRows = 25;
        header.Tiles = new Av1TileInfo();
        header.Tiles.TileColumns = 2;
        header.Tiles.TileRows = 2;
        header.Tiles.TileColumnsLog2 = 1;
        header.Tiles.TileRowsLog2 = 1;
        header.Tiles.TileSizeBytes = 2;
        header.Tiles.ContextUpdateTileId = 3;
        header.Tiles.MiColumnStarts = new int[] { 0, 16, 30 };
        header.Tiles.MiRowStarts = new int[] { 0, 16, 25 };
        return header;
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

    static AvifSecurityLimits CreateLimits()
    {
        AvifSecurityLimits limits = new AvifSecurityLimits();
        limits.MaxTileCount = 4;
        limits.MaxWorkerCount = 2;
        return limits;
    }
}
