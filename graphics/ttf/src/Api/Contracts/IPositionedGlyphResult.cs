namespace ttf;

/// <summary>Defines one immutable glyph positioned by text layout.</summary>
public interface IPositionedGlyphResult
{
    /// <summary>Gets the font glyph identifier.</summary><returns>The glyph ID.</returns>
    int GetGlyphId();
    /// <summary>Gets the first source cluster.</summary><returns>The cluster index.</returns>
    int GetClusterStart();
    /// <summary>Gets the exclusive source cluster end.</summary><returns>The cluster boundary.</returns>
    int GetClusterEnd();
    /// <summary>Gets the attributed source UTF-16 start.</summary><returns>The source offset.</returns>
    int GetUtf16Start();
    /// <summary>Gets the attributed source UTF-16 length.</summary><returns>The source length.</returns>
    int GetUtf16Length();
    /// <summary>Gets the logical glyph index in its run.</summary><returns>The logical index.</returns>
    int GetLogicalIndex();
    /// <summary>Gets the visual glyph index in its run.</summary><returns>The visual index.</returns>
    int GetVisualIndex();
    /// <summary>Gets the positioned x coordinate.</summary><returns>The x coordinate.</returns>
    double GetX();
    /// <summary>Gets the positioned y coordinate.</summary><returns>The y coordinate.</returns>
    double GetY();
    /// <summary>Gets the horizontal glyph advance.</summary><returns>The x advance.</returns>
    double GetXAdvance();
    /// <summary>Gets the vertical glyph advance.</summary><returns>The y advance.</returns>
    double GetYAdvance();
    /// <summary>Gets the layout justification adjustment.</summary><returns>The inline adjustment.</returns>
    double GetJustificationAdvance();
    /// <summary>Gets the exact positioned ink bounds.</summary><returns>The ink bounds.</returns>
    TextInkBounds GetInkBounds();
    /// <summary>Gets the resolved font instance.</summary><returns>The font instance.</returns>
    FontInstance GetFontInstance();
    /// <summary>Gets the resolved font face.</summary><returns>The font face.</returns>
    FontFace GetFontFace();
}
