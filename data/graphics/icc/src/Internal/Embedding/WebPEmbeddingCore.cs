namespace icc;

internal static class WebPEmbeddingCore
{
    internal const uint Riff = 0x52494646;
    internal const uint WebP = 0x57454250;
    internal const uint Vp8x = 0x56503858;
    internal const uint Iccp = 0x49434350;
    internal const uint Vp8 = 0x56503820;
    internal const uint Vp8l = 0x5650384c;
    internal const uint Anim = 0x414e494d;
    internal const uint Anmf = 0x414e4d46;
    internal const uint Alph = 0x414c5048;
    internal const int IccFlag = 0x20;

    internal static WebPProfileData Analyze(
        System.ReadOnlyMemory<byte> carrierBytes, IccResourceLimits limits)
    {
        WebPProfileData result = new WebPProfileData();
        if (carrierBytes.Length < 12
            || ReadFourCc(carrierBytes.Span, 0) != Riff
            || ReadFourCc(carrierBytes.Span, 8) != WebP)
        {
            return Fail(result, IccOperationStatus.InvalidData,
                "The carrier does not contain a complete RIFF WEBP header.",
                0, carrierBytes.Length < 12 ? carrierBytes.Length : 12);
        }
        uint encodedRiffSize = ReadUInt32Little(carrierBytes.Span, 4);
        if ((encodedRiffSize & 1) != 0 || encodedRiffSize < 4
            || encodedRiffSize > 0xfffffff6)
        {
            return Fail(result, IccOperationStatus.InvalidData,
                "The WebP RIFF size is invalid.", 4, 4);
        }
        ulong encodedFileSize = (ulong)encodedRiffSize + 8;
        if (encodedFileSize != (ulong)carrierBytes.Length)
        {
            return Fail(result, IccOperationStatus.InvalidData,
                "The WebP RIFF size does not match the carrier length.",
                4, 4);
        }

        bool sawImage = false;
        bool profilePositionClosed = false;
        int chunkIndex = 0;
        int offset = 12;
        while (offset < carrierBytes.Length)
        {
            if (offset > carrierBytes.Length - 8)
            {
                return Fail(result, IccOperationStatus.InvalidData,
                    "The WebP carrier contains a truncated chunk header.",
                    offset, carrierBytes.Length - offset);
            }
            uint type = ReadFourCc(carrierBytes.Span, offset);
            uint encodedLength = ReadUInt32Little(carrierBytes.Span,
                offset + 4);
            if (encodedLength > int.MaxValue)
            {
                return Fail(result,
                    IccOperationStatus.ResourceLimitExceeded,
                    "A WebP chunk exceeds the supported managed size.",
                    offset + 4, 4);
            }
            int length = (int)encodedLength;
            int padding = length & 1;
            if (length > carrierBytes.Length - offset - 8
                || padding > carrierBytes.Length - offset - 8 - length)
            {
                return Fail(result, IccOperationStatus.InvalidData,
                    "A WebP chunk extends beyond the declared RIFF data.",
                    offset, carrierBytes.Length - offset);
            }
            if (padding != 0
                && carrierBytes.Span[offset + 8 + length] != 0)
            {
                return Fail(result, IccOperationStatus.InvalidData,
                    "An odd-sized WebP chunk has a nonzero RIFF padding byte.",
                    offset + 8 + length, 1);
            }

            WebPChunkRegion chunk = new WebPChunkRegion();
            chunk.Start = offset;
            chunk.End = offset + 8 + length + padding;
            chunk.DataOffset = offset + 8;
            chunk.DataLength = length;
            chunk.Type = type;
            if (type == Vp8x)
            {
                if (result.HasExtendedHeader || chunkIndex != 0
                    || length != 10)
                {
                    return Fail(result, IccOperationStatus.InvalidData,
                        "The WebP VP8X chunk must be unique, first, and 10 bytes long.",
                        offset, chunk.End - offset);
                }
                if ((carrierBytes.Span[chunk.DataOffset] & 0xc1) != 0
                    || carrierBytes.Span[chunk.DataOffset + 1] != 0
                    || carrierBytes.Span[chunk.DataOffset + 2] != 0
                    || carrierBytes.Span[chunk.DataOffset + 3] != 0)
                {
                    return Fail(result, IccOperationStatus.InvalidData,
                        "The WebP VP8X reserved flag or bytes are nonzero.",
                        chunk.DataOffset, 4);
                }
                int width = 1 + ReadUInt24Little(carrierBytes.Span,
                    chunk.DataOffset + 4);
                int height = 1 + ReadUInt24Little(carrierBytes.Span,
                    chunk.DataOffset + 7);
                if ((ulong)width * (ulong)height > uint.MaxValue)
                {
                    return Fail(result, IccOperationStatus.InvalidData,
                        "The WebP VP8X canvas dimensions exceed the permitted pixel count.",
                        chunk.DataOffset + 4, 6);
                }
                result.HasExtendedHeader = true;
                result.IccFlagSet = (carrierBytes.Span[chunk.DataOffset]
                    & IccFlag) != 0;
                result.ExtendedHeaderChunk = chunk;
            }
            else if (type == Iccp)
            {
                if (result.ProfileChunk != null)
                {
                    return Fail(result, IccOperationStatus.InvalidData,
                        "The WebP carrier contains more than one ICCP chunk.",
                        offset, chunk.End - offset);
                }
                if (profilePositionClosed)
                {
                    return Fail(result, IccOperationStatus.InvalidData,
                        "The WebP ICCP chunk must precede image data.",
                        offset, chunk.End - offset);
                }
                if (length > limits.MaximumProfileBytes)
                {
                    return Fail(result,
                        IccOperationStatus.ResourceLimitExceeded,
                        "The WebP ICCP profile exceeds the configured profile-size limit.",
                        chunk.DataOffset, length);
                }
                IccOperationResult probe = ProbeProfile(
                    carrierBytes.Slice(chunk.DataOffset, length), limits);
                if (probe.Status != IccOperationStatus.Success)
                {
                    return Fail(result, probe.Status,
                        $"The WebP ICCP profile is invalid: {probe.Message}",
                        chunk.DataOffset, length);
                }
                result.ProfileChunk = chunk;
                result.HasProfile = true;
            }
            else if (type == Vp8 || type == Vp8l || type == Anmf)
            {
                if (result.ImageChunk == null)
                {
                    result.ImageChunk = chunk;
                }
                sawImage = true;
                profilePositionClosed = true;
            }
            else if (type == Anim || type == Alph)
            {
                profilePositionClosed = true;
            }
            offset = chunk.End;
            chunkIndex = chunkIndex + 1;
        }

        if (!sawImage)
        {
            return Fail(result, IccOperationStatus.InvalidData,
                "The WebP carrier contains no image data chunk.",
                carrierBytes.Length, 0);
        }
        if (result.HasProfile && !result.HasExtendedHeader)
        {
            return Fail(result, IccOperationStatus.InvalidData,
                "A WebP ICCP chunk requires a preceding VP8X header.",
                result.ProfileChunk == null ? 12
                    : result.ProfileChunk.Start, 8);
        }
        if (result.HasExtendedHeader
            && result.IccFlagSet != result.HasProfile)
        {
            WebPChunkRegion extended = RequireExtendedHeader(result);
            return Fail(result, IccOperationStatus.InvalidData,
                "The WebP VP8X ICC flag and ICCP chunk presence disagree.",
                extended.DataOffset, 1);
        }

        result.IsValid = true;
        result.Status = IccOperationStatus.Success;
        result.Message = result.HasProfile
            ? "The WebP carrier contains one ICCP profile chunk."
            : "The WebP RIFF chunk structure is valid.";
        return result;
    }

