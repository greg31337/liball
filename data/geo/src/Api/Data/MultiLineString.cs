namespace geo;

/// <summary>Stores the homogeneous line components of a multi-line string payload.</summary>
public class MultiLineString
{
    /// <summary>The line components, or an empty array when the multi-line string is empty.</summary>
    public LineString[] LineStrings = [];
}
