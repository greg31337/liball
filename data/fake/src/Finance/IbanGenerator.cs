using System;
using System.Text;

namespace fake;

internal static class IbanGenerator
{
    const string Marker = "fake.synthetic-iban.v1";

    internal static string Bban(
        FakeRandom random,
        string countryCode,
        int maximumCount,
        int maximumOutputSize)
    {
        IbanFormat format = IbanRegistryStore.Current().FindInternal(countryCode);
        return GenerateBban(random, format, maximumCount, maximumOutputSize);
    }

    internal static IbanRecord Record(
        FakeRandom random,
        int maximumCount,
        int maximumOutputSize)
    {
        IbanFormat format = IbanRegistryStore.Current().Choose(
            random,
            FakeLimits.DefaultMaximumOutputSize);
        return Record(random, format, maximumCount, maximumOutputSize);
    }

    internal static IbanRecord Record(
        FakeRandom random,
        string countryCode,
        int maximumCount,
        int maximumOutputSize)
    {
        IbanFormat format = IbanRegistryStore.Current().FindInternal(countryCode);
        return Record(random, format, maximumCount, maximumOutputSize);
    }

    internal static string Electronic(
        FakeRandom random,
        int maximumCount,
        int maximumOutputSize)
    {
        string result = Record(
            random,
            maximumCount,
            FakeLimits.DefaultMaximumOutputSize).Electronic;
        Guard.OutputSize(result.Length, maximumOutputSize, "iban");
        return result;
    }

    internal static string Electronic(
        FakeRandom random,
        string countryCode,
        int maximumCount,
        int maximumOutputSize)
    {
        string result = Record(
            random,
            countryCode,
            maximumCount,
            FakeLimits.DefaultMaximumOutputSize).Electronic;
        Guard.OutputSize(result.Length, maximumOutputSize, "iban");
        return result;
    }

    internal static string Print(
        FakeRandom random,
        int maximumCount,
        int maximumOutputSize)
    {
        string result = Record(
            random,
            maximumCount,
            FakeLimits.DefaultMaximumOutputSize).Print;
        Guard.OutputSize(result.Length, maximumOutputSize, "ibanPrint");
        return result;
    }

    internal static string Print(
        FakeRandom random,
        string countryCode,
        int maximumCount,
        int maximumOutputSize)
    {
        string result = Record(
            random,
            countryCode,
            maximumCount,
            FakeLimits.DefaultMaximumOutputSize).Print;
        Guard.OutputSize(result.Length, maximumOutputSize, "ibanPrint");
        return result;
    }

    internal static bool IsValid(string value, int maximumCount)
    {
        Guard.NotNull(value, "value");
        Guard.Count(value.Length, maximumCount, "value");
        string normalized = Normalize(value);
        if (normalized.Length < 5 || normalized.Length > 34)
        {
            return false;
        }
        IbanFormat? format = IbanRegistryStore.Current().FindPrefix(
            normalized.Substring(0, 2));
        if (format == null || normalized.Length != format.IbanLength ||
            !AsciiDigit(normalized[2]) || !AsciiDigit(normalized[3]))
        {
            return false;
        }
        if (!MatchesPattern(
            normalized.Substring(4),
            format.BbanPattern))
        {
            return false;
        }
        return Mod97(normalized) == 1;
    }

    internal static string Format(
        string value,
        int maximumCount,
        int maximumOutputSize)
    {
        Guard.NotNull(value, "value");
        if (!IsValid(value, maximumCount))
        {
            throw new ArgumentException(
                "value must be a structurally valid IBAN with a valid MOD 97 checksum.",
                "value");
        }
        string electronic = Normalize(value);
        return PrintText(electronic, maximumOutputSize);
    }

    static IbanRecord Record(
        FakeRandom random,
        IbanFormat format,
        int maximumCount,
        int maximumOutputSize)
    {
        string bban = GenerateBban(
            random,
            format,
            maximumCount,
            FakeLimits.DefaultMaximumOutputSize);
        string provisional = format.IbanPrefix + "00" + bban;
        int checkValue = 98 - Mod97(provisional);
        string checkDigits = checkValue.ToString("D2", System.Globalization.CultureInfo.InvariantCulture);
        string electronic = format.IbanPrefix + checkDigits + bban;
        string print = PrintText(electronic, FakeLimits.DefaultMaximumOutputSize);
        IbanRecord result = new IbanRecord();
        result.Electronic = electronic;
        result.Print = print;
        result.Bban = bban;
        result.CountryCode = format.CountryCode;
        result.IbanPrefix = format.IbanPrefix;
        result.Length = electronic.Length;
        result.BbanPattern = format.BbanPattern;
        result.BankCode = Part(bban, format.BankCodeStart, format.BankCodeLength);
        result.BranchCode = Part(bban, format.BranchCodeStart, format.BranchCodeLength);
        result.AccountCode = Part(bban, format.AccountCodeStart, format.AccountCodeLength);
        result.IsMod97Valid = Mod97(electronic) == 1;
        result.IsSynthetic = true;
        result.TestMarker = Marker;
        result.RepresentsActiveAccount = false;
        result.RegistryVersion = format.RegistryVersion;
        Guard.OutputSize(Size(result), maximumOutputSize, "ibanRecord");
        return result;
    }

