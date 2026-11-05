using System;

namespace random;

/// <summary>
/// Generates double-precision values from the generalized normal distribution, also called the generalized error or exponential-power distribution.
/// </summary>
/// <remarks>
/// For location <c>mu</c>, scale <c>alpha</c>, and shape <c>p</c>, the density is
/// <c>p exp(-(|x-mu|/alpha)^p) / (2 alpha Gamma(1/p))</c>. Thus <c>alpha</c>
/// is the exponential-power scale rather than the standard deviation. Shape one is
/// Laplace and shape two is normal with standard deviation <c>alpha/sqrt(2)</c>.
/// The sampler retains the supplied mutable generator and is not safe for concurrent
/// use without external synchronization.
/// </remarks>
public class GeneralizedNormalSampler : IGeneralizedNormalSampler
{
    IRandomSource generator;
    GammaSampler boostedGammaSampler;
    double location;
    double scale;
    double inverseShape;
    bool reciprocalOverflow;
    RandomMathMode mathMode;

    /// <summary>
    /// Creates a generalized normal sampler with the requested raw location, scale, and shape.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits and selects the math mode.</param>
    /// <param name="location">The finite distribution location.</param>
    /// <param name="scale">The finite nonnegative exponential-power scale.</param>
    /// <param name="shape">The finite positive exponential-power shape.</param>
    /// <remarks>
    /// A zero scale defines a point mass at the location and consumes no random state.
    /// Representational tail overflow can return an infinity.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the location or scale is non-finite, the scale is negative, or the shape is non-finite or not positive.</exception>
    public GeneralizedNormalSampler(IRandomSource generator, double location, double scale, double shape)
    {
        ValidateParameters(generator, location, scale, shape);
        this.generator = generator;
        boostedGammaSampler = CreateBoostedGammaSampler(generator, shape);
        Initialize(generator, location, scale, shape);
    }

    /// <summary>
    /// Returns one value from this sampler's generalized normal distribution.
    /// </summary>
    /// <returns>A generalized normal value, including a possible signed infinity after representational overflow.</returns>
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

        double signedMagnitude = generator.NextBoolean() ? magnitude : -magnitude;
        return location + scale * signedMagnitude;
    }

    /// <summary>
    /// Fills a destination with values from this sampler's generalized normal distribution.
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

    void Initialize(IRandomSource generator, double location, double scale, double shape)
    {
        double selectedInverseShape = 1.0 / shape;
        bool selectedReciprocalOverflow = double.IsPositiveInfinity(selectedInverseShape);
        this.location = location;
        this.scale = scale;
        inverseShape = selectedReciprocalOverflow ? double.MaxValue : selectedInverseShape;
        reciprocalOverflow = selectedReciprocalOverflow;
        mathMode = generator.MathMode();
    }

    static GammaSampler CreateBoostedGammaSampler(IRandomSource generator, double shape)
    {
        double inverseShape = 1.0 / shape;
        double boostedShape = double.IsPositiveInfinity(inverseShape) ? double.MaxValue : inverseShape + 1.0;
        return new GammaSampler(generator, boostedShape, 1.0);
    }

    internal static void ValidateParameters(IRandomSource generator, double location, double scale, double shape)
    {
        Guard.NotNull(generator, "generator");
        Guard.Finite(location, "location");
        Guard.Finite(scale, "scale");
        if (scale < 0.0)
        {
            throw RandomArgumentException.Create("scale", "The scale must not be negative.");
        }

        Guard.Finite(shape, "shape");
        if (shape <= 0.0)
        {
            throw RandomArgumentException.Create("shape", "The shape must be positive.");
        }
    }
}
