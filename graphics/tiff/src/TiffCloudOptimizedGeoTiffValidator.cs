using System;
using System.IO;

namespace tiff;

/// <summary>Validates the structural ordering and geospatial requirements used by Cloud Optimized GeoTIFF files.</summary>
internal static class TiffCloudOptimizedGeoTiffValidator
{
    internal const ulong ReducedResolutionMask = 1;

    /// <summary>Parses and validates the complete TIFF source, returning every detected structural problem as diagnostics.</summary>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffCloudOptimizedGeoTiffValidationResult Validate(byte[] fileBytes)
    {
        return Validate(fileBytes, new TiffReadOptions());
    }

    /// <summary>Parses and validates the complete TIFF source, returning every detected structural problem as diagnostics.</summary>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <param name="options">The read or write policy; a null value selects defaults.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffCloudOptimizedGeoTiffValidationResult Validate(byte[] fileBytes, TiffReadOptions options)
    {
        if (fileBytes == null)
        {
            return Validate(ReadOnlySpan<byte>.Empty, options);
        }

        return Validate(fileBytes.AsSpan(), options);
    }

    /// <summary>Parses and validates the complete TIFF source, returning every detected structural problem as diagnostics.</summary>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffCloudOptimizedGeoTiffValidationResult Validate(ReadOnlySpan<byte> fileBytes)
    {
        return Validate(fileBytes, new TiffReadOptions());
    }

    /// <summary>Parses and validates the complete TIFF source, returning every detected structural problem as diagnostics.</summary>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <param name="options">The read or write policy; a null value selects defaults.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffCloudOptimizedGeoTiffValidationResult Validate(ReadOnlySpan<byte> fileBytes, TiffReadOptions options)
    {
        TiffFile file = TiffFile.OpenMetadata(fileBytes, options);
        return ValidateAndClose(file);
    }

    /// <summary>Parses and validates the complete TIFF source, returning every detected structural problem as diagnostics.</summary>
    /// <param name="path">The TIFF file path.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffCloudOptimizedGeoTiffValidationResult Validate(string path)
    {
        return Validate(path, new TiffReadOptions());
    }

    /// <summary>Parses and validates the complete TIFF source, returning every detected structural problem as diagnostics.</summary>
    /// <param name="path">The TIFF file path.</param>
    /// <param name="options">The read or write policy; a null value selects defaults.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffCloudOptimizedGeoTiffValidationResult Validate(string path, TiffReadOptions options)
    {
        TiffFile file = TiffFile.OpenMetadata(path, options);
        return ValidateAndClose(file);
    }

    /// <summary>Parses and validates the complete TIFF source, returning every detected structural problem as diagnostics.</summary>
    /// <param name="stream">The readable or writable stream positioned at the TIFF data.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffCloudOptimizedGeoTiffValidationResult Validate(Stream stream)
    {
        return Validate(stream, new TiffReadOptions());
    }

    /// <summary>Parses and validates the complete TIFF source, returning every detected structural problem as diagnostics.</summary>
    /// <param name="stream">The readable or writable stream positioned at the TIFF data.</param>
    /// <param name="options">The read or write policy; a null value selects defaults.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffCloudOptimizedGeoTiffValidationResult Validate(Stream stream, TiffReadOptions options)
    {
        TiffFile file = TiffFile.OpenMetadata(stream, options);
        return ValidateAndClose(file);
    }

    internal static TiffCloudOptimizedGeoTiffValidationResult ValidateAndClose(TiffFile file)
    {
        TiffCloudOptimizedGeoTiffValidationResult result = Validate(file);
        if (file != null)
        {
            file.Close();
        }

        return result;
    }

