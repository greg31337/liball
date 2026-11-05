namespace icc;

internal static class Jpeg2000EmbeddingCore
{
    internal const uint Signature = 0x6a502020;
    internal const uint FileType = 0x66747970;
    internal const uint Jp2Header = 0x6a703268;
    internal const uint LayerHeader = 0x6a706c68;
    internal const uint ColourGroup = 0x63677270;
    internal const uint ColourSpecification = 0x636f6c72;
    internal const uint Jp2Brand = 0x6a703220;
    internal const uint JpxBrand = 0x6a707820;
    internal const uint JpxBaselineBrand = 0x6a707862;

    internal static Jpeg2000ProfileData Analyze(
        System.ReadOnlyMemory<byte> carrierBytes, IccResourceLimits limits)
    {
        Jpeg2000ProfileData result = new Jpeg2000ProfileData();
        if (carrierBytes.Length < 12
            || ReadUInt32(carrierBytes.Span, 0) != 12
            || ReadUInt32(carrierBytes.Span, 4) != Signature
            || ReadUInt32(carrierBytes.Span, 8) != 0x0d0a870a)
        {
            return Fail(result, IccOperationStatus.InvalidData,
                "The carrier does not contain a complete JPEG 2000 signature box.",
                0, carrierBytes.Length < 12 ? carrierBytes.Length : 12);
        }
        if (limits.MaximumNestingDepth < 0)
        {
            return Fail(result, IccOperationStatus.ResourceLimitExceeded,
                "The configured JPEG 2000 nesting-depth limit is invalid.",
                0, 0);
        }

        Jpeg2000ParseState state = new Jpeg2000ParseState(carrierBytes,
            limits, result);
        Jpeg2000BoxRegion? fileType = ReadBox(state, 12,
            carrierBytes.Length);
        if (fileType == null)
        {
            return result;
        }
        if (fileType.Type != FileType || fileType.DataLength < 8
            || ((fileType.DataLength - 8) & 3) != 0)
        {
            return Fail(result, IccOperationStatus.InvalidData,
                "The JPEG 2000 signature must be followed by a valid File Type box.",
                fileType.Start, fileType.End - fileType.Start);
        }
        bool supportsJp2 = HasBrand(carrierBytes.Span, fileType, Jp2Brand);
        bool supportsJpx = HasBrand(carrierBytes.Span, fileType, JpxBrand)
            || HasBrand(carrierBytes.Span, fileType, JpxBaselineBrand);
        if (!supportsJp2 && !supportsJpx)
        {
            return Fail(result, IccOperationStatus.UnsupportedFeature,
                "The JPEG 2000 File Type box does not declare JP2 or JPX compatibility.",
                fileType.DataOffset, fileType.DataLength);
        }
        result.IsJpx = supportsJpx;

        if (!ParseBoxes(state, fileType.End, carrierBytes.Length, 0, 0,
            new Jpeg2000BoxRegion[0]))
        {
            return result;
        }
        result.Profiles = state.Profiles.ToArray();
        result.IsValid = true;
        result.Status = IccOperationStatus.Success;
        result.Message = result.Profiles.Length == 0
            ? "The JPEG 2000 box structure contains no method 2 or 3 ICC profile."
            : $"The JPEG 2000 carrier contains {result.Profiles.Length} method 2/3 ICC profile(s).";
        return result;
    }

