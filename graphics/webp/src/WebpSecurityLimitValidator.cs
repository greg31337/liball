using System;

namespace webp;

internal class WebpContainerAllocationSummary
{
    internal long MetadataBytes;
    internal long UnknownChunkBytes;
    internal long RetainedPayloadBytes;
    internal long ChunkCount;
    internal long FrameCount;
    internal int LargestWidth;
    internal int LargestHeight;
    internal long LargestPixelRectangle;
    internal long CanvasPixels;
    internal long AnimationPixels;
    internal bool HasCompressedAlpha;

    internal WebpContainerAllocationSummary()
    {
        MetadataBytes = 0;
        UnknownChunkBytes = 0;
        RetainedPayloadBytes = 0;
        ChunkCount = 0;
        FrameCount = 0;
        LargestWidth = 0;
        LargestHeight = 0;
        LargestPixelRectangle = 0;
        CanvasPixels = 0;
        AnimationPixels = 0;
        HasCompressedAlpha = false;
    }
}

internal static class WebpSecurityLimitValidator
{
    internal static void ValidateLimits(WebpSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        if (limits.MaxMetadataBytes <= 0)
        {
            throw new ArgumentOutOfRangeException("MaxMetadataBytes");
        }

        if (limits.MaxUnknownChunkBytes <= 0)
        {
            throw new ArgumentOutOfRangeException("MaxUnknownChunkBytes");
        }

        if (limits.MaxAggregateAllocationBytes <= 0)
        {
            throw new ArgumentOutOfRangeException("MaxAggregateAllocationBytes");
        }
    }

    internal static string? GetContainerLimitError(ReadOnlySpan<byte> data, WebpSecurityLimits limits)
    {
        if (data.Length > limits.MaxRiffSize)
        {
            return "input RIFF size exceeds the configured security limit";
        }

        WebpContainerAllocationSummary summary = InspectContainer(data);
        if (summary.ChunkCount > limits.MaxChunkCount)
        {
            return "chunk count exceeds the configured security limit";
        }

        if (summary.FrameCount > limits.MaxFrameCount)
        {
            return "animation frame count exceeds the configured security limit";
        }

        if (summary.LargestWidth > limits.MaxCanvasWidth || summary.LargestHeight > limits.MaxCanvasHeight)
        {
            return "image dimensions exceed the configured maximum pixel count or canvas limits";
        }

        if (summary.LargestPixelRectangle > limits.MaxCanvasPixels)
        {
            return "image dimensions exceed the configured maximum pixel count or canvas limits";
        }

        long totalPixels = summary.FrameCount == 0 ? summary.CanvasPixels : summary.AnimationPixels;
        if (totalPixels > limits.MaxTotalPixels)
        {
            return "total pixel count exceeds the configured security limit";
        }

        if (summary.HasCompressedAlpha && limits.MaxRecursionDepth < 2)
        {
            return "compressed alpha decode exceeds the configured recursion depth limit";
        }

        if (summary.MetadataBytes > limits.MaxMetadataBytes)
        {
            return "metadata payload size exceeds the configured security limit";
        }

        if (summary.UnknownChunkBytes > limits.MaxUnknownChunkBytes)
        {
            return "unknown chunk payload size exceeds the configured security limit";
        }

        if (summary.RetainedPayloadBytes > limits.MaxAggregateAllocationBytes)
        {
            return "aggregate allocation size exceeds the configured security limit";
        }

        return null;
    }

    internal static WebpContainerAllocationSummary InspectContainer(ReadOnlySpan<byte> data)
    {
        WebpContainerAllocationSummary summary = new WebpContainerAllocationSummary();
        WebpRiffParseResult riff = WebpRiffParser.Parse(data);
        bool hasVp8X = HasChunk(riff, WebpConstants.FourCcVp8XString);
        summary.ChunkCount = riff.Chunks.Count;
        int index = 0;
        while (index < riff.Chunks.Count)
        {
            WebpChunkInfo chunk = riff.Chunks[index];
            if (IsMetadataChunk(chunk.FourCc))
            {
                AddMetadata(summary, chunk.PayloadLength);
            }
            else if (WebpUnknownChunkMux.IsUnknownChunk(chunk.FourCc))
            {
                AddUnknown(summary, chunk.PayloadLength);
            }
            else if (chunk.FourCc == WebpConstants.FourCcAnimationFrameString)
            {
                summary.FrameCount = AddBytes(summary.FrameCount, 1);
                InspectAnimationFrame(summary, data, chunk);
            }
            else if (chunk.FourCc == WebpConstants.FourCcVp8XString)
            {
                InspectVp8X(summary, data, chunk);
            }
            else if (chunk.FourCc == WebpConstants.FourCcAlphaString)
            {
                InspectAlpha(summary, data, chunk);
                summary.RetainedPayloadBytes = AddBytes(summary.RetainedPayloadBytes, chunk.PayloadLength);
            }
            else if (chunk.FourCc == WebpConstants.FourCcVp8String || chunk.FourCc == WebpConstants.FourCcVp8LString)
            {
                if (!hasVp8X)
                {
                    InspectBasicImage(summary, data, chunk);
                }

                summary.RetainedPayloadBytes = AddBytes(summary.RetainedPayloadBytes, chunk.PayloadLength);
            }
            else if (ShouldRetainKnownPayload(hasVp8X, chunk.FourCc))
            {
                summary.RetainedPayloadBytes = AddBytes(summary.RetainedPayloadBytes, chunk.PayloadLength);
            }

            index = index + 1;
        }

        return summary;
    }

