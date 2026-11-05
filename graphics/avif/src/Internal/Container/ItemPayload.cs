using System;
using System.Collections.Generic;

namespace avif;

internal class ParsedItemPayloadSegment
{
    long sourceOffset;
    long length;

    internal ParsedItemPayloadSegment(long sourceOffset, long length)
    {
        this.sourceOffset = sourceOffset;
        this.length = length;
    }

    internal long GetSourceOffset()
    {
        return sourceOffset;
    }

    internal long GetLength()
    {
        return length;
    }
}

internal class ParsedItemPayload
{
    ParsedItem item;
    List<ParsedItemPayloadSegment> segments;
    long length;

    internal ParsedItemPayload(
        ParsedItem item,
        List<ParsedItemPayloadSegment> segments,
        long length)
    {
        this.item = item;
        this.segments = new List<ParsedItemPayloadSegment>(segments);
        this.length = length;
    }

    internal ParsedItem GetItem()
    {
        return item;
    }

    internal long GetLength()
    {
        return length;
    }

    internal int GetSegmentCount()
    {
        return segments.Count;
    }

    internal ParsedItemPayloadSegment? GetSegment(int index)
    {
        if (index < 0 || index >= segments.Count)
        {
            return null;
        }

        return segments[index];
    }

    internal InputReadResult Read(
        ReadOnlySpan<byte> source,
        long offset,
        Span<byte> destination)
    {
        if (offset < 0)
        {
            return new InputReadResult(AvifResultCode.InvalidArgument, 0, 0, 0);
        }

        if (offset > length)
        {
            CheckedIntegerResult requestedEnd = CheckedMath.AddInt64(
                offset,
                destination.Length);
            if (!requestedEnd.Success)
            {
                return new InputReadResult(requestedEnd.Code, 0, 0, 0);
            }

            return new InputReadResult(
                AvifResultCode.EndOfData,
                0,
                length,
                requestedEnd.Value - length);
        }

        if (destination.Length == 0)
        {
            return new InputReadResult(AvifResultCode.Success, 0, 0, 0);
        }

        if (offset == length)
        {
            return new InputReadResult(
                AvifResultCode.EndOfData,
                0,
                offset,
                destination.Length);
        }

        long available = length - offset;
        int count = destination.Length;
        if (available < count)
        {
            count = (int)available;
        }

        InputReadResult copy = Copy(source, offset, destination.Slice(0, count));
        if (copy.Code != AvifResultCode.Success)
        {
            return copy;
        }

        return new InputReadResult(AvifResultCode.Success, count, 0, 0);
    }

    internal InputReadResult ReadExact(
        ReadOnlySpan<byte> source,
        long offset,
        Span<byte> destination)
    {
        if (offset < 0)
        {
            return new InputReadResult(AvifResultCode.InvalidArgument, 0, 0, 0);
        }

        CheckedIntegerResult requestedEnd = CheckedMath.AddInt64(
            offset,
            destination.Length);
        if (!requestedEnd.Success)
        {
            return new InputReadResult(requestedEnd.Code, 0, 0, 0);
        }

        if (offset > length)
        {
            return new InputReadResult(
                AvifResultCode.EndOfData,
                0,
                length,
                requestedEnd.Value - length);
        }

        if (requestedEnd.Value > length)
        {
            return new InputReadResult(
                AvifResultCode.TruncatedData,
                0,
                length,
                requestedEnd.Value - length);
        }

        if (destination.Length == 0)
        {
            return new InputReadResult(AvifResultCode.Success, 0, 0, 0);
        }

        return Copy(source, offset, destination);
    }

    internal InputReadResult Copy(
        ReadOnlySpan<byte> source,
        long offset,
        Span<byte> destination)
    {
        List<ParsedItemPayloadSegment> selected = new List<ParsedItemPayloadSegment>();
        AvifResultCode sliceCode = AppendSlice(
            offset,
            destination.Length,
            selected,
            segments.Count);
        if (sliceCode != AvifResultCode.Success)
        {
            return new InputReadResult(sliceCode, 0, 0, 0);
        }

        int index = 0;
        while (index < selected.Count)
        {
            ParsedItemPayloadSegment segment = selected[index];
            CheckedRangeResult range = CheckedMath.ResolveRange(
                segment.GetSourceOffset(),
                0,
                segment.GetLength(),
                source.Length);
            if (!range.Success)
            {
                long missingLength = range.End > source.Length
                    ? range.End - source.Length
                    : 0;
                return new InputReadResult(
                    range.Code,
                    0,
                    source.Length,
                    missingLength);
            }

            index++;
        }

        int destinationOffset = 0;
        index = 0;
        while (index < selected.Count)
        {
            ParsedItemPayloadSegment segment = selected[index];
            int segmentLength = (int)segment.GetLength();
            source.Slice((int)segment.GetSourceOffset(), segmentLength).CopyTo(
                destination.Slice(destinationOffset, segmentLength));
            destinationOffset += segmentLength;
            index++;
        }

        return new InputReadResult(
            AvifResultCode.Success,
            destination.Length,
            0,
            0);
    }

