using System;
using System.Collections.Generic;

namespace avif;

// AV1 specification sections 5.11.1, 6.10.1, and 7.10.
internal class Av1TileData
{
    internal int TileIndex;
    internal int TileRow;
    internal int TileColumn;
    internal int Offset;
    internal int Size;
    internal Av1TileGroup? Group;

    internal Av1TileData(
        int tileIndex,
        int tileRow,
        int tileColumn,
        int offset,
        int size)
    {
        TileIndex = tileIndex;
        TileRow = tileRow;
        TileColumn = tileColumn;
        Offset = offset;
        Size = size;
    }

    internal ReadOnlySpan<byte> GetData()
    {
        if (Group == null || Group.Closed || Offset < 0 || Size < 0
            || Offset > Group.Payload.Length - Size)
        {
            return ReadOnlySpan<byte>.Empty;
        }

        return Group.Payload.AsSpan(Offset, Size);
    }
}

internal class Av1TileGroup
{
    internal int StartTile;
    internal int EndTile;
    internal Av1TileData[] Tiles;
    internal byte[] Payload;
    internal ResourceGuard Resources;
    internal int OutstandingTiles;
    internal bool Closed;

    internal Av1TileGroup(
        int startTile,
        int endTile,
        Av1TileData[] tiles,
        byte[] payload,
        ResourceGuard resources)
    {
        StartTile = startTile;
        EndTile = endTile;
        Tiles = tiles;
        Payload = payload;
        Resources = resources;
        OutstandingTiles = tiles.Length;
        Closed = false;
        int index = 0;
        while (index < tiles.Length)
        {
            tiles[index].Group = this;
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
        long payloadLength = Payload.Length;
        Payload = System.Array.Empty<byte>();
        return Resources.TemporaryBytes.Release(payloadLength);
    }
}

internal class Av1TileGroupParseResult
{
    internal AvifResultCode Code;
    internal Av1TileGroup? Group;

