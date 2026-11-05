namespace uri;

/// <summary>
/// Composes reusable URI Template models with optional percent-triplet case canonicalization.
/// </summary>
public interface IUriTemplateComposer
{
    /// <summary>Composes the template model without semantic rewriting.</summary>
    /// <param name="template">The reusable template model.</param>
    /// <param name="limits">The processing limits.</param>
    /// <returns>The composed URI Template text.</returns>
    string ComposeTemplate(UriTemplate template, OperationLimits limits);

    /// <summary>Composes the template while uppercasing percent-triplet hexadecimal digits.</summary>
    /// <param name="template">The reusable template model.</param>
    /// <param name="limits">The processing limits.</param>
    /// <returns>The composed URI Template text with canonical percent-triplet case.</returns>
    string CanonicalizeTemplatePercentCase(
        UriTemplate template,
        OperationLimits limits);
}
