namespace raw;

internal class LsbBitReader
{
    internal BitReaderCore Core;

    internal LsbBitReader(BoundedInput input)
    {
        Core = new BitReaderCore(input, RawBitOrder.LeastSignificantBitFirst);
    }

    internal uint PeekBits(int count)
    {
        return Core.PeekBits(count);
    }

    internal void ConsumeBits(int count)
    {
        Core.ConsumeBits(count);
    }

    internal uint ReadBits(int count)
    {
        return Core.ReadBits(count);
    }

    internal long GetBitPosition()
    {
        return Core.GetBitPosition();
    }

    internal long GetRemainingBitCount()
    {
        return Core.GetRemainingBitCount();
    }

    internal bool IsAtEnd()
    {
        return Core.IsAtEnd();
    }

    internal void AlignToByte()
    {
        Core.AlignToByte();
    }

    internal void AlignToByteWithExpectedPadding(int expectedPaddingBit)
    {
        Core.AlignToByteWithExpectedPadding(expectedPaddingBit);
    }

    internal void ValidatePaddingToEnd(int expectedPaddingBit)
    {
        Core.ValidatePaddingToEnd(expectedPaddingBit);
    }

    internal void RequireEnd()
    {
        Core.RequireEnd();
    }
}
