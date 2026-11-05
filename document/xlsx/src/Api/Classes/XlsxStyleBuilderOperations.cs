using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxStyleBuilder"/> data.
/// </summary>
public class XlsxStyleBuilderOperations : IXlsxStyleBuilderOperations
{
    /// <summary>
    /// Initializes a new xlsx style builder operations instance.
    /// </summary>
    public XlsxStyleBuilderOperations()
    {
    }

    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.SetNumberFormat through the SetNumberFormat operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <param name="format">The format value used by the operation.</param>
    public void SetNumberFormat(XlsxStyleBuilder data, string format)
    {
        data.SetNumberFormat(format);
    }

    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.SetFontName through the SetFontName operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <param name="name">The name value used by the operation.</param>
    public void SetFontName(XlsxStyleBuilder data, string name)
    {
        data.SetFontName(name);
    }

    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.SetFontSize through the SetFontSize operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <param name="size">The size value used by the operation.</param>
    public void SetFontSize(XlsxStyleBuilder data, double size)
    {
        data.SetFontSize(size);
    }

    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.SetBold through the SetBold operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <param name="value">The value value used by the operation.</param>
    public void SetBold(XlsxStyleBuilder data, bool value)
    {
        data.SetBold(value);
    }

    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.SetItalic through the SetItalic operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <param name="value">The value value used by the operation.</param>
    public void SetItalic(XlsxStyleBuilder data, bool value)
    {
        data.SetItalic(value);
    }

    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.SetFontColor through the SetFontColor operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <param name="rgb">The rgb value used by the operation.</param>
    public void SetFontColor(XlsxStyleBuilder data, string rgb)
    {
        data.SetFontColor(rgb);
    }

    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.SetFillColor through the SetFillColor operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <param name="rgb">The rgb value used by the operation.</param>
    public void SetFillColor(XlsxStyleBuilder data, string? rgb)
    {
        data.SetFillColor(rgb);
    }

    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.SetBorder through the SetBorder operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <param name="side">The side value used by the operation.</param>
    /// <param name="style">The style value used by the operation.</param>
    /// <param name="rgb">The rgb value used by the operation.</param>
    public void SetBorder(XlsxStyleBuilder data, string side, string style, string rgb)
    {
        data.SetBorder(side, style, rgb);
    }

    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.SetHorizontalAlignment through the SetHorizontalAlignment operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <param name="value">The value value used by the operation.</param>
    public void SetHorizontalAlignment(XlsxStyleBuilder data, string value)
    {
        data.SetHorizontalAlignment(value);
    }

    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.SetVerticalAlignment through the SetVerticalAlignment operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <param name="value">The value value used by the operation.</param>
    public void SetVerticalAlignment(XlsxStyleBuilder data, string value)
    {
        data.SetVerticalAlignment(value);
    }

    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.SetWrapText through the SetWrapText operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <param name="value">The value value used by the operation.</param>
    public void SetWrapText(XlsxStyleBuilder data, bool value)
    {
        data.SetWrapText(value);
    }

    /// <summary>Sets a complete typed font.</summary>
    /// <param name="data">The style builder.</param><param name="value">The typed font.</param>
    public void SetTypedFont(XlsxStyleBuilder data, XlsxStyleFont value) { data.SetTypedFont(value); }

    /// <summary>Sets a complete typed fill.</summary>
    /// <param name="data">The style builder.</param><param name="value">The typed fill.</param>
    public void SetTypedFill(XlsxStyleBuilder data, XlsxStyleFill value) { data.SetTypedFill(value); }

    /// <summary>Sets a complete typed border.</summary>
    /// <param name="data">The style builder.</param><param name="value">The typed border.</param>
    public void SetTypedBorder(XlsxStyleBuilder data, XlsxStyleBorder value) { data.SetTypedBorder(value); }

    /// <summary>Sets complete typed alignment.</summary>
    /// <param name="data">The style builder.</param><param name="value">The typed alignment.</param>
    public void SetTypedAlignment(XlsxStyleBuilder data, XlsxStyleAlignment value) { data.SetTypedAlignment(value); }

    /// <summary>Sets complete typed protection.</summary>
    /// <param name="data">The style builder.</param><param name="value">The typed protection.</param>
    public void SetTypedProtection(XlsxStyleBuilder data, XlsxStyleProtection value) { data.SetTypedProtection(value); }

    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.AddToWorkbook through the AddToWorkbook operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <param name="workbook">The workbook value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public int AddToWorkbook(XlsxStyleBuilder data, XlsxWorkbook? workbook)
    {
        return data.AddToWorkbook(workbook);
    }

    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.AddToStylesPart through the AddToStylesPart operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <param name="styles">The styles value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public int AddToStylesPart(XlsxStyleBuilder data, XlsxStylesPart? styles)
    {
        return data.AddToStylesPart(styles);
    }

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
    public XlsxCellFormatDefinition BuildCellFormat(XlsxStyleBuilder data, uint numberFormatId, bool hasNumberFormatId, uint fontId, bool hasFontId, uint fillId, bool hasFillId, uint borderId, bool hasBorderId)
    {
        return data.BuildCellFormat(numberFormatId, hasNumberFormatId, fontId, hasFontId, fillId, hasFillId, borderId, hasBorderId);
    }

    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.HasFontValues through the HasFontValues operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public bool HasFontValues(XlsxStyleBuilder data)
    {
        return data.HasFontValues();
    }

    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.HasAlignmentValues through the HasAlignmentValues operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public bool HasAlignmentValues(XlsxStyleBuilder data)
    {
        return data.HasAlignmentValues();
    }

    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.FindBorder through the FindBorder operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <param name="side">The side value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxStyleBorderSetting? FindBorder(XlsxStyleBuilder data, string side)
    {
        return data.FindBorder(side);
    }

    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.BuildFontRawXml through the BuildFontRawXml operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public string BuildFontRawXml(XlsxStyleBuilder data)
    {
        return data.BuildFontRawXml();
    }

    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.BuildFillRawXml through the BuildFillRawXml operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public string BuildFillRawXml(XlsxStyleBuilder data)
    {
        return data.BuildFillRawXml();
    }

    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.BuildBorderRawXml through the BuildBorderRawXml operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public string BuildBorderRawXml(XlsxStyleBuilder data)
    {
        return data.BuildBorderRawXml();
    }

    /// <summary>
    /// Invokes <see cref="XlsxStyleBuilder"/>.BuildCellFormatRawXml through the BuildCellFormatRawXml operation.
    /// </summary>
    /// <param name="data">The xlsx style builder data to operate on.</param>
    /// <param name="format">The format value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public string BuildCellFormatRawXml(XlsxStyleBuilder data, XlsxCellFormatDefinition format)
    {
        return data.BuildCellFormatRawXml(format);
    }
}
