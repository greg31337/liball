namespace chart;
internal static class ChartStatisticalChartsOperations
{
    /// <summary>
    /// Creates the requested histogram series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "histogram">The histogram.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static ChartCartesianSeries CreateHistogramSeries(string id, ChartHistogram histogram, ChartRenderStyle style)
    {
        ChartPlot.ValidateId(id);
        if (histogram == null)
        {
            throw new System.ArgumentNullException("histogram");
        }

        ChartLineAreaChartsOperations.ValidateStyle(style);
        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Bar);
        int index = 0;
        while (index < histogram.BinCount)
        {
            ChartHistogramBin bin = histogram.GetBin(index);
            double center = (bin.Minimum + bin.Maximum) * 0.5;
            ChartBarChartsOperations.AddVerticalBar(series, id + "-bin-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture), center, 0.0, bin.Count, bin.Width(), style);
            index = index + 1;
        }

        return series;
    }

    /// <summary>
    /// Creates the requested histogram series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "binCount">The requested number of histogram or color bins, which must be positive.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateHistogramSeries(string id, double[] values, int binCount, ChartRenderStyle style)
    {
        return CreateHistogramSeries(id, ChartBinnerOperations.BinFixedCount(values, binCount), style);
    }

    /// <summary>
    /// Creates the requested density series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "sampleCount">The requested number of sample entries.</param>
    /// <param name = "bandwidth">The bandwidth.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateDensitySeries(string id, double[] values, int sampleCount, double bandwidth, ChartRenderStyle style)
    {
        ChartPlot.ValidateId(id);
        ChartStatisticsOperations.ValidateFiniteValues(values);
        ValidateSampleCount(sampleCount);
        ValidateBandwidth(bandwidth);
        ChartLineAreaChartsOperations.ValidateStyle(style);
        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Line);
        series.AddCommand(ChartRenderCommand.CreatePolyline(id + "-density", EstimateDensityPoints(values, sampleCount, bandwidth), style));
        return series;
    }

    /// <summary>
    /// Creates the requested density series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "sampleCount">The requested number of sample entries.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateDensitySeries(string id, double[] values, int sampleCount, ChartRenderStyle style)
    {
        return CreateDensitySeries(id, values, sampleCount, SilvermanBandwidth(values), style);
    }

    /// <summary>
    /// Creates the requested ecdf series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateEcdfSeries(string id, double[] values, ChartRenderStyle style)
    {
        ChartPlot.ValidateId(id);
        ChartStatisticsOperations.ValidateFiniteValues(values);
        ChartLineAreaChartsOperations.ValidateStyle(style);
        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Line);
        series.AddCommand(ChartRenderCommand.CreatePolyline(id + "-ecdf", CreateEcdfPoints(values), style));
        return series;
    }

    /// <summary>
    /// Creates the requested box plot series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xCenter">The x center.</param>
    /// <param name = "boxWidth">The box width.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "boxStyle">The rendering style used for box.</param>
    /// <param name = "lineStyle">The rendering style used to stroke generated lines.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateBoxPlotSeries(string id, double xCenter, double boxWidth, double[] values, ChartRenderStyle boxStyle, ChartRenderStyle lineStyle)
    {
        ChartPlot.ValidateId(id);
        ChartLinearScale.ValidateFinite(xCenter, "xCenter");
        ChartBarChartsOperations.ValidateBarWidth(boxWidth, "boxWidth");
        ChartStatisticsOperations.ValidateFiniteValues(values);
        ChartLineAreaChartsOperations.ValidateStyle(boxStyle);
        ChartLineAreaChartsOperations.ValidateStyle(lineStyle);
        ChartBoxPlotSummary summary = CreateBoxPlotSummary(values);
        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Mixed);
        double half = boxWidth * 0.5;
        series.AddCommand(ChartRenderCommand.CreatePolygon(id + "-box", new ChartPoint[] { new ChartPoint(xCenter - half, summary.FirstQuartile), new ChartPoint(xCenter - half, summary.ThirdQuartile), new ChartPoint(xCenter + half, summary.ThirdQuartile), new ChartPoint(xCenter + half, summary.FirstQuartile) }, boxStyle));
        series.AddLine(id + "-median", xCenter - half, summary.Median, xCenter + half, summary.Median, lineStyle);
        series.AddLine(id + "-whisker", xCenter, summary.WhiskerMinimum, xCenter, summary.WhiskerMaximum, lineStyle);
        series.AddLine(id + "-low-cap", xCenter - half, summary.WhiskerMinimum, xCenter + half, summary.WhiskerMinimum, lineStyle);
        series.AddLine(id + "-high-cap", xCenter - half, summary.WhiskerMaximum, xCenter + half, summary.WhiskerMaximum, lineStyle);
        return series;
    }

    /// <summary>
    /// Creates the requested violin series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "center">The center.</param>
    /// <param name = "maximumWidth">The maximum width.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "sampleCount">The requested number of sample entries.</param>
    /// <param name = "bandwidth">The bandwidth.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateViolinSeries(string id, double center, double maximumWidth, double[] values, int sampleCount, double bandwidth, ChartRenderStyle style)
    {
        ChartPlot.ValidateId(id);
        ChartLineAreaChartsOperations.ValidateStyle(style);
        ChartViolinPlan plan = CreateViolinPlan(center, maximumWidth, values, sampleCount, bandwidth);
        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Area);
        series.AddCommand(ChartRenderCommand.CreatePolygon(id + "-violin", plan.PolygonPoints, style));
        return series;
    }

    /// <summary>
    /// Creates the requested error bar series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "lowerErrors">The lower errors.</param>
    /// <param name = "upperErrors">The upper errors.</param>
    /// <param name = "capWidth">The cap width.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateErrorBarSeries(string id, double[] xValues, double[] yValues, double[] lowerErrors, double[] upperErrors, double capWidth, ChartRenderStyle style)
    {
        ChartPlot.ValidateId(id);
        ChartCartesianTransform.ValidateValueArrays(xValues, yValues, 1);
        ValidateMatchingValues(lowerErrors, xValues.Length, "lowerErrors");
        ValidateMatchingValues(upperErrors, xValues.Length, "upperErrors");
        ChartBarChartsOperations.ValidateBarWidth(capWidth, "capWidth");
        ChartLineAreaChartsOperations.ValidateStyle(style);
        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Mixed);
        int index = 0;
        while (index < xValues.Length)
        {
            double low = yValues[index] - lowerErrors[index];
            double high = yValues[index] + upperErrors[index];
            double half = capWidth * 0.5;
            string suffix = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            series.AddLine(id + "-error-" + suffix, xValues[index], low, xValues[index], high, style);
            series.AddLine(id + "-low-cap-" + suffix, xValues[index] - half, low, xValues[index] + half, low, style);
            series.AddLine(id + "-high-cap-" + suffix, xValues[index] - half, high, xValues[index] + half, high, style);
            index = index + 1;
        }

        return series;
    }

    /// <summary>
    /// Creates the requested stem series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "baseline">The baseline.</param>
    /// <param name = "lineStyle">The rendering style used to stroke generated lines.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateStemSeries(string id, double[] xValues, double[] yValues, double baseline, ChartRenderStyle lineStyle)
    {
        ChartPlot.ValidateId(id);
        ChartCartesianTransform.ValidateValueArrays(xValues, yValues, 1);
        ChartLinearScale.ValidateFinite(baseline, "baseline");
        ChartLineAreaChartsOperations.ValidateStyle(lineStyle);
        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Line);
        int index = 0;
        while (index < xValues.Length)
        {
            series.AddLine(id + "-stem-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture), xValues[index], baseline, xValues[index], yValues[index], lineStyle);
            index = index + 1;
        }

        return series;
    }

    /// <summary>
    /// Creates the requested box plot summary from the supplied data and configuration.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The newly created box plot summary.</returns>
    public static ChartBoxPlotSummary CreateBoxPlotSummary(double[] values)
    {
        ChartStatisticsOperations.ValidateFiniteValues(values);
        double[] sorted = ChartDataMatrix.CopyDoubleArray(values);
        System.Array.Sort(sorted);
        ChartBoxPlotSummary summary = new ChartBoxPlotSummary(sorted[0], ChartBinnerOperations.Percentile(sorted, 0.25), ChartBinnerOperations.Percentile(sorted, 0.5), ChartBinnerOperations.Percentile(sorted, 0.75), sorted[sorted.Length - 1]);
        summary.WhiskerMinimum = WhiskerMinimum(sorted, summary.LowerFence);
        summary.WhiskerMaximum = WhiskerMaximum(sorted, summary.UpperFence);
        return summary;
    }

    /// <summary>
    /// Creates the requested violin plan from the supplied data and configuration.
    /// </summary>
    /// <param name = "center">The center.</param>
    /// <param name = "maximumWidth">The maximum width.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "sampleCount">The requested number of sample entries.</param>
    /// <param name = "bandwidth">The bandwidth.</param>
    /// <returns>The newly created violin plan.</returns>
    public static ChartViolinPlan CreateViolinPlan(double center, double maximumWidth, double[] values, int sampleCount, double bandwidth)
    {
        ChartLinearScale.ValidateFinite(center, "center");
        ChartRenderStyle.ValidatePositive(maximumWidth, "maximumWidth");
        ChartStatisticsOperations.ValidateFiniteValues(values);
        ValidateSampleCount(sampleCount);
        ValidateBandwidth(bandwidth);
        if (sampleCount > int.MaxValue / 2)
        {
            throw new System.ArgumentOutOfRangeException("sampleCount");
        }

        ChartPoint[] density = EstimateDensityPoints(values, sampleCount, bandwidth);
        double maximumDensity = MaximumDensity(density);
        ChartPoint[] polygon = new ChartPoint[sampleCount * 2];
        int index = 0;
        while (index < sampleCount)
        {
            double halfWidth = DensityWidth(density[index].Y, maximumDensity, maximumWidth);
            polygon[index] = new ChartPoint(center - halfWidth, density[index].X);
            index = index + 1;
        }

        index = 0;
        while (index < sampleCount)
        {
            int source = sampleCount - index - 1;
            double halfWidth = DensityWidth(density[source].Y, maximumDensity, maximumWidth);
            polygon[sampleCount + index] = new ChartPoint(center + halfWidth, density[source].X);
            index = index + 1;
        }

        return new ChartViolinPlan(center, maximumWidth, density, polygon);
    }

    internal static ChartPoint[] EstimateDensityPoints(double[] values, int sampleCount, double bandwidth)
    {
        ChartStatisticsOperations.ValidateFiniteValues(values);
        ValidateSampleCount(sampleCount);
        ValidateBandwidth(bandwidth);
        double minimum = ChartStatisticsOperations.Min(values);
        double maximum = ChartStatisticsOperations.Max(values);
        if (minimum == maximum)
        {
            minimum = minimum - bandwidth;
            maximum = maximum + bandwidth;
        }

        ChartPoint[] points = new ChartPoint[sampleCount];
        int index = 0;
        while (index < sampleCount)
        {
            double amount = index / (double)(sampleCount - 1);
            double x = minimum + (maximum - minimum) * amount;
            points[index] = new ChartPoint(x, GaussianDensity(values, x, bandwidth));
            index = index + 1;
        }

        return points;
    }

    internal static ChartPoint[] CreateEcdfPoints(double[] values)
    {
        ChartStatisticsOperations.ValidateFiniteValues(values);
        double[] sorted = ChartDataMatrix.CopyDoubleArray(values);
        System.Array.Sort(sorted);
        ChartPoint[] points = new ChartPoint[sorted.Length * 2 + 1];
        points[0] = new ChartPoint(sorted[0], 0.0);
        int output = 1;
        int index = 0;
        while (index < sorted.Length)
        {
            double previous = index / (double)sorted.Length;
            double current = (index + 1) / (double)sorted.Length;
            points[output] = new ChartPoint(sorted[index], previous);
            output = output + 1;
            points[output] = new ChartPoint(sorted[index], current);
            output = output + 1;
            index = index + 1;
        }

        return points;
    }

    internal static double GaussianDensity(double[] values, double x, double bandwidth)
    {
        ChartStatisticsOperations.ValidateFiniteValues(values);
        ChartLinearScale.ValidateFinite(x, "x");
        ValidateBandwidth(bandwidth);
        double scale = 1.0 / (values.Length * bandwidth);
        double sum = 0.0;
        int index = 0;
        while (index < values.Length)
        {
            sum = sum + GaussianKernel((x - values[index]) / bandwidth);
            index = index + 1;
        }

        return scale * sum;
    }

    internal static double GaussianKernel(double value)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        return System.Math.Exp(-0.5 * value * value) / System.Math.Sqrt(2.0 * System.Math.PI);
    }

    internal static double SilvermanBandwidth(double[] values)
    {
        ChartStatisticsOperations.ValidateFiniteValues(values);
        double deviation = ChartStatisticsOperations.StandardDeviation(values);
        if (deviation == 0.0)
        {
            double range = ChartStatisticsOperations.Max(values) - ChartStatisticsOperations.Min(values);
            if (range > 0.0)
            {
                deviation = range;
            }
            else
            {
                deviation = 1.0;
            }
        }

        return 1.06 * deviation / System.Math.Pow(values.Length, 0.2);
    }

    internal static double WhiskerMinimum(double[] sortedValues, double lowerFence)
    {
        ChartStatisticsOperations.ValidateFiniteValues(sortedValues);
        ChartLinearScale.ValidateFinite(lowerFence, "lowerFence");
        int index = 0;
        while (index < sortedValues.Length)
        {
            if (sortedValues[index] >= lowerFence)
            {
                return sortedValues[index];
            }

            index = index + 1;
        }

        return sortedValues[0];
    }

    internal static double WhiskerMaximum(double[] sortedValues, double upperFence)
    {
        ChartStatisticsOperations.ValidateFiniteValues(sortedValues);
        ChartLinearScale.ValidateFinite(upperFence, "upperFence");
        int index = sortedValues.Length - 1;
        while (index >= 0)
        {
            if (sortedValues[index] <= upperFence)
            {
                return sortedValues[index];
            }

            index = index - 1;
        }

        return sortedValues[sortedValues.Length - 1];
    }

    internal static double MaximumDensity(ChartPoint[] densityPoints)
    {
        ChartRenderCommand.ValidatePoints(densityPoints, 1);
        double maximum = densityPoints[0].Y;
        int index = 1;
        while (index < densityPoints.Length)
        {
            if (densityPoints[index].Y > maximum)
            {
                maximum = densityPoints[index].Y;
            }

            index = index + 1;
        }

        return maximum;
    }

    internal static double DensityWidth(double density, double maximumDensity, double maximumWidth)
    {
        ChartStyle.ValidateNonNegative(density, "density");
        ChartRenderStyle.ValidatePositive(maximumWidth, "maximumWidth");
        if (maximumDensity <= 0.0 || double.IsNaN(maximumDensity) || double.IsInfinity(maximumDensity))
        {
            throw new System.ArgumentOutOfRangeException("maximumDensity");
        }

        return maximumWidth * density / maximumDensity;
    }

    internal static void ValidateSampleCount(int sampleCount)
    {
        if (sampleCount < 2)
        {
            throw new System.ArgumentOutOfRangeException("sampleCount");
        }
    }

    internal static void ValidateBandwidth(double bandwidth)
    {
        ChartRenderStyle.ValidatePositive(bandwidth, "bandwidth");
    }

    internal static void ValidateMatchingValues(double[] values, int expectedLength, string name)
    {
        if (expectedLength <= 0)
        {
            throw new System.ArgumentOutOfRangeException("expectedLength");
        }

        ChartScatterChartsOperations.ValidateValues(values, 1, name);
        if (values.Length != expectedLength)
        {
            throw new System.ArgumentException("Value count must match expected length.");
        }
    }
}
