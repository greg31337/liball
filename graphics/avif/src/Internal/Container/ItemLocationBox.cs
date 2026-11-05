using System;
using System.Collections.Generic;

namespace avif;

internal class SizedIntegerReadResult
{
    internal AvifResultCode Code;
    internal ulong Value;

    internal SizedIntegerReadResult(AvifResultCode code, ulong value)
    {
        Code = code;
        Value = value;
    }
}

internal class ItemLocationBoxParseResult
{
    internal AvifResultCode Code;
    internal int Version;
    internal int OffsetSize;
    internal int LengthSize;
    internal int BaseOffsetSize;
    internal int IndexSize;
    internal List<ParsedItemLocation> Locations;

    internal ItemLocationBoxParseResult(
        AvifResultCode code,
        int version,
        int offsetSize,
        int lengthSize,
        int baseOffsetSize,
        int indexSize,
        List<ParsedItemLocation> locations)
    {
        Code = code;
        Version = version;
        OffsetSize = offsetSize;
        LengthSize = lengthSize;
        BaseOffsetSize = baseOffsetSize;
        IndexSize = indexSize;
        Locations = locations;
    }
}

internal class ItemRangeResolveResult
{
    internal AvifResultCode Code;
    internal long Offset;
    internal long Length;

    internal ItemRangeResolveResult(AvifResultCode code, long offset, long length)
    {
        Code = code;
        Offset = offset;
        Length = length;
    }
}

