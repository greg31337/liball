namespace icc;

internal static class JpegEmbeddingCore
{
    internal const int MaximumChunkBytes = 65519;

    internal static JpegProfileData Analyze(System.ReadOnlySpan<byte> carrierBytes,
        IccResourceLimits limits)
    {
        JpegProfileData result = new JpegProfileData();
        if (carrierBytes.Length < 2 || carrierBytes[0] != 0xff
            || carrierBytes[1] != 0xd8)
        {
            return Fail(result, IccOperationStatus.InvalidData,
                "The carrier does not begin with a JPEG SOI marker.", 0,
                carrierBytes.Length < 2 ? carrierBytes.Length : 2);
        }

        System.Collections.Generic.List<JpegIccSegment> segments =
            new System.Collections.Generic.List<JpegIccSegment>();
        int position = 2;
        bool foundEnd = false;
        bool insertionOpen = true;
        while (position < carrierBytes.Length && !foundEnd)
        {
            int markerStart = position;
            if (carrierBytes[position] != 0xff)
            {
                return Fail(result, IccOperationStatus.InvalidData,
                    "JPEG marker data was expected outside entropy-coded scan data.",
                    position, 1);
            }
            while (position < carrierBytes.Length
                && carrierBytes[position] == 0xff)
            {
                position = position + 1;
            }
            if (position >= carrierBytes.Length)
            {
                return Fail(result, IccOperationStatus.InvalidData,
                    "The JPEG carrier ends within a marker prefix.", markerStart,
                    carrierBytes.Length - markerStart);
            }

            int marker = carrierBytes[position];
            position = position + 1;
            if (marker == 0 || marker == 0xd8)
            {
                return Fail(result, IccOperationStatus.InvalidData,
                    "The JPEG carrier contains an invalid marker outside scan data.",
                    markerStart, position - markerStart);
            }
            if (marker == 0xd9)
            {
                foundEnd = true;
            }
            else if (IsStandaloneMarker(marker))
            {
                insertionOpen = false;
            }
            else
            {
                if (position > carrierBytes.Length - 2)
                {
                    return Fail(result, IccOperationStatus.InvalidData,
                        "The JPEG marker length is truncated.", position,
                        carrierBytes.Length - position);
                }
                int segmentLength = ReadUInt16(carrierBytes, position);
                if (segmentLength < 2
                    || segmentLength > carrierBytes.Length - position)
                {
                    return Fail(result, IccOperationStatus.InvalidData,
                        "The JPEG marker declares a length outside the carrier.",
                        position, carrierBytes.Length - position);
                }
                int segmentEnd = position + segmentLength;
                int dataOffset = position + 2;
                int dataLength = segmentLength - 2;
                if (marker == 0xe2 && HasIccIdentifier(carrierBytes,
                    dataOffset, dataLength))
                {
                    if (dataLength < 14)
                    {
                        return Fail(result, IccOperationStatus.InvalidData,
                            "A JPEG ICC APP2 segment is missing its sequence fields.",
                            dataOffset, dataLength);
                    }
                    JpegIccSegment segment = new JpegIccSegment();
                    segment.Start = markerStart;
                    segment.End = segmentEnd;
                    segment.PayloadOffset = dataOffset + 14;
                    segment.PayloadLength = dataLength - 14;
                    segment.SequenceNumber = carrierBytes[dataOffset + 12];
                    segment.SegmentCount = carrierBytes[dataOffset + 13];
                    segments.Add(segment);
                }

                if (insertionOpen && (marker == 0xe0 || marker == 0xe1))
                {
                    result.InsertionOffset = segmentEnd;
                }
                else
                {
                    insertionOpen = false;
                }

                position = segmentEnd;
                if (marker == 0xda)
                {
                    int nextMarker = FindMarkerAfterScan(carrierBytes, position);
                    if (nextMarker < 0)
                    {
                        return Fail(result, IccOperationStatus.InvalidData,
                            "The JPEG entropy-coded scan has no terminating marker.",
                            position, carrierBytes.Length - position);
                    }
                    position = nextMarker;
                }
            }
        }

        if (!foundEnd)
        {
            return Fail(result, IccOperationStatus.InvalidData,
                "The JPEG carrier has no EOI marker.", carrierBytes.Length, 0);
        }
        result.IsValid = true;
        result.Status = IccOperationStatus.Success;
        result.Message = "The JPEG marker structure is valid.";
        result.Segments = segments.ToArray();
        return ValidateIccSegments(carrierBytes, limits, result);
    }

