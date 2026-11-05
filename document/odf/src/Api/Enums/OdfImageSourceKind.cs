namespace odf;

/// <summary>
/// Identifies how a drawing image obtains its content.
/// </summary>
public enum OdfImageSourceKind
{
    /// <summary>The image source is absent or unrecognized.</summary>
    Unknown = 0,
    /// <summary>The image is stored in the OpenDocument package.</summary>
    Package = 1,
    /// <summary>The image refers to an external IRI and is not fetched.</summary>
    External = 2,
    /// <summary>The image is stored in an inline office binary-data element.</summary>
    Base64 = 3
}
