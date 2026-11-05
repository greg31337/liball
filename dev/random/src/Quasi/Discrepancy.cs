using System;

namespace random;

/// <summary>
/// Computes uniformity diagnostics for point-major samples in a unit hypercube.
/// </summary>
/// <remarks>
/// Centered discrepancy and <c>L2Star</c> use quadratic closed forms and require
/// work proportional to the square of the point count. Exact anchored star
/// discrepancy enumerates a finite coordinate grid and is exponential in the
/// dimension, so its overloads enforce an explicit candidate-box budget.
/// </remarks>
internal static class Discrepancy
{
    const long DefaultStarBoxBudget = 10000000L;

    /// <summary>
    /// Computes centered discrepancy for a point-major sample.
    /// </summary>
    /// <param name="points">The non-null point-major coordinates.</param>
    /// <param name="pointCount">The positive number of points.</param>
    /// <param name="dimension">The positive number of coordinates per point.</param>
    /// <returns>The conventional centered-discrepancy value without taking a square root.</returns>
    /// <exception cref="RandomArgumentException">The sample shape is invalid or a coordinate lies outside the unit hypercube.</exception>
    public static double Centered(double[] points, int pointCount, int dimension)
    {
        Guard.NotNull(points, "points");
        return Centered(points.AsSpan(), pointCount, dimension);
    }

    /// <summary>
    /// Computes centered discrepancy for a point-major sample.
    /// </summary>
    /// <param name="points">The exact point-major coordinates.</param>
    /// <param name="pointCount">The positive number of points.</param>
    /// <param name="dimension">The positive number of coordinates per point.</param>
    /// <returns>The conventional centered-discrepancy value without taking a square root.</returns>
    /// <exception cref="RandomArgumentException">The sample shape is invalid or a coordinate lies outside the unit hypercube.</exception>
    public static double Centered(ReadOnlySpan<double> points, int pointCount, int dimension)
    {
        ValidatePoints(points, pointCount, dimension);
        double firstSum = 0.0;
        int firstPoint = 0;
        while (firstPoint < pointCount)
        {
            double product = 1.0;
            int coordinate = 0;
            while (coordinate < dimension)
            {
                double centeredDistance = Math.Abs(points[firstPoint * dimension + coordinate] - 0.5);
                product *= 1.0 + 0.5 * centeredDistance - 0.5 * centeredDistance * centeredDistance;
                coordinate++;
            }

            firstSum += product;
            firstPoint++;
        }

        double pairSum = 0.0;
        firstPoint = 0;
        while (firstPoint < pointCount)
        {
            int secondPoint = 0;
            while (secondPoint < pointCount)
            {
                double product = 1.0;
                int coordinate = 0;
                while (coordinate < dimension)
                {
                    double firstValue = points[firstPoint * dimension + coordinate];
                    double secondValue = points[secondPoint * dimension + coordinate];
                    product *= 1.0 + 0.5 * Math.Abs(firstValue - 0.5) +
                        0.5 * Math.Abs(secondValue - 0.5) - 0.5 * Math.Abs(firstValue - secondValue);
                    coordinate++;
                }

                pairSum += product;
                secondPoint++;
            }

            firstPoint++;
        }

        double count = pointCount;
        double result = Power(13.0 / 12.0, dimension) - 2.0 * firstSum / count + pairSum / (count * count);
        if (result < 0.0)
        {
            return 0.0;
        }

        return result;
    }

    /// <summary>
    /// Computes the anchored <c>L2</c>-star discrepancy for a point-major sample.
    /// </summary>
    /// <param name="points">The non-null point-major coordinates.</param>
    /// <param name="pointCount">The positive number of points.</param>
    /// <param name="dimension">The positive number of coordinates per point.</param>
    /// <returns>The nonnegative square root of the integrated squared anchored discrepancy.</returns>
    /// <exception cref="RandomArgumentException">The sample shape is invalid or a coordinate lies outside the unit hypercube.</exception>
    public static double L2Star(double[] points, int pointCount, int dimension)
    {
        Guard.NotNull(points, "points");
        return L2Star(points.AsSpan(), pointCount, dimension);
    }