    internal AvifResultCode AppendSlice(
        long offset,
        long sliceLength,
        List<ParsedItemPayloadSegment> destination,
        int maximumSegmentCount)
    {
        if (offset < 0
            || sliceLength < 0
            || destination == null
            || maximumSegmentCount < 0)
        {
            return AvifResultCode.InvalidArgument;
        }

        CheckedIntegerResult end = CheckedMath.AddInt64(offset, sliceLength);
        if (!end.Success)
        {
            return end.Code;
        }

        if (end.Value > length)
        {
            return AvifResultCode.InvalidItem;
        }

        long skipped = offset;
        long remaining = sliceLength;
        int index = 0;
        while (index < segments.Count && remaining > 0)
        {
            ParsedItemPayloadSegment segment = segments[index];
            long segmentLength = segment.GetLength();
            if (skipped >= segmentLength)
            {
                skipped -= segmentLength;
                index++;
                continue;
            }

            long available = segmentLength - skipped;
            long selectedLength = remaining < available ? remaining : available;
            if (skipped == 0 && selectedLength == segmentLength)
            {
                if (destination.Count >= maximumSegmentCount)
                {
                    return AvifResultCode.LimitExceeded;
                }

                destination.Add(segment);
            }
            else
            {
                CheckedIntegerResult selectedOffset = CheckedMath.AddInt64(
                    segment.GetSourceOffset(),
                    skipped);
                if (!selectedOffset.Success)
                {
                    return selectedOffset.Code;
                }

                if (destination.Count >= maximumSegmentCount)
                {
                    return AvifResultCode.LimitExceeded;
                }

                destination.Add(new ParsedItemPayloadSegment(
                    selectedOffset.Value,
                    selectedLength));
            }

            remaining -= selectedLength;
            skipped = 0;
            index++;
        }

        return remaining == 0
            ? AvifResultCode.Success
            : AvifResultCode.InvalidItem;
    }
}

internal class ItemPayloadResolveResult
{
    internal AvifResultCode Code;
    internal ParsedItemPayload? Payload;

    internal ItemPayloadResolveResult(
        AvifResultCode code,
        ParsedItemPayload? payload)
    {
        Code = code;
        Payload = payload;
    }
}

internal class EncodedItemExtractionResult
{
    internal AvifResultCode Code;
    internal ParsedItem? Item;
    internal PooledByteBuffer? Data;

    internal EncodedItemExtractionResult(
        AvifResultCode code,
        ParsedItem? item,
        PooledByteBuffer? data)
    {
        Code = code;
        Item = item;
        Data = data;
    }
}

internal class ParsedItemPayloadResolver
{
    Dictionary<uint, ParsedItem> itemsById;
    Dictionary<uint, ParsedItemPayload> payloads;
    Dictionary<uint, AvifResultCode> failures;
    long maximumExtractionBytes;
    int maximumSegmentCount;

    internal ParsedItemPayloadResolver(
        List<ParsedItem> items,
        long maximumExtractionBytes,
        int maximumSegmentCount)
    {
        itemsById = new Dictionary<uint, ParsedItem>();
        payloads = new Dictionary<uint, ParsedItemPayload>();
        failures = new Dictionary<uint, AvifResultCode>();
        this.maximumExtractionBytes = maximumExtractionBytes;
        this.maximumSegmentCount = maximumSegmentCount;
        int index = 0;
        while (index < items.Count)
        {
            ParsedItem item = items[index];
            itemsById.Add(item.GetItemId(), item);
            index++;
        }
    }

    internal int GetCachedPayloadCount()
    {
        return payloads.Count;
    }

