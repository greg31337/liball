using System;

namespace random;

/// <summary>
/// Defines uniformity diagnostics for point-major samples in a unit hypercube.
/// </summary>
public interface IDiscrepancyCalculator
{
    /// <summary>Computes centered discrepancy from an array.</summary>
    double Centered(double[] points, int pointCount, int dimension);

    /// <summary>Computes centered discrepancy from a read-only span.</summary>
    double CenteredSpan(ReadOnlySpan<double> points, int pointCount, int dimension);

    /// <summary>Computes anchored L2-star discrepancy from an array.</summary>
    double L2Star(double[] points, int pointCount, int dimension);

    /// <summary>Computes anchored L2-star discrepancy from a read-only span.</summary>
    double L2StarSpan(ReadOnlySpan<double> points, int pointCount, int dimension);

    /// <summary>Computes exact anchored star discrepancy within a candidate-box budget.</summary>
    double ExactStar(double[] points, int pointCount, int dimension, long maximumBoxes);

    /// <summary>Computes exact anchored star discrepancy from a span within a candidate-box budget.</summary>
    double ExactStarSpan(ReadOnlySpan<double> points, int pointCount, int dimension, long maximumBoxes);
}
