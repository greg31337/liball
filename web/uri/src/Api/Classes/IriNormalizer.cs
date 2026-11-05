using System;

namespace uri;

/// <summary>
/// Provides bounded producer-side NFC normalization and diagnose-only handling.
/// </summary>
public class IriNormalizer : IIriNormalizer
{
    /// <summary>Initializes a stateless IRI normalizer.</summary>
    public IriNormalizer()
    {
    }

    /// <summary>Applies the configured Unicode normalization behavior.</summary>
    /// <param name="text">The complete IRI-reference text.</param>
    /// <param name="options">The normalization options.</param>
    /// <returns>The preserved or normalized IRI-reference text.</returns>
    /// <exception cref="IriConversionException">Normalization or a configured limit fails.</exception>
    public string NormalizeIri(string text, IriConversionOptions options)
    {
        UriDiagnostic? precondition = GetNormalizationPrecondition(text, options);
        if (precondition != null)
        {
            throw new IriConversionException(precondition);
        }

        UriTextResult result = IriNormalizerCore.NormalizeIri(
            text.AsSpan(),
            options.NormalizationMode,
            options.Limits);
        if (!result.Success)
        {
            throw IriConverter.CreateConversionException(result.Diagnostic);
        }

        return result.Text;
    }

    /// <summary>Reports the first component requiring NFC without changing the input.</summary>
    /// <param name="text">The complete IRI-reference text.</param>
    /// <param name="options">The normalization options.</param>
    /// <returns>The first diagnostic, or <c>null</c> when the IRI is in NFC.</returns>
    public UriDiagnostic? DiagnoseIriNormalization(
        string text,
        IriConversionOptions options)
    {
        UriDiagnostic? precondition = GetNormalizationPrecondition(text, options);
        if (precondition != null)
        {
            return precondition;
        }

        UriTextResult result = IriNormalizerCore.NormalizeIri(
            text.AsSpan(),
            UnicodeNormalizationMode.DiagnoseNfc,
            options.Limits);
        return result.Diagnostic;
    }

    internal UriDiagnostic? GetNormalizationPrecondition(
        string? text,
        IriConversionOptions? options)
    {
        UriDiagnostic? optionFailure = IriConverter.GetOptionsDiagnostic(options);
        if (optionFailure != null)
        {
            return optionFailure;
        }

        if (text == null)
        {
            return DiagnosticFactory.CreateInvalidInput(DiagnosticDomain.Iri);
        }

        return null;
    }
}