    internal ItemPayloadResolveResult Resolve(uint itemId)
    {
        if (payloads.ContainsKey(itemId))
        {
            return new ItemPayloadResolveResult(
                AvifResultCode.Success,
                payloads[itemId]);
        }

        if (failures.ContainsKey(itemId))
        {
            return new ItemPayloadResolveResult(failures[itemId], null);
        }

        if (!itemsById.ContainsKey(itemId))
        {
            return new ItemPayloadResolveResult(AvifResultCode.NotFound, null);
        }

        List<ParsedItem> stack = new List<ParsedItem>();
        HashSet<uint> active = new HashSet<uint>();
        stack.Add(itemsById[itemId]);
        active.Add(itemId);
        while (stack.Count > 0)
        {
            int stackIndex = stack.Count - 1;
            ParsedItem item = stack[stackIndex];
            uint currentId = item.GetItemId();
            if (payloads.ContainsKey(currentId) || failures.ContainsKey(currentId))
            {
                active.Remove(currentId);
                stack.RemoveAt(stackIndex);
                continue;
            }

            AvifResultCode itemCode = ValidateItem(item);
            if (itemCode != AvifResultCode.Success)
            {
                failures.Add(currentId, itemCode);
                continue;
            }

            ParsedItemLocation? location = item.GetLocation();
            if ((location ?? throw new InvalidOperationException("Invalid internal state.")).GetConstructionMethod() != 2)
            {
                ItemPayloadResolveResult direct = CreateDirectPayload(item);
                if (direct.Code == AvifResultCode.Success)
                {
                    payloads.Add(currentId, direct.Payload
                        ?? throw new InvalidOperationException("Invalid internal state."));
                }
                else
                {
                    failures.Add(currentId, direct.Code);
                }

                continue;
            }

            ParsedItemReference? reference = ParsedItemDependencyGraph
                .FindLocationReference(item);
            if (reference == null)
            {
                failures.Add(currentId, AvifResultCode.InvalidItem);
                continue;
            }

            bool pushedDependency = false;
            AvifResultCode dependencyCode = AvifResultCode.Success;
            int extentIndex = 0;
            while (extentIndex < location.GetExtentCount())
            {
                ulong referenceIndex = (location
                    .GetExtent(extentIndex) ?? throw new InvalidOperationException("Invalid internal state."))
                    .GetExtentIndex();
                if (referenceIndex == 0
                    || referenceIndex > (ulong)reference.GetTargetCount())
                {
                    dependencyCode = AvifResultCode.InvalidItem;
                    break;
                }

                uint sourceId = reference.GetTargetItemId((int)referenceIndex - 1);
                if (failures.ContainsKey(sourceId))
                {
                    dependencyCode = failures[sourceId];
                    break;
                }

                if (!payloads.ContainsKey(sourceId))
                {
                    if (!itemsById.ContainsKey(sourceId) || active.Contains(sourceId))
                    {
                        dependencyCode = AvifResultCode.InvalidItem;
                        break;
                    }

                    stack.Add(itemsById[sourceId]);
                    active.Add(sourceId);
                    pushedDependency = true;
                    break;
                }

                extentIndex++;
            }

            if (pushedDependency)
            {
                continue;
            }

            if (dependencyCode != AvifResultCode.Success)
            {
                failures.Add(currentId, dependencyCode);
                continue;
            }

            ItemPayloadResolveResult constructed = CreateConstructedPayload(
                item,
                reference);
            if (constructed.Code == AvifResultCode.Success)
            {
                payloads.Add(currentId, constructed.Payload
                    ?? throw new InvalidOperationException("Invalid internal state."));
            }
            else
            {
                failures.Add(currentId, constructed.Code);
            }
        }

        if (payloads.ContainsKey(itemId))
        {
            return new ItemPayloadResolveResult(
                AvifResultCode.Success,
                payloads[itemId]);
        }

        return new ItemPayloadResolveResult(failures[itemId], null);
    }

    internal EncodedItemExtractionResult Extract(
        ReadOnlySpan<byte> source,
        uint itemId)
    {
        ItemPayloadResolveResult resolved = Resolve(itemId);
        if (resolved.Code != AvifResultCode.Success)
        {
            ParsedItem? failedItem = itemsById.ContainsKey(itemId)
                ? itemsById[itemId]
                : null;
            return new EncodedItemExtractionResult(
                resolved.Code,
                failedItem,
                null);
        }

        CheckedIntegerResult allocation = CheckedMath.ComputeAllocationSize(
            (resolved.Payload ?? throw new InvalidOperationException("Invalid internal state.")).GetLength(),
            1,
            maximumExtractionBytes);
        if (!allocation.Success)
        {
            return new EncodedItemExtractionResult(
                allocation.Code,
                resolved.Payload.GetItem(),
                null);
        }

        PooledByteBuffer data = new PooledByteBuffer(
            (int)allocation.Value,
            maximumExtractionBytes,
            true);
        if (data.Code != AvifResultCode.Success)
        {
            return new EncodedItemExtractionResult(
                data.Code,
                resolved.Payload.GetItem(),
                null);
        }

        InputReadResult read = resolved.Payload.ReadExact(
            source,
            0,
            data.GetSpan());
        if (read.Code != AvifResultCode.Success)
        {
            data.Close();
            return new EncodedItemExtractionResult(
                read.Code,
                resolved.Payload.GetItem(),
                null);
        }

        return new EncodedItemExtractionResult(
            AvifResultCode.Success,
            resolved.Payload.GetItem(),
            data);
    }

