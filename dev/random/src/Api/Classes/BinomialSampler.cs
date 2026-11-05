using System;

namespace random;

/// <summary>
/// Generates signed 32-bit counts from a binomial distribution.
/// </summary>
/// <remarks>
/// Up to 64 trials are sampled as exact independent Bernoulli events. Larger trial
/// counts use stable inversion for a small effective mean and transformed rejection
/// otherwise. Probabilities above one-half use binomial symmetry. The sampler retains
/// the supplied mutable generator and is not safe for concurrent use without external
/// synchronization.
/// </remarks>
public class BinomialSampler : IBinomialSampler
{
    const int DirectTrialLimit = 64;
    const double InversionMeanLimit = 30.0;
    const double HalfLogTwoPi = 0.91893853320467274178;
    const double LogTwoPi = 1.83787706640934548356;
    const double FirstStirlingCoefficient = 0.083333333333333333333333333333333333;
    const double SecondStirlingCoefficient = 0.002777777777777777777777777777777778;
    const double ThirdStirlingCoefficient = 0.000793650793650793650793650793650794;
    const double FourthStirlingCoefficient = 0.000595238095238095238095238095238095;
    const double FifthStirlingCoefficient = 0.000841750841750841750841750841750842;
    IRandomSource generator;
    int trials;
    double probability;
    double reducedProbability;
    double reducedComplement;
    bool reflect;
    double firstCoefficient;
    double secondCoefficient;
    double center;
    double acceptanceScale;
    double fastAcceptance;
    int mode;
    double modeLogMass;
    RandomMathMode mathMode;

    /// <summary>
    /// Creates a binomial sampler with the requested trial count and success probability.
    /// </summary>
    /// <param name="generator">The generator that supplies random values and selects the math mode.</param>
    /// <param name="trials">The nonnegative number of independent trials.</param>
    /// <param name="probability">The finite success probability from zero through one inclusive.</param>
    /// <remarks>
    /// Zero trials and probabilities zero or one define deterministic counts that consume
    /// no random state.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the trial count is negative, or the probability is non-finite or outside zero through one.</exception>
    public BinomialSampler(IRandomSource generator, int trials, double probability)
    {
        Guard.NotNull(generator, "generator");
        Guard.Count(trials, "trials");
        Guard.Probability(probability, "probability");
        this.generator = generator;
        this.trials = trials;
        this.probability = probability;
        mathMode = generator.MathMode();
        reflect = probability > 0.5;
        reducedProbability = reflect ? 1.0 - probability : probability;
        reducedComplement = 1.0 - reducedProbability;
        firstCoefficient = 0.0;
        secondCoefficient = 0.0;
        center = 0.0;
        acceptanceScale = 0.0;
        fastAcceptance = 0.0;
        mode = 0;
        modeLogMass = 0.0;

        if (trials <= DirectTrialLimit || reducedProbability == 0.0 || reducedProbability * (double)trials < InversionMeanLimit)
        {
            return;
        }

        double variance = (double)trials * reducedProbability * reducedComplement;
        double rootVariance = Sqrt(variance);
        secondCoefficient = 1.15 + 2.53 * rootVariance;
        firstCoefficient = -0.0873 + 0.0248 * secondCoefficient + 0.01 * reducedProbability;
        center = (double)trials * reducedProbability + 0.5;
        acceptanceScale = (2.83 + 5.1 / secondCoefficient) * rootVariance;
        fastAcceptance = 0.92 - 4.2 / secondCoefficient;
        mode = (int)Math.Floor(((double)trials + 1.0) * reducedProbability);
        modeLogMass = BinomialLogMass(mode);
    }

    /// <summary>
    /// Returns one count from this sampler's binomial distribution.
    /// </summary>
    /// <returns>An integer from zero through the configured trial count inclusive.</returns>
    public int Sample()
    {
        if (trials == 0 || probability == 0.0)
        {
            return 0;
        }

        if (probability == 1.0)
        {
            return trials;
        }

        if (trials <= DirectTrialLimit)
        {
            return SampleDirect();
        }

        int reducedCount;
        if (reducedProbability * (double)trials < InversionMeanLimit)
        {
            reducedCount = SampleInversion();
        }
        else
        {
            reducedCount = SampleTransformedRejection();
        }

        return reflect ? trials - reducedCount : reducedCount;
    }

