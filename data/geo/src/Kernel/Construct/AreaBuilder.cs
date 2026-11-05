namespace geo;

internal static class AreaBuilder
{
    internal static Geometry BuildGeometry(Geometry geometry)
    {
        PolygonizationResult polygonization =
            Polygonizer.GetPolygonization(geometry);
        Polygon[] polygons = TopologyPolygonBuilder.BuildPolygons(
            polygonization.AreaRings);
        Geometry area = OverlayResultExtractor.CreateResult(
            polygons, [], [], geometry, GeometryType.Polygon);
        if (polygons.Length < 2) return area;
        return UnaryUnionEngine.GetUnaryUnion(area);
    }
}
