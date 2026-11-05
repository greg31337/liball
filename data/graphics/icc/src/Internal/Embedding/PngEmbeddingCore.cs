namespace icc;

internal static class PngEmbeddingCore
{
    internal const uint Ihdr = 0x49484452;
    internal const uint Plte = 0x504c5445;
    internal const uint Idat = 0x49444154;
    internal const uint Iend = 0x49454e44;
    internal const uint Iccp = 0x69434350;
    internal const uint Srgb = 0x73524742;
    internal const uint Cicp = 0x63494350;

    internal static PngProfileData Analyze(
        System.ReadOnlyMemory<byte> carrierBytes, IccResourceLimits limits)
    {
        PngProfileData result = new PngProfileData();
        if (!PngImageInspector.HasPngSignature(carrierBytes))
        {
            return Fail(result, IccOperationStatus.InvalidData,
                "The carrier does not begin with the PNG signature.", 0,
                carrierBytes.Length < 8 ? carrierBytes.Length : 8);
        }

        bool sawHeader = false;
        bool sawPalette = false;
        bool sawData = false;
        bool dataEnded = false;
        bool sawEnd = false;
        bool sawSrgb = false;
        bool sawCicp = false;
        int chunkIndex = 0;
        int offset = 8;
        while (offset < carrierBytes.Length && !sawEnd)
        {
            if (offset > carrierBytes.Length - 12)
            {
                return Fail(result, IccOperationStatus.InvalidData,
                    "The PNG carrier contains a truncated chunk header.",
                    offset, carrierBytes.Length - offset);
            }
            uint encodedLength = ReadUInt32(carrierBytes.Span, offset);
            if (encodedLength > int.MaxValue)
            {
                return Fail(result, IccOperationStatus.ResourceLimitExceeded,
                    "A PNG chunk exceeds the supported managed size.", offset,
                    4);
            }
            int length = (int)encodedLength;
            if (length > carrierBytes.Length - offset - 12)
            {
                return Fail(result, IccOperationStatus.InvalidData,
                    "A PNG chunk extends beyond the carrier bytes.", offset,
                    carrierBytes.Length - offset);
            }
            uint type = ReadUInt32(carrierBytes.Span, offset + 4);
            if (!IsValidChunkType(carrierBytes.Span, offset + 4))
            {
                return Fail(result, IccOperationStatus.InvalidData,
                    "A PNG chunk type is invalid or uses the reserved lowercase bit.",
                    offset + 4, 4);
            }
            uint expectedCrc = ReadUInt32(carrierBytes.Span,
                offset + 8 + length);
            uint actualCrc = PngImageInspector.ComputePngCrc(
                carrierBytes.Slice(offset + 4, length + 4));
            if (expectedCrc != actualCrc)
            {
                return Fail(result, IccOperationStatus.InvalidData,
                    "A PNG chunk has an invalid CRC-32 checksum.",
                    offset + 8 + length, 4);
            }

            PngChunkRegion chunk = new PngChunkRegion();
            chunk.Start = offset;
            chunk.End = offset + length + 12;
            chunk.DataOffset = offset + 8;
            chunk.DataLength = length;
            chunk.Type = type;
            if (chunkIndex == 0)
            {
                if (type != Ihdr || length != 13)
                {
                    return Fail(result, IccOperationStatus.InvalidData,
                        "The first PNG chunk must be a 13-byte IHDR chunk.",
                        offset, chunk.End - offset);
                }
                sawHeader = true;
                result.ColorType = carrierBytes.Span[chunk.DataOffset + 9];
                if (result.ColorType != 0 && result.ColorType != 2
                    && result.ColorType != 3 && result.ColorType != 4
                    && result.ColorType != 6)
                {
                    return Fail(result, IccOperationStatus.InvalidData,
                        "The PNG IHDR chunk contains an invalid color type.",
                        chunk.DataOffset + 9, 1);
                }
                result.InsertionOffset = chunk.End;
            }
            else if (type == Ihdr)
            {
                return Fail(result, IccOperationStatus.InvalidData,
                    "The PNG carrier contains more than one IHDR chunk.",
                    offset, chunk.End - offset);
            }
            else if (type == Plte)
            {
                if (sawPalette || sawData)
                {
                    return Fail(result, IccOperationStatus.InvalidData,
                        "The PNG PLTE chunk is duplicated or follows image data.",
                        offset, chunk.End - offset);
                }
                sawPalette = true;
                if (sawData)
                {
                    dataEnded = true;
                }
            }
            else if (type == Idat)
            {
                if (dataEnded)
                {
                    return Fail(result, IccOperationStatus.InvalidData,
                        "PNG IDAT chunks must be consecutive.", offset,
                        chunk.End - offset);
                }
                sawData = true;
            }
            else
            {
                if (sawData)
                {
                    dataEnded = true;
                }
                if (type == Iend)
                {
                    if (length != 0 || !sawData)
                    {
                        return Fail(result, IccOperationStatus.InvalidData,
                            "The PNG IEND chunk must be empty and follow image data.",
                            offset, chunk.End - offset);
                    }
                    sawEnd = true;
                }
                else if (type == Iccp)
                {
                    if (sawPalette || sawData)
                    {
                        return Fail(result, IccOperationStatus.InvalidData,
                            "The PNG iCCP chunk must precede PLTE and IDAT.",
                            offset, chunk.End - offset);
                    }
                    if (result.ProfileChunk != null)
                    {
                        return Fail(result, IccOperationStatus.InvalidData,
                            "The PNG carrier contains more than one iCCP chunk.",
                            offset, chunk.End - offset);
                    }
                    result.ProfileChunk = chunk;
                    DecodeProfileChunk(carrierBytes, limits, result, chunk);
                    if (!result.IsValid && result.Message.Length != 0)
                    {
                        return result;
                    }
                }
                else if (type == Srgb)
                {
                    if (sawSrgb || sawPalette || sawData)
                    {
                        return Fail(result, IccOperationStatus.InvalidData,
                            "The PNG sRGB chunk is duplicated or out of order.",
                            offset, chunk.End - offset);
                    }
                    sawSrgb = true;
                    result.HasSrgb = true;
                }
                else if (type == Cicp)
                {
                    if (sawCicp || sawPalette || sawData)
                    {
                        return Fail(result, IccOperationStatus.InvalidData,
                            "The PNG cICP chunk is duplicated or out of order.",
                            offset, chunk.End - offset);
                    }
                    sawCicp = true;
                    result.HasCicp = true;
                }
            }

            offset = chunk.End;
            chunkIndex = chunkIndex + 1;
        }

        if (!sawHeader || !sawData || !sawEnd)
        {
            return Fail(result, IccOperationStatus.InvalidData,
                "The PNG carrier is missing required IHDR, IDAT, or IEND content.",
                offset, carrierBytes.Length - offset);
        }
        if (offset != carrierBytes.Length)
        {
            return Fail(result, IccOperationStatus.InvalidData,
                "The PNG carrier contains bytes after IEND.", offset,
                carrierBytes.Length - offset);
        }

        result.IsValid = true;
        result.Status = IccOperationStatus.Success;
        result.Message = "The PNG chunk structure is valid.";
        if (result.HasProfile && (sawCicp || sawSrgb))
        {
            result.HasColorConflict = true;
            result.Status = IccOperationStatus.Conflict;
            result.Message = sawCicp
                ? "The PNG contains both cICP and iCCP color signaling; cICP has higher precedence."
                : "The PNG contains both sRGB and iCCP color signaling.";
        }
        else if (result.HasProfile)
        {
            result.Message = $"The PNG carrier contains the '{result.ProfileName}' ICC profile.";
        }
        return result;
    }

