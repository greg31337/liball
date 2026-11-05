using System;

namespace tiff;

internal class TiffSourceRasterWriter
{
    internal static TiffSourceRasterWriteResult Write(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, Span<byte> destination, ReadOnlySpan<byte> source)
    {
        return Write(info, offsets, byteCounts, destination, source, new TiffLimits());
    }

    internal static TiffSourceRasterWriteResult Write(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, Span<byte> destination, ReadOnlySpan<byte> source, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        return Write(info, offsets, byteCounts, destination, source, limits, diagnostics);
    }

    internal static TiffSourceRasterWriteResult Write(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, Span<byte> destination, ReadOnlySpan<byte> source, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        if (info == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Image information is required to write a source-format raster.");
            return TiffSourceRasterWriteResult.Create(0, 0, TiffCompression.Unknown, diagnostics);
        }

        TiffLimits actualLimits = TiffTagValueLoader.NormalizeLimits(limits);
        int[] sampleByteCounts = CalculateByteAlignedSampleByteCounts(info, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffSourceRasterWriteResult.Create(0, 0, info.Compression, diagnostics);
        }

        int bytesPerPixel = TiffRgba8Reader.CalculateBytesPerPixel(sampleByteCounts, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffSourceRasterWriteResult.Create(0, 0, info.Compression, diagnostics);
        }

        ulong pixelCount = TiffRgba8Reader.CalculatePixelCount(info.Width, info.Length, diagnostics);
        ulong expectedSourceByteCount = TiffRgba8Reader.CalculateTotalByteCount(pixelCount, bytesPerPixel, actualLimits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffSourceRasterWriteResult.Create(0, 0, info.Compression, diagnostics);
        }

        if ((ulong)source.Length != expectedSourceByteCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Source raster byte count does not match image dimensions and sample layout.");
            return TiffSourceRasterWriteResult.Create(0, 0, info.Compression, diagnostics);
        }

        if (info.IsTiled)
        {
            return WriteTiles(info, offsets, byteCounts, destination, source, sampleByteCounts, bytesPerPixel, actualLimits, diagnostics);
        }

        return WriteStrips(info, offsets, byteCounts, destination, source, sampleByteCounts, bytesPerPixel, actualLimits, diagnostics);
    }

    internal static TiffSourceRasterWriteResult WriteStrips(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, Span<byte> destination, ReadOnlySpan<byte> source, int[] sampleByteCounts, int bytesPerPixel, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        ulong stripCount = TiffStrileLayout.CalculateStripCount(info, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffSourceRasterWriteResult.Create(0, 0, info.Compression, diagnostics);
        }

        ulong bytesWritten = 0;
        ulong stripIndex = 0;
        while (stripIndex < stripCount)
        {
            byte[] stripBytes = CreateStripBytes(info, source, sampleByteCounts, bytesPerPixel, stripIndex, diagnostics);
            if (diagnostics.HasErrors())
            {
                return TiffSourceRasterWriteResult.Create(stripCount, bytesWritten, info.Compression, diagnostics);
            }

            TiffDecodedStripWriteResult result = TiffDecodedStripWriter.WriteStrip(info, offsets, byteCounts, destination, stripIndex, stripBytes, limits, diagnostics);
            if (diagnostics.HasErrors())
            {
                return TiffSourceRasterWriteResult.Create(stripCount, bytesWritten, info.Compression, diagnostics);
            }

            bytesWritten = bytesWritten + result.BytesWritten;
            stripIndex = stripIndex + 1;
        }

        return TiffSourceRasterWriteResult.Create(stripCount, bytesWritten, info.Compression, diagnostics);
    }

    internal static TiffSourceRasterWriteResult WriteTiles(TiffImageInfo info, ulong[] offsets, ulong[] byteCounts, Span<byte> destination, ReadOnlySpan<byte> source, int[] sampleByteCounts, int bytesPerPixel, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        ulong tileCount = TiffStrileLayout.CalculateTileCount(info, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffSourceRasterWriteResult.Create(0, 0, info.Compression, diagnostics);
        }

        ulong bytesWritten = 0;
        ulong tileIndex = 0;
        while (tileIndex < tileCount)
        {
            byte[] tileBytes = CreateTileBytes(info, source, sampleByteCounts, bytesPerPixel, tileIndex, diagnostics);
            if (diagnostics.HasErrors())
            {
                return TiffSourceRasterWriteResult.Create(tileCount, bytesWritten, info.Compression, diagnostics);
            }

            TiffDecodedTileWriteResult result = TiffDecodedTileWriter.WriteTile(info, offsets, byteCounts, destination, tileIndex, tileBytes, limits, diagnostics);
            if (diagnostics.HasErrors())
            {
                return TiffSourceRasterWriteResult.Create(tileCount, bytesWritten, info.Compression, diagnostics);
            }

            bytesWritten = bytesWritten + result.BytesWritten;
            tileIndex = tileIndex + 1;
        }

        return TiffSourceRasterWriteResult.Create(tileCount, bytesWritten, info.Compression, diagnostics);
    }

