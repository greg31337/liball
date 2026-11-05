using System;
using System.Collections.Generic;

namespace geo;

internal static class RelationGraphBuilder
{
    internal static RelationMatrix BuildRelation(
        Geometry first, Geometry second)
    {
        return BuildRelationWithBoundaryRule(
            first, second, BoundaryNodeRule.Mod2);
    }

    internal static RelationMatrix BuildRelationWithBoundaryRule(
        Geometry first, Geometry second, BoundaryNodeRule boundaryNodeRule)
    {
        GeometryBoundary.ValidateBoundaryNodeRule(boundaryNodeRule);
        return CollectionRelationGraphBuilder.BuildRelation(
            first, second, boundaryNodeRule);
    }

    internal static RelationMatrix BuildAtomicRelation(
        Geometry first, Geometry second)
    {
        ValidateAtomicGeometry(first);
        ValidateAtomicGeometry(second);
        TopologicalDimension firstDimension =
            GeometryInspection.GetTopologicalDimensionValue(first);
        TopologicalDimension secondDimension =
            GeometryInspection.GetTopologicalDimensionValue(second);
        RelationMatrix matrix = new RelationMatrix
        {
            LeftDimension = firstDimension,
            RightDimension = secondDimension
        };
        RelationMatrixAlgorithms.SetCell(
            matrix, TopologicalLocation.Exterior,
            TopologicalLocation.Exterior, RelationDimension.Surface);

        bool firstEmpty = firstDimension == TopologicalDimension.Empty;
        bool secondEmpty = secondDimension == TopologicalDimension.Empty;
        if (firstEmpty || secondEmpty)
        {
            PopulateEmptyRelation(
                matrix, first, second, firstEmpty, secondEmpty);
            return matrix;
        }

        if (firstDimension == TopologicalDimension.Point)
        {
            return BuildPointRelation(first, second);
        }
        if (secondDimension == TopologicalDimension.Point)
        {
            return RelationMatrixAlgorithms.Transpose(
                BuildPointRelation(second, first));
        }
        if (firstDimension == TopologicalDimension.Curve &&
            secondDimension == TopologicalDimension.Curve)
        {
            PopulateCurveCurveRelation(matrix, first, second);
            return matrix;
        }
        if (firstDimension == TopologicalDimension.Curve &&
            secondDimension == TopologicalDimension.Surface)
        {
            PopulateCurveSurfaceRelation(matrix, first, second);
            return matrix;
        }
        if (firstDimension == TopologicalDimension.Surface &&
            secondDimension == TopologicalDimension.Curve)
        {
            return RelationMatrixAlgorithms.Transpose(
                BuildCurveSurfaceRelation(second, first));
        }

        PopulateSurfaceSurfaceRelation(matrix, first, second);
        return matrix;
    }

    internal static RelationMatrix BuildPointRelation(
        Geometry pointGeometry, Geometry other)
    {
        RelationMatrix matrix = new RelationMatrix
        {
            LeftDimension = TopologicalDimension.Point,
            RightDimension = GeometryInspection.GetTopologicalDimensionValue(other)
        };
        RelationMatrixAlgorithms.SetCell(
            matrix, TopologicalLocation.Exterior,
            TopologicalLocation.Exterior, RelationDimension.Surface);
        Point pointPayload = (Point)GeometryDispatch.GetSelectedPayload(pointGeometry);
        if (pointPayload.Coordinate == null)
        {
            throw new InvalidGeometryException(
                "A non-empty point relation requires a coordinate.");
        }
        TopologicalLocation location = LocatePoint(
            pointPayload.Coordinate, other);
        RelationMatrixAlgorithms.SetCell(
            matrix, TopologicalLocation.Interior,
            location, RelationDimension.Point);

        TopologicalDimension otherDimension = matrix.RightDimension;
        if (otherDimension != TopologicalDimension.Point ||
            location != TopologicalLocation.Interior)
        {
            RelationMatrixAlgorithms.SetCell(
                matrix, TopologicalLocation.Exterior,
                TopologicalLocation.Interior,
                ToRelationDimension(otherDimension));
        }
        RelationDimension boundaryDimension = GetBoundaryDimension(other);
        RelationMatrixAlgorithms.SetCellAtLeast(
            matrix, TopologicalLocation.Exterior,
            TopologicalLocation.Boundary, boundaryDimension);
        return matrix;
    }

