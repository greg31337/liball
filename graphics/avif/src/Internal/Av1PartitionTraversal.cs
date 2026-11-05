using System.Collections.Generic;

namespace avif;

// AV1 specification sections 5.11.3, 6.10.3, and 7.10.3.
internal enum Av1PartitionEventKind
{
    Partition = 0,
    Block = 1
}

internal class Av1PartitionEvent
{
    internal Av1PartitionEventKind Kind;
    internal int MiRow;
    internal int MiColumn;
    internal Av1BlockSize BlockSize;
    internal Av1PartitionType Partition;
    internal bool Forced;
    internal int Depth;
    internal int VisibleMiWidth;
    internal int VisibleMiHeight;
    internal int SuperblockIndex;
}

internal class Av1PartitionStep
{
    internal AvifResultCode Code;
    internal Av1PartitionEvent? Event;
    internal bool Complete;

    internal Av1PartitionStep(
        AvifResultCode code,
        Av1PartitionEvent? partitionEvent,
        bool complete)
    {
        Code = code;
        Event = partitionEvent;
        Complete = complete;
    }
}

internal class Av1PartitionTask
{
    internal int Kind;
    internal int MiRow;
    internal int MiColumn;
    internal Av1BlockSize BlockSize;
    internal Av1PartitionType ParentPartition;
    internal int Depth;
    internal int SuperblockIndex;

    internal Av1PartitionTask(
        int kind,
        int miRow,
        int miColumn,
        Av1BlockSize blockSize,
        Av1PartitionType parentPartition,
        int depth,
        int superblockIndex)
    {
        Kind = kind;
        MiRow = miRow;
        MiColumn = miColumn;
        BlockSize = blockSize;
        ParentPartition = parentPartition;
        Depth = depth;
        SuperblockIndex = superblockIndex;
    }
}

internal static class Av1PartitionRules
{
    internal static bool IsAllowed(Av1BlockSize blockSize, Av1PartitionType partition)
    {
        if (Av1ConversionTables.GetSquareBlockIndex(blockSize) < 0
            || partition < Av1PartitionType.None
            || partition > Av1PartitionType.Vertical4)
        {
            return false;
        }

        Av1BlockSize subsize =
            Av1ConversionTables.GetPartitionSubsize(partition, blockSize);
        if (subsize == Av1BlockSize.Invalid)
        {
            return false;
        }

        if (partition == Av1PartitionType.HorizontalA
            || partition == Av1PartitionType.HorizontalB
            || partition == Av1PartitionType.VerticalA
            || partition == Av1PartitionType.VerticalB)
        {
            return Av1ConversionTables.GetPartitionSubsize(
                Av1PartitionType.Split,
                blockSize) != Av1BlockSize.Invalid;
        }

        return true;
    }

    internal static Av1PartitionType GetForcedPartition(
        int miRow,
        int miColumn,
        Av1BlockSize blockSize,
        int rowEnd,
        int columnEnd)
    {
        if (blockSize == Av1BlockSize.Block4x4)
        {
            return Av1PartitionType.None;
        }

        int half = Av1ConversionTables.GetBlockMiWidth(blockSize) >> 1;
        if (half <= 0)
        {
            return Av1PartitionType.Invalid;
        }

        bool hasRows = miRow + half < rowEnd;
        bool hasColumns = miColumn + half < columnEnd;
        if (!hasRows && !hasColumns)
        {
            return Av1PartitionType.Split;
        }

        if (!hasRows)
        {
            return Av1PartitionType.Horizontal;
        }

        if (!hasColumns)
        {
            return Av1PartitionType.Vertical;
        }

        return Av1PartitionType.Invalid;
    }
}

