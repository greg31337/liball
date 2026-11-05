namespace uri;

/// <summary>
/// Resolves Web Link targets and effective contexts without dereferencing them.
/// </summary>
public interface IWebLinkResolver
{
    /// <summary>Resolves every link in an ordered Web Link set.</summary>
    /// <param name="linkSet">The links to resolve.</param>
    /// <param name="representationContext">The representation context, or <c>null</c> when anonymous.</param>
    /// <param name="limits">The processing limits.</param>
    /// <returns>One resolution outcome for each source link.</returns>
    WebLinkResolutionResult[] ResolveWebLinks(
        WebLinkSet linkSet,
        string? representationContext,
        OperationLimits limits);

    /// <summary>Resolves one Web Link target and effective context.</summary>
    /// <param name="link">The link to resolve.</param>
    /// <param name="representationContext">The representation context, or <c>null</c> when anonymous.</param>
    /// <param name="limits">The processing limits.</param>
    /// <returns>The resolution outcome.</returns>
    WebLinkResolutionResult ResolveWebLink(
        WebLink link,
        string? representationContext,
        OperationLimits limits);
}
