using System;

namespace random;

/// <summary>
/// Generates double-precision values from an inverse-Gaussian, or Wald, distribution.
/// </summary>
/// <remarks>
/// Parameters are the positive mean <c>mu</c> and positive shape <c>lambda</c> used by
/// the density proportional to <c>x^(-3/2) exp(-lambda*(x-mu)^2/(2*mu^2*x))</c>.
/// Sampling uses the Michael-Schucany-Haas construction with an algebraically stable
/// root that avoids subtracting nearly equal terms. The sampler retains the supplied
/// mutable generator and is not safe for concurrent use without external synchronization.
/// </remarks>
public class InverseGaussianSampler : IInverseGaussianSampler
{
    IRandomSource generator;
    NormalSampler normalSampler;
    double mean;
    double shape;
    RandomMathMode mathMode;

    /// <summary>
    /// Creates an inverse-Gaussian sampler with the requested mean and shape.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits and selects the math mode.</param>
    /// <param name="mean">The finite positive distribution mean.</param>
    /// <param name="shape">The finite positive Wald shape <c>lambda</c>.</param>
    /// <remarks>Representational underflow and overflow can return zero or positive infinity.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null, or the mean or shape is non-finite or not positive.</exception>
    public InverseGaussianSampler(IRandomSource generator, double mean, double shape)
    {
        ValidateParameters(generator, mean, shape);
        NormalSampler validatedNormalSampler = new NormalSampler(generator, 0.0, 1.0);
        this.generator = generator;
        normalSampler = validatedNormalSampler;
        this.mean = mean;
        this.shape = shape;
        mathMode = generator.MathMode();
    }

    /// <summary>
    /// Returns one value from this sampler's inverse-Gaussian distribution.
    /// </summary>
    /// <returns>A nonnegative inverse-Gaussian value, including zero or positive infinity after representational underflow or overflow.</returns>
    public double Sample()
    {
        double normal = normalSampler.Sample();
        double squared = normal * normal;
        if (squared == 0.0)
        {
            return mean;
        }

        double ratio = mean / shape;
        double rootParameter = ratio * (0.5 * squared);
        double baseValue;
        if (double.IsPositiveInfinity(rootParameter))
        {
            double inverseRatio = shape / mean;
            double inverseRootParameter = (inverseRatio + inverseRatio) / squared;
            if (inverseRootParameter == 0.0)
            {
                return 0.0;
            }

            double rootRatio = Sqrt(1.0 + 2.0 * inverseRootParameter);
            baseValue = inverseRootParameter / (inverseRootParameter + 1.0 + rootRatio);
        }
        else if (rootParameter > 1.0)
        {
            double inverseRootParameter = 1.0 / rootParameter;
            double rootRatio = Sqrt(1.0 + 2.0 * inverseRootParameter);
            baseValue = inverseRootParameter / (inverseRootParameter + 1.0 + rootRatio);
        }
        else
        {
            double root = Sqrt(rootParameter) * Sqrt(rootParameter + 2.0);
            baseValue = 1.0 / (1.0 + rootParameter + root);
        }

        double primary = mean * baseValue;
        double probability = 1.0 / (1.0 + baseValue);
        if (FloatingUniform.NextDoubleOpenOpen(generator) <= probability)
        {
            return primary;
        }

        return mean / baseValue;
    }

    /// <summary>
    /// Fills a destination with values from this sampler's inverse-Gaussian distribution.
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

    double Sqrt(double value)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Sqrt(value);
        }

        return Math.Sqrt(value);
    }

    static void ValidateParameters(IRandomSource generator, double mean, double shape)
    {
        Guard.NotNull(generator, "generator");
        Guard.Finite(mean, "mean");
        if (mean <= 0.0)
        {
            throw RandomArgumentException.Create("mean", "The mean must be positive.");
        }

        Guard.Finite(shape, "shape");
        if (shape <= 0.0)
        {
            throw RandomArgumentException.Create("shape", "The shape must be positive.");
        }
    }
}
