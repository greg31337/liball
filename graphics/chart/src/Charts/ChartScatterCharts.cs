namespace chart;

/// <summary>
/// Creates scatter chart series, layouts, or retained drawing commands.
/// </summary>
/// <remarks>Builder methods validate array lengths and numeric inputs before producing render-ready objects.</remarks>
internal static class ChartScatterChartsOperations
{
    /// <summary>
    /// Creates the requested scatter series from the supplied data and configuration.
    /// </summary>
    /// <param name="id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name="xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name="yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name="shape">The shape.</param>
    /// <param name="markerSize">The marker size.</param>
    /// <param name="style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateScatterSeries(string id, double[] xValues, double[] yValues, ChartMarkerShape shape, double markerSize, ChartRenderStyle style)
    {
        ChartPlot.ValidateId(id);
        ChartRenderCommand.ValidateMarkerShape(shape);
        ChartRenderStyle.ValidatePositive(markerSize, "markerSize");
        ChartLineAreaChartsOperations.ValidateStyle(style);
        ChartCartesianTransform.ValidateValueArrays(xValues, yValues, 1);

        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Scatter);
        int index = 0;
        while (index < xValues.Length)
        {
            string pointId = id + "-point-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            series.AddMarker(pointId, shape, xValues[index], yValues[index], markerSize, style);
            index = index + 1;
        }

