using System.IO;

namespace png;

internal class PngEncoder
{
    internal const int MetadataChunkPositionBeforePlte = 0;
    internal const int MetadataChunkPositionBeforeIdat = 1;
    internal const int MetadataChunkPositionAfterIdat = 2;

    internal static byte[] EncodeToBytes(PngImage image)
    {
        return EncodeToBytes(image, new PngEncoderOptions());
    }

    internal static byte[] EncodeToBytes(PngImage image, PngEncoderOptions options)
    {
        ResetEarlyBailout(options);
        PngChunk[] chunks = CreateChunks(image, options);
        if (options.EarlyBailoutByteCount > 0)
        {
            if (options.WasEarlyBailedOut)
            {
                return new byte[0];
            }

            PngEncodingBailout.ResetByteCount(options);
            return PngChunkDatastreamWriter.WriteToBytesWithBailout(chunks, options.Limits, options);
        }

        return PngChunkDatastreamWriter.WriteToBytes(chunks, options.Limits);
    }

    internal static void EncodeToStream(Stream stream, PngImage image)
    {
        EncodeToStream(stream, image, new PngEncoderOptions());
    }

    internal static void EncodeToStream(Stream stream, PngImage image, PngEncoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(stream);

        ResetEarlyBailout(options);
        PngChunk[] chunks = CreateChunks(image, options);
        if (options.WasEarlyBailedOut)
        {
            return;
        }

        if (options.EarlyBailoutByteCount > 0)
        {
            PngEncodingBailout.ResetByteCount(options);
            PngChunkDatastreamWriter.WriteWithBailout(stream, chunks, options.Limits, options);
            return;
        }

        PngChunkDatastreamWriter.Write(stream, chunks, options.Limits);
    }

    internal static void ResetEarlyBailout(PngEncoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.EarlyBailoutByteCount < 0)
        {
            throw new ArgumentOutOfRangeException("EarlyBailoutByteCount");
        }

