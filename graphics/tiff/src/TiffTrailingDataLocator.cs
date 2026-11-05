namespace tiff;

internal class TiffTrailingDataLocator
{
    internal static TiffByteRange Locate(TiffHeader header, TiffDirectory[] directories, ITiffRandomAccessSource source, TiffReadOptions options, TiffDiagnostics diagnostics)
    {
        if (source == null || !source.GetIsOpen())
        {
            diagnostics.AddError(TiffErrorCode.UnsupportedStream, "An open TIFF source is required to locate trailing data.");
            return new TiffByteRange();
        }

        if (header == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A TIFF header is required to locate trailing data.");
            return new TiffByteRange();
        }

        long sourceLength = source.GetLength();
        if (sourceLength < 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "The TIFF source length cannot be negative.");
            return new TiffByteRange();
        }

        ulong usedEnd = 0;
        if (header.FileKind == TiffFileKind.Classic)
        {
            usedEnd = 8;
        }
        else if (header.FileKind == TiffFileKind.BigTiff)
        {
            usedEnd = 16;
        }

        int index = 0;
        while (directories != null && index < directories.Length)
        {
            ulong[] ancestors = new ulong[1];
            ancestors[0] = directories[index].Offset;
            ulong directoryEnd = LocateDirectory(header, directories[index], source, options, diagnostics, 1, ancestors);
            usedEnd = Maximum(usedEnd, directoryEnd);
            index = index + 1;
        }

