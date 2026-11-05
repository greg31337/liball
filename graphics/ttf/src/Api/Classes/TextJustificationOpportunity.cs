namespace ttf;

/// <summary>Describes bounded expansion available at an existing source-cluster boundary.</summary>
/// <remarks>
/// The opportunity does not insert text. Its boundary remains between existing
/// source clusters so extraction and logical source ranges remain unchanged.
/// </remarks>
public class TextJustificationOpportunity : ITextJustificationOpportunity
{
    TextJustificationOpportunityKind kind;
    int clusterBoundary;
    double maximumExpansion;
    int priority;

    /// <summary>Creates a bounded advanced justification opportunity.</summary>
    /// <param name="kind">The advanced mechanism.</param>
    /// <param name="clusterBoundary">The source-cluster boundary to adjust.</param>
    /// <param name="maximumExpansion">The nonnegative finite available expansion.</param>
    /// <param name="priority">The nonnegative priority, where lower values run first.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">An argument is invalid.</exception>
    public TextJustificationOpportunity(
        TextJustificationOpportunityKind kind,
        int clusterBoundary,
        double maximumExpansion,
        int priority)
    {
        if (kind != TextJustificationOpportunityKind.Jstf &&
            kind != TextJustificationOpportunityKind.Kashida)
        {
            throw new System.ArgumentOutOfRangeException("kind");
        }

        if (clusterBoundary < 0)
        {
            throw new System.ArgumentOutOfRangeException("clusterBoundary");
        }

        if (!double.IsFinite(maximumExpansion) || maximumExpansion < 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumExpansion");
        }

        if (priority < 0)
        {
            throw new System.ArgumentOutOfRangeException("priority");
        }

        this.kind = kind;
        this.clusterBoundary = clusterBoundary;
        this.maximumExpansion = maximumExpansion;
        this.priority = priority;
    }

    /// <summary>Returns the advanced mechanism.</summary>
    /// <returns>The opportunity kind.</returns>
    public TextJustificationOpportunityKind GetKind()
    {
        return kind;
    }

    /// <summary>Returns the existing source-cluster boundary to adjust.</summary>
    /// <returns>The cluster boundary.</returns>
    public int GetClusterBoundary()
    {
        return clusterBoundary;
    }

    /// <summary>Returns the maximum available expansion.</summary>
    /// <returns>The expansion in layout units.</returns>
    public double GetMaximumExpansion()
    {
        return maximumExpansion;
    }

    /// <summary>Returns the priority, where lower values run first.</summary>
    /// <returns>The nonnegative priority.</returns>
    public int GetPriority()
    {
        return priority;
    }
}
