namespace raw;

internal class LsbBitWriter
{
    internal BitWriterCore Core;

    internal LsbBitWriter(System.Memory<byte> destination)
    {
        Core = new BitWriterCore(destination, RawBitOrder.LeastSignificantBitFirst);
    }

    internal static LsbBitWriter CreateStream(System.IO.Stream destination, long maximumBytes)
    {
        LsbBitWriter writer = new LsbBitWriter(default(System.Memory<byte>));
        writer.Core = BitWriterCore.CreateStream(destination, maximumBytes, RawBitOrder.LeastSignificantBitFirst);
        return writer;
    }

    internal void WriteBits(uint value, int count)
    {
        Core.WriteBits(value, count);
    }

    internal void AlignToByte(int paddingBit)
    {
        Core.AlignToByte(paddingBit);
    }

    internal long Finish(int paddingBit)
    {
        return Core.Finish(paddingBit);
    }

    internal long GetBitPosition()
    {
        return Core.GetBitPosition();
    }

    internal long GetBytesWritten()
    {
        return Core.GetBytesWritten();
    }

    internal long GetRemainingBitCapacity()
    {
        return Core.GetRemainingBitCapacity();
    }
}