    internal static void InspectAnimationFrame(WebpContainerAllocationSummary summary, ReadOnlySpan<byte> data, WebpChunkInfo frameChunk)
    {
        if (frameChunk.PayloadLength < WebpConstants.AnimFrameHeaderSize || !CanReadPayload(data, frameChunk))
        {
            return;
        }

        ReadOnlySpan<byte> payload = WebpEndianReader.ReadSlice(data, (int)frameChunk.PayloadOffset, (int)frameChunk.PayloadLength);
        int width = (int)WebpEndianReader.ReadUInt24(payload, 6) + 1;
        int height = (int)WebpEndianReader.ReadUInt24(payload, 9) + 1;
        AddRectangle(summary, width, height, true);
        int offset = WebpConstants.AnimFrameHeaderSize;
        while (offset < payload.Length)
        {
            if (payload.Length - offset < WebpConstants.ChunkHeaderSize)
            {
                return;
            }

            string fourCc = WebpRiffParser.ReadFourCc(payload, offset);
            uint payloadLength = WebpEndianReader.ReadUInt32(payload, offset + WebpConstants.FourCcByteCount);
            long payloadEnd = (long)offset + WebpConstants.ChunkHeaderSize + payloadLength;
            long paddedEnd = payloadEnd + (payloadLength & 1U);
            if (paddedEnd > payload.Length)
            {
                return;
            }

            if (fourCc == WebpConstants.FourCcAlphaString
                || fourCc == WebpConstants.FourCcVp8String
                || fourCc == WebpConstants.FourCcVp8LString)
            {
                summary.RetainedPayloadBytes = AddBytes(summary.RetainedPayloadBytes, payloadLength);
                if (fourCc == WebpConstants.FourCcAlphaString && payloadLength > 0)
                {
                    int headerOffset = offset + WebpConstants.ChunkHeaderSize;
                    summary.HasCompressedAlpha = summary.HasCompressedAlpha
                        || (payload[headerOffset] & WebpConstants.AlphaCompressionMask) == WebpConstants.AlphaCompressionLossless;
                }
            }
            else
            {
                AddUnknown(summary, payloadLength);
            }

            summary.ChunkCount = AddBytes(summary.ChunkCount, 1);
            offset = (int)paddedEnd;
        }
    }

    internal static void InspectVp8X(WebpContainerAllocationSummary summary, ReadOnlySpan<byte> data, WebpChunkInfo chunk)
    {
        if (chunk.PayloadLength != WebpConstants.Vp8XChunkPayloadSize || !CanReadPayload(data, chunk))
        {
            return;
        }

        ReadOnlySpan<byte> payload = WebpEndianReader.ReadSlice(data, (int)chunk.PayloadOffset, WebpConstants.Vp8XChunkPayloadSize);
        int width = (int)WebpEndianReader.ReadUInt24(payload, 4) + 1;
        int height = (int)WebpEndianReader.ReadUInt24(payload, 7) + 1;
        AddRectangle(summary, width, height, false);
    }

