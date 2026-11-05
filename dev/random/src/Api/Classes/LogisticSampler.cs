using System;

namespace random;

/// <summary>
/// Generates double-precision values from a logistic distribution.
/// </summary>
/// <remarks>
/// The log-odds inverse transform uses a uniform value from <c>(0, 1)</c>, protecting
/// both singular endpoints. The sampler retains the supplied mutable generator and is
/// not safe for concurrent use without external synchronization.
/// </remarks>
public class LogisticSampler : ILogisticSampler
{
    IRandomSource generator;
    double location;
    double scale;
    RandomMathMode mathMode;

    /// <summary>
    /// Creates a logistic sampler with the requested location and scale.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits and selects the math mode.</param>
    /// <param name="location">The finite distribution location.</param>
    /// <param name="scale">The finite nonnegative distribution scale.</param>
    /// <remarks>A zero scale defines a point mass at the location and consumes no random state.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the location or scale is non-finite, or the scale is negative.</exception>
    public LogisticSampler(IRandomSource generator, double location, double scale)
    {
        ValidateParameters(generator, location, scale);
        this.generator = generator;
        this.location = location;
        this.scale = scale;
        mathMode = generator.MathMode();
    }

    /// <summary>
    /// Returns one value from this sampler's logistic distribution.
    /// </summary>
    /// <returns>A logistic value, with possible infinity after location-scale overflow.</returns>
    public double Sample()
    {
        if (scale == 0.0)
        {
            return location;
        }

        double unit = FloatingUniform.NextDoubleOpenOpen(generator);
        double firstLogarithm;
        double secondLogarithm;
        if (mathMode == RandomMathMode.Portable)
        {
            firstLogarithm = PortableMath.Log(unit);
            secondLogarithm = PortableMath.Log1p(-unit);
        }
        else
        {
            firstLogarithm = Math.Log(unit);
            secondLogarithm = Math.Log(1.0 - unit);
        }

        return location + scale * (firstLogarithm - secondLogarithm);
    }

    /// <summary>
    /// Fills a destination with values from this sampler's logistic distribution.
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

    static void ValidateParameters(IRandomSource generator, double location, double scale)
    {
        Guard.NotNull(generator, "generator");
        Guard.Finite(location, "location");
        Guard.Finite(scale, "scale");
        if (scale < 0.0)
        {
            throw RandomArgumentException.Create("scale", "The scale must not be negative.");
        }
    }
}
