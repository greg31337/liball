using System.Collections.Generic;

namespace geo;

internal class PrecisionReductionResult
{
    internal Geometry Geometry = new Geometry();
    internal int CollapsedComponentCount;
    internal string[] CollapsedComponentPaths = [];
}

internal static class TopologyPreservingPrecisionReducer
{
    internal static PrecisionReductionResult ReduceGeometry(
        Geometry geometry, PrecisionModel precisionModel)
    {
        SnapRoundingNoder.ValidateFixedPrecision(precisionModel);
        if (!CollectionRelationGraphBuilder.SupportsCoreGeometry(geometry))
        {
            throw new InvalidGeometryException(
                "Precision reduction supports core geometry types only.");
        }
        ValidationResult inputValidation =
            GeometryValidityChecker.ValidateGeometry(geometry);
        if (!inputValidation.IsValid)
        {
            throw new InvalidGeometryException(
                "Precision reduction requires a valid input geometry: " +
                inputValidation.Message);
        }

        List<string> collapsedPaths = new List<string>();
        CollectCollapsedGeometryPaths(
            geometry, geometry.GeometryType.ToString(),
            precisionModel, collapsedPaths);
        Geometry empty = GeometryCollections.CreateRawCollection(
            [], geometry.CoordinateLayout, geometry.SpatialReferenceId);
        Geometry reduced = OverlayEngine.GetUnionWithPrecision(
            geometry, empty, precisionModel);
        ValidationResult outputValidation =
            GeometryValidityChecker.ValidateGeometry(reduced);
        if (!outputValidation.IsValid)
        {
            throw new PrecisionException(
                "Precision reduction did not produce a valid geometry: " +
                outputValidation.Message);
        }

        PrecisionReductionResult result = new PrecisionReductionResult();
        result.Geometry = reduced;
        result.CollapsedComponentCount = collapsedPaths.Count;
        result.CollapsedComponentPaths = collapsedPaths.ToArray();
        return result;
    }

    internal static void CollectCollapsedGeometryPaths(
        Geometry geometry, string path, PrecisionModel precisionModel,
        List<string> collapsedPaths)
    {
        object payload = GeometryDispatch.GetSelectedPayload(geometry);
        switch (geometry.GeometryType)
        {
            case GeometryType.Point:
            case GeometryType.MultiPoint:
                return;
            case GeometryType.LineString:
                LineString line = (LineString)payload;
                AddCollapsedCurvePath(
                    line.Coordinates, path, precisionModel, collapsedPaths);
                return;
            case GeometryType.LinearRing:
                LinearRing ring = (LinearRing)payload;
                AddCollapsedCurvePath(
                    ring.Coordinates, path, precisionModel, collapsedPaths);
                return;
            case GeometryType.Polygon:
                CollectCollapsedPolygonPaths(
                    (Polygon)payload, path, precisionModel, collapsedPaths);
                return;
            case GeometryType.MultiLineString:
                CollectCollapsedMultiLinePaths(
                    (MultiLineString)payload, path,
                    precisionModel, collapsedPaths);
                return;
            case GeometryType.MultiPolygon:
                CollectCollapsedMultiPolygonPaths(
                    (MultiPolygon)payload, path,
                    precisionModel, collapsedPaths);
                return;
            case GeometryType.GeometryCollection:
                CollectCollapsedCollectionPaths(
                    (GeometryCollection)payload, path,
                    precisionModel, collapsedPaths);
                return;
            default:
                throw new InvalidGeometryException(
                    "Precision reduction supports core geometry types only.");
        }
    }

    internal static void AddCollapsedCurvePath(
        Coordinate[] coordinates, string path,
        PrecisionModel precisionModel, List<string> collapsedPaths)
    {
        if (coordinates.Length == 0) return;
        Coordinate[] rounded = OverlayPrecisionReducer.RoundSequence(
            coordinates, precisionModel);
        if (rounded.Length < 2) collapsedPaths.Add(path);
    }

    internal static void CollectCollapsedPolygonPaths(
        Polygon polygon, string path, PrecisionModel precisionModel,
        List<string> collapsedPaths)
    {
        if (polygon.Shell.Coordinates.Length == 0) return;
        Coordinate[] shell = OverlayPrecisionReducer.RoundSequence(
            polygon.Shell.Coordinates, precisionModel);
        if (!OverlayPrecisionReducer.IsUsableAreaRing(shell))
        {
            collapsedPaths.Add(path + "/Shell");
            return;
        }
        int holeIndex = 0;
        while (holeIndex < polygon.Holes.Length)
        {
            Coordinate[] hole = OverlayPrecisionReducer.RoundSequence(
                polygon.Holes[holeIndex].Coordinates, precisionModel);
            if (!OverlayPrecisionReducer.IsUsableAreaRing(hole))
            {
                collapsedPaths.Add(
                    path + "/Holes[" + holeIndex + "]");
            }
            holeIndex++;
        }
    }

    internal static void CollectCollapsedMultiLinePaths(
        MultiLineString multiLineString, string path,
        PrecisionModel precisionModel, List<string> collapsedPaths)
    {
        int index = 0;
        while (index < multiLineString.LineStrings.Length)
        {
            AddCollapsedCurvePath(
                multiLineString.LineStrings[index].Coordinates,
                path + "/LineStrings[" + index + "]",
                precisionModel, collapsedPaths);
            index++;
        }
    }

    internal static void CollectCollapsedMultiPolygonPaths(
        MultiPolygon multiPolygon, string path,
        PrecisionModel precisionModel, List<string> collapsedPaths)
    {
        int index = 0;
        while (index < multiPolygon.Polygons.Length)
        {
            CollectCollapsedPolygonPaths(
                multiPolygon.Polygons[index],
                path + "/Polygons[" + index + "]",
                precisionModel, collapsedPaths);
            index++;
        }
    }

    internal static void CollectCollapsedCollectionPaths(
        GeometryCollection collection, string path,
        PrecisionModel precisionModel, List<string> collapsedPaths)
    {
        int index = 0;
        while (index < collection.Geometries.Length)
        {
            Geometry child = collection.Geometries[index];
            CollectCollapsedGeometryPaths(
                child, path + "/Geometries[" + index + "]/" +
                child.GeometryType, precisionModel, collapsedPaths);
            index++;
        }
    }
}
