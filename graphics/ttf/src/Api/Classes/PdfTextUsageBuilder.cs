namespace ttf;

/// <summary>Coordinates fallback shaping and document-local PDF font resources.</summary>
/// <remarks>
/// Resolved font instances share a resource when their stable instance and PDF
/// option keys match. A different resolved font receives an independent resource.
/// Resource indexes remain stable from first use through finalization.
/// </remarks>
public class PdfTextUsageBuilder : IPdfTextUsageBuilder
{
    PdfTextUsageEngine engine;

    /// <summary>Creates a fallback-aware PDF text usage builder.</summary>
    /// <param name="fontSet">The application-owned fallback font set.</param>
    /// <param name="options">The PDF resource and compatibility options.</param>
    /// <exception cref="System.ArgumentNullException">
    /// <c>fontSet</c> or <c>options</c> is null.
    /// </exception>
    public PdfTextUsageBuilder(FontSet fontSet, PdfFontOptions options)
    {
        engine = new PdfTextUsageEngine(fontSet, options);
    }

    /// <summary>Shapes and records source text using default shaping options.</summary>
    /// <param name="text">The strict UTF-16 source text.</param>
    /// <param name="request">The requested font family and style.</param>
    /// <returns>The shaped, positioned PDF text operation.</returns>
    public PdfTextUsageResult AddText(string text, FontRequest request)
    {
        return engine.AddText(text, request);
    }

    /// <summary>Shapes and records source text using explicit shaping options.</summary>
    /// <param name="text">The source UTF-16 text.</param>
    /// <param name="request">The requested font family and style.</param>
    /// <param name="options">The shaping controls.</param>
    /// <returns>The shaped, positioned PDF text operation.</returns>
    public PdfTextUsageResult AddTextWithOptions(
        string text,
        FontRequest request,
        TextShapingOptions options)
    {
        return engine.AddTextWithOptions(text, request, options);
    }

    /// <summary>Records already fallback-shaped text without reshaping it.</summary>
    /// <param name="shapingResult">The fallback-shaped visual runs.</param>
    /// <returns>The positioned PDF text operation.</returns>
    public PdfTextUsageResult AddShapingResult(
        TextShapingResult shapingResult)
    {
        return engine.AddShapingResult(shapingResult);
    }

    /// <summary>Gets the number of distinct font resources currently allocated.</summary>
    /// <returns>The resource count in first-use order.</returns>
    public int GetResourceCount()
    {
        return engine.GetResourceCount();
    }

    /// <summary>Finalizes every allocated font resource exactly once.</summary>
    /// <returns>Resources in the order used by text-operation run indexes.</returns>
    public PdfFontResource[] FinalizeFonts()
    {
        return engine.FinalizeFonts();
    }

    /// <summary>Reports whether all allocated resources finalized successfully.</summary>
    /// <returns>True after successful finalization.</returns>
    public bool IsFinalized()
    {
        return engine.IsFinalized();
    }

    /// <summary>Gets one successfully finalized resource.</summary>
    /// <param name="resourceIndex">The zero-based resource index.</param>
    /// <returns>The finalized resource.</returns>
    public PdfFontResource GetFinalizedResource(int resourceIndex)
    {
        return engine.GetFinalizedResource(resourceIndex);
    }
}
