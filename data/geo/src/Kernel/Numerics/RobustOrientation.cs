using System;

namespace geo;

internal static class RobustOrientation
{
    internal static int GetOrientationIndex(
        Coordinate first, Coordinate second, Coordinate third)
    {
        VectorMath.ValidateCoordinateValue(first, "First orientation coordinate");
        VectorMath.ValidateCoordinateValue(second, "Second orientation coordinate");
        VectorMath.ValidateCoordinateValue(third, "Third orientation coordinate");
        return GetOrientationIndexValues(
            first.X, first.Y, second.X, second.Y, third.X, third.Y);
    }

    internal static int GetOrientationIndexValues(
        double firstX, double firstY, double secondX, double secondY,
        double thirdX, double thirdY)
    {
        ValidatePlanarValue(firstX, "First X ordinate");
        ValidatePlanarValue(firstY, "First Y ordinate");
        ValidatePlanarValue(secondX, "Second X ordinate");
        ValidatePlanarValue(secondY, "Second Y ordinate");
        ValidatePlanarValue(thirdX, "Third X ordinate");
        ValidatePlanarValue(thirdY, "Third Y ordinate");

        double firstDeltaX = secondX - firstX;
        double firstDeltaY = secondY - firstY;
        double secondDeltaX = thirdX - firstX;
        double secondDeltaY = thirdY - firstY;
        if (double.IsFinite(firstDeltaX) && double.IsFinite(firstDeltaY) &&
            double.IsFinite(secondDeltaX) && double.IsFinite(secondDeltaY))
        {
            FloatingPointFilterResult filtered = FloatingPointFilter.EvaluateDeterminant2x2(
                firstDeltaX, firstDeltaY, secondDeltaX, secondDeltaY);
            if (filtered.Reliable) return filtered.Sign;
        }

        return GetExactOrientationSign(
            firstX, firstY, secondX, secondY, thirdX, thirdY);
    }

    internal static int GetExactOrientationSign(
        double firstX, double firstY, double secondX, double secondY,
        double thirdX, double thirdY)
    {
        ValidatePlanarValue(firstX, "First X ordinate");
        ValidatePlanarValue(firstY, "First Y ordinate");
        ValidatePlanarValue(secondX, "Second X ordinate");
        ValidatePlanarValue(secondY, "Second Y ordinate");
        ValidatePlanarValue(thirdX, "Third X ordinate");
        ValidatePlanarValue(thirdY, "Third Y ordinate");

        BinaryProduct[] products = CreateOrientationProducts(
            firstX, firstY, secondX, secondY, thirdX, thirdY);
        int[] coefficients = [1, -1, -1, 1, 1, -1];
        ExactDyadicValue value = ExactDyadicArithmetic.CreateProductSum(products, coefficients);
        return value.Sign;
    }

    internal static BinaryProduct[] CreateOrientationProducts(
        double firstX, double firstY, double secondX, double secondY,
        double thirdX, double thirdY)
    {
        return
        [
            RobustDeterminant.CreateBinaryProduct(secondX, thirdY),
            RobustDeterminant.CreateBinaryProduct(secondX, firstY),
            RobustDeterminant.CreateBinaryProduct(secondY, thirdX),
            RobustDeterminant.CreateBinaryProduct(secondY, firstX),
            RobustDeterminant.CreateBinaryProduct(firstY, thirdX),
            RobustDeterminant.CreateBinaryProduct(firstX, thirdY)
        ];
    }

    internal static int GetMinimumProductExponent(BinaryProduct[] products)
    {
        int minimum = int.MaxValue;
        int index = 0;
        while (index < products.Length)
        {
            if (products[index].Sign != 0 && products[index].Exponent < minimum)
            {
                minimum = products[index].Exponent;
            }
            index++;
        }
        return minimum;
    }

    internal static int GetAccumulatorLength(BinaryProduct[] products, int minimumExponent)
    {
        int maximumBit = 1;
        int index = 0;
        while (index < products.Length)
        {
            BinaryProduct product = products[index];
            if (product.Sign != 0)
            {
                int productTop = product.Exponent - minimumExponent +
                    RobustDeterminant.GetBitLength(product.Significand);
                if (productTop > maximumBit) maximumBit = productTop;
            }
            index++;
        }
        return (maximumBit + 31) / 32 + 1;
    }

    internal static void AddProductMagnitude(
        uint[] accumulator, BinaryProduct product, int minimumExponent)
    {
        UInt128 significand = product.Significand;
        int bit = product.Exponent - minimumExponent;
        while (significand != 0)
        {
            if ((significand & (UInt128)1) != 0)
            {
                AddPowerOfTwo(accumulator, bit);
            }
            significand >>= 1;
            bit++;
        }
    }

    internal static void AddPowerOfTwo(uint[] accumulator, int bit)
    {
        int word = bit / 32;
        int offset = bit % 32;
        ulong addition = (ulong)1 << offset;
        ulong sum = accumulator[word] + addition;
        accumulator[word] = (uint)(sum & uint.MaxValue);
        ulong carry = sum >> 32;
        word++;
        while (carry != 0)
        {
            sum = accumulator[word] + carry;
            accumulator[word] = (uint)(sum & uint.MaxValue);
            carry = sum >> 32;
            word++;
        }
    }

    internal static int CompareMagnitudes(uint[] first, uint[] second)
    {
        int index = first.Length - 1;
        while (index >= 0)
        {
            if (first[index] < second[index]) return -1;
            if (first[index] > second[index]) return 1;
            index--;
        }
        return 0;
    }

    internal static void ValidatePlanarValue(double value, string description)
    {
        if (!double.IsFinite(value))
        {
            throw new InvalidGeometryException(description + " must be finite.");
        }
    }
}
