namespace ttf;

/// <summary>Reports compatibility facts for one finalized PDF font resource.</summary>
/// <remarks>
/// Findings are limited to the font resource and the profiles requested in
/// <see cref="PdfFontOptions"/>. They do not validate or claim conformance of
/// any PDF document. Document structure, metadata, tagging, color, and every
/// other conformance requirement remain the caller's responsibility.
/// </remarks>
public class PdfFontResourceReport : IPdfFontResourceReport
{
    PdfVersion minimumPdfVersion;
    PdfVersion targetPdfVersion;
    PdfFontEmbeddingRightsOutcome embeddingRightsOutcome;
    FontSubsetEmbeddingDecision embeddingDecision;
    int[] missingUnicodeMappingCodes;
    int actualTextRecommendationCount;
    PdfArchivalProfile archivalProfile;
    PdfAccessibilityProfile accessibilityProfile;
    PdfFontResourceConcern[] concerns;

    internal PdfFontResourceReport(
        PdfVersion minimumPdfVersion,
        PdfVersion targetPdfVersion,
        PdfFontEmbeddingRightsOutcome embeddingRightsOutcome,
        FontSubsetEmbeddingDecision embeddingDecision,
        int[] missingUnicodeMappingCodes,
        int actualTextRecommendationCount,
        PdfArchivalProfile archivalProfile,
        PdfAccessibilityProfile accessibilityProfile,
        PdfFontResourceConcern[] concerns)
    {
        this.minimumPdfVersion = minimumPdfVersion;
        this.targetPdfVersion = targetPdfVersion;
        this.embeddingRightsOutcome = embeddingRightsOutcome;
        this.embeddingDecision = embeddingDecision;
        this.missingUnicodeMappingCodes =
            CopyIntegers(missingUnicodeMappingCodes);
        this.actualTextRecommendationCount =
            actualTextRecommendationCount;
        this.archivalProfile = archivalProfile;
        this.accessibilityProfile = accessibilityProfile;
        this.concerns = CopyConcerns(concerns);
    }

    /// <summary>Gets the earliest PDF version supporting this font strategy.</summary>
    /// <returns>PDF 1.1, PDF 1.2, or PDF 1.6.</returns>
    public PdfVersion GetMinimumPdfVersion()
    {
        return minimumPdfVersion;
    }

    /// <summary>Gets the PDF version selected when the resource was built.</summary>
    /// <returns>The caller's target PDF version.</returns>
    public PdfVersion GetTargetPdfVersion()
    {
        return targetPdfVersion;
    }

    /// <summary>Gets the normalized result of applying embedding rights.</summary>
    /// <returns>The finalized rights outcome.</returns>
    public PdfFontEmbeddingRightsOutcome GetEmbeddingRightsOutcome()
    {
        return embeddingRightsOutcome;
    }

    /// <summary>Gets the complete embedding-right decision and diagnostics.</summary>
    /// <returns>The same immutable decision exposed by the font artifact.</returns>
    public FontSubsetEmbeddingDecision GetEmbeddingDecision()
    {
        return embeddingDecision;
    }

    /// <summary>Gets the number of allocated nonzero codes without original Unicode.</summary>
    /// <returns>The missing mapping count.</returns>
    public int GetMissingUnicodeMappingCount()
    {
        return missingUnicodeMappingCodes.Length;
    }

    /// <summary>Gets one allocated code without original Unicode.</summary>
    /// <param name="index">The zero-based missing-mapping index.</param>
    /// <returns>The one-byte or two-byte character code.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the missing-mapping list.
    /// </exception>
    public int GetMissingUnicodeMappingCode(int index)
    {
        if (index < 0 || index >= missingUnicodeMappingCodes.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return missingUnicodeMappingCodes[index];
    }

    /// <summary>Copies allocated codes without original Unicode.</summary>
    /// <returns>A new array in ascending character-code order.</returns>
    public int[] GetMissingUnicodeMappingCodes()
    {
        return CopyIntegers(missingUnicodeMappingCodes);
    }

    /// <summary>Gets the number of occurrence-level ActualText recommendations.</summary>
    /// <returns>The recommendation count.</returns>
    public int GetActualTextRecommendationCount()
    {
        return actualTextRecommendationCount;
    }

    /// <summary>Reports whether ambiguous observations require ActualText.</summary>
    /// <returns>True when at least one recommendation exists.</returns>
    public bool RequiresActualText()
    {
        return actualTextRecommendationCount != 0;
    }

    /// <summary>Gets the requested PDF/A assessment profile.</summary>
    /// <returns>The archival profile, or <c>None</c>.</returns>
    public PdfArchivalProfile GetArchivalProfile()
    {
        return archivalProfile;
    }

    /// <summary>Gets the requested PDF/UA assessment profile.</summary>
    /// <returns>The accessibility profile, or <c>None</c>.</returns>
    public PdfAccessibilityProfile GetAccessibilityProfile()
    {
        return accessibilityProfile;
    }

    /// <summary>Reports whether requested PDF/A assessment found a font concern.</summary>
    /// <returns>True when at least one PDF/A concern exists.</returns>
    public bool HasPdfAEmbeddingConcerns()
    {
        int index = 0;

        while (index < concerns.Length)
        {
            if (concerns[index].IsPdfAConcern())
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    /// <summary>Reports whether requested PDF/UA assessment found a text concern.</summary>
    /// <returns>True when at least one PDF/UA concern exists.</returns>
    public bool HasPdfUaUnicodeConcerns()
    {
        int index = 0;

        while (index < concerns.Length)
        {
            if (concerns[index].IsPdfUaConcern())
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    /// <summary>Gets the number of requested-profile font concerns.</summary>
    /// <returns>The concern count.</returns>
    public int GetConcernCount()
    {
        return concerns.Length;
    }

    /// <summary>Gets one requested-profile font concern.</summary>
    /// <param name="index">The zero-based concern index.</param>
    /// <returns>The immutable concern.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the concern list.
    /// </exception>
    public PdfFontResourceConcern GetConcern(int index)
    {
        if (index < 0 || index >= concerns.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return concerns[index];
    }

    /// <summary>Copies every requested-profile font concern.</summary>
    /// <returns>A new array in stable concern-kind order.</returns>
    public PdfFontResourceConcern[] GetConcerns()
    {
        return CopyConcerns(concerns);
    }

    /// <summary>Reports whether this font-level assessment claims document conformance.</summary>
    /// <returns>Always false.</returns>
    public bool ClaimsDocumentConformance()
    {
        return false;
    }

    static int[] CopyIntegers(int[] values)
    {
        int[] copy = new int[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    static PdfFontResourceConcern[] CopyConcerns(
        PdfFontResourceConcern[] values)
    {
        PdfFontResourceConcern[] copy =
            new PdfFontResourceConcern[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}
