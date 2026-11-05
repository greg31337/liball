using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Represents a table row used by the PDF generation API.
/// </summary>
public class PdfTableRow : IPdfTableRow
{
    internal List<PdfTableCell> _cells = new List<PdfTableCell>();
    /// <summary>
    /// Exposes the cells currently stored by this object.
    /// </summary>
    internal IReadOnlyList<PdfTableCell> Cells = new List<PdfTableCell>();
    internal bool HasExplicitHeight;
    internal double ExplicitHeight;

    /// <summary>
    /// Initializes a new table row.
    /// </summary>
    public PdfTableRow()
    {
        _cells = new List<PdfTableCell>();
        Cells = _cells;
        HasExplicitHeight = false;
        ExplicitHeight = 0;
    }

    /// <summary>
    /// Adds a block to this table row and returns the stored value.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfTableCell AddBlock(PdfBlock block)
    {
        return AddElement(block);
    }

    /// <summary>
    /// Adds a text block to this table row and returns the stored value.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfTableCell AddTextBlock(PdfTextBlock block)
    {
        return AddElement(block);
    }

    /// <summary>
    /// Adds a stack block to this table row and returns the stored value.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfTableCell AddStackBlock(PdfStackBlock block)
    {
        return AddElement(block);
    }

    /// <summary>
    /// Adds a row block to this table row and returns the stored value.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfTableCell AddRowBlock(PdfRowBlock block)
    {
        return AddElement(block);
    }

    /// <summary>
    /// Adds a column block to this table row and returns the stored value.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfTableCell AddColumnBlock(PdfColumnBlock block)
    {
        return AddElement(block);
    }

    /// <summary>
    /// Adds a grid block to this table row and returns the stored value.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfTableCell AddGridBlock(PdfGridBlock block)
    {
        return AddElement(block);
    }

    /// <summary>
    /// Adds a table block to this table row and returns the stored value.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfTableCell AddTableBlock(PdfTableBlock block)
    {
        return AddElement(block);
    }

    /// <summary>
    /// Adds a list block to this table row and returns the stored value.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfTableCell AddListBlock(PdfListBlock block)
    {
        return AddElement(block);
    }

    /// <summary>
    /// Adds a cell to this table row.
    /// </summary>
    /// <param name="cell">The cell to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void AddCell(PdfTableCell cell)
    {
        if (cell == null)
        {
            throw new ArgumentNullException("cell");
        }

        _cells.Add(cell);
    }

    /// <summary>
    /// Adds a layout element to this table row and returns its cell.
    /// </summary>
    /// <param name="element">The layout element to add.</param>
    /// <returns>The table cell that contains the element.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfTableCell AddElement(IPdfLayoutElement element)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        PdfTableCell cell = new PdfTableCell(element);
        _cells.Add(cell);
        return cell;
    }

    /// <summary>
    /// Sets an exact row height instead of content-driven height.
    /// </summary>
    /// <param name="height">The positive finite row height in PDF points.</param>
    public void SetHeight(double height)
    {
        if (double.IsNaN(height) || double.IsInfinity(height) || height <= 0)
        {
            throw new ArgumentOutOfRangeException("height", "Table row height must be finite and positive.");
        }

        HasExplicitHeight = true;
        ExplicitHeight = height;
    }

    /// <summary>
    /// Restores content-driven row height.
    /// </summary>
    public void ClearHeight()
    {
        HasExplicitHeight = false;
        ExplicitHeight = 0;
    }
}
