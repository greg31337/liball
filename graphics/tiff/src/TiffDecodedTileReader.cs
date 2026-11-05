using System;

namespace tiff;

internal class TiffDecodedTileReader
{
    internal static TiffDecodedTileReadResult ReadTile(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, ulong tileIndex)
    {
        return ReadTile(directory, fileBytes, byteOrder, tileIndex, new TiffLimits());
    }

    internal static TiffDecodedTileReadResult ReadTile(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, ulong tileIndex, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A directory is required to read a decoded tile.");
            return TiffDecodedTileReadResult.Create(tileIndex, 0, TiffCompression.Unknown, new byte[0], diagnostics);
        }

        TiffImageInfoResult infoResult = TiffImageInfoExtractor.Extract(directory, fileBytes, byteOrder, TiffReadOptions.Create(actualLimits));
        diagnostics.Merge(infoResult.Diagnostics);
        if (!infoResult.Success)
        {
            return TiffDecodedTileReadResult.Create(tileIndex, 0, infoResult.Info.Compression, new byte[0], diagnostics);
        }

        TiffIfdEntry? tileOffsetsEntry = directory.FindEntry(TiffTagId.TileOffsets);
        if (tileOffsetsEntry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "TileOffsets is required to read a decoded tile.", directory.ImageIndex, TiffTagId.TileOffsets);
            return TiffDecodedTileReadResult.Create(tileIndex, 0, infoResult.Info.Compression, new byte[0], diagnostics);
        }

        TiffIfdEntry? tileByteCountsEntry = directory.FindEntry(TiffTagId.TileByteCounts);
        if (tileByteCountsEntry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "TileByteCounts is required to read a decoded tile.", directory.ImageIndex, TiffTagId.TileByteCounts);
            return TiffDecodedTileReadResult.Create(tileIndex, 0, infoResult.Info.Compression, new byte[0], diagnostics);
        }

        ulong[] tileOffsets = TiffImageInfoExtractor.ReadUnsignedArray(tileOffsetsEntry, fileBytes, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        ulong[] tileByteCounts = TiffImageInfoExtractor.ReadUnsignedArray(tileByteCountsEntry, fileBytes, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffDecodedTileReadResult.Create(tileIndex, 0, infoResult.Info.Compression, new byte[0], diagnostics);
        }

        return ReadTile(infoResult.Info, tileOffsets, tileByteCounts, fileBytes, tileIndex, actualLimits, diagnostics, byteOrder);
    }

    internal static TiffDecodedTileReadResult ReadTile(TiffImageInfo info, ulong[] tileOffsets, ulong[] tileByteCounts, ReadOnlySpan<byte> fileBytes, ulong tileIndex)
    {
        return ReadTile(info, tileOffsets, tileByteCounts, fileBytes, tileIndex, new TiffLimits());
    }

    internal static TiffDecodedTileReadResult ReadTile(TiffImageInfo info, ulong[] tileOffsets, ulong[] tileByteCounts, ReadOnlySpan<byte> fileBytes, ulong tileIndex, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        return ReadTile(info, tileOffsets, tileByteCounts, fileBytes, tileIndex, actualLimits, diagnostics);
    }

    internal static TiffDecodedTileReadResult ReadTile(TiffImageInfo info, ulong[] tileOffsets, ulong[] tileByteCounts, ReadOnlySpan<byte> fileBytes, ulong tileIndex, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        return ReadTile(info, tileOffsets, tileByteCounts, fileBytes, tileIndex, limits, diagnostics, TiffByteOrder.LittleEndian);
    }

    internal static TiffDecodedTileReadResult ReadTile(TiffImageInfo info, ulong[] tileOffsets, ulong[] tileByteCounts, ReadOnlySpan<byte> fileBytes, ulong tileIndex, TiffLimits limits, TiffDiagnostics diagnostics, TiffByteOrder byteOrder)
    {
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        TiffRawTileReadResult rawResult = TiffRawTileReader.ReadTile(info, tileOffsets, tileByteCounts, fileBytes, tileIndex, actualLimits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffDecodedTileReadResult.Create(tileIndex, 0, GetCompression(info), new byte[0], diagnostics);
        }

        ulong expectedByteCount = TiffStrileLayout.CalculateTileStorageByteCount(info, tileIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffDecodedTileReadResult.Create(tileIndex, expectedByteCount, GetCompression(info), new byte[0], diagnostics);
        }

        TiffCodecContext context = TiffCodecContext.Create(info, true, tileIndex, expectedByteCount, actualLimits);
        context.ByteOrder = byteOrder;
        if (rawResult.IsSparse)
        {
            byte[] sparseBytes = TiffSparseSegment.CreateZeroDecodedBytes(context, diagnostics);
            return TiffDecodedTileReadResult.Create(tileIndex, expectedByteCount, GetCompression(info), sparseBytes, diagnostics);
        }

        TiffCodecResult codecResult = TiffCodec.Decode(context, rawResult.Bytes);
        diagnostics.Merge(codecResult.Diagnostics);
        return TiffDecodedTileReadResult.Create(tileIndex, expectedByteCount, GetCompression(info), codecResult.Bytes, diagnostics);
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
