using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxChartData values.
/// </summary>
public class PptxChartDataOperations : IPptxChartDataOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxChartDataOperations()
    {
    }

    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int CategoryCount(PptxChartData target)
    {
        return target.CategoryCount();
    }

    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int SeriesCount(PptxChartData target)
    {
        return target.SeriesCount();
    }

    /// <summary>
    /// Returns the number of number-format entries.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int NumberFormatCount(PptxChartData target)
    {
        return target.NumberFormatCount();
    }

    /// <summary>
    /// Returns the category without changing the chart data.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetCategory(PptxChartData target, int index)
    {
        return target.GetCategory(index);
    }

    /// <summary>
    /// Returns the series without changing the chart data.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting chart series value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public PptxChartSeries GetSeries(PptxChartData target, int index)
    {
        return target.GetSeries(index);
    }

    /// <summary>
    /// Returns the number format without changing the chart data.
    /// </summary>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string GetNumberFormat(PptxChartData target, int index)
    {
        return target.GetNumberFormat(index);
    }

    /// <summary>
    /// Returns chart categories and series as a row-oriented table of strings.
    /// </summary>
    /// <returns>The resulting collection in presentation or package order.</returns>
    /// <param name="target">The data value to operate on.</param>
    public List<List<string>> ToTable(PptxChartData target)
    {
        return target.ToTable();
    }

    /// <summary>
    /// Reads categories, series, and number formats from a chart into an editable data model.
    /// </summary>
    /// <param name="chart">The chart value to use.</param>
    /// <returns>The resulting chart data value.</returns>
    public PptxChartData FromChart(PptxChart chart)
    {
        return PptxChartData.FromChart(chart);
    }
}
