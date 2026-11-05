using System;

namespace fake;

internal static class ChecksumAlgorithms
{
    static readonly int[,] VerhoeffProducts = new int[,]
    {
        { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 },
        { 1, 2, 3, 4, 0, 6, 7, 8, 9, 5 },
        { 2, 3, 4, 0, 1, 7, 8, 9, 5, 6 },
        { 3, 4, 0, 1, 2, 8, 9, 5, 6, 7 },
        { 4, 0, 1, 2, 3, 9, 5, 6, 7, 8 },
        { 5, 9, 8, 7, 6, 0, 4, 3, 2, 1 },
        { 6, 5, 9, 8, 7, 1, 0, 4, 3, 2 },
        { 7, 6, 5, 9, 8, 2, 1, 0, 4, 3 },
        { 8, 7, 6, 5, 9, 3, 2, 1, 0, 4 },
        { 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 }
    };

    static readonly int[,] VerhoeffPermutations = new int[,]
    {
        { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 },
        { 1, 5, 7, 6, 2, 8, 3, 0, 9, 4 },
        { 5, 8, 0, 3, 7, 9, 6, 1, 4, 2 },
        { 8, 9, 1, 6, 0, 4, 3, 5, 2, 7 },
        { 9, 4, 5, 3, 1, 2, 6, 8, 7, 0 },
        { 4, 2, 8, 6, 5, 7, 3, 9, 0, 1 },
        { 2, 7, 9, 3, 8, 0, 6, 4, 1, 5 },
        { 7, 0, 4, 6, 9, 1, 3, 2, 5, 8 }
    };

    static readonly int[] VerhoeffInverses = new int[] { 0, 4, 3, 2, 1, 5, 6, 7, 8, 9 };

    static readonly int[,] DammTable = new int[,]
    {
        { 0, 3, 1, 7, 5, 9, 8, 6, 4, 2 },
        { 7, 0, 9, 2, 1, 5, 4, 8, 6, 3 },
        { 4, 2, 0, 6, 8, 7, 1, 3, 5, 9 },
        { 1, 7, 5, 0, 9, 8, 3, 4, 2, 6 },
        { 6, 1, 2, 3, 0, 4, 5, 9, 7, 8 },
        { 3, 6, 7, 4, 2, 0, 9, 5, 8, 1 },
        { 5, 8, 6, 9, 7, 2, 0, 1, 3, 4 },
        { 8, 9, 4, 5, 3, 6, 2, 0, 1, 7 },
        { 9, 4, 3, 8, 6, 1, 7, 2, 0, 5 },
        { 2, 5, 8, 1, 4, 3, 6, 7, 9, 0 }
    };

    internal static int Luhn(ReadOnlySpan<char> value, int maximumCount)
    {
        ValidateDigits(value, maximumCount, "value");
        int sum = 0;
        bool doubled = true;
        int index = value.Length - 1;
        while (index >= 0)
        {
            int digit = value[index] - '0';
            if (doubled)
            {
                digit = digit * 2;
                if (digit > 9)
                {
                    digit = digit - 9;
                }
            }
            sum = sum + digit;
            doubled = !doubled;
            index = index - 1;
        }
        return (10 - (sum % 10)) % 10;
    }

    internal static bool IsLuhn(ReadOnlySpan<char> value, int maximumCount)
    {
        if (value.Length == 0)
        {
            return false;
        }
        Guard.Count(value.Length - 1, maximumCount, "value");
        if (!Digits(value))
        {
            return false;
        }
        int expected = Luhn(value.Slice(0, value.Length - 1), maximumCount);
        return value[value.Length - 1] - '0' == expected;
    }

    internal static int Verhoeff(ReadOnlySpan<char> value, int maximumCount)
    {
        ValidateDigits(value, maximumCount, "value");
        int state = 0;
        int position = 0;
        int index = value.Length - 1;
        while (index >= 0)
        {
            int digit = value[index] - '0';
            state = VerhoeffProducts[state, VerhoeffPermutations[(position + 1) % 8, digit]];
            position = position + 1;
            index = index - 1;
        }
        return VerhoeffInverses[state];
    }

    internal static bool IsVerhoeff(ReadOnlySpan<char> value, int maximumCount)
    {
        if (value.Length == 0)
        {
            return false;
        }
        Guard.Count(value.Length - 1, maximumCount, "value");
        if (!Digits(value))
        {
            return false;
        }
        int state = 0;
        int position = 0;
        int index = value.Length - 1;
        while (index >= 0)
        {
            int digit = value[index] - '0';
            state = VerhoeffProducts[state, VerhoeffPermutations[position % 8, digit]];
            position = position + 1;
            index = index - 1;
        }
        return state == 0;
    }

    internal static int Damm(ReadOnlySpan<char> value, int maximumCount)
    {
        ValidateDigits(value, maximumCount, "value");
        int state = 0;
        int index = 0;
        while (index < value.Length)
        {
            state = DammTable[state, value[index] - '0'];
            index = index + 1;
        }
        return state;
    }

