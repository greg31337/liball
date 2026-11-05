using System.IO;

namespace ftp;

internal class FtpTransferOutputBuffer
{
    internal Stream Destination;
    internal byte[] Buffer;
    internal int Count;
    internal long BytesWritten;

    internal FtpTransferOutputBuffer(Stream destination, int bufferBytes)
    {
        Destination = destination;
        Buffer = new byte[bufferBytes];
        Count = 0;
        BytesWritten = 0;
    }

    internal void WriteByte(byte value)
    {
        if (BytesWritten == long.MaxValue)
        {
            throw new IOException("The transfer conversion byte count exceeded Int64 capacity.");
        }

        Buffer[Count] = value;
        Count++;
        BytesWritten++;
        if (Count == Buffer.Length)
        {
            FlushPendingBytes();
        }
    }

    internal void FlushPendingBytes()
    {
        if (Count == 0)
        {
            return;
        }

        Destination.Write(Buffer, 0, Count);
        Count = 0;
    }
}
