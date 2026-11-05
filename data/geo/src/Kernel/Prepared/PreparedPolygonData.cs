using System.Collections.Generic;

namespace geo;

internal class PreparedAreaComponent
{
    internal Polygon Polygon;
    internal Envelope Bounds;

    internal PreparedAreaComponent(Polygon polygon, Envelope bounds)
    {
        Polygon = polygon;
        Bounds = bounds;
    }
}

internal class PreparedPointInAreaLocator
{
    internal PreparedAreaComponent[] Components;

    internal PreparedPointInAreaLocator(Geometry geometry)
    {
        Geometry[] leaves = PreparedSnapshotBuilder.GetPreparedLeaves(
            geometry);
        List<PreparedAreaComponent> components =
            new List<PreparedAreaComponent>();
        int index = 0;
        while (index < leaves.Length)
        {
            Geometry leaf = leaves[index];
            if (leaf.GeometryType == GeometryType.Polygon &&
                !GeometryInspection.IsEmptyValue(leaf))
            {
                Polygon polygon = (Polygon)
                    GeometryDispatch.GetSelectedPayload(leaf);
                components.Add(new PreparedAreaComponent(
                    polygon,
                    EnvelopeAlgorithms.CreateGeometryEnvelope(leaf)));
            }
            index++;
        }
        Components = components.ToArray();
    }

    internal TopologicalLocation Locate(Coordinate coordinate)
    {
        VectorMath.ValidateCoordinateValue(
            coordinate, "Prepared area location coordinate");
        TopologicalLocation result = TopologicalLocation.Exterior;
        int index = 0;
        while (index < Components.Length)
        {
            PreparedAreaComponent component = Components[index];
            if (EnvelopeAlgorithms.CoversEnvelopeCoordinate(
                component.Bounds, coordinate))
            {
                TopologicalLocation location =
                    GeometryPointLocation.LocatePointInPolygon(
                        coordinate, component.Polygon);
                if (location == TopologicalLocation.Interior)
                {
                    return TopologicalLocation.Interior;
                }
                if (location == TopologicalLocation.Boundary)
                {
                    result = TopologicalLocation.Boundary;
                }
            }
            index++;
        }
        return result;
    }
}

internal class PreparedPolygonData
{
    internal Geometry GeometrySnapshot;
    internal Envelope Bounds;
    internal DistanceGeometryParts Parts;
    internal SegmentSpatialIndex SegmentIndex;
    internal Coordinate[] RepresentativePoints;
    internal PreparedPointInAreaLocator PointLocator;

    internal PreparedPolygonData(Geometry geometry)
    {
        GeometrySnapshot = PreparedSnapshotBuilder.CreateValidatedSnapshot(
            geometry);
        PreparedSnapshotBuilder.ValidatePolygonSnapshot(GeometrySnapshot);
        Bounds = EnvelopeAlgorithms.CreateGeometryEnvelope(GeometrySnapshot);
        Parts = new DistanceGeometryParts(GeometrySnapshot);
        SegmentIndex = new SegmentSpatialIndex(Parts.Segments.ToArray());
        RepresentativePoints =
            PreparedSnapshotBuilder.CreateRepresentativePointValues(
                GeometrySnapshot, TopologicalDimension.Surface);
        PointLocator = new PreparedPointInAreaLocator(GeometrySnapshot);
    }
}