    internal Av1TileGroupParseResult(AvifResultCode code, Av1TileGroup? group)
    {
        Code = code;
        Group = group;
    }
}

internal static class Av1TileGroupParser
{
    internal static Av1TileGroupParseResult Parse(
        byte[] payload,
        long bitOffset,
        ParsedAv1FrameHeader? header,
        ResourceGuard resources)
    {
        if (payload == null || header == null || header.ShowExistingFrame
            || header.Tiles == null || resources == null
            || resources.Code != AvifResultCode.Success)
        {
            return Fail(AvifResultCode.InvalidArgument);
        }

        Av1TileInfo info = header.Tiles;
        if (!HasValidTileInfo(info))
        {
            return Fail(AvifResultCode.InvalidArgument);
        }

        long payloadBits = (long)payload.Length * 8;
        if (bitOffset < 0 || bitOffset > payloadBits)
        {
            return Fail(AvifResultCode.InvalidArgument);
        }

        int tileCount = info.TileColumns * info.TileRows;
        BitReader reader = BitReader.Create(payload.Length);
        reader.BitPosition = bitOffset;
        bool rangePresent = false;
        if (tileCount > 1)
        {
            rangePresent = reader.ReadBit(payload) != 0;
        }

        int startTile = 0;
        int endTile = tileCount - 1;
        if (rangePresent)
        {
            int tileNumberBits = info.TileColumnsLog2 + info.TileRowsLog2;
            startTile = (int)reader.ReadBits(payload, tileNumberBits);
            endTile = (int)reader.ReadBits(payload, tileNumberBits);
        }

        if (reader.Code != AvifResultCode.Success)
        {
            return Fail(Normalize(reader.Code));
        }

        if (startTile < 0 || startTile > endTile || endTile >= tileCount)
        {
            return Fail(AvifResultCode.InvalidAv1Bitstream);
        }

        AvifResultCode alignment = reader.AlignToByte(payload, true);
        if (alignment != AvifResultCode.Success)
        {
            return Fail(Normalize(alignment));
        }

        int groupTileCount = endTile - startTile + 1;
        if (!resources.TileCount.CanAdd(groupTileCount)
            || !resources.TemporaryBytes.CanAdd(payload.Length))
        {
            return Fail(AvifResultCode.LimitExceeded);
        }

        int cursor = (int)(reader.BitPosition / 8);
        Av1TileData[] tiles = new Av1TileData[groupTileCount];
        int groupIndex = 0;
        int tileIndex = startTile;
        while (tileIndex <= endTile)
        {
            int tileSize;
            if (tileIndex == endTile)
            {
                tileSize = payload.Length - cursor;
            }
            else
            {
                if (cursor > payload.Length - info.TileSizeBytes)
                {
                    return Fail(AvifResultCode.TruncatedData);
                }

                ulong tileSizeMinusOne = 0;
                int sizeByte = 0;
                while (sizeByte < info.TileSizeBytes)
                {
                    tileSizeMinusOne |= (ulong)payload[cursor + sizeByte]
                        << (sizeByte * 8);
                    sizeByte++;
                }

                cursor += info.TileSizeBytes;
                ulong encodedTileSize = tileSizeMinusOne + 1;
                if (encodedTileSize > int.MaxValue)
                {
                    return Fail(AvifResultCode.LimitExceeded);
                }

                tileSize = (int)encodedTileSize;
            }

            if (tileSize <= 0)
            {
                return Fail(AvifResultCode.InvalidAv1Bitstream);
            }

            if (cursor > payload.Length - tileSize)
            {
                return Fail(AvifResultCode.TruncatedData);
            }

            int tileRow = tileIndex / info.TileColumns;
            int tileColumn = tileIndex - tileRow * info.TileColumns;
            tiles[groupIndex] = new Av1TileData(
                tileIndex,
                tileRow,
                tileColumn,
                cursor,
                tileSize);
            cursor += tileSize;
            tileIndex++;
            groupIndex++;
        }

        if (cursor != payload.Length)
        {
            return Fail(AvifResultCode.InvalidAv1Bitstream);
        }

        resources.TileCount.AddUnchecked(groupTileCount);
        resources.TemporaryBytes.AddUnchecked(payload.Length);
        byte[] ownedPayload = (byte[])payload.Clone();
        Av1TileGroup group = new Av1TileGroup(
            startTile,
            endTile,
            tiles,
            ownedPayload,
            resources);
        return new Av1TileGroupParseResult(AvifResultCode.Success, group);
    }

    static bool HasValidTileInfo(Av1TileInfo info)
    {
        if (info.TileColumns < 1
            || info.TileColumns > Av1SyntaxConstants.MaxTileColumns
            || info.TileRows < 1
            || info.TileRows > Av1SyntaxConstants.MaxTileRows
            || info.TileColumnsLog2 < 0 || info.TileColumnsLog2 > 6
            || info.TileRowsLog2 < 0 || info.TileRowsLog2 > 6
            || !HasMatchingLog2(info.TileColumns, info.TileColumnsLog2)
            || !HasMatchingLog2(info.TileRows, info.TileRowsLog2))
        {
            return false;
        }

        int count = info.TileColumns * info.TileRows;
        if (count == 1)
        {
            return info.TileSizeBytes == 0;
        }

        return info.TileSizeBytes >= 1 && info.TileSizeBytes <= 4
            && info.TileColumnsLog2 + info.TileRowsLog2 > 0;
    }

    static bool HasMatchingLog2(int count, int countLog2)
    {
        int upper = 1 << countLog2;
        int lower = countLog2 == 0 ? 0 : 1 << (countLog2 - 1);
        return count > lower && count <= upper;
    }

    static AvifResultCode Normalize(AvifResultCode code)
    {
        return code == AvifResultCode.TruncatedData
            ? AvifResultCode.TruncatedData
            : AvifResultCode.InvalidAv1Bitstream;
    }

    static Av1TileGroupParseResult Fail(AvifResultCode code)
    {
        return new Av1TileGroupParseResult(code, null);
    }
}

internal class Av1TileWorkItem
{
    internal Av1TileScheduler Scheduler;
    internal Av1TileData Tile;
    internal Av1TileCdfContext? Context;
    internal int State;

