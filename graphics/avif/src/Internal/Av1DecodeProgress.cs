using System;
using System.Collections.Generic;

namespace avif;

internal class Av1RequiredRange
{
    internal long Offset;
    internal long Length;

    internal Av1RequiredRange(long offset, long length)
    {
        Offset = offset;
        Length = length;
    }
}

internal class Av1PartialTileVisibility
{
    internal int TileIndex;
    internal int TileRow;
    internal int TileColumn;
    internal int X;
    internal int Y;
    internal int Width;
    internal int Height;
    internal bool Complete;
}

internal class Av1DecodeProgressSnapshot
{
    internal AvifResultCode Code;
    internal int CompletedTileCount;
    internal int TotalTileCount;
    internal int ContiguousDecodedRows;
    internal int FrameWidth;
    internal int FrameHeight;
    internal bool FrameComplete;
    internal bool[] CompletedTiles;
    internal int[] DecodedRowsPerTile;
    internal Av1PartialTileVisibility[] VisibleTiles;
    internal Av1RequiredRange[] RequiredRanges;

    internal Av1DecodeProgressSnapshot()
    {
        CompletedTiles = System.Array.Empty<bool>();
        DecodedRowsPerTile = System.Array.Empty<int>();
        VisibleTiles = System.Array.Empty<Av1PartialTileVisibility>();
        RequiredRanges = System.Array.Empty<Av1RequiredRange>();
    }
}

// Incremental AV1 decode reporting and bounded resumption state.
internal class Av1DecodeProgress
{
    internal bool AllowIncremental;
    internal AvifCancellation Cancellation;
    internal ParsedAv1FrameHeader? Header;
    internal bool[] CompletedTiles;
    internal int[] DecodedRows;
    internal int[] TileX;
    internal int[] TileY;
    internal int[] TileWidth;
    internal int[] TileHeight;
    internal List<Av1RequiredRange> RequiredRanges;
    internal int CompletedTileCount;
    internal bool Active;
    internal bool FrameComplete;
    internal AvifResultCode Code;

    internal Av1DecodeProgress(
        bool allowIncremental,
        AvifCancellation cancellation)
    {
        AllowIncremental = allowIncremental;
        Cancellation = cancellation;
        CompletedTiles = System.Array.Empty<bool>();
        DecodedRows = System.Array.Empty<int>();
        TileX = System.Array.Empty<int>();
        TileY = System.Array.Empty<int>();
        TileWidth = System.Array.Empty<int>();
        TileHeight = System.Array.Empty<int>();
        RequiredRanges = new List<Av1RequiredRange>();
        Code = cancellation == null
            ? AvifResultCode.InvalidArgument
            : AvifResultCode.Success;
    }

    internal AvifResultCode BeginFrame(ParsedAv1FrameHeader header)
    {
        if (Code != AvifResultCode.Success)
        {
            return Code;
        }
        if (Active)
        {
            return AvifResultCode.InvalidState;
        }
        if (!ValidateHeader(header))
        {
            return AvifResultCode.InvalidArgument;
        }
        AvifResultCode checkpoint = Checkpoint();
        if (checkpoint != AvifResultCode.Success)
        {
            return checkpoint;
        }

        Header = header;
        int tileCount = (header.Tiles ?? throw new InvalidOperationException("Invalid internal state.")).TileColumns * header.Tiles.TileRows;
        CompletedTiles = new bool[tileCount];
        DecodedRows = new int[tileCount];
        TileX = new int[tileCount];
        TileY = new int[tileCount];
        TileWidth = new int[tileCount];
        TileHeight = new int[tileCount];
        RequiredRanges.Clear();
        CompletedTileCount = 0;
        FrameComplete = false;
        BuildGeometry();
        Active = true;
        return AvifResultCode.Success;
    }

    internal AvifResultCode ReportDecodedRows(
        int tileIndex,
        int decodedRows)
    {
        AvifResultCode state = CheckActive();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        if (tileIndex < 0 || tileIndex >= DecodedRows.Length
            || decodedRows < DecodedRows[tileIndex]
            || decodedRows > TileHeight[tileIndex]
            || CompletedTiles[tileIndex]
                && decodedRows != TileHeight[tileIndex])
        {
            return AvifResultCode.InvalidArgument;
        }
        DecodedRows[tileIndex] = decodedRows;
        return AvifResultCode.Success;
    }

