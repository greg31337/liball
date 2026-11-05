using System;
using System.Collections.Generic;

namespace geo;

internal class CircleSearchCell
{
    internal double X;
    internal double Y;
    internal double HalfSize;
    internal double Distance;
    internal double MaximumDistance;
    internal Coordinate? Witness;
}

internal class CircleSearchResult
{
    internal Coordinate? Center;
    internal Coordinate? Witness;
    internal double Distance = double.NegativeInfinity;
}

internal static class InteriorCircleAlgorithms
{
    internal static CircleResult GetMaximumInscribedCircle(
        Geometry polygonal, double tolerance)
    {
        ValidateTolerance(tolerance);
        bool hasSurface = ValidatePolygonalGeometry(polygonal);
        if (!hasSurface) return new CircleResult();
        DistanceGeometryParts boundaryParts =
            new DistanceGeometryParts(polygonal);
        CircleSearchResult search = FindLargestCircle(
            polygonal, boundaryParts, null, tolerance);
        return CreateSearchCircleResult(polygonal, search);
    }

    internal static CircleResult GetLargestEmptyCircle(
        Geometry obstacles, Geometry boundary, double tolerance)
    {
        ValidateTolerance(tolerance);
        GeometryRelations.ValidateSpatialReferencePair(
            obstacles, boundary);
        bool hasSurface = ValidatePolygonalGeometry(boundary);
        if (!hasSurface) return new CircleResult();
        if (!CollectionRelationGraphBuilder.SupportsCoreGeometry(obstacles))
        {
            throw new InvalidGeometryException(
                "Largest empty circle obstacles must use core planar geometry.");
        }
        DistanceGeometryParts boundaryParts =
            new DistanceGeometryParts(boundary);
        DistanceGeometryParts obstacleParts =
            new DistanceGeometryParts(obstacles);
        CircleSearchResult search = FindLargestCircle(
            boundary, boundaryParts, obstacleParts, tolerance);
        return CreateSearchCircleResult(boundary, search);
    }

    internal static Geometry GetPolygonTangents(
        Geometry polygonal, Coordinate point)
    {
        if (point == null)
        {
            throw new InvalidGeometryException(
                "Polygon tangent point is null.");
        }
        bool hasSurface = ValidatePolygonalGeometry(polygonal);
        GeometryFactoryOptions options =
            GeometryNormalization.CreateNormalizationOptions(polygonal);
        if (!hasSurface)
        {
            return GeometryBuilder.CreateEmptyValue(
                GeometryType.MultiLineString, options);
        }
        GeometryBuilder.ValidateCoordinateForLayout(
            point, polygonal.CoordinateLayout);
        Geometry hullGeometry =
            ConvexHullAlgorithm.GetConvexHull(polygonal);
        if (hullGeometry.GeometryType != GeometryType.Polygon ||
            hullGeometry.Polygon == null)
        {
            throw new InvalidGeometryException(
                "Polygon tangents require a non-degenerate polygonal hull.");
        }
        if (GeometryPointLocation.LocatePointInPolygon(
            point, hullGeometry.Polygon) != TopologicalLocation.Exterior)
        {
            throw new InvalidGeometryException(
                "A polygon tangent point must lie outside the polygon hull.");
        }

        Coordinate[] closed =
            hullGeometry.Polygon.Shell.Coordinates;
        int vertexCount = closed.Length - 1;
        Coordinate? positive = null;
        Coordinate? negative = null;
        int candidateIndex = 0;
        while (candidateIndex < vertexCount)
        {
            Coordinate candidate = closed[candidateIndex];
            bool hasPositive = false;
            bool hasNegative = false;
            int pointIndex = 0;
            while (pointIndex < vertexCount)
            {
                int orientation = RobustOrientation.GetOrientationIndex(
                    point, candidate, closed[pointIndex]);
                if (orientation > 0) hasPositive = true;
                if (orientation < 0) hasNegative = true;
                pointIndex++;
            }
            if (!hasNegative)
            {
                positive = SelectNearerTangent(
                    point, positive, candidate);
            }
            if (!hasPositive)
            {
                negative = SelectNearerTangent(
                    point, negative, candidate);
            }
            candidateIndex++;
        }
        if (positive == null || negative == null)
        {
            throw new InvalidGeometryException(
                "Two polygon tangent contacts could not be determined.");
        }
        LineString first = new LineString();
        first.Coordinates =
        [
            RobustLineIntersection.CopyCoordinate(point),
            RobustLineIntersection.CopyCoordinate(positive)
        ];
        LineString second = new LineString();
        second.Coordinates =
        [
            RobustLineIntersection.CopyCoordinate(point),
            RobustLineIntersection.CopyCoordinate(negative)
        ];
        return GeometryBuilder.CreateMultiLineStringValue(
            [first, second], options);
    }

