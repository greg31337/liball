namespace odf;

/// <summary>
/// Identifies where a referenced document resource is located.
/// </summary>
public enum OdfResourceSourceKind
{
    /// <summary>The resource location is absent or unrecognized.</summary>
    Unknown = 0,
    /// <summary>The resource is stored in the OpenDocument package.</summary>
    Package = 1,
    /// <summary>The resource is linked outside the OpenDocument package.</summary>
    External = 2,
    /// <summary>The resource is stored inline in XML.</summary>
    Inline = 3
}
