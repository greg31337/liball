namespace geo;

/// <summary>Stores the ordered coordinates of a line string geometry payload.</summary>
public class LineString
{
    /// <summary>The line coordinates, or an empty array when the line string is empty.</summary>
    public Coordinate[] Coordinates = [];
}
