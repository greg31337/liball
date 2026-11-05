namespace raw;

internal static class BitPacking
{
    internal static int GetPackedByteCount(int sampleCount, int bitDepth)
    {
        ValidateSampleCount(sampleCount);
        ValidateBitDepth(bitDepth);
        long bitCount = CheckedMath.Multiply(sampleCount, bitDepth);
        long roundedBitCount = CheckedMath.Add(bitCount, 7);
        return CheckedMath.ToInt32(roundedBitCount / 8);
    }

    internal static int PackUnsigned(System.ReadOnlySpan<uint> samples, int bitDepth, RawBitOrder bitOrder, System.Span<byte> destination)
    {
        ValidateBitDepth(bitDepth);
        BitUtility.ValidateBitOrder(bitOrder);
        int byteCount = GetPackedByteCount(samples.Length, bitDepth);
        RequireDestination(destination, byteCount);
        uint maximum = (uint)BitUtility.Mask(bitDepth);
        int sampleIndex = 0;
        while (sampleIndex < samples.Length)
        {
            if (samples[sampleIndex] > maximum)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "An unsigned sample does not fit in the requested bit depth.", -1);
            }
            sampleIndex++;
        }

        destination.Slice(0, byteCount).Clear();
        long bitPosition = 0;
        sampleIndex = 0;
        while (sampleIndex < samples.Length)
        {
            WriteSampleBits(destination, bitPosition, samples[sampleIndex], bitDepth, bitOrder);
            bitPosition = CheckedMath.Add(bitPosition, bitDepth);
            sampleIndex++;
        }
        return byteCount;
    }

    internal static int UnpackUnsigned(System.ReadOnlySpan<byte> source, int sampleCount, int bitDepth, RawBitOrder bitOrder, System.Span<uint> destination)
    {
        ValidateSampleCount(sampleCount);
        ValidateBitDepth(bitDepth);
        BitUtility.ValidateBitOrder(bitOrder);
        int byteCount = GetPackedByteCount(sampleCount, bitDepth);
        RequireSource(source, byteCount);
        RequireSampleDestination(destination.Length, sampleCount);
        ValidateGenericPadding(source, sampleCount, bitDepth, bitOrder);

        long bitPosition = 0;
        int sampleIndex = 0;
        while (sampleIndex < sampleCount)
        {
            destination[sampleIndex] = ReadSampleBits(source, bitPosition, bitDepth, bitOrder);
            bitPosition = CheckedMath.Add(bitPosition, bitDepth);
            sampleIndex++;
        }
        return byteCount;
    }

    internal static int PackSigned(System.ReadOnlySpan<int> samples, int bitDepth, RawBitOrder bitOrder, System.Span<byte> destination)
    {
        ValidateBitDepth(bitDepth);
        BitUtility.ValidateBitOrder(bitOrder);
        int byteCount = GetPackedByteCount(samples.Length, bitDepth);
        RequireDestination(destination, byteCount);
        long minimum;
        long maximum;
        if (bitDepth == 32)
        {
            minimum = int.MinValue;
            maximum = int.MaxValue;
        }
        else
        {
            long magnitude = 1L << (bitDepth - 1);
            minimum = -magnitude;
            maximum = magnitude - 1;
        }

        int sampleIndex = 0;
        while (sampleIndex < samples.Length)
        {
            int sample = samples[sampleIndex];
            if (sample < minimum || sample > maximum)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A signed sample does not fit in the requested bit depth.", -1);
            }
            sampleIndex++;
        }

        destination.Slice(0, byteCount).Clear();
        long bitPosition = 0;
        sampleIndex = 0;
        while (sampleIndex < samples.Length)
        {
            uint encoded = BitUtility.ToUInt32Bits(samples[sampleIndex]) & (uint)BitUtility.Mask(bitDepth);
            WriteSampleBits(destination, bitPosition, encoded, bitDepth, bitOrder);
            bitPosition = CheckedMath.Add(bitPosition, bitDepth);
            sampleIndex++;
        }
        return byteCount;
    }

    internal static int UnpackSigned(System.ReadOnlySpan<byte> source, int sampleCount, int bitDepth, RawBitOrder bitOrder, System.Span<int> destination)
    {
        ValidateSampleCount(sampleCount);
        ValidateBitDepth(bitDepth);
        BitUtility.ValidateBitOrder(bitOrder);
        int byteCount = GetPackedByteCount(sampleCount, bitDepth);
        RequireSource(source, byteCount);
        RequireSampleDestination(destination.Length, sampleCount);
        ValidateGenericPadding(source, sampleCount, bitDepth, bitOrder);

        long bitPosition = 0;
        int sampleIndex = 0;
        while (sampleIndex < sampleCount)
        {
            uint encoded = ReadSampleBits(source, bitPosition, bitDepth, bitOrder);
            if (bitDepth == 32)
            {
                destination[sampleIndex] = BitUtility.ToInt32Bits(encoded);
            }
            else
            {
                uint signBit = 1U << (bitDepth - 1);
                if ((encoded & signBit) == 0)
                {
                    destination[sampleIndex] = (int)encoded;
                }
                else
                {
                    destination[sampleIndex] = (int)((long)encoded - (1L << bitDepth));
                }
            }
            bitPosition = CheckedMath.Add(bitPosition, bitDepth);
            sampleIndex++;
        }
        return byteCount;
    }

    internal static int GetNamedPackedByteCount(int sampleCount, RawPacking packing)
    {
        ValidateSampleCount(sampleCount);
        int bitDepth = GetNamedBitDepth(packing);
        int groupSize = GetNamedGroupSize(packing);
        int lowBitCount = bitDepth - 8;
        int fullGroups = sampleCount / groupSize;
        int remainder = sampleCount % groupSize;
        long byteCount = CheckedMath.Multiply(fullGroups, groupSize + lowBitCount * groupSize / 8);
        if (remainder != 0)
        {
            long lowBits = CheckedMath.Multiply(remainder, lowBitCount);
            long lowBytes = CheckedMath.Add(lowBits, 7) / 8;
            byteCount = CheckedMath.Add(byteCount, remainder);
            byteCount = CheckedMath.Add(byteCount, lowBytes);
        }
        return CheckedMath.ToInt32(byteCount);
    }

    internal static int PackRaw10(System.ReadOnlySpan<uint> samples, System.Span<byte> destination)
    {
        return PackNamed(samples, RawPacking.Raw10, destination);
    }

    internal static int UnpackRaw10(System.ReadOnlySpan<byte> source, int sampleCount, System.Span<uint> destination)
    {
        return UnpackNamed(source, sampleCount, RawPacking.Raw10, destination);
    }

    internal static int PackRaw12(System.ReadOnlySpan<uint> samples, System.Span<byte> destination)
    {
        return PackNamed(samples, RawPacking.Raw12, destination);
    }

    internal static int UnpackRaw12(System.ReadOnlySpan<byte> source, int sampleCount, System.Span<uint> destination)
    {
        return UnpackNamed(source, sampleCount, RawPacking.Raw12, destination);
    }

    internal static int PackRaw14(System.ReadOnlySpan<uint> samples, System.Span<byte> destination)
    {
        return PackNamed(samples, RawPacking.Raw14, destination);
    }

    internal static int UnpackRaw14(System.ReadOnlySpan<byte> source, int sampleCount, System.Span<uint> destination)
    {
        return UnpackNamed(source, sampleCount, RawPacking.Raw14, destination);
    }

    internal static int PackNamed(System.ReadOnlySpan<uint> samples, RawPacking packing, System.Span<byte> destination)
    {
        int bitDepth = GetNamedBitDepth(packing);
        int groupSize = GetNamedGroupSize(packing);
        int lowBitCount = bitDepth - 8;
        uint lowMask = (uint)BitUtility.Mask(lowBitCount);
        uint maximum = (uint)BitUtility.Mask(bitDepth);
        int byteCount = GetNamedPackedByteCount(samples.Length, packing);
        RequireDestination(destination, byteCount);

        int sampleIndex = 0;
        while (sampleIndex < samples.Length)
        {
            if (samples[sampleIndex] > maximum)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A sample does not fit in the named raw packing depth.", -1);
            }
            sampleIndex++;
        }

        destination.Slice(0, byteCount).Clear();
        sampleIndex = 0;
        int outputOffset = 0;
        while (sampleIndex < samples.Length)
        {
            int groupCount = groupSize;
            if (samples.Length - sampleIndex < groupCount)
            {
                groupCount = samples.Length - sampleIndex;
            }
            int lowByteOffset = outputOffset + groupCount;
            int groupIndex = 0;
            while (groupIndex < groupCount)
            {
                uint sample = samples[sampleIndex + groupIndex];
                destination[outputOffset + groupIndex] = (byte)(sample >> lowBitCount);
                uint lowValue = sample & lowMask;
                int lowIndex = 0;
                while (lowIndex < lowBitCount)
                {
                    if ((lowValue & (1U << lowIndex)) != 0)
                    {
                        int packedBit = groupIndex * lowBitCount + lowIndex;
                        destination[lowByteOffset + packedBit / 8] |= (byte)(1 << (packedBit % 8));
                    }
                    lowIndex++;
                }
                groupIndex++;
            }
            int lowByteCount = (groupCount * lowBitCount + 7) / 8;
            outputOffset += groupCount + lowByteCount;
            sampleIndex += groupCount;
        }
        return byteCount;
    }

    internal static int UnpackNamed(System.ReadOnlySpan<byte> source, int sampleCount, RawPacking packing, System.Span<uint> destination)
    {
        ValidateSampleCount(sampleCount);
        int bitDepth = GetNamedBitDepth(packing);
        int groupSize = GetNamedGroupSize(packing);
        int lowBitCount = bitDepth - 8;
        int byteCount = GetNamedPackedByteCount(sampleCount, packing);
        RequireSource(source, byteCount);
        RequireSampleDestination(destination.Length, sampleCount);
        ValidateNamedPadding(source, sampleCount, groupSize, lowBitCount);

        int sampleIndex = 0;
        int inputOffset = 0;
        while (sampleIndex < sampleCount)
        {
            int groupCount = groupSize;
            if (sampleCount - sampleIndex < groupCount)
            {
                groupCount = sampleCount - sampleIndex;
            }
            int lowByteOffset = inputOffset + groupCount;
            int groupIndex = 0;
            while (groupIndex < groupCount)
            {
                uint lowValue = 0;
                int lowIndex = 0;
                while (lowIndex < lowBitCount)
                {
                    int packedBit = groupIndex * lowBitCount + lowIndex;
                    uint bit = (uint)((source[lowByteOffset + packedBit / 8] >> (packedBit % 8)) & 1);
                    lowValue |= bit << lowIndex;
                    lowIndex++;
                }
                destination[sampleIndex + groupIndex] = ((uint)source[inputOffset + groupIndex] << lowBitCount) | lowValue;
                groupIndex++;
            }
            int lowByteCount = (groupCount * lowBitCount + 7) / 8;
            inputOffset += groupCount + lowByteCount;
            sampleIndex += groupCount;
        }
        return byteCount;
    }

    internal static void ValidateNamedPadding(System.ReadOnlySpan<byte> source, int sampleCount, int groupSize, int lowBitCount)
    {
        int sampleIndex = 0;
        int inputOffset = 0;
        while (sampleIndex < sampleCount)
        {
            int groupCount = groupSize;
            if (sampleCount - sampleIndex < groupCount)
            {
                groupCount = sampleCount - sampleIndex;
            }
            int usedLowBits = groupCount * lowBitCount;
            int lowByteCount = (usedLowBits + 7) / 8;
            int remainder = usedLowBits % 8;
            if (remainder != 0)
            {
                int lowByteOffset = inputOffset + groupCount;
                int paddingMask = 255 << remainder;
                if ((source[lowByteOffset + lowByteCount - 1] & paddingMask) != 0)
                {
                    throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "Named raw packing contains nonzero group padding.", lowByteOffset + lowByteCount - 1);
                }
            }
            inputOffset += groupCount + lowByteCount;
            sampleIndex += groupCount;
        }
    }

    internal static void WriteSampleBits(System.Span<byte> destination, long bitPosition, uint value, int bitDepth, RawBitOrder bitOrder)
    {
        int bitIndex = 0;
        while (bitIndex < bitDepth)
        {
            int sourceBit;
            int destinationBit;
            if (bitOrder == RawBitOrder.MostSignificantBitFirst)
            {
                sourceBit = bitDepth - 1 - bitIndex;
                destinationBit = 7 - (int)((bitPosition + bitIndex) % 8);
            }
            else
            {
                sourceBit = bitIndex;
                destinationBit = (int)((bitPosition + bitIndex) % 8);
            }
            if ((value & (1U << sourceBit)) != 0)
            {
                int byteIndex = CheckedMath.ToInt32((bitPosition + bitIndex) / 8);
                destination[byteIndex] |= (byte)(1 << destinationBit);
            }
            bitIndex++;
        }
    }

    internal static uint ReadSampleBits(System.ReadOnlySpan<byte> source, long bitPosition, int bitDepth, RawBitOrder bitOrder)
    {
        uint value = 0;
        int bitIndex = 0;
        while (bitIndex < bitDepth)
        {
            int destinationBit;
            int sourceBit;
            if (bitOrder == RawBitOrder.MostSignificantBitFirst)
            {
                destinationBit = bitDepth - 1 - bitIndex;
                sourceBit = 7 - (int)((bitPosition + bitIndex) % 8);
            }
            else
            {
                destinationBit = bitIndex;
                sourceBit = (int)((bitPosition + bitIndex) % 8);
            }
            int byteIndex = CheckedMath.ToInt32((bitPosition + bitIndex) / 8);
            uint bit = (uint)((source[byteIndex] >> sourceBit) & 1);
            value |= bit << destinationBit;
            bitIndex++;
        }
        return value;
    }

    internal static void ValidateGenericPadding(System.ReadOnlySpan<byte> source, int sampleCount, int bitDepth, RawBitOrder bitOrder)
    {
        long bitCount = CheckedMath.Multiply(sampleCount, bitDepth);
        int remainder = (int)(bitCount % 8);
        if (remainder == 0)
        {
            return;
        }
        int byteCount = GetPackedByteCount(sampleCount, bitDepth);
        int mask;
        if (bitOrder == RawBitOrder.MostSignificantBitFirst)
        {
            mask = (1 << (8 - remainder)) - 1;
        }
        else
        {
            mask = 255 << remainder;
        }
        if ((source[byteCount - 1] & mask) != 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "Packed samples contain nonzero trailing padding.", byteCount - 1);
        }
    }

    internal static int GetNamedBitDepth(RawPacking packing)
    {
        switch (packing)
        {
            case RawPacking.Raw10:
                return 10;
            case RawPacking.Raw12:
                return 12;
            case RawPacking.Raw14:
                return 14;
            default:
                throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "The packing is not a named RAW10, RAW12, or RAW14 layout.", -1);
        }
    }

    internal static int GetNamedGroupSize(RawPacking packing)
    {
        switch (packing)
        {
            case RawPacking.Raw10:
                return 4;
            case RawPacking.Raw12:
                return 2;
            case RawPacking.Raw14:
                return 4;
            default:
                throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "The packing is not a named RAW10, RAW12, or RAW14 layout.", -1);
        }
    }

    internal static void ValidateSampleCount(int sampleCount)
    {
        if (sampleCount < 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The sample count cannot be negative.", -1);
        }
    }

    internal static void ValidateBitDepth(int bitDepth)
    {
        if (bitDepth < 1 || bitDepth > 32)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "The sample bit depth must be between 1 and 32.", -1);
        }
    }

    internal static void RequireSource(System.ReadOnlySpan<byte> source, int requiredLength)
    {
        if (source.Length < requiredLength)
        {
            throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "The packed source is shorter than the requested sample count.", source.Length);
        }
    }

    internal static void RequireDestination(System.Span<byte> destination, int requiredLength)
    {
        if (destination.Length < requiredLength)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The packed destination is too short for the samples.", -1);
        }
    }

    internal static void RequireSampleDestination(int destinationLength, int sampleCount)
    {
        if (destinationLength < sampleCount)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The sample destination is shorter than the requested sample count.", -1);
        }
    }
}
