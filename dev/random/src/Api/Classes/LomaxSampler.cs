using System;

namespace random;

/// <summary>
/// Generates double-precision values from a Lomax, or Pareto type II, distribution.
/// </summary>
/// <remarks>
/// This class implements Lomax/Pareto type II, not Pareto type I. For location
/// <c>mu</c>, scale <c>lambda</c>, and shape <c>a</c>, its support begins at
/// <c>mu</c> and samples are <c>mu + lambda*(u^(-1/a)-1)</c> for an open survival
/// probability <c>u</c>. In particular, the unshifted distribution begins at zero;
/// it does not include the type I offset of one. The sampler retains the supplied
/// mutable generator and is not safe for concurrent use without external synchronization.
/// </remarks>
public class LomaxSampler : ILomaxSampler
{
    IRandomSource generator;
    double location;
    double scale;
    double shape;
    RandomMathMode mathMode;

    /// <summary>
    /// Creates a shifted Lomax sampler with the requested location, scale, and shape.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits and selects the math mode.</param>
    /// <param name="location">The finite lower endpoint and distribution location.</param>
    /// <param name="scale">The finite nonnegative type II scale.</param>
    /// <param name="shape">The finite positive tail shape.</param>
    /// <remarks>
    /// A zero scale defines a point mass at the location and consumes no random state.
    /// Tail or location-scale overflow can return positive infinity.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the location or scale is non-finite, the scale is negative, or the shape is non-finite or not positive.</exception>
    public LomaxSampler(IRandomSource generator, double location, double scale, double shape)
    {
        ValidateParameters(generator, location, scale, shape);
        this.generator = generator;
        this.location = location;
        this.scale = scale;
        this.shape = shape;
        mathMode = generator.MathMode();
    }

    /// <summary>
    /// Returns one value from this sampler's Lomax/Pareto type II distribution.
    /// </summary>
    /// <returns>A value at or above the configured location, including a possible positive infinity after representational overflow.</returns>
    public double Sample()
    {
        if (scale == 0.0)
        {
            return location;
        }

        double unit = FloatingUniform.NextDoubleOpenOpen(generator);
        double exponent = -Log(unit) / shape;
        double excess = Expm1(exponent);
        return location + scale * excess;
    }

    /// <summary>
    /// Fills a destination with values from this sampler's Lomax/Pareto type II distribution.
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

    static void ValidateParameters(IRandomSource generator, double location, double scale, double shape)
    {
        Guard.NotNull(generator, "generator");
        Guard.Finite(location, "location");
        Guard.Finite(scale, "scale");
        if (scale < 0.0)
        {
            throw RandomArgumentException.Create("scale", "The scale must not be negative.");
        }

        Guard.Finite(shape, "shape");
        if (shape <= 0.0)
        {
            throw RandomArgumentException.Create("shape", "The shape must be positive.");
        }
    }
}
