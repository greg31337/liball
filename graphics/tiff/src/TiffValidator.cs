using System;
using System.IO;

namespace tiff;

/// <summary>Validates TIFF structure, directory contents, and referenced image-data ranges.</summary>
internal static class TiffValidator
{
    /// <summary>Parses and validates the complete TIFF source, returning every detected structural problem as diagnostics.</summary>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffValidationResult Validate(byte[] fileBytes)
    {
        return Validate(fileBytes, new TiffReadOptions());
    }

    /// <summary>Parses and validates the complete TIFF source, returning every detected structural problem as diagnostics.</summary>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <param name="options">The read or write policy; a null value selects defaults.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffValidationResult Validate(byte[] fileBytes, TiffReadOptions options)
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
    public static TiffValidationResult Validate(ReadOnlySpan<byte> fileBytes)
    {
        return Validate(fileBytes, new TiffReadOptions());
    }

    /// <summary>Parses and validates the complete TIFF source, returning every detected structural problem as diagnostics.</summary>
    /// <param name="fileBytes">The complete TIFF file bytes.</param>
    /// <param name="options">The read or write policy; a null value selects defaults.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffValidationResult Validate(ReadOnlySpan<byte> fileBytes, TiffReadOptions options)
    {
        TiffFile file = TiffFile.OpenMetadata(fileBytes, options);
        return ValidateAndClose(file);
    }

    /// <summary>Parses and validates the complete TIFF source, returning every detected structural problem as diagnostics.</summary>
    /// <param name="path">The TIFF file path.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffValidationResult Validate(string path)
    {
        return Validate(path, new TiffReadOptions());
    }

    /// <summary>Parses and validates the complete TIFF source, returning every detected structural problem as diagnostics.</summary>
    /// <param name="path">The TIFF file path.</param>
    /// <param name="options">The read or write policy; a null value selects defaults.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffValidationResult Validate(string path, TiffReadOptions options)
    {
        TiffFile file = TiffFile.OpenMetadata(path, options);
        return ValidateAndClose(file);
    }

    /// <summary>Parses and validates the complete TIFF source, returning every detected structural problem as diagnostics.</summary>
    /// <param name="stream">The readable or writable stream positioned at the TIFF data.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffValidationResult Validate(Stream stream)
    {
        return Validate(stream, new TiffReadOptions());
    }

    /// <summary>Parses and validates the complete TIFF source, returning every detected structural problem as diagnostics.</summary>
    /// <param name="stream">The readable or writable stream positioned at the TIFF data.</param>
    /// <param name="options">The read or write policy; a null value selects defaults.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffValidationResult Validate(Stream stream, TiffReadOptions options)
    {
        TiffFile file = TiffFile.OpenMetadata(stream, options);
        return ValidateAndClose(file);
    }

    internal static TiffValidationResult ValidateAndClose(TiffFile file)
    {
        TiffValidationResult result = Validate(file);
        if (file != null)
        {
            file.Close();
        }

        return result;
    }

