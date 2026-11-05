using System;

namespace random;

/// <summary>
/// Generates value-precision pairs from a normal-gamma distribution.
/// </summary>
/// <remarks>
/// The precision <c>tau</c> is sampled from a gamma distribution with the requested
/// shape and rate. Conditional on that precision, the value is normal with the
/// requested location and variance <c>1 / (meanPrecision * tau)</c>. Each result uses
/// the fixed layout <c>[value, precision]</c>. Gamma sampling occurs before the
/// conditional standard-normal draw. The sampler retains the supplied mutable
/// generator and is not safe for concurrent use without external synchronization.
/// </remarks>
public class NormalGammaSampler : INormalGammaSampler
{
    GammaSampler gammaSampler;
    NormalSampler normalSampler;
    double location;
    double rate;
    double rootMeanPrecision;
    RandomMathMode mathMode;

    /// <summary>
    /// Creates a normal-gamma sampler.
    /// </summary>
    /// <param name="generator">The generator that supplies random values and selects the math mode.</param>
    /// <param name="location">The finite conditional-normal location.</param>
    /// <param name="meanPrecision">The finite positive multiplier applied to the sampled precision.</param>
    /// <param name="shape">The finite positive gamma shape.</param>
    /// <param name="rate">The finite positive gamma rate.</param>
    /// <remarks>
    /// The gamma component uses shape-rate parameterization, so its mean is
    /// <c>shape / rate</c>. Representational underflow can produce zero precision and
    /// an infinite value deviation; overflow can produce infinite precision and an
    /// exactly zero conditional deviation.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator is null; the location is non-finite; or mean precision, shape, or rate is non-finite or not positive.</exception>
    public NormalGammaSampler(IRandomSource generator, double location, double meanPrecision, double shape, double rate)
    {
        ValidateParameters(generator, location, meanPrecision, shape, rate);
        gammaSampler = new GammaSampler(generator, shape, 1.0);
        normalSampler = new NormalSampler(generator, 0.0, 1.0);
        this.location = location;
        this.rate = rate;
        mathMode = generator.MathMode();
        rootMeanPrecision = Sqrt(meanPrecision);
    }

    /// <summary>
    /// Returns the number of entries in each sampled value-precision pair.
    /// </summary>
    /// <returns>Two.</returns>
    public int ComponentCount()
    {
        return 2;
    }

    /// <summary>
    /// Returns the exact storage length required for a row-major batch.
    /// </summary>
    /// <param name="sampleCount">The nonnegative number of value-precision rows.</param>
    /// <returns>Twice the sample count.</returns>
    /// <exception cref="RandomArgumentException">The sample count is negative or the required length exceeds <c>int.MaxValue</c>.</exception>
    public int RequiredBatchOutputLength(int sampleCount)
    {
        return MultivariateDistributions.RequiredVectorOutputLength(sampleCount, 2);
    }

    /// <summary>
    /// Creates one value-precision pair from this normal-gamma distribution.
    /// </summary>
    /// <returns>A new two-entry array in <c>[value, precision]</c> order.</returns>
    public double[] Sample()
    {
        double[] destination = new double[2];
        SampleInto(destination.AsSpan());
        return destination;
    }

    /// <summary>
    /// Fills a row-major batch with value-precision pairs from this normal-gamma distribution.
    /// </summary>
    /// <param name="sampleCount">The nonnegative number of value-precision rows.</param>
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
            SampleInto(destination.Slice(offset, 2));
            offset += 2;
            row++;
        }
    }

    /// <summary>
    /// Writes one value-precision pair from this normal-gamma distribution.
    /// </summary>
    /// <param name="destination">The two-entry destination, written in <c>[value, precision]</c> order.</param>
    /// <exception cref="RandomArgumentException">The destination length is not two.</exception>
    public void SampleInto(Span<double> destination)
    {
        Guard.ExactSpanLength(destination.Length, 2, "destination");
        double precision = gammaSampler.Sample() / rate;
        double standard = normalSampler.Sample();
        double denominator = rootMeanPrecision * Sqrt(precision);
        double deviation;
        if (denominator == 0.0)
        {
            if (standard == 0.0)
            {
                deviation = 0.0;
            }
            else
            {
                deviation = standard < 0.0 ? double.NegativeInfinity : double.PositiveInfinity;
            }
        }
        else
        {
            deviation = standard / denominator;
        }

        destination[0] = location + deviation;
        destination[1] = precision;
    }

    double Sqrt(double value)
    {
        if (mathMode == RandomMathMode.Portable)
        {
            return PortableMath.Sqrt(value);
        }

        return Math.Sqrt(value);
    }

    static void ValidateParameters(IRandomSource generator, double location, double meanPrecision, double shape, double rate)
    {
        Guard.NotNull(generator, "generator");
        Guard.Finite(location, "location");
        Guard.Finite(meanPrecision, "meanPrecision");
        if (meanPrecision <= 0.0)
        {
            throw RandomArgumentException.Create("meanPrecision", "The mean precision must be positive.");
        }

        Guard.Finite(shape, "shape");
        if (shape <= 0.0)
        {
            throw RandomArgumentException.Create("shape", "The shape must be positive.");
        }

        Guard.Finite(rate, "rate");
        if (rate <= 0.0)
        {
            throw RandomArgumentException.Create("rate", "The rate must be positive.");
        }
    }
}