internal static class ItemLocationBox
{
    internal static ItemLocationBoxParseResult Parse(
        ReadOnlySpan<byte> source,
        IsoBox? box,
        IsoBox metaBox,
        ResourceGuard resources)
    {
        List<ParsedItemLocation> locations = new List<ParsedItemLocation>();
        if (box == null
            || box.Type != "iloc"
            || metaBox == null
            || metaBox.Type != "meta"
            || resources == null)
        {
            return CreateResult(AvifResultCode.InvalidArgument, 0, 0, 0, 0, 0, locations);
        }

        IsoFullBox fullBox = IsoFullBox.Parse(source, box);
        if (fullBox.Code != AvifResultCode.Success)
        {
            return CreateResult(fullBox.Code, 0, 0, 0, 0, 0, locations);
        }

        if (fullBox.Version < 0 || fullBox.Version > 2)
        {
            return CreateResult(
                AvifResultCode.UnsupportedFeature,
                fullBox.Version,
                0,
                0,
                0,
                0,
                locations);
        }

        if (fullBox.Flags != 0 || fullBox.PayloadSize < 4)
        {
            return CreateResult(
                AvifResultCode.InvalidContainer,
                fullBox.Version,
                0,
                0,
                0,
                0,
                locations);
        }

        CheckedRangeResult range = CheckedMath.ResolveRange(
            fullBox.PayloadOffset,
            0,
            fullBox.PayloadSize,
            source.Length);
        if (!range.Success)
        {
            return CreateResult(
                range.Code,
                fullBox.Version,
                0,
                0,
                0,
                0,
                locations);
        }

        ReadOnlySpan<byte> payload = source.Slice(
            (int)fullBox.PayloadOffset,
            (int)fullBox.PayloadSize);
        ByteReader reader = new ByteReader(payload.Length);
        byte firstSizes = reader.ReadUInt8(payload);
        byte secondSizes = reader.ReadUInt8(payload);
        int offsetSize = firstSizes >> 4;
        int lengthSize = firstSizes & 15;
        int baseOffsetSize = secondSizes >> 4;
        int indexSize = secondSizes & 15;
        if (!IsSupportedSize(offsetSize)
            || !IsSupportedSize(lengthSize)
            || !IsSupportedSize(baseOffsetSize)
            || !IsSupportedSize(indexSize)
            || (fullBox.Version == 0 && indexSize != 0))
        {
            return CreateResult(
                AvifResultCode.InvalidContainer,
                fullBox.Version,
                offsetSize,
                lengthSize,
                baseOffsetSize,
                indexSize,
                locations);
        }

        uint itemCount;
        if (fullBox.Version < 2)
        {
            itemCount = reader.ReadUInt16BigEndian(payload);
        }
        else
        {
            itemCount = reader.ReadUInt32BigEndian(payload);
        }

        if (reader.Code != AvifResultCode.Success)
        {
            return CreateResult(
                HandlerBox.NormalizeReaderCode(reader.Code),
                fullBox.Version,
                offsetSize,
                lengthSize,
                baseOffsetSize,
                indexSize,
                locations);
        }

        if (itemCount > int.MaxValue || itemCount > resources.Limits.MaxItemCount)
        {
            return CreateResult(
                AvifResultCode.LimitExceeded,
                fullBox.Version,
                offsetSize,
                lengthSize,
                baseOffsetSize,
                indexSize,
                locations);
        }

        IsoBox? idatBox = null;
        bool duplicateIdat = false;
        int metaChildIndex = 0;
        while (metaChildIndex < metaBox.Children.Count)
        {
            IsoBox child = metaBox.Children[metaChildIndex];
            if (child.Type == "idat")
            {
                if (idatBox == null)
                {
                    idatBox = child;
                }
                else
                {
                    duplicateIdat = true;
                }
            }

            metaChildIndex++;
        }

        HashSet<uint> itemIds = new HashSet<uint>();
        uint itemIndex = 0;
        while (itemIndex < itemCount)
        {
            uint itemId;
            if (fullBox.Version < 2)
            {
                itemId = reader.ReadUInt16BigEndian(payload);
            }
            else
            {
                itemId = reader.ReadUInt32BigEndian(payload);
            }

            int constructionMethod = 0;
            if (fullBox.Version == 1 || fullBox.Version == 2)
            {
                ushort methodField = reader.ReadUInt16BigEndian(payload);
                if ((methodField & 65520) != 0)
                {
                    return CreateResult(
                        AvifResultCode.InvalidItem,
                        fullBox.Version,
                        offsetSize,
                        lengthSize,
                        baseOffsetSize,
                        indexSize,
                        locations);
                }

                constructionMethod = methodField & 15;
            }

            ushort dataReferenceIndex = reader.ReadUInt16BigEndian(payload);
            SizedIntegerReadResult baseOffsetRead = ReadSizedInteger(
                reader,
                payload,
                baseOffsetSize);
            ushort extentCount = reader.ReadUInt16BigEndian(payload);
            if (reader.Code != AvifResultCode.Success
                || baseOffsetRead.Code != AvifResultCode.Success)
            {
                return CreateResult(
                    AvifResultCode.InvalidContainer,
                    fullBox.Version,
                    offsetSize,
                    lengthSize,
                    baseOffsetSize,
                    indexSize,
                    locations);
            }

            if (itemId == 0 || !itemIds.Add(itemId))
            {
                return CreateResult(
                    AvifResultCode.InvalidItem,
                    fullBox.Version,
                    offsetSize,
                    lengthSize,
                    baseOffsetSize,
                    indexSize,
                    locations);
            }

            if (constructionMethod < 0 || constructionMethod > 2)
            {
                return CreateResult(
                    AvifResultCode.UnsupportedFeature,
                    fullBox.Version,
                    offsetSize,
                    lengthSize,
                    baseOffsetSize,
                    indexSize,
                    locations);
            }

            if ((constructionMethod == 1 || constructionMethod == 2)
                && dataReferenceIndex != 0)
            {
                return CreateResult(
                    AvifResultCode.InvalidItem,
                    fullBox.Version,
                    offsetSize,
                    lengthSize,
                    baseOffsetSize,
                    indexSize,
                    locations);
            }

            if (constructionMethod == 1 && (idatBox == null || duplicateIdat))
            {
                return CreateResult(
                    AvifResultCode.InvalidItem,
                    fullBox.Version,
                    offsetSize,
                    lengthSize,
                    baseOffsetSize,
                    indexSize,
                    locations);
            }

            AvifResultCode extentLimitCode = resources.CountReferences(extentCount);
            if (extentLimitCode != AvifResultCode.Success)
            {
                return CreateResult(
                    extentLimitCode,
                    fullBox.Version,
                    offsetSize,
                    lengthSize,
                    baseOffsetSize,
                    indexSize,
                    locations);
            }

            List<ParsedItemExtent> extents = new List<ParsedItemExtent>();
            bool hasKnownTotalLength = true;
            ulong totalLength = 0;
            int extentIndex = 0;
            while (extentIndex < extentCount)
            {
                ulong referenceIndex = 0;
                if ((fullBox.Version == 1 || fullBox.Version == 2) && indexSize > 0)
                {
                    SizedIntegerReadResult indexRead = ReadSizedInteger(
                        reader,
                        payload,
                        indexSize);
                    if (indexRead.Code != AvifResultCode.Success)
                    {
                        return CreateResult(
                            AvifResultCode.InvalidContainer,
                            fullBox.Version,
                            offsetSize,
                            lengthSize,
                            baseOffsetSize,
                            indexSize,
                            locations);
                    }

                    referenceIndex = indexRead.Value;
                }
                else if (constructionMethod == 2)
                {
                    referenceIndex = 1;
                }

                SizedIntegerReadResult extentOffsetRead = ReadSizedInteger(
                    reader,
                    payload,
                    offsetSize);
                SizedIntegerReadResult extentLengthRead = ReadSizedInteger(
                    reader,
                    payload,
                    lengthSize);
                if (extentOffsetRead.Code != AvifResultCode.Success
                    || extentLengthRead.Code != AvifResultCode.Success)
                {
                    return CreateResult(
                        AvifResultCode.InvalidContainer,
                        fullBox.Version,
                        offsetSize,
                        lengthSize,
                        baseOffsetSize,
                        indexSize,
                        locations);
                }

                if (constructionMethod == 2)
                {
                    if (referenceIndex == 0)
                    {
                        return CreateResult(
                            AvifResultCode.InvalidItem,
                            fullBox.Version,
                            offsetSize,
                            lengthSize,
                            baseOffsetSize,
                            indexSize,
                            locations);
                    }
                }
                else if (referenceIndex != 0)
                {
                    return CreateResult(
                        AvifResultCode.InvalidItem,
                        fullBox.Version,
                        offsetSize,
                        lengthSize,
                        baseOffsetSize,
                        indexSize,
                        locations);
                }

                if (extentOffsetRead.Value > ulong.MaxValue - baseOffsetRead.Value)
                {
                    return CreateResult(
                        AvifResultCode.LimitExceeded,
                        fullBox.Version,
                        offsetSize,
                        lengthSize,
                        baseOffsetSize,
                        indexSize,
                        locations);
                }

                ulong relativeStart = baseOffsetRead.Value + extentOffsetRead.Value;
                if (extentLengthRead.Value > ulong.MaxValue - relativeStart)
                {
                    return CreateResult(
                        AvifResultCode.LimitExceeded,
                        fullBox.Version,
                        offsetSize,
                        lengthSize,
                        baseOffsetSize,
                        indexSize,
                        locations);
                }

                bool hasResolvedRange = false;
                long resolvedOffset = 0;
                long resolvedLength = 0;
                if (constructionMethod == 0 && dataReferenceIndex == 0)
                {
                    ItemRangeResolveResult resolved = ResolveRange(
                        relativeStart,
                        extentLengthRead.Value,
                        0,
                        source.Length);
                    if (resolved.Code != AvifResultCode.Success)
                    {
                        return CreateResult(
                            resolved.Code,
                            fullBox.Version,
                            offsetSize,
                            lengthSize,
                            baseOffsetSize,
                            indexSize,
                            locations);
                    }

                    resolvedOffset = resolved.Offset;
                    resolvedLength = resolved.Length;
                    hasResolvedRange = true;
                }
                else if (constructionMethod == 1)
                {
                    ItemRangeResolveResult resolved = ResolveRange(
                        relativeStart,
                        extentLengthRead.Value,
                        (idatBox ?? throw new InvalidOperationException("Invalid internal state.")).PayloadOffset,
                        idatBox.PayloadSize);
                    if (resolved.Code != AvifResultCode.Success)
                    {
                        return CreateResult(
                            resolved.Code,
                            fullBox.Version,
                            offsetSize,
                            lengthSize,
                            baseOffsetSize,
                            indexSize,
                            locations);
                    }

                    resolvedOffset = resolved.Offset;
                    resolvedLength = resolved.Length;
                    hasResolvedRange = true;
                }

                ulong logicalLength = extentLengthRead.Value;
                if (hasResolvedRange)
                {
                    logicalLength = (ulong)resolvedLength;
                }
                else if (logicalLength == 0)
                {
                    hasKnownTotalLength = false;
                }

                if (hasKnownTotalLength)
                {
                    if (logicalLength > ulong.MaxValue - totalLength)
                    {
                        return CreateResult(
                            AvifResultCode.LimitExceeded,
                            fullBox.Version,
                            offsetSize,
                            lengthSize,
                            baseOffsetSize,
                            indexSize,
                            locations);
                    }

                    totalLength += logicalLength;
                }

                extents.Add(new ParsedItemExtent(
                    referenceIndex,
                    extentOffsetRead.Value,
                    extentLengthRead.Value,
                    hasResolvedRange,
                    resolvedOffset,
                    resolvedLength));
                extentIndex++;
            }

            if (!hasKnownTotalLength)
            {
                totalLength = 0;
            }

            AvifResultCode overlapCode = ValidateNoOverlap(
                extents,
                constructionMethod,
                baseOffsetRead.Value);
            if (overlapCode != AvifResultCode.Success)
            {
                return CreateResult(
                    overlapCode,
                    fullBox.Version,
                    offsetSize,
                    lengthSize,
                    baseOffsetSize,
                    indexSize,
                    locations);
            }

            locations.Add(new ParsedItemLocation(
                itemId,
                constructionMethod,
                dataReferenceIndex,
                baseOffsetRead.Value,
                hasKnownTotalLength,
                totalLength,
                extents));
            itemIndex++;
        }

        if (reader.Code != AvifResultCode.Success || reader.Remaining() != 0)
        {
            return CreateResult(
                AvifResultCode.InvalidContainer,
                fullBox.Version,
                offsetSize,
                lengthSize,
                baseOffsetSize,
                indexSize,
                locations);
        }

        return CreateResult(
            AvifResultCode.Success,
            fullBox.Version,
            offsetSize,
            lengthSize,
            baseOffsetSize,
            indexSize,
            locations);
    }

