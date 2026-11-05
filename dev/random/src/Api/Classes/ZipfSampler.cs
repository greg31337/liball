using System;

namespace random;

/// <summary>
/// Generates positive signed 64-bit ranks from a Zipf distribution.
/// </summary>
/// <remarks>
/// For exponent <c>a</c>, positive integer <c>k</c> has mass proportional to
/// <c>k^(-a)</c>. Sampling uses rejection from a continuous power-law envelope.
/// Its acceptance comparison is evaluated in the logarithmic domain so exponents
/// immediately above one and very large exponents do not overflow intermediate
/// powers. The sampler retains the supplied mutable generator and is not safe for
/// concurrent use without external synchronization.
/// </remarks>
public class ZipfSampler : IZipfSampler
{
    const double LogLongLimit = 43.668272375276554;
    const double Ln2 = 0.693147180559945309417232121458176568;
    IRandomSource generator;
    double exponentMinusOne;
    double logEnvelopeIncrement;
    double logExponentMinusOne;
    RandomMathMode mathMode;

    /// <summary>
    /// Creates a Zipf sampler with the requested exponent.
    /// </summary>
    /// <param name="generator">The generator that supplies random values and selects the math mode.</param>
    /// <param name="exponent">The finite exponent greater than one.</param>
    /// <exception cref="RandomArgumentException">The generator is null, or the exponent is non-finite or not greater than one.</exception>
    public ZipfSampler(IRandomSource generator, double exponent)
    {
        Guard.NotNull(generator, "generator");
        Guard.Finite(exponent, "exponent");
        if (exponent <= 1.0)
        {
            throw RandomArgumentException.Create("exponent", "The Zipf exponent must be greater than one.");
        }

        this.generator = generator;
        mathMode = generator.MathMode();
        exponentMinusOne = exponent - 1.0;
        logExponentMinusOne = Log(exponentMinusOne);
        logEnvelopeIncrement = LogExpm1(exponentMinusOne * Ln2);
    }

    /// <summary>
    /// Returns one rank from this sampler's Zipf distribution.
    /// </summary>
    /// <returns>A positive signed 64-bit rank.</returns>
    /// <exception cref="RandomExhaustedException">An accepted rank exceeds the signed 64-bit output capacity.</exception>
    public long Sample()
    {
        while (true)
        {
            double unit = FloatingUniform.NextDoubleOpenOpen(generator);
            double comparison = FloatingUniform.NextDoubleOpenOpen(generator);
            double proposalLog = -Log(unit) / exponentMinusOne;
            double candidateLog = proposalLog;
            double candidate = double.PositiveInfinity;
            if (proposalLog < LogLongLimit)
            {
                candidate = Math.Floor(Exp(proposalLog));
                candidateLog = Log(candidate);
            }

            double inverseCandidate = Exp(-candidateLog);
            double powerLog;
            if (inverseCandidate == 0.0)
            {
                powerLog = Exp(logExponentMinusOne - candidateLog);
            }
            else
            {
                powerLog = exponentMinusOne * Log1p(inverseCandidate);
            }

            double scaledIncrementLog;
            if (powerLog == 0.0)
            {
                scaledIncrementLog = logExponentMinusOne;
            }
            else
            {
                scaledIncrementLog = candidateLog + LogExpm1(powerLog);
            }

            double acceptanceLeft = Log(comparison) + scaledIncrementLog - logEnvelopeIncrement;
            double acceptanceRight = powerLog - exponentMinusOne * Ln2;
            if (acceptanceLeft <= acceptanceRight)
            {
                if (!double.IsFinite(candidate) || candidate < 1.0 || candidate >= 9223372036854775808.0)
                {
                    throw RandomExhaustedException.Create("The Zipf rank exceeds the signed 64-bit output capacity.");
                }

                return (long)candidate;
            }
        }
    }

    /// <summary>
    /// Fills a destination with ranks from this sampler's Zipf distribution.
    /// </summary>
    /// <param name="destination">The destination ranks.</param>
    /// <remarks>The result and generator state match repeated calls to <see cref="Sample()"/>.</remarks>
    /// <exception cref="RandomExhaustedException">An accepted rank exceeds the signed 64-bit output capacity.</exception>
    public void Fill(Span<long> destination)
    {
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = Sample();
            index++;
        }
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

        if (value >= -0.5 && value <= 0.5)
        {
            double term = value;
            double sum = value;
            int index = 2;
            while (index <= 24)
            {
                term = term * value / (double)index;
                sum += term;
                index++;
            }

            return sum;
        }

        return Math.Exp(value) - 1.0;
    }

    double Log(double value)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Log(value);
        }

        return Math.Log(value);
    }

    double Log1p(double value)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Log1p(value);
        }

        if (value < -0.5 || value > 0.5)
        {
            return Math.Log(1.0 + value);
        }

        double reduced = value / (2.0 + value);
        double square = reduced * reduced;
        double term = reduced;
        double sum = term;
        int denominator = 3;
        while (denominator <= 39)
        {
            term *= square;
            sum += term / denominator;
            denominator += 2;
        }

        return 2.0 * sum;
    }

    double LogExpm1(double value)
    {
        if (value > 50.0)
        {
            return value + Log1p(-Exp(-value));
        }

        return Log(Expm1(value));
    }
}
