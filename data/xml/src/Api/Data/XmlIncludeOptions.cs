namespace xml;

/// <summary>
/// Carries XInclude, XPointer, fixup, and resource policy choices.
/// </summary>
public class XmlIncludeOptions
{
    /// <summary>
    /// Whether included XML is recursively processed for includes.
    /// </summary>
    public bool ProcessNestedIncludes;
    /// <summary>
    /// Whether XML base URI fixup is applied.
    /// </summary>
    public bool FixupBaseUri;
    /// <summary>
    /// Whether XML language fixup is applied.
    /// </summary>
    public bool FixupLanguage;
    /// <summary>
    /// Whether fallback wrapper nodes are preserved.
    /// </summary>
    public bool PreserveFallbackNodes;
    /// <summary>
    /// The maximum nested include depth.
    /// </summary>
    public int MaximumDepth;
    /// <summary>
    /// The maximum included resource size in bytes.
    /// </summary>
    public long MaximumResourceBytes;
    /// <summary>
    /// The HTTP Accept metadata.
    /// </summary>
    public string? Accept;
    /// <summary>
    /// The HTTP Accept-Language metadata.
    /// </summary>
    public string? AcceptLanguage;
    /// <summary>
    /// The encoding used for text inclusion when metadata is absent.
    /// </summary>
    public string? TextEncoding;
    /// <summary>
    /// The external resource policy.
    /// </summary>
    public XmlResolverPolicy? ResolverPolicy;
    /// <summary>
    /// The catalogs used for include resolution.
    /// </summary>
    public XmlCatalogEntry[]? CatalogEntries;
}

