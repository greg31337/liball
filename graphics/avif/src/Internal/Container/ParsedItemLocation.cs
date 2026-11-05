using System;
using System.Collections.Generic;

namespace avif;

internal class ParsedItemExtent
{
    ulong extentIndex;
    ulong extentOffset;
    ulong extentLength;
    bool hasResolvedRange;
    long resolvedOffset;
    long resolvedLength;
    ParsedDataView? dataView;

    internal ParsedItemExtent(
        ulong extentIndex,
        ulong extentOffset,
        ulong extentLength,
        bool hasResolvedRange,
        long resolvedOffset,
        long resolvedLength)
    {
        this.extentIndex = extentIndex;
        this.extentOffset = extentOffset;
        this.extentLength = extentLength;
        this.hasResolvedRange = hasResolvedRange;
        this.resolvedOffset = resolvedOffset;
        this.resolvedLength = resolvedLength;
        dataView = hasResolvedRange
            ? new ParsedDataView(resolvedOffset, resolvedLength)
            : null;
    }

    internal ulong GetExtentIndex()
    {
        return extentIndex;
    }

    internal ulong GetExtentOffset()
    {
        return extentOffset;
    }

    internal ulong GetExtentLength()
    {
        return extentLength;
    }

    internal bool HasResolvedRange()
    {
        return hasResolvedRange;
    }

    internal long GetResolvedOffset()
    {
        return resolvedOffset;
    }

    internal long GetResolvedLength()
    {
        return resolvedLength;
    }

    internal InputReadResult Read(
        ReadOnlySpan<byte> source,
        long offset,
        Span<byte> destination)
    {
        if (dataView == null)
        {
            return new InputReadResult(AvifResultCode.InvalidState, 0, 0, 0);
        }

        return dataView.Read(source, offset, destination);
    }

    internal InputReadResult ReadExact(
        ReadOnlySpan<byte> source,
        long offset,
        Span<byte> destination)
    {
        if (dataView == null)
        {
            return new InputReadResult(AvifResultCode.InvalidState, 0, 0, 0);
        }

        return dataView.ReadExact(source, offset, destination);
    }

    internal ParsedItemExtent WithResolvedRange(long offset, long length)
    {
        return new ParsedItemExtent(
            extentIndex,
            extentOffset,
            extentLength,
            true,
            offset,
            length);
    }
}

internal class ParsedItemLocation
{
    uint itemId;
    int constructionMethod;
    ushort dataReferenceIndex;
    ulong baseOffset;
    bool hasKnownTotalLength;
    ulong totalLength;
    List<ParsedItemExtent> extents;
    ParsedDataReference? dataReference;

    internal ParsedItemLocation(
        uint itemId,
        int constructionMethod,
        ushort dataReferenceIndex,
        ulong baseOffset,
        bool hasKnownTotalLength,
        ulong totalLength,
        List<ParsedItemExtent> extents)
    {
        this.itemId = itemId;
        this.constructionMethod = constructionMethod;
        this.dataReferenceIndex = dataReferenceIndex;
        this.baseOffset = baseOffset;
        this.hasKnownTotalLength = hasKnownTotalLength;
        this.totalLength = totalLength;
        this.extents = new List<ParsedItemExtent>(extents);
        dataReference = null;
    }

    internal uint GetItemId()
    {
        return itemId;
    }

    internal int GetConstructionMethod()
    {
        return constructionMethod;
    }

    internal ushort GetDataReferenceIndex()
    {
        return dataReferenceIndex;
    }

    internal ulong GetBaseOffset()
    {
        return baseOffset;
    }

    internal bool HasKnownTotalLength()
    {
        return hasKnownTotalLength;
    }

    internal ulong GetTotalLength()
    {
        return totalLength;
    }

    internal int GetExtentCount()
    {
        return extents.Count;
    }

    internal ParsedItemExtent? GetExtent(int index)
    {
        if (index < 0 || index >= extents.Count)
        {
            return null;
        }

        return extents[index];
    }

    internal bool HasDataReference()
    {
        return dataReference != null;
    }

    internal ParsedDataReference? GetDataReference()
    {
        return dataReference;
    }

    internal ParsedItemLocation WithDataReference(
        ParsedDataReference reference,
        bool knownTotalLength,
        ulong resolvedTotalLength,
        List<ParsedItemExtent> resolvedExtents)
    {
        ParsedItemLocation copy = new ParsedItemLocation(
            itemId,
            constructionMethod,
            dataReferenceIndex,
            baseOffset,
            knownTotalLength,
            resolvedTotalLength,
            resolvedExtents);
        copy.dataReference = reference;
        return copy;
    }
}
