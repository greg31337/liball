using System.Text;

namespace xml;

internal class Utf8Scalar
{
    internal int Value;
    internal int ByteCount;
    internal bool EndOfInput;

    internal Utf8Scalar(int value, int byteCount, bool endOfInput)
    {
        Value = value;
        ByteCount = byteCount;
        EndOfInput = endOfInput;
    }
}

internal class Utf8Codec
{
    internal IByteSource Source;
    internal byte[] SingleByte;

    internal Utf8Codec(IByteSource source)
    {
        Guard.NotNull(source, "source");
        Source = source;
        SingleByte = new byte[1];
    }

    internal Utf8Scalar DecodeUtf8Scalar()
    {
        long start = Source.SourceBytePosition();
        int first = ReadUtf8Byte();
        if (first < 0)
        {
            return new Utf8Scalar(0, 0, true);
        }

        if (first <= 0x7F)
        {
            return new Utf8Scalar(first, 1, false);
        }

        if (first >= 0xC2 && first <= 0xDF)
        {
            int second = ReadUtf8Continuation();
            int scalar = (first - 0xC0) * 0x40 + second - 0x80;
            return new Utf8Scalar(scalar, 2, false);
        }

        if (first >= 0xE0 && first <= 0xEF)
        {
            int second = ReadUtf8Continuation();
            if (first == 0xE0 && second < 0xA0)
            {
                throw CreateUtf8Error("Overlong UTF-8 sequence", start);
            }

            if (first == 0xED && second > 0x9F)
            {
                throw CreateUtf8Error("UTF-8 sequence encodes a surrogate", start);
            }

            int third = ReadUtf8Continuation();
            int scalar = (first - 0xE0) * 0x1000 + (second - 0x80) * 0x40 + third - 0x80;
            return new Utf8Scalar(scalar, 3, false);
        }

        if (first >= 0xF0 && first <= 0xF4)
        {
            int second = ReadUtf8Continuation();
            if (first == 0xF0 && second < 0x90)
            {
                throw CreateUtf8Error("Overlong UTF-8 sequence", start);
            }

            if (first == 0xF4 && second > 0x8F)
            {
                throw CreateUtf8Error("UTF-8 sequence exceeds U+10FFFF", start);
            }

            int third = ReadUtf8Continuation();
            int fourth = ReadUtf8Continuation();
            int scalar = (first - 0xF0) * 0x40000 + (second - 0x80) * 0x1000 + (third - 0x80) * 0x40 + fourth - 0x80;
            return new Utf8Scalar(scalar, 4, false);
        }

        throw CreateUtf8Error("Invalid UTF-8 leading byte", start);
    }

    internal string DecodeUtf8Text()
    {
        ValueBuffer result = new ValueBuffer(64);
        while (true)
        {
            Utf8Scalar scalar = DecodeUtf8Scalar();
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

    internal int ReadUtf8Byte()
    {
        int count = Source.ReadSourceBytes(SingleByte);
        return count == 0 ? -1 : SingleByte[0];
    }

    internal int ReadUtf8Continuation()
    {
        long offset = Source.SourceBytePosition();
        int value = ReadUtf8Byte();
        if (value < 0)
        {
            throw CreateUtf8Error("Truncated UTF-8 sequence", offset);
        }

        if (value < 0x80 || value > 0xBF)
        {
            throw CreateUtf8Error("Invalid UTF-8 continuation byte", offset);
        }

        return value;
    }

    internal DecoderFallbackException CreateUtf8Error(string message, long offset)
    {
        return new DecoderFallbackException(message + " at byte offset " + offset + ".");
    }
}