internal class Av1PartitionTraversal
{
    internal ParsedAv1SequenceHeader Sequence;
    internal ParsedAv1FrameHeader Header;
    internal int TileIndex;
    internal int RowStart;
    internal int RowEnd;
    internal int ColumnStart;
    internal int ColumnEnd;
    internal int SuperblockMiSize;
    internal Av1BlockSize SuperblockBlockSize;
    internal List<Av1PartitionTask> Tasks;
    internal Av1PartitionTask? PendingDecision;
    internal int NextSuperblockRow;
    internal int NextSuperblockColumn;
    internal int NextSuperblockIndex;
    internal long EventCount;
    internal long MaximumEventCount;
    internal bool Closed;
    internal AvifResultCode Code;

    internal Av1PartitionTraversal(
        ParsedAv1SequenceHeader sequence,
        ParsedAv1FrameHeader header,
        int tileIndex,
        AvifSecurityLimits limits)
    {
        Sequence = sequence;
        Header = header;
        TileIndex = tileIndex;
        Tasks = new List<Av1PartitionTask>();
        Code = ValidateAndInitialize(limits);
    }

    internal Av1PartitionStep TakeNext()
    {
        if (Closed)
        {
            return new Av1PartitionStep(AvifResultCode.Closed, null, false);
        }

        if (Code != AvifResultCode.Success)
        {
            return new Av1PartitionStep(Code, null, false);
        }

        if (PendingDecision != null)
        {
            return new Av1PartitionStep(AvifResultCode.InvalidState, null, false);
        }

        if (Tasks.Count == 0 && !AddNextSuperblock())
        {
            return new Av1PartitionStep(AvifResultCode.Success, null, true);
        }

        int taskIndex = Tasks.Count - 1;
        Av1PartitionTask task = Tasks[taskIndex];
        Tasks.RemoveAt(taskIndex);
        EventCount++;
        if (EventCount > MaximumEventCount)
        {
            SetCode(AvifResultCode.LimitExceeded);
            return new Av1PartitionStep(Code, null, false);
        }

        if (task.Kind == 1)
        {
            Av1PartitionEvent blockEvent = CreateBlockEvent(task);
            return new Av1PartitionStep(
                AvifResultCode.Success,
                blockEvent,
                false);
        }

        Av1PartitionType forced = Av1PartitionRules.GetForcedPartition(
            task.MiRow,
            task.MiColumn,
            task.BlockSize,
            RowEnd,
            ColumnEnd);
        bool isForced = forced != Av1PartitionType.Invalid;
        Av1PartitionEvent partitionEvent = CreatePartitionEvent(
            task,
            forced,
            isForced);
        if (isForced)
        {
            if (!ExpandPartition(task, forced))
            {
                return new Av1PartitionStep(Code, null, false);
            }
        }
        else
        {
            PendingDecision = task;
        }

        return new Av1PartitionStep(
            AvifResultCode.Success,
            partitionEvent,
            false);
    }

    internal AvifResultCode SubmitPartition(Av1PartitionType partition)
    {
        if (Closed)
        {
            return AvifResultCode.Closed;
        }

        if (Code != AvifResultCode.Success)
        {
            return Code;
        }

        if (PendingDecision == null)
        {
            return AvifResultCode.InvalidState;
        }

        Av1PartitionTask task = PendingDecision;
        PendingDecision = null;
        if (!Av1PartitionRules.IsAllowed(task.BlockSize, partition))
        {
            SetCode(AvifResultCode.InvalidAv1Bitstream);
            return Code;
        }

        ExpandPartition(task, partition);
        return Code;
    }

    internal AvifResultCode Close()
    {
        if (Closed)
        {
            return AvifResultCode.Success;
        }

        Tasks.Clear();
        PendingDecision = null;
        Closed = true;
        return AvifResultCode.Success;
    }

