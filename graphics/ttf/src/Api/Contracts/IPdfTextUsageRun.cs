namespace ttf;

/// <summary>Defines one fallback-resolved positioned PDF text run.</summary>
public interface IPdfTextUsageRun
{
    /// <summary>Gets the resource index used to select the PDF font.</summary>
    /// <returns>The zero-based document resource index.</returns>
    int GetResourceIndex();

    /// <summary>Gets the resolved immutable font instance.</summary>
    /// <returns>The instance used to shape and encode this run.</returns>
    FontInstance GetFontInstance();

    /// <summary>Gets the run's absolute source UTF-16 start.</summary>
    /// <returns>The zero-based source offset.</returns>
    int GetSourceUtf16Start();

    /// <summary>Gets the run's source UTF-16 length.</summary>
    /// <returns>The source code-unit length.</returns>
    int GetSourceUtf16Length();

    /// <summary>Gets the complete encoded and positioned run.</summary>
    /// <returns>The immutable PDF encoding result.</returns>
    PdfEncodedRun GetEncodedRun();

    /// <summary>Copies the fixed-width PDF character codes.</summary>
    /// <returns>The codes in visual glyph order.</returns>
    byte[] GetCodes();

    /// <summary>Copies all positioned glyphs.</summary>
    /// <returns>The code, source-range, advance, and offset data.</returns>
    PdfPositionedGlyph[] GetPositionedGlyphs();

    /// <summary>Copies all suggested <c>TJ</c> adjustments.</summary>
    /// <returns>The nonzero text adjustments.</returns>
    PdfTjAdjustment[] GetTjAdjustments();
}
