namespace geo;

/// <summary>Stores the homogeneous polygon components of a multi-polygon payload.</summary>
public class MultiPolygon
{
    /// <summary>The polygon components, or an empty array when the multi-polygon is empty.</summary>
    public Polygon[] Polygons = [];
}
