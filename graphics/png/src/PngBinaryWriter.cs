using System.IO;

namespace png;

internal class PngBinaryWriter
{
    internal static void WriteUInt32Span(Span<byte> data, int offset, uint value)
    {
        if (offset < 0 || offset > data.Length - 4)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        data[offset] = (byte)((value >> 24) & 0xFF);
        data[offset + 1] = (byte)((value >> 16) & 0xFF);
        data[offset + 2] = (byte)((value >> 8) & 0xFF);
        data[offset + 3] = (byte)(value & 0xFF);
    }

    internal static void WriteUInt8(byte[] data, int offset, byte value)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (offset < 0 || offset >= data.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        data[offset] = value;
    }

    internal static void WriteUInt16(byte[] data, int offset, ushort value)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (offset < 0 || offset > data.Length - 2)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        data[offset] = (byte)((value >> 8) & 0xFF);
        data[offset + 1] = (byte)(value & 0xFF);
    }

    internal static void WriteUInt32(byte[] data, int offset, uint value)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (offset < 0 || offset > data.Length - 4)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        data[offset] = (byte)((value >> 24) & 0xFF);
        data[offset + 1] = (byte)((value >> 16) & 0xFF);
        data[offset + 2] = (byte)((value >> 8) & 0xFF);
        data[offset + 3] = (byte)(value & 0xFF);
    }

    internal static void WriteUInt8(Stream stream, byte value)
    {
        ArgumentNullException.ThrowIfNull(stream);

        stream.WriteByte(value);
    }

    internal static void WriteUInt16(Stream stream, ushort value)
    {
        ArgumentNullException.ThrowIfNull(stream);

        stream.WriteByte((byte)((value >> 8) & 0xFF));
        stream.WriteByte((byte)(value & 0xFF));
    }

    internal static void WriteUInt32(Stream stream, uint value)
    {
        ArgumentNullException.ThrowIfNull(stream);

        stream.WriteByte((byte)((value >> 24) & 0xFF));
        stream.WriteByte((byte)((value >> 16) & 0xFF));
        stream.WriteByte((byte)((value >> 8) & 0xFF));
        stream.WriteByte((byte)(value & 0xFF));
    }
}
