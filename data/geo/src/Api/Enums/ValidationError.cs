namespace geo;

/// <summary>Identifies a structural, topological, or coverage validation failure.</summary>
public enum ValidationError
{
    /// <summary>No validation failure.</summary>
    None = 0,

    /// <summary>A required geometry or payload is null.</summary>
    NullGeometry = 1,

    /// <summary>The geometry kind is not recognized.</summary>
    InvalidGeometryType = 2,

    /// <summary>The tagged payload does not match the geometry kind.</summary>
    PayloadMismatch = 3,

    /// <summary>The coordinate layout is invalid or inconsistent.</summary>
    InvalidCoordinateLayout = 4,

    /// <summary>A coordinate contains a non-finite X or Y ordinate.</summary>
    NonFiniteCoordinate = 5,

    /// <summary>A coordinate sequence has too few positions.</summary>
    TooFewCoordinates = 6,

    /// <summary>A ring is not closed.</summary>
    RingNotClosed = 7,

    /// <summary>A ring has too few distinct X/Y positions.</summary>
    TooFewDistinctCoordinates = 8,

    /// <summary>A coordinate is repeated where repetition is invalid.</summary>
    RepeatedCoordinate = 9,

    /// <summary>A component intersects itself illegally.</summary>
    SelfIntersection = 10,

    /// <summary>A ring touches itself illegally.</summary>
    RingSelfTouch = 11,

    /// <summary>A polygon hole lies outside its shell.</summary>
    HoleOutsideShell = 12,

    /// <summary>Polygon holes are nested.</summary>
    NestedHoles = 13,

    /// <summary>A polygon interior is disconnected.</summary>
    DisconnectedInterior = 14,

    /// <summary>Two rings are duplicates.</summary>
    DuplicateRings = 15,

    /// <summary>Polygonal components overlap.</summary>
    OverlappingComponents = 16,

    /// <summary>Segments intersect without a required node.</summary>
    NonNodedIntersection = 17,

    /// <summary>A geometry collection contains a reference cycle.</summary>
    CyclicGeometry = 18,

    /// <summary>The configured nesting limit was exceeded.</summary>
    NestingLimitExceeded = 19,

    /// <summary>The configured coordinate limit was exceeded.</summary>
    CoordinateLimitExceeded = 20,

    /// <summary>A circular arc has invalid coordinate cardinality.</summary>
    InvalidArcCardinality = 21,

    /// <summary>Adjacent curve components are not continuous.</summary>
    CurveDiscontinuity = 22,

    /// <summary>A surface has invalid patches or closure.</summary>
    InvalidSurface = 23,

    /// <summary>Coverage components overlap.</summary>
    CoverageOverlap = 24,

    /// <summary>A coverage has a gap within the reported tolerance.</summary>
    CoverageGap = 25,

    /// <summary>A buffer result contains a non-polygonal value.</summary>
    BufferResultNotPolygonal = 26,

    /// <summary>A buffer result violates the required source/result containment.</summary>
    BufferContainmentFailure = 27,

    /// <summary>A buffer boundary lies outside the permitted distance range.</summary>
    BufferDistanceFailure = 28,

    /// <summary>A buffer result violates the expected area direction.</summary>
    BufferAreaFailure = 29
}
