namespace countrycodes;

internal static class AsciiCodeUtility
{
    internal static bool IsAsciiLetter(char value)
    {
        return (value >= 'A' && value <= 'Z') ||
               (value >= 'a' && value <= 'z');
    }

    internal static bool IsAsciiDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    internal static bool IsAsciiAlphanumeric(char value)
    {
        return IsAsciiLetter(value) || IsAsciiDigit(value);
    }

    internal static char ToUpperAscii(char value)
    {
        if (value >= 'a' && value <= 'z')
        {
            return (char)(value - ('a' - 'A'));
        }

        return value;
    }

    internal static bool IsAlpha2(global::System.ReadOnlySpan<char> value)
    {
        return HasAsciiLetters(value, CountryCodeConstants.Alpha2Length);
    }

    internal static bool IsAlpha3(global::System.ReadOnlySpan<char> value)
    {
        return HasAsciiLetters(value, CountryCodeConstants.Alpha3Length);
    }

    internal static bool IsAlpha4(global::System.ReadOnlySpan<char> value)
    {
        return HasAsciiLetters(value, CountryCodeConstants.Alpha4Length);
    }

    internal static bool IsNumeric3(global::System.ReadOnlySpan<char> value)
    {
        if (value.Length != CountryCodeConstants.Numeric3Length)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (!IsAsciiDigit(value[index]))
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    internal static bool TryNormalizeAlpha2(
        global::System.ReadOnlySpan<char> value,
        global::System.Span<char> destination)
    {
        return TryNormalizeLetters(
            value,
            destination,
            CountryCodeConstants.Alpha2Length);
    }

    internal static bool TryNormalizeAlpha3(
        global::System.ReadOnlySpan<char> value,
        global::System.Span<char> destination)
    {
        return TryNormalizeLetters(
            value,
            destination,
            CountryCodeConstants.Alpha3Length);
    }

    internal static bool TryNormalizeAlpha4(
        global::System.ReadOnlySpan<char> value,
        global::System.Span<char> destination)
    {
        return TryNormalizeLetters(
            value,
            destination,
            CountryCodeConstants.Alpha4Length);
    }

    internal static bool TryNormalizeNumeric3(
        global::System.ReadOnlySpan<char> value,
        global::System.Span<char> destination)
    {
        if (!IsNumeric3(value) || destination.Length < CountryCodeConstants.Numeric3Length)
        {
            return false;
        }

        int index = 0;
        while (index < CountryCodeConstants.Numeric3Length)
        {
            destination[index] = value[index];
            index += 1;
        }

        return true;
    }

    internal static int ParseNumeric3Value(global::System.ReadOnlySpan<char> value)
    {
        if (!IsNumeric3(value))
        {
            return -1;
        }

        return (value[0] - '0') * 100 +
               (value[1] - '0') * 10 +
               value[2] - '0';
    }

    internal static bool TryFormatNumeric3(
        int value,
        global::System.Span<char> destination)
    {
        if (value < CountryCodeConstants.MinimumNumericValue ||
            value > CountryCodeConstants.MaximumNumericValue ||
            destination.Length < CountryCodeConstants.Numeric3Length)
        {
            return false;
        }

        destination[0] = (char)('0' + value / 100);
        destination[1] = (char)('0' + value / 10 % 10);
        destination[2] = (char)('0' + value % 10);
        return true;
    }

    internal static bool IsSubdivisionSeparator(char value, bool permissive)
    {
        return value == CountryCodeConstants.SubdivisionSeparator ||
               (permissive && value == '_');
    }

    internal static bool IsSubdivisionCode(
        global::System.ReadOnlySpan<char> value,
        bool permissive)
    {
        int minimumLength = CountryCodeConstants.SubdivisionCountryCodeLength +
                            1 +
                            CountryCodeConstants.SubdivisionMinimumSuffixLength;
        int maximumLength = CountryCodeConstants.SubdivisionCountryCodeLength +
                            1 +
                            CountryCodeConstants.SubdivisionMaximumSuffixLength;
        if (value.Length < minimumLength || value.Length > maximumLength)
        {
            return false;
        }

        if (!IsAsciiLetter(value[0]) ||
            !IsAsciiLetter(value[1]) ||
            !IsSubdivisionSeparator(value[2], permissive))
        {
            return false;
        }

        int index = 3;
        while (index < value.Length)
        {
            if (!IsAsciiAlphanumeric(value[index]))
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    internal static bool TryNormalizeSubdivisionCode(
        global::System.ReadOnlySpan<char> value,
        global::System.Span<char> destination,
        bool permissive)
    {
        if (!IsSubdivisionCode(value, permissive) || destination.Length < value.Length)
        {
            return false;
        }

        destination[0] = ToUpperAscii(value[0]);
        destination[1] = ToUpperAscii(value[1]);
        destination[2] = CountryCodeConstants.SubdivisionSeparator;
        int index = 3;
        while (index < value.Length)
        {
            destination[index] = ToUpperAscii(value[index]);
            index += 1;
        }

        return true;
    }

    internal static bool EqualsOrdinal(
        global::System.ReadOnlySpan<char> left,
        global::System.ReadOnlySpan<char> right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    internal static bool EqualsOrdinalIgnoreAsciiCase(
        global::System.ReadOnlySpan<char> left,
        global::System.ReadOnlySpan<char> right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (ToUpperAscii(left[index]) != ToUpperAscii(right[index]))
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    internal static int CompareOrdinal(
        global::System.ReadOnlySpan<char> left,
        global::System.ReadOnlySpan<char> right)
    {
        int sharedLength = left.Length;
        if (right.Length < sharedLength)
        {
            sharedLength = right.Length;
        }

        int index = 0;
        while (index < sharedLength)
        {
            if (left[index] < right[index])
            {
                return -1;
            }

            if (left[index] > right[index])
            {
                return 1;
            }

            index += 1;
        }

        if (left.Length < right.Length)
        {
            return -1;
        }

        if (left.Length > right.Length)
        {
            return 1;
        }

        return 0;
    }

    internal static int CompareOrdinalIgnoreAsciiCase(
        global::System.ReadOnlySpan<char> left,
        global::System.ReadOnlySpan<char> right)
    {
        int sharedLength = left.Length;
        if (right.Length < sharedLength)
        {
            sharedLength = right.Length;
        }

        int index = 0;
        while (index < sharedLength)
        {
            char leftValue = ToUpperAscii(left[index]);
            char rightValue = ToUpperAscii(right[index]);
            if (leftValue < rightValue)
            {
                return -1;
            }

            if (leftValue > rightValue)
            {
                return 1;
            }

            index += 1;
        }

        if (left.Length < right.Length)
        {
            return -1;
        }

        if (left.Length > right.Length)
        {
            return 1;
        }

        return 0;
    }

    internal static bool HasAsciiLetters(
        global::System.ReadOnlySpan<char> value,
        int expectedLength)
    {
        if (value.Length != expectedLength)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            if (!IsAsciiLetter(value[index]))
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    internal static bool TryNormalizeLetters(
        global::System.ReadOnlySpan<char> value,
        global::System.Span<char> destination,
        int expectedLength)
    {
        if (!HasAsciiLetters(value, expectedLength) || destination.Length < expectedLength)
        {
            return false;
        }

        int index = 0;
        while (index < expectedLength)
        {
            destination[index] = ToUpperAscii(value[index]);
            index += 1;
        }

        return true;
    }
}
