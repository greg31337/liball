namespace geo;

internal static class ConstrainedDelaunayBuilder
{
    internal static Geometry GetConstrainedDelaunayTriangulation(
        Geometry polygonal)
    {
        return PolygonTriangulator.TriangulatePolygonal(
            polygonal, true);
    }
}
