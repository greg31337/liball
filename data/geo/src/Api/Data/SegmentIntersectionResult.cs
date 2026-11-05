namespace geo;

/// <summary>Describes the classification and location of a segment intersection.</summary>
public class SegmentIntersectionResult
{
    /// <summary>The segment intersection classification.</summary>
    public SegmentIntersectionType Type;

    /// <summary>The topological location of the primary witness point.</summary>
    public TopologicalLocation Location;

    /// <summary>The first intersection coordinate, or null when the segments are disjoint.</summary>
    public Coordinate? FirstIntersection;

    /// <summary>The second overlap endpoint, or null when the intersection is not an overlap.</summary>
    public Coordinate? SecondIntersection;

    /// <summary>Whether the segment interiors cross properly.</summary>
    public bool IsProper;
}
