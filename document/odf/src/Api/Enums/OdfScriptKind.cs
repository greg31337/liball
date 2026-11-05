namespace odf;

/// <summary>
/// Identifies how inert script content is stored in an OpenDocument file.
/// </summary>
public enum OdfScriptKind
{
    /// <summary>The script storage kind is absent or unrecognized.</summary>
    Unknown = 0,
    /// <summary>An office:script element in an XML part.</summary>
    Embedded = 1,
    /// <summary>A package entry below the Scripts directory.</summary>
    PackageEntry = 2
}
