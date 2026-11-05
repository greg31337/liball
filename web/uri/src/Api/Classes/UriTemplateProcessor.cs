namespace uri;

/// <summary>
/// Provides composed URI Template expansion and URI processing workflows.
/// </summary>
public class UriTemplateProcessor : IUriTemplateProcessor
{
    /// <summary>Initializes a stateless URI Template processor.</summary>
    public UriTemplateProcessor()
    {
    }

    /// <summary>Expands and parses a URI-reference.</summary>
    /// <param name="template">The reusable template model.</param>
    /// <param name="bindings">The case-sensitive variable bindings.</param>
    /// <param name="expansionOptions">The expansion options.</param>
    /// <param name="parseOptions">The URI parsing options.</param>
    /// <returns>The parsed expanded URI reference.</returns>
    /// <exception cref="UriTemplateExpansionException">Template expansion fails.</exception>
    /// <exception cref="UriSyntaxException">URI parsing or a configured URI limit fails.</exception>
    public UriReference ExpandToUri(
        UriTemplate template,
        UriTemplateBinding[] bindings,
        UriTemplateExpansionOptions expansionOptions,
        UriParseOptions parseOptions)
    {
        UriParseResult result = TemplateProcessorCore.ExpandToUri(
            template,
            bindings,
            expansionOptions,
            parseOptions);
        if (!result.Success)
        {
            if (IsExpansionDiagnostic(result.Diagnostic))
            {
                throw CreateExpansionException(result.Diagnostic);
            }

            throw CreateUriSyntaxException(result.Diagnostic);
        }

        return result.Reference;
    }

    /// <summary>Expands and resolves a URI reference against an absolute base URI.</summary>
    /// <param name="template">The reusable template model.</param>
    /// <param name="bindings">The case-sensitive variable bindings.</param>
    /// <param name="baseUri">The absolute base URI without a fragment.</param>
    /// <param name="expansionOptions">The expansion options.</param>
    /// <param name="resolutionOptions">The URI resolution options.</param>
    /// <returns>The resolved absolute URI text.</returns>
    /// <exception cref="UriTemplateExpansionException">Template expansion fails.</exception>
    /// <exception cref="UriResolutionException">URI resolution or a configured URI limit fails.</exception>
    public string ExpandAndResolve(
        UriTemplate template,
        UriTemplateBinding[] bindings,
        string baseUri,
        UriTemplateExpansionOptions expansionOptions,
        UriResolutionOptions resolutionOptions)
    {
        UriTextResult result = TemplateProcessorCore.ExpandAndResolve(
            template,
            bindings,
            expansionOptions,
            baseUri,
            resolutionOptions);
        if (!result.Success)
        {
            if (IsExpansionDiagnostic(result.Diagnostic))
            {
                throw CreateExpansionException(result.Diagnostic);
            }

            throw CreateResolutionException(result.Diagnostic);
        }

        return result.Text;
    }

    /// <summary>Expands and syntax-normalizes a URI reference.</summary>
    /// <param name="template">The reusable template model.</param>
    /// <param name="bindings">The case-sensitive variable bindings.</param>
    /// <param name="expansionOptions">The expansion options.</param>
    /// <param name="normalizationOptions">The URI normalization options and optional profile.</param>
    /// <returns>The normalized URI-reference text.</returns>
    /// <exception cref="UriTemplateExpansionException">Template expansion fails.</exception>
    /// <exception cref="UriNormalizationException">URI normalization or a configured URI limit fails.</exception>
    public string ExpandAndNormalize(
        UriTemplate template,
        UriTemplateBinding[] bindings,
        UriTemplateExpansionOptions expansionOptions,
        UriNormalizationOptions normalizationOptions)
    {
        UriTextResult result = TemplateProcessorCore.ExpandAndNormalize(
            template,
            bindings,
            expansionOptions,
            normalizationOptions);
        if (!result.Success)
        {
            if (IsExpansionDiagnostic(result.Diagnostic))
            {
                throw CreateExpansionException(result.Diagnostic);
            }

            throw CreateNormalizationException(result.Diagnostic);
        }

        return result.Text;
    }

    internal bool IsExpansionDiagnostic(UriDiagnostic? diagnostic)
    {
        return diagnostic != null &&
            diagnostic.Domain == DiagnosticDomain.UriTemplate;
    }

    internal UriTemplateExpansionException CreateExpansionException(
        UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure = GetFailureDiagnostic(
            diagnostic,
            DiagnosticDomain.UriTemplate);
        return new UriTemplateExpansionException(failure);
    }

    internal UriSyntaxException CreateUriSyntaxException(
        UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure = GetFailureDiagnostic(
            diagnostic,
            DiagnosticDomain.Uri);
        return new UriSyntaxException(failure);
    }

    internal UriResolutionException CreateResolutionException(
        UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure = GetFailureDiagnostic(
            diagnostic,
            DiagnosticDomain.Uri);
        return new UriResolutionException(failure);
    }

    internal UriNormalizationException CreateNormalizationException(
        UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure = GetFailureDiagnostic(
            diagnostic,
            DiagnosticDomain.Uri);
        return new UriNormalizationException(failure);
    }

    internal UriDiagnostic GetFailureDiagnostic(
        UriDiagnostic? diagnostic,
        DiagnosticDomain fallbackDomain)
    {
        if (diagnostic == null)
        {
            return DiagnosticFactory.CreateInvalidInput(fallbackDomain);
        }

        return diagnostic;
    }
}