    internal static RelationMatrix BuildCurveSurfaceRelation(
        Geometry curve, Geometry surface)
    {
        RelationMatrix matrix = new RelationMatrix
        {
            LeftDimension = TopologicalDimension.Curve,
            RightDimension = TopologicalDimension.Surface
        };
        RelationMatrixAlgorithms.SetCell(
            matrix, TopologicalLocation.Exterior,
            TopologicalLocation.Exterior, RelationDimension.Surface);
        PopulateCurveSurfaceRelation(matrix, curve, surface);
        return matrix;
    }

    internal static void PopulateEmptyRelation(
        RelationMatrix matrix, Geometry first, Geometry second,
        bool firstEmpty, bool secondEmpty)
    {
        if (!firstEmpty && secondEmpty)
        {
            RelationMatrixAlgorithms.SetCell(
                matrix, TopologicalLocation.Interior,
                TopologicalLocation.Exterior,
                ToRelationDimension(matrix.LeftDimension));
            RelationMatrixAlgorithms.SetCellAtLeast(
                matrix, TopologicalLocation.Boundary,
                TopologicalLocation.Exterior,
                GetBoundaryDimension(first));
        }
        if (firstEmpty && !secondEmpty)
        {
            RelationMatrixAlgorithms.SetCell(
                matrix, TopologicalLocation.Exterior,
                TopologicalLocation.Interior,
                ToRelationDimension(matrix.RightDimension));
            RelationMatrixAlgorithms.SetCellAtLeast(
                matrix, TopologicalLocation.Exterior,
                TopologicalLocation.Boundary,
                GetBoundaryDimension(second));
        }
    }

    internal static void PopulateCurveCurveRelation(
        RelationMatrix matrix, Geometry first, Geometry second)
    {
        SegmentString[] firstEdges = CreateCurveEdges(first);
        SegmentString[] secondEdges = CreateCurveEdges(second);
        NodingResult noded = IndexedNoder.NodeMutual(
            firstEdges, secondEdges, false);
        LabelFragments(
            matrix, noded.First, first, TopologicalLocation.Interior,
            second, true);
        LabelFragments(
            matrix, noded.Second, second, TopologicalLocation.Interior,
            first, false);
        LabelMutualIntersectionPoints(
            matrix, firstEdges, secondEdges, first, second);
        LabelCurveBoundary(matrix, first, second, true);
        LabelCurveBoundary(matrix, second, first, false);
    }

    internal static void PopulateCurveSurfaceRelation(
        RelationMatrix matrix, Geometry curve, Geometry surface)
    {
        SegmentString[] curveEdges = CreateCurveEdges(curve);
        SegmentString[] surfaceEdges = CreateSurfaceBoundaryEdges(surface);
        NodingResult noded = IndexedNoder.NodeMutual(
            curveEdges, surfaceEdges, false);
        LabelFragments(
            matrix, noded.First, curve, TopologicalLocation.Interior,
            surface, true);
        LabelFragments(
            matrix, noded.Second, surface, TopologicalLocation.Boundary,
            curve, false);
        LabelMutualIntersectionPoints(
            matrix, curveEdges, surfaceEdges, curve, surface);
        LabelCurveBoundary(matrix, curve, surface, true);
        RelationMatrixAlgorithms.SetCell(
            matrix, TopologicalLocation.Exterior,
            TopologicalLocation.Interior, RelationDimension.Surface);
    }

