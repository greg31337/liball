namespace chart;
internal static class ChartCompactChartsOperations
{
    /// <summary>
    /// Creates the requested slope series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "labels">The display labels in the same order as their corresponding values.</param>
    /// <param name = "startValues">The start values in data order.</param>
    /// <param name = "endValues">The end values in data order.</param>
    /// <param name = "startX">The start x.</param>
    /// <param name = "endX">The end x.</param>
    /// <param name = "palette">The categorical colors to apply in stable index order.</param>
    /// <param name = "strokeWidth">The stroke width in chart units, which must be positive.</param>
    /// <returns>The newly created cartesian series.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public static ChartCartesianSeries CreateSlopeSeries(string id, string[] labels, double[] startValues, double[] endValues, double startX, double endX, ChartPalette palette, double strokeWidth)
    {
        ChartPlot.ValidateId(id);
        ValidateLabelsAndPairs(labels, startValues, endValues, 1);
        ChartLinearScale.ValidateFinite(startX, "startX");
        ChartLinearScale.ValidateFinite(endX, "endX");
        if (startX == endX)
        {
            throw new System.ArgumentException("Slope chart x positions must differ.");
        }

        ChartLineAreaChartsOperations.ValidatePalette(palette);
        ChartRenderStyle.ValidatePositive(strokeWidth, "strokeWidth");
        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Line);
        int index = 0;
        while (index < labels.Length)
        {
            string suffix = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            series.AddLine(id + "-slope-" + suffix, startX, startValues[index], endX, endValues[index], ChartRenderStyle.CreateStroke(palette.GetColor(index), strokeWidth));
            index = index + 1;
        }

