using System;

namespace fake;

/// <summary>Associates a nonnegative selection weight with a schema node.</summary>
public class SchemaVariant
{
    /// <summary>The node evaluated when this variant is selected.</summary>
    public SchemaNode Node;

    /// <summary>The relative nonnegative selection weight.</summary>
    public double Weight;

    /// <summary>Initializes one weighted schema variant.</summary>
    /// <param name="node">The node evaluated for this choice.</param>
    /// <param name="weight">A finite nonnegative relative weight.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>node</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>weight</c> is negative or non-finite.</exception>
    public SchemaVariant(SchemaNode node, double weight)
    {
        Guard.NotNull(node, "node");
        if (Double.IsNaN(weight) || Double.IsInfinity(weight) || weight < 0.0)
            throw new ArgumentOutOfRangeException(
                "weight",
                weight,
                "weight must be finite and nonnegative.");
        Node = node;
        Weight = weight;
    }
}
