using System;

namespace fake;

internal static class NumericStringGenerator
{
    const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyz";

    internal static int Digit(FakeRandom random, int radix)
    {
        ValidateRadix(radix);
        return random.IntFromMaximumExclusive(radix);
    }

    internal static string Digits(FakeRandom random, int count, int maximumOutputSize)
    {
        int length = Guard.OutputSize(count, maximumOutputSize, "count");
        char[] result = new char[length];
        FillDigits(random, new Span<char>(result));
        return new string(result);
    }

    internal static int WriteDigits(FakeRandom random, Span<char> destination, int maximumOutputSize)
    {
        Guard.OutputSize(destination.Length, maximumOutputSize, "destination");
        FillDigits(random, destination);
        return destination.Length;
    }

    internal static string Generate(FakeRandom random, int radix, long minimum, long maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits, int maximumOutputSize)
    {
        int maximumLength = MaximumLength(radix, minimum, maximum, maximumInclusive, alwaysSign, minimumDigits);
        Guard.OutputSize(maximumLength, maximumOutputSize, "minimumDigits");
        long value = random.LongFromMinimumAndMaximumAndMaximumInclusive(minimum, maximum, maximumInclusive);
        int length = ValueLength(value, radix, alwaysSign, minimumDigits);
        char[] result = new char[length];
        Format(new Span<char>(result), value, radix, alwaysSign, minimumDigits);
        return new string(result);
    }

    internal static string Generate(FakeRandom random, int radix, ulong minimum, ulong maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits, int maximumOutputSize)
    {
        int maximumLength = MaximumLength(radix, minimum, maximum, maximumInclusive, alwaysSign, minimumDigits);
        Guard.OutputSize(maximumLength, maximumOutputSize, "minimumDigits");
        ulong value = random.ULongFromMinimumAndMaximumAndMaximumInclusive(minimum, maximum, maximumInclusive);
        int length = ValueLength(value, radix, alwaysSign, minimumDigits);
        char[] result = new char[length];
        Format(new Span<char>(result), value, radix, alwaysSign, minimumDigits);
        return new string(result);
    }

    internal static int Write(FakeRandom random, Span<char> destination, int radix, long minimum, long maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits, int maximumOutputSize)
    {
        int maximumLength = MaximumLength(radix, minimum, maximum, maximumInclusive, alwaysSign, minimumDigits);
        Guard.OutputSize(maximumLength, maximumOutputSize, "minimumDigits");
        if (destination.Length < maximumLength)
        {
            throw new ArgumentException($"destination must contain at least {maximumLength} characters for the requested bounds and formatting.", "destination");
        }
        long value = random.LongFromMinimumAndMaximumAndMaximumInclusive(minimum, maximum, maximumInclusive);
        return Format(destination, value, radix, alwaysSign, minimumDigits);
    }

    internal static int Write(FakeRandom random, Span<char> destination, int radix, ulong minimum, ulong maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits, int maximumOutputSize)
    {
        int maximumLength = MaximumLength(radix, minimum, maximum, maximumInclusive, alwaysSign, minimumDigits);
        Guard.OutputSize(maximumLength, maximumOutputSize, "minimumDigits");
        if (destination.Length < maximumLength)
        {
            throw new ArgumentException($"destination must contain at least {maximumLength} characters for the requested bounds and formatting.", "destination");
        }
        ulong value = random.ULongFromMinimumAndMaximumAndMaximumInclusive(minimum, maximum, maximumInclusive);
        return Format(destination, value, radix, alwaysSign, minimumDigits);
    }

