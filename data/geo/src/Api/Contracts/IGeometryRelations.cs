namespace geo;

/// <summary>Computes DE-9IM relations and point locations for geometry values.</summary>
public interface IGeometryRelations
{
    /// <summary>Computes a DE-9IM matrix using the OGC mod-2 boundary rule.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>An owning relation matrix whose rows describe the first geometry.</returns>
    /// <exception cref="InvalidGeometryException">An input is invalid, unsupported, or has a conflicting known spatial reference identifier.</exception>
    RelationMatrix Relate(Geometry first, Geometry second);

    /// <summary>Computes a DE-9IM matrix using a selected line endpoint boundary rule.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="boundaryNodeRule">The rule selecting line endpoint boundary nodes.</param>
    /// <returns>An owning relation matrix whose rows describe the first geometry.</returns>
    /// <exception cref="InvalidGeometryException">An input, boundary rule, or known spatial reference pairing is invalid.</exception>
    RelationMatrix RelateWithBoundaryRule(
        Geometry first, Geometry second, BoundaryNodeRule boundaryNodeRule);

    /// <summary>Determines whether a relation matrix satisfies a DE-9IM pattern.</summary>
    /// <param name="matrix">The relation matrix.</param>
    /// <param name="pattern">Nine T, F, wildcard, or exact-dimension symbols.</param>
    /// <returns>True when every matrix cell satisfies its pattern symbol.</returns>
    /// <exception cref="InvalidGeometryException">The matrix storage or pattern is invalid.</exception>
    bool MatchRelationPattern(RelationMatrix matrix, string pattern);

    /// <summary>Creates the converse of a relation matrix.</summary>
    /// <param name="matrix">The relation matrix to transpose.</param>
    /// <returns>An independent owning transposed matrix.</returns>
    /// <exception cref="InvalidGeometryException">The matrix storage is invalid.</exception>
    RelationMatrix TransposeRelation(RelationMatrix matrix);

    /// <summary>Locates a point in a geometry using planar X and Y topology.</summary>
    /// <param name="point">The point coordinate to locate.</param>
    /// <param name="geometry">The geometry being queried.</param>
    /// <returns>The point's interior, boundary, or exterior location.</returns>
    /// <exception cref="InvalidGeometryException">The point, geometry, or default curve linearization is invalid.</exception>
    TopologicalLocation LocatePoint(Coordinate point, Geometry geometry);
}
