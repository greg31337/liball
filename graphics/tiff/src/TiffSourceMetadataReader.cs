namespace tiff;

internal class TiffSourceMetadataReader
{
    internal static TiffExifReadResult ReadExif(TiffDirectory directory, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffFileKind fileKind, TiffReadOptions options)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffIfdPointerReadResult pointerResult = TiffSourceIfdPointerReader.Read(directory, source, byteOrder, fileKind, options);
        diagnostics.Merge(pointerResult.Diagnostics);
        TiffIfdPointer pointer = TiffExifMetadataReader.FindPointer(pointerResult.Pointers, TiffTagId.ExifIfd);
        if (diagnostics.HasErrors() || pointer.TagId == 0)
        {
            return TiffExifReadResult.Create(new TiffExifMetadata(), diagnostics);
        }

        if (pointer.Directories.Length == 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidOffset, "EXIF IFD pointer did not resolve to a directory.", directory.ImageIndex, TiffTagId.ExifIfd);
            return TiffExifReadResult.Create(new TiffExifMetadata(), diagnostics);
        }

        TiffDirectory metadataDirectory = pointer.Directories[0];
        TiffTagValue[] values = ReadDirectoryValues(metadataDirectory, source, byteOrder, options.Limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffExifReadResult.Create(new TiffExifMetadata(), diagnostics);
        }

        TiffIfdPointer[] interoperabilityPointers = TiffExifMetadataReader.FilterPointers(pointerResult.Pointers, TiffTagId.InteroperabilityIfd);
        TiffExifMetadata metadata = TiffExifMetadata.Create(true, directory.ImageIndex, directory.Offset, pointer.Offset, metadataDirectory, values, interoperabilityPointers);
        return TiffExifReadResult.Create(metadata, diagnostics);
    }

    internal static TiffGpsReadResult ReadGps(TiffDirectory directory, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffFileKind fileKind, TiffReadOptions options)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffIfdPointerReadResult pointerResult = TiffSourceIfdPointerReader.Read(directory, source, byteOrder, fileKind, options);
        diagnostics.Merge(pointerResult.Diagnostics);
        TiffIfdPointer pointer = TiffExifMetadataReader.FindPointer(pointerResult.Pointers, TiffTagId.GpsIfd);
        if (diagnostics.HasErrors() || pointer.TagId == 0)
        {
            return TiffGpsReadResult.Create(new TiffGpsMetadata(), diagnostics);
        }

        if (pointer.Directories.Length == 0)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidOffset, "GPS IFD pointer did not resolve to a directory.", directory.ImageIndex, TiffTagId.GpsIfd);
            return TiffGpsReadResult.Create(new TiffGpsMetadata(), diagnostics);
        }

        TiffDirectory metadataDirectory = pointer.Directories[0];
        TiffTagValue[] values = ReadDirectoryValues(metadataDirectory, source, byteOrder, options.Limits, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffGpsReadResult.Create(new TiffGpsMetadata(), diagnostics);
        }

        TiffGpsMetadata metadata = TiffGpsMetadata.Create(true, directory.ImageIndex, directory.Offset, pointer.Offset, metadataDirectory, values);
        return TiffGpsReadResult.Create(metadata, diagnostics);
    }

    internal static TiffXmpReadResult ReadXmp(TiffDirectory directory, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits)
    {
        TiffMetadataSourceSnapshot snapshot = TiffMetadataSourceSnapshot.Create(directory, source, byteOrder, limits, new int[] { TiffTagId.Xmp });
        TiffXmpReadResult result = TiffXmpMetadataReader.Read(snapshot.Directory, snapshot.Bytes, limits);
        snapshot.Diagnostics.Merge(result.Diagnostics);
        return TiffXmpReadResult.Create(result.Packet, snapshot.Diagnostics);
    }

    internal static TiffIccProfileReadResult ReadIccProfile(TiffDirectory directory, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits)
    {
        TiffMetadataSourceSnapshot snapshot = TiffMetadataSourceSnapshot.Create(directory, source, byteOrder, limits, new int[] { TiffTagId.IccProfile });
        TiffIccProfileReadResult result = TiffIccProfileReader.Read(snapshot.Directory, snapshot.Bytes, limits);
        snapshot.Diagnostics.Merge(result.Diagnostics);
        return TiffIccProfileReadResult.Create(result.Profile, snapshot.Diagnostics);
    }

    internal static TiffPhotoshopReadResult ReadPhotoshop(TiffDirectory directory, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits)
    {
        TiffMetadataSourceSnapshot snapshot = TiffMetadataSourceSnapshot.Create(directory, source, byteOrder, limits, new int[] { TiffTagId.Photoshop });
        TiffPhotoshopReadResult result = TiffPhotoshopMetadataReader.Read(snapshot.Directory, snapshot.Bytes, limits);
        snapshot.Diagnostics.Merge(result.Diagnostics);
        return TiffPhotoshopReadResult.Create(result.Metadata, snapshot.Diagnostics);
    }

    internal static TiffIptcReadResult ReadIptc(TiffDirectory directory, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits)
    {
        TiffMetadataSourceSnapshot snapshot = TiffMetadataSourceSnapshot.Create(directory, source, byteOrder, limits, new int[] { TiffTagId.Iptc });
        TiffIptcReadResult result = TiffIptcMetadataReader.Read(snapshot.Directory, snapshot.Bytes, limits);
        snapshot.Diagnostics.Merge(result.Diagnostics);
        return TiffIptcReadResult.Create(result.Metadata, snapshot.Diagnostics);
    }

    internal static TiffImageDescriptionReadResult ReadImageDescription(TiffDirectory directory, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits)
    {
        TiffMetadataSourceSnapshot snapshot = TiffMetadataSourceSnapshot.Create(directory, source, byteOrder, limits, new int[] { TiffTagId.ImageDescription });
        TiffImageDescriptionReadResult result = TiffImageDescriptionReader.Read(snapshot.Directory, snapshot.Bytes, limits);
        snapshot.Diagnostics.Merge(result.Diagnostics);
        return TiffImageDescriptionReadResult.Create(result.Metadata, snapshot.Diagnostics);
    }

    internal static TiffOmeMetadataReadResult ReadOmeMetadata(TiffDirectory directory, ITiffRandomAccessSource source, TiffLimits limits)
    {
        return TiffOmeMetadataReader.ReadSource(directory, source, limits);
    }

    internal static TiffImageJMetadataReadResult ReadImageJMetadata(TiffDirectory directory, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits)
    {
        int[] tags = new int[] { TiffTagId.ImageDescription, TiffTagId.XResolution, TiffTagId.YResolution, TiffTagId.ResolutionUnit, TiffTagId.ImageJMetadataByteCounts, TiffTagId.ImageJMetadata };
        TiffMetadataSourceSnapshot snapshot = TiffMetadataSourceSnapshot.Create(directory, source, byteOrder, limits, tags);
        TiffImageJMetadataReadResult result = TiffImageJMetadataReader.Read(snapshot.Directory, snapshot.Bytes);
        snapshot.Diagnostics.Merge(result.Diagnostics);
        return TiffImageJMetadataReadResult.Create(result.Metadata, snapshot.Diagnostics);
    }

    internal static TiffGdalMetadataReadResult ReadGdalMetadata(TiffDirectory directory, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits)
    {
        int[] tags = new int[] { TiffTagId.GdalMetadata, TiffTagId.GdalNoData };
        TiffMetadataSourceSnapshot snapshot = TiffMetadataSourceSnapshot.Create(directory, source, byteOrder, limits, tags);
        TiffGdalMetadataReadResult result = TiffGdalMetadataReader.Read(snapshot.Directory, snapshot.Bytes, limits);
        snapshot.Diagnostics.Merge(result.Diagnostics);
        return TiffGdalMetadataReadResult.Create(result.Metadata, snapshot.Diagnostics);
    }

    internal static TiffGeoKeyDirectoryReadResult ReadGeoKeyDirectory(TiffDirectory directory, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits)
    {
        int[] tags = new int[] { TiffTagId.GeoKeyDirectory, TiffTagId.GeoDoubleParams, TiffTagId.GeoAsciiParams, TiffTagId.ModelPixelScale, TiffTagId.ModelTiepoint, TiffTagId.ModelTransformation };
        TiffMetadataSourceSnapshot snapshot = TiffMetadataSourceSnapshot.Create(directory, source, byteOrder, limits, tags);
        TiffGeoKeyDirectoryReadResult result = TiffGeoKeyDirectoryReader.Read(snapshot.Directory, snapshot.Bytes, byteOrder, limits);
        snapshot.Diagnostics.Merge(result.Diagnostics);
        return TiffGeoKeyDirectoryReadResult.Create(result.Directory, snapshot.Diagnostics);
    }

    internal static TiffGeoModelReadResult ReadGeoModel(TiffDirectory directory, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits)
    {
        int[] tags = new int[] { TiffTagId.ModelPixelScale, TiffTagId.ModelTiepoint, TiffTagId.ModelTransformation };
        TiffMetadataSourceSnapshot snapshot = TiffMetadataSourceSnapshot.Create(directory, source, byteOrder, limits, tags);
        TiffGeoModelReadResult result = TiffGeoModelMetadataReader.Read(snapshot.Directory, snapshot.Bytes, byteOrder, limits);
        snapshot.Diagnostics.Merge(result.Diagnostics);
        return TiffGeoModelReadResult.Create(result.Metadata, snapshot.Diagnostics);
    }

    internal static TiffGeoParameterReadResult ReadGeoParameters(TiffDirectory directory, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits)
    {
        int[] tags = new int[] { TiffTagId.GeoDoubleParams, TiffTagId.GeoAsciiParams };
        TiffMetadataSourceSnapshot snapshot = TiffMetadataSourceSnapshot.Create(directory, source, byteOrder, limits, tags);
        TiffGeoParameterReadResult result = TiffGeoParameterReader.Read(snapshot.Directory, snapshot.Bytes, byteOrder, limits);
        snapshot.Diagnostics.Merge(result.Diagnostics);
        return TiffGeoParameterReadResult.Create(result.Metadata, snapshot.Diagnostics);
    }

    internal static TiffVendorMetadataReadResult ReadVendorMetadata(TiffDirectory directory, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits)
    {
        int[] tags = new int[directory.Entries.Length];
        TiffTagRegistry registry = TiffTagRegistry.CreateDefault();
        int index = 0;
        int tagCount = 0;
        while (index < directory.Entries.Length)
        {
            int tagId = directory.Entries[index].TagId;
            bool isRecognitionText = tagId == TiffTagId.ImageDescription || tagId == TiffTagId.Xmp || tagId == TiffTagId.Make || tagId == TiffTagId.Model || tagId == TiffTagId.Software || tagId == TiffTagId.DocumentName || tagId == TiffTagId.PageName;
            bool isPrivate = tagId == TiffTagId.CzLsmInfo || tagId >= 32768 && !registry.Contains(tagId);
            if (isRecognitionText || isPrivate || TiffVendorMetadataReader.IsDngTag(tagId))
            {
                tags[tagCount] = tagId;
                tagCount = tagCount + 1;
            }

            index = index + 1;
        }

        TiffMetadataSourceSnapshot snapshot = TiffMetadataSourceSnapshot.Create(directory, source, byteOrder, limits, tags);
        TiffVendorMetadataReadResult result = TiffVendorMetadataReader.ReadWithByteOrder(snapshot.Directory, snapshot.Bytes, byteOrder, limits);
        snapshot.Diagnostics.Merge(result.Diagnostics);
        return TiffVendorMetadataReadResult.Create(result.Metadata, snapshot.Diagnostics);
    }

    internal static TiffTagValue[] ReadDirectoryValues(TiffDirectory directory, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        TiffTagValue[] values = new TiffTagValue[0];
        int index = 0;
        while (index < directory.Entries.Length)
        {
            TiffTagValue value = TiffExifMetadataReader.ReadSourceValue(directory.Entries[index], source, byteOrder, limits, diagnostics);
            if (diagnostics.HasErrors())
            {
                return new TiffTagValue[0];
            }

            values = TiffExifMetadataReader.AppendValue(values, value);
            index = index + 1;
        }

        return values;
    }
}
