namespace UniversalHtml.FuzzRunner;

internal sealed class DeterministicRandom
{
    const ulong Increment = 0x9E3779B97F4A7C15UL;
    ulong state;

    public DeterministicRandom(ulong seed)
    {
        state = seed;
    }

    public static ulong DeriveSeed(ulong rootSeed, FuzzTarget target,
        int caseIndex)
    {
        ulong value = rootSeed
            ^ ((ulong)target + 1UL) * 0xD1B54A32D192ED03UL
            ^ ((ulong)caseIndex + 1UL) * 0x94D049BB133111EBUL;
        return Mix(value);
    }

    public ulong NextUInt64()
    {
        state += Increment;
        return Mix(state);
    }

    public int NextInt(int exclusiveMaximum)
    {
        if (exclusiveMaximum < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));
        }

        return (int)(NextUInt64() % (uint)exclusiveMaximum);
    }

    public bool Chance(int numerator, int denominator)
    {
        return NextInt(denominator) < numerator;
    }

    public string Pick(string[] values)
    {
        return values[NextInt(values.Length)];
    }

    static ulong Mix(ulong value)
    {
        value ^= value >> 30;
        value *= 0xBF58476D1CE4E5B9UL;
        value ^= value >> 27;
        value *= 0x94D049BB133111EBUL;
        return value ^ value >> 31;
    }
}
