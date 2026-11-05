namespace geo;

/// <summary>Provides repeatable read-only topology and distance queries over an owned geometry snapshot.</summary>
public class PreparedGeometry : IPreparedGeometry
{
    internal PreparedCollectionData Data;

    /// <summary>Creates a prepared service from a valid core geometry snapshot.</summary>
    /// <param name="geometry">The valid core geometry to snapshot and prepare.</param>
    /// <exception cref="InvalidGeometryException">The geometry is null, invalid, or unsupported.</exception>
    public PreparedGeometry(Geometry geometry)
    {
        Data = new PreparedCollectionData(geometry);
    }

    /// <summary>Gets an owning copy of the geometry snapshot used by this service.</summary>
    /// <returns>An independent copy of the prepared geometry.</returns>
    public Geometry GetGeometrySnapshot()
    {
        return GeometryCopy.CopyGeometry(Data.GeometrySnapshot);
    }

    /// <summary>Determines whether the prepared geometry and another geometry have no point in common.</summary>
    /// <param name="geometry">The geometry to query.</param>
    /// <returns>True when the geometries are disjoint.</returns>
    public bool Disjoint(Geometry geometry)
    {
        return PreparedPredicateEvaluator.DisjointPrepared(Data, geometry);
    }

    /// <summary>Determines whether the prepared geometry and another geometry have a point in common.</summary>
    /// <param name="geometry">The geometry to query.</param>
    /// <returns>True when the geometries intersect.</returns>
    public bool Intersects(Geometry geometry)
    {
        return PreparedPredicateEvaluator.IntersectsPrepared(Data, geometry);
    }

    /// <summary>Determines whether the prepared geometry and another geometry meet only through boundaries.</summary>
    /// <param name="geometry">The geometry to query.</param>
    /// <returns>True when the geometries touch.</returns>
    public bool Touches(Geometry geometry)
    {
        return PreparedPredicateEvaluator.TouchesPrepared(Data, geometry);
    }

    /// <summary>Determines whether the prepared geometry crosses another geometry.</summary>
    /// <param name="geometry">The geometry to query.</param>
    /// <returns>True when the geometries cross.</returns>
    public bool Crosses(Geometry geometry)
    {
        return PreparedPredicateEvaluator.CrossesPrepared(Data, geometry);
    }

    /// <summary>Determines whether the prepared geometry lies within another geometry.</summary>
    /// <param name="geometry">The geometry to query.</param>
    /// <returns>True when the prepared geometry is within the query geometry.</returns>
    public bool Within(Geometry geometry)
    {
        return PreparedPredicateEvaluator.WithinPrepared(Data, geometry);
    }

    /// <summary>Determines whether the prepared geometry contains another geometry.</summary>
    /// <param name="geometry">The geometry to query.</param>
    /// <returns>True when the prepared geometry contains the query geometry.</returns>
    public bool Contains(Geometry geometry)
    {
        return PreparedPredicateEvaluator.ContainsPrepared(Data, geometry);
    }

    /// <summary>Determines whether the prepared geometry overlaps another geometry of the same dimension.</summary>
    /// <param name="geometry">The geometry to query.</param>
    /// <returns>True when the geometries overlap.</returns>
    public bool Overlaps(Geometry geometry)
    {
        return PreparedPredicateEvaluator.OverlapsPrepared(Data, geometry);
    }

    /// <summary>Determines whether the prepared geometry covers another geometry.</summary>
    /// <param name="geometry">The geometry to query.</param>
    /// <returns>True when the prepared geometry covers the query geometry.</returns>
    public bool Covers(Geometry geometry)
    {
        return PreparedPredicateEvaluator.CoversPrepared(Data, geometry);
    }

    /// <summary>Determines whether the prepared geometry is covered by another geometry.</summary>
    /// <param name="geometry">The geometry to query.</param>
    /// <returns>True when the prepared geometry is covered by the query geometry.</returns>
    public bool CoveredBy(Geometry geometry)
    {
        return PreparedPredicateEvaluator.CoveredByPrepared(Data, geometry);
    }

    /// <summary>Determines whether another geometry lies strictly in the prepared geometry's interior.</summary>
    /// <param name="geometry">The geometry to query.</param>
    /// <returns>True when the prepared geometry properly contains the query geometry.</returns>
    public bool ContainsProperly(Geometry geometry)
    {
        return PreparedPredicateEvaluator.ContainsProperlyPrepared(
            Data, geometry);
    }

    /// <summary>Computes the DE-9IM relation from the prepared geometry to another geometry.</summary>
    /// <param name="geometry">The geometry to query.</param>
    /// <returns>An owning relation matrix whose rows describe the prepared geometry.</returns>
    /// <exception cref="InvalidGeometryException">The query is invalid, unsupported, or has a conflicting known spatial reference identifier.</exception>
    public RelationMatrix RelatePrepared(Geometry geometry)
    {
        return PreparedPredicateEvaluator.RelatePrepared(Data, geometry);
    }

    /// <summary>Determines whether the prepared relation to another geometry matches a DE-9IM pattern.</summary>
    /// <param name="geometry">The geometry to query.</param>
    /// <param name="pattern">Nine T, F, wildcard, or exact-dimension symbols.</param>
    /// <returns>True when every relation cell satisfies its pattern symbol.</returns>
    /// <exception cref="InvalidGeometryException">The query, pattern, or spatial reference pairing is invalid.</exception>
    public bool MatchPreparedRelation(Geometry geometry, string pattern)
    {
        return PreparedPredicateEvaluator.MatchPreparedRelation(
            Data, geometry, pattern);
    }

    /// <summary>Computes the minimum planar distance from the prepared geometry to another geometry.</summary>
    /// <param name="geometry">The geometry to query.</param>
    /// <returns>The distance, or positive infinity when either geometry is empty.</returns>
    /// <exception cref="InvalidGeometryException">The query is invalid, unsupported, or has a conflicting known spatial reference identifier.</exception>
    public double DistancePrepared(Geometry geometry)
    {
        return PreparedPredicateEvaluator.DistancePrepared(Data, geometry);
    }

    /// <summary>Determines whether another non-empty geometry is within a planar distance of the prepared geometry.</summary>
    /// <param name="geometry">The geometry to query.</param>
    /// <param name="maximumDistance">The finite non-negative maximum distance.</param>
    /// <returns>True when the minimum planar distance does not exceed the limit.</returns>
    /// <exception cref="InvalidGeometryException">The query, distance, or spatial reference pairing is invalid.</exception>
    public bool WithinPreparedDistance(
        Geometry geometry, double maximumDistance)
    {
        return PreparedPredicateEvaluator.WithinPreparedDistance(
            Data, geometry, maximumDistance);
    }

    /// <summary>Finds the owning nearest coordinate pair between the prepared geometry and another geometry.</summary>
    /// <param name="geometry">The geometry to query.</param>
    /// <returns>The minimum-distance witness with the prepared geometry first.</returns>
    /// <exception cref="InvalidGeometryException">The query is invalid, unsupported, or has a conflicting known spatial reference identifier.</exception>
    public DistanceResult NearestPreparedPoints(Geometry geometry)
    {
        return PreparedPredicateEvaluator.NearestPreparedPoints(
            Data, geometry);
    }
}