        return series;
    }

    /// <summary>
    /// Creates the requested bubble series from the supplied data and configuration.
    /// </summary>
    /// <param name="id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name="xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name="yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name="sizeValues">The size values in data order.</param>
    /// <param name="minimumSize">The minimum size.</param>
    /// <param name="maximumSize">The maximum size.</param>
    /// <param name="style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateBubbleSeries(string id, double[] xValues, double[] yValues, double[] sizeValues, double minimumSize, double maximumSize, ChartRenderStyle style)
    {
        ChartPlot.ValidateId(id);
        ChartLineAreaChartsOperations.ValidateStyle(style);
        ChartCartesianTransform.ValidateValueArrays(xValues, yValues, 1);
        ValidateSizeValues(sizeValues, xValues.Length);
        ValidateSizeRange(minimumSize, maximumSize);

        double minimumValue = Minimum(sizeValues);
        double maximumValue = Maximum(sizeValues);
        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Scatter);
        int index = 0;
        while (index < xValues.Length)
        {
            double markerSize = MapBubbleSize(sizeValues[index], minimumValue, maximumValue, minimumSize, maximumSize);
            string pointId = id + "-bubble-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            series.AddMarker(pointId, ChartMarkerShape.Circle, xValues[index], yValues[index], markerSize, style);
            index = index + 1;
        }

        return series;
    }

    /// <summary>
    /// Creates the requested rug series from the supplied data and configuration.
    /// </summary>
    /// <param name="id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name="values">The input values to process.</param>
    /// <param name="baseline">The baseline.</param>
    /// <param name="length">The length.</param>
    /// <param name="orientation">The orientation.</param>
    /// <param name="style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateRugSeries(string id, double[] values, double baseline, double length, ChartOrientation orientation, ChartRenderStyle style)
    {
        ChartPlot.ValidateId(id);
        ValidateValues(values, 1, "values");
        ChartLinearScale.ValidateFinite(baseline, "baseline");
        ValidateRugLength(length);
        ValidateRugOrientation(orientation);
        ChartLineAreaChartsOperations.ValidateStyle(style);

        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Line);
        int index = 0;
        while (index < values.Length)
        {
            string lineId = id + "-rug-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (orientation == ChartOrientation.Horizontal)
            {
                series.AddLine(lineId, values[index], baseline, values[index], baseline + length, style);
            }
            else
            {
                series.AddLine(lineId, baseline, values[index], baseline + length, values[index], style);
            }

            index = index + 1;
        }

        return series;
    }

    /// <summary>
    /// Creates the requested marginal scatter series from the supplied data and configuration.
    /// </summary>
    /// <param name="idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name="xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name="yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name="shape">The shape.</param>
    /// <param name="markerSize">The marker size.</param>
    /// <param name="scatterStyle">The rendering style used for scatter.</param>
    /// <param name="rugStyle">The rendering style used for rug.</param>
    /// <param name="rugLength">The rug length.</param>
    /// <returns>The newly created cartesian series array.</returns>
    public static ChartCartesianSeries[] CreateMarginalScatterSeries(string idPrefix, double[] xValues, double[] yValues, ChartMarkerShape shape, double markerSize, ChartRenderStyle scatterStyle, ChartRenderStyle rugStyle, double rugLength)
    {
        return CreateMarginalScatterSeries(idPrefix, xValues, yValues, shape, markerSize, scatterStyle, rugStyle, 0.0, 0.0, rugLength);
    }

    /// <summary>
    /// Creates the requested marginal scatter series from the supplied data and configuration.
    /// </summary>
    /// <param name="idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name="xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name="yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name="shape">The shape.</param>
    /// <param name="markerSize">The marker size.</param>
    /// <param name="scatterStyle">The rendering style used for scatter.</param>
    /// <param name="rugStyle">The rendering style used for rug.</param>
    /// <param name="xRugBaseline">The x rug baseline.</param>
    /// <param name="yRugBaseline">The y rug baseline.</param>
    /// <param name="rugLength">The rug length.</param>
    /// <returns>The newly created cartesian series array.</returns>
    public static ChartCartesianSeries[] CreateMarginalScatterSeries(string idPrefix, double[] xValues, double[] yValues, ChartMarkerShape shape, double markerSize, ChartRenderStyle scatterStyle, ChartRenderStyle rugStyle, double xRugBaseline, double yRugBaseline, double rugLength)
    {
        ChartPlot.ValidateId(idPrefix);
        ChartCartesianTransform.ValidateValueArrays(xValues, yValues, 1);
        ChartRenderCommand.ValidateMarkerShape(shape);
        ChartRenderStyle.ValidatePositive(markerSize, "markerSize");
        ChartLineAreaChartsOperations.ValidateStyle(scatterStyle);
        ChartLineAreaChartsOperations.ValidateStyle(rugStyle);
        ChartLinearScale.ValidateFinite(xRugBaseline, "xRugBaseline");
        ChartLinearScale.ValidateFinite(yRugBaseline, "yRugBaseline");
        ValidateRugLength(rugLength);

        ChartCartesianSeries[] series = new ChartCartesianSeries[3];
        series[0] = CreateScatterSeries(idPrefix + "-scatter", xValues, yValues, shape, markerSize, scatterStyle);
        series[1] = CreateRugSeries(idPrefix + "-x-rug", xValues, xRugBaseline, rugLength, ChartOrientation.Horizontal, rugStyle);
        series[2] = CreateRugSeries(idPrefix + "-y-rug", yValues, yRugBaseline, rugLength, ChartOrientation.Vertical, rugStyle);
        series[0].SetOrder(1);
        series[1].SetOrder(0);
        series[2].SetOrder(0);
        return series;
    }

    internal static double MapBubbleSize(double value, double minimumValue, double maximumValue, double minimumSize, double maximumSize)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        ChartLinearScale.ValidateFinite(minimumValue, "minimumValue");
        ChartLinearScale.ValidateFinite(maximumValue, "maximumValue");
        ValidateSizeRange(minimumSize, maximumSize);
        if (minimumValue == maximumValue)
        {
            return (minimumSize + maximumSize) * 0.5;
        }

        double amount = (value - minimumValue) / (maximumValue - minimumValue);
        amount = ChartColor.ClampDouble(amount, 0.0, 1.0);
        double minimumArea = minimumSize * minimumSize;
        double maximumArea = maximumSize * maximumSize;
        return System.Math.Sqrt(minimumArea + (maximumArea - minimumArea) * amount);
    }

    internal static double Minimum(double[] values)
    {
        ValidateValues(values, 1, "values");
        double minimum = values[0];
        int index = 1;
        while (index < values.Length)
        {
            if (values[index] < minimum)
            {
                minimum = values[index];
            }

            index = index + 1;
        }

        return minimum;
    }

    internal static double Maximum(double[] values)
    {
        ValidateValues(values, 1, "values");
        double maximum = values[0];
        int index = 1;
        while (index < values.Length)
        {
            if (values[index] > maximum)
            {
                maximum = values[index];
            }

            index = index + 1;
        }

        return maximum;
    }

    internal static void ValidateSizeValues(double[] sizeValues, int expectedLength)
    {
        if (expectedLength <= 0)
        {
            throw new System.ArgumentOutOfRangeException("expectedLength");
        }

        ValidateValues(sizeValues, 1, "sizeValues");
        if (sizeValues.Length != expectedLength)
        {
            throw new System.ArgumentException("Bubble size count must match point count.");
        }
    }

    internal static void ValidateValues(double[] values, int minimumLength, string name)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException(name);
        }

        if (values.Length < minimumLength)
        {
            throw new System.ArgumentException("Value count is below the required length.");
        }

        int index = 0;
        while (index < values.Length)
        {
            ChartLinearScale.ValidateFinite(values[index], name);
            index = index + 1;
        }
    }

    internal static void ValidateSizeRange(double minimumSize, double maximumSize)
    {
        ChartRenderStyle.ValidatePositive(minimumSize, "minimumSize");
        ChartRenderStyle.ValidatePositive(maximumSize, "maximumSize");
        if (maximumSize < minimumSize)
        {
            throw new System.ArgumentException("Maximum bubble size cannot be less than minimum size.");
        }
    }

    internal static void ValidateRugLength(double length)
    {
        ChartLinearScale.ValidateFinite(length, "length");
        if (length == 0.0)
        {
            throw new System.ArgumentOutOfRangeException("length");
        }
    }

    internal static void ValidateRugOrientation(ChartOrientation orientation)
    {
        if (orientation != ChartOrientation.Horizontal && orientation != ChartOrientation.Vertical)
        {
            throw new System.ArgumentException("Rug orientation must be horizontal or vertical.");
        }
    }
}
