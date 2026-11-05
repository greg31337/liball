namespace raw;

internal enum NefVariantKind
{
    None = 0,
    Monochrome = 1,
    Linear = 2,
    Rgb = 3,
    YCbCr = 4,
    SmallYCbCr = 5
}

internal static class NefVariantDecoder
{
    internal const string LinearCodecName = "nikon-nef-uncompressed-linear";
    internal const string SmallYCbCrCodecName = "nikon-nef-small-ycbcr";
    internal const string MonochromeQuirk = "nikon-nef-monochrome";
    internal const string LinearQuirk = "nikon-nef-linear";
    internal const string RgbQuirk = "nikon-nef-rgb";
    internal const string YCbCrQuirk = "nikon-nef-ycbcr";

    internal static bool IsProfile(RawReadProfile? profile)
    {
        if (profile == null || profile.Identity == null || profile.Identity.Format != RawFormat.Nef || profile.Codec == null)
        {
            return false;
        }
        return GetKind(profile) != NefVariantKind.None;
    }

    internal static NefVariantKind GetKind(RawReadProfile profile)
    {
        if (profile == null || profile.Codec == null)
        {
            return NefVariantKind.None;
        }
        if (profile.Codec.Name == SmallYCbCrCodecName)
        {
            return NefVariantKind.SmallYCbCr;
        }
        if (profile.Codec.Name != LinearCodecName)
        {
            return NefVariantKind.None;
        }
        NefVariantKind kind = NefVariantKind.None;
        int count = 0;
        if (NefUncompressedDecoder.HasQuirk(profile.Codec, MonochromeQuirk))
        {
            kind = NefVariantKind.Monochrome;
            count++;
        }
        if (NefUncompressedDecoder.HasQuirk(profile.Codec, LinearQuirk))
        {
            kind = NefVariantKind.Linear;
            count++;
        }
        if (NefUncompressedDecoder.HasQuirk(profile.Codec, RgbQuirk))
        {
            kind = NefVariantKind.Rgb;
            count++;
        }
        if (NefUncompressedDecoder.HasQuirk(profile.Codec, YCbCrQuirk))
        {
            kind = NefVariantKind.YCbCr;
            count++;
        }
        return count == 1 ? kind : NefVariantKind.None;
    }

    internal static bool CanDecode(RawReadProfile profile, TiffImageDescriptor descriptor)
    {
        NefVariantKind kind = GetKind(profile);
        if (kind == NefVariantKind.None || descriptor == null || descriptor.PlanarConfiguration != TiffPlanarConfiguration.Chunky || descriptor.Predictor != TiffPredictor.None || descriptor.StorageKind != TiffStorageKind.Strips || descriptor.Blocks == null || descriptor.Blocks.Length == 0 || descriptor.CompressionCode != (ulong)profile.Codec.Compression)
        {
            return false;
        }
        if (kind == NefVariantKind.SmallYCbCr)
        {
            return descriptor.SamplesPerPixel == 3 && (profile.Codec.Compression == 1 || profile.Codec.Compression == 34713);
        }
        return profile.Codec.Compression == 1 && (descriptor.SamplesPerPixel == 1 || descriptor.SamplesPerPixel == 3);
    }

    internal static void ApplyToImage(RawImageInfo image, RawReadProfile profile)
    {
        NefVariantKind kind = GetKind(profile);
        RawPhotometricInterpretation photometric = RawPhotometricInterpretation.LinearRaw;
        int channels = image.GetChannelCount();
        if (kind == NefVariantKind.Monochrome)
        {
            photometric = image.Photometric == RawPhotometricInterpretation.WhiteIsZero ? RawPhotometricInterpretation.WhiteIsZero : RawPhotometricInterpretation.BlackIsZero;
            channels = 1;
        }
        else if (kind == NefVariantKind.Rgb)
        {
            photometric = RawPhotometricInterpretation.Rgb;
            channels = 3;
        }
        else if (kind == NefVariantKind.YCbCr || kind == NefVariantKind.SmallYCbCr)
        {
            photometric = RawPhotometricInterpretation.YCbCr;
            channels = 3;
        }
        image.Photometric = photometric;
        image.Cfa = null;
        image.Channels = CreateChannels(kind, channels);
        image.Planes = CreatePlanes(image, image.Channels);
    }

