namespace odf;

/// <summary>
/// Identifies the geometry used for a frame clipping contour.
/// </summary>
public enum OdfContourKind
{
    /// <summary>No contour is present.</summary>
    None = 0,
    /// <summary>A polygon contour.</summary>
    Polygon = 1,
    /// <summary>An SVG path contour.</summary>
    Path = 2
}
