using System;

namespace random;

/// <summary>
/// Generates double-precision probability vectors from a Dirichlet distribution.
/// </summary>
/// <remarks>
/// Independent gamma variates are generated with the shape-boost identity
/// <c>Gamma(alpha + 1) * U^(1 / alpha)</c> and retained in logarithmic form.
/// Log-sum-exp normalization prevents all-zero vectors when small concentrations
/// would underflow ordinary gamma values. If the boost exponent itself exceeds
/// double range, its logarithmic magnitude is compared as an exponential race,
/// yielding the correct representable simplex vertex. The final coordinate receives
/// a deterministic remainder correction so each returned vector sums exactly to one
/// in forward double-precision accumulation. The sampler retains the supplied mutable
/// generator and is not safe for concurrent use without external synchronization.
/// </remarks>
public class DirichletSampler : IDirichletSampler
{
    const double OneThird = 0.333333333333333333333333333333333333;
    const double SqueezeCoefficient = 0.0331;
    const double MaximumLogMagnitude = 709.782712893383973096;
    IRandomSource generator;
    NormalSampler normalSampler;
    double[] concentrations;
    double[] rejectionShapes;
    double[] rejectionCoefficients;
    double[] logValues;
    double[] overflowRanks;
    RandomMathMode mathMode;

    /// <summary>
    /// Creates a Dirichlet sampler with the requested concentration vector.
    /// </summary>
    /// <param name="generator">The generator that supplies random values and selects the math mode.</param>
    /// <param name="concentrations">The nonempty finite positive concentration vector.</param>
    /// <remarks>
    /// The concentration array is copied. A one-component vector defines the exact
    /// simplex point <c>[1]</c> and consumes no random state.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator or concentration array is null, the array is empty, or an entry is non-finite or not positive.</exception>
    public DirichletSampler(IRandomSource generator, double[] concentrations)
    {
        Guard.NotNull(generator, "generator");
        Guard.NotNull(concentrations, "concentrations");
        if (concentrations.Length == 0)
        {
            throw RandomArgumentException.Create("concentrations", "At least one concentration is required.");
        }

        int index = 0;
        while (index < concentrations.Length)
        {
            double concentration = concentrations[index];
            if (!double.IsFinite(concentration) || concentration <= 0.0)
            {
                throw RandomArgumentException.Create("concentrations", "Every concentration must be finite and positive.");
            }

            index++;
        }

        this.generator = generator;
        this.concentrations = (double[])concentrations.Clone();
        mathMode = generator.MathMode();
        normalSampler = new NormalSampler(generator, 0.0, 1.0);
        rejectionShapes = new double[concentrations.Length];
        rejectionCoefficients = new double[concentrations.Length];
        logValues = new double[concentrations.Length];
        overflowRanks = new double[concentrations.Length];
        index = 0;
        while (index < concentrations.Length)
        {
            double adjustedShape = concentrations[index] + 1.0;
            double rejectionShape = adjustedShape - OneThird;
            rejectionShapes[index] = rejectionShape;
            rejectionCoefficients[index] = OneThird / Sqrt(rejectionShape);
            index++;
        }
    }

    /// <summary>
    /// Returns the number of components represented by each sampled vector.
    /// </summary>
    /// <returns>The positive component count.</returns>
    public int ComponentCount()
    {
        return concentrations.Length;
    }

    /// <summary>
    /// Returns the exact storage length required for a row-major batch.
    /// </summary>
    /// <param name="sampleCount">The nonnegative number of probability-vector rows.</param>
    /// <returns>The product of the sample count and <see cref="ComponentCount()"/>.</returns>
    /// <exception cref="RandomArgumentException">The sample count is negative or the required length exceeds <c>int.MaxValue</c>.</exception>
    public int RequiredBatchOutputLength(int sampleCount)
    {
        return MultivariateDistributions.RequiredVectorOutputLength(sampleCount, concentrations.Length);
    }

    /// <summary>
    /// Creates one probability vector from this sampler's Dirichlet distribution.
    /// </summary>
    /// <returns>A new nonnegative vector whose entries sum exactly to one.</returns>
    public double[] Sample()
    {
        double[] result = new double[concentrations.Length];
        SampleInto(result.AsSpan());
        return result;
    }

    /// <summary>
    /// Fills a row-major batch with probability vectors from this Dirichlet distribution.
    /// </summary>
    /// <param name="sampleCount">The nonnegative number of probability-vector rows.</param>
    /// <param name="destination">The destination, whose length must equal <see cref="RequiredBatchOutputLength(int)"/> for the sample count.</param>
    /// <remarks>The result and generator state match repeated calls to <see cref="SampleInto(Span{double})"/> in increasing row order.</remarks>
    /// <exception cref="RandomArgumentException">The sample count is invalid or the destination length does not match the required row-major storage.</exception>
    public void Fill(int sampleCount, Span<double> destination)
    {
        int requiredLength = RequiredBatchOutputLength(sampleCount);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        int row = 0;
        int offset = 0;
        while (row < sampleCount)
        {
            SampleInto(destination.Slice(offset, concentrations.Length));
            offset += concentrations.Length;
            row++;
        }
    }

