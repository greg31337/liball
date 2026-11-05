namespace qrcode;

internal class BitBuffer
{
    byte[] Bytes;
    int BitLength;

    internal BitBuffer(int initialCapacityBytes)
    {
        int allocationBytes = CheckedDimensions.CalculateAllocationBytes(
            initialCapacityBytes,
            1,
            QrLimits.DefaultMaximumAllocationBytes);
        Bytes = new byte[allocationBytes];
    }

    internal void AppendBit(bool value)
    {
        int requiredBitLength = CheckedDimensions.CalculateSum(
            BitLength,
            1,
            int.MaxValue,
            "BitLength");
        EnsureBitCapacity(requiredBitLength);
        if (value)
        {
            int byteIndex = BitLength / 8;
            int bitOffset = 7 - BitLength % 8;
            Bytes[byteIndex] = (byte)(Bytes[byteIndex] | 1 << bitOffset);
        }

        BitLength = requiredBitLength;
    }

    internal void AppendBits(uint value, int bitCount)
    {
        if (bitCount < 0 || bitCount > 32)
        {
            throw new QrInputException("The bit count must be between zero and 32.", "bitCount");
        }

        if (bitCount < 32 && value >> bitCount != 0)
        {
            throw new QrInputException("The value does not fit in the requested bit count.", "value");
        }

        int requiredBitLength = CheckedDimensions.CalculateSum(
            BitLength,
            bitCount,
            int.MaxValue,
            "BitLength");
        EnsureBitCapacity(requiredBitLength);
        for (int sourceBit = bitCount - 1; sourceBit >= 0; sourceBit--)
        {
            bool bit = ((value >> sourceBit) & 1U) != 0;
            if (bit)
            {
                int byteIndex = BitLength / 8;
                int bitOffset = 7 - BitLength % 8;
                Bytes[byteIndex] = (byte)(Bytes[byteIndex] | 1 << bitOffset);
            }

            BitLength++;
        }
    }

    internal void AppendBytes(global::System.ReadOnlySpan<byte> values)
    {
        int appendedBitCount = CheckedDimensions.CalculateProduct(
            values.Length,
            8,
            int.MaxValue,
            "BitLength");
        int requiredBitLength = CheckedDimensions.CalculateSum(
            BitLength,
            appendedBitCount,
            int.MaxValue,
            "BitLength");
        EnsureBitCapacity(requiredBitLength);

        if (BitLength % 8 == 0)
        {
            global::System.Span<byte> destination = new global::System.Span<byte>(
                Bytes,
                BitLength / 8,
                values.Length);
            values.CopyTo(destination);
            BitLength = requiredBitLength;
            return;
        }

        for (int valueIndex = 0; valueIndex < values.Length; valueIndex++)
        {
            byte value = values[valueIndex];
            for (int sourceBit = 7; sourceBit >= 0; sourceBit--)
            {
                bool bit = ((value >> sourceBit) & 1) != 0;
                if (bit)
                {
                    int byteIndex = BitLength / 8;
                    int bitOffset = 7 - BitLength % 8;
                    Bytes[byteIndex] = (byte)(Bytes[byteIndex] | 1 << bitOffset);
                }

                BitLength++;
            }
        }
    }

    internal bool GetBit(int index)
    {
        if (index < 0 || index >= BitLength)
        {
            throw new QrInputException("The bit index is outside the buffer.", "index");
        }

        int byteIndex = index / 8;
        int bitOffset = 7 - index % 8;
        return ((Bytes[byteIndex] >> bitOffset) & 1) != 0;
    }

    internal int GetBitLength()
    {
        return BitLength;
    }

    internal int GetByteLength()
    {
        return (BitLength + 7) / 8;
    }

    internal byte[] GetBytes()
    {
        int byteLength = GetByteLength();
        byte[] result = new byte[byteLength];
        global::System.ReadOnlySpan<byte> source = new global::System.ReadOnlySpan<byte>(Bytes, 0, byteLength);
        source.CopyTo(result);
        return result;
    }

    internal void CopyBytes(global::System.Span<byte> destination)
    {
        int byteLength = GetByteLength();
        if (destination.Length < byteLength)
        {
            throw new QrInputException("The destination is too short for the buffered bytes.", "destination");
        }

        global::System.ReadOnlySpan<byte> source = new global::System.ReadOnlySpan<byte>(Bytes, 0, byteLength);
        source.CopyTo(destination);
    }

    internal void ClearBits()
    {
        global::System.Span<byte> bytes = Bytes;
        bytes.Clear();
        BitLength = 0;
    }

    internal void EnsureBitCapacity(int requiredBitLength)
    {
        if (requiredBitLength < 0)
        {
            throw new QrInputException("The required bit length cannot be negative.", "requiredBitLength");
        }

        int paddedBitLength = CheckedDimensions.CalculateSum(
            requiredBitLength,
            7,
            int.MaxValue,
            "BitLength");
        int requiredByteLength = paddedBitLength / 8;
        CheckedDimensions.CalculateAllocationBytes(
            requiredByteLength,
            1,
            QrLimits.DefaultMaximumAllocationBytes);
        if (requiredByteLength <= Bytes.Length)
        {
            return;
        }

        int expandedByteLength = Bytes.Length == 0 ? 1 : Bytes.Length;
        while (expandedByteLength < requiredByteLength)
        {
            if (expandedByteLength > QrLimits.DefaultMaximumAllocationBytes / 2)
            {
                expandedByteLength = requiredByteLength;
            }
            else
            {
                expandedByteLength *= 2;
            }
        }

        byte[] expandedBytes = new byte[expandedByteLength];
        global::System.ReadOnlySpan<byte> source = Bytes;
        source.CopyTo(expandedBytes);
        Bytes = expandedBytes;
    }
}
