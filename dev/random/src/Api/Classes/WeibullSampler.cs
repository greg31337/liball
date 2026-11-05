using System;

namespace random;

/// <summary>
/// Generates double-precision values from a Weibull distribution.
/// </summary>
/// <remarks>
/// For location <c>mu</c>, scale <c>lambda</c>, and shape <c>k</c>, samples are
/// <c>mu + lambda*(-log(u))^(1/k)</c> for an open survival probability <c>u</c>.
/// Using the survival probability directly avoids cancellation from forming <c>1-u</c>.
/// The sampler retains the supplied mutable generator and is not safe for concurrent
/// use without external synchronization.
/// </remarks>
public class WeibullSampler : IWeibullSampler
{
    IRandomSource generator;
    double location;
    double scale;
    double shape;
    RandomMathMode mathMode;

    /// <summary>
    /// Creates a Weibull sampler with the requested location, scale, and shape.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits and selects the math mode.</param>
    /// <param name="location">The finite lower endpoint and distribution location.</param>
    /// <param name="scale">The finite nonnegative distribution scale.</param>
    /// <param name="shape">The finite positive distribution shape.</param>
    /// <remarks>
    /// A zero scale defines a point mass at the location and consumes no random state.
    /// Transform underflow returns the location, while transform or location-scale overflow can return positive infinity.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the location or scale is non-finite, the scale is negative, or the shape is non-finite or not positive.</exception>
    public WeibullSampler(IRandomSource generator, double location, double scale, double shape)
    {
        ValidateParameters(generator, location, scale, shape);
        this.generator = generator;
        this.location = location;
        this.scale = scale;
        this.shape = shape;
        mathMode = generator.MathMode();
    }

    /// <summary>
    /// Returns one value from this sampler's Weibull distribution.
    /// </summary>
    /// <returns>A value at or above the location, including a possible positive infinity after representational overflow.</returns>
    public double Sample()
    {
        if (scale == 0.0)
        {
            return location;
        }

        double unit = FloatingUniform.NextDoubleOpenOpen(generator);
        double exponential = -Log(unit);
        double standard;
        if (shape == 1.0)
        {
            standard = exponential;
        }
        else
        {
            standard = Exp(Log(exponential) / shape);
        }

        return location + scale * standard;
    }

    /// <summary>
    /// Fills a destination with values from this sampler's Weibull distribution.
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

    double Exp(double value)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Exp(value);
        }

        return Math.Exp(value);
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
