using System;
using System.Collections.Generic;
using System.Globalization;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfListBlock"/>.
/// </summary>
public interface IPdfListBlock
{
    /// <summary>
    /// Adds a block to this list block and returns the stored value.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfListItem AddBlock(PdfBlock block);

    /// <summary>
    /// Adds a text block to this list block and returns the stored value.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfListItem AddTextBlock(PdfTextBlock block);

    /// <summary>
    /// Adds a stack block to this list block and returns the stored value.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfListItem AddStackBlock(PdfStackBlock block);

    /// <summary>
    /// Adds a row block to this list block and returns the stored value.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfListItem AddRowBlock(PdfRowBlock block);

    /// <summary>
    /// Adds a column block to this list block and returns the stored value.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfListItem AddColumnBlock(PdfColumnBlock block);

    /// <summary>
    /// Adds a grid block to this list block and returns the stored value.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfListItem AddGridBlock(PdfGridBlock block);

    /// <summary>
    /// Adds a table block to this list block and returns the stored value.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfListItem AddTableBlock(PdfTableBlock block);

    /// <summary>
    /// Adds a list block to this list block and returns the stored value.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfListItem AddListBlock(PdfListBlock block);

    /// <summary>
    /// Adds a layout element to this list block and returns its list item.
    /// </summary>
    /// <param name="element">The layout element to add.</param>
    /// <returns>The list item that contains the element.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    PdfListItem AddElement(IPdfLayoutElement element);

    /// <summary>
    /// Adds an item to this list block.
    /// </summary>
    /// <param name="item">The item to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void AddItem(PdfListItem item);

    /// <summary>
    /// Sets the marker style used for subsequent list rendering.
    /// </summary>
    /// <param name="markerStyle">The marker style to use for this operation.</param>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    void SetMarkerStyle(PdfListMarkerStyle markerStyle);

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
    /// Applies a defensive copy of a reusable element style, including item spacing.
    /// </summary>
    /// <param name="style">The element style to apply.</param>
    void SetStyle(PdfElementStyle style);

    /// <summary>
    /// Sets the spacing used for subsequent PDF generation.
    /// </summary>
    /// <param name="spacing">The spacing to use for this operation.</param>
    void SetSpacing(double spacing);

    /// <summary>
    /// Sets the marker width used for subsequent PDF generation.
    /// </summary>
    /// <param name="markerWidth">The marker width in PDF points.</param>
    void SetMarkerWidth(double markerWidth);

    /// <summary>
    /// Sets the marker gap used for subsequent PDF generation.
    /// </summary>
    /// <param name="markerGap">The marker gap to use for this operation.</param>
    void SetMarkerGap(double markerGap);

    /// <summary>
    /// Sets the start number used for subsequent PDF generation.
    /// </summary>
    /// <param name="startNumber">The start number to use for this operation.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric argument falls outside its supported range.</exception>
    void SetStartNumber(int startNumber);

    /// <summary>
    /// Sets the marker text style used for subsequent PDF generation.
    /// </summary>
    /// <param name="markerTextStyle">The marker text style to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetMarkerTextStyle(PdfTextStyle markerTextStyle);
}
