using System;

namespace random;

/// <summary>
/// Generates double-precision angles from a von Mises distribution.
/// </summary>
/// <remarks>
/// Angles are normalized to <c>[-pi,pi)</c>. Zero and concentrations below
/// <c>1e-8</c> use the uniform circular limit. Moderate concentrations use the
/// Best-Fisher wrapped-Cauchy rejection algorithm, with a small-concentration
/// expansion below <c>1e-5</c>. Concentrations above <c>1e6</c> use the concentrated
/// wrapped-normal limit. Positive infinity is an exact point mass at the normalized
/// mean direction. The sampler retains the supplied mutable generator and is not safe
/// for concurrent use without external synchronization.
/// </remarks>
public class VonMisesSampler : IVonMisesSampler
{
    const double Pi = 3.14159265358979323846;
    const double TwoPi = 6.28318530717958647693;
    const double UniformThreshold = 1.0e-8;
    const double TaylorThreshold = 1.0e-5;
    const double NormalThreshold = 1.0e6;
    IRandomSource generator;
    NormalSampler normalSampler;
    double meanDirection;
    double concentration;
    double rejectionScale;
    RandomMathMode mathMode;

    /// <summary>
    /// Creates a von Mises sampler with the requested mean direction and concentration.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits and selects the math mode.</param>
    /// <param name="meanDirection">The finite mean direction in radians; it is normalized to <c>[-pi,pi)</c>.</param>
    /// <param name="concentration">The nonnegative concentration, including positive infinity.</param>
    /// <remarks>Positive infinity defines a point mass at the normalized mean direction and consumes no random state.</remarks>
    /// <exception cref="RandomArgumentException">The generator is null, the mean direction is non-finite, or the concentration is NaN, negative, or negative infinity.</exception>
    public VonMisesSampler(IRandomSource generator, double meanDirection, double concentration)
    {
        ValidateParameters(generator, meanDirection, concentration);
        this.generator = generator;
        normalSampler = new NormalSampler(generator, 0.0, 1.0);
        Initialize(generator, meanDirection, concentration);
    }

    /// <summary>
    /// Returns one normalized angle from this sampler's von Mises distribution.
    /// </summary>
    /// <returns>An angle in <c>[-pi,pi)</c>.</returns>
    public double Sample()
    {
        if (double.IsPositiveInfinity(concentration))
        {
            return meanDirection;
        }

        if (concentration < UniformThreshold)
        {
            double uniformOffset = Pi * (2.0 * FloatingUniform.NextDoubleClosedOpen(generator) - 1.0);
            return Normalize(meanDirection + uniformOffset);
        }

        if (concentration > NormalThreshold)
        {
            double deviation = Sqrt(1.0 / concentration) * normalSampler.Sample();
            return Normalize(meanDirection + deviation);
        }

        double cosine;
        while (true)
        {
            double firstUnit = FloatingUniform.NextDoubleClosedOpen(generator);
            double proposalCosine = Cos(Pi * firstUnit);
            cosine = (1.0 + rejectionScale * proposalCosine) / (rejectionScale + proposalCosine);
            double acceptance = concentration * (rejectionScale - cosine);
            double secondUnit = FloatingUniform.NextDoubleClosedOpen(generator);
            if (acceptance * (2.0 - acceptance) - secondUnit >= 0.0)
            {
                break;
            }

            if (Log(acceptance / secondUnit) + 1.0 - acceptance >= 0.0)
            {
                break;
            }
        }

        if (cosine < -1.0)
        {
            cosine = -1.0;
        }
        else if (cosine > 1.0)
        {
            cosine = 1.0;
        }

        double angle = Acos(cosine);
        if (FloatingUniform.NextDoubleClosedOpen(generator) < 0.5)
        {
            angle = -angle;
        }

        return Normalize(meanDirection + angle);
    }

    /// <summary>
    /// Fills a destination with normalized angles from this sampler's von Mises distribution.
    /// </summary>
    /// <param name="destination">The destination angles.</param>
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

    double Cos(double value)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Cos(value);
        }

        return Math.Cos(value);
    }

    double Acos(double value)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Acos(value);
        }

        return Math.Acos(value);
    }

    double Log(double value)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Log(value);
        }

        return Math.Log(value);
    }

    void Initialize(IRandomSource generator, double meanDirection, double concentration)
    {
        RandomMathMode selectedMathMode = generator.MathMode();
        double selectedRejectionScale = 0.0;
        if (concentration >= UniformThreshold && concentration <= NormalThreshold)
        {
            if (concentration < TaylorThreshold)
            {
                selectedRejectionScale = 1.0 / concentration + concentration;
            }
            else
            {
                double root;
                if (selectedMathMode == RandomMathMode.Portable)
                {
                    root = PortableMath.Sqrt(1.0 + 4.0 * concentration * concentration);
                }
                else
                {
                    root = Math.Sqrt(1.0 + 4.0 * concentration * concentration);
                }

                double intermediate = 1.0 + root;
                double secondRoot;
                if (selectedMathMode == RandomMathMode.Portable)
                {
                    secondRoot = PortableMath.Sqrt(2.0 * intermediate);
                }
                else
                {
                    secondRoot = Math.Sqrt(2.0 * intermediate);
                }

                double rho = (intermediate - secondRoot) / (2.0 * concentration);
                selectedRejectionScale = (1.0 + rho * rho) / (2.0 * rho);
            }
        }

        this.meanDirection = Normalize(meanDirection);
        this.concentration = concentration;
        rejectionScale = selectedRejectionScale;
        mathMode = selectedMathMode;
    }

    static double Normalize(double angle)
    {
        double normalized = angle % TwoPi;
        if (normalized < -Pi)
        {
            normalized += TwoPi;
        }
        else if (normalized >= Pi)
        {
            normalized -= TwoPi;
        }

        return normalized;
    }

    static void ValidateParameters(IRandomSource generator, double meanDirection, double concentration)
    {
        Guard.NotNull(generator, "generator");
        Guard.Finite(meanDirection, "meanDirection");
        if (double.IsNaN(concentration) || double.IsNegativeInfinity(concentration) || concentration < 0.0)
        {
            throw RandomArgumentException.Create("concentration", "The concentration must be nonnegative or positive infinity.");
        }
    }
}
