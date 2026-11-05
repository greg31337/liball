using System;

namespace random;

/// <summary>
/// Generates double-precision values from a maximum-type Gumbel distribution.
/// </summary>
/// <remarks>
/// The inverse transform is <c>-log(-log(u))</c> for a uniform value from <c>(0, 1)</c>,
/// protecting both logarithmic endpoints. The sampler retains the supplied mutable
/// generator and is not safe for concurrent use without external synchronization.
/// </remarks>
public class GumbelSampler : IGumbelSampler
{
    IRandomSource generator;
    double location;
    double scale;
    RandomMathMode mathMode;

    /// <summary>
    /// Creates a maximum-type Gumbel sampler with the requested location and scale.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits and selects the math mode.</param>
    /// <param name="location">The finite distribution location.</param>
    /// <param name="scale">The finite nonnegative distribution scale.</param>
    /// <remarks>A zero scale defines a point mass at the location and consumes no random state.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the location or scale is non-finite, or the scale is negative.</exception>
    public GumbelSampler(IRandomSource generator, double location, double scale)
    {
        ValidateParameters(generator, location, scale);
        this.generator = generator;
        this.location = location;
        this.scale = scale;
        mathMode = generator.MathMode();
    }

    /// <summary>
    /// Returns one value from this sampler's maximum-type Gumbel distribution.
    /// </summary>
    /// <returns>A Gumbel value, with possible infinity after location-scale overflow.</returns>
    public double Sample()
    {
        if (scale == 0.0)
        {
            return location;
        }

        double unit = FloatingUniform.NextDoubleOpenOpen(generator);
        double inner;
        double standard;
        if (mathMode == RandomMathMode.Portable)
        {
            inner = -PortableMath.Log(unit);
            standard = -PortableMath.Log(inner);
        }
        else
        {
            inner = -Math.Log(unit);
            standard = -Math.Log(inner);
        }

        return location + scale * standard;
    }

    /// <summary>
    /// Fills a destination with values from this sampler's maximum-type Gumbel distribution.
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
