using System;

namespace tiff;

internal class TiffSubIfdWriter
{
    internal static TiffDiagnostics WriteClassic(Span<byte> destination, TiffIfdEntry[] parentEntries, TiffIfdEntry[][][] subIfdChains, TiffByteOrder byteOrder, ulong parentOffset, ulong nextIfdOffset)
    {
        return WriteClassic(destination, parentEntries, subIfdChains, byteOrder, parentOffset, nextIfdOffset, new TiffWriteOptions());
    }

    internal static TiffDiagnostics WriteClassic(Span<byte> destination, TiffIfdEntry[] parentEntries, TiffIfdEntry[][][] subIfdChains, TiffByteOrder byteOrder, ulong parentOffset, ulong nextIfdOffset, TiffWriteOptions options)
    {
        TiffWriteOptions actualOptions = TiffDirectoryWriter.NormalizeOptions(options);
        TiffLimits limits = TiffDirectoryWriter.GetLimits(actualOptions);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateClassicWriteRequest(destination.Length, parentEntries, subIfdChains, byteOrder, parentOffset, nextIfdOffset, limits, diagnostics))
        {
            return diagnostics;
        }

        ulong[] offsets = CalculateClassicSubIfdOffsets(parentOffset, parentEntries, subIfdChains);
        TiffIfdEntry[] actualParentEntries = BuildParentEntries(parentEntries, CreateClassicSubIfdEntry(offsets, parentOffset, parentEntries));
        int parentTableByteCount = TiffDirectoryWriter.CalculateClassicDirectoryByteCount(actualParentEntries.Length);
        TiffDiagnostics parentDiagnostics = TiffDirectoryWriter.WriteClassic(destination.Slice((int)parentOffset, parentTableByteCount), actualParentEntries, byteOrder, nextIfdOffset, actualOptions);
        diagnostics.Merge(parentDiagnostics);