        options.ProducedByteCount = 0;
        options.WasEarlyBailedOut = false;
    }

    internal static PngChunk[] CreateChunks(PngImage image, PngEncoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(image.Pixels);
        ArgumentNullException.ThrowIfNull(image.Pixels.Data);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Limits);
        PngSecurityLimitEnforcer.EnforceImage(image, options.Limits);
        ValidateMetadataOptions(options);
        PngFilterEncoder.ValidateRowFilterStrategy(options.RowFilterStrategy);
        if (options.UseFixedFilter || options.RowFilterStrategy == PngRowFilterStrategy.Fixed)
        {
            PngFilterEncoder.ValidateFixedFilterType(options.FixedFilterType);
        }

        PngImage analyzedImage = PngEncoderContentAnalyzer.Analyze(image, options);
        PngIhdr ihdr = CreateIhdr(analyzedImage.Pixels, options);
        PngSecurityLimitEnforcer.EnforceIhdr(ihdr, options.Limits);
        PngChunk[] result;
        if (options.EncodeAnimation && analyzedImage.Animation != null && analyzedImage.Animation.Frames.Length > 0)
        {
            result = CreateAnimatedChunks(ihdr, analyzedImage, options);
        }
        else
        {
            result = CreateStaticChunks(ihdr, analyzedImage, options);
        }

        if (options.WasEarlyBailedOut)
        {
            return new PngChunk[0];
        }

        return result;
    }

    internal static PngChunk[] CreateStaticChunks(PngIhdr ihdr, PngImage image, PngEncoderOptions options)
    {
        byte[] compressedImageData = EncodePixels(image.Pixels, ihdr, options);
        PngMetadata? preparedMetadata = PngMetadataEncodingProcessor.Prepare(image.Metadata, ihdr, options);
        if (ihdr.ColorType == PngColorType.IndexedColor)
        {
            ArgumentNullException.ThrowIfNull(preparedMetadata);
            return CreateIndexedChunks(ihdr, image.Metadata, preparedMetadata, compressedImageData, options);
        }

        PngChunk[] idatChunks = CreateIdatChunks(compressedImageData, options);
        PngChunk[] beforePlteChunks = SelectPreservedMetadataChunks(preparedMetadata, options, MetadataChunkPositionBeforePlte);
        PngChunk[] beforeIdatChunks = SelectPreservedMetadataChunks(preparedMetadata, options, MetadataChunkPositionBeforeIdat);
        PngChunk[] afterIdatChunks = SelectPreservedMetadataChunks(preparedMetadata, options, MetadataChunkPositionAfterIdat);
        int optionalPaletteCount = GetOptionalPaletteChunkCount(image.Metadata, ihdr, options);
        PngChunk[] chunks = new PngChunk[idatChunks.Length + beforePlteChunks.Length + beforeIdatChunks.Length + afterIdatChunks.Length + optionalPaletteCount + 2];
        int chunkIndex = 0;
        chunks[chunkIndex] = PngIhdrWriter.WriteChunk(ihdr);
        chunkIndex += 1;
        CopyChunks(beforePlteChunks, 0, chunks, chunkIndex, beforePlteChunks.Length);
        chunkIndex += beforePlteChunks.Length;
        if (optionalPaletteCount > 0)
        {
            chunks[chunkIndex] = PngPlteWriter.WriteChunk(image.Metadata.Palette);
            chunkIndex += 1;
        }

        CopyChunks(beforeIdatChunks, 0, chunks, chunkIndex, beforeIdatChunks.Length);
        chunkIndex += beforeIdatChunks.Length;
        CopyChunks(idatChunks, 0, chunks, chunkIndex, idatChunks.Length);
        chunkIndex += idatChunks.Length;
        CopyChunks(afterIdatChunks, 0, chunks, chunkIndex, afterIdatChunks.Length);
        chunks[chunks.Length - 1] = PngIendWriter.WriteChunk();
        return chunks;
    }

    internal static PngChunk[] CreateIndexedChunks(PngIhdr ihdr, PngMetadata metadata, PngMetadata preparedMetadata, byte[] compressedImageData, PngEncoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(metadata.Palette);
        ValidatePalette(metadata.Palette, ihdr);

        PngChunk[] idatChunks = CreateIdatChunks(compressedImageData, options);
        PngChunk[] beforePlteChunks = SelectPreservedMetadataChunks(preparedMetadata, options, MetadataChunkPositionBeforePlte);
        PngChunk[] beforeIdatChunks = SelectPreservedMetadataChunks(preparedMetadata, options, MetadataChunkPositionBeforeIdat);
        PngChunk[] afterIdatChunks = SelectPreservedMetadataChunks(preparedMetadata, options, MetadataChunkPositionAfterIdat);
        int transparencyChunkCount = GetIndexedTransparencyChunkCount(metadata);
        if (transparencyChunkCount > 0)
        {
            beforeIdatChunks = RemoveChunksByType(beforeIdatChunks, PngConstants.ChunkTransparency);
        }

        PngChunk[] chunks = new PngChunk[idatChunks.Length + beforePlteChunks.Length + beforeIdatChunks.Length + afterIdatChunks.Length + transparencyChunkCount + 3];
        int chunkIndex = 0;
        chunks[chunkIndex] = PngIhdrWriter.WriteChunk(ihdr);
        chunkIndex += 1;
        CopyChunks(beforePlteChunks, 0, chunks, chunkIndex, beforePlteChunks.Length);
        chunkIndex += beforePlteChunks.Length;
        chunks[chunkIndex] = PngPlteWriter.WriteChunk(metadata.Palette);
        chunkIndex += 1;
        if (transparencyChunkCount > 0)
        {
            chunks[chunkIndex] = PngTransparencyWriter.WriteIndexedChunk(metadata.IndexedTransparency ?? new PngIndexedTransparency());
            chunkIndex += 1;
        }

        CopyChunks(beforeIdatChunks, 0, chunks, chunkIndex, beforeIdatChunks.Length);
        chunkIndex += beforeIdatChunks.Length;
        CopyChunks(idatChunks, 0, chunks, chunkIndex, idatChunks.Length);
        chunkIndex += idatChunks.Length;
        CopyChunks(afterIdatChunks, 0, chunks, chunkIndex, afterIdatChunks.Length);
        chunks[chunks.Length - 1] = PngIendWriter.WriteChunk();
        return chunks;
    }

    internal static PngChunk[] CreateAnimatedChunks(PngIhdr ihdr, PngImage image, PngEncoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(image.Animation);
        ArgumentNullException.ThrowIfNull(image.Metadata);
        PngChunk[] animationChunks = PngApngEncoder.EncodeAnimation(ihdr, image.Pixels, image.Animation, options);
        PngMetadata? preparedMetadata = PngMetadataEncodingProcessor.Prepare(image.Metadata, ihdr, options);
        PngChunk[] beforePlteChunks = SelectPreservedMetadataChunks(preparedMetadata, options, MetadataChunkPositionBeforePlte);
        PngChunk[] beforeIdatChunks = SelectPreservedMetadataChunks(preparedMetadata, options, MetadataChunkPositionBeforeIdat);
        PngChunk[] afterIdatChunks = SelectPreservedMetadataChunks(preparedMetadata, options, MetadataChunkPositionAfterIdat);
        int paletteChunkCount = 0;
        int transparencyChunkCount = 0;
        if (ihdr.ColorType == PngColorType.IndexedColor)
        {
            ArgumentNullException.ThrowIfNull(image.Metadata.Palette);
            ValidatePalette(image.Metadata.Palette, ihdr);
            paletteChunkCount = 1;
            transparencyChunkCount = GetIndexedTransparencyChunkCount(image.Metadata);
            if (transparencyChunkCount > 0)
            {
                beforeIdatChunks = RemoveChunksByType(beforeIdatChunks, PngConstants.ChunkTransparency);
            }
        }
        else
        {
            paletteChunkCount = GetOptionalPaletteChunkCount(image.Metadata, ihdr, options);
        }

        PngChunk[] chunks = new PngChunk[animationChunks.Length + beforePlteChunks.Length + beforeIdatChunks.Length + afterIdatChunks.Length + paletteChunkCount + transparencyChunkCount + 2];
        int chunkIndex = 0;
        chunks[chunkIndex] = PngIhdrWriter.WriteChunk(ihdr);
        chunkIndex += 1;
        CopyChunks(beforePlteChunks, 0, chunks, chunkIndex, beforePlteChunks.Length);
        chunkIndex += beforePlteChunks.Length;
        if (paletteChunkCount > 0)
        {
            chunks[chunkIndex] = PngPlteWriter.WriteChunk(image.Metadata.Palette);
            chunkIndex += 1;
        }

        if (transparencyChunkCount > 0)
        {
            chunks[chunkIndex] = PngTransparencyWriter.WriteIndexedChunk(image.Metadata.IndexedTransparency ?? new PngIndexedTransparency());
            chunkIndex += 1;
        }

        CopyChunks(beforeIdatChunks, 0, chunks, chunkIndex, beforeIdatChunks.Length);
        chunkIndex += beforeIdatChunks.Length;
        CopyChunks(animationChunks, 0, chunks, chunkIndex, animationChunks.Length);
        chunkIndex += animationChunks.Length;
        CopyChunks(afterIdatChunks, 0, chunks, chunkIndex, afterIdatChunks.Length);
        chunks[chunks.Length - 1] = PngIendWriter.WriteChunk();
        return chunks;
    }

    internal static int GetIndexedTransparencyChunkCount(PngMetadata metadata)
    {
        if (metadata.IndexedTransparency == null)
        {
            return 0;
        }

        ArgumentNullException.ThrowIfNull(metadata.IndexedTransparency.Alpha);
        if (metadata.IndexedTransparency.Alpha.Length == 0)
        {
            return 0;
        }

        PngValidationResult validation = PngTransparencyValidator.ValidateIndexed(metadata.IndexedTransparency, metadata.Palette);
        if (!validation.IsValid)
        {
            throw new InvalidDataException("Indexed transparency is invalid.");
        }

        return 1;
    }

    internal static int GetOptionalPaletteChunkCount(PngMetadata? metadata, PngIhdr ihdr, PngEncoderOptions options)
    {
        if (!ShouldPreserveMetadata(options) || metadata == null)
        {
            return 0;
        }

        ArgumentNullException.ThrowIfNull(metadata.Palette);
        ArgumentNullException.ThrowIfNull(metadata.Palette.Entries);
        if (metadata.Palette.Entries.Length == 0)
        {
            return 0;
        }

        if (ihdr.ColorType != PngColorType.Truecolor && ihdr.ColorType != PngColorType.TruecolorAlpha)
        {
            return 0;
        }

        ValidatePalette(metadata.Palette, ihdr);
        return 1;
    }

    internal static PngChunk[] RemoveChunksByType(PngChunk[] chunks, string chunkType)
    {
        int retainedCount = 0;
        int index = 0;
        while (index < chunks.Length)
        {
            if (chunks[index].Type != chunkType)
            {
                retainedCount += 1;
            }

            index += 1;
        }

        PngChunk[] retained = new PngChunk[retainedCount];
        int retainedIndex = 0;
        index = 0;
        while (index < chunks.Length)
        {
            if (chunks[index].Type != chunkType)
            {
                retained[retainedIndex] = chunks[index];
                retainedIndex += 1;
            }

            index += 1;
        }

        return retained;
    }

    internal static PngIhdr CreateIhdr(PngPixelBuffer pixels, PngEncoderOptions options)
    {
        PngIhdr ihdr = new PngIhdr();
        ihdr.Width = pixels.Width;
        ihdr.Height = pixels.Height;
        ihdr.BitDepth = SelectBitDepth(pixels.Format, options);
        ihdr.ColorType = SelectColorType(pixels.Format, options);
        ihdr.CompressionMethod = options.CompressionMethod;
        ihdr.FilterMethod = options.FilterMethod;
        ihdr.InterlaceMethod = options.InterlaceMethod;
        return ihdr;
    }

    internal static PngBitDepth SelectBitDepth(PngPixelFormat format, PngEncoderOptions options)
    {
        if (!options.UseAutomaticBitDepth)
        {
            return options.BitDepth;
        }

        return GetBitDepth(format);
    }

    internal static PngColorType SelectColorType(PngPixelFormat format, PngEncoderOptions options)
    {
        if (!options.UseAutomaticColorType)
        {
            return options.ColorType;
        }

        return GetColorType(format);
    }

    internal static PngColorType GetColorType(PngPixelFormat format)
    {
        switch (format)
        {
            case PngPixelFormat.Grayscale1:
            case PngPixelFormat.Grayscale2:
            case PngPixelFormat.Grayscale4:
            case PngPixelFormat.Grayscale8:
            case PngPixelFormat.Grayscale16:
                return PngColorType.Grayscale;
            case PngPixelFormat.Indexed1:
            case PngPixelFormat.Indexed2:
            case PngPixelFormat.Indexed4:
            case PngPixelFormat.Indexed8:
                return PngColorType.IndexedColor;
            case PngPixelFormat.Rgb8:
            case PngPixelFormat.Rgb16:
            case PngPixelFormat.Bgr8:
            case PngPixelFormat.Bgr16:
                return PngColorType.Truecolor;
            case PngPixelFormat.GrayscaleAlpha8:
            case PngPixelFormat.GrayscaleAlpha16:
                return PngColorType.GrayscaleAlpha;
            case PngPixelFormat.Rgba8:
            case PngPixelFormat.Rgba16:
            case PngPixelFormat.Bgra8:
            case PngPixelFormat.Bgra16:
            case PngPixelFormat.Argb8:
            case PngPixelFormat.Argb16:
                return PngColorType.TruecolorAlpha;
            default:
                throw new ArgumentOutOfRangeException("Format");
        }
    }

    internal static PngBitDepth GetBitDepth(PngPixelFormat format)
    {
        switch (format)
        {
            case PngPixelFormat.Grayscale1:
            case PngPixelFormat.Indexed1:
                return PngBitDepth.One;
            case PngPixelFormat.Grayscale2:
            case PngPixelFormat.Indexed2:
                return PngBitDepth.Two;
            case PngPixelFormat.Grayscale4:
            case PngPixelFormat.Indexed4:
                return PngBitDepth.Four;
            case PngPixelFormat.Grayscale8:
            case PngPixelFormat.Indexed8:
            case PngPixelFormat.Rgb8:
            case PngPixelFormat.Bgr8:
            case PngPixelFormat.GrayscaleAlpha8:
            case PngPixelFormat.Rgba8:
            case PngPixelFormat.Bgra8:
            case PngPixelFormat.Argb8:
                return PngBitDepth.Eight;
            case PngPixelFormat.Grayscale16:
            case PngPixelFormat.Rgb16:
            case PngPixelFormat.Bgr16:
            case PngPixelFormat.GrayscaleAlpha16:
            case PngPixelFormat.Rgba16:
            case PngPixelFormat.Bgra16:
            case PngPixelFormat.Argb16:
                return PngBitDepth.Sixteen;
            default:
                throw new ArgumentOutOfRangeException("Format");
        }
    }

    internal static byte[] EncodePixels(PngPixelBuffer pixels, PngIhdr ihdr, PngEncoderOptions options)
    {
        PngPixelBuffer normalizedPixels = NormalizeChannelOrder(pixels);
        byte[] filteredScanlines;
        if (ihdr.InterlaceMethod == PngInterlaceMethod.None)
        {
            filteredScanlines = PngNonInterlacedImageEncoder.Encode(normalizedPixels, ihdr, options);
            return CompressImageData(filteredScanlines, options);
        }

        if (ihdr.InterlaceMethod == PngInterlaceMethod.Adam7)
        {
            byte[] imageData = CreateContiguousImageData(normalizedPixels, ihdr);
            filteredScanlines = PngAdam7Encoder.Encode(imageData, ihdr, options);
            return CompressImageData(filteredScanlines, options);
        }

        throw new ArgumentOutOfRangeException("InterlaceMethod");
    }

    internal static PngPixelBuffer NormalizeChannelOrder(PngPixelBuffer pixels)
    {
        switch (pixels.Format)
        {
            case PngPixelFormat.Bgr8:
            case PngPixelFormat.Bgr16:
            case PngPixelFormat.Bgra8:
            case PngPixelFormat.Bgra16:
                return PngChannelTransformer.SwapRgbBgr(pixels);
            case PngPixelFormat.Argb8:
            case PngPixelFormat.Argb16:
                return PngChannelTransformer.SwapRgbaArgb(pixels);
            default:
                return pixels;
        }
    }

    internal static byte[] CompressImageData(byte[] filteredScanlines, PngEncoderOptions options)
    {
        if (options.EarlyBailoutByteCount > 0)
        {
            return PngZlibCompressor.CompressWithBailout(filteredScanlines, 0, filteredScanlines.Length, PngCompressionLevelMapper.Map(options.CompressionLevel), options.Limits, options);
        }

        return PngZlibCompressor.Compress(filteredScanlines, 0, filteredScanlines.Length, PngCompressionLevelMapper.Map(options.CompressionLevel), options.Limits);
    }

    internal static byte[] CreateContiguousImageData(PngPixelBuffer pixels, PngIhdr ihdr)
    {
        PngScanlineLayout layout = PngScanlineLayoutCalculator.Calculate(ihdr);
        PngPixelFormat expectedFormat = PngNonInterlacedImageDecoder.GetNativePixelFormat(ihdr.ColorType, ihdr.BitDepth);
        if (pixels.Width != ihdr.Width)
        {
            throw new ArgumentOutOfRangeException("Width");
        }

        if (pixels.Height != ihdr.Height)
        {
            throw new ArgumentOutOfRangeException("Height");
        }

        if (pixels.Format != expectedFormat)
        {
            throw new ArgumentOutOfRangeException("Format");
        }

        if (layout.RowDataBytes > int.MaxValue || layout.ImageDataBytes > int.MaxValue)
        {
            throw new OverflowException();
        }

        int rowBytes = (int)layout.RowDataBytes;
        if (pixels.Stride < rowBytes)
        {
            throw new ArgumentOutOfRangeException("Stride");
        }

        long requiredBytes = CalculateRequiredBytes(pixels.Stride, pixels.Height, rowBytes);
        if (requiredBytes > pixels.Data.Length)
        {
            throw new ArgumentOutOfRangeException("Data");
        }

        byte[] imageData = new byte[(int)layout.ImageDataBytes];
        int rowIndex = 0;
        while (rowIndex < pixels.Height)
        {
            CopyBytes(pixels.Data, rowIndex * pixels.Stride, imageData, rowIndex * rowBytes, rowBytes);
            rowIndex += 1;
        }

        return imageData;
    }

    internal static PngChunk CreateIdatChunk(byte[] compressedImageData)
    {
        return CreateIdatChunk(compressedImageData, 0, compressedImageData.Length);
    }

    internal static PngChunk[] CreateIdatChunks(byte[] compressedImageData, PngEncoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(compressedImageData);
        if (options.MaxIdatChunkBytes < 0)
        {
            throw new ArgumentOutOfRangeException("MaxIdatChunkBytes");
        }

        int chunkCount = CalculateIdatChunkCount(compressedImageData.Length, options.MaxIdatChunkBytes);
        PngChunk[] chunks = new PngChunk[chunkCount];
        int chunkIndex = 0;
        int dataOffset = 0;
        while (chunkIndex < chunkCount)
        {
            int chunkLength = GetIdatChunkLength(compressedImageData.Length, dataOffset, options.MaxIdatChunkBytes);
            chunks[chunkIndex] = CreateIdatChunk(compressedImageData, dataOffset, chunkLength);
            dataOffset += chunkLength;
            chunkIndex += 1;
        }

        return chunks;
    }

    internal static PngChunk[] SelectPreservedMetadataChunks(PngMetadata? metadata, PngEncoderOptions options, int position)
    {
        if (!ShouldPreserveMetadata(options) || metadata == null)
        {
            return new PngChunk[0];
        }

        ArgumentNullException.ThrowIfNull(metadata.Chunks);

        int count = CountPreservedMetadataChunks(metadata.Chunks, options, position);
        PngChunk[] chunks = new PngChunk[count];
        int sourceIndex = 0;
        int destinationIndex = 0;
        while (sourceIndex < metadata.Chunks.Length)
        {
            PngChunk chunk = metadata.Chunks[sourceIndex];
            if (ShouldPreserveMetadataChunk(chunk, options, position))
            {
                PngChunk preservedChunk = CreatePreservedMetadataChunk(chunk, options);
                chunks[destinationIndex] = PngMetadataEncodingProcessor.TransformTextChunk(preservedChunk, options);
                destinationIndex += 1;
            }

            sourceIndex += 1;
        }

        return PngMetadataEncodingProcessor.SortCanonical(chunks, position);
    }

    internal static bool ShouldPreserveMetadata(PngEncoderOptions options)
    {
        switch (options.MetadataPolicy)
        {
            case PngMetadataPolicy.Preserve:
            case PngMetadataPolicy.KeepSelected:
            case PngMetadataPolicy.RemoveSelected:
                return true;
            case PngMetadataPolicy.Strip:
                return false;
            default:
                throw new ArgumentOutOfRangeException("MetadataPolicy");
        }
    }

    internal static int CountPreservedMetadataChunks(PngChunk[] chunks, PngEncoderOptions options, int position)
    {
        int count = 0;
        int index = 0;
        while (index < chunks.Length)
        {
            if (ShouldPreserveMetadataChunk(chunks[index], options, position))
            {
                count += 1;
            }

            index += 1;
        }

        return count;
    }

    internal static bool ShouldPreserveMetadataChunk(PngChunk chunk, PngEncoderOptions options, int position)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);

        if (IsRegeneratedChunk(chunk.Type) || IsAnimationChunk(chunk.Type))
        {
            return false;
        }

        if (!IsMetadataChunkSelected(chunk.Type, options))
        {
            return false;
        }

        if (IsUnknownChunk(chunk.Type) && !ShouldPreserveUnknownChunk(chunk, options))
        {
            return false;
        }

        if (!PngChunkTypeFlags.IsAncillary(chunk.Type))
        {
            return false;
        }

        return GetMetadataChunkPositionForChunk(chunk) == position;
    }

    internal static bool IsMetadataChunkSelected(string chunkType, PngEncoderOptions options)
    {
        switch (options.MetadataPolicy)
        {
            case PngMetadataPolicy.Preserve:
                return true;
            case PngMetadataPolicy.Strip:
                return false;
            case PngMetadataPolicy.KeepSelected:
                return ContainsChunkType(options.MetadataChunkTypesToKeep, chunkType);
            case PngMetadataPolicy.RemoveSelected:
                return !ContainsChunkType(options.MetadataChunkTypesToRemove, chunkType);
            default:
                throw new ArgumentOutOfRangeException("MetadataPolicy");
        }
    }

    internal static bool ShouldPreserveUnknownChunk(PngChunk chunk, PngEncoderOptions options)
    {
        switch (options.UnknownChunkDisposition)
        {
            case PngChunkDisposition.Preserve:
                return !options.PixelDataChanged || PngChunkTypeFlags.IsSafeToCopy(chunk.Type);
            case PngChunkDisposition.Drop:
                return false;
            case PngChunkDisposition.Replace:
                return FindUnknownChunkReplacement(options.UnknownChunkReplacements, chunk.Type) != null;
            default:
                throw new ArgumentOutOfRangeException("UnknownChunkDisposition");
        }
    }

    internal static PngChunk CreatePreservedMetadataChunk(PngChunk source, PngEncoderOptions options)
    {
        if (IsUnknownChunk(source.Type) && options.UnknownChunkDisposition == PngChunkDisposition.Replace)
        {
            PngUnknownChunkReplacement? replacement = FindUnknownChunkReplacement(options.UnknownChunkReplacements, source.Type);
            if (replacement == null)
            {
                throw new InvalidOperationException("Unknown chunk replacement was not found.");
            }

            PngChunk replacementChunk = PngUnknownChunkPreservation.CreateChunk(replacement.Type, replacement.Data, source.Offset);
            replacementChunk.Placement = source.Placement;
            replacementChunk.PreservationState = PngChunkPreservationState.Created;
            return replacementChunk;
        }

        return CopyChunk(source);
    }

    internal static PngUnknownChunkReplacement? FindUnknownChunkReplacement(PngUnknownChunkReplacement[] replacements, string chunkType)
    {
        int index = 0;
        while (index < replacements.Length)
        {
            if (replacements[index].Type == chunkType)
            {
                return replacements[index];
            }

            index += 1;
        }

        return null;
    }

    internal static bool ContainsChunkType(string[] chunkTypes, string chunkType)
    {
        int index = 0;
        while (index < chunkTypes.Length)
        {
            if (chunkTypes[index] == chunkType)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    internal static void ValidateMetadataOptions(PngEncoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options.MetadataChunkTypesToKeep);
        ArgumentNullException.ThrowIfNull(options.MetadataChunkTypesToRemove);
        ArgumentNullException.ThrowIfNull(options.UnknownChunkReplacements);
        ValidateMetadataChunkTypes(options.MetadataChunkTypesToKeep, "MetadataChunkTypesToKeep");
        ValidateMetadataChunkTypes(options.MetadataChunkTypesToRemove, "MetadataChunkTypesToRemove");
        ValidateUnknownChunkReplacements(options.UnknownChunkReplacements);
        ShouldPreserveMetadata(options);
        ValidateUnknownChunkDisposition(options.UnknownChunkDisposition);
        ValidateTextCompressionOptions(options);
    }

    internal static void ValidateTextCompressionOptions(PngEncoderOptions options)
    {
        if (options.TextCompressionMode != PngTextCompressionMode.Preserve
            && options.TextCompressionMode != PngTextCompressionMode.Uncompressed
            && options.TextCompressionMode != PngTextCompressionMode.Compressed
            && options.TextCompressionMode != PngTextCompressionMode.Automatic)
        {
            throw new ArgumentOutOfRangeException("TextCompressionMode");
        }

        if (options.TextCompressionThreshold < 0)
        {
            throw new ArgumentOutOfRangeException("TextCompressionThreshold");
        }
    }

    internal static void ValidateMetadataChunkTypes(string[] chunkTypes, string name)
    {
        int index = 0;
        while (index < chunkTypes.Length)
        {
            string chunkType = chunkTypes[index];
            ArgumentNullException.ThrowIfNull(chunkType);
            PngValidationResult validation = PngChunkTypeValidator.Validate(chunkType);
            if (!validation.IsValid || !PngChunkTypeFlags.IsAncillary(chunkType))
            {
                throw new ArgumentException("Metadata selection entries must be valid ancillary PNG chunk types.", name);
            }

            index += 1;
        }
    }

    internal static void ValidateUnknownChunkReplacements(PngUnknownChunkReplacement[] replacements)
    {
        int index = 0;
        while (index < replacements.Length)
        {
            PngUnknownChunkReplacement replacement = replacements[index];
            ArgumentNullException.ThrowIfNull(replacement);
            ArgumentNullException.ThrowIfNull(replacement.Type);
            ArgumentNullException.ThrowIfNull(replacement.Data);
            PngValidationResult validation = PngChunkTypeValidator.Validate(replacement.Type);
            if (!validation.IsValid || !PngChunkTypeFlags.IsAncillary(replacement.Type) || PngKnownChunkTypes.IsKnown(replacement.Type))
            {
                throw new ArgumentException("Unknown chunk replacements require a valid unknown ancillary PNG chunk type.");
            }

            int duplicateIndex = 0;
            while (duplicateIndex < index)
            {
                if (replacements[duplicateIndex].Type == replacement.Type)
                {
                    throw new ArgumentException("Unknown chunk replacement types must be unique.");
                }

                duplicateIndex += 1;
            }

            index += 1;
        }
    }

    internal static void ValidateUnknownChunkDisposition(PngChunkDisposition disposition)
    {
        if (disposition != PngChunkDisposition.Preserve
            && disposition != PngChunkDisposition.Drop
            && disposition != PngChunkDisposition.Replace)
        {
            throw new ArgumentOutOfRangeException("UnknownChunkDisposition");
        }
    }

    internal static bool IsUnknownChunk(string chunkType)
    {
        return !PngKnownChunkTypes.IsKnown(chunkType);
    }

    internal static int GetMetadataChunkPosition(string chunkType)
    {
        if (MustAppearBeforePlteAndIdat(chunkType))
        {
            return MetadataChunkPositionBeforePlte;
        }

        if (MustAppearBeforeIdat(chunkType))
        {
            return MetadataChunkPositionBeforeIdat;
        }

        return MetadataChunkPositionAfterIdat;
    }

    internal static int GetMetadataChunkPositionForChunk(PngChunk chunk)
    {
        if (!IsUnknownChunk(chunk.Type))
        {
            return GetMetadataChunkPosition(chunk.Type);
        }

        if (chunk.Placement == PngChunkPlacement.BeforePalette)
        {
            return MetadataChunkPositionBeforePlte;
        }

        if (chunk.Placement == PngChunkPlacement.BeforeImageData)
        {
            return MetadataChunkPositionBeforeIdat;
        }

        return MetadataChunkPositionAfterIdat;
    }

    internal static bool MustAppearBeforePlteAndIdat(string chunkType)
    {
        return chunkType == PngConstants.ChunkChromaticities
            || chunkType == PngConstants.ChunkGamma
            || chunkType == PngConstants.ChunkIccProfile
            || chunkType == PngConstants.ChunkSignificantBits
            || chunkType == PngConstants.ChunkStandardRgb
            || chunkType == PngConstants.ChunkCodingIndependentCodePoints
            || chunkType == PngConstants.ChunkMasteringDisplayColorVolume
            || chunkType == PngConstants.ChunkContentLightLevelInformation;
    }

    internal static bool MustAppearBeforeIdat(string chunkType)
    {
        return chunkType == PngConstants.ChunkTransparency
            || chunkType == PngConstants.ChunkBackgroundColor
            || chunkType == PngConstants.ChunkPaletteHistogram
            || chunkType == PngConstants.ChunkPhysicalPixels
            || chunkType == PngConstants.ChunkSuggestedPalette
            || chunkType == PngConstants.ChunkExif
            || chunkType == PngConstants.ChunkAnimationControl;
    }

    internal static bool IsRegeneratedChunk(string chunkType)
    {
        return chunkType == PngConstants.ChunkIHDR
            || chunkType == PngConstants.ChunkPLTE
            || chunkType == PngConstants.ChunkIDAT
            || chunkType == PngConstants.ChunkIEND;
    }

    internal static bool IsAnimationChunk(string chunkType)
    {
        return chunkType == PngConstants.ChunkAnimationControl
            || chunkType == PngConstants.ChunkFrameControl
            || chunkType == PngConstants.ChunkFrameData;
    }

    internal static PngChunk CreateIdatChunk(byte[] compressedImageData, int offset, int length)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = PngConstants.ChunkIDAT;
        chunk.Data = CopyBytes(compressedImageData, offset, length);
        chunk.Length = (uint)length;
        chunk.IsKnown = true;
        chunk.IsCritical = true;
        chunk.IsPublic = true;
        chunk.IsReservedBitValid = true;
        chunk.IsSafeToCopy = false;
        return chunk;
    }

    internal static int CalculateIdatChunkCount(int dataLength, int maxChunkBytes)
    {
        if (maxChunkBytes <= 0 || dataLength == 0)
        {
            return 1;
        }

        int chunkCount = dataLength / maxChunkBytes;
        if (dataLength % maxChunkBytes != 0)
        {
            chunkCount += 1;
        }

        return chunkCount;
    }

    internal static int GetIdatChunkLength(int dataLength, int dataOffset, int maxChunkBytes)
    {
        int remaining = dataLength - dataOffset;
        if (maxChunkBytes <= 0 || remaining <= maxChunkBytes)
        {
            return remaining;
        }

        return maxChunkBytes;
    }

    internal static void ValidatePalette(PngPalette palette, PngIhdr ihdr)
    {
        PngValidationResult result = PngPlteValidator.Validate(palette, ihdr);
        if (!result.IsValid)
        {
            throw new InvalidDataException("PLTE is invalid.");
        }
    }

    internal static long CalculateRequiredBytes(int stride, int height, int rowBytes)
    {
        if (height == 1)
        {
            return rowBytes;
        }

        return (long)stride * (height - 1) + rowBytes;
    }

    internal static void CopyBytes(byte[] source, int sourceOffset, byte[] destination, int destinationOffset, int length)
    {
        int index = 0;
        while (index < length)
        {
            destination[destinationOffset + index] = source[sourceOffset + index];
            index += 1;
        }
    }

    internal static byte[] CopyBytes(byte[] source, int offset, int length)
    {
        byte[] destination = new byte[length];
        CopyBytes(source, offset, destination, 0, length);
        return destination;
    }

    internal static PngChunk CopyChunk(PngChunk source)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = source.Type;
        chunk.Data = CopyBytes(source.Data, 0, source.Data.Length);
        chunk.Length = (uint)chunk.Data.Length;
        chunk.Crc = source.Crc;
        chunk.Offset = source.Offset;
        chunk.IsKnown = source.IsKnown;
        chunk.IsCritical = source.IsCritical;
        chunk.IsPublic = source.IsPublic;
        chunk.IsReservedBitValid = source.IsReservedBitValid;
        chunk.IsSafeToCopy = source.IsSafeToCopy;
        chunk.Placement = source.Placement;
        chunk.PreservationState = source.PreservationState;
        return chunk;
    }

    internal static void CopyChunks(PngChunk[] source, int sourceOffset, PngChunk[] destination, int destinationOffset, int length)
    {
        int index = 0;
        while (index < length)
        {
            destination[destinationOffset + index] = source[sourceOffset + index];
            index += 1;
        }
    }
}
