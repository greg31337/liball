namespace uri;

/// <summary>
/// Provides bounded strict and same-scheme-compatible URI reference resolution.
/// </summary>
public class UriResolver : IUriResolver
{
    /// <summary>Initializes a stateless URI resolver.</summary>
    public UriResolver()
    {
    }

    /// <summary>Resolves a URI reference or throws when resolution fails.</summary>
    /// <param name="baseUri">The absolute base URI without a fragment.</param>
    /// <param name="reference">The URI reference to resolve.</param>
    /// <param name="options">The resolution options.</param>
    /// <returns>The resolved absolute URI text.</returns>
    /// <exception cref="UriResolutionException">Validation, resolution, or a configured limit fails.</exception>
    public string ResolveReference(
        string baseUri,
        string reference,
        UriResolutionOptions options)
    {
        UriResolutionCoreResult result = UriResolverCore.ResolveReferenceText(
            baseUri,
            reference,
            options);
        if (!result.Success)
        {
            throw CreateResolutionException(result.Diagnostic);
        }

        return result.Text;
    }

    /// <summary>Resolves a URI reference without throwing for expected failure.</summary>
    /// <param name="baseUri">The absolute base URI without a fragment.</param>
    /// <param name="reference">The URI reference to resolve.</param>
    /// <param name="options">The resolution options.</param>
    /// <returns>The resolution outcome.</returns>
    public UriTextResult TryResolveReference(
        string baseUri,
        string reference,
        UriResolutionOptions options)
    {
        UriResolutionCoreResult result = UriResolverCore.ResolveReferenceText(
            baseUri,
            reference,
            options);
        return new UriTextResult(
            result.Success,
            result.Text,
            result.Length,
            result.Diagnostic);
    }

    internal UriResolutionException CreateResolutionException(
        UriDiagnostic? diagnostic)
    {
        UriDiagnostic failure;
        if (diagnostic == null)
        {
            failure = DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri);
        }
        else
        {
            failure = diagnostic;
        }

        return new UriResolutionException(failure);
    }
}
