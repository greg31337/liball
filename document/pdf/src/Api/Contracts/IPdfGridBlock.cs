using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfGridBlock"/>.
/// </summary>
public interface IPdfGridBlock
{
    /// <summary>
    /// Adds a block to this grid block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    void AddBlock(PdfBlock block);

    /// <summary>
    /// Adds a text block to this grid block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    void AddTextBlock(PdfTextBlock block);

    /// <summary>
    /// Adds a stack block to this grid block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    void AddStackBlock(PdfStackBlock block);

    /// <summary>
    /// Adds a row block to this grid block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    void AddRowBlock(PdfRowBlock block);

    /// <summary>
    /// Adds a column block to this grid block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    void AddColumnBlock(PdfColumnBlock block);

    /// <summary>
    /// Adds a grid block to this grid block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    void AddGridBlock(PdfGridBlock block);

    /// <summary>
    /// Adds a table block to this grid block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    void AddTableBlock(PdfTableBlock block);

    /// <summary>
    /// Adds a list block to this grid block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    void AddListBlock(PdfListBlock block);

    /// <summary>
    /// Adds a layout element to this grid block.
    /// </summary>
    /// <param name="element">The layout element to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void AddElement(IPdfLayoutElement element);

    /// <summary>
    /// Sets the padding used for subsequent PDF generation.
    /// </summary>
    /// <param name="padding">The padding to use for this operation.</param>
    void SetPadding(PdfInsets padding);

    /// <summary>
    /// Sets the border used for subsequent PDF generation.
    /// </summary>
    /// <param name="border">The border to use for this operation.</param>
    void SetBorder(PdfInsets border);

    /// <summary>
    /// Sets the margin used for subsequent PDF generation.
    /// </summary>
    /// <param name="margin">The margin to use for this operation.</param>
    void SetMargin(PdfInsets margin);

    /// <summary>
    /// Sets the horizontal and vertical gaps between grid children.
    /// </summary>
    /// <param name="columnSpacing">The non-negative horizontal gap.</param>
    /// <param name="rowSpacing">The non-negative vertical gap.</param>
    void SetGaps(double columnSpacing, double rowSpacing);

    /// <summary>
    /// Applies a defensive copy of a reusable element style and uses item spacing for both grid gaps.
    /// </summary>
    /// <param name="style">The element style to apply.</param>
    void SetStyle(PdfElementStyle style);

    /// <summary>
    /// Returns the row count calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated integer value.</returns>
    int GetRowCount();
}