    internal static Coordinate SelectNearerTangent(
        Coordinate origin, Coordinate? current,
        Coordinate candidate)
    {
        if (current == null) return candidate;
        ExactDyadicValue currentSquared =
            SegmentDistanceAlgorithms.CreateSquaredSegmentLength(
                origin, current);
        ExactDyadicValue candidateSquared =
            SegmentDistanceAlgorithms.CreateSquaredSegmentLength(
                origin, candidate);
        if (ExactDyadicArithmetic.CompareAbsoluteValues(
            candidateSquared, currentSquared) < 0)
        {
            return candidate;
        }
        return current;
    }

    internal static CircleSearchResult FindLargestCircle(
        Geometry domain, DistanceGeometryParts boundaryParts,
        DistanceGeometryParts? obstacleParts, double tolerance)
    {
        Envelope envelope = EnvelopeAlgorithms.CreateGeometryEnvelope(
            domain);
        Coordinate[] sourceCoordinates =
            GeometryInspection.ExtractValidatedCoordinates(domain);
        Coordinate ordinateSource = sourceCoordinates[0];
        CircleSearchResult best = new CircleSearchResult();
        Coordinate? representative =
            RepresentativePointAlgorithm.GetPointOnSurface(domain);
        if (representative != null)
        {
            EvaluateSearchPoint(
                representative, domain, boundaryParts,
                obstacleParts, best);
        }
        Coordinate envelopeMinimum = CreateSearchCoordinate(
            envelope.MinX, envelope.MinY, ordinateSource);
        Coordinate envelopeMaximum = CreateSearchCoordinate(
            envelope.MaxX, envelope.MaxY, ordinateSource);
        Coordinate envelopeCenter =
            RobustLineIntersection.InterpolateLineCoordinate(
                envelopeMinimum, envelopeMaximum, 0.5);
        EvaluateSearchPoint(
            envelopeCenter, domain, boundaryParts,
            obstacleParts, best);

        double width = envelope.MaxX - envelope.MinX;
        double height = envelope.MaxY - envelope.MinY;
        double cellSize = width < height ? width : height;
        if (cellSize <= 0.0 || !double.IsFinite(cellSize))
        {
            return best;
        }
        double halfSize = cellSize / 2.0;
        PriorityQueue<CircleSearchCell, double> cells =
            new PriorityQueue<CircleSearchCell, double>();
        double x = envelope.MinX;
        while (x < envelope.MaxX)
        {
            double y = envelope.MinY;
            while (y < envelope.MaxY)
            {
                AddSearchCell(
                    cells, x + halfSize, y + halfSize,
                    halfSize, ordinateSource, domain,
                    boundaryParts, obstacleParts);
                double nextY = y + cellSize;
                if (nextY == y) break;
                y = nextY;
            }
            double nextX = x + cellSize;
            if (nextX == x) break;
            x = nextX;
        }

        int processed = 0;
        while (cells.Count != 0)
        {
            CircleSearchCell cell = cells.Dequeue();
            if (IsBetterSearchPoint(
                cell.X, cell.Y, cell.Distance, best))
            {
                best.Center = CreateSearchCoordinate(
                    cell.X, cell.Y, ordinateSource);
                best.Witness = cell.Witness;
                best.Distance = cell.Distance;
            }
            if (cell.MaximumDistance - best.Distance > tolerance)
            {
                double childHalfSize = cell.HalfSize / 2.0;
                if (childHalfSize > 0.0 &&
                    cell.X - childHalfSize != cell.X &&
                    cell.X + childHalfSize != cell.X &&
                    cell.Y - childHalfSize != cell.Y &&
                    cell.Y + childHalfSize != cell.Y)
                {
                    AddSearchCell(
                        cells, cell.X - childHalfSize,
                        cell.Y - childHalfSize,
                        childHalfSize, ordinateSource, domain,
                        boundaryParts, obstacleParts);
                    AddSearchCell(
                        cells, cell.X + childHalfSize,
                        cell.Y - childHalfSize,
                        childHalfSize, ordinateSource, domain,
                        boundaryParts, obstacleParts);
                    AddSearchCell(
                        cells, cell.X - childHalfSize,
                        cell.Y + childHalfSize,
                        childHalfSize, ordinateSource, domain,
                        boundaryParts, obstacleParts);
                    AddSearchCell(
                        cells, cell.X + childHalfSize,
                        cell.Y + childHalfSize,
                        childHalfSize, ordinateSource, domain,
                        boundaryParts, obstacleParts);
                }
            }
            processed++;
            if (processed > GeometryConstants.DefaultMaximumCoordinates)
            {
                throw new InvalidGeometryException(
                    "Interior circle search exceeded the work limit.");
            }
        }
        return best;
    }

