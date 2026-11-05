namespace geo;

/// <summary>Specifies bounded well-known text parsing behavior.</summary>
public class WktReadOptions
{
    /// <summary>The maximum number of coordinates accepted from one value.</summary>
    public int MaximumCoordinates = GeometryConstants.DefaultMaximumCoordinates;

    /// <summary>The maximum number of geometry components accepted from one value.</summary>
    public int MaximumComponents = GeometryConstants.DefaultMaximumComponents;

    /// <summary>The maximum nested geometry depth accepted from one value.</summary>
    public int MaximumNestingDepth = GeometryConstants.DefaultMaximumNestingDepth;

    /// <summary>Whether surface and curved extension type names are accepted.</summary>
    public bool AllowExtendedTypes = true;

    /// <summary>Whether an EWKT SRID prefix is accepted.</summary>
    public bool AllowSpatialReferenceId;
}
