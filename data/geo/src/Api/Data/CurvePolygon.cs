namespace geo;

/// <summary>Stores a curved polygon shell and its interior curved rings.</summary>
public class CurvePolygon
{
    /// <summary>The shell curve, or null when the curve polygon is empty.</summary>
    public Geometry? Shell;

    /// <summary>The interior curve rings.</summary>
    public Geometry[] Holes = [];
}