    internal static int[] CalculateByteAlignedSampleByteCounts(TiffImageInfo info, TiffDiagnostics diagnostics)
    {
        int[] byteCounts = new int[info.SamplesPerPixel];
        int sampleIndex = 0;
        while (sampleIndex < info.SamplesPerPixel)
        {
            int bitsPerSample = TiffSubByteSampleUnpacker.GetBitsPerSample(info, sampleIndex, diagnostics);
            if (diagnostics.HasErrors())
            {
                return new int[0];
            }

            if (bitsPerSample < 8 || bitsPerSample % 8 != 0)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Source raster writing requires byte-aligned samples.", info.ImageIndex, TiffTagId.BitsPerSample);
                return new int[0];
            }

            byteCounts[sampleIndex] = TiffSampleFormatConverter.CalculateSampleByteCount(bitsPerSample, diagnostics);
            if (diagnostics.HasErrors())
            {
                return new int[0];
            }

            sampleIndex = sampleIndex + 1;
        }

        return byteCounts;
    }

    internal static byte[] CreateStripBytes(TiffImageInfo info, ReadOnlySpan<byte> source, int[] sampleByteCounts, int bytesPerPixel, ulong stripIndex, TiffDiagnostics diagnostics)
    {
        ulong expectedByteCount = TiffStrileLayout.CalculateStripByteCount(info, stripIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        if (expectedByteCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Strip source bytes exceed the supported array range.");
            return new byte[0];
        }

        byte[] stripBytes = new byte[(int)expectedByteCount];
        ulong rows = TiffStrileLayout.CalculateRowsInStrip(info, stripIndex, diagnostics);
        ulong stripIndexInPlane = TiffStrileLayout.CalculateStripIndexInPlane(info, stripIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        ulong firstRow = stripIndexInPlane * info.RowsPerStrip;
        if (info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
        {
            int sampleIndex = TiffStrileLayout.CalculateStripSampleIndex(info, stripIndex, diagnostics);
            int sampleByteCount = sampleByteCounts[sampleIndex];
            CopySeparateRows(source, stripBytes, info.Width, firstRow, rows, sampleIndex, sampleByteCounts, bytesPerPixel, sampleByteCount);
            return stripBytes;
        }

        ulong rowByteCount = info.Width * (ulong)bytesPerPixel;
        CopyContiguousRows(source, stripBytes, info.Width, firstRow, rows, bytesPerPixel, rowByteCount);
        return stripBytes;
    }

    internal static byte[] CreateTileBytes(TiffImageInfo info, ReadOnlySpan<byte> source, int[] sampleByteCounts, int bytesPerPixel, ulong tileIndex, TiffDiagnostics diagnostics)
    {
        ulong expectedByteCount = TiffStrileLayout.CalculateTileStorageByteCount(info, tileIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        if (expectedByteCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Tile source bytes exceed the supported array range.");
            return new byte[0];
        }

        byte[] tileBytes = new byte[(int)expectedByteCount];
        ulong tilePlaneIndex = CalculateTileIndexInPlane(info, tileIndex, diagnostics);
        ulong tilesAcross = TiffStrileLayout.CalculateTilesAcross(info, diagnostics);
        ulong tileColumn = tilePlaneIndex % tilesAcross;
        ulong tileRow = tilePlaneIndex / tilesAcross;
        ulong firstColumn = tileColumn * info.TileWidth;
        ulong firstRow = tileRow * info.TileLength;
        ulong imageTileWidth = TiffStrileLayout.CalculateTileWidthInImage(info, tileIndex, diagnostics);
        ulong imageTileLength = TiffStrileLayout.CalculateTileLengthInImage(info, tileIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return new byte[0];
        }

        if (info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
        {
            int sampleIndex = TiffStrileLayout.CalculateTileSampleIndex(info, tileIndex, diagnostics);
            int sampleByteCount = sampleByteCounts[sampleIndex];
            CopySeparateTile(source, tileBytes, info.Width, info.TileWidth, firstColumn, firstRow, imageTileWidth, imageTileLength, sampleIndex, sampleByteCounts, bytesPerPixel, sampleByteCount);
            return tileBytes;
        }

        CopyContiguousTile(source, tileBytes, info.Width, info.TileWidth, firstColumn, firstRow, imageTileWidth, imageTileLength, bytesPerPixel);
        return tileBytes;
    }

    internal static ulong CalculateTileIndexInPlane(TiffImageInfo info, ulong tileIndex, TiffDiagnostics diagnostics)
    {
        ulong tileCount = TiffStrileLayout.CalculateTileCount(info, diagnostics);
        if (tileCount == 0)
        {
            return 0;
        }

        if (tileIndex >= tileCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Tile index is outside the available tile range.");
            return 0;
        }

        if (info.PlanarConfiguration == TiffPlanarConfiguration.Separate)
        {
            ulong tilesPerPlane = TiffStrileLayout.CalculateTilesPerPlane(info, diagnostics);
            if (diagnostics.HasErrors())
            {
                return 0;
            }

            return tileIndex % tilesPerPlane;
        }

        return tileIndex;
    }

    internal static void CopyContiguousRows(ReadOnlySpan<byte> source, byte[] destination, ulong width, ulong firstRow, ulong rows, int bytesPerPixel, ulong rowByteCount)
    {
        ulong rowIndex = 0;
        while (rowIndex < rows)
        {
            ulong sourceOffset = (firstRow + rowIndex) * width * (ulong)bytesPerPixel;
            ulong destinationOffset = rowIndex * rowByteCount;
            CopyBytes(source, sourceOffset, destination, destinationOffset, rowByteCount);
            rowIndex = rowIndex + 1;
        }
    }

    internal static void CopySeparateRows(ReadOnlySpan<byte> source, byte[] destination, ulong width, ulong firstRow, ulong rows, int sampleIndex, int[] sampleByteCounts, int bytesPerPixel, int sampleByteCount)
    {
        int channelOffset = CalculateChannelOffset(sampleByteCounts, sampleIndex);
        ulong rowIndex = 0;
        while (rowIndex < rows)
        {
            ulong column = 0;
            while (column < width)
            {
                ulong sourceOffset = ((firstRow + rowIndex) * width + column) * (ulong)bytesPerPixel + (ulong)channelOffset;
                ulong destinationOffset = (rowIndex * width + column) * (ulong)sampleByteCount;
                CopyBytes(source, sourceOffset, destination, destinationOffset, (ulong)sampleByteCount);
                column = column + 1;
            }

            rowIndex = rowIndex + 1;
        }
    }

    internal static void CopyContiguousTile(ReadOnlySpan<byte> source, byte[] destination, ulong imageWidth, ulong tileWidth, ulong firstColumn, ulong firstRow, ulong copyWidth, ulong copyLength, int bytesPerPixel)
    {
        ulong sourceRowByteCount = imageWidth * (ulong)bytesPerPixel;
        ulong destinationRowByteCount = tileWidth * (ulong)bytesPerPixel;
        ulong copyByteCount = copyWidth * (ulong)bytesPerPixel;
        ulong rowIndex = 0;
        while (rowIndex < copyLength)
        {
            ulong sourceOffset = (firstRow + rowIndex) * sourceRowByteCount + firstColumn * (ulong)bytesPerPixel;
            ulong destinationOffset = rowIndex * destinationRowByteCount;
            CopyBytes(source, sourceOffset, destination, destinationOffset, copyByteCount);
            rowIndex = rowIndex + 1;
        }
    }

    internal static void CopySeparateTile(ReadOnlySpan<byte> source, byte[] destination, ulong imageWidth, ulong tileWidth, ulong firstColumn, ulong firstRow, ulong copyWidth, ulong copyLength, int sampleIndex, int[] sampleByteCounts, int bytesPerPixel, int sampleByteCount)
    {
        int channelOffset = CalculateChannelOffset(sampleByteCounts, sampleIndex);
        ulong destinationRowByteCount = tileWidth * (ulong)sampleByteCount;
        ulong rowIndex = 0;
        while (rowIndex < copyLength)
        {
            ulong column = 0;
            while (column < copyWidth)
            {
                ulong sourceOffset = ((firstRow + rowIndex) * imageWidth + firstColumn + column) * (ulong)bytesPerPixel + (ulong)channelOffset;
                ulong destinationOffset = rowIndex * destinationRowByteCount + column * (ulong)sampleByteCount;
                CopyBytes(source, sourceOffset, destination, destinationOffset, (ulong)sampleByteCount);
                column = column + 1;
            }

            rowIndex = rowIndex + 1;
        }
    }

    internal static int CalculateChannelOffset(int[] sampleByteCounts, int sampleIndex)
    {
        int offset = 0;
        int index = 0;
        while (index < sampleIndex)
        {
            offset = offset + sampleByteCounts[index];
            index = index + 1;
        }

        return offset;
    }

    internal static void CopyBytes(ReadOnlySpan<byte> source, ulong sourceOffset, byte[] destination, ulong destinationOffset, ulong byteCount)
    {
        int index = 0;
        while ((ulong)index < byteCount)
        {
            destination[(int)destinationOffset + index] = source[(int)sourceOffset + index];
            index = index + 1;
        }
    }
}
