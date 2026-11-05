namespace uri;

/// <summary>
/// Applies or diagnoses explicit Unicode normalization for IRI references.
/// </summary>
public interface IIriNormalizer
{
    /// <summary>Applies the configured Unicode normalization behavior.</summary>
    /// <param name="text">The complete IRI-reference text.</param>
    /// <param name="options">The normalization options.</param>
    /// <returns>The preserved or normalized IRI-reference text.</returns>
    /// <exception cref="IriConversionException">Normalization or a configured limit fails.</exception>
    string NormalizeIri(string text, IriConversionOptions options);

    /// <summary>Reports the first component requiring NFC without changing the input.</summary>
    /// <param name="text">The complete IRI-reference text.</param>
    /// <param name="options">The normalization options.</param>
    /// <returns>The first diagnostic, or <c>null</c> when the IRI is in NFC.</returns>
    UriDiagnostic? DiagnoseIriNormalization(
        string text,
        IriConversionOptions options);
}
