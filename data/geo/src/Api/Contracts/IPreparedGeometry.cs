namespace geo;

/// <summary>Evaluates repeated planar queries against an immutable prepared geometry snapshot.</summary>
public interface IPreparedGeometry
{
    /// <summary>Gets an owning copy of the geometry snapshot used by this service.</summary>
    /// <returns>An independent copy of the prepared geometry.</returns>
    Geometry GetGeometrySnapshot();

    /// <summary>Determines whether the prepared geometry and another geometry have no point in common.</summary>
    /// <param name="geometry">The geometry to query.</param>
    /// <returns>True when the geometries are disjoint.</returns>
    bool Disjoint(Geometry geometry);

    /// <summary>Determines whether the prepared geometry and another geometry have a point in common.</summary>
    /// <param name="geometry">The geometry to query.</param>
    /// <returns>True when the geometries intersect.</returns>
    bool Intersects(Geometry geometry);

    /// <summary>Determines whether the prepared geometry and another geometry meet only through boundaries.</summary>
    /// <param name="geometry">The geometry to query.</param>
    /// <returns>True when the geometries touch.</returns>
    bool Touches(Geometry geometry);

    /// <summary>Determines whether the prepared geometry crosses another geometry.</summary>
    /// <param name="geometry">The geometry to query.</param>
    /// <returns>True when the geometries cross.</returns>
    bool Crosses(Geometry geometry);

    /// <summary>Determines whether the prepared geometry lies within another geometry.</summary>
    /// <param name="geometry">The geometry to query.</param>
    /// <returns>True when the prepared geometry is within the query geometry.</returns>
    bool Within(Geometry geometry);

    /// <summary>Determines whether the prepared geometry contains another geometry.</summary>
    /// <param name="geometry">The geometry to query.</param>
    /// <returns>True when the prepared geometry contains the query geometry.</returns>
    bool Contains(Geometry geometry);

    /// <summary>Determines whether the prepared geometry overlaps another geometry of the same dimension.</summary>
    /// <param name="geometry">The geometry to query.</param>
    /// <returns>True when the geometries overlap.</returns>
    bool Overlaps(Geometry geometry);

    /// <summary>Determines whether the prepared geometry covers another geometry.</summary>
    /// <param name="geometry">The geometry to query.</param>
    /// <returns>True when the prepared geometry covers the query geometry.</returns>
    bool Covers(Geometry geometry);

    /// <summary>Determines whether the prepared geometry is covered by another geometry.</summary>
    /// <param name="geometry">The geometry to query.</param>
    /// <returns>True when the prepared geometry is covered by the query geometry.</returns>
    bool CoveredBy(Geometry geometry);

    /// <summary>Determines whether another geometry lies strictly in the prepared geometry's interior.</summary>
    /// <param name="geometry">The geometry to query.</param>
    /// <returns>True when the prepared geometry properly contains the query geometry.</returns>
    bool ContainsProperly(Geometry geometry);

    /// <summary>Computes the DE-9IM relation from the prepared geometry to another geometry.</summary>
    /// <param name="geometry">The geometry to query.</param>
    /// <returns>An owning relation matrix whose rows describe the prepared geometry.</returns>
    RelationMatrix RelatePrepared(Geometry geometry);

    /// <summary>Determines whether the prepared relation to another geometry matches a DE-9IM pattern.</summary>
    /// <param name="geometry">The geometry to query.</param>
    /// <param name="pattern">Nine T, F, wildcard, or exact-dimension symbols.</param>
    /// <returns>True when every relation cell satisfies its pattern symbol.</returns>
    bool MatchPreparedRelation(Geometry geometry, string pattern);

    /// <summary>Computes the minimum planar distance from the prepared geometry to another geometry.</summary>
    /// <param name="geometry">The geometry to query.</param>
    /// <returns>The distance, or positive infinity when either geometry is empty.</returns>
    double DistancePrepared(Geometry geometry);

    /// <summary>Determines whether another non-empty geometry is within a planar distance of the prepared geometry.</summary>
    /// <param name="geometry">The geometry to query.</param>
    /// <param name="maximumDistance">The finite non-negative maximum distance.</param>
    /// <returns>True when the minimum planar distance does not exceed the limit.</returns>
    bool WithinPreparedDistance(
        Geometry geometry, double maximumDistance);

    /// <summary>Finds the owning nearest coordinate pair between the prepared geometry and another geometry.</summary>
    /// <param name="geometry">The geometry to query.</param>
    /// <returns>The minimum-distance witness with the prepared geometry first.</returns>
    DistanceResult NearestPreparedPoints(Geometry geometry);
}
