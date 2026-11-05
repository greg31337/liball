using System;
using System.Buffers.Binary;
using System.Text;

namespace avif;

internal class ByteWriter
{
    internal int Position;
    internal int Limit;
    internal AvifResultCode Code;

    internal ByteWriter(int length)
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

    internal AvifResultCode Skip(Span<byte> destination, int count)
    {
        if (CanWrite(destination, count))
        {
            destination.Slice(Position, count).Clear();
            Position += count;
        }

        return Code;
    }

    internal AvifResultCode WriteUInt8(Span<byte> destination, byte value)
    {
        if (CanWrite(destination, 1))
        {
            destination[Position] = value;
            Position++;
        }

        return Code;
    }

    internal AvifResultCode WriteInt8(Span<byte> destination, sbyte value)
    {
        return WriteUInt8(destination, IntegerConversion.ToByte((int)value));
    }

    internal AvifResultCode WriteUInt16BigEndian(Span<byte> destination, ushort value)
    {
        if (CanWrite(destination, 2))
        {
            BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(Position, 2), value);
            Position += 2;
        }

        return Code;
    }

    internal AvifResultCode WriteUInt16LittleEndian(Span<byte> destination, ushort value)
    {
        if (CanWrite(destination, 2))
        {
            BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(Position, 2), value);
            Position += 2;
        }

        return Code;
    }

    internal AvifResultCode WriteInt16BigEndian(Span<byte> destination, short value)
    {
        return WriteUInt16BigEndian(destination, IntegerConversion.ToUInt16(value));
    }

    internal AvifResultCode WriteInt16LittleEndian(Span<byte> destination, short value)
    {
        return WriteUInt16LittleEndian(destination, IntegerConversion.ToUInt16(value));
    }

    internal AvifResultCode WriteUInt24BigEndian(Span<byte> destination, uint value)
    {
        if (value > 16777215)
        {
            if (Code == AvifResultCode.Success)
            {
                Code = AvifResultCode.InvalidArgument;
            }

            return Code;
        }

        if (CanWrite(destination, 3))
        {
            destination[Position] = IntegerConversion.ToByte(value >> 16);
            destination[Position + 1] = IntegerConversion.ToByte(value >> 8);
            destination[Position + 2] = IntegerConversion.ToByte(value);
            Position += 3;
        }

        return Code;
    }

    internal AvifResultCode WriteUInt24LittleEndian(Span<byte> destination, uint value)
    {
        if (value > 16777215)
        {
            if (Code == AvifResultCode.Success)
            {
                Code = AvifResultCode.InvalidArgument;
            }

            return Code;
        }

        if (CanWrite(destination, 3))
        {
            destination[Position] = IntegerConversion.ToByte(value);
            destination[Position + 1] = IntegerConversion.ToByte(value >> 8);
            destination[Position + 2] = IntegerConversion.ToByte(value >> 16);
            Position += 3;
        }

        return Code;
    }

    internal AvifResultCode WriteInt24BigEndian(Span<byte> destination, int value)
    {
        if (value < -8388608 || value > 8388607)
        {
            if (Code == AvifResultCode.Success)
            {
                Code = AvifResultCode.InvalidArgument;
            }

            return Code;
        }

        return WriteUInt24BigEndian(destination, (uint)(value & 16777215));
    }

    internal AvifResultCode WriteInt24LittleEndian(Span<byte> destination, int value)
    {
        if (value < -8388608 || value > 8388607)
        {
            if (Code == AvifResultCode.Success)
            {
                Code = AvifResultCode.InvalidArgument;
            }

            return Code;
        }

        return WriteUInt24LittleEndian(destination, (uint)(value & 16777215));
    }

    internal AvifResultCode WriteUInt32BigEndian(Span<byte> destination, uint value)
    {
        if (CanWrite(destination, 4))
        {
            BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(Position, 4), value);
            Position += 4;
        }

        return Code;
    }

    internal AvifResultCode WriteUInt32LittleEndian(Span<byte> destination, uint value)
    {
        if (CanWrite(destination, 4))
        {
            BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(Position, 4), value);
            Position += 4;
        }

        return Code;
    }

    internal AvifResultCode WriteInt32BigEndian(Span<byte> destination, int value)
    {
        return WriteUInt32BigEndian(destination, IntegerConversion.ToUInt32(value));
    }

    internal AvifResultCode WriteInt32LittleEndian(Span<byte> destination, int value)
    {
        return WriteUInt32LittleEndian(destination, IntegerConversion.ToUInt32(value));
    }

    internal AvifResultCode WriteUInt64BigEndian(Span<byte> destination, ulong value)
    {
        if (CanWrite(destination, 8))
        {
            BinaryPrimitives.WriteUInt64BigEndian(destination.Slice(Position, 8), value);
            Position += 8;
        }

        return Code;
    }

    internal AvifResultCode WriteUInt64LittleEndian(Span<byte> destination, ulong value)
    {
        if (CanWrite(destination, 8))
        {
            BinaryPrimitives.WriteUInt64LittleEndian(destination.Slice(Position, 8), value);
            Position += 8;
        }

        return Code;
    }

    internal AvifResultCode WriteInt64BigEndian(Span<byte> destination, long value)
    {
        return WriteUInt64BigEndian(destination, IntegerConversion.ToUInt64(value));
    }

    internal AvifResultCode WriteInt64LittleEndian(Span<byte> destination, long value)
    {
        return WriteUInt64LittleEndian(destination, IntegerConversion.ToUInt64(value));
    }

    internal AvifResultCode WriteFixed8_8BigEndian(Span<byte> destination, double value)
    {
        if (!TryPrepareFixed(value, 256.0, short.MinValue, short.MaxValue))
        {
            return Code;
        }

        short encoded = (short)System.Math.Round(value * 256.0, System.MidpointRounding.AwayFromZero);
        return WriteInt16BigEndian(destination, encoded);
    }

    internal AvifResultCode WriteFixed16_16BigEndian(Span<byte> destination, double value)
    {
        if (!TryPrepareFixed(value, 65536.0, int.MinValue, int.MaxValue))
        {
            return Code;
        }

        int encoded = (int)System.Math.Round(value * 65536.0, System.MidpointRounding.AwayFromZero);
        return WriteInt32BigEndian(destination, encoded);
    }

    internal AvifResultCode WriteFixed2_30BigEndian(Span<byte> destination, double value)
    {
        if (!TryPrepareFixed(value, 1073741824.0, int.MinValue, int.MaxValue))
        {
            return Code;
        }

        int encoded = (int)System.Math.Round(value * 1073741824.0, System.MidpointRounding.AwayFromZero);
        return WriteInt32BigEndian(destination, encoded);
    }

    internal AvifResultCode WriteFourCc(Span<byte> destination, string value)
    {
        if (Code != AvifResultCode.Success)
        {
            return Code;
        }

        if (value == null || value.Length != 4)
        {
            Code = AvifResultCode.InvalidArgument;
            return Code;
        }

        int index = 0;
        while (index < 4)
        {
            if (value[index] > 255)
            {
                Code = AvifResultCode.InvalidArgument;
                return Code;
            }

            index++;
        }

        if (CanWrite(destination, 4))
        {
            destination[Position] = (byte)value[0];
            destination[Position + 1] = (byte)value[1];
            destination[Position + 2] = (byte)value[2];
            destination[Position + 3] = (byte)value[3];
            Position += 4;
        }

        return Code;
    }

    internal AvifResultCode WriteUuid(Span<byte> destination, ReadOnlySpan<byte> value)
    {
        if (Code != AvifResultCode.Success)
        {
            return Code;
        }

        if (value.Length != 16)
        {
            Code = AvifResultCode.InvalidArgument;
            return Code;
        }

        if (CanWrite(destination, 16))
        {
            value.CopyTo(destination.Slice(Position, 16));
            Position += 16;
        }

        return Code;
    }

    internal AvifResultCode WriteUnsignedFraction32BigEndian(
        Span<byte> destination,
        uint numerator,
        uint denominator)
    {
        if (denominator == 0)
        {
            if (Code == AvifResultCode.Success)
            {
                Code = AvifResultCode.InvalidArgument;
            }

            return Code;
        }

        if (CanWrite(destination, 8))
        {
            BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(Position, 4), numerator);
            BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(Position + 4, 4), denominator);
            Position += 8;
        }

        return Code;
    }

    internal AvifResultCode WriteSignedFraction32BigEndian(
        Span<byte> destination,
        int numerator,
        int denominator)
    {
        if (denominator == 0)
        {
            if (Code == AvifResultCode.Success)
            {
                Code = AvifResultCode.InvalidArgument;
            }

            return Code;
        }

        if (CanWrite(destination, 8))
        {
            BinaryPrimitives.WriteInt32BigEndian(destination.Slice(Position, 4), numerator);
            BinaryPrimitives.WriteInt32BigEndian(destination.Slice(Position + 4, 4), denominator);
            Position += 8;
        }

        return Code;
    }

    internal AvifResultCode WriteUnsignedFraction32LittleEndian(
        Span<byte> destination,
        uint numerator,
        uint denominator)
    {
        if (denominator == 0)
        {
            if (Code == AvifResultCode.Success)
            {
                Code = AvifResultCode.InvalidArgument;
            }

            return Code;
        }

        if (CanWrite(destination, 8))
        {
            BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(Position, 4), numerator);
            BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(Position + 4, 4), denominator);
            Position += 8;
        }

        return Code;
    }

    internal AvifResultCode WriteSignedFraction32LittleEndian(
        Span<byte> destination,
        int numerator,
        int denominator)
    {
        if (denominator == 0)
        {
            if (Code == AvifResultCode.Success)
            {
                Code = AvifResultCode.InvalidArgument;
            }

            return Code;
        }

        if (CanWrite(destination, 8))
        {
            BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(Position, 4), numerator);
            BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(Position + 4, 4), denominator);
            Position += 8;
        }

        return Code;
    }

    internal AvifResultCode WriteUtf8(Span<byte> destination, string value)
    {
        if (!PrepareText(value))
        {
            return Code;
        }

        int byteCount = Encoding.UTF8.GetByteCount(value);
        if (CanWrite(destination, byteCount))
        {
            Encoding.UTF8.GetBytes(value.AsSpan(), destination.Slice(Position, byteCount));
            Position += byteCount;
        }

        return Code;
    }

    internal AvifResultCode WriteNullTerminatedUtf8(Span<byte> destination, string value)
    {
        if (!PrepareText(value))
        {
            return Code;
        }

        int byteCount = Encoding.UTF8.GetByteCount(value);
        if (byteCount == int.MaxValue)
        {
            Code = AvifResultCode.LimitExceeded;
            return Code;
        }

        int totalLength = byteCount + 1;
        if (CanWrite(destination, totalLength))
        {
            Encoding.UTF8.GetBytes(value.AsSpan(), destination.Slice(Position, byteCount));
            destination[Position + byteCount] = 0;
            Position += totalLength;
        }

        return Code;
    }

    internal AvifResultCode WriteLengthPrefixedUtf8(
        Span<byte> destination,
        string value,
        int prefixByteCount)
    {
        if (prefixByteCount != 1 && prefixByteCount != 2 && prefixByteCount != 4)
        {
            if (Code == AvifResultCode.Success)
            {
                Code = AvifResultCode.InvalidArgument;
            }

            return Code;
        }

        if (!PrepareText(value))
        {
            return Code;
        }

        int byteCount = Encoding.UTF8.GetByteCount(value);
        if (prefixByteCount == 1 && byteCount > byte.MaxValue)
        {
            Code = AvifResultCode.LimitExceeded;
            return Code;
        }

        if (prefixByteCount == 2 && byteCount > ushort.MaxValue)
        {
            Code = AvifResultCode.LimitExceeded;
            return Code;
        }

        if (byteCount > int.MaxValue - prefixByteCount)
        {
            Code = AvifResultCode.LimitExceeded;
            return Code;
        }

        int totalLength = prefixByteCount + byteCount;
        if (!CanWrite(destination, totalLength))
        {
            return Code;
        }

        if (prefixByteCount == 1)
        {
            destination[Position] = (byte)byteCount;
        }
        else if (prefixByteCount == 2)
        {
            BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(Position, 2), (ushort)byteCount);
        }
        else
        {
            BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(Position, 4), (uint)byteCount);
        }

        Encoding.UTF8.GetBytes(
            value.AsSpan(),
            destination.Slice(Position + prefixByteCount, byteCount));
        Position += totalLength;
        return Code;
    }

    internal bool CanWrite(Span<byte> destination, int count)
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
            Code = AvifResultCode.BufferTooSmall;
            return false;
        }

        if (Position > destination.Length || count > destination.Length - Position)
        {
            Code = AvifResultCode.BufferTooSmall;
            return false;
        }

        return true;
    }

    internal bool PrepareText(string value)
    {
        if (Code != AvifResultCode.Success)
        {
            return false;
        }

        if (!Utf8Utility.IsValidUtf16(value))
        {
            Code = AvifResultCode.InvalidArgument;
            return false;
        }

        return true;
    }

    internal bool TryPrepareFixed(double value, double scale, double minimum, double maximum)
    {
        if (Code != AvifResultCode.Success)
        {
            return false;
        }

        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            Code = AvifResultCode.InvalidArgument;
            return false;
        }

        double encoded = System.Math.Round(value * scale, System.MidpointRounding.AwayFromZero);
        if (encoded < minimum || encoded > maximum)
        {
            Code = AvifResultCode.InvalidArgument;
            return false;
        }

        return true;
    }
}
