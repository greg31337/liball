namespace ttf;

/// <summary>Provides PDF appearance guidance for a selected color glyph.</summary>
public enum PdfColorGlyphAppearance
{
    /// <summary>No supported appearance can be emitted.</summary>
    None = 0,

    /// <summary>Use the ordinary embedded-font text glyph without a separate appearance.</summary>
    UseTextGlyph = 1,

    /// <summary>Emit a separate vector appearance from the COLR paint graph.</summary>
    DrawVectorAppearance = 2,

    /// <summary>Emit a separate appearance from the sanitized SVG document.</summary>
    DrawSvgAppearance = 3,

    /// <summary>Emit a separate image appearance from an embedded bitmap.</summary>
    DrawImageAppearance = 4,

    /// <summary>Emit an image appearance and also draw the ordinary outline.</summary>
    DrawImageAndOutlineAppearance = 5
}