    /// <summary>Parses and validates the complete TIFF source, returning every detected structural problem as diagnostics.</summary>
    /// <param name="file">The file.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffValidationResult Validate(TiffFile file)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (file == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A TIFF file handle is required for validation.");
            return TiffValidationResult.Create(new TiffHeader(), 0, TiffValidationMode.Strict, diagnostics);
        }

        diagnostics.Merge(file.GetDiagnostics());
        TiffDirectory[] directories = file.GetDirectories();
        int index = 0;
        while (index < directories.Length)
        {
            ValidateDirectory(file.GetHeader(), directories[index], file.GetFileBytes(), file.Options, diagnostics);
            ValidateSubIfdDirectories(file.GetHeader(), directories[index], file.GetFileBytes(), file.Options, diagnostics);
            index = index + 1;
        }

        return TiffValidationResult.Create(file.GetHeader(), directories.Length, TiffValidationPolicy.GetMode(file.Options), diagnostics);
    }

    internal static TiffValidationResult ValidateWithOperation(TiffFile file, TiffOperationMonitor monitor)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (file == null)
        {
            monitor.Start(0);
            diagnostics.AddError(TiffErrorCode.UnknownError, "A TIFF file handle is required for validation.");
            monitor.Complete(0);
            return TiffValidationResult.Create(new TiffHeader(), 0, TiffValidationMode.Strict, diagnostics);
        }

        diagnostics.Merge(file.GetDiagnostics());
        TiffDirectory[] directories = file.GetDirectories();
        long totalUnits = directories.Length;
        monitor.Start(totalUnits);
        int index = 0;
        while (index < directories.Length)
        {
            monitor.CheckCancellation();
            ValidateDirectory(file.GetHeader(), directories[index], file.GetFileBytes(), file.Options, diagnostics);
            ValidateSubIfdDirectoriesWithOperation(file.GetHeader(), directories[index], file.GetFileBytes(), file.Options, diagnostics, monitor);
            index = index + 1;
            if (index < directories.Length)
            {
                monitor.Advance(index, totalUnits);
            }
        }

        monitor.Complete(totalUnits);
        return TiffValidationResult.Create(file.GetHeader(), directories.Length, TiffValidationPolicy.GetMode(file.Options), diagnostics);
    }

    internal static void ValidateSubIfdDirectoriesWithOperation(TiffHeader header, TiffDirectory directory, byte[] fileBytes, TiffReadOptions options, TiffDiagnostics diagnostics, TiffOperationMonitor monitor)
    {
        monitor.CheckCancellation();
        TiffSubIfdReadResult subIfdResult = TiffSubIfdReader.Read(directory, fileBytes, header.ByteOrder, header.FileKind, options);
        diagnostics.Merge(subIfdResult.Diagnostics);
        int subIfdIndex = 0;
        while (subIfdIndex < subIfdResult.SubIfds.Length)
        {
            int directoryIndex = 0;
            while (directoryIndex < subIfdResult.SubIfds[subIfdIndex].Directories.Length)
            {
                monitor.CheckCancellation();
                TiffDirectory subDirectory = subIfdResult.SubIfds[subIfdIndex].Directories[directoryIndex];
                ValidateDirectory(header, subDirectory, fileBytes, options, diagnostics);
                ValidateSubIfdDirectoriesWithOperation(header, subDirectory, fileBytes, options, diagnostics, monitor);
                directoryIndex = directoryIndex + 1;
            }

            subIfdIndex = subIfdIndex + 1;
        }
    }

    internal static void ValidateSubIfdDirectories(TiffHeader header, TiffDirectory directory, byte[] fileBytes, TiffReadOptions options, TiffDiagnostics diagnostics)
    {
        TiffSubIfdReadResult subIfdResult = TiffSubIfdReader.Read(directory, fileBytes, header.ByteOrder, header.FileKind, options);
        diagnostics.Merge(subIfdResult.Diagnostics);
        int subIfdIndex = 0;
        while (subIfdIndex < subIfdResult.SubIfds.Length)
        {
            int directoryIndex = 0;
            while (directoryIndex < subIfdResult.SubIfds[subIfdIndex].Directories.Length)
            {
                TiffDirectory subDirectory = subIfdResult.SubIfds[subIfdIndex].Directories[directoryIndex];
                ValidateDirectory(header, subDirectory, fileBytes, options, diagnostics);
                ValidateSubIfdDirectories(header, subDirectory, fileBytes, options, diagnostics);
                directoryIndex = directoryIndex + 1;
            }

            subIfdIndex = subIfdIndex + 1;
        }
    }

    internal static void ValidateDirectory(TiffHeader header, TiffDirectory directory, byte[] fileBytes, TiffReadOptions options, TiffDiagnostics diagnostics)
    {
        TiffReadOptions actualOptions = options;
        if (actualOptions == null)
        {
            actualOptions = new TiffReadOptions();
        }

        TiffDiagnostics directoryDiagnostics = new TiffDiagnostics();
        TiffDuplicateTagResult duplicateResult = TiffDuplicateTagDetector.DetectEntries(directory.Entries);
        int duplicateIndex = 0;
        while (duplicateIndex < duplicateResult.DuplicateTagIds.Length)
        {
            TiffValidationPolicy.AddConformanceIssue(directoryDiagnostics, actualOptions, TiffErrorCode.DuplicateTag, "IFD contains a duplicate tag.", ToDiagnosticOffset(directory.Offset), directory.ImageIndex, duplicateResult.DuplicateTagIds[duplicateIndex]);
            duplicateIndex = duplicateIndex + 1;
        }

        ValidateRegisteredTagDefinitions(directory, actualOptions, directoryDiagnostics);

        TiffRequiredTagValidationResult requiredResult = TiffRequiredTagValidator.ValidateEntriesForImage(directory.Entries, directory.ImageIndex);
        directoryDiagnostics.Merge(requiredResult.Diagnostics);
        if (!requiredResult.Success)
        {
            MergeDirectoryDiagnostics(diagnostics, directoryDiagnostics, directory);
            return;
        }

        TiffImageInfoResult infoResult = TiffImageInfoExtractor.Extract(directory, fileBytes, header.ByteOrder, actualOptions);
        directoryDiagnostics.Merge(infoResult.Diagnostics);
        if (!infoResult.Success)
        {
            MergeDirectoryDiagnostics(diagnostics, directoryDiagnostics, directory);
            return;
        }

        TiffDiagnostics storageDiagnostics = new TiffDiagnostics();
        if (infoResult.Info.IsTiled)
        {
            ValidateTileRanges(directory, infoResult.Info, fileBytes, header.ByteOrder, actualOptions, storageDiagnostics);
        }
        else
        {
            ValidateStripRanges(directory, infoResult.Info, fileBytes, header.ByteOrder, actualOptions, storageDiagnostics);
        }

        directoryDiagnostics.Merge(storageDiagnostics);
        bool canDecode = ValidateCompressionSupport(infoResult.Info, actualOptions, directoryDiagnostics);
        ValidatePhotometricSupport(infoResult.Info, actualOptions, directoryDiagnostics);
        ValidatePhotometricShape(infoResult.Info, actualOptions, directoryDiagnostics);
        if (canDecode && !storageDiagnostics.HasErrors())
        {
            ValidateDecodedSegments(directory, infoResult.Info, fileBytes, header.ByteOrder, actualOptions, directoryDiagnostics);
        }

        MergeDirectoryDiagnostics(diagnostics, directoryDiagnostics, directory);
    }

    internal static void ValidateRegisteredTagDefinitions(TiffDirectory directory, TiffReadOptions options, TiffDiagnostics diagnostics)
    {
        int index = 0;
        while (index < directory.Entries.Length)
        {
            TiffIfdEntry entry = directory.Entries[index];
            TiffTagDefinition? definition = options.FindTagDefinition(entry.TagId);
            if (definition != null)
            {
                if (!definition.AllowsDataType(entry.DataType))
                {
                    TiffValidationPolicy.AddConformanceIssue(diagnostics, options, TiffErrorCode.InvalidTagType, "IFD tag data type is not allowed by its registered definition.", ToDiagnosticOffset(directory.Offset), directory.ImageIndex, entry.TagId);
                }

                if (!definition.AllowsCount(entry.Count))
                {
                    TiffValidationPolicy.AddConformanceIssue(diagnostics, options, TiffErrorCode.InvalidTagCount, "IFD tag value count is not allowed by its registered definition.", ToDiagnosticOffset(directory.Offset), directory.ImageIndex, entry.TagId);
                }
            }

            index = index + 1;
        }
    }

    internal static void ValidateStripRanges(TiffDirectory directory, TiffImageInfo info, byte[] fileBytes, TiffByteOrder byteOrder, TiffReadOptions options, TiffDiagnostics diagnostics)
    {
        TiffIfdEntry? offsetsEntry = directory.FindEntry(TiffTagId.StripOffsets);
        TiffIfdEntry? byteCountsEntry = directory.FindEntry(TiffTagId.StripByteCounts);
        if (offsetsEntry == null || byteCountsEntry == null)
        {
            return;
        }

        TiffLimits limits = TiffDirectoryReader.GetLimits(options);
        ulong[] offsets = TiffImageInfoExtractor.ReadUnsignedArray(offsetsEntry, fileBytes, byteOrder, limits, directory.ImageIndex, diagnostics);
        ulong[] byteCounts = TiffImageInfoExtractor.ReadUnsignedArray(byteCountsEntry, fileBytes, byteOrder, limits, directory.ImageIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return;
        }

        TiffByteRange[] ranges = TiffStrileLayout.CalculateStripByteRanges(info, offsets, byteCounts, diagnostics);
        diagnostics.FillMissingLocation(ToDiagnosticOffset(directory.Offset), directory.ImageIndex, TiffTagId.StripOffsets);
        ValidateRanges(ranges, fileBytes.Length, directory.ImageIndex, TiffTagId.StripOffsets, diagnostics);
    }

    internal static void ValidateTileRanges(TiffDirectory directory, TiffImageInfo info, byte[] fileBytes, TiffByteOrder byteOrder, TiffReadOptions options, TiffDiagnostics diagnostics)
    {
        TiffIfdEntry? offsetsEntry = directory.FindEntry(TiffTagId.TileOffsets);
        TiffIfdEntry? byteCountsEntry = directory.FindEntry(TiffTagId.TileByteCounts);
        if (offsetsEntry == null || byteCountsEntry == null)
        {
            return;
        }

        TiffLimits limits = TiffDirectoryReader.GetLimits(options);
        ulong[] offsets = TiffImageInfoExtractor.ReadUnsignedArray(offsetsEntry, fileBytes, byteOrder, limits, directory.ImageIndex, diagnostics);
        ulong[] byteCounts = TiffImageInfoExtractor.ReadUnsignedArray(byteCountsEntry, fileBytes, byteOrder, limits, directory.ImageIndex, diagnostics);
        if (diagnostics.HasErrors())
        {
            return;
        }

        TiffByteRange[] ranges = TiffStrileLayout.CalculateTileByteRanges(info, offsets, byteCounts, diagnostics);
        diagnostics.FillMissingLocation(ToDiagnosticOffset(directory.Offset), directory.ImageIndex, TiffTagId.TileOffsets);
        ValidateRanges(ranges, fileBytes.Length, directory.ImageIndex, TiffTagId.TileOffsets, diagnostics);
    }

    internal static void ValidateRanges(TiffByteRange[] ranges, int fileLength, int imageIndex, int tagId, TiffDiagnostics diagnostics)
    {
        int index = 0;
        while (ranges != null && index < ranges.Length)
        {
            ValidateRangeInFile(ranges[index], fileLength, imageIndex, tagId, diagnostics);
            index = index + 1;
        }

        index = 0;
        while (ranges != null && index < ranges.Length)
        {
            int otherIndex = index + 1;
            while (otherIndex < ranges.Length)
            {
                if (RangesOverlap(ranges[index], ranges[otherIndex]))
                {
                    TiffValidationPolicy.AddFatalIssue(diagnostics, TiffErrorCode.OverlappingDataBlock, "TIFF image data byte ranges overlap.", ToDiagnosticOffset(ranges[otherIndex].Offset), imageIndex, tagId);
                    TiffValidationPolicy.AddFatalIssue(diagnostics, TiffErrorCode.InvalidByteRange, "TIFF image data byte ranges overlap.", ToDiagnosticOffset(ranges[otherIndex].Offset), imageIndex, tagId);
                    return;
                }

                otherIndex = otherIndex + 1;
            }

            index = index + 1;
        }
    }

    internal static void ValidateRangeInFile(TiffByteRange range, int fileLength, int imageIndex, int tagId, TiffDiagnostics diagnostics)
    {
        if (range == null || range.IsSparse)
        {
            return;
        }

        if (range.EndOffset > (ulong)fileLength)
        {
            TiffValidationPolicy.AddFatalIssue(diagnostics, TiffErrorCode.InvalidByteRange, "TIFF image data byte range is outside the file.", ToDiagnosticOffset(range.Offset), imageIndex, tagId);
        }
    }

    internal static bool ValidateCompressionSupport(TiffImageInfo info, TiffReadOptions options, TiffDiagnostics diagnostics)
    {
        if (TiffCodec.CanDecodeCompressionWithCodec(info.Compression, options.CompressionCodec))
        {
            return true;
        }

        TiffValidationPolicy.AddConformanceIssue(diagnostics, options, TiffErrorCode.UnsupportedCompression, "No decoder is available for the image compression identifier.", TiffDiagnostic.NoOffset, info.ImageIndex, TiffTagId.Compression);
        return false;
    }

    internal static void ValidatePhotometricSupport(TiffImageInfo info, TiffReadOptions options, TiffDiagnostics diagnostics)
    {
        if (TiffSourceRgbaReader.IsSupportedPhotometric(info.Photometric))
        {
            return;
        }

        TiffValidationPolicy.AddConformanceIssue(diagnostics, options, TiffErrorCode.UnsupportedPhotometric, "The photometric interpretation is recognized only as raw source data and has no supported high-level conversion.", TiffDiagnostic.NoOffset, info.ImageIndex, TiffTagId.PhotometricInterpretation);
    }

    internal static void ValidatePhotometricShape(TiffImageInfo info, TiffReadOptions options, TiffDiagnostics diagnostics)
    {
        int minimumSamples = 1;
        bool exactSamples = false;
        if (info.Photometric == TiffPhotometric.Rgb || info.Photometric == TiffPhotometric.YCbCr || TiffLabPhotometricConverter.IsLabPhotometric(info.Photometric))
        {
            minimumSamples = 3;
        }
        else if (info.Photometric == TiffPhotometric.Cmyk)
        {
            minimumSamples = 4;
        }
        else if (info.Photometric == TiffPhotometric.ColorFilterArray || info.Photometric == TiffPhotometric.LogL)
        {
            exactSamples = true;
        }
        else if (info.Photometric == TiffPhotometric.LogLuv)
        {
            if (info.SamplesPerPixel == 1 || info.SamplesPerPixel >= 3)
            {
                return;
            }

            minimumSamples = 3;
        }

        bool mismatch = info.SamplesPerPixel < minimumSamples;
        if (exactSamples && info.SamplesPerPixel != minimumSamples)
        {
            mismatch = true;
        }

        if (mismatch)
        {
            TiffValidationPolicy.AddConformanceIssue(diagnostics, options, TiffErrorCode.PhotometricMismatch, "SamplesPerPixel conflicts with the photometric interpretation.", TiffDiagnostic.NoOffset, info.ImageIndex, TiffTagId.SamplesPerPixel);
        }
    }

    internal static void ValidateDecodedSegments(TiffDirectory directory, TiffImageInfo info, byte[] fileBytes, TiffByteOrder byteOrder, TiffReadOptions options, TiffDiagnostics diagnostics)
    {
        int offsetsTagId = TiffTagId.StripOffsets;
        int byteCountsTagId = TiffTagId.StripByteCounts;
        if (info.IsTiled)
        {
            offsetsTagId = TiffTagId.TileOffsets;
            byteCountsTagId = TiffTagId.TileByteCounts;
        }

        TiffIfdEntry? offsetsEntry = directory.FindEntry(offsetsTagId);
        TiffIfdEntry? byteCountsEntry = directory.FindEntry(byteCountsTagId);
        if (offsetsEntry == null || byteCountsEntry == null)
        {
            return;
        }

        TiffDiagnostics valueDiagnostics = new TiffDiagnostics();
        TiffLimits limits = TiffDirectoryReader.GetLimits(options);
        ulong[] offsets = TiffImageInfoExtractor.ReadUnsignedArray(offsetsEntry, fileBytes, byteOrder, limits, directory.ImageIndex, valueDiagnostics);
        ulong[] byteCounts = TiffImageInfoExtractor.ReadUnsignedArray(byteCountsEntry, fileBytes, byteOrder, limits, directory.ImageIndex, valueDiagnostics);
        if (valueDiagnostics.HasErrors())
        {
            diagnostics.Merge(valueDiagnostics);
            return;
        }

        TiffByteRange[] ranges = new TiffByteRange[0];
        if (info.IsTiled)
        {
            ranges = TiffStrileLayout.CalculateTileByteRanges(info, offsets, byteCounts, valueDiagnostics);
        }
        else
        {
            ranges = TiffStrileLayout.CalculateStripByteRanges(info, offsets, byteCounts, valueDiagnostics);
        }

        if (valueDiagnostics.HasErrors())
        {
            diagnostics.Merge(valueDiagnostics);
            return;
        }

        int index = 0;
        while (index < ranges.Length)
        {
            TiffByteRange range = ranges[index];
            if (!range.IsSparse)
            {
                ulong expectedByteCount = 0;
                if (info.IsTiled)
                {
                    expectedByteCount = TiffStrileLayout.CalculateTileStorageByteCount(info, (ulong)index, valueDiagnostics);
                }
                else
                {
                    expectedByteCount = TiffStrileLayout.CalculateStripByteCount(info, (ulong)index, valueDiagnostics);
                }

                if (!valueDiagnostics.HasErrors())
                {
                    byte[] encodedBytes = TiffIfdEntry.CopyBytes(fileBytes.AsSpan((int)range.Offset, (int)range.ByteCount));
                    TiffCodecContext context = TiffCodecContext.Create(info, info.IsTiled, (ulong)index, expectedByteCount, limits);
                    context.ByteOrder = byteOrder;
                    TiffDiagnostics codecDiagnostics = new TiffDiagnostics();
                    TiffCodec.ValidateDecodeContext(context, encodedBytes, codecDiagnostics);
                    diagnostics.MergeWithLocation(codecDiagnostics, ToDiagnosticOffset(range.Offset), directory.ImageIndex, offsetsTagId);
                }
            }

            index = index + 1;
        }

        valueDiagnostics.FillMissingLocation(ToDiagnosticOffset(directory.Offset), directory.ImageIndex, offsetsTagId);
        diagnostics.Merge(valueDiagnostics);
    }

    internal static void MergeDirectoryDiagnostics(TiffDiagnostics diagnostics, TiffDiagnostics directoryDiagnostics, TiffDirectory directory)
    {
        directoryDiagnostics.FillMissingLocation(ToDiagnosticOffset(directory.Offset), directory.ImageIndex, TiffDiagnostic.NoTagId);
        diagnostics.Merge(directoryDiagnostics);
    }

    internal static long ToDiagnosticOffset(ulong offset)
    {
        if (offset > long.MaxValue)
        {
            return long.MaxValue;
        }

        return (long)offset;
    }

    internal static bool RangesOverlap(TiffByteRange first, TiffByteRange second)
    {
        if (first == null || second == null || first.IsSparse || second.IsSparse)
        {
            return false;
        }

        return first.Offset < second.EndOffset && second.Offset < first.EndOffset;
    }
}
