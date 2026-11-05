using System;
using System.Collections.Generic;

namespace webp;

internal static class WebpUnknownChunkMux
{
    internal static List<WebpUnknownChunk> Enumerate(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return EnumerateSpan(data.AsSpan());
    }

    internal static List<WebpUnknownChunk> EnumerateSpan(ReadOnlySpan<byte> data)
    {
        WebpRiffParseResult riff = RequireValidWebpSpan(data);
        List<WebpUnknownChunk> unknownChunks = new List<WebpUnknownChunk>();

        int index = 0;
        while (index < riff.Chunks.Count)
        {
            WebpChunkInfo chunk = riff.Chunks[index];
            if (IsUnknownChunk(chunk.FourCc))
            {
                ReadOnlySpan<byte> payload = WebpEndianReader.ReadSlice(data, (int)chunk.PayloadOffset, (int)chunk.PayloadLength);
                unknownChunks.Add(new WebpUnknownChunk(chunk.FourCc, payload.ToArray()));
            }

            index = index + 1;
        }

        return unknownChunks;
    }

    internal static byte[] Preserve(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return PreserveSpan(data.AsSpan());
    }

    internal static byte[] PreserveSpan(ReadOnlySpan<byte> data)
    {
        WebpRiffParseResult riff = RequireValidWebpSpan(data);
        List<WebpRiffChunk> chunks = CopyChunks(data, riff, true);
        return WritePreservingOrder(riff.FormatFourCc, chunks);
    }

    internal static byte[] Strip(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return StripSpan(data.AsSpan());
    }

    internal static byte[] StripSpan(ReadOnlySpan<byte> data)
    {
        WebpRiffParseResult riff = RequireValidWebpSpan(data);
        List<WebpRiffChunk> chunks = CopyChunks(data, riff, false);
        return WritePreservingOrder(riff.FormatFourCc, chunks);
    }

    internal static byte[] Replace(byte[] data, List<WebpUnknownChunk> replacementUnknownChunks)
    {
        ArgumentNullException.ThrowIfNull(data);
        return ReplaceSpan(data.AsSpan(), replacementUnknownChunks, WebpUnknownChunkPlacement.AfterRecognizedChunks);
    }

    internal static byte[] ReplaceSpan(ReadOnlySpan<byte> data, List<WebpUnknownChunk> replacementUnknownChunks, WebpUnknownChunkPlacement placement)
    {
        ValidateReplacementUnknownChunks(replacementUnknownChunks);
        ValidatePlacement(placement);

        WebpRiffParseResult riff = RequireValidWebpSpan(data);
        List<WebpRiffChunk> recognizedChunks = CopyChunks(data, riff, false);
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        if (placement == WebpUnknownChunkPlacement.BeforeRecognizedChunks)
        {
            AddReplacementChunks(chunks, replacementUnknownChunks);
            AddChunks(chunks, recognizedChunks);
        }
        else
        {
            AddChunks(chunks, recognizedChunks);
            AddReplacementChunks(chunks, replacementUnknownChunks);
        }

        return WritePreservingOrder(riff.FormatFourCc, chunks);
    }

    internal static void AddReplacementChunks(List<WebpRiffChunk> chunks, List<WebpUnknownChunk> replacementUnknownChunks)
    {
        int index = 0;
        while (index < replacementUnknownChunks.Count)
        {
            WebpUnknownChunk unknownChunk = replacementUnknownChunks[index];
            chunks.Add(new WebpRiffChunk(unknownChunk.FourCc, unknownChunk.Data));
            index = index + 1;
        }
    }

    internal static void AddChunks(List<WebpRiffChunk> destination, List<WebpRiffChunk> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }

    internal static List<WebpRiffChunk> CopyChunks(ReadOnlySpan<byte> data, WebpRiffParseResult riff, bool includeUnknownChunks)
    {
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();

        int index = 0;
        while (index < riff.Chunks.Count)
        {
            WebpChunkInfo chunk = riff.Chunks[index];
            if (includeUnknownChunks || !IsUnknownChunk(chunk.FourCc))
            {
                ReadOnlySpan<byte> payload = WebpEndianReader.ReadSlice(data, (int)chunk.PayloadOffset, (int)chunk.PayloadLength);
                chunks.Add(new WebpRiffChunk(chunk.FourCc, payload.ToArray()));
            }

            index = index + 1;
        }

        return chunks;
    }

    internal static WebpRiffParseResult RequireValidWebp(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return RequireValidWebpSpan(data.AsSpan());
    }

