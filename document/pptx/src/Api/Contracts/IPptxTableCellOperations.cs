using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxTableCellOperations"/>.
/// </summary>
public interface IPptxTableCellOperations
{
    /// <summary>
    /// Returns the zero-based row coordinate of the cell.
    /// </summary>
    /// <param name="target">The table cell to inspect.</param>
    /// <returns>The zero-based row index.</returns>
    int GetRowIndex(PptxTableCell target);
    /// <summary>
    /// Returns the zero-based column coordinate of the cell.
    /// </summary>
    /// <param name="target">The table cell to inspect.</param>
    /// <returns>The zero-based column index.</returns>
    int GetColumnIndex(PptxTableCell target);
    /// <summary>
    /// Returns the text without changing the table cell.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetText(PptxTableCell target);
    /// <summary>
    /// Updates the text and the underlying presentation data.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetText(PptxTableCell target, string text);
    /// <summary>
    /// Returns the column span without changing the table cell.
    /// </summary>
    /// <returns>The resulting numeric value.</returns>
    /// <param name="target">The data value to operate on.</param>
    int GetColumnSpan(PptxTableCell target);
    /// <summary>
    /// Returns the row span without changing the table cell.
    /// </summary>
    /// <returns>The resulting numeric value.</returns>
    /// <param name="target">The data value to operate on.</param>
    int GetRowSpan(PptxTableCell target);
    /// <summary>
    /// Reports whether the table cell is horizontal merge.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool IsHorizontalMerge(PptxTableCell target);
    /// <summary>
    /// Reports whether the table cell is vertical merge.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool IsVerticalMerge(PptxTableCell target);
    /// <summary>
    /// Reports whether the table cell is merge continuation.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool IsMergeContinuation(PptxTableCell target);
    /// <summary>
    /// Reports whether the table cell is merge origin.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool IsMergeOrigin(PptxTableCell target);
    /// <summary>
    /// Reports whether the table cell is merged.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool IsMerged(PptxTableCell target);
    /// <summary>
    /// Returns the fill without changing the table cell.
    /// </summary>
    /// <returns>The resulting fill value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxFill GetFill(PptxTableCell target);
    /// <summary>
    /// Updates the fill and the underlying presentation data.
    /// </summary>
    /// <param name="fill">The fill value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetFill(PptxTableCell target, PptxFill fill);
    /// <summary>
    /// Returns the border without changing the table cell.
    /// </summary>
    /// <param name="borderName">The border name value to use.</param>
    /// <returns>The resulting line value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxLine GetBorder(PptxTableCell target, string borderName);
    /// <summary>
    /// Updates the border and the underlying presentation data.
    /// </summary>
    /// <param name="borderName">The border name value to use.</param>
    /// <param name="line">The line value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetBorder(PptxTableCell target, string borderName, PptxLine line);
    /// <summary>
    /// Returns the margin left without changing the table cell.
    /// </summary>
    /// <returns>The resulting numeric value.</returns>
    /// <param name="target">The data value to operate on.</param>
    long GetMarginLeft(PptxTableCell target);
    /// <summary>
    /// Returns the margin top without changing the table cell.
    /// </summary>
    /// <returns>The resulting numeric value.</returns>
    /// <param name="target">The data value to operate on.</param>
    long GetMarginTop(PptxTableCell target);
    /// <summary>
    /// Returns the margin right without changing the table cell.
    /// </summary>
    /// <returns>The resulting numeric value.</returns>
    /// <param name="target">The data value to operate on.</param>
    long GetMarginRight(PptxTableCell target);
    /// <summary>
    /// Returns the margin bottom without changing the table cell.
    /// </summary>
    /// <returns>The resulting numeric value.</returns>
    /// <param name="target">The data value to operate on.</param>
    long GetMarginBottom(PptxTableCell target);
    /// <summary>
    /// Returns the margin without changing the table cell.
    /// </summary>
    /// <param name="marginName">The margin name value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    /// <param name="target">The data value to operate on.</param>
    long GetMargin(PptxTableCell target, string marginName);
    /// <summary>
    /// Updates the margins and the underlying presentation data.
    /// </summary>
    /// <param name="left">The left value to use.</param>
    /// <param name="top">The top value to use.</param>
    /// <param name="right">The right value to use.</param>
    /// <param name="bottom">The bottom value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetMargins(PptxTableCell target, long left, long top, long right, long bottom);
    /// <summary>
    /// Updates the margin and the underlying presentation data.
    /// </summary>
    /// <param name="marginName">The margin name value to use.</param>
    /// <param name="margin">The margin value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetMargin(PptxTableCell target, string marginName, long margin);
    /// <summary>
    /// Returns the vertical alignment without changing the table cell.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetVerticalAlignment(PptxTableCell target);
    /// <summary>
    /// Updates the vertical alignment and the underlying presentation data.
    /// </summary>
    /// <param name="alignment">The alignment value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetVerticalAlignment(PptxTableCell target, string alignment);
    /// <summary>
    /// Returns the text direction without changing the table cell.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetTextDirection(PptxTableCell target);
    /// <summary>
    /// Updates the text direction and the underlying presentation data.
    /// </summary>
    /// <param name="direction">The direction value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetTextDirection(PptxTableCell target, string direction);
}
