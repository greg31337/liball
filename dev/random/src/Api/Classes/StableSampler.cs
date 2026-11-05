using System;

namespace random;

/// <summary>
/// Generates double-precision values from a stable distribution.
/// </summary>
/// <remarks>
/// This sampler uses Nolan's S0 parameterization: stability <c>alpha</c> is in
/// <c>(0,2]</c>, skewness <c>beta</c> is in <c>[-1,1]</c>, and a configured value is
/// <c>location + scale * Z</c> for a standardized S0 variate <c>Z</c>. Unlike the S1
/// parameterization, S0 is continuous in stability at <c>alpha = 1</c> and in skewness
/// at <c>beta = 0</c>.
/// <para>
/// The implementation uses the continuous Chambers-Mallows-Stuck rearrangement and a
/// Weron-form correction for extreme representational tails. The exact special cases
/// are normal with variance two at <c>alpha = 2</c>, Cauchy at <c>alpha = 1</c> and
/// <c>beta = 0</c>, and shifted or reflected Levy at <c>alpha = 0.5</c> and
/// <c>|beta| = 1</c>. When <c>alpha &lt; 1</c>, full positive skew has lower support
/// <c>location - scale * tan(pi * alpha / 2)</c>; full negative skew has the reflected
/// upper support.
/// </para>
/// The sampler retains the supplied mutable generator and is not safe for concurrent
/// use without external synchronization.
/// </remarks>
public class StableSampler : IStableSampler
{
    const double PiOver2 = 1.57079632679489661923;
    const double RootTwo = 1.41421356237309504880;
    const double SmallStability = 0.02;
    IRandomSource generator;
    NormalSampler normalSampler;
    double location;
    double scale;
    double stability;
    double skewness;
    double epsilon;
    double adjustedStability;
    double inverseStability;
    double epsilonOverStability;
    double tau;
    double zeta;
    double zetaAngle;
    double weronScale;
    double lowerSupport;
    double upperSupport;
    RandomMathMode mathMode;

    /// <summary>
    /// Creates a stable sampler with the requested S0 parameters.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits and selects the math mode.</param>
    /// <param name="location">The finite S0 location.</param>
    /// <param name="scale">The finite nonnegative S0 scale.</param>
    /// <param name="stability">The stability parameter in <c>(0,2]</c>.</param>
    /// <param name="skewness">The skewness parameter in <c>[-1,1]</c>; zero is symmetric.</param>
    /// <remarks>
    /// A zero scale defines a point mass at the location and consumes no random state.
    /// A positive scale is an ordinary affine transformation in the S0 parameterization.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator is null, location or scale is non-finite, scale is negative, stability cannot be represented in the supported interval, or skewness is outside its interval.</exception>
    public StableSampler(IRandomSource generator, double location, double scale, double stability, double skewness)
    {
        ValidateParameters(generator, location, scale, stability, skewness);
        this.generator = generator;
        normalSampler = new NormalSampler(generator, 0.0, 1.0);
        Initialize(generator, location, scale, stability, skewness);
    }

    /// <summary>
    /// Returns one value from this sampler's stable distribution.
    /// </summary>
    /// <returns>A stable variate, including a possible signed infinity in a representational tail.</returns>
    public double Sample()
    {
        if (scale == 0.0)
        {
            return location;
        }

        double standard;
        if (stability == 2.0)
        {
            standard = RootTwo * normalSampler.Sample();
        }
        else if (stability == 1.0 && tau == 0.0)
        {
            standard = SampleCauchy();
        }
        else if (stability == 0.5 && Math.Abs(skewness) == 1.0)
        {
            standard = SampleLevy();
        }
        else if (stability == 1.0)
        {
            standard = SampleStabilityOne();
        }
        else if (stability <= SmallStability)
        {
            standard = tau == 0.0 ? SampleSymmetricWeron(NextPhi(), NextOmega()) : SampleWeron(NextPhi(), NextOmega());
        }
        else
        {
            standard = tau == 0.0 ? SampleSymmetricCms() : SampleCms();
        }

        return scale * standard + location;
    }

    /// <summary>
    /// Fills a destination with values from this sampler's stable distribution.
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

    double SampleCauchy()
    {
        double halfAngle = NextPhiBy2();
        double tangent = Tan(halfAngle);
        return 2.0 * tangent / (1.0 - tangent * tangent);
    }

