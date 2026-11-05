using System;

namespace random;

/// <summary>
/// Generates double-precision values from a chi distribution.
/// </summary>
/// <remarks>
/// Sampling applies the selected portable or system square-root kernel to a
/// chi-square variate with the same degrees of freedom. The sampler retains the
/// supplied mutable generator and is not safe for concurrent use without external
/// synchronization.
/// </remarks>
public class ChiSampler : IChiSampler
{
    ChiSquareSampler chiSquareSampler;
    RandomMathMode mathMode;

    /// <summary>
    /// Creates a chi sampler with the requested degrees of freedom.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits and selects the math mode.</param>
    /// <param name="degreesOfFreedom">The finite positive degrees of freedom.</param>
    /// <exception cref="RandomArgumentException">The generator is null, or the degrees of freedom are non-finite or not positive.</exception>
    public ChiSampler(IRandomSource generator, double degreesOfFreedom)
    {
        ChiSquareSampler.ValidateParameters(generator, degreesOfFreedom);
        ChiSquareSampler validatedSampler = new ChiSquareSampler(generator, degreesOfFreedom);
        chiSquareSampler = validatedSampler;
        mathMode = generator.MathMode();
    }

    /// <summary>
    /// Returns one value from this sampler's chi distribution.
    /// </summary>
    /// <returns>A nonnegative chi value, or zero after representational underflow.</returns>
    public double Sample()
    {
        double squared = chiSquareSampler.Sample();
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Sqrt(squared);
        }

        return Math.Sqrt(squared);
    }

    /// <summary>
    /// Fills a destination with values from this sampler's chi distribution.
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
