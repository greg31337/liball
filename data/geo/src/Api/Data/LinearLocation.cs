namespace geo;

/// <summary>Stores a component, segment, and fractional position on a linear geometry.</summary>
public class LinearLocation
{
    /// <summary>Whether the location refers to a usable linear component.</summary>
    public bool IsValid = true;

    /// <summary>The zero-based multi-line component index.</summary>
    public int ComponentIndex;

    /// <summary>The zero-based segment index within the component.</summary>
    public int SegmentIndex;

    /// <summary>The fractional position along the segment from zero through one.</summary>
    public double SegmentFraction;
}
