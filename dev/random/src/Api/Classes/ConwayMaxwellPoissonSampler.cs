using System;

namespace random;

/// <summary>
/// Generates signed 64-bit counts from a Conway-Maxwell-Poisson distribution.
/// </summary>
/// <remarks>
/// The probability of count <c>k</c> is proportional to
/// <c>rate^k / (k!)^dispersion</c>. Dispersion one is Poisson, while dispersion
/// zero with rate below one is geometric on failure counts. Other parameter values
/// use exact rejection sampling with a Poisson envelope for underdispersion and a
/// geometric envelope for overdispersion. A separate mode-centered series estimates
/// the log normalizer with a certified omitted-tail bound; its convergence does not
/// affect rejection-sampling correctness. The sampler retains the supplied mutable
/// generator and is not safe for concurrent use without external synchronization.
/// </remarks>
public class ConwayMaxwellPoissonSampler : IConwayMaxwellPoissonSampler
{
    /// <summary>
    /// The maximum number of scaled terms used to certify the normalizing series.
    /// </summary>
    internal const int MaximumNormalizationTerms = ConwayMaxwellPoissonLimits.MaximumNormalizationTerms;

    /// <summary>
    /// The requested relative omitted-tail bound for the normalizing series.
    /// </summary>
    internal const double NormalizationRelativeTolerance = ConwayMaxwellPoissonLimits.NormalizationRelativeTolerance;

    IRandomSource generator;
    PoissonSampler? poissonSampler;
    GeometricSampler? geometricSampler;
    double rate;
    double dispersion;
    double logRate;
    double envelopeLogBound;
    double poissonProposalLogRate;
    double geometricSuccessProbability;
    double logGeometricFailureProbability;
    double logNormalization;
    double normalizationRelativeErrorBound;
    int normalizationTermCount;
    bool normalizationConverged;
    bool deterministic;
    RandomMathMode mathMode;

    /// <summary>
    /// Creates a Conway-Maxwell-Poisson sampler with the requested rate and dispersion.
    /// </summary>
    /// <param name="generator">The generator that supplies random values and selects the math mode.</param>
    /// <param name="rate">The finite nonnegative rate parameter.</param>
    /// <param name="dispersion">The finite nonnegative dispersion parameter.</param>
    /// <remarks>
    /// Dispersion zero requires a rate less than one. A zero rate defines a point mass
    /// at zero. Parameter combinations whose effective mode exceeds signed 64-bit
    /// sampling capacity are rejected before random state is consumed.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator is null; a parameter is non-finite or negative; dispersion is zero with rate at least one; or the effective mode exceeds signed 64-bit sampling capacity.</exception>
    public ConwayMaxwellPoissonSampler(IRandomSource generator, double rate, double dispersion)
    {
        Guard.NotNull(generator, "generator");
        Guard.Finite(rate, "rate");
        if (rate < 0.0)
        {
            throw RandomArgumentException.Create("rate", "The Conway-Maxwell-Poisson rate must be nonnegative.");
        }

        Guard.Finite(dispersion, "dispersion");
        if (dispersion < 0.0)
        {
            throw RandomArgumentException.Create("dispersion", "The Conway-Maxwell-Poisson dispersion must be nonnegative.");
        }

        if (dispersion == 0.0 && rate >= 1.0)
        {
            throw RandomArgumentException.Create("rate", "Dispersion zero requires a rate less than one for a finite normalizer.");
        }

        this.generator = generator;
        this.rate = rate;
        this.dispersion = dispersion;
        mathMode = generator.MathMode();
        poissonSampler = null;
        geometricSampler = null;
        logRate = rate == 0.0 ? double.NegativeInfinity : Log(rate);
        deterministic = rate == 0.0;
        envelopeLogBound = 0.0;
        poissonProposalLogRate = 0.0;
        geometricSuccessProbability = 0.0;
        logGeometricFailureProbability = 0.0;
        logNormalization = 0.0;
        normalizationRelativeErrorBound = 0.0;
        normalizationTermCount = 1;
        normalizationConverged = true;
        if (deterministic)
        {
            return;
        }

        if (dispersion == 0.0)
        {
            geometricSuccessProbability = 1.0 - rate;
            geometricSampler = new GeometricSampler(generator, geometricSuccessProbability);
            logNormalization = -Log1p(-rate);
            return;
        }

        double effectiveLog = logRate / dispersion;
        double maximumEffectiveLog = Log(PoissonSampler.MaximumMean);
        if (double.IsNaN(effectiveLog) || effectiveLog > maximumEffectiveLog)
        {
            throw RandomArgumentException.Create("rate", "The effective Conway-Maxwell-Poisson mode exceeds signed 64-bit sampling capacity.");
        }

        double effectiveRate = effectiveLog == double.NegativeInfinity ? 0.0 : Exp(effectiveLog);
        if (!double.IsFinite(effectiveRate) || effectiveRate > PoissonSampler.MaximumMean)
        {
            throw RandomArgumentException.Create("rate", "The effective Conway-Maxwell-Poisson mode exceeds signed 64-bit sampling capacity.");
        }

        if (dispersion == 1.0)
        {
            poissonSampler = new PoissonSampler(generator, rate);
            logNormalization = rate;
            return;
        }

        ComputeNormalization(effectiveRate);
        if (dispersion > 1.0)
        {
            poissonSampler = new PoissonSampler(generator, effectiveRate);
            poissonProposalLogRate = Log(effectiveRate);
            double boundModeLog = (logRate - poissonProposalLogRate) / (dispersion - 1.0);
            double boundMode = boundModeLog <= 0.0 ? 0.0 : Math.Floor(Exp(boundModeLog));
            envelopeLogBound = LogPoissonEnvelopeRatio(boundMode, poissonProposalLogRate);
            return;
        }

        geometricSuccessProbability = SelectGeometricProbability(effectiveRate);
        if (geometricSuccessProbability <= 0.0)
        {
            throw RandomArgumentException.Create("dispersion", "The overdispersed Conway-Maxwell-Poisson mean exceeds signed 64-bit sampling capacity.");
        }

        if (geometricSuccessProbability >= 1.0)
        {
            geometricSuccessProbability = PortableMath.NextDown(1.0);
        }

        geometricSampler = new GeometricSampler(generator, geometricSuccessProbability);
        logGeometricFailureProbability = Log1p(-geometricSuccessProbability);
        double geometricBoundModeLog = (logRate - logGeometricFailureProbability) / dispersion;
        double geometricBoundMode = geometricBoundModeLog <= 0.0 ? 0.0 : Math.Floor(Exp(geometricBoundModeLog));
        if (!double.IsFinite(geometricBoundMode))
        {
            throw RandomArgumentException.Create("dispersion", "The overdispersed rejection envelope exceeds signed 64-bit sampling capacity.");
        }

        envelopeLogBound = LogGeometricEnvelopeRatio(geometricBoundMode);
    }

