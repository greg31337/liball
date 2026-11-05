namespace uri;

/// <summary>
/// Composes validated URI component models into bounded text output.
/// </summary>
public interface IUriComposer
{
    /// <summary>Validates and composes a URI reference or throws on failure.</summary>
    /// <param name="reference">The component model to compose.</param>
    /// <param name="options">The validation and processing options.</param>
    /// <returns>The composed URI-reference text.</returns>
    /// <exception cref="UriValidationException">Validation or a configured limit fails.</exception>
    string ComposeReference(UriReference reference, UriValidationOptions options);

    /// <summary>Validates and composes a URI reference without throwing for expected failure.</summary>
    /// <param name="reference">The component model to compose.</param>
    /// <param name="options">The validation and processing options.</param>
    /// <returns>The composition outcome.</returns>
    UriTextResult TryComposeReference(UriReference reference, UriValidationOptions options);

    /// <summary>Validates a URI reference and measures its composed output length.</summary>
    /// <param name="reference">The component model to measure.</param>
    /// <param name="options">The validation and processing options.</param>
    /// <returns>The exact number of UTF-16 code units in the composed output.</returns>
    /// <exception cref="UriValidationException">Validation or a configured limit fails.</exception>
    int MeasureReference(UriReference reference, UriValidationOptions options);
}
