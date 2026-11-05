namespace icc;

internal static class CheckedSize
{
    internal static long AddOffsets(long left, long right, long maximum)
    {
        if (left < 0 || right < 0 || maximum < 0)
        {
            throw new IccProfileFormatException("Offsets, sizes, and limits must not be negative.");
        }

        if (right > long.MaxValue - left)
        {
            throw new IccProfileFormatException("Adding the offset and size would overflow a signed 64-bit value.");
        }

        long result = left + right;
        if (result > maximum)
        {
            throw new IccProfileFormatException("The resulting byte position exceeds the configured or available limit.");
        }

        return result;
    }

    internal static uint AddUnsigned(uint left, uint right)
    {
        if (right > uint.MaxValue - left)
        {
            throw new IccProfileFormatException("Adding the unsigned values would wrap around.");
        }

        return left + right;
    }

    internal static int MultiplyCount(int count, int itemSize, int maximumBytes)
    {
        if (count < 0 || itemSize < 0 || maximumBytes < 0)
        {
            throw new IccProfileFormatException("Counts, item sizes, and limits must not be negative.");
        }

        if (count > System.Array.MaxLength)
        {
            throw new IccProfileFormatException("The requested element count cannot be represented by a managed array.");
        }

        if (count != 0 && itemSize > int.MaxValue / count)
        {
            throw new IccProfileFormatException("Multiplying the count and item size would overflow a signed 32-bit value.");
        }

        int result = count * itemSize;
        if (result > System.Array.MaxLength)
        {
            throw new IccProfileFormatException("The requested byte count cannot be represented by a managed array.");
        }

        if (result > maximumBytes)
        {
            throw new IccProfileFormatException("The requested byte count exceeds the configured resource limit.");
        }

        return result;
    }

    internal static int ConvertUnsignedSize(uint value, int maximum)
    {
        if (maximum < 0)
        {
            throw new IccProfileFormatException("A size limit must not be negative.");
        }

        if (value > int.MaxValue)
        {
            throw new IccProfileFormatException("The unsigned size cannot be represented by a signed managed size.");
        }

        int result = (int)value;
        if (result > System.Array.MaxLength)
        {
            throw new IccProfileFormatException("The requested size cannot be represented by a managed array.");
        }

        if (result > maximum)
        {
            throw new IccProfileFormatException("The requested size exceeds the configured resource limit.");
        }

        return result;
    }

    internal static int AlignSize(int size, int alignment, int maximum)
    {
        if (size < 0 || alignment <= 0 || maximum < 0)
        {
            throw new IccProfileFormatException("Sizes and limits must not be negative, and alignment must be positive.");
        }

        int remainder = size % alignment;
        if (remainder == 0)
        {
            if (size > maximum)
            {
                throw new IccProfileFormatException("The aligned size exceeds the configured resource limit.");
            }

            return size;
        }

        int padding = alignment - remainder;
        if (padding > int.MaxValue - size)
        {
            throw new IccProfileFormatException("Alignment would overflow a signed managed size.");
        }

        int result = size + padding;
        if (result > maximum)
        {
            throw new IccProfileFormatException("The aligned size exceeds the configured resource limit.");
        }

        return result;
    }

    internal static long ValidateRange(long offset, long length, long containerLength)
    {
        if (containerLength < 0)
        {
            throw new IccProfileFormatException("A container length must not be negative.");
        }

        return AddOffsets(offset, length, containerLength);
    }

    internal static int AccumulateBytes(int currentTotal, int additionalBytes, int maximumTotal)
    {
        if (currentTotal < 0 || additionalBytes < 0 || maximumTotal < 0)
        {
            throw new IccProfileFormatException("Accumulated byte counts and limits must not be negative.");
        }

        if (additionalBytes > int.MaxValue - currentTotal)
        {
            throw new IccProfileFormatException("Accumulating byte counts would overflow a signed managed size.");
        }

        int result = currentTotal + additionalBytes;
        if (result > maximumTotal)
        {
            throw new IccProfileFormatException("The accumulated byte count exceeds the configured resource limit.");
        }

        return result;
    }

    internal static byte[] CreateByteArray(int count, int itemSize, int maximumBytes)
    {
        int byteCount = MultiplyCount(count, itemSize, maximumBytes);
        return new byte[byteCount];
    }
}