    /// <summary>
    /// Computes the anchored <c>L2</c>-star discrepancy for a point-major sample.
    /// </summary>
    /// <param name="points">The exact point-major coordinates.</param>
    /// <param name="pointCount">The positive number of points.</param>
    /// <param name="dimension">The positive number of coordinates per point.</param>
    /// <returns>The nonnegative square root of the integrated squared anchored discrepancy.</returns>
    /// <exception cref="RandomArgumentException">The sample shape is invalid or a coordinate lies outside the unit hypercube.</exception>
    public static double L2Star(ReadOnlySpan<double> points, int pointCount, int dimension)
    {
        ValidatePoints(points, pointCount, dimension);
        double firstSum = 0.0;
        int firstPoint = 0;
        while (firstPoint < pointCount)
        {
            double product = 1.0;
            int coordinate = 0;
            while (coordinate < dimension)
            {
                double value = points[firstPoint * dimension + coordinate];
                product *= 1.0 - value * value;
                coordinate++;
            }

            firstSum += product;
            firstPoint++;
        }

        double pairSum = 0.0;
        firstPoint = 0;
        while (firstPoint < pointCount)
        {
            int secondPoint = 0;
            while (secondPoint < pointCount)
            {
                double product = 1.0;
                int coordinate = 0;
                while (coordinate < dimension)
                {
                    double firstValue = points[firstPoint * dimension + coordinate];
                    double secondValue = points[secondPoint * dimension + coordinate];
                    product *= 1.0 - Math.Max(firstValue, secondValue);
                    coordinate++;
                }

                pairSum += product;
                secondPoint++;
            }

            firstPoint++;
        }

        double count = pointCount;
        double squared = Power(1.0 / 3.0, dimension) -
            2.0 * Power(0.5, dimension) * firstSum / count + pairSum / (count * count);
        if (squared < 0.0)
        {
            squared = 0.0;
        }

        return Math.Sqrt(squared);
    }

    /// <summary>
    /// Computes exact anchored <c>L-infinity</c> star discrepancy using the default candidate-box budget.
    /// </summary>
    /// <param name="points">The non-null point-major coordinates.</param>
    /// <param name="pointCount">The positive number of points.</param>
    /// <param name="dimension">The positive number of coordinates per point.</param>
    /// <returns>The maximum absolute empirical-volume difference over anchored boxes.</returns>
    /// <exception cref="RandomArgumentException">The sample shape is invalid or a coordinate lies outside the unit hypercube.</exception>
    /// <exception cref="RandomExhaustedException">More than ten million candidate boxes are required.</exception>
    public static double Star(double[] points, int pointCount, int dimension)
    {
        Guard.NotNull(points, "points");
        return Star(points.AsSpan(), pointCount, dimension, DefaultStarBoxBudget);
    }

    /// <summary>
    /// Computes exact anchored <c>L-infinity</c> star discrepancy using the default candidate-box budget.
    /// </summary>
    /// <param name="points">The exact point-major coordinates.</param>
    /// <param name="pointCount">The positive number of points.</param>
    /// <param name="dimension">The positive number of coordinates per point.</param>
    /// <returns>The maximum absolute empirical-volume difference over anchored boxes.</returns>
    /// <exception cref="RandomArgumentException">The sample shape is invalid or a coordinate lies outside the unit hypercube.</exception>
    /// <exception cref="RandomExhaustedException">More than ten million candidate boxes are required.</exception>
    public static double Star(ReadOnlySpan<double> points, int pointCount, int dimension)
    {
        return Star(points, pointCount, dimension, DefaultStarBoxBudget);
    }

    /// <summary>
    /// Computes exact anchored <c>L-infinity</c> star discrepancy within a candidate-box budget.
    /// </summary>
    /// <param name="points">The non-null point-major coordinates.</param>
    /// <param name="pointCount">The positive number of points.</param>
    /// <param name="dimension">The positive number of coordinates per point.</param>
    /// <param name="maximumBoxes">The positive maximum number of candidate boxes to evaluate.</param>
    /// <returns>The maximum absolute empirical-volume difference over anchored boxes.</returns>
    /// <exception cref="RandomArgumentException">The sample shape, a coordinate, or the box budget is invalid.</exception>
    /// <exception cref="RandomExhaustedException">The exact candidate grid exceeds the supplied budget.</exception>
    public static double Star(double[] points, int pointCount, int dimension, long maximumBoxes)
    {
        Guard.NotNull(points, "points");
        return Star(points.AsSpan(), pointCount, dimension, maximumBoxes);
    }