    AvifResultCode ValidateAndInitialize(AvifSecurityLimits limits)
    {
        ResourceGuard resources = new ResourceGuard(limits);
        if (resources.Code != AvifResultCode.Success)
        {
            return resources.Code;
        }

        if (Sequence == null || Header == null || Header.ShowExistingFrame
            || Header.Size == null || Header.Tiles == null
            || Header.Tiles.MiColumnStarts == null
            || Header.Tiles.MiRowStarts == null
            || Header.Tiles.TileColumns <= 0 || Header.Tiles.TileRows <= 0)
        {
            return AvifResultCode.InvalidArgument;
        }

        AvifResultCode dimensions = resources.CheckImageDimensions(
            Header.Size.FrameWidth,
            Header.Size.FrameHeight);
        if (dimensions != AvifResultCode.Success)
        {
            return dimensions;
        }

        int expectedMiColumns = 2 * ((Header.Size.FrameWidth + 7) >> 3);
        int expectedMiRows = 2 * ((Header.Size.FrameHeight + 7) >> 3);
        if (Header.Size.MiColumns != expectedMiColumns
            || Header.Size.MiRows != expectedMiRows)
        {
            return AvifResultCode.InvalidAv1Bitstream;
        }

        long tileCountValue =
            (long)Header.Tiles.TileColumns * Header.Tiles.TileRows;
        if (tileCountValue <= 0 || tileCountValue > int.MaxValue
            || TileIndex < 0 || TileIndex >= tileCountValue
            || Header.Tiles.MiColumnStarts.Length
                != Header.Tiles.TileColumns + 1
            || Header.Tiles.MiRowStarts.Length
                != Header.Tiles.TileRows + 1)
        {
            return AvifResultCode.InvalidArgument;
        }

        int tileRow = TileIndex / Header.Tiles.TileColumns;
        int tileColumn = TileIndex - tileRow * Header.Tiles.TileColumns;
        RowStart = Header.Tiles.MiRowStarts[tileRow];
        RowEnd = Header.Tiles.MiRowStarts[tileRow + 1];
        ColumnStart = Header.Tiles.MiColumnStarts[tileColumn];
        ColumnEnd = Header.Tiles.MiColumnStarts[tileColumn + 1];
        if (RowStart < 0 || RowEnd <= RowStart
            || RowEnd > Header.Size.MiRows
            || ColumnStart < 0 || ColumnEnd <= ColumnStart
            || ColumnEnd > Header.Size.MiColumns)
        {
            return AvifResultCode.InvalidArgument;
        }

        SuperblockMiSize = Sequence.Uses128x128Superblocks() ? 32 : 16;
        SuperblockBlockSize = Sequence.Uses128x128Superblocks()
            ? Av1BlockSize.Block128x128
            : Av1BlockSize.Block64x64;
        if (RowStart % SuperblockMiSize != 0
            || ColumnStart % SuperblockMiSize != 0)
        {
            return AvifResultCode.InvalidAv1Bitstream;
        }

        long tileArea = (long)(RowEnd - RowStart) * (ColumnEnd - ColumnStart);
        long rootRows = (RowEnd - RowStart + SuperblockMiSize - 1)
            / SuperblockMiSize;
        long rootColumns = (ColumnEnd - ColumnStart + SuperblockMiSize - 1)
            / SuperblockMiSize;
        MaximumEventCount = tileArea * 3 + rootRows * rootColumns * 6;
        NextSuperblockRow = RowStart;
        NextSuperblockColumn = ColumnStart;
        NextSuperblockIndex = 0;
        return AvifResultCode.Success;
    }

    bool AddNextSuperblock()
    {
        if (NextSuperblockRow >= RowEnd)
        {
            return false;
        }

        Tasks.Add(new Av1PartitionTask(
            0,
            NextSuperblockRow,
            NextSuperblockColumn,
            SuperblockBlockSize,
            Av1PartitionType.Invalid,
            0,
            NextSuperblockIndex));
        NextSuperblockIndex++;
        NextSuperblockColumn += SuperblockMiSize;
        if (NextSuperblockColumn >= ColumnEnd)
        {
            NextSuperblockColumn = ColumnStart;
            NextSuperblockRow += SuperblockMiSize;
        }

        return true;
    }

