namespace geo;

internal static class GeometryPointLocation
{
    internal static TopologicalLocation LocatePoint(
        Coordinate point, Geometry geometry,
        CurveLinearizationParameters curveParameters)
    {
        VectorMath.ValidateCoordinateValue(point, "Point being located");
        CurveLinearizer.ValidateParameters(curveParameters);
        GeometryCycleGuard.EnsureAcyclic(
            geometry, GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
        return LocatePointInGeometryValue(point, geometry, curveParameters);
    }

    internal static TopologicalLocation LocatePointInGeometryValue(
        Coordinate point, Geometry geometry,
        CurveLinearizationParameters curveParameters)
    {
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
                return LocatePointPayload(point, (Point)payload);
            case GeometryType.LineString:
                return LocatePointOnCoordinateLine(
                    point, ((LineString)payload).Coordinates, false);
            case GeometryType.LinearRing:
                return LocatePointOnCoordinateLine(
                    point, ((LinearRing)payload).Coordinates, true);
            case GeometryType.Polygon:
                return LocatePointInPolygon(point, (Polygon)payload);
            case GeometryType.MultiPoint:
                return LocatePointInMultiPoint(point, (MultiPoint)payload);
            case GeometryType.MultiLineString:
                return LocatePointInMultiLine(point, (MultiLineString)payload);
            case GeometryType.MultiPolygon:
                return LocatePointInMultiPolygon(point, (MultiPolygon)payload);
            case GeometryType.GeometryCollection:
                return LocatePointInGeometryArray(
                    point, ((GeometryCollection)payload).Geometries, curveParameters);
            case GeometryType.Triangle:
                return LocatePointInTriangle(point, (Triangle)payload);
            case GeometryType.Tin:
                return LocatePointInTin(point, (Tin)payload);
            case GeometryType.PolyhedralSurface:
                return LocatePointInPolyhedralSurface(
                    point, (PolyhedralSurface)payload);
            case GeometryType.CircularString:
            case GeometryType.CompoundCurve:
                return LocatePointOnLinearizedCurve(
                    point, geometry, curveParameters);
            case GeometryType.CurvePolygon:
                return LocatePointInCurvePolygon(
                    point, (CurvePolygon)payload, curveParameters);
            case GeometryType.MultiCurve:
                return LocatePointInMultiCurve(
                    point, (MultiCurve)payload, curveParameters);
            case GeometryType.MultiSurface:
                return LocatePointInGeometryArray(
                    point, ((MultiSurface)payload).Surfaces, curveParameters);
            default:
                throw new InvalidGeometryException("Geometry type is not supported for point location.");
        }
    }

    internal static TopologicalLocation LocatePointPayload(Coordinate point, Point payload)
    {
        if (payload == null) throw new InvalidGeometryException("Point payload is null.");
        if (payload.Coordinate == null) return TopologicalLocation.Exterior;
        VectorMath.ValidateCoordinateValue(payload.Coordinate, "Point geometry coordinate");
        return RobustLineIntersection.CoordinatesEqualPlanar(point, payload.Coordinate) ?
            TopologicalLocation.Interior : TopologicalLocation.Exterior;
    }

    internal static TopologicalLocation LocatePointOnCoordinateLine(
        Coordinate point, Coordinate[] coordinates, bool forceClosed)
    {
        RingMath.ValidateRingCoordinates(coordinates);
        if (coordinates.Length == 0) return TopologicalLocation.Exterior;
        if (coordinates.Length == 1)
        {
            return RobustLineIntersection.CoordinatesEqualPlanar(point, coordinates[0]) ?
                TopologicalLocation.Interior : TopologicalLocation.Exterior;
        }
        int index = 0;
        while (index + 1 < coordinates.Length)
        {
            if (SegmentDistanceAlgorithms.IsPointOnSegment(
                point, coordinates[index], coordinates[index + 1]))
            {
                bool closed = forceClosed || RobustLineIntersection.CoordinatesEqualPlanar(
                    coordinates[0], coordinates[coordinates.Length - 1]);
                if (!closed && (RobustLineIntersection.CoordinatesEqualPlanar(
                    point, coordinates[0]) || RobustLineIntersection.CoordinatesEqualPlanar(
                    point, coordinates[coordinates.Length - 1])))
                {
                    return TopologicalLocation.Boundary;
                }
                return TopologicalLocation.Interior;
            }
            index++;
        }
        if (forceClosed && SegmentDistanceAlgorithms.IsPointOnSegment(
            point, coordinates[coordinates.Length - 1], coordinates[0]))
        {
            return TopologicalLocation.Interior;
        }
        return TopologicalLocation.Exterior;
    }

