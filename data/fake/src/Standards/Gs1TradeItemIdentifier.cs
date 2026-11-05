using System;

namespace fake;

internal static class Gs1TradeItemIdentifier
{
    internal const string GeneralSpecificationsVersion = "26.0";
    static readonly int[] CompleteLengths = new int[] { 8, 12, 13, 14 };

    internal static string Generate(IRandomSource random, int length, int maximumCount, int maximumOutputSize)
    {
        ValidateGeneration(length, maximumCount, maximumOutputSize);
        char[] destination = new char[length];
        GenerateUnchecked(random, destination, length, maximumCount);
        return new string(destination);
    }

    internal static int Write(IRandomSource random, Span<char> destination, int length, int maximumCount, int maximumOutputSize)
    {
        ValidateGeneration(length, maximumCount, maximumOutputSize);
        RequireDestination(destination.Length, length);
        GenerateUnchecked(random, destination.Slice(0, length), length, maximumCount);
        return length;
    }

    internal static bool Validate(ReadOnlySpan<char> value, int maximumCount)
    {
        return StandardIdentifierChecks.Validate(StandardIdentifierChecksumKind.Gtin, value, maximumCount);
    }

    internal static string NormalizeToFourteen(ReadOnlySpan<char> value, int maximumCount, int maximumOutputSize)
    {
        RequireValid(value, maximumCount);
        Guard.OutputSize(14, maximumOutputSize, "value");
        char[] destination = new char[14];
        NormalizeUnchecked(value, destination);
        return new string(destination);
    }

    internal static int NormalizeToFourteen(ReadOnlySpan<char> value, Span<char> destination, int maximumCount, int maximumOutputSize)
    {
        RequireValid(value, maximumCount);
        Guard.OutputSize(14, maximumOutputSize, "value");
        RequireDestination(destination.Length, 14);
        NormalizeUnchecked(value, destination.Slice(0, 14));
        return 14;
    }

    internal static int[] Lengths()
    {
        int[] result = new int[CompleteLengths.Length];
        new ReadOnlySpan<int>(CompleteLengths).CopyTo(result);
        return result;
    }

    internal static bool CompleteLength(int length)
    {
        int index = 0;
        while (index < CompleteLengths.Length)
        {
            if (CompleteLengths[index] == length)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static bool PayloadLength(int length)
    {
        return CompleteLength(length + 1);
    }

    static void GenerateUnchecked(IRandomSource random, Span<char> destination, int length, int maximumCount)
    {
        int payloadLength = length - 1;
        int index = 0;
        while (index < payloadLength)
        {
            destination[index] = (char)('0' + random.IntFromMaximumExclusive(10));
            index++;
        }
        int check = StandardIdentifierChecks.Calculate(
            StandardIdentifierChecksumKind.Gtin,
            destination.Slice(0, payloadLength),
            maximumCount);
        destination[payloadLength] = (char)('0' + check);
    }

    static void NormalizeUnchecked(ReadOnlySpan<char> value, Span<char> destination)
    {
        int padding = 14 - value.Length;
        int index = 0;
        while (index < padding)
        {
            destination[index] = '0';
            index++;
        }
        value.CopyTo(destination.Slice(padding));
    }

    static void ValidateGeneration(int length, int maximumCount, int maximumOutputSize)
    {
        if (!CompleteLength(length))
        {
            throw new ArgumentOutOfRangeException("length", length, "length must be 8, 12, 13, or 14 for a GTIN shape.");
        }
        Guard.Count(length - 1, maximumCount, "length");
        Guard.OutputSize(length, maximumOutputSize, "value");
    }

    static void RequireValid(ReadOnlySpan<char> value, int maximumCount)
    {
        if (!Validate(value, maximumCount))
        {
            throw new ArgumentException("value must be a checksum-valid compact GTIN-8, GTIN-12, GTIN-13, or GTIN-14 shape.", "value");
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
