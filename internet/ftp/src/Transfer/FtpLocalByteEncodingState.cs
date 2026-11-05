using System.IO;

namespace ftp;

internal class FtpLocalByteEncodingState
{
    internal int LocalByteSize;
    internal int StorageByteCount;
    internal int UnusedHighBits;
    internal byte[] LogicalByte;
    internal int StoredByteCount;
    internal long LogicalByteCount;
    internal FtpLocalByteBitWriter Writer;

    internal FtpLocalByteEncodingState(int localByteSize, FtpTransferOutputBuffer output)
    {
        LocalByteSize = localByteSize;
        StorageByteCount = (localByteSize + 7) / 8;
        UnusedHighBits = StorageByteCount * 8 - localByteSize;
        LogicalByte = new byte[StorageByteCount];
        StoredByteCount = 0;
        LogicalByteCount = 0;
        Writer = new FtpLocalByteBitWriter(output);
    }

    internal void ConsumeLocalByte(byte value)
    {
        LogicalByte[StoredByteCount] = value;
        StoredByteCount++;
        if (StoredByteCount == StorageByteCount)
        {
            WriteLogicalByte();
            StoredByteCount = 0;
        }
    }

    internal void WriteLogicalByte()
    {
        if (UnusedHighBits != 0)
        {
            int unusedMask = 0xFF << (8 - UnusedHighBits);
            if ((LogicalByte[0] & unusedMask) != 0)
            {
                throw new InvalidDataException("A local TYPE L value has non-zero high padding bits.");
            }
        }

        int storageBit = UnusedHighBits;
        int storageBits = StorageByteCount * 8;
        while (storageBit < storageBits)
        {
            int byteIndex = storageBit / 8;
            int bitOffset = 7 - storageBit % 8;
            int bit = (LogicalByte[byteIndex] >> bitOffset) & 1;
            Writer.WriteBit(bit);
            storageBit++;
        }

        if (LogicalByteCount == long.MaxValue)
        {
            throw new IOException("The TYPE L logical-byte count exceeded Int64 capacity.");
        }

        LogicalByteCount++;
    }

    internal void CompleteEncoding()
    {
        if (StoredByteCount != 0)
        {
            throw new InvalidDataException("The local TYPE L source ended within a logical byte.");
        }

        Writer.CompleteBits();
    }
}
