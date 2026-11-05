using System;

namespace random;

/// <summary>
/// Generates double-precision values from a skewed generalized error distribution.
/// </summary>
/// <remarks>
/// This sampler applies Fernandez-Steel inverse-scale skewing to the generalized normal
/// density <c>f</c> with exponential-power shape <c>p</c>. For skewness <c>k</c>, its
/// standardized density is proportional to <c>f(y/k)</c> on the positive half-line and
/// <c>f(k y)</c> on the negative half-line. A positive draw has probability
/// <c>k^2/(1+k^2)</c>, and its magnitude is multiplied by <c>k</c>; a negative draw has
/// its magnitude divided by <c>k</c>. Skewness one is the symmetric generalized normal.
/// Location and scale are raw transform parameters, not the resulting mean and standard
/// deviation. The sampler retains the supplied mutable generator and is not safe for
/// concurrent use without external synchronization.
/// </remarks>
public class SkewedGeneralizedErrorSampler : ISkewedGeneralizedErrorSampler
{
    IRandomSource generator;
    GammaSampler boostedGammaSampler;
    double location;
    double scale;
    double inverseShape;
    double skewness;
    double positiveProbability;
    bool reciprocalOverflow;
    RandomMathMode mathMode;

    /// <summary>
    /// Creates a skewed generalized error sampler with the requested raw location and scale.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits and selects the math mode.</param>
    /// <param name="location">The finite raw location.</param>
    /// <param name="scale">The finite nonnegative raw scale.</param>
    /// <param name="shape">The finite positive exponential-power shape.</param>
    /// <param name="skewness">The finite positive Fernandez-Steel skewness; one is symmetric.</param>
    /// <remarks>
    /// A zero scale defines a point mass at the location and consumes no random state.
    /// Representational tail or skew scaling overflow can return an infinity.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the location or scale is non-finite, the scale is negative, or shape or skewness is non-finite or not positive.</exception>
    public SkewedGeneralizedErrorSampler(IRandomSource generator, double location, double scale, double shape, double skewness)
    {
        ValidateParameters(generator, location, scale, shape, skewness);
        this.generator = generator;
        boostedGammaSampler = CreateBoostedGammaSampler(generator, shape);
        Initialize(generator, location, scale, shape, skewness);
    }

    /// <summary>
    /// Returns one value from this sampler's skewed generalized error distribution.
    /// </summary>
    /// <returns>A skewed generalized error value, including a possible signed infinity after representational overflow.</returns>
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
            double boostedGamma = boostedGammaSampler.Sample();
            double root = Power(boostedGamma, inverseShape);
            magnitude = FloatingUniform.NextDoubleOpenOpen(generator) * root;
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
    /// Fills a destination with values from this sampler's skewed generalized error distribution.
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

    double Power(double value, double exponent)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Power(value, exponent);
        }

        return Math.Pow(value, exponent);
    }

    void Initialize(IRandomSource generator, double location, double scale, double shape, double skewness)
    {
        double selectedInverseShape = 1.0 / shape;
        bool selectedReciprocalOverflow = double.IsPositiveInfinity(selectedInverseShape);
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
        inverseShape = selectedReciprocalOverflow ? double.MaxValue : selectedInverseShape;
        this.skewness = skewness;
        positiveProbability = selectedPositiveProbability;
        reciprocalOverflow = selectedReciprocalOverflow;
        mathMode = generator.MathMode();
    }

    static GammaSampler CreateBoostedGammaSampler(IRandomSource generator, double shape)
    {
        double inverseShape = 1.0 / shape;
        double boostedShape = double.IsPositiveInfinity(inverseShape) ? double.MaxValue : inverseShape + 1.0;
        return new GammaSampler(generator, boostedShape, 1.0);
    }

    static void ValidateParameters(IRandomSource generator, double location, double scale, double shape, double skewness)
    {
        GeneralizedNormalSampler.ValidateParameters(generator, location, scale, shape);
        Guard.Finite(skewness, "skewness");
        if (skewness <= 0.0)
        {
            throw RandomArgumentException.Create("skewness", "The skewness must be positive.");
        }
    }
}
