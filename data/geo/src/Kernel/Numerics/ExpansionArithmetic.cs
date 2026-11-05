using System;

namespace geo;

internal static class ExpansionArithmetic
{
    internal const double Splitter = 134217729.0;
    internal const double MinimumSafeProduct = 1.0e-290;

    internal static double[] CreateTwoSum(double first, double second)
    {
        ValidateFiniteTerm(first);
        ValidateFiniteTerm(second);
        double sum = first + second;
        if (!double.IsFinite(sum))
        {
            throw new InvalidGeometryException("Expansion sum is outside the finite double range.");
        }
        double virtualSecond = sum - first;
        double error = second - virtualSecond + (first - (sum - virtualSecond));
        return EliminateZeroPair(error, sum);
    }

    internal static double[] CreateTwoDifference(double first, double second)
    {
        ValidateFiniteTerm(first);
        ValidateFiniteTerm(second);
        double difference = first - second;
        if (!double.IsFinite(difference))
        {
            throw new InvalidGeometryException("Expansion difference is outside the finite double range.");
        }
        double virtualSecond = first - difference;
        double error = first - (difference + virtualSecond) + (virtualSecond - second);
        return EliminateZeroPair(error, difference);
    }

    internal static double[] CreateTwoProduct(double first, double second)
    {
        ValidateFiniteTerm(first);
        ValidateFiniteTerm(second);
        double product = first * second;
        if (!CanCreateExactProduct(first, second, product))
        {
            throw new InvalidGeometryException(
                "Expansion product requires finite, normally scaled operands and output.");
        }
        double[] firstParts = CreateSplit(first);
        double[] secondParts = CreateSplit(second);
        double error = ((firstParts[1] * secondParts[1] - product) +
            firstParts[1] * secondParts[0] + firstParts[0] * secondParts[1]) +
            firstParts[0] * secondParts[0];
        return EliminateZeroPair(error, product);
    }

    internal static double[] CreateSplit(double value)
    {
        ValidateFiniteTerm(value);
        double combined = Splitter * value;
        if (!double.IsFinite(combined))
        {
            throw new InvalidGeometryException("Expansion split operand is too large.");
        }
        double high = combined - (combined - value);
        double low = value - high;
        return [low, high];
    }

    internal static double[] GrowExpansion(double[] expansion, double value)
    {
        ValidateExpansion(expansion);
        ValidateFiniteTerm(value);
        double[] output = new double[expansion.Length + 1];
        double accumulator = value;
        int outputCount = 0;
        int index = 0;
        while (index < expansion.Length)
        {
            double[] pair = CreateTwoSum(accumulator, expansion[index]);
            if (pair.Length == 2)
            {
                output[outputCount] = pair[0];
                outputCount++;
                accumulator = pair[1];
            }
            else
            {
                accumulator = pair[0];
            }
            index++;
        }
        if (accumulator != 0.0 || outputCount == 0)
        {
            output[outputCount] = accumulator;
            outputCount++;
        }
        return CopyPrefix(output, outputCount);
    }

    internal static double[] AddExpansions(double[] first, double[] second)
    {
        ValidateExpansion(first);
        ValidateExpansion(second);
        double[] terms = new double[first.Length + second.Length];
        first.CopyTo(terms, 0);
        second.CopyTo(terms, first.Length);
        return CompressExpansion(terms);
    }

    internal static double[] SubtractExpansions(double[] first, double[] second)
    {
        ValidateExpansion(first);
        ValidateExpansion(second);
        double[] negated = new double[second.Length];
        int index = 0;
        while (index < second.Length)
        {
            negated[index] = -second[index];
            index++;
        }
        return AddExpansions(first, negated);
    }

    internal static double[] ScaleExpansion(double[] expansion, double factor)
    {
        ValidateExpansion(expansion);
        ValidateFiniteTerm(factor);
        if (factor == 0.0) return [0.0];
        double[] result = [0.0];
        int index = 0;
        while (index < expansion.Length)
        {
            double[] product = CreateTwoProduct(expansion[index], factor);
            result = AddExpansions(result, product);
            index++;
        }
        return result;
    }

    internal static double[] MultiplyExpansions(double[] first, double[] second)
    {
        ValidateExpansion(first);
        ValidateExpansion(second);
        double[] result = [0.0];
        int index = 0;
        while (index < second.Length)
        {
            double[] scaled = ScaleExpansion(first, second[index]);
            result = AddExpansions(result, scaled);
            index++;
        }
        return result;
    }

    internal static double[] CreateProductDifference(
        double firstX, double firstY, double secondX, double secondY)
    {
        double[] left = CreateTwoProduct(firstX, secondY);
        double[] right = CreateTwoProduct(firstY, secondX);
        return SubtractExpansions(left, right);
    }

    internal static double[] CompressExpansion(double[] expansion)
    {
        ValidateExpansion(expansion);
        double[] sorted = new double[expansion.Length];
        expansion.CopyTo(sorted, 0);
        Array.Sort(sorted, (first, second) => Math.Abs(first).CompareTo(Math.Abs(second)));
        double[] result = [0.0];
        int index = 0;
        while (index < sorted.Length)
        {
            result = GrowExpansion(result, sorted[index]);
            index++;
        }
        return result;
    }

    internal static double EstimateExpansion(double[] expansion)
    {
        ValidateExpansion(expansion);
        double estimate = 0.0;
        int index = 0;
        while (index < expansion.Length)
        {
            estimate += expansion[index];
            index++;
        }
        return estimate;
    }

    internal static int GetExpansionSign(double[] expansion)
    {
        ValidateExpansion(expansion);
        int index = expansion.Length - 1;
        while (index >= 0)
        {
            if (expansion[index] > 0.0) return 1;
            if (expansion[index] < 0.0) return -1;
            index--;
        }
        return 0;
    }

    internal static bool CanCreateExactProduct(
        double first, double second, double product)
    {
        if (!double.IsFinite(first) || !double.IsFinite(second) || !double.IsFinite(product))
        {
            return false;
        }
        if (first == 0.0 || second == 0.0) return true;
        if (Math.Abs(first) > double.MaxValue / Splitter) return false;
        if (Math.Abs(second) > double.MaxValue / Splitter) return false;
        return Math.Abs(product) >= MinimumSafeProduct;
    }

    internal static bool CanCreateProductDifference(
        double firstX, double firstY, double secondX, double secondY)
    {
        double left = firstX * secondY;
        double right = firstY * secondX;
        return CanCreateExactProduct(firstX, secondY, left) &&
            CanCreateExactProduct(firstY, secondX, right);
    }

    internal static double[] EliminateZeroPair(double low, double high)
    {
        if (low != 0.0) return [low, high];
        return [high];
    }

    internal static double[] CopyPrefix(double[] source, int count)
    {
        double[] result = new double[count];
        int index = 0;
        while (index < count)
        {
            result[index] = source[index];
            index++;
        }
        return result;
    }

    internal static void ValidateExpansion(double[] expansion)
    {
        if (expansion == null || expansion.Length == 0)
        {
            throw new InvalidGeometryException("A floating-point expansion cannot be null or empty.");
        }
        int index = 0;
        while (index < expansion.Length)
        {
            ValidateFiniteTerm(expansion[index]);
            index++;
        }
    }

    internal static void ValidateFiniteTerm(double value)
    {
        if (!double.IsFinite(value))
        {
            throw new InvalidGeometryException("Floating-point expansion terms must be finite.");
        }
    }
}
