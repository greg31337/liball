using System;

namespace raw;

internal class TiffImageBlockDecodeParameters
{
    internal ulong CompressionCode;
    internal TiffPredictorParameters? PredictorParameters;
    internal int LzwEarlyChange;
    internal byte[]? JpegTables;
    internal byte[]? LegacyJpegPrefix;
    internal int ExpectedJpegWidth;
    internal int ExpectedJpegHeight;
    internal int ExpectedJpegComponents;

    internal TiffImageBlockDecodeParameters(ulong compressionCode, TiffPredictorParameters? predictorParameters, int lzwEarlyChange, byte[]? jpegTables, byte[]? legacyJpegPrefix, int expectedJpegWidth, int expectedJpegHeight, int expectedJpegComponents)
    {
        CompressionCode = compressionCode;
        PredictorParameters = predictorParameters;
        LzwEarlyChange = lzwEarlyChange;
        JpegTables = jpegTables;
        LegacyJpegPrefix = legacyJpegPrefix;
        ExpectedJpegWidth = expectedJpegWidth;
        ExpectedJpegHeight = expectedJpegHeight;
        ExpectedJpegComponents = expectedJpegComponents;
    }
}

internal class TiffDecodedBlock
{
    internal RawCompression Compression;
    internal byte[]? Bytes;
    internal BaselineJpegImage? BaselineJpeg;
    internal LosslessJpegImage? LosslessJpeg;
    internal bool SharedJpegTablesApplied;
    internal bool LegacyJpegPrefixApplied;
}

