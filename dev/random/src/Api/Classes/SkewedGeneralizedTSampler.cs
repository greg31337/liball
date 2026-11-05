using System;

namespace random;

/// <summary>
/// Generates double-precision values from a Fernandez-Steel skewed generalized t distribution.
/// </summary>
/// <remarks>
/// Before skewing, body shape <c>p</c> and tail shape <c>q</c> define the symmetric
/// density proportional to <c>(1+|z|^p/q)^(-(q+1/p))</c>. Equivalently, the absolute
/// value raised to <c>p</c>, then divided by <c>q</c>, has a beta-prime distribution
/// with shapes <c>1/p</c> and <c>q</c>. Fernandez-Steel skewness <c>k</c> multiplies a
/// positive magnitude by <c>k</c>, divides a negative magnitude by <c>k</c>, and gives
/// the positive side probability <c>k^2/(1+k^2)</c>. Skewness one is symmetric. With
/// <c>p=2</c>, <c>q=nu/2</c>, skewness one, and scale <c>sqrt(2)</c>, this is Student's
/// t with <c>nu</c> degrees of freedom. Location and scale are raw transform parameters,
/// not the resulting mean and standard deviation. The sampler retains the supplied
/// mutable generator and is not safe for concurrent use without external synchronization.
/// </remarks>
public class SkewedGeneralizedTSampler : ISkewedGeneralizedTSampler
{
    IRandomSource generator;
    GammaSampler boostedBodyGammaSampler;
    GammaSampler boostedTailGammaSampler;
    double location;
    double scale;
    double inverseBodyShape;
    double tailShape;
    double skewness;
    double positiveProbability;
    bool reciprocalOverflow;
    RandomMathMode mathMode;

    /// <summary>
    /// Creates a skewed generalized t sampler with the requested raw location and scale.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits and selects the math mode.</param>
    /// <param name="location">The finite raw location.</param>
    /// <param name="scale">The finite nonnegative raw scale.</param>
    /// <param name="bodyShape">The finite positive exponential-power body shape.</param>
    /// <param name="tailShape">The finite positive beta-prime tail shape.</param>
    /// <param name="skewness">The finite positive Fernandez-Steel skewness; one is symmetric.</param>
    /// <remarks>
    /// A zero scale defines a point mass at the location and consumes no random state.
    /// Representational tail or skew scaling overflow can return an infinity.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the location or scale is non-finite, the scale is negative, or a body-shape, tail-shape, or skewness parameter is non-finite or not positive.</exception>
    public SkewedGeneralizedTSampler(IRandomSource generator, double location, double scale, double bodyShape, double tailShape, double skewness)
    {
        ValidateParameters(generator, location, scale, bodyShape, tailShape, skewness);
        this.generator = generator;
        boostedBodyGammaSampler = CreateBoostedGammaSampler(generator, bodyShape);
        boostedTailGammaSampler = new GammaSampler(generator, tailShape + 1.0, 1.0);
        Initialize(generator, location, scale, bodyShape, tailShape, skewness);
    }

    /// <summary>
    /// Returns one value from this sampler's skewed generalized t distribution.
    /// </summary>
    /// <returns>A skewed generalized t value, including a possible signed infinity after representational overflow.</returns>
    public double Sample()
    {
        if (scale == 0.0)
        {
            return location;
        }

        double magnitude;
        if (reciprocalOverflow)
        {
            magnitude = double.PositiveInfinity;
        }
        else
        {
            double bodyGamma = boostedBodyGammaSampler.Sample();
            double tailGamma = boostedTailGammaSampler.Sample();
            double bodyUnit = FloatingUniform.NextDoubleOpenOpen(generator);
            double tailUnit = FloatingUniform.NextDoubleOpenOpen(generator);
            double logarithm = inverseBodyShape * (Log(tailShape) + Log(bodyGamma) - Log(tailGamma));
            logarithm += Log(bodyUnit);
            logarithm -= inverseBodyShape / tailShape * Log(tailUnit);
            magnitude = Exp(logarithm);
        }

        bool positive;
        if (positiveProbability == 0.5)
        {
            positive = generator.NextBoolean();
        }
        else
        {
            positive = ProbabilityUniform.NextBoolean(generator, positiveProbability);
        }

        double skewed = positive ? magnitude * skewness : -magnitude / skewness;
        return location + scale * skewed;
    }

    /// <summary>
    /// Fills a destination with values from this sampler's skewed generalized t distribution.
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

    void Initialize(IRandomSource generator, double location, double scale, double bodyShape, double tailShape, double skewness)
    {
        double selectedInverseBodyShape = 1.0 / bodyShape;
        bool selectedReciprocalOverflow = double.IsPositiveInfinity(selectedInverseBodyShape);
        double selectedPositiveProbability;
        if (skewness > 1.0)
        {
            double inverseSkewness = 1.0 / skewness;
            selectedPositiveProbability = 1.0 / (1.0 + inverseSkewness * inverseSkewness);
        }
        else
        {
            double squaredSkewness = skewness * skewness;
            selectedPositiveProbability = squaredSkewness / (1.0 + squaredSkewness);
        }

        this.location = location;
        this.scale = scale;
        inverseBodyShape = selectedReciprocalOverflow ? double.MaxValue : selectedInverseBodyShape;
        this.tailShape = tailShape;
        this.skewness = skewness;
        positiveProbability = selectedPositiveProbability;
        reciprocalOverflow = selectedReciprocalOverflow;
        mathMode = generator.MathMode();
    }

    static GammaSampler CreateBoostedGammaSampler(IRandomSource generator, double bodyShape)
    {
        double inverseBodyShape = 1.0 / bodyShape;
        double boostedBodyShape = double.IsPositiveInfinity(inverseBodyShape) ? double.MaxValue : inverseBodyShape + 1.0;
        return new GammaSampler(generator, boostedBodyShape, 1.0);
    }

    static void ValidateParameters(IRandomSource generator, double location, double scale, double bodyShape, double tailShape, double skewness)
    {
        GeneralizedNormalSampler.ValidateParameters(generator, location, scale, bodyShape);
        Guard.Finite(tailShape, "tailShape");
        if (tailShape <= 0.0)
        {
            throw RandomArgumentException.Create("tailShape", "The tail shape must be positive.");
        }

        Guard.Finite(skewness, "skewness");
        if (skewness <= 0.0)
        {
            throw RandomArgumentException.Create("skewness", "The skewness must be positive.");
        }
    }
}
