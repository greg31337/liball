namespace pptx;

internal static class PptxChartTypeCore
{
    internal static string Normalize(string chartType)
    {
        string clean = PptxDiagnostic.CleanText(chartType).Replace("-", "").Replace("_", "").ToLowerInvariant();
        if (clean == "column" || clean == "col" || clean == "clusteredcolumn")
        {
            return PptxChartType.Column;
        }

        if (clean == "bar" || clean == "clusteredbar")
        {
            return PptxChartType.Bar;
        }

        if (clean == "line")
        {
            return PptxChartType.Line;
        }

        if (clean == "pie")
        {
            return PptxChartType.Pie;
        }

        if (clean == "doughnut" || clean == "donut")
        {
            return PptxChartType.Doughnut;
        }

        if (clean == "area")
        {
            return PptxChartType.Area;
        }

        if (clean == "scatter" || clean == "xy")
        {
            return PptxChartType.Scatter;
        }

        if (clean == "bubble")
        {
            return PptxChartType.Bubble;
        }

        if (clean == "radar")
        {
            return PptxChartType.Radar;
        }

        if (clean == "stock")
        {
            return PptxChartType.Stock;
        }

        if (clean == "surface")
        {
            return PptxChartType.Surface;
        }

        if (clean == "combo" || clean == "combination")
        {
            return PptxChartType.Combo;
        }

        throw new System.ArgumentException("Unknown chart type.");
    }
}
