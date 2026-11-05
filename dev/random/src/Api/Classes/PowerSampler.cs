using System;

namespace random;

/// <summary>
/// Generates double-precision values from a bounded power distribution.
/// </summary>
/// <remarks>
/// The standard distribution is beta with shapes <c>(a,1)</c>: for positive shape
/// <c>a</c>, samples are <c>u^(1/a)</c> on the unit interval. Location and scale map
/// that value to <c>location + scale*u^(1/a)</c>. The sampler retains the supplied
/// mutable generator and is not safe for concurrent use without external synchronization.
/// </remarks>
public class PowerSampler : IPowerSampler
{
    IRandomSource generator;
    double location;
    double scale;
    double shape;
    RandomMathMode mathMode;

    /// <summary>
    /// Creates a power sampler with the requested location, scale, and shape.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits and selects the math mode.</param>
    /// <param name="location">The finite lower endpoint.</param>
    /// <param name="scale">The finite nonnegative interval scale.</param>
    /// <param name="shape">The finite positive power shape.</param>
    /// <remarks>
    /// A zero scale defines a point mass at the location and consumes no random state.
    /// Adding a very large finite location and scale can overflow.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the location or scale is non-finite, the scale is negative, or the shape is non-finite or not positive.</exception>
    public PowerSampler(IRandomSource generator, double location, double scale, double shape)
    {
        ValidateParameters(generator, location, scale, shape);
        this.generator = generator;
        this.location = location;
        this.scale = scale;
        this.shape = shape;
        mathMode = generator.MathMode();
    }

    /// <summary>
    /// Returns one value from this sampler's bounded power distribution.
    /// </summary>
    /// <returns>A value on the configured bounded transform, including a possible infinity when the finite endpoint sum overflows.</returns>
    public double Sample()
    {
        if (scale == 0.0)
        {
            return location;
        }

        double unit = FloatingUniform.NextDoubleOpenOpen(generator);
        double exponent = Log(unit) / shape;
        double standard = Exp(exponent);
        return location + scale * standard;
    }

    /// <summary>
    /// Fills a destination with values from this sampler's bounded power distribution.
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

    internal static void ValidateParameters(IRandomSource generator, double location, double scale, double shape)
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
