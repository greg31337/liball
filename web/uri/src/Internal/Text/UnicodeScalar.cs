using System;

namespace uri;

internal static class UnicodeScalar
{
    internal static UnicodeScalarResult ReadAt(ReadOnlySpan<char> text, int offset)
    {
        if (offset < 0)
        {
            return new UnicodeScalarResult(false, 0, 0, 0, 0);
        }

        if (offset >= text.Length)
        {
            return new UnicodeScalarResult(false, 0, 0, text.Length, 0);
        }

        char first = text[offset];
        if (first >= '\ud800' && first <= '\udbff')
        {
            if (offset + 1 >= text.Length)
            {
                return new UnicodeScalarResult(false, 0, 0, offset, 1);
            }

            char second = text[offset + 1];
            if (second < '\udc00' || second > '\udfff')
            {
                return new UnicodeScalarResult(false, 0, 0, offset, 1);
            }

            int scalarValue = 65536 + (first - '\ud800') * 1024 + second - '\udc00';
            return new UnicodeScalarResult(true, scalarValue, 2, 0, 0);
        }

        if (first >= '\udc00' && first <= '\udfff')
        {
            return new UnicodeScalarResult(false, 0, 0, offset, 1);
        }

        return new UnicodeScalarResult(true, first, 1, 0, 0);
    }

    internal static bool IsValid(int scalarValue)
    {
        return scalarValue >= 0 &&
            scalarValue <= 1114111 &&
            (scalarValue < 55296 || scalarValue > 57343);
    }

    internal static int GetCodeUnitLength(int scalarValue)
    {
        if (!IsValid(scalarValue))
        {
            return 0;
        }

        if (scalarValue <= 65535)
        {
            return 1;
        }

        return 2;
    }
}
