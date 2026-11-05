using System;

namespace random;

/// <summary>
/// Generates double-precision values from a Rayleigh distribution.
/// </summary>
/// <remarks>
/// For location <c>mu</c> and scale <c>sigma</c>, samples are
/// <c>mu + sigma*sqrt(-2 log(u))</c> for an open survival probability <c>u</c>.
/// Using the survival probability directly avoids cancellation from forming <c>1-u</c>.
/// The sampler retains the supplied mutable generator and is not safe for concurrent
/// use without external synchronization.
/// </remarks>
public class RayleighSampler : IRayleighSampler
{
    IRandomSource generator;
    double location;
    double scale;
    RandomMathMode mathMode;

    /// <summary>
    /// Creates a Rayleigh sampler with the requested location and scale.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits and selects the math mode.</param>
    /// <param name="location">The finite lower endpoint and distribution location.</param>
    /// <param name="scale">The finite nonnegative distribution scale.</param>
    /// <remarks>
    /// A zero scale defines a point mass at the location and consumes no random state.
    /// Location-scale overflow can return positive infinity.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the location or scale is non-finite, or the scale is negative.</exception>
    public RayleighSampler(IRandomSource generator, double location, double scale)
    {
        ValidateParameters(generator, location, scale);
        this.generator = generator;
        this.location = location;
        this.scale = scale;
        mathMode = generator.MathMode();
    }

    /// <summary>
    /// Returns one value from this sampler's Rayleigh distribution.
    /// </summary>
    /// <returns>A value at or above the location, including a possible positive infinity after location-scale overflow.</returns>
    public double Sample()
    {
        if (scale == 0.0)
        {
            return location;
        }

        double unit = FloatingUniform.NextDoubleOpenOpen(generator);
        double logarithm;
        double root;
        if (mathMode == RandomMathMode.Portable)
        {
            logarithm = PortableMath.Log(unit);
            root = PortableMath.Sqrt(-2.0 * logarithm);
        }
        else
        {
            logarithm = Math.Log(unit);
            root = Math.Sqrt(-2.0 * logarithm);
        }

        return location + scale * root;
    }

    /// <summary>
    /// Fills a destination with values from this sampler's Rayleigh distribution.
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
