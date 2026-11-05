namespace uri;

/// <summary>
/// Provides validated, bounded URI reference composition.
/// </summary>
public class UriComposer : IUriComposer
{
    /// <summary>Initializes a stateless URI composer.</summary>
    public UriComposer()
    {
    }

    /// <summary>Validates and composes a URI reference or throws on failure.</summary>
    /// <param name="reference">The component model to compose.</param>
    /// <param name="options">The validation and processing options.</param>
    /// <returns>The composed URI-reference text.</returns>
    /// <exception cref="UriValidationException">Validation or a configured limit fails.</exception>
    public string ComposeReference(
        UriReference reference,
        UriValidationOptions options)
    {
        UriCompositionResult result = UriComposerCore.ComposeValidatedReference(
            reference,
            options);
        if (!result.Success)
        {
            throw CreateValidationException(result.Diagnostic);
        }

        return result.Text;
    }

    /// <summary>Validates and composes a URI reference without throwing for expected failure.</summary>
    /// <param name="reference">The component model to compose.</param>
    /// <param name="options">The validation and processing options.</param>
    /// <returns>The composition outcome.</returns>
    public UriTextResult TryComposeReference(
        UriReference reference,
        UriValidationOptions options)
    {
        UriCompositionResult result = UriComposerCore.ComposeValidatedReference(
            reference,
            options);
        return new UriTextResult(
            result.Success,
            result.Text,
            result.Length,
            result.Diagnostic);
    }

    /// <summary>Validates a URI reference and measures its composed output length.</summary>
    /// <param name="reference">The component model to measure.</param>
    /// <param name="options">The validation and processing options.</param>
    /// <returns>The exact number of UTF-16 code units in the composed output.</returns>
    /// <exception cref="UriValidationException">Validation or a configured limit fails.</exception>
    public int MeasureReference(
        UriReference reference,
        UriValidationOptions options)
    {
        UriMeasureResult result = UriComposerCore.MeasureValidatedReference(
            reference,
            options);
        if (!result.Success)
        {
            throw CreateValidationException(result.Diagnostic);
        }

        return result.Length;
    }

    internal UriValidationException CreateValidationException(UriDiagnostic? diagnostic)
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

        return new UriValidationException(failure);
    }
}
