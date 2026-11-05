namespace chart;
internal static class ChartGridChartsOperations
{
    /// <summary>
    /// Creates the requested heatmap series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xCenters">The x centers.</param>
    /// <param name = "yCenters">The y centers.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "cellWidth">The cell width.</param>
    /// <param name = "cellHeight">The cell height.</param>
    /// <param name = "visualMap">The visual map.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateHeatmapSeries(string id, double[] xCenters, double[] yCenters, ChartDataMatrix values, double cellWidth, double cellHeight, ChartVisualMap visualMap)
    {
        ChartPlot.ValidateId(id);
        ValidateGridInputs(xCenters, yCenters, values);
        ChartRenderStyle.ValidatePositive(cellWidth, "cellWidth");
        ChartRenderStyle.ValidatePositive(cellHeight, "cellHeight");
        ValidateVisualMap(visualMap);
        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Area);
        int row = 0;
        while (row < values.Rows)
        {
            int column = 0;
            while (column < values.Columns)
            {
                double value = values.GetValue(row, column);
                ChartRenderStyle style = ChartRenderStyle.CreateFill(visualMap.MapNumber(value));
                string cellId = id + "-cell-" + row.ToString(System.Globalization.CultureInfo.InvariantCulture) + "-" + column.ToString(System.Globalization.CultureInfo.InvariantCulture);
                series.AddCommand(ChartRenderCommand.CreatePolygon(cellId, CreateCellPoints(xCenters[column], yCenters[row], cellWidth, cellHeight), style));
                column = column + 1;
            }

            row = row + 1;
        }

