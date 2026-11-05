using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxChartPart"/> data.
/// </summary>
public class XlsxChartPartOperations : IXlsxChartPartOperations
{
    /// <summary>
    /// Initializes a new xlsx chart part operations instance.
    /// </summary>
    public XlsxChartPartOperations()
    {
    }

    /// <summary>Creates a chart with one typed plot kind.</summary>
    /// <param name="chartKind">The initial plot kind.</param><param name="title">The chart title.</param>
    /// <returns>The new chart.</returns>
    public XlsxChartPart CreateChart(XlsxChartKind chartKind, string title)
    {
        XlsxChartPart chart = new XlsxChartPart();
        chart.TitleText = XlsxCellValueConverter.NormalizeText(title);
        chart.HasTitle = chart.TitleText.Length > 0;
        chart.AddChartKind(chartKind);
        return chart;
    }

    /// <summary>Adds a typed plot kind.</summary>
    /// <param name="data">The chart part.</param><param name="chartKind">The plot kind.</param>
    /// <returns>Validation diagnostics.</returns>
    public XlsxValidationResult AddChartKind(XlsxChartPart data, XlsxChartKind chartKind)
    {
        return data.AddChartKind(chartKind);
    }

    /// <summary>
    /// Invokes <see cref="XlsxChartPart"/>.AddSeries through the AddSeries operation.
    /// </summary>
    /// <param name="data">The xlsx chart part data to operate on.</param>
    /// <param name="textFormula">The text formula value used by the operation.</param>
    /// <param name="categoryFormula">The category formula value used by the operation.</param>
    /// <param name="valuesFormula">The values formula value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxChartSeriesReference AddSeries(XlsxChartPart data, string textFormula, string categoryFormula, string valuesFormula)
    {
        return data.AddSeries(textFormula, categoryFormula, valuesFormula);
    }

    /// <summary>Adds a series assigned to a typed plot.</summary>
    /// <param name="data">The chart.</param><param name="chartKind">The plot kind.</param><param name="textFormula">The text formula.</param><param name="categoryFormula">The category formula.</param><param name="valuesFormula">The values formula.</param><param name="bubbleSizeFormula">The bubble-size formula.</param>
    /// <returns>The new series.</returns>
    public XlsxChartSeriesReference AddSeriesForChart(XlsxChartPart data, XlsxChartKind chartKind, string textFormula, string categoryFormula, string valuesFormula, string bubbleSizeFormula)
    {
        return data.AddSeriesForChart(chartKind, textFormula, categoryFormula, valuesFormula, bubbleSizeFormula);
    }

    /// <summary>Adds an axis.</summary>
    /// <param name="data">The chart.</param><param name="axisType">The type.</param><param name="axisId">The identifier.</param><param name="crossAxisId">The crossing identifier.</param><param name="position">The position.</param><param name="title">The title.</param>
    /// <returns>The new axis.</returns>
    public XlsxChartAxis AddAxis(XlsxChartPart data, string axisType, string axisId, string crossAxisId, string position, string title)
    {
        return data.AddAxis(axisType, axisId, crossAxisId, position, title);
    }

    /// <summary>Sets a manual chart layout.</summary>
    /// <param name="data">The chart.</param><param name="x">The horizontal position.</param><param name="y">The vertical position.</param><param name="width">The width.</param><param name="height">The height.</param>
    public void SetManualLayout(XlsxChartPart data, string x, string y, string width, string height)
    {
        data.SetManualLayout(x, y, width, height);
    }

    /// <summary>Sets the chart data table.</summary>
    /// <param name="data">The chart.</param><param name="showHorizontalBorder">Whether horizontal borders are shown.</param><param name="showVerticalBorder">Whether vertical borders are shown.</param><param name="showOutline">Whether the outline is shown.</param><param name="showLegendKeys">Whether legend keys are shown.</param>
    public void SetDataTable(XlsxChartPart data, bool showHorizontalBorder, bool showVerticalBorder, bool showOutline, bool showLegendKeys)
    {
        data.SetDataTable(showHorizontalBorder, showVerticalBorder, showOutline, showLegendKeys);
    }

    /// <summary>Sets chart title, style, and legend.</summary>
    /// <param name="data">The chart.</param><param name="title">The title.</param><param name="styleId">The style.</param><param name="legendPosition">The legend position.</param><param name="legendOverlay">Whether the legend overlays the plot.</param>
    public void SetPresentation(XlsxChartPart data, string title, uint styleId, string legendPosition, bool legendOverlay)
    {
        data.SetPresentation(title, styleId, legendPosition, legendOverlay);
    }

    /// <summary>Sets axis gridlines and bounds.</summary>
    /// <param name="data">The chart.</param><param name="axisIndex">The axis index.</param><param name="major">Whether major gridlines are shown.</param><param name="majorColorRgb">The major color.</param><param name="minor">Whether minor gridlines are shown.</param><param name="minorColorRgb">The minor color.</param><param name="minimum">The minimum.</param><param name="maximum">The maximum.</param>
    /// <returns>Validation diagnostics.</returns>
    public XlsxValidationResult SetAxisGridlines(XlsxChartPart data, int axisIndex, bool major, string majorColorRgb, bool minor, string minorColorRgb, string minimum, string maximum)
    {
        return data.SetAxisGridlines(axisIndex, major, majorColorRgb, minor, minorColorRgb, minimum, maximum);
    }

    /// <summary>Sets series appearance.</summary>
    /// <param name="data">The chart.</param><param name="seriesIndex">The series index.</param><param name="colorRgb">The color.</param><param name="labelPosition">The label position.</param><param name="showValue">Whether values are shown.</param><param name="showCategoryName">Whether categories are shown.</param><param name="showSeriesName">Whether names are shown.</param>
    /// <returns>Validation diagnostics.</returns>
    public XlsxValidationResult SetSeriesAppearance(XlsxChartPart data, int seriesIndex, string colorRgb, string labelPosition, bool showValue, bool showCategoryName, bool showSeriesName)
    {
        return data.SetSeriesAppearance(seriesIndex, colorRgb, labelPosition, showValue, showCategoryName, showSeriesName);
    }

    /// <summary>Sets a series trendline.</summary>
    /// <param name="data">The chart.</param><param name="seriesIndex">The series index.</param><param name="trendlineType">The type.</param><param name="name">The name.</param><param name="order">The order.</param><param name="period">The period.</param><param name="displayEquation">Whether to show the equation.</param><param name="displayRSquared">Whether to show R-squared.</param>
    /// <returns>Validation diagnostics.</returns>
    public XlsxValidationResult SetSeriesTrendline(XlsxChartPart data, int seriesIndex, string trendlineType, string name, int order, int period, bool displayEquation, bool displayRSquared)
    {
        return data.SetSeriesTrendline(seriesIndex, trendlineType, name, order, period, displayEquation, displayRSquared);
    }

    /// <summary>Sets series error bars.</summary>
    /// <param name="data">The chart.</param><param name="seriesIndex">The series index.</param><param name="direction">The direction.</param><param name="barType">The bar type.</param><param name="valueType">The value type.</param><param name="value">The value.</param><param name="noEndCap">Whether end caps are suppressed.</param>
    /// <returns>Validation diagnostics.</returns>
    public XlsxValidationResult SetSeriesErrorBars(XlsxChartPart data, int seriesIndex, string direction, string barType, string valueType, string value, bool noEndCap)
    {
        return data.SetSeriesErrorBars(seriesIndex, direction, barType, valueType, value, noEndCap);
    }
}