    internal static bool ParseBoxes(Jpeg2000ParseState state, int start,
        int end, int depth, uint containerType,
        Jpeg2000BoxRegion[] containers)
    {
        int offset = start;
        while (offset < end)
        {
            Jpeg2000BoxRegion? box = ReadBox(state, offset, end);
            if (box == null)
            {
                return false;
            }
            bool colourContainer = containerType == Jp2Header
                || containerType == ColourGroup;
            if (box.Type == ColourSpecification && colourContainer)
            {
                if (!ParseColourSpecification(state, box, containers))
                {
                    return false;
                }
            }
            if (box.Type == Jp2Header || box.Type == LayerHeader
                || box.Type == ColourGroup)
            {
                if (depth >= state.Limits.MaximumNestingDepth)
                {
                    Fail(state.Result,
                        IccOperationStatus.ResourceLimitExceeded,
                        $"The JPEG 2000 box hierarchy exceeds the configured nesting depth of {state.Limits.MaximumNestingDepth}.",
                        box.Start, box.End - box.Start);
                    return false;
                }
                Jpeg2000BoxRegion[] nested = AppendContainer(containers,
                    box);
                if (!ParseBoxes(state, box.DataOffset, box.End, depth + 1,
                    box.Type, nested))
                {
                    return false;
                }
            }
            offset = box.End;
        }
        return true;
    }

    internal static bool ParseColourSpecification(
        Jpeg2000ParseState state, Jpeg2000BoxRegion box,
        Jpeg2000BoxRegion[] containers)
    {
        if (box.DataLength < 3)
        {
            Fail(state.Result, IccOperationStatus.InvalidData,
                "A JPEG 2000 Colour Specification box is shorter than its method header.",
                box.DataOffset, box.DataLength);
            return false;
        }
        int method = state.Carrier.Span[box.DataOffset];
        if (method != 2 && method != 3)
        {
            return true;
        }
        if (method == 3 && !state.Result.IsJpx)
        {
            Fail(state.Result, IccOperationStatus.InvalidData,
                "JPEG 2000 colour method 3 requires JPX compatibility.",
                box.DataOffset, 1);
            return false;
        }
        int profileOffset = box.DataOffset + 3;
        int profileLength = box.DataLength - 3;
        if (profileLength > state.Limits.MaximumProfileBytes)
        {
            Fail(state.Result, IccOperationStatus.ResourceLimitExceeded,
                "A JPEG 2000 Colour Specification ICC payload exceeds the configured profile-size limit.",
                profileOffset, profileLength);
            return false;
        }
        IccReadOptions options = new IccReadOptions();
        options.Mode = IccReadMode.Recovery;
        options.ResourceLimits = state.Limits;
        IccOperationResult probe = new ProfileReaderCore().ProbeMemory(
            state.Carrier.Slice(profileOffset, profileLength), options);
        if (probe.Status != IccOperationStatus.Success)
        {
            Fail(state.Result, probe.Status,
                $"The JPEG 2000 method {method} ICC payload is invalid: {probe.Message}",
                profileOffset, profileLength);
            return false;
        }

        Jpeg2000ProfileEntry entry = new Jpeg2000ProfileEntry();
        entry.Box = box;
        entry.Containers = containers;
        entry.Method = method;
        entry.ProfileOffset = profileOffset;
        entry.ProfileLength = profileLength;
        state.Profiles.Add(entry);
        return true;
    }

