using System.Collections.Generic;

namespace geo;

internal class PreparedPointData
{
    internal Geometry GeometrySnapshot;
    internal Envelope Bounds;
    internal Coordinate[] Coordinates;
    internal Coordinate[] RepresentativePoints;

    internal PreparedPointData(Geometry geometry)
    {
        GeometrySnapshot = PreparedSnapshotBuilder.CreateValidatedSnapshot(
            geometry);
        PreparedSnapshotBuilder.ValidatePointSnapshot(GeometrySnapshot);
        Bounds = EnvelopeAlgorithms.CreateGeometryEnvelope(GeometrySnapshot);
        Coordinates = CreatePointCoordinates(GeometrySnapshot);
        RepresentativePoints =
            PreparedSnapshotBuilder.CreateRepresentativePointValues(
                GeometrySnapshot, TopologicalDimension.Point);
    }

    internal static Coordinate[] CreatePointCoordinates(Geometry geometry)
    {
        Geometry[] leaves = PreparedSnapshotBuilder.GetPreparedLeaves(
            geometry);
        List<Coordinate> coordinates = new List<Coordinate>();
        int index = 0;
        while (index < leaves.Length)
        {
            Geometry leaf = leaves[index];
            if (leaf.GeometryType == GeometryType.Point)
            {
                Point point = (Point)
                    GeometryDispatch.GetSelectedPayload(leaf);
                if (point.Coordinate != null)
                {
                    coordinates.Add(
                        CoordinateSequences.CopyCoordinateValue(
                            point.Coordinate));
                }
            }
            index++;
        }
        return coordinates.ToArray();
    }
}
