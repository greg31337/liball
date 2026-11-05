using System;

namespace random;

/// <summary>
/// Computes uniformity diagnostics for point-major samples in a unit hypercube.
/// </summary>
/// <remarks>
/// Centered and L2-star discrepancy use quadratic closed forms. Exact anchored star
/// discrepancy enumerates a finite coordinate grid and rejects requests whose candidate
/// count exceeds the caller-supplied budget.
/// </remarks>
public class DiscrepancyCalculator : IDiscrepancyCalculator
{
    /// <summary>Creates a discrepancy calculator.</summary>
    public DiscrepancyCalculator()
    {
    }

    /// <summary>Computes centered discrepancy for point-major coordinates.</summary>
    /// <param name="points">The exact point-major coordinate array.</param>
    /// <param name="pointCount">The positive number of points.</param>
    /// <param name="dimension">The positive number of coordinates per point.</param>
    /// <returns>The conventional centered-discrepancy value without a square root.</returns>
    /// <exception cref="RandomArgumentException">The shape is invalid or a coordinate lies outside the unit hypercube.</exception>
    public double Centered(double[] points, int pointCount, int dimension)
    {
        return Discrepancy.Centered(points, pointCount, dimension);
    }

    /// <summary>Computes centered discrepancy for point-major coordinates.</summary>
    /// <param name="points">The exact point-major coordinate span.</param>
    /// <param name="pointCount">The positive number of points.</param>
    /// <param name="dimension">The positive number of coordinates per point.</param>
    /// <returns>The conventional centered-discrepancy value without a square root.</returns>
    /// <exception cref="RandomArgumentException">The shape is invalid or a coordinate lies outside the unit hypercube.</exception>
    public double CenteredSpan(ReadOnlySpan<double> points, int pointCount, int dimension)
    {
        return Discrepancy.Centered(points, pointCount, dimension);
    }

    /// <summary>Computes anchored L2-star discrepancy for point-major coordinates.</summary>
    /// <param name="points">The exact point-major coordinate array.</param>
    /// <param name="pointCount">The positive number of points.</param>
    /// <param name="dimension">The positive number of coordinates per point.</param>
    /// <returns>The nonnegative square root of integrated squared anchored discrepancy.</returns>
    /// <exception cref="RandomArgumentException">The shape is invalid or a coordinate lies outside the unit hypercube.</exception>
    public double L2Star(double[] points, int pointCount, int dimension)
    {
        return Discrepancy.L2Star(points, pointCount, dimension);
    }

    /// <summary>Computes anchored L2-star discrepancy for point-major coordinates.</summary>
    /// <param name="points">The exact point-major coordinate span.</param>
    /// <param name="pointCount">The positive number of points.</param>
    /// <param name="dimension">The positive number of coordinates per point.</param>
    /// <returns>The nonnegative square root of integrated squared anchored discrepancy.</returns>
    /// <exception cref="RandomArgumentException">The shape is invalid or a coordinate lies outside the unit hypercube.</exception>
    public double L2StarSpan(ReadOnlySpan<double> points, int pointCount, int dimension)
    {
        return Discrepancy.L2Star(points, pointCount, dimension);
    }

    /// <summary>Computes exact anchored star discrepancy within a candidate-box budget.</summary>
    /// <param name="points">The exact point-major coordinate array.</param>
    /// <param name="pointCount">The positive number of points.</param>
    /// <param name="dimension">The positive number of coordinates per point.</param>
    /// <param name="maximumBoxes">The positive maximum candidate-box count.</param>
    /// <returns>The maximum absolute empirical-volume difference over anchored boxes.</returns>
    /// <exception cref="RandomArgumentException">The shape, a coordinate, or the budget is invalid.</exception>
    /// <exception cref="RandomExhaustedException">The exact candidate grid exceeds the supplied budget.</exception>
    public double ExactStar(double[] points, int pointCount, int dimension, long maximumBoxes)
    {
        return Discrepancy.Star(points, pointCount, dimension, maximumBoxes);
    }

    /// <summary>Computes exact anchored star discrepancy within a candidate-box budget.</summary>
    /// <param name="points">The exact point-major coordinate span.</param>
    /// <param name="pointCount">The positive number of points.</param>
    /// <param name="dimension">The positive number of coordinates per point.</param>
    /// <param name="maximumBoxes">The positive maximum candidate-box count.</param>
    /// <returns>The maximum absolute empirical-volume difference over anchored boxes.</returns>
    /// <exception cref="RandomArgumentException">The shape, a coordinate, or the budget is invalid.</exception>
    /// <exception cref="RandomExhaustedException">The exact candidate grid exceeds the supplied budget.</exception>
    public double ExactStarSpan(ReadOnlySpan<double> points, int pointCount, int dimension, long maximumBoxes)
    {
        return Discrepancy.Star(points, pointCount, dimension, maximumBoxes);
    }
}
