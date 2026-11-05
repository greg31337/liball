using System;

namespace avif;

internal class Av1EncoderSpatialLayer
{
    internal int Width;
    internal int Height;

    internal Av1EncoderSpatialLayer(int width, int height)
    {
        Width = width;
        Height = height;
    }
}

internal class Av1EncoderTileLayerPlanRequest
{
    internal Av1EncoderOptions Options;
    internal int Width;
    internal int Height;
    internal int SuperblockSize;
    internal int FrameCount;
    internal int TemporalLayerCount;
    internal Av1EncoderSpatialLayer[] SpatialLayers;
    internal bool Progressive;

    internal Av1EncoderTileLayerPlanRequest()
    {
        Options = new Av1EncoderOptions();
        SuperblockSize = Av1SyntaxConstants.MinSuperblockSize;
        FrameCount = 1;
        TemporalLayerCount = 1;
        SpatialLayers = System.Array.Empty<Av1EncoderSpatialLayer>();
    }
}

internal class Av1EncoderTileJob
{
    internal int TileIndex;
    internal int TileRow;
    internal int TileColumn;
    internal int MiRowStart;
    internal int MiRowEnd;
    internal int MiColumnStart;
    internal int MiColumnEnd;
    internal int X;
    internal int Y;
    internal int Width;
    internal int Height;
    internal int WorkerIndex;
    internal int EntropyContextId;
    internal int DependencyTileIndex;

    internal Av1EncoderTileJob()
    {
        DependencyTileIndex = -1;
    }
}

internal class Av1EncoderLayerFramePlan
{
    internal int FrameIndex;
    internal int TemporalId;
    internal int SpatialId;
    internal int Width;
    internal int Height;
    internal int OperatingPointIndex;
    internal int ReferenceSpatialId;
    internal bool Output;

    internal Av1EncoderLayerFramePlan()
    {
        ReferenceSpatialId = -1;
    }
}

internal class Av1EncoderTileLayerPlan
{
    internal AvifResultCode Code;
    internal Av1TileInfo? Tiles;
    internal Av1EncoderTileJob[] TileJobs;
    internal int WorkerCount;
    internal Av1OperatingPoint[] OperatingPoints;
    internal Av1ScalabilityMetadata? Scalability;
    internal Av1EncoderLayerFramePlan[] LayerFrames;

    internal Av1EncoderTileLayerPlan(AvifResultCode code)
    {
        Code = code;
        TileJobs = System.Array.Empty<Av1EncoderTileJob>();
        OperatingPoints = System.Array.Empty<Av1OperatingPoint>();
        LayerFrames = System.Array.Empty<Av1EncoderLayerFramePlan>();
    }
}

internal static class Av1EncoderTileLayerPlanner
{
    internal static Av1EncoderTileLayerPlan Plan(
        Av1EncoderTileLayerPlanRequest request)
    {
        AvifResultCode validation = Validate(request);
        Av1EncoderTileLayerPlan result =
            new Av1EncoderTileLayerPlan(validation);
        if (validation != AvifResultCode.Success)
        {
            return result;
        }

        int[] tileLogs = request.Options.AutoTiling
            ? ChooseAutomaticTileLogs(request)
            : new int[]
            {
                request.Options.TileColumnsLog2,
                request.Options.TileRowsLog2
            };
        if (tileLogs.Length == 0)
        {
            result.Code = AvifResultCode.LimitExceeded;
            return result;
        }
        result.Tiles = CreateTileInfo(request, tileLogs[0], tileLogs[1]);
        int tileCount = result.Tiles.TileColumns * result.Tiles.TileRows;
        if (tileCount > request.Options.Limits.MaxTileCount)
        {
            result.Code = AvifResultCode.LimitExceeded;
            result.Tiles = null;
            return result;
        }
        result.WorkerCount = request.Options.WorkerCount;
        if (result.WorkerCount > request.Options.Limits.MaxWorkerCount)
        {
            result.WorkerCount = request.Options.Limits.MaxWorkerCount;
        }
        if (result.WorkerCount > tileCount)
        {
            result.WorkerCount = tileCount;
        }
        result.TileJobs = CreateTileJobs(
            request,
            result.Tiles,
            result.WorkerCount);

        Av1EncoderSpatialLayer[] layers = GetSpatialLayers(request);
        int temporalLayerCount = GetTemporalLayerCount(request);
        result.OperatingPoints = CreateOperatingPoints(
            layers.Length,
            temporalLayerCount);
        result.Scalability = CreateScalability(layers);
        result.LayerFrames = CreateLayerFrames(
            request.FrameCount,
            temporalLayerCount,
            layers,
            request.Progressive || request.Options.ProgressiveOutput);
        return result;
    }