    internal static void DecodeProfileChunk(
        System.ReadOnlyMemory<byte> carrierBytes, IccResourceLimits limits,
        PngProfileData result, PngChunkRegion chunk)
    {
        int separator = FindNull(carrierBytes.Span, chunk.DataOffset,
            chunk.DataLength);
        int nameLength = separator - chunk.DataOffset;
        if (separator < 0 || !IsValidProfileName(carrierBytes.Span,
            chunk.DataOffset, nameLength))
        {
            Fail(result, IccOperationStatus.InvalidData,
                "The PNG iCCP profile name is invalid.", chunk.DataOffset,
                chunk.DataLength);
            return;
        }
        int methodOffset = separator + 1;
        if (methodOffset >= chunk.End - 4
            || carrierBytes.Span[methodOffset] != 0)
        {
            Fail(result, IccOperationStatus.InvalidData,
                "The PNG iCCP compression method is missing or unsupported.",
                methodOffset, methodOffset < chunk.End - 4 ? 1 : 0);
            return;
        }
        int compressedOffset = methodOffset + 1;
        int compressedLength = chunk.DataOffset + chunk.DataLength
            - compressedOffset;
        if (compressedLength == 0)
        {
            Fail(result, IccOperationStatus.InvalidData,
                "The PNG iCCP chunk contains no compressed profile data.",
                compressedOffset, 0);
            return;
        }
        int maximum = limits.MaximumProfileBytes
            < limits.MaximumDecompressedBytes
            ? limits.MaximumProfileBytes : limits.MaximumDecompressedBytes;
        ZlibDecodeResult decompressed = ZlibCodec.Decode(
            carrierBytes.Slice(compressedOffset, compressedLength), maximum);
        if (!decompressed.Success)
        {
            Fail(result, decompressed.LimitExceeded
                    ? IccOperationStatus.ResourceLimitExceeded
                    : IccOperationStatus.InvalidData,
                $"The PNG iCCP zlib stream is invalid: {decompressed.Error}",
                compressedOffset, compressedLength);
            return;
        }

        IccReadOptions probeOptions = new IccReadOptions();
        probeOptions.Mode = IccReadMode.Recovery;
        probeOptions.ResourceLimits = limits;
        IccOperationResult probe = new ProfileReaderCore().ProbeMemory(
            decompressed.Bytes, probeOptions);
        if (probe.Status != IccOperationStatus.Success)
        {
            Fail(result, probe.Status,
                $"The decompressed PNG iCCP profile is invalid: {probe.Message}",
                compressedOffset, compressedLength);
            return;
        }
        uint profileSpace = ReadUInt32(decompressed.Bytes, 16);
        bool grayImage = result.ColorType == 0 || result.ColorType == 4;
        bool colorImage = result.ColorType == 2 || result.ColorType == 3
            || result.ColorType == 6;
        if ((grayImage && profileSpace != IccColorSpaceSignatures.Gray)
            || (colorImage && profileSpace != IccColorSpaceSignatures.Rgb))
        {
            Fail(result, IccOperationStatus.Conflict,
                "The PNG image color type and embedded ICC profile data color space do not match.",
                chunk.Start, chunk.End - chunk.Start);
            return;
        }
        result.ProfileName = System.Text.Encoding.Latin1.GetString(
            carrierBytes.Span.Slice(chunk.DataOffset, nameLength));
        result.ProfileBytes = decompressed.Bytes;
        result.HasProfile = true;
        result.IsValid = true;
    }

