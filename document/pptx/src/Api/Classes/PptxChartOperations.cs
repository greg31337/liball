using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxChart values.
/// </summary>
public class PptxChartOperations : IPptxChartOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxChartOperations()
    {
    }

    /// <summary>
    /// Reports whether the chart contains or enables part.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool HasPart(PptxChart target)
    {
        return target.HasPart();
    }

    /// <summary>
    /// Returns the relationships without changing the chart.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<PptxRelationship> GetRelationships(PptxChart target)
    {
        return target.GetRelationships();
    }

    /// <summary>
    /// Returns the related parts without changing the chart.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<PptxPart> GetRelatedParts(PptxChart target)
    {
        return target.GetRelatedParts();
    }

    /// <summary>
    /// Returns the data without changing the chart.
    /// </summary>
    /// <returns>The resulting chart data value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxChartData GetData(PptxChart target)
    {
        return target.GetData();
    }

    /// <summary>
    /// Extracts data table from the chart without removing it.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<List<string>> ExtractDataTable(PptxChart target)
    {
        return target.ExtractDataTable();
    }

    /// <summary>
    /// Returns the chart type without changing the chart.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetChartType(PptxChart target)
    {
        return target.GetChartType();
    }

    /// <summary>
    /// Updates the chart type and the underlying presentation data.
    /// </summary>
    /// <param name="chartType">The chart type value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetChartType(PptxChart target, string chartType)
    {
        target.SetChartType(chartType);
    }

    /// <summary>
    /// Updates the data and the underlying presentation data.
    /// </summary>
    /// <param name="data">The caller-supplied data to apply.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetData(PptxChart target, PptxChartData data)
    {
        target.SetData(data);
    }

    /// <summary>
    /// Updates the data and the underlying presentation data.
    /// </summary>
    /// <param name="chartType">The chart type value to use.</param>
    /// <param name="data">The caller-supplied data to apply.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetDataWithChartType(PptxChart target, string chartType, PptxChartData data)
    {
        target.SetDataWithChartType(chartType, data);
    }

    /// <summary>
    /// Applies the update caches operation to the current chart.
    /// </summary>
    /// <param name="data">The caller-supplied data to apply.</param>
    /// <param name="target">The data value to operate on.</param>
    public void UpdateCaches(PptxChart target, PptxChartData data)
    {
        target.UpdateCaches(data);
    }

    /// <summary>
    /// Updates the embedded workbook and the underlying presentation data.
    /// </summary>
    /// <param name="data">The caller-supplied data to apply.</param>
    /// <returns>The resulting part value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxPart SetEmbeddedWorkbook(PptxChart target, PptxChartData data)
    {
        return target.SetEmbeddedWorkbook(data);
    }

    /// <summary>
    /// Updates the embedded workbook bytes and the underlying presentation data.
    /// </summary>
    /// <param name="bytes">The binary payload to use. The value must not be <c>null</c>.</param>
    /// <returns>The resulting part value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxPart SetEmbeddedWorkbookBytes(PptxChart target, byte[] bytes)
    {
        return target.SetEmbeddedWorkbookBytes(bytes);
    }

    /// <summary>
    /// Returns the embedded workbook part without changing the chart.
    /// </summary>
    /// <returns>The embedded workbook part, or <c>null</c> when none is related.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxPart? GetEmbeddedWorkbookPart(PptxChart target)
    {
        return target.GetEmbeddedWorkbookPart();
    }

    /// <summary>
    /// Returns the external data relationship id without changing the chart.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetExternalDataRelationshipId(PptxChart target)
    {
        return target.GetExternalDataRelationshipId();
    }

    /// <summary>
    /// Updates the external data relationship id and the underlying presentation data.
    /// </summary>
    /// <param name="relationshipId">The relationship identifier used in the owning XML part.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetExternalDataRelationshipId(PptxChart target, string relationshipId)
    {
        target.SetExternalDataRelationshipId(relationshipId);
    }

    /// <summary>
    /// Returns the title without changing the chart.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetTitle(PptxChart target)
    {
        return target.GetTitle();
    }

    /// <summary>
    /// Updates the title and the underlying presentation data.
    /// </summary>
    /// <param name="titleText">The title text value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetTitle(PptxChart target, string titleText)
    {
        target.SetTitle(titleText);
    }

    /// <summary>
    /// Returns the legend position without changing the chart.
    /// </summary>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetLegendPosition(PptxChart target)
    {
        return target.GetLegendPosition();
    }

    /// <summary>
    /// Updates the legend position and the underlying presentation data.
    /// </summary>
    /// <param name="position">The position value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetLegendPosition(PptxChart target, string position)
    {
        target.SetLegendPosition(position);
    }

    /// <summary>
    /// Returns the axis title without changing the chart.
    /// </summary>
    /// <param name="axisName">The axis name value to use.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetAxisTitle(PptxChart target, string axisName)
    {
        return target.GetAxisTitle(axisName);
    }

    /// <summary>
    /// Updates the axis title and the underlying presentation data.
    /// </summary>
    /// <param name="axisName">The axis name value to use.</param>
    /// <param name="titleText">The title text value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetAxisTitle(PptxChart target, string axisName, string titleText)
    {
        target.SetAxisTitle(axisName, titleText);
    }

    /// <summary>
    /// Reports whether the chart contains or enables major gridlines.
    /// </summary>
    /// <param name="axisName">The axis name value to use.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool HasMajorGridlines(PptxChart target, string axisName)
    {
        return target.HasMajorGridlines(axisName);
    }

    /// <summary>
    /// Updates the major gridlines and the underlying presentation data.
    /// </summary>
    /// <param name="axisName">The axis name value to use.</param>
    /// <param name="enabled"><c>true</c> to enable the setting; otherwise, <c>false</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetMajorGridlines(PptxChart target, string axisName, bool enabled)
    {
        target.SetMajorGridlines(axisName, enabled);
    }

    /// <summary>
    /// Reports whether an axis has minor gridlines.
    /// </summary>
    /// <param name="target">The chart to inspect.</param>
    /// <param name="axisName">The category or value axis name.</param>
    /// <returns><c>true</c> when minor gridlines are present.</returns>
    public bool HasMinorGridlines(PptxChart target, string axisName)
    {
        return target.HasMinorGridlines(axisName);
    }

    /// <summary>
    /// Enables or removes minor gridlines on an axis.
    /// </summary>
    /// <param name="target">The chart to modify.</param>
    /// <param name="axisName">The category or value axis name.</param>
    /// <param name="enabled"><c>true</c> to enable minor gridlines.</param>
    public void SetMinorGridlines(PptxChart target, string axisName, bool enabled)
    {
        target.SetMinorGridlines(axisName, enabled);
    }

    /// <summary>
    /// Returns typed scale settings for an axis.
    /// </summary>
    /// <param name="target">The chart to inspect.</param>
    /// <param name="axisName">The category or value axis name.</param>
    /// <returns>The axis scale settings.</returns>
    public PptxChartAxisScale GetAxisScale(PptxChart target, string axisName)
    {
        return target.GetAxisScale(axisName);
    }

    /// <summary>
    /// Applies typed scale settings to an axis.
    /// </summary>
    /// <param name="target">The chart to modify.</param>
    /// <param name="axisName">The category or value axis name.</param>
    /// <param name="scale">The scale settings.</param>
    public void SetAxisScale(PptxChart target, string axisName, PptxChartAxisScale scale)
    {
        target.SetAxisScale(axisName, scale);
    }

    /// <summary>
    /// Reports whether the chart contains or enables markers.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool HasMarkers(PptxChart target)
    {
        return target.HasMarkers();
    }

    /// <summary>
    /// Updates the markers and the underlying presentation data.
    /// </summary>
    /// <param name="enabled"><c>true</c> to enable the setting; otherwise, <c>false</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetMarkers(PptxChart target, bool enabled)
    {
        target.SetMarkers(enabled);
    }

    /// <summary>
    /// Reports whether the chart contains or enables data labels.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool HasDataLabels(PptxChart target)
    {
        return target.HasDataLabels();
    }

    /// <summary>
    /// Updates the data labels and the underlying presentation data.
    /// </summary>
    /// <param name="enabled"><c>true</c> to enable the setting; otherwise, <c>false</c>.</param>
    /// <param name="target">The data value to operate on.</param>
    public void SetDataLabels(PptxChart target, bool enabled)
    {
        target.SetDataLabels(enabled);
    }

    /// <summary>
    /// Returns formatting for one series in logical series order.
    /// </summary>
    /// <param name="target">The chart to inspect.</param>
    /// <param name="seriesIndex">The zero-based logical series index.</param>
    /// <returns>The series formatting.</returns>
    public PptxChartSeriesFormat GetSeriesFormat(PptxChart target, int seriesIndex)
    {
        return target.GetSeriesFormat(seriesIndex);
    }

    /// <summary>
    /// Applies formatting to one series in logical series order.
    /// </summary>
    /// <param name="target">The chart to modify.</param>
    /// <param name="seriesIndex">The zero-based logical series index.</param>
    /// <param name="format">The formatting to apply.</param>
    public void SetSeriesFormat(PptxChart target, int seriesIndex, PptxChartSeriesFormat format)
    {
        target.SetSeriesFormat(seriesIndex, format);
    }

    /// <summary>
    /// Adds or replaces the related chart style part.
    /// </summary>
    /// <param name="target">The chart to modify.</param>
    /// <param name="xml">The chart style XML.</param>
    /// <returns>The chart style part.</returns>
    public PptxPart SetStyleXml(PptxChart target, string xml)
    {
        return target.SetStyleXml(xml);
    }

    /// <summary>
    /// Adds or replaces the related chart color-style part.
    /// </summary>
    /// <param name="target">The chart to modify.</param>
    /// <param name="xml">The chart color-style XML.</param>
    /// <returns>The chart color-style part.</returns>
    public PptxPart SetColorStyleXml(PptxChart target, string xml)
    {
        return target.SetColorStyleXml(xml);
    }

    /// <summary>
    /// Returns the style part without changing the chart.
    /// </summary>
    /// <returns>The chart style part, or <c>null</c> when none is related.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxPart? GetStylePart(PptxChart target)
    {
        return target.GetStylePart();
    }

    /// <summary>
    /// Returns the color style part without changing the chart.
    /// </summary>
    /// <returns>The chart color style part, or <c>null</c> when none is related.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxPart? GetColorStylePart(PptxChart target)
    {
        return target.GetColorStylePart();
    }

    /// <summary>
    /// Returns the related style part without changing the chart.
    /// </summary>
    /// <param name="relationshipType">The Open Packaging Convention relationship type URI.</param>
    /// <returns>The related internal part, or <c>null</c> when the relationship cannot be resolved.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxPart? GetRelatedStylePart(PptxChart target, string relationshipType)
    {
        return target.GetRelatedStylePart(relationshipType);
    }
}