    internal static void AddSearchCell(
        PriorityQueue<CircleSearchCell, double> cells,
        double x, double y, double halfSize,
        Coordinate ordinateSource, Geometry domain,
        DistanceGeometryParts boundaryParts,
        DistanceGeometryParts? obstacleParts)
    {
        CircleSearchCell cell = CreateSearchCell(
            x, y, halfSize, ordinateSource,
            domain, boundaryParts, obstacleParts);
        cells.Enqueue(cell, -cell.MaximumDistance);
        if (cells.Count > GeometryConstants.DefaultMaximumCoordinates)
        {
            throw new InvalidGeometryException(
                "Interior circle search exceeded the cell limit.");
        }
    }

    internal static CircleSearchCell CreateSearchCell(
        double x, double y, double halfSize,
        Coordinate ordinateSource, Geometry domain,
        DistanceGeometryParts boundaryParts,
        DistanceGeometryParts? obstacleParts)
    {
        Coordinate point = CreateSearchCoordinate(
            x, y, ordinateSource);
        CircleSearchResult evaluation = EvaluateCircleDistance(
            point, domain, boundaryParts, obstacleParts);
        CircleSearchCell cell = new CircleSearchCell();
        cell.X = x;
        cell.Y = y;
        cell.HalfSize = halfSize;
        cell.Distance = evaluation.Distance;
        cell.Witness = evaluation.Witness;
        cell.MaximumDistance = evaluation.Distance +
            EnvelopeAlgorithms.ScaledDistance(halfSize, halfSize);
        return cell;
    }

    internal static void EvaluateSearchPoint(
        Coordinate point, Geometry domain,
        DistanceGeometryParts boundaryParts,
        DistanceGeometryParts? obstacleParts,
        CircleSearchResult best)
    {
        CircleSearchResult evaluation = EvaluateCircleDistance(
            point, domain, boundaryParts, obstacleParts);
        if (IsBetterSearchPoint(
            point.X, point.Y, evaluation.Distance, best))
        {
            best.Center =
                RobustLineIntersection.CopyCoordinate(point);
            best.Witness = evaluation.Witness;
            best.Distance = evaluation.Distance;
        }
    }

    internal static CircleSearchResult EvaluateCircleDistance(
        Coordinate point, Geometry domain,
        DistanceGeometryParts boundaryParts,
        DistanceGeometryParts? obstacleParts)
    {
        CircleSearchResult result = new CircleSearchResult();
        result.Center = point;
        DistanceResult boundaryDistance = GetNearestConstraint(
            point, boundaryParts);
        if (!boundaryDistance.HasResult ||
            boundaryDistance.SecondCoordinate == null)
        {
            throw new InvalidGeometryException(
                "Circle search domain has no boundary facets.");
        }
        result.Distance = boundaryDistance.Distance;
        result.Witness = boundaryDistance.SecondCoordinate;
        TopologicalLocation domainLocation = LocateSearchPoint(
            point, domain);
        if (domainLocation == TopologicalLocation.Exterior)
        {
            result.Distance = -result.Distance;
            return result;
        }
        if (domainLocation == TopologicalLocation.Boundary)
        {
            result.Distance = 0.0;
            return result;
        }
        if (obstacleParts != null && HasConstraintFacets(obstacleParts))
        {
            DistanceResult obstacleDistance = GetNearestConstraint(
                point, obstacleParts);
            if (obstacleDistance.HasResult &&
                obstacleDistance.Distance < result.Distance)
            {
                result.Distance = obstacleDistance.Distance;
                result.Witness = obstacleDistance.SecondCoordinate;
            }
            if (LocateSearchPoint(point, obstacleParts.RootGeometry) !=
                TopologicalLocation.Exterior)
            {
                result.Distance = -obstacleDistance.Distance;
                result.Witness = obstacleDistance.SecondCoordinate;
            }
        }
        return result;
    }