    internal static IccOperationResult ProbeProfile(
        System.ReadOnlyMemory<byte> profileBytes, IccResourceLimits limits)
    {
        IccReadOptions options = new IccReadOptions();
        options.Mode = IccReadMode.Recovery;
        options.ResourceLimits = limits;
        return new ProfileReaderCore().ProbeMemory(profileBytes, options);
    }

    internal static byte[] InsertProfile(System.ReadOnlySpan<byte> carrierBytes,
        byte[] profileBytes, IccResourceLimits limits)
    {
        WebPProfileData data = Analyze(carrierBytes.ToArray(), limits);
        RequireValidCarrier(data);
        RequireProfileSize(profileBytes, limits);
        if (data.HasProfile)
        {
            throw new IccEmbeddingException(
                "The WebP carrier already contains an ICCP profile; use replacement instead.");
        }
        byte[] profileChunk = EncodeChunk(Iccp, profileBytes);
        if (data.HasExtendedHeader)
        {
            WebPChunkRegion extended = RequireExtendedHeader(data);
            byte[] output = InsertBytes(carrierBytes, extended.End,
                profileChunk, limits.MaximumCarrierBytes);
            output[extended.DataOffset] = (byte)(output[extended.DataOffset]
                | IccFlag);
            WriteRiffSize(output);
            return output;
        }

        WebPChunkRegion image = RequireImageChunk(data);
        WebPCanvasSize canvas = ReadCanvasSize(carrierBytes, image);
        byte[] extendedChunk = EncodeExtendedHeader(canvas);
        byte[] added = new byte[extendedChunk.Length + profileChunk.Length];
        System.Array.Copy(extendedChunk, added, extendedChunk.Length);
        System.Array.Copy(profileChunk, 0, added, extendedChunk.Length,
            profileChunk.Length);
        byte[] promoted = InsertBytes(carrierBytes, 12, added,
            limits.MaximumCarrierBytes);
        WriteRiffSize(promoted);
        return promoted;
    }

