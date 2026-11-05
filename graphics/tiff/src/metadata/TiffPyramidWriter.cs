using System;

namespace tiff;

internal class TiffPyramidWriter
{
    internal const ulong ReducedResolutionMask = 1;

    internal static TiffPyramidWriteResult WriteClassic(Span<byte> destination, TiffIfdEntry[] parentEntries, TiffIfdEntry[][] overviewDirectories, TiffByteOrder byteOrder, ulong parentOffset, ulong nextIfdOffset)
    {
        return WriteClassic(destination, parentEntries, overviewDirectories, byteOrder, parentOffset, nextIfdOffset, new TiffWriteOptions());
    }

    internal static TiffPyramidWriteResult WriteClassic(Span<byte> destination, TiffIfdEntry[] parentEntries, TiffIfdEntry[][] overviewDirectories, TiffByteOrder byteOrder, ulong parentOffset, ulong nextIfdOffset, TiffWriteOptions options)
    {
        TiffIfdEntry[][][] subIfdChains = BuildOverviewSubIfdChains(overviewDirectories, byteOrder, 4);
        TiffDiagnostics diagnostics = TiffSubIfdWriter.WriteClassic(destination, parentEntries, subIfdChains, byteOrder, parentOffset, nextIfdOffset, options);
        ulong byteCount = 0;
        if (!diagnostics.HasErrors())
        {
            byteCount = TiffSubIfdWriter.CalculateClassicByteCountAsUInt64(parentEntries, subIfdChains);
        }

        return TiffPyramidWriteResult.Create(parentOffset, byteCount, diagnostics);
    }

    internal static TiffPyramidWriteResult WriteBigTiff(Span<byte> destination, TiffIfdEntry[] parentEntries, TiffIfdEntry[][] overviewDirectories, TiffByteOrder byteOrder, ulong parentOffset, ulong nextIfdOffset)
    {
        return WriteBigTiff(destination, parentEntries, overviewDirectories, byteOrder, parentOffset, nextIfdOffset, new TiffWriteOptions());
    }

    internal static TiffPyramidWriteResult WriteBigTiff(Span<byte> destination, TiffIfdEntry[] parentEntries, TiffIfdEntry[][] overviewDirectories, TiffByteOrder byteOrder, ulong parentOffset, ulong nextIfdOffset, TiffWriteOptions options)
    {
        TiffIfdEntry[][][] subIfdChains = BuildOverviewSubIfdChains(overviewDirectories, byteOrder, 8);
        TiffDiagnostics diagnostics = TiffSubIfdWriter.WriteBigTiff(destination, parentEntries, subIfdChains, byteOrder, parentOffset, nextIfdOffset, options);
        ulong byteCount = 0;
        if (!diagnostics.HasErrors())
        {
            byteCount = TiffSubIfdWriter.CalculateBigTiffByteCountAsUInt64(parentEntries, subIfdChains);
        }

        return TiffPyramidWriteResult.Create(parentOffset, byteCount, diagnostics);
    }

    internal static int CalculateClassicWriteByteCount(TiffIfdEntry[] parentEntries, TiffIfdEntry[][] overviewDirectories)
    {
        ulong byteCount = TiffSubIfdWriter.CalculateClassicByteCountAsUInt64(parentEntries, BuildOverviewSubIfdChains(overviewDirectories, TiffByteOrder.LittleEndian, 4));
        if (byteCount > int.MaxValue)
        {
            return int.MaxValue;
        }

        return (int)byteCount;
    }

    internal static int CalculateBigTiffWriteByteCount(TiffIfdEntry[] parentEntries, TiffIfdEntry[][] overviewDirectories)
    {
        ulong byteCount = TiffSubIfdWriter.CalculateBigTiffByteCountAsUInt64(parentEntries, BuildOverviewSubIfdChains(overviewDirectories, TiffByteOrder.LittleEndian, 8));
        if (byteCount > int.MaxValue)
        {
            return int.MaxValue;
        }

        return (int)byteCount;
    }

