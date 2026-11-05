namespace odf;

internal class ChartRangeParseResult
{
    internal OdfCellRange[] Ranges = [];
    internal bool IsValid = true;
}

internal static class ChartSourceRangeModel
{
    internal static void ResolveChartSourceRanges(OdfDocument document)
    {
        int chartIndex = 0;
        while (chartIndex < document.Charts.Length)
        {
            ResolveChartRanges(document, document.Charts[chartIndex]);
            chartIndex = chartIndex + 1;
        }
    }

    static void ResolveChartRanges(OdfDocument document, OdfChart chart)
    {
        ResolveTextRanges(document, chart, chart.Title);
        ResolveTextRanges(document, chart, chart.Subtitle);
        ResolveTextRanges(document, chart, chart.Footer);
        OdfChartPlotArea? plot = chart.PlotArea;
        if (plot == null)
        {
            return;
        }
        ChartRangeParseResult plotRanges = ParseRanges(
            document,
            chart,
            plot.CellRangeAddress);
        plot.SourceRanges = plotRanges.Ranges;
        plot.IsSourceRangeValid = plotRanges.IsValid;
        int axisIndex = 0;
        while (axisIndex < plot.Axes.Length)
        {
            OdfChartCategories? categories =
                plot.Axes[axisIndex].Categories;
            if (categories != null)
            {
                ChartRangeParseResult categoryRanges = ParseRanges(
                    document,
                    chart,
                    categories.CellRangeAddress);
                categories.Ranges = categoryRanges.Ranges;
                categories.IsRangeValid = categoryRanges.IsValid;
            }
            axisIndex = axisIndex + 1;
        }
        int seriesIndex = 0;
        while (seriesIndex < plot.Series.Length)
        {
            ResolveSeriesRanges(
                document,
                chart,
                plot.Series[seriesIndex]);
            seriesIndex = seriesIndex + 1;
        }
    }

    static void ResolveTextRanges(
        OdfDocument document,
        OdfChart chart,
        OdfChartText? text)
    {
        if (text == null)
        {
            return;
        }
        ChartRangeParseResult parsed = ParseRanges(
            document,
            chart,
            text.CellRange);
        text.SourceRanges = parsed.Ranges;
        text.IsSourceRangeValid = parsed.IsValid;
    }

    static void ResolveSeriesRanges(
        OdfDocument document,
        OdfChart chart,
        OdfChartSeries series)
    {
        ChartRangeParseResult values = ParseRanges(
            document,
            chart,
            series.ValuesCellRangeAddress);
        series.ValueRanges = values.Ranges;
        series.IsValueRangeValid = values.IsValid;
        ChartRangeParseResult labels = ParseRanges(
            document,
            chart,
            series.LabelCellAddress);
        series.LabelRanges = labels.Ranges;
        series.IsLabelRangeValid = labels.IsValid;
        int domainIndex = 0;
        while (domainIndex < series.Domains.Length)
        {
            OdfChartDomain domain = series.Domains[domainIndex];
            ChartRangeParseResult ranges = ParseRanges(
                document,
                chart,
                domain.CellRangeAddress);
            domain.Ranges = ranges.Ranges;
            domain.IsRangeValid = ranges.IsValid;
            domainIndex = domainIndex + 1;
        }
        int errorIndex = 0;
        while (errorIndex < series.ErrorIndicators.Length)
        {
            OdfChartStyleMetadata metadata =
                series.ErrorIndicators[errorIndex].StyleMetadata;
            ChartRangeParseResult lower = ParseRanges(
                document,
                chart,
                metadata.ErrorLowerRange);
            metadata.ErrorLowerRanges = lower.Ranges;
            metadata.IsErrorLowerRangeValid = lower.IsValid;
            ChartRangeParseResult upper = ParseRanges(
                document,
                chart,
                metadata.ErrorUpperRange);
            metadata.ErrorUpperRanges = upper.Ranges;
            metadata.IsErrorUpperRangeValid = upper.IsValid;
            errorIndex = errorIndex + 1;
        }
    }