    double SampleLevy()
    {
        double normal = normalSampler.Sample();
        double squared = normal * normal;
        if (squared == 0.0)
        {
            return skewness > 0.0 ? double.PositiveInfinity : double.NegativeInfinity;
        }

        return skewness * (1.0 / squared - 1.0);
    }

    double SampleStabilityOne()
    {
        double halfAngle = NextPhiBy2();
        double omega = NextOmega();
        double a = Tan(halfAngle);
        double aSquared = a * a;
        double oneMinus = 1.0 - aSquared;
        double onePlus = 1.0 + aSquared;
        double coefficient = onePlus * (1.0 + 2.0 * halfAngle * tau) / (omega * oneMinus);
        double logarithm = Log(coefficient);
        double factor = 2.0 * (a + 2.0 * halfAngle * tau * a) / oneMinus;
        return factor + tau * logarithm;
    }

    double SampleCms()
    {
        double halfAngle = NextPhiBy2();
        double omega = NextOmega();
        double a = Tan(halfAngle);
        double tangentRatio = TanRatio(epsilon * halfAngle);
        double b = epsilon * halfAngle * tangentRatio;
        double aSquared = a * a;
        double bSquared = b * b;
        double oneMinusA = 1.0 - aSquared;
        double onePlusA = 1.0 + aSquared;
        double oneMinusB = 1.0 - bSquared;
        double onePlusB = 1.0 + bSquared;
        double coefficient = onePlusA * (oneMinusB + 2.0 * halfAngle * tangentRatio * tau) /
            (omega * oneMinusA * onePlusB);
        double logarithm = Log(coefficient);
        double exponentialTerm = ExponentialRatio(epsilonOverStability * logarithm) *
            (logarithm * inverseStability);
        double factor = 2.0 * ((a - b) * (1.0 + a * b) -
            halfAngle * tau * tangentRatio * (b * oneMinusA - 2.0 * a)) /
            (oneMinusA * onePlusB);
        double value = (1.0 + epsilon * exponentialTerm) * factor + tau * exponentialTerm;
        if (lowerSupport < value && value < upperSupport)
        {
            return value;
        }

        return SampleWeron(halfAngle * 2.0, omega);
    }

    double SampleSymmetricCms()
    {
        double halfAngle = NextPhiBy2();
        double omega = NextOmega();
        double a = Tan(halfAngle);
        double b = epsilon * halfAngle * TanRatio(epsilon * halfAngle);
        double aSquared = a * a;
        double bSquared = b * b;
        double oneMinusA = 1.0 - aSquared;
        double onePlusA = 1.0 + aSquared;
        double oneMinusB = 1.0 - bSquared;
        double onePlusB = 1.0 + bSquared;
        double coefficient = onePlusA * oneMinusB / (omega * oneMinusA * onePlusB);
        double logarithm = Log(coefficient);
        double exponentialTerm = ExponentialRatio(epsilonOverStability * logarithm) *
            (logarithm * inverseStability);
        double factor = 2.0 * (a - b) * (1.0 + a * b) / (oneMinusA * onePlusB);
        double value = (1.0 + epsilon * exponentialTerm) * factor;
        if (double.IsFinite(value))
        {
            return value;
        }

        return SampleSymmetricWeron(halfAngle * 2.0, omega);
    }

    double SampleWeron(double angle, double omega)
    {
        double firstTerm = Sin(adjustedStability * angle + zetaAngle);
        if (firstTerm == 0.0)
        {
            return zeta;
        }

        firstTerm /= Power(Cos(angle), inverseStability);
        double secondTerm = Power(Cos(epsilon * angle - zetaAngle) / omega, epsilonOverStability);
        if (secondTerm == 0.0)
        {
            return zeta;
        }

        double value = firstTerm * secondTerm * weronScale + zeta;
        if (value <= lowerSupport)
        {
            return lowerSupport;
        }

        return value < upperSupport ? value : upperSupport;
    }

    double SampleSymmetricWeron(double angle, double omega)
    {
        double firstTerm = Sin(adjustedStability * angle) /
            Power(Cos(angle), inverseStability);
        if (firstTerm == 0.0)
        {
            return 0.0;
        }

        double secondTerm = Power(Cos(epsilon * angle) / omega, epsilonOverStability);
        if (secondTerm == 0.0)
        {
            return 0.0;
        }

        return firstTerm * secondTerm;
    }

