using System;

namespace tiff;

internal class TiffSubIfdReader
{
    internal static TiffSubIfdReadResult Read(TiffDirectory? directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffFileKind fileKind)
    {
        return Read(directory, fileBytes, byteOrder, fileKind, new TiffReadOptions());
    }

    internal static TiffSubIfdReadResult Read(TiffDirectory? directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffFileKind fileKind, TiffReadOptions options)
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

    internal static TiffSubIfdReadResult ReadAtDepth(TiffDirectory? directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffFileKind fileKind, TiffReadOptions options, int depth, ulong[] ancestors)
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

        if (!IsSupportedOffsetDataType(entry.DataType))
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "SubIFDs tag must store IFD offsets.", directory.ImageIndex, TiffTagId.SubIFDs);
            return TiffSubIfdReadResult.Create(subIfds, diagnostics);
        }

        TiffTagValueLoadResult loadResult = TiffTagValueLoader.LoadBytes(entry, fileBytes, limits);
        diagnostics.Merge(loadResult.Diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffSubIfdReadResult.Create(subIfds, diagnostics);
        }

        ulong[] offsets = DecodeOffsets(loadResult.Bytes, entry.DataType, entry.Count, byteOrder, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffSubIfdReadResult.Create(subIfds, diagnostics);
        }

        int index = 0;
        while (index < offsets.Length)
        {
            if (offsets[index] != 0)
            {
                subIfds = ReadOneSubIfd(subIfds, directory, fileBytes, byteOrder, fileKind, actualOptions, depth, ancestors, offsets[index], index, diagnostics);
            }

            index = index + 1;
        }

        return TiffSubIfdReadResult.Create(subIfds, diagnostics);
    }

    internal static TiffSubIfd[] ReadOneSubIfd(TiffSubIfd[] subIfds, TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffFileKind fileKind, TiffReadOptions options, int depth, ulong[] ancestors, ulong offset, int subIfdIndex, TiffDiagnostics diagnostics)
    {
        if (TiffDirectoryReader.ContainsOffset(ancestors, offset))
        {
            diagnostics.AddErrorAt(TiffErrorCode.CyclicIfdChain, "SubIFD tree contains a cycle.", TiffDirectoryReader.ToDiagnosticOffset(offset));
            return subIfds;
        }

        TiffDirectoryReadResult directoryResult = ReadDirectoryChain(fileBytes, byteOrder, fileKind, offset, options);
        diagnostics.Merge(directoryResult.Diagnostics);
        TiffSubIfd subIfd = TiffSubIfd.Create(directory.ImageIndex, directory.Offset, subIfdIndex, depth, offset, directoryResult.Directories);
        subIfds = AppendSubIfd(subIfds, subIfd);

        int index = 0;
        while (index < directoryResult.Directories.Length)
        {
            ulong[] childAncestors = TiffDirectoryReader.AppendOffset(ancestors, directoryResult.Directories[index].Offset);
            TiffSubIfdReadResult nestedResult = ReadAtDepth(directoryResult.Directories[index], fileBytes, byteOrder, fileKind, options, depth + 1, childAncestors);
            diagnostics.Merge(nestedResult.Diagnostics);
            subIfds = AppendSubIfds(subIfds, nestedResult.SubIfds);
            index = index + 1;
        }

        return subIfds;
    }

    internal static TiffDirectoryReadResult ReadDirectoryChain(ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffFileKind fileKind, ulong offset, TiffReadOptions options)
    {
        if (fileKind == TiffFileKind.Classic)
        {
            return TiffDirectoryReader.ReadClassic(fileBytes, byteOrder, offset, options);
        }

        if (fileKind == TiffFileKind.BigTiff)
        {
            return TiffDirectoryReader.ReadBigTiff(fileBytes, byteOrder, offset, options);
        }

        TiffDiagnostics diagnostics = new TiffDiagnostics();
        diagnostics.AddError(TiffErrorCode.InvalidTiffVersion, "SubIFD parsing requires classic TIFF or BigTIFF.");
        return TiffDirectoryReadResult.Create(new TiffDirectory[0], diagnostics);
    }

    internal static bool IsSupportedOffsetDataType(TiffDataType dataType)
    {
        switch (dataType)
        {
            case TiffDataType.Long:
            case TiffDataType.Ifd:
            case TiffDataType.Long8:
            case TiffDataType.Ifd8:
                return true;
            default:
                return false;
        }
    }

    internal static ulong[] DecodeOffsets(byte[] bytes, TiffDataType dataType, long count, TiffByteOrder byteOrder, TiffDiagnostics diagnostics)
    {
        if (count < 0 || count > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagCount, "SubIFDs count is outside the supported range.");
            return new ulong[0];
        }

        int elementSize = GetOffsetElementSize(dataType);
        if (elementSize == 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagType, "SubIFDs tag must store IFD offsets.");
            return new ulong[0];
        }

        long byteCount = (long)elementSize * count;
        if (bytes == null || bytes.Length < byteCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "SubIFDs offset bytes are shorter than the declared count.");
            return new ulong[0];
        }

        ulong[] offsets = new ulong[(int)count];
        int index = 0;
        while (index < offsets.Length)
        {
            int offset = index * elementSize;
            if (elementSize == 4)
            {
                offsets[index] = TiffBinaryReader.ReadUInt32(bytes.AsSpan(offset, 4), byteOrder);
            }
            else
            {
                offsets[index] = TiffBinaryReader.ReadUInt64(bytes.AsSpan(offset, 8), byteOrder);
            }

            index = index + 1;
        }

        return offsets;
    }

    internal static int GetOffsetElementSize(TiffDataType dataType)
    {
        switch (dataType)
        {
            case TiffDataType.Long:
            case TiffDataType.Ifd:
                return 4;
            case TiffDataType.Long8:
            case TiffDataType.Ifd8:
                return 8;
            default:
                return 0;
        }
    }

    internal static TiffSubIfd[] AppendSubIfd(TiffSubIfd[] subIfds, TiffSubIfd subIfd)
    {
        if (subIfds == null)
        {
            TiffSubIfd[] single = new TiffSubIfd[1];
            single[0] = subIfd.Copy();
            return single;
        }

        TiffSubIfd[] copy = new TiffSubIfd[subIfds.Length + 1];
        int index = 0;
        while (index < subIfds.Length)
        {
            copy[index] = subIfds[index].Copy();
            index = index + 1;
        }

        copy[copy.Length - 1] = subIfd.Copy();
        return copy;
    }

    internal static TiffSubIfd[] AppendSubIfds(TiffSubIfd[] first, TiffSubIfd[] second)
    {
        if (second == null)
        {
            return CopySubIfds(first);
        }

        TiffSubIfd[] combined = CopySubIfds(first);
        int index = 0;
        while (index < second.Length)
        {
            combined = AppendSubIfd(combined, second[index]);
            index = index + 1;
        }

        return combined;
    }

    internal static TiffSubIfd[] CopySubIfds(TiffSubIfd[] subIfds)
    {
        return TiffSubIfdReadResult.CopySubIfds(subIfds);
    }
}