    internal static RawSensorImage Decode(RawDocument document, RawImageInfo info, RawDecodeOptions options)
    {
        if (document == null || document.TiffGraph == null || document.FormatDetails == null || document.FormatDetails.Nikon == null || document.ReadProfile == null)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedProfile, "Variant NEF decoding requires retained profile-qualified TIFF metadata.", -1);
        }
        TiffImageDescriptor descriptor = NefUncompressedDecoder.FindDescriptor(document.TiffGraph, info);
        RawSensorImage image = Decode(document.Source, document.TiffGraph, descriptor, info, document.FormatDetails.Nikon, document.ReadProfile, options);
        image.Diagnostics = document.Diagnostics;
        return image;
    }

    internal static RawSensorImage Decode(System.ReadOnlyMemory<byte> source, TiffIfdGraph graph, TiffImageDescriptor descriptor, RawImageInfo info, RawNikonNefMetadata metadata, RawReadProfile profile, RawDecodeOptions options)
    {
        NefVariantKind kind = Validate(source, graph, descriptor, info, metadata, profile, options);
        if (kind == NefVariantKind.SmallYCbCr)
        {
            return DecodeSmall(source, graph, descriptor, info, metadata, options);
        }
        int channels = info.GetChannelCount();
        long rowSamplesLong = CheckedMath.Multiply(info.Width, channels);
        long totalSamples = CheckedMath.Multiply(rowSamplesLong, info.Height);
        CheckedMath.DecodedSize(totalSamples, 2, options.Limits);
        CheckedMath.AllocationSize(totalSamples, 2, options.Limits);
        ushort[] stored = new ushort[CheckedMath.ToInt32(totalSamples)];
        DecodeLinearBlocks(source, graph, descriptor, profile, stored, info.Width, info.Height, channels, options);
        ushort[] normalized = Cr2LosslessJpegDecoder.Normalize(stored, info.BitDepth, options.Limits, options.CancellationToken);
        RawStoredImage? storedImage = CreateStoredImage(info, stored, CheckedMath.ToInt32(rowSamplesLong), options);
        RawBlackLevel? black = CreateBlackLevel(metadata, channels);
        return new RawSensorImage(info, normalized, null, CheckedMath.ToInt32(rowSamplesLong), true, storedImage, null, metadata.MaskedAreas, black, metadata.WhiteLevels, null, null, NefUncompressedDecoder.GetDiagnostics(metadata));
    }

    internal static NefVariantKind Validate(System.ReadOnlyMemory<byte> source, TiffIfdGraph graph, TiffImageDescriptor descriptor, RawImageInfo info, RawNikonNefMetadata metadata, RawReadProfile profile, RawDecodeOptions options)
    {
        if (options == null || !options.Validate().IsValid() || graph == null || graph.Header == null || descriptor == null || info == null || metadata == null || !CanDecode(profile, descriptor))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A complete explicit variant NEF decode profile is required.", -1);
        }
        NefVariantKind kind = GetKind(profile);
        int channels = kind == NefVariantKind.Monochrome ? 1 : kind == NefVariantKind.Linear ? CheckedMath.ToInt32(CheckedMath.ToInt64(descriptor.SamplesPerPixel)) : 3;
        if (source.Length == 0 || info.Width <= 0 || info.Height <= 0 || descriptor.Width != (ulong)info.Width || descriptor.Height != (ulong)info.Height || metadata.StoredWidth != info.Width || metadata.StoredHeight != info.Height || profile.Geometry.StoredWidth != info.Width || profile.Geometry.StoredHeight != info.Height || profile.Geometry.Cfa != null || info.Cfa != null || info.GetChannelCount() != channels)
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "The variant NEF geometry or non-CFA semantics disagree with the selected profile.", -1);
        }
        int precision = GetPrecision(descriptor);
        if (precision != 12 && precision != 14 && precision != 16 || precision != info.BitDepth || precision != profile.Fingerprint.BitDepth || profile.Codec.PlanarConfiguration != 1 || profile.Codec.Predictor != 0 || profile.Codec.RowAlignment <= 0)
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "Variant NEF decoding requires matching unsigned 12-, 14-, or 16-bit chunky samples.", -1);
        }
        if (descriptor.SampleFormats.Length != 0 && descriptor.SampleFormats[0] != TiffSampleFormat.UnsignedInteger)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "Variant NEF decoding requires unsigned integer samples.", -1);
        }
        ValidateSemantics(kind, descriptor);
        if (kind == NefVariantKind.SmallYCbCr && (precision != 12 || profile.Codec.StoredWordBits != 12 || profile.Codec.Packing != RawPacking.PackedBits))
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "Small NEF decoding requires its reviewed twelve-bit packed profile.", -1);
        }
        ValidateBlocks(descriptor, info.Height);
        if (kind != NefVariantKind.SmallYCbCr)
        {
            NefUncompressedDecoder.ValidatePacking(profile.Codec, precision);
            NefUncompressedDecoder.ValidateQuirks(profile.Codec);
        }
        return kind;
    }

    internal static void ValidateSemantics(NefVariantKind kind, TiffImageDescriptor descriptor)
    {
        bool valid = kind == NefVariantKind.Monochrome && descriptor.SamplesPerPixel == 1 && (descriptor.Photometric == RawPhotometricInterpretation.BlackIsZero || descriptor.Photometric == RawPhotometricInterpretation.WhiteIsZero);
        valid = valid || kind == NefVariantKind.Linear && descriptor.Photometric == RawPhotometricInterpretation.LinearRaw && (descriptor.SamplesPerPixel == 1 || descriptor.SamplesPerPixel == 3);
        valid = valid || kind == NefVariantKind.Rgb && descriptor.SamplesPerPixel == 3 && descriptor.Photometric == RawPhotometricInterpretation.Rgb;
        valid = valid || kind == NefVariantKind.YCbCr && descriptor.SamplesPerPixel == 3 && descriptor.Photometric == RawPhotometricInterpretation.YCbCr;
        valid = valid || kind == NefVariantKind.SmallYCbCr && descriptor.SamplesPerPixel == 3 && (descriptor.Photometric == RawPhotometricInterpretation.YCbCr || descriptor.Photometric == RawPhotometricInterpretation.LinearRaw);
        if (!valid)
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "The TIFF photometric interpretation disagrees with the explicit NEF variant profile.", -1);
        }
    }

    internal static void ValidateBlocks(TiffImageDescriptor descriptor, int height)
    {
        if (descriptor.RowsPerStrip == 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "Variant NEF strips require a positive RowsPerStrip value.", -1);
        }
        int expected = CheckedMath.ToInt32(CheckedMath.ToInt64(TiffImageData.CeilingDivide((ulong)height, descriptor.RowsPerStrip)));
        if (descriptor.Blocks.Length != expected)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "Variant NEF strips do not exactly cover the stored image.", -1);
        }
        int index = 0;
        while (index < descriptor.Blocks.Length)
        {
            TiffDataBlock block = descriptor.Blocks[index];
            if (block == null || block.Tiled || block.Index != index || block.Plane != 0 || block.IndexInPlane != index || block.ByteCount == 0)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "Variant NEF strips are not one complete ordered chunky plane.", -1);
            }
            index++;
        }
    }

    internal static void DecodeLinearBlocks(System.ReadOnlyMemory<byte> source, TiffIfdGraph graph, TiffImageDescriptor descriptor, RawReadProfile profile, ushort[] destination, int width, int height, int channels, RawDecodeOptions options)
    {
        int precision = GetPrecision(descriptor);
        int rowSamples = CheckedMath.ToInt32(CheckedMath.Multiply(width, channels));
        int rowBytes = NefUncompressedDecoder.GetRowBytes(rowSamples, profile.Codec, precision);
        int rowStride = NefUncompressedDecoder.Align(rowBytes, profile.Codec.RowAlignment);
        uint[] row = new uint[rowSamples];
        int blockIndex = 0;
        int imageRow = 0;
        while (blockIndex < descriptor.Blocks.Length)
        {
            TiffDataBlock block = descriptor.Blocks[blockIndex];
            int rows = CheckedMath.ToInt32(CheckedMath.ToInt64(descriptor.RowsPerStrip));
            if (rows > height - imageRow)
            {
                rows = height - imageRow;
            }
            bool blockBitstream = NefUncompressedDecoder.HasQuirk(profile.Codec, NefUncompressedDecoder.BlockBitstreamQuirk);
            long required = blockBitstream ? NefUncompressedDecoder.GetBlockBitstreamBytes(rowSamples, rows, profile.Codec, precision) : CheckedMath.Multiply(rowStride, rows);
            required = blockBitstream ? NefUncompressedDecoder.AlignLong(required, profile.Codec.RowAlignment) : required;
            if (block.ByteCount != (ulong)required)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidLength, "A variant NEF strip byte count does not match its profiled row layout.", CheckedMath.ToInt64(block.Offset));
            }
            long offset = RawCodec.RelativeTiffOffset(block.Offset, graph);
            long end = CheckedMath.RangeEnd(offset, required, source.Length);
            System.ReadOnlySpan<byte> bytes = source.Span.Slice(CheckedMath.ToInt32(offset), CheckedMath.ToInt32(end - offset));
            if (blockBitstream)
            {
                int sampleCount = CheckedMath.ToInt32(CheckedMath.Multiply(rowSamples, rows));
                int packedBytes = BitPacking.GetPackedByteCount(sampleCount, precision);
                System.ReadOnlySpan<byte> packed = bytes.Slice(0, packedBytes);
                RawBitOrder bitOrder = NefUncompressedDecoder.GetBitOrder(profile.Codec, descriptor.FillOrder);
                BitPacking.ValidateGenericPadding(packed, sampleCount, precision, bitOrder);
                int sampleIndex = 0;
                while (sampleIndex < sampleCount)
                {
                    uint value = BitPacking.ReadSampleBits(packed, CheckedMath.Multiply(sampleIndex, precision), precision, bitOrder);
                    destination[imageRow * rowSamples + sampleIndex] = NefUncompressedDecoder.ToUShort(value, precision);
                    sampleIndex++;
                }
                imageRow += rows;
                blockIndex++;
                continue;
            }
            int localRow = 0;
            while (localRow < rows)
            {
                options.CheckCancellation();
                NefUncompressedDecoder.DecodeRow(bytes.Slice(localRow * rowStride, rowBytes), row, profile.Codec, precision, graph.Header.ByteOrder, descriptor.FillOrder);
                int index = 0;
                int destinationOffset = (imageRow + localRow) * rowSamples;
                while (index < rowSamples)
                {
                    destination[destinationOffset + index] = NefUncompressedDecoder.ToUShort(row[index], precision);
                    index++;
                }
                localRow++;
            }
            imageRow += rows;
            blockIndex++;
        }
    }

    internal static RawSensorImage DecodeSmall(System.ReadOnlyMemory<byte> source, TiffIfdGraph graph, TiffImageDescriptor descriptor, RawImageInfo info, RawNikonNefMetadata metadata, RawDecodeOptions options)
    {
        if (info.BitDepth != 12 || (info.Width & 1) != 0 || options.PreserveStoredSamples)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "Small NEF requires even-width 12-bit output and cannot expose a full-resolution exact stored buffer.", -1);
        }
        int rowBytes = CheckedMath.ToInt32(CheckedMath.Multiply(info.Width, 3));
        int rowStride = rowBytes;
        long totalSamples = CheckedMath.Multiply(CheckedMath.PixelCount(info.Width, info.Height, options.Limits), 3);
        CheckedMath.DecodedSize(totalSamples, 2, options.Limits);
        CheckedMath.AllocationSize(totalSamples, 2, options.Limits);
        ushort[] samples = new ushort[CheckedMath.ToInt32(totalSamples)];
        int imageRow = 0;
        int blockIndex = 0;
        while (blockIndex < descriptor.Blocks.Length)
        {
            TiffDataBlock block = descriptor.Blocks[blockIndex];
            int rows = CheckedMath.ToInt32(CheckedMath.ToInt64(descriptor.RowsPerStrip));
            if (rows > info.Height - imageRow)
            {
                rows = info.Height - imageRow;
            }
            long required = CheckedMath.Multiply(rowStride, rows);
            if (block.ByteCount != (ulong)required)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidLength, "A small NEF strip must contain exactly three packed bytes per pixel.", CheckedMath.ToInt64(block.Offset));
            }
            long offset = RawCodec.RelativeTiffOffset(block.Offset, graph);
            long end = CheckedMath.RangeEnd(offset, required, source.Length);
            System.ReadOnlySpan<byte> bytes = source.Span.Slice(CheckedMath.ToInt32(offset), CheckedMath.ToInt32(end - offset));
            int localRow = 0;
            while (localRow < rows)
            {
                options.CheckCancellation();
                DecodeSmallRow(bytes.Slice(localRow * rowStride, rowBytes), samples, (imageRow + localRow) * info.Width * 3, info.Width);
                localRow++;
            }
            imageRow += rows;
            blockIndex++;
        }
        ushort[] normalized = Cr2LosslessJpegDecoder.Normalize(samples, 12, options.Limits, options.CancellationToken);
        RawBlackLevel? black = CreateBlackLevel(metadata, 3);
        return new RawSensorImage(info, normalized, null, info.Width * 3, true, null, null, metadata.MaskedAreas, black, metadata.WhiteLevels, null, null, NefUncompressedDecoder.GetDiagnostics(metadata));
    }

    internal static void DecodeSmallRow(System.ReadOnlySpan<byte> source, ushort[] destination, int destinationOffset, int width)
    {
        int column = 0;
        while (column < width)
        {
            int sourceOffset = column * 3;
            int firstOffset = destinationOffset + column * 3;
            int secondOffset = firstOffset + 3;
            destination[firstOffset] = (ushort)(source[sourceOffset] | (source[sourceOffset + 1] & 15) << 8);
            destination[secondOffset] = (ushort)(source[sourceOffset + 2] << 4 | source[sourceOffset + 1] >> 4);
            destination[firstOffset + 1] = (ushort)(source[sourceOffset + 3] | (source[sourceOffset + 4] & 15) << 8);
            destination[firstOffset + 2] = (ushort)(source[sourceOffset + 5] << 4 | source[sourceOffset + 4] >> 4);
            destination[secondOffset + 1] = destination[firstOffset + 1];
            destination[secondOffset + 2] = destination[firstOffset + 2];
            column += 2;
        }
        column = 1;
        while (column < width)
        {
            int current = destinationOffset + (column - 1) * 3;
            int nextColumn = column + 1 < width ? column + 1 : column - 1;
            int next = destinationOffset + nextColumn * 3;
            int output = destinationOffset + column * 3;
            destination[output + 1] = (ushort)((destination[current + 1] + destination[next + 1]) >> 1);
            destination[output + 2] = (ushort)((destination[current + 2] + destination[next + 2]) >> 1);
            column += 2;
        }
    }

    internal static RawStoredImage? CreateStoredImage(RawImageInfo info, ushort[] samples, int rowStride, RawDecodeOptions options)
    {
        if (!options.PreserveStoredSamples)
        {
            return null;
        }
        CheckedMath.AllocationSize(samples.Length, 4, options.Limits);
        uint[] exact = new uint[samples.Length];
        int index = 0;
        while (index < samples.Length)
        {
            exact[index] = samples[index];
            index++;
        }
        return new RawStoredImage(info, exact, null, null, rowStride, true, null);
    }

    internal static RawBlackLevel? CreateBlackLevel(RawNikonNefMetadata metadata, int channels)
    {
        if (metadata == null || metadata.BlackLevels == null || metadata.BlackLevels.Length == 0)
        {
            return null;
        }
        if (metadata.BlackLevels.Length == 1)
        {
            return RawBlackLevel.CreateScalar(metadata.BlackLevels[0], channels);
        }
        if (metadata.BlackLevels.Length == channels && metadata.BlackLevelRepeatWidth == 1 && metadata.BlackLevelRepeatHeight == 1)
        {
            double[] values = new double[channels];
            System.Array.Copy(metadata.BlackLevels, values, channels);
            return RawBlackLevel.CreatePerChannel(values);
        }
        throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "A non-CFA NEF black level must be scalar or contain one value per channel.", -1);
    }

    internal static int GetPrecision(TiffImageDescriptor descriptor)
    {
        if (descriptor == null || descriptor.BitsPerSample == null || descriptor.BitsPerSample.Length == 0 || descriptor.BitsPerSample.Length != 1 && descriptor.BitsPerSample.Length != (int)descriptor.SamplesPerPixel || descriptor.BitsPerSample[0] > int.MaxValue)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "Variant NEF decoding requires one precision or one matching value per channel.", descriptor == null || descriptor.Ifd == null ? -1 : descriptor.Ifd.AbsoluteOffset);
        }
        ulong precision = descriptor.BitsPerSample[0];
        int index = 1;
        while (index < descriptor.BitsPerSample.Length)
        {
            if (descriptor.BitsPerSample[index] != precision)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "Variant NEF channel precisions must match.", descriptor.Ifd == null ? -1 : descriptor.Ifd.AbsoluteOffset);
            }
            index++;
        }
        return (int)precision;
    }

    internal static RawChannelDescription[] CreateChannels(NefVariantKind kind, int count)
    {
        if (kind == NefVariantKind.Monochrome)
        {
            return new RawChannelDescription[] { new RawChannelDescription("Gray", RawChannelKind.Gray) };
        }
        if (kind == NefVariantKind.Rgb)
        {
            return new RawChannelDescription[] { new RawChannelDescription("R", RawChannelKind.Red), new RawChannelDescription("G", RawChannelKind.Green), new RawChannelDescription("B", RawChannelKind.Blue) };
        }
        if (kind == NefVariantKind.YCbCr || kind == NefVariantKind.SmallYCbCr)
        {
            return new RawChannelDescription[] { new RawChannelDescription("Y", RawChannelKind.Gray), new RawChannelDescription("Cb", RawChannelKind.Custom), new RawChannelDescription("Cr", RawChannelKind.Custom) };
        }
        RawChannelDescription[] channels = new RawChannelDescription[count];
        int index = 0;
        while (index < count)
        {
            channels[index] = new RawChannelDescription("Linear" + index, RawChannelKind.Custom);
            index++;
        }
        return channels;
    }

    internal static RawPlaneInfo[] CreatePlanes(RawImageInfo image, RawChannelDescription[] channels)
    {
        RawPlaneInfo[] planes = new RawPlaneInfo[channels.Length];
        int index = 0;
        while (index < planes.Length)
        {
            planes[index] = new RawPlaneInfo(index, channels[index].Name, channels[index].Kind, image.Width, image.Height, image.BitDepth, image.Width);
            index++;
        }
        return planes;
    }
}