    static int[] ChooseAutomaticTileLogs(
        Av1EncoderTileLayerPlanRequest request)
    {
        int superblockColumns = DivideCeiling(
            request.Width,
            request.SuperblockSize);
        int superblockRows = DivideCeiling(
            request.Height,
            request.SuperblockSize);
        int columnLog2 = 0;
        int rowLog2 = 0;
        while (columnLog2 < 6
            && DivideCeiling(request.Width, 1 << columnLog2)
                > Av1SyntaxConstants.MaxTileWidth)
        {
            columnLog2++;
        }
        while (rowLog2 < 6
            && (long)DivideCeiling(request.Width, 1 << columnLog2)
                * DivideCeiling(request.Height, 1 << rowLog2)
                > Av1SyntaxConstants.MaxTileArea)
        {
            rowLog2++;
        }
        if (1 << columnLog2 > superblockColumns
            || 1 << rowLog2 > superblockRows)
        {
            return System.Array.Empty<int>();
        }

        int desiredWorkers = request.Options.WorkerCount;
        if (desiredWorkers > request.Options.Limits.MaxWorkerCount)
        {
            desiredWorkers = request.Options.Limits.MaxWorkerCount;
        }
        while ((1 << columnLog2) * (1 << rowLog2) < desiredWorkers)
        {
            bool canSplitColumns = columnLog2 < 6
                && 1 << (columnLog2 + 1) <= superblockColumns;
            bool canSplitRows = rowLog2 < 6
                && 1 << (rowLog2 + 1) <= superblockRows;
            if (!canSplitColumns && !canSplitRows)
            {
                break;
            }
            int tileWidth = DivideCeiling(request.Width, 1 << columnLog2);
            int tileHeight = DivideCeiling(request.Height, 1 << rowLog2);
            if (canSplitColumns && (!canSplitRows || tileWidth >= tileHeight))
            {
                columnLog2++;
            }
            else
            {
                rowLog2++;
            }
        }
        return new int[] { columnLog2, rowLog2 };
    }

    static Av1TileInfo CreateTileInfo(
        Av1EncoderTileLayerPlanRequest request,
        int columnLog2,
        int rowLog2)
    {
        Av1TileInfo tiles = new Av1TileInfo();
        tiles.UniformSpacing = true;
        tiles.TileColumnsLog2 = columnLog2;
        tiles.TileRowsLog2 = rowLog2;
        tiles.TileColumns = 1 << columnLog2;
        tiles.TileRows = 1 << rowLog2;
        tiles.MiColumnStarts = CreateMiStarts(
            request.Width,
            request.SuperblockSize,
            tiles.TileColumns);
        tiles.MiRowStarts = CreateMiStarts(
            request.Height,
            request.SuperblockSize,
            tiles.TileRows);
        tiles.ContextUpdateTileId = 0;
        tiles.TileSizeBytes = tiles.TileColumns * tiles.TileRows == 1 ? 0 : 4;
        return tiles;
    }

    static int[] CreateMiStarts(
        int pixels,
        int superblockSize,
        int tileCount)
    {
        int superblocks = DivideCeiling(pixels, superblockSize);
        int miSize = superblockSize / Av1SyntaxConstants.MiSize;
        int[] starts = new int[tileCount + 1];
        int tile = 0;
        while (tile < tileCount)
        {
            starts[tile] = tile * superblocks / tileCount * miSize;
            tile++;
        }
        starts[tileCount] = DivideCeiling(
            pixels,
            Av1SyntaxConstants.MiSize);
        return starts;
    }

