namespace raw;

internal static class NefUncompressedDecoder
{
    internal const string CodecName = "nikon-nef-uncompressed";
    internal const string PackedMostSignificantBitFirstQuirk = "nikon-nef-packed-msb-first";
    internal const string PackedLeastSignificantBitFirstQuirk = "nikon-nef-packed-lsb-first";
    internal const string WordLittleEndianQuirk = "nikon-nef-word-little-endian";
    internal const string WordBigEndianQuirk = "nikon-nef-word-big-endian";
    internal const string LeftAlignedWordQuirk = "nikon-nef-word-left-aligned";
    internal const string BlockBitstreamQuirk = "nikon-nef-packed-block-bitstream";
    internal const string TrimmedEdgeTilesQuirk = "nikon-nef-trimmed-edge-tiles";

    internal static bool IsProfile(RawReadProfile? profile)
    {
        return profile != null && profile.Identity != null && profile.Identity.Format == RawFormat.Nef && profile.Codec != null && profile.Codec.Name == CodecName && profile.Codec.Compression == 1;
    }

    internal static bool CanDecode(RawReadProfile profile, TiffImageDescriptor descriptor)
    {
        return IsProfile(profile) && descriptor != null && descriptor.CompressionCode == 1 && descriptor.SamplesPerPixel == 1 && descriptor.PlanarConfiguration == TiffPlanarConfiguration.Chunky && descriptor.Predictor == TiffPredictor.None && descriptor.StorageKind != TiffStorageKind.None && descriptor.StorageKind != TiffStorageKind.Mixed;
    }

