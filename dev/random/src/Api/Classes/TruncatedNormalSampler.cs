using System;

namespace random;

/// <summary>
/// Generates double-precision values from a normal distribution restricted to an interval.
/// </summary>
/// <remarks>
/// Bounds are expressed in the same location and scale as the returned values and may be
/// infinite. Broad intervals containing the mean use normal rejection. One-sided tails use
/// an optimized translated-exponential proposal; finite tail intervals use its truncated
/// form. Finite intervals whose standard-normal log density varies by at most one use a
/// uniform density-envelope proposal, so narrow intervals retain a bounded acceptance rate
/// even far into a tail.
/// <para>
/// Equal finite bounds define a point mass. A zero standard deviation is valid only when its
/// mean lies within the bounds; both cases consume no random state. If standardization makes
/// distinct finite bounds indistinguishable, sampling uses the approximately uniform density
/// in the original interval instead of rejecting indefinitely.
/// </para>
/// The sampler retains the supplied mutable generator and is not safe for concurrent use
/// without external synchronization.
/// </remarks>
public class TruncatedNormalSampler : ITruncatedNormalSampler
{
    const int PointAlgorithm = 0;
    const int NormalAlgorithm = 1;
    const int UniformAlgorithm = 2;
    const int PositiveTailAlgorithm = 3;
    const int NegativeTailAlgorithm = 4;
    const int OriginalUniformAlgorithm = 5;
    IRandomSource generator;
    NormalSampler normalSampler;
    double mean;
    double standardDeviation;
    double lowerBound;
    double upperBound;
    double standardLower;
    double standardUpper;
    double pointValue;
    int algorithm;
    RandomMathMode mathMode;

    /// <summary>
    /// Creates a normal sampler with the requested mean and standard deviation, restricted to the requested bounds.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits and selects the math mode.</param>
    /// <param name="mean">The finite untruncated-normal mean.</param>
    /// <param name="standardDeviation">The finite nonnegative untruncated-normal standard deviation.</param>
    /// <param name="lowerBound">The inclusive lower bound, including negative infinity.</param>
    /// <param name="upperBound">The inclusive upper bound, including positive infinity.</param>
    /// <remarks>
    /// Equal finite bounds define a point mass at that bound. A zero standard deviation
    /// defines a point mass at the mean when the mean is inside the interval.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the mean or standard deviation is invalid, a bound is NaN, the bounds are reversed, equal bounds are non-finite, or a zero-deviation mean is outside the bounds.</exception>
    public TruncatedNormalSampler(
        IRandomSource generator,
        double mean,
        double standardDeviation,
        double lowerBound,
        double upperBound)
    {
        ValidateParameters(generator, mean, standardDeviation, lowerBound, upperBound);
        this.generator = generator;
        normalSampler = new NormalSampler(generator, 0.0, 1.0);
        Initialize(generator, mean, standardDeviation, lowerBound, upperBound);
    }

    /// <summary>
    /// Returns one value from this sampler's truncated normal distribution.
    /// </summary>
    /// <returns>A value within the configured inclusive bounds.</returns>
    public double Sample()
    {
        if (algorithm == PointAlgorithm)
        {
            return pointValue;
        }

        if (algorithm == OriginalUniformAlgorithm)
        {
            return FloatingUniform.NextDouble(generator, lowerBound, upperBound);
        }

        double standard;
        if (algorithm == NormalAlgorithm)
        {
            standard = SampleCentral();
        }
        else if (algorithm == UniformAlgorithm)
        {
            standard = SampleUniformEnvelope();
        }
        else if (algorithm == PositiveTailAlgorithm)
        {
            standard = SamplePositiveTail(standardLower, standardUpper);
        }
        else
        {
            standard = -SamplePositiveTail(-standardUpper, -standardLower);
        }

        double value = mean + standardDeviation * standard;
        if (value < lowerBound)
        {
            return lowerBound;
        }

        if (value > upperBound)
        {
            return upperBound;
        }

        return value;
    }

    /// <summary>
    /// Fills a destination with values from this sampler's truncated normal distribution.
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

    double SampleCentral()
    {
        while (true)
        {
            double candidate = normalSampler.Sample();
            if (candidate >= standardLower && candidate <= standardUpper)
            {
                return candidate;
            }
        }
    }

    double SampleUniformEnvelope()
    {
        double mode;
        if (standardLower > 0.0)
        {
            mode = standardLower;
        }
        else if (standardUpper < 0.0)
        {
            mode = standardUpper;
        }
        else
        {
            mode = 0.0;
        }

        double width = standardUpper - standardLower;
        while (true)
        {
            double unit = FloatingUniform.NextDoubleOpenOpen(generator);
            double candidate;
            if (unit < 0.5)
            {
                candidate = standardLower + width * unit;
            }
            else
            {
                candidate = standardUpper - width * (1.0 - unit);
            }

            double logAcceptance = -0.5 * (candidate - mode) * (candidate + mode);
            if (Log(FloatingUniform.NextDoubleOpenOpen(generator)) <= logAcceptance)
            {
                return candidate;
            }
        }
    }

