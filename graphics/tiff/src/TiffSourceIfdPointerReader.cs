namespace tiff;

internal class TiffSourceIfdPointerReader
{
    internal static TiffIfdPointerReadResult Read(TiffDirectory? directory, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffFileKind fileKind, TiffReadOptions options)
    {
        ulong[] ancestors = new ulong[1];
        if (directory != null)
        {
            ancestors[0] = directory.Offset;
        }

        return ReadAtDepth(directory, source, byteOrder, fileKind, options, 1, ancestors);
    }

    internal static TiffIfdPointerReadResult ReadAtDepth(TiffDirectory? directory, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffFileKind fileKind, TiffReadOptions options, int depth, ulong[] ancestors)
    {
        TiffReadOptions actualOptions = TiffDirectoryReader.NormalizeOptions(options);
        TiffLimits limits = TiffDirectoryReader.GetLimits(actualOptions);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffIfdPointer[] pointers = new TiffIfdPointer[0];
        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A parent directory is required to read IFD pointer tags.");
            return TiffIfdPointerReadResult.Create(pointers, diagnostics);
        }

        if (!limits.AllowsSubIfdDepth(depth))
        {
            diagnostics.AddError(TiffErrorCode.IfdCountLimitExceeded, "IFD pointer traversal exceeds the configured depth limit.");
            return TiffIfdPointerReadResult.Create(pointers, diagnostics);
        }

        int[] tagIds = TiffIfdPointerReader.GetPointerTagIds();
        int index = 0;
        while (index < tagIds.Length)
        {
            pointers = ReadPointerTag(pointers, directory, source, byteOrder, fileKind, actualOptions, depth, ancestors, tagIds[index], diagnostics);
            index = index + 1;
        }

        return TiffIfdPointerReadResult.Create(pointers, diagnostics);
    }

    internal static TiffIfdPointer[] ReadPointerTag(TiffIfdPointer[] pointers, TiffDirectory directory, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffFileKind fileKind, TiffReadOptions options, int depth, ulong[] ancestors, int tagId, TiffDiagnostics diagnostics)
    {
        TiffLimits limits = TiffDirectoryReader.GetLimits(options);
        TiffIfdEntry? entry = directory.FindEntry(tagId);
        if (entry == null)
        {
            return pointers;
        }

        if (!TiffSubIfdReader.IsSupportedOffsetDataType(entry.DataType))
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "IFD pointer tag must store an IFD offset.", directory.ImageIndex, tagId);
            return pointers;
        }

        if (entry.Count != 1)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "IFD pointer tag must contain exactly one offset.", directory.ImageIndex, tagId);
            return pointers;
        }

        TiffTagValueLoadResult loadResult = TiffTagValueLoader.LoadSourceBytes(entry, source, limits);
        diagnostics.Merge(loadResult.Diagnostics);
        if (diagnostics.HasErrors())
        {
            return pointers;
        }

        ulong[] offsets = TiffSubIfdReader.DecodeOffsets(loadResult.Bytes, entry.DataType, entry.Count, byteOrder, diagnostics);
        if (diagnostics.HasErrors() || offsets.Length == 0 || offsets[0] == 0)
        {
            return pointers;
        }

        return ReadOnePointer(pointers, directory, source, byteOrder, fileKind, options, depth, ancestors, tagId, offsets[0], diagnostics);
    }

    internal static TiffIfdPointer[] ReadOnePointer(TiffIfdPointer[] pointers, TiffDirectory directory, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffFileKind fileKind, TiffReadOptions options, int depth, ulong[] ancestors, int tagId, ulong offset, TiffDiagnostics diagnostics)
    {
        if (TiffDirectoryReader.ContainsOffset(ancestors, offset))
        {
            diagnostics.AddErrorAt(TiffErrorCode.CyclicIfdChain, "IFD pointer tree contains a cycle.", TiffDirectoryReader.ToDiagnosticOffset(offset));
            return pointers;
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
            diagnostics.AddError(TiffErrorCode.InvalidTiffVersion, "IFD pointer parsing requires classic TIFF or BigTIFF.");
            return pointers;
        }

        diagnostics.Merge(directoryResult.Diagnostics);
        TiffIfdPointer pointer = TiffIfdPointer.Create(directory.ImageIndex, directory.Offset, tagId, depth, offset, directoryResult.Directories);
        pointers = TiffIfdPointerReader.AppendPointer(pointers, pointer);

        int index = 0;
        while (index < directoryResult.Directories.Length)
        {
            ulong[] childAncestors = TiffDirectoryReader.AppendOffset(ancestors, directoryResult.Directories[index].Offset);
            TiffIfdPointerReadResult nestedResult = ReadAtDepth(directoryResult.Directories[index], source, byteOrder, fileKind, options, depth + 1, childAncestors);
            diagnostics.Merge(nestedResult.Diagnostics);
            pointers = TiffIfdPointerReader.AppendPointers(pointers, nestedResult.Pointers);
            index = index + 1;
        }

        return pointers;
    }
}
