using System;

namespace tiff;

internal class TiffRawTileReader
{
    internal static TiffRawTileReadResult ReadSourceTile(TiffDirectory directory, ITiffRandomAccessSource source, TiffByteOrder byteOrder, ulong tileIndex, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A directory is required to read a raw tile.");
            return TiffRawTileReadResult.Create(tileIndex, new TiffByteRange(), new byte[0], diagnostics);
        }

        TiffImageInfoResult infoResult = TiffSourceImageInfoExtractor.Extract(source, directory, byteOrder, TiffReadOptions.Create(actualLimits));
        diagnostics.Merge(infoResult.Diagnostics);
        if (!infoResult.Success)
        {
            return TiffRawTileReadResult.Create(tileIndex, new TiffByteRange(), new byte[0], diagnostics);
        }

        TiffIfdEntry? offsetsEntry = directory.FindEntry(TiffTagId.TileOffsets);
        TiffIfdEntry? byteCountsEntry = directory.FindEntry(TiffTagId.TileByteCounts);
        if (offsetsEntry == null || byteCountsEntry == null)
        {
            diagnostics.AddError(TiffErrorCode.MissingRequiredTag, "TileOffsets and TileByteCounts are required to read a raw tile.");
            return TiffRawTileReadResult.Create(tileIndex, new TiffByteRange(), new byte[0], diagnostics);
        }

        ulong[] offsets = TiffSourceImageInfoExtractor.ReadUnsignedArray(source, offsetsEntry, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        ulong[] byteCounts = TiffSourceImageInfoExtractor.ReadUnsignedArray(source, byteCountsEntry, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        TiffByteRange[] ranges = TiffStrileLayout.CalculateTileByteRanges(infoResult.Info, offsets, byteCounts, diagnostics);
        if (diagnostics.HasErrors() || tileIndex > int.MaxValue || tileIndex >= (ulong)ranges.Length)
        {
            if (!diagnostics.HasErrors())
            {
                diagnostics.AddError(TiffErrorCode.InvalidOffset, "Tile index is outside the available tile range.");
            }

            return TiffRawTileReadResult.Create(tileIndex, new TiffByteRange(), new byte[0], diagnostics);
        }

        TiffByteRange range = ranges[(int)tileIndex];
        byte[] bytes = TiffRawStripReader.ReadSourceRangeBytes(range, source, actualLimits, diagnostics);
        return TiffRawTileReadResult.Create(tileIndex, range, bytes, diagnostics);
    }

    internal static TiffRawTileReadResult ReadTile(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, ulong tileIndex)
    {
        return ReadTile(directory, fileBytes, byteOrder, tileIndex, new TiffLimits());
    }

    internal static TiffRawTileReadResult ReadTile(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, ulong tileIndex, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A directory is required to read a raw tile.");
            return TiffRawTileReadResult.Create(tileIndex, new TiffByteRange(), new byte[0], diagnostics);
        }

        TiffImageInfoResult infoResult = TiffImageInfoExtractor.Extract(directory, fileBytes, byteOrder, TiffReadOptions.Create(actualLimits));
        diagnostics.Merge(infoResult.Diagnostics);
        if (!infoResult.Success)
        {
            return TiffRawTileReadResult.Create(tileIndex, new TiffByteRange(), new byte[0], diagnostics);
        }

        TiffIfdEntry? tileOffsetsEntry = directory.FindEntry(TiffTagId.TileOffsets);
        if (tileOffsetsEntry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "TileOffsets is required to read a raw tile.", directory.ImageIndex, TiffTagId.TileOffsets);
            return TiffRawTileReadResult.Create(tileIndex, new TiffByteRange(), new byte[0], diagnostics);
        }

        TiffIfdEntry? tileByteCountsEntry = directory.FindEntry(TiffTagId.TileByteCounts);
        if (tileByteCountsEntry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "TileByteCounts is required to read a raw tile.", directory.ImageIndex, TiffTagId.TileByteCounts);
            return TiffRawTileReadResult.Create(tileIndex, new TiffByteRange(), new byte[0], diagnostics);
        }

        ulong[] tileOffsets = TiffImageInfoExtractor.ReadUnsignedArray(tileOffsetsEntry, fileBytes, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        ulong[] tileByteCounts = TiffImageInfoExtractor.ReadUnsignedArray(tileByteCountsEntry, fileBytes, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffRawTileReadResult.Create(tileIndex, new TiffByteRange(), new byte[0], diagnostics);
        }

        return ReadTile(infoResult.Info, tileOffsets, tileByteCounts, fileBytes, tileIndex, actualLimits, diagnostics);
    }

    internal static TiffRawTileReadResult ReadTile(TiffImageInfo info, ulong[] tileOffsets, ulong[] tileByteCounts, ReadOnlySpan<byte> fileBytes, ulong tileIndex)
    {
        return ReadTile(info, tileOffsets, tileByteCounts, fileBytes, tileIndex, new TiffLimits());
    }

    internal static TiffRawTileReadResult ReadTile(TiffImageInfo info, ulong[] tileOffsets, ulong[] tileByteCounts, ReadOnlySpan<byte> fileBytes, ulong tileIndex, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        return ReadTile(info, tileOffsets, tileByteCounts, fileBytes, tileIndex, actualLimits, diagnostics);
    }

    internal static TiffRawTileReadResult ReadTile(TiffImageInfo info, ulong[] tileOffsets, ulong[] tileByteCounts, ReadOnlySpan<byte> fileBytes, ulong tileIndex, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        TiffByteRange[] ranges = TiffStrileLayout.CalculateTileByteRanges(info, tileOffsets, tileByteCounts, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffRawTileReadResult.Create(tileIndex, new TiffByteRange(), new byte[0], diagnostics);
        }

        if (tileIndex > int.MaxValue || tileIndex >= (ulong)ranges.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Tile index is outside the available tile range.");
            return TiffRawTileReadResult.Create(tileIndex, new TiffByteRange(), new byte[0], diagnostics);
        }

        TiffByteRange range = ranges[(int)tileIndex];
        byte[] bytes = ReadRangeBytes(range, fileBytes, actualLimits, diagnostics);
        return TiffRawTileReadResult.Create(tileIndex, range, bytes, diagnostics);
    }

    internal static byte[] ReadRangeBytes(TiffByteRange range, ReadOnlySpan<byte> fileBytes, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (range == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A byte range is required to read raw tile bytes.");
            return new byte[0];
        }

        if (range.IsSparse)
        {
            return new byte[0];
        }

        if (range.Offset > ulong.MaxValue - range.ByteCount)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Raw tile byte range end offset overflows the supported range.");
            return new byte[0];
        }

        if (range.Offset > long.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Raw tile byte offset exceeds the supported signed range.");
            return new byte[0];
        }

        if (range.ByteCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Raw tile byte count is too large to allocate.");
            return new byte[0];
        }

        long byteCount = (long)range.ByteCount;
        if (!actualLimits.AllowsAllocation(byteCount))
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Raw tile byte count exceeds the configured allocation limit.");
            return new byte[0];
        }

        if (!TiffMath.IsByteRangeWithinLength((long)range.Offset, byteCount, fileBytes.Length))
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Raw tile byte range is outside the available TIFF data.");
            return new byte[0];
        }

        return TiffIfdEntry.CopyBytes(fileBytes.Slice((int)range.Offset, (int)range.ByteCount));
    }
}
