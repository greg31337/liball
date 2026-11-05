namespace ttf;

/// <summary>Defines one immutable positioned line in a text layout.</summary>
public interface ITextLineResult
{
    /// <summary>Gets the zero-based line index.</summary><returns>The line index.</returns>
    int GetLineIndex();
    /// <summary>Gets the first logical cluster.</summary><returns>The cluster index.</returns>
    int GetClusterStart();
    /// <summary>Gets the exclusive logical cluster end.</summary><returns>The cluster boundary.</returns>
    int GetClusterEnd();
    /// <summary>Gets the logical cluster count.</summary><returns>The cluster count.</returns>
    int GetClusterCount();
    /// <summary>Gets the first visible cluster.</summary><returns>The cluster index.</returns>
    int GetVisibleClusterStart();
    /// <summary>Gets the exclusive visible cluster end.</summary><returns>The cluster boundary.</returns>
    int GetVisibleClusterEnd();
    /// <summary>Gets the source UTF-16 start.</summary><returns>The source offset.</returns>
    int GetUtf16Start();
    /// <summary>Gets the source UTF-16 length.</summary><returns>The source length.</returns>
    int GetUtf16Length();
    /// <summary>Gets the visible source UTF-16 start.</summary><returns>The visible source offset.</returns>
    int GetVisibleUtf16Start();
    /// <summary>Gets the visible source UTF-16 length.</summary><returns>The visible source length.</returns>
    int GetVisibleUtf16Length();
    /// <summary>Gets the writing mode.</summary><returns>The line writing mode.</returns>
    TextWritingMode GetWritingMode();
    /// <summary>Gets the baseline coordinate.</summary><returns>The baseline coordinate.</returns>
    double GetBaseline();
    /// <summary>Gets the line's inline alignment offset.</summary><returns>The inline offset.</returns>
    double GetInlineOffset();
    /// <summary>Gets the visible source advance before an inserted hyphen or ellipsis.</summary><returns>The content advance.</returns>
    double GetContentAdvance();
    /// <summary>Gets the positioned inline advance.</summary><returns>The line advance.</returns>
    double GetAdvance();
    /// <summary>Gets the line advance before justification expansion.</summary><returns>The natural advance.</returns>
    double GetNaturalAdvance();
    /// <summary>Gets the requested justification target.</summary><returns>The target advance.</returns>
    double GetJustificationTargetAdvance();
    /// <summary>Gets the total justification expansion applied to the line.</summary><returns>The nonnegative expansion.</returns>
    double GetJustificationExpansion();
    /// <summary>Reports whether applied expansion reached the target advance.</summary><returns>True when the target was reached.</returns>
    bool IsFullyJustified();
    /// <summary>Gets the total adjustment assigned at one source-cluster boundary.</summary><param name="clusterBoundary">A boundary belonging to the line.</param><returns>The nonnegative adjustment.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>clusterBoundary</c> is outside the line.</exception>
    double GetJustificationBoundaryAdjustment(int clusterBoundary);
    /// <summary>Gets the number of individual justification applications.</summary><returns>The application count.</returns>
    int GetJustificationApplicationCount();
    /// <summary>Gets one applied justification adjustment.</summary><param name="index">The zero-based application index.</param><returns>The immutable application.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>index</c> is invalid.</exception>
    TextJustificationApplication GetJustificationApplication(int index);
    /// <summary>Gets the number of source tabs assigned to this line.</summary><returns>The tab count.</returns>
    int GetTabCount();
    /// <summary>Reports whether line-edge whitespace was hidden.</summary><returns>True when whitespace was trimmed.</returns>
    bool WasWhitespaceTrimmed();
    /// <summary>Reports whether a visible hyphen was inserted at the line end.</summary><returns>True when a hyphen was inserted.</returns>
    bool HasInsertedHyphen();
    /// <summary>Reports whether the line ends with the configured ellipsis.</summary><returns>True when an ellipsis was inserted.</returns>
    bool HasEllipsis();
    /// <summary>Gets the inserted ellipsis text.</summary><returns>The configured ellipsis text.</returns>
    /// <exception cref="System.InvalidOperationException">The line has no ellipsis.</exception>
    string GetEllipsisText();
    /// <summary>Reports whether a mandatory source break ended this line.</summary><returns>True when the line ended at a hard break.</returns>
    bool EndedByHardBreak();
    /// <summary>Gets the line ascent.</summary><returns>The nonnegative ascent.</returns>
    double GetAscent();
    /// <summary>Gets the line descent.</summary><returns>The nonnegative descent.</returns>
    double GetDescent();
    /// <summary>Gets the line gap.</summary><returns>The nonnegative line gap.</returns>
    double GetLineGap();
    /// <summary>Gets the total line height.</summary><returns>The line height.</returns>
    double GetLineHeight();
    /// <summary>Gets the exact advance bounds.</summary><returns>The advance bounds.</returns>
    TextInkBounds GetAdvanceBounds();
    /// <summary>Gets the exact ink bounds.</summary><returns>The ink bounds.</returns>
    TextInkBounds GetInkBounds();
    /// <summary>Gets the positioned run count.</summary><returns>The run count.</returns>
    int GetRunCount();
    /// <summary>Gets one run in visual order.</summary><param name="visualIndex">The visual run index.</param><returns>The run.</returns>
    PositionedRunResult GetVisualRun(int visualIndex);
    /// <summary>Gets one run in logical order.</summary><param name="logicalIndex">The logical run index.</param><returns>The run.</returns>
    PositionedRunResult GetLogicalRun(int logicalIndex);
    /// <summary>Maps a logical run index to visual order.</summary><param name="logicalIndex">The logical run index.</param><returns>The visual index.</returns>
    int GetVisualRunIndex(int logicalIndex);
    /// <summary>Maps a visual run index to logical order.</summary><param name="visualIndex">The visual run index.</param><returns>The logical index.</returns>
    int GetLogicalRunIndex(int visualIndex);
}
