using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxWorksheetRange"/> data.
/// </summary>
public class XlsxWorksheetRangeOperations : IXlsxWorksheetRangeOperations
{
    /// <summary>
    /// Initializes a new xlsx worksheet range operations instance.
    /// </summary>
    public XlsxWorksheetRangeOperations()
    {
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetRange"/>.Clear through the Clear operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet range data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetRangeResult Clear(XlsxWorksheetRange data)
    {
        return data.Clear();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetRange"/>.ClearValues through the ClearValues operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet range data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetRangeResult ClearValues(XlsxWorksheetRange data)
    {
        return data.ClearValues();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetRange"/>.ClearStyles through the ClearStyles operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet range data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetRangeResult ClearStyles(XlsxWorksheetRange data)
    {
        return data.ClearStyles();
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetRange"/>.SetStyle through the SetStyle operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet range data to operate on.</param>
    /// <param name="style">The style value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetRangeResult SetStyle(XlsxWorksheetRange data, uint style)
    {
        return data.SetStyle(style);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetRange"/>.SetFormula through the SetFormula operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet range data to operate on.</param>
    /// <param name="formula">The formula value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetRangeResult SetFormula(XlsxWorksheetRange data, string formula)
    {
        return data.SetFormula(formula);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetRange"/>.FillValue through the FillValue operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet range data to operate on.</param>
    /// <param name="value">The value value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetRangeResult FillValue(XlsxWorksheetRange data, XlsxCellValue value)
    {
        return data.FillValue(value);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetRange"/>.FillText through the FillText operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet range data to operate on.</param>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetRangeResult FillText(XlsxWorksheetRange data, string text)
    {
        return data.FillText(text);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetRange"/>.FillNumber through the FillNumber operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet range data to operate on.</param>
    /// <param name="number">The number value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetRangeResult FillNumber(XlsxWorksheetRange data, double number)
    {
        return data.FillNumber(number);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetRange"/>.FillBoolean through the FillBoolean operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet range data to operate on.</param>
    /// <param name="value">The value value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetRangeResult FillBoolean(XlsxWorksheetRange data, bool value)
    {
        return data.FillBoolean(value);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetRange"/>.FillFormula through the FillFormula operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet range data to operate on.</param>
    /// <param name="formula">The formula value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetRangeResult FillFormula(XlsxWorksheetRange data, string formula)
    {
        return data.FillFormula(formula);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetRange"/>.FillSeries through the FillSeries operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet range data to operate on.</param>
    /// <param name="start">The start value used by the operation.</param>
    /// <param name="step">The step value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetRangeResult FillSeries(XlsxWorksheetRange data, double start, double step)
    {
        return data.FillSeries(start, step);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetRange"/>.FillLinearSeries through the FillLinearSeries operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet range data to operate on.</param>
    /// <param name="start">The start value used by the operation.</param>
    /// <param name="step">The step value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetRangeResult FillLinearSeries(XlsxWorksheetRange data, double start, double step)
    {
        return data.FillLinearSeries(start, step);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetRange"/>.CopyTo through the CopyTo operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet range data to operate on.</param>
    /// <param name="topLeftReference">The top left reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetRangeResult CopyTo(XlsxWorksheetRange data, string topLeftReference)
    {
        return data.CopyTo(topLeftReference);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetRange"/>.MoveTo through the MoveTo operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet range data to operate on.</param>
    /// <param name="topLeftReference">The top left reference value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetRangeResult MoveTo(XlsxWorksheetRange data, string topLeftReference)
    {
        return data.MoveTo(topLeftReference);
    }

    /// <summary>
    /// Invokes <see cref="XlsxWorksheetRange"/>.Transpose through the Transpose operation.
    /// </summary>
    /// <param name="data">The xlsx worksheet range data to operate on.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxWorksheetRangeResult Transpose(XlsxWorksheetRange data)
    {
        return data.Transpose();
    }
}
