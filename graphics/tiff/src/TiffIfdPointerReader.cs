using System;

namespace tiff;

internal class TiffIfdPointerReader
{
    internal static TiffIfdPointerReadResult Read(TiffDirectory? directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffFileKind fileKind)
    {
        return Read(directory, fileBytes, byteOrder, fileKind, new TiffReadOptions());
    }

    internal static TiffIfdPointerReadResult Read(TiffDirectory? directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffFileKind fileKind, TiffReadOptions options)
    {
        ulong[] ancestors = new ulong[1];
        if (directory == null)
        {
            ancestors[0] = 0;
        }
        else
        {
            ancestors[0] = directory.Offset;
        }

        return ReadAtDepth(directory, fileBytes, byteOrder, fileKind, options, 1, ancestors);
    }

    internal static TiffIfdPointerReadResult ReadAtDepth(TiffDirectory? directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffFileKind fileKind, TiffReadOptions options, int depth, ulong[] ancestors)
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

        int[] tagIds = GetPointerTagIds();
        int index = 0;
        while (index < tagIds.Length)
        {
            pointers = ReadPointerTag(pointers, directory, fileBytes, byteOrder, fileKind, actualOptions, depth, ancestors, tagIds[index], diagnostics);
            index = index + 1;
        }

        return TiffIfdPointerReadResult.Create(pointers, diagnostics);
    }

    internal static TiffIfdPointer[] ReadPointerTag(TiffIfdPointer[] pointers, TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffFileKind fileKind, TiffReadOptions options, int depth, ulong[] ancestors, int tagId, TiffDiagnostics diagnostics)
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

        TiffTagValueLoadResult loadResult = TiffTagValueLoader.LoadBytes(entry, fileBytes, limits);
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

        return ReadOnePointer(pointers, directory, fileBytes, byteOrder, fileKind, options, depth, ancestors, tagId, offsets[0], diagnostics);
    }

    internal static TiffIfdPointer[] ReadOnePointer(TiffIfdPointer[] pointers, TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffFileKind fileKind, TiffReadOptions options, int depth, ulong[] ancestors, int tagId, ulong offset, TiffDiagnostics diagnostics)
    {
        if (TiffDirectoryReader.ContainsOffset(ancestors, offset))
        {
            diagnostics.AddErrorAt(TiffErrorCode.CyclicIfdChain, "IFD pointer tree contains a cycle.", TiffDirectoryReader.ToDiagnosticOffset(offset));
            return pointers;
        }

        TiffDirectoryReadResult directoryResult = TiffSubIfdReader.ReadDirectoryChain(fileBytes, byteOrder, fileKind, offset, options);
        diagnostics.Merge(directoryResult.Diagnostics);
        TiffIfdPointer pointer = TiffIfdPointer.Create(directory.ImageIndex, directory.Offset, tagId, depth, offset, directoryResult.Directories);
        pointers = AppendPointer(pointers, pointer);

        int index = 0;
        while (index < directoryResult.Directories.Length)
        {
            ulong[] childAncestors = TiffDirectoryReader.AppendOffset(ancestors, directoryResult.Directories[index].Offset);
            TiffIfdPointerReadResult nestedResult = ReadAtDepth(directoryResult.Directories[index], fileBytes, byteOrder, fileKind, options, depth + 1, childAncestors);
            diagnostics.Merge(nestedResult.Diagnostics);
            pointers = AppendPointers(pointers, nestedResult.Pointers);
            index = index + 1;
        }

        return pointers;
    }

    internal static int[] GetPointerTagIds()
    {
        int[] tagIds = new int[3];
        tagIds[0] = TiffTagId.ExifIfd;
        tagIds[1] = TiffTagId.GpsIfd;
        tagIds[2] = TiffTagId.InteroperabilityIfd;
        return tagIds;
    }

    internal static bool IsPointerTagId(int tagId)
    {
        int[] tagIds = GetPointerTagIds();
        int index = 0;
        while (index < tagIds.Length)
        {
            if (tagIds[index] == tagId)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static TiffIfdPointer[] AppendPointer(TiffIfdPointer[] pointers, TiffIfdPointer pointer)
    {
        if (pointers == null)
        {
            TiffIfdPointer[] single = new TiffIfdPointer[1];
            single[0] = pointer.Copy();
            return single;
        }

        TiffIfdPointer[] copy = new TiffIfdPointer[pointers.Length + 1];
        int index = 0;
        while (index < pointers.Length)
        {
            copy[index] = pointers[index].Copy();
            index = index + 1;
        }

        copy[copy.Length - 1] = pointer.Copy();
        return copy;
    }

    internal static TiffIfdPointer[] AppendPointers(TiffIfdPointer[] first, TiffIfdPointer[] second)
    {
        if (second == null)
        {
            return CopyPointers(first);
        }

        TiffIfdPointer[] combined = CopyPointers(first);
        int index = 0;
        while (index < second.Length)
        {
            combined = AppendPointer(combined, second[index]);
            index = index + 1;
        }

        return combined;
    }

    internal static TiffIfdPointer[] CopyPointers(TiffIfdPointer[] pointers)
    {
        return TiffIfdPointerReadResult.CopyPointers(pointers);
    }
}