    static ChartRangeParseResult ParseRanges(
        OdfDocument document,
        OdfChart chart,
        string lexical)
    {
        ChartRangeParseResult result = new ChartRangeParseResult();
        if (lexical.Length == 0)
        {
            return result;
        }
        OdfCellRangeListValue parsed =
            OdfCellAddressParser.ParseRangeList(lexical);
        result.IsValid = parsed.IsValid;
        if (!parsed.IsValid)
        {
            return result;
        }
        OdfCellRange[] ranges = new OdfCellRange[parsed.Ranges.Length];
        int index = 0;
        while (index < parsed.Ranges.Length)
        {
            ranges[index] = ConvertRange(
                document,
                chart,
                parsed.Ranges[index]);
            index = index + 1;
        }
        result.Ranges = ranges;
        return result;
    }

    static OdfCellRange ConvertRange(
        OdfDocument document,
        OdfChart chart,
        OdfCellRangeValue source)
    {
        OdfCellRange result = new OdfCellRange();
        result.LexicalValue = source.LexicalValue;
        result.StartTableName = source.Start.HasSheet
            ? source.Start.SheetName
            : "";
        result.EndTableName = source.HasEnd && source.End.HasSheet
            ? source.End.SheetName
            : result.StartTableName;
        result.HasColumns = source.Start.HasColumn;
        result.HasRows = source.Start.HasRow;
        result.HasEnd = source.HasEnd;
        result.StartColumnIndex = source.Start.HasColumn
            ? ToZeroBasedIndex(source.Start.ColumnIndex)
            : 0;
        result.StartRowIndex = source.Start.HasRow
            ? ToZeroBasedIndex(source.Start.Row)
            : 0;
        OdfCellAddressValue end = source.HasEnd
            ? source.End
            : source.Start;
        result.EndColumnIndex = end.HasColumn
            ? ToZeroBasedIndex(end.ColumnIndex)
            : result.StartColumnIndex;
        result.EndRowIndex = end.HasRow
            ? ToZeroBasedIndex(end.Row)
            : result.StartRowIndex;
        result.IsValid = source.IsValid;
        result.Table = ResolveRangeTable(document, chart, result);
        CaptureRangeOriginalValues(result);
        return result;
    }

    static OdfTable? ResolveRangeTable(
        OdfDocument document,
        OdfChart chart,
        OdfCellRange range)
    {
        if (range.StartTableName.Length == 0)
        {
            return chart.DataTable;
        }
        if (range.EndTableName.Length > 0
            && range.EndTableName != range.StartTableName)
        {
            return null;
        }
        if (chart.DataTable != null
            && chart.DataTable.Name == range.StartTableName)
        {
            return chart.DataTable;
        }
        OdfTable? result = null;
        int count = 0;
        int index = 0;
        while (index < document.Tables.Length)
        {
            OdfTable table = document.Tables[index];
            if (table.Name == range.StartTableName)
            {
                if (table.PartPath == chart.PartPath)
                {
                    return table;
                }
                result = table;
                count = count + 1;
            }
            index = index + 1;
        }
        return count == 1 ? result : null;
    }

    static long ToZeroBasedIndex(
        global::System.Numerics.BigInteger value)
    {
        if (value <= 1)
        {
            return 0;
        }
        global::System.Numerics.BigInteger zeroBased = value - 1;
        return zeroBased > long.MaxValue
            ? long.MaxValue
            : (long)zeroBased;
    }

    static void CaptureRangeOriginalValues(OdfCellRange range)
    {
        range.OriginalLexicalValue = range.LexicalValue;
        range.OriginalStartTableName = range.StartTableName;
        range.OriginalEndTableName = range.EndTableName;
        range.OriginalStartColumnIndex = range.StartColumnIndex;
        range.OriginalEndColumnIndex = range.EndColumnIndex;
        range.OriginalStartRowIndex = range.StartRowIndex;
        range.OriginalEndRowIndex = range.EndRowIndex;
        range.OriginalHasColumns = range.HasColumns;
        range.OriginalHasRows = range.HasRows;
        range.OriginalHasEnd = range.HasEnd;
    }
}