    internal static byte[] ReplaceProfile(
        System.ReadOnlySpan<byte> carrierBytes, int profileIndex,
        byte[] profileBytes, IccResourceLimits limits)
    {
        RequireProfileIndex(profileIndex);
        WebPProfileData data = Analyze(carrierBytes.ToArray(), limits);
        RequireValidCarrier(data);
        RequireProfileSize(profileBytes, limits);
        WebPChunkRegion profile = RequireProfileChunk(data);
        byte[] chunk = EncodeChunk(Iccp, profileBytes);
        byte[] output = ReplaceBytes(carrierBytes, profile.Start,
            profile.End, chunk, limits.MaximumCarrierBytes);
        WebPChunkRegion extended = RequireExtendedHeader(data);
        output[extended.DataOffset] = (byte)(output[extended.DataOffset]
            | IccFlag);
        WriteRiffSize(output);
        return output;
    }

    internal static byte[] RemoveProfile(System.ReadOnlySpan<byte> carrierBytes,
        int profileIndex, IccResourceLimits limits)
    {
        RequireProfileIndex(profileIndex);
        WebPProfileData data = Analyze(carrierBytes.ToArray(), limits);
        RequireValidCarrier(data);
        WebPChunkRegion profile = RequireProfileChunk(data);
        byte[] output = ReplaceBytes(carrierBytes, profile.Start,
            profile.End, new byte[0], limits.MaximumCarrierBytes);
        WebPChunkRegion extended = RequireExtendedHeader(data);
        output[extended.DataOffset] = (byte)(output[extended.DataOffset]
            & ~IccFlag);
        WriteRiffSize(output);
        return output;
    }

    internal static byte[] EncodeChunk(uint type, byte[] payload)
    {
        int padding = payload.Length & 1;
        long length = (long)payload.Length + 8 + padding;
        if (length > System.Array.MaxLength)
        {
            throw new IccEmbeddingException(
                "The encoded WebP chunk exceeds the managed size limit.");
        }
        byte[] chunk = new byte[(int)length];
        WriteFourCc(chunk, 0, type);
        WriteUInt32Little(chunk, 4, (uint)payload.Length);
        System.Array.Copy(payload, 0, chunk, 8, payload.Length);
        return chunk;
    }

