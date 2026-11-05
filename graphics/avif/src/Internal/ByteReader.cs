using System;
using System.Buffers.Binary;
using System.Text;

namespace avif;

internal class ByteReader
{
    internal int Position;
    internal int Limit;
    internal AvifResultCode Code;

    internal ByteReader(int length)
    {
        Position = 0;
        if (length < 0)
        {
            Limit = 0;
            Code = AvifResultCode.InvalidArgument;
        }
        else
        {
            Limit = length;
            Code = AvifResultCode.Success;
        }
    }

    internal int Remaining()
    {
        return Limit - Position;
    }

    internal AvifResultCode Skip(ReadOnlySpan<byte> source, int count)
    {
        if (CanRead(source, count))
        {
            Position += count;
        }

        return Code;
    }

    internal byte ReadUInt8(ReadOnlySpan<byte> source)
    {
        if (!CanRead(source, 1))
        {
            return 0;
        }

        byte value = source[Position];
        Position++;
        return value;
    }

    internal sbyte ReadInt8(ReadOnlySpan<byte> source)
    {
        return IntegerConversion.ToInt8(ReadUInt8(source));
    }

    internal ushort ReadUInt16BigEndian(ReadOnlySpan<byte> source)
    {
        if (!CanRead(source, 2))
        {
            return 0;
        }

        ushort value = BinaryPrimitives.ReadUInt16BigEndian(source.Slice(Position, 2));
        Position += 2;
        return value;
    }

