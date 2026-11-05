using System;
using System.Collections.Generic;

namespace gltf;

internal class BufferPackOptions
{
    internal int MaximumBufferByteLength = int.MaxValue;
    internal bool Partition;
    internal bool Interleave = true;
}

internal class BufferPackItem
{
    internal byte[] Bytes = [];
    internal int ElementCount;
    internal int ElementByteLength;
    internal int ElementAlignment = 1;
    internal GltfBufferViewTarget Target = GltfBufferViewTarget.None;
    internal int InterleaveGroup = GltfConstants.AbsentIndex;
    internal bool PreserveLayout;
    internal bool HasByteStride;
    internal int ByteStride;
}

internal class BufferPackPlacement
{
    internal int Buffer;
    internal int BufferView;
    internal int AccessorByteOffset;
}

internal class BufferPackView
{
    internal int Buffer;
    internal int ByteOffset;
    internal int ByteLength;
    internal bool HasByteStride;
    internal int ByteStride;
    internal GltfBufferViewTarget Target = GltfBufferViewTarget.None;
}

internal class BufferPackBuffer
{
    internal byte[] Bytes = [];
}

internal class BufferPackResult
{
    internal List<BufferPackBuffer> Buffers = new List<BufferPackBuffer>();
    internal List<BufferPackView> BufferViews = new List<BufferPackView>();
    internal List<BufferPackPlacement> Placements = new List<BufferPackPlacement>();
}

internal class BufferPackUnit
{
    internal List<int> ItemIndices = new List<int>();
    internal List<int> AccessorByteOffsets = new List<int>();
    internal byte[] Bytes = [];
    internal GltfBufferViewTarget Target = GltfBufferViewTarget.None;
    internal bool HasByteStride;
    internal int ByteStride;
    internal int Buffer;
    internal int ByteOffset;
}

internal class BufferPacker
{
    internal BufferPackResult PackBuffers(
        List<BufferPackItem> items,
        BufferPackOptions options)
    {
        ValidatePackInput(items, options);
        List<BufferPackUnit> units = CreatePackUnits(items, options);
        List<int> bufferByteLengths = AssignPackUnits(units, options);
        BufferPackResult result = CreatePackResult(
            items.Count,
            units,
            bufferByteLengths);
        return result;
    }