    /// <summary>
    /// Fills a destination with counts from this sampler's binomial distribution.
    /// </summary>
    /// <param name="destination">The destination counts.</param>
    /// <remarks>The result and generator state match repeated calls to <see cref="Sample()"/>.</remarks>
    public void Fill(Span<int> destination)
    {
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = Sample();
            index++;
        }
    }

    int SampleDirect()
    {
        int count = 0;
        int index = 0;
        while (index < trials)
        {
            if (ProbabilityUniform.NextBoolean(generator, probability))
            {
                count++;
            }

            index++;
        }

        return count;
    }

    int SampleInversion()
    {
        double mass = Exp((double)trials * Log1p(-reducedProbability));
        double cumulative = mass;
        double unit = FloatingUniform.NextDoubleClosedOpen(generator);
        int count = 0;
        double odds = reducedProbability / reducedComplement;
        while (unit >= cumulative && count < trials)
        {
            count++;
            mass *= ((double)(trials - count + 1) / (double)count) * odds;
            double next = cumulative + mass;
            if (next == cumulative)
            {
                break;
            }

            cumulative = next;
        }

        return count;
    }

    int SampleTransformedRejection()
    {
        while (true)
        {
            double centeredUnit = FloatingUniform.NextDoubleOpenOpen(generator) - 0.5;
            double secondUnit = FloatingUniform.NextDoubleOpenOpen(generator);
            double distance = 0.5 - Math.Abs(centeredUnit);
            double candidateValue = (2.0 * firstCoefficient / distance + secondCoefficient) * centeredUnit + center;
            if (candidateValue < 0.0 || candidateValue >= (double)trials + 1.0)
            {
                continue;
            }

            int candidate = (int)Math.Floor(candidateValue);
            if (distance >= 0.07 && secondUnit <= fastAcceptance)
            {
                return candidate;
            }

            if (distance < 0.013 && secondUnit > distance)
            {
                continue;
            }

            double proposal = firstCoefficient / (distance * distance) + secondCoefficient;
            double left = Log(secondUnit * acceptanceScale / proposal);
            double right = BinomialLogMass(candidate) - modeLogMass;
            if (left <= right)
            {
                return candidate;
            }
        }
    }

    double BinomialLogMass(int count)
    {
        if (count == 0)
        {
            return (double)trials * Log(reducedComplement);
        }

        if (count == trials)
        {
            return (double)trials * Log(reducedProbability);
        }

        double countValue = (double)count;
        double remaining = (double)(trials - count);
        double trialValue = (double)trials;
        double result = StirlingError(trialValue) - StirlingError(countValue) - StirlingError(remaining);
        result -= DeviancePart(countValue, trialValue * reducedProbability);
        result -= DeviancePart(remaining, trialValue * reducedComplement);
        result -= 0.5 * (LogTwoPi + Log(countValue) + Log1p(-countValue / trialValue));
        return result;
    }

    double StirlingError(double value)
    {
        if (value < 16.0)
        {
            return PortableSpecialFunctions.LogGamma(value + 1.0) -
                (value + 0.5) * Log(value) + value - HalfLogTwoPi;
        }

        double square = value * value;
        return (FirstStirlingCoefficient -
            (SecondStirlingCoefficient -
            (ThirdStirlingCoefficient -
            (FourthStirlingCoefficient - FifthStirlingCoefficient / square) / square) / square) / square) / value;
    }

    double DeviancePart(double value, double reference)
    {
        double difference = value - reference;
        if (Math.Abs(difference) < 0.1 * (value + reference))
        {
            double ratio = difference / (value + reference);
            double sum = difference * ratio;
            double term = 2.0 * value * ratio;
            double squaredRatio = ratio * ratio;
            int denominator = 3;
            while (denominator <= 1001)
            {
                term *= squaredRatio;
                double next = sum + term / denominator;
                if (next == sum)
                {
                    return next;
                }

                sum = next;
                denominator += 2;
            }

            return sum;
        }

        if (value == 0.0)
        {
            return reference;
        }

        return value * Log(value / reference) + reference - value;
    }

    double Sqrt(double value)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Sqrt(value);
        }

        return Math.Sqrt(value);
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