    internal static void PopulateSurfaceSurfaceRelation(
        RelationMatrix matrix, Geometry first, Geometry second)
    {
        SegmentString[] firstEdges = CreateSurfaceBoundaryEdges(first);
        SegmentString[] secondEdges = CreateSurfaceBoundaryEdges(second);
        NodingResult noded = IndexedNoder.NodeMutual(
            firstEdges, secondEdges, false);
        LabelFragments(
            matrix, noded.First, first, TopologicalLocation.Boundary,
            second, true);
        LabelFragments(
            matrix, noded.Second, second, TopologicalLocation.Boundary,
            first, false);
        bool properIntersection = LabelMutualIntersectionPoints(
            matrix, firstEdges, secondEdges, first, second);

        Coordinate firstInterior = FindSurfaceInteriorPoint(first);
        Coordinate secondInterior = FindSurfaceInteriorPoint(second);
        TopologicalLocation firstInteriorLocation =
            LocatePoint(firstInterior, second);
        TopologicalLocation secondInteriorLocation =
            LocatePoint(secondInterior, first);
        bool boundaryFirstInsideSecond = HasCurveCell(
            matrix, TopologicalLocation.Boundary,
            TopologicalLocation.Interior);
        bool boundarySecondInsideFirst = HasCurveCell(
            matrix, TopologicalLocation.Interior,
            TopologicalLocation.Boundary);
        bool interiorsIntersect = properIntersection ||
            boundaryFirstInsideSecond || boundarySecondInsideFirst ||
            firstInteriorLocation == TopologicalLocation.Interior ||
            secondInteriorLocation == TopologicalLocation.Interior;
        bool firstHasExteriorPart = HasCurveCell(
            matrix, TopologicalLocation.Boundary,
            TopologicalLocation.Exterior) ||
            boundarySecondInsideFirst ||
            firstInteriorLocation == TopologicalLocation.Exterior;
        bool secondHasExteriorPart = HasCurveCell(
            matrix, TopologicalLocation.Exterior,
            TopologicalLocation.Boundary) ||
            boundaryFirstInsideSecond ||
            secondInteriorLocation == TopologicalLocation.Exterior;

        if (interiorsIntersect)
        {
            RelationMatrixAlgorithms.SetCell(
                matrix, TopologicalLocation.Interior,
                TopologicalLocation.Interior, RelationDimension.Surface);
        }
        if (firstHasExteriorPart)
        {
            RelationMatrixAlgorithms.SetCell(
                matrix, TopologicalLocation.Interior,
                TopologicalLocation.Exterior, RelationDimension.Surface);
        }
        if (secondHasExteriorPart)
        {
            RelationMatrixAlgorithms.SetCell(
                matrix, TopologicalLocation.Exterior,
                TopologicalLocation.Interior, RelationDimension.Surface);
        }
    }

    internal static void LabelFragments(
        RelationMatrix matrix, SegmentString[] fragments,
        Geometry owner, TopologicalLocation ownerLocation,
        Geometry other, bool ownerIsFirst)
    {
        int fragmentIndex = 0;
        while (fragmentIndex < fragments.Length)
        {
            Coordinate[] coordinates = fragments[fragmentIndex].Coordinates;
            int segmentIndex = 0;
            while (segmentIndex + 1 < coordinates.Length)
            {
                Coordinate midpoint = CreateMidpoint(
                    coordinates[segmentIndex], coordinates[segmentIndex + 1]);
                TopologicalLocation locatedOwner = LocatePoint(midpoint, owner);
                if (locatedOwner == ownerLocation)
                {
                    TopologicalLocation otherLocation = LocatePoint(midpoint, other);
                    if (ownerIsFirst)
                    {
                        RelationMatrixAlgorithms.SetCellAtLeast(
                            matrix, ownerLocation, otherLocation,
                            RelationDimension.Curve);
                    }
                    else
                    {
                        RelationMatrixAlgorithms.SetCellAtLeast(
                            matrix, otherLocation, ownerLocation,
                            RelationDimension.Curve);
                    }
                }
                segmentIndex++;
            }
            fragmentIndex++;
        }
    }

