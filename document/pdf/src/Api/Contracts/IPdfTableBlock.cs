using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfTableBlock"/>.
/// </summary>
public interface IPdfTableBlock
{
    /// <summary>
    /// Adds a row to this table block and returns the stored value.
    /// </summary>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfTableRow AddRow();

    /// <summary>
    /// Adds a header row to this table block and returns the stored value.
    /// </summary>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfTableRow AddHeaderRow();

    /// <summary>
    /// Adds a footer row to this table block and returns the stored value.
    /// </summary>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfTableRow AddFooterRow();

    /// <summary>
    /// Adds a header row to this table block.
    /// </summary>
    /// <param name="row">The row to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void AddExistingHeaderRow(PdfTableRow row);

    /// <summary>
    /// Adds a row to this table block.
    /// </summary>
    /// <param name="row">The row to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void AddExistingRow(PdfTableRow row);

    /// <summary>
    /// Adds a footer row to this table block.
    /// </summary>
    /// <param name="row">The row to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void AddExistingFooterRow(PdfTableRow row);

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
    /// Applies a defensive copy of a reusable element style to the table container.
    /// </summary>
    /// <param name="style">The element style to apply.</param>
    void SetStyle(PdfElementStyle style);

    /// <summary>
    /// Applies reusable table and header, body, and footer cell styles.
    /// </summary>
    /// <param name="style">The table style to apply.</param>
    void SetTableStyle(PdfTableStyle style);

    /// <summary>
    /// Replaces fixed widths with reusable column sizing definitions.
    /// </summary>
    /// <param name="definitions">One definition for each table column.</param>
    void SetColumnDefinitions(PdfTableColumnDefinition[] definitions);

    /// <summary>
    /// Sets whether pagination may break row-span groups.
    /// </summary>
    /// <param name="policy">The row split policy.</param>
    void SetRowSplitPolicy(PdfTableRowSplitPolicy policy);

    /// <summary>
    /// Returns the most recently resolved width for a column.
    /// </summary>
    /// <param name="columnIndex">The zero-based column index.</param>
    /// <returns>The resolved width in PDF points.</returns>
    double GetResolvedColumnWidth(int columnIndex);

    /// <summary>
    /// Returns the row count calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated integer value.</returns>
    int GetRowCount();

    /// <summary>
    /// Returns the header row count calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated integer value.</returns>
    int GetHeaderRowCount();

    /// <summary>
    /// Returns the footer row count calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated integer value.</returns>
    int GetFooterRowCount();

    /// <summary>
    /// Measures a header rows height without emitting drawing commands.
    /// </summary>
    /// <param name="availableHeight">The available height in PDF points.</param>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    double MeasureHeaderRowsHeight(double availableHeight);

    /// <summary>
    /// Measures a footer rows height without emitting drawing commands.
    /// </summary>
    /// <param name="availableHeight">The available height in PDF points.</param>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    double MeasureFooterRowsHeight(double availableHeight);
}
