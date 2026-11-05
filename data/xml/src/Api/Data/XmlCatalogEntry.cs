namespace xml;

/// <summary>
/// Carries one XML Catalog 1.1 entry or group.
/// </summary>
public class XmlCatalogEntry
{
    /// <summary>
    /// The catalog entry kind.
    /// </summary>
    public XmlCatalogEntryKind Kind;
    /// <summary>
    /// The public identifier match value.
    /// </summary>
    public string? PublicIdentifier;
    /// <summary>
    /// The system identifier match value.
    /// </summary>
    public string? SystemIdentifier;
    /// <summary>
    /// The URI match value.
    /// </summary>
    public string? Uri;
    /// <summary>
    /// The absolute replacement URI for an exact or suffix entry.
    /// </summary>
    public string? TargetUri;
    /// <summary>
    /// The replacement prefix for a rewrite entry.
    /// </summary>
    public string? RewritePrefix;
    /// <summary>
    /// The suffix match value.
    /// </summary>
    public string? Suffix;
    /// <summary>
    /// The delegate or next-catalog URI.
    /// </summary>
    public string? CatalogUri;
    /// <summary>
    /// The effective base URI.
    /// </summary>
    public string? BaseUri;
    /// <summary>
    /// Whether public identifier matching is preferred.
    /// </summary>
    public bool PreferPublic;
    /// <summary>
    /// The ordered child entries of a group.
    /// </summary>
    public XmlCatalogEntry[]? Entries;
    /// <summary>
    /// The source range.
    /// </summary>
    public SourceRange? Range;
}