    internal static bool LabelMutualIntersectionPoints(
        RelationMatrix matrix, SegmentString[] firstEdges,
        SegmentString[] secondEdges, Geometry first, Geometry second)
    {
        bool properIntersection = false;
        int firstEdgeIndex = 0;
        while (firstEdgeIndex < firstEdges.Length)
        {
            Coordinate[] firstCoordinates = firstEdges[firstEdgeIndex].Coordinates;
            int firstSegmentIndex = 0;
            while (firstSegmentIndex + 1 < firstCoordinates.Length)
            {
                int secondEdgeIndex = 0;
                while (secondEdgeIndex < secondEdges.Length)
                {
                    Coordinate[] secondCoordinates =
                        secondEdges[secondEdgeIndex].Coordinates;
                    int secondSegmentIndex = 0;
                    while (secondSegmentIndex + 1 < secondCoordinates.Length)
                    {
                        SegmentIntersectionResult intersection =
                            RobustLineIntersection.GetSegmentIntersection(
                                firstCoordinates[firstSegmentIndex],
                                firstCoordinates[firstSegmentIndex + 1],
                                secondCoordinates[secondSegmentIndex],
                                secondCoordinates[secondSegmentIndex + 1]);
                        if (intersection.IsProper) properIntersection = true;
                        LabelIntersectionWitness(
                            matrix, intersection.FirstIntersection,
                            first, second);
                        LabelIntersectionWitness(
                            matrix, intersection.SecondIntersection,
                            first, second);
                        secondSegmentIndex++;
                    }
                    secondEdgeIndex++;
                }
                firstSegmentIndex++;
            }
            firstEdgeIndex++;
        }
        return properIntersection;
    }

    internal static void LabelIntersectionWitness(
        RelationMatrix matrix, Coordinate? witness,
        Geometry first, Geometry second)
    {
        if (witness == null) return;
        TopologicalLocation firstLocation = LocatePoint(witness, first);
        TopologicalLocation secondLocation = LocatePoint(witness, second);
        RelationMatrixAlgorithms.SetCellAtLeast(
            matrix, firstLocation, secondLocation,
            RelationDimension.Point);
    }

    internal static void LabelCurveBoundary(
        RelationMatrix matrix, Geometry curve, Geometry other,
        bool curveIsFirst)
    {
        Coordinate[] coordinates = GetCurveCoordinates(curve);
        if (coordinates.Length == 0 || CoordinatesAreClosed(coordinates)) return;
        LabelCurveBoundaryPoint(
            matrix, coordinates[0], other, curveIsFirst);
        LabelCurveBoundaryPoint(
            matrix, coordinates[coordinates.Length - 1], other, curveIsFirst);
    }

    internal static void LabelCurveBoundaryPoint(
        RelationMatrix matrix, Coordinate coordinate,
        Geometry other, bool curveIsFirst)
    {
        TopologicalLocation otherLocation = LocatePoint(coordinate, other);
        if (curveIsFirst)
        {
            RelationMatrixAlgorithms.SetCellAtLeast(
                matrix, TopologicalLocation.Boundary,
                otherLocation, RelationDimension.Point);
        }
        else
        {
            RelationMatrixAlgorithms.SetCellAtLeast(
                matrix, otherLocation,
                TopologicalLocation.Boundary, RelationDimension.Point);
        }
    }

    internal static SegmentString[] CreateCurveEdges(Geometry geometry)
    {
        Coordinate[] coordinates = GetCurveCoordinates(geometry);
        return [SegmentNoding.CreateSegmentString(coordinates, geometry, 0)];
    }

