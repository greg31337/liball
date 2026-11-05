namespace ttf;

/// <summary>Contains every supported representation resolved for one glyph.</summary>
public class ColorGlyph : IColorGlyph
{
    int glyphId;
    int paletteIndex;
    ColorGlyphRepresentation selectedRepresentation;
    PdfColorGlyphAppearance pdfAppearance;
    ColorPaintGraph? paintGraph;
    ColorBitmapStrike[] bitmapStrikes;
    ColorSvgDocument? svgDocument;
    GlyphOutline? monochromeOutline;
    int selectedBitmapStrikeIndex;
    ColorGlyphOptions resolutionOptions;
    FontOperationOptions operationOptions;
    FontDiagnostic[] diagnostics;

    internal ColorGlyph(
        int glyphId,
        int paletteIndex,
        ColorGlyphRepresentation selectedRepresentation,
        PdfColorGlyphAppearance pdfAppearance,
        ColorPaintGraph? paintGraph,
        ColorBitmapStrike[] bitmapStrikes,
        ColorSvgDocument? svgDocument,
        GlyphOutline? monochromeOutline,
        int selectedBitmapStrikeIndex,
        ColorGlyphOptions resolutionOptions,
        FontDiagnostic[] diagnostics)
    {
        this.glyphId = glyphId;
        this.paletteIndex = paletteIndex;
        this.selectedRepresentation = selectedRepresentation;
        this.pdfAppearance = pdfAppearance;
        this.paintGraph = paintGraph;
        this.bitmapStrikes = bitmapStrikes;
        this.svgDocument = svgDocument;
        this.monochromeOutline = monochromeOutline;
        this.selectedBitmapStrikeIndex = selectedBitmapStrikeIndex;
        this.resolutionOptions = resolutionOptions.Copy();
        operationOptions = resolutionOptions.GetOperationOptions();
        this.diagnostics = CopyDiagnostics(diagnostics);
    }

    /// <summary>Gets the resolved glyph identifier.</summary>
    /// <returns>The glyph identifier.</returns>
    public int GetGlyphId() { return glyphId; }

    /// <summary>Gets the selected CPAL palette index.</summary>
    /// <returns>The palette index.</returns>
    public int GetPaletteIndex() { return paletteIndex; }

    /// <summary>Gets the representation selected by the resolver.</summary>
    /// <returns>The selected representation, or none.</returns>
    public ColorGlyphRepresentation GetSelectedRepresentation()
    {
        return selectedRepresentation;
    }

    /// <summary>Gets guidance for emitting this selection into a PDF appearance.</summary>
    /// <returns>The appearance guidance.</returns>
    public PdfColorGlyphAppearance GetPdfAppearanceGuidance() { return pdfAppearance; }

    /// <summary>Gets the normalized COLR paint graph when available.</summary>
    /// <returns>The graph, or null when this glyph has no supported COLR description.</returns>
    public ColorPaintGraph? GetPaintGraph() { return paintGraph; }

    /// <summary>Copies all resolved bitmap strikes for this glyph.</summary>
    /// <returns>A new array in deterministic format and table order.</returns>
    public ColorBitmapStrike[] GetBitmapStrikes()
    {
        return bitmapStrikes.AsSpan().ToArray();
    }

    /// <summary>Gets the bitmap strike selected by the resolver.</summary>
    /// <returns>The selected strike, or null when a bitmap was not selected.</returns>
    public ColorBitmapStrike? GetSelectedBitmapStrike()
    {
        if (selectedBitmapStrikeIndex < 0)
        {
            return null;
        }

        return bitmapStrikes[selectedBitmapStrikeIndex];
    }

    /// <summary>Gets the sanitized SVG document when available.</summary>
    /// <returns>The SVG document, or null.</returns>
    public ColorSvgDocument? GetSvgDocument() { return svgDocument; }

    /// <summary>Gets the ordinary outline usable as a monochrome fallback.</summary>
    /// <returns>The glyph outline, or null when the face has no supported outlines.</returns>
    public GlyphOutline? GetMonochromeOutline() { return monochromeOutline; }

    /// <summary>Gets guidance for using the ordinary monochrome outline.</summary>
    /// <returns>The fallback guidance for the selected representation.</returns>
    public ColorMonochromeFallbackGuidance GetMonochromeFallbackGuidance()
    {
        if (selectedRepresentation == ColorGlyphRepresentation.MonochromeOutline)
        {
            return ColorMonochromeFallbackGuidance.Selected;
        }
        if (selectedBitmapStrikeIndex >= 0 &&
            bitmapStrikes[selectedBitmapStrikeIndex].ShouldDrawOutline())
        {
            return monochromeOutline == null ?
                ColorMonochromeFallbackGuidance.RequiredButUnavailable :
                ColorMonochromeFallbackGuidance.Required;
        }
        return monochromeOutline == null ?
            ColorMonochromeFallbackGuidance.Unavailable :
            ColorMonochromeFallbackGuidance.Available;
    }

    /// <summary>Gets the palette and representation controls used for resolution.</summary>
    /// <returns>An independent mutable copy.</returns>
    public ColorGlyphOptions GetResolutionOptions()
    {
        return resolutionOptions.Copy();
    }

    /// <summary>Gets the shared operation controls used for resolution.</summary>
    /// <returns>An independent mutable copy.</returns>
    public FontOperationOptions GetOperationOptions()
    {
        return operationOptions.Copy();
    }

    /// <summary>Gets the deterministic color-resolution diagnostic count.</summary>
    /// <returns>The diagnostic count.</returns>
    public int GetDiagnosticCount() { return diagnostics.Length; }

    /// <summary>Gets one color-resolution diagnostic.</summary>
    /// <param name="index">The zero-based diagnostic index.</param>
    /// <returns>The diagnostic.</returns>
    public FontDiagnostic GetDiagnostic(int index)
    {
        if (index < 0 || index >= diagnostics.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return diagnostics[index];
    }

    static FontDiagnostic[] CopyDiagnostics(FontDiagnostic[] values)
    {
        FontDiagnostic[] copy = new FontDiagnostic[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}
