namespace raw;

internal class MsbBitReader
{
    internal BitReaderCore Core;

    internal MsbBitReader(BoundedInput input)
    {
        Core = new BitReaderCore(input, RawBitOrder.MostSignificantBitFirst);
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

internal class BitReaderCore
{
    internal BoundedInput Input;
    internal RawBitOrder BitOrder;
    internal ulong Buffer;
    internal int BufferedBitCount;
    internal long BitPosition;

    internal BitReaderCore(BoundedInput input, RawBitOrder bitOrder)
    {
        if (input == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A bounded input is required.", -1);
        }
        BitUtility.ValidateBitOrder(bitOrder);
        Input = input;
        BitOrder = bitOrder;
        Buffer = 0;
        BufferedBitCount = 0;
        BitPosition = 0;
    }

    internal uint PeekBits(int count)
    {
        BitUtility.ValidateBitCount(count);
        Fill(count);
        if (count == 0)
        {
            return 0;
        }

        ulong mask = BitUtility.Mask(count);
        if (BitOrder == RawBitOrder.MostSignificantBitFirst)
        {
            return (uint)((Buffer >> (BufferedBitCount - count)) & mask);
        }
        return (uint)(Buffer & mask);
    }

    internal void ConsumeBits(int count)
    {
        PeekBits(count);
        if (count == 0)
        {
            return;
        }

        BufferedBitCount -= count;
        if (BitOrder == RawBitOrder.MostSignificantBitFirst)
        {
            Buffer &= BitUtility.Mask(BufferedBitCount);
        }
        else
        {
            Buffer >>= count;
        }
        BitPosition = CheckedMath.Add(BitPosition, count);
    }

    internal uint ReadBits(int count)
    {
        uint value = PeekBits(count);
        ConsumeBits(count);
        return value;
    }

    internal long GetBitPosition()
    {
        return BitPosition;
    }

    internal long GetRemainingBitCount()
    {
        long inputBits = CheckedMath.Multiply(Input.GetRemaining(), 8);
        return CheckedMath.Add(inputBits, BufferedBitCount);
    }

    internal bool IsAtEnd()
    {
        return BufferedBitCount == 0 && Input.GetRemaining() == 0;
    }

    internal void AlignToByte()
    {
        int remainder = (int)(BitPosition % 8);
        if (remainder != 0)
        {
            ConsumeBits(8 - remainder);
        }
    }

    internal void AlignToByteWithExpectedPadding(int expectedPaddingBit)
    {
        BitUtility.ValidatePaddingBit(expectedPaddingBit);
        int remainder = (int)(BitPosition % 8);
        if (remainder == 0)
        {
            return;
        }

        int count = 8 - remainder;
        uint value = PeekBits(count);
        if (!BitUtility.IsPadding(value, count, expectedPaddingBit))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "Bit padding does not match the expected value.", GetCurrentByteOffset());
        }
        ConsumeBits(count);
    }

    internal void ValidatePaddingToEnd(int expectedPaddingBit)
    {
        BitUtility.ValidatePaddingBit(expectedPaddingBit);
        while (!IsAtEnd())
        {
            long remaining = GetRemainingBitCount();
            int count = 32;
            if (remaining < count)
            {
                count = (int)remaining;
            }
            uint value = PeekBits(count);
            if (!BitUtility.IsPadding(value, count, expectedPaddingBit))
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "Trailing bit padding does not match the expected value.", GetCurrentByteOffset());
            }
            ConsumeBits(count);
        }
    }

    internal void RequireEnd()
    {
        if (!IsAtEnd())
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "Unread bits remain in the bounded input.", GetCurrentByteOffset());
        }
    }

    internal void Fill(int count)
    {
        while (BufferedBitCount < count)
        {
            byte value = Input.ReadByte();
            if (BitOrder == RawBitOrder.MostSignificantBitFirst)
            {
                Buffer = (Buffer << 8) | value;
            }
            else
            {
                Buffer |= (ulong)value << BufferedBitCount;
            }
            BufferedBitCount += 8;
        }
    }

    internal long GetCurrentByteOffset()
    {
        return CheckedMath.Add(Input.Start, BitPosition / 8);
    }
}

internal static class BitUtility
{
    internal static byte ToUInt8Bits(sbyte value)
    {
        return (byte)(value & byte.MaxValue);
    }

    internal static sbyte ToInt8Bits(byte value)
    {
        int signed = value <= sbyte.MaxValue ? value : value - 256;
        return (sbyte)signed;
    }

    internal static ushort ToUInt16Bits(short value)
    {
        return (ushort)(value & ushort.MaxValue);
    }

    internal static short ToInt16Bits(ushort value)
    {
        int signed = value <= short.MaxValue ? value : value - 65536;
        return (short)signed;
    }

    internal static uint ToUInt32Bits(int value)
    {
        if (value >= 0)
        {
            return (uint)value;
        }
        return uint.MaxValue - (uint)(-(value + 1));
    }

    internal static int ToInt32Bits(uint value)
    {
        if (value <= int.MaxValue)
        {
            return (int)value;
        }
        return -1 - (int)(uint.MaxValue - value);
    }

    internal static byte WrapUInt8(int value)
    {
        int wrapped = value % 256;
        if (wrapped < 0)
        {
            wrapped += 256;
        }
        return (byte)wrapped;
    }

    internal static ushort AddUInt16(ushort left, ushort right)
    {
        int sum = left + right;
        return (ushort)(sum <= ushort.MaxValue ? sum : sum - 65536);
    }

    internal static ushort SubtractUInt16(ushort left, ushort right)
    {
        int difference = left - right;
        return (ushort)(difference >= 0 ? difference : difference + 65536);
    }

    internal static uint AddUInt32(uint left, uint right)
    {
        if (left <= uint.MaxValue - right)
        {
            return left + right;
        }
        return left - (uint.MaxValue - right) - 1U;
    }

    internal static uint SubtractUInt32(uint left, uint right)
    {
        if (left >= right)
        {
            return left - right;
        }
        return uint.MaxValue - (right - left - 1U);
    }

    internal static ulong AddUInt64(ulong left, ulong right)
    {
        if (left <= ulong.MaxValue - right)
        {
            return left + right;
        }
        return left - (ulong.MaxValue - right) - 1UL;
    }

    internal static ulong SubtractUInt64(ulong left, ulong right)
    {
        if (left >= right)
        {
            return left - right;
        }
        return ulong.MaxValue - (right - left - 1UL);
    }

    internal static void ValidateBitCount(int count)
    {
        if (count < 0 || count > 32)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Bit counts must be between 0 and 32.", -1);
        }
    }

    internal static void ValidatePaddingBit(int paddingBit)
    {
        if (paddingBit != 0 && paddingBit != 1)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A padding bit must be zero or one.", -1);
        }
    }

    internal static void ValidateBitOrder(RawBitOrder bitOrder)
    {
        if (bitOrder != RawBitOrder.MostSignificantBitFirst && bitOrder != RawBitOrder.LeastSignificantBitFirst)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "The bit order is not defined.", -1);
        }
    }

    internal static ulong Mask(int count)
    {
        if (count == 0)
        {
            return 0;
        }
        if (count >= 64)
        {
            return ulong.MaxValue;
        }
        return (1UL << count) - 1UL;
    }

    internal static bool IsPadding(uint value, int count, int paddingBit)
    {
        if (paddingBit == 0)
        {
            return value == 0;
        }
        return value == (uint)Mask(count);
    }
}
