using System;

namespace random;

/// <summary>
/// Generates double-precision values from an inverse-gamma distribution.
/// </summary>
/// <remarks>
/// Parameters use the shape-scale convention: if <c>G</c> is gamma with the requested
/// shape and unit scale, the returned value is <c>scale/G</c>. The sampler retains the
/// supplied mutable generator and is not safe for concurrent use without external synchronization.
/// </remarks>
public class InverseGammaSampler : IInverseGammaSampler
{
    GammaSampler gammaSampler;
    double scale;

    /// <summary>
    /// Creates an inverse-gamma sampler with the requested shape and scale.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits and selects the math mode.</param>
    /// <param name="shape">The finite positive distribution shape.</param>
    /// <param name="scale">The finite nonnegative inverse-gamma scale.</param>
    /// <remarks>
    /// A zero scale defines a point mass at zero and consumes no random state.
    /// A zero gamma denominator or division overflow returns positive infinity.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the shape is non-finite or not positive, or the scale is non-finite or negative.</exception>
    public InverseGammaSampler(IRandomSource generator, double shape, double scale)
    {
        ValidateParameters(generator, shape, scale);
        GammaSampler validatedGammaSampler = new GammaSampler(generator, shape, 1.0);
        gammaSampler = validatedGammaSampler;
        this.scale = scale;
    }

    /// <summary>
    /// Returns one value from this sampler's inverse-gamma distribution.
    /// </summary>
    /// <returns>A nonnegative inverse-gamma value, including a possible positive infinity in the representational tail.</returns>
    public double Sample()
    {
        if (scale == 0.0)
        {
            return 0.0;
        }

        double denominator = gammaSampler.Sample();
        if (denominator == 0.0)
        {
            return double.PositiveInfinity;
        }

        return scale / denominator;
    }

    /// <summary>
    /// Fills a destination with values from this sampler's inverse-gamma distribution.
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

    static void ValidateParameters(IRandomSource generator, double shape, double scale)
    {
        Guard.NotNull(generator, "generator");
        Guard.Finite(shape, "shape");
        if (shape <= 0.0)
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
