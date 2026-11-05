using System;

namespace random;

/// <summary>
/// Generates double-precision values from a Pareto type I distribution.
/// </summary>
/// <remarks>
/// This class implements Pareto type I, not Lomax/Pareto type II. For positive
/// minimum <c>xm</c> and shape <c>a</c>, its support begins at <c>xm</c> and samples
/// are <c>xm*u^(-1/a)</c> for an open survival probability <c>u</c>. The parameter
/// named minimum is also called the Pareto type I scale or threshold. The sampler
/// retains the supplied mutable generator and is not safe for concurrent use without
/// external synchronization.
/// </remarks>
public class ParetoSampler : IParetoSampler
{
    IRandomSource generator;
    double minimum;
    double shape;
    RandomMathMode mathMode;

    /// <summary>
    /// Creates a Pareto type I sampler with the requested minimum and shape.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits and selects the math mode.</param>
    /// <param name="minimum">The finite positive lower endpoint, also called the type I scale or threshold.</param>
    /// <param name="shape">The finite positive tail shape.</param>
    /// <remarks>Tail or scaling overflow can return positive infinity.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null, or the minimum or shape is non-finite or not positive.</exception>
    public ParetoSampler(IRandomSource generator, double minimum, double shape)
    {
        ValidateParameters(generator, minimum, shape);
        this.generator = generator;
        this.minimum = minimum;
        this.shape = shape;
        mathMode = generator.MathMode();
    }

    /// <summary>
    /// Returns one value from this sampler's Pareto type I distribution.
    /// </summary>
    /// <returns>A value at or above the configured minimum, including a possible positive infinity after representational overflow.</returns>
    public double Sample()
    {
        double unit = FloatingUniform.NextDoubleOpenOpen(generator);
        double exponent = -Log(unit) / shape;
        double excess = Expm1(exponent);
        return minimum + minimum * excess;
    }

    /// <summary>
    /// Fills a destination with values from this sampler's Pareto type I distribution.
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

    double Log(double value)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Log(value);
        }

        return Math.Log(value);
    }

    double Expm1(double value)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Expm1(value);
        }

        if (value <= 0.5)
        {
            double half = value * 0.5;
            return 2.0 * Math.Exp(half) * Math.Sinh(half);
        }

        return Math.Exp(value) - 1.0;
    }

    static void ValidateParameters(IRandomSource generator, double minimum, double shape)
    {
        Guard.NotNull(generator, "generator");
        Guard.Finite(minimum, "minimum");
        if (minimum <= 0.0)
        {
            throw RandomArgumentException.Create("minimum", "The Pareto type I minimum must be positive.");
        }

        Guard.Finite(shape, "shape");
        if (shape <= 0.0)
        {
            throw RandomArgumentException.Create("shape", "The shape must be positive.");
        }
    }
}
