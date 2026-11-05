namespace geo;

/// <summary>Classifies the intersection of two closed line segments.</summary>
public enum SegmentIntersectionType
{
    /// <summary>The segments do not intersect.</summary>
    None = 0,

    /// <summary>The segment interiors cross at one point.</summary>
    ProperPoint = 1,

    /// <summary>The segments meet at one or more endpoints.</summary>
    Endpoint = 2,

    /// <summary>Collinear segments share exactly one point.</summary>
    CollinearPoint = 3,

    /// <summary>Collinear segments share a non-zero-length interval.</summary>
    CollinearOverlap = 4
}
