namespace chart;

/// <summary>
/// Creates bar chart series, layouts, or retained drawing commands.
/// </summary>
/// <remarks>Builder methods validate array lengths and numeric inputs before producing render-ready objects.</remarks>
internal static class ChartBarChartsOperations
{
    /// <summary>
    /// Creates the requested vertical bar series from the supplied data and configuration.
    /// </summary>
    /// <param name="id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name="xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name="yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name="barWidth">The bar width.</param>
    /// <param name="baseline">The baseline.</param>
    /// <param name="style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateVerticalBarSeries(string id, double[] xValues, double[] yValues, double barWidth, double baseline, ChartRenderStyle style)
    {
        ChartPlot.ValidateId(id);
        ValidateBarWidth(barWidth, "barWidth");
        ChartLinearScale.ValidateFinite(baseline, "baseline");
        ChartLineAreaChartsOperations.ValidateStyle(style);
        ChartCartesianTransform.ValidateValueArrays(xValues, yValues, 1);

        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Bar);
        int index = 0;
        while (index < xValues.Length)
        {
            AddVerticalBar(series, id + "-bar-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture), xValues[index], baseline, yValues[index], barWidth, style);
            index = index + 1;
        }

        return series;
    }

    /// <summary>
    /// Creates the requested horizontal bar series from the supplied data and configuration.
    /// </summary>
    /// <param name="id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name="yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name="xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name="barHeight">The bar height.</param>
    /// <param name="baseline">The baseline.</param>
    /// <param name="style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateHorizontalBarSeries(string id, double[] yValues, double[] xValues, double barHeight, double baseline, ChartRenderStyle style)
    {
        ChartPlot.ValidateId(id);
        ValidateBarWidth(barHeight, "barHeight");
        ChartLinearScale.ValidateFinite(baseline, "baseline");
        ChartLineAreaChartsOperations.ValidateStyle(style);
        ChartCartesianTransform.ValidateValueArrays(xValues, yValues, 1);

        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Bar);
        int index = 0;
        while (index < yValues.Length)
        {
            AddHorizontalBar(series, id + "-bar-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture), baseline, xValues[index], yValues[index], barHeight, style);
            index = index + 1;
        }

        return series;
    }

    /// <summary>
    /// Creates the requested grouped vertical bar series from the supplied data and configuration.
    /// </summary>
    /// <param name="idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name="xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name="values">The input values to process.</param>
    /// <param name="groupWidth">The group width.</param>
    /// <param name="innerGap">The inner gap.</param>
    /// <param name="baseline">The baseline.</param>
    /// <param name="palette">The categorical colors to apply in stable index order.</param>
    /// <returns>The newly created cartesian series array.</returns>
    public static ChartCartesianSeries[] CreateGroupedVerticalBarSeries(string idPrefix, double[] xValues, ChartDataMatrix values, double groupWidth, double innerGap, double baseline, ChartPalette palette)
    {
        ChartPlot.ValidateId(idPrefix);
        ChartLineAreaChartsOperations.ValidateMatrixRows(values, xValues, 1);
        ValidateBarWidth(groupWidth, "groupWidth");
        ChartStyle.ValidateNonNegative(innerGap, "innerGap");
        ChartLinearScale.ValidateFinite(baseline, "baseline");
        ChartLineAreaChartsOperations.ValidatePalette(palette);

        double barWidth = GroupedBarWidth(values.Columns, groupWidth, innerGap);
        ChartCartesianSeries[] series = new ChartCartesianSeries[values.Columns];
        int column = 0;
        while (column < values.Columns)
        {
            ChartRenderStyle style = ChartRenderStyle.CreateFill(palette.GetColor(column));
            string id = idPrefix + "-" + column.ToString(System.Globalization.CultureInfo.InvariantCulture);
            series[column] = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Bar);
            series[column].SetOrder(column);
            double offset = GroupedBarOffset(column, values.Columns, groupWidth, innerGap);
            int row = 0;
            while (row < values.Rows)
            {
                AddVerticalBar(series[column], id + "-bar-" + row.ToString(System.Globalization.CultureInfo.InvariantCulture), xValues[row] + offset, baseline, values.GetValue(row, column), barWidth, style);
                row = row + 1;
            }

            column = column + 1;
        }