    double NextPhi()
    {
        return 2.0 * NextPhiBy2();
    }

    double NextPhiBy2()
    {
        return PiOver2 * (FloatingUniform.NextDoubleOpenOpen(generator) - 0.5);
    }

    double NextOmega()
    {
        return -Log(FloatingUniform.NextDoubleOpenOpen(generator));
    }

    double TanRatio(double value)
    {
        if (value == 0.0)
        {
            return 1.0;
        }

        return Tan(value) / value;
    }

    double ExponentialRatio(double value)
    {
        if (Math.Abs(value) < 0.5)
        {
            if (value == 0.0)
            {
                return 1.0;
            }

            return Expm1(value) / value;
        }

        if (value < double.PositiveInfinity)
        {
            return (Exp(value) - 1.0) / value;
        }

        return value;
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

        double half = value * 0.5;
        return 2.0 * Math.Exp(half) * Math.Sinh(half);
    }

    double Sin(double value)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Sin(value);
        }

        return Math.Sin(value);
    }

    double Cos(double value)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Cos(value);
        }

        return Math.Cos(value);
    }

    double Tan(double value)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Tan(value);
        }

        return Math.Tan(value);
    }

    double Atan(double value)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Atan(value);
        }

        return Math.Atan(value);
    }

    double Power(double value, double exponent)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Power(value, exponent);
        }

        return Math.Pow(value, exponent);
    }

    void Initialize(IRandomSource generator, double location, double scale, double stability, double skewness)
    {
        this.location = location;
        this.scale = scale;
        this.stability = stability;
        this.skewness = skewness;
        mathMode = generator.MathMode();
        epsilon = 1.0 - stability;
        adjustedStability = 1.0 - epsilon;
        inverseStability = 1.0 / adjustedStability;
        epsilonOverStability = inverseStability - 1.0;
        tau = ComputeTau(skewness);
        zeta = 0.0;
        zetaAngle = 0.0;
        weronScale = 1.0;
        lowerSupport = double.NegativeInfinity;
        upperSupport = double.PositiveInfinity;

        if (stability != 1.0 && stability != 2.0 && tau != 0.0)
        {
            if (adjustedStability > 1.0)
            {
                zeta = skewness * Tan((2.0 - adjustedStability) * PiOver2);
            }
            else
            {
                zeta = -skewness * Tan(adjustedStability * PiOver2);
            }

            zetaAngle = Atan(-zeta);
            weronScale = Power(1.0 + zeta * zeta, 0.5 / adjustedStability);
            if (stability < 1.0 && Math.Abs(skewness) == 1.0)
            {
                if (skewness > 0.0)
                {
                    lowerSupport = zeta;
                }
                else
                {
                    upperSupport = zeta;
                }
            }
        }
    }

    double ComputeTau(double beta)
    {
        if (Math.Abs(epsilon) < 0.5)
        {
            return beta / (TanRatio(epsilon * PiOver2) * PiOver2);
        }

        if (adjustedStability > 1.0)
        {
            double reflected = 2.0 - adjustedStability;
            return beta * PiOver2 * epsilon * reflected * -TanRatio(reflected * PiOver2);
        }

        return beta * PiOver2 * epsilon * adjustedStability * TanRatio(adjustedStability * PiOver2);
    }

    static void ValidateParameters(IRandomSource generator, double location, double scale, double stability, double skewness)
    {
        Guard.NotNull(generator, "generator");
        Guard.Finite(location, "location");
        Guard.Finite(scale, "scale");
        if (scale < 0.0)
        {
            throw RandomArgumentException.Create("scale", "The scale must not be negative.");
        }

        Guard.Finite(stability, "stability");
        double epsilon = 1.0 - stability;
        if (!(-1.0 <= epsilon && epsilon < 1.0))
        {
            throw RandomArgumentException.Create("stability", "The stability must be representably within (0, 2].");
        }

        Guard.Finite(skewness, "skewness");
        if (skewness < -1.0 || skewness > 1.0)
        {
            throw RandomArgumentException.Create("skewness", "The skewness must be within [-1, 1].");
        }
    }
}