    internal static JpegProfileData ValidateIccSegments(
        System.ReadOnlySpan<byte> carrierBytes, IccResourceLimits limits,
        JpegProfileData result)
    {
        if (result.Segments.Length == 0)
        {
            return result;
        }

        bool[] seen = new bool[256];
        int expectedCount = result.Segments[0].SegmentCount;
        long totalBytes = 0;
        int index = 0;
        while (index < result.Segments.Length)
        {
            JpegIccSegment segment = result.Segments[index];
            if (segment.SegmentCount == 0 || segment.SequenceNumber == 0
                || segment.SequenceNumber > segment.SegmentCount)
            {
                return Fail(result, IccOperationStatus.InvalidData,
                    "A JPEG ICC APP2 segment has an invalid sequence number or segment count.",
                    segment.Start, segment.End - segment.Start);
            }
            if (segment.SegmentCount != expectedCount)
            {
                return Fail(result, IccOperationStatus.InvalidData,
                    "JPEG ICC APP2 segments declare inconsistent segment counts.",
                    segment.Start, segment.End - segment.Start);
            }
            if (seen[segment.SequenceNumber])
            {
                return Fail(result, IccOperationStatus.InvalidData,
                    "The JPEG ICC APP2 sequence contains a duplicate segment number.",
                    segment.Start, segment.End - segment.Start);
            }
            seen[segment.SequenceNumber] = true;
            totalBytes = totalBytes + segment.PayloadLength;
            if (totalBytes > limits.MaximumProfileBytes
                || totalBytes > System.Array.MaxLength)
            {
                return Fail(result, IccOperationStatus.ResourceLimitExceeded,
                    "The segmented JPEG ICC profile exceeds the configured or managed profile-size limit.",
                    result.Segments[0].Start,
                    segment.End - result.Segments[0].Start);
            }
            index = index + 1;
        }
        if (result.Segments.Length != expectedCount)
        {
            JpegIccSegment first = result.Segments[0];
            JpegIccSegment last = result.Segments[result.Segments.Length - 1];
            return Fail(result, IccOperationStatus.InvalidData,
                "The JPEG ICC APP2 sequence is missing one or more declared segments.",
                first.Start, last.End - first.Start);
        }

        byte[] profileBytes = new byte[(int)totalBytes];
        int destinationOffset = 0;
        int sequence = 1;
        while (sequence <= expectedCount)
        {
            JpegIccSegment segment = FindSequenceSegment(result.Segments,
                sequence);
            carrierBytes.Slice(segment.PayloadOffset, segment.PayloadLength)
                .CopyTo(new System.Span<byte>(profileBytes, destinationOffset,
                    segment.PayloadLength));
            destinationOffset = destinationOffset + segment.PayloadLength;
            sequence = sequence + 1;
        }
        result.HasProfile = true;
        result.ProfileBytes = profileBytes;
        IccReadOptions probeOptions = new IccReadOptions();
        probeOptions.Mode = IccReadMode.Recovery;
        probeOptions.ResourceLimits = limits;
        IccOperationResult probe = new ProfileReaderCore().ProbeMemory(
            profileBytes, probeOptions);
        if (probe.Status != IccOperationStatus.Success)
        {
            JpegIccSegment first = result.Segments[0];
            JpegIccSegment last = result.Segments[result.Segments.Length - 1];
            return Fail(result, probe.Status,
                $"The reassembled JPEG ICC profile is invalid: {probe.Message}",
                first.Start, last.End - first.Start);
        }
        result.Message = $"The JPEG carrier contains a complete {result.Segments.Length}-segment ICC profile.";
        return result;
    }

    internal static JpegIccSegment FindSequenceSegment(
        JpegIccSegment[] segments, int sequence)
    {
        int index = 0;
        while (index < segments.Length)
        {
            if (segments[index].SequenceNumber == sequence)
            {
                return segments[index];
            }
            index = index + 1;
        }
        throw new IccEmbeddingException(
            "The JPEG ICC APP2 sequence is incomplete.");
    }

