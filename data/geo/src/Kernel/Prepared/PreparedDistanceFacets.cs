namespace geo;

internal class PreparedDistanceFacets
{
    internal Geometry GeometrySnapshot;
    internal DistanceGeometryParts Parts;
    internal SegmentSpatialIndex SegmentIndex;
    internal Envelope Bounds;

    internal PreparedDistanceFacets(Geometry geometry)
    {
        GeometrySnapshot = GeometryCopy.CopyGeometry(geometry);
        Parts = new DistanceGeometryParts(GeometrySnapshot);
        SegmentIndex = new SegmentSpatialIndex(Parts.Segments.ToArray());
        Bounds = EnvelopeAlgorithms.CreateGeometryEnvelope(GeometrySnapshot);
    }
}