    bool ExpandPartition(Av1PartitionTask task, Av1PartitionType partition)
    {
        if (!Av1PartitionRules.IsAllowed(task.BlockSize, partition))
        {
            SetCode(AvifResultCode.InvalidAv1Bitstream);
            return false;
        }

        Av1BlockSize subsize =
            Av1ConversionTables.GetPartitionSubsize(partition, task.BlockSize);
        int half = Av1ConversionTables.GetBlockMiWidth(task.BlockSize) >> 1;
        bool hasRows = task.MiRow + half < RowEnd;
        bool hasColumns = task.MiColumn + half < ColumnEnd;
        int childDepth = task.Depth + 1;
        if (partition == Av1PartitionType.None)
        {
            PushBlock(task, task.MiRow, task.MiColumn, subsize, partition, childDepth);
        }
        else if (partition == Av1PartitionType.Horizontal)
        {
            if (hasRows)
            {
                PushBlock(
                    task,
                    task.MiRow + half,
                    task.MiColumn,
                    subsize,
                    partition,
                    childDepth);
            }

            PushBlock(task, task.MiRow, task.MiColumn, subsize, partition, childDepth);
        }
        else if (partition == Av1PartitionType.Vertical)
        {
            if (hasColumns)
            {
                PushBlock(
                    task,
                    task.MiRow,
                    task.MiColumn + half,
                    subsize,
                    partition,
                    childDepth);
            }

            PushBlock(task, task.MiRow, task.MiColumn, subsize, partition, childDepth);
        }
        else if (partition == Av1PartitionType.Split)
        {
            if (hasRows && hasColumns)
            {
                PushPartition(
                    task,
                    task.MiRow + half,
                    task.MiColumn + half,
                    subsize,
                    childDepth);
            }

            if (hasRows)
            {
                PushPartition(
                    task,
                    task.MiRow + half,
                    task.MiColumn,
                    subsize,
                    childDepth);
            }

            if (hasColumns)
            {
                PushPartition(
                    task,
                    task.MiRow,
                    task.MiColumn + half,
                    subsize,
                    childDepth);
            }

            PushPartition(task, task.MiRow, task.MiColumn, subsize, childDepth);
        }
        else if (partition == Av1PartitionType.HorizontalA)
        {
            Av1BlockSize split = Av1ConversionTables.GetPartitionSubsize(
                Av1PartitionType.Split,
                task.BlockSize);
            PushBlock(
                task,
                task.MiRow + half,
                task.MiColumn,
                subsize,
                partition,
                childDepth);
            PushBlock(
                task,
                task.MiRow,
                task.MiColumn + half,
                split,
                partition,
                childDepth);
            PushBlock(task, task.MiRow, task.MiColumn, split, partition, childDepth);
        }
        else if (partition == Av1PartitionType.HorizontalB)
        {
            Av1BlockSize split = Av1ConversionTables.GetPartitionSubsize(
                Av1PartitionType.Split,
                task.BlockSize);
            PushBlock(
                task,
                task.MiRow + half,
                task.MiColumn + half,
                split,
                partition,
                childDepth);
            PushBlock(
                task,
                task.MiRow + half,
                task.MiColumn,
                split,
                partition,
                childDepth);
            PushBlock(task, task.MiRow, task.MiColumn, subsize, partition, childDepth);
        }
        else if (partition == Av1PartitionType.VerticalA)
        {
            Av1BlockSize split = Av1ConversionTables.GetPartitionSubsize(
                Av1PartitionType.Split,
                task.BlockSize);
            PushBlock(
                task,
                task.MiRow,
                task.MiColumn + half,
                subsize,
                partition,
                childDepth);
            PushBlock(
                task,
                task.MiRow + half,
                task.MiColumn,
                split,
                partition,
                childDepth);
            PushBlock(task, task.MiRow, task.MiColumn, split, partition, childDepth);
        }
        else if (partition == Av1PartitionType.VerticalB)
        {
            Av1BlockSize split = Av1ConversionTables.GetPartitionSubsize(
                Av1PartitionType.Split,
                task.BlockSize);
            PushBlock(
                task,
                task.MiRow + half,
                task.MiColumn + half,
                split,
                partition,
                childDepth);
            PushBlock(
                task,
                task.MiRow,
                task.MiColumn + half,
                split,
                partition,
                childDepth);
            PushBlock(task, task.MiRow, task.MiColumn, subsize, partition, childDepth);
        }
        else if (partition == Av1PartitionType.Horizontal4)
        {
            int quarter = half >> 1;
            int index = 3;
            while (index >= 0)
            {
                int row = task.MiRow + index * quarter;
                if (row < RowEnd)
                {
                    PushBlock(
                        task,
                        row,
                        task.MiColumn,
                        subsize,
                        partition,
                        childDepth);
                }

                index--;
            }
        }
        else if (partition == Av1PartitionType.Vertical4)
        {
            int quarter = half >> 1;
            int index = 3;
            while (index >= 0)
            {
                int column = task.MiColumn + index * quarter;
                if (column < ColumnEnd)
                {
                    PushBlock(
                        task,
                        task.MiRow,
                        column,
                        subsize,
                        partition,
                        childDepth);
                }

                index--;
            }
        }

        return Code == AvifResultCode.Success;
    }