    internal static byte[] ReplaceProfile(
        System.ReadOnlySpan<byte> carrierBytes, int profileIndex,
        byte[] profileBytes, IccResourceLimits limits)
    {
        if (profileBytes == null || profileBytes.Length
            > limits.MaximumProfileBytes)
        {
            throw new IccEmbeddingException(
                "The ICC profile exceeds the configured JPEG 2000 embedding limit.");
        }
        Jpeg2000ProfileData data = Analyze(carrierBytes.ToArray(), limits);
        if (!data.IsValid)
        {
            throw new IccEmbeddingException(data.Message);
        }
        if (profileIndex < 0 || profileIndex >= data.Profiles.Length)
        {
            throw new IccEmbeddingException(
                $"The JPEG 2000 carrier does not expose an ICC profile at index {profileIndex}.");
        }
        Jpeg2000ProfileEntry entry = data.Profiles[profileIndex];
        byte[] replacement = EncodeColourBox(carrierBytes, entry,
            profileBytes);
        long outputLength = (long)carrierBytes.Length
            - (entry.Box.End - entry.Box.Start) + replacement.Length;
        if (outputLength > limits.MaximumCarrierBytes
            || outputLength > System.Array.MaxLength)
        {
            throw new IccEmbeddingException(
                "The updated JPEG 2000 carrier exceeds the configured or managed size limit.");
        }
        byte[] output = new byte[(int)outputLength];
        carrierBytes.Slice(0, entry.Box.Start).CopyTo(output);
        System.Array.Copy(replacement, 0, output, entry.Box.Start,
            replacement.Length);
        carrierBytes.Slice(entry.Box.End).CopyTo(new System.Span<byte>(output,
            entry.Box.Start + replacement.Length,
            carrierBytes.Length - entry.Box.End));

        int difference = replacement.Length
            - (entry.Box.End - entry.Box.Start);
        int containerIndex = 0;
        while (containerIndex < entry.Containers.Length)
        {
            Jpeg2000BoxRegion container = entry.Containers[containerIndex];
            if (!container.ExtendsToEnd)
            {
                long changedLength = (long)(container.End - container.Start)
                    + difference;
                if (container.HeaderLength == 8)
                {
                    if (changedLength > uint.MaxValue)
                    {
                        throw new IccEmbeddingException(
                            "A resized JPEG 2000 box exceeds its 32-bit length field.");
                    }
                    WriteUInt32(output, container.Start,
                        (uint)changedLength);
                }
                else
                {
                    WriteUInt64(output, container.Start + 8,
                        changedLength);
                }
            }
            containerIndex = containerIndex + 1;
        }
        return output;
    }

    internal static byte[] EncodeColourBox(
        System.ReadOnlySpan<byte> carrierBytes,
        Jpeg2000ProfileEntry entry, byte[] profileBytes)
    {
        long encodedLength = (long)entry.Box.HeaderLength + 3
            + profileBytes.Length;
        if (encodedLength > System.Array.MaxLength)
        {
            throw new IccEmbeddingException(
                "The encoded JPEG 2000 Colour Specification box exceeds the managed size limit.");
        }
        if (entry.Box.HeaderLength == 8 && !entry.Box.ExtendsToEnd
            && encodedLength > uint.MaxValue)
        {
            throw new IccEmbeddingException(
                "The encoded JPEG 2000 Colour Specification box exceeds its 32-bit length field.");
        }
        byte[] result = new byte[(int)encodedLength];
        if (entry.Box.ExtendsToEnd)
        {
            WriteUInt32(result, 0, 0);
        }
        else if (entry.Box.HeaderLength == 16)
        {
            WriteUInt32(result, 0, 1);
            WriteUInt64(result, 8, encodedLength);
        }
        else
        {
            WriteUInt32(result, 0, (uint)encodedLength);
        }
        WriteUInt32(result, 4, ColourSpecification);
        result[entry.Box.HeaderLength] = carrierBytes[entry.Box.DataOffset];
        result[entry.Box.HeaderLength + 1] =
            carrierBytes[entry.Box.DataOffset + 1];
        result[entry.Box.HeaderLength + 2] =
            carrierBytes[entry.Box.DataOffset + 2];
        System.Array.Copy(profileBytes, 0, result,
            entry.Box.HeaderLength + 3, profileBytes.Length);
        return result;
    }

