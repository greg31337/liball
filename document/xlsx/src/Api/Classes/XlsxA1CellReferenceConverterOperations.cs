using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides instance-based public access to <see cref="XlsxA1CellReferenceConverter"/> operations.
/// </summary>
public class XlsxA1CellReferenceConverterOperations : IXlsxA1CellReferenceConverterOperations
{
    /// <summary>
    /// Initializes a new XlsxA1CellReferenceConverterOperations instance.
    /// </summary>
    public XlsxA1CellReferenceConverterOperations()
    {
    }

    /// <summary>
    /// Invokes the ParseString form of <see cref="XlsxA1CellReferenceConverter"/>.Parse.
    /// </summary>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxA1CellReferenceResult ParseString(string? text)
    {
        return XlsxA1CellReferenceConverter.Parse(text);
    }

    /// <summary>
    /// Invokes the ParseSpan form of <see cref="XlsxA1CellReferenceConverter"/>.Parse.
    /// </summary>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxA1CellReferenceResult ParseSpan(System.ReadOnlySpan<char> text)
    {
        return XlsxA1CellReferenceConverter.Parse(text);
    }

    /// <summary>
    /// Invokes the ParseLeading form of <see cref="XlsxA1CellReferenceConverter"/>.ParseLeading.
    /// </summary>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxA1CellReferenceResult ParseLeading(System.ReadOnlySpan<char> text)
    {
        return XlsxA1CellReferenceConverter.ParseLeading(text);
    }

    /// <summary>
    /// Invokes the FormatValue form of <see cref="XlsxA1CellReferenceConverter"/>.Format.
    /// </summary>
    /// <param name="value">The value value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxA1CellReferenceResult FormatValue(XlsxCellReferenceValue value)
    {
        return XlsxA1CellReferenceConverter.Format(value);
    }

    /// <summary>
    /// Invokes the FormatCoordinates form of <see cref="XlsxA1CellReferenceConverter"/>.Format.
    /// </summary>
    /// <param name="row">The row value used by the operation.</param>
    /// <param name="column">The column value used by the operation.</param>
    /// <param name="absoluteRow">The absoluteRow value used by the operation.</param>
    /// <param name="absoluteColumn">The absoluteColumn value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxA1CellReferenceResult FormatCoordinates(int row, int column, bool absoluteRow, bool absoluteColumn)
    {
        return XlsxA1CellReferenceConverter.Format(row, column, absoluteRow, absoluteColumn);
    }

    /// <summary>
    /// Invokes the FormatToSpan form of <see cref="XlsxA1CellReferenceConverter"/>.Format.
    /// </summary>
    /// <param name="row">The row value used by the operation.</param>
    /// <param name="column">The column value used by the operation.</param>
    /// <param name="absoluteRow">The absoluteRow value used by the operation.</param>
    /// <param name="absoluteColumn">The absoluteColumn value used by the operation.</param>
    /// <param name="destination">The destination value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxA1CellReferenceResult FormatToSpan(int row, int column, bool absoluteRow, bool absoluteColumn, System.Span<char> destination)
    {
        return XlsxA1CellReferenceConverter.Format(row, column, absoluteRow, absoluteColumn, destination);
    }

    /// <summary>
    /// Invokes the FormatString form of <see cref="XlsxA1CellReferenceConverter"/>.FormatString.
    /// </summary>
    /// <param name="row">The row value used by the operation.</param>
    /// <param name="column">The column value used by the operation.</param>
    /// <param name="absoluteRow">The absoluteRow value used by the operation.</param>
    /// <param name="absoluteColumn">The absoluteColumn value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public string FormatString(int row, int column, bool absoluteRow, bool absoluteColumn)
    {
        return XlsxA1CellReferenceConverter.FormatString(row, column, absoluteRow, absoluteColumn);
    }
}
