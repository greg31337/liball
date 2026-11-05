using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace http;

internal static class CompressionCodec
{
    internal static HttpContentCodingResult EncodeKnown(
        byte[] content,
        string coding,
        int maximumOutputSize)
    {
        if (Ascii.EqualsStringsIgnoreCase(coding, HttpContentCodings.Identity))
        {
            return CreateIdentity(content, coding, maximumOutputSize);
        }

        if (content.Length == 0 &&
            (Ascii.EqualsStringsIgnoreCase(coding, HttpContentCodings.Gzip) ||
            Ascii.EqualsStringsIgnoreCase(coding, HttpContentCodings.XGzip)))
        {
            return CreateEncodedResult(
                content,
                new byte[]
                {
                    31, 139, 8, 0, 0, 0, 0, 0, 0, 3,
                    3, 0, 0, 0, 0, 0, 0, 0, 0, 0
                },
                coding,
                maximumOutputSize);
        }

        if (content.Length == 0 &&
            Ascii.EqualsStringsIgnoreCase(coding, HttpContentCodings.Deflate))
        {
            return CreateEncodedResult(
                content,
                new byte[] { 120, 156, 3, 0, 0, 0, 0, 1 },
                coding,
                maximumOutputSize);
        }

        MemoryStream destination = new MemoryStream();
        if (Ascii.EqualsStringsIgnoreCase(coding, HttpContentCodings.Gzip) ||
            Ascii.EqualsStringsIgnoreCase(coding, HttpContentCodings.XGzip))
        {
            using (GZipStream encoder = new GZipStream(
                destination,
                CompressionLevel.Optimal,
                true))
            {
                encoder.Write(content, 0, content.Length);
            }
        }
        else if (Ascii.EqualsStringsIgnoreCase(coding, HttpContentCodings.Deflate))
        {
            using (ZLibStream encoder = new ZLibStream(
                destination,
                CompressionLevel.Optimal,
                true))
            {
                encoder.Write(content, 0, content.Length);
            }
        }
        else
        {
            using (BrotliStream encoder = new BrotliStream(
                destination,
                CompressionLevel.Optimal,
                true))
            {
                encoder.Write(content, 0, content.Length);
            }
        }

        byte[] transformed = destination.ToArray();
        destination.Dispose();
        return CreateEncodedResult(content, transformed, coding, maximumOutputSize);
    }

    internal static HttpContentCodingResult DecodeKnown(
        byte[] content,
        string coding,
        int maximumOutputSize)
    {
        if (Ascii.EqualsStringsIgnoreCase(coding, HttpContentCodings.Identity))
        {
            return CreateIdentity(content, coding, maximumOutputSize);
        }

        Task<HttpContentCodingResult> task = Task.Run(() =>
            DecodeKnownCore(content, coding, maximumOutputSize));
        while (!task.IsCompleted)
        {
            Thread.Yield();
        }

        if (task.Status != TaskStatus.RanToCompletion)
        {
            AggregateException? failure = task.Exception;
            string reason = "The coded content is malformed.";
            if (failure != null)
            {
                reason = "The coded content is malformed: " +
                    failure.GetBaseException().Message;
            }

            return CreateFailure(
                HttpContentCodingOutcome.Malformed,
                content,
                coding,
                reason);
        }

        HttpContentCodingResult result = task.Result;
        if (result.Outcome == HttpContentCodingOutcome.Decoded &&
            !IsCompleteEncoding(content, coding, result.Content))
        {
            return CreateFailure(
                HttpContentCodingOutcome.Malformed,
                content,
                coding,
                "The coded content is truncated or has invalid integrity metadata.");
        }

        return result;
    }

    internal static bool IsSupported(string coding)
    {
        return Ascii.EqualsStringsIgnoreCase(coding, HttpContentCodings.Identity) ||
            Ascii.EqualsStringsIgnoreCase(coding, HttpContentCodings.Gzip) ||
            Ascii.EqualsStringsIgnoreCase(coding, HttpContentCodings.XGzip) ||
            Ascii.EqualsStringsIgnoreCase(coding, HttpContentCodings.Deflate) ||
            Ascii.EqualsStringsIgnoreCase(coding, HttpContentCodings.Br);
    }

    internal static byte[] CopyContent(byte[] content)
    {
        byte[] result = new byte[content.Length];
        int index = 0;
        while (index < content.Length)
        {
            result[index] = content[index];
            index = index + 1;
        }

        return result;
    }

    internal static bool IsCompleteEncoding(
        byte[] content,
        string coding,
        byte[] decoded)
    {
        if (Ascii.EqualsStringsIgnoreCase(coding, HttpContentCodings.Gzip) ||
            Ascii.EqualsStringsIgnoreCase(coding, HttpContentCodings.XGzip))
        {
            return HasValidGzipTrailer(content, decoded);
        }

        if (Ascii.EqualsStringsIgnoreCase(coding, HttpContentCodings.Deflate))
        {
            return HasValidZlibEnvelope(content, decoded);
        }

        return content.Length != 0;
    }

