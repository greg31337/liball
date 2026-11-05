using System;

namespace tiff;

internal class TiffDecodedStripReader
{
    internal static TiffDecodedStripReadResult ReadStrip(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, ulong stripIndex)
    {
        return ReadStrip(directory, fileBytes, byteOrder, stripIndex, new TiffLimits());
    }

    internal static TiffDecodedStripReadResult ReadStrip(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, ulong stripIndex, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A directory is required to read a decoded strip.");
            return TiffDecodedStripReadResult.Create(stripIndex, 0, TiffCompression.Unknown, new byte[0], diagnostics);
        }

        TiffImageInfoResult infoResult = TiffImageInfoExtractor.Extract(directory, fileBytes, byteOrder, TiffReadOptions.Create(actualLimits));
        diagnostics.Merge(infoResult.Diagnostics);
        if (!infoResult.Success)
        {
            return TiffDecodedStripReadResult.Create(stripIndex, 0, infoResult.Info.Compression, new byte[0], diagnostics);
        }

        TiffIfdEntry? stripOffsetsEntry = directory.FindEntry(TiffTagId.StripOffsets);
        if (stripOffsetsEntry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "StripOffsets is required to read a decoded strip.", directory.ImageIndex, TiffTagId.StripOffsets);
            return TiffDecodedStripReadResult.Create(stripIndex, 0, infoResult.Info.Compression, new byte[0], diagnostics);
        }

        TiffIfdEntry? stripByteCountsEntry = directory.FindEntry(TiffTagId.StripByteCounts);
        if (stripByteCountsEntry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "StripByteCounts is required to read a decoded strip.", directory.ImageIndex, TiffTagId.StripByteCounts);
            return TiffDecodedStripReadResult.Create(stripIndex, 0, infoResult.Info.Compression, new byte[0], diagnostics);
        }

        ulong[] stripOffsets = TiffImageInfoExtractor.ReadUnsignedArray(stripOffsetsEntry, fileBytes, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        ulong[] stripByteCounts = TiffImageInfoExtractor.ReadUnsignedArray(stripByteCountsEntry, fileBytes, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffDecodedStripReadResult.Create(stripIndex, 0, infoResult.Info.Compression, new byte[0], diagnostics);
        }

        return ReadStrip(infoResult.Info, stripOffsets, stripByteCounts, fileBytes, stripIndex, actualLimits, diagnostics, byteOrder);
    }

    internal static TiffDecodedStripReadResult ReadStrip(TiffImageInfo info, ulong[] stripOffsets, ulong[] stripByteCounts, ReadOnlySpan<byte> fileBytes, ulong stripIndex)
    {
        return ReadStrip(info, stripOffsets, stripByteCounts, fileBytes, stripIndex, new TiffLimits());
    }

    internal static TiffDecodedStripReadResult ReadStrip(TiffImageInfo info, ulong[] stripOffsets, ulong[] stripByteCounts, ReadOnlySpan<byte> fileBytes, ulong stripIndex, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        return ReadStrip(info, stripOffsets, stripByteCounts, fileBytes, stripIndex, actualLimits, diagnostics);
    }

    internal static TiffDecodedStripReadResult ReadStrip(TiffImageInfo info, ulong[] stripOffsets, ulong[] stripByteCounts, ReadOnlySpan<byte> fileBytes, ulong stripIndex, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        return ReadStrip(info, stripOffsets, stripByteCounts, fileBytes, stripIndex, limits, diagnostics, TiffByteOrder.LittleEndian);
    }

    internal static TiffDecodedStripReadResult ReadStrip(TiffImageInfo info, ulong[] stripOffsets, ulong[] stripByteCounts, ReadOnlySpan<byte> fileBytes, ulong stripIndex, TiffLimits limits, TiffDiagnostics diagnostics, TiffByteOrder byteOrder)
    {
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        TiffRawStripReadResult rawResult = TiffRawStripReader.ReadStrip(info, stripOffsets, stripByteCounts, fileBytes, stripIndex, actualLimits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffDecodedStripReadResult.Create(stripIndex, 0, GetCompression(info), new byte[0], diagnostics);
        }

        ulong expectedByteCount = TiffStrileLayout.CalculateStripByteCount(info, stripIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffDecodedStripReadResult.Create(stripIndex, expectedByteCount, GetCompression(info), new byte[0], diagnostics);
        }

        TiffCodecContext context = TiffCodecContext.Create(info, false, stripIndex, expectedByteCount, actualLimits);
        context.ByteOrder = byteOrder;
        if (rawResult.IsSparse)
        {
            byte[] sparseBytes = TiffSparseSegment.CreateZeroDecodedBytes(context, diagnostics);
            return TiffDecodedStripReadResult.Create(stripIndex, expectedByteCount, GetCompression(info), sparseBytes, diagnostics);
        }

        TiffCodecResult codecResult = TiffCodec.Decode(context, rawResult.Bytes);
        diagnostics.Merge(codecResult.Diagnostics);
        return TiffDecodedStripReadResult.Create(stripIndex, expectedByteCount, GetCompression(info), codecResult.Bytes, diagnostics);
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
