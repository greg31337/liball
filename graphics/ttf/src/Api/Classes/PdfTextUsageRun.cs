namespace ttf;

/// <summary>Contains one fallback-resolved positioned PDF text run.</summary>
public class PdfTextUsageRun : IPdfTextUsageRun
{
    int resourceIndex;
    FontInstance fontInstance;
    int sourceUtf16Start;
    int sourceUtf16Length;
    PdfEncodedRun encodedRun;

    internal PdfTextUsageRun(
        int resourceIndex,
        FontInstance fontInstance,
        int sourceUtf16Start,
        int sourceUtf16Length,
        PdfEncodedRun encodedRun)
    {
        this.resourceIndex = resourceIndex;
        this.fontInstance = fontInstance;
        this.sourceUtf16Start = sourceUtf16Start;
        this.sourceUtf16Length = sourceUtf16Length;
        this.encodedRun = encodedRun;
    }

    /// <summary>Gets the resource index used to select the PDF font.</summary>
    /// <returns>The zero-based document resource index.</returns>
    public int GetResourceIndex() { return resourceIndex; }

    /// <summary>Gets the resolved immutable font instance.</summary>
    /// <returns>The instance used to shape and encode this run.</returns>
    public FontInstance GetFontInstance() { return fontInstance; }

    /// <summary>Gets the run's absolute source UTF-16 start.</summary>
    /// <returns>The zero-based source offset.</returns>
    public int GetSourceUtf16Start() { return sourceUtf16Start; }

    /// <summary>Gets the run's source UTF-16 length.</summary>
    /// <returns>The source code-unit length.</returns>
    public int GetSourceUtf16Length() { return sourceUtf16Length; }

    /// <summary>Gets the complete encoded and positioned run.</summary>
    /// <returns>The immutable PDF encoding result.</returns>
    public PdfEncodedRun GetEncodedRun() { return encodedRun; }

    /// <summary>Copies the fixed-width PDF character codes.</summary>
    /// <returns>The codes in visual glyph order.</returns>
    public byte[] GetCodes() { return encodedRun.GetCodes(); }

    /// <summary>Copies all positioned glyphs.</summary>
    /// <returns>The code, source-range, advance, and offset data.</returns>
    public PdfPositionedGlyph[] GetPositionedGlyphs()
    {
        return encodedRun.GetGlyphs();
    }

    /// <summary>Copies all suggested <c>TJ</c> adjustments.</summary>
    /// <returns>The nonzero text adjustments.</returns>
    public PdfTjAdjustment[] GetTjAdjustments()
    {
        return encodedRun.GetTextAdjustments();
    }
}