    static string GenerateBban(
        FakeRandom random,
        IbanFormat format,
        int maximumCount,
        int maximumOutputSize)
    {
        Guard.Count(format.BbanLength, maximumCount, "bban");
        Guard.OutputSize(format.BbanLength, maximumOutputSize, "bban");
        char[] result = new char[format.BbanLength];
        string pattern = format.BbanPattern;
        int patternIndex = 0;
        int resultIndex = 0;
        while (patternIndex < pattern.Length)
        {
            int length = 0;
            while (patternIndex < pattern.Length && AsciiDigit(pattern[patternIndex]))
            {
                length = length * 10 + pattern[patternIndex] - '0';
                patternIndex = patternIndex + 1;
            }
            char kind = pattern[patternIndex + 1];
            patternIndex = patternIndex + 2;
            int end = resultIndex + length;
            while (resultIndex < end)
            {
                result[resultIndex] = RandomCharacter(random, kind);
                resultIndex = resultIndex + 1;
            }
        }
        return new string(result);
    }

    static char RandomCharacter(FakeRandom random, char kind)
    {
        if (kind == 'n')
        {
            return (char)('0' + random.IntFromMaximumExclusive(10));
        }
        if (kind == 'a')
        {
            return (char)('A' + random.IntFromMaximumExclusive(26));
        }
        int value = random.IntFromMaximumExclusive(36);
        return value < 10
            ? (char)('0' + value)
            : (char)('A' + value - 10);
    }

    static bool MatchesPattern(string bban, string pattern)
    {
        int patternIndex = 0;
        int valueIndex = 0;
        while (patternIndex < pattern.Length)
        {
            int length = 0;
            while (patternIndex < pattern.Length && AsciiDigit(pattern[patternIndex]))
            {
                length = length * 10 + pattern[patternIndex] - '0';
                patternIndex = patternIndex + 1;
            }
            char kind = pattern[patternIndex + 1];
            patternIndex = patternIndex + 2;
            int end = valueIndex + length;
            if (end > bban.Length)
            {
                return false;
            }
            while (valueIndex < end)
            {
                if (!Matches(bban[valueIndex], kind))
                {
                    return false;
                }
                valueIndex = valueIndex + 1;
            }
        }
        return valueIndex == bban.Length;
    }

    static bool Matches(char value, char kind)
    {
        if (kind == 'n')
        {
            return AsciiDigit(value);
        }
        if (kind == 'a')
        {
            return UpperAsciiLetter(value);
        }
        return AsciiDigit(value) || UpperAsciiLetter(value);
    }

    static int Mod97(string electronic)
    {
        int remainder = 0;
        int index = 4;
        while (index < electronic.Length)
        {
            remainder = AddCharacter(remainder, electronic[index]);
            index = index + 1;
        }
        index = 0;
        while (index < 4)
        {
            remainder = AddCharacter(remainder, electronic[index]);
            index = index + 1;
        }
        return remainder;
    }

    static int AddCharacter(int remainder, char value)
    {
        if (AsciiDigit(value))
        {
            return (remainder * 10 + value - '0') % 97;
        }
        return (remainder * 100 + value - 'A' + 10) % 97;
    }

    static string Normalize(string value)
    {
        StringBuilder result = new StringBuilder(value.Length);
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character != ' ')
            {
                if (!AsciiDigit(character) && !AsciiLetter(character))
                {
                    return string.Empty;
                }
                result.Append(char.ToUpperInvariant(character));
            }
            index = index + 1;
        }
        return result.ToString();
    }

    static string PrintText(string electronic, int maximumOutputSize)
    {
        int spaces = (electronic.Length - 1) / 4;
        StringBuilder result = new StringBuilder(electronic.Length + spaces);
        int index = 0;
        while (index < electronic.Length)
        {
            if (index != 0 && index % 4 == 0)
            {
                result.Append(' ');
            }
            result.Append(electronic[index]);
            index = index + 1;
        }
        Guard.OutputSize(result.Length, maximumOutputSize, "ibanPrint");
        return result.ToString();
    }

    static string Part(string value, int start, int length)
    {
        return length == 0 ? string.Empty : value.Substring(start, length);
    }

    static int Size(IbanRecord value)
    {
        return value.Electronic.Length + value.Print.Length + value.Bban.Length +
            value.CountryCode.Length + value.IbanPrefix.Length + value.BbanPattern.Length +
            value.BankCode.Length + value.BranchCode.Length + value.AccountCode.Length +
            value.TestMarker.Length + value.RegistryVersion.Length;
    }

    static bool AsciiDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    static bool AsciiLetter(char value)
    {
        return UpperAsciiLetter(value) || value >= 'a' && value <= 'z';
    }

    static bool UpperAsciiLetter(char value)
    {
        return value >= 'A' && value <= 'Z';
    }
}
