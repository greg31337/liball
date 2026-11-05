using System;

namespace random;

/// <summary>
/// Generates double-precision values from a chi-square distribution.
/// </summary>
/// <remarks>
/// Sampling uses the gamma relationship with shape <c>degreesOfFreedom / 2</c>
/// and scale two. The sampler retains the supplied mutable generator and is not
/// safe for concurrent use without external synchronization.
/// </remarks>
public class ChiSquareSampler : IChiSquareSampler
{
    GammaSampler gammaSampler;

    /// <summary>
    /// Creates a chi-square sampler with the requested degrees of freedom.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits and selects the math mode.</param>
    /// <param name="degreesOfFreedom">The finite positive degrees of freedom.</param>
    /// <remarks>
    /// If halving the smallest positive degrees of freedom underflows, the smallest
    /// positive gamma shape is used so the representable result remains defined.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator is null, or the degrees of freedom are non-finite or not positive.</exception>
    public ChiSquareSampler(IRandomSource generator, double degreesOfFreedom)
    {
        ValidateParameters(generator, degreesOfFreedom);
        double shape = degreesOfFreedom * 0.5;
        if (shape == 0.0)
        {
            shape = double.Epsilon;
        }

        GammaSampler validatedSampler = new GammaSampler(generator, shape, 2.0);
        gammaSampler = validatedSampler;
    }

    /// <summary>
    /// Returns one value from this sampler's chi-square distribution.
    /// </summary>
    /// <returns>A nonnegative chi-square value, or zero after representational underflow.</returns>
    public double Sample()
    {
        return gammaSampler.Sample();
    }

    /// <summary>
    /// Fills a destination with values from this sampler's chi-square distribution.
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

    internal static void ValidateParameters(IRandomSource generator, double degreesOfFreedom)
    {
        Guard.NotNull(generator, "generator");
        Guard.Finite(degreesOfFreedom, "degreesOfFreedom");
        if (degreesOfFreedom <= 0.0)
        {
            throw RandomArgumentException.Create("degreesOfFreedom", "The degrees of freedom must be positive.");
        }
    }
}