    internal static byte[] InsertProfile(System.ReadOnlySpan<byte> carrierBytes,
        byte[] profileBytes, IccResourceLimits limits)
    {
        PngProfileData data = Analyze(carrierBytes.ToArray(), limits);
        RequireValidCarrier(data);
        if (data.HasProfile)
        {
            throw new IccEmbeddingException(
                "The PNG carrier already contains an iCCP profile; use replacement instead.");
        }
        RequireNoColorConflict(data);
        ValidateProfileColorSpace(profileBytes, data.ColorType);
        byte[] chunk = EncodeProfileChunk(profileBytes, "ICC Profile");
        return RewriteCarrier(carrierBytes, data, chunk,
            data.InsertionOffset, false, limits.MaximumCarrierBytes);
    }

    internal static byte[] ReplaceProfile(System.ReadOnlySpan<byte> carrierBytes,
        int profileIndex, byte[] profileBytes, IccResourceLimits limits)
    {
        RequireProfileIndex(profileIndex);
        PngProfileData data = Analyze(carrierBytes.ToArray(), limits);
        RequireValidCarrier(data);
        RequireExistingProfile(data);
        RequireNoColorConflict(data);
        ValidateProfileColorSpace(profileBytes, data.ColorType);
        byte[] chunk = EncodeProfileChunk(profileBytes, data.ProfileName);
        PngChunkRegion existing = RequireProfileChunk(data);
        return RewriteCarrier(carrierBytes, data, chunk, existing.Start,
            true, limits.MaximumCarrierBytes);
    }

