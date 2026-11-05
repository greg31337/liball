using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfTableRow"/>.
/// </summary>
public interface IPdfTableRow
{
    /// <summary>
    /// Adds a block to this table row and returns the stored value.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfTableCell AddBlock(PdfBlock block);

    /// <summary>
    /// Adds a text block to this table row and returns the stored value.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfTableCell AddTextBlock(PdfTextBlock block);

    /// <summary>
    /// Adds a stack block to this table row and returns the stored value.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfTableCell AddStackBlock(PdfStackBlock block);

    /// <summary>
    /// Adds a row block to this table row and returns the stored value.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfTableCell AddRowBlock(PdfRowBlock block);

    /// <summary>
    /// Adds a column block to this table row and returns the stored value.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfTableCell AddColumnBlock(PdfColumnBlock block);

    /// <summary>
    /// Adds a grid block to this table row and returns the stored value.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfTableCell AddGridBlock(PdfGridBlock block);

    /// <summary>
    /// Adds a table block to this table row and returns the stored value.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfTableCell AddTableBlock(PdfTableBlock block);

    /// <summary>
    /// Adds a list block to this table row and returns the stored value.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfTableCell AddListBlock(PdfListBlock block);

    /// <summary>
    /// Adds a cell to this table row.
    /// </summary>
    /// <param name="cell">The cell to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void AddCell(PdfTableCell cell);

    /// <summary>
    /// Adds a layout element to this table row and returns its cell.
    /// </summary>
    /// <param name="element">The layout element to add.</param>
    /// <returns>The table cell that contains the element.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    PdfTableCell AddElement(IPdfLayoutElement element);

    /// <summary>
    /// Sets an exact row height instead of content-driven height.
    /// </summary>
    /// <param name="height">The positive finite row height in PDF points.</param>
    void SetHeight(double height);

    /// <summary>
    /// Restores content-driven row height.
    /// </summary>
    void ClearHeight();
}
