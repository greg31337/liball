using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Represents a report table used by the PDF generation API.
/// </summary>
public class PdfReportTable : IPdfReportTable
{
    internal List<string> _headers = new List<string>();
    internal List<string[]> _rows = new List<string[]>();
    internal List<int> _rowKinds = new List<int>();
    internal double[] _columnWidths = new double[0];
    /// <summary>
    /// Exposes the headers currently stored by this object.
    /// </summary>
    internal IReadOnlyList<string> Headers = new List<string>();
    /// <summary>
    /// Exposes the rows currently stored by this object.
    /// </summary>
    internal IReadOnlyList<string[]> Rows = new List<string[]>();
    /// <summary>
    /// Indicates whether this object has column widths.
    /// </summary>
    internal bool HasColumnWidths;
    /// <summary>
    /// Exposes the column widths currently stored by this object.
    /// </summary>
    internal double[] ColumnWidths = new double[0];

    /// <summary>
    /// Initializes a new report table.
    /// </summary>
    /// <param name="headers">The headers to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    public PdfReportTable(string[] headers)
    {
        if (headers == null)
        {
            throw new ArgumentNullException("headers");
        }

        if (headers.Length == 0)
        {
            throw new ArgumentException("Report table must have at least one header.", "headers");
        }

        _headers = new List<string>();
        for (int index = 0; index < headers.Length; index++)
        {
            if (headers[index] == null)
            {
                throw new ArgumentNullException("headers");
            }

            _headers.Add(headers[index]);
        }

        _rows = new List<string[]>();
        _rowKinds = new List<int>();
        Headers = _headers;
        Rows = _rows;
        HasColumnWidths = false;
        ColumnWidths = new double[0];
        _columnWidths = new double[0];
    }

    /// <summary>
    /// Sets the column widths used for subsequent PDF generation.
    /// </summary>
    /// <param name="columnWidths">The column widths to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric argument falls outside its supported range.</exception>
    public void SetColumnWidths(double[] columnWidths)
    {
        if (columnWidths == null)
        {
            throw new ArgumentNullException("columnWidths");
        }

        if (columnWidths.Length != _headers.Count)
        {
            throw new ArgumentException("Column width count must match header count.", "columnWidths");
        }

        _columnWidths = new double[columnWidths.Length];
        for (int index = 0; index < columnWidths.Length; index++)
        {
            if (double.IsNaN(columnWidths[index]) || double.IsInfinity(columnWidths[index]) || columnWidths[index] <= 0)
            {
                throw new ArgumentOutOfRangeException("columnWidths", "Column widths must be positive.");
            }

            _columnWidths[index] = columnWidths[index];
        }

        ColumnWidths = _columnWidths;
        HasColumnWidths = true;
    }

    /// <summary>
    /// Adds a row to this report table.
    /// </summary>
    /// <param name="cells">The cells to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    public void AddRow(string[] cells)
    {
        if (cells == null)
        {
            throw new ArgumentNullException("cells");
        }

        if (cells.Length != _headers.Count)
        {
            throw new ArgumentException("Cell count must match header count.", "cells");
        }

        string[] copy = new string[cells.Length];
        for (int index = 0; index < cells.Length; index++)
        {
            if (cells[index] == null)
            {
                throw new ArgumentNullException("cells");
            }

            copy[index] = cells[index];
        }

        _rows.Add(copy);
        _rowKinds.Add(0);
    }

    /// <summary>Adds a full-width group heading row.</summary>
    /// <param name="label">The non-empty group label.</param>
    public void AddGroupRow(string label)
    {
        if (label == null)
        {
            throw new ArgumentNullException("label");
        }

        if (label.Length == 0)
        {
            throw new ArgumentException("Report table group label cannot be empty.", "label");
        }

        string[] cells = new string[_headers.Count];
        cells[0] = label;
        for (int index = 1; index < cells.Length; index++)
        {
            cells[index] = string.Empty;
        }

        _rows.Add(cells);
        _rowKinds.Add(1);
    }

    /// <summary>Adds a visually emphasized totals row.</summary>
    /// <param name="cells">The total cells matching the header count.</param>
    public void AddTotalsRow(string[] cells)
    {
        if (cells == null)
        {
            throw new ArgumentNullException("cells");
        }

        if (cells.Length != _headers.Count)
        {
            throw new ArgumentException("Total cell count must match header count.", "cells");
        }

        string[] copy = new string[cells.Length];
        for (int index = 0; index < cells.Length; index++)
        {
            if (cells[index] == null)
            {
                throw new ArgumentNullException("cells");
            }

            copy[index] = cells[index];
        }

        _rows.Add(copy);
        _rowKinds.Add(2);
    }

