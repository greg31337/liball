namespace image;

internal static class IntegerBitPattern
{
    internal static sbyte ToSigned8(byte value)
    {
        return value <= 127 ? (sbyte)value : (sbyte)((int)value - 256);
    }

    internal static short ToSigned16(ushort value)
    {
        return value <= 32767 ? (short)value : (short)((int)value - 65536);
    }

    internal static int ToSigned32(uint value)
    {
        return value <= 2147483647 ? (int)value : (int)((long)value - 4294967296);
    }

    internal static long ToSigned64(ulong value)
    {
        return value <= 9223372036854775807 ? (long)value : -1 - (long)(ulong.MaxValue - value);
    }

    internal static uint ToUnsigned32(int value)
    {
        return value >= 0 ? (uint)value : uint.MaxValue - (uint)(-(long)value) + 1;
    }

    internal static ulong ToUnsigned64(long value)
    {
        return value >= 0 ? (ulong)value : ulong.MaxValue - (ulong)(-(value + 1));
    }
}
