namespace geo;

/// <summary>Stores one integer-keyed spatial index entry.</summary>
public class SpatialIndexItem
{
    /// <summary>The caller-assigned integer identifier.</summary>
    public int Id;

    /// <summary>The indexed envelope.</summary>
    public Envelope Envelope = new Envelope();

    /// <summary>The optional geometry used for explicit query refinement.</summary>
    public Geometry? Geometry;

    /// <summary>The optional caller payload.</summary>
    public object? Payload;
}