    internal static byte[] RemoveProfile(System.ReadOnlySpan<byte> carrierBytes,
        int profileIndex, IccResourceLimits limits)
    {
        RequireProfileIndex(profileIndex);
        PngProfileData data = Analyze(carrierBytes.ToArray(), limits);
        RequireValidCarrier(data);
        RequireExistingProfile(data);
        PngChunkRegion existing = RequireProfileChunk(data);
        return RewriteCarrier(carrierBytes, data, new byte[0], existing.Start,
            true, limits.MaximumCarrierBytes);
    }

    internal static byte[] EncodeProfileChunk(byte[] profileBytes,
        string profileName)
    {
        byte[] name = System.Text.Encoding.Latin1.GetBytes(profileName);
        byte[] compressed = ZlibCodec.EncodeStored(profileBytes);
        long dataLengthValue = (long)name.Length + 2 + compressed.Length;
        long chunkLengthValue = dataLengthValue + 12;
        if (dataLengthValue > int.MaxValue
            || chunkLengthValue > System.Array.MaxLength)
        {
            throw new IccEmbeddingException(
                "The encoded PNG iCCP chunk exceeds the managed size limit.");
        }
        int dataLength = (int)dataLengthValue;
        byte[] chunk = new byte[(int)chunkLengthValue];
        WriteUInt32(chunk, 0, (uint)dataLength);
        WriteUInt32(chunk, 4, Iccp);
        System.Array.Copy(name, 0, chunk, 8, name.Length);
        chunk[8 + name.Length] = 0;
        chunk[9 + name.Length] = 0;
        System.Array.Copy(compressed, 0, chunk, 10 + name.Length,
            compressed.Length);
        uint crc = PngImageInspector.ComputePngCrc(
            new System.ReadOnlyMemory<byte>(chunk, 4, dataLength + 4));
        WriteUInt32(chunk, 8 + dataLength, crc);
        return chunk;
    }

    internal static byte[] RewriteCarrier(System.ReadOnlySpan<byte> carrierBytes,
        PngProfileData data, byte[] insertionBytes, int insertionOffset,
        bool removeExisting, int maximumCarrierBytes)
    {
        int removedBytes = 0;
        if (removeExisting)
        {
            PngChunkRegion existing = RequireProfileChunk(data);
            removedBytes = existing.End - existing.Start;
        }
        long outputLengthValue = (long)carrierBytes.Length - removedBytes
            + insertionBytes.Length;
        if (outputLengthValue > maximumCarrierBytes
            || outputLengthValue > System.Array.MaxLength)
        {
            throw new IccEmbeddingException(
                "The updated PNG carrier exceeds the configured or managed carrier-size limit.");
        }
        byte[] output = new byte[(int)outputLengthValue];
        int destinationOffset = 0;
        carrierBytes.Slice(0, insertionOffset).CopyTo(output);
        destinationOffset = insertionOffset;
        System.Array.Copy(insertionBytes, 0, output, destinationOffset,
            insertionBytes.Length);
        destinationOffset = destinationOffset + insertionBytes.Length;
        int sourceOffset = insertionOffset;
        if (removeExisting)
        {
            PngChunkRegion existing = RequireProfileChunk(data);
            sourceOffset = existing.End;
        }
        carrierBytes.Slice(sourceOffset).CopyTo(
            new System.Span<byte>(output, destinationOffset,
                carrierBytes.Length - sourceOffset));
        return output;
    }

