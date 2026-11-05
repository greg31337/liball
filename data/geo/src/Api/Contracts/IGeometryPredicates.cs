namespace geo;

/// <summary>Evaluates structural, normalized, topological, and distance predicates.</summary>
public interface IGeometryPredicates
{
    /// <summary>Determines whether two geometries have identical metadata, structure, and ordinates.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>True when the geometry values are exactly equal.</returns>
    bool AreExactlyEqual(Geometry first, Geometry second);

    /// <summary>Determines whether matching geometry structures have ordinates within a tolerance.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="tolerance">The finite non-negative ordinate tolerance.</param>
    /// <returns>True when the structures match and every ordinate is within the tolerance.</returns>
    bool AreEqualWithinTolerance(
        Geometry first, Geometry second, double tolerance);

    /// <summary>Determines whether canonical normalized geometry values are structurally equal.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>True when normalized copies of the values are equal.</returns>
    bool AreNormalizedEqual(Geometry first, Geometry second);

    /// <summary>Determines whether two core geometries represent the same planar point set.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>True when the geometries are topologically equal.</returns>
    bool AreTopologicallyEqual(Geometry first, Geometry second);

    /// <summary>Determines whether two core geometries have no point in common.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>True when the geometries are disjoint.</returns>
    bool Disjoint(Geometry first, Geometry second);

    /// <summary>Determines whether two core geometries have at least one point in common.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>True when the geometries intersect.</returns>
    bool Intersects(Geometry first, Geometry second);

    /// <summary>Determines whether core geometries meet through boundaries without interior overlap.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>True when the geometries touch.</returns>
    bool Touches(Geometry first, Geometry second);

    /// <summary>Determines whether core geometries cross according to their topological dimensions.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>True when the geometries cross.</returns>
    bool Crosses(Geometry first, Geometry second);

    /// <summary>Determines whether the first core geometry lies within the second.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>True when the first geometry is within the second.</returns>
    bool Within(Geometry first, Geometry second);

    /// <summary>Determines whether the first core geometry contains the second.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>True when the first geometry contains the second.</returns>
    bool Contains(Geometry first, Geometry second);

    /// <summary>Determines whether equal-dimensional core geometries partially overlap.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>True when the geometries overlap.</returns>
    bool Overlaps(Geometry first, Geometry second);

    /// <summary>Determines whether every point of the second core geometry is covered by the first.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>True when the first geometry covers the second.</returns>
    bool Covers(Geometry first, Geometry second);

    /// <summary>Determines whether every point of the first core geometry is covered by the second.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>True when the first geometry is covered by the second.</returns>
    bool CoveredBy(Geometry first, Geometry second);

    /// <summary>Determines whether the second core geometry lies strictly in the first geometry's interior.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>True when the first geometry properly contains the second.</returns>
    bool ContainsProperly(Geometry first, Geometry second);

    /// <summary>Determines whether two non-empty core geometries are within a planar distance.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="maximumDistance">The finite non-negative maximum distance.</param>
    /// <returns>True when the minimum planar distance does not exceed the limit.</returns>
    bool WithinDistance(
        Geometry first, Geometry second, double maximumDistance);

    /// <summary>Determines topological equality after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>True when the linearized geometries are topologically equal.</returns>
    bool AreTopologicallyEqualAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters);

    /// <summary>Determines disjointness after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>True when the linearized geometries are disjoint.</returns>
    bool DisjointAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters);

    /// <summary>Determines intersection after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>True when the linearized geometries intersect.</returns>
    bool IntersectsAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters);

    /// <summary>Determines boundary touching after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>True when the linearized geometries touch.</returns>
    bool TouchesAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters);

    /// <summary>Determines crossing after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>True when the linearized geometries cross.</returns>
    bool CrossesAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters);

    /// <summary>Determines within containment after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>True when the first linearized geometry is within the second.</returns>
    bool WithinAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters);

    /// <summary>Determines containment after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>True when the first linearized geometry contains the second.</returns>
    bool ContainsAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters);

    /// <summary>Determines overlap after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>True when the linearized geometries overlap.</returns>
    bool OverlapsAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters);

    /// <summary>Determines coverage after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>True when the first linearized geometry covers the second.</returns>
    bool CoversAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters);

    /// <summary>Determines reverse coverage after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>True when the first linearized geometry is covered by the second.</returns>
    bool CoveredByAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters);

    /// <summary>Determines proper containment after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>True when the first linearized geometry properly contains the second.</returns>
    bool ContainsProperlyAfterCurveLinearization(
        Geometry first, Geometry second,
        CurveLinearizationParameters parameters);

    /// <summary>Determines bounded distance after explicitly bounded curve linearization.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="maximumDistance">The finite non-negative maximum distance.</param>
    /// <param name="parameters">The arc subdivision and approximation-bound policy.</param>
    /// <returns>True when the linearized geometries are within the distance limit.</returns>
    bool WithinDistanceAfterCurveLinearization(
        Geometry first, Geometry second, double maximumDistance,
        CurveLinearizationParameters parameters);
}
