namespace geo;

/// <summary>Specifies bounded well-known binary parsing behavior.</summary>
public class WkbReadOptions
{
    /// <summary>The accepted well-known binary flavor.</summary>
    public WkbFlavor Flavor;

    /// <summary>The maximum number of coordinates accepted from one value.</summary>
    public int MaximumCoordinates = GeometryConstants.DefaultMaximumCoordinates;

    /// <summary>The maximum number of geometry components accepted from one value.</summary>
    public int MaximumComponents = GeometryConstants.DefaultMaximumComponents;

    /// <summary>The maximum nested geometry depth accepted from one value.</summary>
    public int MaximumNestingDepth = GeometryConstants.DefaultMaximumNestingDepth;

    /// <summary>The maximum encoded input size accepted in bytes.</summary>
    public int MaximumInputBytes = GeometryConstants.DefaultMaximumInputBytes;

    /// <summary>Whether unconsumed trailing bytes cause a parsing failure.</summary>
    public bool RequireExactLength = true;
}
