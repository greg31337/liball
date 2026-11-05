using System.IO;

namespace png;

internal class PngBinaryReader
{
    internal static uint ReadUInt32Span(ReadOnlySpan<byte> data, int offset)
    {
        if (offset < 0 || offset > data.Length - 4)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        return ((uint)data[offset] << 24)
            | ((uint)data[offset + 1] << 16)
            | ((uint)data[offset + 2] << 8)
            | data[offset + 3];
    }

    internal static byte ReadUInt8(byte[] data, int offset)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (offset < 0 || offset >= data.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        return data[offset];
    }

    internal static ushort ReadUInt16(byte[] data, int offset)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (offset < 0 || offset > data.Length - 2)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        return (ushort)((data[offset] << 8) | data[offset + 1]);
    }

    internal static uint ReadUInt32(byte[] data, int offset)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (offset < 0 || offset > data.Length - 4)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        return ((uint)data[offset] << 24)
            | ((uint)data[offset + 1] << 16)
            | ((uint)data[offset + 2] << 8)
            | data[offset + 3];
    }

    internal static byte ReadUInt8(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        int value = stream.ReadByte();
        if (value < 0)
        {
            throw new EndOfStreamException();
        }

        return (byte)value;
    }

    internal static ushort ReadUInt16(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        int byte0 = stream.ReadByte();
        int byte1 = stream.ReadByte();
        if (byte0 < 0 || byte1 < 0)
        {
            throw new EndOfStreamException();
        }

        return (ushort)((byte0 << 8) | byte1);
    }

    internal static uint ReadUInt32(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        int byte0 = stream.ReadByte();
        int byte1 = stream.ReadByte();
        int byte2 = stream.ReadByte();
        int byte3 = stream.ReadByte();
        if (byte0 < 0 || byte1 < 0 || byte2 < 0 || byte3 < 0)
        {
            throw new EndOfStreamException();
        }

        return ((uint)byte0 << 24)
            | ((uint)byte1 << 16)
            | ((uint)byte2 << 8)
            | (uint)byte3;
    }
}
