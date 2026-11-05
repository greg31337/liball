namespace ttf;

/// <summary>Defines document-level fallback-aware PDF font usage operations.</summary>
public interface IPdfTextUsageBuilder
{
    /// <summary>Shapes and records source text using default shaping options.</summary>
    /// <param name="text">The strict UTF-16 source text.</param>
    /// <param name="request">The requested font family and style.</param>
    /// <returns>The shaped, positioned PDF text operation.</returns>
    PdfTextUsageResult AddText(string text, FontRequest request);

    /// <summary>Shapes and records source text using explicit shaping options.</summary>
    /// <param name="text">The source UTF-16 text.</param>
    /// <param name="request">The requested font family and style.</param>
    /// <param name="options">The shaping controls.</param>
    /// <returns>The shaped, positioned PDF text operation.</returns>
    PdfTextUsageResult AddTextWithOptions(
        string text,
        FontRequest request,
        TextShapingOptions options);

    /// <summary>Records already fallback-shaped text without reshaping it.</summary>
    /// <param name="shapingResult">The fallback-shaped visual runs.</param>
    /// <returns>The positioned PDF text operation.</returns>
    PdfTextUsageResult AddShapingResult(TextShapingResult shapingResult);

    /// <summary>Gets the number of distinct font resources currently allocated.</summary>
    /// <returns>The resource count in first-use order.</returns>
    int GetResourceCount();

    /// <summary>Finalizes every allocated font resource exactly once.</summary>
    /// <returns>Resources in the order used by text-operation run indexes.</returns>
    PdfFontResource[] FinalizeFonts();

    /// <summary>Reports whether all allocated resources finalized successfully.</summary>
    /// <returns>True after successful finalization.</returns>
    bool IsFinalized();

    /// <summary>Gets one successfully finalized resource.</summary>
    /// <param name="resourceIndex">The zero-based resource index.</param>
    /// <returns>The finalized resource.</returns>
    PdfFontResource GetFinalizedResource(int resourceIndex);
}
