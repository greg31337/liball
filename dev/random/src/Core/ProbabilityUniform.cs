namespace random;

internal static class ProbabilityUniform
{
    internal static bool NextBoolean(IRandomSource generator, double probability)
    {
        Guard.NotNull(generator, "generator");
        Guard.Probability(probability, "probability");
        if (probability == 0.0)
        {
            return false;
        }

        if (probability == 1.0)
        {
            return true;
        }

        DyadicProbability dyadic = DyadicProbability.FromDouble(probability);
        return NextBoolean(generator, dyadic);
    }

    internal static bool NextBoolean(IRandomSource generator, float probability)
    {
        Guard.NotNull(generator, "generator");
        Guard.Probability(probability, "probability");
        if (probability == 0.0f)
        {
            return false;
        }

        if (probability == 1.0f)
        {
            return true;
        }

        DyadicProbability dyadic = DyadicProbability.FromSingle(probability);
        return NextBoolean(generator, dyadic);
    }

    internal static bool NextBoolean(IRandomSource generator, DyadicProbability probability)
    {
        Guard.NotNull(generator, "generator");
        Guard.NotNull(probability, "probability");
        ulong numerator = probability.Numerator();
        int bitIndex = probability.DenominatorBits() - 1;
        while (bitIndex >= 0)
        {
            bool probabilityBit = false;
            if (bitIndex < 64)
            {
                probabilityBit = ((numerator >> bitIndex) & 1UL) != 0UL;
            }

            bool randomBit = generator.NextBoolean();
            if (randomBit != probabilityBit)
            {
                return probabilityBit;
            }

            bitIndex--;
        }

        return false;
    }
}