    internal Av1TileWorkItem(
        Av1TileScheduler scheduler,
        Av1TileData tile,
        Av1TileCdfContext context)
    {
        Scheduler = scheduler;
        Tile = tile;
        Context = context;
        State = 0;
    }
}

internal class Av1TileScheduler
{
    internal ResourceGuard Resources;
    internal Av1CdfContextManager Contexts;
    internal Av1DecodeProgress? Progress;
    internal ParsedAv1FrameHeader? Header;
    internal Av1TileData[] Tiles;
    internal bool[] ScheduledTiles;
    internal List<Av1TileGroup> Groups;
    internal List<Av1TileWorkItem> WorkItems;
    internal int WorkerCount;
    internal int InFlightCount;
    internal int CompletedTileCount;
    internal int NextExpectedTile;
    internal int NextScheduleTile;
    internal bool FrameActive;
    internal bool GroupsComplete;
    internal bool Closed;
    internal AvifResultCode Code;

    internal Av1TileScheduler(
        AvifSecurityLimits limits,
        Av1CdfContextManager contexts)
    {
        Resources = new ResourceGuard(limits);
        Contexts = contexts;
        Groups = new List<Av1TileGroup>();
        WorkItems = new List<Av1TileWorkItem>();
        Tiles = System.Array.Empty<Av1TileData>();
        ScheduledTiles = System.Array.Empty<bool>();
        Code = Resources.Code;
        if (Code == AvifResultCode.Success && contexts == null)
        {
            Code = AvifResultCode.InvalidArgument;
        }
    }

    internal AvifResultCode BeginFrame(
        ParsedAv1FrameHeader header,
        int requestedWorkerCount)
    {
        return BeginFrameWithProgress(header, requestedWorkerCount, null);
    }

    internal AvifResultCode BeginFrameWithProgress(
        ParsedAv1FrameHeader header,
        int requestedWorkerCount,
        Av1DecodeProgress? progress)
    {
        if (Closed)
        {
            return AvifResultCode.Closed;
        }

        if (Code != AvifResultCode.Success)
        {
            return Code;
        }

        if (FrameActive)
        {
            return AvifResultCode.InvalidState;
        }

        if (header == null || header.Tiles == null || requestedWorkerCount <= 0)
        {
            return AvifResultCode.InvalidArgument;
        }

        long tileCountValue = (long)header.Tiles.TileColumns * header.Tiles.TileRows;
        if (tileCountValue <= 0 || tileCountValue > int.MaxValue)
        {
            return AvifResultCode.InvalidArgument;
        }

        int tileCount = (int)tileCountValue;
        if (tileCount > Resources.Limits.MaxTileCount)
        {
            return AvifResultCode.LimitExceeded;
        }

        int workers = requestedWorkerCount < tileCount
            ? requestedWorkerCount
            : tileCount;
        AvifResultCode workersCode = Resources.AcquireWorkers(workers);
        if (workersCode != AvifResultCode.Success)
        {
            return workersCode;
        }

        AvifResultCode contextCode = Contexts.BeginFrame(header);
        if (contextCode != AvifResultCode.Success)
        {
            Resources.ReleaseWorkers(workers);
            return contextCode;
        }

        if (progress != null)
        {
            AvifResultCode progressCode = progress.BeginFrame(header);
            if (progressCode != AvifResultCode.Success)
            {
                Contexts.AbortFrame();
                Resources.ReleaseWorkers(workers);
                return progressCode;
            }
        }

        Resources.BeginTileSet();
        Header = header;
        Progress = progress;
        Tiles = new Av1TileData[tileCount];
        ScheduledTiles = new bool[tileCount];
        Groups.Clear();
        WorkItems.Clear();
        WorkerCount = workers;
        InFlightCount = 0;
        CompletedTileCount = 0;
        NextExpectedTile = 0;
        NextScheduleTile = 0;
        GroupsComplete = false;
        FrameActive = true;
        return AvifResultCode.Success;
    }

