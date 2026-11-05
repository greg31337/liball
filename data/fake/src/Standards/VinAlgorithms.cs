using System;

namespace fake;

internal static class VinAlgorithms
{
    internal const int Length = 17;
    internal const int MinimumModelYear = 1980;
    internal const int MaximumModelYear = 2039;
    internal const string Alphabet = "ABCDEFGHJKLMNPRSTUVWXYZ0123456789";
    const string ModelYearCodes = "ABCDEFGHJKLMNPRSTVWXY123456789";
    static readonly int[] Weights = new int[] { 8, 7, 6, 5, 4, 3, 2, 10, 0, 9, 8, 7, 6, 5, 4, 3, 2 };

    internal static int Calculate(ReadOnlySpan<char> value, int maximumCount)
    {
        RequireShape(value, maximumCount);
        int sum = 0;
        int index = 0;
        while (index < Length)
        {
            sum += Transliterate(value[index]) * Weights[index];
            index++;
        }
        return sum % 11;
    }

    internal static bool Validate(ReadOnlySpan<char> value, int maximumCount)
    {
        if (value.Length != Length)
        {
            return false;
        }
        Guard.Count(value.Length, maximumCount, "value");
        if (!Characters(value))
        {
            return false;
        }
        int expected = Calculate(value, maximumCount);
        return value[8] == CheckCharacter(expected);
    }

    internal static string Apply(ReadOnlySpan<char> value, int maximumCount, int maximumOutputSize)
    {
        int check = Calculate(value, maximumCount);
        Guard.OutputSize(Length, maximumOutputSize, "value");
        char[] destination = value.ToArray();
        destination[8] = CheckCharacter(check);
        return new string(destination);
    }

    internal static int Apply(ReadOnlySpan<char> value, Span<char> destination, int maximumCount, int maximumOutputSize)
    {
        int check = Calculate(value, maximumCount);
        Guard.OutputSize(Length, maximumOutputSize, "value");
        RequireDestination(destination.Length, Length);
        value.CopyTo(destination);
        destination[8] = CheckCharacter(check);
        return Length;
    }

    internal static char CheckCharacter(int value)
    {
        if (value == 10)
        {
            return 'X';
        }
        return (char)('0' + value);
    }

    internal static char ModelYearCode(int modelYear)
    {
        ValidateModelYear(modelYear);
        return ModelYearCodes[(modelYear - MinimumModelYear) % ModelYearCodes.Length];
    }

    internal static int ModelYear(ReadOnlySpan<char> value, int maximumCount)
    {
        RequireValid(value, maximumCount);
        int cycleIndex = ModelYearCodes.IndexOf(value[9]);
        if (cycleIndex < 0)
        {
            throw new ArgumentException("value contains an unsupported model-year code.", "value");
        }
        if (value[6] >= '0' && value[6] <= '9')
        {
            return MinimumModelYear + cycleIndex;
        }
        return 2010 + cycleIndex;
    }

    internal static string Generate(IRandomSource random, int modelYear, int maximumCount, int maximumOutputSize)
    {
        ValidateGeneration(modelYear, maximumCount, maximumOutputSize);
        char[] destination = new char[Length];
        FillGenerated(random, destination, modelYear, maximumCount);
        return new string(destination);
    }

    internal static string Generate(IRandomSource random, ReadOnlySpan<char> wmi, int modelYear, int maximumCount, int maximumOutputSize)
    {
        RequirePart(wmi, 3, "wmi");
        ValidateGeneration(modelYear, maximumCount, maximumOutputSize);
        char[] destination = new char[Length];
        FillGenerated(random, destination, modelYear, maximumCount);
        wmi.CopyTo(destination);
        destination[8] = CheckCharacter(Calculate(destination, maximumCount));
        return new string(destination);
    }