    internal static Jpeg2000BoxRegion? ReadBox(Jpeg2000ParseState state,
        int offset, int end)
    {
        if (offset > end - 8)
        {
            Fail(state.Result, IccOperationStatus.InvalidData,
                "The JPEG 2000 carrier contains a truncated box header.",
                offset, end - offset);
            return null;
        }
        uint encodedLength = ReadUInt32(state.Carrier.Span, offset);
        int headerLength = 8;
        long boxLength = encodedLength;
        bool extendsToEnd = encodedLength == 0;
        if (encodedLength == 1)
        {
            if (offset > end - 16)
            {
                Fail(state.Result, IccOperationStatus.InvalidData,
                    "The JPEG 2000 carrier contains a truncated extended box header.",
                    offset, end - offset);
                return null;
            }
            headerLength = 16;
            boxLength = ReadUInt64(state.Carrier.Span, offset + 8);
        }
        else if (extendsToEnd)
        {
            boxLength = end - offset;
        }
        if (boxLength < headerLength)
        {
            Fail(state.Result, IccOperationStatus.InvalidData,
                "A JPEG 2000 box length is shorter than its header.",
                offset, headerLength);
            return null;
        }
        if (boxLength > end - offset)
        {
            IccOperationStatus status = boxLength > int.MaxValue
                ? IccOperationStatus.ResourceLimitExceeded
                : IccOperationStatus.InvalidData;
            Fail(state.Result, status,
                "A JPEG 2000 box extends beyond its containing box.",
                offset, end - offset);
            return null;
        }
        Jpeg2000BoxRegion box = new Jpeg2000BoxRegion();
        box.Start = offset;
        box.End = offset + (int)boxLength;
        box.HeaderLength = headerLength;
        box.DataOffset = offset + headerLength;
        box.DataLength = (int)boxLength - headerLength;
        box.Type = ReadUInt32(state.Carrier.Span, offset + 4);
        box.ExtendsToEnd = extendsToEnd;
        return box;
    }

    internal static bool HasBrand(System.ReadOnlySpan<byte> carrierBytes,
        Jpeg2000BoxRegion fileType, uint brand)
    {
        if (ReadUInt32(carrierBytes, fileType.DataOffset) == brand)
        {
            return true;
        }
        int offset = fileType.DataOffset + 8;
        while (offset < fileType.End)
        {
            if (ReadUInt32(carrierBytes, offset) == brand)
            {
                return true;
            }
            offset = offset + 4;
        }
        return false;
    }

    internal static Jpeg2000BoxRegion[] AppendContainer(
        Jpeg2000BoxRegion[] containers, Jpeg2000BoxRegion box)
    {
        Jpeg2000BoxRegion[] result =
            new Jpeg2000BoxRegion[containers.Length + 1];
        System.Array.Copy(containers, result, containers.Length);
        result[containers.Length] = box;
        return result;
    }

    internal static Jpeg2000ProfileData Fail(Jpeg2000ProfileData result,
        IccOperationStatus status, string message, int offset, int length)
    {
        result.IsValid = false;
        result.Profiles = new Jpeg2000ProfileEntry[0];
        result.Status = status;
        result.Message = message;
        result.ErrorOffset = offset;
        result.ErrorLength = length;
        return result;
    }

    internal static uint ReadUInt32(System.ReadOnlySpan<byte> bytes,
        int offset)
    {
        return ((uint)bytes[offset] << 24)
            | ((uint)bytes[offset + 1] << 16)
            | ((uint)bytes[offset + 2] << 8)
            | bytes[offset + 3];
    }

    internal static long ReadUInt64(System.ReadOnlySpan<byte> bytes,
        int offset)
    {
        uint high = ReadUInt32(bytes, offset);
        if (high != 0)
        {
            return (long)int.MaxValue + 1;
        }
        return ReadUInt32(bytes, offset + 4);
    }

    internal static void WriteUInt32(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)((value >> 24) & 0xff);
        bytes[offset + 1] = (byte)((value >> 16) & 0xff);
        bytes[offset + 2] = (byte)((value >> 8) & 0xff);
        bytes[offset + 3] = (byte)(value & 0xff);
    }

    internal static void WriteUInt64(byte[] bytes, int offset, long value)
    {
        WriteUInt32(bytes, offset, (uint)((value >> 32) & 0xffffffff));
        WriteUInt32(bytes, offset + 4, (uint)(value & 0xffffffff));
    }
}
