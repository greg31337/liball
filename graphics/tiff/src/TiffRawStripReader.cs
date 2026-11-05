using System;

namespace tiff;

internal class TiffRawStripReader
{
    internal static TiffRawStripReadResult ReadSourceStrip(TiffDirectory directory, ITiffRandomAccessSource source, TiffByteOrder byteOrder, ulong stripIndex, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A directory is required to read a raw strip.");
            return TiffRawStripReadResult.Create(stripIndex, new TiffByteRange(), new byte[0], diagnostics);
        }

        TiffImageInfoResult infoResult = TiffSourceImageInfoExtractor.Extract(source, directory, byteOrder, TiffReadOptions.Create(actualLimits));
        diagnostics.Merge(infoResult.Diagnostics);
        if (!infoResult.Success)
        {
            return TiffRawStripReadResult.Create(stripIndex, new TiffByteRange(), new byte[0], diagnostics);
        }

        TiffIfdEntry? offsetsEntry = directory.FindEntry(TiffTagId.StripOffsets);
        TiffIfdEntry? byteCountsEntry = directory.FindEntry(TiffTagId.StripByteCounts);
        if (offsetsEntry == null || byteCountsEntry == null)
        {
            diagnostics.AddError(TiffErrorCode.MissingRequiredTag, "StripOffsets and StripByteCounts are required to read a raw strip.");
            return TiffRawStripReadResult.Create(stripIndex, new TiffByteRange(), new byte[0], diagnostics);
        }

        ulong[] offsets = TiffSourceImageInfoExtractor.ReadUnsignedArray(source, offsetsEntry, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        ulong[] byteCounts = TiffSourceImageInfoExtractor.ReadUnsignedArray(source, byteCountsEntry, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        TiffByteRange[] ranges = TiffStrileLayout.CalculateStripByteRanges(infoResult.Info, offsets, byteCounts, diagnostics);
        if (diagnostics.HasErrors() || stripIndex > int.MaxValue || stripIndex >= (ulong)ranges.Length)
        {
            if (!diagnostics.HasErrors())
            {
                diagnostics.AddError(TiffErrorCode.InvalidOffset, "Strip index is outside the available strip range.");
            }

            return TiffRawStripReadResult.Create(stripIndex, new TiffByteRange(), new byte[0], diagnostics);
        }

        TiffByteRange range = ranges[(int)stripIndex];
        byte[] bytes = ReadSourceRangeBytes(range, source, actualLimits, diagnostics);
        return TiffRawStripReadResult.Create(stripIndex, range, bytes, diagnostics);
    }

    internal static byte[] ReadSourceRangeBytes(TiffByteRange range, ITiffRandomAccessSource source, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (range == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A byte range is required to read raw strip bytes.");
            return new byte[0];
        }

        if (range.IsSparse)
        {
            return new byte[0];
        }

        if (range.Offset > long.MaxValue || range.ByteCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Raw strip byte range exceeds the supported range.");
            return new byte[0];
        }

        if (!actualLimits.AllowsAllocation((long)range.ByteCount))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Raw strip byte count exceeds the configured allocation limit.");
            return new byte[0];
        }

        return TiffRandomAccessSourceReader.ReadBytes(source, (long)range.Offset, (int)range.ByteCount, diagnostics);
    }

    internal static TiffRawStripReadResult ReadStrip(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, ulong stripIndex)
    {
        return ReadStrip(directory, fileBytes, byteOrder, stripIndex, new TiffLimits());
    }

    internal static TiffRawStripReadResult ReadStrip(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, ulong stripIndex, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A directory is required to read a raw strip.");
            return TiffRawStripReadResult.Create(stripIndex, new TiffByteRange(), new byte[0], diagnostics);
        }

        TiffImageInfoResult infoResult = TiffImageInfoExtractor.Extract(directory, fileBytes, byteOrder, TiffReadOptions.Create(actualLimits));
        diagnostics.Merge(infoResult.Diagnostics);
        if (!infoResult.Success)
        {
            return TiffRawStripReadResult.Create(stripIndex, new TiffByteRange(), new byte[0], diagnostics);
        }