    internal static TopologicalLocation LocatePointInPolygon(
        Coordinate point, Polygon polygon)
    {
        if (polygon == null || polygon.Shell == null || polygon.Holes == null)
        {
            throw new InvalidGeometryException("Polygon payload, shell, or hole array is null.");
        }
        TopologicalLocation shell = RingPointLocation.LocatePointByRayCrossing(
            point, polygon.Shell.Coordinates);
        if (shell != TopologicalLocation.Interior) return shell;
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            LinearRing hole = polygon.Holes[index];
            if (hole == null) throw new InvalidGeometryException("Polygon hole is null.");
            TopologicalLocation location = RingPointLocation.LocatePointByRayCrossing(
                point, hole.Coordinates);
            if (location == TopologicalLocation.Boundary) return TopologicalLocation.Boundary;
            if (location == TopologicalLocation.Interior) return TopologicalLocation.Exterior;
            index++;
        }
        return TopologicalLocation.Interior;
    }

    internal static TopologicalLocation LocatePointInMultiPoint(
        Coordinate point, MultiPoint multiPoint)
    {
        if (multiPoint == null || multiPoint.Points == null)
        {
            throw new InvalidGeometryException("MultiPoint payload or component array is null.");
        }
        int index = 0;
        while (index < multiPoint.Points.Length)
        {
            if (LocatePointPayload(point, multiPoint.Points[index]) ==
                TopologicalLocation.Interior) return TopologicalLocation.Interior;
            index++;
        }
        return TopologicalLocation.Exterior;
    }

    internal static TopologicalLocation LocatePointInMultiLine(
        Coordinate point, MultiLineString multiLine)
    {
        if (multiLine == null || multiLine.LineStrings == null)
        {
            throw new InvalidGeometryException("MultiLineString payload or component array is null.");
        }
        int boundaryCount = 0;
        int index = 0;
        while (index < multiLine.LineStrings.Length)
        {
            LineString line = multiLine.LineStrings[index];
            if (line == null) throw new InvalidGeometryException("MultiLineString component is null.");
            TopologicalLocation location = LocatePointOnCoordinateLine(
                point, line.Coordinates, false);
            if (location == TopologicalLocation.Interior) return TopologicalLocation.Interior;
            if (location == TopologicalLocation.Boundary) boundaryCount++;
            index++;
        }
        if (boundaryCount == 0) return TopologicalLocation.Exterior;
        return boundaryCount % 2 == 0 ?
            TopologicalLocation.Interior : TopologicalLocation.Boundary;
    }

    internal static TopologicalLocation LocatePointInMultiPolygon(
        Coordinate point, MultiPolygon multiPolygon)
    {
        if (multiPolygon == null || multiPolygon.Polygons == null)
        {
            throw new InvalidGeometryException("MultiPolygon payload or component array is null.");
        }
        TopologicalLocation result = TopologicalLocation.Exterior;
        int index = 0;
        while (index < multiPolygon.Polygons.Length)
        {
            TopologicalLocation location = LocatePointInPolygon(
                point, multiPolygon.Polygons[index]);
            if (location == TopologicalLocation.Interior) return location;
            if (location == TopologicalLocation.Boundary) result = location;
            index++;
        }
        return result;
    }

    internal static TopologicalLocation LocatePointInTriangle(
        Coordinate point, Triangle triangle)
    {
        if (triangle == null || triangle.Shell == null)
        {
            throw new InvalidGeometryException("Triangle payload or shell is null.");
        }
        return RingPointLocation.LocatePointByRayCrossing(
            point, triangle.Shell.Coordinates);
    }

    internal static TopologicalLocation LocatePointInTin(Coordinate point, Tin tin)
    {
        if (tin == null || tin.Triangles == null)
        {
            throw new InvalidGeometryException("TIN payload or patch array is null.");
        }
        TopologicalLocation result = TopologicalLocation.Exterior;
        int index = 0;
        while (index < tin.Triangles.Length)
        {
            TopologicalLocation location = LocatePointInTriangle(point, tin.Triangles[index]);
            if (location == TopologicalLocation.Interior) return location;
            if (location == TopologicalLocation.Boundary) result = location;
            index++;
        }
        return result;
    }

    internal static TopologicalLocation LocatePointInPolyhedralSurface(
        Coordinate point, PolyhedralSurface surface)
    {
        if (surface == null || surface.Patches == null)
        {
            throw new InvalidGeometryException("PolyhedralSurface payload or patch array is null.");
        }
        TopologicalLocation result = TopologicalLocation.Exterior;
        int index = 0;
        while (index < surface.Patches.Length)
        {
            TopologicalLocation location = LocatePointInPolygon(point, surface.Patches[index]);
            if (location == TopologicalLocation.Interior) return location;
            if (location == TopologicalLocation.Boundary) result = location;
            index++;
        }
        return result;
    }

    internal static TopologicalLocation LocatePointOnLinearizedCurve(
        Coordinate point, Geometry curve, CurveLinearizationParameters parameters)
    {
        Coordinate[] coordinates = CurveLinearizer.LinearizeCurveGeometry(curve, parameters);
        bool closed = coordinates.Length > 1 &&
            RobustLineIntersection.CoordinatesEqualPlanar(
                coordinates[0], coordinates[coordinates.Length - 1]);
        return LocatePointOnCoordinateLine(point, coordinates, closed);
    }

    internal static TopologicalLocation LocatePointInCurvePolygon(
        Coordinate point, CurvePolygon polygon,
        CurveLinearizationParameters parameters)
    {
        if (polygon == null || polygon.Holes == null)
        {
            throw new InvalidGeometryException("CurvePolygon payload or hole array is null.");
        }
        if (polygon.Shell == null) return TopologicalLocation.Exterior;
        Coordinate[] shell = CurveLinearizer.LinearizeCurveGeometry(
            polygon.Shell, parameters);
        TopologicalLocation shellLocation = RingPointLocation.LocatePointByRayCrossing(
            point, shell);
        if (shellLocation != TopologicalLocation.Interior) return shellLocation;
        int index = 0;
        while (index < polygon.Holes.Length)
        {
            Coordinate[] hole = CurveLinearizer.LinearizeCurveGeometry(
                polygon.Holes[index], parameters);
            TopologicalLocation location = RingPointLocation.LocatePointByRayCrossing(
                point, hole);
            if (location == TopologicalLocation.Boundary) return location;
            if (location == TopologicalLocation.Interior) return TopologicalLocation.Exterior;
            index++;
        }
        return TopologicalLocation.Interior;
    }

    internal static TopologicalLocation LocatePointInMultiCurve(
        Coordinate point, MultiCurve multiCurve,
        CurveLinearizationParameters parameters)
    {
        if (multiCurve == null || multiCurve.Curves == null)
        {
            throw new InvalidGeometryException("MultiCurve payload or component array is null.");
        }
        int boundaryCount = 0;
        int index = 0;
        while (index < multiCurve.Curves.Length)
        {
            TopologicalLocation location = LocatePointOnLinearizedCurve(
                point, multiCurve.Curves[index], parameters);
            if (location == TopologicalLocation.Interior) return location;
            if (location == TopologicalLocation.Boundary) boundaryCount++;
            index++;
        }
        if (boundaryCount == 0) return TopologicalLocation.Exterior;
        return boundaryCount % 2 == 0 ?
            TopologicalLocation.Interior : TopologicalLocation.Boundary;
    }

    internal static TopologicalLocation LocatePointInGeometryArray(
        Coordinate point, Geometry[] geometries,
        CurveLinearizationParameters parameters)
    {
        if (geometries == null)
        {
            throw new InvalidGeometryException("Geometry component array is null.");
        }
        TopologicalLocation result = TopologicalLocation.Exterior;
        int index = 0;
        while (index < geometries.Length)
        {
            TopologicalLocation location = LocatePointInGeometryValue(
                point, geometries[index], parameters);
            if (location == TopologicalLocation.Interior) return location;
            if (location == TopologicalLocation.Boundary) result = location;
            index++;
        }
        return result;
    }
}
