using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfReportTable"/>.
/// </summary>
public interface IPdfReportTable
{
    /// <summary>
    /// Sets the column widths used for subsequent PDF generation.
    /// </summary>
    /// <param name="columnWidths">The column widths to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric argument falls outside its supported range.</exception>
    void SetColumnWidths(double[] columnWidths);

    /// <summary>
    /// Adds a row to this report table.
    /// </summary>
    /// <param name="cells">The cells to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    void AddRow(string[] cells);

    /// <summary>Adds a full-width group heading row.</summary>
    /// <param name="label">The non-empty group label.</param>
    void AddGroupRow(string label);

    /// <summary>Adds a visually emphasized totals row.</summary>
    /// <param name="cells">The total cells matching the header count.</param>
    void AddTotalsRow(string[] cells);
}
