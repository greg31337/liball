using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Defines public operations for <see cref="XlsxChartPart"/> data.
/// </summary>
public interface IXlsxChartPartOperations
{
    /// <summary>Creates a chart with one typed plot kind.</summary>
    /// <param name="chartKind">The initial plot kind.</param>
    /// <param name="title">The chart title.</param>
    /// <returns>The new chart part.</returns>
    XlsxChartPart CreateChart(XlsxChartKind chartKind, string title);
    /// <summary>Adds a typed plot kind, forming a combo chart when another plot already exists.</summary>
    /// <param name="data">The chart part to update.</param>
    /// <param name="chartKind">The plot kind to add.</param>
    /// <returns>Validation diagnostics for the operation.</returns>
    XlsxValidationResult AddChartKind(XlsxChartPart data, XlsxChartKind chartKind);
    /// <summary>
    /// Invokes <see cref="XlsxChartPart"/>.AddSeries through the AddSeries operation.
    /// </summary>
    /// <param name="data">The xlsx chart part data to operate on.</param>
    /// <param name="textFormula">The text formula value used by the operation.</param>
    /// <param name="categoryFormula">The category formula value used by the operation.</param>
    /// <param name="valuesFormula">The values formula value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    XlsxChartSeriesReference AddSeries(XlsxChartPart data, string textFormula, string categoryFormula, string valuesFormula);
    /// <summary>Adds a series assigned to a specific plot in a typed or combo chart.</summary>
    /// <param name="data">The chart part to update.</param><param name="chartKind">The plot kind.</param><param name="textFormula">The series-name formula.</param><param name="categoryFormula">The category or X-value formula.</param><param name="valuesFormula">The values or Y-value formula.</param><param name="bubbleSizeFormula">The bubble-size formula.</param>
    /// <returns>The new series.</returns>
    XlsxChartSeriesReference AddSeriesForChart(XlsxChartPart data, XlsxChartKind chartKind, string textFormula, string categoryFormula, string valuesFormula, string bubbleSizeFormula);
    /// <summary>Adds a typed category or value axis.</summary>
    /// <param name="data">The chart part to update.</param><param name="axisType">The axis type.</param><param name="axisId">The axis identifier.</param><param name="crossAxisId">The crossing axis identifier.</param><param name="position">The axis position.</param><param name="title">The axis title.</param>
    /// <returns>The new axis.</returns>
    XlsxChartAxis AddAxis(XlsxChartPart data, string axisType, string axisId, string crossAxisId, string position, string title);
    /// <summary>Sets a manual plot-area layout.</summary>
    /// <param name="data">The chart part to update.</param><param name="x">The horizontal position.</param><param name="y">The vertical position.</param><param name="width">The width.</param><param name="height">The height.</param>
    void SetManualLayout(XlsxChartPart data, string x, string y, string width, string height);
    /// <summary>Configures the chart data table.</summary>
    /// <param name="data">The chart part to update.</param><param name="showHorizontalBorder">Whether to show horizontal borders.</param><param name="showVerticalBorder">Whether to show vertical borders.</param><param name="showOutline">Whether to show the outline.</param><param name="showLegendKeys">Whether to show legend keys.</param>
    void SetDataTable(XlsxChartPart data, bool showHorizontalBorder, bool showVerticalBorder, bool showOutline, bool showLegendKeys);
    /// <summary>Edits the chart title, style, and legend.</summary>
    /// <param name="data">The chart part to update.</param><param name="title">The title.</param><param name="styleId">The style identifier.</param><param name="legendPosition">The legend position.</param><param name="legendOverlay">Whether the legend overlays the plot.</param>
    void SetPresentation(XlsxChartPart data, string title, uint styleId, string legendPosition, bool legendOverlay);
    /// <summary>Edits axis gridlines and scale bounds.</summary>
    /// <param name="data">The chart part to update.</param><param name="axisIndex">The axis index.</param><param name="major">Whether major gridlines are shown.</param><param name="majorColorRgb">The major gridline color.</param><param name="minor">Whether minor gridlines are shown.</param><param name="minorColorRgb">The minor gridline color.</param><param name="minimum">The minimum scale value.</param><param name="maximum">The maximum scale value.</param>
    /// <returns>Validation diagnostics for the operation.</returns>
    XlsxValidationResult SetAxisGridlines(XlsxChartPart data, int axisIndex, bool major, string majorColorRgb, bool minor, string minorColorRgb, string minimum, string maximum);
    /// <summary>Edits series color and data labels.</summary>
    /// <param name="data">The chart part to update.</param><param name="seriesIndex">The series index.</param><param name="colorRgb">The RGB color.</param><param name="labelPosition">The label position.</param><param name="showValue">Whether to show values.</param><param name="showCategoryName">Whether to show category names.</param><param name="showSeriesName">Whether to show series names.</param>
    /// <returns>Validation diagnostics for the operation.</returns>
    XlsxValidationResult SetSeriesAppearance(XlsxChartPart data, int seriesIndex, string colorRgb, string labelPosition, bool showValue, bool showCategoryName, bool showSeriesName);
    /// <summary>Edits the trendline for a series.</summary>
    /// <param name="data">The chart part to update.</param><param name="seriesIndex">The series index.</param><param name="trendlineType">The trendline type.</param><param name="name">The trendline name.</param><param name="order">The polynomial order.</param><param name="period">The moving-average period.</param><param name="displayEquation">Whether to display the equation.</param><param name="displayRSquared">Whether to display R-squared.</param>
    /// <returns>Validation diagnostics for the operation.</returns>
    XlsxValidationResult SetSeriesTrendline(XlsxChartPart data, int seriesIndex, string trendlineType, string name, int order, int period, bool displayEquation, bool displayRSquared);
    /// <summary>Edits error bars for a series.</summary>
    /// <param name="data">The chart part to update.</param><param name="seriesIndex">The series index.</param><param name="direction">The error direction.</param><param name="barType">The error bar type.</param><param name="valueType">The error value type.</param><param name="value">The error value.</param><param name="noEndCap">Whether end caps are suppressed.</param>
    /// <returns>Validation diagnostics for the operation.</returns>
    XlsxValidationResult SetSeriesErrorBars(XlsxChartPart data, int seriesIndex, string direction, string barType, string valueType, string value, bool noEndCap);
}