    internal AvifResultCode ReportTileCompleted(int tileIndex)
    {
        AvifResultCode state = CheckActive();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        if (tileIndex < 0 || tileIndex >= CompletedTiles.Length)
        {
            return AvifResultCode.InvalidArgument;
        }
        if (CompletedTiles[tileIndex])
        {
            return AvifResultCode.InvalidState;
        }
        CompletedTiles[tileIndex] = true;
        DecodedRows[tileIndex] = TileHeight[tileIndex];
        CompletedTileCount++;
        return AvifResultCode.Success;
    }

    internal AvifResultCode RequireRange(long offset, long length)
    {
        AvifResultCode state = CheckActive();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        CheckedIntegerResult end = CheckedMath.AddInt64(offset, length);
        if (offset < 0 || length <= 0 || !end.Success)
        {
            return AvifResultCode.InvalidArgument;
        }
        RequiredRanges.Add(new Av1RequiredRange(offset, length));
        SortAndMergeRanges();
        return AvifResultCode.NeedMoreData;
    }

    internal AvifResultCode SatisfyRange(long offset, long length)
    {
        AvifResultCode state = CheckActive();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        CheckedIntegerResult endResult = CheckedMath.AddInt64(offset, length);
        if (offset < 0 || length <= 0 || !endResult.Success)
        {
            return AvifResultCode.InvalidArgument;
        }

        long end = endResult.Value;
        List<Av1RequiredRange> remaining = new List<Av1RequiredRange>();
        int index = 0;
        while (index < RequiredRanges.Count)
        {
            Av1RequiredRange required = RequiredRanges[index];
            long requiredEnd = required.Offset + required.Length;
            if (end <= required.Offset || offset >= requiredEnd)
            {
                remaining.Add(new Av1RequiredRange(
                    required.Offset, required.Length));
            }
            else
            {
                if (offset > required.Offset)
                {
                    remaining.Add(new Av1RequiredRange(
                        required.Offset, offset - required.Offset));
                }
                if (end < requiredEnd)
                {
                    remaining.Add(new Av1RequiredRange(
                        end, requiredEnd - end));
                }
            }
            index++;
        }
        RequiredRanges = remaining;
        return AvifResultCode.Success;
    }

    internal AvifResultCode ObserveRead(InputReadResult result)
    {
        if (result == null)
        {
            return AvifResultCode.InvalidArgument;
        }
        if (result.Code == AvifResultCode.NeedMoreData
            && result.MissingLength > 0)
        {
            return RequireRange(result.MissingOffset, result.MissingLength);
        }
        return result.Code;
    }

    internal AvifResultCode Checkpoint()
    {
        if (Code != AvifResultCode.Success)
        {
            return Code;
        }
        if (Cancellation == null)
        {
            Code = AvifResultCode.InvalidArgument;
            Active = false;
            return Code;
        }
        if (Cancellation.IsCancellationRequested())
        {
            Code = AvifResultCode.Cancelled;
            Active = false;
            return Code;
        }
        return AvifResultCode.Success;
    }

    internal AvifResultCode CompleteFrame()
    {
        AvifResultCode state = CheckActive();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        if (CompletedTileCount != CompletedTiles.Length
            || RequiredRanges.Count != 0)
        {
            return AvifResultCode.InvalidState;
        }
        FrameComplete = true;
        Active = false;
        return AvifResultCode.Success;
    }

    internal AvifResultCode Fail(AvifResultCode code)
    {
        if (code == AvifResultCode.Success)
        {
            return AvifResultCode.InvalidArgument;
        }
        if (Code == AvifResultCode.Success)
        {
            Code = code;
        }
        Active = false;
        FrameComplete = false;
        return Code;
    }