    internal static WebpRiffParseResult RequireValidWebpSpan(ReadOnlySpan<byte> data)
    {
        WebpSecurityLimits limits = new WebpSecurityLimits();
        string? limitError = WebpSecurityLimitValidator.GetContainerLimitError(data, limits);
        if (limitError != null)
        {
            throw new ArgumentException(limitError, "data");
        }

        WebpValidationResult signature = WebpSignature.ValidateRiffWebp(data);
        if (signature.HasErrors())
        {
            throw new ArgumentException("input is not a valid RIFF WEBP container", "data");
        }

        WebpRiffParseResult riff = WebpRiffParser.Parse(data);
        if (riff.HasErrors())
        {
            throw new ArgumentException("input is not a valid RIFF container", "data");
        }

        return riff;
    }

    internal static bool IsUnknownChunk(string fourCc)
    {
        return !IsKnownWebpChunk(fourCc);
    }

    internal static bool IsKnownWebpChunk(string fourCc)
    {
        return fourCc == WebpConstants.FourCcVp8XString
            || fourCc == WebpConstants.FourCcIccProfileString
            || fourCc == WebpConstants.FourCcAnimString
            || fourCc == WebpConstants.FourCcAnimationFrameString
            || fourCc == WebpConstants.FourCcAlphaString
            || fourCc == WebpConstants.FourCcVp8String
            || fourCc == WebpConstants.FourCcVp8LString
            || fourCc == WebpConstants.FourCcExifString
            || fourCc == WebpConstants.FourCcXmpString;
    }

    internal static void ValidateReplacementUnknownChunks(List<WebpUnknownChunk> unknownChunks)
    {
        ArgumentNullException.ThrowIfNull(unknownChunks);

        long payloadBytes = 0;
        int index = 0;
        while (index < unknownChunks.Count)
        {
            WebpUnknownChunk unknownChunk = unknownChunks[index];
            if (unknownChunk == null)
            {
                throw new ArgumentException("unknown chunk list contains a null chunk", "replacementUnknownChunks");
            }

            ValidateFourCc(unknownChunk.FourCc, "replacementUnknownChunks");
            if (!IsUnknownChunk(unknownChunk.FourCc))
            {
                throw new ArgumentException("replacement chunks must not use known WebP FourCC values", "replacementUnknownChunks");
            }

            ArgumentNullException.ThrowIfNull(unknownChunk.Data);
            payloadBytes = WebpSecurityLimitValidator.AddBytes(payloadBytes, unknownChunk.Data.Length);
            index = index + 1;
        }

        WebpSecurityLimits limits = new WebpSecurityLimits();
        if (payloadBytes > limits.MaxUnknownChunkBytes)
        {
            throw new ArgumentException("unknown chunk payload size exceeds the configured security limit", "replacementUnknownChunks");
        }

        WebpSecurityLimitValidator.ValidateOutputBytes(payloadBytes, limits, "replacementUnknownChunks");
    }

    internal static void ValidatePlacement(WebpUnknownChunkPlacement placement)
    {
        if (placement != WebpUnknownChunkPlacement.BeforeRecognizedChunks
            && placement != WebpUnknownChunkPlacement.AfterRecognizedChunks)
        {
            throw new ArgumentOutOfRangeException("placement");
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

    internal static byte[] WritePreservingOrder(string formatFourCc, List<WebpRiffChunk> chunks)
    {
        long riffSize = WebpRiffWriter.ComputeRiffSize(chunks);
        if (riffSize + 8L > int.MaxValue)
        {
            throw new InvalidOperationException("RIFF output is too large for a byte array");
        }

        if (riffSize + 8L > WebpConstants.DefaultMaxAggregateAllocationBytes)
        {
            throw new InvalidOperationException("RIFF output exceeds the aggregate allocation security limit");
        }

        List<byte> data = new List<byte>((int)(riffSize + 8L));
        AppendFourCc(data, WebpConstants.FourCcRiffString);
        WebpEndianWriter.AppendUInt32(data, (uint)riffSize);
        AppendFourCc(data, formatFourCc);

        int index = 0;
        while (index < chunks.Count)
        {
            WebpRiffChunk chunk = chunks[index];
            AppendFourCc(data, chunk.FourCc);
            WebpEndianWriter.AppendUInt32(data, (uint)chunk.Payload.Length);
            WebpEndianWriter.AppendChunkPayload(data, chunk.Payload);
            index = index + 1;
        }

        return data.ToArray();
    }

    internal static void AppendFourCc(List<byte> data, string fourCc)
    {
        data.Add((byte)fourCc[0]);
        data.Add((byte)fourCc[1]);
        data.Add((byte)fourCc[2]);
        data.Add((byte)fourCc[3]);
    }
}