    /// <summary>
    /// Returns one count from this sampler's Conway-Maxwell-Poisson distribution.
    /// </summary>
    /// <returns>A nonnegative signed 64-bit count.</returns>
    /// <exception cref="RandomExhaustedException">A geometric proposal exceeds signed 64-bit output capacity.</exception>
    public long Sample()
    {
        if (deterministic)
        {
            return 0L;
        }

        if (dispersion == 0.0)
        {
            return GetGeometricSampler().SampleFailures();
        }

        if (dispersion == 1.0)
        {
            return GetPoissonSampler().Sample();
        }

        if (dispersion > 1.0)
        {
            return SamplePoissonEnvelope();
        }

        return SampleGeometricEnvelope();
    }

    /// <summary>
    /// Fills a destination with counts from this sampler's Conway-Maxwell-Poisson distribution.
    /// </summary>
    /// <param name="destination">The destination counts.</param>
    /// <remarks>The result and generator state match repeated calls to <see cref="Sample()"/>.</remarks>
    /// <exception cref="RandomExhaustedException">A geometric proposal exceeds signed 64-bit output capacity.</exception>
    public void Fill(Span<long> destination)
    {
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = Sample();
            index++;
        }
    }

    /// <summary>
    /// Reports whether the mode-centered normalizing series met its relative-error target.
    /// </summary>
    /// <returns><c>true</c> when the omitted-tail bound is within <see cref="NormalizationRelativeTolerance"/>; otherwise <c>false</c>.</returns>
    public bool NormalizationConverged()
    {
        return normalizationConverged;
    }

    /// <summary>
    /// Returns the number of scaled series terms used by the normalizer diagnostic.
    /// </summary>
    /// <returns>A positive term count no greater than <see cref="MaximumNormalizationTerms"/>.</returns>
    public int NormalizationTermCount()
    {
        return normalizationTermCount;
    }

    /// <summary>
    /// Returns the certified relative upper bound for mass omitted from the normalizing series.
    /// </summary>
    /// <returns>The omitted-tail bound relative to the retained partial sum, or positive infinity when no finite bound was reached.</returns>
    public double NormalizationRelativeErrorBound()
    {
        return normalizationRelativeErrorBound;
    }

    /// <summary>
    /// Returns the natural logarithm of the certified approximate normalizing constant.
    /// </summary>
    /// <returns>The logarithm of the retained mode-scaled series; its relative error is bounded by <see cref="NormalizationRelativeErrorBound()"/>.</returns>
    /// <exception cref="RandomExhaustedException">The normalizing series did not converge within <see cref="MaximumNormalizationTerms"/>.</exception>
    public double LogNormalization()
    {
        if (!normalizationConverged)
        {
            throw RandomExhaustedException.Create("The Conway-Maxwell-Poisson normalizing series did not converge within the term limit.");
        }

        return logNormalization;
    }

    long SamplePoissonEnvelope()
    {
        while (true)
        {
            long candidate = GetPoissonSampler().Sample();
            double acceptanceLog = LogPoissonEnvelopeRatio((double)candidate, poissonProposalLogRate) - envelopeLogBound;
            if (acceptanceLog >= 0.0 || Log(FloatingUniform.NextDoubleOpenOpen(generator)) <= acceptanceLog)
            {
                return candidate;
            }
        }
    }

    long SampleGeometricEnvelope()
    {
        while (true)
        {
            long candidate = GetGeometricSampler().SampleFailures();
            double acceptanceLog = LogGeometricEnvelopeRatio((double)candidate) - envelopeLogBound;
            if (acceptanceLog >= 0.0 || Log(FloatingUniform.NextDoubleOpenOpen(generator)) <= acceptanceLog)
            {
                return candidate;
            }
        }
    }

    double LogPoissonEnvelopeRatio(double count, double proposalLogRate)
    {
        return count * (logRate - proposalLogRate) +
            (1.0 - dispersion) * LogFactorial(count);
    }

    double LogGeometricEnvelopeRatio(double count)
    {
        return count * (logRate - logGeometricFailureProbability) -
            dispersion * LogFactorial(count) - Log(geometricSuccessProbability);
    }

    void ComputeNormalization(double effectiveRate)
    {
        long center = (long)Math.Floor(effectiveRate);
        double centerLogWeight = (double)center * logRate - dispersion * LogFactorial((double)center);
        long lowerIndex = center;
        long upperIndex = center;
        double lowerTerm = 1.0;
        double upperTerm = 1.0;
        double sum = 1.0;
        double compensation = 0.0;
        normalizationTermCount = 1;
        while (true)
        {
            double lowerTail = LowerTailBound(lowerIndex, lowerTerm);
            double upperTail = UpperTailBound(upperIndex, upperTerm);
            double tail = lowerTail + upperTail;
            normalizationRelativeErrorBound = tail / sum;
            if (normalizationRelativeErrorBound <= NormalizationRelativeTolerance)
            {
                normalizationConverged = true;
                break;
            }

            if (normalizationTermCount >= MaximumNormalizationTerms)
            {
                normalizationConverged = false;
                break;
            }

            double nextTerm;
            if (lowerIndex > 0L && lowerTail >= upperTail)
            {
                double ratio = Exp(dispersion * Log((double)lowerIndex) - logRate);
                lowerTerm *= ratio;
                lowerIndex--;
                nextTerm = lowerTerm;
            }
            else
            {
                upperIndex++;
                double ratio = Exp(logRate - dispersion * Log((double)upperIndex));
                upperTerm *= ratio;
                nextTerm = upperTerm;
            }

            double adjusted = nextTerm - compensation;
            double next = sum + adjusted;
            compensation = (next - sum) - adjusted;
            sum = next;
            normalizationTermCount++;
        }

        logNormalization = centerLogWeight + Log(sum);
    }

    double LowerTailBound(long index, double term)
    {
        if (index == 0L || term == 0.0)
        {
            return 0.0;
        }

        double ratio = Exp(dispersion * Log((double)index) - logRate);
        if (ratio >= 1.0)
        {
            return double.PositiveInfinity;
        }

        return term * ratio / (1.0 - ratio);
    }

    double UpperTailBound(long index, double term)
    {
        if (term == 0.0)
        {
            return 0.0;
        }

        double ratio = Exp(logRate - dispersion * Log((double)(index + 1L)));
        if (ratio >= 1.0)
        {
            return double.PositiveInfinity;
        }

        return term * ratio / (1.0 - ratio);
    }

    PoissonSampler GetPoissonSampler()
    {
        if (poissonSampler == null)
        {
            throw RandomStateException.Create("The Conway-Maxwell-Poisson Poisson proposal is unavailable.");
        }

        return poissonSampler;
    }

    GeometricSampler GetGeometricSampler()
    {
        if (geometricSampler == null)
        {
            throw RandomStateException.Create("The Conway-Maxwell-Poisson geometric proposal is unavailable.");
        }

        return geometricSampler;
    }

    double SelectGeometricProbability(double effectiveRate)
    {
        double product = 2.0 * effectiveRate * dispersion;
        double probability;
        if (double.IsFinite(product))
        {
            probability = 2.0 * dispersion / (product + 1.0 + dispersion);
        }
        else
        {
            probability = 0.0;
        }

        if (rate < 1.0)
        {
            double geometricLimit = 1.0 - rate;
            if (geometricLimit > probability)
            {
                probability = geometricLimit;
            }
        }

        return probability;
    }

    double LogFactorial(double count)
    {
        return PortableSpecialFunctions.LogGamma(count + 1.0);
    }

    double Exp(double value)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Exp(value);
        }

        return Math.Exp(value);
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
}
