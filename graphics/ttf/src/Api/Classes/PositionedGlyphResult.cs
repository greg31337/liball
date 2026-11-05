namespace ttf;

/// <summary>Represents one immutable glyph positioned by text layout.</summary>
public class PositionedGlyphResult : IPositionedGlyphResult
{
    PositionedGlyph glyph;

    internal PositionedGlyphResult(PositionedGlyph glyph)
    {
        this.glyph = glyph;
    }

    /// <summary>Gets the font glyph identifier.</summary><returns>The glyph ID.</returns>
    public int GetGlyphId() { return glyph.GetGlyphId(); }
    /// <summary>Gets the first source cluster.</summary><returns>The cluster index.</returns>
    public int GetClusterStart() { return glyph.GetClusterIndex(); }
    /// <summary>Gets the exclusive source cluster end.</summary><returns>The cluster boundary.</returns>
    public int GetClusterEnd() { return glyph.GetClusterEnd(); }
    /// <summary>Gets the attributed source UTF-16 start.</summary><returns>The source offset.</returns>
    public int GetUtf16Start() { return glyph.GetUtf16Start(); }
    /// <summary>Gets the attributed source UTF-16 length.</summary><returns>The source length.</returns>
    public int GetUtf16Length() { return glyph.GetUtf16Length(); }
    /// <summary>Gets the logical glyph index in its run.</summary><returns>The logical index.</returns>
    public int GetLogicalIndex() { return glyph.GetLogicalIndex(); }
    /// <summary>Gets the visual glyph index in its run.</summary><returns>The visual index.</returns>
    public int GetVisualIndex() { return glyph.GetVisualIndex(); }
    /// <summary>Gets the positioned x coordinate.</summary><returns>The x coordinate.</returns>
    public double GetX() { return glyph.GetX(); }
    /// <summary>Gets the positioned y coordinate.</summary><returns>The y coordinate.</returns>
    public double GetY() { return glyph.GetY(); }
    /// <summary>Gets the horizontal glyph advance.</summary><returns>The x advance.</returns>
    public double GetXAdvance() { return glyph.GetXAdvance(); }
    /// <summary>Gets the vertical glyph advance.</summary><returns>The y advance.</returns>
    public double GetYAdvance() { return glyph.GetYAdvance(); }
    /// <summary>Gets the layout justification adjustment.</summary><returns>The inline adjustment.</returns>
    public double GetJustificationAdvance() { return glyph.GetJustificationAdvance(); }
    /// <summary>Gets the exact positioned ink bounds.</summary><returns>The ink bounds.</returns>
    public TextInkBounds GetInkBounds() { return glyph.GetInkBounds(); }
    /// <summary>Gets the resolved font instance.</summary><returns>The font instance.</returns>
    public FontInstance GetFontInstance() { return glyph.GetFontInstance(); }
    /// <summary>Gets the resolved font face.</summary><returns>The font face.</returns>
    public FontFace GetFontFace() { return glyph.GetFontFace(); }
}
