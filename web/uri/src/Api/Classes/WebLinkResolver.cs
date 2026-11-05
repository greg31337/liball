namespace uri;

/// <summary>
/// Provides bounded Web Link target and context resolution without dereferencing.
/// </summary>
public class WebLinkResolver : IWebLinkResolver
{
    /// <summary>Initializes a stateless Web Link resolver.</summary>
    public WebLinkResolver()
    {
    }

    /// <summary>Resolves every link in an ordered Web Link set.</summary>
    /// <param name="linkSet">The links to resolve.</param>
    /// <param name="representationContext">The representation context, or <c>null</c> when anonymous.</param>
    /// <param name="limits">The processing limits.</param>
    /// <returns>One resolution outcome for each source link.</returns>
    public WebLinkResolutionResult[] ResolveWebLinks(
        WebLinkSet linkSet,
        string? representationContext,
        OperationLimits limits)
    {
        return WebLinkResolverCore.ResolveWebLinks(
            linkSet,
            representationContext,
            limits);
    }

    /// <summary>Resolves one Web Link target and effective context.</summary>
    /// <param name="link">The link to resolve.</param>
    /// <param name="representationContext">The representation context, or <c>null</c> when anonymous.</param>
    /// <param name="limits">The processing limits.</param>
    /// <returns>The resolution outcome.</returns>
    public WebLinkResolutionResult ResolveWebLink(
        WebLink link,
        string? representationContext,
        OperationLimits limits)
    {
        return WebLinkResolverCore.ResolveWebLink(
            link,
            representationContext,
            limits);
    }
}
