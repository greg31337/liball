using System;

namespace random;

/// <summary>
/// Samples values from a finite empirical distribution.
/// </summary>
/// <remarks>
/// The equal-weight form selects an observation with an unbiased bounded integer draw.
/// The weighted form preprocesses finite nonnegative relative weights with Vose's alias
/// method. Observations and weighted preprocessing are copied during construction, so
/// later caller mutations do not affect sampling. The sampler retains the supplied
/// mutable generator and is not safe for concurrent use without external synchronization.
/// </remarks>
public class EmpiricalSampler : IEmpiricalSampler
{
    IRandomSource generator;
    double[] observations;
    AliasTable? aliasTable;
    bool weighted;

    /// <summary>
    /// Creates a weighted empirical sampler.
    /// </summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="observations">The nonempty finite observation array.</param>
    /// <param name="weights">Finite nonnegative relative weights corresponding one-to-one with the observations.</param>
    /// <remarks>
    /// Weights need not sum to one. Zero-weight observations are never selected, and
    /// exactly one positive weight defines a deterministic sample without entropy use.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator, observations, or weights are null; an array is empty; their lengths differ; an observation is non-finite; a weight is non-finite or negative; or every weight is zero.</exception>
    public EmpiricalSampler(IRandomSource generator, double[] observations, double[] weights)
    {
        Guard.NotNull(generator, "generator");
        ValidateObservations(observations);
        Guard.NotNull(weights, "weights");
        Guard.ExactSpanLength(weights.Length, observations.Length, "weights");
        ValidateWeights(weights);
        AliasTable validatedAliasTable = new AliasTable(weights);
        this.generator = generator;
        this.observations = (double[])observations.Clone();
        aliasTable = validatedAliasTable;
        weighted = true;
    }

    /// <summary>
    /// Creates an empirical sampler that selects every observation with equal probability.
    /// </summary>
    /// <param name="generator">The generator that supplies random values.</param>
    /// <param name="observations">The nonempty finite observation array.</param>
    /// <returns>An unweighted empirical sampler.</returns>
    /// <exception cref="RandomArgumentException">The generator or observations are null, the observation array is empty, or an observation is non-finite.</exception>
    internal static EmpiricalSampler CreateUnweighted(IRandomSource generator, double[] observations)
    {
        Guard.NotNull(generator, "generator");
        ValidateObservations(observations);
        double[] equalWeights = new double[observations.Length];
        int index = 0;
        while (index < equalWeights.Length)
        {
            equalWeights[index] = 1.0;
            index++;
        }

        EmpiricalSampler sampler = new EmpiricalSampler(generator, observations, equalWeights);
        sampler.aliasTable = null;
        sampler.weighted = false;
        return sampler;
    }

    /// <summary>
    /// Returns the number of observations represented by this sampler.
    /// </summary>
    /// <returns>The positive observation count.</returns>
    public int ObservationCount()
    {
        return observations.Length;
    }

    /// <summary>
    /// Returns one value from this empirical distribution.
    /// </summary>
    /// <returns>One of the observations supplied during construction.</returns>
    public double Sample()
    {
        int index;
        if (weighted)
        {
            if (aliasTable == null)
            {
                throw RandomStateException.Create("The empirical sampler has no alias table.");
            }

            index = aliasTable.Sample(generator);
        }
        else
        {
            index = IntegerUniform.NextInt32(generator, observations.Length);
        }

        return observations[index];
    }

    /// <summary>
    /// Fills a destination with values from this empirical distribution.
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

    static void ValidateObservations(double[] observations)
    {
        Guard.NotNull(observations, "observations");
        if (observations.Length == 0)
        {
            throw RandomArgumentException.Create("observations", "At least one observation is required.");
        }

        int index = 0;
        while (index < observations.Length)
        {
            if (!double.IsFinite(observations[index]))
            {
                throw RandomArgumentException.Create("observations", "Every observation must be finite.");
            }

            index++;
        }
    }

    static void ValidateWeights(double[] weights)
    {
        int positiveCount = 0;
        int index = 0;
        while (index < weights.Length)
        {
            double weight = weights[index];
            if (!double.IsFinite(weight) || weight < 0.0)
            {
                throw RandomArgumentException.Create("weights", "Every weight must be finite and nonnegative.");
            }

            if (weight > 0.0)
            {
                positiveCount++;
            }

            index++;
        }

        if (positiveCount == 0)
        {
            throw RandomArgumentException.Create("weights", "At least one weight must be positive.");
        }
    }
}
