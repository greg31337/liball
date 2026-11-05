namespace random;

internal class DyadicProbability
{
    ulong numerator;
    int denominatorBits;

    internal DyadicProbability(ulong numerator, int denominatorBits)
    {
        this.numerator = numerator;
        this.denominatorBits = denominatorBits;
    }

    internal static DyadicProbability FromDouble(double probability)
    {
        Guard.Probability(probability, "probability");
        ulong bits = PortableMath.DoubleBits(probability);
        int exponentBits = (int)((bits & PortableMath.DoubleExponentMask) >> 52);
        ulong fractionBits = bits & PortableMath.DoubleFractionMask;
        ulong value;
        int power;
        if (exponentBits == 0)
        {
            value = fractionBits;
            power = 1074;
        }
        else
        {
            value = (1UL << 52) | fractionBits;
            power = 1075 - exponentBits;
        }

        return Reduce(value, power);
    }

    internal static DyadicProbability FromSingle(float probability)
    {
        Guard.Probability(probability, "probability");
        uint bits = PortableMath.SingleBits(probability);
        int exponentBits = (int)((bits & PortableMath.SingleExponentMask) >> 23);
        uint fractionBits = bits & PortableMath.SingleFractionMask;
        ulong value;
        int power;
        if (exponentBits == 0)
        {
            value = fractionBits;
            power = 149;
        }
        else
        {
            value = (1U << 23) | fractionBits;
            power = 150 - exponentBits;
        }

        return Reduce(value, power);
    }

    internal ulong Numerator()
    {
        return numerator;
    }

    internal int DenominatorBits()
    {
        return denominatorBits;
    }

    internal static DyadicProbability Reduce(ulong value, int power)
    {
        if (value == 0UL)
        {
            return new DyadicProbability(0UL, 0);
        }

        while ((value & 1UL) == 0UL && power > 0)
        {
            value >>= 1;
            power--;
        }

        return new DyadicProbability(value, power);
    }
}
