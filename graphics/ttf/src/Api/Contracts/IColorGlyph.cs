namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="ColorGlyph"/>.</summary>
public interface IColorGlyph
{
    /// <summary>Gets the resolved glyph identifier.</summary>
    /// <returns>The glyph identifier.</returns>
    int GetGlyphId();

    /// <summary>Gets the selected CPAL palette index.</summary>
    /// <returns>The palette index.</returns>
    int GetPaletteIndex();

    /// <summary>Gets the representation selected by the resolver.</summary>
    /// <returns>The selected representation, or none.</returns>
    ColorGlyphRepresentation GetSelectedRepresentation();

    /// <summary>Gets guidance for emitting this selection into a PDF appearance.</summary>
    /// <returns>The appearance guidance.</returns>
    PdfColorGlyphAppearance GetPdfAppearanceGuidance();

    /// <summary>Gets the normalized COLR paint graph when available.</summary>
    /// <returns>The graph, or null when this glyph has no supported COLR description.</returns>
    ColorPaintGraph? GetPaintGraph();

    /// <summary>Copies all resolved bitmap strikes for this glyph.</summary>
    /// <returns>A new array in deterministic format and table order.</returns>
    ColorBitmapStrike[] GetBitmapStrikes();

    /// <summary>Gets the bitmap strike selected by the resolver.</summary>
    /// <returns>The selected strike, or null when a bitmap was not selected.</returns>
    ColorBitmapStrike? GetSelectedBitmapStrike();

    /// <summary>Gets the sanitized SVG document when available.</summary>
    /// <returns>The SVG document, or null.</returns>
    ColorSvgDocument? GetSvgDocument();

    /// <summary>Gets the ordinary outline usable as a monochrome fallback.</summary>
    /// <returns>The glyph outline, or null when the face has no supported outlines.</returns>
    GlyphOutline? GetMonochromeOutline();

    /// <summary>Gets guidance for using the ordinary monochrome outline.</summary>
    /// <returns>The fallback guidance for the selected representation.</returns>
    ColorMonochromeFallbackGuidance GetMonochromeFallbackGuidance();

    /// <summary>Gets the palette and representation controls used for resolution.</summary>
    /// <returns>An independent mutable copy.</returns>
    ColorGlyphOptions GetResolutionOptions();

    /// <summary>Gets the shared operation controls used for resolution.</summary>
    /// <returns>An independent mutable copy.</returns>
    FontOperationOptions GetOperationOptions();

    /// <summary>Gets the deterministic color-resolution diagnostic count.</summary>
    /// <returns>The diagnostic count.</returns>
    int GetDiagnosticCount();

    /// <summary>Gets one color-resolution diagnostic.</summary>
    /// <param name="index">The zero-based diagnostic index.</param>
    /// <returns>The diagnostic.</returns>
    FontDiagnostic GetDiagnostic(int index);
}
