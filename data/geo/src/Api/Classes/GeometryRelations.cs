namespace geo;

/// <summary>Provides DE-9IM relation and planar point-location operations.</summary>
public class GeometryRelations : IGeometryRelations
{
    /// <summary>Creates a geometry relation provider.</summary>
    public GeometryRelations()
    {
    }

    /// <summary>Computes a DE-9IM matrix using the OGC mod-2 boundary rule.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <returns>An owning relation matrix whose rows describe the first geometry.</returns>
    /// <exception cref="InvalidGeometryException">An input is invalid, unsupported, or has a conflicting known spatial reference identifier.</exception>
    public RelationMatrix Relate(Geometry first, Geometry second)
    {
        Geometry[] values = SurfaceKernelProjector.ProjectPair(
            first, second);
        return RelationGraphBuilder.BuildRelation(values[0], values[1]);
    }

    /// <summary>Computes a DE-9IM matrix using a selected line endpoint boundary rule.</summary>
    /// <param name="first">The first geometry.</param>
    /// <param name="second">The second geometry.</param>
    /// <param name="boundaryNodeRule">The rule selecting line endpoint boundary nodes.</param>
    /// <returns>An owning relation matrix whose rows describe the first geometry.</returns>
    /// <exception cref="InvalidGeometryException">An input, boundary rule, or known spatial reference pairing is invalid.</exception>
    public RelationMatrix RelateWithBoundaryRule(
        Geometry first, Geometry second, BoundaryNodeRule boundaryNodeRule)
    {
        Geometry[] values = SurfaceKernelProjector.ProjectPair(
            first, second);
        return RelationGraphBuilder.BuildRelationWithBoundaryRule(
            values[0], values[1], boundaryNodeRule);
    }

    /// <summary>Determines whether a relation matrix satisfies a DE-9IM pattern.</summary>
    /// <param name="matrix">The relation matrix.</param>
    /// <param name="pattern">Nine T, F, wildcard, or exact-dimension symbols.</param>
    /// <returns>True when every matrix cell satisfies its pattern symbol.</returns>
    /// <exception cref="InvalidGeometryException">The matrix storage or pattern is invalid.</exception>
    public bool MatchRelationPattern(RelationMatrix matrix, string pattern)
    {
        return RelationMatrixAlgorithms.MatchesPattern(matrix, pattern);
    }

    /// <summary>Creates the converse of a relation matrix.</summary>
    /// <param name="matrix">The relation matrix to transpose.</param>
    /// <returns>An independent owning transposed matrix.</returns>
    /// <exception cref="InvalidGeometryException">The matrix storage is invalid.</exception>
    public RelationMatrix TransposeRelation(RelationMatrix matrix)
    {
        return RelationMatrixAlgorithms.Transpose(matrix);
    }

    /// <summary>Locates a point in a geometry using planar X and Y topology.</summary>
    /// <param name="point">The point coordinate to locate.</param>
    /// <param name="geometry">The geometry being queried.</param>
    /// <returns>The point's interior, boundary, or exterior location.</returns>
    /// <exception cref="InvalidGeometryException">The point, geometry, or default curve linearization is invalid.</exception>
    public TopologicalLocation LocatePoint(Coordinate point, Geometry geometry)
    {
        VectorMath.ValidateCoordinateValue(point, "Point being located");
        if (CollectionRelationGraphBuilder.SupportsCoreGeometry(geometry))
        {
            RelationGeometryParts parts = new RelationGeometryParts(geometry);
            return CollectionRelationGraphBuilder.LocatePoint(
                point, parts, BoundaryNodeRule.Mod2);
        }
        return GeometryPointLocation.LocatePoint(
            point, geometry, new CurveLinearizationParameters());
    }

    internal static void ValidateSpatialReferencePair(
        Geometry first, Geometry second)
    {
        GeometryInspection.ValidateRootMetadata(first);
        GeometryInspection.ValidateRootMetadata(second);
        bool firstKnown = first.SpatialReferenceId !=
            GeometryConstants.UnknownSpatialReferenceId;
        bool secondKnown = second.SpatialReferenceId !=
            GeometryConstants.UnknownSpatialReferenceId;
        if (firstKnown && secondKnown &&
            first.SpatialReferenceId != second.SpatialReferenceId)
        {
            throw new InvalidGeometryException(
                "Geometry spatial reference identifiers must match when both are known.");
        }
    }
}
