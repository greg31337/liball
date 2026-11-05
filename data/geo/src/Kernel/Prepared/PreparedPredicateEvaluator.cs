namespace geo;

internal static class PreparedPredicateEvaluator
{
    internal static bool DisjointPrepared(
        PreparedCollectionData prepared, Geometry geometry)
    {
        Envelope queryBounds = CreateValidatedQueryEnvelope(
            prepared, geometry);
        if (EnvelopeAlgorithms.IsDisjointEnvelopeValue(
            prepared.Bounds, queryBounds))
        {
            return true;
        }
        RelationMatrix matrix = BuildPreparedRelationValue(
            prepared, geometry);
        return GeometryPredicateAlgorithms.MatrixIsDisjoint(matrix);
    }

    internal static bool IntersectsPrepared(
        PreparedCollectionData prepared, Geometry geometry)
    {
        Envelope queryBounds = CreateValidatedQueryEnvelope(
            prepared, geometry);
        if (!EnvelopeAlgorithms.IntersectsEnvelopeValue(
            prepared.Bounds, queryBounds))
        {
            return false;
        }
        RelationMatrix matrix = BuildPreparedRelationValue(
            prepared, geometry);
        return !GeometryPredicateAlgorithms.MatrixIsDisjoint(matrix);
    }

    internal static bool TouchesPrepared(
        PreparedCollectionData prepared, Geometry geometry)
    {
        Envelope queryBounds = CreateValidatedQueryEnvelope(
            prepared, geometry);
        if (!EnvelopeAlgorithms.IntersectsEnvelopeValue(
            prepared.Bounds, queryBounds))
        {
            return false;
        }
        RelationMatrix matrix = BuildPreparedRelationValue(
            prepared, geometry);
        return RelationMatrixAlgorithms.MatchesPattern(
            matrix, De9ImPatterns.TouchesInteriorBoundary) ||
            RelationMatrixAlgorithms.MatchesPattern(
                matrix, De9ImPatterns.TouchesBoundaryInterior) ||
            RelationMatrixAlgorithms.MatchesPattern(
                matrix, De9ImPatterns.TouchesBoundaryBoundary);
    }

    internal static bool CrossesPrepared(
        PreparedCollectionData prepared, Geometry geometry)
    {
        Envelope queryBounds = CreateValidatedQueryEnvelope(
            prepared, geometry);
        if (!EnvelopeAlgorithms.IntersectsEnvelopeValue(
            prepared.Bounds, queryBounds))
        {
            return false;
        }
        RelationMatrix matrix = BuildPreparedRelationValue(
            prepared, geometry);
        if (matrix.LeftDimension < matrix.RightDimension)
        {
            return RelationMatrixAlgorithms.MatchesPattern(
                matrix, De9ImPatterns.CrossesLowerDimension);
        }
        if (matrix.LeftDimension > matrix.RightDimension)
        {
            return RelationMatrixAlgorithms.MatchesPattern(
                matrix, De9ImPatterns.CrossesHigherDimension);
        }
        if (matrix.LeftDimension == TopologicalDimension.Curve)
        {
            return RelationMatrixAlgorithms.MatchesPattern(
                matrix, De9ImPatterns.CrossesLineLine);
        }
        return false;
    }

    internal static bool WithinPrepared(
        PreparedCollectionData prepared, Geometry geometry)
    {
        Envelope queryBounds = CreateValidatedQueryEnvelope(
            prepared, geometry);
        if (!EnvelopeAlgorithms.CoversEnvelopeValue(
            queryBounds, prepared.Bounds))
        {
            return false;
        }
        RelationMatrix matrix = BuildPreparedRelationValue(
            prepared, geometry);
        return RelationMatrixAlgorithms.MatchesPattern(
            matrix, De9ImPatterns.Within);
    }

