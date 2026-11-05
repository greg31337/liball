namespace qrcode;

internal class BitReader
{
    byte[] Bytes;
    int BitLength;
    int Position;

    internal BitReader(byte[] bytes, int bitLength)
    {
        if (bytes == null)
        {
            throw new QrInputException("The byte buffer cannot be null.", "bytes");
        }

        if (bitLength < 0)
        {
            throw new QrInputException("The bit length cannot be negative.", "bitLength");
        }

        int availableBitCount = CheckedDimensions.CalculateProduct(
            bytes.Length,
            8,
            int.MaxValue,
            "BitLength");
        if (bitLength > availableBitCount)
        {
            throw new QrInputException("The bit length exceeds the supplied byte buffer.", "bitLength");
        }

        Bytes = new byte[bytes.Length];
        global::System.ReadOnlySpan<byte> source = bytes;
        source.CopyTo(Bytes);
        BitLength = bitLength;
    }

    internal bool ReadBit()
    {
        if (Position >= BitLength)
        {
            throw new QrDecodeException(
                "The encoded data ended before another bit could be read.",
                QrFailureReason.InvalidData,
                -1,
                0);
        }

        int byteIndex = Position / 8;
        int bitOffset = 7 - Position % 8;
        bool value = ((Bytes[byteIndex] >> bitOffset) & 1) != 0;
        Position++;
        return value;
    }

    internal uint ReadBits(int bitCount)
    {
        if (bitCount < 0 || bitCount > 32)
        {
            throw new QrInputException("The bit count must be between zero and 32.", "bitCount");
        }

        if (bitCount > BitLength - Position)
        {
            throw new QrDecodeException(
                "The encoded data ended before the requested bits could be read.",
                QrFailureReason.InvalidData,
                -1,
                0);
        }

        uint value = 0;
        for (int index = 0; index < bitCount; index++)
        {
            value <<= 1;
            if (ReadBit())
            {
                value |= 1U;
            }
        }

        return value;
    }

    internal void ReadBytes(int byteCount, global::System.Span<byte> destination)
    {
        if (byteCount < 0)
        {
            throw new QrInputException("The byte count cannot be negative.", "byteCount");
        }

        if (destination.Length < byteCount)
        {
            throw new QrInputException("The destination is too short for the requested bytes.", "destination");
        }

        int requestedBitCount = CheckedDimensions.CalculateProduct(
            byteCount,
            8,
            int.MaxValue,
            "BitLength");
        if (requestedBitCount > BitLength - Position)
        {
            throw new QrDecodeException(
                "The encoded data ended before the requested bytes could be read.",
                QrFailureReason.InvalidData,
                -1,
                0);
        }

        for (int index = 0; index < byteCount; index++)
        {
            destination[index] = (byte)ReadBits(8);
        }
    }

    internal void SkipBits(int bitCount)
    {
        if (bitCount < 0)
        {
            throw new QrInputException("The bit count cannot be negative.", "bitCount");
        }

        if (bitCount > BitLength - Position)
        {
            throw new QrDecodeException(
                "The encoded data ended before the requested bits could be skipped.",
                QrFailureReason.InvalidData,
                -1,
                0);
        }

        Position += bitCount;
    }

    internal void SeekBitPosition(int position)
    {
        if (position < 0 || position > BitLength)
        {
            throw new QrInputException("The bit position is outside the reader.", "position");
        }

        Position = position;
    }

    internal int GetPosition()
    {
        return Position;
    }

    internal int GetRemainingBitCount()
    {
        return BitLength - Position;
    }
}