        return series;
    }

    /// <summary>
    /// Creates the requested matrix image series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "visualMap">The visual map.</param>
    /// <returns>The newly created cartesian series.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static ChartCartesianSeries CreateMatrixImageSeries(string id, ChartDataMatrix values, ChartVisualMap visualMap)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        return CreateHeatmapSeries(id, IndexCenters(values.Columns), IndexCenters(values.Rows), values, 1.0, 1.0, visualMap);
    }

    /// <summary>
    /// Creates the requested calendar heatmap series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "dates">The dates.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "cellWidth">The cell width.</param>
    /// <param name = "cellHeight">The cell height.</param>
    /// <param name = "visualMap">The visual map.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateCalendarHeatmapSeries(string id, System.DateTime[] dates, double[] values, double cellWidth, double cellHeight, ChartVisualMap visualMap)
    {
        ChartPlot.ValidateId(id);
        ValidateDatesAndValues(dates, values);
        ChartRenderStyle.ValidatePositive(cellWidth, "cellWidth");
        ChartRenderStyle.ValidatePositive(cellHeight, "cellHeight");
        ValidateVisualMap(visualMap);
        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Area);
        System.DateTime start = dates[0].Date;
        int index = 0;
        while (index < dates.Length)
        {
            double week = WeekIndex(start, dates[index]);
            double day = (int)dates[index].DayOfWeek;
            ChartRenderStyle style = ChartRenderStyle.CreateFill(visualMap.MapNumber(values[index]));
            series.AddCommand(ChartRenderCommand.CreatePolygon(id + "-day-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture), CreateCellPoints(week, day, cellWidth, cellHeight), style));
            index = index + 1;
        }

        return series;
    }

    /// <summary>
    /// Groups values into 2 d using the supplied data and configuration.
    /// </summary>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "xBinCount">The requested number of x bin entries.</param>
    /// <param name = "yBinCount">The requested number of y bin entries.</param>
    /// <returns>The resulting histogram2 d.</returns>
    public static ChartHistogram2D Bin2D(double[] xValues, double[] yValues, int xBinCount, int yBinCount)
    {
        ChartCartesianTransform.ValidateValueArrays(xValues, yValues, 1);
        ValidateBinCount(xBinCount, "xBinCount");
        ValidateBinCount(yBinCount, "yBinCount");
        double xMinimum = ChartStatisticsOperations.Min(xValues);
        double xMaximum = ChartStatisticsOperations.Max(xValues);
        double yMinimum = ChartStatisticsOperations.Min(yValues);
        double yMaximum = ChartStatisticsOperations.Max(yValues);
        if (xMinimum == xMaximum)
        {
            xMinimum = xMinimum - 0.5;
            xMaximum = xMaximum + 0.5;
        }

        if (yMinimum == yMaximum)
        {
            yMinimum = yMinimum - 0.5;
            yMaximum = yMaximum + 0.5;
        }

        ChartDataMatrix counts = new ChartDataMatrix(yBinCount, xBinCount);
        double xWidth = (xMaximum - xMinimum) / xBinCount;
        double yWidth = (yMaximum - yMinimum) / yBinCount;
        int index = 0;
        while (index < xValues.Length)
        {
            int xIndex = ChartBinnerOperations.FindBinIndex(xValues[index], xMinimum, xMaximum, xWidth, xBinCount);
            int yIndex = ChartBinnerOperations.FindBinIndex(yValues[index], yMinimum, yMaximum, yWidth, yBinCount);
            counts.SetValue(yIndex, xIndex, counts.GetValue(yIndex, xIndex) + 1.0);
            index = index + 1;
        }

        return new ChartHistogram2D(counts, xMinimum, xMaximum, yMinimum, yMaximum);
    }

    /// <summary>
    /// Creates the requested histogram2 d series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "histogram">The histogram.</param>
    /// <param name = "visualMap">The visual map.</param>
    /// <returns>The newly created cartesian series.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static ChartCartesianSeries CreateHistogram2DSeries(string id, ChartHistogram2D histogram, ChartVisualMap visualMap)
    {
        ChartPlot.ValidateId(id);
        if (histogram == null)
        {
            throw new System.ArgumentNullException("histogram");
        }

        return CreateHeatmapSeries(id, BinCenters(histogram.XMinimum, histogram.XBinWidth, histogram.Counts.Columns), BinCenters(histogram.YMinimum, histogram.YBinWidth, histogram.Counts.Rows), histogram.Counts, histogram.XBinWidth, histogram.YBinWidth, visualMap);
    }

    /// <summary>
    /// Applies the hexbin operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "radius">The radius in chart units, which must be positive.</param>
    /// <returns>The resulting hexbin result.</returns>
    public static ChartHexbinResult Hexbin(double[] xValues, double[] yValues, double radius)
    {
        ChartCartesianTransform.ValidateValueArrays(xValues, yValues, 1);
        ChartRenderStyle.ValidatePositive(radius, "radius");
        ChartHexbinResult result = new ChartHexbinResult(radius);
        int index = 0;
        while (index < xValues.Length)
        {
            double centerX = HexCenterX(xValues[index], radius);
            double centerY = HexCenterY(yValues[index], radius);
            result.AddOrIncrement(centerX, centerY);
            index = index + 1;
        }

        return result;
    }

    /// <summary>
    /// Creates the requested hexbin series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "result">The validation result that receives any discovered issues.</param>
    /// <param name = "visualMap">The visual map.</param>
    /// <returns>The newly created cartesian series.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static ChartCartesianSeries CreateHexbinSeries(string id, ChartHexbinResult result, ChartVisualMap visualMap)
    {
        ChartPlot.ValidateId(id);
        if (result == null)
        {
            throw new System.ArgumentNullException("result");
        }

        ValidateVisualMap(visualMap);
        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Area);
        int index = 0;
        while (index < result.CellCount)
        {
            ChartHexbinCell cell = result.CellAt(index);
            ChartRenderStyle style = ChartRenderStyle.CreateFill(visualMap.MapNumber(cell.Count));
            series.AddCommand(ChartRenderCommand.CreatePolygon(id + "-hex-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture), CreateHexagonPoints(cell.CenterX, cell.CenterY, result.Radius), style));
            index = index + 1;
        }

        return series;
    }

    /// <summary>
    /// Applies the contour segments operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "xCenters">The x centers.</param>
    /// <param name = "yCenters">The y centers.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "level">The level.</param>
    /// <returns>The resulting contour segment array.</returns>
    public static ChartContourSegment[] ContourSegments(double[] xCenters, double[] yCenters, ChartDataMatrix values, double level)
    {
        ValidateGridInputs(xCenters, yCenters, values);
        ChartLinearScale.ValidateFinite(level, "level");
        System.Collections.Generic.List<ChartContourSegment> segments = new System.Collections.Generic.List<ChartContourSegment>();
        int row = 0;
        while (row < values.Rows - 1)
        {
            int column = 0;
            while (column < values.Columns - 1)
            {
                AddCellContourSegments(segments, xCenters, yCenters, values, row, column, level);
                column = column + 1;
            }

            row = row + 1;
        }

        return segments.ToArray();
    }

    /// <summary>
    /// Creates the requested contour line series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xCenters">The x centers.</param>
    /// <param name = "yCenters">The y centers.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "levels">The levels.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateContourLineSeries(string id, double[] xCenters, double[] yCenters, ChartDataMatrix values, double[] levels, ChartRenderStyle style)
    {
        ChartPlot.ValidateId(id);
        ValidateLevels(levels);
        ChartLineAreaChartsOperations.ValidateStyle(style);
        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Line);
        int levelIndex = 0;
        while (levelIndex < levels.Length)
        {
            ChartContourSegment[] segments = ContourSegments(xCenters, yCenters, values, levels[levelIndex]);
            int segmentIndex = 0;
            while (segmentIndex < segments.Length)
            {
                ChartContourSegment segment = segments[segmentIndex];
                string segmentId = id + "-level-" + levelIndex.ToString(System.Globalization.CultureInfo.InvariantCulture) + "-segment-" + segmentIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
                series.AddLine(segmentId, segment.Start.X, segment.Start.Y, segment.End.X, segment.End.Y, style);
                segmentIndex = segmentIndex + 1;
            }

            levelIndex = levelIndex + 1;
        }

        return series;
    }

    /// <summary>
    /// Creates the requested filled contour series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xCenters">The x centers.</param>
    /// <param name = "yCenters">The y centers.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "visualMap">The visual map.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateFilledContourSeries(string id, double[] xCenters, double[] yCenters, ChartDataMatrix values, ChartVisualMap visualMap)
    {
        return CreateHeatmapSeries(id, xCenters, yCenters, values, CellSpacing(xCenters), CellSpacing(yCenters), visualMap);
    }

    internal static void AddCellContourSegments(System.Collections.Generic.List<ChartContourSegment> segments, double[] xCenters, double[] yCenters, ChartDataMatrix values, int row, int column, double level)
    {
        if (segments == null)
        {
            throw new System.ArgumentNullException("segments");
        }

        double x0 = xCenters[column];
        double x1 = xCenters[column + 1];
        double y0 = yCenters[row];
        double y1 = yCenters[row + 1];
        double v00 = values.GetValue(row, column);
        double v10 = values.GetValue(row, column + 1);
        double v11 = values.GetValue(row + 1, column + 1);
        double v01 = values.GetValue(row + 1, column);
        ChartPoint[] points = new ChartPoint[4];
        int count = 0;
        count = AddContourIntersection(points, count, x0, y0, v00, x1, y0, v10, level);
        count = AddContourIntersection(points, count, x1, y0, v10, x1, y1, v11, level);
        count = AddContourIntersection(points, count, x1, y1, v11, x0, y1, v01, level);
        count = AddContourIntersection(points, count, x0, y1, v01, x0, y0, v00, level);
        if (count >= 2)
        {
            segments.Add(new ChartContourSegment(level, points[0], points[1]));
        }

        if (count >= 4)
        {
            segments.Add(new ChartContourSegment(level, points[2], points[3]));
        }
    }

    internal static int AddContourIntersection(ChartPoint[] points, int count, double x0, double y0, double v0, double x1, double y1, double v1, double level)
    {
        if (!CrossesLevel(v0, v1, level))
        {
            return count;
        }

        if (count >= points.Length)
        {
            return count;
        }

        double amount = (level - v0) / (v1 - v0);
        points[count] = new ChartPoint(x0 + (x1 - x0) * amount, y0 + (y1 - y0) * amount);
        return count + 1;
    }

    internal static bool CrossesLevel(double first, double second, double level)
    {
        ChartLinearScale.ValidateFinite(first, "first");
        ChartLinearScale.ValidateFinite(second, "second");
        ChartLinearScale.ValidateFinite(level, "level");
        if (first == second)
        {
            return false;
        }

        return (first <= level && second > level) || (second <= level && first > level);
    }

    internal static ChartPoint[] CreateCellPoints(double centerX, double centerY, double width, double height)
    {
        ChartLinearScale.ValidateFinite(centerX, "centerX");
        ChartLinearScale.ValidateFinite(centerY, "centerY");
        ChartRenderStyle.ValidatePositive(width, "width");
        ChartRenderStyle.ValidatePositive(height, "height");
        double halfWidth = width * 0.5;
        double halfHeight = height * 0.5;
        return new ChartPoint[]
        {
            new ChartPoint(centerX - halfWidth, centerY - halfHeight),
            new ChartPoint(centerX - halfWidth, centerY + halfHeight),
            new ChartPoint(centerX + halfWidth, centerY + halfHeight),
            new ChartPoint(centerX + halfWidth, centerY - halfHeight)
        };
    }

    internal static ChartPoint[] CreateHexagonPoints(double centerX, double centerY, double radius)
    {
        ChartLinearScale.ValidateFinite(centerX, "centerX");
        ChartLinearScale.ValidateFinite(centerY, "centerY");
        ChartRenderStyle.ValidatePositive(radius, "radius");
        ChartPoint[] points = new ChartPoint[6];
        int index = 0;
        while (index < 6)
        {
            double angle = System.Math.PI / 6.0 + index * System.Math.PI / 3.0;
            points[index] = new ChartPoint(centerX + System.Math.Cos(angle) * radius, centerY + System.Math.Sin(angle) * radius);
            index = index + 1;
        }

        return points;
    }

    internal static double[] IndexCenters(int count)
    {
        ValidateBinCount(count, "count");
        double[] centers = new double[count];
        int index = 0;
        while (index < count)
        {
            centers[index] = index + 0.5;
            index = index + 1;
        }

        return centers;
    }

    internal static double[] BinCenters(double minimum, double width, int count)
    {
        ChartLinearScale.ValidateFinite(minimum, "minimum");
        ChartRenderStyle.ValidatePositive(width, "width");
        ValidateBinCount(count, "count");
        double[] centers = new double[count];
        int index = 0;
        while (index < count)
        {
            centers[index] = minimum + width * (index + 0.5);
            index = index + 1;
        }

        return centers;
    }

    internal static double CellSpacing(double[] centers)
    {
        ChartScatterChartsOperations.ValidateValues(centers, 1, "centers");
        if (centers.Length == 1)
        {
            return 1.0;
        }

        return System.Math.Abs(centers[1] - centers[0]);
    }

    internal static double HexCenterX(double value, double radius)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        ChartRenderStyle.ValidatePositive(radius, "radius");
        double step = System.Math.Sqrt(3.0) * radius;
        return System.Math.Round(value / step) * step;
    }

    internal static double HexCenterY(double value, double radius)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        ChartRenderStyle.ValidatePositive(radius, "radius");
        double step = 1.5 * radius;
        return System.Math.Round(value / step) * step;
    }

    internal static double WeekIndex(System.DateTime start, System.DateTime value)
    {
        int days = (int)(value.Date - start.Date).TotalDays;
        return days / 7;
    }

    internal static void ValidateGridInputs(double[] xCenters, double[] yCenters, ChartDataMatrix values)
    {
        ChartScatterChartsOperations.ValidateValues(xCenters, 1, "xCenters");
        ChartScatterChartsOperations.ValidateValues(yCenters, 1, "yCenters");
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        if (values.Columns != xCenters.Length || values.Rows != yCenters.Length)
        {
            throw new System.ArgumentException("Grid center counts must match matrix dimensions.");
        }

        int index = 0;
        while (index < values.Values.Length)
        {
            ChartLinearScale.ValidateFinite(values.Values[index], "values");
            index = index + 1;
        }
    }

    internal static void ValidateDatesAndValues(System.DateTime[] dates, double[] values)
    {
        if (dates == null)
        {
            throw new System.ArgumentNullException("dates");
        }

        ChartScatterChartsOperations.ValidateValues(values, 1, "values");
        if (dates.Length != values.Length)
        {
            throw new System.ArgumentException("Date and value counts must match.");
        }
    }

    internal static void ValidateVisualMap(ChartVisualMap visualMap)
    {
        if (visualMap == null)
        {
            throw new System.ArgumentNullException("visualMap");
        }
    }

    internal static void ValidateBinCount(int count, string name)
    {
        if (count <= 0)
        {
            throw new System.ArgumentOutOfRangeException(name);
        }
    }

    internal static void ValidateLevels(double[] levels)
    {
        ChartScatterChartsOperations.ValidateValues(levels, 1, "levels");
    }
}
