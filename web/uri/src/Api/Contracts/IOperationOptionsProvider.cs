namespace uri;

/// <summary>
/// Supplies independently configurable options for identifier operations.
/// </summary>
public interface IOperationOptionsProvider
{
    /// <summary>Gets URI parsing options.</summary>
    /// <returns>The URI parsing options.</returns>
    UriParseOptions GetUriParseOptions();

    /// <summary>Gets URI validation options.</summary>
    /// <returns>The URI validation options.</returns>
    UriValidationOptions GetUriValidationOptions();

    /// <summary>Gets URI normalization options.</summary>
    /// <returns>The URI normalization options.</returns>
    UriNormalizationOptions GetUriNormalizationOptions();

    /// <summary>Gets URI resolution options.</summary>
    /// <returns>The URI resolution options.</returns>
    UriResolutionOptions GetUriResolutionOptions();

    /// <summary>Gets URI relativization options.</summary>
    /// <returns>The URI relativization options.</returns>
    UriRelativizationOptions GetUriRelativizationOptions();

    /// <summary>Gets URI comparison options.</summary>
    /// <returns>The URI comparison options.</returns>
    UriComparisonOptions GetUriComparisonOptions();

    /// <summary>Gets percent-codec options.</summary>
    /// <returns>The percent-codec options.</returns>
    PercentCodecOptions GetPercentCodecOptions();

    /// <summary>Gets URI query options.</summary>
    /// <returns>The URI query options.</returns>
    UriQueryOptions GetUriQueryOptions();

    /// <summary>Gets IRI parsing options.</summary>
    /// <returns>The IRI parsing options.</returns>
    IriParseOptions GetIriParseOptions();

    /// <summary>Gets IRI conversion options.</summary>
    /// <returns>The IRI conversion options.</returns>
    IriConversionOptions GetIriConversionOptions();

    /// <summary>Gets URI Template parsing options.</summary>
    /// <returns>The URI Template parsing options.</returns>
    UriTemplateParseOptions GetUriTemplateParseOptions();

    /// <summary>Gets URI Template expansion options.</summary>
    /// <returns>The URI Template expansion options.</returns>
    UriTemplateExpansionOptions GetUriTemplateExpansionOptions();

    /// <summary>Gets URN parsing options.</summary>
    /// <returns>The URN parsing options.</returns>
    UrnParseOptions GetUrnParseOptions();

    /// <summary>Gets URN locator merge options.</summary>
    /// <returns>The URN locator merge options.</returns>
    UrnLocatorMergeOptions GetUrnLocatorMergeOptions();

    /// <summary>Gets Web Link parsing options.</summary>
    /// <returns>The Web Link parsing options.</returns>
    WebLinkParseOptions GetWebLinkParseOptions();
}
