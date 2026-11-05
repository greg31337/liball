using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Defines public operations for <see cref="XlsxCellValue"/> data.
/// </summary>
public interface IXlsxCellValueOperations
{
    /// <summary>
    /// Invokes <see cref="XlsxCellValue"/>.CreateBlank through the CreateBlank operation.
    /// </summary>
    /// <returns>The value produced by the operation.</returns>
    XlsxCellValue CreateBlank();
    /// <summary>
    /// Invokes <see cref="XlsxCellValue"/>.CreateNumber through the CreateNumber operation.
    /// </summary>
    /// <param name="number">The number value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxCellValue CreateNumber(double number);
    /// <summary>Creates an exact invariant decimal cell value.</summary>
    /// <param name="number">The decimal number.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxCellValue CreateDecimal(decimal number);
    /// <summary>
    /// Invokes <see cref="XlsxCellValue"/>.CreateText through the CreateText operation.
    /// </summary>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxCellValue CreateText(string? text);
    /// <summary>
    /// Invokes <see cref="XlsxCellValue"/>.CreateSharedString through the CreateSharedString operation.
    /// </summary>
    /// <param name="index">The index value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxCellValue CreateSharedString(uint index);
    /// <summary>
    /// Invokes <see cref="XlsxCellValue"/>.CreateInlineString through the CreateInlineString operation.
    /// </summary>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxCellValue CreateInlineString(string text);
    /// <summary>
    /// Invokes <see cref="XlsxCellValue"/>.CreateBoolean through the CreateBoolean operation.
    /// </summary>
    /// <param name="boolean">The boolean value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxCellValue CreateBoolean(bool boolean);
    /// <summary>
    /// Invokes <see cref="XlsxCellValue"/>.CreateError through the CreateError operation.
    /// </summary>
    /// <param name="error">The error value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxCellValue CreateError(string error);
    /// <summary>Creates a typed Excel error cell value.</summary>
    /// <param name="error">The Excel error kind.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxCellValue CreateTypedError(XlsxCellErrorKind error);
    /// <summary>
    /// Invokes <see cref="XlsxCellValue"/>.CreateDateTime through the CreateDateTime operation.
    /// </summary>
    /// <param name="dateTime">The date time value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxCellValue CreateDateTime(System.DateTime dateTime);
    /// <summary>Creates a date cell from exact ISO date or date-time text.</summary>
    /// <param name="isoDateText">ISO date or date-time text.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxCellValue CreateIsoDate(string isoDateText);
    /// <summary>Creates an elapsed duration stored as an exact Excel day fraction.</summary>
    /// <param name="duration">The elapsed duration.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxCellValue CreateDuration(System.TimeSpan duration);
    /// <summary>Creates a formula value with a typed cached result.</summary>
    /// <param name="formula">Excel formula text without a leading equals sign.</param>
    /// <param name="cachedValue">The typed cached formula result.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxCellValue CreateFormulaValue(string formula, XlsxCellValue cachedValue);
    /// <summary>
    /// Invokes <see cref="XlsxCellValue"/>.CreateRichText through the CreateRichText operation.
    /// </summary>
    /// <param name="plainText">The plain text value used by the operation.</param>
    /// <param name="rawXml">The raw xml value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxCellValue CreateRichText(string plainText, string rawXml);
    /// <summary>Creates a typed rich-text cell value including phonetic content.</summary>
    /// <param name="inlineString">The typed inline string content.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxCellValue CreateRichTextValue(XlsxInlineString inlineString);
}
