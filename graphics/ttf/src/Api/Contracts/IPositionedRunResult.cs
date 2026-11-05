namespace ttf;

/// <summary>Defines one immutable positioned shaping-run fragment.</summary>
public interface IPositionedRunResult
{
    /// <summary>Gets the containing line index.</summary><returns>The line index.</returns>
    int GetLineIndex();
    /// <summary>Gets the prepared source-run index.</summary><returns>The source-run index.</returns>
    int GetSourceRunIndex();
    /// <summary>Gets the logical run index on its line.</summary><returns>The logical index.</returns>
    int GetLogicalIndex();
    /// <summary>Gets the visual run index on its line.</summary><returns>The visual index.</returns>
    int GetVisualIndex();
    /// <summary>Gets the first logical cluster.</summary><returns>The cluster index.</returns>
    int GetClusterStart();
    /// <summary>Gets the exclusive logical cluster end.</summary><returns>The cluster boundary.</returns>
    int GetClusterEnd();
    /// <summary>Gets the cluster count.</summary><returns>The cluster count.</returns>
    int GetClusterCount();
    /// <summary>Gets the source UTF-16 start.</summary><returns>The source offset.</returns>
    int GetUtf16Start();
    /// <summary>Gets the source UTF-16 length.</summary><returns>The source length.</returns>
    int GetUtf16Length();
    /// <summary>Gets the resolved bidi level.</summary><returns>The bidi level.</returns>
    int GetBidiLevel();
    /// <summary>Gets the shaping direction.</summary><returns>The run direction.</returns>
    ShapingDirection GetDirection();
    /// <summary>Gets the positioned x coordinate.</summary><returns>The x coordinate.</returns>
    double GetX();
    /// <summary>Gets the positioned y coordinate.</summary><returns>The y coordinate.</returns>
    double GetY();
    /// <summary>Gets the natural inline advance.</summary><returns>The natural advance.</returns>
    double GetNaturalAdvance();
    /// <summary>Gets the positioned inline advance.</summary><returns>The adjusted advance.</returns>
    double GetPositionedAdvance();
    /// <summary>Gets the run ascent.</summary><returns>The ascent.</returns>
    double GetAscent();
    /// <summary>Gets the run descent.</summary><returns>The descent.</returns>
    double GetDescent();
    /// <summary>Gets the run line gap.</summary><returns>The line gap.</returns>
    double GetLineGap();
    /// <summary>Gets the exact positioned ink bounds.</summary><returns>The ink bounds.</returns>
    TextInkBounds GetInkBounds();
    /// <summary>Reports whether resolved font data exists.</summary><returns>True when font data exists.</returns>
    bool HasResolvedFont();
    /// <summary>Gets the resolved font instance.</summary><returns>The font instance.</returns>
    FontInstance GetFontInstance();
    /// <summary>Gets the resolved font face.</summary><returns>The font face.</returns>
    FontFace GetFontFace();
    /// <summary>Gets the positioned glyph count.</summary><returns>The glyph count.</returns>
    int GetGlyphCount();
    /// <summary>Gets one glyph in visual order.</summary><param name="visualIndex">The visual glyph index.</param><returns>The glyph.</returns>
    PositionedGlyphResult GetVisualGlyph(int visualIndex);
    /// <summary>Gets one glyph in logical order.</summary><param name="logicalIndex">The logical glyph index.</param><returns>The glyph.</returns>
    PositionedGlyphResult GetLogicalGlyph(int logicalIndex);
    /// <summary>Maps a logical glyph index to visual order.</summary><param name="logicalIndex">The logical glyph index.</param><returns>The visual index.</returns>
    int GetVisualGlyphIndex(int logicalIndex);
    /// <summary>Maps a visual glyph index to logical order.</summary><param name="visualIndex">The visual glyph index.</param><returns>The logical index.</returns>
    int GetLogicalGlyphIndex(int visualIndex);
}
