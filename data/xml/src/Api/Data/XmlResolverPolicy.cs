namespace xml;

/// <summary>
/// Carries external resource access policy and limits.
/// </summary>
public class XmlResolverPolicy
{
    /// <summary>
    /// The default external resource access decision.
    /// </summary>
    public XmlResourceAccess Access;
    /// <summary>
    /// The case-insensitive URI schemes that may be resolved.
    /// </summary>
    public string[]? AllowedSchemes;
    /// <summary>
    /// The normalized file roots that may be resolved.
    /// </summary>
    public string[]? AllowedRoots;
    /// <summary>
    /// Whether resource redirects are permitted.
    /// </summary>
    public bool AllowRedirects;
    /// <summary>
    /// The maximum redirect count.
    /// </summary>
    public int MaximumRedirects;
    /// <summary>
    /// The maximum nested resolution depth.
    /// </summary>
    public int MaximumDepth;
    /// <summary>
    /// The maximum bytes read from one resource.
    /// </summary>
    public long MaximumResourceBytes;
    /// <summary>
    /// The maximum deterministic resolver work units.
    /// </summary>
    public long MaximumWorkUnits;
    /// <summary>
    /// Whether resolved resources may be cached.
    /// </summary>
    public bool CacheResources;
    /// <summary>
    /// The catalogs consulted before the caller resolver.
    /// </summary>
    public XmlCatalogEntry[]? CatalogEntries;
    /// <summary>
    /// The HTTP Accept metadata supplied for the current resource request.
    /// </summary>
    public string? Accept;
    /// <summary>
    /// The HTTP Accept-Language metadata supplied for the current resource request.
    /// </summary>
    public string? AcceptLanguage;
}