    internal void ValidatePackInput(
        List<BufferPackItem> items,
        BufferPackOptions options)
    {
        if (items == null)
        {
            throw new ArgumentNullException("items");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (options.MaximumBufferByteLength < 1)
        {
            throw new ArgumentOutOfRangeException("options");
        }

        int index = 0;
        while (index < items.Count)
        {
            BufferPackItem item = items[index];
            if (item == null || item.Bytes == null)
            {
                throw new ArgumentException(
                    $"Buffer pack item {index} is null or has null bytes.",
                    "items");
            }

            if (item.ElementCount < 1 || item.ElementByteLength < 1)
            {
                throw new ArgumentException(
                    $"Buffer pack item {index} must contain one or more non-empty elements.",
                    "items");
            }

            int expectedByteLength = CheckedArithmetic.MultiplyNonNegativeInt32(
                item.ElementCount,
                item.ElementByteLength);
            if (item.Bytes.Length != expectedByteLength)
            {
                throw new ArgumentException(
                    $"Buffer pack item {index} byte length does not match its element layout.",
                    "items");
            }

            if (item.ElementAlignment != 1
                && item.ElementAlignment != 2
                && item.ElementAlignment != 4)
            {
                throw new ArgumentException(
                    $"Buffer pack item {index} alignment must be 1, 2, or 4.",
                    "items");
            }

            if (item.ElementByteLength % item.ElementAlignment != 0)
            {
                throw new ArgumentException(
                    $"Buffer pack item {index} element length does not preserve its required alignment.",
                    "items");
            }

            if (!IsKnownTarget(item.Target))
            {
                throw new ArgumentException(
                    $"Buffer pack item {index} has an unknown target.",
                    "items");
            }

            if (item.InterleaveGroup < GltfConstants.AbsentIndex)
            {
                throw new ArgumentException(
                    $"Buffer pack item {index} has an invalid interleave group.",
                    "items");
            }

            if (item.InterleaveGroup != GltfConstants.AbsentIndex
                && item.Target != GltfBufferViewTarget.ArrayBuffer)
            {
                throw new ArgumentException(
                    $"Buffer pack item {index} requests interleaving without an array-buffer target.",
                    "items");
            }

            if (item.PreserveLayout
                && item.InterleaveGroup != GltfConstants.AbsentIndex)
            {
                throw new ArgumentException(
                    $"Buffer pack item {index} cannot preserve and interleave its layout.",
                    "items");
            }

            if (item.HasByteStride
                && (!item.PreserveLayout
                    || item.ByteStride < GltfConstants.MinimumByteStride
                    || item.ByteStride > GltfConstants.MaximumByteStride
                    || item.ByteStride % 4 != 0))
            {
                throw new ArgumentException(
                    $"Buffer pack item {index} has an invalid preserved byte stride.",
                    "items");
            }

            index++;
        }
    }

    internal List<BufferPackUnit> CreatePackUnits(
        List<BufferPackItem> items,
        BufferPackOptions options)
    {
        List<BufferPackUnit> result = new List<BufferPackUnit>();
        bool[] consumed = new bool[items.Count];
        int targetRank = 0;
        while (targetRank < 3)
        {
            GltfBufferViewTarget target = GetTargetForRank(targetRank);
            int itemIndex = 0;
            while (itemIndex < items.Count)
            {
                BufferPackItem item = items[itemIndex];
                if (!consumed[itemIndex] && item.Target == target)
                {
                    if (options.Interleave
                        && item.InterleaveGroup != GltfConstants.AbsentIndex)
                    {
                        BufferPackUnit interleaved = CreateInterleavedUnit(
                            items,
                            consumed,
                            item.InterleaveGroup);
                        result.Add(interleaved);
                    }
                    else
                    {
                        consumed[itemIndex] = true;
                        result.Add(CreateTightUnit(item, itemIndex));
                    }
                }

                itemIndex++;
            }

            targetRank++;
        }

        return result;
    }

    internal BufferPackUnit CreateTightUnit(
        BufferPackItem item,
        int itemIndex)
    {
        BufferPackUnit result = new BufferPackUnit();
        result.ItemIndices.Add(itemIndex);
        result.AccessorByteOffsets.Add(0);
        result.Target = item.Target;
        if (item.PreserveLayout)
        {
            result.HasByteStride = item.HasByteStride;
            result.ByteStride = item.ByteStride;
            result.Bytes = new byte[item.Bytes.Length];
            item.Bytes.CopyTo(result.Bytes, 0);
            return result;
        }

        if (item.Target == GltfBufferViewTarget.ArrayBuffer
            && item.ElementByteLength % 4 != 0)
        {
            int stride = AlignByteOffset(item.ElementByteLength, 4);
            if (stride > GltfConstants.MaximumByteStride)
            {
                throw new ArgumentException(
                    $"Array-buffer element byte stride {stride} is outside the supported range.",
                    "item");
            }

            result.HasByteStride = true;
            result.ByteStride = stride;
            int byteLength = CheckedArithmetic.MultiplyNonNegativeInt32(
                stride,
                item.ElementCount);
            result.Bytes = new byte[byteLength];
            int elementIndex = 0;
            while (elementIndex < item.ElementCount)
            {
                int sourceOffset = CheckedArithmetic.MultiplyNonNegativeInt32(
                    elementIndex,
                    item.ElementByteLength);
                int destinationOffset = CheckedArithmetic.MultiplyNonNegativeInt32(
                    elementIndex,
                    stride);
                Array.Copy(
                    item.Bytes,
                    sourceOffset,
                    result.Bytes,
                    destinationOffset,
                    item.ElementByteLength);
                elementIndex++;
            }

            return result;
        }

        result.Bytes = new byte[item.Bytes.Length];
        item.Bytes.CopyTo(result.Bytes, 0);
        return result;
    }

    internal BufferPackUnit CreateInterleavedUnit(
        List<BufferPackItem> items,
        bool[] consumed,
        int interleaveGroup)
    {
        BufferPackUnit result = new BufferPackUnit();
        result.Target = GltfBufferViewTarget.ArrayBuffer;
        result.HasByteStride = true;
        int elementCount = -1;
        int stride = 0;
        int itemIndex = 0;
        while (itemIndex < items.Count)
        {
            BufferPackItem item = items[itemIndex];
            if (!consumed[itemIndex]
                && item.InterleaveGroup == interleaveGroup)
            {
                if (item.Target != GltfBufferViewTarget.ArrayBuffer)
                {
                    throw new ArgumentException(
                        "Every interleaved item must have an array-buffer target.",
                        "items");
                }

                if (elementCount < 0)
                {
                    elementCount = item.ElementCount;
                }
                else if (item.ElementCount != elementCount)
                {
                    throw new ArgumentException(
                        $"Interleave group {interleaveGroup} has inconsistent element counts.",
                        "items");
                }

                stride = AlignByteOffset(stride, 4);
                result.ItemIndices.Add(itemIndex);
                result.AccessorByteOffsets.Add(stride);
                stride = CheckedArithmetic.AddNonNegativeInt32(
                    stride,
                    item.ElementByteLength);
                consumed[itemIndex] = true;
            }

            itemIndex++;
        }

        stride = AlignByteOffset(stride, 4);
        if (stride < GltfConstants.MinimumByteStride
            || stride > GltfConstants.MaximumByteStride)
        {
            throw new ArgumentException(
                $"Interleave group {interleaveGroup} byte stride {stride} is outside the supported range.",
                "items");
        }

        result.ByteStride = stride;
        int byteLength = CheckedArithmetic.MultiplyNonNegativeInt32(
            stride,
            elementCount);
        result.Bytes = new byte[byteLength];
        int groupItemIndex = 0;
        while (groupItemIndex < result.ItemIndices.Count)
        {
            int sourceItemIndex = result.ItemIndices[groupItemIndex];
            BufferPackItem sourceItem = items[sourceItemIndex];
            int accessorByteOffset = result.AccessorByteOffsets[groupItemIndex];
            int elementIndex = 0;
            while (elementIndex < elementCount)
            {
                int sourceOffset = CheckedArithmetic.MultiplyNonNegativeInt32(
                    elementIndex,
                    sourceItem.ElementByteLength);
                int destinationElementOffset = CheckedArithmetic.MultiplyNonNegativeInt32(
                    elementIndex,
                    stride);
                int destinationOffset = CheckedArithmetic.AddNonNegativeInt32(
                    destinationElementOffset,
                    accessorByteOffset);
                Array.Copy(
                    sourceItem.Bytes,
                    sourceOffset,
                    result.Bytes,
                    destinationOffset,
                    sourceItem.ElementByteLength);
                elementIndex++;
            }

            groupItemIndex++;
        }

        return result;
    }

    internal List<int> AssignPackUnits(
        List<BufferPackUnit> units,
        BufferPackOptions options)
    {
        List<int> bufferByteLengths = new List<int>();
        int unitIndex = 0;
        while (unitIndex < units.Count)
        {
            BufferPackUnit unit = units[unitIndex];
            if (unit.Bytes.Length > options.MaximumBufferByteLength)
            {
                throw new ArgumentException(
                    $"Packed view {unitIndex} exceeds the maximum buffer byte length.",
                    "items");
            }

            if (bufferByteLengths.Count == 0)
            {
                bufferByteLengths.Add(0);
            }

            int bufferIndex = bufferByteLengths.Count - 1;
            int byteOffset = AlignByteOffset(
                bufferByteLengths[bufferIndex],
                4);
            long byteEnd = (long)byteOffset + unit.Bytes.Length;
            if (byteEnd > options.MaximumBufferByteLength)
            {
                if (!options.Partition)
                {
                    throw new ArgumentException(
                        "Packed data exceeds the maximum buffer byte length and partitioning is disabled.",
                        "options");
                }

                bufferIndex++;
                bufferByteLengths.Add(0);
                byteOffset = 0;
                byteEnd = unit.Bytes.Length;
            }

            unit.Buffer = bufferIndex;
            unit.ByteOffset = byteOffset;
            bufferByteLengths[bufferIndex] = (int)byteEnd;
            unitIndex++;
        }

        return bufferByteLengths;
    }

    internal BufferPackResult CreatePackResult(
        int itemCount,
        List<BufferPackUnit> units,
        List<int> bufferByteLengths)
    {
        BufferPackResult result = new BufferPackResult();
        int itemIndex = 0;
        while (itemIndex < itemCount)
        {
            result.Placements.Add(new BufferPackPlacement());
            itemIndex++;
        }

        int bufferIndex = 0;
        while (bufferIndex < bufferByteLengths.Count)
        {
            BufferPackBuffer buffer = new BufferPackBuffer();
            buffer.Bytes = new byte[bufferByteLengths[bufferIndex]];
            result.Buffers.Add(buffer);
            bufferIndex++;
        }

        int unitIndex = 0;
        while (unitIndex < units.Count)
        {
            BufferPackUnit unit = units[unitIndex];
            BufferPackView view = new BufferPackView();
            view.Buffer = unit.Buffer;
            view.ByteOffset = unit.ByteOffset;
            view.ByteLength = unit.Bytes.Length;
            view.HasByteStride = unit.HasByteStride;
            view.ByteStride = unit.ByteStride;
            view.Target = unit.Target;
            int bufferViewIndex = result.BufferViews.Count;
            result.BufferViews.Add(view);
            Array.Copy(
                unit.Bytes,
                0,
                result.Buffers[unit.Buffer].Bytes,
                unit.ByteOffset,
                unit.Bytes.Length);

            int unitItemIndex = 0;
            while (unitItemIndex < unit.ItemIndices.Count)
            {
                int packedItemIndex = unit.ItemIndices[unitItemIndex];
                BufferPackPlacement placement = result.Placements[packedItemIndex];
                placement.Buffer = unit.Buffer;
                placement.BufferView = bufferViewIndex;
                placement.AccessorByteOffset = unit.AccessorByteOffsets[unitItemIndex];
                unitItemIndex++;
            }

            unitIndex++;
        }

        return result;
    }

    internal int AlignByteOffset(int value, int alignment)
    {
        if (value < 0 || alignment < 1)
        {
            throw new ArgumentOutOfRangeException("value");
        }

        int remainder = value % alignment;
        if (remainder == 0)
        {
            return value;
        }

        return CheckedArithmetic.AddNonNegativeInt32(
            value,
            alignment - remainder);
    }

    internal bool IsKnownTarget(GltfBufferViewTarget target)
    {
        switch (target)
        {
            case GltfBufferViewTarget.None:
            case GltfBufferViewTarget.ArrayBuffer:
            case GltfBufferViewTarget.ElementArrayBuffer:
                return true;
            default:
                return false;
        }
    }

    internal GltfBufferViewTarget GetTargetForRank(int rank)
    {
        switch (rank)
        {
            case 0:
                return GltfBufferViewTarget.None;
            case 1:
                return GltfBufferViewTarget.ArrayBuffer;
            case 2:
                return GltfBufferViewTarget.ElementArrayBuffer;
            default:
                throw new ArgumentOutOfRangeException("rank");
        }
    }
}
