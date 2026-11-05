using System;

namespace tiff;

internal class TiffScanlineWriter
{
    internal static TiffScanlineWriteResult WriteScanline(TiffDirectory directory, Span<byte> destination, TiffByteOrder byteOrder, ulong row, ReadOnlySpan<byte> source)
    {
        return WriteScanline(directory, destination, byteOrder, row, 0, source, new TiffLimits());
    }

    internal static TiffScanlineWriteResult WriteScanline(TiffDirectory directory, Span<byte> destination, TiffByteOrder byteOrder, ulong row, int sampleIndex, ReadOnlySpan<byte> source)
    {
        return WriteScanline(directory, destination, byteOrder, row, sampleIndex, source, new TiffLimits());
    }

    internal static TiffScanlineWriteResult WriteScanline(TiffDirectory directory, Span<byte> destination, TiffByteOrder byteOrder, ulong row, int sampleIndex, ReadOnlySpan<byte> source, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A directory is required to write a scanline.");
            return TiffScanlineWriteResult.Create(row, sampleIndex, 0, 0, TiffCompression.Unknown, 0, diagnostics);
        }

        TiffImageInfoResult infoResult = TiffImageInfoExtractor.Extract(directory, destination, byteOrder, TiffReadOptions.Create(actualLimits));
        diagnostics.Merge(infoResult.Diagnostics);
        if (!infoResult.Success)
        {
            return TiffScanlineWriteResult.Create(row, sampleIndex, 0, 0, infoResult.Info.Compression, 0, diagnostics);
        }

        if (!ValidateStrippedImage(infoResult.Info, diagnostics))
        {
            return TiffScanlineWriteResult.Create(row, sampleIndex, 0, 0, infoResult.Info.Compression, 0, diagnostics);
        }

