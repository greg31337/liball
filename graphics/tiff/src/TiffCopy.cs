namespace tiff;

/// <summary>Copies metadata or compatible raw image data between the public reader and writer models.</summary>
internal static class TiffCopy
{
    /// <summary>Creates a detached copy of the metadata data so later mutations do not affect the source.</summary>
    /// <param name="image">The image.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffMetadataCopyResult CopyMetadata(TiffImage image)
    {
        bool preserveUnknown = image != null && TiffRawUnknownTagPreserver.ShouldPreserveUnknownTags(image.Options);
        return CopyMetadataWithPolicy(image, preserveUnknown);
    }

    /// <summary>Copies metadata while explicitly preserving unregistered tags.</summary>
    /// <param name="image">The source image.</param>
    /// <returns>A result containing the produced value and diagnostics.</returns>
    public static TiffMetadataCopyResult CopyMetadataPreservingUnknown(TiffImage image)
    {
        return CopyMetadataWithPolicy(image, true);
    }

    internal static TiffMetadataCopyResult CopyMetadataWithPolicy(TiffImage? image, bool preserveUnknown)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffTagValue[] values = new TiffTagValue[0];
        if (image == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A source image is required to copy metadata.");
            return TiffMetadataCopyResult.Create(values, diagnostics);
        }

        TiffIfdEntry[] tags = image.GetTags();
        TiffLimits limits = TiffDirectoryReader.GetLimits(image.Options);
        int index = 0;
        while (index < tags.Length)
        {
            if (ShouldCopyMetadataTagWithPolicy(tags[index], image.Options, preserveUnknown))
            {
                TiffTagValue value = TiffExifMetadataReader.ReadSourceValue(tags[index], image.Source, image.Header.ByteOrder, limits, diagnostics);
                if (!diagnostics.HasErrors())
                {
                    values = TiffImageDefinition.SetTagValue(values, value);
                }
            }

            index = index + 1;
        }

