namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="PdfFontResourceReport"/>.</summary>
public interface IPdfFontResourceReport
{
    /// <summary>Gets the earliest PDF version supporting this font strategy.</summary>
    /// <returns>PDF 1.1, PDF 1.2, or PDF 1.6.</returns>
    PdfVersion GetMinimumPdfVersion();

    /// <summary>Gets the PDF version selected when the resource was built.</summary>
    /// <returns>The caller's target PDF version.</returns>
    PdfVersion GetTargetPdfVersion();

    /// <summary>Gets the normalized result of applying embedding rights.</summary>
    /// <returns>The finalized rights outcome.</returns>
    PdfFontEmbeddingRightsOutcome GetEmbeddingRightsOutcome();

    /// <summary>Gets the complete embedding-right decision and diagnostics.</summary>
    /// <returns>The same immutable decision exposed by the font artifact.</returns>
    FontSubsetEmbeddingDecision GetEmbeddingDecision();

    /// <summary>Gets the number of allocated nonzero codes without original Unicode.</summary>
    /// <returns>The missing mapping count.</returns>
    int GetMissingUnicodeMappingCount();

    /// <summary>Gets one allocated code without original Unicode.</summary>
    /// <param name="index">The zero-based missing-mapping index.</param>
    /// <returns>The one-byte or two-byte character code.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the missing-mapping list.
    /// </exception>
    int GetMissingUnicodeMappingCode(int index);

    /// <summary>Copies allocated codes without original Unicode.</summary>
    /// <returns>A new array in ascending character-code order.</returns>
    int[] GetMissingUnicodeMappingCodes();

    /// <summary>Gets the number of occurrence-level ActualText recommendations.</summary>
    /// <returns>The recommendation count.</returns>
    int GetActualTextRecommendationCount();

    /// <summary>Reports whether ambiguous observations require ActualText.</summary>
    /// <returns>True when at least one recommendation exists.</returns>
    bool RequiresActualText();

    /// <summary>Gets the requested PDF/A assessment profile.</summary>
    /// <returns>The archival profile, or <c>None</c>.</returns>
    PdfArchivalProfile GetArchivalProfile();

    /// <summary>Gets the requested PDF/UA assessment profile.</summary>
    /// <returns>The accessibility profile, or <c>None</c>.</returns>
    PdfAccessibilityProfile GetAccessibilityProfile();

    /// <summary>Reports whether requested PDF/A assessment found a font concern.</summary>
    /// <returns>True when at least one PDF/A concern exists.</returns>
    bool HasPdfAEmbeddingConcerns();

    /// <summary>Reports whether requested PDF/UA assessment found a text concern.</summary>
    /// <returns>True when at least one PDF/UA concern exists.</returns>
    bool HasPdfUaUnicodeConcerns();

    /// <summary>Gets the number of requested-profile font concerns.</summary>
    /// <returns>The concern count.</returns>
    int GetConcernCount();

    /// <summary>Gets one requested-profile font concern.</summary>
    /// <param name="index">The zero-based concern index.</param>
    /// <returns>The immutable concern.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the concern list.
    /// </exception>
    PdfFontResourceConcern GetConcern(int index);

    /// <summary>Copies every requested-profile font concern.</summary>
    /// <returns>A new array in stable concern-kind order.</returns>
    PdfFontResourceConcern[] GetConcerns();

    /// <summary>Reports whether this font-level assessment claims document conformance.</summary>
    /// <returns>Always false.</returns>
    bool ClaimsDocumentConformance();
}
