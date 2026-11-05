using System;

namespace tiff;

internal class TiffSeriesBuilder
{
    internal static TiffSeriesGroupResult GroupSource(TiffDirectory[] directories, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffReadOptions options)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (directories == null || source == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Directories and a source are required to group TIFF series.");
            return TiffSeriesGroupResult.Create(new TiffSeries[0], diagnostics);
        }

        if (byteOrder != TiffByteOrder.LittleEndian && byteOrder != TiffByteOrder.BigEndian)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "Series grouping requires a known byte order.");
            return TiffSeriesGroupResult.Create(new TiffSeries[0], diagnostics);
        }

        TiffLimits limits = TiffDirectoryReader.GetLimits(options);
        TiffSeries[] series = new TiffSeries[0];
        int imageIndex = 0;
        while (imageIndex < directories.Length)
        {
            TiffImageJMetadata metadata = ReadSourceImageJMetadata(directories[imageIndex], source, limits, diagnostics);
            if (diagnostics.HasErrors())
            {
                return TiffSeriesGroupResult.Create(new TiffSeries[0], diagnostics);
            }

            if (metadata.HasImageJ)
            {
                int requestedPlaneCount = CalculateImageJPlaneCount(metadata);
                int available = directories.Length - imageIndex;
                int planeCount = requestedPlaneCount;
                if (planeCount > available)
                {
                    planeCount = available;
                }

                if (planeCount <= 0)
                {
                    planeCount = 1;
                }

                ulong width = ReadSourceDimension(directories[imageIndex], source, byteOrder, limits, TiffTagId.ImageWidth, diagnostics);
                ulong length = ReadSourceDimension(directories[imageIndex], source, byteOrder, limits, TiffTagId.ImageLength, diagnostics);
                int sizeC = NormalizeDimension(metadata.Channels);
                int sizeZ = NormalizeDimension(metadata.Slices);
                int sizeT = NormalizeDimension(metadata.Frames);
                TiffSeriesPlane[] planes = CreatePlanes(series.Length, directories, imageIndex, planeCount, sizeC, sizeZ);
                series = AppendSeries(series, TiffSeries.Create(series.Length, true, "ImageJ", width, length, sizeC, sizeZ, sizeT, planes));
                imageIndex = imageIndex + planeCount;
            }
            else
            {
                ulong width = ReadSourceDimension(directories[imageIndex], source, byteOrder, limits, TiffTagId.ImageWidth, diagnostics);
                ulong length = ReadSourceDimension(directories[imageIndex], source, byteOrder, limits, TiffTagId.ImageLength, diagnostics);
                int planeCount = CountSourceDimensionRun(directories, source, byteOrder, limits, imageIndex, width, length, diagnostics);
                TiffSeriesPlane[] planes = CreatePlanes(series.Length, directories, imageIndex, planeCount, 1, 1);
                series = AppendSeries(series, TiffSeries.Create(series.Length, false, "Dimensions", width, length, 1, 1, planeCount, planes));
                imageIndex = imageIndex + planeCount;
            }
        }

        return TiffSeriesGroupResult.Create(series, diagnostics);
    }

    internal static TiffSeriesGroupResult Group(TiffDirectory[] directories, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (directories == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "Directories are required to group TIFF series.");
            return TiffSeriesGroupResult.Create(new TiffSeries[0], diagnostics);
        }

        if (byteOrder != TiffByteOrder.LittleEndian && byteOrder != TiffByteOrder.BigEndian)
        {
            diagnostics.AddError(TiffErrorCode.InvalidByteOrder, "Series grouping requires a known byte order.");
            return TiffSeriesGroupResult.Create(new TiffSeries[0], diagnostics);
        }

        TiffSeries[] series = new TiffSeries[0];
        int imageIndex = 0;
        while (imageIndex < directories.Length)
        {
            TiffImageJMetadataReadResult imageJ = TiffImageJMetadataReader.Read(directories[imageIndex], fileBytes);
            diagnostics.Merge(imageJ.Diagnostics);
            if (diagnostics.HasErrors())
            {
                return TiffSeriesGroupResult.Create(new TiffSeries[0], diagnostics);
            }

            if (imageJ.Metadata.HasImageJ)
            {
                TiffSeries imageJSeries = CreateImageJSeries(series.Length, directories, fileBytes, byteOrder, imageIndex, imageJ.Metadata, diagnostics);
                series = AppendSeries(series, imageJSeries);
                imageIndex = imageIndex + imageJSeries.PlaneCount();
            }
            else
            {
                TiffSeries dimensionSeries = CreateDimensionSeries(series.Length, directories, fileBytes, byteOrder, imageIndex, diagnostics);
                series = AppendSeries(series, dimensionSeries);
                imageIndex = imageIndex + dimensionSeries.PlaneCount();
            }
        }

        return TiffSeriesGroupResult.Create(series, diagnostics);
    }

    internal static TiffSeries CreateImageJSeries(int seriesIndex, TiffDirectory[] directories, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, int startIndex, TiffImageJMetadata metadata, TiffDiagnostics diagnostics)
    {
        int requestedPlaneCount = CalculateImageJPlaneCount(metadata);
        int available = directories.Length - startIndex;
        int planeCount = requestedPlaneCount;
        if (planeCount > available)
        {
            planeCount = available;
        }

        if (planeCount <= 0)
        {
            planeCount = 1;
        }

        ulong width = ReadDimension(directories[startIndex], fileBytes, byteOrder, TiffTagId.ImageWidth, diagnostics);
        ulong length = ReadDimension(directories[startIndex], fileBytes, byteOrder, TiffTagId.ImageLength, diagnostics);
        int sizeC = NormalizeDimension(metadata.Channels);
        int sizeZ = NormalizeDimension(metadata.Slices);
        int sizeT = NormalizeDimension(metadata.Frames);
        TiffSeriesPlane[] planes = CreatePlanes(seriesIndex, directories, startIndex, planeCount, sizeC, sizeZ);
        return TiffSeries.Create(seriesIndex, true, "ImageJ", width, length, sizeC, sizeZ, sizeT, planes);
    }

    internal static TiffSeries CreateDimensionSeries(int seriesIndex, TiffDirectory[] directories, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, int startIndex, TiffDiagnostics diagnostics)
    {
        ulong width = ReadDimension(directories[startIndex], fileBytes, byteOrder, TiffTagId.ImageWidth, diagnostics);
        ulong length = ReadDimension(directories[startIndex], fileBytes, byteOrder, TiffTagId.ImageLength, diagnostics);
        int planeCount = CountMatchingDimensionRun(directories, fileBytes, byteOrder, startIndex, width, length, diagnostics);
        TiffSeriesPlane[] planes = CreatePlanes(seriesIndex, directories, startIndex, planeCount, 1, 1);
        return TiffSeries.Create(seriesIndex, false, "Dimensions", width, length, 1, 1, planeCount, planes);
    }

    internal static int CalculateImageJPlaneCount(TiffImageJMetadata metadata)
    {
        if (metadata == null)
        {
            return 1;
        }

        if (metadata.ImageCount > 0)
        {
            return metadata.ImageCount;
        }

        int product = 1;
        bool hasDimension = false;
        if (metadata.Channels > 0)
        {
            product = product * metadata.Channels;
            hasDimension = true;
        }

        if (metadata.Slices > 0)
        {
            product = product * metadata.Slices;
            hasDimension = true;
        }

        if (metadata.Frames > 0)
        {
            product = product * metadata.Frames;
            hasDimension = true;
        }

        if (hasDimension)
        {
            return product;
        }

        return 1;
    }

    internal static int CountMatchingDimensionRun(TiffDirectory[] directories, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, int startIndex, ulong width, ulong length, TiffDiagnostics diagnostics)
    {
        int count = 0;
        int index = startIndex;
        while (index < directories.Length)
        {
            ulong currentWidth = ReadDimension(directories[index], fileBytes, byteOrder, TiffTagId.ImageWidth, diagnostics);
            ulong currentLength = ReadDimension(directories[index], fileBytes, byteOrder, TiffTagId.ImageLength, diagnostics);
            if (currentWidth != width || currentLength != length)
            {
                return count;
            }

            count = count + 1;
            index = index + 1;
        }

        return count;
    }

    internal static int CountSourceDimensionRun(TiffDirectory[] directories, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits, int startIndex, ulong width, ulong length, TiffDiagnostics diagnostics)
    {
        int count = 0;
        int index = startIndex;
        while (index < directories.Length)
        {
            ulong currentWidth = ReadSourceDimension(directories[index], source, byteOrder, limits, TiffTagId.ImageWidth, diagnostics);
            ulong currentLength = ReadSourceDimension(directories[index], source, byteOrder, limits, TiffTagId.ImageLength, diagnostics);
            if (currentWidth != width || currentLength != length)
            {
                return count;
            }

            count = count + 1;
            index = index + 1;
        }

        return count;
    }

    internal static ulong ReadDimension(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffByteOrder byteOrder, int tagId, TiffDiagnostics diagnostics)
    {
        TiffLimits limits = new TiffLimits();
        return TiffImageInfoExtractor.ReadOptionalUnsignedScalar(directory, fileBytes, byteOrder, limits, tagId, 0, diagnostics);
    }

    internal static ulong ReadSourceDimension(TiffDirectory directory, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits, int tagId, TiffDiagnostics diagnostics)
    {
        return TiffSourceImageInfoExtractor.ReadOptionalUnsignedScalar(source, directory, byteOrder, limits, tagId, 0, diagnostics);
    }

    internal static TiffImageJMetadata ReadSourceImageJMetadata(TiffDirectory directory, ITiffRandomAccessSource source, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        TiffIfdEntry? entry = directory.FindEntry(TiffTagId.ImageDescription);
        if (entry == null)
        {
            return new TiffImageJMetadata();
        }

        if (entry.DataType != TiffDataType.Ascii)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "ImageDescription must use ASCII data for series grouping.", directory.ImageIndex, TiffTagId.ImageDescription);
            return new TiffImageJMetadata();
        }

        TiffTagValueLoadResult loadResult = TiffTagValueLoader.LoadSourceBytes(entry, source, limits);
        diagnostics.Merge(loadResult.Diagnostics);
        if (diagnostics.HasErrors())
        {
            return new TiffImageJMetadata();
        }

        TiffAsciiResult ascii = TiffAscii.Decode(loadResult.Bytes);
        diagnostics.Merge(ascii.Diagnostics);
        if (diagnostics.HasErrors())
        {
            return new TiffImageJMetadata();
        }

        return TiffImageJMetadataReader.Parse(ascii.Text, loadResult.Bytes);
    }

    internal static TiffSeriesPlane FindPlane(TiffSeries series, int channelIndex, int zIndex, int timeIndex, TiffDiagnostics diagnostics)
    {
        if (series == null || channelIndex < 0 || zIndex < 0 || timeIndex < 0 || channelIndex >= series.SizeC || zIndex >= series.SizeZ || timeIndex >= series.SizeT)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "C, Z, or T coordinate is outside the selected series dimensions.");
            return new TiffSeriesPlane();
        }

        int index = 0;
        while (index < series.Planes.Length)
        {
            TiffSeriesPlane plane = series.Planes[index];
            if (plane.ChannelIndex == channelIndex && plane.ZIndex == zIndex && plane.TimeIndex == timeIndex)
            {
                return plane.Copy();
            }

            index = index + 1;
        }

        diagnostics.AddError(TiffErrorCode.InvalidOffset, "The selected series does not contain the requested C/Z/T plane.");
        return new TiffSeriesPlane();
    }

    internal static TiffSeriesPlane[] CreatePlanes(int seriesIndex, TiffDirectory[] directories, int startIndex, int planeCount, int sizeC, int sizeZ)
    {
        TiffSeriesPlane[] planes = new TiffSeriesPlane[planeCount];
        int channelCount = NormalizeDimension(sizeC);
        int depthCount = NormalizeDimension(sizeZ);
        int index = 0;
        while (index < planeCount)
        {
            TiffDirectory directory = directories[startIndex + index];
            int channelIndex = index % channelCount;
            int zIndex = (int)(((long)index / (long)channelCount) % (long)depthCount);
            int timeIndex = (int)((long)index / ((long)channelCount * (long)depthCount));
            planes[index] = TiffSeriesPlane.Create(seriesIndex, index, channelIndex, zIndex, timeIndex, directory.ImageIndex, directory);
            index = index + 1;
        }

        return planes;
    }

    internal static int NormalizeDimension(int value)
    {
        if (value <= 0)
        {
            return 1;
        }

        return value;
    }

    internal static TiffSeries[] AppendSeries(TiffSeries[] series, TiffSeries value)
    {
        TiffSeries[] result = new TiffSeries[series.Length + 1];
        int index = 0;
        while (index < series.Length)
        {
            result[index] = series[index].Copy();
            index = index + 1;
        }

        result[index] = value.Copy();
        return result;
    }
}
