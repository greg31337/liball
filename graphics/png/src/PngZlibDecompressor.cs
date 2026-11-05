using System.IO;
using System.IO.Compression;

namespace png;

internal class PngZlibDecompressor
{
    internal static byte[] Decompress(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Decompress(data, 0, data.Length, new PngSecurityLimits());
    }

    internal static byte[] Decompress(byte[] data, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(data);

        return Decompress(data, 0, data.Length, limits);
    }

    internal static byte[] Decompress(byte[] data, int offset, int length)
    {
        return Decompress(data, offset, length, new PngSecurityLimits());
    }

    internal static byte[] Decompress(byte[] data, int offset, int length, PngSecurityLimits limits)
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

        MemoryStream input = new MemoryStream(data, offset, length, false);
        byte[] result = Decompress(input, limits);
        input.Dispose();
        return result;
    }

    internal static byte[] Decompress(Stream stream)
    {
        return Decompress(stream, new PngSecurityLimits());
    }

    internal static byte[] Decompress(Stream stream, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(limits);

        ZLibStream zlibStream = new ZLibStream(stream, CompressionMode.Decompress, true);
        byte[] result = ReadDecompressedBytes(zlibStream, limits);
        zlibStream.Dispose();
        return result;
    }

    internal static byte[] ReadDecompressedBytes(Stream stream, PngSecurityLimits limits)
    {
        MemoryStream output = new MemoryStream();
        byte[] buffer = new byte[8192];
        long totalLength = 0;
        while (true)
        {
            int read = stream.Read(buffer, 0, buffer.Length);
            if (read == 0)
            {
                break;
            }

            if (limits.MaxDecompressedBytes > 0 && totalLength > limits.MaxDecompressedBytes - read)
            {
                throw new OverflowException();
            }

            if (totalLength > int.MaxValue - read)
            {
                throw new OverflowException();
            }

            output.Write(buffer, 0, read);
            totalLength += read;
        }

        byte[] result = output.ToArray();
        output.Dispose();
        return result;
    }
}
