using System;
using System.Collections.Generic;

namespace webp;

internal static class WebpMetadataMux
{
    internal static WebpMetadata Extract(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return ExtractSpan(data.AsSpan());
    }

    internal static WebpMetadata ExtractSpan(ReadOnlySpan<byte> data)
    {
        WebpRiffParseResult riff = RequireValidWebpSpan(data);
        WebpMetadata metadata = new WebpMetadata();

        int index = 0;
        while (index < riff.Chunks.Count)
        {
            WebpChunkInfo chunk = riff.Chunks[index];
            ReadOnlySpan<byte> payload = WebpEndianReader.ReadSlice(data, (int)chunk.PayloadOffset, (int)chunk.PayloadLength);
            if (chunk.FourCc == WebpConstants.FourCcIccProfileString)
            {
                metadata.IccProfile = payload.ToArray();
            }
            else if (chunk.FourCc == WebpConstants.FourCcExifString)
            {
                metadata.Exif = payload.ToArray();
            }
            else if (chunk.FourCc == WebpConstants.FourCcXmpString)
            {
                metadata.Xmp = payload.ToArray();
            }

            index = index + 1;
        }

        return metadata;
    }

    internal static byte[] Replace(byte[] data, WebpMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(metadata);
        return ReplaceSpan(data.AsSpan(), metadata);
    }

    internal static byte[] ReplaceSpan(ReadOnlySpan<byte> data, WebpMetadata metadata)
    {
        ValidateReplacementMetadata(metadata);
        WebpRiffParseResult riff = RequireValidWebpSpan(data);
        List<WebpRiffChunk> chunks = new List<WebpRiffChunk>();
        bool hasVp8X = false;
        bool needsMetadata = HasAnyMetadata(metadata);

        int index = 0;
        while (index < riff.Chunks.Count)
        {
            WebpChunkInfo chunk = riff.Chunks[index];
            ReadOnlySpan<byte> payload = WebpEndianReader.ReadSlice(data, (int)chunk.PayloadOffset, (int)chunk.PayloadLength);

            if (IsMetadataChunk(chunk.FourCc))
            {
            }
            else if (chunk.FourCc == WebpConstants.FourCcVp8XString)
            {
                hasVp8X = true;
                chunks.Add(new WebpRiffChunk(chunk.FourCc, UpdateVp8XMetadataFlags(payload, metadata)));
            }
            else
            {
                chunks.Add(new WebpRiffChunk(chunk.FourCc, payload.ToArray()));
            }

            index = index + 1;
        }

        if (!hasVp8X && needsMetadata)
        {
            chunks.Add(new WebpRiffChunk(WebpConstants.FourCcVp8XString, CreateVp8XPayloadForBasicSpan(data, metadata)));
        }

        AddMetadataChunks(chunks, metadata);
        return WebpRiffWriter.Write(WebpConstants.FourCcWebpString, chunks);
    }

    internal static byte[] Strip(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return StripSpan(data.AsSpan());
    }

    internal static byte[] StripSpan(ReadOnlySpan<byte> data)
    {
        WebpMetadata metadata = new WebpMetadata();
        return ReplaceSpan(data, metadata);
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

    internal static void ValidateMetadataLimits(WebpMetadata metadata)
    {
        WebpSecurityLimits limits = new WebpSecurityLimits();
        long bytes = WebpDecoder.GetMetadataBytes(metadata);
        if (bytes > limits.MaxMetadataBytes)
        {
            throw new ArgumentException("metadata payload size exceeds the configured security limit", "metadata");
        }

        WebpSecurityLimitValidator.ValidateOutputBytes(bytes, limits, "metadata");
    }

    internal static void ValidateReplacementMetadata(WebpMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        WebpContainerWriter.ValidateMetadata(metadata);
        ValidateMetadataLimits(metadata);
    }

    internal static void ValidateOptionalMetadataPayload(byte[]? payload, string parameterName)
    {
        if (payload == null)
        {
            return;
        }

        if (payload.Length == 0)
        {
            throw new ArgumentException("metadata payload must not be empty", parameterName);
        }

        WebpSecurityLimits limits = new WebpSecurityLimits();
        if (payload.Length > limits.MaxMetadataBytes)
        {
            throw new ArgumentException("metadata payload size exceeds the configured security limit", parameterName);
        }

        WebpSecurityLimitValidator.ValidateOutputBytes(payload.Length, limits, parameterName);
    }

    internal static bool HasAnyMetadata(WebpMetadata metadata)
    {
        return metadata.IccProfile != null || metadata.Exif != null || metadata.Xmp != null;
    }

    internal static bool IsMetadataChunk(string fourCc)
    {
        return fourCc == WebpConstants.FourCcIccProfileString
            || fourCc == WebpConstants.FourCcExifString
            || fourCc == WebpConstants.FourCcXmpString;
    }

    internal static void AddMetadataChunks(List<WebpRiffChunk> chunks, WebpMetadata metadata)
    {
        if (metadata.IccProfile != null)
        {
            chunks.Add(new WebpRiffChunk(WebpConstants.FourCcIccProfileString, metadata.IccProfile));
        }

        if (metadata.Exif != null)
        {
            chunks.Add(new WebpRiffChunk(WebpConstants.FourCcExifString, metadata.Exif));
        }

        if (metadata.Xmp != null)
        {
            chunks.Add(new WebpRiffChunk(WebpConstants.FourCcXmpString, metadata.Xmp));
        }
    }

    internal static byte[] UpdateVp8XMetadataFlags(ReadOnlySpan<byte> payload, WebpMetadata metadata)
    {
        if (payload.Length != WebpConstants.Vp8XChunkPayloadSize)
        {
            throw new ArgumentException("VP8X payload must be exactly 10 bytes", "data");
        }

        byte[] updated = payload.ToArray();
        byte flags = updated[0];
        flags = (byte)(flags & 0xD3);
        flags = ApplyMetadataFlags(flags, metadata);
        updated[0] = flags;
        return updated;
    }

    internal static byte[] CreateVp8XPayloadForBasic(byte[] data, WebpMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(data);
        return CreateVp8XPayloadForBasicSpan(data.AsSpan(), metadata);
    }

    internal static byte[] CreateVp8XPayloadForBasicSpan(ReadOnlySpan<byte> data, WebpMetadata metadata)
    {
        WebpBasicContainerParseResult basic = WebpBasicContainerParser.Parse(data, WebpStrictness.Permissive);
        if (basic.HasErrors())
        {
            throw new ArgumentException("metadata insertion requires either VP8X or a valid basic still-image container", "data");
        }

        byte[] payload = new byte[WebpConstants.Vp8XChunkPayloadSize];
        byte flags = 0;
        if (basic.HasAlpha)
        {
            flags = (byte)(flags | WebpConstants.Vp8XFeatureAlpha);
        }

        flags = ApplyMetadataFlags(flags, metadata);
        payload[0] = flags;
        WebpEndianWriter.WriteUInt24(payload, 4, (uint)(basic.Width - 1));
        WebpEndianWriter.WriteUInt24(payload, 7, (uint)(basic.Height - 1));
        return payload;
    }

    internal static byte ApplyMetadataFlags(byte flags, WebpMetadata metadata)
    {
        if (metadata.IccProfile != null)
        {
            flags = (byte)(flags | WebpConstants.Vp8XFeatureIccProfile);
        }

        if (metadata.Exif != null)
        {
            flags = (byte)(flags | WebpConstants.Vp8XFeatureExif);
        }

        if (metadata.Xmp != null)
        {
            flags = (byte)(flags | WebpConstants.Vp8XFeatureXmp);
        }

        return flags;
    }
}