    internal AvifResultCode ValidateItem(ParsedItem item)
    {
        if (item.GetProtectionIndex() != 0)
        {
            return AvifResultCode.UnsupportedProtection;
        }

        if (!item.HasLocation())
        {
            return AvifResultCode.NotFound;
        }

        return AvifResultCode.Success;
    }

    internal ItemPayloadResolveResult CreateDirectPayload(ParsedItem item)
    {
        ParsedItemLocation? location = item.GetLocation();
        List<ParsedItemPayloadSegment> segments = new List<ParsedItemPayloadSegment>();
        long totalLength = 0;
        int index = 0;
        while (index < (location ?? throw new InvalidOperationException("Invalid internal state.")).GetExtentCount())
        {
            ParsedItemExtent? extent = location.GetExtent(index);
            if (!(extent ?? throw new InvalidOperationException("Invalid internal state.")).HasResolvedRange())
            {
                return new ItemPayloadResolveResult(
                    AvifResultCode.InvalidState,
                    null);
            }

            CheckedIntegerResult nextLength = CheckedMath.AddInt64(
                totalLength,
                extent.GetResolvedLength());
            if (!nextLength.Success)
            {
                return new ItemPayloadResolveResult(nextLength.Code, null);
            }

            if (extent.GetResolvedLength() > 0)
            {
                if (segments.Count >= maximumSegmentCount)
                {
                    return new ItemPayloadResolveResult(
                        AvifResultCode.LimitExceeded,
                        null);
                }

                segments.Add(new ParsedItemPayloadSegment(
                    extent.GetResolvedOffset(),
                    extent.GetResolvedLength()));
            }

            totalLength = nextLength.Value;
            index++;
        }

        return new ItemPayloadResolveResult(
            AvifResultCode.Success,
            new ParsedItemPayload(item, segments, totalLength));
    }

    internal ItemPayloadResolveResult CreateConstructedPayload(
        ParsedItem item,
        ParsedItemReference reference)
    {
        ParsedItemLocation? location = item.GetLocation();
        List<ParsedItemPayloadSegment> segments = new List<ParsedItemPayloadSegment>();
        long totalLength = 0;
        int index = 0;
        while (index < (location ?? throw new InvalidOperationException("Invalid internal state.")).GetExtentCount())
        {
            ParsedItemExtent? extent = location.GetExtent(index);
            ulong referenceIndex = (extent ?? throw new InvalidOperationException("Invalid internal state.")).GetExtentIndex();
            uint sourceId = reference.GetTargetItemId((int)referenceIndex - 1);
            ParsedItemPayload sourcePayload = payloads[sourceId];
            if (location.GetBaseOffset() > ulong.MaxValue - extent.GetExtentOffset())
            {
                return new ItemPayloadResolveResult(
                    AvifResultCode.LimitExceeded,
                    null);
            }

            ulong unsignedOffset = location.GetBaseOffset() + extent.GetExtentOffset();
            if (unsignedOffset > long.MaxValue)
            {
                return new ItemPayloadResolveResult(
                    AvifResultCode.LimitExceeded,
                    null);
            }

            long sourceOffset = (long)unsignedOffset;
            if (sourceOffset > sourcePayload.GetLength())
            {
                return new ItemPayloadResolveResult(
                    AvifResultCode.InvalidItem,
                    null);
            }

            long available = sourcePayload.GetLength() - sourceOffset;
            ulong declaredLength = extent.GetExtentLength();
            long selectedLength;
            if (declaredLength == 0)
            {
                selectedLength = available;
            }
            else
            {
                if (declaredLength > (ulong)available)
                {
                    return new ItemPayloadResolveResult(
                        AvifResultCode.InvalidItem,
                        null);
                }

                selectedLength = (long)declaredLength;
            }

            AvifResultCode sliceCode = sourcePayload.AppendSlice(
                sourceOffset,
                selectedLength,
                segments,
                maximumSegmentCount);
            if (sliceCode != AvifResultCode.Success)
            {
                return new ItemPayloadResolveResult(sliceCode, null);
            }

            CheckedIntegerResult nextLength = CheckedMath.AddInt64(
                totalLength,
                selectedLength);
            if (!nextLength.Success)
            {
                return new ItemPayloadResolveResult(nextLength.Code, null);
            }

            totalLength = nextLength.Value;
            index++;
        }

        return new ItemPayloadResolveResult(
            AvifResultCode.Success,
            new ParsedItemPayload(item, segments, totalLength));
    }
}
