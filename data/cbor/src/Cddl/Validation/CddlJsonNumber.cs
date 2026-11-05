using System.Numerics;

namespace cbor;

internal class CddlJsonNumber
{
    internal CborValue Value;
    internal BigInteger Coefficient;
    internal int Scale;

    internal CddlJsonNumber(
        CborValue value,
        BigInteger coefficient,
        int scale)
    {
        Value = value;
        Coefficient = coefficient;
        Scale = scale;
    }

    internal bool IsExactlyRepresentable(int additionalInformation)
    {
        int precision;
        int minimumPower;
        int maximumExponent;
        if (additionalInformation == 25)
        {
            precision = 11;
            minimumPower = -24;
            maximumExponent = 15;
        }
        else if (additionalInformation == 26)
        {
            precision = 24;
            minimumPower = -149;
            maximumExponent = 127;
        }
        else
        {
            precision = 53;
            minimumPower = -1074;
            maximumExponent = 1023;
        }

        BigInteger magnitude = BigInteger.Abs(Coefficient);
        if (magnitude.IsZero)
        {
            return true;
        }
        int power = 0;
        if (Scale < 0)
        {
            int denominatorPower = -Scale;
            int remainingFives = denominatorPower;
            while (remainingFives > 0 && magnitude % 5 == 0)
            {
                magnitude = magnitude / 5;
                remainingFives = remainingFives - 1;
            }
            if (remainingFives > 0)
            {
                return false;
            }
            power = -denominatorPower;
        }
        else
        {
            int remainingTens = Scale;
            while (remainingTens > 0)
            {
                magnitude = magnitude * 5;
                power = power + 1;
                remainingTens = remainingTens - 1;
            }
        }
        while (magnitude % 2 == 0)
        {
            magnitude = magnitude / 2;
            power = power + 1;
        }
        long bitLength = magnitude.GetBitLength();
        long highestExponent = bitLength + power - 1;
        return bitLength <= precision
            && power >= minimumPower
            && highestExponent <= maximumExponent;
    }
}