    internal static SegmentString[] CreateSurfaceBoundaryEdges(Geometry geometry)
    {
        Polygon polygon = GetPolygon(geometry);
        SegmentString[] edges = new SegmentString[polygon.Holes.Length + 1];
        edges[0] = SegmentNoding.CreateSegmentString(
            polygon.Shell.Coordinates, polygon.Shell, 0);
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            LinearRing hole = polygon.Holes[index];
            if (hole == null)
            {
                throw new InvalidGeometryException("Polygon hole is null.");
            }
            edges[index + 1] = SegmentNoding.CreateSegmentString(
                hole.Coordinates, hole, index + 1);
            index++;
        }
        return edges;
    }

    internal static Coordinate[] GetCurveCoordinates(Geometry geometry)
    {
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        if (geometry.GeometryType == GeometryType.LineString)
        {
            return ((LineString)payload).Coordinates;
        }
        if (geometry.GeometryType == GeometryType.LinearRing)
        {
            return ((LinearRing)payload).Coordinates;
        }
        throw new InvalidGeometryException(
            "Relation curve must be a LineString or LinearRing.");
    }

    internal static Polygon GetPolygon(Geometry geometry)
    {
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        if (geometry.GeometryType != GeometryType.Polygon)
        {
            throw new InvalidGeometryException(
                "Relation surface must be a Polygon.");
        }
        Polygon polygon = (Polygon)payload;
        if (polygon.Shell == null || polygon.Holes == null)
        {
            throw new InvalidGeometryException(
                "Polygon payload, shell, or hole array is null.");
        }
        return polygon;
    }

    internal static Coordinate FindSurfaceInteriorPoint(Geometry geometry)
    {
        Polygon polygon = GetPolygon(geometry);
        List<double> ordinates = new List<double>();
        AddRingYOrdinates(polygon.Shell, ordinates);
        int holeIndex = 0;
        while (holeIndex < polygon.Holes.Length)
        {
            AddRingYOrdinates(polygon.Holes[holeIndex], ordinates);
            holeIndex++;
        }
        ordinates.Sort();
        int index = 0;
        while (index + 1 < ordinates.Count)
        {
            double lower = ordinates[index];
            double upper = ordinates[index + 1];
            if (lower < upper)
            {
                double y = MidpointOrdinate(lower, upper);
                if (y > lower && y < upper)
                {
                    Coordinate? point = FindInteriorPointOnScanline(
                        polygon, y);
                    if (point != null) return point;
                }
            }
            index++;
        }
        throw new InvalidGeometryException(
            "A non-empty polygon must contain a representable interior point.");
    }

    internal static Coordinate? FindInteriorPointOnScanline(
        Polygon polygon, double y)
    {
        List<double> intersections = new List<double>();
        AddRingScanlineIntersections(
            polygon.Shell, y, intersections);
        int holeIndex = 0;
        while (holeIndex < polygon.Holes.Length)
        {
            AddRingScanlineIntersections(
                polygon.Holes[holeIndex], y, intersections);
            holeIndex++;
        }
        intersections.Sort();
        int index = 0;
        while (index + 1 < intersections.Count)
        {
            double left = intersections[index];
            double right = intersections[index + 1];
            if (left < right)
            {
                double x = MidpointOrdinate(left, right);
                if (x > left && x < right)
                {
                    Coordinate candidate = new Coordinate { X = x, Y = y };
                    if (GeometryPointLocation.LocatePointInPolygon(
                        candidate, polygon) == TopologicalLocation.Interior)
                    {
                        return candidate;
                    }
                }
            }
            index += 2;
        }
        return null;
    }

    internal static void AddRingYOrdinates(
        LinearRing ring, List<double> ordinates)
    {
        if (ring == null || ring.Coordinates == null)
        {
            throw new InvalidGeometryException(
                "Polygon ring or coordinate sequence is null.");
        }
        int index = 0;
        while (index < ring.Coordinates.Length)
        {
            ordinates.Add(ring.Coordinates[index].Y);
            index++;
        }
    }

    internal static void AddRingScanlineIntersections(
        LinearRing ring, double y, List<double> intersections)
    {
        Coordinate[] coordinates = ring.Coordinates;
        int index = 0;
        while (index + 1 < coordinates.Length)
        {
            Coordinate start = coordinates[index];
            Coordinate end = coordinates[index + 1];
            if (start.Y < y && end.Y > y || end.Y < y && start.Y > y)
            {
                double factor = (y - start.Y) / (end.Y - start.Y);
                intersections.Add(RobustLineIntersection.InterpolateLineOrdinate(
                    start.X, end.X, factor));
            }
            index++;
        }
    }

    internal static Coordinate CreateMidpoint(
        Coordinate start, Coordinate end)
    {
        Coordinate midpoint = new Coordinate();
        midpoint.X = MidpointOrdinate(start.X, end.X);
        midpoint.Y = MidpointOrdinate(start.Y, end.Y);
        return midpoint;
    }

    internal static double MidpointOrdinate(double first, double second)
    {
        return (first * 0.5) + (second * 0.5);
    }

    internal static TopologicalLocation LocatePoint(
        Coordinate point, Geometry geometry)
    {
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
                return GeometryPointLocation.LocatePointPayload(
                    point, (Point)payload);
            case GeometryType.LineString:
                return GeometryPointLocation.LocatePointOnCoordinateLine(
                    point, ((LineString)payload).Coordinates, false);
            case GeometryType.LinearRing:
                return GeometryPointLocation.LocatePointOnCoordinateLine(
                    point, ((LinearRing)payload).Coordinates, true);
            case GeometryType.Polygon:
                return GeometryPointLocation.LocatePointInPolygon(
                    point, (Polygon)payload);
            default:
                throw new InvalidGeometryException(
                    "Relation geometry type is not supported.");
        }
    }

    internal static RelationDimension GetBoundaryDimension(Geometry geometry)
    {
        if (geometry.GeometryType == GeometryType.Point)
        {
            return RelationDimension.False;
        }
        if (geometry.GeometryType == GeometryType.Polygon)
        {
            return RelationDimension.Curve;
        }
        Coordinate[] coordinates = GetCurveCoordinates(geometry);
        return CoordinatesAreClosed(coordinates) ?
            RelationDimension.False : RelationDimension.Point;
    }

    internal static bool CoordinatesAreClosed(Coordinate[] coordinates)
    {
        return coordinates.Length > 0 &&
            RobustLineIntersection.CoordinatesEqualPlanar(
                coordinates[0], coordinates[coordinates.Length - 1]);
    }

    internal static bool HasCurveCell(
        RelationMatrix matrix, TopologicalLocation row,
        TopologicalLocation column)
    {
        return RelationMatrixAlgorithms.GetCell(matrix, row, column) ==
            RelationDimension.Curve;
    }

    internal static RelationDimension ToRelationDimension(
        TopologicalDimension dimension)
    {
        switch (dimension)
        {
            case TopologicalDimension.Point:
                return RelationDimension.Point;
            case TopologicalDimension.Curve:
                return RelationDimension.Curve;
            case TopologicalDimension.Surface:
                return RelationDimension.Surface;
            case TopologicalDimension.Empty:
                return RelationDimension.False;
            default:
                throw new InvalidGeometryException(
                    "Topological dimension is not supported by a relation matrix.");
        }
    }

    internal static void ValidateAtomicGeometry(Geometry geometry)
    {
        GeometryInspection.EnsureGeometryGraph(geometry);
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
                GeometryInspection.ExtractValidatedCoordinates(geometry);
                return;
            case GeometryType.LineString:
                GeometryInspection.ExtractValidatedCoordinates(geometry);
                Coordinate[] lineCoordinates = GetCurveCoordinates(geometry);
                if (lineCoordinates.Length == 1)
                {
                    throw new InvalidGeometryException(
                        "A LineString must be empty or contain at least two coordinates.");
                }
                return;
            case GeometryType.LinearRing:
                GeometryInspection.ExtractValidatedCoordinates(geometry);
                GeometryBuilder.ValidateLinearRingCoordinates(
                    GetCurveCoordinates(geometry));
                return;
            case GeometryType.Polygon:
                GeometryInspection.ExtractValidatedCoordinates(geometry);
                Polygon polygon = GetPolygon(geometry);
                GeometryBuilder.ValidateLinearRingCoordinates(
                    polygon.Shell.Coordinates);
                if (polygon.Shell.Coordinates.Length == 0 &&
                    polygon.Holes.Length != 0)
                {
                    throw new InvalidGeometryException(
                        "An empty polygon shell cannot contain holes.");
                }
                int holeIndex = 0;
                while (holeIndex < polygon.Holes.Length)
                {
                    LinearRing hole = polygon.Holes[holeIndex];
                    if (hole == null)
                    {
                        throw new InvalidGeometryException("Polygon hole is null.");
                    }
                    GeometryBuilder.ValidateLinearRingCoordinates(hole.Coordinates);
                    if (hole.Coordinates.Length == 0)
                    {
                        throw new InvalidGeometryException(
                            "A polygon hole cannot be empty.");
                    }
                    holeIndex++;
                }
                return;
            default:
                throw new InvalidGeometryException(
                    "Relation graph labeling currently supports atomic Point, LineString, LinearRing, and Polygon values.");
        }
    }
}