    double SamplePositiveTail(double lower, double upper)
    {
        double rate;
        if (lower > 1.0e154)
        {
            rate = lower;
        }
        else
        {
            rate = 0.5 * lower + Sqrt(0.25 * lower * lower + 1.0);
        }

        while (true)
        {
            double distance;
            if (double.IsPositiveInfinity(upper))
            {
                distance = -Log(FloatingUniform.NextDoubleOpenOpen(generator)) / rate;
            }
            else
            {
                double rateWidth = rate * (upper - lower);
                double retainedProbability = -Expm1(-rateWidth);
                double unit = FloatingUniform.NextDoubleOpenOpen(generator);
                distance = -Log1p(-retainedProbability * unit) / rate;
            }

            double candidate = lower + distance;
            if (candidate > upper)
            {
                candidate = upper;
            }

            double difference = candidate - rate;
            double logAcceptance = -0.5 * difference * difference;
            if (Log(FloatingUniform.NextDoubleOpenOpen(generator)) <= logAcceptance)
            {
                return candidate;
            }
        }
    }

    bool UseUniformEnvelope(double lower, double upper)
    {
        if (!double.IsFinite(lower) || !double.IsFinite(upper))
        {
            return false;
        }

        double width = upper - lower;
        if (!double.IsFinite(width))
        {
            return false;
        }

        double modeMagnitude;
        double farMagnitude;
        if (lower > 0.0)
        {
            modeMagnitude = lower;
            farMagnitude = upper;
        }
        else if (upper < 0.0)
        {
            modeMagnitude = -upper;
            farMagnitude = -lower;
        }
        else
        {
            modeMagnitude = 0.0;
            farMagnitude = Math.Max(-lower, upper);
        }

        double logDensityRange = 0.5 * width * (farMagnitude + modeMagnitude);
        if (logDensityRange <= 1.0)
        {
            return true;
        }

        if (lower >= 0.0)
        {
            double rate;
            if (lower > 1.0e154)
            {
                rate = lower;
            }
            else
            {
                rate = 0.5 * lower + Sqrt(0.25 * lower * lower + 1.0);
            }

            return upper <= rate;
        }

        if (upper <= 0.0)
        {
            double reflectedLower = -upper;
            double rate;
            if (reflectedLower > 1.0e154)
            {
                rate = reflectedLower;
            }
            else
            {
                rate = 0.5 * reflectedLower + Sqrt(0.25 * reflectedLower * reflectedLower + 1.0);
            }

            return -lower <= rate;
        }

        return false;
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
        double squared = reduced * reduced;
        double term = reduced;
        double sum = term;
        int denominator = 3;
        while (denominator <= 39)
        {
            term *= squared;
            sum += term / denominator;
            denominator += 2;
        }

        return 2.0 * sum;
    }

    double Expm1(double value)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Expm1(value);
        }

        if (Math.Abs(value) <= 0.5)
        {
            double half = value * 0.5;
            return 2.0 * Math.Exp(half) * Math.Sinh(half);
        }

        return Math.Exp(value) - 1.0;
    }

    void Initialize(
        IRandomSource generator,
        double mean,
        double standardDeviation,
        double lowerBound,
        double upperBound)
    {
        this.mean = mean;
        this.standardDeviation = standardDeviation;
        this.lowerBound = lowerBound;
        this.upperBound = upperBound;
        standardLower = double.NegativeInfinity;
        standardUpper = double.PositiveInfinity;
        pointValue = mean;
        algorithm = PointAlgorithm;
        mathMode = generator.MathMode();

        if (standardDeviation == 0.0)
        {
            return;
        }

        if (lowerBound == upperBound)
        {
            pointValue = lowerBound;
            return;
        }

        standardLower = double.IsNegativeInfinity(lowerBound)
            ? double.NegativeInfinity
            : (lowerBound - mean) / standardDeviation;
        standardUpper = double.IsPositiveInfinity(upperBound)
            ? double.PositiveInfinity
            : (upperBound - mean) / standardDeviation;

        if (standardLower == standardUpper && double.IsFinite(standardLower))
        {
            algorithm = OriginalUniformAlgorithm;
            return;
        }

        if (double.IsPositiveInfinity(standardLower))
        {
            pointValue = lowerBound;
            return;
        }

        if (double.IsNegativeInfinity(standardUpper))
        {
            pointValue = upperBound;
            return;
        }

        if (UseUniformEnvelope(standardLower, standardUpper))
        {
            algorithm = UniformAlgorithm;
        }
        else if (standardLower >= 0.0)
        {
            algorithm = PositiveTailAlgorithm;
        }
        else if (standardUpper <= 0.0)
        {
            algorithm = NegativeTailAlgorithm;
        }
        else
        {
            algorithm = NormalAlgorithm;
        }
    }

    static void ValidateParameters(
        IRandomSource generator,
        double mean,
        double standardDeviation,
        double lowerBound,
        double upperBound)
    {
        Guard.NotNull(generator, "generator");
        Guard.Finite(mean, "mean");
        Guard.Finite(standardDeviation, "standardDeviation");
        if (standardDeviation < 0.0)
        {
            throw RandomArgumentException.Create("standardDeviation", "The standard deviation must not be negative.");
        }

        if (double.IsNaN(lowerBound))
        {
            throw RandomArgumentException.Create("lowerBound", "The lower bound must not be NaN.");
        }

        if (double.IsNaN(upperBound) || upperBound < lowerBound)
        {
            throw RandomArgumentException.Create("upperBound", "The upper bound must not be NaN or less than the lower bound.");
        }

        if (lowerBound == upperBound && !double.IsFinite(lowerBound))
        {
            throw RandomArgumentException.Create("upperBound", "Equal bounds must be finite.");
        }

        if (standardDeviation == 0.0 && (mean < lowerBound || mean > upperBound))
        {
            throw RandomArgumentException.Create("standardDeviation", "A zero-deviation mean must lie within the bounds.");
        }
    }
}
