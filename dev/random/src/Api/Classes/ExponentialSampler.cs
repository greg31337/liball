using System;

namespace random;

/// <summary>
/// Generates double-precision values from an exponential distribution.
/// </summary>
/// <remarks>
/// Positive scales transform a uniform value from the open interval <c>(0, 1)</c>,
/// so samples are strictly positive and logarithms never receive an endpoint. The
/// sampler retains the supplied mutable generator and is not safe for concurrent
/// use without external synchronization.
/// </remarks>
public class ExponentialSampler : IExponentialSampler
{
    IRandomSource generator;
    double scale;
    RandomMathMode mathMode;

    /// <summary>
    /// Creates an exponential sampler with the requested scale.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits and selects the math mode.</param>
    /// <param name="scale">The finite nonnegative distribution scale.</param>
    /// <remarks>
    /// A zero scale defines a point mass at zero and consumes no random state.
    /// Multiplication by a very large scale can return positive infinity after overflow.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator is null, or the scale is non-finite or negative.</exception>
    public ExponentialSampler(IRandomSource generator, double scale)
    {
        Guard.NotNull(generator, "generator");
        Guard.Finite(scale, "scale");
        if (scale < 0.0)
        {
            throw RandomArgumentException.Create("scale", "The scale must not be negative.");
        }

        this.generator = generator;
        this.scale = scale;
        mathMode = generator.MathMode();
    }

    /// <summary>
    /// Returns one value from this sampler's exponential distribution.
    /// </summary>
    /// <returns>A strictly positive value for a positive scale, or zero when the scale is zero.</returns>
    public double Sample()
    {
        if (scale == 0.0)
        {
            return 0.0;
        }

        double unit = FloatingUniform.NextDoubleOpenOpen(generator);
        double logarithm;
        if (mathMode == RandomMathMode.Portable)
        {
            logarithm = PortableMath.Log(unit);
        }
        else
        {
            logarithm = Math.Log(unit);
        }

        return scale * -logarithm;
    }

    /// <summary>
    /// Fills a destination with values from this sampler's exponential distribution.
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
}