    internal static byte[] EncodeExtendedHeader(WebPCanvasSize canvas)
    {
        byte[] payload = new byte[10];
        payload[0] = IccFlag;
        WriteUInt24Little(payload, 4, canvas.Width - 1);
        WriteUInt24Little(payload, 7, canvas.Height - 1);
        return EncodeChunk(Vp8x, payload);
    }

    internal static WebPCanvasSize ReadCanvasSize(
        System.ReadOnlySpan<byte> carrierBytes, WebPChunkRegion image)
    {
        WebPCanvasSize result = new WebPCanvasSize();
        if (image.Type == Vp8)
        {
            if (image.DataLength < 10
                || (carrierBytes[image.DataOffset] & 1) != 0
                || carrierBytes[image.DataOffset + 3] != 0x9d
                || carrierBytes[image.DataOffset + 4] != 0x01
                || carrierBytes[image.DataOffset + 5] != 0x2a)
            {
                throw new IccEmbeddingException(
                    "The simple VP8 bitstream header cannot supply canvas dimensions for VP8X promotion.");
            }
            result.Width = (int)(ReadUInt16Little(carrierBytes,
                image.DataOffset + 6) & 0x3fff);
            result.Height = (int)(ReadUInt16Little(carrierBytes,
                image.DataOffset + 8) & 0x3fff);
        }
        else if (image.Type == Vp8l)
        {
            if (image.DataLength < 5
                || carrierBytes[image.DataOffset] != 0x2f)
            {
                throw new IccEmbeddingException(
                    "The simple VP8L bitstream header cannot supply canvas dimensions for VP8X promotion.");
            }
            uint bits = ReadUInt32Little(carrierBytes,
                image.DataOffset + 1);
            if ((bits >> 29) != 0)
            {
                throw new IccEmbeddingException(
                    "The simple VP8L bitstream version is invalid.");
            }
            result.Width = (int)(bits & 0x3fff) + 1;
            result.Height = (int)((bits >> 14) & 0x3fff) + 1;
        }
        else
        {
            throw new IccEmbeddingException(
                "A simple WebP carrier has no VP8 or VP8L canvas header.");
        }
        if (result.Width <= 0 || result.Height <= 0
            || (ulong)result.Width * (ulong)result.Height > uint.MaxValue)
        {
            throw new IccEmbeddingException(
                "The simple WebP canvas dimensions are invalid.");
        }
        return result;
    }

    internal static byte[] InsertBytes(System.ReadOnlySpan<byte> source,
        int offset, byte[] addition, int maximumCarrierBytes)
    {
        long outputLength = (long)source.Length + addition.Length;
        RequireOutputLength(outputLength, maximumCarrierBytes);
        byte[] output = new byte[(int)outputLength];
        source.Slice(0, offset).CopyTo(output);
        System.Array.Copy(addition, 0, output, offset, addition.Length);
        source.Slice(offset).CopyTo(new System.Span<byte>(output,
            offset + addition.Length, source.Length - offset));
        return output;
    }

    internal static byte[] ReplaceBytes(System.ReadOnlySpan<byte> source,
        int start, int end, byte[] replacement, int maximumCarrierBytes)
    {
        long outputLength = (long)source.Length - (end - start)
            + replacement.Length;
        RequireOutputLength(outputLength, maximumCarrierBytes);
        byte[] output = new byte[(int)outputLength];
        source.Slice(0, start).CopyTo(output);
        System.Array.Copy(replacement, 0, output, start,
            replacement.Length);
        source.Slice(end).CopyTo(new System.Span<byte>(output,
            start + replacement.Length, source.Length - end));
        return output;
    }

    internal static void RequireOutputLength(long outputLength,
        int maximumCarrierBytes)
    {
        if (outputLength > maximumCarrierBytes
            || outputLength > System.Array.MaxLength
            || outputLength - 8 > 0xfffffff6)
        {
            throw new IccEmbeddingException(
                "The updated WebP carrier exceeds the configured, managed, or RIFF size limit.");
        }
    }

