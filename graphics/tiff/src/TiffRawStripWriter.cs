using System;

namespace tiff;

internal class TiffRawStripWriter
{
    internal static TiffRawStripWriteResult WriteStrip(TiffDirectory directory, Span<byte> destination, TiffByteOrder byteOrder, ulong stripIndex, ReadOnlySpan<byte> stripBytes)
    {
        return WriteStrip(directory, destination, byteOrder, stripIndex, stripBytes, new TiffLimits());
    }

    internal static TiffRawStripWriteResult WriteStrip(TiffDirectory directory, Span<byte> destination, TiffByteOrder byteOrder, ulong stripIndex, ReadOnlySpan<byte> stripBytes, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A directory is required to write a raw strip.");
            return TiffRawStripWriteResult.Create(stripIndex, new TiffByteRange(), 0, diagnostics);
        }

        TiffImageInfoResult infoResult = TiffImageInfoExtractor.Extract(directory, destination, byteOrder, TiffReadOptions.Create(actualLimits));
        diagnostics.Merge(infoResult.Diagnostics);
        if (!infoResult.Success)
        {
            return TiffRawStripWriteResult.Create(stripIndex, new TiffByteRange(), 0, diagnostics);
        }

        TiffIfdEntry? stripOffsetsEntry = directory.FindEntry(TiffTagId.StripOffsets);
        if (stripOffsetsEntry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "StripOffsets is required to write a raw strip.", directory.ImageIndex, TiffTagId.StripOffsets);
            return TiffRawStripWriteResult.Create(stripIndex, new TiffByteRange(), 0, diagnostics);
        }

        TiffIfdEntry? stripByteCountsEntry = directory.FindEntry(TiffTagId.StripByteCounts);
        if (stripByteCountsEntry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "StripByteCounts is required to write a raw strip.", directory.ImageIndex, TiffTagId.StripByteCounts);
            return TiffRawStripWriteResult.Create(stripIndex, new TiffByteRange(), 0, diagnostics);
        }

        ulong[] stripOffsets = TiffImageInfoExtractor.ReadUnsignedArray(stripOffsetsEntry, destination, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        ulong[] stripByteCounts = TiffImageInfoExtractor.ReadUnsignedArray(stripByteCountsEntry, destination, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffRawStripWriteResult.Create(stripIndex, new TiffByteRange(), 0, diagnostics);
        }

        return WriteStrip(infoResult.Info, stripOffsets, stripByteCounts, destination, stripIndex, stripBytes, diagnostics);
    }

    internal static TiffRawStripWriteResult WriteStrip(TiffImageInfo info, ulong[] stripOffsets, ulong[] stripByteCounts, Span<byte> destination, ulong stripIndex, ReadOnlySpan<byte> stripBytes)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        return WriteStrip(info, stripOffsets, stripByteCounts, destination, stripIndex, stripBytes, diagnostics);
    }

    internal static TiffRawStripWriteResult WriteStrip(TiffImageInfo info, ulong[] stripOffsets, ulong[] stripByteCounts, Span<byte> destination, ulong stripIndex, ReadOnlySpan<byte> stripBytes, TiffDiagnostics diagnostics)
    {
        TiffByteRange[] ranges = TiffStrileLayout.CalculateStripByteRanges(info, stripOffsets, stripByteCounts, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffRawStripWriteResult.Create(stripIndex, new TiffByteRange(), 0, diagnostics);
        }

        if (stripIndex > int.MaxValue || stripIndex >= (ulong)ranges.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Strip index is outside the available strip range.");
            return TiffRawStripWriteResult.Create(stripIndex, new TiffByteRange(), 0, diagnostics);
        }

        TiffByteRange range = ranges[(int)stripIndex];
        ulong bytesWritten = WriteRangeBytes(range, destination, stripBytes, diagnostics);
        return TiffRawStripWriteResult.Create(stripIndex, range, bytesWritten, diagnostics);
    }

    internal static ulong WriteRangeBytes(TiffByteRange range, Span<byte> destination, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        if (range == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A byte range is required to write raw strip bytes.");
            return 0;
        }

        if (range.IsSparse)
        {
            if (source.Length != 0)
            {
                diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Sparse raw strip ranges can only be written with empty data.");
            }

            return 0;
        }

        if (range.ByteCount > int.MaxValue || range.ByteCount != (ulong)source.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Raw strip source byte count does not match StripByteCounts.");
            return 0;
        }

        if (range.Offset > ulong.MaxValue - range.ByteCount)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Raw strip byte range end offset overflows the supported range.");
            return 0;
        }

        if (range.Offset > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Raw strip byte offset exceeds the supported destination range.");
            return 0;
        }

        if (!TiffMath.IsByteRangeWithinLength((long)range.Offset, (long)range.ByteCount, destination.Length))
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Raw strip byte range is outside the destination buffer.");
            return 0;
        }

        CopyBytes(source, destination.Slice((int)range.Offset, source.Length));
        return (ulong)source.Length;
    }

    internal static void CopyBytes(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        int index = 0;
        while (index < source.Length)
        {
            destination[index] = source[index];
            index = index + 1;
        }
    }
}
