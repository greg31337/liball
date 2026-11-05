using System;

namespace tiff;

internal class TiffDecodedTileWriter
{
    internal static TiffDecodedTileWriteResult WriteTile(TiffDirectory directory, Span<byte> destination, TiffByteOrder byteOrder, ulong tileIndex, ReadOnlySpan<byte> decodedBytes)
    {
        return WriteTile(directory, destination, byteOrder, tileIndex, decodedBytes, new TiffLimits());
    }

    internal static TiffDecodedTileWriteResult WriteTile(TiffDirectory directory, Span<byte> destination, TiffByteOrder byteOrder, ulong tileIndex, ReadOnlySpan<byte> decodedBytes, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A directory is required to write a decoded tile.");
            return TiffDecodedTileWriteResult.Create(tileIndex, 0, TiffCompression.Unknown, 0, diagnostics);
        }

        TiffImageInfoResult infoResult = TiffImageInfoExtractor.Extract(directory, destination, byteOrder, TiffReadOptions.Create(actualLimits));
        diagnostics.Merge(infoResult.Diagnostics);
        if (!infoResult.Success)
        {
            return TiffDecodedTileWriteResult.Create(tileIndex, 0, infoResult.Info.Compression, 0, diagnostics);
        }

        TiffIfdEntry? tileOffsetsEntry = directory.FindEntry(TiffTagId.TileOffsets);
        if (tileOffsetsEntry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "TileOffsets is required to write a decoded tile.", directory.ImageIndex, TiffTagId.TileOffsets);
            return TiffDecodedTileWriteResult.Create(tileIndex, 0, infoResult.Info.Compression, 0, diagnostics);
        }

        TiffIfdEntry? tileByteCountsEntry = directory.FindEntry(TiffTagId.TileByteCounts);
        if (tileByteCountsEntry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "TileByteCounts is required to write a decoded tile.", directory.ImageIndex, TiffTagId.TileByteCounts);
            return TiffDecodedTileWriteResult.Create(tileIndex, 0, infoResult.Info.Compression, 0, diagnostics);
        }

        ulong[] tileOffsets = TiffImageInfoExtractor.ReadUnsignedArray(tileOffsetsEntry, destination, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        ulong[] tileByteCounts = TiffImageInfoExtractor.ReadUnsignedArray(tileByteCountsEntry, destination, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffDecodedTileWriteResult.Create(tileIndex, 0, infoResult.Info.Compression, 0, diagnostics);
        }

        return WriteTile(infoResult.Info, tileOffsets, tileByteCounts, destination, tileIndex, decodedBytes, actualLimits, diagnostics);
    }

    internal static TiffDecodedTileWriteResult WriteTile(TiffImageInfo info, ulong[] tileOffsets, ulong[] tileByteCounts, Span<byte> destination, ulong tileIndex, ReadOnlySpan<byte> decodedBytes)
    {
        return WriteTile(info, tileOffsets, tileByteCounts, destination, tileIndex, decodedBytes, new TiffLimits());
    }

    internal static TiffDecodedTileWriteResult WriteTile(TiffImageInfo info, ulong[] tileOffsets, ulong[] tileByteCounts, Span<byte> destination, ulong tileIndex, ReadOnlySpan<byte> decodedBytes, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        return WriteTile(info, tileOffsets, tileByteCounts, destination, tileIndex, decodedBytes, actualLimits, diagnostics);
    }

    internal static TiffDecodedTileWriteResult WriteTile(TiffImageInfo info, ulong[] tileOffsets, ulong[] tileByteCounts, Span<byte> destination, ulong tileIndex, ReadOnlySpan<byte> decodedBytes, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        ulong expectedByteCount = TiffStrileLayout.CalculateTileStorageByteCount(info, tileIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffDecodedTileWriteResult.Create(tileIndex, expectedByteCount, GetCompression(info), 0, diagnostics);
        }

        TiffCodecContext context = TiffCodecContext.Create(info, true, tileIndex, expectedByteCount, actualLimits);
        TiffCodecResult codecResult = TiffCodec.Encode(context, decodedBytes);
        diagnostics.Merge(codecResult.Diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffDecodedTileWriteResult.Create(tileIndex, expectedByteCount, GetCompression(info), 0, diagnostics);
        }

        if (CanKeepSparseTile(info, tileOffsets, tileByteCounts, tileIndex, codecResult.Bytes, diagnostics))
        {
            return TiffDecodedTileWriteResult.Create(tileIndex, expectedByteCount, GetCompression(info), 0, diagnostics);
        }

        if (diagnostics.HasErrors())
        {
            return TiffDecodedTileWriteResult.Create(tileIndex, expectedByteCount, GetCompression(info), 0, diagnostics);
        }

        TiffRawTileWriteResult rawResult = TiffRawTileWriter.WriteTile(info, tileOffsets, tileByteCounts, destination, tileIndex, codecResult.Bytes, diagnostics);
        return TiffDecodedTileWriteResult.Create(tileIndex, expectedByteCount, GetCompression(info), rawResult.BytesWritten, diagnostics);
    }

    internal static bool CanKeepSparseTile(TiffImageInfo info, ulong[] tileOffsets, ulong[] tileByteCounts, ulong tileIndex, byte[] encodedBytes, TiffDiagnostics diagnostics)
    {
        TiffByteRange[] ranges = TiffStrileLayout.CalculateTileByteRanges(info, tileOffsets, tileByteCounts, diagnostics);
        if (diagnostics.HasErrors())
        {
            return false;
        }

        if (tileIndex > int.MaxValue || tileIndex >= (ulong)ranges.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Tile index is outside the available tile range.");
            return false;
        }

        TiffByteRange range = ranges[(int)tileIndex];
        if (!range.IsSparse)
        {
            return false;
        }

        if (TiffSparseSegment.IsAllZero(encodedBytes))
        {
            return true;
        }

        diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Sparse decoded tiles can only be written with zero-valued data.");
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
