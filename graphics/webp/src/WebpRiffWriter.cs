using System;
using System.Collections.Generic;

namespace webp;

internal static class WebpRiffWriter
{
    internal static long ComputeRiffSize(List<WebpRiffChunk> chunks)
    {
        ValidateChunks(chunks);

        long size = WebpConstants.FourCcByteCount;
        int index = 0;
        while (index < chunks.Count)
        {
            WebpRiffChunk chunk = chunks[index];
            long chunkSize = WebpConstants.ChunkHeaderSize + chunk.Payload.Length;
            if ((chunk.Payload.Length & 1) != 0)
            {
                chunkSize = chunkSize + 1;
            }

            if (size > WebpConstants.MaxRiffSizeField - chunkSize)
            {
                throw new InvalidOperationException("RIFF size exceeds the WebP maximum");
            }

            size = size + chunkSize;
            index = index + 1;
        }

        return size;
    }

    internal static byte[] Write(string formatFourCc, List<WebpRiffChunk> chunks)
    {
        ValidateFourCc(formatFourCc, "formatFourCc");
        long riffSize = ComputeRiffSize(chunks);
        if (riffSize + 8L > int.MaxValue)
        {
            throw new InvalidOperationException("RIFF output is too large for a byte array");
        }

        if (riffSize + 8L > WebpConstants.DefaultMaxAggregateAllocationBytes)
        {
            throw new InvalidOperationException("RIFF output exceeds the aggregate allocation security limit");
        }

        List<WebpRiffChunk> orderedChunks = OrderChunks(chunks);
        List<byte> data = new List<byte>((int)(riffSize + 8L));

        AppendFourCc(data, WebpConstants.FourCcRiffString);
        WebpEndianWriter.AppendUInt32(data, (uint)riffSize);
        AppendFourCc(data, formatFourCc);

        int index = 0;
        while (index < orderedChunks.Count)
        {
            WebpRiffChunk chunk = orderedChunks[index];
            AppendFourCc(data, chunk.FourCc);
            WebpEndianWriter.AppendUInt32(data, (uint)chunk.Payload.Length);
            WebpEndianWriter.AppendChunkPayload(data, chunk.Payload);
            index = index + 1;
        }

        return data.ToArray();
    }

    internal static List<WebpRiffChunk> OrderChunks(List<WebpRiffChunk> chunks)
    {
        ValidateChunks(chunks);

        List<WebpRiffChunk> remaining = new List<WebpRiffChunk>();
        int copyIndex = 0;
        while (copyIndex < chunks.Count)
        {
            WebpRiffChunk source = chunks[copyIndex];
            remaining.Add(new WebpRiffChunk(source.FourCc, source.Payload));
            copyIndex = copyIndex + 1;
        }

        List<WebpRiffChunk> ordered = new List<WebpRiffChunk>();
        while (remaining.Count > 0)
        {
            int bestIndex = 0;
            int index = 1;
            while (index < remaining.Count)
            {
                if (IsBefore(remaining[index], remaining[bestIndex]))
                {
                    bestIndex = index;
                }

                index = index + 1;
            }

            ordered.Add(remaining[bestIndex]);
            remaining.RemoveAt(bestIndex);
        }

        return ordered;
    }

    internal static bool IsBefore(WebpRiffChunk left, WebpRiffChunk right)
    {
        int leftOrder = GetChunkOrder(left.FourCc);
        int rightOrder = GetChunkOrder(right.FourCc);

        if (leftOrder < rightOrder)
        {
            return true;
        }

        if (leftOrder > rightOrder)
        {
            return false;
        }

        return string.CompareOrdinal(left.FourCc, right.FourCc) < 0;
    }

    internal static int GetChunkOrder(string fourCc)
    {
        if (fourCc == WebpConstants.FourCcVp8XString)
        {
            return 0;
        }

        if (fourCc == WebpConstants.FourCcIccProfileString)
        {
            return 1;
        }

        if (fourCc == WebpConstants.FourCcAnimString)
        {
            return 2;
        }

        if (fourCc == WebpConstants.FourCcAnimationFrameString)
        {
            return 3;
        }

        if (fourCc == WebpConstants.FourCcAlphaString)
        {
            return 4;
        }

        if (fourCc == WebpConstants.FourCcVp8String)
        {
            return 5;
        }

        if (fourCc == WebpConstants.FourCcVp8LString)
        {
            return 6;
        }

        if (fourCc == WebpConstants.FourCcExifString)
        {
            return 7;
        }

        if (fourCc == WebpConstants.FourCcXmpString)
        {
            return 8;
        }

        return 1000;
    }

    internal static void ValidateChunks(List<WebpRiffChunk> chunks)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        int index = 0;
        while (index < chunks.Count)
        {
            WebpRiffChunk chunk = chunks[index];
            if (chunk == null)
            {
                throw new ArgumentException("chunk list contains a null chunk", "chunks");
            }

            ValidateFourCc(chunk.FourCc, "chunks");
            ArgumentNullException.ThrowIfNull(chunk.Payload);
            index = index + 1;
        }
    }

    internal static void ValidateFourCc(string fourCc, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(fourCc);
        if (fourCc.Length != WebpConstants.FourCcByteCount)
        {
            throw new ArgumentException("FourCC values must be exactly four bytes", parameterName);
        }

        int index = 0;
        while (index < fourCc.Length)
        {
            if (fourCc[index] > 255)
            {
                throw new ArgumentException("FourCC values must contain only byte-sized characters", parameterName);
            }

            index = index + 1;
        }
    }

    internal static void AppendFourCc(List<byte> data, string fourCc)
    {
        data.Add((byte)fourCc[0]);
        data.Add((byte)fourCc[1]);
        data.Add((byte)fourCc[2]);
        data.Add((byte)fourCc[3]);
    }
}