    internal static byte[] InsertProfile(System.ReadOnlySpan<byte> carrierBytes,
        byte[] profileBytes, IccResourceLimits limits)
    {
        JpegProfileData data = Analyze(carrierBytes, limits);
        RequireValidCarrier(data);
        if (data.HasProfile)
        {
            throw new IccEmbeddingException(
                "The JPEG carrier already contains an ICC profile; use replacement instead.");
        }
        byte[] segments = EncodeSegments(profileBytes);
        return RewriteCarrier(carrierBytes, data, segments,
            data.InsertionOffset, false, limits.MaximumCarrierBytes);
    }

    internal static byte[] ReplaceProfile(System.ReadOnlySpan<byte> carrierBytes,
        int profileIndex, byte[] profileBytes, IccResourceLimits limits)
    {
        RequireProfileIndex(profileIndex);
        JpegProfileData data = Analyze(carrierBytes, limits);
        RequireValidCarrier(data);
        RequireExistingProfile(data);
        byte[] segments = EncodeSegments(profileBytes);
        return RewriteCarrier(carrierBytes, data, segments,
            data.Segments[0].Start, true, limits.MaximumCarrierBytes);
    }

    internal static byte[] RemoveProfile(System.ReadOnlySpan<byte> carrierBytes,
        int profileIndex, IccResourceLimits limits)
    {
        RequireProfileIndex(profileIndex);
        JpegProfileData data = Analyze(carrierBytes, limits);
        RequireValidCarrier(data);
        RequireExistingProfile(data);
        return RewriteCarrier(carrierBytes, data, new byte[0],
            data.Segments[0].Start, true, limits.MaximumCarrierBytes);
    }

    internal static byte[] EncodeSegments(byte[] profileBytes)
    {
        if (profileBytes == null || profileBytes.Length == 0)
        {
            throw new IccEmbeddingException(
                "A non-empty encoded ICC profile is required for JPEG embedding.");
        }
        int segmentCount = (profileBytes.Length + MaximumChunkBytes - 1)
            / MaximumChunkBytes;
        if (segmentCount > 255)
        {
            throw new IccEmbeddingException(
                "The ICC profile requires more than 255 JPEG APP2 segments.");
        }
        int outputLength = profileBytes.Length + (segmentCount * 18);
        byte[] output = new byte[outputLength];
        int sourceOffset = 0;
        int destinationOffset = 0;
        int sequence = 1;
        while (sequence <= segmentCount)
        {
            int chunkLength = profileBytes.Length - sourceOffset;
            if (chunkLength > MaximumChunkBytes)
            {
                chunkLength = MaximumChunkBytes;
            }
            output[destinationOffset] = 0xff;
            output[destinationOffset + 1] = 0xe2;
            WriteUInt16(output, destinationOffset + 2, chunkLength + 16);
            WriteIdentifier(output, destinationOffset + 4);
            output[destinationOffset + 16] = (byte)sequence;
            output[destinationOffset + 17] = (byte)segmentCount;
            System.Array.Copy(profileBytes, sourceOffset, output,
                destinationOffset + 18, chunkLength);
            sourceOffset = sourceOffset + chunkLength;
            destinationOffset = destinationOffset + chunkLength + 18;
            sequence = sequence + 1;
        }
        return output;
    }