    internal AvifResultCode AddTileGroup(byte[] payload, long bitOffset)
    {
        if (Closed)
        {
            return AvifResultCode.Closed;
        }

        if (Code != AvifResultCode.Success)
        {
            return Code;
        }

        if (!FrameActive || GroupsComplete)
        {
            return AvifResultCode.InvalidState;
        }

        Av1TileGroupParseResult parsed = Av1TileGroupParser.Parse(
            payload,
            bitOffset,
            Header,
            Resources);
        if (parsed.Code != AvifResultCode.Success)
        {
            Abort(parsed.Code);
            return Code;
        }

        Av1TileGroup? group = parsed.Group;
        if ((group ?? throw new InvalidOperationException("Invalid internal state.")).StartTile != NextExpectedTile)
        {
            Resources.TileCount.Release(group.Tiles.Length);
            group.Close();
            Abort(AvifResultCode.InvalidAv1Bitstream);
            return Code;
        }

        int index = 0;
        while (index < group.Tiles.Length)
        {
            Av1TileData tile = group.Tiles[index];
            if (tile.TileIndex < 0 || tile.TileIndex >= Tiles.Length
                || Tiles[tile.TileIndex] != null)
            {
                Resources.TileCount.Release(group.Tiles.Length);
                group.Close();
                Abort(AvifResultCode.InvalidAv1Bitstream);
                return Code;
            }

            Tiles[tile.TileIndex] = tile;
            index++;
        }

        Groups.Add(group);
        NextExpectedTile = group.EndTile + 1;
        return AvifResultCode.Success;
    }

    internal AvifResultCode CompleteGroups()
    {
        if (Closed)
        {
            return AvifResultCode.Closed;
        }

        if (Code != AvifResultCode.Success)
        {
            return Code;
        }

        if (!FrameActive || GroupsComplete)
        {
            return AvifResultCode.InvalidState;
        }

        if (NextExpectedTile != Tiles.Length)
        {
            Abort(AvifResultCode.InvalidAv1Bitstream);
            return Code;
        }

        GroupsComplete = true;
        return AvifResultCode.Success;
    }

    internal Av1TileWorkItem? TakeNextTile()
    {
        if (!Closed && Code == AvifResultCode.Success && FrameActive
            && Progress != null)
        {
            AvifResultCode checkpoint = Progress.Checkpoint();
            if (checkpoint != AvifResultCode.Success)
            {
                Abort(checkpoint);
                return null;
            }
        }
        if (Closed || Code != AvifResultCode.Success || !FrameActive
            || !GroupsComplete || InFlightCount >= WorkerCount)
        {
            return null;
        }

        while (NextScheduleTile < Tiles.Length
            && ScheduledTiles[NextScheduleTile])
        {
            NextScheduleTile++;
        }

        if (NextScheduleTile >= Tiles.Length)
        {
            return null;
        }

        int tileIndex = NextScheduleTile;
        Av1TileCdfContext? context = Contexts.CreateTileContext(tileIndex);
        if (context == null)
        {
            Abort(Contexts.Code);
            return null;
        }

        ScheduledTiles[tileIndex] = true;
        NextScheduleTile++;
        InFlightCount++;
        Av1TileWorkItem work = new Av1TileWorkItem(
            this,
            Tiles[tileIndex],
            context);
        WorkItems.Add(work);
        return work;
    }

    internal AvifResultCode ReportDecodedRows(
        Av1TileWorkItem? work,
        int decodedRows)
    {
        if (Closed)
        {
            return AvifResultCode.Closed;
        }
        if (Code != AvifResultCode.Success)
        {
            return Code;
        }
        if (!FrameActive || Progress == null || work == null
            || work.State != 0
            || !object.ReferenceEquals(work.Scheduler, this)
            || work.Tile == null)
        {
            return AvifResultCode.InvalidArgument;
        }
        AvifResultCode result = Progress.ReportDecodedRows(
            work.Tile.TileIndex, decodedRows);
        if (result == AvifResultCode.Cancelled)
        {
            Abort(result);
        }
        return result;
    }

