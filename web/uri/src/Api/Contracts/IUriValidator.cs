namespace uri;

/// <summary>
/// Validates URI text and caller-created URI component models.
/// </summary>
public interface IUriValidator
{
    /// <summary>Validates complete URI-reference text and returns its first failure.</summary>
    /// <param name="text">The complete input text.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The validation outcome.</returns>
    UriValidationResult ValidateReference(string text, UriValidationOptions options);

    /// <summary>Validates a caller-created URI component model and returns its first failure.</summary>
    /// <param name="reference">The component model to validate.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The validation outcome.</returns>
    UriValidationResult ValidateComponents(UriReference reference, UriValidationOptions options);

    /// <summary>Validates a component model and returns every deterministic diagnostic.</summary>
    /// <param name="reference">The component model to validate.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>All diagnostics in component order.</returns>
    UriDiagnostic[] ValidateAllDiagnostics(UriReference reference, UriValidationOptions options);
}
