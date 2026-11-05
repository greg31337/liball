using System.Collections.Generic;
using System.IO;

namespace png;

internal class PngChunkListReader
{
    internal static PngChunkListResult ReadSpan(ReadOnlySpan<byte> data, int offset, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);

        if (offset < 0 || offset > data.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        List<PngChunk> chunks = new List<PngChunk>();
        int currentOffset = offset;
        while (currentOffset < data.Length)
        {
            EnforceChunkCountLimit(chunks.Count, limits);
            PngChunk chunk = PngRawChunkReader.ReadSpan(data, currentOffset, limits);
            chunks.Add(chunk);
            currentOffset += 12 + (int)chunk.Length;
            if (chunk.Type == PngConstants.ChunkIEND)
            {
                return CreateResult(chunks, CopyTrailingBytesSpan(data, currentOffset), true, limits);
            }
        }

        return CreateMissingIendResult(chunks, limits);
    }

    internal static byte[] CopyTrailingBytesSpan(ReadOnlySpan<byte> data, int offset)
    {
        if (offset >= data.Length)
        {
            return new byte[0];
        }

        byte[] trailingBytes = new byte[data.Length - offset];
        int index = 0;
        while (index < trailingBytes.Length)
        {
            trailingBytes[index] = data[offset + index];
            index += 1;
        }

        return trailingBytes;
    }

    internal static PngChunkListResult Read(byte[] data, int offset)
    {
        return Read(data, offset, new PngSecurityLimits());
    }

    internal static PngChunkListResult Read(byte[] data, int offset, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(limits);

        if (offset < 0 || offset > data.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        List<PngChunk> chunks = new List<PngChunk>();
        int currentOffset = offset;
        while (currentOffset < data.Length)
        {
            EnforceChunkCountLimit(chunks.Count, limits);
            PngChunk chunk = PngRawChunkReader.Read(data, currentOffset, limits);
            chunks.Add(chunk);
            currentOffset += 12 + (int)chunk.Length;
            if (chunk.Type == PngConstants.ChunkIEND)
            {
                return CreateResult(chunks, CopyTrailingBytes(data, currentOffset), true, limits);
            }
        }

        return CreateMissingIendResult(chunks, limits);
    }

    internal static PngChunkListResult Read(Stream stream)
    {
        return Read(stream, new PngSecurityLimits());
    }

    internal static PngChunkListResult Read(Stream stream, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(limits);

        List<PngChunk> chunks = new List<PngChunk>();
        long currentOffset = PngConstants.SignatureLength;
        if (stream.CanSeek)
        {
            currentOffset = stream.Position;
        }

        while (!IsAtEnd(stream))
        {
            EnforceChunkCountLimit(chunks.Count, limits);
            PngChunk chunk = PngRawChunkReader.Read(stream, limits);
            if (!stream.CanSeek)
            {
                chunk.Offset = currentOffset;
            }

            chunks.Add(chunk);
            currentOffset += 12 + chunk.Length;
            if (chunk.Type == PngConstants.ChunkIEND)
            {
                return CreateResult(chunks, ReadTrailingBytes(stream), true, limits);
            }
        }

        return CreateMissingIendResult(chunks, limits);
    }

    internal static void EnforceChunkCountLimit(int chunkCount, PngSecurityLimits limits)
    {
        if (limits.MaxChunkCount > 0 && chunkCount >= limits.MaxChunkCount)
        {
            throw new OverflowException();
        }
    }

    internal static bool IsAtEnd(Stream stream)
    {
        if (!stream.CanSeek)
        {
            return false;
        }

        return stream.Position >= stream.Length;
    }

    internal static byte[] CopyTrailingBytes(byte[] data, int offset)
    {
        if (offset >= data.Length)
        {
            return new byte[0];
        }

        byte[] trailingBytes = new byte[data.Length - offset];
        int index = 0;
        while (index < trailingBytes.Length)
        {
            trailingBytes[index] = data[offset + index];
            index += 1;
        }

        return trailingBytes;
    }

    internal static byte[] ReadTrailingBytes(Stream stream)
    {
        MemoryStream trailing = new MemoryStream();
        byte[] buffer = new byte[4096];
        while (true)
        {
            int read = stream.Read(buffer, 0, buffer.Length);
            if (read == 0)
            {
                break;
            }

            trailing.Write(buffer, 0, read);
        }

        return trailing.ToArray();
    }

    internal static PngChunkListResult CreateResult(List<PngChunk> chunks, byte[] trailingBytes, bool foundIend, PngSecurityLimits limits)
    {
        PngChunkListResult result = new PngChunkListResult();
        result.Chunks = chunks.ToArray();
        PngSecurityLimitEnforcer.EnforceDatastream(result.Chunks, limits);
        PngChunkPlacementProcessor.AnnotatePreservedChunks(result.Chunks);
        result.TrailingBytes = trailingBytes;
        result.FoundIend = foundIend;
        return result;
    }

    internal static PngChunkListResult CreateMissingIendResult(List<PngChunk> chunks, PngSecurityLimits limits)
    {
        PngChunkListResult result = CreateResult(chunks, new byte[0], false, limits);
        PngWarning warning = new PngWarning();
        warning.Code = "PNG_IEND_MISSING";
        warning.Message = "PNG chunk list ended before IEND.";
        result.Warnings = new PngWarning[] { warning };
        return result;
    }
}
