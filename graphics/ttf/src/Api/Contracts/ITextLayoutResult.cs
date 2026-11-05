namespace ttf;

/// <summary>Defines one immutable width-specific text layout snapshot.</summary>
public interface ITextLayoutResult
{
    /// <summary>Gets the retained source text.</summary>
    /// <returns>The original source text.</returns>
    string GetSourceText();

    /// <summary>Gets the maximum inline advance used for this reflow.</summary>
    /// <returns>The requested maximum advance.</returns>
    double GetMaximumAdvance();

    /// <summary>Gets the line writing mode.</summary>
    /// <returns>The writing mode.</returns>
    TextWritingMode GetWritingMode();

    /// <summary>Gets the produced line count.</summary>
    /// <returns>The line count.</returns>
    int GetLineCount();

    /// <summary>Gets one positioned line.</summary>
    /// <param name="index">The zero-based line index.</param>
    /// <returns>The immutable line result.</returns>
    TextLineResult GetLine(int index);

    /// <summary>Gets one line's source UTF-16 start.</summary>
    /// <param name="index">The zero-based line index.</param>
    /// <returns>The source start.</returns>
    int GetLineUtf16Start(int index);

    /// <summary>Gets one line's source UTF-16 length.</summary>
    /// <param name="index">The zero-based line index.</param>
    /// <returns>The source length.</returns>
    int GetLineUtf16Length(int index);

    /// <summary>Gets one line's positioned inline advance.</summary>
    /// <param name="index">The zero-based line index.</param>
    /// <returns>The line advance.</returns>
    double GetLineAdvance(int index);

    /// <summary>Gets the exact paragraph advance bounds.</summary>
    /// <returns>The union of line advance bounds.</returns>
    TextInkBounds GetAdvanceBounds();

    /// <summary>Gets the exact paragraph ink bounds.</summary>
    /// <returns>The union of positioned glyph ink.</returns>
    TextInkBounds GetInkBounds();

    /// <summary>Gets the flattened positioned-run count.</summary>
    /// <returns>The run count.</returns>
    int GetRunCount();

    /// <summary>Gets one flattened run in visual line order.</summary>
    /// <param name="visualIndex">The visual run index.</param>
    /// <returns>The positioned run.</returns>
    PositionedRunResult GetVisualRun(int visualIndex);

    /// <summary>Gets one flattened run in logical source order.</summary>
    /// <param name="logicalIndex">The logical run index.</param>
    /// <returns>The positioned run.</returns>
    PositionedRunResult GetLogicalRun(int logicalIndex);

    /// <summary>Maps a logical paragraph run index to visual order.</summary>
    /// <param name="logicalIndex">The logical run index.</param>
    /// <returns>The visual run index.</returns>
    int GetVisualRunIndex(int logicalIndex);

    /// <summary>Maps a visual paragraph run index to logical order.</summary>
    /// <param name="visualIndex">The visual run index.</param>
    /// <returns>The logical run index.</returns>
    int GetLogicalRunIndex(int visualIndex);

    /// <summary>Gets the distinct resolved font-instance count.</summary>
    /// <returns>The font count.</returns>
    int GetResolvedFontCount();

    /// <summary>Gets one distinct resolved font instance.</summary>
    /// <param name="index">The zero-based font index.</param>
    /// <returns>The resolved font instance.</returns>
    FontInstance GetResolvedFont(int index);

    /// <summary>Gets the shaping-safe caret-stop count.</summary>
    /// <returns>The caret-stop count.</returns>
    int GetCaretStopCount();

    /// <summary>Gets one shaping-safe logical-to-visual caret mapping.</summary>
    /// <param name="index">The zero-based caret-stop index.</param>
    /// <returns>The immutable caret stop.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    TextCaretStopResult GetCaretStop(int index);

    /// <summary>Reports whether a UTF-16 index is a visible shaping-safe caret boundary.</summary>
    /// <param name="utf16Index">The source UTF-16 index.</param>
    /// <returns>True when at least one caret stop exists at the index.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>utf16Index</c> is outside the source.</exception>
    bool IsCaretBoundary(int utf16Index);

    /// <summary>Finds the nearest shaping-safe caret on the nearest line.</summary>
    /// <param name="x">The finite visual x coordinate.</param>
    /// <param name="y">The finite visual y coordinate.</param>
    /// <returns>The hit-test result.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">A coordinate is non-finite.</exception>
    TextHitTestResult HitTest(double x, double y);

    /// <summary>Builds visual rectangles for a shaping-safe source range.</summary>
    /// <param name="utf16Start">The source UTF-16 start.</param>
    /// <param name="utf16Length">The source UTF-16 length.</param>
    /// <returns>Rectangles in visual line order.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">The range is outside the source.</exception>
    /// <exception cref="System.ArgumentException">An endpoint is not a visible shaping-safe boundary.</exception>
    TextSelectionRectangleResult[] GetSelectionRectangles(
        int utf16Start,
        int utf16Length);

    /// <summary>Reports whether maximum-line trimming truncated the text.</summary>
    /// <returns>True when text was truncated.</returns>
    bool IsTruncated();

    /// <summary>Gets the first omitted cluster when truncated.</summary>
    /// <returns>The truncation cluster boundary.</returns>
    int GetTruncationClusterIndex();

    /// <summary>Gets the first omitted source UTF-16 offset when truncated.</summary>
    /// <returns>The truncation source offset.</returns>
    int GetTruncationUtf16Index();
}
