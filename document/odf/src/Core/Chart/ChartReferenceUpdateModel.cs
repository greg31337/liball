namespace odf;

internal static class ChartReferenceUpdateModel
{
    internal static void RenameStyleReferences(
        OdfDocument document,
        string oldName,
        string newName)
    {
        int index = 0;
        while (index < document.Charts.Length)
        {
            RenameChart(document.Charts[index], oldName, newName);
            index = index + 1;
        }
    }

    static void RenameChart(
        OdfChart chart,
        string oldName,
        string newName)
    {
        if (chart.StyleName == oldName)
        {
            chart.StyleName = newName;
        }
        RenameText(chart.Title, oldName, newName);
        RenameText(chart.Subtitle, oldName, newName);
        RenameText(chart.Footer, oldName, newName);
        OdfChartLegend? legend = chart.Legend;
        if (legend != null && legend.StyleName == oldName)
        {
            legend.StyleName = newName;
        }
        RenamePlot(chart.PlotArea, oldName, newName);
    }

    static void RenameText(
        OdfChartText? text,
        string oldName,
        string newName)
    {
        if (text != null && text.StyleName == oldName)
        {
            text.StyleName = newName;
        }
    }

    static void RenamePlot(
        OdfChartPlotArea? plot,
        string oldName,
        string newName)
    {
        if (plot == null)
        {
            return;
        }
        if (plot.StyleName == oldName)
        {
            plot.StyleName = newName;
        }
        int index = 0;
        while (index < plot.Axes.Length)
        {
            RenameAxis(plot.Axes[index], oldName, newName);
            index = index + 1;
        }
        RenameSurface(plot.Wall, oldName, newName);
        RenameSurface(plot.Floor, oldName, newName);
        index = 0;
        while (index < plot.Series.Length)
        {
            RenameSeries(plot.Series[index], oldName, newName);
            index = index + 1;
        }
        RenameStock(plot.StockGainMarker, oldName, newName);
        RenameStock(plot.StockLossMarker, oldName, newName);
        RenameStock(plot.StockRangeLine, oldName, newName);
    }

    static void RenameAxis(
        OdfChartAxis axis,
        string oldName,
        string newName)
    {
        if (axis.StyleName == oldName)
        {
            axis.StyleName = newName;
        }
        RenameText(axis.Title, oldName, newName);
        int index = 0;
        while (index < axis.Grids.Length)
        {
            OdfChartGrid grid = axis.Grids[index];
            if (grid.StyleName == oldName)
            {
                grid.StyleName = newName;
            }
            index = index + 1;
        }
    }

    static void RenameSurface(
        OdfChartSurface? surface,
        string oldName,
        string newName)
    {
        if (surface != null && surface.StyleName == oldName)
        {
            surface.StyleName = newName;
        }
    }

    static void RenameSeries(
        OdfChartSeries series,
        string oldName,
        string newName)
    {
        if (series.StyleName == oldName)
        {
            series.StyleName = newName;
        }
        RenameLabel(series.Label, oldName, newName);
        int index = 0;
        while (index < series.DataPoints.Length)
        {
            OdfChartDataPoint point = series.DataPoints[index];
            if (point.StyleName == oldName)
            {
                point.StyleName = newName;
            }
            RenameLabel(point.Label, oldName, newName);
            index = index + 1;
        }
        index = 0;
        while (index < series.ErrorIndicators.Length)
        {
            OdfChartErrorIndicator error = series.ErrorIndicators[index];
            if (error.StyleName == oldName)
            {
                error.StyleName = newName;
            }
            index = index + 1;
        }
        index = 0;
        while (index < series.RegressionCurves.Length)
        {
            OdfChartRegressionCurve curve = series.RegressionCurves[index];
            if (curve.StyleName == oldName)
            {
                curve.StyleName = newName;
            }
            OdfChartEquation? equation = curve.Equation;
            if (equation != null && equation.StyleName == oldName)
            {
                equation.StyleName = newName;
            }
            index = index + 1;
        }
    }

    static void RenameLabel(
        OdfChartDataLabel? label,
        string oldName,
        string newName)
    {
        if (label != null && label.StyleName == oldName)
        {
            label.StyleName = newName;
        }
    }

    static void RenameStock(
        OdfChartStockComponent? component,
        string oldName,
        string newName)
    {
        if (component != null && component.StyleName == oldName)
        {
            component.StyleName = newName;
        }
    }
}
