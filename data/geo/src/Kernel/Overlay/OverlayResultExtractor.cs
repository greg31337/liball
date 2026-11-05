using System.Collections.Generic;

namespace geo;

internal static class OverlayResultExtractor
{
    internal static Geometry CreateResult(
        Polygon[] polygons, TopologyEdge[] lines,
        Coordinate[] points, Geometry metadataSource,
        GeometryType emptyType)
    {
        List<Geometry> components = new List<Geometry>();
        int index = 0;
        while (index < polygons.Length)
        {
            components.Add(CreatePolygonGeometry(
                polygons[index], metadataSource));
            index++;
        }
        index = 0;
        LineString[] mergedLines = OverlayLineMerger.MergeEdges(lines);
        while (index < mergedLines.Length)
        {
            components.Add(CreateLineGeometry(
                mergedLines[index].Coordinates, metadataSource));
            index++;
        }
        index = 0;
        while (index < points.Length)
        {
            components.Add(CreatePointGeometry(
                points[index], metadataSource));
            index++;
        }
        return CreateCollectionResult(
            components, metadataSource, emptyType);
    }

    internal static Geometry CreatePolygonGeometry(
        Polygon polygon, Geometry metadataSource)
    {
        Geometry geometry = CreateGeometryShell(
            GeometryType.Polygon, metadataSource);
        geometry.Polygon = polygon;
        return geometry;
    }

    internal static Geometry CreateLineGeometry(
        Coordinate[] coordinates, Geometry metadataSource)
    {
        Geometry geometry = CreateGeometryShell(
            GeometryType.LineString, metadataSource);
        LineString line = new LineString();
        line.Coordinates = CopyCoordinates(coordinates);
        geometry.LineString = line;
        return geometry;
    }

    internal static Geometry CreatePointGeometry(
        Coordinate coordinate, Geometry metadataSource)
    {
        Geometry geometry = CreateGeometryShell(
            GeometryType.Point, metadataSource);
        Point point = new Point();
        point.Coordinate = RobustLineIntersection.CopyCoordinate(coordinate);
        geometry.Point = point;
        return geometry;
    }

    internal static Geometry CreateCollectionResult(
        List<Geometry> components, Geometry metadataSource,
        GeometryType emptyType)
    {
        if (components.Count == 0)
        {
            return GeometryBuilder.CreateEmptyValue(
                emptyType,
                GeometryNormalization.CreateNormalizationOptions(
                    metadataSource));
        }
        if (components.Count == 1) return components[0];
        bool allPolygons = true;
        bool allLines = true;
        bool allPoints = true;
        int index = 0;
        while (index < components.Count)
        {
            if (components[index].GeometryType != GeometryType.Polygon)
            {
                allPolygons = false;
            }
            if (components[index].GeometryType != GeometryType.LineString)
            {
                allLines = false;
            }
            if (components[index].GeometryType != GeometryType.Point)
            {
                allPoints = false;
            }
            index++;
        }
        if (allPolygons)
        {
            Geometry result = CreateGeometryShell(
                GeometryType.MultiPolygon, metadataSource);
            MultiPolygon payload = new MultiPolygon();
            payload.Polygons = new Polygon[components.Count];
            index = 0;
            while (index < components.Count)
            {
                payload.Polygons[index] = (Polygon)
                    GeometryDispatch.GetSelectedPayload(components[index]);
                index++;
            }
            GeometryNormalization.SortPolygonValues(payload.Polygons);
            result.MultiPolygon = payload;
            return result;
        }
        if (allLines)
        {
            Geometry result = CreateGeometryShell(
                GeometryType.MultiLineString, metadataSource);
            MultiLineString payload = new MultiLineString();
            payload.LineStrings = new LineString[components.Count];
            index = 0;
            while (index < components.Count)
            {
                payload.LineStrings[index] = (LineString)
                    GeometryDispatch.GetSelectedPayload(components[index]);
                index++;
            }
            GeometryNormalization.SortLineStringValues(payload.LineStrings);
            result.MultiLineString = payload;
            return result;
        }
        if (allPoints)
        {
            Geometry result = CreateGeometryShell(
                GeometryType.MultiPoint, metadataSource);
            MultiPoint payload = new MultiPoint();
            payload.Points = new Point[components.Count];
            index = 0;
            while (index < components.Count)
            {
                payload.Points[index] = (Point)
                    GeometryDispatch.GetSelectedPayload(components[index]);
                index++;
            }
            GeometryNormalization.SortPointValues(payload.Points);
            result.MultiPoint = payload;
            return result;
        }
        Geometry collection = CreateGeometryShell(
            GeometryType.GeometryCollection, metadataSource);
        GeometryCollection collectionPayload = new GeometryCollection();
        collectionPayload.Geometries = components.ToArray();
        GeometryNormalization.SortGeometryValues(
            collectionPayload.Geometries);
        collection.GeometryCollection = collectionPayload;
        return collection;
    }

    internal static Geometry CreateGeometryShell(
        GeometryType type, Geometry metadataSource)
    {
        Geometry geometry = new Geometry();
        geometry.Point = null;
        geometry.GeometryType = type;
        geometry.CoordinateLayout = metadataSource.CoordinateLayout;
        geometry.SpatialReferenceId = metadataSource.SpatialReferenceId;
        return geometry;
    }

    internal static Coordinate[] CopyCoordinates(Coordinate[] coordinates)
    {
        Coordinate[] copy = new Coordinate[coordinates.Length];
        int index = 0;
        while (index < coordinates.Length)
        {
            copy[index] = RobustLineIntersection.CopyCoordinate(
                coordinates[index]);
            index++;
        }
        return copy;
    }

    internal static bool IsPointCoveredByPolygons(
        Coordinate point, Polygon[] polygons)
    {
        int index = 0;
        while (index < polygons.Length)
        {
            if (GeometryPointLocation.LocatePointInPolygon(
                point, polygons[index]) != TopologicalLocation.Exterior)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static bool IsPointCoveredByLines(
        Coordinate point, TopologyEdge[] lines)
    {
        int lineIndex = 0;
        while (lineIndex < lines.Length)
        {
            Coordinate[] coordinates = lines[lineIndex].Coordinates;
            int segmentIndex = 0;
            while (segmentIndex + 1 < coordinates.Length)
            {
                if (SegmentDistanceAlgorithms.IsPointOnSegment(
                    point, coordinates[segmentIndex],
                    coordinates[segmentIndex + 1])) return true;
                segmentIndex++;
            }
            lineIndex++;
        }
        return false;
    }

    internal static void AddOrReconcileCoordinate(
        List<Coordinate> coordinates, Coordinate candidate)
    {
        int index = 0;
        while (index < coordinates.Count)
        {
            if (RobustLineIntersection.CoordinatesEqualPlanar(
                coordinates[index], candidate))
            {
                OrdinateReconciler.ReconcileCoordinateInto(
                    coordinates[index], candidate);
                return;
            }
            index++;
        }
        coordinates.Add(RobustLineIntersection.CopyCoordinate(candidate));
    }
}
