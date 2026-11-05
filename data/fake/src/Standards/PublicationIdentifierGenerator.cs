using System;

namespace fake;

internal static class PublicationIdentifierGenerator
{
    internal const int DefaultDoiRegistrantDigits = 4;
    internal const int DefaultDoiSuffixLength = 8;
    const string DoiSuffixAlphabet = "0123456789abcdefghijklmnopqrstuvwxyz";

    internal static string GenerateDoi(IRandomSource random, int registrantDigits, int suffixLength, int maximumCount, int maximumOutputSize)
    {
        int length = DoiLength(registrantDigits, suffixLength, maximumCount, maximumOutputSize);
        char[] destination = new char[length];
        WriteDoiUnchecked(random, destination, registrantDigits, suffixLength);
        return new string(destination);
    }

    internal static int WriteDoi(IRandomSource random, Span<char> destination, int registrantDigits, int suffixLength, int maximumCount, int maximumOutputSize)
    {
        int length = DoiLength(registrantDigits, suffixLength, maximumCount, maximumOutputSize);
        RequireDestination(destination.Length, length);
        WriteDoiUnchecked(random, destination.Slice(0, length), registrantDigits, suffixLength);
        return length;
    }

    internal static bool ValidateDoi(ReadOnlySpan<char> value)
    {
        if (value.Length < 6 || value[0] != '1' || value[1] != '0' || value[2] != '.')
        {
            return false;
        }
        int index = 3;
        int digits = 0;
        while (index < value.Length && value[index] >= '0' && value[index] <= '9')
        {
            digits++;
            index++;
        }
        if (digits == 0 || index >= value.Length || value[index] != '/')
        {
            return false;
        }
        index++;
        if (index >= value.Length)
        {
            return false;
        }
        while (index < value.Length)
        {
            if (!DoiSuffixCharacter(value[index]))
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static string DoiUrl(ReadOnlySpan<char> value, int maximumOutputSize)
    {
        RequireDoi(value);
        int length = Guard.OutputSize((long)"https://doi.org/".Length + value.Length, maximumOutputSize, "value");
        char[] destination = new char[length];
        WriteDoiUrlUnchecked(value, destination);
        return new string(destination);
    }

    internal static int WriteDoiUrl(ReadOnlySpan<char> value, Span<char> destination, int maximumOutputSize)
    {
        RequireDoi(value);
        int length = Guard.OutputSize((long)"https://doi.org/".Length + value.Length, maximumOutputSize, "value");
        RequireDestination(destination.Length, length);
        WriteDoiUrlUnchecked(value, destination.Slice(0, length));
        return length;
    }

    internal static string GenerateIsbn10(IRandomSource random, BookIdentifierStyle style, int maximumCount, int maximumOutputSize)
    {
        int length = DisplayLength(StandardIdentifierChecksumKind.Isbn10, style);
        Guard.OutputSize(length, maximumOutputSize, "value");
        Guard.Count(9, maximumCount, "value");
        char[] compact = new char[10];
        FillDigits(random, compact, 9);
        compact[9] = CheckCharacter(StandardIdentifierChecks.Calculate(StandardIdentifierChecksumKind.Isbn10, new ReadOnlySpan<char>(compact).Slice(0, 9), maximumCount));
        char[] destination = new char[length];
        FormatUnchecked(compact, destination, StandardIdentifierChecksumKind.Isbn10, style);
        return new string(destination);
    }

    internal static int WriteIsbn10(IRandomSource random, Span<char> destination, BookIdentifierStyle style, int maximumCount, int maximumOutputSize)
    {
        int length = DisplayLength(StandardIdentifierChecksumKind.Isbn10, style);
        Guard.OutputSize(length, maximumOutputSize, "value");
        Guard.Count(9, maximumCount, "value");
        RequireDestination(destination.Length, length);
        char[] compact = new char[10];
        FillDigits(random, compact, 9);
        compact[9] = CheckCharacter(StandardIdentifierChecks.Calculate(StandardIdentifierChecksumKind.Isbn10, new ReadOnlySpan<char>(compact).Slice(0, 9), maximumCount));
        FormatUnchecked(compact, destination.Slice(0, length), StandardIdentifierChecksumKind.Isbn10, style);
        return length;
    }

    internal static string GenerateIsbn13(IRandomSource random, BookIdentifierStyle style, int maximumCount, int maximumOutputSize)
    {
        int length = DisplayLength(StandardIdentifierChecksumKind.Isbn13, style);
        Guard.OutputSize(length, maximumOutputSize, "value");
        Guard.Count(12, maximumCount, "value");
        char[] compact = new char[13];
        compact[0] = '9';
        compact[1] = '7';
        compact[2] = random.IntFromMaximumExclusive(2) == 0 ? '8' : '9';
        FillDigits(random, new Span<char>(compact).Slice(3, 9), 9);
        compact[12] = CheckCharacter(StandardIdentifierChecks.Calculate(StandardIdentifierChecksumKind.Isbn13, new ReadOnlySpan<char>(compact).Slice(0, 12), maximumCount));
        char[] destination = new char[length];
        FormatUnchecked(compact, destination, StandardIdentifierChecksumKind.Isbn13, style);
        return new string(destination);
    }

    internal static int WriteIsbn13(IRandomSource random, Span<char> destination, BookIdentifierStyle style, int maximumCount, int maximumOutputSize)
    {
        int length = DisplayLength(StandardIdentifierChecksumKind.Isbn13, style);
        Guard.OutputSize(length, maximumOutputSize, "value");
        Guard.Count(12, maximumCount, "value");
        RequireDestination(destination.Length, length);
        char[] compact = new char[13];
        compact[0] = '9';
        compact[1] = '7';
        compact[2] = random.IntFromMaximumExclusive(2) == 0 ? '8' : '9';
        FillDigits(random, new Span<char>(compact).Slice(3, 9), 9);
        compact[12] = CheckCharacter(StandardIdentifierChecks.Calculate(StandardIdentifierChecksumKind.Isbn13, new ReadOnlySpan<char>(compact).Slice(0, 12), maximumCount));
        FormatUnchecked(compact, destination.Slice(0, length), StandardIdentifierChecksumKind.Isbn13, style);
        return length;
    }

    internal static string GenerateIssn(IRandomSource random, BookIdentifierStyle style, int maximumCount, int maximumOutputSize)
    {
        int length = DisplayLength(StandardIdentifierChecksumKind.Issn, style);
        Guard.OutputSize(length, maximumOutputSize, "value");
        Guard.Count(7, maximumCount, "value");
        char[] compact = new char[8];
        FillDigits(random, compact, 7);
        compact[7] = CheckCharacter(StandardIdentifierChecks.Calculate(StandardIdentifierChecksumKind.Issn, new ReadOnlySpan<char>(compact).Slice(0, 7), maximumCount));
        char[] destination = new char[length];
        FormatUnchecked(compact, destination, StandardIdentifierChecksumKind.Issn, style);
        return new string(destination);
    }

    internal static int WriteIssn(IRandomSource random, Span<char> destination, BookIdentifierStyle style, int maximumCount, int maximumOutputSize)
    {
        int length = DisplayLength(StandardIdentifierChecksumKind.Issn, style);
        Guard.OutputSize(length, maximumOutputSize, "value");
        Guard.Count(7, maximumCount, "value");
        RequireDestination(destination.Length, length);
        char[] compact = new char[8];
        FillDigits(random, compact, 7);
        compact[7] = CheckCharacter(StandardIdentifierChecks.Calculate(StandardIdentifierChecksumKind.Issn, new ReadOnlySpan<char>(compact).Slice(0, 7), maximumCount));
        FormatUnchecked(compact, destination.Slice(0, length), StandardIdentifierChecksumKind.Issn, style);
        return length;
    }

    internal static string Format(ReadOnlySpan<char> value, StandardIdentifierChecksumKind kind, BookIdentifierStyle style, int maximumCount, int maximumOutputSize)
    {
        RequireCompact(value, kind, maximumCount);
        int length = DisplayLength(kind, style);
        Guard.OutputSize(length, maximumOutputSize, "value");
        char[] destination = new char[length];
        FormatUnchecked(value, destination, kind, style);
        return new string(destination);
    }

    internal static int Format(ReadOnlySpan<char> value, Span<char> destination, StandardIdentifierChecksumKind kind, BookIdentifierStyle style, int maximumCount, int maximumOutputSize)
    {
        RequireCompact(value, kind, maximumCount);
        int length = DisplayLength(kind, style);
        Guard.OutputSize(length, maximumOutputSize, "value");
        RequireDestination(destination.Length, length);
        FormatUnchecked(value, destination.Slice(0, length), kind, style);
        return length;
    }

    static int DoiLength(int registrantDigits, int suffixLength, int maximumCount, int maximumOutputSize)
    {
        Guard.Count(registrantDigits, maximumCount, "registrantDigits");
        Guard.Count(suffixLength, maximumCount, "suffixLength");
        if (registrantDigits == 0)
        {
            throw new ArgumentOutOfRangeException("registrantDigits", registrantDigits, "registrantDigits must be greater than zero.");
        }
        if (suffixLength == 0)
        {
            throw new ArgumentOutOfRangeException("suffixLength", suffixLength, "suffixLength must be greater than zero.");
        }
        return Guard.OutputSize((long)registrantDigits + suffixLength + 4L, maximumOutputSize, "value");
    }

    static int DisplayLength(StandardIdentifierChecksumKind kind, BookIdentifierStyle style)
    {
        ValidateStyle(style);
        int compactLength = 8;
        int separatorCount = 1;
        int labelLength = 5;
        if (kind == StandardIdentifierChecksumKind.Isbn10)
        {
            compactLength = 10;
            separatorCount = 3;
        }
        else if (kind == StandardIdentifierChecksumKind.Isbn13)
        {
            compactLength = 13;
            separatorCount = 4;
        }
        if (style == BookIdentifierStyle.Compact)
        {
            return compactLength;
        }
        int result = compactLength + separatorCount;
        if (style == BookIdentifierStyle.Labeled)
        {
            result += labelLength;
        }
        return result;
    }

    static void WriteDoiUnchecked(IRandomSource random, Span<char> destination, int registrantDigits, int suffixLength)
    {
        destination[0] = '1';
        destination[1] = '0';
        destination[2] = '.';
        int index = 0;
        while (index < registrantDigits)
        {
            destination[3 + index] = (char)('0' + random.IntFromMaximumExclusive(10));
            index++;
        }
        destination[3 + registrantDigits] = '/';
        index = 0;
        while (index < suffixLength)
        {
            destination[4 + registrantDigits + index] = DoiSuffixAlphabet[random.IntFromMaximumExclusive(DoiSuffixAlphabet.Length)];
            index++;
        }
    }

    static void WriteDoiUrlUnchecked(ReadOnlySpan<char> value, Span<char> destination)
    {
        string prefix = "https://doi.org/";
        prefix.AsSpan().CopyTo(destination);
        value.CopyTo(destination.Slice(prefix.Length));
    }

    static void FillDigits(IRandomSource random, Span<char> destination, int count)
    {
        int index = 0;
        while (index < count)
        {
            destination[index] = (char)('0' + random.IntFromMaximumExclusive(10));
            index++;
        }
    }

    static char CheckCharacter(int value)
    {
        if (value == 10)
        {
            return 'X';
        }
        return (char)('0' + value);
    }

    static void FormatUnchecked(ReadOnlySpan<char> value, Span<char> destination, StandardIdentifierChecksumKind kind, BookIdentifierStyle style)
    {
        if (style == BookIdentifierStyle.Compact)
        {
            value.CopyTo(destination);
            return;
        }
        int destinationIndex = 0;
        if (style == BookIdentifierStyle.Labeled)
        {
            string label = kind == StandardIdentifierChecksumKind.Issn ? "ISSN " : "ISBN ";
            label.AsSpan().CopyTo(destination);
            destinationIndex = label.Length;
        }
        char separator = style == BookIdentifierStyle.Spaced ? ' ' : '-';
        if (kind == StandardIdentifierChecksumKind.Isbn10)
        {
            destinationIndex = CopyPart(value, 0, 1, destination, destinationIndex, separator);
            destinationIndex = CopyPart(value, 1, 3, destination, destinationIndex, separator);
            destinationIndex = CopyPart(value, 4, 5, destination, destinationIndex, separator);
            value.Slice(9, 1).CopyTo(destination.Slice(destinationIndex));
            return;
        }
        if (kind == StandardIdentifierChecksumKind.Isbn13)
        {
            destinationIndex = CopyPart(value, 0, 3, destination, destinationIndex, separator);
            destinationIndex = CopyPart(value, 3, 1, destination, destinationIndex, separator);
            destinationIndex = CopyPart(value, 4, 3, destination, destinationIndex, separator);
            destinationIndex = CopyPart(value, 7, 5, destination, destinationIndex, separator);
            value.Slice(12, 1).CopyTo(destination.Slice(destinationIndex));
            return;
        }
        value.Slice(0, 4).CopyTo(destination.Slice(destinationIndex));
        destinationIndex += 4;
        destination[destinationIndex] = separator;
        value.Slice(4, 4).CopyTo(destination.Slice(destinationIndex + 1));
    }

    static int CopyPart(ReadOnlySpan<char> source, int sourceIndex, int count, Span<char> destination, int destinationIndex, char separator)
    {
        source.Slice(sourceIndex, count).CopyTo(destination.Slice(destinationIndex));
        destinationIndex += count;
        destination[destinationIndex] = separator;
        return destinationIndex + 1;
    }

    static void RequireCompact(ReadOnlySpan<char> value, StandardIdentifierChecksumKind kind, int maximumCount)
    {
        if (!StandardIdentifierChecks.Validate(kind, value, maximumCount))
        {
            throw new ArgumentException("value must be a checksum-valid compact ISBN or ISSN for the selected formatter.", "value");
        }
    }

    static void RequireDoi(ReadOnlySpan<char> value)
    {
        if (!ValidateDoi(value))
        {
            throw new ArgumentException("value must match the restricted DOI shape 10.<digits>/<ASCII suffix>.", "value");
        }
    }

    static bool DoiSuffixCharacter(char value)
    {
        bool digit = value >= '0' && value <= '9';
        bool lower = value >= 'a' && value <= 'z';
        bool upper = value >= 'A' && value <= 'Z';
        return digit || lower || upper || value == '.' || value == '_' || value == '-';
    }

    static void ValidateStyle(BookIdentifierStyle style)
    {
        if (style != BookIdentifierStyle.Compact &&
            style != BookIdentifierStyle.Hyphenated &&
            style != BookIdentifierStyle.Spaced &&
            style != BookIdentifierStyle.Labeled)
        {
            throw new ArgumentOutOfRangeException("style", style, "style is not a supported book identifier display style.");
        }
    }

    static void RequireDestination(int actualLength, int requiredLength)
    {
        if (actualLength < requiredLength)
        {
            throw new ArgumentException($"destination must contain at least {requiredLength} elements.", "destination");
        }
    }
}
