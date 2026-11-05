namespace avif.Tests;

public class Av1EncoderTileLayerPlanTests
{
    [Test]
    public void AutomaticTilingMeetsAv1LimitsAndSchedulesIndependentWorkers()
    {
        Av1EncoderTileLayerPlanRequest request = CreateRequest(9000, 5000);
        request.Options.WorkerCount = 8;
        request.Options.Limits.MaxWorkerCount = 3;

        Av1EncoderTileLayerPlan result =
            Av1EncoderTileLayerPlanner.Plan(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((result.Tiles ?? throw new InvalidOperationException("Invalid internal state.")).TileColumns, Is.EqualTo(4));
            Assert.That(result.Tiles.TileRows, Is.EqualTo(2));
            Assert.That(result.Tiles.TileColumnsLog2, Is.EqualTo(2));
            Assert.That(result.Tiles.TileRowsLog2, Is.EqualTo(1));
            Assert.That(result.TileJobs, Has.Length.EqualTo(8));
            Assert.That(result.WorkerCount, Is.EqualTo(3));
            Assert.That(GetWorkers(result.TileJobs),
                Is.EqualTo(new int[] { 0, 1, 2, 0, 1, 2, 0, 1 }));
            Assert.That(GetDependencies(result.TileJobs),
                Is.EqualTo(new int[] { -1, -1, -1, -1, -1, -1, -1, -1 }));
            Assert.That(GetContexts(result.TileJobs),
                Is.EqualTo(new int[] { 0, 1, 2, 3, 4, 5, 6, 7 }));
            Assert.That(result.TileJobs[7].X + result.TileJobs[7].Width,
                Is.EqualTo(9000));
            Assert.That(result.TileJobs[7].Y + result.TileJobs[7].Height,
                Is.EqualTo(5000));
            Assert.That(MaximumWidth(result.TileJobs), Is.LessThanOrEqualTo(4096));
            Assert.That(MaximumArea(result.TileJobs),
                Is.LessThanOrEqualTo(Av1SyntaxConstants.MaxTileArea));
        });
    }

    [Test]
    public void ManualTilingUsesRequestedGeometryAndBoundsWorkerCount()
    {
        Av1EncoderTileLayerPlanRequest request = CreateRequest(1920, 1080);
        request.Options.AutoTiling = false;
        request.Options.TileColumnsLog2 = 1;
        request.Options.TileRowsLog2 = 1;
        request.Options.WorkerCount = 6;
        request.Options.Limits.MaxWorkerCount = 2;

        Av1EncoderTileLayerPlan result =
            Av1EncoderTileLayerPlanner.Plan(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((result.Tiles ?? throw new InvalidOperationException("Invalid internal state.")).TileColumns, Is.EqualTo(2));
            Assert.That(result.Tiles.TileRows, Is.EqualTo(2));
            Assert.That(result.Tiles.TileSizeBytes, Is.EqualTo(4));
            Assert.That(result.WorkerCount, Is.EqualTo(2));
            Assert.That(GetWorkers(result.TileJobs),
                Is.EqualTo(new int[] { 0, 1, 0, 1 }));
            Assert.That((result.Tiles.MiColumnStarts ?? throw new InvalidOperationException("Invalid internal state."))[0], Is.Zero);
            Assert.That(result.Tiles.MiColumnStarts[2], Is.EqualTo(480));
            Assert.That((result.Tiles.MiRowStarts ?? throw new InvalidOperationException("Invalid internal state."))[2], Is.EqualTo(270));
        });
    }

    [Test]
    public void LayerPlanBuildsCumulativeOperatingPointsAndProgressiveOutputs()
    {
        Av1EncoderTileLayerPlanRequest request = CreateRequest(1920, 1080);
        request.FrameCount = 4;
        request.TemporalLayerCount = 3;
        request.Progressive = true;
        request.SpatialLayers = new Av1EncoderSpatialLayer[]
        {
            new Av1EncoderSpatialLayer(640, 360),
            new Av1EncoderSpatialLayer(1280, 720),
            new Av1EncoderSpatialLayer(1920, 1080)
        };

        Av1EncoderTileLayerPlan result =
            Av1EncoderTileLayerPlanner.Plan(request);
        ParsedAv1Metadata metadata = new ParsedAv1Metadata(
            (ulong)Av1MetadataType.Scalability);
        metadata.Scalability = result.Scalability;
        Av1MetadataParseResult parsed = Av1MetadataParser.ParsePayload(
            Av1MetadataWriter.WritePayload(metadata).Data);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(GetOperatingPointIdcs(result.OperatingPoints),
                Is.EqualTo(new int[] { 263, 775, 1799 }));
            Assert.That((result.Scalability ?? throw new InvalidOperationException("Invalid internal state.")).SpatialLayerMaximumWidths,
                Is.EqualTo(new int[] { 640, 1280, 1920 }));
            Assert.That(result.Scalability.SpatialLayerMaximumHeights,
                Is.EqualTo(new int[] { 360, 720, 1080 }));
            Assert.That(result.Scalability.SpatialLayerReferenceIds,
                Is.EqualTo(new int[] { 0, 0, 1 }));
            Assert.That(GetTemporalIds(result.LayerFrames),
                Is.EqualTo(new int[]
                {
                    0, 0, 0,
                    2, 2, 2,
                    1, 1, 1,
                    2, 2, 2
                }));
            Assert.That(GetOutputs(result.LayerFrames),
                Is.EqualTo(new bool[]
                {
                    true, true, true,
                    true, true, true,
                    true, true, true,
                    true, true, true
                }));
            Assert.That(parsed.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(((parsed.Metadata ?? throw new InvalidOperationException("Invalid internal state.")).Scalability ?? throw new InvalidOperationException("Invalid internal state.")).SpatialLayerMaximumWidths,
                Is.EqualTo(new int[] { 640, 1280, 1920 }));
        });
    }

    [Test]
    public void NonProgressiveAndInvalidPlansUseHighestLayerOrFailCleanly()
    {
        Av1EncoderTileLayerPlanRequest display = CreateRequest(1280, 720);
        display.FrameCount = 2;
        display.SpatialLayers = new Av1EncoderSpatialLayer[]
        {
            new Av1EncoderSpatialLayer(640, 360),
            new Av1EncoderSpatialLayer(1280, 720)
        };
        Av1EncoderTileLayerPlan displayPlan =
            Av1EncoderTileLayerPlanner.Plan(display);

        Av1EncoderTileLayerPlanRequest badTiles = CreateRequest(9000, 5000);
        badTiles.Options.AutoTiling = false;
        Av1EncoderTileLayerPlanRequest badLayers = CreateRequest(1280, 720);
        badLayers.SpatialLayers = new Av1EncoderSpatialLayer[]
        {
            new Av1EncoderSpatialLayer(1280, 720),
            new Av1EncoderSpatialLayer(640, 360)
        };

        Assert.Multiple(() =>
        {
            Assert.That(GetOutputs(displayPlan.LayerFrames),
                Is.EqualTo(new bool[] { false, true, false, true }));
            Assert.That(Av1EncoderTileLayerPlanner.Plan(badTiles).Code,
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(Av1EncoderTileLayerPlanner.Plan(badLayers).Code,
                Is.EqualTo(AvifResultCode.InvalidArgument));
        });
    }

    static Av1EncoderTileLayerPlanRequest CreateRequest(int width, int height)
    {
        Av1EncoderTileLayerPlanRequest request =
            new Av1EncoderTileLayerPlanRequest();
        request.Width = width;
        request.Height = height;
        return request;
    }

    static int[] GetWorkers(Av1EncoderTileJob[] jobs)
    {
        int[] values = new int[jobs.Length];
        int index = 0;
        while (index < jobs.Length)
        {
            values[index] = jobs[index].WorkerIndex;
            index++;
        }
        return values;
    }

    static int[] GetDependencies(Av1EncoderTileJob[] jobs)
    {
        int[] values = new int[jobs.Length];
        int index = 0;
        while (index < jobs.Length)
        {
            values[index] = jobs[index].DependencyTileIndex;
            index++;
        }
        return values;
    }

    static int[] GetContexts(Av1EncoderTileJob[] jobs)
    {
        int[] values = new int[jobs.Length];
        int index = 0;
        while (index < jobs.Length)
        {
            values[index] = jobs[index].EntropyContextId;
            index++;
        }
        return values;
    }

    static int MaximumWidth(Av1EncoderTileJob[] jobs)
    {
        int maximum = 0;
        int index = 0;
        while (index < jobs.Length)
        {
            if (jobs[index].Width > maximum)
            {
                maximum = jobs[index].Width;
            }
            index++;
        }
        return maximum;
    }

    static long MaximumArea(Av1EncoderTileJob[] jobs)
    {
        long maximum = 0;
        int index = 0;
        while (index < jobs.Length)
        {
            long area = (long)jobs[index].Width * jobs[index].Height;
            if (area > maximum)
            {
                maximum = area;
            }
            index++;
        }
        return maximum;
    }

    static int[] GetOperatingPointIdcs(Av1OperatingPoint[] points)
    {
        int[] values = new int[points.Length];
        int index = 0;
        while (index < points.Length)
        {
            values[index] = points[index].Idc;
            index++;
        }
        return values;
    }

    static int[] GetTemporalIds(Av1EncoderLayerFramePlan[] frames)
    {
        int[] values = new int[frames.Length];
        int index = 0;
        while (index < frames.Length)
        {
            values[index] = frames[index].TemporalId;
            index++;
        }
        return values;
    }

    static bool[] GetOutputs(Av1EncoderLayerFramePlan[] frames)
    {
        bool[] values = new bool[frames.Length];
        int index = 0;
        while (index < frames.Length)
        {
            values[index] = frames[index].Output;
            index++;
        }
        return values;
    }
}