    internal static TiffIfdEntry[][][] BuildOverviewSubIfdChains(TiffIfdEntry[][] overviewDirectories, TiffByteOrder byteOrder, int slotByteCount)
    {
        if (overviewDirectories == null)
        {
            return new TiffIfdEntry[0][][];
        }

        TiffIfdEntry[][][] chains = new TiffIfdEntry[overviewDirectories.Length][][];
        int index = 0;
        while (index < overviewDirectories.Length)
        {
            chains[index] = new TiffIfdEntry[1][];
            chains[index][0] = BuildReducedOverviewEntries(overviewDirectories[index], byteOrder, slotByteCount);
            index = index + 1;
        }

        return chains;
    }

    internal static TiffIfdEntry[] BuildReducedOverviewEntries(TiffIfdEntry[] entries, TiffByteOrder byteOrder, int slotByteCount)
    {
        int count = CountNonNewSubfileTypeEntries(entries);
        TiffIfdEntry[] result = new TiffIfdEntry[count + 1];
        ulong newSubfileType = 0;
        int sourceIndex = 0;
        int targetIndex = 0;
        while (entries != null && sourceIndex < entries.Length)
        {
            if (entries[sourceIndex] != null && entries[sourceIndex].TagId == TiffTagId.NewSubfileType)
            {
                newSubfileType = newSubfileType | ReadNewSubfileTypeValue(entries[sourceIndex], byteOrder);
            }
            else if (entries[sourceIndex] != null)
            {
                result[targetIndex] = entries[sourceIndex].Copy();
                targetIndex = targetIndex + 1;
            }

            sourceIndex = sourceIndex + 1;
        }

        result[targetIndex] = CreateNewSubfileTypeEntry(newSubfileType | ReducedResolutionMask, byteOrder, slotByteCount);
        return result;
    }

    internal static int CountNonNewSubfileTypeEntries(TiffIfdEntry[] entries)
    {
        int count = 0;
        int index = 0;
        while (entries != null && index < entries.Length)
        {
            if (entries[index] != null && entries[index].TagId != TiffTagId.NewSubfileType)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal static ulong ReadNewSubfileTypeValue(TiffIfdEntry entry, TiffByteOrder byteOrder)
    {
        if (entry == null)
        {
            return 0;
        }

        if (entry.ValueOffsetBytes != null && entry.ValueOffsetBytes.Length >= 4)
        {
            return TiffBinaryReader.ReadUInt32(entry.ValueOffsetBytes.AsSpan(0, 4), byteOrder);
        }

        return entry.ValueOffset;
    }

    internal static TiffIfdEntry CreateNewSubfileTypeEntry(ulong value, TiffByteOrder byteOrder, int slotByteCount)
    {
        int actualSlotByteCount = slotByteCount;
        if (actualSlotByteCount != 8)
        {
            actualSlotByteCount = 4;
        }

        byte[] valueOffsetBytes = new byte[actualSlotByteCount];
        TiffBinaryWriter.WriteUInt32(valueOffsetBytes.AsSpan(0, 4), (uint)value, byteOrder);

        TiffIfdEntry entry = new TiffIfdEntry();
        entry.TagId = TiffTagId.NewSubfileType;
        entry.DataType = TiffDataType.Long;
        entry.Count = 1;
        entry.RawCount = 1;
        if (actualSlotByteCount == 8)
        {
            entry.ValueOffset = TiffBinaryReader.ReadUInt64(valueOffsetBytes.AsSpan(0, 8), byteOrder);
        }
        else
        {
            entry.ValueOffset = TiffBinaryReader.ReadUInt32(valueOffsetBytes.AsSpan(0, 4), byteOrder);
        }

        entry.ValueOffsetBytes = valueOffsetBytes;
        entry.ValueByteCount = 4;
        entry.ValueSlotByteCount = actualSlotByteCount;
        return entry;
    }
}
