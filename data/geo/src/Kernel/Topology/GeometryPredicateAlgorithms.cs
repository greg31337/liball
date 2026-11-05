using System;

namespace geo;

internal static class GeometryPredicateAlgorithms
{
    internal static bool Disjoint(Geometry first, Geometry second)
    {
        RelationMatrix matrix = BuildValidatedRelation(first, second);
        return MatrixIsDisjoint(matrix);
    }

    internal static bool Intersects(Geometry first, Geometry second)
    {
        RelationMatrix matrix = BuildValidatedRelation(first, second);
        return !MatrixIsDisjoint(matrix);
    }

    internal static bool Touches(Geometry first, Geometry second)
    {
        RelationMatrix matrix = BuildValidatedRelation(first, second);
        return RelationMatrixAlgorithms.MatchesPattern(
            matrix, De9ImPatterns.TouchesInteriorBoundary) ||
            RelationMatrixAlgorithms.MatchesPattern(
                matrix, De9ImPatterns.TouchesBoundaryInterior) ||
            RelationMatrixAlgorithms.MatchesPattern(
                matrix, De9ImPatterns.TouchesBoundaryBoundary);
    }

    internal static bool Crosses(Geometry first, Geometry second)
    {
        RelationMatrix matrix = BuildValidatedRelation(first, second);
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

    internal static bool Within(Geometry first, Geometry second)
    {
        RelationMatrix matrix = BuildValidatedRelation(first, second);
        return RelationMatrixAlgorithms.MatchesPattern(
            matrix, De9ImPatterns.Within);
    }

    internal static bool Contains(Geometry first, Geometry second)
    {
        RelationMatrix matrix = BuildValidatedRelation(first, second);
        return RelationMatrixAlgorithms.MatchesPattern(
            matrix, De9ImPatterns.Contains);
    }

    internal static bool Overlaps(Geometry first, Geometry second)
    {
        RelationMatrix matrix = BuildValidatedRelation(first, second);
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

    internal static bool Covers(Geometry first, Geometry second)
    {
        RelationMatrix matrix = BuildValidatedRelation(first, second);
        return RelationMatrixAlgorithms.MatchesPattern(
            matrix, De9ImPatterns.CoversInteriorInterior) ||
            RelationMatrixAlgorithms.MatchesPattern(
                matrix, De9ImPatterns.CoversInteriorBoundary) ||
            RelationMatrixAlgorithms.MatchesPattern(
                matrix, De9ImPatterns.CoversBoundaryInterior) ||
            RelationMatrixAlgorithms.MatchesPattern(
                matrix, De9ImPatterns.CoversBoundaryBoundary);
    }

    internal static bool CoveredBy(Geometry first, Geometry second)
    {
        RelationMatrix matrix = BuildValidatedRelation(first, second);
        return RelationMatrixAlgorithms.MatchesPattern(
            matrix, De9ImPatterns.CoveredByInteriorInterior) ||
            RelationMatrixAlgorithms.MatchesPattern(
                matrix, De9ImPatterns.CoveredByInteriorBoundary) ||
            RelationMatrixAlgorithms.MatchesPattern(
                matrix, De9ImPatterns.CoveredByBoundaryInterior) ||
            RelationMatrixAlgorithms.MatchesPattern(
                matrix, De9ImPatterns.CoveredByBoundaryBoundary);
    }

    internal static bool ContainsProperly(Geometry first, Geometry second)
    {
        RelationMatrix matrix = BuildValidatedRelation(first, second);
        return RelationMatrixAlgorithms.MatchesPattern(
            matrix, De9ImPatterns.ContainsProperly);
    }

    internal static bool WithinDistance(
        Geometry first, Geometry second, double maximumDistance)
    {
        return DistanceAlgorithm.IsWithinDistance(
            first, second, maximumDistance);
    }

    internal static RelationMatrix BuildValidatedRelation(
        Geometry first, Geometry second)
    {
        GeometryRelations.ValidateSpatialReferencePair(first, second);
        return RelationGraphBuilder.BuildRelation(first, second);
    }

    internal static bool MatrixIsDisjoint(RelationMatrix matrix)
    {
        return RelationMatrixAlgorithms.MatchesPattern(
            matrix, De9ImPatterns.Disjoint);
    }

    internal static void ValidateMaximumDistance(double maximumDistance)
    {
        DistanceAlgorithm.ValidateDistanceLimit(maximumDistance);
    }

    internal static bool AnyCoordinatePairWithinDistance(
        Coordinate[] first, Coordinate[] second, double maximumDistance)
    {
        int firstIndex = 0;
        while (firstIndex < first.Length)
        {
            int secondIndex = 0;
            while (secondIndex < second.Length)
            {
                DistanceResult result =
                    SegmentDistanceAlgorithms.GetNearestPointPair(
                        first[firstIndex], second[secondIndex]);
                if (DistanceIsWithinLimit(result.Distance, maximumDistance))
                {
                    return true;
                }
                secondIndex++;
            }
            firstIndex++;
        }
        return false;
    }

    internal static bool AnyCoordinateEdgeWithinDistance(
        Coordinate[] coordinates, SegmentString[] edges,
        double maximumDistance)
    {
        int coordinateIndex = 0;
        while (coordinateIndex < coordinates.Length)
        {
            int edgeIndex = 0;
            while (edgeIndex < edges.Length)
            {
                Coordinate[] edgeCoordinates = edges[edgeIndex].Coordinates;
                int segmentIndex = 0;
                while (segmentIndex + 1 < edgeCoordinates.Length)
                {
                    DistanceResult result =
                        SegmentDistanceAlgorithms.GetNearestPointSegment(
                            coordinates[coordinateIndex],
                            edgeCoordinates[segmentIndex],
                            edgeCoordinates[segmentIndex + 1]);
                    if (DistanceIsWithinLimit(
                        result.Distance, maximumDistance)) return true;
                    segmentIndex++;
                }
                edgeIndex++;
            }
            coordinateIndex++;
        }
        return false;
    }

    internal static bool AnyEdgePairWithinDistance(
        SegmentString[] first, SegmentString[] second,
        double maximumDistance)
    {
        int firstEdgeIndex = 0;
        while (firstEdgeIndex < first.Length)
        {
            Coordinate[] firstCoordinates =
                first[firstEdgeIndex].Coordinates;
            int firstSegmentIndex = 0;
            while (firstSegmentIndex + 1 < firstCoordinates.Length)
            {
                int secondEdgeIndex = 0;
                while (secondEdgeIndex < second.Length)
                {
                    Coordinate[] secondCoordinates =
                        second[secondEdgeIndex].Coordinates;
                    int secondSegmentIndex = 0;
                    while (secondSegmentIndex + 1 < secondCoordinates.Length)
                    {
                        DistanceResult result =
                            SegmentDistanceAlgorithms.GetNearestSegmentPair(
                                firstCoordinates[firstSegmentIndex],
                                firstCoordinates[firstSegmentIndex + 1],
                                secondCoordinates[secondSegmentIndex],
                                secondCoordinates[secondSegmentIndex + 1]);
                        if (DistanceIsWithinLimit(
                            result.Distance, maximumDistance)) return true;
                        secondSegmentIndex++;
                    }
                    secondEdgeIndex++;
                }
                firstSegmentIndex++;
            }
            firstEdgeIndex++;
        }
        return false;
    }

    internal static bool DistanceIsWithinLimit(
        double distance, double maximumDistance)
    {
        return DistanceAlgorithm.IsDistanceWithinLimit(
            distance, maximumDistance);
    }
}