internal static class TiffImageBlockDecoder
{
    internal static TiffDecodedBlock Decode(byte[]? source, TiffImageBlockDecodeParameters? parameters, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        if (source == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A TIFF image block is required.", -1);
        }
        if (parameters == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "TIFF image block decode parameters are required.", -1);
        }
        CheckedMath.FileSize(source.Length, limits);
        cancellationToken.ThrowIfCancellationRequested();
        if (parameters.CompressionCode == 1)
        {
            return DecodeBytes(source, RawCompression.None, parameters, limits, cancellationToken);
        }
        if (parameters.CompressionCode == 32773)
        {
            return DecodeBytes(source, RawCompression.PackBits, parameters, limits, cancellationToken);
        }
        if (parameters.CompressionCode == 5)
        {
            return DecodeBytes(source, RawCompression.Lzw, parameters, limits, cancellationToken);
        }
        if (parameters.CompressionCode == 8)
        {
            return DecodeBytes(source, RawCompression.AdobeDeflate, parameters, limits, cancellationToken);
        }
        if (parameters.CompressionCode == 32946)
        {
            return DecodeBytes(source, RawCompression.Deflate, parameters, limits, cancellationToken);
        }
        if (parameters.CompressionCode == 6 || parameters.CompressionCode == 7)
        {
            return DecodeJpeg(source, parameters, limits, cancellationToken);
        }
        throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedCodec, "The TIFF image block compression is not supported.", -1);
    }

    internal static TiffDecodedBlock DecodeBytes(byte[] source, RawCompression compression, TiffImageBlockDecodeParameters parameters, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        if (parameters.PredictorParameters == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Byte-oriented TIFF decoding requires predictor geometry.", -1);
        }
        int decodedLength = TiffPredictorCodec.GetDecodedLength(parameters.PredictorParameters, limits);
        byte[] destination;
        if (compression == RawCompression.None)
        {
            if (source.Length != decodedLength)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidLength, "An uncompressed TIFF block does not match its decoded geometry.", -1);
            }
            CheckedMath.AllocationSize(decodedLength, 1, limits);
            destination = source.AsSpan().ToArray();
            TiffPredictorCodec.Decode(destination, parameters.PredictorParameters, limits, cancellationToken);
        }
        else if (compression == RawCompression.PackBits)
        {
            destination = PackBitsCodec.Decode(source, decodedLength, limits, cancellationToken);
            TiffPredictorCodec.Decode(destination, parameters.PredictorParameters, limits, cancellationToken);
        }
        else if (compression == RawCompression.Lzw)
        {
            destination = LzwCodec.DecodeTiff(source, parameters.LzwEarlyChange, parameters.PredictorParameters, limits, cancellationToken);
        }
        else
        {
            destination = DeflateCodec.DecodeTiff(source, compression, parameters.PredictorParameters, limits, cancellationToken);
        }
        TiffDecodedBlock result = new TiffDecodedBlock();
        result.Compression = compression;
        result.Bytes = destination;
        result.BaselineJpeg = null;
        result.LosslessJpeg = null;
        result.SharedJpegTablesApplied = false;
        result.LegacyJpegPrefixApplied = false;
        return result;
    }

    internal static TiffDecodedBlock DecodeJpeg(byte[] source, TiffImageBlockDecodeParameters parameters, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        if (parameters.PredictorParameters != null && parameters.PredictorParameters.Predictor != TiffPredictor.None)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "TIFF predictors cannot be applied outside a JPEG codestream.", -1);
        }
        byte[] jpeg = source;
        bool sharedTables = false;
        bool legacyPrefix = false;
        if (parameters.CompressionCode == 6 && !HasStartOfImage(source))
        {
            jpeg = JoinLegacyJpeg(parameters.LegacyJpegPrefix, source, limits);
            legacyPrefix = true;
        }
        else
        {
            if (!HasStartOfImage(source) || !HasEndOfImage(source))
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A TIFF JPEG image stream must contain SOI and EOI markers.", -1);
            }
            if (parameters.JpegTables != null && parameters.JpegTables.Length != 0)
            {
                jpeg = MergeJpegTables(parameters.JpegTables, source, limits);
                sharedTables = true;
            }
        }

        JpegDocument document = JpegParser.Parse(jpeg, limits, cancellationToken);
        if (document.Frames.Count != 1)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "A TIFF JPEG block must contain exactly one frame.", -1);
        }
        JpegFrameHeader frame = document.Frames[0];
        ValidateJpegGeometry(frame, parameters);
        TiffDecodedBlock result = new TiffDecodedBlock();
        result.Bytes = null;
        result.SharedJpegTablesApplied = sharedTables;
        result.LegacyJpegPrefixApplied = legacyPrefix;
        if (frame.ProcessMarker == 0xc0)
        {
            result.Compression = RawCompression.BaselineJpeg;
            result.BaselineJpeg = BaselineJpegCodec.Decode(document, limits, cancellationToken);
            result.LosslessJpeg = null;
            return result;
        }
        if (frame.ProcessMarker == 0xc3)
        {
            result.Compression = RawCompression.LosslessJpeg;
            result.BaselineJpeg = null;
            result.LosslessJpeg = LosslessJpegCodec.Decode(document, limits, cancellationToken);
            return result;
        }
        throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedCodec, "The TIFF JPEG process is neither baseline sequential nor lossless Huffman.", (frame.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
    }

    internal static void ValidateJpegGeometry(JpegFrameHeader frame, TiffImageBlockDecodeParameters parameters)
    {
        if (parameters.ExpectedJpegWidth < 0 || parameters.ExpectedJpegHeight < 0 || parameters.ExpectedJpegComponents < 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Expected TIFF JPEG geometry cannot be negative.", -1);
        }
        if (parameters.ExpectedJpegWidth != 0 && frame.Width != parameters.ExpectedJpegWidth || parameters.ExpectedJpegHeight != 0 && frame.Height != parameters.ExpectedJpegHeight || parameters.ExpectedJpegComponents != 0 && frame.Components.Count != parameters.ExpectedJpegComponents)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "The JPEG frame does not match its TIFF block geometry.", (frame.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
        }
    }

    internal static byte[] MergeJpegTables(byte[] tables, byte[] image, RawLimits limits)
    {
        if (!HasStartOfImage(tables) || !HasEndOfImage(tables))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "TIFF JPEGTables must be an SOI/EOI table stream.", -1);
        }
        if (!HasStartOfImage(image) || !HasEndOfImage(image))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "An abbreviated TIFF JPEG image must contain SOI and EOI markers.", -1);
        }
        long length = CheckedMath.Add(tables.Length - 2, image.Length - 2);
        CheckedMath.FileSize(length, limits);
        CheckedMath.AllocationSize(length, 1, limits);
        byte[] result = new byte[CheckedMath.ToInt32(length)];
        System.Array.Copy(tables, 0, result, 0, tables.Length - 2);
        System.Array.Copy(image, 2, result, tables.Length - 2, image.Length - 2);
        return result;
    }

    internal static byte[] JoinLegacyJpeg(byte[]? prefix, byte[] entropy, RawLimits limits)
    {
        if (prefix == null || prefix.Length < 2 || !HasStartOfImage(prefix) || HasEndOfImage(prefix))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "A legacy TIFF JPEG entropy block requires an SOI header prefix without EOI.", -1);
        }
        int entropyLength = entropy.Length;
        bool entropyHasEnd = HasEndOfImage(entropy);
        long length = CheckedMath.Add(prefix.Length, entropyLength);
        if (!entropyHasEnd)
        {
            length = CheckedMath.Add(length, 2);
        }
        CheckedMath.FileSize(length, limits);
        CheckedMath.AllocationSize(length, 1, limits);
        byte[] result = new byte[CheckedMath.ToInt32(length)];
        System.Array.Copy(prefix, 0, result, 0, prefix.Length);
        System.Array.Copy(entropy, 0, result, prefix.Length, entropyLength);
        if (!entropyHasEnd)
        {
            result[result.Length - 2] = 0xff;
            result[result.Length - 1] = 0xd9;
        }
        return result;
    }

    internal static bool HasStartOfImage(byte[] source)
    {
        return source != null && source.Length >= 2 && source[0] == 0xff && source[1] == 0xd8;
    }

    internal static bool HasEndOfImage(byte[] source)
    {
        return source != null && source.Length >= 2 && source[source.Length - 2] == 0xff && source[source.Length - 1] == 0xd9;
    }
}