        return series;
    }

    /// <summary>
    /// Creates the requested dumbbell series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "startValues">The start values in data order.</param>
    /// <param name = "endValues">The end values in data order.</param>
    /// <param name = "markerSize">The marker size.</param>
    /// <param name = "lineStyle">The rendering style used to stroke generated lines.</param>
    /// <param name = "startStyle">The rendering style used for start.</param>
    /// <param name = "endStyle">The rendering style used for end.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateDumbbellSeries(string id, double[] yValues, double[] startValues, double[] endValues, double markerSize, ChartRenderStyle lineStyle, ChartRenderStyle startStyle, ChartRenderStyle endStyle)
    {
        ChartPlot.ValidateId(id);
        ValidateTriples(yValues, startValues, endValues, 1);
        ChartRenderStyle.ValidatePositive(markerSize, "markerSize");
        ChartLineAreaChartsOperations.ValidateStyle(lineStyle);
        ChartLineAreaChartsOperations.ValidateStyle(startStyle);
        ChartLineAreaChartsOperations.ValidateStyle(endStyle);
        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Mixed);
        int index = 0;
        while (index < yValues.Length)
        {
            string suffix = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            series.AddLine(id + "-connector-" + suffix, startValues[index], yValues[index], endValues[index], yValues[index], lineStyle);
            series.AddMarker(id + "-start-" + suffix, ChartMarkerShape.Circle, startValues[index], yValues[index], markerSize, startStyle);
            series.AddMarker(id + "-end-" + suffix, ChartMarkerShape.Circle, endValues[index], yValues[index], markerSize, endStyle);
            index = index + 1;
        }

        return series;
    }

    /// <summary>
    /// Creates the requested bump series from the supplied data and configuration.
    /// </summary>
    /// <param name = "idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name = "labels">The display labels in the same order as their corresponding values.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "ranks">The ranks.</param>
    /// <param name = "palette">The categorical colors to apply in stable index order.</param>
    /// <param name = "strokeWidth">The stroke width in chart units, which must be positive.</param>
    /// <returns>The newly created cartesian series array.</returns>
    public static ChartCartesianSeries[] CreateBumpSeries(string idPrefix, string[] labels, double[] xValues, ChartDataMatrix ranks, ChartPalette palette, double strokeWidth)
    {
        ChartPlot.ValidateId(idPrefix);
        ValidateBumpInputs(labels, xValues, ranks);
        ChartLineAreaChartsOperations.ValidatePalette(palette);
        ChartRenderStyle.ValidatePositive(strokeWidth, "strokeWidth");
        ChartCartesianSeries[] series = new ChartCartesianSeries[labels.Length];
        int column = 0;
        while (column < labels.Length)
        {
            string id = idPrefix + "-" + column.ToString(System.Globalization.CultureInfo.InvariantCulture);
            series[column] = ChartLineAreaChartsOperations.CreateLineSeries(id, xValues, ChartLineAreaChartsOperations.ColumnValues(ranks, column), ChartRenderStyle.CreateStroke(palette.GetColor(column), strokeWidth));
            series[column].SetOrder(column);
            column = column + 1;
        }

        return series;
    }

    /// <summary>
    /// Computes the layout for table within the supplied bounds.
    /// </summary>
    /// <param name = "headers">The headers.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "rowCount">The number of logical rows represented by the input.</param>
    /// <param name = "columnCount">The number of logical columns represented by the input.</param>
    /// <param name = "bounds">The rectangle that constrains the generated layout or drawing.</param>
    /// <param name = "headerHeight">The header height.</param>
    /// <returns>The resulting table layout.</returns>
    public static ChartTableLayout LayoutTable(string[] headers, string[] values, int rowCount, int columnCount, ChartRect bounds, double headerHeight)
    {
        ValidateTableInputs(headers, values, rowCount, columnCount, bounds, headerHeight);
        ChartTableLayout layout = new ChartTableLayout(rowCount, columnCount);
        double columnWidth = bounds.Width / columnCount;
        double bodyHeight = (bounds.Height - headerHeight) / rowCount;
        int column = 0;
        while (column < columnCount)
        {
            layout.AddCell(new ChartTableCellLayout(-1, column, true, headers[column], new ChartRect(bounds.X + column * columnWidth, bounds.Y, columnWidth, headerHeight)));
            column = column + 1;
        }

        int row = 0;
        while (row < rowCount)
        {
            column = 0;
            while (column < columnCount)
            {
                string text = values[row * columnCount + column];
                ChartRect cellBounds = new ChartRect(bounds.X + column * columnWidth, bounds.Y + headerHeight + row * bodyHeight, columnWidth, bodyHeight);
                layout.AddCell(new ChartTableCellLayout(row, column, false, text, cellBounds));
                column = column + 1;
            }

            row = row + 1;
        }

        return layout;
    }

    /// <summary>
    /// Adds the supplied table to scene to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "layout">The layout.</param>
    /// <param name = "cellStyle">The rendering style used for cell.</param>
    /// <param name = "textStyle">The rendering style used for generated text.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static void AddTableToScene(IChartRenderTarget scene, string id, ChartTableLayout layout, ChartRenderStyle cellStyle, ChartRenderStyle textStyle)
    {
        if (scene == null)
        {
            throw new System.ArgumentNullException("scene");
        }

        ChartPlot.ValidateId(id);
        if (layout == null)
        {
            throw new System.ArgumentNullException("layout");
        }

        ChartLineAreaChartsOperations.ValidateStyle(cellStyle);
        ChartLineAreaChartsOperations.ValidateStyle(textStyle);
        int index = 0;
        while (index < layout.CellCount)
        {
            ChartTableCellLayout cell = layout.CellAt(index);
            string suffix = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            scene.AddRect(id + "-cell-" + suffix, cell.Bounds.X, cell.Bounds.Y, cell.Bounds.Width, cell.Bounds.Height, 0.0, 0.0, cellStyle);
            scene.AddText(id + "-text-" + suffix, cell.Text, cell.Bounds.X + 4.0, cell.Bounds.Y + cell.Bounds.Height * 0.5, textStyle);
            index = index + 1;
        }
    }

    internal static ChartRenderScene CreateTableScene(string id, int width, int height, string[] headers, string[] values, int rowCount, int columnCount, ChartRect bounds, double headerHeight, ChartRenderStyle cellStyle, ChartRenderStyle textStyle)
    {
        ChartTableLayout layout = LayoutTable(headers, values, rowCount, columnCount, bounds, headerHeight);
        ChartRenderScene scene = new ChartRenderScene(width, height);
        AddTableToScene(scene, id, layout, cellStyle, textStyle);
        return scene;
    }

    /// <summary>
    /// Computes the layout for sparkline within the supplied bounds.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "bounds">The rectangle that constrains the generated layout or drawing.</param>
    /// <returns>The resulting sparkline layout.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public static ChartSparklineLayout LayoutSparkline(double[] values, ChartRect bounds)
    {
        ChartStatisticsOperations.ValidateFiniteValues(values);
        if (values.Length < 2)
        {
            throw new System.ArgumentException("Sparkline requires at least two values.");
        }

        ChartAxis.ValidatePlotArea(bounds);
        double minimum = ChartStatisticsOperations.Min(values);
        double maximum = ChartStatisticsOperations.Max(values);
        ChartPoint[] points = new ChartPoint[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            double x = bounds.X;
            if (values.Length > 1)
            {
                x = bounds.X + bounds.Width * index / (double)(values.Length - 1);
            }

            double y = bounds.Y + bounds.Height * 0.5;
            if (minimum != maximum)
            {
                double amount = (values[index] - minimum) / (maximum - minimum);
                y = bounds.Bottom() - bounds.Height * amount;
            }

            points[index] = new ChartPoint(x, y);
            index = index + 1;
        }

        return new ChartSparklineLayout(points, minimum, maximum);
    }

    /// <summary>
    /// Adds the supplied sparkline to scene to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "layout">The layout.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static void AddSparklineToScene(IChartRenderTarget scene, string id, ChartSparklineLayout layout, ChartRenderStyle style)
    {
        if (scene == null)
        {
            throw new System.ArgumentNullException("scene");
        }

        ChartPlot.ValidateId(id);
        if (layout == null)
        {
            throw new System.ArgumentNullException("layout");
        }

        ChartLineAreaChartsOperations.ValidateStyle(style);
        scene.AddPolyline(id + "-sparkline", layout.Points, style);
    }

    /// <summary>
    /// Creates the requested sparkline scene from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created render scene.</returns>
    public static ChartRenderScene CreateSparklineScene(string id, int width, int height, double[] values, ChartRenderStyle style)
    {
        ChartRenderScene scene = new ChartRenderScene(width, height);
        AddSparklineToScene(scene, id, LayoutSparkline(values, new ChartRect(0.0, 0.0, width, height)), style);
        return scene;
    }

    internal static void ValidateLabelsAndPairs(string[] labels, double[] startValues, double[] endValues, int minimumLength)
    {
        ValidateLabels(labels, minimumLength);
        ChartCartesianTransform.ValidateValueArrays(startValues, endValues, minimumLength);
        if (labels.Length != startValues.Length)
        {
            throw new System.ArgumentException("Label count must match values.");
        }
    }

    internal static void ValidateTriples(double[] first, double[] second, double[] third, int minimumLength)
    {
        ChartCartesianTransform.ValidateValueArrays(first, second, minimumLength);
        ChartCartesianTransform.ValidateValueArrays(first, third, minimumLength);
    }

    internal static void ValidateBumpInputs(string[] labels, double[] xValues, ChartDataMatrix ranks)
    {
        ValidateLabels(labels, 1);
        ChartLineAreaChartsOperations.ValidateMatrixRows(ranks, xValues, 2);
        if (ranks.Columns != labels.Length)
        {
            throw new System.ArgumentException("Bump chart label count must match rank columns.");
        }
    }

    internal static void ValidateTableInputs(string[] headers, string[] values, int rowCount, int columnCount, ChartRect bounds, double headerHeight)
    {
        ValidateLabels(headers, 1);
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        if (rowCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException("rowCount");
        }

        if (columnCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException("columnCount");
        }

        if (headers.Length != columnCount)
        {
            throw new System.ArgumentException("Header count must match column count.");
        }

        if (values.Length != (long)rowCount * columnCount)
        {
            throw new System.ArgumentException("Table value count must match rows times columns.");
        }

        ChartAxis.ValidatePlotArea(bounds);
        ChartRenderStyle.ValidatePositive(headerHeight, "headerHeight");
        if (headerHeight >= bounds.Height)
        {
            throw new System.ArgumentException("Header height must leave space for body rows.");
        }

        int index = 0;
        while (index < values.Length)
        {
            ChartTextBlock.ValidateText(values[index], "values");
            index = index + 1;
        }
    }

    internal static void ValidateLabels(string[] labels, int minimumLength)
    {
        if (labels == null)
        {
            throw new System.ArgumentNullException("labels");
        }

        if (labels.Length < minimumLength)
        {
            throw new System.ArgumentException("Not enough labels.");
        }

        int index = 0;
        while (index < labels.Length)
        {
            ChartTextBlock.ValidateText(labels[index], "labels");
            index = index + 1;
        }
    }
}