    /// <summary>
    /// Computes exact anchored <c>L-infinity</c> star discrepancy within a candidate-box budget.
    /// </summary>
    /// <param name="points">The exact point-major coordinates.</param>
    /// <param name="pointCount">The positive number of points.</param>
    /// <param name="dimension">The positive number of coordinates per point.</param>
    /// <param name="maximumBoxes">The positive maximum number of candidate boxes to evaluate.</param>
    /// <returns>The maximum absolute empirical-volume difference over anchored boxes.</returns>
    /// <exception cref="RandomArgumentException">The sample shape, a coordinate, or the box budget is invalid.</exception>
    /// <exception cref="RandomExhaustedException">The exact candidate grid exceeds the supplied budget.</exception>
    public static double Star(ReadOnlySpan<double> points, int pointCount, int dimension, long maximumBoxes)
    {
        ValidatePoints(points, pointCount, dimension);
        if (maximumBoxes <= 0L)
        {
            throw RandomArgumentException.Create("maximumBoxes", "The candidate-box budget must be positive.");
        }

        double[][] candidates = CandidateCoordinates(points, pointCount, dimension);
        ValidateCandidateBudget(candidates, maximumBoxes);
        int[] indexes = new int[dimension];
        double maximum = 0.0;
        while (true)
        {
            double volume = 1.0;
            int coordinate = 0;
            while (coordinate < dimension)
            {
                volume *= candidates[coordinate][indexes[coordinate]];
                coordinate++;
            }

            int closedCount = 0;
            int openCount = 0;
            int point = 0;
            while (point < pointCount)
            {
                bool closed = true;
                bool open = true;
                coordinate = 0;
                while (coordinate < dimension)
                {
                    double value = points[point * dimension + coordinate];
                    double upper = candidates[coordinate][indexes[coordinate]];
                    if (value >= upper)
                    {
                        open = false;
                    }

                    if (value > upper || upper >= 1.0 && value >= 1.0)
                    {
                        closed = false;
                    }

                    coordinate++;
                }

                if (closed)
                {
                    closedCount++;
                }

                if (open)
                {
                    openCount++;
                }

                point++;
            }

            double closedExcess = (double)closedCount / (double)pointCount - volume;
            double openDeficit = volume - (double)openCount / (double)pointCount;
            if (closedExcess > maximum)
            {
                maximum = closedExcess;
            }

            if (openDeficit > maximum)
            {
                maximum = openDeficit;
            }

            coordinate = dimension - 1;
            while (coordinate >= 0)
            {
                indexes[coordinate]++;
                if (indexes[coordinate] < candidates[coordinate].Length)
                {
                    break;
                }

                indexes[coordinate] = 0;
                coordinate--;
            }

            if (coordinate < 0)
            {
                return maximum;
            }
        }
    }

    static void ValidatePoints(ReadOnlySpan<double> points, int pointCount, int dimension)
    {
        int requiredLength = Guard.MatrixElementCount(pointCount, dimension, "pointCount", "dimension");
        Guard.ExactSpanLength(points.Length, requiredLength, "points");
        int index = 0;
        while (index < points.Length)
        {
            double value = points[index];
            if (!double.IsFinite(value) || value < 0.0 || value > 1.0)
            {
                throw RandomArgumentException.Create("points", "Every coordinate must be finite and between zero and one inclusive.");
            }

            index++;
        }
    }

    static double[][] CandidateCoordinates(ReadOnlySpan<double> points, int pointCount, int dimension)
    {
        double[][] result = new double[dimension][];
        int coordinate = 0;
        while (coordinate < dimension)
        {
            double[] work = new double[pointCount + 1];
            int point = 0;
            while (point < pointCount)
            {
                work[point] = points[point * dimension + coordinate];
                point++;
            }

            work[pointCount] = 1.0;
            Array.Sort(work);
            int uniqueCount = 1;
            int index = 1;
            while (index < work.Length)
            {
                if (work[index] != work[index - 1])
                {
                    uniqueCount++;
                }

                index++;
            }

            double[] unique = new double[uniqueCount];
            unique[0] = work[0];
            int uniqueIndex = 1;
            index = 1;
            while (index < work.Length)
            {
                if (work[index] != work[index - 1])
                {
                    unique[uniqueIndex] = work[index];
                    uniqueIndex++;
                }

                index++;
            }

            result[coordinate] = unique;
            coordinate++;
        }

        return result;
    }

    static void ValidateCandidateBudget(double[][] candidates, long maximumBoxes)
    {
        long count = 1L;
        int coordinate = 0;
        while (coordinate < candidates.Length)
        {
            int coordinateCount = candidates[coordinate].Length;
            if (count > maximumBoxes / coordinateCount)
            {
                throw RandomExhaustedException.Create("The exact star-discrepancy candidate grid exceeds the candidate-box budget.");
            }

            count *= coordinateCount;
            coordinate++;
        }
    }

    static double Power(double value, int exponent)
    {
        double result = 1.0;
        int index = 0;
        while (index < exponent)
        {
            result *= value;
            index++;
        }

        return result;
    }
}