        if (usedEnd > (ulong)sourceLength)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Referenced TIFF data extends past the source length.");
            return new TiffByteRange();
        }

        ulong byteCount = (ulong)sourceLength - usedEnd;
        return TiffByteRange.Create(usedEnd, byteCount, (ulong)sourceLength, byteCount == 0);
    }

    internal static ulong LocateDirectory(TiffHeader header, TiffDirectory directory, ITiffRandomAccessSource source, TiffReadOptions options, TiffDiagnostics diagnostics, int depth, ulong[] ancestors)
    {
        if (directory == null)
        {
            return 0;
        }

        TiffLimits limits = TiffDirectoryReader.GetLimits(options);
        if (!limits.AllowsSubIfdDepth(depth))
        {
            diagnostics.AddError(TiffErrorCode.IfdCountLimitExceeded, "Trailing-data discovery exceeds the configured IFD depth limit.");
            return 0;
        }

        ulong usedEnd = CalculateDirectoryEnd(header, directory, diagnostics);
        int entryIndex = 0;
        while (entryIndex < directory.Entries.Length)
        {
            TiffIfdEntry entry = directory.Entries[entryIndex];
            if (entry.HasExternalValue())
            {
                usedEnd = Maximum(usedEnd, CalculateRangeEnd(entry.ValueOffset, (ulong)entry.ValueByteCount, diagnostics));
            }

            entryIndex = entryIndex + 1;
        }

        usedEnd = Maximum(usedEnd, LocateImageData(directory, source, header.ByteOrder, limits, diagnostics));
        usedEnd = Maximum(usedEnd, LocateChildTag(header, directory, source, options, diagnostics, depth, ancestors, TiffTagId.SubIFDs));
        usedEnd = Maximum(usedEnd, LocateChildTag(header, directory, source, options, diagnostics, depth, ancestors, TiffTagId.ExifIfd));
        usedEnd = Maximum(usedEnd, LocateChildTag(header, directory, source, options, diagnostics, depth, ancestors, TiffTagId.GpsIfd));
        usedEnd = Maximum(usedEnd, LocateChildTag(header, directory, source, options, diagnostics, depth, ancestors, TiffTagId.InteroperabilityIfd));
        return usedEnd;
    }

    internal static ulong CalculateDirectoryEnd(TiffHeader header, TiffDirectory directory, TiffDiagnostics diagnostics)
    {
        ulong byteCount;
        if (header.FileKind == TiffFileKind.Classic)
        {
            byteCount = 2UL + ((ulong)directory.Entries.Length * 12UL) + 4UL;
        }
        else if (header.FileKind == TiffFileKind.BigTiff)
        {
            byteCount = 8UL + ((ulong)directory.Entries.Length * 20UL) + 8UL;
        }
        else
        {
            diagnostics.AddError(TiffErrorCode.InvalidTiffVersion, "Trailing-data discovery requires classic TIFF or BigTIFF.");
            return 0;
        }

        return CalculateRangeEnd(directory.Offset, byteCount, diagnostics);
    }

    internal static ulong LocateImageData(TiffDirectory directory, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        TiffIfdEntry? offsetsEntry = directory.FindEntry(TiffTagId.StripOffsets);
        TiffIfdEntry? byteCountsEntry = directory.FindEntry(TiffTagId.StripByteCounts);
        if (offsetsEntry == null || byteCountsEntry == null)
        {
            offsetsEntry = directory.FindEntry(TiffTagId.TileOffsets);
            byteCountsEntry = directory.FindEntry(TiffTagId.TileByteCounts);
        }

        if (offsetsEntry == null || byteCountsEntry == null)
        {
            return 0;
        }

        ulong[] offsets = TiffSourceImageInfoExtractor.ReadUnsignedArray(source, offsetsEntry, byteOrder, limits, directory.ImageIndex, diagnostics);
        ulong[] byteCounts = TiffSourceImageInfoExtractor.ReadUnsignedArray(source, byteCountsEntry, byteOrder, limits, directory.ImageIndex, diagnostics);
        if (offsets.Length != byteCounts.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagCount, "Image-data offsets and byte counts must contain the same number of values.");
            return 0;
        }

        ulong usedEnd = 0;
        int index = 0;
        while (index < offsets.Length)
        {
            if (offsets[index] != 0 || byteCounts[index] != 0)
            {
                usedEnd = Maximum(usedEnd, CalculateRangeEnd(offsets[index], byteCounts[index], diagnostics));
            }

            index = index + 1;
        }

        return usedEnd;
    }

    internal static ulong LocateChildTag(TiffHeader header, TiffDirectory directory, ITiffRandomAccessSource source, TiffReadOptions options, TiffDiagnostics diagnostics, int depth, ulong[] ancestors, int tagId)
    {
        TiffIfdEntry? entry = directory.FindEntry(tagId);
        if (entry == null)
        {
            return 0;
        }

        if (!TiffSubIfdReader.IsSupportedOffsetDataType(entry.DataType))
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "IFD pointer tag must store IFD offsets.", directory.ImageIndex, tagId);
            return 0;
        }

        TiffTagValueLoadResult loadResult = TiffTagValueLoader.LoadSourceBytes(entry, source, TiffDirectoryReader.GetLimits(options));
        diagnostics.Merge(loadResult.Diagnostics);
        ulong[] offsets = TiffSubIfdReader.DecodeOffsets(loadResult.Bytes, entry.DataType, entry.Count, header.ByteOrder, diagnostics);
        ulong usedEnd = 0;
        int index = 0;
        while (index < offsets.Length)
        {
            if (offsets[index] != 0)
            {
                usedEnd = Maximum(usedEnd, LocateChildOffset(header, source, options, diagnostics, depth, ancestors, offsets[index]));
            }

            index = index + 1;
        }

        return usedEnd;
    }

    internal static ulong LocateChildOffset(TiffHeader header, ITiffRandomAccessSource source, TiffReadOptions options, TiffDiagnostics diagnostics, int depth, ulong[] ancestors, ulong offset)
    {
        if (TiffDirectoryReader.ContainsOffset(ancestors, offset))
        {
            diagnostics.AddErrorAt(TiffErrorCode.CyclicIfdChain, "Nested IFD pointers contain a cycle.", TiffDirectoryReader.ToDiagnosticOffset(offset));
            return 0;
        }

        TiffDirectoryReadResult result;
        if (header.FileKind == TiffFileKind.Classic)
        {
            result = TiffDirectoryReader.ReadClassicSource(source, header.ByteOrder, offset, options);
        }
        else
        {
            result = TiffDirectoryReader.ReadBigTiffSource(source, header.ByteOrder, offset, options);
        }

        diagnostics.Merge(result.Diagnostics);
        ulong usedEnd = 0;
        int index = 0;
        while (index < result.Directories.Length)
        {
            ulong[] childAncestors = TiffDirectoryReader.AppendOffset(ancestors, result.Directories[index].Offset);
            usedEnd = Maximum(usedEnd, LocateDirectory(header, result.Directories[index], source, options, diagnostics, depth + 1, childAncestors));
            index = index + 1;
        }

        return usedEnd;
    }

    internal static ulong CalculateRangeEnd(ulong offset, ulong byteCount, TiffDiagnostics diagnostics)
    {
        if (offset > ulong.MaxValue - byteCount)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Referenced TIFF byte range overflows.");
            return 0;
        }

        return offset + byteCount;
    }

    internal static ulong Maximum(ulong first, ulong second)
    {
        if (first > second)
        {
            return first;
        }

        return second;
    }
}