    internal Av1DecodeProgressSnapshot GetSnapshot()
    {
        Av1DecodeProgressSnapshot snapshot = new Av1DecodeProgressSnapshot();
        snapshot.Code = Code != AvifResultCode.Success
            ? Code
            : (RequiredRanges.Count > 0
                ? AvifResultCode.NeedMoreData
                : AvifResultCode.Success);
        snapshot.CompletedTileCount = CompletedTileCount;
        snapshot.TotalTileCount = CompletedTiles.Length;
        snapshot.FrameWidth = Header == null ? 0 : (Header.Size ?? throw new InvalidOperationException("Invalid internal state.")).FrameWidth;
        snapshot.FrameHeight = Header == null ? 0 : (Header.Size ?? throw new InvalidOperationException("Invalid internal state.")).FrameHeight;
        snapshot.FrameComplete = FrameComplete;
        snapshot.CompletedTiles = (bool[])CompletedTiles.Clone();
        snapshot.DecodedRowsPerTile = (int[])DecodedRows.Clone();
        snapshot.RequiredRanges = CopyRanges();
        bool expose = AllowIncremental || FrameComplete;
        snapshot.VisibleTiles = expose
            ? CreateVisibleTiles()
            : System.Array.Empty<Av1PartialTileVisibility>();
        snapshot.ContiguousDecodedRows = expose
            ? ComputeContiguousDecodedRows()
            : 0;
        return snapshot;
    }

    static bool ValidateHeader(ParsedAv1FrameHeader header)
    {
        if (header == null || header.Size == null || header.Tiles == null
            || header.Size.FrameWidth <= 0 || header.Size.FrameHeight <= 0
            || header.Tiles.TileColumns <= 0 || header.Tiles.TileRows <= 0
            || header.Tiles.MiColumnStarts == null
            || header.Tiles.MiRowStarts == null
            || header.Tiles.MiColumnStarts.Length
                != header.Tiles.TileColumns + 1
            || header.Tiles.MiRowStarts.Length
                != header.Tiles.TileRows + 1)
        {
            return false;
        }
        long tileCount = (long)header.Tiles.TileColumns
            * header.Tiles.TileRows;
        if (tileCount > System.Array.MaxLength
            || header.Tiles.MiColumnStarts[0] != 0
            || header.Tiles.MiRowStarts[0] != 0)
        {
            return false;
        }
        int column = 0;
        while (column < header.Tiles.TileColumns)
        {
            long start = (long)header.Tiles.MiColumnStarts[column] * 4;
            long end = (long)header.Tiles.MiColumnStarts[column + 1] * 4;
            if (start < 0 || start >= header.Size.FrameWidth
                || end <= start)
            {
                return false;
            }
            column++;
        }
        long frameRight = (long)header.Tiles.MiColumnStarts[
            header.Tiles.TileColumns] * 4;
        if (frameRight < header.Size.FrameWidth)
        {
            return false;
        }
        int row = 0;
        while (row < header.Tiles.TileRows)
        {
            long start = (long)header.Tiles.MiRowStarts[row] * 4;
            long end = (long)header.Tiles.MiRowStarts[row + 1] * 4;
            if (start < 0 || start >= header.Size.FrameHeight
                || end <= start)
            {
                return false;
            }
            row++;
        }
        long frameBottom = (long)header.Tiles.MiRowStarts[
            header.Tiles.TileRows] * 4;
        if (frameBottom < header.Size.FrameHeight)
        {
            return false;
        }
        return true;
    }

    void BuildGeometry()
    {
        int tileRow = 0;
        while (tileRow < ((Header ?? throw new InvalidOperationException("Invalid internal state.")).Tiles ?? throw new InvalidOperationException("Invalid internal state.")).TileRows)
        {
            int y = (int)((long)((Header.Tiles ?? throw new InvalidOperationException("Invalid internal state.")).MiRowStarts ?? throw new InvalidOperationException("Invalid internal state."))[tileRow] * 4);
            long bottomValue = (long)(Header.Tiles.MiRowStarts ?? throw new InvalidOperationException("Invalid internal state."))[
                tileRow + 1] * 4;
            int bottom = bottomValue > (Header.Size ?? throw new InvalidOperationException("Invalid internal state.")).FrameHeight
                ? Header.Size.FrameHeight
                : (int)bottomValue;
            int tileColumn = 0;
            while (tileColumn < Header.Tiles.TileColumns)
            {
                int x = (int)((long)(Header.Tiles.MiColumnStarts ?? throw new InvalidOperationException("Invalid internal state."))[
                    tileColumn] * 4);
                long rightValue = (long)Header.Tiles.MiColumnStarts[
                    tileColumn + 1] * 4;
                int right = rightValue > Header.Size.FrameWidth
                    ? Header.Size.FrameWidth
                    : (int)rightValue;
                int tileIndex = tileRow * Header.Tiles.TileColumns
                    + tileColumn;
                TileX[tileIndex] = x;
                TileY[tileIndex] = y;
                TileWidth[tileIndex] = right - x;
                TileHeight[tileIndex] = bottom - y;
                tileColumn++;
            }
            tileRow++;
        }
    }

