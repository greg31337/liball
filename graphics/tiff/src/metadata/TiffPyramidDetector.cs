using System;

namespace tiff;

internal class TiffPyramidDetector
{
    internal const ulong ReducedResolutionMask = 1;

    internal static TiffPyramidDetectionResult Detect(TiffDirectory parent, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffFileKind fileKind)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (parent == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A parent directory is required to detect TIFF pyramids.");
            return TiffPyramidDetectionResult.Create(new TiffPyramid(), diagnostics);
        }

        TiffSubIfdReadResult subIfdResult = TiffSubIfdReader.Read(parent, fileBytes, byteOrder, fileKind);
        diagnostics.Merge(subIfdResult.Diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffPyramidDetectionResult.Create(new TiffPyramid(), diagnostics);
        }

        TiffPyramidDetectionResult result = Detect(parent, subIfdResult.SubIfds, fileBytes, byteOrder);
        diagnostics.Merge(result.Diagnostics);
        return TiffPyramidDetectionResult.Create(result.Pyramid, diagnostics);
    }

    internal static TiffPyramidDetectionResult Detect(TiffDirectory parent, TiffSubIfd[] subIfds, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (parent == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A parent directory is required to detect TIFF pyramids.");
            return TiffPyramidDetectionResult.Create(new TiffPyramid(), diagnostics);
        }

        if (byteOrder != TiffByteOrder.LittleEndian && byteOrder != TiffByteOrder.BigEndian)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "Pyramid detection requires a known byte order.");
            return TiffPyramidDetectionResult.Create(new TiffPyramid(), diagnostics);
        }

        ulong baseWidth = ReadDimension(parent, fileBytes, byteOrder, TiffTagId.ImageWidth, diagnostics);
        ulong baseLength = ReadDimension(parent, fileBytes, byteOrder, TiffTagId.ImageLength, diagnostics);
        TiffPyramidLevel[] levels = new TiffPyramidLevel[0];
        int subIfdIndex = 0;
        while (subIfds != null && subIfdIndex < subIfds.Length)
        {
            levels = AppendReducedLevels(levels, parent, subIfds[subIfdIndex], fileBytes, byteOrder, baseWidth, baseLength, diagnostics);
            subIfdIndex = subIfdIndex + 1;
        }

        TiffPyramid pyramid = TiffPyramid.Create(levels.Length > 0, parent.ImageIndex, baseWidth, baseLength, levels);
        return TiffPyramidDetectionResult.Create(pyramid, diagnostics);
    }

    internal static TiffPyramidLevel[] AppendReducedLevels(TiffPyramidLevel[] levels, TiffDirectory parent, TiffSubIfd subIfd, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, ulong baseWidth, ulong baseLength, TiffDiagnostics diagnostics)
    {
        if (subIfd == null || subIfd.Directories == null)
        {
            return levels;
        }

        int index = 0;
        while (index < subIfd.Directories.Length)
        {
            TiffDirectory directory = subIfd.Directories[index];
            ulong width = ReadDimension(directory, fileBytes, byteOrder, TiffTagId.ImageWidth, diagnostics);
            ulong length = ReadDimension(directory, fileBytes, byteOrder, TiffTagId.ImageLength, diagnostics);
            bool reduced = IsReducedResolution(directory, fileBytes, byteOrder, baseWidth, baseLength, width, length, diagnostics);
            if (reduced)
            {
                TiffPyramidLevel level = TiffPyramidLevel.Create(levels.Length, parent.ImageIndex, subIfd.SubIfdIndex, directory.ImageIndex, width, length, true, directory);
                levels = AppendLevel(levels, level);
            }

            index = index + 1;
        }

        return levels;
    }

    internal static bool IsReducedResolution(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, ulong baseWidth, ulong baseLength, ulong width, ulong length, TiffDiagnostics diagnostics)
    {
        ulong newSubfileType = ReadUnsignedTag(directory, fileBytes, byteOrder, TiffTagId.NewSubfileType, diagnostics);
        if ((newSubfileType & ReducedResolutionMask) != 0)
        {
            return true;
        }

        ulong subfileType = ReadUnsignedTag(directory, fileBytes, byteOrder, TiffTagId.SubfileType, diagnostics);
        if (subfileType == 2)
        {
            return true;
        }

        return baseWidth > 0 && baseLength > 0 && width > 0 && length > 0 && width < baseWidth && length < baseLength;
    }

    internal static ulong ReadDimension(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, int tagId, TiffDiagnostics diagnostics)
    {
        return TiffImageInfoExtractor.ReadOptionalUnsignedScalar(directory, fileBytes, byteOrder, new TiffLimits(), tagId, 0, diagnostics);
    }

    internal static ulong ReadUnsignedTag(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, int tagId, TiffDiagnostics diagnostics)
    {
        return TiffImageInfoExtractor.ReadOptionalUnsignedScalar(directory, fileBytes, byteOrder, new TiffLimits(), tagId, 0, diagnostics);
    }

    internal static TiffPyramidLevel[] AppendLevel(TiffPyramidLevel[] levels, TiffPyramidLevel level)
    {
        TiffPyramidLevel[] result = new TiffPyramidLevel[levels.Length + 1];
        int index = 0;
        while (index < levels.Length)
        {
            result[index] = levels[index].Copy();
            index = index + 1;
        }

        result[index] = level.Copy();
        return result;
    }
}
