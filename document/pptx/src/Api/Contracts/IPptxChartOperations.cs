using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxChartOperations"/>.
/// </summary>
public interface IPptxChartOperations
{
    /// <summary>
    /// Reports whether the chart contains or enables part.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool HasPart(PptxChart target);
    /// <summary>
    /// Returns the relationships without changing the chart.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<PptxRelationship> GetRelationships(PptxChart target);
    /// <summary>
    /// Returns the related parts without changing the chart.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<PptxPart> GetRelatedParts(PptxChart target);
    /// <summary>
    /// Returns the data without changing the chart.
    /// </summary>
    /// <returns>The resulting chart data value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxChartData GetData(PptxChart target);
    /// <summary>
    /// Extracts data table from the chart without removing it.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<List<string>> ExtractDataTable(PptxChart target);
    /// <summary>
    /// Returns the chart type without changing the chart.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetChartType(PptxChart target);
    /// <summary>
    /// Updates the chart type and the underlying presentation data.
    /// </summary>
    /// <param name="chartType">The chart type value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetChartType(PptxChart target, string chartType);
    /// <summary>
    /// Updates the data and the underlying presentation data.
    /// </summary>
    /// <param name="data">The caller-supplied data to apply.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetData(PptxChart target, PptxChartData data);
    /// <summary>
    /// Updates the data and the underlying presentation data.
    /// </summary>
    /// <param name="chartType">The chart type value to use.</param>
    /// <param name="data">The caller-supplied data to apply.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetDataWithChartType(PptxChart target, string chartType, PptxChartData data);
    /// <summary>
    /// Applies the update caches operation to the current chart.
    /// </summary>
    /// <param name="data">The caller-supplied data to apply.</param>
    /// <param name="target">The data value to operate on.</param>
    void UpdateCaches(PptxChart target, PptxChartData data);
    /// <summary>
    /// Updates the embedded workbook and the underlying presentation data.
    /// </summary>
    /// <param name="data">The caller-supplied data to apply.</param>
    /// <returns>The resulting part value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxPart SetEmbeddedWorkbook(PptxChart target, PptxChartData data);
    /// <summary>
    /// Updates the embedded workbook bytes and the underlying presentation data.
    /// </summary>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <returns>The resulting part value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxPart SetEmbeddedWorkbookBytes(PptxChart target, byte[] bytes);
    /// <summary>
    /// Returns the embedded workbook part without changing the chart.
    /// </summary>
    /// <returns>The embedded workbook part, or <c>null</c> when none is related.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxPart? GetEmbeddedWorkbookPart(PptxChart target);
    /// <summary>
    /// Returns the external data relationship id without changing the chart.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetExternalDataRelationshipId(PptxChart target);
    /// <summary>
    /// Updates the external data relationship id and the underlying presentation data.
    /// </summary>
    /// <param name="relationshipId">The relationship identifier used in the owning XML part.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetExternalDataRelationshipId(PptxChart target, string relationshipId);
    /// <summary>
    /// Returns the title without changing the chart.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetTitle(PptxChart target);
    /// <summary>
    /// Updates the title and the underlying presentation data.
    /// </summary>
    /// <param name="titleText">The title text value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetTitle(PptxChart target, string titleText);
    /// <summary>
    /// Returns the legend position without changing the chart.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetLegendPosition(PptxChart target);
    /// <summary>
    /// Updates the legend position and the underlying presentation data.
    /// </summary>
    /// <param name="position">The position value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetLegendPosition(PptxChart target, string position);
    /// <summary>
    /// Returns the axis title without changing the chart.
    /// </summary>
    /// <param name="axisName">The axis name value to use.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetAxisTitle(PptxChart target, string axisName);
    /// <summary>
    /// Updates the axis title and the underlying presentation data.
    /// </summary>
    /// <param name="axisName">The axis name value to use.</param>
    /// <param name="titleText">The title text value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetAxisTitle(PptxChart target, string axisName, string titleText);
    /// <summary>
    /// Reports whether the chart contains or enables major gridlines.
    /// </summary>
    /// <param name="axisName">The axis name value to use.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool HasMajorGridlines(PptxChart target, string axisName);
    /// <summary>
    /// Updates the major gridlines and the underlying presentation data.
    /// </summary>
    /// <param name="axisName">The axis name value to use.</param>
    /// <param name="enabled"><c>true</c> to enable the setting; otherwise, <c>false</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetMajorGridlines(PptxChart target, string axisName, bool enabled);
    /// <summary>
    /// Reports whether an axis has minor gridlines.
    /// </summary>
    /// <param name="target">The chart to inspect.</param>
    /// <param name="axisName">The category or value axis name.</param>
    /// <returns><c>true</c> when minor gridlines are present.</returns>
    bool HasMinorGridlines(PptxChart target, string axisName);
    /// <summary>
    /// Enables or removes minor gridlines on an axis.
    /// </summary>
    /// <param name="target">The chart to modify.</param>
    /// <param name="axisName">The category or value axis name.</param>
    /// <param name="enabled"><c>true</c> to enable minor gridlines.</param>
    void SetMinorGridlines(PptxChart target, string axisName, bool enabled);
    /// <summary>
    /// Returns typed scale settings for an axis.
    /// </summary>
    /// <param name="target">The chart to inspect.</param>
    /// <param name="axisName">The category or value axis name.</param>
    /// <returns>The axis scale settings.</returns>
    PptxChartAxisScale GetAxisScale(PptxChart target, string axisName);
    /// <summary>
    /// Applies typed scale settings to an axis.
    /// </summary>
    /// <param name="target">The chart to modify.</param>
    /// <param name="axisName">The category or value axis name.</param>
    /// <param name="scale">The scale settings.</param>
    void SetAxisScale(PptxChart target, string axisName, PptxChartAxisScale scale);
    /// <summary>
    /// Reports whether the chart contains or enables markers.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool HasMarkers(PptxChart target);
    /// <summary>
    /// Updates the markers and the underlying presentation data.
    /// </summary>
    /// <param name="enabled"><c>true</c> to enable the setting; otherwise, <c>false</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetMarkers(PptxChart target, bool enabled);
    /// <summary>
    /// Reports whether the chart contains or enables data labels.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool HasDataLabels(PptxChart target);
    /// <summary>
    /// Updates the data labels and the underlying presentation data.
    /// </summary>
    /// <param name="enabled"><c>true</c> to enable the setting; otherwise, <c>false</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    void SetDataLabels(PptxChart target, bool enabled);
    /// <summary>
    /// Returns formatting for one series in logical series order.
    /// </summary>
    /// <param name="target">The chart to inspect.</param>
    /// <param name="seriesIndex">The zero-based logical series index.</param>
    /// <returns>The series formatting.</returns>
    PptxChartSeriesFormat GetSeriesFormat(PptxChart target, int seriesIndex);
    /// <summary>
    /// Applies formatting to one series in logical series order.
    /// </summary>
    /// <param name="target">The chart to modify.</param>
    /// <param name="seriesIndex">The zero-based logical series index.</param>
    /// <param name="format">The formatting to apply.</param>
    void SetSeriesFormat(PptxChart target, int seriesIndex, PptxChartSeriesFormat format);
    /// <summary>
    /// Adds or replaces the related chart style part.
    /// </summary>
    /// <param name="target">The chart to modify.</param>
    /// <param name="xml">The chart style XML.</param>
    /// <returns>The chart style part.</returns>
    PptxPart SetStyleXml(PptxChart target, string xml);
    /// <summary>
    /// Adds or replaces the related chart color-style part.
    /// </summary>
    /// <param name="target">The chart to modify.</param>
    /// <param name="xml">The chart color-style XML.</param>
    /// <returns>The chart color-style part.</returns>
    PptxPart SetColorStyleXml(PptxChart target, string xml);
    /// <summary>
    /// Returns the style part without changing the chart.
    /// </summary>
    /// <returns>The chart style part, or <c>null</c> when none is related.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxPart? GetStylePart(PptxChart target);
    /// <summary>
    /// Returns the color style part without changing the chart.
    /// </summary>
    /// <returns>The chart color style part, or <c>null</c> when none is related.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxPart? GetColorStylePart(PptxChart target);
    /// <summary>
    /// Returns the related style part without changing the chart.
    /// </summary>
    /// <param name="relationshipType">The Open Packaging Convention relationship type URI.</param>
    /// <returns>The related internal part, or <c>null</c> when the relationship cannot be resolved.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxPart? GetRelatedStylePart(PptxChart target, string relationshipType);
}
