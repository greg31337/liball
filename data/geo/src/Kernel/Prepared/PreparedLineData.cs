namespace geo;

internal class PreparedLineData
{
    internal Geometry GeometrySnapshot;
    internal Envelope Bounds;
    internal DistanceGeometryParts Parts;
    internal SegmentSpatialIndex SegmentIndex;
    internal Coordinate[] RepresentativePoints;

    internal PreparedLineData(Geometry geometry)
    {
        GeometrySnapshot = PreparedSnapshotBuilder.CreateValidatedSnapshot(
            geometry);
        PreparedSnapshotBuilder.ValidateLineSnapshot(GeometrySnapshot);
        Bounds = EnvelopeAlgorithms.CreateGeometryEnvelope(GeometrySnapshot);
        Parts = new DistanceGeometryParts(GeometrySnapshot);
        SegmentIndex = new SegmentSpatialIndex(Parts.Segments.ToArray());
        RepresentativePoints =
            PreparedSnapshotBuilder.CreateRepresentativePointValues(
                GeometrySnapshot, TopologicalDimension.Curve);
    }
}
