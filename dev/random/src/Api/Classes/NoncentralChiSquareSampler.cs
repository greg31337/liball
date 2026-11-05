using System;

namespace random;

/// <summary>
/// Generates double-precision values from a noncentral chi-square distribution.
/// </summary>
/// <remarks>
/// Zero noncentrality delegates to the central chi-square sampler. For degrees of
/// freedom greater than one, sampling uses an independent central chi-square with one
/// fewer degree plus the square of a normal shifted by the square root of the
/// noncentrality. At or below one degree, sampling uses the Poisson mixture with mean
/// <c>noncentrality / 2</c> and conditional degrees <c>degreesOfFreedom + 2 * count</c>.
/// <para>
/// The Poisson mixture uses product inversion for small means and transformed rejection
/// for large means. Its mean is bounded below the signed 64-bit count limit with a
/// ten-standard-deviation margin. This limit applies only when degrees of freedom are at
/// most one; the shifted-normal construction does not require a Poisson count.
/// </para>
/// The sampler retains the supplied mutable generator and is not safe for concurrent
/// use without external synchronization.
/// </remarks>
public class NoncentralChiSquareSampler : INoncentralChiSquareSampler
{
    IRandomSource generator;
    ChiSquareSampler centralSampler;
    ChiSquareSampler? remainderSampler;
    NormalSampler normalSampler;
    PoissonCountSampler? poissonSampler;
    double degreesOfFreedom;
    double noncentrality;
    double rootNoncentrality;

    /// <summary>
    /// Creates a noncentral chi-square sampler with the requested parameters.
    /// </summary>
    /// <param name="generator">The generator that supplies random bits and selects the math mode.</param>
    /// <param name="degreesOfFreedom">The finite positive degrees of freedom.</param>
    /// <param name="noncentrality">The finite nonnegative noncentrality.</param>
    /// <remarks>
    /// For degrees of freedom at most one, half the noncentrality must not exceed
    /// <c>9223372006484770816</c>, the supported Poisson-mixture mean ceiling.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator is null, degrees of freedom is non-finite or not positive, noncentrality is non-finite or negative, or the required Poisson mean exceeds its safe count range.</exception>
    public NoncentralChiSquareSampler(IRandomSource generator, double degreesOfFreedom, double noncentrality)
    {
        ValidateParameters(generator, degreesOfFreedom, noncentrality);
        this.generator = generator;
        centralSampler = new ChiSquareSampler(generator, degreesOfFreedom);
        normalSampler = new NormalSampler(generator, 0.0, 1.0);
        remainderSampler = null;
        poissonSampler = null;
        this.degreesOfFreedom = degreesOfFreedom;
        this.noncentrality = noncentrality;
        rootNoncentrality = 0.0;

        if (noncentrality == 0.0)
        {
            return;
        }

        if (degreesOfFreedom >= 1.0)
        {
            RandomMathMode mathMode = generator.MathMode();
            rootNoncentrality = mathMode == RandomMathMode.Portable
                ? PortableMath.Sqrt(noncentrality)
                : Math.Sqrt(noncentrality);
            if (degreesOfFreedom > 1.0)
            {
                remainderSampler = new ChiSquareSampler(generator, degreesOfFreedom - 1.0);
            }
        }
        else
        {
            poissonSampler = new PoissonCountSampler(generator, noncentrality * 0.5);
        }
    }

    /// <summary>
    /// Returns one value from this sampler's noncentral chi-square distribution.
    /// </summary>
    /// <returns>A nonnegative value, including possible positive infinity after representational overflow.</returns>
    public double Sample()
    {
        if (noncentrality == 0.0)
        {
            return centralSampler.Sample();
        }

        if (degreesOfFreedom >= 1.0)
        {
            double remainder = degreesOfFreedom == 1.0 ? 0.0 : GetRemainderSampler().Sample();
            double shiftedNormal = normalSampler.Sample() + rootNoncentrality;
            double square = shiftedNormal * shiftedNormal;
            return remainder + square;
        }

        long count = GetPoissonSampler().Sample();
        double shape = degreesOfFreedom * 0.5 + (double)count;
        if (shape == 0.0)
        {
            shape = double.Epsilon;
        }

        GammaSampler conditional = new GammaSampler(generator, shape, 2.0);
        return conditional.Sample();
    }

    /// <summary>
    /// Fills a destination with values from this sampler's noncentral chi-square distribution.
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

    ChiSquareSampler GetRemainderSampler()
    {
        if (remainderSampler == null)
        {
            throw RandomStateException.Create("The noncentral chi-square remainder sampler is unavailable.");
        }

        return remainderSampler;
    }

    PoissonCountSampler GetPoissonSampler()
    {
        if (poissonSampler == null)
        {
            throw RandomStateException.Create("The noncentral chi-square Poisson sampler is unavailable.");
        }

        return poissonSampler;
    }

    internal static void ValidateParameters(IRandomSource generator, double degreesOfFreedom, double noncentrality)
    {
        ChiSquareSampler.ValidateParameters(generator, degreesOfFreedom);
        Guard.Finite(noncentrality, "noncentrality");
        if (noncentrality < 0.0)
        {
            throw RandomArgumentException.Create("noncentrality", "The noncentrality must not be negative.");
        }

        if (degreesOfFreedom < 1.0 && noncentrality * 0.5 > PoissonCountSampler.MaximumMean)
        {
            throw RandomArgumentException.Create("noncentrality", "The noncentrality requires a Poisson count outside the safe signed 64-bit range.");
        }
    }
}
