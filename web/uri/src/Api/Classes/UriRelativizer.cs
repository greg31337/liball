namespace uri;

/// <summary>
/// Provides bounded, policy-selected, verified URI relativization.
/// </summary>
public class UriRelativizer : IUriRelativizer
{
    /// <summary>Initializes a stateless URI relativizer.</summary>
    public UriRelativizer()
    {
    }

    /// <summary>Relativizes an absolute target URI or throws when no verified reference can be created.</summary>
    /// <param name="baseUri">The absolute base URI without a fragment.</param>
    /// <param name="targetUri">The absolute target URI.</param>
    /// <param name="options">The relativization options.</param>
    /// <returns>The selected URI reference text.</returns>
    /// <exception cref="UriResolutionException">Validation, relativization, or a configured limit fails.</exception>
    public string RelativizeReference(
        string baseUri,
        string targetUri,
        UriRelativizationOptions options)
    {
        UriRelativizationCoreResult result = UriRelativizerCore.RelativizeReferenceText(
            baseUri,
            targetUri,
            options);
        if (!result.Success)
        {
            throw CreateResolutionException(result.Diagnostic);
        }

        return result.Text;
    }

    /// <summary>Relativizes an absolute target URI without throwing for expected failure.</summary>
    /// <param name="baseUri">The absolute base URI without a fragment.</param>
    /// <param name="targetUri">The absolute target URI.</param>
    /// <param name="options">The relativization options.</param>
    /// <returns>The relativization outcome.</returns>
    public UriTextResult TryRelativizeReference(
        string baseUri,
        string targetUri,
        UriRelativizationOptions options)
    {
        UriRelativizationCoreResult result = UriRelativizerCore.RelativizeReferenceText(
            baseUri,
            targetUri,
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
