namespace ttf;

/// <summary>Represents one immutable width-specific text layout snapshot.</summary>
public class TextLayoutResult : ITextLayoutResult
{
    TextLayout layout;

    internal TextLayoutResult(TextLayout layout)
    {
        this.layout = layout;
    }

    /// <summary>Gets the retained source text.</summary>
    /// <returns>The original source text.</returns>
    public string GetSourceText()
    {
        string? value = layout.GetSourceText();
        System.ArgumentNullException.ThrowIfNull(value);
        return value;
    }

    /// <summary>Gets the maximum inline advance used for this reflow.</summary>
    /// <returns>The requested maximum advance.</returns>
    public double GetMaximumAdvance() { return layout.GetMaximumAdvance(); }

    /// <summary>Gets the line writing mode.</summary>
    /// <returns>The writing mode.</returns>
    public TextWritingMode GetWritingMode() { return layout.GetWritingMode(); }

    /// <summary>Gets the produced line count.</summary>
    /// <returns>The line count.</returns>
    public int GetLineCount() { return layout.GetLineCount(); }

    /// <summary>Gets one positioned line.</summary>
    /// <param name="index">The zero-based line index.</param>
    /// <returns>The immutable line result.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    public TextLineResult GetLine(int index)
    {
        return new TextLineResult(layout.GetLine(index), index);
    }

    /// <summary>Gets one line's source UTF-16 start.</summary>
    /// <param name="index">The zero-based line index.</param>
    /// <returns>The source start.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    public int GetLineUtf16Start(int index)
    {
        return layout.GetLine(index).GetUtf16Start();
    }

    /// <summary>Gets one line's source UTF-16 length.</summary>
    /// <param name="index">The zero-based line index.</param>
    /// <returns>The source length.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    public int GetLineUtf16Length(int index)
    {
        return layout.GetLine(index).GetUtf16Length();
    }

    /// <summary>Gets one line's positioned inline advance.</summary>
    /// <param name="index">The zero-based line index.</param>
    /// <returns>The line advance.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    public double GetLineAdvance(int index)
    {
        return layout.GetLine(index).GetAdvance();
    }

    /// <summary>Gets the exact paragraph advance bounds.</summary>
    /// <returns>The union of line advance bounds.</returns>
    public TextInkBounds GetAdvanceBounds()
    {
        return layout.GetAdvanceBounds();
    }

    /// <summary>Gets the exact paragraph ink bounds.</summary>
    /// <returns>The union of positioned glyph ink.</returns>
    public TextInkBounds GetInkBounds()
    {
        return layout.GetInkBounds();
    }

    /// <summary>Gets the flattened positioned-run count.</summary>
    /// <returns>The run count.</returns>
    public int GetRunCount() { return layout.GetPositionedRunCount(); }

    /// <summary>Gets one flattened run in visual line order.</summary>
    /// <param name="visualIndex">The visual run index.</param>
    /// <returns>The positioned run.</returns>
    public PositionedRunResult GetVisualRun(int visualIndex)
    {
        PositionedRun run = layout.GetPositionedRun(visualIndex);
        return new PositionedRunResult(run, FindLineIndex(run));
    }

    /// <summary>Gets one flattened run in logical source order.</summary>
    /// <param name="logicalIndex">The logical run index.</param>
    /// <returns>The positioned run.</returns>
    public PositionedRunResult GetLogicalRun(int logicalIndex)
    {
        PositionedRun run = layout.GetLogicalRun(logicalIndex);
        return new PositionedRunResult(run, FindLineIndex(run));
    }

    /// <summary>Maps a logical paragraph run index to visual order.</summary>
    /// <param name="logicalIndex">The logical run index.</param>
    /// <returns>The visual run index.</returns>
    public int GetVisualRunIndex(int logicalIndex)
    {
        return layout.GetVisualRunIndex(logicalIndex);
    }

    /// <summary>Maps a visual paragraph run index to logical order.</summary>
    /// <param name="visualIndex">The visual run index.</param>
    /// <returns>The logical run index.</returns>
    public int GetLogicalRunIndex(int visualIndex)
    {
        return layout.GetLogicalRunIndex(visualIndex);
    }

    /// <summary>Gets the distinct resolved font-instance count.</summary>
    /// <returns>The font count.</returns>
    public int GetResolvedFontCount() { return layout.GetResolvedFontCount(); }

