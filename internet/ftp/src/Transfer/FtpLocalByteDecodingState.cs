using System.IO;

namespace ftp;

internal class FtpLocalByteDecodingState
{
    internal int LocalByteSize;
    internal int StorageByteCount;
    internal int UnusedHighBits;
    internal byte[] LogicalByte;
    internal int LogicalBitCount;
    internal bool PartialBitsNonzero;
    internal bool HasExpectedLogicalBytes;
    internal long ExpectedLogicalBytes;
    internal long DecodedLogicalBytes;
    internal int PaddingBitCount;
    internal FtpTransferOutputBuffer Output;

    internal FtpLocalByteDecodingState(
        int localByteSize,
        bool hasExpectedLogicalBytes,
        long expectedLogicalBytes,
        FtpTransferOutputBuffer output)
    {
        LocalByteSize = localByteSize;
        StorageByteCount = (localByteSize + 7) / 8;
        UnusedHighBits = StorageByteCount * 8 - localByteSize;
        LogicalByte = new byte[StorageByteCount];
        LogicalBitCount = 0;
        PartialBitsNonzero = false;
        HasExpectedLogicalBytes = hasExpectedLogicalBytes;
        ExpectedLogicalBytes = expectedLogicalBytes;
        DecodedLogicalBytes = 0;
        PaddingBitCount = 0;
        Output = output;
    }

    internal void ConsumeWireByte(byte value)
    {
        int bitOffset = 7;
        while (bitOffset >= 0)
        {
            int bit = (value >> bitOffset) & 1;
            ConsumeWireBit(bit);
            bitOffset--;
        }
    }

    internal void ConsumeWireBit(int value)
    {
        if (HasExpectedLogicalBytes && DecodedLogicalBytes == ExpectedLogicalBytes)
        {
            PaddingBitCount++;
            if (PaddingBitCount > 7)
            {
                throw new InvalidDataException("The TYPE L wire stream has more than one final padding segment.");
            }

            if (value != 0)
            {
                throw new InvalidDataException("TYPE L wire padding bits must be zero.");
            }

            return;
        }

        int storageBit = UnusedHighBits + LogicalBitCount;
        int byteIndex = storageBit / 8;
        int bitOffset = 7 - storageBit % 8;
        if (value != 0)
        {
            LogicalByte[byteIndex] = (byte)(LogicalByte[byteIndex] | (1 << bitOffset));
            PartialBitsNonzero = true;
        }

        LogicalBitCount++;
        if (LogicalBitCount == LocalByteSize)
        {
            WriteLogicalByte();
        }
    }

    internal void WriteLogicalByte()
    {
        int index = 0;
        while (index < LogicalByte.Length)
        {
            Output.WriteByte(LogicalByte[index]);
            LogicalByte[index] = 0;
            index++;
        }

        if (DecodedLogicalBytes == long.MaxValue)
        {
            throw new IOException("The TYPE L logical-byte count exceeded Int64 capacity.");
        }

        DecodedLogicalBytes++;
        LogicalBitCount = 0;
        PartialBitsNonzero = false;
    }

    internal void CompleteDecoding()
    {
        if (HasExpectedLogicalBytes)
        {
            if (DecodedLogicalBytes != ExpectedLogicalBytes || LogicalBitCount != 0)
            {
                throw new InvalidDataException("The TYPE L wire stream ended before the expected logical-byte count.");
            }

            int expectedPaddingBits = GetExpectedPaddingBits();
            if (PaddingBitCount != expectedPaddingBits)
            {
                throw new InvalidDataException("The TYPE L wire stream has an invalid padded length.");
            }

            return;
        }

        if (LogicalBitCount > 7)
        {
            throw new InvalidDataException("The TYPE L wire stream ended within a logical byte.");
        }

        if (PartialBitsNonzero)
        {
            throw new InvalidDataException("TYPE L wire padding bits must be zero.");
        }
    }

    internal int GetExpectedPaddingBits()
    {
        int usedBits = (int)((ExpectedLogicalBytes % 8) * (LocalByteSize % 8) % 8);
        if (usedBits == 0)
        {
            return 0;
        }

        return 8 - usedBits;
    }
}
