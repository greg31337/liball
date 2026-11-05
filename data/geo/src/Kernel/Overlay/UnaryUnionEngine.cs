namespace geo;

internal static class UnaryUnionEngine
{
    internal static Geometry GetUnaryUnion(Geometry geometry)
    {
        RelationGeometryParts parts = new RelationGeometryParts(geometry);
        Geometry empty = GeometryCollections.CreateRawCollection(
            [], geometry.CoordinateLayout, geometry.SpatialReferenceId);
        return OverlayEngine.GetUnion(parts.Root, empty);
    }

    internal static Geometry GetDissolvedGeometry(Geometry geometry)
    {
        return GetUnaryUnion(geometry);
    }
}
