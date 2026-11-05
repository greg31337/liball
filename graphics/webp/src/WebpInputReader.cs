using System;
using System.IO;

namespace webp;

internal static class WebpInputReader
{
    internal static byte[] ReadFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return File.ReadAllBytes(path);
    }

    internal static byte[] ReadFileWithLimit(string path, long maxBytes)
    {
        ArgumentNullException.ThrowIfNull(path);
        ValidateMaxBytes(maxBytes);
        FileInfo info = new FileInfo(path);
        if (info.Length > maxBytes)
        {
            throw new ArgumentException("WebP input allocation exceeds the configured security limit", "path");
        }

        return File.ReadAllBytes(path);
    }

    internal static byte[] ReadStream(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead)
        {
            throw new ArgumentException("WebP input stream must be readable", "stream");
        }

        if (stream.CanSeek)
        {
            return ReadSeekableStream(stream);
        }

        return ReadNonSeekableStream(stream);
    }

    internal static byte[] ReadStreamWithLimit(Stream stream, long maxBytes)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ValidateMaxBytes(maxBytes);
        if (!stream.CanRead)
        {
            throw new ArgumentException("WebP input stream must be readable", "stream");
        }

        if (stream.CanSeek)
        {
            return ReadSeekableStreamWithLimit(stream, maxBytes);
        }

        return ReadNonSeekableStreamWithLimit(stream, maxBytes);
    }

    internal static byte[] ReadStreamWithDefaultLimit(Stream stream)
    {
        return ReadStreamWithLimit(stream, WebpConstants.DefaultMaxAggregateAllocationBytes);
    }

    internal static byte[] ReadFileWithDefaultLimit(string path)
    {
        return ReadFileWithLimit(path, WebpConstants.DefaultMaxAggregateAllocationBytes);
    }

    internal static byte[] ReadSeekableStream(Stream stream)
    {
        long remaining = stream.Length - stream.Position;
        if (remaining < 0 || remaining > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException("stream");
        }

        byte[] data = new byte[(int)remaining];
        int offset = 0;
        while (offset < data.Length)
        {
            int count = stream.Read(data, offset, data.Length - offset);
            if (count == 0)
            {
                break;
            }

            offset = offset + count;
        }

        if (offset == data.Length)
        {
            return data;
        }

        byte[] truncated = new byte[offset];
        Array.Copy(data, 0, truncated, 0, offset);
        return truncated;
    }

    internal static byte[] ReadSeekableStreamWithLimit(Stream stream, long maxBytes)
    {
        long remaining = stream.Length - stream.Position;
        if (remaining < 0 || remaining > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException("stream");
        }

        if (remaining > maxBytes)
        {
            throw new ArgumentException("WebP input allocation exceeds the configured security limit", "stream");
        }

        byte[] data = new byte[(int)remaining];
        int offset = 0;
        while (offset < data.Length)
        {
            int count = stream.Read(data, offset, data.Length - offset);
            if (count == 0)
            {
                break;
            }

            offset = offset + count;
        }

        if (offset == data.Length)
        {
            return data;
        }

        byte[] truncated = new byte[offset];
        Array.Copy(data, 0, truncated, 0, offset);
        return truncated;
    }

    internal static byte[] ReadNonSeekableStream(Stream stream)
    {
        using (MemoryStream output = new MemoryStream())
        {
            byte[] buffer = new byte[8192];
            int count = stream.Read(buffer, 0, buffer.Length);
            while (count > 0)
            {
                output.Write(buffer, 0, count);
                count = stream.Read(buffer, 0, buffer.Length);
            }

            return output.ToArray();
        }
    }

    internal static byte[] ReadNonSeekableStreamWithLimit(Stream stream, long maxBytes)
    {
        int bufferLength = 8192;
        if (maxBytes < bufferLength)
        {
            bufferLength = (int)maxBytes;
        }

        using (MemoryStream output = new MemoryStream())
        {
            byte[] buffer = new byte[bufferLength];
            int count = stream.Read(buffer, 0, buffer.Length);
            while (count > 0)
            {
                if (output.Length > maxBytes - count)
                {
                    throw new ArgumentException("WebP input allocation exceeds the configured security limit", "stream");
                }

                output.Write(buffer, 0, count);
                count = stream.Read(buffer, 0, buffer.Length);
            }

            return output.ToArray();
        }
    }

    internal static void ValidateMaxBytes(long maxBytes)
    {
        if (maxBytes <= 0)
        {
            throw new ArgumentOutOfRangeException("maxBytes");
        }
    }
}