    internal AvifResultCode CompleteTile(Av1TileWorkItem? work)
    {
        if (Closed)
        {
            return AvifResultCode.Closed;
        }

        if (Code != AvifResultCode.Success)
        {
            return Code;
        }

        if (!FrameActive || work == null || work.State != 0
            || !object.ReferenceEquals(work.Scheduler, this)
            || work.Tile == null || work.Context == null)
        {
            return AvifResultCode.InvalidArgument;
        }
        if (Progress != null)
        {
            AvifResultCode checkpoint = Progress.Checkpoint();
            if (checkpoint != AvifResultCode.Success)
            {
                Abort(checkpoint);
                return Code;
            }
        }

        AvifResultCode finishCode = Contexts.FinishTile(work.Context);
        if (finishCode != AvifResultCode.Success)
        {
            Abort(finishCode);
            return Code;
        }

        work.State = 1;
        work.Context = null;
        InFlightCount--;
        CompletedTileCount++;
        if (Progress != null)
        {
            AvifResultCode progressCode = Progress.ReportTileCompleted(
                work.Tile.TileIndex);
            if (progressCode != AvifResultCode.Success)
            {
                Abort(progressCode);
                return Code;
            }
        }
        Av1TileGroup? group = work.Tile.Group;
        (group ?? throw new InvalidOperationException("Invalid internal state.")).OutstandingTiles--;
        if (group.OutstandingTiles == 0)
        {
            group.Close();
        }

        if (CompletedTileCount == Tiles.Length)
        {
            if (Progress != null)
            {
                AvifResultCode progressCode = Progress.CompleteFrame();
                if (progressCode != AvifResultCode.Success)
                {
                    Abort(progressCode);
                    return Code;
                }
            }
            AvifResultCode completeCode = Contexts.CompleteFrame();
            if (completeCode != AvifResultCode.Success)
            {
                Abort(completeCode);
                return Code;
            }

            FinishFrame();
        }

        return AvifResultCode.Success;
    }

    internal AvifResultCode FailFrame(AvifResultCode code)
    {
        if (Closed)
        {
            return AvifResultCode.Closed;
        }

        if (!FrameActive || code == AvifResultCode.Success)
        {
            return AvifResultCode.InvalidArgument;
        }

        Abort(code);
        return Code;
    }

    internal AvifResultCode Close()
    {
        if (Closed)
        {
            return AvifResultCode.Success;
        }

        if (FrameActive)
        {
            Abort(AvifResultCode.Closed);
        }

        ClearFrameStorage();
        Closed = true;
        return AvifResultCode.Success;
    }

    void FinishFrame()
    {
        CloseGroups();
        Resources.ReleaseWorkers(WorkerCount);
        Header = null;
        Progress = null;
        WorkerCount = 0;
        InFlightCount = 0;
        FrameActive = false;
        GroupsComplete = false;
        ClearFrameStorage();
    }

    void Abort(AvifResultCode code)
    {
        if (Code == AvifResultCode.Success)
        {
            Code = code;
        }
        if (Progress != null)
        {
            Progress.Fail(code);
        }

        int index = 0;
        while (index < WorkItems.Count)
        {
            if (WorkItems[index].State == 0)
            {
                WorkItems[index].State = 2;
                WorkItems[index].Context = null;
            }

            index++;
        }

        CloseGroups();
        if (FrameActive)
        {
            Contexts.AbortFrame();
            Resources.ReleaseWorkers(WorkerCount);
        }

        Header = null;
        Progress = null;
        WorkerCount = 0;
        InFlightCount = 0;
        FrameActive = false;
        GroupsComplete = false;
        ClearFrameStorage();
    }

    void CloseGroups()
    {
        int index = 0;
        while (index < Groups.Count)
        {
            Groups[index].Close();
            index++;
        }

        Groups.Clear();
    }

    void ClearFrameStorage()
    {
        Tiles = System.Array.Empty<Av1TileData>();
        ScheduledTiles = System.Array.Empty<bool>();
        WorkItems.Clear();
        NextExpectedTile = 0;
        NextScheduleTile = 0;
        CompletedTileCount = 0;
    }
}
