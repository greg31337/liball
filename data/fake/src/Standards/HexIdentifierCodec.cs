using System;

namespace fake;

internal static class HexIdentifierCodec
{
    const string Alphabet = "0123456789abcdef";

    internal static void Format(ReadOnlySpan<byte> value, Span<char> destination)
    {
        int index = 0;
        while (index < value.Length)
        {
            byte current = value[index];
            destination[index * 2] = Alphabet[current >> 4];
            destination[(index * 2) + 1] = Alphabet[current & 15];
            index++;
        }
    }

    internal static bool Validate(ReadOnlySpan<char> value, int expectedLength)
    {
        if (value.Length != expectedLength)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            if (Decode(value[index]) < 0)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static void Parse(ReadOnlySpan<char> value, Span<byte> destination)
    {
        int byteLength = value.Length / 2;
        int index = 0;
        while (index < byteLength)
        {
            int high = Decode(value[index * 2]);
            int low = Decode(value[(index * 2) + 1]);
            destination[index] = (byte)((high << 4) | low);
            index++;
        }
    }

    static int Decode(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }
        if (value >= 'a' && value <= 'f')
        {
            return value - 'a' + 10;
        }
        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }
        return -1;
    }
}
