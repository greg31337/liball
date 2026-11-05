using System;

namespace raw;

internal class NikonCompressionInfo
{
    internal int Precision;
    internal bool Lossless;
    internal int TreeIndex;
    internal int SplitRow;
    internal int[] Predictors;
    internal ushort[] Curve;

    internal NikonCompressionInfo()
    {
        Predictors = new int[4];
        Curve = System.Array.Empty<ushort>();
    }
}

internal class NikonLosslessEncoding
{
    internal byte[] EntropyBytes;
    internal byte[] CompressionInfo;

    internal NikonLosslessEncoding(byte[] entropyBytes, byte[] compressionInfo)
    {
        EntropyBytes = entropyBytes;
        CompressionInfo = compressionInfo;
    }
}

internal class NikonLossyEncoding
{
    internal byte[] EntropyBytes;
    internal byte[] CompressionInfo;
    internal int MaximumError;

    internal NikonLossyEncoding(byte[] entropyBytes, byte[] compressionInfo, int maximumError)
    {
        EntropyBytes = entropyBytes;
        CompressionInfo = compressionInfo;
        MaximumError = maximumError;
    }
}

internal class NikonPrefixTable
{
    internal int[] Counts;
    internal byte[] Symbols;
    internal int[] FirstCodes;
    internal int[] FirstIndexes;
    internal int MaximumLength;

    internal NikonPrefixTable(byte[] counts, byte[] symbols)
    {
        Counts = new int[17];
        Symbols = symbols;
        FirstCodes = new int[17];
        FirstIndexes = new int[17];
        int available = 1;
        int code = 0;
        int symbolIndex = 0;
        int length = 1;
        while (length <= 16)
        {
            int count = counts[length - 1];
            Counts[length] = count;
            available = available * 2 - count;
            if (available < 0)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A Nikon prefix tree is oversubscribed.", -1);
            }
            code <<= 1;
            FirstCodes[length] = code;
            FirstIndexes[length] = symbolIndex;
            if (count != 0)
            {
                MaximumLength = length;
            }
            code += count;
            symbolIndex += count;
            length++;
        }
        if (symbolIndex == 0 || symbolIndex != symbols.Length)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A Nikon prefix tree has an invalid symbol count.", -1);
        }
    }

    internal byte Decode(MsbBitReader reader)
    {
        int code = 0;
        int length = 1;
        while (length <= MaximumLength)
        {
            code = (code << 1) | (int)reader.ReadBits(1);
            int offset = code - FirstCodes[length];
            if (offset >= 0 && offset < Counts[length])
            {
                return Symbols[FirstIndexes[length] + offset];
            }
            length++;
        }
        throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "The compressed NEF bit sequence does not identify a prefix symbol.", reader.Core.GetCurrentByteOffset());
    }
}

internal static class NikonCompressionCodec
{
    internal const string LosslessCodecName = "nikon-nef-tiff-34713-lossless";
    internal const string LossyCodecName = "nikon-nef-tiff-34713-lossy";
    internal const string LossyTableQuirk = "nikon-lossy-huffman-tree-3-no-split";
    internal const string LossyCurveQuirk = "nikon-lossy-explicit-two-to-one-curve";
    internal const string LossyQuantizationQuirk = "nikon-lossy-round-half-up-maximum-error-1";
    internal const int LossyMaximumError = 1;

    internal static bool IsProfile(RawReadProfile? profile)
    {
        return profile != null && profile.Identity != null && profile.Identity.Format == RawFormat.Nef && profile.Codec != null && profile.Codec.Compression == 34713 && (profile.Codec.Name == LosslessCodecName || profile.Codec.Name == LossyCodecName);
    }

