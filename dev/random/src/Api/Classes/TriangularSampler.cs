using System;

namespace random;

/// <summary>
/// Generates double-precision values from a triangular distribution.
/// </summary>
/// <remarks>
/// The distribution has finite bounds and a mode between them, inclusive. The inverse
/// transform evaluates the rising branch upward from the minimum and the falling branch
/// downward from the maximum, avoiding cancellation near either endpoint. A mode equal
/// to either bound selects the corresponding one-sided triangular density. The sampler
/// retains the supplied mutable generator and is not safe for concurrent use without
/// external synchronization.
/// </remarks>
public class TriangularSampler : ITriangularSampler
{
    IRandomSource generator;
    double minimum;
    double maximum;
    double width;
    double modeFraction;
    bool overflowingWidth;
    RandomMathMode mathMode;

    /// <summary>
    /// Creates a triangular sampler with the requested bounds and mode.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits and selects the math mode.</param>
    /// <param name="minimum">The finite inclusive lower bound.</param>
    /// <param name="maximum">The finite inclusive upper bound.</param>
    /// <param name="mode">The finite mode between the bounds, inclusive.</param>
    /// <remarks>
    /// Equal bounds require the same mode and define a point mass that consumes no random state.
    /// A mode equal to the minimum or maximum is valid and produces a one-sided triangular density.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator is null, a parameter is non-finite, the maximum is below the minimum, or the mode is outside the bounds.</exception>
    public TriangularSampler(IRandomSource generator, double minimum, double maximum, double mode)
    {
        ValidateParameters(generator, minimum, maximum, mode);
        this.generator = generator;
        Initialize(generator, minimum, maximum, mode);
    }

    /// <summary>
    /// Returns one value from this sampler's triangular distribution.
    /// </summary>
    /// <returns>A value between the configured bounds, or the exact bound for a point mass.</returns>
    public double Sample()
    {
        if (minimum == maximum)
        {
            return minimum;
        }

        double unit = FloatingUniform.NextDoubleClosedOpen(generator);
        if (unit == 0.0)
        {
            return minimum;
        }

        double result;
        if (unit < modeFraction)
        {
            double weight = Sqrt(unit * modeFraction);
            result = InterpolateFromMinimum(weight);
        }
        else
        {
            double complement = Sqrt((1.0 - unit) * (1.0 - modeFraction));
            result = InterpolateFromMaximum(complement);
        }

        if (result < minimum)
        {
            return minimum;
        }

        if (result > maximum)
        {
            return maximum;
        }

        return result;
    }

    /// <summary>
    /// Fills a destination with values from this sampler's triangular distribution.
    /// </summary>
    /// <param name="destination">The destination values.</param>
    /// <remarks>The result and generator state match repeated calls to <see cref="Sample()"/>.</remarks>
    public void Fill(Span<double> destination)
    {
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = Sample();
            index++;
        }
    }

    double InterpolateFromMinimum(double weight)
    {
        if (overflowingWidth)
        {
            return minimum * (1.0 - weight) + maximum * weight;
        }

        return minimum + width * weight;
    }

    double InterpolateFromMaximum(double complement)
    {
        if (overflowingWidth)
        {
            return minimum * complement + maximum * (1.0 - complement);
        }

        return maximum - width * complement;
    }

    double Sqrt(double value)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Sqrt(value);
        }

        return Math.Sqrt(value);
    }

    void Initialize(IRandomSource generator, double minimum, double maximum, double mode)
    {
        double selectedWidth = maximum - minimum;
        bool selectedOverflowingWidth = double.IsPositiveInfinity(selectedWidth);
        double selectedModeFraction;
        if (minimum == maximum)
        {
            selectedModeFraction = 0.5;
        }
        else if (selectedOverflowingWidth)
        {
            double halfMinimum = minimum * 0.5;
            double halfMaximum = maximum * 0.5;
            double halfMode = mode * 0.5;
            selectedModeFraction = (halfMode - halfMinimum) / (halfMaximum - halfMinimum);
        }
        else
        {
            selectedModeFraction = (mode - minimum) / selectedWidth;
        }

        this.minimum = minimum;
        this.maximum = maximum;
        width = selectedWidth;
        modeFraction = selectedModeFraction;
        overflowingWidth = selectedOverflowingWidth;
        mathMode = generator.MathMode();
    }

    static void ValidateParameters(IRandomSource generator, double minimum, double maximum, double mode)
    {
        Guard.NotNull(generator, "generator");
        Guard.Finite(minimum, "minimum");
        Guard.Finite(maximum, "maximum");
        if (maximum < minimum)
        {
            throw RandomArgumentException.Create("maximum", "The maximum must be greater than or equal to the minimum.");
        }

        Guard.Finite(mode, "mode");
        if (mode < minimum || mode > maximum)
        {
            throw RandomArgumentException.Create("mode", "The mode must be between the minimum and maximum inclusive.");
        }
    }
}