    internal static int Write(IRandomSource random, Span<char> destination, int modelYear, int maximumCount, int maximumOutputSize)
    {
        ValidateGeneration(modelYear, maximumCount, maximumOutputSize);
        RequireDestination(destination.Length, Length);
        FillGenerated(random, destination.Slice(0, Length), modelYear, maximumCount);
        return Length;
    }

    internal static int Write(IRandomSource random, ReadOnlySpan<char> wmi, Span<char> destination, int modelYear, int maximumCount, int maximumOutputSize)
    {
        RequirePart(wmi, 3, "wmi");
        ValidateGeneration(modelYear, maximumCount, maximumOutputSize);
        RequireDestination(destination.Length, Length);
        Span<char> target = destination.Slice(0, Length);
        FillGenerated(random, target, modelYear, maximumCount);
        wmi.CopyTo(target);
        target[8] = CheckCharacter(Calculate(target, maximumCount));
        return Length;
    }

    internal static string Wmi(ReadOnlySpan<char> value, int maximumCount, int maximumOutputSize)
    {
        RequireValid(value, maximumCount);
        Guard.OutputSize(3, maximumOutputSize, "value");
        return new string(value.Slice(0, 3));
    }

    static void FillGenerated(IRandomSource random, Span<char> destination, int modelYear, int maximumCount)
    {
        int index = 0;
        while (index < Length)
        {
            destination[index] = Alphabet[random.IntFromMaximumExclusive(Alphabet.Length)];
            index++;
        }
        if (modelYear <= 2009)
        {
            destination[6] = (char)('0' + random.IntFromMaximumExclusive(10));
        }
        else
        {
            destination[6] = Alphabet[random.IntFromMaximumExclusive(23)];
        }
        destination[8] = '0';
        destination[9] = ModelYearCode(modelYear);
        index = 11;
        while (index < Length)
        {
            destination[index] = (char)('0' + random.IntFromMaximumExclusive(10));
            index++;
        }
        destination[8] = CheckCharacter(Calculate(destination, maximumCount));
    }

    static int Transliterate(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }
        if (value >= 'A' && value <= 'H')
        {
            return value - 'A' + 1;
        }
        if (value >= 'J' && value <= 'N')
        {
            return value - 'J' + 1;
        }
        if (value == 'P')
        {
            return 7;
        }
        if (value == 'R')
        {
            return 9;
        }
        if (value >= 'S' && value <= 'Z')
        {
            return value - 'S' + 2;
        }
        return -1;
    }

    static bool Characters(ReadOnlySpan<char> value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (Alphabet.IndexOf(value[index]) < 0)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    static void ValidateGeneration(int modelYear, int maximumCount, int maximumOutputSize)
    {
        ValidateModelYear(modelYear);
        Guard.Count(Length, maximumCount, "value");
        Guard.OutputSize(Length, maximumOutputSize, "value");
    }

    static void ValidateModelYear(int modelYear)
    {
        if (modelYear < MinimumModelYear || modelYear > MaximumModelYear)
        {
            throw new ArgumentOutOfRangeException("modelYear", modelYear, "modelYear must be between 1980 and 2039 for an unambiguous generated VIN shape.");
        }
    }

    static void RequirePart(ReadOnlySpan<char> value, int length, string parameterName)
    {
        if (value.Length != length || !Characters(value))
        {
            throw new ArgumentException($"{parameterName} must contain exactly {length} permitted uppercase VIN characters.", parameterName);
        }
    }

    static void RequireShape(ReadOnlySpan<char> value, int maximumCount)
    {
        if (value.Length != Length || !Characters(value))
        {
            throw new ArgumentException("value must contain exactly 17 permitted uppercase VIN characters.", "value");
        }
        Guard.Count(value.Length, maximumCount, "value");
    }

    static void RequireValid(ReadOnlySpan<char> value, int maximumCount)
    {
        if (!Validate(value, maximumCount))
        {
            throw new ArgumentException("value must be a structurally valid 17-character VIN with the correct check digit.", "value");
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
