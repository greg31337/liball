using System;

namespace random;

/// <summary>
/// Generates double-precision values from Student's t distribution.
/// </summary>
/// <remarks>
/// Sampling divides a standard normal variate by the square root of an independent
/// chi-square variate normalized by its degrees of freedom. The sampler retains the
/// supplied mutable generator and is not safe for concurrent use without external
/// synchronization.
/// </remarks>
public class StudentTSampler : IStudentTSampler
{
    NormalSampler normalSampler;
    ChiSquareSampler chiSquareSampler;
    double degreesOfFreedom;
    RandomMathMode mathMode;

    /// <summary>
    /// Creates a Student's t sampler with the requested degrees of freedom.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits and selects the math mode.</param>
    /// <param name="degreesOfFreedom">The finite positive degrees of freedom.</param>
    /// <exception cref="RandomArgumentException">The generator is null, or the degrees of freedom are non-finite or not positive.</exception>
    public StudentTSampler(IRandomSource generator, double degreesOfFreedom)
    {
        ChiSquareSampler.ValidateParameters(generator, degreesOfFreedom);
        NormalSampler validatedNormalSampler = new NormalSampler(generator, 0.0, 1.0);
        ChiSquareSampler validatedChiSquareSampler = new ChiSquareSampler(generator, degreesOfFreedom);
        normalSampler = validatedNormalSampler;
        chiSquareSampler = validatedChiSquareSampler;
        this.degreesOfFreedom = degreesOfFreedom;
        mathMode = generator.MathMode();
    }

    /// <summary>
    /// Returns one value from this sampler's Student's t distribution.
    /// </summary>
    /// <returns>A signed t variate; representational tail overflow can return an infinity.</returns>
    public double Sample()
    {
        double normal = normalSampler.Sample();
        double squared = chiSquareSampler.Sample();
        double normalized = squared / degreesOfFreedom;
        double denominator;
        if (mathMode == RandomMathMode.Portable)
        {
            denominator = PortableMath.Sqrt(normalized);
        }
        else
        {
            denominator = Math.Sqrt(normalized);
        }

        if (denominator == 0.0)
        {
            if (normal == 0.0)
            {
                return normal;
            }

            return normal < 0.0 ? double.NegativeInfinity : double.PositiveInfinity;
        }

        return normal / denominator;
    }

    /// <summary>
    /// Fills a destination with values from this sampler's Student's t distribution.
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
