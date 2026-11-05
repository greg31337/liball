using System;

namespace tiff;

internal class TiffSourceRasterReader
{
    internal static TiffSourceRasterReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder)
    {
        return Read(directory, fileBytes, byteOrder, new TiffLimits());
    }

    internal static TiffSourceRasterReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A directory is required to read a source-format raster.");
            return TiffSourceRasterReadResult.Create(new TiffImageInfo(), 0, 0, 0, 0, new byte[0], diagnostics);
        }

        TiffImageInfoResult infoResult = TiffImageInfoExtractor.Extract(directory, fileBytes, byteOrder, TiffReadOptions.Create(actualLimits));
        diagnostics.Merge(infoResult.Diagnostics);
        if (!infoResult.Success)
        {
            return TiffSourceRasterReadResult.Create(infoResult.Info, infoResult.Info.Width, infoResult.Info.Length, 0, 0, new byte[0], diagnostics);
        }

        int offsetsTagId = TiffTagId.StripOffsets;
        int byteCountsTagId = TiffTagId.StripByteCounts;
        if (infoResult.Info.IsTiled)
        {
            offsetsTagId = TiffTagId.TileOffsets;
            byteCountsTagId = TiffTagId.TileByteCounts;
        }

        TiffIfdEntry? offsetsEntry = directory.FindEntry(offsetsTagId);
        if (offsetsEntry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "Image data offsets are required to read a source-format raster.", directory.ImageIndex, offsetsTagId);
            return TiffSourceRasterReadResult.Create(infoResult.Info, infoResult.Info.Width, infoResult.Info.Length, 0, 0, new byte[0], diagnostics);
        }

        TiffIfdEntry? byteCountsEntry = directory.FindEntry(byteCountsTagId);
        if (byteCountsEntry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "Image data byte counts are required to read a source-format raster.", directory.ImageIndex, byteCountsTagId);
            return TiffSourceRasterReadResult.Create(infoResult.Info, infoResult.Info.Width, infoResult.Info.Length, 0, 0, new byte[0], diagnostics);
        }

        ulong[] offsets = TiffImageInfoExtractor.ReadUnsignedArray(offsetsEntry, fileBytes, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        ulong[] byteCounts = TiffImageInfoExtractor.ReadUnsignedArray(byteCountsEntry, fileBytes, byteOrder, actualLimits, directory.ImageIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffSourceRasterReadResult.Create(infoResult.Info, infoResult.Info.Width, infoResult.Info.Length, 0, 0, new byte[0], diagnostics);
        }

        return Read(infoResult.Info, offsets, byteCounts, fileBytes, actualLimits, diagnostics, byteOrder);
    }

    internal static TiffSourceRasterReadResult Read(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ReadOnlySpan<byte> fileBytes)
    {
        return Read(info, offsets, byteCounts, fileBytes, new TiffLimits());
    }

    internal static TiffSourceRasterReadResult Read(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ReadOnlySpan<byte> fileBytes, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        return Read(info, offsets, byteCounts, fileBytes, limits, diagnostics, TiffByteOrder.LittleEndian);
    }

    internal static TiffSourceRasterReadResult Read(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, ReadOnlySpan<byte> fileBytes, TiffLimits limits, TiffDiagnostics diagnostics, TiffByteOrder byteOrder)
    {
        if (info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required to read a source-format raster.");
            return TiffSourceRasterReadResult.Create(new TiffImageInfo(), 0, 0, 0, 0, new byte[0], diagnostics);
        }

        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        byte[] sourceBytes = TiffRgba8Reader.ReadSourceBytes(info, offsets, byteCounts, fileBytes, actualLimits, diagnostics, byteOrder);
        if (diagnostics.HasErrors())
        {
            return TiffSourceRasterReadResult.Create(info, info.Width, info.Length, 0, 0, new byte[0], diagnostics);
        }

        TiffImageInfo contiguousInfo = TiffRgba8Reader.CreateFullContiguousInfo(info);
        int[] sampleByteCounts = TiffRgba8Reader.CalculateSampleByteCounts(contiguousInfo, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffSourceRasterReadResult.Create(contiguousInfo, info.Width, info.Length, 0, 0, new byte[0], diagnostics);
        }

        int bytesPerPixel = TiffRgba8Reader.CalculateBytesPerPixel(sampleByteCounts, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffSourceRasterReadResult.Create(contiguousInfo, info.Width, info.Length, 0, 0, new byte[0], diagnostics);
        }

        TiffOrientationTransformResult oriented = TiffOrientationTransformer.TransformInterleavedBytes(info.Width, info.Length, bytesPerPixel, info.Orientation, sourceBytes, actualLimits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffSourceRasterReadResult.Create(contiguousInfo, info.Width, info.Length, 0, 0, new byte[0], diagnostics);
        }

        return TiffSourceRasterReadResult.Create(contiguousInfo, oriented.Width, oriented.Length, oriented.RowByteCount, oriented.TotalByteCount, oriented.Bytes, diagnostics);
    }
}
