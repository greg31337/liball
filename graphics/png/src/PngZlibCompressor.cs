using System.IO;
using System.IO.Compression;

namespace png;

internal class PngZlibCompressor
{
    internal static byte[] Compress(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Compress(data, 0, data.Length, CompressionLevel.SmallestSize, new PngSecurityLimits());
    }

    internal static byte[] Compress(byte[] data, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Compress(data, 0, data.Length, CompressionLevel.SmallestSize, limits);
    }

    internal static byte[] Compress(byte[] data, CompressionLevel compressionLevel)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Compress(data, 0, data.Length, compressionLevel, new PngSecurityLimits());
    }

    internal static byte[] Compress(byte[] data, int offset, int length)
    {
        return Compress(data, offset, length, CompressionLevel.SmallestSize, new PngSecurityLimits());
    }

    internal static byte[] Compress(byte[] data, int offset, int length, PngSecurityLimits limits)
    {
        return Compress(data, offset, length, CompressionLevel.SmallestSize, limits);
    }

    internal static byte[] Compress(byte[] data, int offset, int length, CompressionLevel compressionLevel, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(limits);

        if (offset < 0 || offset > data.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (length < 0 || length > data.Length - offset)
        {
            throw new ArgumentOutOfRangeException("length");
        }

        if (limits.MaxDecompressedBytes > 0 && length > limits.MaxDecompressedBytes)
        {
            throw new OverflowException();
        }

        MemoryStream output = new MemoryStream();
        ZLibStream zlibStream = new ZLibStream(output, compressionLevel, true);
        zlibStream.Write(data, offset, length);
        zlibStream.Dispose();
        byte[] result = output.ToArray();
        output.Dispose();

        if (limits.MaxIdatBytes > 0 && result.Length > limits.MaxIdatBytes)
        {
            throw new OverflowException();
        }

        return result;
    }

    internal static byte[] CompressWithBailout(byte[] data, int offset, int length, CompressionLevel compressionLevel, PngSecurityLimits limits, PngEncoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(limits);
        ArgumentNullException.ThrowIfNull(options);

        if (offset < 0 || offset > data.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (length < 0 || length > data.Length - offset)
        {
            throw new ArgumentOutOfRangeException("length");
        }

        if (limits.MaxDecompressedBytes > 0 && length > limits.MaxDecompressedBytes)
        {
            throw new OverflowException();
        }

        MemoryStream output = new MemoryStream();
        ZLibStream zlibStream = new ZLibStream(output, compressionLevel, true);
        long recordedBytes = 0;
        int written = 0;
        while (written < length && !options.WasEarlyBailedOut)
        {
            int blockLength = length - written;
            if (blockLength > 4096)
            {
                blockLength = 4096;
            }

            zlibStream.Write(data, offset + written, blockLength);
            long producedBytes = output.Length - recordedBytes;
            if (producedBytes > 0)
            {
                PngEncodingBailout.RecordBytes(options, producedBytes);
                recordedBytes = output.Length;
            }

            written += blockLength;
        }

        zlibStream.Dispose();
        long finalBytes = output.Length - recordedBytes;
        if (finalBytes > 0)
        {
            PngEncodingBailout.RecordBytes(options, finalBytes);
        }

        if (options.WasEarlyBailedOut)
        {
            output.Dispose();
            return new byte[0];
        }

        byte[] result = output.ToArray();
        output.Dispose();
        if (limits.MaxIdatBytes > 0 && result.Length > limits.MaxIdatBytes)
        {
            throw new OverflowException();
        }

        return result;
    }
}
