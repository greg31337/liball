using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Defines public operations for <see cref="XlsxStyleBuilder"/> data.
/// </summary>
public interface IXlsxStyleBuilderOperations
{
    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.SetNumberFormat through the SetNumberFormat operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <param name="format">The format value used by the operation.</param>
    void SetNumberFormat(XlsxStyleBuilder data, string format);
    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.SetFontName through the SetFontName operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    void SetFontName(XlsxStyleBuilder data, string name);
    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.SetFontSize through the SetFontSize operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <param name="size">The size value used by the operation.</param>
    void SetFontSize(XlsxStyleBuilder data, double size);
    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.SetBold through the SetBold operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <param name="value">The value value used by the operation.</param>
    void SetBold(XlsxStyleBuilder data, bool value);
    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.SetItalic through the SetItalic operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <param name="value">The value value used by the operation.</param>
    void SetItalic(XlsxStyleBuilder data, bool value);
    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.SetFontColor through the SetFontColor operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <param name="rgb">The rgb value used by the operation.</param>
    void SetFontColor(XlsxStyleBuilder data, string rgb);
    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.SetFillColor through the SetFillColor operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <param name="rgb">The rgb value used by the operation.</param>
    void SetFillColor(XlsxStyleBuilder data, string? rgb);
    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.SetBorder through the SetBorder operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <param name="side">The side value used by the operation.</param>
    /// <param name="style">The style value used by the operation.</param>
    /// <param name="rgb">The rgb value used by the operation.</param>
    void SetBorder(XlsxStyleBuilder data, string side, string style, string rgb);
    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.SetHorizontalAlignment through the SetHorizontalAlignment operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <param name="value">The value value used by the operation.</param>
    void SetHorizontalAlignment(XlsxStyleBuilder data, string value);
    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.SetVerticalAlignment through the SetVerticalAlignment operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <param name="value">The value value used by the operation.</param>
    void SetVerticalAlignment(XlsxStyleBuilder data, string value);
    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.SetWrapText through the SetWrapText operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <param name="value">The value value used by the operation.</param>
    void SetWrapText(XlsxStyleBuilder data, bool value);
    /// <summary>Sets a complete typed font.</summary>
    /// <param name="data">The style builder.</param><param name="value">The typed font.</param>
    void SetTypedFont(XlsxStyleBuilder data, XlsxStyleFont value);
    /// <summary>Sets a complete typed fill.</summary>
    /// <param name="data">The style builder.</param><param name="value">The typed fill.</param>
    void SetTypedFill(XlsxStyleBuilder data, XlsxStyleFill value);
    /// <summary>Sets a complete typed border.</summary>
    /// <param name="data">The style builder.</param><param name="value">The typed border.</param>
    void SetTypedBorder(XlsxStyleBuilder data, XlsxStyleBorder value);
    /// <summary>Sets complete typed alignment.</summary>
    /// <param name="data">The style builder.</param><param name="value">The typed alignment.</param>
    void SetTypedAlignment(XlsxStyleBuilder data, XlsxStyleAlignment value);
    /// <summary>Sets complete typed protection.</summary>
    /// <param name="data">The style builder.</param><param name="value">The typed protection.</param>
    void SetTypedProtection(XlsxStyleBuilder data, XlsxStyleProtection value);
    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.AddToWorkbook through the AddToWorkbook operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <param name="workbook">The workbook value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    int AddToWorkbook(XlsxStyleBuilder data, XlsxWorkbook? workbook);
    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.AddToStylesPart through the AddToStylesPart operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <param name="styles">The styles value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    int AddToStylesPart(XlsxStyleBuilder data, XlsxStylesPart? styles);
    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.BuildCellFormat through the BuildCellFormat operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <param name="numberFormatId">The number format id value used by the operation.</param>
    /// <param name="hasNumberFormatId">The has number format id value used by the operation.</param>
    /// <param name="fontId">The font id value used by the operation.</param>
    /// <param name="hasFontId">The has font id value used by the operation.</param>
    /// <param name="fillId">The fill id value used by the operation.</param>
    /// <param name="hasFillId">The has fill id value used by the operation.</param>
    /// <param name="borderId">The border id value used by the operation.</param>
    /// <param name="hasBorderId">The has border id value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxCellFormatDefinition BuildCellFormat(XlsxStyleBuilder data, uint numberFormatId, bool hasNumberFormatId, uint fontId, bool hasFontId, uint fillId, bool hasFillId, uint borderId, bool hasBorderId);
    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.HasFontValues through the HasFontValues operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    bool HasFontValues(XlsxStyleBuilder data);
    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.HasAlignmentValues through the HasAlignmentValues operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    bool HasAlignmentValues(XlsxStyleBuilder data);
    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.FindBorder through the FindBorder operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <param name="side">The side value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxStyleBorderSetting? FindBorder(XlsxStyleBuilder data, string side);
    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.BuildFontRawXml through the BuildFontRawXml operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    string BuildFontRawXml(XlsxStyleBuilder data);
    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.BuildFillRawXml through the BuildFillRawXml operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    string BuildFillRawXml(XlsxStyleBuilder data);
    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.BuildBorderRawXml through the BuildBorderRawXml operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    string BuildBorderRawXml(XlsxStyleBuilder data);
    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.BuildCellFormatRawXml through the BuildCellFormatRawXml operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <param name="format">The format value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    string BuildCellFormatRawXml(XlsxStyleBuilder data, XlsxCellFormatDefinition format);
}