    /// <summary>Gets one distinct resolved font instance.</summary>
    /// <param name="index">The zero-based font index.</param>
    /// <returns>The resolved font instance.</returns>
    public FontInstance GetResolvedFont(int index)
    {
        return layout.GetResolvedFont(index);
    }

    /// <summary>Gets the shaping-safe caret-stop count.</summary>
    /// <returns>The caret-stop count.</returns>
    public int GetCaretStopCount() { return layout.GetCaretStopCount(); }

    /// <summary>Gets one shaping-safe logical-to-visual caret mapping.</summary>
    /// <param name="index">The zero-based caret-stop index.</param>
    /// <returns>The immutable caret stop.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    public TextCaretStopResult GetCaretStop(int index)
    {
        TextCaretStop stop = layout.GetCaretStop(index);
        return new TextCaretStopResult(
            stop,
            layout.GetLine(stop.GetLineIndex()));
    }

    /// <summary>Reports whether a UTF-16 index is a visible shaping-safe caret boundary.</summary>
    /// <param name="utf16Index">The source UTF-16 index.</param>
    /// <returns>True when at least one caret stop exists at the index.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>utf16Index</c> is outside the source.</exception>
    public bool IsCaretBoundary(int utf16Index)
    {
        return layout.IsCaretBoundary(utf16Index);
    }

    /// <summary>Finds the nearest shaping-safe caret on the nearest line.</summary>
    /// <param name="x">The finite visual x coordinate.</param>
    /// <param name="y">The finite visual y coordinate.</param>
    /// <returns>The hit-test result.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">A coordinate is non-finite.</exception>
    public TextHitTestResult HitTest(double x, double y)
    {
        TextHitTestData hit = layout.HitTest(x, y);
        return new TextHitTestResult(
            hit,
            layout.GetLine(hit.GetLineIndex()));
    }

    /// <summary>Builds visual rectangles for a shaping-safe source range.</summary>
    /// <param name="utf16Start">The source UTF-16 start.</param>
    /// <param name="utf16Length">The source UTF-16 length.</param>
    /// <returns>Rectangles in visual line order.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">The range is outside the source.</exception>
    /// <exception cref="System.ArgumentException">An endpoint is not a visible shaping-safe boundary.</exception>
    public TextSelectionRectangleResult[] GetSelectionRectangles(
        int utf16Start,
        int utf16Length)
    {
        TextSelectionRectangle[] rectangles = layout.GetSelection(
            utf16Start,
            utf16Length);
        TextSelectionRectangleResult[] results =
            new TextSelectionRectangleResult[rectangles.Length];
        int index = 0;
        while (index < rectangles.Length)
        {
            results[index] = new TextSelectionRectangleResult(
                rectangles[index]);
            index += 1;
        }
        return results;
    }

    /// <summary>Reports whether maximum-line trimming truncated the text.</summary>
    /// <returns>True when text was truncated.</returns>
    public bool IsTruncated() { return layout.IsTruncated(); }

    /// <summary>Gets the first omitted cluster when truncated.</summary>
    /// <returns>The truncation cluster boundary.</returns>
    /// <exception cref="System.InvalidOperationException">The layout is not truncated.</exception>
    public int GetTruncationClusterIndex()
    {
        return layout.GetTruncationClusterIndex();
    }

    /// <summary>Gets the first omitted source UTF-16 offset when truncated.</summary>
    /// <returns>The truncation source offset.</returns>
    /// <exception cref="System.InvalidOperationException">The layout is not truncated.</exception>
    public int GetTruncationUtf16Index()
    {
        return layout.GetTruncationUtf16Index();
    }

    int FindLineIndex(PositionedRun run)
    {
        int lineIndex = 0;
        while (lineIndex < layout.GetLineCount())
        {
            TextLine line = layout.GetLine(lineIndex);
            int runIndex = 0;
            while (runIndex < line.GetRunCount())
            {
                if (object.ReferenceEquals(line.GetVisualRun(runIndex), run))
                {
                    return lineIndex;
                }
                runIndex += 1;
            }
            lineIndex += 1;
        }
        throw new System.InvalidOperationException(
            "A positioned run is not owned by this layout.");
    }
}
