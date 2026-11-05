using System;

namespace avif;

internal class Av1LargeScaleTileEntry
{
    internal int AnchorFrameIndex;
    internal int AnchorTileRow;
    internal int AnchorTileColumn;
    internal int DataOffset;
    internal int DataSize;
    internal Av1LargeScaleTileList? List;

    internal ReadOnlySpan<byte> GetData()
    {
        if (List == null || List.Closed || DataOffset < 0 || DataSize < 0
            || DataOffset > List.Payload.Length - DataSize)
        {
            return ReadOnlySpan<byte>.Empty;
        }
        return List.Payload.AsSpan(DataOffset, DataSize);
    }
}

internal class Av1LargeScaleTileList
{
    internal int OutputTileColumns;
    internal int OutputTileRows;
    internal Av1LargeScaleTileEntry[] Entries;
    internal byte[] Payload;
    internal ResourceGuard Resources;
    internal bool Closed;

    internal Av1LargeScaleTileList(
        int outputTileColumns,
        int outputTileRows,
        Av1LargeScaleTileEntry[] entries,
        byte[] payload,
        ResourceGuard resources)
    {
        OutputTileColumns = outputTileColumns;
        OutputTileRows = outputTileRows;
        Entries = entries;
        Payload = payload;
        Resources = resources;
        int index = 0;
        while (index < entries.Length)
        {
            entries[index].List = this;
            index++;
        }
    }

    internal AvifResultCode Close()
    {
        if (Closed)
        {
            return AvifResultCode.Success;
        }
        Closed = true;
        long length = Payload.Length;
        int count = Entries.Length;
        Payload = System.Array.Empty<byte>();
        Entries = System.Array.Empty<Av1LargeScaleTileEntry>();
        AvifResultCode tileCode = Resources.TileCount.Release(count);
        AvifResultCode byteCode = Resources.TemporaryBytes.Release(length);
        return tileCode != AvifResultCode.Success ? tileCode : byteCode;
    }
}

internal class Av1LargeScaleTileListParseResult
{
    internal AvifResultCode Code;
    internal Av1LargeScaleTileList? List;

    internal Av1LargeScaleTileListParseResult(
        AvifResultCode code,
        Av1LargeScaleTileList? list)
    {
        Code = code;
        List = list;
    }
}

internal class Av1LargeScaleTileOutputRequest
{
    internal ParsedAv1SequenceHeader? Sequence;
    internal ParsedAv1FrameHeader? Header;
    internal Av1LargeScaleTileList? List;
    internal PlaneBuffer[][] DecodedTiles;
    internal PlaneBuffer[] DestinationPlanes;

    internal Av1LargeScaleTileOutputRequest()
    {
        DecodedTiles = System.Array.Empty<PlaneBuffer[]>();
        DestinationPlanes = System.Array.Empty<PlaneBuffer>();
    }
}