    internal static void ValidateProfileColorSpace(byte[] profileBytes,
        int colorType)
    {
        if (profileBytes == null || profileBytes.Length < 20)
        {
            throw new IccEmbeddingException(
                "A complete ICC profile is required for PNG embedding.");
        }
        uint profileSpace = ReadUInt32(profileBytes, 16);
        bool grayImage = colorType == 0 || colorType == 4;
        bool colorImage = colorType == 2 || colorType == 3 || colorType == 6;
        if ((grayImage && profileSpace != IccColorSpaceSignatures.Gray)
            || (colorImage && profileSpace != IccColorSpaceSignatures.Rgb))
        {
            throw new IccEmbeddingException(
                "The PNG image color type and ICC profile data color space do not match.");
        }
    }

    internal static bool IsValidProfileName(System.ReadOnlySpan<byte> bytes,
        int offset, int length)
    {
        if (length < 1 || length > 79 || bytes[offset] == 0x20
            || bytes[offset + length - 1] == 0x20)
        {
            return false;
        }
        int index = 0;
        bool previousSpace = false;
        while (index < length)
        {
            int value = bytes[offset + index];
            bool printable = (value >= 0x20 && value <= 0x7e)
                || value >= 0xa1;
            if (!printable || (value == 0x20 && previousSpace))
            {
                return false;
            }
            previousSpace = value == 0x20;
            index = index + 1;
        }
        return true;
    }

    internal static int FindNull(System.ReadOnlySpan<byte> bytes, int offset,
        int length)
    {
        int index = 0;
        while (index < length)
        {
            if (bytes[offset + index] == 0)
            {
                return offset + index;
            }
            index = index + 1;
        }
        return -1;
    }

    internal static bool IsValidChunkType(System.ReadOnlySpan<byte> bytes,
        int offset)
    {
        int index = 0;
        while (index < 4)
        {
            int value = bytes[offset + index];
            if (!((value >= 0x41 && value <= 0x5a)
                || (value >= 0x61 && value <= 0x7a)))
            {
                return false;
            }
            index = index + 1;
        }
        return bytes[offset + 2] >= 0x41 && bytes[offset + 2] <= 0x5a;
    }

    internal static void RequireValidCarrier(PngProfileData data)
    {
        if (!data.IsValid)
        {
            throw new IccEmbeddingException(data.Message);
        }
    }

    internal static void RequireExistingProfile(PngProfileData data)
    {
        if (!data.HasProfile)
        {
            throw new IccEmbeddingException(
                "The PNG carrier does not contain an iCCP profile.");
        }
    }

    internal static void RequireNoColorConflict(PngProfileData data)
    {
        if (data.HasSrgb || data.HasCicp)
        {
            throw new IccEmbeddingException(data.HasCicp
                ? "PNG cICP color signaling takes precedence over iCCP; profile mutation is ambiguous."
                : "The PNG sRGB and iCCP color-signaling chunks cannot be combined by profile mutation.");
        }
    }

    internal static PngChunkRegion RequireProfileChunk(PngProfileData data)
    {
        if (data.ProfileChunk == null)
        {
            throw new IccEmbeddingException(
                "The PNG carrier does not contain an iCCP profile chunk.");
        }
        return data.ProfileChunk;
    }

    internal static void RequireProfileIndex(int profileIndex)
    {
        if (profileIndex != 0)
        {
            throw new IccEmbeddingException(
                "A PNG carrier exposes one iCCP profile at index zero.");
        }
    }

    internal static PngProfileData Fail(PngProfileData result,
        IccOperationStatus status, string message, int offset, int length)
    {
        result.IsValid = false;
        result.HasProfile = false;
        result.Status = status;
        result.Message = message;
        result.ErrorOffset = offset;
        result.ErrorLength = length;
        result.ProfileBytes = new byte[0];
        return result;
    }

    internal static uint ReadUInt32(System.ReadOnlySpan<byte> bytes, int offset)
    {
        return ((uint)bytes[offset] << 24)
            | ((uint)bytes[offset + 1] << 16)
            | ((uint)bytes[offset + 2] << 8)
            | bytes[offset + 3];
    }

    internal static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)((value >> 24) & 0xff);
        bytes[offset + 1] = (byte)((value >> 16) & 0xff);
        bytes[offset + 2] = (byte)((value >> 8) & 0xff);
        bytes[offset + 3] = (byte)(value & 0xff);
    }
}