    AvifResultCode CheckActive()
    {
        AvifResultCode checkpoint = Checkpoint();
        if (checkpoint != AvifResultCode.Success)
        {
            return checkpoint;
        }
        return Active ? AvifResultCode.Success : AvifResultCode.InvalidState;
    }

    void SortAndMergeRanges()
    {
        int index = 1;
        while (index < RequiredRanges.Count)
        {
            Av1RequiredRange value = RequiredRanges[index];
            int position = index;
            while (position > 0
                && RequiredRanges[position - 1].Offset > value.Offset)
            {
                RequiredRanges[position] = RequiredRanges[position - 1];
                position--;
            }
            RequiredRanges[position] = value;
            index++;
        }

        List<Av1RequiredRange> merged = new List<Av1RequiredRange>();
        index = 0;
        while (index < RequiredRanges.Count)
        {
            Av1RequiredRange current = RequiredRanges[index];
            if (merged.Count == 0)
            {
                merged.Add(new Av1RequiredRange(
                    current.Offset, current.Length));
            }
            else
            {
                Av1RequiredRange previous = merged[merged.Count - 1];
                long previousEnd = previous.Offset + previous.Length;
                long currentEnd = current.Offset + current.Length;
                if (current.Offset <= previousEnd)
                {
                    if (currentEnd > previousEnd)
                    {
                        previous.Length = currentEnd - previous.Offset;
                    }
                }
                else
                {
                    merged.Add(new Av1RequiredRange(
                        current.Offset, current.Length));
                }
            }
            index++;
        }
        RequiredRanges = merged;
    }

    Av1RequiredRange[] CopyRanges()
    {
        Av1RequiredRange[] copy =
            new Av1RequiredRange[RequiredRanges.Count];
        int index = 0;
        while (index < copy.Length)
        {
            copy[index] = new Av1RequiredRange(
                RequiredRanges[index].Offset,
                RequiredRanges[index].Length);
            index++;
        }
        return copy;
    }

    Av1PartialTileVisibility[] CreateVisibleTiles()
    {
        int count = 0;
        int tile = 0;
        while (tile < DecodedRows.Length)
        {
            if (DecodedRows[tile] > 0)
            {
                count++;
            }
            tile++;
        }
        Av1PartialTileVisibility[] visible =
            new Av1PartialTileVisibility[count];
        int output = 0;
        tile = 0;
        while (tile < DecodedRows.Length)
        {
            if (DecodedRows[tile] > 0)
            {
                Av1PartialTileVisibility region =
                    new Av1PartialTileVisibility();
                region.TileIndex = tile;
                region.TileRow = tile / ((Header ?? throw new InvalidOperationException("Invalid internal state.")).Tiles ?? throw new InvalidOperationException("Invalid internal state.")).TileColumns;
                region.TileColumn = tile
                    - region.TileRow * (Header.Tiles ?? throw new InvalidOperationException("Invalid internal state.")).TileColumns;
                region.X = TileX[tile];
                region.Y = TileY[tile];
                region.Width = TileWidth[tile];
                region.Height = DecodedRows[tile];
                region.Complete = CompletedTiles[tile];
                visible[output] = region;
                output++;
            }
            tile++;
        }
        return visible;
    }

    int ComputeContiguousDecodedRows()
    {
        if (Header == null)
        {
            return 0;
        }
        int contiguous = 0;
        int tileRow = 0;
        while (tileRow < (Header.Tiles ?? throw new InvalidOperationException("Invalid internal state.")).TileRows)
        {
            int minimum = int.MaxValue;
            int column = 0;
            while (column < Header.Tiles.TileColumns)
            {
                int tile = tileRow * Header.Tiles.TileColumns + column;
                if (DecodedRows[tile] < minimum)
                {
                    minimum = DecodedRows[tile];
                }
                column++;
            }
            int rowHeight = TileHeight[
                tileRow * Header.Tiles.TileColumns];
            contiguous += minimum;
            if (minimum < rowHeight)
            {
                break;
            }
            tileRow++;
        }
        return contiguous > (Header.Size ?? throw new InvalidOperationException("Invalid internal state.")).FrameHeight
            ? Header.Size.FrameHeight
            : contiguous;
    }
}
