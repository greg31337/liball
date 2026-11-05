using System;
using System.Net;
using System.Text;

namespace markdown;

internal class MarkdownEntity
{
    internal static string Decode(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (text.IndexOf('&') < 0)
        {
            return text;
        }

        StringBuilder builder = new StringBuilder(text.Length);
        int index = 0;

        while (index < text.Length)
        {
            MarkdownEntityDecodeResult result = DecodeAt(text, index);
            if (result.Success)
            {
                builder.Append(result.Text);
                index = index + result.Length;
            }
            else
            {
                builder.Append(text[index]);
                index = index + 1;
            }
        }

        return builder.ToString();
    }

    internal static MarkdownEntityDecodeResult DecodeAt(string text, int index)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (index < 0 || index > text.Length)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        if (index == text.Length || text[index] != '&')
        {
            return MarkdownEntityDecodeResult.NoMatch();
        }

        if (text.Length - index > 2 && text[index + 1] == '#')
        {
            return DecodeNumericAt(text, index);
        }

        return DecodeNamedAt(text, index);
    }

    internal static MarkdownEntityDecodeResult DecodeNumericAt(string text, int index)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (index < 0 || index > text.Length)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        if (text.Length - index < 3 || text[index] != '&' || text[index + 1] != '#')
        {
            return MarkdownEntityDecodeResult.NoMatch();
        }

        int numberBase = 10;
        int digitIndex = index + 2;
        if (digitIndex < text.Length && (text[digitIndex] == 'x' || text[digitIndex] == 'X'))
        {
            numberBase = 16;
            digitIndex = digitIndex + 1;
        }

        int firstDigit = digitIndex;
        long codePoint = 0;

        while (digitIndex < text.Length && IsDigitForBase(text[digitIndex], numberBase))
        {
            int digitValue = GetDigitValue(text[digitIndex]);
            if (codePoint <= 1114111)
            {
                if (codePoint > (1114111 - digitValue) / numberBase)
                {
                    codePoint = 1114112;
                }
                else
                {
                    codePoint = codePoint * numberBase + digitValue;
                }
            }

            digitIndex = digitIndex + 1;
        }

        int digitCount = digitIndex - firstDigit;
        if (numberBase == 10 && digitCount > 7 || numberBase == 16 && digitCount > 6)
        {
            return MarkdownEntityDecodeResult.NoMatch();
        }

        if (digitIndex == firstDigit)
        {
            return MarkdownEntityDecodeResult.NoMatch();
        }

        if (digitIndex >= text.Length || text[digitIndex] != ';')
        {
            return MarkdownEntityDecodeResult.NoMatch();
        }

        string decoded = GetStringForCodePoint(codePoint);
        return MarkdownEntityDecodeResult.Create(true, decoded, digitIndex - index + 1);
    }

    internal static MarkdownEntityDecodeResult DecodeNamedAt(string text, int index)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (index < 0 || index > text.Length)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        if (text.Length - index < 2 || text[index] != '&')
        {
            return MarkdownEntityDecodeResult.NoMatch();
        }

        int nameIndex = index + 1;
        while (nameIndex < text.Length && IsAsciiLetterOrDigit(text[nameIndex]))
        {
            nameIndex = nameIndex + 1;
        }

        if (nameIndex == index + 1)
        {
            return MarkdownEntityDecodeResult.NoMatch();
        }

        if (nameIndex >= text.Length || text[nameIndex] != ';')
        {
            return MarkdownEntityDecodeResult.NoMatch();
        }

        string candidate = text.Substring(index, nameIndex - index + 1);
        string decoded = WebUtility.HtmlDecode(candidate);
        if (decoded == candidate)
        {
            decoded = DecodeAdditionalNamedEntity(candidate);
            if (decoded.Length == 0)
            {
                return MarkdownEntityDecodeResult.NoMatch();
            }
        }

        return MarkdownEntityDecodeResult.Create(true, decoded, candidate.Length);
    }

    internal static string DecodeAdditionalNamedEntity(string candidate)
    {
        switch (candidate)
        {
            case "&Dcaron;":
                return "\u010e";
            case "&HilbertSpace;":
                return "\u210b";
            case "&DifferentialD;":
                return "\u2146";
            case "&ClockwiseContourIntegral;":
                return "\u2232";
            case "&ngE;":
                return "\u2267\u0338";
            default:
                return "";
        }
    }

    internal static bool IsAsciiLetterOrDigit(char character)
    {
        return character >= 'a' && character <= 'z'
            || character >= 'A' && character <= 'Z'
            || character >= '0' && character <= '9';
    }

    internal static bool IsDigitForBase(char character, int numberBase)
    {
        if (numberBase == 10)
        {
            return character >= '0' && character <= '9';
        }

        if (numberBase == 16)
        {
            return character >= '0' && character <= '9'
                || character >= 'a' && character <= 'f'
                || character >= 'A' && character <= 'F';
        }

        return false;
    }

    internal static int GetDigitValue(char character)
    {
        if (character >= '0' && character <= '9')
        {
            return character - '0';
        }

        if (character >= 'a' && character <= 'f')
        {
            return character - 'a' + 10;
        }

        if (character >= 'A' && character <= 'F')
        {
            return character - 'A' + 10;
        }

        return -1;
    }

    internal static string GetStringForCodePoint(long codePoint)
    {
        if (!IsValidCodePoint(codePoint))
        {
            return MarkdownInputNormalizer.NullReplacementCharacter.ToString();
        }

        return char.ConvertFromUtf32((int)codePoint);
    }

    internal static bool IsValidCodePoint(long codePoint)
    {
        if (codePoint <= 0 || codePoint > 1114111)
        {
            return false;
        }

        if (codePoint >= 55296 && codePoint <= 57343)
        {
            return false;
        }

        return true;
    }
}
