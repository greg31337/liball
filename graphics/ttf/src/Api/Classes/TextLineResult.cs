namespace ttf;

/// <summary>Represents one immutable positioned line in a text layout.</summary>
public class TextLineResult : ITextLineResult
{
    TextLine line;
    int lineIndex;

    internal TextLineResult(TextLine line, int lineIndex)
    {
        this.line = line;
        this.lineIndex = lineIndex;
    }

    /// <summary>Gets the zero-based line index.</summary><returns>The line index.</returns>
    public int GetLineIndex() { return lineIndex; }
    /// <summary>Gets the first logical cluster.</summary><returns>The cluster index.</returns>
    public int GetClusterStart() { return line.GetClusterStart(); }
    /// <summary>Gets the exclusive logical cluster end.</summary><returns>The cluster boundary.</returns>
    public int GetClusterEnd() { return line.GetClusterEnd(); }
    /// <summary>Gets the logical cluster count.</summary><returns>The cluster count.</returns>
    public int GetClusterCount() { return line.GetClusterEnd() - line.GetClusterStart(); }
    /// <summary>Gets the first visible cluster.</summary><returns>The cluster index.</returns>
    public int GetVisibleClusterStart() { return line.GetVisibleClusterStart(); }
    /// <summary>Gets the exclusive visible cluster end.</summary><returns>The cluster boundary.</returns>
    public int GetVisibleClusterEnd() { return line.GetVisibleClusterEnd(); }
    /// <summary>Gets the source UTF-16 start.</summary><returns>The source offset.</returns>
    public int GetUtf16Start() { return line.GetUtf16Start(); }
    /// <summary>Gets the source UTF-16 length.</summary><returns>The source length.</returns>
    public int GetUtf16Length() { return line.GetUtf16Length(); }
    /// <summary>Gets the visible source UTF-16 start.</summary><returns>The visible source offset.</returns>
    public int GetVisibleUtf16Start() { return line.GetVisibleUtf16Start(); }
    /// <summary>Gets the visible source UTF-16 length.</summary><returns>The visible source length.</returns>
    public int GetVisibleUtf16Length() { return line.GetVisibleUtf16Length(); }
    /// <summary>Gets the writing mode.</summary><returns>The line writing mode.</returns>
    public TextWritingMode GetWritingMode() { return line.GetWritingMode(); }
    /// <summary>Gets the baseline coordinate.</summary><returns>The baseline coordinate.</returns>
    public double GetBaseline() { return line.GetBaseline(); }
    /// <summary>Gets the line's inline alignment offset.</summary><returns>The inline offset.</returns>
    public double GetInlineOffset() { return line.GetInlineOffset(); }
    /// <summary>Gets the visible source advance before an inserted hyphen or ellipsis.</summary><returns>The content advance.</returns>
    public double GetContentAdvance() { return line.GetContentAdvance(); }
    /// <summary>Gets the positioned inline advance.</summary><returns>The line advance.</returns>
    public double GetAdvance() { return line.GetAdvance(); }
    /// <summary>Gets the line advance before justification expansion.</summary><returns>The natural advance.</returns>
    public double GetNaturalAdvance() { return line.GetNaturalAdvance(); }
    /// <summary>Gets the requested justification target.</summary><returns>The target advance.</returns>
    public double GetJustificationTargetAdvance()
    {
        return line.GetJustificationTargetAdvance();
    }
    /// <summary>Gets the total justification expansion applied to the line.</summary><returns>The nonnegative expansion.</returns>
    public double GetJustificationExpansion()
    {
        return line.GetJustificationExpansion();
    }
    /// <summary>Reports whether applied expansion reached the target advance.</summary><returns>True when the target was reached.</returns>
    public bool IsFullyJustified() { return line.IsFullyJustified(); }
    /// <summary>Gets the total adjustment assigned at one source-cluster boundary.</summary><param name="clusterBoundary">A boundary belonging to the line.</param><returns>The nonnegative adjustment.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>clusterBoundary</c> is outside the line.</exception>
    public double GetJustificationBoundaryAdjustment(int clusterBoundary)
    {
        return line.GetJustificationBoundaryAdjustment(clusterBoundary);
    }
    /// <summary>Gets the number of individual justification applications.</summary><returns>The application count.</returns>
    public int GetJustificationApplicationCount()
    {
        return line.GetJustificationApplicationCount();
    }
    /// <summary>Gets one applied justification adjustment.</summary><param name="index">The zero-based application index.</param><returns>The immutable application.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    public TextJustificationApplication GetJustificationApplication(int index)
    {
        return new TextJustificationApplication(
            line.GetJustificationApplication(index));
    }
    /// <summary>Gets the number of source tabs assigned to this line.</summary><returns>The tab count.</returns>
    public int GetTabCount() { return line.GetTabCount(); }
    /// <summary>Reports whether line-edge whitespace was hidden.</summary><returns>True when whitespace was trimmed.</returns>
    public bool WasWhitespaceTrimmed() { return line.WasWhitespaceTrimmed(); }
    /// <summary>Reports whether a visible hyphen was inserted at the line end.</summary><returns>True when a hyphen was inserted.</returns>
    public bool HasInsertedHyphen() { return line.HasInsertedHyphen(); }
    /// <summary>Reports whether the line ends with the configured ellipsis.</summary><returns>True when an ellipsis was inserted.</returns>
    public bool HasEllipsis() { return line.HasEllipsis(); }
    /// <summary>Gets the inserted ellipsis text.</summary><returns>The configured ellipsis text.</returns>
    /// <exception cref="System.InvalidOperationException">The line has no ellipsis.</exception>
    public string GetEllipsisText() { return line.GetEllipsisText(); }
    /// <summary>Reports whether a mandatory source break ended this line.</summary><returns>True when the line ended at a hard break.</returns>
    public bool EndedByHardBreak() { return line.EndedByHardBreak(); }
    /// <summary>Gets the line ascent.</summary><returns>The nonnegative ascent.</returns>
    public double GetAscent() { return line.GetAscent(); }
    /// <summary>Gets the line descent.</summary><returns>The nonnegative descent.</returns>
    public double GetDescent() { return line.GetDescent(); }
    /// <summary>Gets the line gap.</summary><returns>The nonnegative line gap.</returns>
    public double GetLineGap() { return line.GetLineGap(); }
    /// <summary>Gets the total line height.</summary><returns>The line height.</returns>
    public double GetLineHeight() { return line.GetLineHeight(); }
    /// <summary>Gets the exact advance bounds.</summary><returns>The advance bounds.</returns>
    public TextInkBounds GetAdvanceBounds() { return line.GetAdvanceBounds(); }
    /// <summary>Gets the exact ink bounds.</summary><returns>The ink bounds.</returns>
    public TextInkBounds GetInkBounds() { return line.GetInkBounds(); }
    /// <summary>Gets the positioned run count.</summary><returns>The run count.</returns>
    public int GetRunCount() { return line.GetRunCount(); }

    /// <summary>Gets one run in visual order.</summary><param name="visualIndex">The visual run index.</param><returns>The run.</returns>
    public PositionedRunResult GetVisualRun(int visualIndex)
    {
        return new PositionedRunResult(line.GetVisualRun(visualIndex), lineIndex);
    }

    /// <summary>Gets one run in logical order.</summary><param name="logicalIndex">The logical run index.</param><returns>The run.</returns>
    public PositionedRunResult GetLogicalRun(int logicalIndex)
    {
        return new PositionedRunResult(line.GetLogicalRun(logicalIndex), lineIndex);
    }

    /// <summary>Maps a logical run index to visual order.</summary><param name="logicalIndex">The logical run index.</param><returns>The visual index.</returns>
    public int GetVisualRunIndex(int logicalIndex)
    {
        return line.GetVisualRunIndex(logicalIndex);
    }

    /// <summary>Maps a visual run index to logical order.</summary><param name="visualIndex">The visual run index.</param><returns>The logical index.</returns>
    public int GetLogicalRunIndex(int visualIndex)
    {
        return line.GetLogicalRunIndex(visualIndex);
    }
}
