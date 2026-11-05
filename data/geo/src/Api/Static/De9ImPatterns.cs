namespace geo;

/// <summary>Provides DE-9IM patterns used by named topological predicates.</summary>
public static class De9ImPatterns
{
    /// <summary>The topological equality pattern.</summary>
    public const string TopologicallyEquals = "T*F**FFF*";

    /// <summary>The disjoint pattern.</summary>
    public const string Disjoint = "FF*FF****";

    /// <summary>The within pattern.</summary>
    public const string Within = "T*F**F***";

    /// <summary>The contains pattern.</summary>
    public const string Contains = "T*****FF*";

    /// <summary>The contains-properly pattern.</summary>
    public const string ContainsProperly = "T**FF*FF*";

    /// <summary>The touches pattern where the first interior meets the second boundary.</summary>
    public const string TouchesInteriorBoundary = "FT*******";

    /// <summary>The touches pattern where the first boundary meets the second interior.</summary>
    public const string TouchesBoundaryInterior = "F**T*****";

    /// <summary>The touches pattern where both boundaries meet.</summary>
    public const string TouchesBoundaryBoundary = "F***T****";

    /// <summary>The crosses pattern for a lower-dimensional first geometry.</summary>
    public const string CrossesLowerDimension = "T*T******";

    /// <summary>The crosses pattern for a higher-dimensional first geometry.</summary>
    public const string CrossesHigherDimension = "T*****T**";

    /// <summary>The crosses pattern for two lineal geometries.</summary>
    public const string CrossesLineLine = "0********";

    /// <summary>The overlaps pattern for point or areal geometries.</summary>
    public const string OverlapsPointOrArea = "T*T***T**";

    /// <summary>The overlaps pattern for lineal geometries.</summary>
    public const string OverlapsLine = "1*T***T**";

    /// <summary>An intersects pattern matching interior-to-interior contact.</summary>
    public const string IntersectsInteriorInterior = "T********";

    /// <summary>An intersects pattern matching interior-to-boundary contact.</summary>
    public const string IntersectsInteriorBoundary = "*T*******";

    /// <summary>An intersects pattern matching boundary-to-interior contact.</summary>
    public const string IntersectsBoundaryInterior = "***T*****";

    /// <summary>An intersects pattern matching boundary-to-boundary contact.</summary>
    public const string IntersectsBoundaryBoundary = "****T****";

    /// <summary>A covers pattern matching interior-to-interior contact.</summary>
    public const string CoversInteriorInterior = "T*****FF*";

    /// <summary>A covers pattern matching interior-to-boundary contact.</summary>
    public const string CoversInteriorBoundary = "*T****FF*";

    /// <summary>A covers pattern matching boundary-to-interior contact.</summary>
    public const string CoversBoundaryInterior = "***T**FF*";

    /// <summary>A covers pattern matching boundary-to-boundary contact.</summary>
    public const string CoversBoundaryBoundary = "****T*FF*";

    /// <summary>A covered-by pattern matching interior-to-interior contact.</summary>
    public const string CoveredByInteriorInterior = "T*F**F***";

    /// <summary>A covered-by pattern matching interior-to-boundary contact.</summary>
    public const string CoveredByInteriorBoundary = "*TF**F***";

    /// <summary>A covered-by pattern matching boundary-to-interior contact.</summary>
    public const string CoveredByBoundaryInterior = "**FT*F***";

    /// <summary>A covered-by pattern matching boundary-to-boundary contact.</summary>
    public const string CoveredByBoundaryBoundary = "**F*TF***";
}