    internal static byte[] RewriteCarrier(System.ReadOnlySpan<byte> carrierBytes,
        JpegProfileData data, byte[] insertionBytes, int insertionOffset,
        bool removeExisting, int maximumCarrierBytes)
    {
        int removedBytes = 0;
        int index = 0;
        if (removeExisting)
        {
            while (index < data.Segments.Length)
            {
                removedBytes = removedBytes + data.Segments[index].End
                    - data.Segments[index].Start;
                index = index + 1;
            }
        }
        long outputLengthValue = (long)carrierBytes.Length - removedBytes
            + insertionBytes.Length;
        if (outputLengthValue > maximumCarrierBytes
            || outputLengthValue > System.Array.MaxLength)
        {
            throw new IccEmbeddingException(
                "The updated JPEG carrier exceeds the configured or managed carrier-size limit.");
        }

        byte[] output = new byte[(int)outputLengthValue];
        int sourceOffset = 0;
        int destinationOffset = 0;
        bool inserted = false;
        int segmentIndex = 0;
        while (sourceOffset < carrierBytes.Length)
        {
            if (!inserted && sourceOffset == insertionOffset)
            {
                System.Array.Copy(insertionBytes, 0, output,
                    destinationOffset, insertionBytes.Length);
                destinationOffset = destinationOffset + insertionBytes.Length;
                inserted = true;
            }
            if (removeExisting && segmentIndex < data.Segments.Length
                && sourceOffset == data.Segments[segmentIndex].Start)
            {
                sourceOffset = data.Segments[segmentIndex].End;
                segmentIndex = segmentIndex + 1;
            }
            else
            {
                output[destinationOffset] = carrierBytes[sourceOffset];
                destinationOffset = destinationOffset + 1;
                sourceOffset = sourceOffset + 1;
            }
        }
        if (!inserted)
        {
            System.Array.Copy(insertionBytes, 0, output, destinationOffset,
                insertionBytes.Length);
        }
        return output;
    }

    internal static int FindMarkerAfterScan(System.ReadOnlySpan<byte> bytes,
        int offset)
    {
        int position = offset;
        while (position < bytes.Length - 1)
        {
            if (bytes[position] != 0xff)
            {
                position = position + 1;
            }
            else
            {
                int markerStart = position;
                while (position < bytes.Length && bytes[position] == 0xff)
                {
                    position = position + 1;
                }
                if (position >= bytes.Length)
                {
                    return -1;
                }
                int marker = bytes[position];
                if (marker == 0 || (marker >= 0xd0 && marker <= 0xd7))
                {
                    position = position + 1;
                }
                else
                {
                    return markerStart;
                }
            }
        }
        return -1;
    }

    internal static bool HasIccIdentifier(System.ReadOnlySpan<byte> bytes,
        int offset, int length)
    {
        return length >= 12 && bytes[offset] == 0x49
            && bytes[offset + 1] == 0x43 && bytes[offset + 2] == 0x43
            && bytes[offset + 3] == 0x5f && bytes[offset + 4] == 0x50
            && bytes[offset + 5] == 0x52 && bytes[offset + 6] == 0x4f
            && bytes[offset + 7] == 0x46 && bytes[offset + 8] == 0x49
            && bytes[offset + 9] == 0x4c && bytes[offset + 10] == 0x45
            && bytes[offset + 11] == 0;
    }

    internal static bool IsStandaloneMarker(int marker)
    {
        return marker == 0x01 || (marker >= 0xd0 && marker <= 0xd7);
    }

    internal static void RequireValidCarrier(JpegProfileData data)
    {
        if (!data.IsValid)
        {
            throw new IccEmbeddingException(data.Message);
        }
    }

    internal static void RequireExistingProfile(JpegProfileData data)
    {
        if (!data.HasProfile)
        {
            throw new IccEmbeddingException(
                "The JPEG carrier does not contain an ICC profile.");
        }
    }

    internal static void RequireProfileIndex(int profileIndex)
    {
        if (profileIndex != 0)
        {
            throw new IccEmbeddingException(
                "A JPEG carrier exposes one logical ICC profile at index zero.");
        }
    }

    internal static JpegProfileData Fail(JpegProfileData result,
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

    internal static int ReadUInt16(System.ReadOnlySpan<byte> bytes, int offset)
    {
        return (bytes[offset] << 8) | bytes[offset + 1];
    }

    internal static void WriteUInt16(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)((value >> 8) & 0xff);
        bytes[offset + 1] = (byte)(value & 0xff);
    }

    internal static void WriteIdentifier(byte[] bytes, int offset)
    {
        bytes[offset] = 0x49;
        bytes[offset + 1] = 0x43;
        bytes[offset + 2] = 0x43;
        bytes[offset + 3] = 0x5f;
        bytes[offset + 4] = 0x50;
        bytes[offset + 5] = 0x52;
        bytes[offset + 6] = 0x4f;
        bytes[offset + 7] = 0x46;
        bytes[offset + 8] = 0x49;
        bytes[offset + 9] = 0x4c;
        bytes[offset + 10] = 0x45;
        bytes[offset + 11] = 0;
    }
}
