using System;

namespace tiff;

internal class TiffGeoModelMetadataReader
{
    internal static TiffGeoModelReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffHeader header)
    {
        if (header == null)
        {
            TiffDiagnostics diagnostics = new TiffDiagnostics();
            diagnostics.AddError(TiffErrorCode.UnknownError, "A TIFF header is required to read GeoTIFF model metadata.");
            return TiffGeoModelReadResult.Create(new TiffGeoModelMetadata(), diagnostics);
        }

        return Read(directory, fileBytes, header.ByteOrder);
    }

    internal static TiffGeoModelReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder)
    {
        return Read(directory, fileBytes, byteOrder, new TiffLimits());
    }

    internal static TiffGeoModelReadResult Read(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffLimits limits)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (directory == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A directory is required to read GeoTIFF model metadata.");
            return TiffGeoModelReadResult.Create(new TiffGeoModelMetadata(), diagnostics);
        }

        double[] pixelScale = ReadDoubleTag(directory, fileBytes, byteOrder, limits, TiffTagId.ModelPixelScale, 3, true, diagnostics);
        double[] tiepointValues = ReadDoubleTag(directory, fileBytes, byteOrder, limits, TiffTagId.ModelTiepoint, 6, false, diagnostics);
        double[] transformation = ReadDoubleTag(directory, fileBytes, byteOrder, limits, TiffTagId.ModelTransformation, 16, true, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffGeoModelReadResult.Create(new TiffGeoModelMetadata(), diagnostics);
        }

        bool hasPixelScale = pixelScale.Length > 0;
        bool hasTiepoints = tiepointValues.Length > 0;
        bool hasTransformation = transformation.Length > 0;
        TiffGeoTiepoint[] tiepoints = CreateTiepoints(tiepointValues);
        TiffGeoModelMetadata metadata = TiffGeoModelMetadata.Create(hasPixelScale, pixelScale, hasTiepoints, tiepoints, hasTransformation, transformation);
        TiffGeoKeyResolver.ValidateModelTags(metadata, directory.ImageIndex, diagnostics);
        return TiffGeoModelReadResult.Create(metadata, diagnostics);
    }

    internal static double[] ReadDoubleTag(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, TiffLimits limits, int tagId, long expectedCount, bool exactCount, TiffDiagnostics diagnostics)
    {
        TiffIfdEntry? entry = directory.FindEntry(tagId);
        if (entry == null)
        {
            return new double[0];
        }

        if (entry.DataType != TiffDataType.Double)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "GeoTIFF model tag must use DOUBLE data.", directory.ImageIndex, tagId);
            return new double[0];
        }

        if (!IsValidCount(entry.Count, expectedCount, exactCount))
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "GeoTIFF model tag has an invalid value count.", directory.ImageIndex, tagId);
            return new double[0];
        }

        TiffTagValueLoadResult loadResult = TiffTagValueLoader.LoadBytes(entry, fileBytes, limits);
        diagnostics.Merge(loadResult.Diagnostics);
        if (!loadResult.Success)
        {
            return new double[0];
        }

        return DecodeDoubles(loadResult.Bytes, entry.Count, byteOrder, diagnostics);
    }

    internal static bool IsValidCount(long actualCount, long expectedCount, bool exactCount)
    {
        if (actualCount <= 0)
        {
            return false;
        }

        if (exactCount)
        {
            return actualCount == expectedCount;
        }

        return actualCount % expectedCount == 0;
    }

    internal static double[] DecodeDoubles(byte[] bytes, long count, TiffByteOrder byteOrder, TiffDiagnostics diagnostics)
    {
        if (count > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "GeoTIFF model tag count is too large to allocate.");
            return new double[0];
        }

        long expectedByteCount = count * 8;
        if (bytes == null || bytes.Length != expectedByteCount)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteRange, "GeoTIFF model tag byte count does not match its value count.");
            return new double[0];
        }

        double[] values = new double[(int)count];
        int index = 0;
        while (index < values.Length)
        {
            int offset = index * 8;
            values[index] = TiffBinaryReader.ReadDouble(bytes.AsSpan(offset, 8), byteOrder);
            index = index + 1;
        }

        return values;
    }

    internal static TiffGeoTiepoint[] CreateTiepoints(double[] values)
    {
        if (values == null || values.Length == 0)
        {
            return new TiffGeoTiepoint[0];
        }

        TiffGeoTiepoint[] tiepoints = new TiffGeoTiepoint[values.Length / 6];
        int index = 0;
        while (index < tiepoints.Length)
        {
            int offset = index * 6;
            tiepoints[index] = TiffGeoTiepoint.Create(values[offset], values[offset + 1], values[offset + 2], values[offset + 3], values[offset + 4], values[offset + 5]);
            index = index + 1;
        }

        return tiepoints;
    }
}
