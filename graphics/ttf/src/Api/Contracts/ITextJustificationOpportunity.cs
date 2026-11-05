namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="TextJustificationOpportunity"/>.</summary>
public interface ITextJustificationOpportunity
{
    /// <summary>Returns the advanced mechanism.</summary>
    /// <returns>The opportunity kind.</returns>
    TextJustificationOpportunityKind GetKind();

    /// <summary>Returns the existing source-cluster boundary to adjust.</summary>
    /// <returns>The cluster boundary.</returns>
    int GetClusterBoundary();

    /// <summary>Returns the maximum available expansion.</summary>
    /// <returns>The expansion in layout units.</returns>
    double GetMaximumExpansion();

    /// <summary>Returns the priority, where lower values run first.</summary>
    /// <returns>The nonnegative priority.</returns>
    int GetPriority();
}
