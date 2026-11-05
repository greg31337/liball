using System;

namespace tiff;

internal class TiffDecodedStripWriter
{
    internal static TiffDecodedStripWriteResult WriteStrip(TiffDirectory directory, Span<byte> destination, TiffByteOrder byteOrder, ulong stripIndex, ReadOnlySpan<byte> decodedBytes)
    {
        return WriteStrip(directory, destination, byteOrder, stripIndex, decodedBytes, new TiffLimits());
    }

    internal static TiffDecodedStripWriteResult WriteStrip(TiffDirectory directory, Span<byte> destination, TiffByteOrder byteOrder, ulong stripIndex, ReadOnlySpan<byte> decodedBytes, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A directory is required to write a decoded strip.");
            return TiffDecodedStripWriteResult.Create(stripIndex, 0, TiffCompression.Unknown, 0, diagnostics);
        }

        TiffImageInfoResult infoResult = TiffImageInfoExtractor.Extract(directory, destination, byteOrder, TiffReadOptions.Create(actualLimits));
        diagnostics.Merge(infoResult.Diagnostics);
        if (!infoResult.Success)
        {
            return TiffDecodedStripWriteResult.Create(stripIndex, 0, infoResult.Info.Compression, 0, diagnostics);
        }

        TiffIfdEntry? stripOffsetsEntry = directory.FindEntry(TiffTagId.StripOffsets);
        if (stripOffsetsEntry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "StripOffsets is required to write a decoded strip.", directory.ImageIndex, TiffTagId.StripOffsets);
            return TiffDecodedStripWriteResult.Create(stripIndex, 0, infoResult.Info.Compression, 0, diagnostics);
        }

        TiffIfdEntry? stripByteCountsEntry = directory.FindEntry(TiffTagId.StripByteCounts);
        if (stripByteCountsEntry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "StripByteCounts is required to write a decoded strip.", directory.ImageIndex, TiffTagId.StripByteCounts);
            return TiffDecodedStripWriteResult.Create(stripIndex, 0, infoResult.Info.Compression, 0, diagnostics);
        }

        ulong[] stripOffsets = TiffImageInfoExtractor.ReadUnsignedArray(stripOffsetsEntry, destination, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        ulong[] stripByteCounts = TiffImageInfoExtractor.ReadUnsignedArray(stripByteCountsEntry, destination, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffDecodedStripWriteResult.Create(stripIndex, 0, infoResult.Info.Compression, 0, diagnostics);
        }

        return WriteStrip(infoResult.Info, stripOffsets, stripByteCounts, destination, stripIndex, decodedBytes, actualLimits, diagnostics);
    }

    internal static TiffDecodedStripWriteResult WriteStrip(TiffImageInfo info, ulong[] stripOffsets, ulong[] stripByteCounts, Span<byte> destination, ulong stripIndex, ReadOnlySpan<byte> decodedBytes)
    {
        return WriteStrip(info, stripOffsets, stripByteCounts, destination, stripIndex, decodedBytes, new TiffLimits());
    }

    internal static TiffDecodedStripWriteResult WriteStrip(TiffImageInfo info, ulong[] stripOffsets, ulong[] stripByteCounts, Span<byte> destination, ulong stripIndex, ReadOnlySpan<byte> decodedBytes, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        return WriteStrip(info, stripOffsets, stripByteCounts, destination, stripIndex, decodedBytes, actualLimits, diagnostics);
    }

    internal static TiffDecodedStripWriteResult WriteStrip(TiffImageInfo info, ulong[] stripOffsets, ulong[] stripByteCounts, Span<byte> destination, ulong stripIndex, ReadOnlySpan<byte> decodedBytes, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        ulong expectedByteCount = TiffStrileLayout.CalculateStripByteCount(info, stripIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffDecodedStripWriteResult.Create(stripIndex, expectedByteCount, GetCompression(info), 0, diagnostics);
        }

        TiffCodecContext context = TiffCodecContext.Create(info, false, stripIndex, expectedByteCount, actualLimits);
        TiffCodecResult codecResult = TiffCodec.Encode(context, decodedBytes);
        diagnostics.Merge(codecResult.Diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffDecodedStripWriteResult.Create(stripIndex, expectedByteCount, GetCompression(info), 0, diagnostics);
        }

        if (CanKeepSparseStrip(info, stripOffsets, stripByteCounts, stripIndex, codecResult.Bytes, diagnostics))
        {
            return TiffDecodedStripWriteResult.Create(stripIndex, expectedByteCount, GetCompression(info), 0, diagnostics);
        }

        if (diagnostics.HasErrors())
        {
            return TiffDecodedStripWriteResult.Create(stripIndex, expectedByteCount, GetCompression(info), 0, diagnostics);
        }

        TiffRawStripWriteResult rawResult = TiffRawStripWriter.WriteStrip(info, stripOffsets, stripByteCounts, destination, stripIndex, codecResult.Bytes, diagnostics);
        return TiffDecodedStripWriteResult.Create(stripIndex, expectedByteCount, GetCompression(info), rawResult.BytesWritten, diagnostics);
    }

    internal static bool CanKeepSparseStrip(TiffImageInfo info, ulong[] stripOffsets, ulong[] stripByteCounts, ulong stripIndex, byte[] encodedBytes, TiffDiagnostics diagnostics)
    {
        TiffByteRange[] ranges = TiffStrileLayout.CalculateStripByteRanges(info, stripOffsets, stripByteCounts, diagnostics);
        if (diagnostics.HasErrors())
        {
            return false;
        }

        if (stripIndex > int.MaxValue || stripIndex >= (ulong)ranges.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Strip index is outside the available strip range.");
            return false;
        }

        TiffByteRange range = ranges[(int)stripIndex];
        if (!range.IsSparse)
        {
            return false;
        }

        if (TiffSparseSegment.IsAllZero(encodedBytes))
        {
            return true;
        }

        diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Sparse decoded strips can only be written with zero-valued data.");
        return false;
    }

    internal static TiffCompression GetCompression(TiffImageInfo info)
    {
        if (info == null)
        {
            return TiffCompression.Unknown;
        }

        return info.Compression;
    }
}
