namespace ttf;

/// <summary>Represents one immutable positioned shaping-run fragment.</summary>
public class PositionedRunResult : IPositionedRunResult
{
    PositionedRun run;
    int lineIndex;

    internal PositionedRunResult(PositionedRun run, int lineIndex)
    {
        this.run = run;
        this.lineIndex = lineIndex;
    }

    /// <summary>Gets the containing line index.</summary><returns>The line index.</returns>
    public int GetLineIndex() { return lineIndex; }
    /// <summary>Gets the prepared source-run index.</summary><returns>The source-run index.</returns>
    public int GetSourceRunIndex() { return run.GetSourceRunIndex(); }
    /// <summary>Gets the logical run index on its line.</summary><returns>The logical index.</returns>
    public int GetLogicalIndex() { return run.GetLogicalIndex(); }
    /// <summary>Gets the visual run index on its line.</summary><returns>The visual index.</returns>
    public int GetVisualIndex() { return run.GetVisualIndex(); }
    /// <summary>Gets the first logical cluster.</summary><returns>The cluster index.</returns>
    public int GetClusterStart() { return run.GetClusterStart(); }
    /// <summary>Gets the exclusive logical cluster end.</summary><returns>The cluster boundary.</returns>
    public int GetClusterEnd() { return run.GetClusterEnd(); }
    /// <summary>Gets the cluster count.</summary><returns>The cluster count.</returns>
    public int GetClusterCount() { return run.GetClusterEnd() - run.GetClusterStart(); }
    /// <summary>Gets the source UTF-16 start.</summary><returns>The source offset.</returns>
    public int GetUtf16Start() { return run.GetUtf16Start(); }
    /// <summary>Gets the source UTF-16 length.</summary><returns>The source length.</returns>
    public int GetUtf16Length() { return run.GetUtf16Length(); }
    /// <summary>Gets the resolved bidi level.</summary><returns>The bidi level.</returns>
    public int GetBidiLevel() { return run.GetBidiLevel(); }
    /// <summary>Gets the shaping direction.</summary><returns>The run direction.</returns>
    public ShapingDirection GetDirection() { return run.GetDirection(); }
    /// <summary>Gets the positioned x coordinate.</summary><returns>The x coordinate.</returns>
    public double GetX() { return run.GetX(); }
    /// <summary>Gets the positioned y coordinate.</summary><returns>The y coordinate.</returns>
    public double GetY() { return run.GetY(); }
    /// <summary>Gets the natural inline advance.</summary><returns>The natural advance.</returns>
    public double GetNaturalAdvance() { return run.GetNaturalAdvance(); }
    /// <summary>Gets the positioned inline advance.</summary><returns>The adjusted advance.</returns>
    public double GetPositionedAdvance() { return run.GetPositionedAdvance(); }
    /// <summary>Gets the run ascent.</summary><returns>The ascent.</returns>
    public double GetAscent() { return run.GetAscent(); }
    /// <summary>Gets the run descent.</summary><returns>The descent.</returns>
    public double GetDescent() { return run.GetDescent(); }
    /// <summary>Gets the run line gap.</summary><returns>The line gap.</returns>
    public double GetLineGap() { return run.GetLineGap(); }
    /// <summary>Gets the exact positioned ink bounds.</summary><returns>The ink bounds.</returns>
    public TextInkBounds GetInkBounds() { return run.GetInkBounds(); }
    /// <summary>Reports whether resolved font data exists.</summary><returns>True when font data exists.</returns>
    public bool HasResolvedFont() { return run.HasResolvedFont(); }
    /// <summary>Gets the resolved font instance.</summary><returns>The font instance.</returns>
    public FontInstance GetFontInstance() { return run.GetFontInstance(); }
    /// <summary>Gets the resolved font face.</summary><returns>The font face.</returns>
    public FontFace GetFontFace() { return run.GetFontFace(); }
    /// <summary>Gets the positioned glyph count.</summary><returns>The glyph count.</returns>
    public int GetGlyphCount() { return run.GetGlyphCount(); }

    /// <summary>Gets one glyph in visual order.</summary><param name="visualIndex">The visual glyph index.</param><returns>The glyph.</returns>
    public PositionedGlyphResult GetVisualGlyph(int visualIndex)
    {
        return new PositionedGlyphResult(run.GetGlyph(visualIndex));
    }

    /// <summary>Gets one glyph in logical order.</summary><param name="logicalIndex">The logical glyph index.</param><returns>The glyph.</returns>
    public PositionedGlyphResult GetLogicalGlyph(int logicalIndex)
    {
        return new PositionedGlyphResult(run.GetLogicalGlyph(logicalIndex));
    }

    /// <summary>Maps a logical glyph index to visual order.</summary><param name="logicalIndex">The logical glyph index.</param><returns>The visual index.</returns>
    public int GetVisualGlyphIndex(int logicalIndex)
    {
        return run.GetVisualGlyphIndex(logicalIndex);
    }

    /// <summary>Maps a visual glyph index to logical order.</summary><param name="visualIndex">The visual glyph index.</param><returns>The logical index.</returns>
    public int GetLogicalGlyphIndex(int visualIndex)
    {
        return run.GetLogicalGlyphIndex(visualIndex);
    }
}
