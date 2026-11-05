using System;
using System.Globalization;

namespace fake;

internal class DecimalSequenceState
{
    internal ulong StartValue;
    internal ulong IncrementValue;
    internal int MinimumDigits;
    ulong CurrentValue;
    bool Exhausted;
    int MaximumOutputSize;
    object Sync;

    internal DecimalSequenceState(ulong start, ulong increment, int minimumDigits, int maximumOutputSize)
    {
        Validate(increment, minimumDigits, maximumOutputSize);
        StartValue = start;
        IncrementValue = increment;
        MinimumDigits = minimumDigits;
        CurrentValue = start;
        Exhausted = false;
        MaximumOutputSize = maximumOutputSize;
        Sync = new object();
    }

    internal string Next()
    {
        lock (Sync)
        {
            if (Exhausted)
            {
                throw FakeException.Create("Decimal identifier sequence overflowed its unsigned 64-bit range.");
            }
            string digits = CurrentValue.ToString(CultureInfo.InvariantCulture);
            int length = digits.Length > MinimumDigits ? digits.Length : MinimumDigits;
            Guard.OutputSize(length, MaximumOutputSize, "value");
            char[] result = new char[length];
            int padding = length - digits.Length;
            int index = 0;
            while (index < padding)
            {
                result[index] = '0';
                index++;
            }
            digits.AsSpan().CopyTo(new Span<char>(result).Slice(padding));
            if (CurrentValue > ulong.MaxValue - IncrementValue)
            {
                Exhausted = true;
            }
            else
            {
                CurrentValue += IncrementValue;
            }
            return new string(result);
        }
    }

    internal bool Matches(ulong start, ulong increment, int minimumDigits)
    {
        return StartValue == start
            && IncrementValue == increment
            && MinimumDigits == minimumDigits;
    }

    internal static void Validate(ulong increment, int minimumDigits, int maximumOutputSize)
    {
        if (increment == 0UL)
        {
            throw new ArgumentOutOfRangeException("increment", increment, "increment must be greater than zero.");
        }
        Guard.NonNegative(minimumDigits, "minimumDigits");
        Guard.OutputSize(minimumDigits, maximumOutputSize, "minimumDigits");
    }
}
