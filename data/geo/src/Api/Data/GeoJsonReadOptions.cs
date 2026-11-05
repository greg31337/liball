namespace geo;

/// <summary>Specifies bounded GeoJSON parsing behavior.</summary>
public class GeoJsonReadOptions
{
    /// <summary>Whether all RFC 7946 structural requirements are enforced.</summary>
    public bool Strict = true;

    /// <summary>Whether arbitrary foreign members are retained.</summary>
    public bool PreserveForeignMembers = true;

    /// <summary>The maximum number of coordinates accepted from one document.</summary>
    public int MaximumCoordinates = GeometryConstants.DefaultMaximumCoordinates;

    /// <summary>The maximum number of object components accepted from one document.</summary>
    public int MaximumComponents = GeometryConstants.DefaultMaximumComponents;

    /// <summary>The maximum nested object depth accepted from one document.</summary>
    public int MaximumNestingDepth = GeometryConstants.DefaultMaximumNestingDepth;

    /// <summary>The maximum UTF-8 input size accepted in bytes.</summary>
    public int MaximumInputBytes = GeometryConstants.DefaultMaximumInputBytes;
}
