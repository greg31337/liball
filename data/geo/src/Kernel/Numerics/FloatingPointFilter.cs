using System;

namespace geo;

internal class FloatingPointFilterResult
{
    internal bool Reliable;
    internal int Sign;
    internal double Estimate;
    internal double ErrorBound;
}

internal static class FloatingPointFilter
{
    internal const double DeterminantErrorCoefficient = 3.3306690738754716e-16;

    internal static FloatingPointFilterResult EvaluateDeterminant2x2(
        double firstX, double firstY, double secondX, double secondY)
    {
        ValidateFiniteInput(firstX, "First X value");
        ValidateFiniteInput(firstY, "First Y value");
        ValidateFiniteInput(secondX, "Second X value");
        ValidateFiniteInput(secondY, "Second Y value");
        double left = firstX * secondY;
        double right = firstY * secondX;
        double determinant = left - right;
        FloatingPointFilterResult result = new FloatingPointFilterResult();
        result.Estimate = determinant;
        if (!double.IsFinite(left) || !double.IsFinite(right) || !double.IsFinite(determinant))
        {
            result.ErrorBound = double.PositiveInfinity;
            return result;
        }
        double productSum = Math.Abs(left) + Math.Abs(right);
        if (!double.IsFinite(productSum))
        {
            result.ErrorBound = double.PositiveInfinity;
            return result;
        }
        result.ErrorBound = DeterminantErrorCoefficient * productSum;
        if (determinant > result.ErrorBound)
        {
            result.Reliable = true;
            result.Sign = 1;
            return result;
        }
        if (determinant < -result.ErrorBound)
        {
            result.Reliable = true;
            result.Sign = -1;
            return result;
        }
        if (productSum == 0.0 && ProductsAreExactlyZero(
            firstX, firstY, secondX, secondY))
        {
            result.Reliable = true;
        }
        return result;
    }

    internal static bool ProductsAreExactlyZero(
        double firstX, double firstY, double secondX, double secondY)
    {
        bool leftZero = firstX == 0.0 || secondY == 0.0;
        bool rightZero = firstY == 0.0 || secondX == 0.0;
        return leftZero && rightZero;
    }

    internal static void ValidateFiniteInput(double value, string description)
    {
        if (!double.IsFinite(value))
        {
            throw new InvalidGeometryException(description + " must be finite.");
        }
    }
}
