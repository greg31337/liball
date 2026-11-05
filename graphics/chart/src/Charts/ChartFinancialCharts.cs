namespace chart;

/// <summary>
/// Creates financial chart series, layouts, or retained drawing commands.
/// </summary>
/// <remarks>Builder methods validate array lengths and numeric inputs before producing render-ready objects.</remarks>
internal static class ChartFinancialChartsOperations
{
    /// <summary>
    /// Creates the requested candlestick series from the supplied data and configuration.
    /// </summary>
    /// <param name="id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name="xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name="openValues">The open values in data order.</param>
    /// <param name="highValues">The high values in data order.</param>
    /// <param name="lowValues">The low values in data order.</param>
    /// <param name="closeValues">The close values in data order.</param>
    /// <param name="bodyWidth">The body width.</param>
    /// <param name="upStyle">The rendering style used for up.</param>
    /// <param name="downStyle">The rendering style used for down.</param>
    /// <param name="wickStyle">The rendering style used for wick.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateCandlestickSeries(string id, double[] xValues, double[] openValues, double[] highValues, double[] lowValues, double[] closeValues, double bodyWidth, ChartRenderStyle upStyle, ChartRenderStyle downStyle, ChartRenderStyle wickStyle)
    {
        ChartPlot.ValidateId(id);
        ValidateOhlcValues(xValues, openValues, highValues, lowValues, closeValues, 1);
        ChartBarChartsOperations.ValidateBarWidth(bodyWidth, "bodyWidth");
        ChartLineAreaChartsOperations.ValidateStyle(upStyle);
        ChartLineAreaChartsOperations.ValidateStyle(downStyle);
        ChartLineAreaChartsOperations.ValidateStyle(wickStyle);

        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Mixed);
        int index = 0;
        while (index < xValues.Length)
        {
            string suffix = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            ChartRenderStyle bodyStyle = CandleStyle(openValues[index], closeValues[index], upStyle, downStyle);
            series.AddLine(id + "-wick-" + suffix, xValues[index], lowValues[index], xValues[index], highValues[index], wickStyle);
            ChartBarChartsOperations.AddVerticalBar(series, id + "-body-" + suffix, xValues[index], openValues[index], closeValues[index], bodyWidth, bodyStyle);
            index = index + 1;
        }