    internal ushort ReadUInt16LittleEndian(ReadOnlySpan<byte> source)
    {
        if (!CanRead(source, 2))
        {
            return 0;
        }

        ushort value = BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(Position, 2));
        Position += 2;
        return value;
    }

    internal short ReadInt16BigEndian(ReadOnlySpan<byte> source)
    {
        return IntegerConversion.ToInt16(ReadUInt16BigEndian(source));
    }

    internal short ReadInt16LittleEndian(ReadOnlySpan<byte> source)
    {
        return IntegerConversion.ToInt16(ReadUInt16LittleEndian(source));
    }

    internal uint ReadUInt24BigEndian(ReadOnlySpan<byte> source)
    {
        if (!CanRead(source, 3))
        {
            return 0;
        }

        uint value = (uint)(source[Position] << 16)
            | (uint)(source[Position + 1] << 8)
            | source[Position + 2];
        Position += 3;
        return value;
    }

    internal uint ReadUInt24LittleEndian(ReadOnlySpan<byte> source)
    {
        if (!CanRead(source, 3))
        {
            return 0;
        }

        uint value = source[Position]
            | (uint)(source[Position + 1] << 8)
            | (uint)(source[Position + 2] << 16);
        Position += 3;
        return value;
    }

    internal int ReadInt24BigEndian(ReadOnlySpan<byte> source)
    {
        uint value = ReadUInt24BigEndian(source);
        if ((value & 8388608) != 0)
        {
            return (int)value - 16777216;
        }

        return (int)value;
    }

    internal int ReadInt24LittleEndian(ReadOnlySpan<byte> source)
    {
        uint value = ReadUInt24LittleEndian(source);
        if ((value & 8388608) != 0)
        {
            return (int)value - 16777216;
        }

        return (int)value;
    }

    internal uint ReadUInt32BigEndian(ReadOnlySpan<byte> source)
    {
        if (!CanRead(source, 4))
        {
            return 0;
        }

        uint value = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(Position, 4));
        Position += 4;
        return value;
    }

    internal uint ReadUInt32LittleEndian(ReadOnlySpan<byte> source)
    {
        if (!CanRead(source, 4))
        {
            return 0;
        }

        uint value = BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(Position, 4));
        Position += 4;
        return value;
    }

    internal int ReadInt32BigEndian(ReadOnlySpan<byte> source)
    {
        return IntegerConversion.ToInt32(ReadUInt32BigEndian(source));
    }

    internal int ReadInt32LittleEndian(ReadOnlySpan<byte> source)
    {
        return IntegerConversion.ToInt32(ReadUInt32LittleEndian(source));
    }

    internal ulong ReadUInt64BigEndian(ReadOnlySpan<byte> source)
    {
        if (!CanRead(source, 8))
        {
            return 0;
        }

        ulong value = BinaryPrimitives.ReadUInt64BigEndian(source.Slice(Position, 8));
        Position += 8;
        return value;
    }

    internal ulong ReadUInt64LittleEndian(ReadOnlySpan<byte> source)
    {
        if (!CanRead(source, 8))
        {
            return 0;
        }

        ulong value = BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(Position, 8));
        Position += 8;
        return value;
    }

    internal long ReadInt64BigEndian(ReadOnlySpan<byte> source)
    {
        return IntegerConversion.ToInt64(ReadUInt64BigEndian(source));
    }

    internal long ReadInt64LittleEndian(ReadOnlySpan<byte> source)
    {
        return IntegerConversion.ToInt64(ReadUInt64LittleEndian(source));
    }

    internal double ReadFixed8_8BigEndian(ReadOnlySpan<byte> source)
    {
        return ReadInt16BigEndian(source) / 256.0;
    }

    internal double ReadFixed16_16BigEndian(ReadOnlySpan<byte> source)
    {
        return ReadInt32BigEndian(source) / 65536.0;
    }

    internal double ReadFixed2_30BigEndian(ReadOnlySpan<byte> source)
    {
        return ReadInt32BigEndian(source) / 1073741824.0;
    }

    internal FourCc? ReadFourCc(ReadOnlySpan<byte> source)
    {
        if (!CanRead(source, 4))
        {
            return null;
        }

        uint value = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(Position, 4));
        Position += 4;
        return new FourCc(value);
    }

    internal byte[] ReadUuid(ReadOnlySpan<byte> source)
    {
        if (!CanRead(source, 16))
        {
            return System.Array.Empty<byte>();
        }

        byte[] value = source.Slice(Position, 16).ToArray();
        Position += 16;
        return value;
    }

    internal IntegerFraction? ReadUnsignedFraction32BigEndian(ReadOnlySpan<byte> source)
    {
        if (!CanRead(source, 8))
        {
            return null;
        }

        uint numerator = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(Position, 4));
        uint denominator = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(Position + 4, 4));
        if (denominator == 0)
        {
            Code = AvifResultCode.InvalidData;
            return null;
        }

        Position += 8;
        return new IntegerFraction(numerator, denominator);
    }

    internal IntegerFraction? ReadSignedFraction32BigEndian(ReadOnlySpan<byte> source)
    {
        if (!CanRead(source, 8))
        {
            return null;
        }

        int numerator = BinaryPrimitives.ReadInt32BigEndian(source.Slice(Position, 4));
        int denominator = BinaryPrimitives.ReadInt32BigEndian(source.Slice(Position + 4, 4));
        if (denominator == 0)
        {
            Code = AvifResultCode.InvalidData;
            return null;
        }

        Position += 8;
        return new IntegerFraction(numerator, denominator);
    }

    internal IntegerFraction? ReadUnsignedFraction32LittleEndian(ReadOnlySpan<byte> source)
    {
        if (!CanRead(source, 8))
        {
            return null;
        }

        uint numerator = BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(Position, 4));
        uint denominator = BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(Position + 4, 4));
        if (denominator == 0)
        {
            Code = AvifResultCode.InvalidData;
            return null;
        }

        Position += 8;
        return new IntegerFraction(numerator, denominator);
    }

    internal IntegerFraction? ReadSignedFraction32LittleEndian(ReadOnlySpan<byte> source)
    {
        if (!CanRead(source, 8))
        {
            return null;
        }

        int numerator = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(Position, 4));
        int denominator = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(Position + 4, 4));
        if (denominator == 0)
        {
            Code = AvifResultCode.InvalidData;
            return null;
        }

        Position += 8;
        return new IntegerFraction(numerator, denominator);
    }

    internal string ReadUtf8(ReadOnlySpan<byte> source, int byteLength)
    {
        if (!CanRead(source, byteLength))
        {
            return string.Empty;
        }

        ReadOnlySpan<byte> encoded = source.Slice(Position, byteLength);
        if (!Utf8Utility.IsValid(encoded))
        {
            Code = AvifResultCode.InvalidData;
            return string.Empty;
        }

        string value = Encoding.UTF8.GetString(encoded);
        Position += byteLength;
        return value;
    }

    internal string ReadNullTerminatedUtf8(ReadOnlySpan<byte> source)
    {
        if (Code != AvifResultCode.Success)
        {
            return string.Empty;
        }

        int scanLimit = Limit;
        if (source.Length < scanLimit)
        {
            scanLimit = source.Length;
        }

        if (Position > scanLimit)
        {
            Code = AvifResultCode.TruncatedData;
            return string.Empty;
        }

        int index = Position;
        while (index < scanLimit && source[index] != 0)
        {
            index++;
        }

        if (index == scanLimit)
        {
            Code = source.Length < Limit ? AvifResultCode.TruncatedData : AvifResultCode.InvalidData;
            return string.Empty;
        }

        ReadOnlySpan<byte> encoded = source.Slice(Position, index - Position);
        if (!Utf8Utility.IsValid(encoded))
        {
            Code = AvifResultCode.InvalidData;
            return string.Empty;
        }

        string value = Encoding.UTF8.GetString(encoded);
        Position = index + 1;
        return value;
    }

    internal string ReadLengthPrefixedUtf8(ReadOnlySpan<byte> source, int prefixByteCount)
    {
        if (prefixByteCount != 1 && prefixByteCount != 2 && prefixByteCount != 4)
        {
            if (Code == AvifResultCode.Success)
            {
                Code = AvifResultCode.InvalidArgument;
            }

            return string.Empty;
        }

        if (!CanRead(source, prefixByteCount))
        {
            return string.Empty;
        }

        uint encodedLength;
        if (prefixByteCount == 1)
        {
            encodedLength = source[Position];
        }
        else if (prefixByteCount == 2)
        {
            encodedLength = BinaryPrimitives.ReadUInt16BigEndian(source.Slice(Position, 2));
        }
        else
        {
            encodedLength = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(Position, 4));
        }

        if (encodedLength > int.MaxValue - prefixByteCount)
        {
            Code = AvifResultCode.LimitExceeded;
            return string.Empty;
        }

        int length = (int)encodedLength;
        int totalLength = prefixByteCount + length;
        if (!CanRead(source, totalLength))
        {
            return string.Empty;
        }

        ReadOnlySpan<byte> encoded = source.Slice(Position + prefixByteCount, length);
        if (!Utf8Utility.IsValid(encoded))
        {
            Code = AvifResultCode.InvalidData;
            return string.Empty;
        }

        string value = Encoding.UTF8.GetString(encoded);
        Position += totalLength;
        return value;
    }

    internal bool CanRead(ReadOnlySpan<byte> source, int count)
    {
        if (Code != AvifResultCode.Success)
        {
            return false;
        }

        if (count < 0)
        {
            Code = AvifResultCode.InvalidArgument;
            return false;
        }

        if (Position > Limit || count > Limit - Position)
        {
            Code = AvifResultCode.TruncatedData;
            return false;
        }

        if (Position > source.Length || count > source.Length - Position)
        {
            Code = AvifResultCode.TruncatedData;
            return false;
        }

        return true;
    }
}
