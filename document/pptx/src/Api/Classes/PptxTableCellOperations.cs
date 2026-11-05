using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxTableCell values.
/// </summary>
public class PptxTableCellOperations : IPptxTableCellOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxTableCellOperations()
    {
    }

    /// <summary>
    /// Returns the zero-based row coordinate of the cell.
    /// </summary>
    /// <param name="target">The table cell to inspect.</param>
    /// <returns>The zero-based row index.</returns>
    public int GetRowIndex(PptxTableCell target)
    {
        return target.RowIndex;
    }

    /// <summary>
    /// Returns the zero-based column coordinate of the cell.
    /// </summary>
    /// <param name="target">The table cell to inspect.</param>
    /// <returns>The zero-based column index.</returns>
    public int GetColumnIndex(PptxTableCell target)
    {
        return target.ColumnIndex;
    }

    /// <summary>
    /// Returns the text without changing the table cell.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetText(PptxTableCell target)
    {
        return target.GetText();
    }

    /// <summary>
    /// Updates the text and the underlying presentation data.
    /// </summary>
    /// <param name="text">The text to read, write, or compare.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetText(PptxTableCell target, string text)
    {
        target.SetText(text);
    }

    /// <summary>
    /// Returns the column span without changing the table cell.
    /// </summary>
    /// <returns>The resulting numeric value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int GetColumnSpan(PptxTableCell target)
    {
        return target.GetColumnSpan();
    }

    /// <summary>
    /// Returns the row span without changing the table cell.
    /// </summary>
    /// <returns>The resulting numeric value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int GetRowSpan(PptxTableCell target)
    {
        return target.GetRowSpan();
    }

    /// <summary>
    /// Reports whether the table cell is horizontal merge.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool IsHorizontalMerge(PptxTableCell target)
    {
        return target.IsHorizontalMerge();
    }

    /// <summary>
    /// Reports whether the table cell is vertical merge.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool IsVerticalMerge(PptxTableCell target)
    {
        return target.IsVerticalMerge();
    }

    /// <summary>
    /// Reports whether the table cell is merge continuation.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool IsMergeContinuation(PptxTableCell target)
    {
        return target.IsMergeContinuation();
    }

    /// <summary>
    /// Reports whether the table cell is merge origin.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool IsMergeOrigin(PptxTableCell target)
    {
        return target.IsMergeOrigin();
    }

    /// <summary>
    /// Reports whether the table cell is merged.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool IsMerged(PptxTableCell target)
    {
        return target.IsMerged();
    }

    /// <summary>
    /// Returns the fill without changing the table cell.
    /// </summary>
    /// <returns>The resulting fill value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxFill GetFill(PptxTableCell target)
    {
        return target.GetFill();
    }

    /// <summary>
    /// Updates the fill and the underlying presentation data.
    /// </summary>
    /// <param name="fill">The fill value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetFill(PptxTableCell target, PptxFill fill)
    {
        target.SetFill(fill);
    }

    /// <summary>
    /// Returns the border without changing the table cell.
    /// </summary>
    /// <param name="borderName">The border name value to use.</param>
    /// <returns>The resulting line value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxLine GetBorder(PptxTableCell target, string borderName)
    {
        return target.GetBorder(borderName);
    }

    /// <summary>
    /// Updates the border and the underlying presentation data.
    /// </summary>
    /// <param name="borderName">The border name value to use.</param>
    /// <param name="line">The line value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetBorder(PptxTableCell target, string borderName, PptxLine line)
    {
        target.SetBorder(borderName, line);
    }

    /// <summary>
    /// Returns the margin left without changing the table cell.
    /// </summary>
    /// <returns>The resulting numeric value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public long GetMarginLeft(PptxTableCell target)
    {
        return target.GetMarginLeft();
    }

    /// <summary>
    /// Returns the margin top without changing the table cell.
    /// </summary>
    /// <returns>The resulting numeric value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public long GetMarginTop(PptxTableCell target)
    {
        return target.GetMarginTop();
    }

    /// <summary>
    /// Returns the margin right without changing the table cell.
    /// </summary>
    /// <returns>The resulting numeric value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public long GetMarginRight(PptxTableCell target)
    {
        return target.GetMarginRight();
    }

    /// <summary>
    /// Returns the margin bottom without changing the table cell.
    /// </summary>
    /// <returns>The resulting numeric value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public long GetMarginBottom(PptxTableCell target)
    {
        return target.GetMarginBottom();
    }

    /// <summary>
    /// Returns the margin without changing the table cell.
    /// </summary>
    /// <param name="marginName">The margin name value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public long GetMargin(PptxTableCell target, string marginName)
    {
        return target.GetMargin(marginName);
    }

    /// <summary>
    /// Updates the margins and the underlying presentation data.
    /// </summary>
    /// <param name="left">The left value to use.</param>
    /// <param name="top">The top value to use.</param>
    /// <param name="right">The right value to use.</param>
    /// <param name="bottom">The bottom value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetMargins(PptxTableCell target, long left, long top, long right, long bottom)
    {
        target.SetMargins(left, top, right, bottom);
    }

    /// <summary>
    /// Updates the margin and the underlying presentation data.
    /// </summary>
    /// <param name="marginName">The margin name value to use.</param>
    /// <param name="margin">The margin value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetMargin(PptxTableCell target, string marginName, long margin)
    {
        target.SetMargin(marginName, margin);
    }

    /// <summary>
    /// Returns the vertical alignment without changing the table cell.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetVerticalAlignment(PptxTableCell target)
    {
        return target.GetVerticalAlignment();
    }

    /// <summary>
    /// Updates the vertical alignment and the underlying presentation data.
    /// </summary>
    /// <param name="alignment">The alignment value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetVerticalAlignment(PptxTableCell target, string alignment)
    {
        target.SetVerticalAlignment(alignment);
    }

    /// <summary>
    /// Returns the text direction without changing the table cell.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetTextDirection(PptxTableCell target)
    {
        return target.GetTextDirection();
    }

    /// <summary>
    /// Updates the text direction and the underlying presentation data.
    /// </summary>
    /// <param name="direction">The direction value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetTextDirection(PptxTableCell target, string direction)
    {
        target.SetTextDirection(direction);
    }
}
