using System;

namespace random;

/// <summary>
/// Generates double-precision values from an Erlang distribution.
/// </summary>
/// <remarks>
/// Erlang is the positive-integer-shape specialization of the gamma distribution.
/// This sampler uses the gamma rejection core, including for very large integer shapes.
/// It retains the supplied mutable generator and is not safe for concurrent use without
/// external synchronization.
/// </remarks>
public class ErlangSampler : IErlangSampler
{
    GammaSampler gammaSampler;

    /// <summary>
    /// Creates an Erlang sampler with the requested integer shape and scale.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits and selects the math mode.</param>
    /// <param name="shape">The positive integer distribution shape.</param>
    /// <param name="scale">The finite nonnegative distribution scale.</param>
    /// <remarks>A zero scale defines a point mass at zero and consumes no random state.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the shape is not positive, or the scale is non-finite or negative.</exception>
    public ErlangSampler(IRandomSource generator, int shape, double scale)
    {
        ValidateParameters(generator, shape, scale);
        GammaSampler validatedSampler = new GammaSampler(generator, (double)shape, scale);
        gammaSampler = validatedSampler;
    }

    /// <summary>
    /// Returns one value from this sampler's Erlang distribution.
    /// </summary>
    /// <returns>A nonnegative Erlang value, or zero when the scale is zero.</returns>
    public double Sample()
    {
        return gammaSampler.Sample();
    }

    /// <summary>
    /// Fills a destination with values from this sampler's Erlang distribution.
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

    static void ValidateParameters(IRandomSource generator, int shape, double scale)
    {
        Guard.NotNull(generator, "generator");
        if (shape <= 0)
        {
            throw RandomArgumentException.Create("shape", "The shape must be positive.");
        }

        Guard.Finite(scale, "scale");
        if (scale < 0.0)
        {
            throw RandomArgumentException.Create("scale", "The scale must not be negative.");
        }
    }
}