    /// <summary>Parses and validates the complete TIFF source, returning every detected structural problem as diagnostics.</summary>
    /// <param name="file">The file.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffCloudOptimizedGeoTiffValidationResult Validate(TiffFile file)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (file == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A TIFF file handle is required for Cloud Optimized GeoTIFF validation.");
            return TiffCloudOptimizedGeoTiffValidationResult.Create(new TiffHeader(), 0, 0, 0, TiffValidationMode.Strict, diagnostics);
        }

        diagnostics.Merge(file.GetDiagnostics());
        TiffHeader header = file.GetHeader();
        TiffCloudOptimizedDirectorySet set = ReadSourceDirectorySet(file, diagnostics);
        TiffCloudOptimizedTileRange[] ranges = ValidateSourceDirectories(header, set, file.Source, file.Options, diagnostics);
        int fullResolutionCount = CountFullResolutionDirectories(set);
        int overviewCount = set.Directories.Length - fullResolutionCount;
        int internalOverviewCount = CountInternalOverviewDirectories(set);
        TiffCloudOptimizedGeoTiffValidationResult result = TiffCloudOptimizedGeoTiffValidationResult.Create(header, set.Directories.Length, fullResolutionCount, overviewCount, TiffValidationPolicy.GetMode(file.Options), diagnostics);
        result.InternalOverviewImageCount = internalOverviewCount;
        result.HasRangeFriendlyLayout = !diagnostics.HasErrors();
        ApplyOverallTileRange(result, ranges);
        return result;
    }

    internal static TiffCloudOptimizedDirectorySet ReadSourceDirectorySet(TiffFile file, TiffDiagnostics diagnostics)
    {
        TiffCloudOptimizedDirectorySet set = new TiffCloudOptimizedDirectorySet();
        if (file == null || !file.GetIsOpen())
        {
            return set;
        }

        TiffHeader header = file.GetHeader();
        TiffDirectory[] topLevelDirectories = file.GetDirectories();
        int parentBaseIndex = -1;
        int index = 0;
        while (index < topLevelDirectories.Length)
        {
            TiffDirectory directory = topLevelDirectories[index];
            bool reduced = IsReducedResolutionDirectorySource(directory, file.Source, header.ByteOrder, file.Options, diagnostics);
            TiffImageInfoResult infoResult = TiffSourceImageInfoExtractor.Extract(file.Source, directory, header.ByteOrder, file.Options);
            diagnostics.Merge(infoResult.Diagnostics);
            if (!reduced)
            {
                parentBaseIndex = set.Directories.Length;
            }

            set.Append(directory, infoResult.Info, reduced, false, parentBaseIndex);
            int baseIndexForChildren = parentBaseIndex;
            TiffSubIfdReadResult subIfdResult = TiffSourceSubIfdReader.Read(directory, file.Source, header.ByteOrder, header.FileKind, file.Options);
            diagnostics.Merge(subIfdResult.Diagnostics);
            AppendInternalDirectories(set, subIfdResult.SubIfds, baseIndexForChildren, file.Source, header.ByteOrder, file.Options, diagnostics);
            index = index + 1;
        }

        return set;
    }

    internal static void AppendInternalDirectories(TiffCloudOptimizedDirectorySet set, TiffSubIfd[] subIfds, int parentBaseIndex, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffReadOptions options, TiffDiagnostics diagnostics)
    {
        int subIfdIndex = 0;
        while (subIfds != null && subIfdIndex < subIfds.Length)
        {
            int directoryIndex = 0;
            while (directoryIndex < subIfds[subIfdIndex].Directories.Length)
            {
                TiffDirectory directory = subIfds[subIfdIndex].Directories[directoryIndex];
                directory.ImageIndex = set.Directories.Length;
                bool reduced = IsReducedResolutionDirectorySource(directory, source, byteOrder, options, diagnostics);
                TiffImageInfoResult infoResult = TiffSourceImageInfoExtractor.Extract(source, directory, byteOrder, options);
                diagnostics.Merge(infoResult.Diagnostics);
                set.Append(directory, infoResult.Info, reduced, true, parentBaseIndex);
                directoryIndex = directoryIndex + 1;
            }

            subIfdIndex = subIfdIndex + 1;
        }
    }

    internal static TiffCloudOptimizedTileRange[] ValidateSourceDirectories(TiffHeader header, TiffCloudOptimizedDirectorySet set, ITiffRandomAccessSource source, TiffReadOptions options, TiffDiagnostics diagnostics)
    {
        int count = set.Directories.Length;
        TiffCloudOptimizedTileRange[] ranges = new TiffCloudOptimizedTileRange[count];
        if (count == 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Cloud Optimized GeoTIFF validation requires at least one image directory.");
            return ranges;
        }

        ValidateDirectoryOffsetOrder(set.Directories, diagnostics);
        ulong metadataEnd = 0;
        int index = 0;
        while (index < count)
        {
            ranges[index] = new TiffCloudOptimizedTileRange();
            TiffDirectory directory = set.Directories[index];
            TiffImageInfo info = set.Infos[index];
            ValidateDirectoryContainerRules(header, directory, diagnostics);
            ulong directoryMetadataEnd = CalculateDirectoryMetadataEnd(header, directory, diagnostics);
            if (directoryMetadataEnd > metadataEnd)
            {
                metadataEnd = directoryMetadataEnd;
            }

            if (!info.IsTiled)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Cloud Optimized GeoTIFF image directories must use tiled storage.", directory.ImageIndex, TiffTagId.TileOffsets);
            }
            else
            {
                ranges[index] = ReadAndValidateTileRange(header, directory, info, source, options, diagnostics);
            }

            if (set.Reduced[index])
            {
                ValidateSourceOverviewOrder(set, index, diagnostics);
            }
            else
            {
                ValidateFullResolutionGeoTiffTagsSource(header, directory, source, options, diagnostics);
            }

            if (set.Internal[index] && !set.Reduced[index])
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Cloud Optimized GeoTIFF SubIFDs must be reduced-resolution overviews.", directory.ImageIndex, TiffTagId.NewSubfileType);
            }

            index = index + 1;
        }

        ValidateMetadataBeforeTileData(metadataEnd, ranges, diagnostics);
        ValidateRangeFriendlyOverviewDataOrder(set, ranges, diagnostics);
        if (CountFullResolutionDirectories(set) == 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Cloud Optimized GeoTIFF validation requires at least one full-resolution image directory.");
        }

        return ranges;
    }

    internal static TiffImageInfo[] ReadImageInfos(TiffHeader header, TiffDirectory[] directories, byte[] fileBytes, TiffReadOptions options, TiffDiagnostics diagnostics)
    {
        TiffImageInfo[] infos = new TiffImageInfo[directories.Length];
        int index = 0;
        while (index < directories.Length)
        {
            TiffImageInfoResult result = TiffImageInfoExtractor.Extract(directories[index], fileBytes, header.ByteOrder, options);
            diagnostics.Merge(result.Diagnostics);
            infos[index] = result.Info.Copy();
            index = index + 1;
        }

        return infos;
    }

    internal static void ValidateDirectoryContainerRules(TiffHeader header, TiffDirectory directory, TiffDiagnostics diagnostics)
    {
        ulong alignment = 2;
        if (header.FileKind == TiffFileKind.BigTiff)
        {
            alignment = 8;
        }

        if (directory.Offset % alignment != 0)
        {
            diagnostics.AddErrorAt(TiffErrorCode.InvalidOffset, "Cloud Optimized GeoTIFF image-directory offsets must follow container alignment rules.", TiffDirectoryReader.ToDiagnosticOffset(directory.Offset));
        }

        int index = 0;
        while (index < directory.Entries.Length)
        {
            TiffIfdEntry entry = directory.Entries[index];
            if (entry.HasExternalValue() && entry.ValueOffset % alignment != 0)
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidOffset, "Cloud Optimized GeoTIFF external tag values must follow container alignment rules.", directory.ImageIndex, entry.TagId);
            }

            index = index + 1;
        }
    }

    internal static ulong CalculateDirectoryMetadataEnd(TiffHeader header, TiffDirectory directory, TiffDiagnostics diagnostics)
    {
        ulong fixedByteCount = 6;
        ulong entryByteCount = 12;
        if (header.FileKind == TiffFileKind.BigTiff)
        {
            fixedByteCount = 16;
            entryByteCount = 20;
        }

        if ((ulong)directory.Entries.Length > (ulong.MaxValue - fixedByteCount) / entryByteCount)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Cloud Optimized GeoTIFF directory byte count overflows the supported range.");
            return ulong.MaxValue;
        }

        ulong directoryByteCount = fixedByteCount + (ulong)directory.Entries.Length * entryByteCount;
        ulong metadataEnd = AddRangeEnd(directory.Offset, directoryByteCount, diagnostics);
        int index = 0;
        while (index < directory.Entries.Length)
        {
            TiffIfdEntry entry = directory.Entries[index];
            if (entry.HasExternalValue() && entry.ValueByteCount >= 0)
            {
                ulong end = AddRangeEnd(entry.ValueOffset, (ulong)entry.ValueByteCount, diagnostics);
                if (end > metadataEnd)
                {
                    metadataEnd = end;
                }
            }

            index = index + 1;
        }

        return metadataEnd;
    }

    internal static ulong AddRangeEnd(ulong offset, ulong byteCount, TiffDiagnostics diagnostics)
    {
        if (offset > ulong.MaxValue - byteCount)
        {
            diagnostics.AddError(TiffErrorCode.IntegerOverflow, "Cloud Optimized GeoTIFF byte range end overflows the supported range.");
            return ulong.MaxValue;
        }

        return offset + byteCount;
    }

    internal static TiffCloudOptimizedTileRange ReadAndValidateTileRange(TiffHeader header, TiffDirectory directory, TiffImageInfo info, ITiffRandomAccessSource source, TiffReadOptions options, TiffDiagnostics diagnostics)
    {
        TiffCloudOptimizedTileRange range = new TiffCloudOptimizedTileRange();
        TiffIfdEntry? offsetsEntry = directory.FindEntry(TiffTagId.TileOffsets);
        TiffIfdEntry? byteCountsEntry = directory.FindEntry(TiffTagId.TileByteCounts);
        if (offsetsEntry == null || byteCountsEntry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "Cloud Optimized GeoTIFF tiled directories must include TileOffsets and TileByteCounts.", directory.ImageIndex, TiffTagId.TileOffsets);
            return range;
        }

        ValidateTileOffsetTypes(header, directory, offsetsEntry, byteCountsEntry, diagnostics);
        TiffLimits limits = TiffDirectoryReader.GetLimits(options);
        TiffDiagnostics readDiagnostics = new TiffDiagnostics();
        ulong[] offsets = TiffSourceImageInfoExtractor.ReadUnsignedArray(source, offsetsEntry, header.ByteOrder, limits, directory.ImageIndex, readDiagnostics);
        ulong[] byteCounts = TiffSourceImageInfoExtractor.ReadUnsignedArray(source, byteCountsEntry, header.ByteOrder, limits, directory.ImageIndex, readDiagnostics);
        diagnostics.Merge(readDiagnostics);
        if (readDiagnostics.HasErrors())
        {
            return range;
        }

        ValidateTileOffsetByteCounts(directory, offsets, byteCounts, diagnostics);
        TiffDiagnostics countDiagnostics = new TiffDiagnostics();
        ulong expectedCount = TiffStrileLayout.CalculateTileCount(info, countDiagnostics);
        diagnostics.Merge(countDiagnostics);
        if (expectedCount != (ulong)offsets.Length)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "Cloud Optimized GeoTIFF tile arrays must match the calculated tile count.", directory.ImageIndex, TiffTagId.TileOffsets);
        }

        long sourceLength = source.GetLength();
        int index = 0;
        while (index < offsets.Length && index < byteCounts.Length)
        {
            ulong end = AddRangeEnd(offsets[index], byteCounts[index], diagnostics);
            if (byteCounts[index] > 0)
            {
                if (sourceLength < 0 || offsets[index] > long.MaxValue || end > (ulong)sourceLength)
                {
                    diagnostics.AddErrorForTag(TiffErrorCode.InvalidByteRange, "Cloud Optimized GeoTIFF tile range is outside the source.", directory.ImageIndex, TiffTagId.TileOffsets);
                }

                if (!range.HasData)
                {
                    range.HasData = true;
                    range.FirstOffset = offsets[index];
                }

                range.EndOffset = end;
            }

            index = index + 1;
        }

        return range;
    }

    internal static void ValidateTileOffsetTypes(TiffHeader header, TiffDirectory directory, TiffIfdEntry offsetsEntry, TiffIfdEntry byteCountsEntry, TiffDiagnostics diagnostics)
    {
        if (!IsOffsetArrayType(offsetsEntry.DataType) || !IsOffsetArrayType(byteCountsEntry.DataType))
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "Cloud Optimized GeoTIFF tile offset and byte-count arrays require unsigned integer types.", directory.ImageIndex, TiffTagId.TileOffsets);
            return;
        }

        if (header.FileKind == TiffFileKind.Classic && (offsetsEntry.DataType == TiffDataType.Long8 || byteCountsEntry.DataType == TiffDataType.Long8))
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "Classic Cloud Optimized GeoTIFF tile arrays cannot use LONG8 values.", directory.ImageIndex, TiffTagId.TileOffsets);
        }
    }

    internal static bool IsOffsetArrayType(TiffDataType dataType)
    {
        return dataType == TiffDataType.Short || dataType == TiffDataType.Long || dataType == TiffDataType.Long8;
    }

    internal static void ValidateSourceOverviewOrder(TiffCloudOptimizedDirectorySet set, int index, TiffDiagnostics diagnostics)
    {
        int parentIndex = set.ParentBaseIndices[index];
        if (parentIndex < 0 || parentIndex >= index || set.Reduced[parentIndex])
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Cloud Optimized GeoTIFF overview directories must follow a full-resolution image directory.");
            return;
        }

        int previousIndex = parentIndex;
        int candidate = parentIndex + 1;
        while (candidate < index)
        {
            if (set.Reduced[candidate] && set.ParentBaseIndices[candidate] == parentIndex)
            {
                previousIndex = candidate;
            }

            candidate = candidate + 1;
        }

        TiffImageInfo info = set.Infos[index];
        TiffImageInfo previousInfo = set.Infos[previousIndex];
        if (info.Width >= previousInfo.Width || info.Length >= previousInfo.Length)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Cloud Optimized GeoTIFF overview dimensions must decrease from the previous level.", set.Directories[index].ImageIndex, TiffTagId.NewSubfileType);
        }
    }

    internal static void ValidateFullResolutionGeoTiffTagsSource(TiffHeader header, TiffDirectory directory, ITiffRandomAccessSource source, TiffReadOptions options, TiffDiagnostics diagnostics)
    {
        TiffLimits limits = TiffDirectoryReader.GetLimits(options);
        if (directory.FindEntry(TiffTagId.GeoKeyDirectory) == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "Cloud Optimized GeoTIFF full-resolution directories must include GeoKeyDirectoryTag.", directory.ImageIndex, TiffTagId.GeoKeyDirectory);
        }
        else
        {
            TiffGeoKeyDirectoryReadResult keyResult = TiffSourceMetadataReader.ReadGeoKeyDirectory(directory, source, header.ByteOrder, limits);
            diagnostics.Merge(keyResult.Diagnostics);
        }

        TiffGeoModelReadResult modelResult = TiffSourceMetadataReader.ReadGeoModel(directory, source, header.ByteOrder, limits);
        diagnostics.Merge(modelResult.Diagnostics);
        if (!HasUsableModelGeoreference(modelResult.Metadata))
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "Cloud Optimized GeoTIFF full-resolution directories must include model georeference tags.", directory.ImageIndex, TiffTagId.ModelTiepoint);
        }
    }

    internal static bool IsReducedResolutionDirectorySource(TiffDirectory directory, ITiffRandomAccessSource source, TiffByteOrder byteOrder, TiffReadOptions options, TiffDiagnostics diagnostics)
    {
        TiffLimits limits = TiffDirectoryReader.GetLimits(options);
        ulong newSubfileType = TiffSourceImageInfoExtractor.ReadOptionalUnsignedScalar(source, directory, byteOrder, limits, TiffTagId.NewSubfileType, 0, diagnostics);
        if ((newSubfileType & ReducedResolutionMask) != 0)
        {
            return true;
        }

        ulong subfileType = TiffSourceImageInfoExtractor.ReadOptionalUnsignedScalar(source, directory, byteOrder, limits, TiffTagId.SubfileType, 0, diagnostics);
        return subfileType == 2;
    }

    internal static void ValidateMetadataBeforeTileData(ulong metadataEnd, TiffCloudOptimizedTileRange[] ranges, TiffDiagnostics diagnostics)
    {
        int index = 0;
        while (index < ranges.Length)
        {
            if (ranges[index] != null && ranges[index].HasData && ranges[index].FirstOffset < metadataEnd)
            {
                diagnostics.AddError(TiffErrorCode.InvalidByteRange, "Cloud Optimized GeoTIFF image data must follow all image-directory metadata.");
                return;
            }

            index = index + 1;
        }
    }

    internal static void ValidateRangeFriendlyOverviewDataOrder(TiffCloudOptimizedDirectorySet set, TiffCloudOptimizedTileRange[] ranges, TiffDiagnostics diagnostics)
    {
        int index = 0;
        while (index < set.Directories.Length)
        {
            if (set.Reduced[index] && ranges[index].HasData)
            {
                int parentIndex = set.ParentBaseIndices[index];
                if (parentIndex >= 0 && ranges[parentIndex].HasData && ranges[index].EndOffset > ranges[parentIndex].FirstOffset)
                {
                    diagnostics.AddErrorForTag(TiffErrorCode.InvalidByteRange, "Cloud Optimized GeoTIFF overview tile data must precede full-resolution tile data.", set.Directories[index].ImageIndex, TiffTagId.TileOffsets);
                }

                int previousOverview = -1;
                int candidate = 0;
                while (candidate < index)
                {
                    if (set.Reduced[candidate] && set.ParentBaseIndices[candidate] == parentIndex)
                    {
                        previousOverview = candidate;
                    }

                    candidate = candidate + 1;
                }

                if (previousOverview >= 0 && ranges[previousOverview].HasData && ranges[index].EndOffset > ranges[previousOverview].FirstOffset)
                {
                    diagnostics.AddErrorForTag(TiffErrorCode.InvalidByteRange, "Cloud Optimized GeoTIFF smaller overview tile data must precede larger overview tile data.", set.Directories[index].ImageIndex, TiffTagId.TileOffsets);
                }
            }

            index = index + 1;
        }
    }

    internal static int CountFullResolutionDirectories(TiffCloudOptimizedDirectorySet set)
    {
        int count = 0;
        int index = 0;
        while (index < set.Reduced.Length)
        {
            if (!set.Reduced[index])
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal static int CountInternalOverviewDirectories(TiffCloudOptimizedDirectorySet set)
    {
        int count = 0;
        int index = 0;
        while (index < set.Reduced.Length)
        {
            if (set.Reduced[index] && set.Internal[index])
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }

    internal static void ApplyOverallTileRange(TiffCloudOptimizedGeoTiffValidationResult result, TiffCloudOptimizedTileRange[] ranges)
    {
        bool hasData = false;
        int index = 0;
        while (index < ranges.Length)
        {
            if (ranges[index] != null && ranges[index].HasData)
            {
                if (!hasData || ranges[index].FirstOffset < result.FirstTileOffset)
                {
                    result.FirstTileOffset = ranges[index].FirstOffset;
                }

                if (!hasData || ranges[index].EndOffset > result.LastTileEndOffset)
                {
                    result.LastTileEndOffset = ranges[index].EndOffset;
                }

                hasData = true;
            }

            index = index + 1;
        }
    }

    internal static void ValidateDirectories(TiffHeader header, TiffDirectory[] directories, TiffImageInfo[] infos, byte[] fileBytes, TiffReadOptions options, TiffDiagnostics diagnostics)
    {
        if (directories == null || directories.Length == 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidOffset, "Cloud Optimized GeoTIFF validation requires at least one image directory.");
            return;
        }

        ValidateDirectoryOffsetOrder(directories, diagnostics);
        int fullResolutionCount = 0;
        TiffImageInfo previousInfo = new TiffImageInfo();
        bool hasPrevious = false;
        int index = 0;
        while (index < directories.Length)
        {
            bool reduced = IsReducedResolutionDirectory(directories[index], fileBytes, header.ByteOrder, diagnostics);
            if (infos[index].IsTiled)
            {
                ValidateTileOffsetByteCounts(directories[index], fileBytes, header.ByteOrder, options, diagnostics);
            }
            else
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Cloud Optimized GeoTIFF image directories must use tiled storage.", directories[index].ImageIndex, TiffTagId.TileOffsets);
            }

            if (reduced)
            {
                ValidateOverviewOrder(directories[index], infos[index], previousInfo, hasPrevious, diagnostics);
            }
            else
            {
                fullResolutionCount = fullResolutionCount + 1;
                ValidateFullResolutionGeoTiffTags(header, directories[index], fileBytes, options, diagnostics);
            }

            previousInfo = infos[index].Copy();
            hasPrevious = true;
            index = index + 1;
        }

        if (fullResolutionCount == 0)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Cloud Optimized GeoTIFF validation requires at least one full-resolution image directory.");
        }
    }

    internal static void ValidateDirectoryOffsetOrder(TiffDirectory[] directories, TiffDiagnostics diagnostics)
    {
        int index = 1;
        while (directories != null && index < directories.Length)
        {
            if (directories[index].Offset <= directories[index - 1].Offset)
            {
                diagnostics.AddError(TiffErrorCode.InvalidOffset, "Cloud Optimized GeoTIFF image directories must be stored in increasing file-offset order.");
                return;
            }

            index = index + 1;
        }
    }

    internal static void ValidateOverviewOrder(TiffDirectory directory, TiffImageInfo info, TiffImageInfo previousInfo, bool hasPrevious, TiffDiagnostics diagnostics)
    {
        if (!hasPrevious)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, "Cloud Optimized GeoTIFF overview directories must follow a full-resolution image directory.");
            return;
        }

        if (info.Width >= previousInfo.Width || info.Length >= previousInfo.Length)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Cloud Optimized GeoTIFF overview dimensions must decrease from the previous image directory.", directory.ImageIndex, TiffTagId.NewSubfileType);
        }
    }

    internal static void ValidateFullResolutionGeoTiffTags(TiffHeader header, TiffDirectory directory, byte[] fileBytes, TiffReadOptions options, TiffDiagnostics diagnostics)
    {
        TiffLimits limits = TiffDirectoryReader.GetLimits(options);
        if (directory.FindEntry(TiffTagId.GeoKeyDirectory) == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "Cloud Optimized GeoTIFF full-resolution directories must include GeoKeyDirectoryTag.", directory.ImageIndex, TiffTagId.GeoKeyDirectory);
        }
        else
        {
            TiffGeoKeyDirectoryReadResult keyResult = TiffGeoKeyDirectoryReader.Read(directory, fileBytes, header.ByteOrder, limits);
            diagnostics.Merge(keyResult.Diagnostics);
        }

        TiffGeoModelReadResult modelResult = TiffGeoModelMetadataReader.Read(directory, fileBytes, header.ByteOrder, limits);
        diagnostics.Merge(modelResult.Diagnostics);
        if (!HasUsableModelGeoreference(modelResult.Metadata))
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "Cloud Optimized GeoTIFF full-resolution directories must include model georeference tags.", directory.ImageIndex, TiffTagId.ModelTiepoint);
        }
    }

    internal static bool HasUsableModelGeoreference(TiffGeoModelMetadata metadata)
    {
        if (metadata == null)
        {
            return false;
        }

        return (metadata.HasPixelScale && metadata.HasTiepoints) || metadata.HasTransformation;
    }

    internal static void ValidateTileOffsetByteCounts(TiffDirectory directory, byte[] fileBytes, TiffByteOrder byteOrder, TiffReadOptions options, TiffDiagnostics diagnostics)
    {
        TiffIfdEntry? offsetsEntry = directory.FindEntry(TiffTagId.TileOffsets);
        TiffIfdEntry? byteCountsEntry = directory.FindEntry(TiffTagId.TileByteCounts);
        if (offsetsEntry == null || byteCountsEntry == null)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.MissingRequiredTag, "Cloud Optimized GeoTIFF tiled directories must include TileOffsets and TileByteCounts.", directory.ImageIndex, TiffTagId.TileOffsets);
            return;
        }

        TiffDiagnostics readDiagnostics = new TiffDiagnostics();
        TiffLimits limits = TiffDirectoryReader.GetLimits(options);
        ulong[] offsets = TiffImageInfoExtractor.ReadUnsignedArray(offsetsEntry, fileBytes, byteOrder, limits, directory.ImageIndex, readDiagnostics);
        ulong[] byteCounts = TiffImageInfoExtractor.ReadUnsignedArray(byteCountsEntry, fileBytes, byteOrder, limits, directory.ImageIndex, readDiagnostics);
        diagnostics.Merge(readDiagnostics);
        if (readDiagnostics.HasErrors())
        {
            return;
        }

        ValidateTileOffsetByteCounts(directory, offsets, byteCounts, diagnostics);
    }

    internal static void ValidateTileOffsetByteCounts(TiffDirectory directory, ulong[] offsets, ulong[] byteCounts, TiffDiagnostics diagnostics)
    {
        if (offsets == null || byteCounts == null || offsets.Length != byteCounts.Length)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagCount, "Cloud Optimized GeoTIFF TileOffsets and TileByteCounts must have matching counts.", directory.ImageIndex, TiffTagId.TileOffsets);
            return;
        }

        ulong previousEnd = 0;
        bool hasPrevious = false;
        int index = 0;
        while (index < offsets.Length)
        {
            if (byteCounts[index] > 0)
            {
                if (hasPrevious && offsets[index] < previousEnd)
                {
                    diagnostics.AddErrorForTag(TiffErrorCode.InvalidByteRange, "Cloud Optimized GeoTIFF tile byte ranges must be monotonically ordered and non-overlapping.", directory.ImageIndex, TiffTagId.TileOffsets);
                    return;
                }

                if (ulong.MaxValue - offsets[index] < byteCounts[index])
                {
                    diagnostics.AddErrorForTag(TiffErrorCode.IntegerOverflow, "Cloud Optimized GeoTIFF tile byte range end offset overflows the supported range.", directory.ImageIndex, TiffTagId.TileOffsets);
                    return;
                }

                previousEnd = offsets[index] + byteCounts[index];
                hasPrevious = true;
            }

            index = index + 1;
        }
    }

    internal static bool IsReducedResolutionDirectory(TiffDirectory directory, byte[] fileBytes, TiffByteOrder byteOrder, TiffDiagnostics diagnostics)
    {
        ulong newSubfileType = TiffImageInfoExtractor.ReadOptionalUnsignedScalar(directory, fileBytes, byteOrder, new TiffLimits(), TiffTagId.NewSubfileType, 0, diagnostics);
        if ((newSubfileType & ReducedResolutionMask) != 0)
        {
            return true;
        }

        ulong subfileType = TiffImageInfoExtractor.ReadOptionalUnsignedScalar(directory, fileBytes, byteOrder, new TiffLimits(), TiffTagId.SubfileType, 0, diagnostics);
        return subfileType == 2;
    }

    internal static int CountFullResolutionDirectories(TiffDirectory[] directories, byte[] fileBytes, TiffByteOrder byteOrder, TiffDiagnostics diagnostics)
    {
        int count = 0;
        int index = 0;
        while (directories != null && index < directories.Length)
        {
            if (!IsReducedResolutionDirectory(directories[index], fileBytes, byteOrder, diagnostics))
            {
                count = count + 1;
            }

            index = index + 1;
        }

        return count;
    }
}
