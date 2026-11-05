using System;

using System.Buffers.Binary;

namespace random;

internal class BinaryCodec
{
    internal static byte ReadByte(byte[] source, int offset)
    {
        Guard.NotNull(source, "source");
        return ReadByte(source.AsSpan(), offset);
    }

    internal static byte ReadByte(ReadOnlySpan<byte> source, int offset)
    {
        EnsureAvailable(source.Length, offset, 1, "source");
        return source[offset];
    }

    internal static ushort ReadUInt16(byte[] source, int offset)
    {
        Guard.NotNull(source, "source");
        return ReadUInt16(source.AsSpan(), offset);
    }

    internal static ushort ReadUInt16(ReadOnlySpan<byte> source, int offset)
    {
        EnsureAvailable(source.Length, offset, 2, "source");
        return BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(offset, 2));
    }

    internal static uint ReadUInt32(byte[] source, int offset)
    {
        Guard.NotNull(source, "source");
        return ReadUInt32(source.AsSpan(), offset);
    }

    internal static uint ReadUInt32(ReadOnlySpan<byte> source, int offset)
    {
        EnsureAvailable(source.Length, offset, 4, "source");
        return BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(offset, 4));
    }

    internal static ulong ReadUInt64(byte[] source, int offset)
    {
        Guard.NotNull(source, "source");
        return ReadUInt64(source.AsSpan(), offset);
    }

    internal static ulong ReadUInt64(ReadOnlySpan<byte> source, int offset)
    {
        EnsureAvailable(source.Length, offset, 8, "source");
        return BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(offset, 8));
    }

    internal static UInt128 ReadUInt128(byte[] source, int offset)
    {
        Guard.NotNull(source, "source");
        return ReadUInt128(source.AsSpan(), offset);
    }

    internal static UInt128 ReadUInt128(ReadOnlySpan<byte> source, int offset)
    {
        EnsureAvailable(source.Length, offset, 16, "source");
        return BinaryPrimitives.ReadUInt128LittleEndian(source.Slice(offset, 16));
    }

    internal static byte[] ReadBytes(byte[] source, int offset, int length)
    {
        Guard.NotNull(source, "source");
        return ReadBytes(source.AsSpan(), offset, length);
    }

    internal static byte[] ReadBytes(ReadOnlySpan<byte> source, int offset, int length)
    {
        EnsureAvailable(source.Length, offset, length, "source");
        return source.Slice(offset, length).ToArray();
    }

    internal static int WriteByte(byte[] destination, int offset, byte value)
    {
        Guard.NotNull(destination, "destination");
        return WriteByte(destination.AsSpan(), offset, value);
    }

    internal static int WriteByte(Span<byte> destination, int offset, byte value)
    {
        EnsureAvailable(destination.Length, offset, 1, "destination");
        destination[offset] = value;
        return offset + 1;
    }

    internal static int WriteUInt16(byte[] destination, int offset, ushort value)
    {
        Guard.NotNull(destination, "destination");
        return WriteUInt16(destination.AsSpan(), offset, value);
    }

    internal static int WriteUInt16(Span<byte> destination, int offset, ushort value)
    {
        EnsureAvailable(destination.Length, offset, 2, "destination");
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(offset, 2), value);
        return offset + 2;
    }

    internal static int WriteUInt32(byte[] destination, int offset, uint value)
    {
        Guard.NotNull(destination, "destination");
        return WriteUInt32(destination.AsSpan(), offset, value);
    }

    internal static int WriteUInt32(Span<byte> destination, int offset, uint value)
    {
        EnsureAvailable(destination.Length, offset, 4, "destination");
        BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(offset, 4), value);
        return offset + 4;
    }

    internal static int WriteUInt64(byte[] destination, int offset, ulong value)
    {
        Guard.NotNull(destination, "destination");
        return WriteUInt64(destination.AsSpan(), offset, value);
    }

    internal static int WriteUInt64(Span<byte> destination, int offset, ulong value)
    {
        EnsureAvailable(destination.Length, offset, 8, "destination");
        BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice(offset, 8), value);
        return offset + 8;
    }

    internal static int WriteUInt128(byte[] destination, int offset, UInt128 value)
    {
        Guard.NotNull(destination, "destination");
        return WriteUInt128(destination.AsSpan(), offset, value);
    }

    internal static int WriteUInt128(Span<byte> destination, int offset, UInt128 value)
    {
        EnsureAvailable(destination.Length, offset, 16, "destination");
        BinaryPrimitives.WriteUInt128LittleEndian(destination.Slice(offset, 16), value);
        return offset + 16;
    }

    internal static int WriteBytes(byte[] destination, int offset, byte[] source)
    {
        Guard.NotNull(destination, "destination");
        Guard.NotNull(source, "source");
        return WriteBytes(destination.AsSpan(), offset, source.AsSpan());
    }

    internal static int WriteBytes(Span<byte> destination, int offset, ReadOnlySpan<byte> source)
    {
        EnsureAvailable(destination.Length, offset, source.Length, "destination");
        source.CopyTo(destination.Slice(offset, source.Length));
        return offset + source.Length;
    }

    internal static void EnsureAvailable(int bufferLength, int offset, int length, string bufferArgumentName)
    {
        if (offset < 0)
        {
            throw RandomArgumentException.Create("offset", "The byte offset must not be negative.");
        }

        if (length < 0)
        {
            throw RandomArgumentException.Create("length", "The byte length must not be negative.");
        }

        if (offset > bufferLength || length > bufferLength - offset)
        {
            throw RandomArgumentException.Create(bufferArgumentName, "The buffer does not contain the requested byte range.");
        }
    }
}