    internal PdfTableBlock CreateBlock(double totalWidth, PdfParagraphStyle headerStyle, PdfParagraphStyle cellStyle)
    {
        if (headerStyle == null)
        {
            throw new ArgumentNullException("headerStyle");
        }

        if (cellStyle == null)
        {
            throw new ArgumentNullException("cellStyle");
        }

        if (double.IsNaN(totalWidth) || double.IsInfinity(totalWidth) || totalWidth <= 0)
        {
            throw new ArgumentOutOfRangeException("totalWidth", "Report table width must be positive.");
        }

        PdfTableBlock table = new PdfTableBlock(CreateColumnWidths(totalWidth));
        PdfTableRow header = table.AddHeaderRow();
        for (int index = 0; index < _headers.Count; index++)
        {
            PdfTableCell cell = header.AddTextBlock(new PdfTextBlock(_headers[index], headerStyle));
            StyleHeaderCell(cell);
        }

        for (int rowIndex = 0; rowIndex < _rows.Count; rowIndex++)
        {
            PdfTableRow row = table.AddRow();
            if (_rowKinds[rowIndex] == 1)
            {
                PdfTableCell groupCell = row.AddTextBlock(new PdfTextBlock(_rows[rowIndex][0], headerStyle));
                groupCell.SetColumnSpan(_headers.Count);
                StyleGroupCell(groupCell);
            }
            else
            {
                for (int cellIndex = 0; cellIndex < _rows[rowIndex].Length; cellIndex++)
                {
                    PdfParagraphStyle style = cellStyle;
                    if (_rowKinds[rowIndex] == 2)
                    {
                        style = headerStyle;
                    }

                    PdfTableCell cell = row.AddTextBlock(new PdfTextBlock(_rows[rowIndex][cellIndex], style));
                    if (_rowKinds[rowIndex] == 2)
                    {
                        StyleTotalsCell(cell);
                    }
                    else
                    {
                        StyleBodyCell(cell, rowIndex);
                    }
                }
            }
        }

        return table;
    }

    internal double[] CreateColumnWidths(double totalWidth)
    {
        if (HasColumnWidths)
        {
            double[] copy = new double[_columnWidths.Length];
            for (int index = 0; index < _columnWidths.Length; index++)
            {
                copy[index] = _columnWidths[index];
            }

            return copy;
        }

        double[] widths = new double[_headers.Count];
        double width = totalWidth / _headers.Count;
        for (int index = 0; index < widths.Length; index++)
        {
            widths[index] = width;
        }

        return widths;
    }

    internal void StyleHeaderCell(PdfTableCell cell)
    {
        cell.SetPadding(PdfInsets.Uniform(4));
        cell.SetBackground(PdfColor.Gray(0.88));
        cell.SetBorder(new PdfInsets(0, 0, 0, 1));
        cell.SetBorderColor(PdfColor.Gray(0.7));
    }

    internal void StyleBodyCell(PdfTableCell cell, int rowIndex)
    {
        cell.SetPadding(PdfInsets.Uniform(4));
        cell.SetBorder(new PdfInsets(0, 0, 0, 1));
        cell.SetBorderColor(PdfColor.Gray(0.82));
        if (rowIndex % 2 == 1)
        {
            cell.SetBackground(PdfColor.Gray(0.96));
        }
    }

    internal void StyleGroupCell(PdfTableCell cell)
    {
        cell.SetPadding(new PdfInsets(4, 6, 4, 6));
        cell.SetBackground(PdfColor.Gray(0.82));
        cell.SetBorder(new PdfInsets(0, 1, 0, 1));
        cell.SetBorderColor(PdfColor.Gray(0.66));
    }

    internal void StyleTotalsCell(PdfTableCell cell)
    {
        cell.SetPadding(new PdfInsets(4, 6, 4, 6));
        cell.SetBackground(PdfColor.Gray(0.9));
        cell.SetBorder(new PdfInsets(0, 1.5, 0, 0));
        cell.SetBorderColor(PdfColor.Gray(0.55));
    }
}