        TiffIfdEntry? stripOffsetsEntry = directory.FindEntry(TiffTagId.StripOffsets);
        if (stripOffsetsEntry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "StripOffsets is required to read a raw strip.", directory.ImageIndex, TiffTagId.StripOffsets);
            return TiffRawStripReadResult.Create(stripIndex, new TiffByteRange(), new byte[0], diagnostics);
        }

        TiffIfdEntry? stripByteCountsEntry = directory.FindEntry(TiffTagId.StripByteCounts);
        if (stripByteCountsEntry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "StripByteCounts is required to read a raw strip.", directory.ImageIndex, TiffTagId.StripByteCounts);
            return TiffRawStripReadResult.Create(stripIndex, new TiffByteRange(), new byte[0], diagnostics);
        }

        ulong[] stripOffsets = TiffImageInfoExtractor.ReadUnsignedArray(stripOffsetsEntry, fileBytes, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        ulong[] stripByteCounts = TiffImageInfoExtractor.ReadUnsignedArray(stripByteCountsEntry, fileBytes, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffRawStripReadResult.Create(stripIndex, new TiffByteRange(), new byte[0], diagnostics);
        }

        return ReadStrip(infoResult.Info, stripOffsets, stripByteCounts, fileBytes, stripIndex, actualLimits, diagnostics);
    }

    internal static TiffRawStripReadResult ReadStrip(TiffImageInfo info, ulong[] stripOffsets, ulong[] stripByteCounts, ReadOnlySpan<byte> fileBytes, ulong stripIndex)
    {
        return ReadStrip(info, stripOffsets, stripByteCounts, fileBytes, stripIndex, new TiffLimits());
    }

    internal static TiffRawStripReadResult ReadStrip(TiffImageInfo info, ulong[] stripOffsets, ulong[] stripByteCounts, ReadOnlySpan<byte> fileBytes, ulong stripIndex, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        return ReadStrip(info, stripOffsets, stripByteCounts, fileBytes, stripIndex, actualLimits, diagnostics);
    }

    internal static TiffRawStripReadResult ReadStrip(TiffImageInfo info, ulong[] stripOffsets, ulong[] stripByteCounts, ReadOnlySpan<byte> fileBytes, ulong stripIndex, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        TiffByteRange[] ranges = TiffStrileLayout.CalculateStripByteRanges(info, stripOffsets, stripByteCounts, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffRawStripReadResult.Create(stripIndex, new TiffByteRange(), new byte[0], diagnostics);
        }

        if (stripIndex > int.MaxValue || stripIndex >= (ulong)ranges.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Strip index is outside the available strip range.");
            return TiffRawStripReadResult.Create(stripIndex, new TiffByteRange(), new byte[0], diagnostics);
        }

        TiffByteRange range = ranges[(int)stripIndex];
        byte[] bytes = ReadRangeBytes(range, fileBytes, actualLimits, diagnostics);
        return TiffRawStripReadResult.Create(stripIndex, range, bytes, diagnostics);
    }

    internal static byte[] ReadRangeBytes(TiffByteRange range, ReadOnlySpan<byte> fileBytes, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (range == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A byte range is required to read raw strip bytes.");
            return new byte[0];
        }

        if (range.IsSparse)
        {
            return new byte[0];
        }

        if (range.Offset > ulong.MaxValue - range.ByteCount)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Raw strip byte range end offset overflows the supported range.");
            return new byte[0];
        }

        if (range.Offset > long.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Raw strip byte offset exceeds the supported signed range.");
            return new byte[0];
        }

        if (range.ByteCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Raw strip byte count is too large to allocate.");
            return new byte[0];
        }

        long byteCount = (long)range.ByteCount;
        if (!actualLimits.AllowsAllocation(byteCount))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Raw strip byte count exceeds the configured allocation limit.");
            return new byte[0];
        }

        if (!TiffMath.IsByteRangeWithinLength((long)range.Offset, byteCount, fileBytes.Length))
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Raw strip byte range is outside the available TIFF data.");
            return new byte[0];
        }

        return TiffIfdEntry.CopyBytes(fileBytes.Slice((int)range.Offset, (int)range.ByteCount));
    }
}
