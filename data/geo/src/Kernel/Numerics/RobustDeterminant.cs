using System;

namespace geo;

internal class BinaryProduct
{
    internal int Sign;
    internal UInt128 Significand;
    internal int Exponent;
}

internal static class RobustDeterminant
{
    internal const ulong FractionMask = 0x000fffffffffffffUL;
    internal const ulong ExponentMask = 0x7ffUL;
    internal const ulong HiddenBit = 0x0010000000000000UL;

    internal static int GetSign2x2(
        double firstX, double firstY, double secondX, double secondY)
    {
        FloatingPointFilterResult filtered = FloatingPointFilter.EvaluateDeterminant2x2(
            firstX, firstY, secondX, secondY);
        if (filtered.Reliable) return filtered.Sign;
        if (ExpansionArithmetic.CanCreateProductDifference(
            firstX, firstY, secondX, secondY))
        {
            double[] expansion = ExpansionArithmetic.CreateProductDifference(
                firstX, firstY, secondX, secondY);
            return ExpansionArithmetic.GetExpansionSign(expansion);
        }
        return GetExactProductDifferenceSign(firstX, firstY, secondX, secondY);
    }

    internal static double GetEstimate2x2(
        double firstX, double firstY, double secondX, double secondY)
    {
        FloatingPointFilterResult filtered = FloatingPointFilter.EvaluateDeterminant2x2(
            firstX, firstY, secondX, secondY);
        return filtered.Estimate;
    }

    internal static DoubleDouble GetDoubleDoubleValue2x2(
        double firstX, double firstY, double secondX, double secondY)
    {
        DoubleDouble left = new DoubleDouble(firstX, 0.0).MultiplyValue(
            new DoubleDouble(secondY, 0.0));
        DoubleDouble right = new DoubleDouble(firstY, 0.0).MultiplyValue(
            new DoubleDouble(secondX, 0.0));
        return left.SubtractValue(right);
    }

    internal static int GetExactProductDifferenceSign(
        double firstX, double firstY, double secondX, double secondY)
    {
        FloatingPointFilter.ValidateFiniteInput(firstX, "First X value");
        FloatingPointFilter.ValidateFiniteInput(firstY, "First Y value");
        FloatingPointFilter.ValidateFiniteInput(secondX, "Second X value");
        FloatingPointFilter.ValidateFiniteInput(secondY, "Second Y value");
        BinaryProduct left = CreateBinaryProduct(firstX, secondY);
        BinaryProduct right = CreateBinaryProduct(firstY, secondX);
        if (left.Sign == 0) return -right.Sign;
        if (right.Sign == 0) return left.Sign;
        if (left.Sign != right.Sign) return left.Sign;
        int magnitude = CompareProductMagnitudes(left, right);
        return left.Sign > 0 ? magnitude : -magnitude;
    }

    internal static BinaryProduct CreateBinaryProduct(double first, double second)
    {
        BinaryProduct product = new BinaryProduct();
        if (first == 0.0 || second == 0.0) return product;
        product.Sign = (first < 0.0) == (second < 0.0) ? 1 : -1;
        ulong firstBits = (ulong)BitConverter.DoubleToInt64Bits(Math.Abs(first));
        ulong secondBits = (ulong)BitConverter.DoubleToInt64Bits(Math.Abs(second));
        ulong firstFraction = firstBits & FractionMask;
        ulong secondFraction = secondBits & FractionMask;
        int firstEncodedExponent = (int)(firstBits >> 52 & ExponentMask);
        int secondEncodedExponent = (int)(secondBits >> 52 & ExponentMask);
        ulong firstSignificand = firstFraction;
        ulong secondSignificand = secondFraction;
        int firstExponent = -1074;
        int secondExponent = -1074;
        if (firstEncodedExponent != 0)
        {
            firstSignificand |= HiddenBit;
            firstExponent = firstEncodedExponent - 1075;
        }
        if (secondEncodedExponent != 0)
        {
            secondSignificand |= HiddenBit;
            secondExponent = secondEncodedExponent - 1075;
        }
        product.Significand = (UInt128)firstSignificand * secondSignificand;
        product.Exponent = firstExponent + secondExponent;
        return product;
    }

    internal static int CompareProductMagnitudes(BinaryProduct first, BinaryProduct second)
    {
        int firstBits = GetBitLength(first.Significand);
        int secondBits = GetBitLength(second.Significand);
        int firstTop = first.Exponent + firstBits;
        int secondTop = second.Exponent + secondBits;
        if (firstTop < secondTop) return -1;
        if (firstTop > secondTop) return 1;
        UInt128 firstAligned = first.Significand;
        UInt128 secondAligned = second.Significand;
        if (first.Exponent < second.Exponent)
        {
            secondAligned <<= second.Exponent - first.Exponent;
        }
        else if (second.Exponent < first.Exponent)
        {
            firstAligned <<= first.Exponent - second.Exponent;
        }
        if (firstAligned < secondAligned) return -1;
        if (firstAligned > secondAligned) return 1;
        return 0;
    }

    internal static int GetBitLength(UInt128 value)
    {
        int length = 0;
        while (value != 0)
        {
            value >>= 1;
            length++;
        }
        return length;
    }
}
