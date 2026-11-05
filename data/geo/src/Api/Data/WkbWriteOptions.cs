namespace geo;

/// <summary>Specifies how well-known binary is encoded.</summary>
public class WkbWriteOptions
{
    /// <summary>The byte order of encoded numeric values.</summary>
    public ByteOrder ByteOrder;

    /// <summary>The well-known binary flavor to emit.</summary>
    public WkbFlavor Flavor;

    /// <summary>Whether an SRID is emitted when the selected flavor supports it.</summary>
    public bool IncludeSpatialReferenceId;
}