    internal static bool HasValidGzipTrailer(byte[] content, byte[] decoded)
    {
        if (content.Length < 18 || content[0] != 31 || content[1] != 139 ||
            content[2] != 8 || (content[3] & 224) != 0)
        {
            return false;
        }

        int trailer = content.Length - 8;
        uint expectedCrc = (uint)content[trailer] |
            ((uint)content[trailer + 1] << 8) |
            ((uint)content[trailer + 2] << 16) |
            ((uint)content[trailer + 3] << 24);
        uint expectedSize = (uint)content[trailer + 4] |
            ((uint)content[trailer + 5] << 8) |
            ((uint)content[trailer + 6] << 16) |
            ((uint)content[trailer + 7] << 24);
        if (expectedSize > decoded.Length)
        {
            return false;
        }

        int start = decoded.Length - (int)expectedSize;
        return CalculateCrc32(decoded, start) == expectedCrc;
    }

    internal static bool HasValidZlibEnvelope(byte[] content, byte[] decoded)
    {
        if (content.Length < 6 || (content[0] & 15) != 8 ||
            (content[0] >> 4) > 7 ||
            (((int)content[0] << 8) + content[1]) % 31 != 0)
        {
            return false;
        }

        int trailer = content.Length - 4;
        uint expected = ((uint)content[trailer] << 24) |
            ((uint)content[trailer + 1] << 16) |
            ((uint)content[trailer + 2] << 8) |
            content[trailer + 3];
        return CalculateAdler32(decoded) == expected;
    }

    internal static uint CalculateCrc32(byte[] content, int start)
    {
        uint crc = 4294967295;
        int index = start;
        while (index < content.Length)
        {
            crc = crc ^ content[index];
            int bit = 0;
            while (bit < 8)
            {
                uint mask = (crc & 1) == 0 ? 0 : 3988292384;
                crc = (crc >> 1) ^ mask;
                bit = bit + 1;
            }

            index = index + 1;
        }

        return crc ^ 4294967295;
    }

    internal static uint CalculateAdler32(byte[] content)
    {
        uint first = 1;
        uint second = 0;
        int index = 0;
        while (index < content.Length)
        {
            first = (first + content[index]) % 65521;
            second = (second + first) % 65521;
            index = index + 1;
        }

        return (second << 16) | first;
    }

    internal static HttpContentCodingResult CreateEncodedResult(
        byte[] original,
        byte[] transformed,
        string coding,
        int maximumOutputSize)
    {
        if (transformed.Length > maximumOutputSize)
        {
            return CreateFailure(
                HttpContentCodingOutcome.LimitExceeded,
                original,
                coding,
                "The encoded content exceeds the configured output limit.");
        }

        HttpContentCodingResult result = new HttpContentCodingResult();
        result.Outcome = HttpContentCodingOutcome.Encoded;
        result.Content = transformed;
        result.AppliedCodings.Add(coding);
        return result;
    }

    static HttpContentCodingResult DecodeKnownCore(
        byte[] content,
        string coding,
        int maximumOutputSize)
    {
        MemoryStream source = new MemoryStream(content, false);
        Stream decoder;
        if (Ascii.EqualsStringsIgnoreCase(coding, HttpContentCodings.Gzip) ||
            Ascii.EqualsStringsIgnoreCase(coding, HttpContentCodings.XGzip))
        {
            decoder = new GZipStream(source, CompressionMode.Decompress, false);
        }
        else if (Ascii.EqualsStringsIgnoreCase(coding, HttpContentCodings.Deflate))
        {
            decoder = new ZLibStream(source, CompressionMode.Decompress, false);
        }
        else
        {
            decoder = new BrotliStream(source, CompressionMode.Decompress, false);
        }

        MemoryStream destination = new MemoryStream();
        byte[] buffer = new byte[8192];
        bool exceeded = false;
        using (decoder)
        {
            int read = 1;
            while (read != 0 && !exceeded)
            {
                long remaining = (long)maximumOutputSize - destination.Length;
                int requested = buffer.Length;
                if (remaining < requested)
                {
                    requested = (int)remaining + 1;
                }

                read = decoder.Read(buffer, 0, requested);
                if (read > remaining)
                {
                    exceeded = true;
                }
                else if (read != 0)
                {
                    destination.Write(buffer, 0, read);
                }
            }
        }

        source.Dispose();
        if (exceeded)
        {
            destination.Dispose();
            return CreateFailure(
                HttpContentCodingOutcome.LimitExceeded,
                content,
                coding,
                "The decoded content exceeds the configured output limit.");
        }

        byte[] transformed = destination.ToArray();
        destination.Dispose();
        HttpContentCodingResult result = new HttpContentCodingResult();
        result.Outcome = HttpContentCodingOutcome.Decoded;
        result.Content = transformed;
        result.AppliedCodings.Add(coding);
        return result;
    }

    static HttpContentCodingResult CreateIdentity(
        byte[] content,
        string coding,
        int maximumOutputSize)
    {
        if (content.Length > maximumOutputSize)
        {
            return CreateFailure(
                HttpContentCodingOutcome.LimitExceeded,
                content,
                coding,
                "The identity content exceeds the configured output limit.");
        }

        HttpContentCodingResult result = new HttpContentCodingResult();
        result.Outcome = HttpContentCodingOutcome.Identity;
        result.Content = CopyContent(content);
        result.AppliedCodings.Add(coding);
        return result;
    }

    static HttpContentCodingResult CreateFailure(
        HttpContentCodingOutcome outcome,
        byte[] content,
        string coding,
        string reason)
    {
        HttpContentCodingResult result = new HttpContentCodingResult();
        result.Outcome = outcome;
        result.Content = CopyContent(content);
        result.RemainingCodings.Add(coding);
        result.Reason = reason;
        return result;
    }
}
