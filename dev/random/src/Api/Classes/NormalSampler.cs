using System;

namespace random;

/// <summary>
/// Generates double-precision values from a normal distribution.
/// </summary>
/// <remarks>
/// Sampling uses a 128-layer ziggurat table with rejection checks and an explicit
/// exponential-tail algorithm. The sampler retains the supplied mutable generator
/// and is not safe for concurrent use without external synchronization.
/// </remarks>
public class NormalSampler : INormalSampler
{
    const int LayerCount = 128;
    const double IntegerScale = 2147483648.0;
    const double TailBoundary = 3.442619855899;
    const double LayerArea = 9.91256303526217e-3;
    static readonly double[] widths = new double[LayerCount];
    static readonly double[] densities = new double[LayerCount];
    static readonly uint[] thresholds = InitializeTables();
    IRandomSource generator;
    double mean;
    double standardDeviation;
    RandomMathMode mathMode;

    /// <summary>
    /// Creates a normal sampler with the requested mean and standard deviation.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits and selects the math mode.</param>
    /// <param name="mean">The finite distribution mean.</param>
    /// <param name="standardDeviation">The finite nonnegative standard deviation.</param>
    /// <remarks>
    /// A zero standard deviation defines a point mass at the mean and consumes no random state.
    /// Scaling an unbounded variate can overflow when the configured magnitude approaches the
    /// limits of <c>double</c>.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the mean or standard deviation is non-finite, or the standard deviation is negative.</exception>
    public NormalSampler(IRandomSource generator, double mean, double standardDeviation)
    {
        Guard.NotNull(generator, "generator");
        Guard.Finite(mean, "mean");
        Guard.Finite(standardDeviation, "standardDeviation");
        if (standardDeviation < 0.0)
        {
            throw RandomArgumentException.Create("standardDeviation", "The standard deviation must not be negative.");
        }

        this.generator = generator;
        this.mean = mean;
        this.standardDeviation = standardDeviation;
        mathMode = generator.MathMode();
    }

    /// <summary>
    /// Returns one value from this sampler's normal distribution.
    /// </summary>
    /// <returns>A normally distributed value, or the exact mean when the standard deviation is zero.</returns>
    public double Sample()
    {
        if (standardDeviation == 0.0)
        {
            return mean;
        }

        double standard = SampleStandard();
        return mean + standardDeviation * standard;
    }

    /// <summary>
    /// Fills a destination with values from this sampler's normal distribution.
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

    double SampleStandard()
    {
        while (true)
        {
            uint word = generator.NextUInt32();
            int layer = (int)(word & (LayerCount - 1U));
            int signedWord = SignedValue(word);
            uint magnitude = Magnitude(signedWord);
            if (magnitude < thresholds[layer])
            {
                return (double)signedWord * widths[layer];
            }

            double candidate = (double)signedWord * widths[layer];
            if (layer == 0)
            {
                return SampleTail(signedWord < 0);
            }

            double height = densities[layer] + FloatingUniform.NextDoubleClosedOpen(generator) * (densities[layer - 1] - densities[layer]);
            if (height < Density(candidate))
            {
                return candidate;
            }
        }
    }

    double SampleTail(bool negative)
    {
        while (true)
        {
            double distance = -Log(FloatingUniform.NextDoubleOpenOpen(generator)) / TailBoundary;
            double height = -Log(FloatingUniform.NextDoubleOpenOpen(generator));
            if (distance * distance <= height + height)
            {
                double value = TailBoundary + distance;
                return negative ? -value : value;
            }
        }
    }

    double Density(double value)
    {
        double exponent = -0.5 * value * value;
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Exp(exponent);
        }

        return Math.Exp(exponent);
    }

    double Log(double value)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Log(value);
        }

        return Math.Log(value);
    }

    static int SignedValue(uint word)
    {
        if (word <= int.MaxValue)
        {
            return (int)word;
        }

        return int.MinValue + (int)(word - 0x80000000U);
    }

    static uint Magnitude(int value)
    {
        if (value == int.MinValue)
        {
            return 0x80000000U;
        }

        if (value < 0)
        {
            return (uint)(-value);
        }

        return (uint)value;
    }

    static uint[] InitializeTables()
    {
        uint[] initializedThresholds = new uint[LayerCount];
        double boundary = TailBoundary;
        double previousBoundary = boundary;
        double boundaryDensity = PortableMath.Exp(-0.5 * boundary * boundary);
        double baseWidth = LayerArea / boundaryDensity;

        initializedThresholds[0] = (uint)(boundary / baseWidth * IntegerScale);
        initializedThresholds[1] = 0U;
        widths[0] = baseWidth / IntegerScale;
        widths[LayerCount - 1] = boundary / IntegerScale;
        densities[0] = 1.0;
        densities[LayerCount - 1] = boundaryDensity;

        int layer = LayerCount - 2;
        while (layer >= 1)
        {
            double nextDensity = LayerArea / boundary + PortableMath.Exp(-0.5 * boundary * boundary);
            boundary = PortableMath.Sqrt(-2.0 * PortableMath.Log(nextDensity));
            initializedThresholds[layer + 1] = (uint)(boundary / previousBoundary * IntegerScale);
            previousBoundary = boundary;
            densities[layer] = PortableMath.Exp(-0.5 * boundary * boundary);
            widths[layer] = boundary / IntegerScale;
            layer--;
        }

        return initializedThresholds;
    }
}
