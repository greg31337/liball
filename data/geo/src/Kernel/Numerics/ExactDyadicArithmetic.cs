using System;

namespace geo;

internal class ExactDyadicValue
{
    internal int Sign;
    internal uint[] Magnitude = [];
    internal int Exponent;
}

internal static class ExactDyadicArithmetic
{
    internal static ExactDyadicValue CreateProductSum(
        BinaryProduct[] products, int[] coefficients)
    {
        if (products == null || coefficients == null || products.Length != coefficients.Length)
        {
            throw new InvalidGeometryException(
                "Exact product sums require matching product and coefficient arrays.");
        }
        ExactDyadicValue result = new ExactDyadicValue();
        int minimumExponent = RobustOrientation.GetMinimumProductExponent(products);
        if (minimumExponent == int.MaxValue) return result;
        int accumulatorLength = RobustOrientation.GetAccumulatorLength(
            products, minimumExponent);
        uint[] positive = new uint[accumulatorLength];
        uint[] negative = new uint[accumulatorLength];
        int index = 0;
        while (index < products.Length)
        {
            BinaryProduct product = products[index];
            if (product.Sign != 0 && coefficients[index] != 0)
            {
                int sign = product.Sign * coefficients[index];
                uint[] accumulator = sign > 0 ? positive : negative;
                RobustOrientation.AddProductMagnitude(
                    accumulator, product, minimumExponent);
            }
            index++;
        }
        int comparison = RobustOrientation.CompareMagnitudes(positive, negative);
        if (comparison == 0) return result;
        result.Sign = comparison;
        result.Exponent = minimumExponent;
        if (comparison > 0)
        {
            result.Magnitude = SubtractMagnitudes(positive, negative);
        }
        else
        {
            result.Magnitude = SubtractMagnitudes(negative, positive);
        }
        return result;
    }

    internal static uint[] SubtractMagnitudes(uint[] larger, uint[] smaller)
    {
        uint[] difference = new uint[larger.Length];
        ulong borrow = 0;
        int index = 0;
        while (index < larger.Length)
        {
            ulong left = larger[index];
            ulong right = smaller[index] + borrow;
            if (left >= right)
            {
                difference[index] = (uint)(left - right);
                borrow = 0;
            }
            else
            {
                difference[index] = (uint)((1UL << 32) + left - right);
                borrow = 1;
            }
            index++;
        }
        return difference;
    }

    internal static double DivideValues(ExactDyadicValue numerator, ExactDyadicValue denominator)
    {
        ValidateExactValue(numerator, "Exact numerator");
        ValidateExactValue(denominator, "Exact denominator");
        if (denominator.Sign == 0)
        {
            throw new InvalidGeometryException("An exact value cannot be divided by zero.");
        }
        if (numerator.Sign == 0) return 0.0;
        int numeratorBit = GetHighestBitIndex(numerator.Magnitude);
        int denominatorBit = GetHighestBitIndex(denominator.Magnitude);
        double numeratorMantissa = GetNormalizedMantissa(
            numerator.Magnitude, numeratorBit);
        double denominatorMantissa = GetNormalizedMantissa(
            denominator.Magnitude, denominatorBit);
        int exponent = numerator.Exponent + numeratorBit -
            denominator.Exponent - denominatorBit;
        double quotient = Math.ScaleB(numeratorMantissa / denominatorMantissa, exponent);
        if (numerator.Sign != denominator.Sign) quotient = -quotient;
        return quotient;
    }

    internal static int CompareAbsoluteValues(
        ExactDyadicValue first, ExactDyadicValue second)
    {
        ValidateExactValue(first, "First exact value");
        ValidateExactValue(second, "Second exact value");
        if (first.Sign == 0) return second.Sign == 0 ? 0 : -1;
        if (second.Sign == 0) return 1;
        int firstHighest = GetHighestBitIndex(first.Magnitude);
        int secondHighest = GetHighestBitIndex(second.Magnitude);
        int firstTop = first.Exponent + firstHighest;
        int secondTop = second.Exponent + secondHighest;
        if (firstTop < secondTop) return -1;
        if (firstTop > secondTop) return 1;
        int minimumPower = first.Exponent < second.Exponent ?
            first.Exponent : second.Exponent;
        int power = firstTop;
        while (power >= minimumPower)
        {
            bool firstBit = GetMagnitudeBit(first, power);
            bool secondBit = GetMagnitudeBit(second, power);
            if (firstBit != secondBit) return firstBit ? 1 : -1;
            power--;
        }
        return 0;
    }

    internal static bool GetMagnitudeBit(ExactDyadicValue value, int power)
    {
        int bit = power - value.Exponent;
        if (bit < 0) return false;
        int word = bit / 32;
        if (word >= value.Magnitude.Length) return false;
        int offset = bit % 32;
        return (value.Magnitude[word] & ((uint)1 << offset)) != 0;
    }

    internal static int GetHighestBitIndex(uint[] magnitude)
    {
        int word = magnitude.Length - 1;
        while (word >= 0 && magnitude[word] == 0) word--;
        if (word < 0) return -1;
        uint value = magnitude[word];
        int bit = 0;
        while (value > 1)
        {
            value >>= 1;
            bit++;
        }
        return word * 32 + bit;
    }

    internal static double GetNormalizedMantissa(uint[] magnitude, int highestBit)
    {
        int lowestBit = highestBit - 52;
        if (lowestBit < 0) lowestBit = 0;
        ulong leading = 0;
        int bit = highestBit;
        while (bit >= lowestBit)
        {
            leading <<= 1;
            int word = bit / 32;
            int offset = bit % 32;
            if ((magnitude[word] & ((uint)1 << offset)) != 0) leading |= 1;
            bit--;
        }
        int collectedBits = highestBit - lowestBit + 1;
        return Math.ScaleB((double)leading, 1 - collectedBits);
    }

    internal static void ValidateExactValue(ExactDyadicValue value, string description)
    {
        if (value == null || value.Magnitude == null)
        {
            throw new InvalidGeometryException(description + " is null.");
        }
    }
}
