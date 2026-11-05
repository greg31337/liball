using System;

namespace uri;

/// <summary>
/// Provides strict URI text and component-model validation.
/// </summary>
public class UriValidator : IUriValidator
{
    /// <summary>Initializes a stateless URI validator.</summary>
    public UriValidator()
    {
    }

    /// <summary>Validates complete URI-reference text and returns its first failure.</summary>
    /// <param name="text">The complete input text.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The validation outcome.</returns>
    public UriValidationResult ValidateReference(
        string text,
        UriValidationOptions options)
    {
        if (text == null)
        {
            UriDiagnostic invalid = DiagnosticFactory.CreateInvalidInput(
                DiagnosticDomain.Uri);
            return CreateValidationFailure(invalid);
        }

        return UriValidatorCore.ValidateReference(text.AsSpan(), options);
    }

    /// <summary>Validates a caller-created URI component model and returns its first failure.</summary>
    /// <param name="reference">The component model to validate.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The validation outcome.</returns>
    public UriValidationResult ValidateComponents(
        UriReference reference,
        UriValidationOptions options)
    {
        return UriValidatorCore.ValidateComponents(reference, options);
    }

    /// <summary>Validates a component model and returns every deterministic diagnostic.</summary>
    /// <param name="reference">The component model to validate.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>All diagnostics in component order.</returns>
    public UriDiagnostic[] ValidateAllDiagnostics(
        UriReference reference,
        UriValidationOptions options)
    {
        return UriValidatorCore.ValidateAllDiagnostics(reference, options);
    }

    internal UriValidationResult CreateValidationFailure(UriDiagnostic diagnostic)
    {
        return new UriValidationResult(
            false,
            new UriReference(),
            diagnostic,
            new UriDiagnostic[] { diagnostic });
    }
}
