using System;

namespace random;

/// <summary>
/// Generates double-precision values from a noncentral Fisher-Snedecor F distribution.
/// </summary>
/// <remarks>
/// Positive noncentrality samples an independent noncentral chi-square numerator and
/// central chi-square denominator, divides each by its degrees of freedom, and combines
/// the ratio in the log domain. Zero noncentrality delegates to
/// <see cref="FisherSnedecorSampler"/> so the central special case retains its established
/// sequence and boundary behavior. The sampler retains the supplied mutable generator and
/// is not safe for concurrent use without external synchronization.
/// </remarks>
public class NoncentralFSampler : INoncentralFSampler
{
    FisherSnedecorSampler centralSampler;
    NoncentralChiSquareSampler numeratorSampler;
    ChiSquareSampler denominatorSampler;
    double noncentrality;
    double logDegreeRatio;
    RandomMathMode mathMode;

    /// <summary>
    /// Creates a noncentral Fisher-Snedecor F sampler with the requested parameters.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits and selects the math mode.</param>
    /// <param name="numeratorDegreesOfFreedom">The finite positive numerator degrees of freedom.</param>
    /// <param name="denominatorDegreesOfFreedom">The finite positive denominator degrees of freedom.</param>
    /// <param name="noncentrality">The finite nonnegative numerator noncentrality.</param>
    /// <remarks>
    /// When numerator degrees of freedom are below one, the Poisson-mixture limit described
    /// by <see cref="NoncentralChiSquareSampler"/> also applies.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator is null, either degree count is non-finite or not positive, noncentrality is non-finite or negative, or the required Poisson mean exceeds its safe count range.</exception>
    public NoncentralFSampler(
        IRandomSource generator,
        double numeratorDegreesOfFreedom,
        double denominatorDegreesOfFreedom,
        double noncentrality)
    {
        ValidateParameters(generator, numeratorDegreesOfFreedom, denominatorDegreesOfFreedom, noncentrality);
        centralSampler = new FisherSnedecorSampler(generator, numeratorDegreesOfFreedom, denominatorDegreesOfFreedom);
        numeratorSampler = new NoncentralChiSquareSampler(generator, numeratorDegreesOfFreedom, noncentrality);
        denominatorSampler = new ChiSquareSampler(generator, denominatorDegreesOfFreedom);
        this.noncentrality = noncentrality;
        mathMode = generator.MathMode();
        logDegreeRatio = Log(denominatorDegreesOfFreedom) - Log(numeratorDegreesOfFreedom);
    }

    /// <summary>
    /// Returns one value from this sampler's noncentral Fisher-Snedecor F distribution.
    /// </summary>
    /// <returns>A nonnegative value, including zero or positive infinity after representational underflow or overflow.</returns>
    public double Sample()
    {
        if (noncentrality == 0.0)
        {
            return centralSampler.Sample();
        }

        double numerator = numeratorSampler.Sample();
        double denominator = denominatorSampler.Sample();
        if (numerator == 0.0)
        {
            return 0.0;
        }

        if (denominator == 0.0 || double.IsPositiveInfinity(numerator))
        {
            return double.PositiveInfinity;
        }

        if (double.IsPositiveInfinity(denominator))
        {
            return 0.0;
        }

        double logarithm = logDegreeRatio + Log(numerator) - Log(denominator);
        return Exp(logarithm);
    }

    /// <summary>
    /// Fills a destination with values from this sampler's noncentral Fisher-Snedecor F distribution.
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

    double Log(double value)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Log(value);
        }

        return Math.Log(value);
    }

    double Exp(double value)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Exp(value);
        }

        return Math.Exp(value);
    }

    static void ValidateParameters(
        IRandomSource generator,
        double numeratorDegreesOfFreedom,
        double denominatorDegreesOfFreedom,
        double noncentrality)
    {
        ChiSquareSampler.ValidateParameters(generator, numeratorDegreesOfFreedom);
        Guard.Finite(denominatorDegreesOfFreedom, "denominatorDegreesOfFreedom");
        if (denominatorDegreesOfFreedom <= 0.0)
        {
            throw RandomArgumentException.Create("denominatorDegreesOfFreedom", "The denominator degrees of freedom must be positive.");
        }

        Guard.Finite(noncentrality, "noncentrality");
        if (noncentrality < 0.0)
        {
            throw RandomArgumentException.Create("noncentrality", "The noncentrality must not be negative.");
        }

        if (numeratorDegreesOfFreedom < 1.0 && noncentrality * 0.5 > PoissonCountSampler.MaximumMean)
        {
            throw RandomArgumentException.Create("noncentrality", "The noncentrality requires a Poisson count outside the safe signed 64-bit range.");
        }
    }
}