    internal static RawSensorImage Decode(RawDocument document, RawImageInfo info, RawDecodeOptions options)
    {
        if (document == null || document.TiffGraph == null || document.FormatDetails == null || document.FormatDetails.Nikon == null || document.ReadProfile == null)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedProfile, "Uncompressed NEF decoding requires retained profile-qualified TIFF metadata.", -1);
        }
        TiffImageDescriptor descriptor = FindDescriptor(document.TiffGraph, info);
        RawSensorImage image = Decode(document.Source, document.TiffGraph, descriptor, info, document.FormatDetails.Nikon, document.ReadProfile, options);
        image.Diagnostics = document.Diagnostics;
        return image;
    }

    internal static RawSensorImage Decode(System.ReadOnlyMemory<byte> source, TiffIfdGraph graph, TiffImageDescriptor descriptor, RawImageInfo info, RawNikonNefMetadata metadata, RawReadProfile profile, RawDecodeOptions options)
    {
        Validate(source, graph, descriptor, info, metadata, profile, options);
        long pixelCount = CheckedMath.PixelCount(info.Width, info.Height, options.Limits);
        CheckedMath.DecodedSize(pixelCount, 2, options.Limits);
        CheckedMath.AllocationSize(pixelCount, 2, options.Limits);
        ushort[] storedSamples = new ushort[CheckedMath.ToInt32(pixelCount)];
        int blockIndex = 0;
        while (blockIndex < descriptor.Blocks.Length)
        {
            options.CheckCancellation();
            DecodeBlock(source, graph, descriptor, profile, blockIndex, storedSamples, info.Width, info.Height, options);
            blockIndex++;
        }
        ushort[] normalized = Cr2LosslessJpegDecoder.Normalize(storedSamples, info.BitDepth, options.Limits, options.CancellationToken);
        RawStoredImage? stored = CreateStoredImage(info, storedSamples, options);
        RawBlackLevel? black = CreateBlackLevel(metadata);
        return new RawSensorImage(info, normalized, null, info.Width, true, stored, null, metadata.MaskedAreas, black, metadata.WhiteLevels, null, null, GetDiagnostics(metadata));
    }

    internal static RawDiagnostic[] GetDiagnostics(RawNikonNefMetadata metadata)
    {
        if (metadata == null || metadata.Diagnostics == null)
        {
            return System.Array.Empty<RawDiagnostic>();
        }
        return metadata.Diagnostics;
    }

    internal static RawBlackLevel? CreateBlackLevel(RawNikonNefMetadata metadata)
    {
        if (metadata == null || metadata.BlackLevels == null || metadata.BlackLevels.Length == 0)
        {
            return null;
        }
        long expected = CheckedMath.Multiply(metadata.BlackLevelRepeatWidth, metadata.BlackLevelRepeatHeight);
        if (metadata.BlackLevelRepeatWidth <= 0 || metadata.BlackLevelRepeatHeight <= 0 || expected != metadata.BlackLevels.Length)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "Nikon black-level values do not match their repeat geometry.", -1);
        }
        double[] values = new double[metadata.BlackLevels.Length];
        System.Array.Copy(metadata.BlackLevels, values, values.Length);
        return new RawBlackLevel(metadata.BlackLevelRepeatWidth, metadata.BlackLevelRepeatHeight, 1, values, null, null);
    }

    internal static RawStoredImage? CreateStoredImage(RawImageInfo info, ushort[] samples, RawDecodeOptions options)
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
            if ((index & 65535) == 0)
            {
                options.CheckCancellation();
            }
            exact[index] = samples[index];
            index++;
        }
        return new RawStoredImage(info, exact, null, null, info.Width, true, null);
    }

    internal static void Validate(System.ReadOnlyMemory<byte> source, TiffIfdGraph graph, TiffImageDescriptor descriptor, RawImageInfo info, RawNikonNefMetadata metadata, RawReadProfile profile, RawDecodeOptions options)
    {
        if (options == null || !options.Validate().IsValid() || graph == null || graph.Header == null || descriptor == null || info == null || metadata == null || !CanDecode(profile, descriptor))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A complete profile-qualified uncompressed NEF decode request is required.", -1);
        }
        if (source.Length == 0 || info.Width <= 0 || info.Height <= 0 || descriptor.Width != (ulong)info.Width || descriptor.Height != (ulong)info.Height || metadata.StoredWidth != info.Width || metadata.StoredHeight != info.Height || metadata.Compression != RawCompression.None || profile.Geometry.StoredWidth != info.Width || profile.Geometry.StoredHeight != info.Height)
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "The uncompressed NEF image geometry does not match its selected profile.", -1);
        }
        int precision = GetPrecision(descriptor);
        if (precision != 12 && precision != 14 && precision != 16 || profile.Fingerprint.BitDepth != precision || info.BitDepth != precision)
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "Uncompressed NEF decoding requires matching 12-, 14-, or 16-bit precision.", descriptor.Ifd == null ? -1 : descriptor.Ifd.AbsoluteOffset);
        }
        if (descriptor.SampleFormats.Length != 0 && descriptor.SampleFormats[0] != TiffSampleFormat.UnsignedInteger || profile.Codec.PlanarConfiguration != 1 || profile.Codec.Predictor != 0 || profile.Codec.RowAlignment <= 0)
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "The uncompressed NEF sample representation disagrees with its selected profile.", descriptor.Ifd == null ? -1 : descriptor.Ifd.AbsoluteOffset);
        }
        ValidatePacking(profile.Codec, precision);
        ValidateQuirks(profile.Codec);
        ValidateBlocks(descriptor, info.Width, info.Height);
    }

    internal static void ValidatePacking(RawProfileCodec codec, int precision)
    {
        if (codec.Packing == RawPacking.ByteAligned)
        {
            if (codec.StoredWordBits != 16)
            {
                throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "Unpacked NEF samples must use sixteen-bit stored words.", -1);
            }
            return;
        }
        if (codec.Packing == RawPacking.PackedBits && codec.StoredWordBits == precision)
        {
            return;
        }
        if (codec.Packing == RawPacking.Raw12 && precision == 12 && codec.StoredWordBits == 12 || codec.Packing == RawPacking.Raw14 && precision == 14 && codec.StoredWordBits == 14)
        {
            return;
        }
        throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "The selected NEF profile declares an unsupported packed-sample layout.", -1);
    }

    internal static void ValidateQuirks(RawProfileCodec codec)
    {
        if (HasQuirk(codec, PackedMostSignificantBitFirstQuirk) && HasQuirk(codec, PackedLeastSignificantBitFirstQuirk) || HasQuirk(codec, WordLittleEndianQuirk) && HasQuirk(codec, WordBigEndianQuirk))
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "The selected NEF profile contains conflicting byte or bit order quirks.", -1);
        }
        if (codec.Packing == RawPacking.ByteAligned && HasQuirk(codec, BlockBitstreamQuirk) || codec.Packing != RawPacking.ByteAligned && HasQuirk(codec, LeftAlignedWordQuirk))
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "The selected NEF profile applies a layout quirk to an incompatible packing.", -1);
        }
    }

    internal static void ValidateBlocks(TiffImageDescriptor descriptor, int width, int height)
    {
        int expected;
        if (descriptor.StorageKind == TiffStorageKind.Strips)
        {
            if (descriptor.RowsPerStrip == 0)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "Uncompressed NEF strips require a positive RowsPerStrip value.", -1);
            }
            expected = CheckedMath.ToInt32(CheckedMath.ToInt64(TiffImageData.CeilingDivide((ulong)height, descriptor.RowsPerStrip)));
        }
        else
        {
            if (descriptor.TileWidth == 0 || descriptor.TileHeight == 0)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "Uncompressed NEF tiles require positive tile geometry.", -1);
            }
            ulong across = TiffImageData.CeilingDivide((ulong)width, descriptor.TileWidth);
            ulong down = TiffImageData.CeilingDivide((ulong)height, descriptor.TileHeight);
            expected = CheckedMath.ToInt32(CheckedMath.Multiply(CheckedMath.ToInt64(across), CheckedMath.ToInt64(down)));
        }
        if (descriptor.Blocks == null || descriptor.Blocks.Length != expected)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The uncompressed NEF block count does not exactly cover the stored image.", descriptor.Ifd == null ? -1 : descriptor.Ifd.AbsoluteOffset);
        }
        int index = 0;
        while (index < descriptor.Blocks.Length)
        {
            TiffDataBlock block = descriptor.Blocks[index];
            bool tiled = descriptor.StorageKind == TiffStorageKind.Tiles;
            if (block == null || block.Tiled != tiled || block.Plane != 0 || block.IndexInPlane != index || block.ByteCount == 0)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "The uncompressed NEF blocks are not in one complete ordered plane.", descriptor.Ifd == null ? -1 : descriptor.Ifd.AbsoluteOffset);
            }
            index++;
        }
    }

    internal static void DecodeBlock(System.ReadOnlyMemory<byte> source, TiffIfdGraph graph, TiffImageDescriptor descriptor, RawReadProfile profile, int blockIndex, ushort[] destination, int imageWidth, int imageHeight, RawDecodeOptions options)
    {
        TiffDataBlock block = descriptor.Blocks[blockIndex];
        NefBlockGeometry geometry = GetBlockGeometry(descriptor, profile.Codec, blockIndex, imageWidth, imageHeight);
        int rowBytes = GetRowBytes(geometry.StoredWidth, profile.Codec, GetPrecision(descriptor));
        int rowStride = Align(rowBytes, profile.Codec.RowAlignment);
        bool blockBitstream = HasQuirk(profile.Codec, BlockBitstreamQuirk);
        long required = blockBitstream ? GetBlockBitstreamBytes(geometry.StoredWidth, geometry.StoredHeight, profile.Codec, GetPrecision(descriptor)) : CheckedMath.Multiply(rowStride, geometry.StoredHeight);
        required = blockBitstream ? AlignLong(required, profile.Codec.RowAlignment) : required;
        if (block.ByteCount != (ulong)required)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "An uncompressed NEF strip or tile byte count does not match its profile row layout.", CheckedMath.ToInt64(block.Offset));
        }
        long offset = RawCodec.RelativeTiffOffset(block.Offset, graph);
        long end = CheckedMath.RangeEnd(offset, required, source.Length);
        System.ReadOnlySpan<byte> bytes = source.Span.Slice(CheckedMath.ToInt32(offset), CheckedMath.ToInt32(end - offset));
        if (blockBitstream)
        {
            DecodeBlockBitstream(bytes, geometry, profile.Codec, GetPrecision(descriptor), descriptor.FillOrder, destination, imageWidth, imageHeight, options);
        }
        else
        {
            DecodeRows(bytes, geometry, profile.Codec, GetPrecision(descriptor), rowBytes, rowStride, destination, imageWidth, imageHeight, graph.Header.ByteOrder, descriptor.FillOrder, options);
        }
    }

    internal static void DecodeRows(System.ReadOnlySpan<byte> source, NefBlockGeometry geometry, RawProfileCodec codec, int precision, int rowBytes, int rowStride, ushort[] destination, int imageWidth, int imageHeight, RawByteOrder tiffByteOrder, ulong fillOrder, RawDecodeOptions options)
    {
        CheckedMath.AllocationSize(geometry.StoredWidth, 4, options.Limits);
        uint[] rowSamples = new uint[geometry.StoredWidth];
        int row = 0;
        while (row < geometry.StoredHeight)
        {
            options.CheckCancellation();
            System.ReadOnlySpan<byte> bytes = source.Slice(row * rowStride, rowBytes);
            DecodeRow(bytes, rowSamples, codec, precision, tiffByteOrder, fillOrder);
            int imageY = geometry.Y + row;
            if (imageY < imageHeight)
            {
                int copyWidth = geometry.ValidWidth;
                int sourceIndex = 0;
                int destinationIndex = imageY * imageWidth + geometry.X;
                while (sourceIndex < copyWidth)
                {
                    destination[destinationIndex + sourceIndex] = ToUShort(rowSamples[sourceIndex], precision);
                    sourceIndex++;
                }
            }
            row++;
        }
    }

    internal static void DecodeBlockBitstream(System.ReadOnlySpan<byte> source, NefBlockGeometry geometry, RawProfileCodec codec, int precision, ulong fillOrder, ushort[] destination, int imageWidth, int imageHeight, RawDecodeOptions options)
    {
        long sampleCount = CheckedMath.Multiply(geometry.StoredWidth, geometry.StoredHeight);
        int packedBytes = BitPacking.GetPackedByteCount(CheckedMath.ToInt32(sampleCount), precision);
        System.ReadOnlySpan<byte> packed = source.Slice(0, packedBytes);
        RawBitOrder bitOrder = GetBitOrder(codec, fillOrder);
        BitPacking.ValidateGenericPadding(packed, CheckedMath.ToInt32(sampleCount), precision, bitOrder);
        int row = 0;
        while (row < geometry.StoredHeight)
        {
            options.CheckCancellation();
            int imageY = geometry.Y + row;
            int column = 0;
            while (column < geometry.ValidWidth && imageY < imageHeight)
            {
                long sampleIndex = CheckedMath.Add(CheckedMath.Multiply(row, geometry.StoredWidth), column);
                uint value = BitPacking.ReadSampleBits(packed, CheckedMath.Multiply(sampleIndex, precision), precision, bitOrder);
                destination[imageY * imageWidth + geometry.X + column] = ToUShort(value, precision);
                column++;
            }
            row++;
        }
    }

    internal static void DecodeRow(System.ReadOnlySpan<byte> source, uint[] destination, RawProfileCodec codec, int precision, RawByteOrder tiffByteOrder, ulong fillOrder)
    {
        if (codec.Packing == RawPacking.ByteAligned)
        {
            RawByteOrder byteOrder = GetByteOrder(codec, tiffByteOrder);
            int index = 0;
            while (index < destination.Length)
            {
                ushort word = EndianReader.ReadUInt16(source.Slice(index * 2, 2), byteOrder);
                uint value = word;
                if (HasQuirk(codec, LeftAlignedWordQuirk) && precision < 16)
                {
                    int lowBits = 16 - precision;
                    uint lowMask = (1U << lowBits) - 1U;
                    if ((value & lowMask) != 0)
                    {
                        throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A left-aligned unpacked NEF word contains nonzero padding bits.", index * 2);
                    }
                    value >>= lowBits;
                }
                destination[index] = RequirePrecision(value, precision, index * 2);
                index++;
            }
            return;
        }
        if (codec.Packing == RawPacking.PackedBits)
        {
            BitPacking.UnpackUnsigned(source, destination.Length, precision, GetBitOrder(codec, fillOrder), destination);
            return;
        }
        BitPacking.UnpackNamed(source, destination.Length, codec.Packing, destination);
    }

    internal static NefBlockGeometry GetBlockGeometry(TiffImageDescriptor descriptor, RawProfileCodec codec, int blockIndex, int imageWidth, int imageHeight)
    {
        if (descriptor.StorageKind == TiffStorageKind.Strips)
        {
            int stripY = CheckedMath.ToInt32(CheckedMath.Multiply(blockIndex, CheckedMath.ToInt64(descriptor.RowsPerStrip)));
            int stripHeight = CheckedMath.ToInt32(CheckedMath.ToInt64(descriptor.RowsPerStrip));
            if (stripHeight > imageHeight - stripY)
            {
                stripHeight = imageHeight - stripY;
            }
            return new NefBlockGeometry(0, stripY, imageWidth, stripHeight, imageWidth, stripHeight);
        }
        int tilesAcross = CheckedMath.ToInt32(CheckedMath.ToInt64(TiffImageData.CeilingDivide((ulong)imageWidth, descriptor.TileWidth)));
        int tileWidth = CheckedMath.ToInt32(CheckedMath.ToInt64(descriptor.TileWidth));
        int tileHeight = CheckedMath.ToInt32(CheckedMath.ToInt64(descriptor.TileHeight));
        int x = (blockIndex % tilesAcross) * tileWidth;
        int y = (blockIndex / tilesAcross) * tileHeight;
        int validWidth = tileWidth > imageWidth - x ? imageWidth - x : tileWidth;
        int validHeight = tileHeight > imageHeight - y ? imageHeight - y : tileHeight;
        int storedWidth = HasQuirk(codec, TrimmedEdgeTilesQuirk) ? validWidth : tileWidth;
        int storedHeight = HasQuirk(codec, TrimmedEdgeTilesQuirk) ? validHeight : tileHeight;
        return new NefBlockGeometry(x, y, storedWidth, storedHeight, validWidth, validHeight);
    }

    internal static int GetRowBytes(int sampleCount, RawProfileCodec codec, int precision)
    {
        if (codec.Packing == RawPacking.ByteAligned)
        {
            return CheckedMath.ToInt32(CheckedMath.Multiply(sampleCount, 2));
        }
        if (codec.Packing == RawPacking.PackedBits)
        {
            return BitPacking.GetPackedByteCount(sampleCount, precision);
        }
        return BitPacking.GetNamedPackedByteCount(sampleCount, codec.Packing);
    }

    internal static long GetBlockBitstreamBytes(int width, int height, RawProfileCodec codec, int precision)
    {
        if (codec.Packing != RawPacking.PackedBits)
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "A continuous NEF block bitstream requires generic packed-bit layout.", -1);
        }
        long bits = CheckedMath.Multiply(CheckedMath.Multiply(width, height), precision);
        return (bits + 7) / 8;
    }

    internal static int Align(int value, int alignment)
    {
        return CheckedMath.ToInt32(AlignLong(value, alignment));
    }

    internal static long AlignLong(long value, int alignment)
    {
        if (alignment <= 0)
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "NEF row alignment must be positive.", -1);
        }
        long remainder = value % alignment;
        return remainder == 0 ? value : CheckedMath.Add(value, alignment - remainder);
    }

    internal static int GetPrecision(TiffImageDescriptor descriptor)
    {
        if (descriptor.BitsPerSample == null || descriptor.BitsPerSample.Length != 1 || descriptor.BitsPerSample[0] > int.MaxValue)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "Uncompressed NEF decoding requires one integer BitsPerSample value.", descriptor.Ifd == null ? -1 : descriptor.Ifd.AbsoluteOffset);
        }
        return (int)descriptor.BitsPerSample[0];
    }

    internal static RawBitOrder GetBitOrder(RawProfileCodec codec, ulong fillOrder)
    {
        if (HasQuirk(codec, PackedMostSignificantBitFirstQuirk))
        {
            return RawBitOrder.MostSignificantBitFirst;
        }
        if (HasQuirk(codec, PackedLeastSignificantBitFirstQuirk))
        {
            return RawBitOrder.LeastSignificantBitFirst;
        }
        if (fillOrder == 1)
        {
            return RawBitOrder.MostSignificantBitFirst;
        }
        if (fillOrder == 2)
        {
            return RawBitOrder.LeastSignificantBitFirst;
        }
        throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "The uncompressed NEF FillOrder is not supported by its profile.", -1);
    }

    internal static RawByteOrder GetByteOrder(RawProfileCodec codec, RawByteOrder tiffByteOrder)
    {
        if (HasQuirk(codec, WordLittleEndianQuirk))
        {
            return RawByteOrder.LittleEndian;
        }
        if (HasQuirk(codec, WordBigEndianQuirk))
        {
            return RawByteOrder.BigEndian;
        }
        return tiffByteOrder;
    }

    internal static uint RequirePrecision(uint value, int precision, int offset)
    {
        uint maximum = precision == 16 ? ushort.MaxValue : (1U << precision) - 1U;
        if (value > maximum)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "An unpacked NEF sample exceeds its declared precision.", offset);
        }
        return value;
    }

    internal static ushort ToUShort(uint value, int precision)
    {
        return (ushort)RequirePrecision(value, precision, -1);
    }

    internal static bool HasQuirk(RawProfileCodec codec, string quirk)
    {
        if (codec == null || codec.Quirks == null)
        {
            return false;
        }
        int index = 0;
        while (index < codec.Quirks.Length)
        {
            if (codec.Quirks[index] == quirk)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static TiffImageDescriptor FindDescriptor(TiffIfdGraph graph, RawImageInfo info)
    {
        int index = 0;
        while (index < graph.Images.Length)
        {
            TiffImageDescriptor descriptor = graph.Images[index];
            if (descriptor != null && descriptor.ImageIndex == info.Index)
            {
                return descriptor;
            }
            index++;
        }
        throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "The selected NEF raw image has no TIFF descriptor.", -1);
    }
}

internal class NefBlockGeometry
{
    internal int X;
    internal int Y;
    internal int StoredWidth;
    internal int StoredHeight;
    internal int ValidWidth;
    internal int ValidHeight;

    internal NefBlockGeometry(int x, int y, int storedWidth, int storedHeight, int validWidth, int validHeight)
    {
        X = x;
        Y = y;
        StoredWidth = storedWidth;
        StoredHeight = storedHeight;
        ValidWidth = validWidth;
        ValidHeight = validHeight;
    }
}
