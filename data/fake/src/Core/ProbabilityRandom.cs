using System;

namespace fake;

internal static class ProbabilityRandom
{
    internal static bool Probability(FakeRandom random, double probability)
    {
        FloatingRandom.Finite(probability, "probability");
        if (probability < 0.0 || probability > 1.0)
        {
            throw new ArgumentOutOfRangeException("probability", probability, "probability must be between zero and one, inclusive.");
        }
        if (probability == 0.0)
        {
            return false;
        }
        if (probability == 1.0)
        {
            return true;
        }
        return random.Double() < probability;
    }

    internal static bool Ratio(FakeRandom random, long numerator, long denominator)
    {
        if (denominator <= 0L)
        {
            throw new ArgumentOutOfRangeException("denominator", denominator, "denominator must be greater than zero.");
        }
        if (numerator < 0L || numerator > denominator)
        {
            throw new ArgumentOutOfRangeException("numerator", numerator, "numerator must be between zero and denominator, inclusive.");
        }
        if (numerator == 0L)
        {
            return false;
        }
        if (numerator == denominator)
        {
            return true;
        }
        return random.ULongFromMaximumExclusive((ulong)denominator) < (ulong)numerator;
    }
}