    internal static void RequireProfileSize(byte[] profileBytes,
        IccResourceLimits limits)
    {
        if (profileBytes == null || profileBytes.Length
            > limits.MaximumProfileBytes)
        {
            throw new IccEmbeddingException(
                "The ICC profile exceeds the configured WebP embedding limit.");
        }
    }

    internal static void RequireProfileIndex(int profileIndex)
    {
        if (profileIndex != 0)
        {
            throw new IccEmbeddingException(
                "A WebP carrier exposes one ICCP profile at index zero.");
        }
    }

    internal static void RequireValidCarrier(WebPProfileData data)
    {
        if (!data.IsValid)
        {
            throw new IccEmbeddingException(data.Message);
        }
    }

    internal static WebPChunkRegion RequireProfileChunk(
        WebPProfileData data)
    {
        if (data.ProfileChunk == null)
        {
            throw new IccEmbeddingException(
                "The WebP carrier does not contain an ICCP profile.");
        }
        return data.ProfileChunk;
    }

    internal static WebPChunkRegion RequireExtendedHeader(
        WebPProfileData data)
    {
        if (data.ExtendedHeaderChunk == null)
        {
            throw new IccEmbeddingException(
                "The WebP carrier does not contain a VP8X header.");
        }
        return data.ExtendedHeaderChunk;
    }

    internal static WebPChunkRegion RequireImageChunk(WebPProfileData data)
    {
        if (data.ImageChunk == null)
        {
            throw new IccEmbeddingException(
                "The WebP carrier does not contain image data.");
        }
        return data.ImageChunk;
    }

    internal static WebPProfileData Fail(WebPProfileData result,
        IccOperationStatus status, string message, int offset, int length)
    {
        result.IsValid = false;
        result.HasProfile = false;
        result.Status = status;
        result.Message = message;
        result.ErrorOffset = offset;
        result.ErrorLength = length;
        return result;
    }

    internal static uint ReadFourCc(System.ReadOnlySpan<byte> bytes,
        int offset)
    {
        return ((uint)bytes[offset] << 24)
            | ((uint)bytes[offset + 1] << 16)
            | ((uint)bytes[offset + 2] << 8)
            | bytes[offset + 3];
    }

    internal static uint ReadUInt32Little(System.ReadOnlySpan<byte> bytes,
        int offset)
    {
        return bytes[offset] | ((uint)bytes[offset + 1] << 8)
            | ((uint)bytes[offset + 2] << 16)
            | ((uint)bytes[offset + 3] << 24);
    }

    internal static uint ReadUInt16Little(System.ReadOnlySpan<byte> bytes,
        int offset)
    {
        return bytes[offset] | ((uint)bytes[offset + 1] << 8);
    }

    internal static int ReadUInt24Little(System.ReadOnlySpan<byte> bytes,
        int offset)
    {
        return bytes[offset] | (bytes[offset + 1] << 8)
            | (bytes[offset + 2] << 16);
    }

    internal static void WriteFourCc(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)((value >> 24) & 0xff);
        bytes[offset + 1] = (byte)((value >> 16) & 0xff);
        bytes[offset + 2] = (byte)((value >> 8) & 0xff);
        bytes[offset + 3] = (byte)(value & 0xff);
    }

    internal static void WriteUInt32Little(byte[] bytes, int offset,
        uint value)
    {
        bytes[offset] = (byte)(value & 0xff);
        bytes[offset + 1] = (byte)((value >> 8) & 0xff);
        bytes[offset + 2] = (byte)((value >> 16) & 0xff);
        bytes[offset + 3] = (byte)((value >> 24) & 0xff);
    }

    internal static void WriteUInt24Little(byte[] bytes, int offset,
        int value)
    {
        bytes[offset] = (byte)(value & 0xff);
        bytes[offset + 1] = (byte)((value >> 8) & 0xff);
        bytes[offset + 2] = (byte)((value >> 16) & 0xff);
    }

    internal static void WriteRiffSize(byte[] carrierBytes)
    {
        WriteUInt32Little(carrierBytes, 4,
            (uint)(carrierBytes.Length - 8));
    }
}
