namespace xml;

/// <summary>
/// Carries XML canonicalization method and subset controls.
/// </summary>
public class XmlCanonicalizationOptions
{
    /// <summary>
    /// The canonicalization method.
    /// </summary>
    public XmlCanonicalizationMethod Method;
    /// <summary>
    /// Whether comments are included.
    /// </summary>
    public bool IncludeComments;
    /// <summary>
    /// The node selection, or null to canonicalize the full document.
    /// </summary>
    public XmlNodeSelection? Selection;
    /// <summary>
    /// The inclusive prefix list used by exclusive canonicalization.
    /// </summary>
    public string[]? InclusivePrefixes;
    /// <summary>
    /// Whether the experimental profile propagates the default namespace.
    /// </summary>
    public bool PropagateDefaultNamespace;
    /// <summary>
    /// Whether the experimental profile performs configured URI rewriting.
    /// </summary>
    public bool RewriteUris;
}