    internal static bool ContainsPrepared(
        PreparedCollectionData prepared, Geometry geometry)
    {
        Envelope queryBounds = CreateValidatedQueryEnvelope(
            prepared, geometry);
        if (!EnvelopeAlgorithms.CoversEnvelopeValue(
            prepared.Bounds, queryBounds))
        {
            return false;
        }
        RelationMatrix matrix = BuildPreparedRelationValue(
            prepared, geometry);
        return RelationMatrixAlgorithms.MatchesPattern(
            matrix, De9ImPatterns.Contains);
    }

    internal static bool OverlapsPrepared(
        PreparedCollectionData prepared, Geometry geometry)
    {
        Envelope queryBounds = CreateValidatedQueryEnvelope(
            prepared, geometry);
        if (!EnvelopeAlgorithms.IntersectsEnvelopeValue(
            prepared.Bounds, queryBounds))
        {
            return false;
        }
        RelationMatrix matrix = BuildPreparedRelationValue(
            prepared, geometry);
        if (matrix.LeftDimension != matrix.RightDimension)
        {
            return false;
        }
        if (matrix.LeftDimension == TopologicalDimension.Curve)
        {
            return RelationMatrixAlgorithms.MatchesPattern(
                matrix, De9ImPatterns.OverlapsLine);
        }
        if (matrix.LeftDimension == TopologicalDimension.Point ||
            matrix.LeftDimension == TopologicalDimension.Surface)
        {
            return RelationMatrixAlgorithms.MatchesPattern(
                matrix, De9ImPatterns.OverlapsPointOrArea);
        }
        return false;
    }

    internal static bool CoversPrepared(
        PreparedCollectionData prepared, Geometry geometry)
    {
        Envelope queryBounds = CreateValidatedQueryEnvelope(
            prepared, geometry);
        if (!EnvelopeAlgorithms.CoversEnvelopeValue(
            prepared.Bounds, queryBounds))
        {
            return false;
        }
        RelationMatrix matrix = BuildPreparedRelationValue(
            prepared, geometry);
        return RelationMatrixAlgorithms.MatchesPattern(
            matrix, De9ImPatterns.CoversInteriorInterior) ||
            RelationMatrixAlgorithms.MatchesPattern(
                matrix, De9ImPatterns.CoversInteriorBoundary) ||
            RelationMatrixAlgorithms.MatchesPattern(
                matrix, De9ImPatterns.CoversBoundaryInterior) ||
            RelationMatrixAlgorithms.MatchesPattern(
                matrix, De9ImPatterns.CoversBoundaryBoundary);
    }

    internal static bool CoveredByPrepared(
        PreparedCollectionData prepared, Geometry geometry)
    {
        Envelope queryBounds = CreateValidatedQueryEnvelope(
            prepared, geometry);
        if (!EnvelopeAlgorithms.CoversEnvelopeValue(
            queryBounds, prepared.Bounds))
        {
            return false;
        }
        RelationMatrix matrix = BuildPreparedRelationValue(
            prepared, geometry);
        return RelationMatrixAlgorithms.MatchesPattern(
            matrix, De9ImPatterns.CoveredByInteriorInterior) ||
            RelationMatrixAlgorithms.MatchesPattern(
                matrix, De9ImPatterns.CoveredByInteriorBoundary) ||
            RelationMatrixAlgorithms.MatchesPattern(
                matrix, De9ImPatterns.CoveredByBoundaryInterior) ||
            RelationMatrixAlgorithms.MatchesPattern(
                matrix, De9ImPatterns.CoveredByBoundaryBoundary);
    }

    internal static bool ContainsProperlyPrepared(
        PreparedCollectionData prepared, Geometry geometry)
    {
        Envelope queryBounds = CreateValidatedQueryEnvelope(
            prepared, geometry);
        if (!EnvelopeAlgorithms.CoversEnvelopeValue(
            prepared.Bounds, queryBounds))
        {
            return false;
        }
        RelationMatrix matrix = BuildPreparedRelationValue(
            prepared, geometry);
        return RelationMatrixAlgorithms.MatchesPattern(
            matrix, De9ImPatterns.ContainsProperly);
    }

