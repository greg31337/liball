using System;

namespace random;

/// <summary>
/// Samples from a weighted mixture of caller-supplied scalar components.
/// </summary>
/// <remarks>
/// Components use built-in <c>Func&lt;double&gt;</c> delegates. Relative weights are
/// preprocessed with Vose's alias method. Each sample selects its component first and
/// then invokes exactly that delegate once; zero-weight components are never invoked.
/// The component and weight arrays are defensively copied or preprocessed, though the
/// delegate objects and their captured state remain caller-owned. The sampler retains
/// the supplied mutable selection generator and is not safe for concurrent use without
/// external synchronization.
/// </remarks>
public class MixtureSampler : IMixtureSampler
{
    IRandomSource generator;
    Func<double>[] components;
    AliasTable aliasTable;

    /// <summary>
    /// Creates a weighted mixture sampler.
    /// </summary>
    /// <param name="generator">The generator used only to select component indexes.</param>
    /// <param name="components">The nonempty array of non-null scalar sampling delegates.</param>
    /// <param name="weights">Finite nonnegative relative weights corresponding one-to-one with the components.</param>
    /// <remarks>
    /// Weights need not sum to one. Exactly one positive weight selects its component
    /// without consuming state from the selection generator. Component return values
    /// and exceptions are propagated unchanged.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator, components, weights, or a component is null; an array is empty; their lengths differ; a weight is non-finite or negative; or every weight is zero.</exception>
    public MixtureSampler(IRandomSource generator, Func<double>[] components, double[] weights)
    {
        Guard.NotNull(generator, "generator");
        ValidateComponents(components);
        Guard.NotNull(weights, "weights");
        Guard.ExactSpanLength(weights.Length, components.Length, "weights");
        ValidateWeights(weights);
        AliasTable validatedAliasTable = new AliasTable(weights);
        this.generator = generator;
        this.components = (Func<double>[])components.Clone();
        aliasTable = validatedAliasTable;
    }

    /// <summary>
    /// Returns the number of mixture components.
    /// </summary>
    /// <returns>The positive component count.</returns>
    public int ComponentCount()
    {
        return components.Length;
    }

    /// <summary>
    /// Selects and invokes one mixture component.
    /// </summary>
    /// <returns>The selected component's unmodified return value.</returns>
    public double Sample()
    {
        int index = aliasTable.Sample(generator);
        return components[index]();
    }

    /// <summary>
    /// Fills a destination with values from this mixture.
    /// </summary>
    /// <param name="destination">The destination values.</param>
    /// <remarks>The result and selection-generator state match repeated calls to <see cref="Sample()"/>.</remarks>
    public void Fill(Span<double> destination)
    {
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = Sample();
            index++;
        }
    }

    static void ValidateComponents(Func<double>[] components)
    {
        Guard.NotNull(components, "components");
        if (components.Length == 0)
        {
            throw RandomArgumentException.Create("components", "At least one component is required.");
        }

        int index = 0;
        while (index < components.Length)
        {
            if (components[index] == null)
            {
                throw RandomArgumentException.Create("components", "Every component must be non-null.");
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
