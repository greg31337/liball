namespace xml;

/// <summary>
/// Identifies an XML Catalog entry kind.
/// </summary>
public enum XmlCatalogEntryKind
{
    /// <summary>
    /// Identifies a group entry.
    /// </summary>
    Group = 0,
    /// <summary>
    /// Identifies an exact public identifier entry.
    /// </summary>
    Public = 1,
    /// <summary>
    /// Identifies an exact system identifier entry.
    /// </summary>
    System = 2,
    /// <summary>
    /// Identifies an exact URI entry.
    /// </summary>
    Uri = 3,
    /// <summary>
    /// Identifies a system identifier rewrite entry.
    /// </summary>
    RewriteSystem = 4,
    /// <summary>
    /// Identifies a URI rewrite entry.
    /// </summary>
    RewriteUri = 5,
    /// <summary>
    /// Identifies a system identifier suffix entry.
    /// </summary>
    SystemSuffix = 6,
    /// <summary>
    /// Identifies a URI suffix entry.
    /// </summary>
    UriSuffix = 7,
    /// <summary>
    /// Identifies a public identifier delegate entry.
    /// </summary>
    DelegatePublic = 8,
    /// <summary>
    /// Identifies a system identifier delegate entry.
    /// </summary>
    DelegateSystem = 9,
    /// <summary>
    /// Identifies a URI delegate entry.
    /// </summary>
    DelegateUri = 10,
    /// <summary>
    /// Identifies a chained catalog entry.
    /// </summary>
    NextCatalog = 11
}