    internal static RelationMatrix RelatePrepared(
        PreparedCollectionData prepared, Geometry geometry)
    {
        ValidatePreparedQuery(prepared, geometry);
        return BuildPreparedRelationValue(prepared, geometry);
    }

    internal static bool MatchPreparedRelation(
        PreparedCollectionData prepared, Geometry geometry, string pattern)
    {
        RelationMatrix matrix = RelatePrepared(prepared, geometry);
        return RelationMatrixAlgorithms.MatchesPattern(matrix, pattern);
    }

    internal static double DistancePrepared(
        PreparedCollectionData prepared, Geometry geometry)
    {
        return NearestPreparedPoints(prepared, geometry).Distance;
    }

    internal static bool WithinPreparedDistance(
        PreparedCollectionData prepared, Geometry geometry,
        double maximumDistance)
    {
        DistanceAlgorithm.ValidateDistanceLimit(maximumDistance);
        ValidatePreparedDistanceQuery(prepared, geometry);
        DistanceGeometryParts queryParts =
            new DistanceGeometryParts(geometry);
        Envelope queryBounds =
            EnvelopeAlgorithms.CreateGeometryEnvelope(geometry);
        SegmentSpatialIndex queryIndex = new SegmentSpatialIndex(
            queryParts.Segments.ToArray());
        return DistanceAlgorithm.ArePartsWithinDistance(
            prepared.Parts, prepared.SegmentIndex, prepared.Bounds,
            queryParts, queryIndex, queryBounds, maximumDistance);
    }

    internal static DistanceResult NearestPreparedPoints(
        PreparedCollectionData prepared, Geometry geometry)
    {
        ValidatePreparedDistanceQuery(prepared, geometry);
        DistanceGeometryParts queryParts =
            new DistanceGeometryParts(geometry);
        SegmentSpatialIndex queryIndex = new SegmentSpatialIndex(
            queryParts.Segments.ToArray());
        return DistanceAlgorithm.GetIndexedDistanceWithIndexes(
            prepared.Parts, prepared.SegmentIndex,
            queryParts, queryIndex);
    }

    internal static Envelope CreateValidatedQueryEnvelope(
        PreparedCollectionData prepared, Geometry geometry)
    {
        ValidatePreparedQuery(prepared, geometry);
        return EnvelopeAlgorithms.CreateGeometryEnvelope(geometry);
    }

    internal static void ValidatePreparedQuery(
        PreparedCollectionData prepared, Geometry geometry)
    {
        ValidatePreparedData(prepared);
        ValidatePreparedQueryGeometry(geometry);
        GeometryRelations.ValidateSpatialReferencePair(
            prepared.GeometrySnapshot, geometry);
    }

    internal static void ValidatePreparedDistanceQuery(
        PreparedCollectionData prepared, Geometry geometry)
    {
        ValidatePreparedData(prepared);
        ValidatePreparedQueryGeometry(geometry);
        GeometryRelations.ValidateSpatialReferencePair(
            prepared.GeometrySnapshot, geometry);
    }

    internal static void ValidatePreparedQueryGeometry(Geometry geometry)
    {
        ValidationResult validation =
            GeometryValidityChecker.ValidateGeometry(geometry);
        if (!validation.IsValid)
        {
            throw new InvalidGeometryException(
                "Prepared query geometry is invalid: " +
                validation.Message);
        }
        if (!CollectionRelationGraphBuilder.SupportsCoreGeometry(geometry))
        {
            throw new InvalidGeometryException(
                "Prepared queries support only core linear geometry types.");
        }
    }

    internal static void ValidatePreparedData(
        PreparedCollectionData prepared)
    {
        if (prepared == null)
        {
            throw new InvalidGeometryException(
                "Prepared geometry data is null.");
        }
    }

    internal static RelationMatrix BuildPreparedRelationValue(
        PreparedCollectionData prepared, Geometry geometry)
    {
        return RelationGraphBuilder.BuildRelation(
            prepared.GeometrySnapshot, geometry);
    }
}
