using System.Collections.Generic;

namespace geo;

internal static class OverlayPrecisionReducer
{
    internal static Geometry ReduceInput(
        RelationGeometryParts parts, PrecisionModel precisionModel)
    {
        SnapRoundingNoder.ValidateFixedPrecision(precisionModel);
        List<Geometry> components = new List<Geometry>();
        AddReducedPoints(components, parts, precisionModel);
        AddReducedCurves(components, parts, precisionModel);
        AddReducedSurfaces(components, parts, precisionModel);
        return GeometryCollections.CreateRawCollection(
            components.ToArray(), parts.Root.CoordinateLayout,
            parts.Root.SpatialReferenceId);
    }

    internal static void AddReducedPoints(
        List<Geometry> components, RelationGeometryParts parts,
        PrecisionModel precisionModel)
    {
        int index = 0;
        while (index < parts.PointGeometries.Length)
        {
            Point point = (Point)GeometryDispatch.GetSelectedPayload(
                parts.PointGeometries[index]);
            if (point.Coordinate != null)
            {
                Geometry geometry = CreateGeometry(
                    GeometryType.Point, parts.Root);
                Point reduced = new Point();
                reduced.Coordinate = SnapRoundingNoder.RoundCoordinate(
                    point.Coordinate, precisionModel);
                geometry.Point = reduced;
                components.Add(geometry);
            }
            index++;
        }
    }

    internal static void AddReducedCurves(
        List<Geometry> components, RelationGeometryParts parts,
        PrecisionModel precisionModel)
    {
        int index = 0;
        while (index < parts.CurveGeometries.Length)
        {
            Coordinate[] coordinates = RoundSequence(
                RelationGraphBuilder.GetCurveCoordinates(
                    parts.CurveGeometries[index]), precisionModel);
            if (coordinates.Length >= 2)
            {
                Geometry geometry = CreateGeometry(
                    GeometryType.LineString, parts.Root);
                LineString line = new LineString();
                line.Coordinates = coordinates;
                geometry.LineString = line;
                components.Add(geometry);
            }
            index++;
        }
    }

    internal static void AddReducedSurfaces(
        List<Geometry> components, RelationGeometryParts parts,
        PrecisionModel precisionModel)
    {
        int surfaceIndex = 0;
        while (surfaceIndex < parts.SurfaceGeometries.Length)
        {
            Polygon source = RelationGraphBuilder.GetPolygon(
                parts.SurfaceGeometries[surfaceIndex]);
            Coordinate[] shellCoordinates = RoundSequence(
                source.Shell.Coordinates, precisionModel);
            if (IsUsableAreaRing(shellCoordinates))
            {
                List<LinearRing> holes = new List<LinearRing>();
                int holeIndex = 0;
                while (holeIndex < source.Holes.Length)
                {
                    Coordinate[] holeCoordinates = RoundSequence(
                        source.Holes[holeIndex].Coordinates,
                        precisionModel);
                    if (IsUsableAreaRing(holeCoordinates))
                    {
                        LinearRing hole = new LinearRing();
                        hole.Coordinates = holeCoordinates;
                        holes.Add(hole);
                    }
                    holeIndex++;
                }
                Geometry geometry = CreateGeometry(
                    GeometryType.Polygon, parts.Root);
                Polygon polygon = new Polygon();
                polygon.Shell.Coordinates = shellCoordinates;
                polygon.Holes = holes.ToArray();
                geometry.Polygon = polygon;
                components.Add(geometry);
            }
            surfaceIndex++;
        }
    }

    internal static Coordinate[] RoundSequence(
        Coordinate[] coordinates, PrecisionModel precisionModel)
    {
        List<Coordinate> rounded = new List<Coordinate>();
        int index = 0;
        while (index < coordinates.Length)
        {
            Coordinate coordinate = SnapRoundingNoder.RoundCoordinate(
                coordinates[index], precisionModel);
            if (rounded.Count == 0 ||
                !RobustLineIntersection.CoordinatesEqualPlanar(
                    rounded[rounded.Count - 1], coordinate))
            {
                rounded.Add(coordinate);
            }
            else
            {
                OrdinateReconciler.ReconcileCoordinateInto(
                    rounded[rounded.Count - 1], coordinate);
            }
            index++;
        }
        return rounded.ToArray();
    }

    internal static bool IsUsableAreaRing(Coordinate[] coordinates)
    {
        if (coordinates.Length < 4) return false;
        if (!RobustLineIntersection.CoordinatesEqualPlanar(
            coordinates[0], coordinates[coordinates.Length - 1]))
        {
            return false;
        }
        return RingMath.GetRingOrientation(coordinates) !=
            RingOrientation.Undefined;
    }

    internal static Geometry CreateGeometry(
        GeometryType geometryType, Geometry metadataSource)
    {
        Geometry geometry = new Geometry();
        geometry.Point = null;
        geometry.GeometryType = geometryType;
        geometry.CoordinateLayout = metadataSource.CoordinateLayout;
        geometry.SpatialReferenceId = metadataSource.SpatialReferenceId;
        return geometry;
    }
}