    internal static bool CanDecode(RawReadProfile profile, TiffImageDescriptor descriptor, RawNikonNefMetadata metadata)
    {
        return IsProfile(profile) && descriptor != null && metadata != null && metadata.CompressionInfo != null && metadata.CompressionInfo.Length != 0 && descriptor.CompressionCode == 34713 && descriptor.SamplesPerPixel == 1 && descriptor.PlanarConfiguration == TiffPlanarConfiguration.Chunky && descriptor.Predictor == TiffPredictor.None && descriptor.StorageKind == TiffStorageKind.Strips && descriptor.Blocks != null && descriptor.Blocks.Length == 1;
    }

    internal static RawSensorImage Decode(RawDocument document, RawImageInfo info, RawDecodeOptions options)
    {
        if (document == null || document.TiffGraph == null || document.FormatDetails == null || document.FormatDetails.Nikon == null || document.ReadProfile == null)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedProfile, "Compressed NEF decoding requires retained profile-qualified TIFF metadata.", -1);
        }
        TiffImageDescriptor descriptor = NefUncompressedDecoder.FindDescriptor(document.TiffGraph, info);
        RawSensorImage image = Decode(document.Source, document.TiffGraph, descriptor, info, document.FormatDetails.Nikon, document.ReadProfile, options);
        image.Diagnostics = document.Diagnostics;
        return image;
    }

    internal static RawSensorImage Decode(System.ReadOnlyMemory<byte> source, TiffIfdGraph graph, TiffImageDescriptor descriptor, RawImageInfo info, RawNikonNefMetadata metadata, RawReadProfile profile, RawDecodeOptions options)
    {
        Validate(source, graph, descriptor, info, metadata, profile, options);
        NikonCompressionInfo compression = ParseInfo(metadata.CompressionInfo, metadata.CompressionInfoByteOrder, info.BitDepth, info.Height);
        bool profileLossless = profile.Codec.Name == LosslessCodecName;
        if (compression.Lossless != profileLossless || profileLossless && metadata.Compression != RawCompression.NikonLossless || !profileLossless && metadata.Compression != RawCompression.NikonLossy)
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "The Nikon compression parameters disagree with the selected lossless or lossy profile.", -1);
        }
        long count = CheckedMath.PixelCount(info.Width, info.Height, options.Limits);
        CheckedMath.DecodedSize(count, 2, options.Limits);
        CheckedMath.AllocationSize(count, 2, options.Limits);
        ushort[] stored = new ushort[CheckedMath.ToInt32(count)];
        TiffDataBlock block = descriptor.Blocks[0];
        long offset = RawCodec.RelativeTiffOffset(block.Offset, graph);
        long length = CheckedMath.ToInt64(block.ByteCount);
        CheckedMath.RangeEnd(offset, length, source.Length);
        BoundedInput input = BoundedInput.CreateWithAbsoluteOrigin(source, offset, length, offset, options.Limits);
        MsbBitReader reader = new MsbBitReader(input);
        DecodeSamples(reader, stored, info.Width, info.Height, compression, options);
        reader.AlignToByteWithExpectedPadding(0);
        reader.RequireEnd();
        ushort[] normalized = Cr2LosslessJpegDecoder.Normalize(stored, info.BitDepth, options.Limits, options.CancellationToken);
        RawStoredImage? storedImage = NefUncompressedDecoder.CreateStoredImage(info, stored, options);
        RawBlackLevel? black = NefUncompressedDecoder.CreateBlackLevel(metadata);
        return new RawSensorImage(info, normalized, null, info.Width, true, storedImage, null, metadata.MaskedAreas, black, metadata.WhiteLevels, null, null, NefUncompressedDecoder.GetDiagnostics(metadata));
    }

    internal static void Validate(System.ReadOnlyMemory<byte> source, TiffIfdGraph graph, TiffImageDescriptor descriptor, RawImageInfo info, RawNikonNefMetadata metadata, RawReadProfile profile, RawDecodeOptions options)
    {
        if (options == null || !options.Validate().IsValid() || graph == null || graph.Header == null || descriptor == null || info == null || metadata == null || !CanDecode(profile, descriptor, metadata))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A complete profile-qualified compressed NEF decode request is required.", -1);
        }
        if (info.Width <= 0 || info.Height <= 0 || descriptor.Width != (ulong)info.Width || descriptor.Height != (ulong)info.Height || metadata.StoredWidth != info.Width || metadata.StoredHeight != info.Height || profile.Geometry.StoredWidth != info.Width || profile.Geometry.StoredHeight != info.Height)
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "The compressed NEF image geometry does not match its selected profile.", -1);
        }
        int precision = NefUncompressedDecoder.GetPrecision(descriptor);
        if (precision != 12 && precision != 14 || info.BitDepth != precision || profile.Fingerprint.BitDepth != precision || profile.Codec.StoredWordBits != precision || profile.Codec.Packing != RawPacking.PackedBits || profile.Codec.Predictor != 0 || profile.Codec.PlanarConfiguration != 1)
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "Compressed NEF decoding requires one matching unsigned 12- or 14-bit plane.", -1);
        }
        TiffDataBlock block = descriptor.Blocks[0];
        if (block == null || block.Tiled || block.Plane != 0 || block.Index != 0 || block.IndexInPlane != 0 || block.ByteCount == 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "Compressed NEF decoding requires one ordered nonempty strip.", -1);
        }
    }

    internal static NikonCompressionInfo ParseInfo(byte[] bytes, RawByteOrder byteOrder, int precision, int height)
    {
        if (bytes == null || bytes.Length < 12 || precision != 12 && precision != 14)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "Nikon compression parameters are missing or truncated.", -1);
        }
        int version0 = bytes[0];
        int version1 = bytes[1];
        if (version0 != 0x44 && version0 != 0x46 && version0 != 0x49)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "The Nikon compression-parameter version is not supported.", -1);
        }
        int position = version0 == 0x49 || version1 == 0x58 ? 2112 : 2;
        if (position + 10 > bytes.Length)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "Nikon compression predictors are truncated.", position);
        }
        NikonCompressionInfo result = new NikonCompressionInfo();
        result.Precision = precision;
        result.Lossless = version0 == 0x46;
        result.TreeIndex = result.Lossless ? 2 : 0;
        if (precision == 14)
        {
            result.TreeIndex += 3;
        }
        result.Predictors[0] = ReadUInt16(bytes, position, byteOrder);
        result.Predictors[1] = ReadUInt16(bytes, position + 2, byteOrder);
        result.Predictors[2] = ReadUInt16(bytes, position + 4, byteOrder);
        result.Predictors[3] = ReadUInt16(bytes, position + 6, byteOrder);
        position += 8;
        int curveSize = ReadUInt16(bytes, position, byteOrder);
        position += 2;
        int domain = 1 << precision;
        if (result.Lossless)
        {
            result.Curve = IdentityCurve(domain);
        }
        else if (version0 == 0x44 && (version1 == 0x20 || version1 == 0x40) && curveSize > 1)
        {
            if (version1 == 0x40)
            {
                domain >>= 2;
            }
            if (curveSize > domain + 1 || domain % (curveSize - 1) != 0 || position + curveSize * 2 > bytes.Length)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "The Nikon interpolated compression curve is invalid or truncated.", position);
            }
            ushort[] anchors = new ushort[curveSize];
            int index = 0;
            while (index < curveSize)
            {
                anchors[index] = (ushort)ReadUInt16(bytes, position + index * 2, byteOrder);
                index++;
            }
            result.Curve = InterpolateCurve(anchors, domain);
            if (bytes.Length >= 564)
            {
                result.SplitRow = ReadUInt16(bytes, 562, byteOrder);
            }
        }
        else
        {
            if (curveSize < 2 || curveSize > domain + 1 || position + curveSize * 2 > bytes.Length)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "The Nikon compression curve is invalid or truncated.", position);
            }
            result.Curve = new ushort[curveSize];
            int index = 0;
            while (index < curveSize)
            {
                result.Curve[index] = (ushort)ReadUInt16(bytes, position + index * 2, byteOrder);
                index++;
            }
        }
        if (result.SplitRow >= height)
        {
            result.SplitRow = 0;
        }
        int predictorIndex = 0;
        while (predictorIndex < result.Predictors.Length)
        {
            if (result.Predictors[predictorIndex] < 0 || result.Predictors[predictorIndex] >= result.Curve.Length)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "A Nikon initial predictor exceeds the compression curve.", position);
            }
            predictorIndex++;
        }
        int curveIndex = 0;
        while (curveIndex < result.Curve.Length)
        {
            if (result.Curve[curveIndex] >= 1 << precision)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "A Nikon compression curve value exceeds the declared precision.", curveIndex);
            }
            curveIndex++;
        }
        return result;
    }

    internal static void DecodeSamples(MsbBitReader reader, ushort[] destination, int width, int height, NikonCompressionInfo info, RawDecodeOptions options)
    {
        int[] vertical = new int[] { info.Predictors[0], info.Predictors[1], info.Predictors[2], info.Predictors[3] };
        int row = 0;
        while (row < height)
        {
            options.CheckCancellation();
            int parity = row & 1;
            int[] predictor = new int[] { vertical[parity], vertical[parity + 2] };
            int treeIndex = info.TreeIndex;
            int minimum = 0;
            int maximum = info.Curve.Length;
            if (info.SplitRow > 0 && row >= info.SplitRow)
            {
                treeIndex++;
                minimum = 16;
                maximum += 32;
            }
            NikonPrefixTable table = CreateTable(treeIndex);
            int column = 0;
            while (column < width)
            {
                int channel = column & 1;
                predictor[channel] += DecodeDifference(reader, table, info.Precision);
                int value = predictor[channel];
                if (value + minimum < 0 || value + minimum >= maximum)
                {
                    throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A compressed NEF predictor is outside its valid range.", reader.Core.GetCurrentByteOffset());
                }
                if (column < 2)
                {
                    vertical[parity + channel * 2] = value;
                }
                int curveIndex = value;
                if (curveIndex < 0)
                {
                    curveIndex = 0;
                }
                if (curveIndex >= info.Curve.Length)
                {
                    curveIndex = info.Curve.Length - 1;
                }
                int output = options.ApplyLinearization ? info.Curve[curveIndex] : curveIndex;
                destination[row * width + column] = (ushort)output;
                column++;
            }
            row++;
        }
    }

    internal static int DecodeDifference(MsbBitReader reader, NikonPrefixTable table, int precision)
    {
        int symbol = table.Decode(reader);
        int length = symbol & 15;
        int shift = symbol >> 4;
        if (length > precision || shift > length)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A Nikon difference symbol exceeds its precision.", reader.Core.GetCurrentByteOffset());
        }
        if (length == 0)
        {
            return 0;
        }
        uint bits = reader.ReadBits(length - shift);
        int difference = (int)((((bits << 1) + 1U) << shift) >> 1);
        if ((difference & 1 << (length - 1)) == 0)
        {
            difference -= (1 << length) - (shift == 0 ? 1 : 0);
        }
        return difference;
    }

    internal static NikonPrefixTable CreateTable(int index)
    {
        switch (index)
        {
            case 0: return new NikonPrefixTable(new byte[] { 0, 1, 5, 1, 1, 1, 1, 1, 1, 2, 0, 0, 0, 0, 0, 0 }, new byte[] { 5, 4, 3, 6, 2, 7, 1, 0, 8, 9, 11, 10, 12, 0 });
            case 1: return new NikonPrefixTable(new byte[] { 0, 1, 5, 1, 1, 1, 1, 1, 1, 2, 0, 0, 0, 0, 0, 0 }, new byte[] { 0x39, 0x5a, 0x38, 0x27, 0x16, 5, 4, 3, 2, 1, 0, 11, 12, 12 });
            case 2: return new NikonPrefixTable(new byte[] { 0, 1, 4, 2, 3, 1, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, new byte[] { 5, 4, 6, 3, 7, 2, 8, 1, 9, 0, 10, 11, 12 });
            case 3: return new NikonPrefixTable(new byte[] { 0, 1, 4, 3, 1, 1, 1, 1, 1, 2, 0, 0, 0, 0, 0, 0 }, new byte[] { 5, 6, 4, 7, 8, 3, 9, 2, 1, 0, 10, 11, 12, 13, 14 });
            case 4: return new NikonPrefixTable(new byte[] { 0, 1, 5, 1, 1, 1, 1, 1, 1, 1, 2, 0, 0, 0, 0, 0 }, new byte[] { 8, 0x5c, 0x4b, 0x3a, 0x29, 7, 6, 5, 4, 3, 2, 1, 0, 13, 14 });
            case 5: return new NikonPrefixTable(new byte[] { 0, 1, 4, 2, 2, 3, 1, 2, 0, 0, 0, 0, 0, 0, 0, 0 }, new byte[] { 7, 6, 8, 5, 9, 4, 10, 3, 11, 12, 2, 0, 1, 13, 14 });
            default: throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "The Nikon compression tree index is invalid.", -1);
        }
    }

    internal static NikonLosslessEncoding EncodeLossless(ushort[] samples, int width, int height, int rowStride, int precision, RawByteOrder byteOrder, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        if (samples == null || width < 2 || height < 2 || rowStride < width || precision != 12 && precision != 14)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "Lossless NEF encoding requires at least two rows and columns of matching 12- or 14-bit samples.", -1);
        }
        CheckedMath.RequireLimits(limits);
        long required = CheckedMath.Add(CheckedMath.Multiply(height - 1, rowStride), width);
        if (samples.LongLength < required)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidLength, "The lossless NEF input does not cover its declared row geometry.", -1);
        }
        int[] initial = CreateInitialPredictors(samples, rowStride, false);
        byte[] entropy = EncodeEntropy(samples, width, height, rowStride, precision, precision == 14 ? 5 : 2, initial, false, limits, cancellationToken);
        byte[] compressionInfo = new byte[12];
        compressionInfo[0] = 0x46;
        int index = 0;
        while (index < initial.Length)
        {
            EndianWriter.WriteUInt16(compressionInfo.AsSpan(2 + index * 2, 2), (ushort)initial[index], byteOrder);
            index++;
        }
        EndianWriter.WriteUInt16(compressionInfo.AsSpan(10, 2), 0, byteOrder);
        return new NikonLosslessEncoding(entropy, compressionInfo);
    }

    internal static NikonLossyEncoding EncodeLossy(ushort[] samples, int width, int height, int rowStride, int precision, RawByteOrder byteOrder, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        if (samples == null || width < 2 || height < 2 || rowStride < width || precision != 12 && precision != 14)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "Lossy NEF encoding requires at least two rows and columns of matching 12- or 14-bit samples.", -1);
        }
        CheckedMath.RequireLimits(limits);
        long required = CheckedMath.Add(CheckedMath.Multiply(height - 1, rowStride), width);
        if (samples.LongLength < required)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidLength, "The lossy NEF input does not cover its declared row geometry.", -1);
        }
        int[] initial = CreateInitialPredictors(samples, rowStride, true);
        byte[] entropy = EncodeEntropy(samples, width, height, rowStride, precision, precision == 14 ? 3 : 0, initial, true, limits, cancellationToken);
        int curveLength = (1 << (precision - 1)) + 1;
        int parameterLength = CheckedMath.ToInt32(CheckedMath.Add(12, CheckedMath.Multiply(curveLength, 2)));
        CheckedMath.AllocationSize(parameterLength, 1, limits);
        byte[] compressionInfo = new byte[parameterLength];
        compressionInfo[0] = 0x44;
        int predictorIndex = 0;
        while (predictorIndex < initial.Length)
        {
            EndianWriter.WriteUInt16(compressionInfo.AsSpan(2 + predictorIndex * 2, 2), (ushort)initial[predictorIndex], byteOrder);
            predictorIndex++;
        }
        EndianWriter.WriteUInt16(compressionInfo.AsSpan(10, 2), (ushort)curveLength, byteOrder);
        int maximum = (1 << precision) - 1;
        int curveIndex = 0;
        while (curveIndex < curveLength)
        {
            int value = curveIndex * 2;
            if (value > maximum)
            {
                value = maximum;
            }
            EndianWriter.WriteUInt16(compressionInfo.AsSpan(12 + curveIndex * 2, 2), (ushort)value, byteOrder);
            curveIndex++;
        }
        return new NikonLossyEncoding(entropy, compressionInfo, LossyMaximumError);
    }

    internal static int[] CreateInitialPredictors(ushort[] samples, int rowStride, bool lossy)
    {
        return new int[] { EncodedValue(samples[0], lossy), EncodedValue(samples[rowStride], lossy), EncodedValue(samples[1], lossy), EncodedValue(samples[rowStride + 1], lossy) };
    }

    internal static byte[] EncodeEntropy(ushort[] samples, int width, int height, int rowStride, int precision, int treeIndex, int[] initial, bool lossy, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        NikonPrefixTable table = CreateTable(treeIndex);
        int[] codes = new int[precision + 1];
        int[] codeLengths = new int[precision + 1];
        CreateEncoderTable(table, codes, codeLengths);
        long bitCount = CountEncodedBits(samples, width, height, rowStride, precision, initial, codeLengths, lossy, cancellationToken);
        int byteCount = CheckedMath.ToInt32((CheckedMath.Add(bitCount, 7)) / 8);
        CheckedMath.AllocationSize(byteCount, 1, limits);
        byte[] entropy = new byte[byteCount];
        MsbBitWriter writer = new MsbBitWriter(entropy);
        WriteEncodedSamples(writer, samples, width, height, rowStride, precision, initial, codes, codeLengths, lossy, cancellationToken);
        long written = writer.Finish(0);
        if (written != byteCount)
        {
            throw new RawWriteException(RawDiagnosticCode.OutputValidationFailed, "Nikon compressed entropy sizing did not match the encoded byte count.", -1);
        }
        return entropy;
    }

    internal static void CreateEncoderTable(NikonPrefixTable table, int[] codes, int[] codeLengths)
    {
        if (table == null || codes == null || codeLengths == null || codes.Length != codeLengths.Length)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A complete Nikon prefix encoder table is required.", -1);
        }
        int length = 1;
        while (length <= table.MaximumLength)
        {
            int offset = 0;
            while (offset < table.Counts[length])
            {
                int symbolIndex = table.FirstIndexes[length] + offset;
                int tableSymbol = table.Symbols[symbolIndex];
                if (tableSymbol < 0 || tableSymbol >= codes.Length)
                {
                    throw new RawWriteException(RawDiagnosticCode.InvalidProfile, "The Nikon prefix table contains an unsupported symbol.", -1);
                }
                if (codeLengths[tableSymbol] == 0)
                {
                    codes[tableSymbol] = table.FirstCodes[length] + offset;
                    codeLengths[tableSymbol] = length;
                }
                offset++;
            }
            length++;
        }
        int symbol = 0;
        while (symbol < codeLengths.Length)
        {
            if (codeLengths[symbol] == 0)
            {
                throw new RawWriteException(RawDiagnosticCode.InvalidProfile, "The Nikon lossless prefix table does not encode every required difference length.", -1);
            }
            symbol++;
        }
    }

    internal static long CountEncodedBits(ushort[] samples, int width, int height, int rowStride, int precision, int[] initial, int[] codeLengths, bool lossy, System.Threading.CancellationToken cancellationToken)
    {
        int[] vertical = new int[] { initial[0], initial[1], initial[2], initial[3] };
        long bitCount = 0;
        int row = 0;
        while (row < height)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int parity = row & 1;
            int[] predictor = new int[] { vertical[parity], vertical[parity + 2] };
            int column = 0;
            while (column < width)
            {
                int channel = column & 1;
                int value = EncodedValue(samples[row * rowStride + column], lossy);
                int difference = value - predictor[channel];
                int length = DifferenceLength(difference, precision);
                bitCount = CheckedMath.Add(bitCount, codeLengths[length] + length);
                predictor[channel] = value;
                if (column < 2)
                {
                    vertical[parity + channel * 2] = value;
                }
                column++;
            }
            row++;
        }
        return bitCount;
    }

    internal static void WriteEncodedSamples(MsbBitWriter writer, ushort[] samples, int width, int height, int rowStride, int precision, int[] initial, int[] codes, int[] codeLengths, bool lossy, System.Threading.CancellationToken cancellationToken)
    {
        int[] vertical = new int[] { initial[0], initial[1], initial[2], initial[3] };
        int row = 0;
        while (row < height)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int parity = row & 1;
            int[] predictor = new int[] { vertical[parity], vertical[parity + 2] };
            int column = 0;
            while (column < width)
            {
                int channel = column & 1;
                int value = EncodedValue(samples[row * rowStride + column], lossy);
                int difference = value - predictor[channel];
                int length = DifferenceLength(difference, precision);
                writer.WriteBits((uint)codes[length], codeLengths[length]);
                if (length != 0)
                {
                    int bits = difference >= 0 ? difference : difference + (1 << length) - 1;
                    writer.WriteBits((uint)bits, length);
                }
                predictor[channel] = value;
                if (column < 2)
                {
                    vertical[parity + channel * 2] = value;
                }
                column++;
            }
            row++;
        }
    }

    internal static int DifferenceLength(int difference, int precision)
    {
        int magnitude = difference < 0 ? -difference : difference;
        int length = 0;
        while (magnitude != 0)
        {
            length++;
            magnitude >>= 1;
        }
        if (length > precision)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidArgument, "A lossless NEF difference exceeds the selected precision.", -1);
        }
        return length;
    }

    internal static int EncodedValue(ushort value, bool lossy)
    {
        return lossy ? (value + 1) / 2 : value;
    }

    internal static int ReadUInt16(byte[] bytes, int offset, RawByteOrder order)
    {
        if (offset < 0 || offset + 2 > bytes.Length)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "A Nikon compression word is truncated.", offset);
        }
        if (order == RawByteOrder.LittleEndian)
        {
            return bytes[offset] | bytes[offset + 1] << 8;
        }
        if (order == RawByteOrder.BigEndian)
        {
            return bytes[offset] << 8 | bytes[offset + 1];
        }
        throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "The Nikon compression byte order is invalid.", offset);
    }

    internal static ushort[] IdentityCurve(int length)
    {
        ushort[] curve = new ushort[length];
        int index = 0;
        while (index < length)
        {
            curve[index] = (ushort)index;
            index++;
        }
        return curve;
    }

    internal static ushort[] InterpolateCurve(ushort[] anchors, int length)
    {
        ushort[] curve = new ushort[length];
        int step = length / (anchors.Length - 1);
        int index = 0;
        while (index < length)
        {
            int segment = index / step;
            if (segment >= anchors.Length - 1)
            {
                segment = anchors.Length - 2;
            }
            int distance = index - segment * step;
            long value = (long)anchors[segment] * (step - distance) + (long)anchors[segment + 1] * distance;
            value /= step;
            if (value < 0 || value > ushort.MaxValue)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "A Nikon interpolated curve value exceeds unsigned sixteen-bit range.", index);
            }
            curve[index] = (ushort)value;
            index++;
        }
        return curve;
    }
}