        return series;
    }

    /// <summary>
    /// Creates the requested ohlc series from the supplied data and configuration.
    /// </summary>
    /// <param name="id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name="xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name="openValues">The open values in data order.</param>
    /// <param name="highValues">The high values in data order.</param>
    /// <param name="lowValues">The low values in data order.</param>
    /// <param name="closeValues">The close values in data order.</param>
    /// <param name="tickWidth">The tick width.</param>
    /// <param name="upStyle">The rendering style used for up.</param>
    /// <param name="downStyle">The rendering style used for down.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateOhlcSeries(string id, double[] xValues, double[] openValues, double[] highValues, double[] lowValues, double[] closeValues, double tickWidth, ChartRenderStyle upStyle, ChartRenderStyle downStyle)
    {
        ChartPlot.ValidateId(id);
        ValidateOhlcValues(xValues, openValues, highValues, lowValues, closeValues, 1);
        ChartBarChartsOperations.ValidateBarWidth(tickWidth, "tickWidth");
        ChartLineAreaChartsOperations.ValidateStyle(upStyle);
        ChartLineAreaChartsOperations.ValidateStyle(downStyle);

        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Mixed);
        double half = tickWidth * 0.5;
        int index = 0;
        while (index < xValues.Length)
        {
            string suffix = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            ChartRenderStyle style = CandleStyle(openValues[index], closeValues[index], upStyle, downStyle);
            series.AddLine(id + "-range-" + suffix, xValues[index], lowValues[index], xValues[index], highValues[index], style);
            series.AddLine(id + "-open-" + suffix, xValues[index] - half, openValues[index], xValues[index], openValues[index], style);
            series.AddLine(id + "-close-" + suffix, xValues[index], closeValues[index], xValues[index] + half, closeValues[index], style);
            index = index + 1;
        }

        return series;
    }

    /// <summary>
    /// Creates the requested volume series from the supplied data and configuration.
    /// </summary>
    /// <param name="id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name="xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name="volumeValues">The volume values in data order.</param>
    /// <param name="barWidth">The bar width.</param>
    /// <param name="baseline">The baseline.</param>
    /// <param name="style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateVolumeSeries(string id, double[] xValues, double[] volumeValues, double barWidth, double baseline, ChartRenderStyle style)
    {
        ChartPlot.ValidateId(id);
        ValidateVolumes(xValues, volumeValues, 1);
        ChartBarChartsOperations.ValidateBarWidth(barWidth, "barWidth");
        ChartLinearScale.ValidateFinite(baseline, "baseline");
        ChartLineAreaChartsOperations.ValidateStyle(style);

        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Bar);
        int index = 0;
        while (index < xValues.Length)
        {
            ChartBarChartsOperations.AddVerticalBar(series, id + "-volume-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture), xValues[index], baseline, volumeValues[index], barWidth, style);
            index = index + 1;
        }

        return series;
    }

    /// <summary>
    /// Creates the requested volume series from the supplied data and configuration.
    /// </summary>
    /// <param name="id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name="xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name="openValues">The open values in data order.</param>
    /// <param name="closeValues">The close values in data order.</param>
    /// <param name="volumeValues">The volume values in data order.</param>
    /// <param name="barWidth">The bar width.</param>
    /// <param name="baseline">The baseline.</param>
    /// <param name="upStyle">The rendering style used for up.</param>
    /// <param name="downStyle">The rendering style used for down.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateVolumeSeries(string id, double[] xValues, double[] openValues, double[] closeValues, double[] volumeValues, double barWidth, double baseline, ChartRenderStyle upStyle, ChartRenderStyle downStyle)
    {
        ChartPlot.ValidateId(id);
        ChartCartesianTransform.ValidateValueArrays(xValues, openValues, 1);
        ChartCartesianTransform.ValidateValueArrays(xValues, closeValues, 1);
        ValidateVolumes(xValues, volumeValues, 1);
        ChartBarChartsOperations.ValidateBarWidth(barWidth, "barWidth");
        ChartLinearScale.ValidateFinite(baseline, "baseline");
        ChartLineAreaChartsOperations.ValidateStyle(upStyle);
        ChartLineAreaChartsOperations.ValidateStyle(downStyle);

        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Bar);
        int index = 0;
        while (index < xValues.Length)
        {
            ChartRenderStyle style = CandleStyle(openValues[index], closeValues[index], upStyle, downStyle);
            ChartBarChartsOperations.AddVerticalBar(series, id + "-volume-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture), xValues[index], baseline, volumeValues[index], barWidth, style);
            index = index + 1;
        }

        return series;
    }

    /// <summary>
    /// Creates the requested moving average series from the supplied data and configuration.
    /// </summary>
    /// <param name="id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name="xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name="values">The input values to process.</param>
    /// <param name="period">The period.</param>
    /// <param name="style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateMovingAverageSeries(string id, double[] xValues, double[] values, int period, ChartRenderStyle style)
    {
        ChartPlot.ValidateId(id);
        ChartCartesianTransform.ValidateValueArrays(xValues, values, 1);
        ValidatePeriod(period, values.Length);
        ChartLineAreaChartsOperations.ValidateStyle(style);
        return ChartLineAreaChartsOperations.CreateLineSeries(id, MovingAverageXValues(xValues, period), MovingAverage(values, period), style);
    }

    /// <summary>
    /// Applies the moving average operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name="values">The input values to process.</param>
    /// <param name="period">The period.</param>
    /// <returns>The resulting double array.</returns>
    public static double[] MovingAverage(double[] values, int period)
    {
        ChartStatisticsOperations.ValidateFiniteValues(values);
        ValidatePeriod(period, values.Length);

        double[] averages = new double[values.Length - period + 1];
        double sum = 0.0;
        int index = 0;
        while (index < period)
        {
            sum = sum + values[index];
            index = index + 1;
        }

        averages[0] = sum / period;
        index = period;
        while (index < values.Length)
        {
            sum = sum + values[index] - values[index - period];
            averages[index - period + 1] = sum / period;
            index = index + 1;
        }

        return averages;
    }

    /// <summary>
    /// Applies the moving average x values operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name="xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name="period">The period.</param>
    /// <returns>The resulting double array.</returns>
    public static double[] MovingAverageXValues(double[] xValues, int period)
    {
        ChartStatisticsOperations.ValidateFiniteValues(xValues);
        ValidatePeriod(period, xValues.Length);
        double[] result = new double[xValues.Length - period + 1];
        int index = period - 1;
        while (index < xValues.Length)
        {
            result[index - period + 1] = xValues[index];
            index = index + 1;
        }

        return result;
    }

    /// <summary>
    /// Generates financial date ticks from the supplied data and configuration.
    /// </summary>
    /// <param name="dates">The dates.</param>
    /// <param name="stride">The number of bytes between the start of adjacent bitmap rows.</param>
    /// <param name="pattern">The pattern.</param>
    /// <returns>The resulting tick array.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref="System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public static ChartTick[] GenerateFinancialDateTicks(System.DateTime[] dates, int stride, string pattern)
    {
        ValidateDates(dates);
        if (stride <= 0)
        {
            throw new System.ArgumentOutOfRangeException("stride");
        }

        if (pattern == null)
        {
            throw new System.ArgumentNullException("pattern");
        }

        if (pattern.Length == 0)
        {
            throw new System.ArgumentException("Date pattern cannot be empty.");
        }

        int lastStrideIndex = ((dates.Length - 1) / stride) * stride;
        int count = (dates.Length - 1) / stride + 1;
        if (lastStrideIndex != dates.Length - 1)
        {
            count = count + 1;
        }

        ChartTick[] ticks = new ChartTick[count];
        int output = 0;
        int index = 0;
        while (index < dates.Length)
        {
            ticks[output] = new ChartTick(ChartTickKind.Explicit, index, ChartTickFormatterOperations.FormatDate(dates[index], pattern), output);
            output = output + 1;
            index = index + stride;
        }

        if (lastStrideIndex != dates.Length - 1)
        {
            ticks[output] = new ChartTick(ChartTickKind.Explicit, dates.Length - 1, ChartTickFormatterOperations.FormatDate(dates[dates.Length - 1], pattern), output);
        }

        return ticks;
    }

    /// <summary>
    /// Applies the date index values operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name="dates">The dates.</param>
    /// <returns>The resulting double array.</returns>
    public static double[] DateIndexValues(System.DateTime[] dates)
    {
        ValidateDates(dates);
        double[] values = new double[dates.Length];
        int index = 0;
        while (index < dates.Length)
        {
            values[index] = index;
            index = index + 1;
        }

        return values;
    }

    /// <summary>
    /// Applies the date tick values operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name="dates">The dates.</param>
    /// <returns>The resulting double array.</returns>
    public static double[] DateTickValues(System.DateTime[] dates)
    {
        ValidateDates(dates);
        double[] values = new double[dates.Length];
        int index = 0;
        while (index < dates.Length)
        {
            values[index] = dates[index].Ticks;
            index = index + 1;
        }

        return values;
    }

    /// <summary>
    /// Formats price as deterministic display text.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="decimalPlaces">The decimal places.</param>
    /// <param name="currencySymbol">The currency symbol.</param>
    /// <returns>The resulting string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static string FormatPrice(double value, int decimalPlaces, string currencySymbol)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        ChartTickFormatterOperations.ValidateDecimalPlaces(decimalPlaces);
        if (currencySymbol == null)
        {
            throw new System.ArgumentNullException("currencySymbol");
        }

        return currencySymbol + ChartTickFormatterOperations.FormatNumber(value, decimalPlaces);
    }

    /// <summary>
    /// Formats volume as deterministic display text.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The resulting string.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public static string FormatVolume(double value)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        if (value < 0.0)
        {
            throw new System.ArgumentOutOfRangeException("value");
        }

        return ChartTickFormatterOperations.FormatSiPrefix(value, 2);
    }

    /// <summary>
    /// Formats change as deterministic display text.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="decimalPlaces">The decimal places.</param>
    /// <returns>The resulting string.</returns>
    public static string FormatChange(double value, int decimalPlaces)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        ChartTickFormatterOperations.ValidateDecimalPlaces(decimalPlaces);
        if (value > 0.0)
        {
            return "+" + ChartTickFormatterOperations.FormatNumber(value, decimalPlaces);
        }

        return ChartTickFormatterOperations.FormatNumber(value, decimalPlaces);
    }

    /// <summary>
    /// Formats percent change as deterministic display text.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="decimalPlaces">The decimal places.</param>
    /// <returns>The resulting string.</returns>
    public static string FormatPercentChange(double value, int decimalPlaces)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        ChartTickFormatterOperations.ValidateDecimalPlaces(decimalPlaces);
        if (value > 0.0)
        {
            return "+" + ChartTickFormatterOperations.FormatPercent(value, decimalPlaces);
        }

        return ChartTickFormatterOperations.FormatPercent(value, decimalPlaces);
    }

    internal static ChartRenderStyle CandleStyle(double openValue, double closeValue, ChartRenderStyle upStyle, ChartRenderStyle downStyle)
    {
        ChartLinearScale.ValidateFinite(openValue, "openValue");
        ChartLinearScale.ValidateFinite(closeValue, "closeValue");
        ChartLineAreaChartsOperations.ValidateStyle(upStyle);
        ChartLineAreaChartsOperations.ValidateStyle(downStyle);
        if (closeValue >= openValue)
        {
            return upStyle;
        }

        return downStyle;
    }

    internal static void ValidateOhlcValues(double[] xValues, double[] openValues, double[] highValues, double[] lowValues, double[] closeValues, int minimumLength)
    {
        ChartCartesianTransform.ValidateValueArrays(xValues, openValues, minimumLength);
        ChartCartesianTransform.ValidateValueArrays(xValues, highValues, minimumLength);
        ChartCartesianTransform.ValidateValueArrays(xValues, lowValues, minimumLength);
        ChartCartesianTransform.ValidateValueArrays(xValues, closeValues, minimumLength);

        int index = 0;
        while (index < xValues.Length)
        {
            if (highValues[index] < openValues[index] || highValues[index] < closeValues[index] || highValues[index] < lowValues[index])
            {
                throw new System.ArgumentException("High values must be greater than or equal to open, low, and close values.");
            }

            if (lowValues[index] > openValues[index] || lowValues[index] > closeValues[index] || lowValues[index] > highValues[index])
            {
                throw new System.ArgumentException("Low values must be less than or equal to open, high, and close values.");
            }

            index = index + 1;
        }
    }

    internal static void ValidateVolumes(double[] xValues, double[] volumeValues, int minimumLength)
    {
        ChartCartesianTransform.ValidateValueArrays(xValues, volumeValues, minimumLength);
        int index = 0;
        while (index < volumeValues.Length)
        {
            if (volumeValues[index] < 0.0)
            {
                throw new System.ArgumentOutOfRangeException("volumeValues");
            }

            index = index + 1;
        }
    }

    internal static void ValidatePeriod(int period, int valueCount)
    {
        if (period <= 0)
        {
            throw new System.ArgumentOutOfRangeException("period");
        }

        if (valueCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException("valueCount");
        }

        if (period > valueCount)
        {
            throw new System.ArgumentException("Moving average period cannot exceed value count.");
        }
    }

    internal static void ValidateDates(System.DateTime[] dates)
    {
        if (dates == null)
        {
            throw new System.ArgumentNullException("dates");
        }

        if (dates.Length == 0)
        {
            throw new System.ArgumentException("Dates cannot be empty.");
        }
    }
}