    void PushPartition(
        Av1PartitionTask parent,
        int row,
        int column,
        Av1BlockSize size,
        int depth)
    {
        Tasks.Add(new Av1PartitionTask(
            0,
            row,
            column,
            size,
            Av1PartitionType.Split,
            depth,
            parent.SuperblockIndex));
    }

    void PushBlock(
        Av1PartitionTask parent,
        int row,
        int column,
        Av1BlockSize size,
        Av1PartitionType partition,
        int depth)
    {
        Tasks.Add(new Av1PartitionTask(
            1,
            row,
            column,
            size,
            partition,
            depth,
            parent.SuperblockIndex));
    }

    Av1PartitionEvent CreatePartitionEvent(
        Av1PartitionTask task,
        Av1PartitionType partition,
        bool forced)
    {
        Av1PartitionEvent partitionEvent = new Av1PartitionEvent();
        partitionEvent.Kind = Av1PartitionEventKind.Partition;
        partitionEvent.MiRow = task.MiRow;
        partitionEvent.MiColumn = task.MiColumn;
        partitionEvent.BlockSize = task.BlockSize;
        partitionEvent.Partition = partition;
        partitionEvent.Forced = forced;
        partitionEvent.Depth = task.Depth;
        partitionEvent.SuperblockIndex = task.SuperblockIndex;
        return partitionEvent;
    }

    Av1PartitionEvent CreateBlockEvent(Av1PartitionTask task)
    {
        Av1PartitionEvent blockEvent = new Av1PartitionEvent();
        blockEvent.Kind = Av1PartitionEventKind.Block;
        blockEvent.MiRow = task.MiRow;
        blockEvent.MiColumn = task.MiColumn;
        blockEvent.BlockSize = task.BlockSize;
        blockEvent.Partition = task.ParentPartition;
        blockEvent.Forced = false;
        blockEvent.Depth = task.Depth;
        int width = Av1ConversionTables.GetBlockMiWidth(task.BlockSize);
        int height = Av1ConversionTables.GetBlockMiHeight(task.BlockSize);
        int remainingWidth = ColumnEnd - task.MiColumn;
        int remainingHeight = RowEnd - task.MiRow;
        blockEvent.VisibleMiWidth = width < remainingWidth ? width : remainingWidth;
        blockEvent.VisibleMiHeight = height < remainingHeight ? height : remainingHeight;
        blockEvent.SuperblockIndex = task.SuperblockIndex;
        return blockEvent;
    }

    void SetCode(AvifResultCode code)
    {
        if (Code == AvifResultCode.Success)
        {
            Code = code;
        }
    }
}