// AV1 specification sections 5.11.2, 6.10.2, and 7.3.
internal static class Av1LargeScaleTiles
{
    internal static Av1LargeScaleTileListParseResult Parse(
        byte[] payload,
        ParsedAv1SequenceHeader sequence,
        ParsedAv1FrameHeader header,
        ResourceGuard resources,
        Av1LayerSelection selection)
    {
        if (selection == null || selection.Code != AvifResultCode.Success)
        {
            return Fail(AvifResultCode.InvalidArgument);
        }
        if (!selection.LargeScaleTiles)
        {
            return Fail(AvifResultCode.UnsupportedFeature);
        }
        if (payload == null || resources == null
            || resources.Code != AvifResultCode.Success)
        {
            return Fail(AvifResultCode.InvalidArgument);
        }
        AvifResultCode configuration = ValidateConfiguration(sequence, header);
        if (configuration != AvifResultCode.Success)
        {
            return Fail(configuration);
        }
        if (payload.Length < 10)
        {
            return Fail(AvifResultCode.TruncatedData);
        }

        int outputColumns = payload[0] + 1;
        int outputRows = payload[1] + 1;
        int entryCount = (payload[2] << 8 | payload[3]) + 1;
        if (entryCount > 512
            || entryCount > outputColumns * outputRows)
        {
            return Fail(AvifResultCode.InvalidAv1Bitstream);
        }
        if (!resources.TileCount.CanAdd(entryCount)
            || !resources.TemporaryBytes.CanAdd(payload.Length))
        {
            return Fail(AvifResultCode.LimitExceeded);
        }

        int tileWidth = GetTileWidth(header);
        int tileHeight = GetTileHeight(header);
        long outputWidth = (long)outputColumns * tileWidth;
        long outputHeight = (long)outputRows * tileHeight;
        if (outputWidth > int.MaxValue || outputHeight > int.MaxValue)
        {
            return Fail(AvifResultCode.LimitExceeded);
        }
        AvifResultCode dimensions = resources.CheckImageDimensions(
            (int)outputWidth, (int)outputHeight);
        if (dimensions != AvifResultCode.Success)
        {
            return Fail(dimensions);
        }

        Av1LargeScaleTileEntry[] entries =
            new Av1LargeScaleTileEntry[entryCount];
        int cursor = 4;
        int index = 0;
        while (index < entryCount)
        {
            if (cursor > payload.Length - 5)
            {
                return Fail(AvifResultCode.TruncatedData);
            }
            Av1LargeScaleTileEntry entry = new Av1LargeScaleTileEntry();
            entry.AnchorFrameIndex = payload[cursor];
            entry.AnchorTileRow = payload[cursor + 1];
            entry.AnchorTileColumn = payload[cursor + 2];
            int dataSize = (payload[cursor + 3] << 8
                | payload[cursor + 4]) + 1;
            cursor += 5;
            if (entry.AnchorFrameIndex > 127
                || entry.AnchorTileRow >= (header.Tiles ?? throw new InvalidOperationException("Invalid internal state.")).TileRows
                || entry.AnchorTileColumn >= header.Tiles.TileColumns)
            {
                return Fail(AvifResultCode.InvalidAv1Bitstream);
            }
            if (cursor > payload.Length - dataSize)
            {
                return Fail(AvifResultCode.TruncatedData);
            }
            entry.DataOffset = cursor;
            entry.DataSize = dataSize;
            entries[index] = entry;
            cursor += dataSize;
            index++;
        }
        if (cursor != payload.Length)
        {
            return Fail(AvifResultCode.InvalidAv1Bitstream);
        }

        resources.TileCount.AddUnchecked(entryCount);
        resources.TemporaryBytes.AddUnchecked(payload.Length);
        Av1LargeScaleTileList list = new Av1LargeScaleTileList(
            outputColumns,
            outputRows,
            entries,
            (byte[])payload.Clone(),
            resources);
        return new Av1LargeScaleTileListParseResult(
            AvifResultCode.Success, list);
    }

    internal static AvifResultCode Compose(
        Av1LargeScaleTileOutputRequest request)
    {
        AvifResultCode validation = ValidateOutput(request);
        if (validation != AvifResultCode.Success)
        {
            return validation;
        }

        int[][][] samples = SnapshotTiles(request);
        int tileWidth = GetTileWidth(request.Header);
        int tileHeight = GetTileHeight(request.Header);
        int tile = 0;
        while (tile < request.DecodedTiles.Length)
        {
            int destinationTileX = tile % (request.List ?? throw new InvalidOperationException("Invalid internal state.")).OutputTileColumns;
            int destinationTileY = tile / request.List.OutputTileColumns;
            int plane = 0;
            while (plane < 3)
            {
                int subX = plane == 0
                    || !(request.Sequence ?? throw new InvalidOperationException("Invalid internal state.")).HasChromaSubsamplingX() ? 0 : 1;
                int subY = plane == 0
                    || !(request.Sequence ?? throw new InvalidOperationException("Invalid internal state.")).HasChromaSubsamplingY() ? 0 : 1;
                int width = tileWidth >> subX;
                int height = tileHeight >> subY;
                int destinationX = destinationTileX * width;
                int destinationY = destinationTileY * height;
                int y = 0;
                while (y < height)
                {
                    int x = 0;
                    while (x < width)
                    {
                        WriteSample(
                            request.DestinationPlanes[plane],
                            destinationX + x,
                            destinationY + y,
                            samples[tile][plane][y * width + x]);
                        x++;
                    }
                    y++;
                }
                plane++;
            }
            tile++;
        }
        return AvifResultCode.Success;
    }