    static Av1EncoderTileJob[] CreateTileJobs(
        Av1EncoderTileLayerPlanRequest request,
        Av1TileInfo tiles,
        int workerCount)
    {
        Av1EncoderTileJob[] jobs =
            new Av1EncoderTileJob[tiles.TileColumns * tiles.TileRows];
        int tile = 0;
        while (tile < jobs.Length)
        {
            int row = tile / tiles.TileColumns;
            int column = tile - row * tiles.TileColumns;
            Av1EncoderTileJob job = new Av1EncoderTileJob();
            job.TileIndex = tile;
            job.TileRow = row;
            job.TileColumn = column;
            job.MiRowStart = (tiles.MiRowStarts ?? throw new InvalidOperationException("Invalid internal state."))[row];
            job.MiRowEnd = tiles.MiRowStarts[row + 1];
            job.MiColumnStart = (tiles.MiColumnStarts ?? throw new InvalidOperationException("Invalid internal state."))[column];
            job.MiColumnEnd = tiles.MiColumnStarts[column + 1];
            job.X = job.MiColumnStart * Av1SyntaxConstants.MiSize;
            job.Y = job.MiRowStart * Av1SyntaxConstants.MiSize;
            int pixelEndX = job.MiColumnEnd * Av1SyntaxConstants.MiSize;
            int pixelEndY = job.MiRowEnd * Av1SyntaxConstants.MiSize;
            if (pixelEndX > request.Width)
            {
                pixelEndX = request.Width;
            }
            if (pixelEndY > request.Height)
            {
                pixelEndY = request.Height;
            }
            job.Width = pixelEndX - job.X;
            job.Height = pixelEndY - job.Y;
            job.WorkerIndex = tile % workerCount;
            job.EntropyContextId = tile;
            jobs[tile] = job;
            tile++;
        }
        return jobs;
    }

    static Av1EncoderSpatialLayer[] GetSpatialLayers(
        Av1EncoderTileLayerPlanRequest request)
    {
        if (request.SpatialLayers.Length != 0)
        {
            return request.SpatialLayers;
        }
        if (request.Options.SpatialLayers.Length != 0)
        {
            Av1EncoderSpatialLayer[] configured =
                new Av1EncoderSpatialLayer[request.Options.SpatialLayers.Length];
            int index = 0;
            while (index < configured.Length)
            {
                configured[index] = new Av1EncoderSpatialLayer(
                    request.Options.SpatialLayers[index].Width,
                    request.Options.SpatialLayers[index].Height);
                index++;
            }
            return configured;
        }
        return new Av1EncoderSpatialLayer[]
        {
            new Av1EncoderSpatialLayer(request.Width, request.Height)
        };
    }

    static int GetTemporalLayerCount(
        Av1EncoderTileLayerPlanRequest request)
    {
        return request.TemporalLayerCount == 1
            ? request.Options.TemporalLayerCount
            : request.TemporalLayerCount;
    }

    static Av1OperatingPoint[] CreateOperatingPoints(
        int spatialLayerCount,
        int temporalLayerCount)
    {
        Av1OperatingPoint[] points =
            new Av1OperatingPoint[spatialLayerCount];
        int temporalMask = (1 << temporalLayerCount) - 1;
        int spatial = 0;
        while (spatial < spatialLayerCount)
        {
            int spatialMask = (1 << (spatial + 1)) - 1;
            int idc = spatialLayerCount == 1 && temporalLayerCount == 1
                ? 0
                : temporalMask | spatialMask << 8;
            points[spatial] = new Av1OperatingPoint(idc, 0, 0);
            spatial++;
        }
        return points;
    }

    static Av1ScalabilityMetadata CreateScalability(
        Av1EncoderSpatialLayer[] layers)
    {
        Av1ScalabilityMetadata scalability = new Av1ScalabilityMetadata();
        scalability.ModeIdc = 14;
        scalability.SpatialLayerCount = layers.Length;
        scalability.SpatialLayerDimensionsPresent = true;
        scalability.SpatialLayerDescriptionsPresent = layers.Length > 1;
        scalability.SpatialLayerMaximumWidths = new int[layers.Length];
        scalability.SpatialLayerMaximumHeights = new int[layers.Length];
        scalability.SpatialLayerReferenceIds = layers.Length > 1
            ? new int[layers.Length]
            : System.Array.Empty<int>();
        int index = 0;
        while (index < layers.Length)
        {
            scalability.SpatialLayerMaximumWidths[index] = layers[index].Width;
            scalability.SpatialLayerMaximumHeights[index] = layers[index].Height;
            if (layers.Length > 1)
            {
                scalability.SpatialLayerReferenceIds[index] = index == 0
                    ? 0
                    : index - 1;
            }
            index++;
        }
        return scalability;
    }

