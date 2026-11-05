using System;

namespace tiff;

internal class TiffDirectoryReader
{
    internal static TiffDirectoryReadResult ReadSource(TiffHeader header, ITiffRandomAccessSource source, TiffReadOptions options)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (header == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A TIFF header is required to read directories.");
            return TiffDirectoryReadResult.Create(new TiffDirectory[0], diagnostics);
        }

        if (header.FileKind == TiffFileKind.Classic)
        {
            return ReadClassicSource(source, header.ByteOrder, header.FirstIfdOffset, options);
        }

        if (header.FileKind == TiffFileKind.BigTiff)
        {
            return ReadBigTiffSource(source, header.ByteOrder, header.FirstIfdOffset, options);
        }

        diagnostics.AddError(TiffErrorCode.InvalidTiffVersion, "TIFF header must be classic TIFF or BigTIFF.");
        return TiffDirectoryReadResult.Create(new TiffDirectory[0], diagnostics);
    }

    internal static TiffDirectoryReadResult ReadClassicSource(ITiffRandomAccessSource source, TiffByteOrder byteOrder, ulong firstIfdOffset, TiffReadOptions options)
    {
        TiffReadOptions actualOptions = NormalizeOptions(options);
        TiffLimits limits = GetLimits(actualOptions);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffDirectory[] directories = new TiffDirectory[0];
        ulong[] visitedOffsets = new ulong[0];
        ulong currentOffset = firstIfdOffset;
        int imageIndex = 0;

        if (!IsKnownByteOrder(byteOrder))
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "Classic IFD chain requires a known byte order.");
            return TiffDirectoryReadResult.Create(directories, diagnostics);
        }

        while (currentOffset != 0)
        {
            if (!limits.AllowsIfdCount(imageIndex + 1))
            {
                diagnostics.AddErrorAt(TiffErrorCode.IfdCountLimitExceeded, "Classic IFD chain exceeds the configured directory count limit.", ToDiagnosticOffset(currentOffset));
                return TiffDirectoryReadResult.Create(directories, diagnostics);
            }

            if (ContainsOffset(visitedOffsets, currentOffset))
            {
                diagnostics.AddErrorAt(TiffErrorCode.CyclicIfdChain, "Classic IFD chain contains a cycle.", ToDiagnosticOffset(currentOffset));
                return TiffDirectoryReadResult.Create(directories, diagnostics);
            }

            if (currentOffset > long.MaxValue)
            {
                diagnostics.AddErrorAt(TiffErrorCode.InvalidOffset, "Classic IFD offset exceeds the supported signed range.", ToDiagnosticOffset(currentOffset));
                return TiffDirectoryReadResult.Create(directories, diagnostics);
            }

            visitedOffsets = AppendOffset(visitedOffsets, currentOffset);
            if (source == null || !source.GetIsOpen() || currentOffset > (ulong)source.GetLength() || (ulong)source.GetLength() - currentOffset < 2)
            {
                diagnostics.AddErrorAt(TiffErrorCode.InvalidOffset, "Classic IFD offset is outside the source byte range.", ToDiagnosticOffset(currentOffset));
                return TiffDirectoryReadResult.Create(directories, diagnostics);
            }

            byte[] countBytes = TiffRandomAccessSourceReader.ReadBytes(source, (long)currentOffset, 2, diagnostics);
            if (diagnostics.HasErrors())
            {
                return TiffDirectoryReadResult.Create(directories, diagnostics);
            }

            ushort tagCount = TiffBinaryReader.ReadUInt16(countBytes, byteOrder);
            if (!limits.AllowsTagCountPerIfd(tagCount))
            {
                diagnostics.AddErrorAt(TiffErrorCode.TagCountLimitExceeded, "Classic IFD exceeds the configured tag count limit.", ToDiagnosticOffset(currentOffset));
                return TiffDirectoryReadResult.Create(directories, diagnostics);
            }

            long directoryByteCount = 2 + ((long)tagCount * 12) + 4;
            if (directoryByteCount > int.MaxValue)
            {
                diagnostics.AddErrorAt(TiffErrorCode.AllocationLimitExceeded, "Classic IFD is too large to allocate.", ToDiagnosticOffset(currentOffset));
                return TiffDirectoryReadResult.Create(directories, diagnostics);
            }

            if ((ulong)source.GetLength() - currentOffset < (ulong)directoryByteCount)
            {
                diagnostics.AddErrorAt(TiffErrorCode.TruncatedStream, "Classic IFD extends past the source byte range.", ToDiagnosticOffset(currentOffset));
                return TiffDirectoryReadResult.Create(directories, diagnostics);
            }

            byte[] directoryBytes = TiffRandomAccessSourceReader.ReadBytes(source, (long)currentOffset, (int)directoryByteCount, diagnostics);
            if (diagnostics.HasErrors())
            {
                return TiffDirectoryReadResult.Create(directories, diagnostics);
            }

            TiffIfdEntry[] entries = new TiffIfdEntry[tagCount];
            int entryIndex = 0;
            while (entryIndex < tagCount)
            {
                int entryOffset = 2 + (entryIndex * 12);
                TiffIfdEntryReadResult entryResult = TiffIfdEntry.ReadClassic(directoryBytes.AsSpan(entryOffset, 12), byteOrder);
                diagnostics.MergeWithLocation(entryResult.Diagnostics, ToDiagnosticOffset(currentOffset + (ulong)entryOffset), imageIndex, entryResult.Entry.TagId);
                entries[entryIndex] = entryResult.Entry;
                entryIndex = entryIndex + 1;
            }

            int nextOffsetPosition = 2 + (tagCount * 12);
            ulong nextOffset = TiffBinaryReader.ReadUInt32(directoryBytes.AsSpan(nextOffsetPosition, 4), byteOrder);
            directories = AppendDirectory(directories, TiffDirectory.Create(imageIndex, currentOffset, nextOffset, entries));
            currentOffset = nextOffset;
            imageIndex = imageIndex + 1;
        }

        return TiffDirectoryReadResult.Create(directories, diagnostics);
    }

    internal static TiffDirectoryReadResult ReadBigTiffSource(ITiffRandomAccessSource source, TiffByteOrder byteOrder, ulong firstIfdOffset, TiffReadOptions options)
    {
        TiffReadOptions actualOptions = NormalizeOptions(options);
        TiffLimits limits = GetLimits(actualOptions);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffDirectory[] directories = new TiffDirectory[0];
        ulong[] visitedOffsets = new ulong[0];
        ulong currentOffset = firstIfdOffset;
        int imageIndex = 0;

        if (!IsKnownByteOrder(byteOrder))
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "BigTIFF IFD chain requires a known byte order.");
            return TiffDirectoryReadResult.Create(directories, diagnostics);
        }

        while (currentOffset != 0)
        {
            if (!limits.AllowsIfdCount(imageIndex + 1))
            {
                diagnostics.AddErrorAt(TiffErrorCode.IfdCountLimitExceeded, "BigTIFF IFD chain exceeds the configured directory count limit.", ToDiagnosticOffset(currentOffset));
                return TiffDirectoryReadResult.Create(directories, diagnostics);
            }

            if (ContainsOffset(visitedOffsets, currentOffset))
            {
                diagnostics.AddErrorAt(TiffErrorCode.CyclicIfdChain, "BigTIFF IFD chain contains a cycle.", ToDiagnosticOffset(currentOffset));
                return TiffDirectoryReadResult.Create(directories, diagnostics);
            }

            if (currentOffset > long.MaxValue)
            {
                diagnostics.AddErrorAt(TiffErrorCode.InvalidOffset, "BigTIFF IFD offset exceeds the supported signed range.", ToDiagnosticOffset(currentOffset));
                return TiffDirectoryReadResult.Create(directories, diagnostics);
            }

            visitedOffsets = AppendOffset(visitedOffsets, currentOffset);
            if (source == null || !source.GetIsOpen() || currentOffset > (ulong)source.GetLength() || (ulong)source.GetLength() - currentOffset < 8)
            {
                diagnostics.AddErrorAt(TiffErrorCode.InvalidOffset, "BigTIFF IFD offset is outside the source byte range.", ToDiagnosticOffset(currentOffset));
                return TiffDirectoryReadResult.Create(directories, diagnostics);
            }

            byte[] countBytes = TiffRandomAccessSourceReader.ReadBytes(source, (long)currentOffset, 8, diagnostics);
            if (diagnostics.HasErrors())
            {
                return TiffDirectoryReadResult.Create(directories, diagnostics);
            }

            ulong rawTagCount = TiffBinaryReader.ReadUInt64(countBytes, byteOrder);
            if (!AllowsRawTagCount(rawTagCount, limits))
            {
                diagnostics.AddErrorAt(TiffErrorCode.TagCountLimitExceeded, "BigTIFF IFD exceeds the configured tag count limit.", ToDiagnosticOffset(currentOffset));
                return TiffDirectoryReadResult.Create(directories, diagnostics);
            }

            if (rawTagCount > int.MaxValue)
            {
                diagnostics.AddErrorAt(TiffErrorCode.InvalidTagCount, "BigTIFF IFD entry count is larger than supported signed range.", ToDiagnosticOffset(currentOffset));
                return TiffDirectoryReadResult.Create(directories, diagnostics);
            }

            int tagCount = (int)rawTagCount;
            long directoryByteCount = 8 + ((long)tagCount * 20) + 8;
            if (directoryByteCount > int.MaxValue)
            {
                diagnostics.AddErrorAt(TiffErrorCode.AllocationLimitExceeded, "BigTIFF IFD is too large to allocate.", ToDiagnosticOffset(currentOffset));
                return TiffDirectoryReadResult.Create(directories, diagnostics);
            }

            if ((ulong)source.GetLength() - currentOffset < (ulong)directoryByteCount)
            {
                diagnostics.AddErrorAt(TiffErrorCode.TruncatedStream, "BigTIFF IFD extends past the source byte range.", ToDiagnosticOffset(currentOffset));
                return TiffDirectoryReadResult.Create(directories, diagnostics);
            }

            byte[] directoryBytes = TiffRandomAccessSourceReader.ReadBytes(source, (long)currentOffset, (int)directoryByteCount, diagnostics);
            if (diagnostics.HasErrors())
            {
                return TiffDirectoryReadResult.Create(directories, diagnostics);
            }

            TiffIfdEntry[] entries = new TiffIfdEntry[tagCount];
            int entryIndex = 0;
            while (entryIndex < tagCount)
            {
                int entryOffset = 8 + (entryIndex * 20);
                TiffIfdEntryReadResult entryResult = TiffIfdEntry.ReadBigTiff(directoryBytes.AsSpan(entryOffset, 20), byteOrder);
                diagnostics.MergeWithLocation(entryResult.Diagnostics, ToDiagnosticOffset(currentOffset + (ulong)entryOffset), imageIndex, entryResult.Entry.TagId);
                entries[entryIndex] = entryResult.Entry;
                entryIndex = entryIndex + 1;
            }

            int nextOffsetPosition = 8 + (tagCount * 20);
            ulong nextOffset = TiffBinaryReader.ReadUInt64(directoryBytes.AsSpan(nextOffsetPosition, 8), byteOrder);
            directories = AppendDirectory(directories, TiffDirectory.Create(imageIndex, currentOffset, nextOffset, entries));
            currentOffset = nextOffset;
            imageIndex = imageIndex + 1;
        }

        return TiffDirectoryReadResult.Create(directories, diagnostics);
    }

    internal static TiffDirectoryReadResult Read(TiffHeader header, ReadOnlySpan<byte> fileBytes)
    {
        return Read(header, fileBytes, new TiffReadOptions());
    }

    internal static TiffDirectoryReadResult Read(TiffHeader header, ReadOnlySpan<byte> fileBytes, TiffReadOptions options)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (header == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A TIFF header is required to read directories.");
            return TiffDirectoryReadResult.Create(new TiffDirectory[0], diagnostics);
        }

        if (header.FileKind == TiffFileKind.Classic)
        {
            return ReadClassic(fileBytes, header.ByteOrder, header.FirstIfdOffset, options);
        }

        if (header.FileKind == TiffFileKind.BigTiff)
        {
            return ReadBigTiff(fileBytes, header.ByteOrder, header.FirstIfdOffset, options);
        }

        diagnostics.AddError(TiffErrorCode.InvalidTiffVersion, "TIFF header must be classic TIFF or BigTIFF.");
        return TiffDirectoryReadResult.Create(new TiffDirectory[0], diagnostics);
    }

    internal static TiffDirectoryReadResult ReadClassic(ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, ulong firstIfdOffset)
    {
        return ReadClassic(fileBytes, byteOrder, firstIfdOffset, new TiffReadOptions());
    }

    internal static TiffDirectoryReadResult ReadClassic(ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, ulong firstIfdOffset, TiffReadOptions options)
    {
        TiffReadOptions actualOptions = NormalizeOptions(options);
        TiffLimits limits = GetLimits(actualOptions);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffDirectory[] directories = new TiffDirectory[0];
        ulong[] visitedOffsets = new ulong[0];
        ulong currentOffset = firstIfdOffset;
        int imageIndex = 0;

        if (!IsKnownByteOrder(byteOrder))
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "Classic IFD chain requires a known byte order.");
            return TiffDirectoryReadResult.Create(directories, diagnostics);
        }

        while (currentOffset != 0)
        {
            if (!limits.AllowsIfdCount(imageIndex + 1))
            {
                diagnostics.AddErrorAt(TiffErrorCode.IfdCountLimitExceeded, "Classic IFD chain exceeds the configured directory count limit.", ToDiagnosticOffset(currentOffset));
                return TiffDirectoryReadResult.Create(directories, diagnostics);
            }

            if (ContainsOffset(visitedOffsets, currentOffset))
            {
                diagnostics.AddErrorAt(TiffErrorCode.CyclicIfdChain, "Classic IFD chain contains a cycle.", ToDiagnosticOffset(currentOffset));
                return TiffDirectoryReadResult.Create(directories, diagnostics);
            }

            visitedOffsets = AppendOffset(visitedOffsets, currentOffset);
            if (!HasRange(fileBytes.Length, currentOffset, 2))
            {
                diagnostics.AddErrorAt(TiffErrorCode.InvalidOffset, "Classic IFD offset is outside the file byte range.", ToDiagnosticOffset(currentOffset));
                return TiffDirectoryReadResult.Create(directories, diagnostics);
            }

            int offset = (int)currentOffset;
            ushort tagCount = TiffBinaryReader.ReadUInt16(fileBytes.Slice(offset, 2), byteOrder);
            if (!limits.AllowsTagCountPerIfd(tagCount))
            {
                diagnostics.AddErrorAt(TiffErrorCode.TagCountLimitExceeded, "Classic IFD exceeds the configured tag count limit.", ToDiagnosticOffset(currentOffset));
                return TiffDirectoryReadResult.Create(directories, diagnostics);
            }

            long entryBytes = (long)tagCount * 12;
            long directoryBytes = 2 + entryBytes + 4;
            if (!HasRange(fileBytes.Length, currentOffset, directoryBytes))
            {
                diagnostics.AddErrorAt(TiffErrorCode.TruncatedStream, "Classic IFD extends past the file byte range.", ToDiagnosticOffset(currentOffset));
                return TiffDirectoryReadResult.Create(directories, diagnostics);
            }

            TiffIfdEntry[] entries = new TiffIfdEntry[tagCount];
            int entryIndex = 0;
            while (entryIndex < tagCount)
            {
                int entryOffset = offset + 2 + (entryIndex * 12);
                TiffIfdEntryReadResult entryResult = TiffIfdEntry.ReadClassic(fileBytes.Slice(entryOffset, 12), byteOrder);
                diagnostics.MergeWithLocation(entryResult.Diagnostics, entryOffset, imageIndex, entryResult.Entry.TagId);
                entries[entryIndex] = entryResult.Entry;
                entryIndex = entryIndex + 1;
            }

            int nextOffsetPosition = offset + 2 + (tagCount * 12);
            ulong nextOffset = TiffBinaryReader.ReadUInt32(fileBytes.Slice(nextOffsetPosition, 4), byteOrder);
            directories = AppendDirectory(directories, TiffDirectory.Create(imageIndex, currentOffset, nextOffset, entries));
            currentOffset = nextOffset;
            imageIndex = imageIndex + 1;
        }

        return TiffDirectoryReadResult.Create(directories, diagnostics);
    }

    internal static TiffDirectoryReadResult ReadBigTiff(ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, ulong firstIfdOffset)
    {
        return ReadBigTiff(fileBytes, byteOrder, firstIfdOffset, new TiffReadOptions());
    }

    internal static TiffDirectoryReadResult ReadBigTiff(ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, ulong firstIfdOffset, TiffReadOptions options)
    {
        TiffReadOptions actualOptions = NormalizeOptions(options);
        TiffLimits limits = GetLimits(actualOptions);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffDirectory[] directories = new TiffDirectory[0];
        ulong[] visitedOffsets = new ulong[0];
        ulong currentOffset = firstIfdOffset;
        int imageIndex = 0;

        if (!IsKnownByteOrder(byteOrder))
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "BigTIFF IFD chain requires a known byte order.");
            return TiffDirectoryReadResult.Create(directories, diagnostics);
        }

        while (currentOffset != 0)
        {
            if (!limits.AllowsIfdCount(imageIndex + 1))
            {
                diagnostics.AddErrorAt(TiffErrorCode.IfdCountLimitExceeded, "BigTIFF IFD chain exceeds the configured directory count limit.", ToDiagnosticOffset(currentOffset));
                return TiffDirectoryReadResult.Create(directories, diagnostics);
            }

            if (ContainsOffset(visitedOffsets, currentOffset))
            {
                diagnostics.AddErrorAt(TiffErrorCode.CyclicIfdChain, "BigTIFF IFD chain contains a cycle.", ToDiagnosticOffset(currentOffset));
                return TiffDirectoryReadResult.Create(directories, diagnostics);
            }

            visitedOffsets = AppendOffset(visitedOffsets, currentOffset);
            if (!HasRange(fileBytes.Length, currentOffset, 8))
            {
                diagnostics.AddErrorAt(TiffErrorCode.InvalidOffset, "BigTIFF IFD offset is outside the file byte range.", ToDiagnosticOffset(currentOffset));
                return TiffDirectoryReadResult.Create(directories, diagnostics);
            }

            int offset = (int)currentOffset;
            ulong rawTagCount = TiffBinaryReader.ReadUInt64(fileBytes.Slice(offset, 8), byteOrder);
            if (!AllowsRawTagCount(rawTagCount, limits))
            {
                diagnostics.AddErrorAt(TiffErrorCode.TagCountLimitExceeded, "BigTIFF IFD exceeds the configured tag count limit.", ToDiagnosticOffset(currentOffset));
                return TiffDirectoryReadResult.Create(directories, diagnostics);
            }

            if (rawTagCount > int.MaxValue)
            {
                diagnostics.AddErrorAt(TiffErrorCode.InvalidTagCount, "BigTIFF IFD entry count is larger than supported signed range.", ToDiagnosticOffset(currentOffset));
                return TiffDirectoryReadResult.Create(directories, diagnostics);
            }

            int tagCount = (int)rawTagCount;
            long entryBytes = (long)tagCount * 20;
            long directoryBytes = 8 + entryBytes + 8;
            if (!HasRange(fileBytes.Length, currentOffset, directoryBytes))
            {
                diagnostics.AddErrorAt(TiffErrorCode.TruncatedStream, "BigTIFF IFD extends past the file byte range.", ToDiagnosticOffset(currentOffset));
                return TiffDirectoryReadResult.Create(directories, diagnostics);
            }

            TiffIfdEntry[] entries = new TiffIfdEntry[tagCount];
            int entryIndex = 0;
            while (entryIndex < tagCount)
            {
                int entryOffset = offset + 8 + (entryIndex * 20);
                TiffIfdEntryReadResult entryResult = TiffIfdEntry.ReadBigTiff(fileBytes.Slice(entryOffset, 20), byteOrder);
                diagnostics.MergeWithLocation(entryResult.Diagnostics, entryOffset, imageIndex, entryResult.Entry.TagId);
                entries[entryIndex] = entryResult.Entry;
                entryIndex = entryIndex + 1;
            }

            int nextOffsetPosition = offset + 8 + (tagCount * 20);
            ulong nextOffset = TiffBinaryReader.ReadUInt64(fileBytes.Slice(nextOffsetPosition, 8), byteOrder);
            directories = AppendDirectory(directories, TiffDirectory.Create(imageIndex, currentOffset, nextOffset, entries));
            currentOffset = nextOffset;
            imageIndex = imageIndex + 1;
        }

        return TiffDirectoryReadResult.Create(directories, diagnostics);
    }

    internal static bool IsKnownByteOrder(TiffByteOrder byteOrder)
    {
        return byteOrder == TiffByteOrder.LittleEndian || byteOrder == TiffByteOrder.BigEndian;
    }

    internal static TiffReadOptions NormalizeOptions(TiffReadOptions options)
    {
        if (options == null)
        {
            return new TiffReadOptions();
        }

        return options;
    }

    internal static TiffLimits GetLimits(TiffReadOptions options)
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

    internal static bool AllowsRawTagCount(ulong tagCount, TiffLimits limits)
    {
        if (limits == null)
        {
            return tagCount <= (ulong)TiffLimits.DefaultMaximumTagCountPerIfd;
        }

        if (limits.MaximumTagCountPerIfd < 0)
        {
            return false;
        }

        return tagCount <= (ulong)limits.MaximumTagCountPerIfd;
    }

    internal static bool ContainsOffset(ulong[] offsets, ulong offset)
    {
        if (offsets == null)
        {
            return false;
        }

        int index = 0;
        while (index < offsets.Length)
        {
            if (offsets[index] == offset)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static ulong[] AppendOffset(ulong[] offsets, ulong offset)
    {
        if (offsets == null)
        {
            ulong[] single = new ulong[1];
            single[0] = offset;
            return single;
        }

        ulong[] copy = new ulong[offsets.Length + 1];
        int index = 0;
        while (index < offsets.Length)
        {
            copy[index] = offsets[index];
            index = index + 1;
        }

        copy[copy.Length - 1] = offset;
        return copy;
    }

    internal static TiffDirectory[] AppendDirectory(TiffDirectory[] directories, TiffDirectory directory)
    {
        if (directories == null)
        {
            TiffDirectory[] single = new TiffDirectory[1];
            single[0] = directory.Copy();
            return single;
        }

        TiffDirectory[] copy = new TiffDirectory[directories.Length + 1];
        int index = 0;
        while (index < directories.Length)
        {
            copy[index] = directories[index].Copy();
            index = index + 1;
        }

        copy[copy.Length - 1] = directory.Copy();
        return copy;
    }

    internal static bool HasRange(int fileLength, ulong offset, long byteCount)
    {
        if (byteCount < 0)
        {
            return false;
        }

        if (offset > (ulong)fileLength)
        {
            return false;
        }

        ulong remaining = (ulong)fileLength - offset;
        return remaining >= (ulong)byteCount;
    }

    internal static long ToDiagnosticOffset(ulong offset)
    {
        if (offset > long.MaxValue)
        {
            return TiffDiagnostic.NoOffset;
        }

        return (long)offset;
    }
}
