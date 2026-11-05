using System;

namespace random;

/// <summary>
/// Generates signed 64-bit failure counts from a negative-binomial distribution.
/// </summary>
/// <remarks>
/// A sample is the number of failures observed before the configured positive success
/// shape is reached. Integer success shapes through 32 are sampled as exact sums of
/// failure-count geometric variates. Fractional and larger shapes use the equivalent
/// gamma-Poisson mixture with gamma shape <c>successes</c> and Poisson mean
/// <c>gamma * (1-probability) / probability</c>. The sampler retains the supplied mutable
/// generator and is not safe for concurrent use without external synchronization.
/// </remarks>
public class NegativeBinomialSampler : INegativeBinomialSampler
{
    const int DirectSuccessLimit = 32;
    IRandomSource generator;
    GeometricSampler? geometricSampler;
    GammaSampler? gammaSampler;
    double successes;
    double probability;
    double logOdds;
    double maximumLogMean;
    bool direct;
    RandomMathMode mathMode;

    /// <summary>
    /// Creates a negative-binomial failure-count sampler.
    /// </summary>
    /// <param name="generator">The generator that supplies random values and selects the math mode.</param>
    /// <param name="successes">The finite positive success shape.</param>
    /// <param name="probability">The finite success probability greater than zero and at most one.</param>
    /// <remarks>
    /// Fractional success shapes are supported through the gamma-Poisson definition.
    /// A probability of one defines zero failures and consumes no random state.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the success shape is non-finite or not positive, or the probability is non-finite or outside <c>(0, 1]</c>.</exception>
    public NegativeBinomialSampler(IRandomSource generator, double successes, double probability)
    {
        Guard.NotNull(generator, "generator");
        Guard.Finite(successes, "successes");
        if (successes <= 0.0)
        {
            throw RandomArgumentException.Create("successes", "The success shape must be positive.");
        }

        Guard.Probability(probability, "probability");
        if (probability == 0.0)
        {
            throw RandomArgumentException.Create("probability", "The success probability must be greater than zero.");
        }

        this.generator = generator;
        this.successes = successes;
        this.probability = probability;
        mathMode = generator.MathMode();
        geometricSampler = null;
        gammaSampler = null;
        logOdds = 0.0;
        maximumLogMean = 0.0;
        direct = successes <= (double)DirectSuccessLimit && successes == Math.Floor(successes);
        if (probability == 1.0)
        {
            return;
        }

        if (direct)
        {
            geometricSampler = new GeometricSampler(generator, probability);
        }
        else
        {
            gammaSampler = new GammaSampler(generator, successes, 1.0);
            logOdds = Log1p(-probability) - Log(probability);
            maximumLogMean = Log(PoissonCountSampler.MaximumMean);
        }
    }

    /// <summary>
    /// Returns the number of failures before the configured success shape is reached.
    /// </summary>
    /// <returns>A nonnegative signed 64-bit failure count.</returns>
    /// <exception cref="RandomExhaustedException">The sampled failure count or conditional Poisson mean exceeds the signed 64-bit output capacity.</exception>
    public long Sample()
    {
        if (probability == 1.0)
        {
            return 0L;
        }

        if (direct)
        {
            return SampleDirect();
        }

        return SampleGammaPoisson();
    }

    /// <summary>
    /// Fills a destination with failure counts from this sampler's negative-binomial distribution.
    /// </summary>
    /// <param name="destination">The destination failure counts.</param>
    /// <remarks>The result and generator state match repeated calls to <see cref="Sample()"/>.</remarks>
    /// <exception cref="RandomExhaustedException">A sampled failure count or conditional Poisson mean exceeds the signed 64-bit output capacity.</exception>
    public void Fill(Span<long> destination)
    {
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = Sample();
            index++;
        }
    }

    long SampleDirect()
    {
        long total = 0L;
        int index = 0;
        int successCount = (int)successes;
        while (index < successCount)
        {
            long failures = GetGeometricSampler().SampleFailures();
            if (failures > long.MaxValue - total)
            {
                throw RandomExhaustedException.Create("The negative-binomial failure count exceeds the signed 64-bit output capacity.");
            }

            total += failures;
            index++;
        }

        return total;
    }

    long SampleGammaPoisson()
    {
        double gamma = GetGammaSampler().Sample();
        if (gamma == 0.0)
        {
            return 0L;
        }

        if (!double.IsFinite(gamma))
        {
            throw RandomExhaustedException.Create("The negative-binomial conditional Poisson mean exceeds the signed 64-bit output capacity.");
        }

        double logMean = Log(gamma) + logOdds;
        if (!double.IsFinite(logMean) || logMean > maximumLogMean)
        {
            throw RandomExhaustedException.Create("The negative-binomial conditional Poisson mean exceeds the signed 64-bit output capacity.");
        }

        double mean = Exp(logMean);
        if (!double.IsFinite(mean) || mean > PoissonCountSampler.MaximumMean)
        {
            throw RandomExhaustedException.Create("The negative-binomial conditional Poisson mean exceeds the signed 64-bit output capacity.");
        }

        PoissonCountSampler poisson = new PoissonCountSampler(generator, mean);
        return poisson.Sample();
    }

    GeometricSampler GetGeometricSampler()
    {
        if (geometricSampler == null)
        {
            throw RandomStateException.Create("The negative-binomial geometric sampler is unavailable.");
        }

        return geometricSampler;
    }

    GammaSampler GetGammaSampler()
    {
        if (gammaSampler == null)
        {
            throw RandomStateException.Create("The negative-binomial gamma sampler is unavailable.");
        }

        return gammaSampler;
    }

    double Log(double value)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Log(value);
        }

        return Math.Log(value);
    }

    double Log1p(double value)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Log1p(value);
        }

        if (value < -0.5 || value > 0.5)
        {
            return Math.Log(1.0 + value);
        }

        double reduced = value / (2.0 + value);
        double square = reduced * reduced;
        double term = reduced;
        double sum = term;
        int denominator = 3;
        while (denominator <= 39)
        {
            term *= square;
            sum += term / denominator;
            denominator += 2;
        }

        return 2.0 * sum;
    }

    double Exp(double value)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Exp(value);
        }

        return Math.Exp(value);
    }
}
