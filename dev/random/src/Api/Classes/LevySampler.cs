using System;

namespace random;

/// <summary>
/// Generates double-precision values from a Levy distribution.
/// </summary>
/// <remarks>
/// A Levy value with location <c>mu</c> and scale <c>c</c> is sampled as
/// <c>mu + c/z^2</c> for a standard normal variate <c>z</c>. Location is the lower
/// support endpoint, not the mean; the Levy distribution has no finite mean. The
/// sampler retains the supplied mutable generator and is not safe for concurrent use
/// without external synchronization.
/// </remarks>
public class LevySampler : ILevySampler
{
    NormalSampler normalSampler;
    double location;
    double scale;

    /// <summary>
    /// Creates a Levy sampler with the requested location and scale.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits and selects the math mode.</param>
    /// <param name="location">The finite lower endpoint and distribution location.</param>
    /// <param name="scale">The finite nonnegative distribution scale.</param>
    /// <remarks>
    /// A zero scale defines a point mass at the location and consumes no random state.
    /// A zero or underflowed normal square, or location-scale overflow, returns positive infinity.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the location or scale is non-finite, or the scale is negative.</exception>
    public LevySampler(IRandomSource generator, double location, double scale)
    {
        ValidateParameters(generator, location, scale);
        NormalSampler validatedNormalSampler = new NormalSampler(generator, 0.0, 1.0);
        normalSampler = validatedNormalSampler;
        this.location = location;
        this.scale = scale;
    }

    /// <summary>
    /// Returns one value from this sampler's Levy distribution.
    /// </summary>
    /// <returns>A value at or above the location, including a possible positive infinity in the representational tail.</returns>
    public double Sample()
    {
        if (scale == 0.0)
        {
            return location;
        }

        double normal = normalSampler.Sample();
        double squared = normal * normal;
        if (squared == 0.0)
        {
            return double.PositiveInfinity;
        }

        return location + scale / squared;
    }

    /// <summary>
    /// Fills a destination with values from this sampler's Levy distribution.
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
