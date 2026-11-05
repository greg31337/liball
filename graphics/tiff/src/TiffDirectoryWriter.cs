using System;

namespace tiff;

internal class TiffDirectoryWriter
{
    internal static TiffDiagnostics WriteClassic(Span<byte> destination, TiffDirectory directory, TiffByteOrder byteOrder)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A directory is required to write a classic IFD.");
            return diagnostics;
        }

        return WriteClassic(destination, directory.Entries, byteOrder, directory.NextIfdOffset, new TiffWriteOptions());
    }

    internal static TiffDiagnostics WriteClassic(Span<byte> destination, TiffIfdEntry[] entries, TiffByteOrder byteOrder, ulong nextIfdOffset)
    {
        return WriteClassic(destination, entries, byteOrder, nextIfdOffset, new TiffWriteOptions());
    }

    internal static TiffDiagnostics WriteClassic(Span<byte> destination, TiffIfdEntry[] entries, TiffByteOrder byteOrder, ulong nextIfdOffset, TiffWriteOptions options)
    {
        TiffWriteOptions actualOptions = NormalizeOptions(options);
        TiffLimits limits = GetLimits(actualOptions);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateClassicWriteRequest(destination.Length, entries, byteOrder, nextIfdOffset, limits, diagnostics))
        {
            return diagnostics;
        }

        TiffIfdEntry[] sortedEntries = SortEntriesByTagId(entries);
        TiffBinaryWriter.WriteUInt16(destination.Slice(0, 2), (ushort)sortedEntries.Length, byteOrder);
        int index = 0;
        while (index < sortedEntries.Length)
        {
            int offset = 2 + (index * 12);
            WriteClassicEntry(destination.Slice(offset, 12), sortedEntries[index], byteOrder, diagnostics);
            index = index + 1;
        }

        int nextOffsetPosition = 2 + (sortedEntries.Length * 12);
        TiffBinaryWriter.WriteUInt32(destination.Slice(nextOffsetPosition, 4), (uint)nextIfdOffset, byteOrder);
        return diagnostics;
    }

    internal static TiffDiagnostics WriteClassicChain(Span<byte> destination, TiffIfdEntry[][] directories, TiffByteOrder byteOrder, ulong firstIfdOffset)
    {
        return WriteClassicChain(destination, directories, byteOrder, firstIfdOffset, new TiffWriteOptions());
    }

    internal static TiffDiagnostics WriteClassicChain(Span<byte> destination, TiffIfdEntry[][] directories, TiffByteOrder byteOrder, ulong firstIfdOffset, TiffWriteOptions options)
    {
        TiffWriteOptions actualOptions = NormalizeOptions(options);
        TiffLimits limits = GetLimits(actualOptions);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateClassicChainWriteRequest(destination.Length, directories, byteOrder, firstIfdOffset, limits, diagnostics))
        {
            return diagnostics;
        }

        ulong currentOffset = firstIfdOffset;
        int index = 0;
        while (index < directories.Length)
        {
            int byteCount = CalculateClassicDirectoryByteCount(GetEntryCount(directories[index]));
            ulong nextOffset = CalculateNextDirectoryOffset(currentOffset, (ulong)byteCount, 2, index, directories.Length);
            TiffDiagnostics directoryDiagnostics = WriteClassic(destination.Slice((int)currentOffset, byteCount), directories[index], byteOrder, nextOffset, actualOptions);
            diagnostics.Merge(directoryDiagnostics);
            currentOffset = nextOffset;
            index = index + 1;
        }

        return diagnostics;
    }

    internal static TiffDiagnostics WriteClassicWithValueBytes(Span<byte> destination, TiffIfdEntry[] entries, byte[][] valueBytes, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset)
    {
        return WriteClassicWithValueBytes(destination, entries, valueBytes, byteOrder, directoryOffset, nextIfdOffset, new TiffWriteOptions());
    }

    internal static TiffDiagnostics WriteClassicWithValueBytes(Span<byte> destination, TiffIfdEntry[] entries, byte[][] valueBytes, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset, TiffWriteOptions options)
    {
        TiffWriteOptions actualOptions = NormalizeOptions(options);
        TiffLimits limits = GetLimits(actualOptions);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateClassicValueWriteRequest(destination.Length, entries, valueBytes, byteOrder, directoryOffset, nextIfdOffset, limits, diagnostics))
        {
            return diagnostics;
        }

        TiffIfdEntry[] sortedEntries = SortEntriesByTagId(entries);
        byte[][] sortedValues = SortValueBytesByTagId(entries, valueBytes);
        ulong valuePosition = (ulong)CalculateClassicDirectoryByteCount(sortedEntries.Length);

        TiffBinaryWriter.WriteUInt16(destination.Slice(0, 2), (ushort)sortedEntries.Length, byteOrder);
        int index = 0;
        while (index < sortedEntries.Length)
        {
            byte[] bytes = NormalizeValueBytes(sortedValues[index]);
            int entryOffset = 2 + (index * 12);
            if (bytes.Length <= 4)
            {
                WriteClassicEntryWithInlineValue(destination.Slice(entryOffset, 12), sortedEntries[index], bytes, byteOrder);
            }
            else
            {
                valuePosition = AlignExternalValueOffset(valuePosition, 2);
                ulong fileOffset = directoryOffset + valuePosition;
                WriteClassicEntryWithExternalValue(destination.Slice(entryOffset, 12), sortedEntries[index], fileOffset, byteOrder);
                CopyBytes(bytes, destination.Slice((int)valuePosition, bytes.Length));
                valuePosition = valuePosition + (ulong)bytes.Length;
            }

            index = index + 1;
        }

        int nextOffsetPosition = 2 + (sortedEntries.Length * 12);
        TiffBinaryWriter.WriteUInt32(destination.Slice(nextOffsetPosition, 4), (uint)nextIfdOffset, byteOrder);
        return diagnostics;
    }

    internal static TiffDiagnostics WriteClassicChainWithValueBytes(Span<byte> destination, TiffIfdEntry[][] directories, byte[][][] valueBytes, TiffByteOrder byteOrder, ulong firstIfdOffset)
    {
        return WriteClassicChainWithValueBytes(destination, directories, valueBytes, byteOrder, firstIfdOffset, new TiffWriteOptions());
    }

    internal static TiffDiagnostics WriteClassicChainWithValueBytes(Span<byte> destination, TiffIfdEntry[][] directories, byte[][][] valueBytes, TiffByteOrder byteOrder, ulong firstIfdOffset, TiffWriteOptions options)
    {
        TiffWriteOptions actualOptions = NormalizeOptions(options);
        TiffLimits limits = GetLimits(actualOptions);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateClassicValueChainWriteRequest(destination.Length, directories, valueBytes, byteOrder, firstIfdOffset, limits, diagnostics))
        {
            return diagnostics;
        }

        ulong currentOffset = firstIfdOffset;
        int index = 0;
        while (index < directories.Length)
        {
            int byteCount = CalculateClassicDirectoryLayoutByteCount(directories[index], valueBytes[index]);
            ulong nextOffset = CalculateNextDirectoryOffset(currentOffset, (ulong)byteCount, 2, index, directories.Length);
            TiffDiagnostics directoryDiagnostics = WriteClassicWithValueBytes(destination.Slice((int)currentOffset, byteCount), directories[index], valueBytes[index], byteOrder, currentOffset, nextOffset, actualOptions);
            diagnostics.Merge(directoryDiagnostics);
            currentOffset = nextOffset;
            index = index + 1;
        }

        return diagnostics;
    }

    internal static void WriteClassicEntry(Span<byte> destination, TiffIfdEntry entry, TiffByteOrder byteOrder, TiffDiagnostics diagnostics)
    {
        if (!ValidateClassicEntry(entry, diagnostics))
        {
            return;
        }

        TiffBinaryWriter.WriteUInt16(destination.Slice(0, 2), (ushort)entry.TagId, byteOrder);
        TiffBinaryWriter.WriteUInt16(destination.Slice(2, 2), (ushort)entry.DataType, byteOrder);
        TiffBinaryWriter.WriteUInt32(destination.Slice(4, 4), (uint)entry.Count, byteOrder);
        TiffBinaryWriter.WriteUInt32(destination.Slice(8, 4), (uint)entry.ValueOffset, byteOrder);
    }

    internal static TiffDiagnostics WriteBigTiff(Span<byte> destination, TiffDirectory directory, TiffByteOrder byteOrder)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A directory is required to write a BigTIFF IFD.");
            return diagnostics;
        }

        return WriteBigTiff(destination, directory.Entries, byteOrder, directory.NextIfdOffset, new TiffWriteOptions());
    }

    internal static TiffDiagnostics WriteBigTiff(Span<byte> destination, TiffIfdEntry[] entries, TiffByteOrder byteOrder, ulong nextIfdOffset)
    {
        return WriteBigTiff(destination, entries, byteOrder, nextIfdOffset, new TiffWriteOptions());
    }

    internal static TiffDiagnostics WriteBigTiff(Span<byte> destination, TiffIfdEntry[] entries, TiffByteOrder byteOrder, ulong nextIfdOffset, TiffWriteOptions options)
    {
        TiffWriteOptions actualOptions = NormalizeOptions(options);
        TiffLimits limits = GetLimits(actualOptions);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateBigTiffWriteRequest(destination.Length, entries, byteOrder, limits, diagnostics))
        {
            return diagnostics;
        }

        TiffIfdEntry[] sortedEntries = SortEntriesByTagId(entries);
        TiffBinaryWriter.WriteUInt64(destination.Slice(0, 8), (ulong)sortedEntries.Length, byteOrder);
        int index = 0;
        while (index < sortedEntries.Length)
        {
            int offset = 8 + (index * 20);
            WriteBigTiffEntry(destination.Slice(offset, 20), sortedEntries[index], byteOrder, diagnostics);
            index = index + 1;
        }

        int nextOffsetPosition = 8 + (sortedEntries.Length * 20);
        TiffBinaryWriter.WriteUInt64(destination.Slice(nextOffsetPosition, 8), nextIfdOffset, byteOrder);
        return diagnostics;
    }

    internal static TiffDiagnostics WriteBigTiffChain(Span<byte> destination, TiffIfdEntry[][] directories, TiffByteOrder byteOrder, ulong firstIfdOffset)
    {
        return WriteBigTiffChain(destination, directories, byteOrder, firstIfdOffset, new TiffWriteOptions());
    }

    internal static TiffDiagnostics WriteBigTiffChain(Span<byte> destination, TiffIfdEntry[][] directories, TiffByteOrder byteOrder, ulong firstIfdOffset, TiffWriteOptions options)
    {
        TiffWriteOptions actualOptions = NormalizeOptions(options);
        TiffLimits limits = GetLimits(actualOptions);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateBigTiffChainWriteRequest(destination.Length, directories, byteOrder, firstIfdOffset, limits, diagnostics))
        {
            return diagnostics;
        }

        ulong currentOffset = firstIfdOffset;
        int index = 0;
        while (index < directories.Length)
        {
            int byteCount = CalculateBigTiffDirectoryByteCount(GetEntryCount(directories[index]));
            ulong nextOffset = CalculateNextDirectoryOffset(currentOffset, (ulong)byteCount, 8, index, directories.Length);
            TiffDiagnostics directoryDiagnostics = WriteBigTiff(destination.Slice((int)currentOffset, byteCount), directories[index], byteOrder, nextOffset, actualOptions);
            diagnostics.Merge(directoryDiagnostics);
            currentOffset = nextOffset;
            index = index + 1;
        }

        return diagnostics;
    }

    internal static TiffDiagnostics WriteBigTiffWithValueBytes(Span<byte> destination, TiffIfdEntry[] entries, byte[][] valueBytes, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset)
    {
        return WriteBigTiffWithValueBytes(destination, entries, valueBytes, byteOrder, directoryOffset, nextIfdOffset, new TiffWriteOptions());
    }

    internal static TiffDiagnostics WriteBigTiffWithValueBytes(Span<byte> destination, TiffIfdEntry[] entries, byte[][] valueBytes, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset, TiffWriteOptions options)
    {
        TiffWriteOptions actualOptions = NormalizeOptions(options);
        TiffLimits limits = GetLimits(actualOptions);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateBigTiffValueWriteRequest(destination.Length, entries, valueBytes, byteOrder, directoryOffset, limits, diagnostics))
        {
            return diagnostics;
        }

        TiffIfdEntry[] sortedEntries = SortEntriesByTagId(entries);
        byte[][] sortedValues = SortValueBytesByTagId(entries, valueBytes);
        ulong valuePosition = (ulong)CalculateBigTiffDirectoryByteCount(sortedEntries.Length);

        TiffBinaryWriter.WriteUInt64(destination.Slice(0, 8), (ulong)sortedEntries.Length, byteOrder);
        int index = 0;
        while (index < sortedEntries.Length)
        {
            byte[] bytes = NormalizeValueBytes(sortedValues[index]);
            int entryOffset = 8 + (index * 20);
            if (bytes.Length <= 8)
            {
                WriteBigTiffEntryWithInlineValue(destination.Slice(entryOffset, 20), sortedEntries[index], bytes, byteOrder);
            }
            else
            {
                valuePosition = AlignExternalValueOffset(valuePosition, 8);
                ulong fileOffset = directoryOffset + valuePosition;
                WriteBigTiffEntryWithExternalValue(destination.Slice(entryOffset, 20), sortedEntries[index], fileOffset, byteOrder);
                CopyBytes(bytes, destination.Slice((int)valuePosition, bytes.Length));
                valuePosition = valuePosition + (ulong)bytes.Length;
            }

            index = index + 1;
        }

        int nextOffsetPosition = 8 + (sortedEntries.Length * 20);
        TiffBinaryWriter.WriteUInt64(destination.Slice(nextOffsetPosition, 8), nextIfdOffset, byteOrder);
        return diagnostics;
    }

    internal static TiffDiagnostics WriteBigTiffChainWithValueBytes(Span<byte> destination, TiffIfdEntry[][] directories, byte[][][] valueBytes, TiffByteOrder byteOrder, ulong firstIfdOffset)
    {
        return WriteBigTiffChainWithValueBytes(destination, directories, valueBytes, byteOrder, firstIfdOffset, new TiffWriteOptions());
    }

    internal static TiffDiagnostics WriteBigTiffChainWithValueBytes(Span<byte> destination, TiffIfdEntry[][] directories, byte[][][] valueBytes, TiffByteOrder byteOrder, ulong firstIfdOffset, TiffWriteOptions options)
    {
        TiffWriteOptions actualOptions = NormalizeOptions(options);
        TiffLimits limits = GetLimits(actualOptions);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateBigTiffValueChainWriteRequest(destination.Length, directories, valueBytes, byteOrder, firstIfdOffset, limits, diagnostics))
        {
            return diagnostics;
        }

        ulong currentOffset = firstIfdOffset;
        int index = 0;
        while (index < directories.Length)
        {
            int byteCount = CalculateBigTiffDirectoryLayoutByteCount(directories[index], valueBytes[index]);
            ulong nextOffset = CalculateNextDirectoryOffset(currentOffset, (ulong)byteCount, 8, index, directories.Length);
            TiffDiagnostics directoryDiagnostics = WriteBigTiffWithValueBytes(destination.Slice((int)currentOffset, byteCount), directories[index], valueBytes[index], byteOrder, currentOffset, nextOffset, actualOptions);
            diagnostics.Merge(directoryDiagnostics);
            currentOffset = nextOffset;
            index = index + 1;
        }

        return diagnostics;
    }

    internal static void WriteBigTiffEntry(Span<byte> destination, TiffIfdEntry entry, TiffByteOrder byteOrder, TiffDiagnostics diagnostics)
    {
        if (!ValidateBigTiffEntry(entry, diagnostics))
        {
            return;
        }

        TiffBinaryWriter.WriteUInt16(destination.Slice(0, 2), (ushort)entry.TagId, byteOrder);
        TiffBinaryWriter.WriteUInt16(destination.Slice(2, 2), (ushort)entry.DataType, byteOrder);
        TiffBinaryWriter.WriteUInt64(destination.Slice(4, 8), (ulong)entry.Count, byteOrder);
        TiffBinaryWriter.WriteUInt64(destination.Slice(12, 8), entry.ValueOffset, byteOrder);
    }

    internal static bool ValidateClassicWriteRequest(int destinationLength, TiffIfdEntry[] entries, TiffByteOrder byteOrder, ulong nextIfdOffset, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        int entryCount = GetEntryCount(entries);
        if (byteOrder != TiffByteOrder.LittleEndian && byteOrder != TiffByteOrder.BigEndian)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "Classic IFD writer requires a known byte order.");
        }

        if (entries == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Classic IFD writer requires entries.");
        }

        if (entryCount > ushort.MaxValue || !limits.AllowsTagCountPerIfd(entryCount))
        {
            diagnostics.AddError(TiffErrorCode.TagCountLimitExceeded, "Classic IFD entry count exceeds the supported or configured limit.");
        }

        if (!TiffMath.IsClassicOffset(nextIfdOffset))
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Classic IFD next offset must fit in 32 bits.");
        }

        int requiredLength = CalculateClassicDirectoryByteCount(entryCount);
        if (destinationLength < requiredLength)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Classic IFD destination buffer is too short.");
        }

        int index = 0;
        while (entries != null && index < entries.Length)
        {
            ValidateClassicEntry(entries[index], diagnostics);
            index = index + 1;
        }

        return !diagnostics.HasErrors();
    }

    internal static bool ValidateClassicChainWriteRequest(int destinationLength, TiffIfdEntry[][] directories, TiffByteOrder byteOrder, ulong firstIfdOffset, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        return ValidateDirectoryChainWriteRequest(destinationLength, directories, new byte[0][][], byteOrder, firstIfdOffset, limits, 4, 2, true, false, diagnostics);
    }

    internal static bool ValidateClassicValueWriteRequest(int destinationLength, TiffIfdEntry[] entries, byte[][] valueBytes, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        int entryCount = GetEntryCount(entries);
        if (byteOrder != TiffByteOrder.LittleEndian && byteOrder != TiffByteOrder.BigEndian)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "Classic IFD value writer requires a known byte order.");
        }

        if (entries == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Classic IFD value writer requires entries.");
        }

        if (valueBytes == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Classic IFD value writer requires value byte arrays.");
        }

        if (entries != null && valueBytes != null && entries.Length != valueBytes.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagCount, "Classic IFD value byte array count must match the entry count.");
        }

        if (entryCount > ushort.MaxValue || !limits.AllowsTagCountPerIfd(entryCount))
        {
            diagnostics.AddError(TiffErrorCode.TagCountLimitExceeded, "Classic IFD entry count exceeds the supported or configured limit.");
        }

        if (!TiffMath.IsClassicOffset(nextIfdOffset))
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Classic IFD next offset must fit in 32 bits.");
        }

        if (entries != null && valueBytes != null && entries.Length == valueBytes.Length)
        {
            ValidateValueBytes(entries, valueBytes, true, diagnostics);
            ulong requiredLength = CalculateClassicDirectoryLayoutByteCountAsUInt64(entries, valueBytes);
            if (requiredLength > (ulong)destinationLength)
            {
                diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Classic IFD destination buffer is too short for entries and external values.");
            }

            if (!ValidateExternalValueOffsets(entries, valueBytes, 4, 2, directoryOffset, true, diagnostics))
            {
                return false;
            }
        }

        return !diagnostics.HasErrors();
    }

    internal static bool ValidateClassicValueChainWriteRequest(int destinationLength, TiffIfdEntry[][] directories, byte[][][] valueBytes, TiffByteOrder byteOrder, ulong firstIfdOffset, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        return ValidateDirectoryChainWriteRequest(destinationLength, directories, valueBytes, byteOrder, firstIfdOffset, limits, 4, 2, true, true, diagnostics);
    }

    internal static bool ValidateClassicEntry(TiffIfdEntry entry, TiffDiagnostics diagnostics)
    {
        if (entry == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Classic IFD entry is required.");
            return false;
        }

        if (entry.TagId < 0 || entry.TagId > ushort.MaxValue)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Classic IFD tag id must fit in 16 bits.", TiffDiagnostic.NoImageIndex, entry.TagId);
            return false;
        }

        if (!TiffDataTypeInfo.IsKnown(entry.DataType))
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "Classic IFD entry data type is invalid.", TiffDiagnostic.NoImageIndex, entry.TagId);
            return false;
        }

        if (entry.Count < 0 || entry.Count > uint.MaxValue)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "Classic IFD entry count must fit in 32 bits.", TiffDiagnostic.NoImageIndex, entry.TagId);
            return false;
        }

        if (!TiffMath.IsClassicOffset(entry.ValueOffset))
        {
            diagnostics.AddErrorForTag(TiffErrorCode.IntegerOverflow, "Classic IFD entry value or offset must fit in 32 bits.", TiffDiagnostic.NoImageIndex, entry.TagId);
            return false;
        }

        return true;
    }

    internal static bool ValidateBigTiffWriteRequest(int destinationLength, TiffIfdEntry[] entries, TiffByteOrder byteOrder, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        int entryCount = GetEntryCount(entries);
        if (byteOrder != TiffByteOrder.LittleEndian && byteOrder != TiffByteOrder.BigEndian)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "BigTIFF IFD writer requires a known byte order.");
        }

        if (entries == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "BigTIFF IFD writer requires entries.");
        }

        if (!limits.AllowsTagCountPerIfd(entryCount))
        {
            diagnostics.AddError(TiffErrorCode.TagCountLimitExceeded, "BigTIFF IFD entry count exceeds the configured limit.");
        }

        int requiredLength = CalculateBigTiffDirectoryByteCount(entryCount);
        if (destinationLength < requiredLength)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "BigTIFF IFD destination buffer is too short.");
        }

        int index = 0;
        while (entries != null && index < entries.Length)
        {
            ValidateBigTiffEntry(entries[index], diagnostics);
            index = index + 1;
        }

        return !diagnostics.HasErrors();
    }

    internal static bool ValidateBigTiffChainWriteRequest(int destinationLength, TiffIfdEntry[][] directories, TiffByteOrder byteOrder, ulong firstIfdOffset, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        return ValidateDirectoryChainWriteRequest(destinationLength, directories, new byte[0][][], byteOrder, firstIfdOffset, limits, 8, 8, false, false, diagnostics);
    }

    internal static bool ValidateBigTiffValueWriteRequest(int destinationLength, TiffIfdEntry[] entries, byte[][] valueBytes, TiffByteOrder byteOrder, ulong directoryOffset, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        int entryCount = GetEntryCount(entries);
        if (byteOrder != TiffByteOrder.LittleEndian && byteOrder != TiffByteOrder.BigEndian)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "BigTIFF IFD value writer requires a known byte order.");
        }

        if (entries == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "BigTIFF IFD value writer requires entries.");
        }

        if (valueBytes == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "BigTIFF IFD value writer requires value byte arrays.");
        }

        if (entries != null && valueBytes != null && entries.Length != valueBytes.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagCount, "BigTIFF IFD value byte array count must match the entry count.");
        }

        if (!limits.AllowsTagCountPerIfd(entryCount))
        {
            diagnostics.AddError(TiffErrorCode.TagCountLimitExceeded, "BigTIFF IFD entry count exceeds the configured limit.");
        }

        if (entries != null && valueBytes != null && entries.Length == valueBytes.Length)
        {
            ValidateValueBytes(entries, valueBytes, false, diagnostics);
            ulong requiredLength = CalculateBigTiffDirectoryLayoutByteCountAsUInt64(entries, valueBytes);
            if (requiredLength > (ulong)destinationLength)
            {
                diagnostics.AddError(TiffErrorCode.InvalidByteRange, "BigTIFF IFD destination buffer is too short for entries and external values.");
            }

            if (!ValidateExternalValueOffsets(entries, valueBytes, 8, 8, directoryOffset, false, diagnostics))
            {
                return false;
            }
        }

        return !diagnostics.HasErrors();
    }

    internal static bool ValidateBigTiffValueChainWriteRequest(int destinationLength, TiffIfdEntry[][] directories, byte[][][] valueBytes, TiffByteOrder byteOrder, ulong firstIfdOffset, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        return ValidateDirectoryChainWriteRequest(destinationLength, directories, valueBytes, byteOrder, firstIfdOffset, limits, 8, 8, false, true, diagnostics);
    }

    internal static bool ValidateBigTiffEntry(TiffIfdEntry entry, TiffDiagnostics diagnostics)
    {
        if (entry == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "BigTIFF IFD entry is required.");
            return false;
        }

        if (entry.TagId < 0 || entry.TagId > ushort.MaxValue)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "BigTIFF IFD tag id must fit in 16 bits.", TiffDiagnostic.NoImageIndex, entry.TagId);
            return false;
        }

        if (!TiffDataTypeInfo.IsKnown(entry.DataType))
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "BigTIFF IFD entry data type is invalid.", TiffDiagnostic.NoImageIndex, entry.TagId);
            return false;
        }

        if (entry.Count < 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "BigTIFF IFD entry count cannot be negative.", TiffDiagnostic.NoImageIndex, entry.TagId);
            return false;
        }

        return true;
    }

    internal static bool ValidateDirectoryChainWriteRequest(int destinationLength, TiffIfdEntry[][] directories, byte[][][] valueBytes, TiffByteOrder byteOrder, ulong firstIfdOffset, TiffLimits limits, int slotByteCount, ulong alignment, bool classic, bool hasValueBytes, TiffDiagnostics diagnostics)
    {
        TiffLimits actualLimits = limits;
        if (actualLimits == null)
        {
            actualLimits = new TiffLimits();
        }

        int directoryCount = GetDirectoryCount(directories);
        if (byteOrder != TiffByteOrder.LittleEndian && byteOrder != TiffByteOrder.BigEndian)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "IFD chain writer requires a known byte order.");
        }

        if (directories == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "IFD chain writer requires directories.");
        }

        if (hasValueBytes && valueBytes == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "IFD chain value writer requires value byte arrays.");
        }

        if (hasValueBytes && directories != null && valueBytes != null && directories.Length != valueBytes.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagCount, "IFD chain value byte array count must match the directory count.");
        }

        if (!actualLimits.AllowsIfdCount(directoryCount))
        {
            diagnostics.AddError(TiffErrorCode.IfdCountLimitExceeded, "IFD chain directory count exceeds the configured limit.");
        }

        if (directoryCount > 0)
        {
            if (firstIfdOffset == 0)
            {
                diagnostics.AddError(TiffErrorCode.InvalidOffset, "IFD chain first directory offset must be non-zero when directories are written.");
            }

            if (!IsAlignedOffset(firstIfdOffset, alignment))
            {
                diagnostics.AddError(TiffErrorCode.InvalidOffset, "IFD chain first directory offset is not aligned.");
            }

            if (firstIfdOffset > (ulong)destinationLength)
            {
                diagnostics.AddError(TiffErrorCode.InvalidOffset, "IFD chain first directory offset is outside the destination buffer.");
            }

            if (classic && !TiffMath.IsClassicOffset(firstIfdOffset))
            {
                diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Classic IFD chain first offset must fit in 32 bits.");
            }
        }

        if (directories != null && hasValueBytes && valueBytes != null && directories.Length == valueBytes.Length)
        {
            ValidateDirectoryChainItems(destinationLength, directories, valueBytes, byteOrder, firstIfdOffset, actualLimits, slotByteCount, alignment, classic, hasValueBytes, diagnostics);
        }

        if (directories != null && !hasValueBytes)
        {
            ValidateDirectoryChainItems(destinationLength, directories, new byte[0][][], byteOrder, firstIfdOffset, actualLimits, slotByteCount, alignment, classic, hasValueBytes, diagnostics);
        }

        return !diagnostics.HasErrors();
    }

    internal static void ValidateDirectoryChainItems(int destinationLength, TiffIfdEntry[][] directories, byte[][][] valueBytes, TiffByteOrder byteOrder, ulong firstIfdOffset, TiffLimits limits, int slotByteCount, ulong alignment, bool classic, bool hasValueBytes, TiffDiagnostics diagnostics)
    {
        ulong currentOffset = firstIfdOffset;
        int index = 0;
        while (index < directories.Length)
        {
            ulong byteCount = CalculateOneDirectoryChainByteCountAsUInt64(directories[index], valueBytes, index, slotByteCount, alignment, hasValueBytes);
            bool canValidateDirectory = true;
            if (byteCount == ulong.MaxValue || byteCount > int.MaxValue)
            {
                diagnostics.AddError(TiffErrorCode.IntegerOverflow, "IFD chain directory byte count exceeds the supported range.");
                canValidateDirectory = false;
            }

            ulong endOffset = currentOffset;
            if (currentOffset > ulong.MaxValue - byteCount)
            {
                diagnostics.AddError(TiffErrorCode.IntegerOverflow, "IFD chain directory byte range overflows the supported offset range.");
                canValidateDirectory = false;
            }
            else
            {
                endOffset = currentOffset + byteCount;
                if (endOffset > (ulong)destinationLength)
                {
                    diagnostics.AddError(TiffErrorCode.InvalidByteRange, "IFD chain directory byte range is outside the destination buffer.");
                }
            }

            ulong nextOffset = 0;
            if (index + 1 < directories.Length)
            {
                if (!CanAlignExternalValueOffset(endOffset, alignment))
                {
                    diagnostics.AddError(TiffErrorCode.IntegerOverflow, "IFD chain next offset alignment overflows the supported offset range.");
                    canValidateDirectory = false;
                }
                else
                {
                    nextOffset = AlignExternalValueOffset(endOffset, alignment);
                    if (classic && !TiffMath.IsClassicOffset(nextOffset))
                    {
                        diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Classic IFD chain next offset must fit in 32 bits.");
                    }
                }
            }

            if (canValidateDirectory)
            {
                ValidateOneDirectoryChainItem((int)byteCount, directories[index], valueBytes, index, byteOrder, currentOffset, nextOffset, limits, classic, hasValueBytes, diagnostics);
            }

            currentOffset = nextOffset;
            index = index + 1;
        }
    }

    internal static void ValidateOneDirectoryChainItem(int destinationLength, TiffIfdEntry[] entries, byte[][][] valueBytes, int index, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset, TiffLimits limits, bool classic, bool hasValueBytes, TiffDiagnostics diagnostics)
    {
        if (hasValueBytes)
        {
            if (classic)
            {
                ValidateClassicValueWriteRequest(destinationLength, entries, valueBytes[index], byteOrder, directoryOffset, nextIfdOffset, limits, diagnostics);
            }
            else
            {
                ValidateBigTiffValueWriteRequest(destinationLength, entries, valueBytes[index], byteOrder, directoryOffset, limits, diagnostics);
            }

            return;
        }

        if (classic)
        {
            ValidateClassicWriteRequest(destinationLength, entries, byteOrder, nextIfdOffset, limits, diagnostics);
        }
        else
        {
            ValidateBigTiffWriteRequest(destinationLength, entries, byteOrder, limits, diagnostics);
        }
    }

    internal static void WriteClassicEntryWithInlineValue(Span<byte> destination, TiffIfdEntry entry, byte[] valueBytes, TiffByteOrder byteOrder)
    {
        TiffBinaryWriter.WriteUInt16(destination.Slice(0, 2), (ushort)entry.TagId, byteOrder);
        TiffBinaryWriter.WriteUInt16(destination.Slice(2, 2), (ushort)entry.DataType, byteOrder);
        TiffBinaryWriter.WriteUInt32(destination.Slice(4, 4), (uint)entry.Count, byteOrder);
        ClearBytes(destination.Slice(8, 4));
        CopyBytes(valueBytes, destination.Slice(8, valueBytes.Length));
    }

    internal static void WriteClassicEntryWithExternalValue(Span<byte> destination, TiffIfdEntry entry, ulong valueOffset, TiffByteOrder byteOrder)
    {
        TiffBinaryWriter.WriteUInt16(destination.Slice(0, 2), (ushort)entry.TagId, byteOrder);
        TiffBinaryWriter.WriteUInt16(destination.Slice(2, 2), (ushort)entry.DataType, byteOrder);
        TiffBinaryWriter.WriteUInt32(destination.Slice(4, 4), (uint)entry.Count, byteOrder);
        TiffBinaryWriter.WriteUInt32(destination.Slice(8, 4), (uint)valueOffset, byteOrder);
    }

    internal static void WriteBigTiffEntryWithInlineValue(Span<byte> destination, TiffIfdEntry entry, byte[] valueBytes, TiffByteOrder byteOrder)
    {
        TiffBinaryWriter.WriteUInt16(destination.Slice(0, 2), (ushort)entry.TagId, byteOrder);
        TiffBinaryWriter.WriteUInt16(destination.Slice(2, 2), (ushort)entry.DataType, byteOrder);
        TiffBinaryWriter.WriteUInt64(destination.Slice(4, 8), (ulong)entry.Count, byteOrder);
        ClearBytes(destination.Slice(12, 8));
        CopyBytes(valueBytes, destination.Slice(12, valueBytes.Length));
    }

    internal static void WriteBigTiffEntryWithExternalValue(Span<byte> destination, TiffIfdEntry entry, ulong valueOffset, TiffByteOrder byteOrder)
    {
        TiffBinaryWriter.WriteUInt16(destination.Slice(0, 2), (ushort)entry.TagId, byteOrder);
        TiffBinaryWriter.WriteUInt16(destination.Slice(2, 2), (ushort)entry.DataType, byteOrder);
        TiffBinaryWriter.WriteUInt64(destination.Slice(4, 8), (ulong)entry.Count, byteOrder);
        TiffBinaryWriter.WriteUInt64(destination.Slice(12, 8), valueOffset, byteOrder);
    }

    internal static TiffIfdEntry[] SortEntriesByTagId(TiffIfdEntry[] entries)
    {
        TiffIfdEntry[] sorted = TiffDirectory.CopyEntries(entries);
        int index = 1;
        while (index < sorted.Length)
        {
            TiffIfdEntry current = sorted[index];
            int position = index - 1;
            while (position >= 0 && sorted[position].TagId > current.TagId)
            {
                sorted[position + 1] = sorted[position];
                position = position - 1;
            }

            sorted[position + 1] = current;
            index = index + 1;
        }

        return sorted;
    }

    internal static byte[][] SortValueBytesByTagId(TiffIfdEntry[] entries, byte[][] valueBytes)
    {
        TiffIfdEntry[] sortedEntries = TiffDirectory.CopyEntries(entries);
        byte[][] sorted = CopyValueBytesArray(valueBytes);
        int index = 1;
        while (index < sorted.Length)
        {
            TiffIfdEntry currentEntry = sortedEntries[index];
            byte[] currentBytes = sorted[index];
            int position = index - 1;
            while (position >= 0 && sortedEntries[position].TagId > currentEntry.TagId)
            {
                sorted[position + 1] = sorted[position];
                sortedEntries[position + 1] = sortedEntries[position];
                position = position - 1;
            }

            sorted[position + 1] = currentBytes;
            sortedEntries[position + 1] = currentEntry;
            index = index + 1;
        }

        return sorted;
    }

    internal static int CalculateClassicDirectoryByteCount(int entryCount)
    {
        if (entryCount < 0)
        {
            return 0;
        }

        return 2 + (entryCount * 12) + 4;
    }

    internal static int CalculateBigTiffDirectoryByteCount(int entryCount)
    {
        if (entryCount < 0)
        {
            return 0;
        }

        return 8 + (entryCount * 20) + 8;
    }

    internal static int CalculateClassicDirectoryLayoutByteCount(TiffIfdEntry[] entries, byte[][] valueBytes)
    {
        ulong byteCount = CalculateClassicDirectoryLayoutByteCountAsUInt64(entries, valueBytes);
        if (byteCount > int.MaxValue)
        {
            return 0;
        }

        return (int)byteCount;
    }

    internal static int CalculateBigTiffDirectoryLayoutByteCount(TiffIfdEntry[] entries, byte[][] valueBytes)
    {
        ulong byteCount = CalculateBigTiffDirectoryLayoutByteCountAsUInt64(entries, valueBytes);
        if (byteCount > int.MaxValue)
        {
            return 0;
        }

        return (int)byteCount;
    }

    internal static int CalculateClassicDirectoryChainByteCount(TiffIfdEntry[][] directories)
    {
        ulong byteCount = CalculateClassicDirectoryChainByteCountAsUInt64(directories);
        if (byteCount > int.MaxValue)
        {
            return 0;
        }

        return (int)byteCount;
    }

    internal static int CalculateBigTiffDirectoryChainByteCount(TiffIfdEntry[][] directories)
    {
        ulong byteCount = CalculateBigTiffDirectoryChainByteCountAsUInt64(directories);
        if (byteCount > int.MaxValue)
        {
            return 0;
        }

        return (int)byteCount;
    }

    internal static int CalculateClassicDirectoryValueChainByteCount(TiffIfdEntry[][] directories, byte[][][] valueBytes)
    {
        ulong byteCount = CalculateClassicDirectoryValueChainByteCountAsUInt64(directories, valueBytes);
        if (byteCount > int.MaxValue)
        {
            return 0;
        }

        return (int)byteCount;
    }

    internal static int CalculateBigTiffDirectoryValueChainByteCount(TiffIfdEntry[][] directories, byte[][][] valueBytes)
    {
        ulong byteCount = CalculateBigTiffDirectoryValueChainByteCountAsUInt64(directories, valueBytes);
        if (byteCount > int.MaxValue)
        {
            return 0;
        }

        return (int)byteCount;
    }

    internal static ulong CalculateClassicDirectoryLayoutByteCountAsUInt64(TiffIfdEntry[] entries, byte[][] valueBytes)
    {
        return CalculateDirectoryLayoutByteCountAsUInt64(entries, valueBytes, 4, 2);
    }

    internal static ulong CalculateBigTiffDirectoryLayoutByteCountAsUInt64(TiffIfdEntry[] entries, byte[][] valueBytes)
    {
        return CalculateDirectoryLayoutByteCountAsUInt64(entries, valueBytes, 8, 8);
    }

    internal static ulong CalculateClassicDirectoryChainByteCountAsUInt64(TiffIfdEntry[][] directories)
    {
        return CalculateDirectoryChainByteCountAsUInt64(directories, new byte[0][][], 4, 2, false);
    }

    internal static ulong CalculateBigTiffDirectoryChainByteCountAsUInt64(TiffIfdEntry[][] directories)
    {
        return CalculateDirectoryChainByteCountAsUInt64(directories, new byte[0][][], 8, 8, false);
    }

    internal static ulong CalculateClassicDirectoryValueChainByteCountAsUInt64(TiffIfdEntry[][] directories, byte[][][] valueBytes)
    {
        return CalculateDirectoryChainByteCountAsUInt64(directories, valueBytes, 4, 2, true);
    }

    internal static ulong CalculateBigTiffDirectoryValueChainByteCountAsUInt64(TiffIfdEntry[][] directories, byte[][][] valueBytes)
    {
        return CalculateDirectoryChainByteCountAsUInt64(directories, valueBytes, 8, 8, true);
    }

    internal static ulong CalculateDirectoryChainByteCountAsUInt64(TiffIfdEntry[][] directories, byte[][][] valueBytes, int slotByteCount, ulong alignment, bool hasValueBytes)
    {
        int directoryCount = GetDirectoryCount(directories);
        ulong position = 0;
        int index = 0;
        while (index < directoryCount)
        {
            ulong byteCount = CalculateOneDirectoryChainByteCountAsUInt64(directories[index], valueBytes, index, slotByteCount, alignment, hasValueBytes);
            if (byteCount == ulong.MaxValue || position > ulong.MaxValue - byteCount)
            {
                return ulong.MaxValue;
            }

            position = position + byteCount;
            if (index + 1 < directoryCount)
            {
                if (!CanAlignExternalValueOffset(position, alignment))
                {
                    return ulong.MaxValue;
                }

                position = AlignExternalValueOffset(position, alignment);
            }

            index = index + 1;
        }

        return position;
    }

    internal static ulong CalculateOneDirectoryChainByteCountAsUInt64(TiffIfdEntry[] entries, byte[][][] valueBytes, int index, int slotByteCount, ulong alignment, bool hasValueBytes)
    {
        if (!hasValueBytes)
        {
            return (ulong)CalculateDirectoryTableByteCount(GetEntryCount(entries), slotByteCount);
        }

        if (valueBytes == null || index >= valueBytes.Length)
        {
            return ulong.MaxValue;
        }

        return CalculateDirectoryLayoutByteCountAsUInt64(entries, valueBytes[index], slotByteCount, alignment);
    }

    internal static ulong CalculateDirectoryLayoutByteCountAsUInt64(TiffIfdEntry[] entries, byte[][] valueBytes, int slotByteCount, ulong alignment)
    {
        int entryCount = GetEntryCount(entries);
        ulong position = (ulong)CalculateDirectoryTableByteCount(entryCount, slotByteCount);
        byte[][] actualValueBytes = valueBytes;
        if (entries != null && valueBytes != null && entries.Length == valueBytes.Length)
        {
            actualValueBytes = SortValueBytesByTagId(entries, valueBytes);
        }

        int valueCount = GetValueArrayCount(actualValueBytes);
        int index = 0;
        while (index < valueCount)
        {
            byte[] bytes = NormalizeValueBytes(actualValueBytes[index]);
            if (bytes.Length > slotByteCount)
            {
                if (!CanAlignExternalValueOffset(position, alignment))
                {
                    return ulong.MaxValue;
                }

                position = AlignExternalValueOffset(position, alignment);
                if (position > ulong.MaxValue - (ulong)bytes.Length)
                {
                    return ulong.MaxValue;
                }

                position = position + (ulong)bytes.Length;
            }

            index = index + 1;
        }

        return position;
    }

    internal static int CalculateDirectoryTableByteCount(int entryCount, int slotByteCount)
    {
        if (entryCount < 0)
        {
            return 0;
        }

        if (slotByteCount == 4)
        {
            return CalculateClassicDirectoryByteCount(entryCount);
        }

        return CalculateBigTiffDirectoryByteCount(entryCount);
    }

    internal static int GetEntryCount(TiffIfdEntry[] entries)
    {
        if (entries == null)
        {
            return 0;
        }

        return entries.Length;
    }

    internal static int GetDirectoryCount(TiffIfdEntry[][] directories)
    {
        if (directories == null)
        {
            return 0;
        }

        return directories.Length;
    }

    internal static int GetValueArrayCount(byte[][] valueBytes)
    {
        if (valueBytes == null)
        {
            return 0;
        }

        return valueBytes.Length;
    }

    internal static int GetValueDirectoryCount(byte[][][] valueBytes)
    {
        if (valueBytes == null)
        {
            return 0;
        }

        return valueBytes.Length;
    }

    internal static TiffWriteOptions NormalizeOptions(TiffWriteOptions options)
    {
        if (options == null)
        {
            return new TiffWriteOptions();
        }

        return options;
    }

    internal static TiffLimits GetLimits(TiffWriteOptions options)
    {
        if (options == null)
        {
            return new TiffLimits();
        }

        if (options.Limits == null)
        {
            options.Limits = new TiffLimits();
        }

        options.Limits = options.GetMemoryPolicy().ApplyTo(options.Limits);
        return options.Limits;
    }

    internal static void ValidateValueBytes(TiffIfdEntry[] entries, byte[][] valueBytes, bool classic, TiffDiagnostics diagnostics)
    {
        if (entries == null || valueBytes == null || entries.Length != valueBytes.Length)
        {
            return;
        }

        int index = 0;
        while (index < entries.Length)
        {
            if (classic)
            {
                ValidateClassicValueEntry(entries[index], diagnostics);
            }
            else
            {
                ValidateBigTiffValueEntry(entries[index], diagnostics);
            }

            if (entries[index] != null && TiffDataTypeInfo.IsKnown(entries[index].DataType) && entries[index].Count >= 0 && valueBytes[index] != null)
            {
                int elementSize = TiffDataTypeInfo.GetElementSize(entries[index].DataType);
                if (entries[index].Count > long.MaxValue / elementSize)
                {
                    diagnostics.AddErrorForTag(TiffErrorCode.IntegerOverflow, "IFD entry value byte count overflows the supported range.", TiffDiagnostic.NoImageIndex, entries[index].TagId);
                }
                else
                {
                    long expectedByteCount = TiffDataTypeInfo.CalculateValueByteCount(entries[index].DataType, entries[index].Count);
                    if (expectedByteCount != valueBytes[index].Length)
                    {
                        diagnostics.AddErrorForTag(TiffErrorCode.InvalidByteRange, "IFD entry value bytes do not match the data type and count.", TiffDiagnostic.NoImageIndex, entries[index].TagId);
                    }
                }
            }

            if (valueBytes[index] == null && entries[index] != null)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.UnknownError, "IFD entry value bytes are required.", TiffDiagnostic.NoImageIndex, entries[index].TagId);
            }

            index = index + 1;
        }
    }

    internal static bool ValidateClassicValueEntry(TiffIfdEntry entry, TiffDiagnostics diagnostics)
    {
        if (entry == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Classic IFD entry is required.");
            return false;
        }

        if (entry.TagId < 0 || entry.TagId > ushort.MaxValue)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Classic IFD tag id must fit in 16 bits.", TiffDiagnostic.NoImageIndex, entry.TagId);
            return false;
        }

        if (!TiffDataTypeInfo.IsKnown(entry.DataType))
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "Classic IFD entry data type is invalid.", TiffDiagnostic.NoImageIndex, entry.TagId);
            return false;
        }

        if (entry.Count < 0 || entry.Count > uint.MaxValue)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "Classic IFD entry count must fit in 32 bits.", TiffDiagnostic.NoImageIndex, entry.TagId);
            return false;
        }

        return true;
    }

    internal static bool ValidateBigTiffValueEntry(TiffIfdEntry entry, TiffDiagnostics diagnostics)
    {
        if (entry == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "BigTIFF IFD entry is required.");
            return false;
        }

        if (entry.TagId < 0 || entry.TagId > ushort.MaxValue)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "BigTIFF IFD tag id must fit in 16 bits.", TiffDiagnostic.NoImageIndex, entry.TagId);
            return false;
        }

        if (!TiffDataTypeInfo.IsKnown(entry.DataType))
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "BigTIFF IFD entry data type is invalid.", TiffDiagnostic.NoImageIndex, entry.TagId);
            return false;
        }

        if (entry.Count < 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "BigTIFF IFD entry count cannot be negative.", TiffDiagnostic.NoImageIndex, entry.TagId);
            return false;
        }

        return true;
    }

    internal static bool ValidateExternalValueOffsets(TiffIfdEntry[] entries, byte[][] valueBytes, int slotByteCount, ulong alignment, ulong directoryOffset, bool classic, TiffDiagnostics diagnostics)
    {
        if (entries == null || valueBytes == null || entries.Length != valueBytes.Length)
        {
            return false;
        }

        byte[][] sortedValues = SortValueBytesByTagId(entries, valueBytes);
        ulong position = (ulong)CalculateDirectoryTableByteCount(entries.Length, slotByteCount);
        int index = 0;
        while (index < sortedValues.Length)
        {
            byte[] bytes = NormalizeValueBytes(sortedValues[index]);
            if (bytes.Length > slotByteCount)
            {
                if (!CanAlignExternalValueOffset(position, alignment))
                {
                    diagnostics.AddError(TiffErrorCode.IntegerOverflow, "External value alignment overflows the supported offset range.");
                    return false;
                }

                position = AlignExternalValueOffset(position, alignment);
                if (directoryOffset > ulong.MaxValue - position)
                {
                    diagnostics.AddError(TiffErrorCode.IntegerOverflow, "External value offset overflows the supported offset range.");
                    return false;
                }

                ulong fileOffset = directoryOffset + position;
                if (classic && !TiffMath.IsClassicOffset(fileOffset))
                {
                    diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Classic external value offset must fit in 32 bits.");
                    return false;
                }

                if (position > ulong.MaxValue - (ulong)bytes.Length)
                {
                    diagnostics.AddError(TiffErrorCode.IntegerOverflow, "External value byte layout overflows the supported offset range.");
                    return false;
                }

                position = position + (ulong)bytes.Length;
            }

            index = index + 1;
        }

        return true;
    }

    internal static ulong AlignExternalValueOffset(ulong offset, ulong alignment)
    {
        ulong remainder = offset % alignment;
        if (remainder == 0)
        {
            return offset;
        }

        return offset + (alignment - remainder);
    }

    internal static bool CanAlignExternalValueOffset(ulong offset, ulong alignment)
    {
        ulong remainder = offset % alignment;
        if (remainder == 0)
        {
            return true;
        }

        return offset <= ulong.MaxValue - (alignment - remainder);
    }

    internal static ulong CalculateNextDirectoryOffset(ulong currentOffset, ulong byteCount, ulong alignment, int index, int directoryCount)
    {
        if (index + 1 >= directoryCount)
        {
            return 0;
        }

        if (currentOffset > ulong.MaxValue - byteCount)
        {
            return ulong.MaxValue;
        }

        ulong nextOffset = currentOffset + byteCount;
        if (!CanAlignExternalValueOffset(nextOffset, alignment))
        {
            return ulong.MaxValue;
        }

        return AlignExternalValueOffset(nextOffset, alignment);
    }

    internal static bool IsAlignedOffset(ulong offset, ulong alignment)
    {
        return offset % alignment == 0;
    }

    internal static byte[][] CopyValueBytesArray(byte[][] valueBytes)
    {
        if (valueBytes == null)
        {
            return new byte[0][];
        }

        byte[][] copy = new byte[valueBytes.Length][];
        int index = 0;
        while (index < valueBytes.Length)
        {
            copy[index] = NormalizeValueBytes(valueBytes[index]);
            index = index + 1;
        }

        return copy;
    }

    internal static byte[] NormalizeValueBytes(byte[] valueBytes)
    {
        if (valueBytes == null)
        {
            return new byte[0];
        }

        return valueBytes;
    }

    internal static void CopyBytes(byte[] source, Span<byte> destination)
    {
        int index = 0;
        while (index < source.Length)
        {
            destination[index] = source[index];
            index = index + 1;
        }
    }

    internal static void ClearBytes(Span<byte> destination)
    {
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = 0;
            index = index + 1;
        }
    }
}