    internal static void InspectBasicImage(WebpContainerAllocationSummary summary, ReadOnlySpan<byte> data, WebpChunkInfo chunk)
    {
        if (!CanReadPayload(data, chunk))
        {
            return;
        }

        ReadOnlySpan<byte> payload = WebpEndianReader.ReadSlice(data, (int)chunk.PayloadOffset, (int)chunk.PayloadLength);
        if (chunk.FourCc == WebpConstants.FourCcVp8String && payload.Length >= WebpConstants.Vp8LossyKeyFrameHeaderSize)
        {
            int width = WebpEndianReader.ReadUInt16(payload, 6) & 0x3FFF;
            int height = WebpEndianReader.ReadUInt16(payload, 8) & 0x3FFF;
            AddRectangle(summary, width, height, false);
        }
        else if (chunk.FourCc == WebpConstants.FourCcVp8LString && payload.Length >= WebpConstants.Vp8LHeaderSize)
        {
            uint bits = WebpEndianReader.ReadUInt32(payload, 1);
            int width = (int)(bits & 0x3FFFU) + 1;
            int height = (int)((bits >> 14) & 0x3FFFU) + 1;
            AddRectangle(summary, width, height, false);
        }
    }

    internal static void InspectAlpha(WebpContainerAllocationSummary summary, ReadOnlySpan<byte> data, WebpChunkInfo chunk)
    {
        if (chunk.PayloadLength == 0 || !CanReadPayload(data, chunk))
        {
            return;
        }

        byte header = data[(int)chunk.PayloadOffset];
        summary.HasCompressedAlpha = summary.HasCompressedAlpha
            || (header & WebpConstants.AlphaCompressionMask) == WebpConstants.AlphaCompressionLossless;
    }

    internal static void AddRectangle(WebpContainerAllocationSummary summary, int width, int height, bool animationFrame)
    {
        if (width > summary.LargestWidth)
        {
            summary.LargestWidth = width;
        }

        if (height > summary.LargestHeight)
        {
            summary.LargestHeight = height;
        }

        long pixels = (long)width * (long)height;
        if (pixels > summary.LargestPixelRectangle)
        {
            summary.LargestPixelRectangle = pixels;
        }

        if (animationFrame)
        {
            summary.AnimationPixels = AddBytes(summary.AnimationPixels, pixels);
        }
        else
        {
            summary.CanvasPixels = pixels;
        }
    }

    internal static bool CanReadPayload(ReadOnlySpan<byte> data, WebpChunkInfo chunk)
    {
        if (chunk.PayloadOffset < 0 || chunk.PayloadLength > int.MaxValue)
        {
            return false;
        }

        long payloadEnd = chunk.PayloadOffset + chunk.PayloadLength;
        return payloadEnd >= chunk.PayloadOffset && payloadEnd <= data.Length;
    }

    internal static bool HasChunk(WebpRiffParseResult riff, string fourCc)
    {
        int index = 0;
        while (index < riff.Chunks.Count)
        {
            if (riff.Chunks[index].FourCc == fourCc)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool IsMetadataChunk(string fourCc)
    {
        return fourCc == WebpConstants.FourCcIccProfileString
            || fourCc == WebpConstants.FourCcExifString
            || fourCc == WebpConstants.FourCcXmpString;
    }

    internal static bool ShouldRetainKnownPayload(bool hasVp8X, string fourCc)
    {
        if (fourCc == WebpConstants.FourCcAlphaString)
        {
            return true;
        }

        if (fourCc == WebpConstants.FourCcVp8String || fourCc == WebpConstants.FourCcVp8LString)
        {
            return true;
        }

        return !hasVp8X && (fourCc == WebpConstants.FourCcVp8String || fourCc == WebpConstants.FourCcVp8LString);
    }

    internal static void AddMetadata(WebpContainerAllocationSummary summary, long bytes)
    {
        summary.MetadataBytes = AddBytes(summary.MetadataBytes, bytes);
        summary.RetainedPayloadBytes = AddBytes(summary.RetainedPayloadBytes, bytes);
    }

    internal static void AddUnknown(WebpContainerAllocationSummary summary, long bytes)
    {
        summary.UnknownChunkBytes = AddBytes(summary.UnknownChunkBytes, bytes);
        summary.RetainedPayloadBytes = AddBytes(summary.RetainedPayloadBytes, bytes);
    }

    internal static long AddBytes(long current, long bytes)
    {
        if (bytes < 0 || current > long.MaxValue - bytes)
        {
            return long.MaxValue;
        }

        return current + bytes;
    }

    internal static void ValidateOutputBytes(long bytes, WebpSecurityLimits limits, string parameterName)
    {
        if (bytes > limits.MaxAggregateAllocationBytes)
        {
            throw new ArgumentException("aggregate allocation size exceeds the configured security limit", parameterName);
        }
    }

    internal static long GetInputByteLimit(WebpSecurityLimits limits)
    {
        if (limits.MaxRiffSize < limits.MaxAggregateAllocationBytes)
        {
            return limits.MaxRiffSize;
        }

        return limits.MaxAggregateAllocationBytes;
    }
}
