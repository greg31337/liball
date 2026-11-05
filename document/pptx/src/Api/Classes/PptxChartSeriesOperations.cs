using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxChartSeries values.
/// </summary>
public class PptxChartSeriesOperations : IPptxChartSeriesOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxChartSeriesOperations()
    {
    }

    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int CategoryCount(PptxChartSeries target)
    {
        return target.CategoryCount();
    }

    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int ValueCount(PptxChartSeries target)
    {
        return target.ValueCount();
    }

    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int LabelCount(PptxChartSeries target)
    {
        return target.LabelCount();
    }

    /// <summary>
    /// Returns the number of horizontal coordinate values.
    /// </summary>
    /// <param name="target">The chart series to inspect.</param>
    /// <returns>The number of horizontal coordinates.</returns>
    public int XValueCount(PptxChartSeries target)
    {
        return target.XValueCount();
    }

    /// <summary>
    /// Returns the number of vertical coordinate values.
    /// </summary>
    /// <param name="target">The chart series to inspect.</param>
    /// <returns>The number of vertical coordinates.</returns>
    public int YValueCount(PptxChartSeries target)
    {
        return target.YValueCount();
    }

    /// <summary>
    /// Returns the number of bubble-size values.
    /// </summary>
    /// <param name="target">The chart series to inspect.</param>
    /// <returns>The number of bubble sizes.</returns>
    public int BubbleSizeCount(PptxChartSeries target)
    {
        return target.BubbleSizeCount();
    }

    /// <summary>
    /// Returns the category without changing the chart series.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetCategory(PptxChartSeries target, int index)
    {
        return target.GetCategory(index);
    }

    /// <summary>
    /// Returns the value without changing the chart series.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetValue(PptxChartSeries target, int index)
    {
        return target.GetValue(index);
    }

    /// <summary>
    /// Returns the label without changing the chart series.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetLabel(PptxChartSeries target, int index)
    {
        return target.GetLabel(index);
    }

    /// <summary>
    /// Returns one horizontal coordinate.
    /// </summary>
    /// <param name="target">The chart series to inspect.</param>
    /// <param name="index">The zero-based point index.</param>
    /// <returns>The horizontal coordinate text.</returns>
    public string GetXValue(PptxChartSeries target, int index)
    {
        return target.GetXValue(index);
    }

    /// <summary>
    /// Returns one vertical coordinate.
    /// </summary>
    /// <param name="target">The chart series to inspect.</param>
    /// <param name="index">The zero-based point index.</param>
    /// <returns>The vertical coordinate text.</returns>
    public string GetYValue(PptxChartSeries target, int index)
    {
        return target.GetYValue(index);
    }

    /// <summary>
    /// Returns one bubble size.
    /// </summary>
    /// <param name="target">The chart series to inspect.</param>
    /// <param name="index">The zero-based point index.</param>
    /// <returns>The bubble size text.</returns>
    public string GetBubbleSize(PptxChartSeries target, int index)
    {
        return target.GetBubbleSize(index);
    }
}
