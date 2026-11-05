using System.Collections.Generic;

namespace geo;

internal static class PreparedSnapshotBuilder
{
    internal static Geometry CreateValidatedSnapshot(Geometry geometry)
    {
        ValidationResult validation =
            GeometryValidityChecker.ValidateGeometry(geometry);
        if (!validation.IsValid)
        {
            throw new InvalidGeometryException(
                "Prepared geometry is invalid: " + validation.Message);
        }
        if (!CollectionRelationGraphBuilder.SupportsCoreGeometry(geometry))
        {
            throw new InvalidGeometryException(
                "Prepared geometry supports only core linear geometry types.");
        }
        return GeometryCopy.CopyGeometry(geometry);
    }

    internal static Geometry[] GetPreparedLeaves(Geometry geometry)
    {
        return ComponentWalker.FlattenLeafGeometries(
            geometry,
            GeometryConstants.DefaultMaximumNestingDepth,
            GeometryConstants.DefaultMaximumComponents);
    }

    internal static void ValidatePointSnapshot(Geometry geometry)
    {
        ValidateSnapshotDimensionValue(
            geometry, TopologicalDimension.Point, "point");
    }

    internal static void ValidateLineSnapshot(Geometry geometry)
    {
        ValidateSnapshotDimensionValue(
            geometry, TopologicalDimension.Curve, "line");
    }

    internal static void ValidatePolygonSnapshot(Geometry geometry)
    {
        ValidateSnapshotDimensionValue(
            geometry, TopologicalDimension.Surface, "polygon");
    }

    internal static void ValidateSnapshotDimensionValue(
        Geometry geometry, TopologicalDimension expected, string description)
    {
        Geometry[] leaves = GetPreparedLeaves(geometry);
        int index = 0;
        while (index < leaves.Length)
        {
            Geometry leaf = leaves[index];
            if (!IsPreparedLeafAllowed(leaf, expected))
            {
                throw new InvalidGeometryException(
                    "Prepared " + description +
                    " data contains another topological dimension.");
            }
            index++;
        }
    }

    internal static bool IsPreparedLeafAllowed(
        Geometry geometry, TopologicalDimension expected)
    {
        if (geometry.GeometryType == GeometryType.GeometryCollection)
        {
            return GeometryInspection.IsEmptyValue(geometry);
        }
        switch (expected)
        {
            case TopologicalDimension.Point:
                return geometry.GeometryType == GeometryType.Point ||
                    geometry.GeometryType == GeometryType.MultiPoint;
            case TopologicalDimension.Curve:
                return geometry.GeometryType == GeometryType.LineString ||
                    geometry.GeometryType == GeometryType.LinearRing ||
                    geometry.GeometryType == GeometryType.MultiLineString;
            case TopologicalDimension.Surface:
                return geometry.GeometryType == GeometryType.Polygon ||
                    geometry.GeometryType == GeometryType.MultiPolygon;
            default:
                return false;
        }
    }

    internal static Coordinate[] CreateRepresentativePointValues(
        Geometry geometry, TopologicalDimension dimension)
    {
        Geometry[] leaves = GetPreparedLeaves(geometry);
        List<Coordinate> representatives = new List<Coordinate>();
        int index = 0;
        while (index < leaves.Length)
        {
            Geometry leaf = leaves[index];
            if (!GeometryInspection.IsEmptyValue(leaf) &&
                GeometryInspection.GetTopologicalDimensionValue(leaf) ==
                    dimension)
            {
                Coordinate? representative =
                    RepresentativePointAlgorithm.GetPointOnSurface(leaf);
                if (representative != null)
                {
                    representatives.Add(representative);
                }
            }
            index++;
        }
        return representatives.ToArray();
    }

    internal static Coordinate[] CombineRepresentativePointValues(
        PreparedPointData points, PreparedLineData lines,
        PreparedPolygonData polygons)
    {
        int length = points.RepresentativePoints.Length +
            lines.RepresentativePoints.Length +
            polygons.RepresentativePoints.Length;
        Coordinate[] combined = new Coordinate[length];
        int destination = 0;
        destination = CopyRepresentativePointValues(
            points.RepresentativePoints, combined, destination);
        destination = CopyRepresentativePointValues(
            lines.RepresentativePoints, combined, destination);
        CopyRepresentativePointValues(
            polygons.RepresentativePoints, combined, destination);
        return combined;
    }

    internal static int CopyRepresentativePointValues(
        Coordinate[] source, Coordinate[] destination, int destinationIndex)
    {
        int index = 0;
        while (index < source.Length)
        {
            destination[destinationIndex] =
                CoordinateSequences.CopyCoordinateValue(source[index]);
            destinationIndex++;
            index++;
        }
        return destinationIndex;
    }
}

internal class PreparedCollectionData
{
    internal Geometry GeometrySnapshot;
    internal Envelope Bounds;
    internal DistanceGeometryParts Parts;
    internal SegmentSpatialIndex SegmentIndex;
    internal PreparedPointData PointData;
    internal PreparedLineData LineData;
    internal PreparedPolygonData PolygonData;
    internal Coordinate[] RepresentativePoints;
    internal PreparedPointInAreaLocator PointLocator;

    internal PreparedCollectionData(Geometry geometry)
    {
        GeometrySnapshot = PreparedSnapshotBuilder.CreateValidatedSnapshot(
            geometry);
        Bounds = EnvelopeAlgorithms.CreateGeometryEnvelope(GeometrySnapshot);
        Parts = new DistanceGeometryParts(GeometrySnapshot);
        SegmentIndex = new SegmentSpatialIndex(Parts.Segments.ToArray());

        Geometry points = GeometryCollections.ExtractTopologicalDimensionValue(
            GeometrySnapshot, TopologicalDimension.Point);
        Geometry lines = GeometryCollections.ExtractTopologicalDimensionValue(
            GeometrySnapshot, TopologicalDimension.Curve);
        Geometry polygons =
            GeometryCollections.ExtractTopologicalDimensionValue(
                GeometrySnapshot, TopologicalDimension.Surface);
        PointData = new PreparedPointData(points);
        LineData = new PreparedLineData(lines);
        PolygonData = new PreparedPolygonData(polygons);
        RepresentativePoints =
            PreparedSnapshotBuilder.CombineRepresentativePointValues(
                PointData, LineData, PolygonData);
        PointLocator = PolygonData.PointLocator;
    }
}