        TiffIfdEntry? stripOffsetsEntry = directory.FindEntry(TiffTagId.StripOffsets);
        if (stripOffsetsEntry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "StripOffsets is required to write a scanline.", directory.ImageIndex, TiffTagId.StripOffsets);
            return TiffScanlineWriteResult.Create(row, sampleIndex, 0, 0, infoResult.Info.Compression, 0, diagnostics);
        }

        TiffIfdEntry? stripByteCountsEntry = directory.FindEntry(TiffTagId.StripByteCounts);
        if (stripByteCountsEntry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "StripByteCounts is required to write a scanline.", directory.ImageIndex, TiffTagId.StripByteCounts);
            return TiffScanlineWriteResult.Create(row, sampleIndex, 0, 0, infoResult.Info.Compression, 0, diagnostics);
        }

        ulong[] stripOffsets = TiffImageInfoExtractor.ReadUnsignedArray(stripOffsetsEntry, destination, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        ulong[] stripByteCounts = TiffImageInfoExtractor.ReadUnsignedArray(stripByteCountsEntry, destination, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffScanlineWriteResult.Create(row, sampleIndex, 0, 0, infoResult.Info.Compression, 0, diagnostics);
        }

        return WriteScanline(infoResult.Info, stripOffsets, stripByteCounts, destination, row, sampleIndex, source, actualLimits, diagnostics);
    }

    internal static TiffScanlineWriteResult WriteScanline(TiffImageInfo info, ulong[] stripOffsets, ulong[] stripByteCounts, Span<byte> destination, ulong row, ReadOnlySpan<byte> source)
    {
        return WriteScanline(info, stripOffsets, stripByteCounts, destination, row, 0, source, new TiffLimits());
    }

    internal static TiffScanlineWriteResult WriteScanline(TiffImageInfo info, ulong[] stripOffsets, ulong[] stripByteCounts, Span<byte> destination, ulong row, int sampleIndex, ReadOnlySpan<byte> source)
    {
        return WriteScanline(info, stripOffsets, stripByteCounts, destination, row, sampleIndex, source, new TiffLimits());
    }

    internal static TiffScanlineWriteResult WriteScanline(TiffImageInfo info, ulong[] stripOffsets, ulong[] stripByteCounts, Span<byte> destination, ulong row, int sampleIndex, ReadOnlySpan<byte> source, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        return WriteScanline(info, stripOffsets, stripByteCounts, destination, row, sampleIndex, source, actualLimits, diagnostics);
    }

    internal static TiffScanlineWriteResult WriteScanline(TiffImageInfo info, ulong[] stripOffsets, ulong[] stripByteCounts, Span<byte> destination, ulong row, int sampleIndex, ReadOnlySpan<byte> source, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (!ValidateStrippedImage(info, diagnostics))
        {
            return TiffScanlineWriteResult.Create(row, sampleIndex, 0, 0, GetCompression(info), 0, diagnostics);
        }

        ulong stripIndex = TiffStrileLayout.CalculateStripIndexForRowAndSample(info, row, sampleIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffScanlineWriteResult.Create(row, sampleIndex, stripIndex, 0, GetCompression(info), 0, diagnostics);
        }

        ulong scanlineByteCount = TiffScanlineReader.CalculateScanlineByteCount(info, stripIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffScanlineWriteResult.Create(row, sampleIndex, stripIndex, scanlineByteCount, GetCompression(info), 0, diagnostics);
        }

        if (!ValidateSourceLength(source, scanlineByteCount, diagnostics))
        {
            return TiffScanlineWriteResult.Create(row, sampleIndex, stripIndex, scanlineByteCount, GetCompression(info), 0, diagnostics);
        }

        TiffDecodedStripReadResult stripResult = TiffDecodedStripReader.ReadStrip(info, stripOffsets, stripByteCounts, destination, stripIndex, actualLimits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffScanlineWriteResult.Create(row, sampleIndex, stripIndex, scanlineByteCount, GetCompression(info), 0, diagnostics);
        }

        ulong sourceOffset = TiffScanlineReader.CalculateSourceOffset(info, row, stripIndex, scanlineByteCount, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffScanlineWriteResult.Create(row, sampleIndex, stripIndex, scanlineByteCount, GetCompression(info), 0, diagnostics);
        }

        byte[] stripBytes = TiffIfdEntry.CopyBytes(stripResult.Bytes);
        CopyScanlineToStrip(source, stripBytes, sourceOffset, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffScanlineWriteResult.Create(row, sampleIndex, stripIndex, scanlineByteCount, GetCompression(info), 0, diagnostics);
        }

        TiffDecodedStripWriteResult writeResult = TiffDecodedStripWriter.WriteStrip(info, stripOffsets, stripByteCounts, destination, stripIndex, stripBytes, actualLimits, diagnostics);
        return TiffScanlineWriteResult.Create(row, sampleIndex, stripIndex, scanlineByteCount, GetCompression(info), writeResult.BytesWritten, diagnostics);
    }

    internal static bool ValidateSourceLength(ReadOnlySpan<byte> source, ulong scanlineByteCount, TiffDiagnostics diagnostics)
    {
        if (scanlineByteCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Scanline byte count is too large to write.");
            return false;
        }

        if ((ulong)source.Length != scanlineByteCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Scanline source byte count does not match the expected row byte count.");
            return false;
        }

        return true;
    }

    internal static void CopyScanlineToStrip(ReadOnlySpan<byte> source, byte[] stripBytes, ulong sourceOffset, TiffDiagnostics diagnostics)
    {
        if (stripBytes == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Decoded strip bytes are required to write a scanline.");
            return;
        }

        if (sourceOffset > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Scanline byte offset exceeds the supported array range.");
            return;
        }

        if (sourceOffset > ulong.MaxValue - (ulong)source.Length)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Scanline byte range end offset overflows the supported range.");
            return;
        }

        ulong endOffset = sourceOffset + (ulong)source.Length;
        if (endOffset > (ulong)stripBytes.Length)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Scanline byte range is outside the decoded strip.");
            return;
        }

        TiffRawStripWriter.CopyBytes(source, stripBytes.AsSpan((int)sourceOffset, source.Length));
    }

    internal static bool ValidateStrippedImage(TiffImageInfo info, TiffDiagnostics diagnostics)
    {
        if (info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required to write a scanline.");
            return false;
        }

        if (info.IsTiled)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Scanline writing requires stripped image data.");
            return false;
        }

        return true;
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