        return series;
    }

    internal static ChartCartesianSeries[] CreateGroupedHorizontalBarSeries(string idPrefix, double[] yValues, ChartDataMatrix values, double groupHeight, double innerGap, double baseline, ChartPalette palette)
    {
        ChartPlot.ValidateId(idPrefix);
        ChartLineAreaChartsOperations.ValidateMatrixRows(values, yValues, 1);
        ValidateBarWidth(groupHeight, "groupHeight");
        ChartStyle.ValidateNonNegative(innerGap, "innerGap");
        ChartLinearScale.ValidateFinite(baseline, "baseline");
        ChartLineAreaChartsOperations.ValidatePalette(palette);

        double barHeight = GroupedBarWidth(values.Columns, groupHeight, innerGap);
        ChartCartesianSeries[] series = new ChartCartesianSeries[values.Columns];
        int column = 0;
        while (column < values.Columns)
        {
            ChartRenderStyle style = ChartRenderStyle.CreateFill(palette.GetColor(column));
            string id = idPrefix + "-" + column.ToString(System.Globalization.CultureInfo.InvariantCulture);
            series[column] = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Bar);
            series[column].SetOrder(column);
            double offset = GroupedBarOffset(column, values.Columns, groupHeight, innerGap);
            int row = 0;
            while (row < values.Rows)
            {
                AddHorizontalBar(series[column], id + "-bar-" + row.ToString(System.Globalization.CultureInfo.InvariantCulture), baseline, values.GetValue(row, column), yValues[row] + offset, barHeight, style);
                row = row + 1;
            }

            column = column + 1;
        }

        return series;
    }

    /// <summary>
    /// Creates the requested stacked vertical bar series from the supplied data and configuration.
    /// </summary>
    /// <param name="idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name="xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name="values">The input values to process.</param>
    /// <param name="barWidth">The bar width.</param>
    /// <param name="mode">The mode.</param>
    /// <param name="palette">The categorical colors to apply in stable index order.</param>
    /// <returns>The newly created cartesian series array.</returns>
    public static ChartCartesianSeries[] CreateStackedVerticalBarSeries(string idPrefix, double[] xValues, ChartDataMatrix values, double barWidth, ChartStackMode mode, ChartPalette palette)
    {
        ChartPlot.ValidateId(idPrefix);
        ChartLineAreaChartsOperations.ValidateMatrixRows(values, xValues, 1);
        ValidateBarWidth(barWidth, "barWidth");
        ChartLineAreaChartsOperations.ValidatePalette(palette);

        ChartStackResult stack = ChartStackerOperations.Stack(values, mode);
        ChartCartesianSeries[] series = new ChartCartesianSeries[values.Columns];
        int column = 0;
        while (column < values.Columns)
        {
            ChartRenderStyle style = ChartRenderStyle.CreateFill(palette.GetColor(column));
            string id = idPrefix + "-" + column.ToString(System.Globalization.CultureInfo.InvariantCulture);
            series[column] = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Bar);
            series[column].SetOrder(column);
            int row = 0;
            while (row < values.Rows)
            {
                AddVerticalBar(series[column], id + "-bar-" + row.ToString(System.Globalization.CultureInfo.InvariantCulture), xValues[row], stack.GetStart(row, column), stack.GetEnd(row, column), barWidth, style);
                row = row + 1;
            }

            column = column + 1;
        }

        return series;
    }

    internal static ChartCartesianSeries[] CreateStackedHorizontalBarSeries(string idPrefix, double[] yValues, ChartDataMatrix values, double barHeight, ChartStackMode mode, ChartPalette palette)
    {
        ChartPlot.ValidateId(idPrefix);
        ChartLineAreaChartsOperations.ValidateMatrixRows(values, yValues, 1);
        ValidateBarWidth(barHeight, "barHeight");
        ChartLineAreaChartsOperations.ValidatePalette(palette);

        ChartStackResult stack = ChartStackerOperations.Stack(values, mode);
        ChartCartesianSeries[] series = new ChartCartesianSeries[values.Columns];
        int column = 0;
        while (column < values.Columns)
        {
            ChartRenderStyle style = ChartRenderStyle.CreateFill(palette.GetColor(column));
            string id = idPrefix + "-" + column.ToString(System.Globalization.CultureInfo.InvariantCulture);
            series[column] = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Bar);
            series[column].SetOrder(column);
            int row = 0;
            while (row < values.Rows)
            {
                AddHorizontalBar(series[column], id + "-bar-" + row.ToString(System.Globalization.CultureInfo.InvariantCulture), stack.GetStart(row, column), stack.GetEnd(row, column), yValues[row], barHeight, style);
                row = row + 1;
            }

            column = column + 1;
        }

        return series;
    }

    /// <summary>
    /// Creates the requested percent stacked vertical bar series from the supplied data and configuration.
    /// </summary>
    /// <param name="idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name="xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name="values">The input values to process.</param>
    /// <param name="barWidth">The bar width.</param>
    /// <param name="palette">The categorical colors to apply in stable index order.</param>
    /// <returns>The newly created cartesian series array.</returns>
    public static ChartCartesianSeries[] CreatePercentStackedVerticalBarSeries(string idPrefix, double[] xValues, ChartDataMatrix values, double barWidth, ChartPalette palette)
    {
        return CreateStackedVerticalBarSeries(idPrefix, xValues, values, barWidth, ChartStackMode.Percent, palette);
    }

    internal static ChartCartesianSeries[] CreatePercentStackedHorizontalBarSeries(string idPrefix, double[] yValues, ChartDataMatrix values, double barHeight, ChartPalette palette)
    {
        return CreateStackedHorizontalBarSeries(idPrefix, yValues, values, barHeight, ChartStackMode.Percent, palette);
    }

    internal static ChartCartesianSeries CreateFunnelSeries(string id, double[] values, double centerX, double topY, double stageHeight, double stageGap, double maximumWidth, ChartPalette palette)
    {
        ChartPlot.ValidateId(id);
        ValidateFunnelValues(values);
        ChartLinearScale.ValidateFinite(centerX, "centerX");
        ChartLinearScale.ValidateFinite(topY, "topY");
        ChartRenderStyle.ValidatePositive(stageHeight, "stageHeight");
        ChartStyle.ValidateNonNegative(stageGap, "stageGap");
        ChartRenderStyle.ValidatePositive(maximumWidth, "maximumWidth");
        ChartLineAreaChartsOperations.ValidatePalette(palette);

        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Mixed);
        int stage = 0;
        while (stage < values.Length)
        {
            double topWidth = maximumWidth * values[stage] / values[0];
            double bottomWidth = topWidth;
            if (stage < values.Length - 1)
            {
                bottomWidth = maximumWidth * values[stage + 1] / values[0];
            }

            double y1 = topY + stage * (stageHeight + stageGap);
            double y2 = y1 + stageHeight;
            ChartRenderStyle style = ChartRenderStyle.CreateFill(palette.GetColor(stage));
            string suffix = stage.ToString(System.Globalization.CultureInfo.InvariantCulture);
            series.AddCommand(ChartRenderCommand.CreatePolygon(id + "-stage-" + suffix, new ChartPoint[] { new ChartPoint(centerX - topWidth * 0.5, y1), new ChartPoint(centerX + topWidth * 0.5, y1), new ChartPoint(centerX + bottomWidth * 0.5, y2), new ChartPoint(centerX - bottomWidth * 0.5, y2) }, style));
            stage = stage + 1;
        }

        return series;
    }

    internal static ChartRenderScene CreateFunnelScene(string id, int width, int height, double[] values, double centerX, double topY, double stageHeight, double stageGap, double maximumWidth, ChartPalette palette)
    {
        ChartCartesianSeries series = CreateFunnelSeries(id, values, centerX, topY, stageHeight, stageGap, maximumWidth, palette);
        ChartRenderScene scene = new ChartRenderScene(width, height);
        int index = 0;
        while (index < series.CommandCount)
        {
            scene.AddCommand(series.Commands[index]);
            index = index + 1;
        }

        return scene;
    }

    /// <summary>
    /// Creates the requested range bar series from the supplied data and configuration.
    /// </summary>
    /// <param name="id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name="xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name="lowValues">The low values in data order.</param>
    /// <param name="highValues">The high values in data order.</param>
    /// <param name="barWidth">The bar width.</param>
    /// <param name="style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateRangeBarSeries(string id, double[] xValues, double[] lowValues, double[] highValues, double barWidth, ChartRenderStyle style)
    {
        ChartPlot.ValidateId(id);
        ValidateBarWidth(barWidth, "barWidth");
        ChartLineAreaChartsOperations.ValidateStyle(style);
        ChartCartesianTransform.ValidateValueArrays(xValues, lowValues, 1);
        ChartCartesianTransform.ValidateValueArrays(xValues, highValues, 1);

        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Bar);
        int index = 0;
        while (index < xValues.Length)
        {
            AddVerticalBar(series, id + "-range-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture), xValues[index], lowValues[index], highValues[index], barWidth, style);
            index = index + 1;
        }

        return series;
    }

    /// <summary>
    /// Creates the requested lollipop series from the supplied data and configuration.
    /// </summary>
    /// <param name="id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name="xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name="yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name="baseline">The baseline.</param>
    /// <param name="markerShape">The marker shape.</param>
    /// <param name="markerSize">The marker size.</param>
    /// <param name="lineStyle">The rendering style used to stroke generated lines.</param>
    /// <param name="markerStyle">The rendering style used for marker.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateLollipopSeries(string id, double[] xValues, double[] yValues, double baseline, ChartMarkerShape markerShape, double markerSize, ChartRenderStyle lineStyle, ChartRenderStyle markerStyle)
    {
        ChartPlot.ValidateId(id);
        ChartCartesianTransform.ValidateValueArrays(xValues, yValues, 1);
        ChartLinearScale.ValidateFinite(baseline, "baseline");
        ChartRenderCommand.ValidateMarkerShape(markerShape);
        ChartRenderStyle.ValidatePositive(markerSize, "markerSize");
        ChartLineAreaChartsOperations.ValidateStyle(lineStyle);
        ChartLineAreaChartsOperations.ValidateStyle(markerStyle);

        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Mixed);
        int index = 0;
        while (index < xValues.Length)
        {
            string suffix = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            series.AddLine(id + "-stem-" + suffix, xValues[index], baseline, xValues[index], yValues[index], lineStyle);
            series.AddMarker(id + "-marker-" + suffix, markerShape, xValues[index], yValues[index], markerSize, markerStyle);
            index = index + 1;
        }

        return series;
    }

    /// <summary>
    /// Creates the requested waterfall series from the supplied data and configuration.
    /// </summary>
    /// <param name="id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name="xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name="deltaValues">The delta values in data order.</param>
    /// <param name="barWidth">The bar width.</param>
    /// <param name="positiveStyle">The rendering style used for positive.</param>
    /// <param name="negativeStyle">The rendering style used for negative.</param>
    /// <param name="connectorStyle">The rendering style used for connector.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateWaterfallSeries(string id, double[] xValues, double[] deltaValues, double barWidth, ChartRenderStyle positiveStyle, ChartRenderStyle negativeStyle, ChartRenderStyle connectorStyle)
    {
        ChartPlot.ValidateId(id);
        ChartCartesianTransform.ValidateValueArrays(xValues, deltaValues, 1);
        ValidateBarWidth(barWidth, "barWidth");
        ChartLineAreaChartsOperations.ValidateStyle(positiveStyle);
        ChartLineAreaChartsOperations.ValidateStyle(negativeStyle);
        ChartLineAreaChartsOperations.ValidateStyle(connectorStyle);

        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Mixed);
        double start = 0.0;
        int index = 0;
        while (index < deltaValues.Length)
        {
            double end = start + deltaValues[index];
            ChartRenderStyle style = positiveStyle;
            if (deltaValues[index] < 0.0)
            {
                style = negativeStyle;
            }

            string suffix = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            AddVerticalBar(series, id + "-bar-" + suffix, xValues[index], start, end, barWidth, style);
            if (index < deltaValues.Length - 1)
            {
                series.AddLine(id + "-connector-" + suffix, xValues[index] + barWidth * 0.5, end, xValues[index + 1] - barWidth * 0.5, end, connectorStyle);
            }

            start = end;
            index = index + 1;
        }

        return series;
    }

    internal static void AddVerticalBar(ChartCartesianSeries series, string id, double xCenter, double yStart, double yEnd, double barWidth, ChartRenderStyle style)
    {
        if (series == null)
        {
            throw new System.ArgumentNullException("series");
        }

        ChartPlot.ValidateId(id);
        ChartLinearScale.ValidateFinite(xCenter, "xCenter");
        ChartLinearScale.ValidateFinite(yStart, "yStart");
        ChartLinearScale.ValidateFinite(yEnd, "yEnd");
        ValidateBarWidth(barWidth, "barWidth");
        ChartLineAreaChartsOperations.ValidateStyle(style);
        double half = barWidth * 0.5;
        series.AddCommand(ChartRenderCommand.CreatePolygon(id, new ChartPoint[] { new ChartPoint(xCenter - half, yStart), new ChartPoint(xCenter - half, yEnd), new ChartPoint(xCenter + half, yEnd), new ChartPoint(xCenter + half, yStart) }, style));
    }

    internal static void AddHorizontalBar(ChartCartesianSeries series, string id, double xStart, double xEnd, double yCenter, double barHeight, ChartRenderStyle style)
    {
        if (series == null)
        {
            throw new System.ArgumentNullException("series");
        }

        ChartPlot.ValidateId(id);
        ChartLinearScale.ValidateFinite(xStart, "xStart");
        ChartLinearScale.ValidateFinite(xEnd, "xEnd");
        ChartLinearScale.ValidateFinite(yCenter, "yCenter");
        ValidateBarWidth(barHeight, "barHeight");
        ChartLineAreaChartsOperations.ValidateStyle(style);
        double half = barHeight * 0.5;
        series.AddCommand(ChartRenderCommand.CreatePolygon(id, new ChartPoint[] { new ChartPoint(xStart, yCenter - half), new ChartPoint(xEnd, yCenter - half), new ChartPoint(xEnd, yCenter + half), new ChartPoint(xStart, yCenter + half) }, style));
    }

    internal static double GroupedBarWidth(int columnCount, double groupWidth, double innerGap)
    {
        if (columnCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException("columnCount");
        }

        ValidateBarWidth(groupWidth, "groupWidth");
        ChartStyle.ValidateNonNegative(innerGap, "innerGap");
        double available = groupWidth - innerGap * (columnCount - 1);
        if (available <= 0.0)
        {
            throw new System.ArgumentException("Grouped bar inner gap leaves no bar width.");
        }

        return available / columnCount;
    }

    internal static double GroupedBarOffset(int column, int columnCount, double groupWidth, double innerGap)
    {
        if (column < 0 || column >= columnCount)
        {
            throw new System.ArgumentOutOfRangeException("column");
        }

        double barWidth = GroupedBarWidth(columnCount, groupWidth, innerGap);
        return (0.0 - groupWidth * 0.5) + barWidth * 0.5 + column * (barWidth + innerGap);
    }

    internal static void ValidateFunnelValues(double[] values)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        if (values.Length == 0)
        {
            throw new System.ArgumentException("Funnel charts require at least one stage.");
        }

        int index = 0;
        while (index < values.Length)
        {
            ChartRenderStyle.ValidatePositive(values[index], "values");
            if (index > 0 && values[index] > values[index - 1])
            {
                throw new System.ArgumentException("Funnel stage values must be non-increasing.");
            }

            index = index + 1;
        }
    }

    internal static void ValidateBarWidth(double value, string name)
    {
        ChartRenderStyle.ValidatePositive(value, name);
    }
}
