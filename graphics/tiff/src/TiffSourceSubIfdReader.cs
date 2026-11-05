namespace tiff;

internal class TiffSourceSubIfdReader
{
    internal static TiffSubIfdReadResult Read(TiffDirectory? directory, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffFileKind fileKind, TiffReadOptions options)
    {
        ulong[] ancestors = new ulong[1];
        if (directory != null)
        {
            ancestors[0] = directory.Offset;
        }

        return ReadAtDepth(directory, source, byteOrder, fileKind, options, 1, ancestors);
    }

    internal static TiffSubIfdReadResult ReadAtDepth(TiffDirectory? directory, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffFileKind fileKind, TiffReadOptions options, int depth, ulong[] ancestors)
    {
        TiffReadOptions actualOptions = TiffDirectoryReader.NormalizeOptions(options);
        TiffLimits limits = TiffDirectoryReader.GetLimits(actualOptions);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffSubIfd[] subIfds = new TiffSubIfd[0];
        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A parent directory is required to read SubIFDs.");
            return TiffSubIfdReadResult.Create(subIfds, diagnostics);
        }

        if (!limits.AllowsSubIfdDepth(depth))
        {
            diagnostics.AddErrorForTag(TiffErrorCode.IfdCountLimitExceeded, "SubIFD traversal exceeds the configured depth limit.", directory.ImageIndex, TiffTagId.SubIFDs);
            return TiffSubIfdReadResult.Create(subIfds, diagnostics);
        }

        TiffIfdEntry? entry = directory.FindEntry(TiffTagId.SubIFDs);
        if (entry == null)
        {
            return TiffSubIfdReadResult.Create(subIfds, diagnostics);
        }

        if (!TiffSubIfdReader.IsSupportedOffsetDataType(entry.DataType))
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "SubIFDs tag must store IFD offsets.", directory.ImageIndex, TiffTagId.SubIFDs);
            return TiffSubIfdReadResult.Create(subIfds, diagnostics);
        }

        TiffTagValueLoadResult loadResult = TiffTagValueLoader.LoadSourceBytes(entry, source, limits);
        diagnostics.Merge(loadResult.Diagnostics);
        ulong[] offsets = TiffSubIfdReader.DecodeOffsets(loadResult.Bytes, entry.DataType, entry.Count, byteOrder, diagnostics);
        int index = 0;
        while (index < offsets.Length)
        {
            if (offsets[index] != 0)
            {
                subIfds = ReadOneSubIfd(subIfds, directory, source, byteOrder, fileKind, actualOptions, depth, ancestors, offsets[index], index, diagnostics);
            }

            index = index + 1;
        }

        return TiffSubIfdReadResult.Create(subIfds, diagnostics);
    }

    internal static TiffSubIfd[] ReadOneSubIfd(TiffSubIfd[] subIfds, TiffDirectory directory, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffFileKind fileKind, TiffReadOptions options, int depth, ulong[] ancestors, ulong offset, int subIfdIndex, TiffDiagnostics diagnostics)
    {
        if (TiffDirectoryReader.ContainsOffset(ancestors, offset))
        {
            diagnostics.AddErrorAt(TiffErrorCode.CyclicIfdChain, "SubIFD tree contains a cycle.", TiffDirectoryReader.ToDiagnosticOffset(offset));
            return subIfds;
        }

        TiffDirectoryReadResult directoryResult;
        if (fileKind == TiffFileKind.Classic)
        {
            directoryResult = TiffDirectoryReader.ReadClassicSource(source, byteOrder, offset, options);
        }
        else if (fileKind == TiffFileKind.BigTiff)
        {
            directoryResult = TiffDirectoryReader.ReadBigTiffSource(source, byteOrder, offset, options);
        }
        else
        {
            diagnostics.AddError(TiffErrorCode.InvalidTiffVersion, "SubIFD parsing requires classic TIFF or BigTIFF.");
            return subIfds;
        }

        diagnostics.Merge(directoryResult.Diagnostics);
        TiffSubIfd subIfd = TiffSubIfd.Create(directory.ImageIndex, directory.Offset, subIfdIndex, depth, offset, directoryResult.Directories);
        subIfds = TiffSubIfdReader.AppendSubIfd(subIfds, subIfd);
        int index = 0;
        while (index < directoryResult.Directories.Length)
        {
            ulong[] childAncestors = TiffDirectoryReader.AppendOffset(ancestors, directoryResult.Directories[index].Offset);
            TiffSubIfdReadResult nestedResult = ReadAtDepth(directoryResult.Directories[index], source, byteOrder, fileKind, options, depth + 1, childAncestors);
            diagnostics.Merge(nestedResult.Diagnostics);
            subIfds = TiffSubIfdReader.AppendSubIfds(subIfds, nestedResult.SubIfds);
            index = index + 1;
        }

        return subIfds;
    }
}
