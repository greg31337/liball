using System;

namespace random;

/// <summary>
/// Generates double-precision values from a Fisher-Snedecor F distribution.
/// </summary>
/// <remarks>
/// Sampling uses beta odds with shapes equal to half the numerator and denominator
/// degrees of freedom. The degree ratio and beta odds are combined in the log domain
/// to avoid overflowing intermediate products. The sampler retains the supplied mutable
/// generator and is not safe for concurrent use without external synchronization.
/// </remarks>
public class FisherSnedecorSampler : IFisherSnedecorSampler
{
    BetaSampler betaSampler;
    double logDegreeRatio;
    RandomMathMode mathMode;

    /// <summary>
    /// Creates a Fisher-Snedecor F sampler with the requested degrees of freedom.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits and selects the math mode.</param>
    /// <param name="numeratorDegreesOfFreedom">The finite positive numerator degrees of freedom.</param>
    /// <param name="denominatorDegreesOfFreedom">The finite positive denominator degrees of freedom.</param>
    /// <remarks>Representational tail underflow and overflow can return zero or positive infinity.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null, or either degree count is non-finite or not positive.</exception>
    public FisherSnedecorSampler(IRandomSource generator, double numeratorDegreesOfFreedom, double denominatorDegreesOfFreedom)
    {
        ValidateParameters(generator, numeratorDegreesOfFreedom, denominatorDegreesOfFreedom);
        double firstShape = numeratorDegreesOfFreedom * 0.5;
        double secondShape = denominatorDegreesOfFreedom * 0.5;
        if (firstShape == 0.0)
        {
            firstShape = double.Epsilon;
        }

        if (secondShape == 0.0)
        {
            secondShape = double.Epsilon;
        }

        RandomMathMode selectedMathMode = generator.MathMode();
        BetaSampler validatedSampler = new BetaSampler(generator, firstShape, secondShape);
        double selectedLogDegreeRatio;
        if (selectedMathMode == RandomMathMode.Portable)
        {
            selectedLogDegreeRatio = PortableMath.Log(denominatorDegreesOfFreedom) - PortableMath.Log(numeratorDegreesOfFreedom);
        }
        else
        {
            selectedLogDegreeRatio = Math.Log(denominatorDegreesOfFreedom) - Math.Log(numeratorDegreesOfFreedom);
        }

        betaSampler = validatedSampler;
        logDegreeRatio = selectedLogDegreeRatio;
        mathMode = selectedMathMode;
    }

    /// <summary>
    /// Returns one value from this sampler's Fisher-Snedecor F distribution.
    /// </summary>
    /// <returns>A nonnegative F variate, including zero or positive infinity after representational underflow or overflow.</returns>
    public double Sample()
    {
        double betaValue = betaSampler.Sample();
        if (betaValue == 0.0)
        {
            return 0.0;
        }

        if (betaValue == 1.0)
        {
            return double.PositiveInfinity;
        }

        double logarithm;
        if (mathMode == RandomMathMode.Portable)
        {
            logarithm = logDegreeRatio + PortableMath.Log(betaValue) - PortableMath.Log1p(-betaValue);
            return PortableMath.Exp(logarithm);
        }

        logarithm = logDegreeRatio + Math.Log(betaValue) - Math.Log(1.0 - betaValue);
        return Math.Exp(logarithm);
    }

    /// <summary>
    /// Fills a destination with values from this sampler's Fisher-Snedecor F distribution.
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

    static void ValidateParameters(IRandomSource generator, double numeratorDegreesOfFreedom, double denominatorDegreesOfFreedom)
    {
        Guard.NotNull(generator, "generator");
        Guard.Finite(numeratorDegreesOfFreedom, "numeratorDegreesOfFreedom");
        if (numeratorDegreesOfFreedom <= 0.0)
        {
            throw RandomArgumentException.Create("numeratorDegreesOfFreedom", "The numerator degrees of freedom must be positive.");
        }

        Guard.Finite(denominatorDegreesOfFreedom, "denominatorDegreesOfFreedom");
        if (denominatorDegreesOfFreedom <= 0.0)
        {
            throw RandomArgumentException.Create("denominatorDegreesOfFreedom", "The denominator degrees of freedom must be positive.");
        }
    }
}
