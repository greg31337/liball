using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxChartDataOperations"/>.
/// </summary>
public interface IPptxChartDataOperations
{
    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int CategoryCount(PptxChartData target);
    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int SeriesCount(PptxChartData target);
    /// <summary>
    /// Returns the number of number-format entries.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int NumberFormatCount(PptxChartData target);
    /// <summary>
    /// Returns the category without changing the chart data.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetCategory(PptxChartData target, int index);
    /// <summary>
    /// Returns the series without changing the chart data.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting chart series value.</returns>
    /// <param name="target">The data value to operate on.</param>
    PptxChartSeries GetSeries(PptxChartData target, int index);
    /// <summary>
    /// Returns the number format without changing the chart data.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string GetNumberFormat(PptxChartData target, int index);
    /// <summary>
    /// Returns chart categories and series as a row-oriented table of strings.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    List<List<string>> ToTable(PptxChartData target);
    /// <summary>
    /// Reads categories, series, and number formats from a chart into an editable data model.
    /// </summary>
    /// <param name="chart">The chart value to use.</param>
    /// <returns>The resulting chart data value.</returns>
    PptxChartData FromChart(PptxChart chart);
}