        WriteOffsetArray(destination, offsets, TiffDataType.Long, byteOrder, parentOffset, parentTableByteCount, 4, 2);
        WriteClassicSubIfdChains(destination, subIfdChains, offsets, byteOrder, actualOptions, diagnostics);
        return diagnostics;
    }

    internal static TiffDiagnostics WriteBigTiff(Span<byte> destination, TiffIfdEntry[] parentEntries, TiffIfdEntry[][][] subIfdChains, TiffByteOrder byteOrder, ulong parentOffset, ulong nextIfdOffset)
    {
        return WriteBigTiff(destination, parentEntries, subIfdChains, byteOrder, parentOffset, nextIfdOffset, new TiffWriteOptions());
    }

    internal static TiffDiagnostics WriteBigTiff(Span<byte> destination, TiffIfdEntry[] parentEntries, TiffIfdEntry[][][] subIfdChains, TiffByteOrder byteOrder, ulong parentOffset, ulong nextIfdOffset, TiffWriteOptions options)
    {
        TiffWriteOptions actualOptions = TiffDirectoryWriter.NormalizeOptions(options);
        TiffLimits limits = TiffDirectoryWriter.GetLimits(actualOptions);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateBigTiffWriteRequest(destination.Length, parentEntries, subIfdChains, byteOrder, parentOffset, limits, diagnostics))
        {
            return diagnostics;
        }

        ulong[] offsets = CalculateBigTiffSubIfdOffsets(parentOffset, parentEntries, subIfdChains);
        TiffIfdEntry[] actualParentEntries = BuildParentEntries(parentEntries, CreateBigTiffSubIfdEntry(offsets, parentOffset, parentEntries));
        int parentTableByteCount = TiffDirectoryWriter.CalculateBigTiffDirectoryByteCount(actualParentEntries.Length);
        TiffDiagnostics parentDiagnostics = TiffDirectoryWriter.WriteBigTiff(destination.Slice((int)parentOffset, parentTableByteCount), actualParentEntries, byteOrder, nextIfdOffset, actualOptions);
        diagnostics.Merge(parentDiagnostics);

        WriteOffsetArray(destination, offsets, TiffDataType.Long8, byteOrder, parentOffset, parentTableByteCount, 8, 8);
        WriteBigTiffSubIfdChains(destination, subIfdChains, offsets, byteOrder, actualOptions, diagnostics);
        return diagnostics;
    }

    internal static bool ValidateClassicWriteRequest(int destinationLength, TiffIfdEntry[] parentEntries, TiffIfdEntry[][][] subIfdChains, TiffByteOrder byteOrder, ulong parentOffset, ulong nextIfdOffset, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        bool valid = ValidateWriteRequest(destinationLength, parentEntries, subIfdChains, byteOrder, parentOffset, limits, 4, 2, true, diagnostics);
        if (!TiffMath.IsClassicOffset(nextIfdOffset))
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Classic parent IFD next offset must fit in 32 bits.");
        }

        return valid && !diagnostics.HasErrors();
    }

    internal static bool ValidateBigTiffWriteRequest(int destinationLength, TiffIfdEntry[] parentEntries, TiffIfdEntry[][][] subIfdChains, TiffByteOrder byteOrder, ulong parentOffset, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        return ValidateWriteRequest(destinationLength, parentEntries, subIfdChains, byteOrder, parentOffset, limits, 8, 8, false, diagnostics);
    }

    internal static bool ValidateWriteRequest(int destinationLength, TiffIfdEntry[] parentEntries, TiffIfdEntry[][][] subIfdChains, TiffByteOrder byteOrder, ulong parentOffset, TiffLimits limits, int slotByteCount, ulong alignment, bool classic, TiffDiagnostics diagnostics)
    {
        TiffLimits actualLimits = limits;
        if (actualLimits == null)
        {
            actualLimits = new TiffLimits();
        }

        if (byteOrder != TiffByteOrder.LittleEndian && byteOrder != TiffByteOrder.BigEndian)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "SubIFD writer requires a known byte order.");
        }

        if (parentEntries == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "SubIFD writer requires parent entries.");
        }

        if (subIfdChains == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "SubIFD writer requires SubIFD chains.");
        }

        if (!actualLimits.AllowsSubIfdDepth(1))
        {
            diagnostics.AddErrorForTag(TiffErrorCode.IfdCountLimitExceeded, "SubIFD writing exceeds the configured depth limit.", TiffDiagnostic.NoImageIndex, TiffTagId.SubIFDs);
        }

        if (subIfdChains != null && !actualLimits.AllowsIfdCount(1 + CountSubIfdDirectories(subIfdChains)))
        {
            diagnostics.AddError(TiffErrorCode.IfdCountLimitExceeded, "SubIFD writer directory count exceeds the configured limit.");
        }

        if (parentOffset == 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Parent IFD offset must be non-zero.");
        }

        if (parentOffset > int.MaxValue || parentOffset > (ulong)destinationLength)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Parent IFD offset is outside the destination buffer.");
        }

        if (!TiffDirectoryWriter.IsAlignedOffset(parentOffset, alignment))
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Parent IFD offset is not aligned.");
        }

        if (classic && !TiffMath.IsClassicOffset(parentOffset))
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Classic parent IFD offset must fit in 32 bits.");
        }

        if (parentEntries != null && subIfdChains != null)
        {
            ValidateLayout(destinationLength, parentEntries, subIfdChains, parentOffset, slotByteCount, alignment, classic, diagnostics);
        }

        return !diagnostics.HasErrors();
    }

    internal static void ValidateLayout(int destinationLength, TiffIfdEntry[] parentEntries, TiffIfdEntry[][][] subIfdChains, ulong parentOffset, int slotByteCount, ulong alignment, bool classic, TiffDiagnostics diagnostics)
    {
        ulong byteCount = CalculateByteCountAsUInt64(parentEntries, subIfdChains, slotByteCount, alignment);
        if (byteCount == ulong.MaxValue || byteCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "SubIFD byte layout exceeds the supported range.");
            return;
        }

        if (parentOffset > ulong.MaxValue - byteCount)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "SubIFD byte layout overflows the supported offset range.");
            return;
        }

        if (parentOffset + byteCount > (ulong)destinationLength)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "SubIFD byte layout is outside the destination buffer.");
        }

        if (classic)
        {
            ulong[] offsets = CalculateClassicSubIfdOffsets(parentOffset, parentEntries, subIfdChains);
            ValidateClassicOffsets(offsets, diagnostics);
        }

        ValidateSubIfdChains(subIfdChains, diagnostics);
    }

    internal static void ValidateClassicOffsets(ulong[] offsets, TiffDiagnostics diagnostics)
    {
        int index = 0;
        while (index < offsets.Length)
        {
            if (!TiffMath.IsClassicOffset(offsets[index]))
            {
                diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Classic SubIFD offset must fit in 32 bits.");
                return;
            }

            index = index + 1;
        }
    }

    internal static void ValidateSubIfdChains(TiffIfdEntry[][][] subIfdChains, TiffDiagnostics diagnostics)
    {
        int index = 0;
        while (subIfdChains != null && index < subIfdChains.Length)
        {
            if (subIfdChains[index] == null || subIfdChains[index].Length == 0)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidOffset, "Each SubIFD offset must reference at least one directory.", TiffDiagnostic.NoImageIndex, TiffTagId.SubIFDs);
                return;
            }

            index = index + 1;
        }
    }

    internal static int CalculateClassicByteCount(TiffIfdEntry[] parentEntries, TiffIfdEntry[][][] subIfdChains)
    {
        ulong byteCount = CalculateClassicByteCountAsUInt64(parentEntries, subIfdChains);
        if (byteCount > int.MaxValue)
        {
            return 0;
        }

        return (int)byteCount;
    }

    internal static int CalculateBigTiffByteCount(TiffIfdEntry[] parentEntries, TiffIfdEntry[][][] subIfdChains)
    {
        ulong byteCount = CalculateBigTiffByteCountAsUInt64(parentEntries, subIfdChains);
        if (byteCount > int.MaxValue)
        {
            return 0;
        }

        return (int)byteCount;
    }

    internal static ulong CalculateClassicByteCountAsUInt64(TiffIfdEntry[] parentEntries, TiffIfdEntry[][][] subIfdChains)
    {
        return CalculateByteCountAsUInt64(parentEntries, subIfdChains, 4, 2);
    }

    internal static ulong CalculateBigTiffByteCountAsUInt64(TiffIfdEntry[] parentEntries, TiffIfdEntry[][][] subIfdChains)
    {
        return CalculateByteCountAsUInt64(parentEntries, subIfdChains, 8, 8);
    }

    internal static ulong CalculateByteCountAsUInt64(TiffIfdEntry[] parentEntries, TiffIfdEntry[][][] subIfdChains, int slotByteCount, ulong alignment)
    {
        int parentEntryCount = CountParentEntriesWithSubIfd(parentEntries, GetSubIfdCount(subIfdChains));
        ulong position = (ulong)TiffDirectoryWriter.CalculateDirectoryTableByteCount(parentEntryCount, slotByteCount);
        ulong offsetArrayByteCount = (ulong)(GetSubIfdCount(subIfdChains) * slotByteCount);
        if (offsetArrayByteCount > (ulong)slotByteCount)
        {
            position = AlignAndAdd(position, offsetArrayByteCount, alignment);
            if (position == ulong.MaxValue)
            {
                return ulong.MaxValue;
            }
        }

        int index = 0;
        while (subIfdChains != null && index < subIfdChains.Length)
        {
            if (!TiffDirectoryWriter.CanAlignExternalValueOffset(position, alignment))
            {
                return ulong.MaxValue;
            }

            position = TiffDirectoryWriter.AlignExternalValueOffset(position, alignment);
            ulong chainByteCount = CalculateSubIfdChainByteCount(subIfdChains[index], slotByteCount);
            if (chainByteCount == ulong.MaxValue || position > ulong.MaxValue - chainByteCount)
            {
                return ulong.MaxValue;
            }

            position = position + chainByteCount;
            index = index + 1;
        }

        return position;
    }

    internal static ulong CalculateSubIfdChainByteCount(TiffIfdEntry[][] chain, int slotByteCount)
    {
        if (slotByteCount == 4)
        {
            return TiffDirectoryWriter.CalculateClassicDirectoryChainByteCountAsUInt64(chain);
        }

        return TiffDirectoryWriter.CalculateBigTiffDirectoryChainByteCountAsUInt64(chain);
    }

    internal static ulong AlignAndAdd(ulong offset, ulong byteCount, ulong alignment)
    {
        if (!TiffDirectoryWriter.CanAlignExternalValueOffset(offset, alignment))
        {
            return ulong.MaxValue;
        }

        ulong aligned = TiffDirectoryWriter.AlignExternalValueOffset(offset, alignment);
        if (aligned > ulong.MaxValue - byteCount)
        {
            return ulong.MaxValue;
        }

        return aligned + byteCount;
    }

    internal static ulong[] CalculateClassicSubIfdOffsets(ulong parentOffset, TiffIfdEntry[] parentEntries, TiffIfdEntry[][][] subIfdChains)
    {
        return CalculateSubIfdOffsets(parentOffset, parentEntries, subIfdChains, 4, 2);
    }

    internal static ulong[] CalculateBigTiffSubIfdOffsets(ulong parentOffset, TiffIfdEntry[] parentEntries, TiffIfdEntry[][][] subIfdChains)
    {
        return CalculateSubIfdOffsets(parentOffset, parentEntries, subIfdChains, 8, 8);
    }

    internal static ulong[] CalculateSubIfdOffsets(ulong parentOffset, TiffIfdEntry[] parentEntries, TiffIfdEntry[][][] subIfdChains, int slotByteCount, ulong alignment)
    {
        int subIfdCount = GetSubIfdCount(subIfdChains);
        ulong[] offsets = new ulong[subIfdCount];
        int parentEntryCount = CountParentEntriesWithSubIfd(parentEntries, subIfdCount);
        ulong position = (ulong)TiffDirectoryWriter.CalculateDirectoryTableByteCount(parentEntryCount, slotByteCount);
        ulong offsetArrayByteCount = (ulong)(subIfdCount * slotByteCount);
        if (offsetArrayByteCount > (ulong)slotByteCount)
        {
            position = AlignAndAdd(position, offsetArrayByteCount, alignment);
        }

        int index = 0;
        while (index < subIfdCount)
        {
            position = TiffDirectoryWriter.AlignExternalValueOffset(position, alignment);
            offsets[index] = parentOffset + position;
            ulong chainByteCount = CalculateSubIfdChainByteCount(subIfdChains[index], slotByteCount);
            position = position + chainByteCount;
            index = index + 1;
        }

        return offsets;
    }

    internal static TiffIfdEntry CreateClassicSubIfdEntry(ulong[] offsets, ulong parentOffset, TiffIfdEntry[] parentEntries)
    {
        ulong valueOffset = 0;
        if (offsets.Length == 1)
        {
            valueOffset = offsets[0];
        }
        else if (offsets.Length > 1)
        {
            int parentEntryCount = CountParentEntriesWithSubIfd(parentEntries, offsets.Length);
            ulong tableByteCount = (ulong)TiffDirectoryWriter.CalculateClassicDirectoryByteCount(parentEntryCount);
            valueOffset = parentOffset + TiffDirectoryWriter.AlignExternalValueOffset(tableByteCount, 2);
        }

        return CreateSubIfdEntry(TiffDataType.Long, offsets.Length, valueOffset, 4);
    }

    internal static TiffIfdEntry CreateBigTiffSubIfdEntry(ulong[] offsets, ulong parentOffset, TiffIfdEntry[] parentEntries)
    {
        ulong valueOffset = 0;
        if (offsets.Length == 1)
        {
            valueOffset = offsets[0];
        }
        else if (offsets.Length > 1)
        {
            int parentEntryCount = CountParentEntriesWithSubIfd(parentEntries, offsets.Length);
            ulong tableByteCount = (ulong)TiffDirectoryWriter.CalculateBigTiffDirectoryByteCount(parentEntryCount);
            valueOffset = parentOffset + TiffDirectoryWriter.AlignExternalValueOffset(tableByteCount, 8);
        }

        return CreateSubIfdEntry(TiffDataType.Long8, offsets.Length, valueOffset, 8);
    }

    internal static TiffIfdEntry CreateSubIfdEntry(TiffDataType dataType, int count, ulong valueOffset, int slotByteCount)
    {
        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = TiffTagId.SubIFDs;
        entry.DataType = dataType;
        entry.Count = count;
        entry.RawCount = (ulong)count;
        entry.ValueOffset = valueOffset;
        entry.ValueByteCount = TiffDataTypeInfo.CalculateValueByteCount(dataType, count);
        entry.ValueSlotByteCount = slotByteCount;
        entry.ValueOffsetBytes = new byte[slotByteCount];
        return entry;
    }

    internal static TiffIfdEntry[] BuildParentEntries(TiffIfdEntry[] parentEntries, TiffIfdEntry subIfdEntry)
    {
        int subIfdCount = (int)subIfdEntry.Count;
        if (subIfdCount == 0)
        {
            return RemoveSubIfdEntries(parentEntries);
        }

        int nonSubIfdCount = CountNonSubIfdEntries(parentEntries);
        TiffIfdEntry[] entries = new TiffIfdEntry[nonSubIfdCount + 1];
        int sourceIndex = 0;
        int targetIndex = 0;
        while (parentEntries != null && sourceIndex < parentEntries.Length)
        {
            if (parentEntries[sourceIndex] != null && parentEntries[sourceIndex].TagId != TiffTagId.SubIFDs)
            {
                entries[targetIndex] = parentEntries[sourceIndex].Copy();
                targetIndex = targetIndex + 1;
            }

            sourceIndex = sourceIndex + 1;
        }

        entries[targetIndex] = subIfdEntry.Copy();
        return entries;
    }

    internal static TiffIfdEntry[] RemoveSubIfdEntries(TiffIfdEntry[] parentEntries)
    {
        int count = CountNonSubIfdEntries(parentEntries);
        TiffIfdEntry[] entries = new TiffIfdEntry[count];
        int sourceIndex = 0;
        int targetIndex = 0;
        while (parentEntries != null && sourceIndex < parentEntries.Length)
        {
            if (parentEntries[sourceIndex] != null && parentEntries[sourceIndex].TagId != TiffTagId.SubIFDs)
            {
                entries[targetIndex] = parentEntries[sourceIndex].Copy();
                targetIndex = targetIndex + 1;
            }

            sourceIndex = sourceIndex + 1;
        }

        return entries;
    }

    internal static void WriteOffsetArray(Span<byte> destination, ulong[] offsets, TiffDataType dataType, TiffByteOrder byteOrder, ulong parentOffset, int parentTableByteCount, int elementSize, ulong alignment)
    {
        if ((ulong)(offsets.Length * elementSize) <= (ulong)elementSize)
        {
            return;
        }

        ulong relativeOffset = TiffDirectoryWriter.AlignExternalValueOffset((ulong)parentTableByteCount, alignment);
        int start = (int)(parentOffset + relativeOffset);
        int index = 0;
        while (index < offsets.Length)
        {
            int offset = start + (index * elementSize);
            if (dataType == TiffDataType.Long || dataType == TiffDataType.Ifd)
            {
                TiffBinaryWriter.WriteUInt32(destination.Slice(offset, 4), (uint)offsets[index], byteOrder);
            }
            else
            {
                TiffBinaryWriter.WriteUInt64(destination.Slice(offset, 8), offsets[index], byteOrder);
            }

            index = index + 1;
        }
    }

    internal static void WriteClassicSubIfdChains(Span<byte> destination, TiffIfdEntry[][][] subIfdChains, ulong[] offsets, TiffByteOrder byteOrder, TiffWriteOptions options, TiffDiagnostics diagnostics)
    {
        int index = 0;
        while (index < offsets.Length)
        {
            TiffDiagnostics chainDiagnostics = TiffDirectoryWriter.WriteClassicChain(destination, subIfdChains[index], byteOrder, offsets[index], options);
            diagnostics.Merge(chainDiagnostics);
            index = index + 1;
        }
    }

    internal static void WriteBigTiffSubIfdChains(Span<byte> destination, TiffIfdEntry[][][] subIfdChains, ulong[] offsets, TiffByteOrder byteOrder, TiffWriteOptions options, TiffDiagnostics diagnostics)
    {
        int index = 0;
        while (index < offsets.Length)
        {
            TiffDiagnostics chainDiagnostics = TiffDirectoryWriter.WriteBigTiffChain(destination, subIfdChains[index], byteOrder, offsets[index], options);
            diagnostics.Merge(chainDiagnostics);
            index = index + 1;
        }
    }

    internal static int CountParentEntriesWithSubIfd(TiffIfdEntry[] parentEntries, int subIfdCount)
    {
        int count = CountNonSubIfdEntries(parentEntries);
        if (subIfdCount > 0)
        {
            count = count + 1;
        }

        return count;
    }

    internal static int CountNonSubIfdEntries(TiffIfdEntry[] parentEntries)
    {
        int count = 0;
        int index = 0;
        while (parentEntries != null && index < parentEntries.Length)
        {
            if (parentEntries[index] != null && parentEntries[index].TagId != TiffTagId.SubIFDs)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal static int GetSubIfdCount(TiffIfdEntry[][][] subIfdChains)
    {
        if (subIfdChains == null)
        {
            return 0;
        }

        return subIfdChains.Length;
    }

    internal static int CountSubIfdDirectories(TiffIfdEntry[][][] subIfdChains)
    {
        int count = 0;
        int index = 0;
        while (subIfdChains != null && index < subIfdChains.Length)
        {
            count = count + TiffDirectoryWriter.GetDirectoryCount(subIfdChains[index]);
            index = index + 1;
        }

        return count;
    }
}