        return TiffMetadataCopyResult.Create(values, diagnostics);
    }

    /// <summary>Rewrites one image without decoding its image segments when compatibility permits.</summary>
    /// <param name="image">The source image.</param>
    /// <param name="writer">The destination writer.</param>
    /// <param name="options">The rewrite policy.</param>
    /// <returns>The rewrite result.</returns>
    public static TiffMetadataRewriteResult RewriteImageMetadata(TiffImage image, TiffWriter writer, TiffMetadataRewriteOptions options)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffMetadataRewriteOptions actualOptions = options == null ? new TiffMetadataRewriteOptions() : options.Copy();
        bool requested = actualOptions.PassThroughRawImageData;
        if (image == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A source image is required to rewrite metadata.");
            return TiffMetadataRewriteResult.Create(-1, false, false, requested, false, false, "A source image is required.", diagnostics);
        }

        if (writer == null || !writer.GetIsOpen())
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "An open destination TIFF writer is required to rewrite metadata.");
            return TiffMetadataRewriteResult.Create(-1, false, false, requested, false, false, "An open destination writer is required.", diagnostics);
        }

        ValidateRewriteChanges(actualOptions, writer.GetByteOrder(), diagnostics);
        string incompatibility = GetRawPassThroughIncompatibility(image, writer);
        bool canPassThrough = incompatibility.Length == 0;
        if (requested && !canPassThrough)
        {
            diagnostics.AddError(TiffErrorCode.InvalidTagValue, incompatibility);
            return TiffMetadataRewriteResult.Create(-1, false, false, true, false, false, incompatibility, diagnostics);
        }

        if (diagnostics.HasErrors())
        {
            return TiffMetadataRewriteResult.Create(-1, false, false, requested, canPassThrough, false, incompatibility, diagnostics);
        }

        TiffImageDefinition definition = new TiffImageDefinition();
        bool passedThrough = false;
        if (requested)
        {
            TiffRawImageDataCopyResult raw = CopyRawImageData(image);
            diagnostics.Merge(raw.Diagnostics);
            if (diagnostics.HasErrors())
            {
                string reason = "Encoded image segments could not be read for pass-through.";
                return TiffMetadataRewriteResult.Create(-1, false, false, true, false, false, reason, diagnostics);
            }

            definition = raw.Definition;
            passedThrough = true;
        }

        TiffMetadataCopyResult metadata = CopyMetadataWithPolicy(image, actualOptions.PreserveUnknownMetadata);
        diagnostics.Merge(metadata.Diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffMetadataRewriteResult.Create(-1, false, false, requested, canPassThrough, false, incompatibility, diagnostics);
        }

        int index = 0;
        while (index < metadata.Values.Length)
        {
            definition.SetTag(metadata.Values[index]);
            index = index + 1;
        }

        index = 0;
        while (index < actualOptions.Updates.Length)
        {
            definition.SetTag(actualOptions.Updates[index]);
            index = index + 1;
        }

        index = 0;
        while (index < actualOptions.RemovedTagIds.Length)
        {
            definition.RemoveTag(actualOptions.RemovedTagIds[index]);
            index = index + 1;
        }

        bool removeExif = ContainsInteger(actualOptions.RemovedTagIds, TiffTagId.ExifIfd);
        bool removeGps = ContainsInteger(actualOptions.RemovedTagIds, TiffTagId.GpsIfd);
        TiffExifReadResult exif = new TiffExifReadResult();
        TiffGpsReadResult gps = new TiffGpsReadResult();
        if (!removeExif)
        {
            exif = image.ReadExif();
            diagnostics.Merge(exif.Diagnostics);
        }

        if (!removeGps)
        {
            gps = image.ReadGps();
            diagnostics.Merge(gps.Diagnostics);
        }

        if (diagnostics.HasErrors())
        {
            return TiffMetadataRewriteResult.Create(-1, false, false, requested, canPassThrough, false, incompatibility, diagnostics);
        }

        int destinationImageIndex = writer.AddImage(definition);
        if (destinationImageIndex < 0)
        {
            diagnostics.Merge(writer.GetDiagnostics());
            return TiffMetadataRewriteResult.Create(-1, false, false, requested, canPassThrough, false, incompatibility, diagnostics);
        }

        if (exif.Metadata.HasExif)
        {
            diagnostics.Merge(writer.WriteExif(destinationImageIndex, exif.Metadata));
        }

        if (gps.Metadata.HasGps)
        {
            diagnostics.Merge(writer.WriteGps(destinationImageIndex, gps.Metadata));
        }

        bool unknownPreserved = actualOptions.PreserveUnknownMetadata;
        string reasonText = incompatibility;
        if (!requested && reasonText.Length == 0)
        {
            reasonText = "Encoded image-data pass-through was not requested.";
        }

        return TiffMetadataRewriteResult.Create(destinationImageIndex, true, unknownPreserved, requested, canPassThrough, passedThrough, reasonText, diagnostics);
    }

    /// <summary>Creates a detached copy of the metadata data so later mutations do not affect the source.</summary>
    /// <param name="image">The image.</param>
    /// <param name="destination">The destination buffer that receives the encoded or decoded bytes.</param>
    /// <returns>The resulting TIFF image definition.</returns>
    public static TiffImageDefinition CopyMetadata(TiffImage image, TiffImageDefinition destination)
    {
        TiffImageDefinition result = NormalizeDefinition(destination);
        TiffMetadataCopyResult metadata = CopyMetadata(image);
        int index = 0;
        while (index < metadata.Values.Length)
        {
            result.SetTag(metadata.Values[index]);
            index = index + 1;
        }

        return result;
    }

    /// <summary>Creates a detached copy of the metadata data so later mutations do not affect the source.</summary>
    /// <param name="image">The image.</param>
    /// <param name="writer">The writer.</param>
    /// <param name="imageIndex">The zero-based image index.</param>
    /// <returns>The resulting TIFF diagnostics.</returns>
    public static TiffDiagnostics CopyMetadata(TiffImage image, TiffWriter writer, int imageIndex)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (writer == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A destination TIFF writer is required to copy metadata.");
            return diagnostics;
        }

        TiffMetadataCopyResult metadata = CopyMetadata(image);
        diagnostics.Merge(metadata.Diagnostics);
        if (diagnostics.HasErrors())
        {
            return diagnostics;
        }

        int index = 0;
        while (index < metadata.Values.Length)
        {
            diagnostics.Merge(writer.WriteTag(imageIndex, metadata.Values[index]));
            if (diagnostics.HasErrors())
            {
                return diagnostics;
            }

            index = index + 1;
        }

        return diagnostics;
    }

    /// <summary>Creates a detached copy of the raw image data data so later mutations do not affect the source.</summary>
    /// <param name="image">The image.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffRawImageDataCopyResult CopyRawImageData(TiffImage image)
    {
        return CopyRawImageData(image, new TiffImageDefinition());
    }

    internal static TiffRawImageDataCopyResult CopyRawImageDataWithOperation(TiffImage image, TiffOperationMonitor monitor)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffImageDefinition definition = new TiffImageDefinition();
        byte[][] rawStrips = new byte[0][];
        byte[][] rawTiles = new byte[0][];
        bool isTiled = false;
        monitor.CheckCancellation();
        if (image == null)
        {
            monitor.Start(0);
            diagnostics.AddError(TiffErrorCode.UnknownError, "A source image is required to copy raw image data.");
            return TiffRawImageDataCopyResult.Create(isTiled, rawStrips, rawTiles, definition, diagnostics);
        }

        TiffImageInfo info = image.GetInfo();
        diagnostics.Merge(image.GetDiagnostics());
        if (diagnostics.HasErrors())
        {
            monitor.Start(0);
            return TiffRawImageDataCopyResult.Create(isTiled, rawStrips, rawTiles, definition, diagnostics);
        }

        ulong segmentCount = info.IsTiled ? image.GetTileCount() : image.GetStripCount();
        long totalUnits = TiffOperationMonitor.ConvertUnits(segmentCount);
        monitor.Start(totalUnits);
        CopyRawImageDataTags(image, definition, diagnostics);
        monitor.CheckCancellation();
        if (diagnostics.HasErrors())
        {
            return TiffRawImageDataCopyResult.Create(info.IsTiled, rawStrips, rawTiles, definition, diagnostics);
        }

        if (info.IsTiled)
        {
            isTiled = true;
            rawTiles = CopyRawTilesWithOperation(image, diagnostics, monitor, totalUnits);
            int tileIndex = 0;
            while (tileIndex < rawTiles.Length)
            {
                monitor.CheckCancellation();
                definition.SetRawTile(tileIndex, rawTiles[tileIndex]);
                tileIndex = tileIndex + 1;
            }
        }
        else
        {
            rawStrips = CopyRawStripsWithOperation(image, diagnostics, monitor, totalUnits);
            int stripIndex = 0;
            while (stripIndex < rawStrips.Length)
            {
                monitor.CheckCancellation();
                definition.SetRawStrip(stripIndex, rawStrips[stripIndex]);
                stripIndex = stripIndex + 1;
            }
        }

        if (!diagnostics.HasErrors())
        {
            monitor.Complete(totalUnits);
        }

        return TiffRawImageDataCopyResult.Create(isTiled, rawStrips, rawTiles, definition, diagnostics);
    }

    /// <summary>Creates a detached copy of the raw image data data so later mutations do not affect the source.</summary>
    /// <param name="image">The image.</param>
    /// <param name="destination">The destination buffer that receives the encoded or decoded bytes.</param>
    /// <returns>A result containing the produced value and any diagnostics.</returns>
    public static TiffRawImageDataCopyResult CopyRawImageData(TiffImage image, TiffImageDefinition destination)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffImageDefinition definition = NormalizeDefinition(destination);
        byte[][] rawStrips = new byte[0][];
        byte[][] rawTiles = new byte[0][];
        bool isTiled = false;
        if (image == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A source image is required to copy raw image data.");
            return TiffRawImageDataCopyResult.Create(isTiled, rawStrips, rawTiles, definition, diagnostics);
        }

        TiffImageInfo info = image.GetInfo();
        diagnostics.Merge(image.GetDiagnostics());
        if (diagnostics.HasErrors())
        {
            return TiffRawImageDataCopyResult.Create(isTiled, rawStrips, rawTiles, definition, diagnostics);
        }

        CopyRawImageDataTags(image, definition, diagnostics);
        if (diagnostics.HasErrors())
        {
            return TiffRawImageDataCopyResult.Create(info.IsTiled, rawStrips, rawTiles, definition, diagnostics);
        }

        if (info.IsTiled)
        {
            isTiled = true;
            rawTiles = CopyRawTiles(image, diagnostics);
            int tileIndex = 0;
            while (tileIndex < rawTiles.Length)
            {
                definition.SetRawTile(tileIndex, rawTiles[tileIndex]);
                tileIndex = tileIndex + 1;
            }
        }
        else
        {
            rawStrips = CopyRawStrips(image, diagnostics);
            int stripIndex = 0;
            while (stripIndex < rawStrips.Length)
            {
                definition.SetRawStrip(stripIndex, rawStrips[stripIndex]);
                stripIndex = stripIndex + 1;
            }
        }

        return TiffRawImageDataCopyResult.Create(isTiled, rawStrips, rawTiles, definition, diagnostics);
    }

    /// <summary>Creates a detached copy of the raw image data data so later mutations do not affect the source.</summary>
    /// <param name="image">The image.</param>
    /// <param name="writer">The writer.</param>
    /// <param name="imageIndex">The zero-based image index.</param>
    /// <returns>The resulting TIFF diagnostics.</returns>
    public static TiffDiagnostics CopyRawImageData(TiffImage image, TiffWriter writer, int imageIndex)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (writer == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "A destination TIFF writer is required to copy raw image data.");
            return diagnostics;
        }

        TiffRawImageDataCopyResult result = CopyRawImageData(image);
        diagnostics.Merge(result.Diagnostics);
        if (diagnostics.HasErrors())
        {
            return diagnostics;
        }

        int tagIndex = 0;
        while (tagIndex < result.Definition.Tags.Length)
        {
            diagnostics.Merge(writer.WriteTag(imageIndex, result.Definition.Tags[tagIndex]));
            if (diagnostics.HasErrors())
            {
                return diagnostics;
            }

            tagIndex = tagIndex + 1;
        }

        int stripIndex = 0;
        while (stripIndex < result.RawStrips.Length)
        {
            diagnostics.Merge(writer.WriteRawStrip(imageIndex, stripIndex, result.RawStrips[stripIndex]).Diagnostics);
            if (diagnostics.HasErrors())
            {
                return diagnostics;
            }

            stripIndex = stripIndex + 1;
        }

        int tileIndex = 0;
        while (tileIndex < result.RawTiles.Length)
        {
            diagnostics.Merge(writer.WriteRawTile(imageIndex, tileIndex, result.RawTiles[tileIndex]).Diagnostics);
            if (diagnostics.HasErrors())
            {
                return diagnostics;
            }

            tileIndex = tileIndex + 1;
        }

        return diagnostics;
    }

    internal static byte[][] CopyRawStrips(TiffImage image, TiffDiagnostics diagnostics)
    {
        ulong stripCount = image.GetStripCount();
        diagnostics.Merge(image.GetDiagnostics());
        if (diagnostics.HasErrors())
        {
            return new byte[0][];
        }

        if (stripCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Strip count is too large to copy.");
            return new byte[0][];
        }

        byte[][] strips = new byte[(int)stripCount][];
        int index = 0;
        while (index < strips.Length)
        {
            TiffRawStripReadResult result = image.ReadRawStrip(index);
            diagnostics.Merge(result.Diagnostics);
            if (diagnostics.HasErrors())
            {
                return new byte[0][];
            }

            strips[index] = TiffTagValue.CopyByteArray(result.Bytes);
            index = index + 1;
        }

        return strips;
    }

    internal static byte[][] CopyRawTiles(TiffImage image, TiffDiagnostics diagnostics)
    {
        ulong tileCount = image.GetTileCount();
        diagnostics.Merge(image.GetDiagnostics());
        if (diagnostics.HasErrors())
        {
            return new byte[0][];
        }

        if (tileCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Tile count is too large to copy.");
            return new byte[0][];
        }

        byte[][] tiles = new byte[(int)tileCount][];
        int index = 0;
        while (index < tiles.Length)
        {
            TiffRawTileReadResult result = image.ReadRawTile(index);
            diagnostics.Merge(result.Diagnostics);
            if (diagnostics.HasErrors())
            {
                return new byte[0][];
            }

            tiles[index] = TiffTagValue.CopyByteArray(result.Bytes);
            index = index + 1;
        }

        return tiles;
    }

    internal static byte[][] CopyRawStripsWithOperation(TiffImage image, TiffDiagnostics diagnostics, TiffOperationMonitor monitor, long totalUnits)
    {
        ulong stripCount = image.GetStripCount();
        diagnostics.Merge(image.GetDiagnostics());
        if (diagnostics.HasErrors())
        {
            return new byte[0][];
        }

        if (stripCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Strip count is too large to copy.");
            return new byte[0][];
        }

        byte[][] strips = new byte[(int)stripCount][];
        int index = 0;
        while (index < strips.Length)
        {
            monitor.CheckCancellation();
            TiffRawStripReadResult result = image.ReadRawStrip(index);
            diagnostics.Merge(result.Diagnostics);
            if (diagnostics.HasErrors())
            {
                return new byte[0][];
            }

            strips[index] = TiffTagValue.CopyByteArray(result.Bytes);
            index = index + 1;
            if (index < strips.Length)
            {
                monitor.Advance(index, totalUnits);
            }
        }

        return strips;
    }

    internal static byte[][] CopyRawTilesWithOperation(TiffImage image, TiffDiagnostics diagnostics, TiffOperationMonitor monitor, long totalUnits)
    {
        ulong tileCount = image.GetTileCount();
        diagnostics.Merge(image.GetDiagnostics());
        if (diagnostics.HasErrors())
        {
            return new byte[0][];
        }

        if (tileCount > int.MaxValue)
        {
            diagnostics.AddError(TiffErrorCode.AllocationLimitExceeded, "Tile count is too large to copy.");
            return new byte[0][];
        }

        byte[][] tiles = new byte[(int)tileCount][];
        int index = 0;
        while (index < tiles.Length)
        {
            monitor.CheckCancellation();
            TiffRawTileReadResult result = image.ReadRawTile(index);
            diagnostics.Merge(result.Diagnostics);
            if (diagnostics.HasErrors())
            {
                return new byte[0][];
            }

            tiles[index] = TiffTagValue.CopyByteArray(result.Bytes);
            index = index + 1;
            if (index < tiles.Length)
            {
                monitor.Advance(index, totalUnits);
            }
        }

        return tiles;
    }

    internal static void CopyRawImageDataTags(TiffImage image, TiffImageDefinition destination, TiffDiagnostics diagnostics)
    {
        TiffIfdEntry[] tags = image.GetTags();
        TiffLimits limits = TiffDirectoryReader.GetLimits(image.Options);
        int index = 0;
        while (index < tags.Length)
        {
            if (ShouldCopyRawImageDataTag(tags[index]))
            {
                TiffTagValue sourceValue = TiffExifMetadataReader.ReadSourceValue(tags[index], image.Source, image.Header.ByteOrder, limits, diagnostics);
                if (diagnostics.HasErrors())
                {
                    return;
                }

                TiffTagValue? destinationValue = TiffTagDefaultLookup.FindValue(destination.Tags, sourceValue.TagId);
                if (destinationValue == null)
                {
                    destination.SetTag(sourceValue);
                }
                else if (!TagValuesEqual(sourceValue, destinationValue, diagnostics))
                {
                    diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Destination image definition is not compatible with the source raw image data.", image.ImageIndex, sourceValue.TagId);
                    return;
                }
            }

            index = index + 1;
        }
    }

    internal static TiffImageDefinition NormalizeDefinition(TiffImageDefinition definition)
    {
        if (definition == null)
        {
            return new TiffImageDefinition();
        }

        return definition.Copy();
    }

    internal static bool ShouldCopyMetadataTag(TiffIfdEntry entry, TiffReadOptions options)
    {
        return ShouldCopyMetadataTagWithPolicy(entry, options, TiffRawUnknownTagPreserver.ShouldPreserveUnknownTags(options));
    }

    internal static bool ShouldCopyMetadataTagWithPolicy(TiffIfdEntry entry, TiffReadOptions options, bool preserveUnknown)
    {
        if (entry == null)
        {
            return false;
        }

        if (IsExcludedMetadataCopyTag(entry.TagId))
        {
            return false;
        }

        if (TiffRawUnknownTagPreserver.IsUnknownTag(entry, options) && !preserveUnknown)
        {
            return false;
        }

        return true;
    }

    internal static bool IsProtectedRewriteTag(int tagId)
    {
        return IsExcludedMetadataCopyTag(tagId);
    }

    internal static bool IsProtectedMetadataRemovalTag(int tagId)
    {
        if (tagId == TiffTagId.ExifIfd || tagId == TiffTagId.GpsIfd)
        {
            return false;
        }

        return IsExcludedMetadataCopyTag(tagId);
    }

    internal static void ValidateRewriteChanges(TiffMetadataRewriteOptions options, TiffByteOrder byteOrder, TiffDiagnostics diagnostics)
    {
        int index = 0;
        while (index < options.Updates.Length)
        {
            TiffTagValue value = options.Updates[index];
            if (value == null || value.TagId <= 0 || value.TagId > 65535 || IsProtectedRewriteTag(value.TagId))
            {
                int tagId = value == null ? 0 : value.TagId;
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Metadata updates cannot change structural image-layout or IFD-pointer tags.", TiffDiagnostic.NoImageIndex, tagId);
                return;
            }

            TiffExifMetadataWriter.EncodeValue(value, byteOrder, diagnostics);
            if (diagnostics.HasErrors())
            {
                return;
            }

            index = index + 1;
        }

        index = 0;
        while (index < options.RemovedTagIds.Length)
        {
            int tagId = options.RemovedTagIds[index];
            if (tagId <= 0 || tagId > 65535 || IsProtectedMetadataRemovalTag(tagId))
            {
                diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "Metadata removal cannot remove structural image-layout tags.", TiffDiagnostic.NoImageIndex, tagId);
                return;
            }

            index = index + 1;
        }
    }

    internal static string GetRawPassThroughIncompatibility(TiffImage image, TiffWriter writer)
    {
        if (!image.GetIsOpen())
        {
            return "The source TIFF image is closed.";
        }

        if (!image.GetCanReadImageData())
        {
            return "Image payload reads are disabled for the source TIFF handle.";
        }

        if (!writer.GetIsOpen())
        {
            return "The destination TIFF writer is closed.";
        }

        if (image.Header.ByteOrder != writer.GetByteOrder())
        {
            return "The source and destination byte orders differ, so encoded image data cannot be passed through safely.";
        }

        if (image.GetSubImageCount() > 0)
        {
            return "The source image has SubIFDs; rewriting the complete image tree requires copying each child image separately.";
        }

        TiffImageInfo info = image.GetInfo();
        if (image.GetDiagnostics().HasErrors())
        {
            return "The source image layout could not be read.";
        }

        ulong segmentCount = info.IsTiled ? image.GetTileCount() : image.GetStripCount();
        if (segmentCount == 0)
        {
            return "The source image does not contain any encoded strips or tiles to pass through.";
        }

        if (segmentCount > int.MaxValue)
        {
            return "The encoded strip or tile count exceeds the supported rewrite range.";
        }

        return "";
    }

    internal static bool ContainsInteger(int[] values, int value)
    {
        int index = 0;
        while (values != null && index < values.Length)
        {
            if (values[index] == value)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool IsExcludedMetadataCopyTag(int tagId)
    {
        switch (tagId)
        {
            case TiffTagId.NewSubfileType:
            case TiffTagId.SubfileType:
            case TiffTagId.ImageWidth:
            case TiffTagId.ImageLength:
            case TiffTagId.BitsPerSample:
            case TiffTagId.Compression:
            case TiffTagId.PhotometricInterpretation:
            case TiffTagId.Thresholding:
            case TiffTagId.CellWidth:
            case TiffTagId.CellLength:
            case TiffTagId.FillOrder:
            case TiffTagId.StripOffsets:
            case TiffTagId.Orientation:
            case TiffTagId.SamplesPerPixel:
            case TiffTagId.RowsPerStrip:
            case TiffTagId.StripByteCounts:
            case TiffTagId.MinSampleValue:
            case TiffTagId.MaxSampleValue:
            case TiffTagId.PlanarConfiguration:
            case TiffTagId.FreeOffsets:
            case TiffTagId.FreeByteCounts:
            case TiffTagId.GrayResponseUnit:
            case TiffTagId.GrayResponseCurve:
            case TiffTagId.T4Options:
            case TiffTagId.T6Options:
            case TiffTagId.Predictor:
            case TiffTagId.ColorMap:
            case TiffTagId.TileWidth:
            case TiffTagId.TileLength:
            case TiffTagId.TileOffsets:
            case TiffTagId.TileByteCounts:
            case TiffTagId.SubIFDs:
            case TiffTagId.ExtraSamples:
            case TiffTagId.SampleFormat:
            case TiffTagId.SMinSampleValue:
            case TiffTagId.SMaxSampleValue:
            case TiffTagId.JPEGTables:
            case TiffTagId.YCbCrCoefficients:
            case TiffTagId.YCbCrSubSampling:
            case TiffTagId.YCbCrPositioning:
            case TiffTagId.ReferenceBlackWhite:
            case TiffTagId.ExifIfd:
            case TiffTagId.GpsIfd:
            case TiffTagId.InteroperabilityIfd:
                return true;
            default:
                return false;
        }
    }

    internal static bool ShouldCopyRawImageDataTag(TiffIfdEntry entry)
    {
        if (entry == null)
        {
            return false;
        }

        return IsRawImageDataLayoutTag(entry.TagId);
    }

    internal static bool IsRawImageDataLayoutTag(int tagId)
    {
        switch (tagId)
        {
            case TiffTagId.NewSubfileType:
            case TiffTagId.SubfileType:
            case TiffTagId.ImageWidth:
            case TiffTagId.ImageLength:
            case TiffTagId.BitsPerSample:
            case TiffTagId.Compression:
            case TiffTagId.PhotometricInterpretation:
            case TiffTagId.Thresholding:
            case TiffTagId.FillOrder:
            case TiffTagId.Orientation:
            case TiffTagId.SamplesPerPixel:
            case TiffTagId.RowsPerStrip:
            case TiffTagId.MinSampleValue:
            case TiffTagId.MaxSampleValue:
            case TiffTagId.PlanarConfiguration:
            case TiffTagId.GrayResponseUnit:
            case TiffTagId.GrayResponseCurve:
            case TiffTagId.T4Options:
            case TiffTagId.T6Options:
            case TiffTagId.Predictor:
            case TiffTagId.ColorMap:
            case TiffTagId.TileWidth:
            case TiffTagId.TileLength:
            case TiffTagId.ExtraSamples:
            case TiffTagId.SampleFormat:
            case TiffTagId.SMinSampleValue:
            case TiffTagId.SMaxSampleValue:
            case TiffTagId.TransferFunction:
            case TiffTagId.TransferRange:
            case TiffTagId.JPEGTables:
            case TiffTagId.YCbCrCoefficients:
            case TiffTagId.YCbCrSubSampling:
            case TiffTagId.YCbCrPositioning:
            case TiffTagId.ReferenceBlackWhite:
                return true;
            default:
                return false;
        }
    }

    internal static bool TagValuesEqual(TiffTagValue first, TiffTagValue second, TiffDiagnostics diagnostics)
    {
        if (first == null || second == null)
        {
            return false;
        }

        if (first.TagId != second.TagId || first.DataType != second.DataType || first.Count != second.Count)
        {
            return false;
        }

        byte[] firstBytes = TiffExifMetadataWriter.EncodeValue(first, TiffByteOrder.LittleEndian, diagnostics);
        byte[] secondBytes = TiffExifMetadataWriter.EncodeValue(second, TiffByteOrder.LittleEndian, diagnostics);
        if (diagnostics.HasErrors())
        {
            return false;
        }

        if (firstBytes.Length != secondBytes.Length)
        {
            return false;
        }

        int index = 0;
        while (index < firstBytes.Length)
        {
            if (firstBytes[index] != secondBytes[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
