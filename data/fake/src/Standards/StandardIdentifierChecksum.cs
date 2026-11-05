using System;

namespace fake;

internal enum StandardIdentifierChecksumKind
{
    Isbn10 = 0,
    Isbn13 = 1,
    Issn = 2,
    Ean8 = 3,
    UpcA = 4,
    Gtin = 5
}

internal static class StandardIdentifierChecks
{
    static readonly int[] Isbn10Weights = new int[] { 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    static readonly int[] IssnWeights = new int[] { 2, 3, 4, 5, 6, 7, 8 };
    static readonly int[] GtinWeights = new int[] { 3, 1 };

    internal static int Calculate(
        StandardIdentifierChecksumKind kind,
        ReadOnlySpan<char> value,
        int maximumCount)
    {
        Guard.Count(value.Length, maximumCount, "value");
        if (!PayloadLength(kind, value.Length))
        {
            throw new ArgumentException(PayloadMessage(kind), "value");
        }
        ValidateDigits(value);
        if (kind == StandardIdentifierChecksumKind.Isbn13 && !IsbnPrefix(value))
        {
            throw new ArgumentException("value must begin with the ISBN prefix 978 or 979.", "value");
        }
        if (kind == StandardIdentifierChecksumKind.Isbn10)
        {
            return ChecksumAlgorithms.Modulo(value, Isbn10Weights.AsSpan(), 11, maximumCount);
        }
        if (kind == StandardIdentifierChecksumKind.Issn)
        {
            return ChecksumAlgorithms.Modulo(value, IssnWeights.AsSpan(), 11, maximumCount);
        }
        return ChecksumAlgorithms.Modulo(value, GtinWeights.AsSpan(), 10, maximumCount);
    }

    internal static bool Validate(
        StandardIdentifierChecksumKind kind,
        ReadOnlySpan<char> value,
        int maximumCount)
    {
        if (!CompleteLength(kind, value.Length))
        {
            return false;
        }
        Guard.Count(value.Length - 1, maximumCount, "value");
        ReadOnlySpan<char> payload = value.Slice(0, value.Length - 1);
        if (!Digits(payload))
        {
            return false;
        }
        if (kind == StandardIdentifierChecksumKind.Isbn13 && !IsbnPrefix(payload))
        {
            return false;
        }
        int check = CheckValue(kind, value[value.Length - 1]);
        if (check < 0)
        {
            return false;
        }
        return Calculate(kind, payload, maximumCount) == check;
    }

    static int CheckValue(StandardIdentifierChecksumKind kind, char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }
        bool permitsX = kind == StandardIdentifierChecksumKind.Isbn10;
        permitsX = permitsX || kind == StandardIdentifierChecksumKind.Issn;
        if (permitsX && value == 'X')
        {
            return 10;
        }
        return -1;
    }

    static bool PayloadLength(StandardIdentifierChecksumKind kind, int length)
    {
        if (kind == StandardIdentifierChecksumKind.Isbn10)
        {
            return length == 9;
        }
        if (kind == StandardIdentifierChecksumKind.Isbn13)
        {
            return length == 12;
        }
        if (kind == StandardIdentifierChecksumKind.Issn || kind == StandardIdentifierChecksumKind.Ean8)
        {
            return length == 7;
        }
        if (kind == StandardIdentifierChecksumKind.UpcA)
        {
            return length == 11;
        }
        return Gs1TradeItemIdentifier.PayloadLength(length);
    }

    static bool CompleteLength(StandardIdentifierChecksumKind kind, int length)
    {
        if (kind == StandardIdentifierChecksumKind.Isbn10)
        {
            return length == 10;
        }
        if (kind == StandardIdentifierChecksumKind.Isbn13)
        {
            return length == 13;
        }
        if (kind == StandardIdentifierChecksumKind.Issn || kind == StandardIdentifierChecksumKind.Ean8)
        {
            return length == 8;
        }
        if (kind == StandardIdentifierChecksumKind.UpcA)
        {
            return length == 12;
        }
        return Gs1TradeItemIdentifier.CompleteLength(length);
    }

    static string PayloadMessage(StandardIdentifierChecksumKind kind)
    {
        if (kind == StandardIdentifierChecksumKind.Isbn10)
        {
            return "value must contain the first 9 digits of an ISBN-10.";
        }
        if (kind == StandardIdentifierChecksumKind.Isbn13)
        {
            return "value must contain the first 12 digits of an ISBN-13.";
        }
        if (kind == StandardIdentifierChecksumKind.Issn)
        {
            return "value must contain the first 7 digits of an ISSN.";
        }
        if (kind == StandardIdentifierChecksumKind.Ean8)
        {
            return "value must contain the first 7 digits of an EAN-8.";
        }
        if (kind == StandardIdentifierChecksumKind.UpcA)
        {
            return "value must contain the first 11 digits of a UPC-A.";
        }
        return "value must contain 7, 11, 12, or 13 GTIN payload digits.";
    }

    static bool IsbnPrefix(ReadOnlySpan<char> value)
    {
        if (value.Length < 3)
        {
            return false;
        }
        bool prefix978 = value[0] == '9' && value[1] == '7' && value[2] == '8';
        bool prefix979 = value[0] == '9' && value[1] == '7' && value[2] == '9';
        return prefix978 || prefix979;
    }

    static void ValidateDigits(ReadOnlySpan<char> value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                throw new ArgumentException($"value must contain ASCII decimal digits only; invalid character at index {index}.", "value");
            }
            index = index + 1;
        }
    }

    static bool Digits(ReadOnlySpan<char> value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }
}
