using System;

namespace random;

/// <summary>
/// Generates double-precision values from a Burr type XII distribution.
/// </summary>
/// <remarks>
/// For positive shapes <c>c</c> and <c>k</c>, the standard survival function is
/// <c>(1+x^c)^(-k)</c> on the nonnegative half-line. An open survival probability
/// is transformed as <c>(u^(-1/k)-1)^(1/c)</c>. Location and scale then apply an
/// ordinary affine transform. The sampler retains the supplied mutable generator
/// and is not safe for concurrent use without external synchronization.
/// </remarks>
public class BurrSampler : IBurrSampler
{
    IRandomSource generator;
    double location;
    double scale;
    double firstShape;
    double secondShape;
    RandomMathMode mathMode;

    /// <summary>
    /// Creates a Burr type XII sampler with the requested location, scale, and shapes.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits and selects the math mode.</param>
    /// <param name="location">The finite lower endpoint.</param>
    /// <param name="scale">The finite nonnegative distribution scale.</param>
    /// <param name="firstShape">The finite positive inner power shape <c>c</c>.</param>
    /// <param name="secondShape">The finite positive outer tail shape <c>k</c>.</param>
    /// <remarks>
    /// A zero scale defines a point mass at the location and consumes no random state.
    /// Transform or location-scale overflow can return positive infinity.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the location or scale is non-finite, the scale is negative, or either shape is non-finite or not positive.</exception>
    public BurrSampler(IRandomSource generator, double location, double scale, double firstShape, double secondShape)
    {
        ValidateParameters(generator, location, scale, firstShape, secondShape);
        this.generator = generator;
        this.location = location;
        this.scale = scale;
        this.firstShape = firstShape;
        this.secondShape = secondShape;
        mathMode = generator.MathMode();
    }

    /// <summary>
    /// Returns one value from this sampler's Burr type XII distribution.
    /// </summary>
    /// <returns>A value at or above the location, including a possible positive infinity after representational overflow.</returns>
    public double Sample()
    {
        if (scale == 0.0)
        {
            return location;
        }

        double unit = FloatingUniform.NextDoubleOpenOpen(generator);
        double outerExponent = -Log(unit) / secondShape;
        double excess = Expm1(outerExponent);
        double standard;
        if (firstShape == 1.0)
        {
            standard = excess;
        }
        else if (excess == 0.0 || double.IsPositiveInfinity(excess))
        {
            standard = excess;
        }
        else
        {
            standard = Exp(Log(excess) / firstShape);
        }

        return location + scale * standard;
    }

    /// <summary>
    /// Fills a destination with values from this sampler's Burr type XII distribution.
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

    double Expm1(double value)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Expm1(value);
        }

        if (value <= 0.5)
        {
            double half = value * 0.5;
            return 2.0 * Math.Exp(half) * Math.Sinh(half);
        }

        return Math.Exp(value) - 1.0;
    }

    static void ValidateParameters(IRandomSource generator, double location, double scale, double firstShape, double secondShape)
    {
        Guard.NotNull(generator, "generator");
        Guard.Finite(location, "location");
        Guard.Finite(scale, "scale");
        if (scale < 0.0)
        {
            throw RandomArgumentException.Create("scale", "The scale must not be negative.");
        }

        Guard.Finite(firstShape, "firstShape");
        if (firstShape <= 0.0)
        {
            throw RandomArgumentException.Create("firstShape", "The first shape must be positive.");
        }

        Guard.Finite(secondShape, "secondShape");
        if (secondShape <= 0.0)
        {
            throw RandomArgumentException.Create("secondShape", "The second shape must be positive.");
        }
    }
}
