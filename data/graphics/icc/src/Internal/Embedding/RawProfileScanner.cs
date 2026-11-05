namespace icc;

internal class RawProfileRegion
{
    internal int Offset;
    internal int Size;
    internal int AlignedEnd;
}

internal static class RawProfileScanner
{
    internal static IccByteLocation[] FindProfileLocations(System.ReadOnlySpan<byte> carrierBytes, IccResourceLimits limits)
    {
        ValidateScanLimits(limits);
        System.Collections.Generic.List<IccByteLocation> locations = new System.Collections.Generic.List<IccByteLocation>();
        int signatureOffset = 0;
        while (signatureOffset <= carrierBytes.Length - 4)
        {
            if (ReadUInt32(carrierBytes, signatureOffset) == IccConstants.ProfileFileSignature)
            {
                int profileOffset = signatureOffset - 36;
                if (profileOffset >= 0 && IsValidCandidate(carrierBytes, profileOffset, limits))
                {
                    int declaredSize = (int)ReadUInt32(carrierBytes, profileOffset);
                    IccByteLocation location = new IccByteLocation();
                    location.Offset = profileOffset;
                    location.Length = declaredSize;
                    locations.Add(location);
                }
            }

            signatureOffset = signatureOffset + 1;
        }

        return locations.ToArray();
    }

    internal static void ValidateScanLimits(IccResourceLimits limits)
    {
        if (limits == null)
        {
            throw new IccProfileValidationException("Raw profile scan resource limits cannot be null.");
        }

        if (limits.MaximumProfileBytes < IccConstants.MinimumProfileSize)
        {
            throw new IccProfileValidationException($"The maximum profile size must be at least {IccConstants.MinimumProfileSize} bytes.");
        }

        if (limits.MaximumCarrierBytes < 0)
        {
            throw new IccProfileValidationException("The maximum buffered carrier size cannot be negative.");
        }

        TagTableCodec.ValidateTableLimits(limits);
    }

    internal static bool IsValidCandidate(System.ReadOnlySpan<byte> carrierBytes, int profileOffset, IccResourceLimits limits)
    {
        int availableBytes = carrierBytes.Length - profileOffset;
        if (availableBytes < IccConstants.MinimumProfileSize)
        {
            return false;
        }

        uint encodedSize = ReadUInt32(carrierBytes, profileOffset);
        if (encodedSize > int.MaxValue || encodedSize > limits.MaximumProfileBytes)
        {
            return false;
        }

        int declaredSize = (int)encodedSize;
        if (declaredSize < IccConstants.MinimumProfileSize || declaredSize > availableBytes)
        {
            return false;
        }

        if (ReadUInt32(carrierBytes, profileOffset + 36) != IccConstants.ProfileFileSignature)
        {
            return false;
        }

        uint encodedTagCount = ReadUInt32(carrierBytes, profileOffset + IccConstants.HeaderSize);
        if (encodedTagCount > int.MaxValue || encodedTagCount > limits.MaximumTagCount)
        {
            return false;
        }

        int tagCount = (int)encodedTagCount;
        long tableEndValue = IccConstants.MinimumProfileSize + ((long)tagCount * IccConstants.TagTableEntrySize);
        if (tableEndValue > declaredSize)
        {
            return false;
        }

        int tableEnd = (int)tableEndValue;
        RawProfileRegion[] regions = new RawProfileRegion[tagCount];
        uint[] signatures = new uint[tagCount];
        int regionCount = 0;
        int totalPayloadBytes = 0;
        int entryIndex = 0;
        while (entryIndex < tagCount)
        {
            int entryOffset = profileOffset + IccConstants.MinimumProfileSize + (entryIndex * IccConstants.TagTableEntrySize);
            uint signature = ReadUInt32(carrierBytes, entryOffset);
            if (ContainsSignature(signatures, entryIndex, signature))
            {
                return false;
            }

            signatures[entryIndex] = signature;
            uint encodedOffset = ReadUInt32(carrierBytes, entryOffset + 4);
            uint encodedPayloadSize = ReadUInt32(carrierBytes, entryOffset + 8);
            if (encodedOffset > int.MaxValue || encodedOffset > declaredSize)
            {
                return false;
            }

            if (encodedPayloadSize > int.MaxValue || encodedPayloadSize > limits.MaximumTagBytes)
            {
                return false;
            }

            int relativeOffset = (int)encodedOffset;
            int payloadSize = (int)encodedPayloadSize;
            if ((relativeOffset & (IccConstants.TagPayloadAlignment - 1)) != 0 || relativeOffset < tableEnd || payloadSize < 8)
            {
                return false;
            }

            if (payloadSize > declaredSize - relativeOffset)
            {
                return false;
            }

            int payloadEnd = relativeOffset + payloadSize;
            long alignedEndValue = ((long)payloadEnd + IccConstants.TagPayloadAlignment - 1) & -IccConstants.TagPayloadAlignment;
            if (alignedEndValue > declaredSize)
            {
                return false;
            }

            int alignedEnd = (int)alignedEndValue;
            if (ContainsNonZeroPadding(carrierBytes, profileOffset + payloadEnd, alignedEnd - payloadEnd))
            {
                return false;
            }

            int sharedRegionIndex = FindSharedRegion(regions, regionCount, relativeOffset, payloadSize);
            if (sharedRegionIndex < 0)
            {
                if (payloadSize > int.MaxValue - totalPayloadBytes)
                {
                    return false;
                }

                totalPayloadBytes = totalPayloadBytes + payloadSize;
                if (totalPayloadBytes > limits.MaximumTotalTagBytes)
                {
                    return false;
                }

                RawProfileRegion region = new RawProfileRegion();
                region.Offset = relativeOffset;
                region.Size = payloadSize;
                region.AlignedEnd = alignedEnd;
                regions[regionCount] = region;
                regionCount = regionCount + 1;
            }

            entryIndex = entryIndex + 1;
        }

        SortRegions(regions, regionCount);
        int expectedOffset = tableEnd;
        int regionIndex = 0;
        while (regionIndex < regionCount)
        {
            RawProfileRegion region = regions[regionIndex];
            if (region.Offset != expectedOffset)
            {
                return false;
            }

            expectedOffset = region.AlignedEnd;
            regionIndex = regionIndex + 1;
        }

        return expectedOffset == declaredSize;
    }

    internal static bool ContainsSignature(uint[] signatures, int count, uint signature)
    {
        int index = 0;
        while (index < count)
        {
            if (signatures[index] == signature)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static int FindSharedRegion(RawProfileRegion[] regions, int count, int offset, int size)
    {
        int index = 0;
        while (index < count)
        {
            RawProfileRegion region = regions[index];
            if (region.Offset == offset && region.Size == size)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static bool ContainsNonZeroPadding(System.ReadOnlySpan<byte> carrierBytes, int offset, int count)
    {
        int index = 0;
        while (index < count)
        {
            if (carrierBytes[offset + index] != 0)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static void SortRegions(RawProfileRegion[] regions, int count)
    {
        int index = 1;
        while (index < count)
        {
            RawProfileRegion current = regions[index];
            int destination = index;
            while (destination > 0 && regions[destination - 1].Offset > current.Offset)
            {
                regions[destination] = regions[destination - 1];
                destination = destination - 1;
            }

            regions[destination] = current;
            index = index + 1;
        }
    }

    internal static uint ReadUInt32(System.ReadOnlySpan<byte> bytes, int offset)
    {
        return ((uint)bytes[offset] << 24) | ((uint)bytes[offset + 1] << 16) | ((uint)bytes[offset + 2] << 8) | bytes[offset + 3];
    }
}
