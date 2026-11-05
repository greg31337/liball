namespace geo;

/// <summary>Stores a polygon shell and its interior rings.</summary>
public class Polygon
{
    /// <summary>The polygon shell, which is empty when the polygon is empty.</summary>
    public LinearRing Shell = new LinearRing();

    /// <summary>The polygon interior rings.</summary>
    public LinearRing[] Holes = [];
}
