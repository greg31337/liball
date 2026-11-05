using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxChartSeriesOperations"/>.
/// </summary>
public interface IPptxChartSeriesOperations
{
    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int CategoryCount(PptxChartSeries target);
    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int ValueCount(PptxChartSeries target);
    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int LabelCount(PptxChartSeries target);
    /// <summary>
    /// Returns the number of horizontal coordinate values.
    /// </summary>
    /// <param name="target">The chart series to inspect.</param>
    /// <returns>The number of horizontal coordinates.</returns>
    int XValueCount(PptxChartSeries target);
    /// <summary>
    /// Returns the number of vertical coordinate values.
    /// </summary>
    /// <param name="target">The chart series to inspect.</param>
    /// <returns>The number of vertical coordinates.</returns>
    int YValueCount(PptxChartSeries target);
    /// <summary>
    /// Returns the number of bubble-size values.
    /// </summary>
    /// <param name="target">The chart series to inspect.</param>
    /// <returns>The number of bubble sizes.</returns>
    int BubbleSizeCount(PptxChartSeries target);
    /// <summary>
    /// Returns the category without changing the chart series.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetCategory(PptxChartSeries target, int index);
    /// <summary>
    /// Returns the value without changing the chart series.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetValue(PptxChartSeries target, int index);
    /// <summary>
    /// Returns the label without changing the chart series.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetLabel(PptxChartSeries target, int index);
    /// <summary>
    /// Returns one horizontal coordinate.
    /// </summary>
    /// <param name="target">The chart series to inspect.</param>
    /// <param name="index">The zero-based point index.</param>
    /// <returns>The horizontal coordinate text.</returns>
    string GetXValue(PptxChartSeries target, int index);
    /// <summary>
    /// Returns one vertical coordinate.
    /// </summary>
    /// <param name="target">The chart series to inspect.</param>
    /// <param name="index">The zero-based point index.</param>
    /// <returns>The vertical coordinate text.</returns>
    string GetYValue(PptxChartSeries target, int index);
    /// <summary>
    /// Returns one bubble size.
    /// </summary>
    /// <param name="target">The chart series to inspect.</param>
    /// <param name="index">The zero-based point index.</param>
    /// <returns>The bubble size text.</returns>
    string GetBubbleSize(PptxChartSeries target, int index);
}