    internal static AvifResultCode ValidateConfiguration(
        ParsedAv1SequenceHeader sequence,
        ParsedAv1FrameHeader header)
    {
        if (sequence == null || header == null || header.Size == null
            || header.Tiles == null || header.Quantization == null
            || header.Segmentation == null || header.LoopFilter == null
            || header.LoopFilter.Levels == null
            || header.LoopFilter.Levels.Length < 2
            || header.Tiles.MiColumnStarts == null
            || header.Tiles.MiRowStarts == null
            || header.Tiles.TileColumns < 1 || header.Tiles.TileRows < 1
            || header.Tiles.MiColumnStarts.Length
                != header.Tiles.TileColumns + 1
            || header.Tiles.MiRowStarts.Length
                != header.Tiles.TileRows + 1)
        {
            return AvifResultCode.InvalidArgument;
        }

        if (sequence.IsSuperresEnabled() || sequence.IsOrderHintEnabled()
            || sequence.IsStillPicture()
            || sequence.HasFilmGrainParameters()
            || sequence.HasTimingInfo() || sequence.HasDecoderModelInfo()
            || sequence.HasInitialDisplayDelay()
            || sequence.IsRestorationEnabled() || sequence.IsCdefEnabled()
            || sequence.IsMonochrome()
            || header.ShowExistingFrame
            || header.FrameType != Av1FrameType.Inter
            || !header.ShowFrame || header.ErrorResilientMode
            || !header.DisableCdfUpdate || !header.DisableFrameEndUpdateCdf
            || header.Quantization.DeltaLoopFilterPresent
            || header.Quantization.DeltaQPresent
            || header.FrameSizeOverride || header.RefreshFrameFlags != 0
            || header.UseReferenceFrameMotionVectors
            || header.Segmentation.TemporalUpdate
            || header.ReferenceSelect
            || header.LoopFilter.Levels[0] != 0
            || header.LoopFilter.Levels[1] != 0)
        {
            return AvifResultCode.NonConformingData;
        }
        if (header.Size.FrameWidth != (long)header.Size.MiColumns * 4
            || header.Size.FrameHeight != (long)header.Size.MiRows * 4
            || header.Size.UpscaledWidth != header.Size.FrameWidth
            || header.Tiles.MiColumnStarts[header.Tiles.TileColumns]
                != header.Size.MiColumns
            || header.Tiles.MiRowStarts[header.Tiles.TileRows]
                != header.Size.MiRows
            || !HasUniformCameraTiles(sequence, header.Tiles))
        {
            return AvifResultCode.NonConformingData;
        }
        return AvifResultCode.Success;
    }

    static bool HasUniformCameraTiles(
        ParsedAv1SequenceHeader sequence,
        Av1TileInfo tiles)
    {
        int tileHeight = sequence.Uses128x128Superblocks() ? 128 : 64;
        int expectedWidth = -1;
        if ((tiles.MiColumnStarts ?? throw new InvalidOperationException("Invalid internal state."))[0] != 0
            || (tiles.MiRowStarts ?? throw new InvalidOperationException("Invalid internal state."))[0] != 0)
        {
            return false;
        }
        int row = 0;
        while (row < tiles.TileRows)
        {
            int height = (tiles.MiRowStarts[row + 1]
                - tiles.MiRowStarts[row]) * 4;
            if (height != tileHeight)
            {
                return false;
            }
            row++;
        }
        int column = 0;
        while (column < tiles.TileColumns)
        {
            int width = (tiles.MiColumnStarts[column + 1]
                - tiles.MiColumnStarts[column]) * 4;
            if (width <= 0 || width % tileHeight != 0
                || expectedWidth >= 0 && width != expectedWidth)
            {
                return false;
            }
            expectedWidth = width;
            column++;
        }
        return true;
    }