    static void FillDigits(FakeRandom random, Span<char> destination)
    {
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = Alphabet[random.IntFromMaximumExclusive(10)];
            index = index + 1;
        }
    }

    static int MaximumLength(int radix, long minimum, long maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits)
    {
        ValidateRadix(radix);
        Guard.NonNegative(minimumDigits, "minimumDigits");
        ValidateRange(minimum, maximum, maximumInclusive);

        long upper = maximumInclusive ? maximum : maximum - 1L;
        ulong minimumMagnitude = Magnitude(minimum);
        ulong upperMagnitude = Magnitude(upper);
        ulong greatestMagnitude = minimumMagnitude > upperMagnitude ? minimumMagnitude : upperMagnitude;
        int digits = DigitCount(greatestMagnitude, radix);
        int paddedDigits = digits > minimumDigits ? digits : minimumDigits;
        int signLength = alwaysSign || minimum < 0L ? 1 : 0;
        return Guard.OutputSize((long)paddedDigits + (long)signLength, int.MaxValue, "minimumDigits");
    }

    static int MaximumLength(int radix, ulong minimum, ulong maximum, bool maximumInclusive, bool alwaysSign, int minimumDigits)
    {
        ValidateRadix(radix);
        Guard.NonNegative(minimumDigits, "minimumDigits");
        ValidateRange(minimum, maximum, maximumInclusive);

        ulong upper = maximumInclusive ? maximum : maximum - 1UL;
        int digits = DigitCount(upper, radix);
        int paddedDigits = digits > minimumDigits ? digits : minimumDigits;
        int signLength = alwaysSign ? 1 : 0;
        return Guard.OutputSize((long)paddedDigits + (long)signLength, int.MaxValue, "minimumDigits");
    }

    static void ValidateRadix(int radix)
    {
        if (radix < 2 || radix > Alphabet.Length)
        {
            throw new ArgumentOutOfRangeException("radix", radix, "radix must be between 2 and 36, inclusive.");
        }
    }

    static void ValidateRange(long minimum, long maximum, bool maximumInclusive)
    {
        if (maximumInclusive)
        {
            if (maximum < minimum)
            {
                throw new ArgumentOutOfRangeException("maximum", maximum, "maximum must be greater than or equal to minimum when it is inclusive.");
            }
            return;
        }
        if (maximum <= minimum)
        {
            throw new ArgumentOutOfRangeException("maximum", maximum, "maximum must be greater than minimum when it is exclusive.");
        }
    }

    static void ValidateRange(ulong minimum, ulong maximum, bool maximumInclusive)
    {
        if (maximumInclusive)
        {
            if (maximum < minimum)
            {
                throw new ArgumentOutOfRangeException("maximum", maximum, "maximum must be greater than or equal to minimum when it is inclusive.");
            }
            return;
        }
        if (maximum <= minimum)
        {
            throw new ArgumentOutOfRangeException("maximum", maximum, "maximum must be greater than minimum when it is exclusive.");
        }
    }

    static int ValueLength(long value, int radix, bool alwaysSign, int minimumDigits)
    {
        int naturalDigits = DigitCount(Magnitude(value), radix);
        int digits = naturalDigits > minimumDigits ? naturalDigits : minimumDigits;
        int signLength = value < 0L || alwaysSign ? 1 : 0;
        return digits + signLength;
    }

    static int ValueLength(ulong value, int radix, bool alwaysSign, int minimumDigits)
    {
        int naturalDigits = DigitCount(value, radix);
        int digits = naturalDigits > minimumDigits ? naturalDigits : minimumDigits;
        int signLength = alwaysSign ? 1 : 0;
        return digits + signLength;
    }

    static int Format(Span<char> destination, long value, int radix, bool alwaysSign, int minimumDigits)
    {
        ulong remaining = Magnitude(value);
        int naturalDigits = DigitCount(remaining, radix);
        int digits = naturalDigits > minimumDigits ? naturalDigits : minimumDigits;
        int signLength = value < 0L || alwaysSign ? 1 : 0;
        int length = digits + signLength;
        int index = length - 1;
        int digitIndex = 0;
        while (digitIndex < naturalDigits)
        {
            destination[index] = Alphabet[(int)(remaining % (ulong)radix)];
            remaining = remaining / (ulong)radix;
            index = index - 1;
            digitIndex = digitIndex + 1;
        }
        while (index >= signLength)
        {
            destination[index] = '0';
            index = index - 1;
        }
        if (signLength == 1)
        {
            destination[0] = value < 0L ? '-' : '+';
        }
        return length;
    }

    static int Format(Span<char> destination, ulong value, int radix, bool alwaysSign, int minimumDigits)
    {
        ulong remaining = value;
        int naturalDigits = DigitCount(remaining, radix);
        int digits = naturalDigits > minimumDigits ? naturalDigits : minimumDigits;
        int signLength = alwaysSign ? 1 : 0;
        int length = digits + signLength;
        int index = length - 1;
        int digitIndex = 0;
        while (digitIndex < naturalDigits)
        {
            destination[index] = Alphabet[(int)(remaining % (ulong)radix)];
            remaining = remaining / (ulong)radix;
            index = index - 1;
            digitIndex = digitIndex + 1;
        }
        while (index >= signLength)
        {
            destination[index] = '0';
            index = index - 1;
        }
        if (signLength == 1)
        {
            destination[0] = '+';
        }
        return length;
    }

    static int DigitCount(ulong value, int radix)
    {
        int count = 1;
        ulong remaining = value;
        while (remaining >= (ulong)radix)
        {
            remaining = remaining / (ulong)radix;
            count = count + 1;
        }
        return count;
    }

    static ulong Magnitude(long value)
    {
        if (value >= 0L)
        {
            return (ulong)value;
        }
        return (ulong)(-(value + 1L)) + 1UL;
    }
}
