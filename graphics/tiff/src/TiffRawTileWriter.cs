using System;

namespace tiff;

internal class TiffRawTileWriter
{
    internal static TiffRawTileWriteResult WriteTile(TiffDirectory directory, Span<byte> destination, TiffByteOrder byteOrder, ulong tileIndex, ReadOnlySpan<byte> tileBytes)
    {
        return WriteTile(directory, destination, byteOrder, tileIndex, tileBytes, new TiffLimits());
    }

    internal static TiffRawTileWriteResult WriteTile(TiffDirectory directory, Span<byte> destination, TiffByteOrder byteOrder, ulong tileIndex, ReadOnlySpan<byte> tileBytes, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A directory is required to write a raw tile.");
            return TiffRawTileWriteResult.Create(tileIndex, new TiffByteRange(), 0, diagnostics);
        }

        TiffImageInfoResult infoResult = TiffImageInfoExtractor.Extract(directory, destination, byteOrder, TiffReadOptions.Create(actualLimits));
        diagnostics.Merge(infoResult.Diagnostics);
        if (!infoResult.Success)
        {
            return TiffRawTileWriteResult.Create(tileIndex, new TiffByteRange(), 0, diagnostics);
        }

        TiffIfdEntry? tileOffsetsEntry = directory.FindEntry(TiffTagId.TileOffsets);
        if (tileOffsetsEntry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "TileOffsets is required to write a raw tile.", directory.ImageIndex, TiffTagId.TileOffsets);
            return TiffRawTileWriteResult.Create(tileIndex, new TiffByteRange(), 0, diagnostics);
        }

        TiffIfdEntry? tileByteCountsEntry = directory.FindEntry(TiffTagId.TileByteCounts);
        if (tileByteCountsEntry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "TileByteCounts is required to write a raw tile.", directory.ImageIndex, TiffTagId.TileByteCounts);
            return TiffRawTileWriteResult.Create(tileIndex, new TiffByteRange(), 0, diagnostics);
        }

        ulong[] tileOffsets = TiffImageInfoExtractor.ReadUnsignedArray(tileOffsetsEntry, destination, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        ulong[] tileByteCounts = TiffImageInfoExtractor.ReadUnsignedArray(tileByteCountsEntry, destination, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffRawTileWriteResult.Create(tileIndex, new TiffByteRange(), 0, diagnostics);
        }

        return WriteTile(infoResult.Info, tileOffsets, tileByteCounts, destination, tileIndex, tileBytes, diagnostics);
    }

    internal static TiffRawTileWriteResult WriteTile(TiffImageInfo info, ulong[] tileOffsets, ulong[] tileByteCounts, Span<byte> destination, ulong tileIndex, ReadOnlySpan<byte> tileBytes)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        return WriteTile(info, tileOffsets, tileByteCounts, destination, tileIndex, tileBytes, diagnostics);
    }

    internal static TiffRawTileWriteResult WriteTile(TiffImageInfo info, ulong[] tileOffsets, ulong[] tileByteCounts, Span<byte> destination, ulong tileIndex, ReadOnlySpan<byte> tileBytes, TiffDiagnostics diagnostics)
    {
        TiffByteRange[] ranges = TiffStrileLayout.CalculateTileByteRanges(info, tileOffsets, tileByteCounts, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffRawTileWriteResult.Create(tileIndex, new TiffByteRange(), 0, diagnostics);
        }

        if (tileIndex > int.MaxValue || tileIndex >= (ulong)ranges.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Tile index is outside the available tile range.");
            return TiffRawTileWriteResult.Create(tileIndex, new TiffByteRange(), 0, diagnostics);
        }

        TiffByteRange range = ranges[(int)tileIndex];
        ulong bytesWritten = WriteRangeBytes(range, destination, tileBytes, diagnostics);
        return TiffRawTileWriteResult.Create(tileIndex, range, bytesWritten, diagnostics);
    }

    internal static ulong WriteRangeBytes(TiffByteRange range, Span<byte> destination, ReadOnlySpan<byte> source, TiffDiagnostics diagnostics)
    {
        if (range == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A byte range is required to write raw tile bytes.");
            return 0;
        }

        if (range.IsSparse)
        {
            if (source.Length != 0)
            {
                diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Sparse raw tile ranges can only be written with empty data.");
            }

            return 0;
        }

        if (range.ByteCount > int.MaxValue || range.ByteCount != (ulong)source.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Raw tile source byte count does not match TileByteCounts.");
            return 0;
        }

        if (range.Offset > ulong.MaxValue - range.ByteCount)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Raw tile byte range end offset overflows the supported range.");
            return 0;
        }

        if (range.Offset > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Raw tile byte offset exceeds the supported destination range.");
            return 0;
        }

        if (!TiffMath.IsByteRangeWithinLength((long)range.Offset, (long)range.ByteCount, destination.Length))
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Raw tile byte range is outside the destination buffer.");
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