    static AvifResultCode ValidateOutput(
        Av1LargeScaleTileOutputRequest request)
    {
        if (request == null || request.Sequence == null
            || request.Header == null || request.List == null
            || request.List.Closed || request.DecodedTiles == null
            || request.DestinationPlanes == null
            || request.List.Entries == null
            || request.List.OutputTileColumns <= 0
            || request.List.OutputTileRows <= 0
            || request.DecodedTiles.Length != request.List.Entries.Length
            || request.DestinationPlanes.Length != 3)
        {
            return AvifResultCode.InvalidArgument;
        }
        AvifResultCode configuration = ValidateConfiguration(
            request.Sequence, request.Header);
        if (configuration != AvifResultCode.Success)
        {
            return configuration;
        }

        int tileWidth = GetTileWidth(request.Header);
        int tileHeight = GetTileHeight(request.Header);
        long outputWidth = (long)request.List.OutputTileColumns * tileWidth;
        long outputHeight = (long)request.List.OutputTileRows * tileHeight;
        if (outputWidth > int.MaxValue || outputHeight > int.MaxValue)
        {
            return AvifResultCode.LimitExceeded;
        }
        int bytesPerSample = request.Sequence.GetBitDepth() > 8 ? 2 : 1;
        int plane = 0;
        while (plane < 3)
        {
            PlaneBuffer destination = request.DestinationPlanes[plane];
            if (destination == null)
            {
                return AvifResultCode.InvalidArgument;
            }
            AvifResultCode state = destination.GetStateCode();
            if (state != AvifResultCode.Success)
            {
                return state;
            }
            int subX = plane == 0
                || !request.Sequence.HasChromaSubsamplingX() ? 0 : 1;
            int subY = plane == 0
                || !request.Sequence.HasChromaSubsamplingY() ? 0 : 1;
            int width = request.List.OutputTileColumns * (tileWidth >> subX);
            int height = request.List.OutputTileRows * (tileHeight >> subY);
            if (destination.Width < width || destination.Height < height
                || destination.BytesPerSample != bytesPerSample)
            {
                return AvifResultCode.InvalidArgument;
            }
            plane++;
        }

        int tile = 0;
        while (tile < request.DecodedTiles.Length)
        {
            if (request.DecodedTiles[tile] == null
                || request.DecodedTiles[tile].Length != 3)
            {
                return AvifResultCode.InvalidArgument;
            }
            plane = 0;
            while (plane < 3)
            {
                PlaneBuffer source = request.DecodedTiles[tile][plane];
                if (source == null)
                {
                    return AvifResultCode.InvalidArgument;
                }
                AvifResultCode state = source.GetStateCode();
                if (state != AvifResultCode.Success)
                {
                    return state;
                }
                int subX = plane == 0
                    || !request.Sequence.HasChromaSubsamplingX() ? 0 : 1;
                int subY = plane == 0
                    || !request.Sequence.HasChromaSubsamplingY() ? 0 : 1;
                if (source.Width < (tileWidth >> subX)
                    || source.Height < (tileHeight >> subY)
                    || source.BytesPerSample != bytesPerSample)
                {
                    return AvifResultCode.InvalidArgument;
                }
                plane++;
            }
            tile++;
        }
        return AvifResultCode.Success;
    }

    static int[][][] SnapshotTiles(
        Av1LargeScaleTileOutputRequest request)
    {
        int tileWidth = GetTileWidth(request.Header);
        int tileHeight = GetTileHeight(request.Header);
        int[][][] samples = new int[request.DecodedTiles.Length][][];
        int tile = 0;
        while (tile < samples.Length)
        {
            samples[tile] = new int[3][];
            int plane = 0;
            while (plane < 3)
            {
                int subX = plane == 0
                    || !(request.Sequence ?? throw new InvalidOperationException("Invalid internal state.")).HasChromaSubsamplingX() ? 0 : 1;
                int subY = plane == 0
                    || !(request.Sequence ?? throw new InvalidOperationException("Invalid internal state.")).HasChromaSubsamplingY() ? 0 : 1;
                int width = tileWidth >> subX;
                int height = tileHeight >> subY;
                samples[tile][plane] = new int[width * height];
                int y = 0;
                while (y < height)
                {
                    int x = 0;
                    while (x < width)
                    {
                        samples[tile][plane][y * width + x] = ReadSample(
                            request.DecodedTiles[tile][plane], x, y);
                        x++;
                    }
                    y++;
                }
                plane++;
            }
            tile++;
        }
        return samples;
    }

    static int GetTileWidth(ParsedAv1FrameHeader? header)
    {
        return ((((header ?? throw new InvalidOperationException("Invalid internal state.")).Tiles ?? throw new InvalidOperationException("Invalid internal state.")).MiColumnStarts ?? throw new InvalidOperationException("Invalid internal state."))[1]
            - ((header.Tiles ?? throw new InvalidOperationException("Invalid internal state.")).MiColumnStarts ?? throw new InvalidOperationException("Invalid internal state."))[0]) * 4;
    }

    static int GetTileHeight(ParsedAv1FrameHeader? header)
    {
        return ((((header ?? throw new InvalidOperationException("Invalid internal state.")).Tiles ?? throw new InvalidOperationException("Invalid internal state.")).MiRowStarts ?? throw new InvalidOperationException("Invalid internal state."))[1]
            - ((header.Tiles ?? throw new InvalidOperationException("Invalid internal state.")).MiRowStarts ?? throw new InvalidOperationException("Invalid internal state."))[0]) * 4;
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
        row[offset] = IntegerConversion.ToByte(value);
        if (plane.BytesPerSample == 2)
        {
            row[offset + 1] = IntegerConversion.ToByte(value >> 8);
        }
    }

    static Av1LargeScaleTileListParseResult Fail(AvifResultCode code)
    {
        return new Av1LargeScaleTileListParseResult(code, null);
    }
}