    /// <summary>
    /// Writes one probability vector from this sampler's Dirichlet distribution.
    /// </summary>
    /// <param name="destination">The destination, whose length must equal <see cref="ComponentCount()"/>.</param>
    /// <remarks>
    /// Every entry is finite and nonnegative, and forward accumulation of the entries
    /// equals exactly one. Mathematically positive components can round to zero when
    /// concentration is extremely sparse.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The destination length does not equal <see cref="ComponentCount()"/>.</exception>
    public void SampleInto(Span<double> destination)
    {
        Guard.ExactSpanLength(destination.Length, concentrations.Length, "destination");
        if (concentrations.Length == 1)
        {
            destination[0] = 1.0;
            return;
        }

        double maximumLogValue = double.NegativeInfinity;
        int index = 0;
        while (index < concentrations.Length)
        {
            double logValue = SampleLogGamma(index);
            logValues[index] = logValue;
            if (logValue > maximumLogValue)
            {
                maximumLogValue = logValue;
            }

            index++;
        }

        if (maximumLogValue == double.NegativeInfinity)
        {
            SetExtremeSparseVertex(destination);
            return;
        }

        double total = 0.0;
        double compensation = 0.0;
        index = 0;
        while (index < logValues.Length)
        {
            double scaled = logValues[index] == double.NegativeInfinity ? 0.0 : Exp(logValues[index] - maximumLogValue);
            destination[index] = scaled;
            double adjusted = scaled - compensation;
            double next = total + adjusted;
            compensation = (next - total) - adjusted;
            total = next;
            index++;
        }

        index = 0;
        while (index < destination.Length)
        {
            destination[index] /= total;
            index++;
        }

        CorrectSimplexSum(destination);
    }

    double SampleLogGamma(int index)
    {
        double acceptedLog;
        while (true)
        {
            double normal = normalSampler.Sample();
            double root = 1.0 + rejectionCoefficients[index] * normal;
            if (root <= 0.0)
            {
                continue;
            }

            double rootSquared = root * root;
            double volume = rootSquared * root;
            double unit = FloatingUniform.NextDoubleOpenOpen(generator);
            double normalSquared = normal * normal;
            double normalFourth = normalSquared * normalSquared;
            if (unit < 1.0 - SqueezeCoefficient * normalFourth)
            {
                acceptedLog = Log(rejectionShapes[index]) + Log(volume);
                break;
            }

            double acceptance = 0.5 * normalSquared + rejectionShapes[index] * (1.0 - volume + Log(volume));
            if (Log(unit) < acceptance)
            {
                acceptedLog = Log(rejectionShapes[index]) + Log(volume);
                break;
            }
        }

        double boostUnit = FloatingUniform.NextDoubleOpenOpen(generator);
        double negativeLogUnit = -Log(boostUnit);
        double magnitudeLog = Log(negativeLogUnit) - Log(concentrations[index]);
        overflowRanks[index] = magnitudeLog;
        if (magnitudeLog > MaximumLogMagnitude)
        {
            return double.NegativeInfinity;
        }

        return acceptedLog - Exp(magnitudeLog);
    }

    void SetExtremeSparseVertex(Span<double> destination)
    {
        int selected = 0;
        double minimumRank = overflowRanks[0];
        int index = 1;
        while (index < overflowRanks.Length)
        {
            if (overflowRanks[index] < minimumRank)
            {
                minimumRank = overflowRanks[index];
                selected = index;
            }

            index++;
        }

        index = 0;
        while (index < destination.Length)
        {
            destination[index] = index == selected ? 1.0 : 0.0;
            index++;
        }
    }

    void CorrectSimplexSum(Span<double> destination)
    {
        int last = destination.Length - 1;
        double prefix = 0.0;
        int index = 0;
        while (index < last)
        {
            prefix += destination[index];
            index++;
        }

        if (prefix > 1.0)
        {
            double scale = PortableMath.NextDown(1.0) / prefix;
            prefix = 0.0;
            index = 0;
            while (index < last)
            {
                destination[index] *= scale;
                prefix += destination[index];
                index++;
            }
        }

        destination[last] = 1.0 - prefix;
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

    double Sqrt(double value)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Sqrt(value);
        }

        return Math.Sqrt(value);
    }
}
