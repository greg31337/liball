using System;

namespace uri;

/// <summary>
/// Provides bounded URI normalization and change analysis.
/// </summary>
public class UriNormalizer : IUriNormalizer
{
    /// <summary>Initializes a stateless URI normalizer.</summary>
    public UriNormalizer()
    {
    }

    /// <summary>Reports generic and explicitly profiled transformations without changing the input.</summary>
    /// <param name="text">The complete URI-reference text.</param>
    /// <param name="options">The normalization options.</param>
    /// <returns>The required syntax and explicit-profile transformations in order.</returns>
    /// <exception cref="UriNormalizationException">Validation or a configured limit fails.</exception>
    public UriNormalizationChange[] AnalyzeNormalization(
        string text,
        UriNormalizationOptions options)
    {
        UriSchemeProfile? profile = null;
        if (options != null)
        {
            profile = options.Profile;
        }

        UriParseResult parsed = ParseForNormalization(
            text,
            options,
            profile != null);
        if (!parsed.Success)
        {
            throw CreateNormalizationException(parsed.Diagnostic);
        }

        return UriNormalizerCore.AnalyzeNormalizationChanges(
            parsed.Reference,
            profile);
    }

    /// <summary>Applies generic syntax normalization.</summary>
    /// <param name="text">The complete URI-reference text.</param>
    /// <param name="options">The normalization options.</param>
    /// <returns>The normalized URI-reference text.</returns>
    /// <exception cref="UriNormalizationException">Validation or a configured limit fails.</exception>
    public string NormalizeSyntax(string text, UriNormalizationOptions options)
    {
        UriParseResult parsed = ParseForNormalization(text, options, false);
        if (!parsed.Success)
        {
            throw CreateNormalizationException(parsed.Diagnostic);
        }

        UriReference normalized = UriNormalizerCore.NormalizeSyntaxReference(
            parsed.Reference);
        return ComposeNormalizedReference(normalized, options);
    }

    /// <summary>Applies generic normalization and an explicit or built-in scheme profile.</summary>
    /// <param name="text">The complete URI-reference text.</param>
    /// <param name="options">The normalization options and optional profile.</param>
    /// <returns>The normalized URI-reference text.</returns>
    /// <exception cref="UriNormalizationException">Validation, profile selection, or a configured limit fails.</exception>
    public string NormalizeWithProfile(string text, UriNormalizationOptions options)
    {
        UriParseResult parsed = ParseForNormalization(text, options, true);
        if (!parsed.Success)
        {
            throw CreateNormalizationException(parsed.Diagnostic);
        }

        UriReference normalized;
        if (options.Profile == null)
        {
            normalized = UriNormalizerCore.NormalizeWithBuiltInSchemeProfile(
                parsed.Reference);
        }
        else
        {
            normalized = UriNormalizerCore.NormalizeWithSchemeProfile(
                parsed.Reference,
                options.Profile);
        }

        return ComposeNormalizedReference(normalized, options);
    }

    internal UriParseResult ParseForNormalization(
        string? text,
        UriNormalizationOptions? options,
        bool validateProfile)
    {
        if (options == null || options.Limits == null || text == null)
        {
            return CreateNormalizationParseFailure(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri));
        }

        if (validateProfile &&
            options.Profile != null &&
            !UriNormalizerCore.IsValidSchemeProfile(options.Profile))
        {
            return CreateNormalizationParseFailure(
                DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Uri));
        }

        UriValidationOptions validationOptions = new UriValidationOptions();
        validationOptions.Limits = options.Limits;
        UriValidationResult validation = UriValidatorCore.ValidateReference(
            text.AsSpan(),
            validationOptions);
        return new UriParseResult(
            validation.Success,
            validation.Reference,
            validation.Diagnostic);
    }

    internal string ComposeNormalizedReference(
        UriReference reference,
        UriNormalizationOptions options)
    {
        UriCompositionResult composed = UriComposerCore.RecomposeParsedReference(
            reference,
            options.Limits);
        if (!composed.Success)
        {
            throw CreateNormalizationException(composed.Diagnostic);
        }

        return composed.Text;
    }

    internal UriParseResult CreateNormalizationParseFailure(
        UriDiagnostic diagnostic)
    {
        return new UriParseResult(false, new UriReference(), diagnostic);
    }

    internal UriNormalizationException CreateNormalizationException(
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

        return new UriNormalizationException(failure);
    }
}
