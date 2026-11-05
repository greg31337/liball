namespace svg;

/// <summary>Defines stable accessibility-tree construction failures.</summary>
public enum SvgAccessibilityTreeError
{
    /// <summary>No tree construction failure occurred.</summary>
    None = 0,
    /// <summary>The document or tree options were invalid.</summary>
    InvalidArgument = 1,
    /// <summary>The configured node or text bound was exceeded.</summary>
    LimitExceeded = 2,
    /// <summary>Accessible semantics could not be calculated.</summary>
    SemanticFailure = 3,
    /// <summary>World-space accessibility geometry could not be calculated.</summary>
    GeometryFailure = 4
}
