using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides instance-based public access to <see cref="XlsxA1ColumnNameConverter"/> operations.
/// </summary>
public class XlsxA1ColumnNameConverterOperations : IXlsxA1ColumnNameConverterOperations
{
    /// <summary>
    /// Initializes a new XlsxA1ColumnNameConverterOperations instance.
    /// </summary>
    public XlsxA1ColumnNameConverterOperations()
    {
    }

    /// <summary>
    /// Invokes the ParseString form of <see cref="XlsxA1ColumnNameConverter"/>.Parse.
    /// </summary>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxA1ColumnNameResult ParseString(string text)
    {
        return XlsxA1ColumnNameConverter.Parse(text);
    }

    /// <summary>
    /// Invokes the ParseSpan form of <see cref="XlsxA1ColumnNameConverter"/>.Parse.
    /// </summary>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxA1ColumnNameResult ParseSpan(System.ReadOnlySpan<char> text)
    {
        return XlsxA1ColumnNameConverter.Parse(text);
    }

    /// <summary>
    /// Invokes the ParseLeading form of <see cref="XlsxA1ColumnNameConverter"/>.ParseLeading.
    /// </summary>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxA1ColumnNameResult ParseLeading(System.ReadOnlySpan<char> text)
    {
        return XlsxA1ColumnNameConverter.ParseLeading(text);
    }

    /// <summary>
    /// Invokes the Format form of <see cref="XlsxA1ColumnNameConverter"/>.Format.
    /// </summary>
    /// <param name="column">The column value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxA1ColumnNameResult Format(int column)
    {
        return XlsxA1ColumnNameConverter.Format(column);
    }

    /// <summary>
    /// Invokes the FormatToSpan form of <see cref="XlsxA1ColumnNameConverter"/>.Format.
    /// </summary>
    /// <param name="column">The column value used by the operation.</param>
    /// <param name="destination">The destination value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxA1ColumnNameResult FormatToSpan(int column, System.Span<char> destination)
    {
        return XlsxA1ColumnNameConverter.Format(column, destination);
    }

    /// <summary>
    /// Invokes the FormatString form of <see cref="XlsxA1ColumnNameConverter"/>.FormatString.
    /// </summary>
    /// <param name="column">The column value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public string FormatString(int column)
    {
        return XlsxA1ColumnNameConverter.FormatString(column);
    }
}
