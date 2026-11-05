namespace avif;

internal static class IntegerConversion
{
    internal static byte ToByte(int value)
    {
        return (byte)(value & byte.MaxValue);
    }

    internal static byte ToByte(uint value)
    {
        return (byte)(value & byte.MaxValue);
    }

    internal static byte ToByte(long value)
    {
        return (byte)(value & byte.MaxValue);
    }

    internal static byte ToByte(ulong value)
    {
        return (byte)(value & byte.MaxValue);
    }

    internal static byte ToByte(ushort value)
    {
        return (byte)(value & byte.MaxValue);
    }

    internal static sbyte ToInt8(byte value)
    {
        return value <= sbyte.MaxValue
            ? (sbyte)value
            : (sbyte)(value - 256);
    }

    internal static short ToInt16(ushort value)
    {
        return value <= short.MaxValue
            ? (short)value
            : (short)(value - 65536);
    }

    internal static ushort ToUInt16(short value)
    {
        return value >= 0
            ? (ushort)value
            : (ushort)(value + 65536);
    }

    internal static int ToInt32(uint value)
    {
        return value <= int.MaxValue
            ? (int)value
            : (int)(value - 2147483648U) + int.MinValue;
    }

    internal static uint ToUInt32(int value)
    {
        return value >= 0
            ? (uint)value
            : (uint)((long)value + 4294967296L);
    }

    internal static long ToInt64(ulong value)
    {
        return value <= long.MaxValue
            ? (long)value
            : (long)(value - 9223372036854775808UL) + long.MinValue;
    }

    internal static ulong ToUInt64(long value)
    {
        return value >= 0
            ? (ulong)value
            : (ulong)(value - long.MinValue) + 9223372036854775808UL;
    }
}
