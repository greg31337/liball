using System;

namespace random;

/// <summary>
/// Generates double-precision values from a gamma distribution.
/// </summary>
/// <remarks>
/// The sampler uses Marsaglia-Tsang rejection for shapes at least one and a
/// shape-boost transform below one. Parameters use the shape-scale convention.
/// The sampler retains the supplied mutable generator and is not safe for concurrent
/// use without external synchronization.
/// </remarks>
public class GammaSampler : IGammaSampler
{
    const double OneThird = 0.333333333333333333333333333333333333;
    const double SqueezeCoefficient = 0.0331;
    IRandomSource generator;
    NormalSampler normalSampler;
    double shape;
    double scale;
    double logScale;
    double rejectionShape;
    double rejectionCoefficient;
    RandomMathMode mathMode;

    /// <summary>
    /// Creates a gamma sampler with the requested shape and scale.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits and selects the math mode.</param>
    /// <param name="shape">The finite positive distribution shape.</param>
    /// <param name="scale">The finite nonnegative distribution scale.</param>
    /// <remarks>
    /// A zero scale defines a point mass at zero and consumes no random state.
    /// Positive-shape underflow can return zero, and scaling overflow can return positive infinity.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the shape is non-finite or not positive, or the scale is non-finite or negative.</exception>
    public GammaSampler(IRandomSource generator, double shape, double scale)
    {
        ValidateParameters(generator, shape, scale);
        this.generator = generator;
        normalSampler = new NormalSampler(generator, 0.0, 1.0);
        Initialize(generator, shape, scale);
    }

    /// <summary>
    /// Returns one value from this sampler's gamma distribution.
    /// </summary>
    /// <returns>A nonnegative gamma value, or zero when the scale is zero or the result underflows.</returns>
    public double Sample()
    {
        if (scale == 0.0)
        {
            return 0.0;
        }

        double standard = SampleAdjustedShape();
        if (shape < 1.0)
        {
            double unit = FloatingUniform.NextDoubleOpenOpen(generator);
            double exponent = logScale + Log(standard) + Log(unit) / shape;
            return Exp(exponent);
        }

        return scale * standard;
    }

    /// <summary>
    /// Fills a destination with values from this sampler's gamma distribution.
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

    double SampleAdjustedShape()
    {
        while (true)
        {
            double normal = normalSampler.Sample();
            double root = 1.0 + rejectionCoefficient * normal;
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
                return rejectionShape * volume;
            }

            double acceptance = 0.5 * normalSquared + rejectionShape * (1.0 - volume + Log(volume));
            if (Log(unit) < acceptance)
            {
                return rejectionShape * volume;
            }
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

    void Initialize(IRandomSource generator, double shape, double scale)
    {
        RandomMathMode selectedMathMode = generator.MathMode();
        double selectedAdjustedShape = shape < 1.0 ? shape + 1.0 : shape;
        double selectedRejectionShape = selectedAdjustedShape - OneThird;
        double root;
        if (selectedMathMode == RandomMathMode.Portable)
        {
            root = PortableMath.Sqrt(selectedRejectionShape);
        }
        else
        {
            root = Math.Sqrt(selectedRejectionShape);
        }

        this.shape = shape;
        this.scale = scale;
        logScale = scale == 0.0 ? 0.0 : selectedMathMode == RandomMathMode.Portable ? PortableMath.Log(scale) : Math.Log(scale);
        rejectionShape = selectedRejectionShape;
        rejectionCoefficient = OneThird / root;
        mathMode = selectedMathMode;
    }

    static void ValidateParameters(IRandomSource generator, double shape, double scale)
    {
        Guard.NotNull(generator, "generator");
        Guard.Finite(shape, "shape");
        if (shape <= 0.0)
        {
            throw RandomArgumentException.Create("shape", "The shape must be positive.");
        }

        Guard.Finite(scale, "scale");
        if (scale < 0.0)
        {
            throw RandomArgumentException.Create("scale", "The scale must not be negative.");
        }
    }
}
