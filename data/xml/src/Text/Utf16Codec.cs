using System;
using System.Text;

namespace xml;

internal enum Utf16ByteOrder
{
    BigEndian = 0,
    LittleEndian = 1
}

internal class Utf16Scalar
{
    internal int Value;
    internal int ByteCount;
    internal bool EndOfInput;

    internal Utf16Scalar(int value, int byteCount, bool endOfInput)
    {
        Value = value;
        ByteCount = byteCount;
        EndOfInput = endOfInput;
    }
}

internal class Utf16Codec
{
    internal IByteSource Source;
    internal Utf16ByteOrder ByteOrder;
    internal byte[] SingleByte;

    internal Utf16Codec(IByteSource source, Utf16ByteOrder fallbackByteOrder)
    {
        Guard.NotNull(source, "source");
        if (fallbackByteOrder != Utf16ByteOrder.BigEndian && fallbackByteOrder != Utf16ByteOrder.LittleEndian)
        {
            throw new ArgumentOutOfRangeException("fallbackByteOrder");
        }

        Source = source;
        SingleByte = new byte[1];
        ByteOrderMarkKind byteOrderMark = source.InspectByteOrderMark();
        if (byteOrderMark == ByteOrderMarkKind.Utf16BigEndian)
        {
            ByteOrder = Utf16ByteOrder.BigEndian;
        }
        else if (byteOrderMark == ByteOrderMarkKind.Utf16LittleEndian)
        {
            ByteOrder = Utf16ByteOrder.LittleEndian;
        }
        else if (byteOrderMark == ByteOrderMarkKind.None)
        {
            ByteOrder = fallbackByteOrder;
        }
        else
        {
            throw CreateUtf16Error("Unexpected byte-order mark for UTF-16 input", 0);
        }
    }

    internal Utf16Scalar DecodeUtf16Scalar()
    {
        long start = Source.SourceBytePosition();
        int first = ReadUtf16CodeUnit();
        if (first < 0)
        {
            return new Utf16Scalar(0, 0, true);
        }

        if (first >= 0xD800 && first <= 0xDBFF)
        {
            long secondOffset = Source.SourceBytePosition();
            int second = ReadUtf16CodeUnit();
            if (second < 0)
            {
                throw CreateUtf16Error("Truncated UTF-16 surrogate pair", secondOffset);
            }

            if (second < 0xDC00 || second > 0xDFFF)
            {
                throw CreateUtf16Error("Invalid UTF-16 low surrogate", secondOffset);
            }

            int scalar = 0x10000 + (first - 0xD800) * 0x400 + second - 0xDC00;
            return new Utf16Scalar(scalar, 4, false);
        }

        if (first >= 0xDC00 && first <= 0xDFFF)
        {
            throw CreateUtf16Error("Unpaired UTF-16 low surrogate", start);
        }

        return new Utf16Scalar(first, 2, false);
    }

    internal string DecodeUtf16Text()
    {
        ValueBuffer result = new ValueBuffer(64);
        while (true)
        {
            Utf16Scalar scalar = DecodeUtf16Scalar();
            if (scalar.EndOfInput)
            {
                break;
            }

            if (scalar.Value <= 0xFFFF)
            {
                result.AppendCharacter((char)scalar.Value);
            }
            else
            {
                int adjusted = scalar.Value - 0x10000;
                result.AppendCharacter((char)(0xD800 + adjusted / 0x400));
                result.AppendCharacter((char)(0xDC00 + adjusted % 0x400));
            }
        }

        return result.GetText();
    }

    internal int ReadUtf16CodeUnit()
    {
        long offset = Source.SourceBytePosition();
        int first = ReadUtf16Byte();
        if (first < 0)
        {
            return -1;
        }

        int second = ReadUtf16Byte();
        if (second < 0)
        {
            throw CreateUtf16Error("Odd number of bytes in UTF-16 input", offset);
        }

        if (ByteOrder == Utf16ByteOrder.BigEndian)
        {
            return first * 0x100 + second;
        }

        return second * 0x100 + first;
    }

    internal int ReadUtf16Byte()
    {
        int count = Source.ReadSourceBytes(SingleByte);
        return count == 0 ? -1 : SingleByte[0];
    }

    internal DecoderFallbackException CreateUtf16Error(string message, long offset)
    {
        return new DecoderFallbackException(message + " at byte offset " + offset + ".");
    }
}
