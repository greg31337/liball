namespace icc.Tests;

internal static class IndependentBigEndian
{
    internal static byte[] EncodeUInt16(ushort value)
    {
        byte[] bytes = new byte[2];
        WriteUInt16(bytes, 0, value);
        return bytes;
    }

    internal static byte[] EncodeUInt32(uint value)
    {
        byte[] bytes = new byte[4];
        WriteUInt32(bytes, 0, value);
        return bytes;
    }

    internal static byte[] EncodeUInt64(ulong value)
    {
        byte[] bytes = new byte[8];
        WriteUInt64(bytes, 0, value);
        return bytes;
    }

    internal static void WriteUInt16(byte[] destination, int offset,
        ushort value)
    {
        destination[offset] = (byte)(value >> 8);
        destination[offset + 1] = (byte)value;
    }

    internal static void WriteInt16(byte[] destination, int offset,
        short value)
    {
        WriteUInt16(destination, offset, unchecked((ushort)value));
    }

    internal static void WriteUInt32(byte[] destination, int offset,
        uint value)
    {
        destination[offset] = (byte)(value >> 24);
        destination[offset + 1] = (byte)(value >> 16);
        destination[offset + 2] = (byte)(value >> 8);
        destination[offset + 3] = (byte)value;
    }

    internal static void WriteInt32(byte[] destination, int offset,
        int value)
    {
        WriteUInt32(destination, offset, unchecked((uint)value));
    }

    internal static void WriteUInt64(byte[] destination, int offset,
        ulong value)
    {
        destination[offset] = (byte)(value >> 56);
        destination[offset + 1] = (byte)(value >> 48);
        destination[offset + 2] = (byte)(value >> 40);
        destination[offset + 3] = (byte)(value >> 32);
        destination[offset + 4] = (byte)(value >> 24);
        destination[offset + 5] = (byte)(value >> 16);
        destination[offset + 6] = (byte)(value >> 8);
        destination[offset + 7] = (byte)value;
    }

    internal static void WriteInt64(byte[] destination, int offset,
        long value)
    {
        WriteUInt64(destination, offset, unchecked((ulong)value));
    }

    internal static void WriteSingle(byte[] destination, int offset,
        float value)
    {
        WriteInt32(destination, offset, BitConverter.SingleToInt32Bits(value));
    }

    internal static void WriteDouble(byte[] destination, int offset,
        double value)
    {
        WriteInt64(destination, offset, BitConverter.DoubleToInt64Bits(value));
    }
}