    internal static bool IsSupportedSize(int size)
    {
        return size == 0 || size == 4 || size == 8;
    }

    internal static AvifResultCode ResolveDataReferences(
        List<ParsedItemLocation> locations,
        List<ParsedDataReference> references,
        long sourceLength)
    {
        if (locations == null || references == null || sourceLength < 0)
        {
            return AvifResultCode.InvalidArgument;
        }

        int locationIndex = 0;
        while (locationIndex < locations.Count)
        {
            ParsedItemLocation location = locations[locationIndex];
            ushort dataReferenceIndex = location.GetDataReferenceIndex();
            if (dataReferenceIndex == 0)
            {
                locationIndex++;
                continue;
            }

            if (dataReferenceIndex > references.Count)
            {
                return AvifResultCode.InvalidItem;
            }

            ParsedDataReference reference = references[dataReferenceIndex - 1];
            List<ParsedItemExtent> resolvedExtents = new List<ParsedItemExtent>();
            bool hasKnownTotalLength = location.HasKnownTotalLength();
            ulong totalLength = location.GetTotalLength();
            if (reference.IsSelfContained())
            {
                hasKnownTotalLength = true;
                totalLength = 0;
                int extentIndex = 0;
                while (extentIndex < location.GetExtentCount())
                {
                    ParsedItemExtent? extent = location.GetExtent(extentIndex);
                    if ((extent ?? throw new InvalidOperationException("Invalid internal state.")).GetExtentOffset() > ulong.MaxValue - location.GetBaseOffset())
                    {
                        return AvifResultCode.LimitExceeded;
                    }

                    ulong relativeStart = location.GetBaseOffset()
                        + extent.GetExtentOffset();
                    ItemRangeResolveResult resolved = ResolveRange(
                        relativeStart,
                        extent.GetExtentLength(),
                        0,
                        sourceLength);
                    if (resolved.Code != AvifResultCode.Success)
                    {
                        return resolved.Code;
                    }

                    if ((ulong)resolved.Length > ulong.MaxValue - totalLength)
                    {
                        return AvifResultCode.LimitExceeded;
                    }

                    totalLength += (ulong)resolved.Length;
                    resolvedExtents.Add(extent.WithResolvedRange(
                        resolved.Offset,
                        resolved.Length));
                    extentIndex++;
                }
            }
            else
            {
                int extentIndex = 0;
                while (extentIndex < location.GetExtentCount())
                {
                    resolvedExtents.Add(location.GetExtent(extentIndex)
                        ?? throw new InvalidOperationException("Invalid internal state."));
                    extentIndex++;
                }
            }

            locations[locationIndex] = location.WithDataReference(
                reference,
                hasKnownTotalLength,
                totalLength,
                resolvedExtents);
            locationIndex++;
        }

        return AvifResultCode.Success;
    }

