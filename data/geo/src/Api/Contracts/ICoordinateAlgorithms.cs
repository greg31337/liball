namespace geo;

/// <summary>Provides robust planar coordinate, segment, vector, and ring algorithms.</summary>
public interface ICoordinateAlgorithms
{
    /// <summary>Compares coordinates lexicographically by X, Y, Z, and M.</summary>
    /// <param name="first">The first coordinate.</param>
    /// <param name="second">The second coordinate.</param>
    /// <returns>A negative value, zero, or a positive value according to coordinate order.</returns>
    int CompareCoordinates(Coordinate first, Coordinate second);

    /// <summary>Interpolates all available ordinates between two coordinates.</summary>
    /// <param name="start">The interpolation start.</param>
    /// <param name="end">The interpolation end.</param>
    /// <param name="factor">The finite interpolation factor.</param>
    /// <returns>An owning interpolated coordinate.</returns>
    Coordinate InterpolateCoordinate(Coordinate start, Coordinate end, double factor);

    /// <summary>Gets the exact planar orientation sign of three coordinates.</summary>
    /// <param name="first">The first coordinate.</param>
    /// <param name="second">The second coordinate.</param>
    /// <param name="third">The third coordinate.</param>
    /// <returns>One for counter-clockwise, minus one for clockwise, or zero for collinear.</returns>
    int OrientationIndex(Coordinate first, Coordinate second, Coordinate third);

    /// <summary>Locates a coordinate relative to a closed line segment.</summary>
    /// <param name="point">The coordinate to locate.</param>
    /// <param name="segmentStart">The segment start.</param>
    /// <param name="segmentEnd">The segment end.</param>
    /// <returns>Boundary at an endpoint, Interior between endpoints, or Exterior otherwise.</returns>
    TopologicalLocation LocatePointOnSegment(
        Coordinate point, Coordinate segmentStart, Coordinate segmentEnd);

    /// <summary>Classifies two closed segments and returns owning intersection witnesses.</summary>
    /// <param name="firstStart">The first segment start.</param>
    /// <param name="firstEnd">The first segment end.</param>
    /// <param name="secondStart">The second segment start.</param>
    /// <param name="secondEnd">The second segment end.</param>
    /// <returns>The segment intersection result.</returns>
    SegmentIntersectionResult IntersectSegments(
        Coordinate firstStart, Coordinate firstEnd,
        Coordinate secondStart, Coordinate secondEnd);

    /// <summary>Finds the nearest owning witness pair on two closed segments.</summary>
    /// <param name="firstStart">The first segment start.</param>
    /// <param name="firstEnd">The first segment end.</param>
    /// <param name="secondStart">The second segment start.</param>
    /// <param name="secondEnd">The second segment end.</param>
    /// <returns>The nearest witness result and segment locations.</returns>
    DistanceResult NearestSegmentPoints(
        Coordinate firstStart, Coordinate firstEnd,
        Coordinate secondStart, Coordinate secondEnd);

    /// <summary>Computes the signed planar area of an open or closed ring sequence.</summary>
    /// <param name="coordinates">The ring coordinates.</param>
    /// <returns>Positive area for counter-clockwise order and negative area for clockwise order.</returns>
    double SignedRingArea(Coordinate[] coordinates);

    /// <summary>Gets the robust orientation of an open or closed ring sequence.</summary>
    /// <param name="coordinates">The ring coordinates.</param>
    /// <returns>The ring orientation, or Undefined for zero exact signed area.</returns>
    RingOrientation RingOrientationOf(Coordinate[] coordinates);

    /// <summary>Computes the planar dot product of two coordinate vectors.</summary>
    /// <param name="first">The first vector.</param>
    /// <param name="second">The second vector.</param>
    /// <returns>The planar dot product.</returns>
    double DotProduct(Coordinate first, Coordinate second);

    /// <summary>Computes the planar cross product of two coordinate vectors.</summary>
    /// <param name="first">The first vector.</param>
    /// <param name="second">The second vector.</param>
    /// <returns>The signed planar cross product.</returns>
    double CrossProduct(Coordinate first, Coordinate second);

    /// <summary>Computes the smaller unsigned angle between two non-zero planar vectors.</summary>
    /// <param name="first">The first vector.</param>
    /// <param name="second">The second vector.</param>
    /// <returns>The angle in radians from zero through pi.</returns>
    double AngleBetween(Coordinate first, Coordinate second);
}
