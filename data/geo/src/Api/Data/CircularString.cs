namespace geo;

/// <summary>Stores the control coordinates of a circular string payload.</summary>
public class CircularString
{
    /// <summary>The circular arc coordinates, or an empty array when the curve is empty.</summary>
    public Coordinate[] Coordinates = [];
}