    internal static SizedIntegerReadResult ReadSizedInteger(
        ByteReader reader,
        ReadOnlySpan<byte> source,
        int size)
    {
        if (size == 0)
        {
            return new SizedIntegerReadResult(AvifResultCode.Success, 0);
        }

        ulong value;
        if (size == 4)
        {
            value = reader.ReadUInt32BigEndian(source);
        }
        else if (size == 8)
        {
            value = reader.ReadUInt64BigEndian(source);
        }
        else
        {
            return new SizedIntegerReadResult(AvifResultCode.InvalidContainer, 0);
        }

        return new SizedIntegerReadResult(reader.Code, value);
    }

    internal static ItemRangeResolveResult ResolveRange(
        ulong relativeStart,
        ulong declaredLength,
        long originOffset,
        long originLength)
    {
        if (originOffset < 0 || originLength < 0)
        {
            return new ItemRangeResolveResult(AvifResultCode.InvalidArgument, 0, 0);
        }

        if (relativeStart > (ulong)originLength)
        {
            return new ItemRangeResolveResult(AvifResultCode.InvalidItem, 0, 0);
        }

        ulong available = (ulong)originLength - relativeStart;
        ulong effectiveLength = declaredLength == 0 ? available : declaredLength;
        if (effectiveLength > available
            || relativeStart > long.MaxValue
            || effectiveLength > long.MaxValue)
        {
            return new ItemRangeResolveResult(AvifResultCode.InvalidItem, 0, 0);
        }

        CheckedIntegerResult absoluteStart = CheckedMath.AddInt64(
            originOffset,
            (long)relativeStart);
        if (!absoluteStart.Success)
        {
            return new ItemRangeResolveResult(absoluteStart.Code, 0, 0);
        }

        CheckedIntegerResult absoluteEnd = CheckedMath.AddInt64(
            absoluteStart.Value,
            (long)effectiveLength);
        if (!absoluteEnd.Success)
        {
            return new ItemRangeResolveResult(absoluteEnd.Code, 0, 0);
        }

        return new ItemRangeResolveResult(
            AvifResultCode.Success,
            absoluteStart.Value,
            (long)effectiveLength);
    }

