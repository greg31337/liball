namespace ttf;

/// <summary>Defines one immutable adjustment applied to a justified line.</summary>
public interface ITextJustificationApplication
{
    /// <summary>Returns the mechanism that supplied the expansion.</summary>
    /// <returns>The application kind.</returns>
    TextJustificationApplicationKind GetKind();

    /// <summary>Returns the adjusted source-cluster boundary.</summary>
    /// <returns>The source-cluster boundary.</returns>
    int GetClusterBoundary();

    /// <summary>Returns the expansion applied at the boundary.</summary>
    /// <returns>The positive expansion in layout units.</returns>
    double GetExpansion();
}