    internal static DistanceResult GetNearestConstraint(
        Coordinate point, DistanceGeometryParts parts)
    {
        DistanceResult best = new DistanceResult();
        int pointIndex = 0;
        while (pointIndex < parts.Points.Count)
        {
            DistanceResult candidate =
                SegmentDistanceAlgorithms.GetNearestPointPair(
                    point, parts.Points[pointIndex].Coordinate);
            DistanceAlgorithm.SelectCandidate(best, candidate);
            pointIndex++;
        }
        int segmentIndex = 0;
        while (segmentIndex < parts.Segments.Count)
        {
            DistanceResult candidate =
                SegmentDistanceAlgorithms.GetNearestPointSegment(
                    point, parts.Segments[segmentIndex].Start,
                    parts.Segments[segmentIndex].End);
            DistanceAlgorithm.SelectCandidate(best, candidate);
            segmentIndex++;
        }
        return best;
    }

    internal static bool HasConstraintFacets(
        DistanceGeometryParts parts)
    {
        return parts.Points.Count != 0 || parts.Segments.Count != 0;
    }

    internal static bool IsBetterSearchPoint(
        double x, double y, double distance,
        CircleSearchResult best)
    {
        if (best.Center == null) return true;
        if (distance > best.Distance) return true;
        if (distance < best.Distance) return false;
        if (x < best.Center.X) return true;
        if (x > best.Center.X) return false;
        return y < best.Center.Y;
    }

    internal static TopologicalLocation LocateSearchPoint(
        Coordinate point, Geometry geometry)
    {
        return GeometryPointLocation.LocatePoint(
            point, geometry, new CurveLinearizationParameters());
    }

    internal static Coordinate CreateSearchCoordinate(
        double x, double y, Coordinate ordinateSource)
    {
        Coordinate coordinate = new Coordinate();
        coordinate.X = VectorMath.RequireFiniteResult(
            x, "Circle search X ordinate");
        coordinate.Y = VectorMath.RequireFiniteResult(
            y, "Circle search Y ordinate");
        coordinate.Z = ordinateSource.Z;
        coordinate.M = ordinateSource.M;
        return coordinate;
    }

    internal static CircleResult CreateSearchCircleResult(
        Geometry source, CircleSearchResult search)
    {
        if (search.Center == null || search.Witness == null ||
            search.Distance < 0.0)
        {
            return new CircleResult();
        }
        MinimumBoundingCircleState state =
            new MinimumBoundingCircleState();
        state.Center = search.Center;
        state.Radius = search.Distance;
        state.Witnesses =
            [RobustLineIntersection.CopyCoordinate(search.Witness)];
        return MinimumShapes.CreateCircleResult(source, state);
    }

    internal static bool ValidatePolygonalGeometry(Geometry geometry)
    {
        GeometryInspection.EnsureGeometryGraph(geometry);
        Geometry[] leaves = ComponentWalker.FlattenLeafGeometries(
            geometry,
            GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        bool hasSurface = false;
        int index = 0;
        while (index < leaves.Length)
        {
            Geometry leaf = leaves[index];
            if (leaf.GeometryType == GeometryType.Polygon)
            {
                GeometryInspection.ExtractValidatedCoordinates(leaf);
                if (!GeometryInspection.IsEmptyValue(leaf))
                {
                    hasSurface = true;
                }
            }
            else
            {
                bool emptyContainer =
                    (leaf.GeometryType == GeometryType.MultiPolygon ||
                        leaf.GeometryType == GeometryType.GeometryCollection) &&
                    GeometryInspection.IsEmptyValue(leaf);
                if (!emptyContainer)
                {
                    throw new InvalidGeometryException(
                        "Circle domain must contain only polygonal geometry.");
                }
            }
            index++;
        }
        return hasSurface;
    }

    internal static void ValidateTolerance(double tolerance)
    {
        if (!double.IsFinite(tolerance) || tolerance <= 0.0)
        {
            throw new InvalidGeometryException(
                "Circle search tolerance must be finite and positive.");
        }
    }
}