    static Av1EncoderLayerFramePlan[] CreateLayerFrames(
        int frameCount,
        int temporalLayerCount,
        Av1EncoderSpatialLayer[] layers,
        bool progressive)
    {
        Av1EncoderLayerFramePlan[] frames =
            new Av1EncoderLayerFramePlan[frameCount * layers.Length];
        int frame = 0;
        while (frame < frameCount)
        {
            int temporalId = GetTemporalId(frame, temporalLayerCount);
            int spatial = 0;
            while (spatial < layers.Length)
            {
                int index = frame * layers.Length + spatial;
                Av1EncoderLayerFramePlan plan =
                    new Av1EncoderLayerFramePlan();
                plan.FrameIndex = frame;
                plan.TemporalId = temporalId;
                plan.SpatialId = spatial;
                plan.Width = layers[spatial].Width;
                plan.Height = layers[spatial].Height;
                plan.OperatingPointIndex = spatial;
                plan.ReferenceSpatialId = spatial - 1;
                plan.Output = progressive || spatial == layers.Length - 1;
                frames[index] = plan;
                spatial++;
            }
            frame++;
        }
        return frames;
    }

    static int GetTemporalId(int frameIndex, int temporalLayerCount)
    {
        if (temporalLayerCount == 1 || frameIndex == 0)
        {
            return 0;
        }
        int zeros = 0;
        int value = frameIndex;
        while ((value & 1) == 0 && zeros < temporalLayerCount - 1)
        {
            zeros++;
            value >>= 1;
        }
        return temporalLayerCount - 1 - zeros;
    }

    static int DivideCeiling(int value, int divisor)
    {
        return value / divisor + (value % divisor == 0 ? 0 : 1);
    }

    static AvifResultCode Validate(
        Av1EncoderTileLayerPlanRequest request)
    {
        if (request == null || request.Options == null
            || request.Options.Limits == null
            || request.Width <= 0 || request.Height <= 0
            || request.Width > request.Options.Limits.MaxWidth
            || request.Height > request.Options.Limits.MaxHeight
            || (long)request.Width * request.Height
                > request.Options.Limits.MaxPixels
            || request.SuperblockSize != Av1SyntaxConstants.MinSuperblockSize
                && request.SuperblockSize
                    != Av1SyntaxConstants.MaxSuperblockSize
            || request.FrameCount < 1
            || request.FrameCount > request.Options.Limits.MaxFrameCount
            || GetTemporalLayerCount(request) < 1
            || GetTemporalLayerCount(request)
                > Av1SyntaxConstants.MaxTemporalLayers
            || request.SpatialLayers == null
            || request.SpatialLayers.Length
                >= Av1SyntaxConstants.MaxSpatialLayers + 1
            || request.Options.SpatialLayers == null
            || request.Options.SpatialLayers.Length
                >= Av1SyntaxConstants.MaxSpatialLayers + 1
            || request.Options.WorkerCount <= 0
            || request.Options.Limits.MaxWorkerCount <= 0
            || request.Options.Limits.MaxTileCount <= 0
            || !request.Options.AutoTiling
                && (request.Options.TileColumnsLog2 < 0
                    || request.Options.TileColumnsLog2 > 6
                    || request.Options.TileRowsLog2 < 0
                    || request.Options.TileRowsLog2 > 6))
        {
            return AvifResultCode.InvalidArgument;
        }
        int spatial = 0;
        int previousWidth = 0;
        int previousHeight = 0;
        Av1EncoderSpatialLayer[] selectedLayers = GetSpatialLayers(request);
        while (spatial < selectedLayers.Length)
        {
            Av1EncoderSpatialLayer layer = selectedLayers[spatial];
            if (layer == null || layer.Width <= previousWidth
                || layer.Height <= previousHeight
                || layer.Width > request.Width || layer.Height > request.Height)
            {
                return AvifResultCode.InvalidArgument;
            }
            previousWidth = layer.Width;
            previousHeight = layer.Height;
            spatial++;
        }
        if (previousWidth != request.Width
            || previousHeight != request.Height)
        {
            return AvifResultCode.InvalidArgument;
        }
        if (!request.Options.AutoTiling)
        {
            int columns = 1 << request.Options.TileColumnsLog2;
            int rows = 1 << request.Options.TileRowsLog2;
            int superblockColumns = DivideCeiling(
                request.Width,
                request.SuperblockSize);
            int superblockRows = DivideCeiling(
                request.Height,
                request.SuperblockSize);
            if (columns > superblockColumns || rows > superblockRows
                || columns * rows > request.Options.Limits.MaxTileCount
                || DivideCeiling(request.Width, columns)
                    > Av1SyntaxConstants.MaxTileWidth
                || (long)DivideCeiling(request.Width, columns)
                    * DivideCeiling(request.Height, rows)
                    > Av1SyntaxConstants.MaxTileArea)
            {
                return AvifResultCode.InvalidArgument;
            }
        }
        return AvifResultCode.Success;
    }
}
