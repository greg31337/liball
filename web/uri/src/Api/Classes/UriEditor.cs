using System;

namespace uri;

/// <summary>
/// Provides bounded URI component construction and validated copy editing.
/// </summary>
public class UriEditor : IUriEditor
{
    /// <summary>Initializes a stateless URI editor.</summary>
    public UriEditor()
    {
    }

    /// <summary>Builds validated URI-reference text from a component model.</summary>
    /// <param name="components">The component model to build.</param>
    /// <param name="options">The validation and processing options.</param>
    /// <returns>The built URI-reference text.</returns>
    /// <exception cref="UriValidationException">Component validation or a configured limit fails.</exception>
    public string BuildFromComponents(
        UriReference components,
        UriValidationOptions options)
    {
        UriEditCoreResult result = UriEditorCore.BuildFromComponents(
            components,
            options);
        if (!result.Success)
        {
            throw CreateValidationException(result.Diagnostic);
        }

        return result.Text;
    }

    /// <summary>Builds URI-reference text without throwing for expected failure.</summary>
    /// <param name="components">The component model to build.</param>
    /// <param name="options">The validation and processing options.</param>
    /// <returns>The build outcome.</returns>
    public UriTextResult TryBuildFromComponents(
        UriReference components,
        UriValidationOptions options)
    {
        UriEditCoreResult result = UriEditorCore.BuildFromComponents(
            components,
            options);
        return new UriTextResult(
            result.Success,
            result.Text,
            result.Length,
            result.Diagnostic);
    }

    /// <summary>Creates validated URI-reference text by applying explicit component changes.</summary>
    /// <param name="source">The source URI-reference text.</param>
    /// <param name="changes">The component changes; unselected components are kept.</param>
    /// <param name="options">The validation and processing options.</param>
    /// <returns>The edited URI-reference text.</returns>
    /// <exception cref="UriValidationException">Source or edited component validation, or a configured limit, fails.</exception>
    public string CopyWithChanges(
        string source,
        UriReferenceChanges changes,
        UriValidationOptions options)
    {
        UriValidationResult sourceValidation = ValidateEditSource(
            source,
            options);
        if (!sourceValidation.Success)
        {
            throw CreateValidationException(sourceValidation.Diagnostic);
        }

        UriEditCoreResult result = UriEditorCore.CopyWithChanges(
            sourceValidation.Reference,
            changes,
            options);
        if (!result.Success)
        {
            throw CreateValidationException(result.Diagnostic);
        }

        return result.Text;
    }

    internal UriValidationResult ValidateEditSource(
        string? source,
        UriValidationOptions? options)
    {
        if (source == null || options == null || options.Limits == null)
        {
            UriDiagnostic invalid = DiagnosticFactory.CreateInvalidInput(
                DiagnosticDomain.Uri);
            return new UriValidationResult(
                false,
                new UriReference(),
                invalid,
                new UriDiagnostic[] { invalid });
        }

        return UriValidatorCore.ValidateReference(
            source.AsSpan(),
            options);
    }

    internal UriValidationException CreateValidationException(
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

        return new UriValidationException(failure);
    }
}
