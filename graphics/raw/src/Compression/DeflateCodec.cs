using System;

namespace raw;

internal static class DeflateCodec
{
    internal const int BufferSize = 65536;

    internal static byte[] Decode(byte[]? source, int decodedLength, bool zlibWrapper, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        if (source == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Deflate input is required.", -1);
        }
        if (decodedLength < 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The Deflate decoded length cannot be negative.", -1);
        }
        CheckedMath.FileSize(source.Length, limits);
        CheckedMath.AllocationSize(decodedLength, 1, limits);
        cancellationToken.ThrowIfCancellationRequested();

        byte[] destination = new byte[decodedLength];
        using (System.IO.MemoryStream input = new System.IO.MemoryStream(source, false))
        using (System.IO.Stream decompressor = CreateDecoder(input, zlibWrapper))
        {
            int outputOffset = 0;
            while (outputOffset < destination.Length)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int requestLength = destination.Length - outputOffset;
                if (requestLength > BufferSize)
                {
                    requestLength = BufferSize;
                }
                int read = decompressor.Read(destination, outputOffset, requestLength);
                if (read == 0)
                {
                    throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "The Deflate stream ends before the declared decoded length is produced.", input.Position);
                }
                outputOffset += read;
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (decompressor.ReadByte() >= 0)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "The Deflate stream exceeds the declared decoded length.", input.Position);
            }
        }
        return destination;
    }

    internal static byte[] DecodeTiff(byte[] source, RawCompression compression, TiffPredictorParameters predictorParameters, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        ValidateTiffCompression(compression);
        int decodedLength = TiffPredictorCodec.GetDecodedLength(predictorParameters, limits);
        byte[] destination = Decode(source, decodedLength, true, limits, cancellationToken);
        TiffPredictorCodec.Decode(destination, predictorParameters, limits, cancellationToken);
        return destination;
    }

    internal static byte[] Encode(System.ReadOnlySpan<byte> source, bool zlibWrapper, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        CheckedMath.DecodedSize(source.Length, 1, limits);
        int maximumLength = GetMaximumEncodedLength(source.Length);
        CheckedMath.AllocationSize(maximumLength, 1, limits);
        cancellationToken.ThrowIfCancellationRequested();

        byte[] buffer = new byte[maximumLength];
        int encodedLength;
        using (System.IO.MemoryStream output = new System.IO.MemoryStream(buffer, 0, buffer.Length, true, true))
        {
            output.SetLength(0);
            using (System.IO.Stream compressor = CreateEncoder(output, zlibWrapper))
            {
                int inputOffset = 0;
                while (inputOffset < source.Length)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    int writeLength = source.Length - inputOffset;
                    if (writeLength > BufferSize)
                    {
                        writeLength = BufferSize;
                    }
                    compressor.Write(source.Slice(inputOffset, writeLength));
                    inputOffset += writeLength;
                }
                cancellationToken.ThrowIfCancellationRequested();
            }
            encodedLength = CheckedMath.ToInt32(output.Position);
        }

        CheckedMath.AllocationSize(encodedLength, 1, limits);
        return buffer.AsSpan(0, encodedLength).ToArray();
    }

    internal static byte[] EncodeTiff(System.ReadOnlySpan<byte> source, RawCompression compression, TiffPredictorParameters predictorParameters, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        ValidateTiffCompression(compression);
        int expectedLength = TiffPredictorCodec.GetDecodedLength(predictorParameters, limits);
        if (source.Length != expectedLength)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The Deflate input length does not match its TIFF predictor geometry.", -1);
        }
        CheckedMath.AllocationSize(source.Length, 1, limits);
        cancellationToken.ThrowIfCancellationRequested();
        byte[] predicted = source.ToArray();
        TiffPredictorCodec.Encode(predicted, predictorParameters, limits, cancellationToken);
        return Encode(predicted, true, limits, cancellationToken);
    }

    internal static int GetMaximumEncodedLength(int sourceLength)
    {
        if (sourceLength < 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The Deflate input length cannot be negative.", -1);
        }
        long length = sourceLength;
        long maximumLength = CheckedMath.Add(length, length >> 12);
        maximumLength = CheckedMath.Add(maximumLength, length >> 14);
        maximumLength = CheckedMath.Add(maximumLength, length >> 25);
        maximumLength = CheckedMath.Add(maximumLength, 13);
        return CheckedMath.ToInt32(maximumLength);
    }

    internal static System.IO.Stream CreateDecoder(System.IO.Stream input, bool zlibWrapper)
    {
        if (zlibWrapper)
        {
            return new System.IO.Compression.ZLibStream(input, System.IO.Compression.CompressionMode.Decompress, true);
        }
        return new System.IO.Compression.DeflateStream(input, System.IO.Compression.CompressionMode.Decompress, true);
    }

    internal static System.IO.Stream CreateEncoder(System.IO.Stream output, bool zlibWrapper)
    {
        if (zlibWrapper)
        {
            return new System.IO.Compression.ZLibStream(output, System.IO.Compression.CompressionLevel.Optimal, true);
        }
        return new System.IO.Compression.DeflateStream(output, System.IO.Compression.CompressionLevel.Optimal, true);
    }

    internal static void ValidateTiffCompression(RawCompression compression)
    {
        if (compression != RawCompression.Deflate && compression != RawCompression.AdobeDeflate)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "TIFF Deflate requires the Deflate or Adobe Deflate compression identifier.", -1);
        }
    }
}
