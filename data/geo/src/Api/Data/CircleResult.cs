namespace geo;

/// <summary>Stores a circle geometry, center, radius, unit, and witness coordinates.</summary>
public class CircleResult
{
    /// <summary>The constructed circle geometry, or null when no circle exists.</summary>
    public Geometry? Geometry;

    /// <summary>The circle center, or null when no circle exists.</summary>
    public Coordinate? Center;

    /// <summary>The circle radius.</summary>
    public double Radius;

    /// <summary>The unit used for the circle radius.</summary>
    public SpatialUnit Unit = SpatialUnit.Meters;

    /// <summary>The source coordinates determining the circle.</summary>
    public Coordinate[] Witnesses = [];
}