    internal static bool IsDamm(ReadOnlySpan<char> value, int maximumCount)
    {
        if (value.Length == 0)
        {
            return false;
        }
        Guard.Count(value.Length - 1, maximumCount, "value");
        if (!Digits(value))
        {
            return false;
        }
        return Damm(value, maximumCount) == 0;
    }

    internal static int Modulo(
        ReadOnlySpan<char> value,
        ReadOnlySpan<int> weights,
        int modulus,
        int maximumCount)
    {
        ValidateDigits(value, maximumCount, "value");
        int sum = 0;
        int position = 0;
        int index = value.Length - 1;
        while (index >= 0)
        {
            int digit = value[index] - '0';
            int weight = weights[position % weights.Length] % modulus;
            sum = (sum + (digit * weight)) % modulus;
            position = position + 1;
            index = index - 1;
        }
        return (modulus - sum) % modulus;
    }

    internal static bool IsModulo10(
        ReadOnlySpan<char> value,
        ReadOnlySpan<int> weights,
        int maximumCount)
    {
        if (value.Length == 0)
        {
            return false;
        }
        Guard.Count(value.Length - 1, maximumCount, "value");
        if (!Digits(value))
        {
            return false;
        }
        int expected = Modulo(value.Slice(0, value.Length - 1), weights, 10, maximumCount);
        return value[value.Length - 1] - '0' == expected;
    }

    internal static bool IsModulo11(
        ReadOnlySpan<char> value,
        ReadOnlySpan<int> weights,
        int maximumCount)
    {
        if (value.Length == 0)
        {
            return false;
        }
        Guard.Count(value.Length - 1, maximumCount, "value");
        ReadOnlySpan<char> payload = value.Slice(0, value.Length - 1);
        if (!Digits(payload))
        {
            return false;
        }
        int actual = CheckValue(value[value.Length - 1]);
        if (actual < 0)
        {
            return false;
        }
        return Modulo(payload, weights, 11, maximumCount) == actual;
    }

    internal static int Mod97(ReadOnlySpan<char> value, int maximumCount)
    {
        ValidateDigits(value, maximumCount, "value");
        int remainder = Remainder97(value);
        return 98 - ((remainder * 100) % 97);
    }

    internal static bool IsMod97(ReadOnlySpan<char> value, int maximumCount)
    {
        if (value.Length < 2)
        {
            return false;
        }
        Guard.Count(value.Length - 2, maximumCount, "value");
        if (!Digits(value))
        {
            return false;
        }
        return Remainder97(value) == 1;
    }

    internal static int[] Weights(ReadOnlySpan<int> weights, int maximumCount)
    {
        Guard.Count(weights.Length, maximumCount, "weights");
        if (weights.Length == 0)
        {
            throw new ArgumentException("weights cannot be empty.", "weights");
        }
        int[] result = new int[weights.Length];
        int index = 0;
        while (index < weights.Length)
        {
            if (weights[index] <= 0)
            {
                throw new ArgumentOutOfRangeException("weights", weights[index], $"weights[{index}] must be greater than zero.");
            }
            result[index] = weights[index];
            index = index + 1;
        }
        return result;
    }

    internal static int AppendDigit(
        ReadOnlySpan<char> value,
        Span<char> destination,
        int check,
        int maximumOutputSize)
    {
        int length = Guard.OutputSize((long)value.Length + 1L, maximumOutputSize, "value");
        RequireDestination(destination, length);
        value.CopyTo(destination);
        destination[value.Length] = CheckCharacter(check);
        return length;
    }

    internal static int AppendMod97(
        ReadOnlySpan<char> value,
        Span<char> destination,
        int check,
        int maximumOutputSize)
    {
        int length = Guard.OutputSize((long)value.Length + 2L, maximumOutputSize, "value");
        RequireDestination(destination, length);
        value.CopyTo(destination);
        destination[value.Length] = (char)('0' + (check / 10));
        destination[value.Length + 1] = (char)('0' + (check % 10));
        return length;
    }

    internal static char CheckCharacter(int check)
    {
        return check == 10 ? 'X' : (char)('0' + check);
    }

    static int CheckValue(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }
        if (value == 'X')
        {
            return 10;
        }
        return -1;
    }

    static int Remainder97(ReadOnlySpan<char> value)
    {
        int remainder = 0;
        int index = 0;
        while (index < value.Length)
        {
            remainder = ((remainder * 10) + (value[index] - '0')) % 97;
            index = index + 1;
        }
        return remainder;
    }

    static void ValidateDigits(ReadOnlySpan<char> value, int maximumCount, string parameterName)
    {
        Guard.Count(value.Length, maximumCount, parameterName);
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character < '0' || character > '9')
            {
                throw new ArgumentException($"{parameterName} must contain ASCII decimal digits only; invalid character at index {index}.", parameterName);
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

    static void RequireDestination(Span<char> destination, int requiredLength)
    {
        if (destination.Length < requiredLength)
        {
            throw new ArgumentException($"destination must contain at least {requiredLength} characters.", "destination");
        }
    }
}