    internal static AvifResultCode ValidateNoOverlap(
        List<ParsedItemExtent> extents,
        int constructionMethod,
        ulong baseOffset)
    {
        if (extents.Count < 2)
        {
            return AvifResultCode.Success;
        }

        List<ParsedItemExtent> sorted = new List<ParsedItemExtent>(extents);
        sorted.Sort(CompareExtents);
        bool hasRange = false;
        ulong previousIndex = 0;
        ulong maximumEnd = 0;
        int index = 0;
        while (index < sorted.Count)
        {
            ParsedItemExtent extent = sorted[index];
            ulong referenceIndex = constructionMethod == 2 ? extent.GetExtentIndex() : 0;
            ulong start;
            ulong length;
            if (extent.HasResolvedRange())
            {
                start = (ulong)extent.GetResolvedOffset();
                length = (ulong)extent.GetResolvedLength();
            }
            else
            {
                if (extent.GetExtentOffset() > ulong.MaxValue - baseOffset)
                {
                    return AvifResultCode.LimitExceeded;
                }

                start = baseOffset + extent.GetExtentOffset();
                length = extent.GetExtentLength();
            }

            if (length == 0)
            {
                index++;
                continue;
            }

            if (length > ulong.MaxValue - start)
            {
                return AvifResultCode.LimitExceeded;
            }

            ulong end = start + length;
            if (!hasRange || referenceIndex != previousIndex)
            {
                hasRange = true;
                previousIndex = referenceIndex;
                maximumEnd = end;
            }
            else
            {
                if (start < maximumEnd)
                {
                    return AvifResultCode.InvalidItem;
                }

                if (end > maximumEnd)
                {
                    maximumEnd = end;
                }
            }

            index++;
        }

        return AvifResultCode.Success;
    }

    internal static int CompareExtents(ParsedItemExtent left, ParsedItemExtent right)
    {
        int indexComparison = left.GetExtentIndex().CompareTo(right.GetExtentIndex());
        if (indexComparison != 0)
        {
            return indexComparison;
        }

        if (left.HasResolvedRange() && right.HasResolvedRange())
        {
            return left.GetResolvedOffset().CompareTo(right.GetResolvedOffset());
        }

        return left.GetExtentOffset().CompareTo(right.GetExtentOffset());
    }

    internal static ItemLocationBoxParseResult CreateResult(
        AvifResultCode code,
        int version,
        int offsetSize,
        int lengthSize,
        int baseOffsetSize,
        int indexSize,
        List<ParsedItemLocation> locations)
    {
        return new ItemLocationBoxParseResult(
            code,
            version,
            offsetSize,
            lengthSize,
            baseOffsetSize,
            indexSize,
            locations);
    }
}
