namespace chart;
internal static class ChartOverlayChartsOperations
{
    /// <summary>
    /// Creates the requested horizontal reference line from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xMinimum">The x minimum.</param>
    /// <param name = "xMaximum">The x maximum.</param>
    /// <param name = "y">The y.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateHorizontalReferenceLine(string id, double xMinimum, double xMaximum, double y, ChartRenderStyle style)
    {
        ChartPlot.ValidateId(id);
        ValidateRange(xMinimum, xMaximum, "x");
        ChartLinearScale.ValidateFinite(y, "y");
        ChartLineAreaChartsOperations.ValidateStyle(style);
        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Line);
        series.AddLine(id + "-line", xMinimum, y, xMaximum, y, style);
        return series;
    }

    /// <summary>
    /// Creates the requested vertical reference line from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "x">The x.</param>
    /// <param name = "yMinimum">The y minimum.</param>
    /// <param name = "yMaximum">The y maximum.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateVerticalReferenceLine(string id, double x, double yMinimum, double yMaximum, ChartRenderStyle style)
    {
        ChartPlot.ValidateId(id);
        ChartLinearScale.ValidateFinite(x, "x");
        ValidateRange(yMinimum, yMaximum, "y");
        ChartLineAreaChartsOperations.ValidateStyle(style);
        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Line);
        series.AddLine(id + "-line", x, yMinimum, x, yMaximum, style);
        return series;
    }

    /// <summary>
    /// Creates the requested horizontal reference band from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xMinimum">The x minimum.</param>
    /// <param name = "xMaximum">The x maximum.</param>
    /// <param name = "yMinimum">The y minimum.</param>
    /// <param name = "yMaximum">The y maximum.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateHorizontalReferenceBand(string id, double xMinimum, double xMaximum, double yMinimum, double yMaximum, ChartRenderStyle style)
    {
        ChartPlot.ValidateId(id);
        ValidateRange(xMinimum, xMaximum, "x");
        ValidateRange(yMinimum, yMaximum, "y");
        ChartLineAreaChartsOperations.ValidateStyle(style);
        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Area);
        series.AddCommand(ChartRenderCommand.CreatePolygon(id + "-band", new ChartPoint[] { new ChartPoint(xMinimum, yMinimum), new ChartPoint(xMinimum, yMaximum), new ChartPoint(xMaximum, yMaximum), new ChartPoint(xMaximum, yMinimum) }, style));
        return series;
    }

    /// <summary>
    /// Creates the requested vertical reference band from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xMinimum">The x minimum.</param>
    /// <param name = "xMaximum">The x maximum.</param>
    /// <param name = "yMinimum">The y minimum.</param>
    /// <param name = "yMaximum">The y maximum.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateVerticalReferenceBand(string id, double xMinimum, double xMaximum, double yMinimum, double yMaximum, ChartRenderStyle style)
    {
        return CreateHorizontalReferenceBand(id, xMinimum, xMaximum, yMinimum, yMaximum, style);
    }

    /// <summary>
    /// Creates the requested confidence band from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "lowerValues">The lower values in data order.</param>
    /// <param name = "upperValues">The upper values in data order.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateConfidenceBand(string id, double[] xValues, double[] lowerValues, double[] upperValues, ChartRenderStyle style)
    {
        ChartPlot.ValidateId(id);
        ChartCartesianTransform.ValidateValueArrays(xValues, lowerValues, 2);
        ChartCartesianTransform.ValidateValueArrays(xValues, upperValues, 2);
        ChartLineAreaChartsOperations.ValidateStyle(style);
        ValidateLowerUpper(lowerValues, upperValues);
        return ChartLineAreaChartsOperations.CreateAreaSeries(id, xValues, upperValues, lowerValues, style);
    }

    internal static ChartCartesianSeries CreateStandardDeviationBand(string id, double[] xValues, double[] centerValues, double[] standardDeviations, double multiplier, ChartRenderStyle style)
    {
        ChartCartesianTransform.ValidateValueArrays(xValues, centerValues, 2);
        ChartCartesianTransform.ValidateValueArrays(xValues, standardDeviations, 2);
        ChartRenderStyle.ValidatePositive(multiplier, "multiplier");
        double[] lower = new double[xValues.Length];
        double[] upper = new double[xValues.Length];
        int index = 0;
        while (index < xValues.Length)
        {
            if (standardDeviations[index] < 0.0)
            {
                throw new System.ArgumentOutOfRangeException("standardDeviations");
            }

            double amount = standardDeviations[index] * multiplier;
            lower[index] = centerValues[index] - amount;
            upper[index] = centerValues[index] + amount;
            index = index + 1;
        }

        return CreateConfidenceBand(id, xValues, lower, upper, style);
    }

    /// <summary>
    /// Creates the requested trendline series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "xMinimum">The x minimum.</param>
    /// <param name = "xMaximum">The x maximum.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateTrendlineSeries(string id, double[] xValues, double[] yValues, double xMinimum, double xMaximum, ChartRenderStyle style)
    {
        ChartPlot.ValidateId(id);
        ChartStatisticsOperations.ValidateFinitePairs(xValues, yValues);
        ValidateRange(xMinimum, xMaximum, "x");
        ChartLineAreaChartsOperations.ValidateStyle(style);
        ChartLinearTrendline trendline = FitLinearTrendline(xValues, yValues);
        return ChartLineAreaChartsOperations.CreateLineSeries(id, new double[] { xMinimum, xMaximum }, new double[] { trendline.Predict(xMinimum), trendline.Predict(xMaximum) }, style);
    }

    internal static ChartCartesianSeries CreatePolynomialTrendlineSeries(string id, double[] xValues, double[] yValues, int degree, double xMinimum, double xMaximum, int sampleCount, ChartRenderStyle style)
    {
        ValidateRange(xMinimum, xMaximum, "x");
        if (sampleCount < 2)
        {
            throw new System.ArgumentOutOfRangeException("sampleCount");
        }

        ChartLineAreaChartsOperations.ValidateStyle(style);
        ChartPolynomialTrendline trendline = FitPolynomialTrendline(xValues, yValues, degree);
        double[] fittedX = new double[sampleCount];
        double[] fittedY = new double[sampleCount];
        int index = 0;
        while (index < sampleCount)
        {
            double amount = index / (double)(sampleCount - 1);
            fittedX[index] = xMinimum + (xMaximum - xMinimum) * amount;
            fittedY[index] = trendline.Predict(fittedX[index]);
            index = index + 1;
        }

        return ChartLineAreaChartsOperations.CreateLineSeries(id, fittedX, fittedY, style);
    }

    internal static ChartCartesianSeries[] CreateControlChartSeries(string idPrefix, double[] xValues, double[] values, double sigmaMultiplier, ChartRenderStyle dataStyle, ChartRenderStyle centerStyle, ChartRenderStyle limitStyle)
    {
        ChartPlot.ValidateId(idPrefix);
        ChartCartesianTransform.ValidateValueArrays(xValues, values, 2);
        ChartRenderStyle.ValidatePositive(sigmaMultiplier, "sigmaMultiplier");
        ChartLineAreaChartsOperations.ValidateStyle(dataStyle);
        ChartLineAreaChartsOperations.ValidateStyle(centerStyle);
        ChartLineAreaChartsOperations.ValidateStyle(limitStyle);
        double mean = ChartStatisticsOperations.Mean(values);
        double deviation = ChartStatisticsOperations.PopulationStandardDeviation(values);
        double xMinimum = ChartStatisticsOperations.Min(xValues);
        double xMaximum = ChartStatisticsOperations.Max(xValues);
        ValidateRange(xMinimum, xMaximum, "x");
        ChartCartesianSeries[] series = new ChartCartesianSeries[4];
        series[0] = ChartLineAreaChartsOperations.CreateLineSeries(idPrefix + "-data", xValues, values, dataStyle);
        series[1] = CreateHorizontalReferenceLine(idPrefix + "-center", xMinimum, xMaximum, mean, centerStyle);
        series[2] = CreateHorizontalReferenceLine(idPrefix + "-lower", xMinimum, xMaximum, mean - deviation * sigmaMultiplier, limitStyle);
        series[3] = CreateHorizontalReferenceLine(idPrefix + "-upper", xMinimum, xMaximum, mean + deviation * sigmaMultiplier, limitStyle);
        int index = 0;
        while (index < series.Length)
        {
            series[index].SetOrder(index);
            index = index + 1;
        }

        return series;
    }

    /// <summary>
    /// Creates the requested mean line from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xMinimum">The x minimum.</param>
    /// <param name = "xMaximum">The x maximum.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateMeanLine(string id, double xMinimum, double xMaximum, double[] values, ChartRenderStyle style)
    {
        return CreateHorizontalReferenceLine(id, xMinimum, xMaximum, ChartStatisticsOperations.Mean(values), style);
    }

    /// <summary>
    /// Creates the requested median line from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xMinimum">The x minimum.</param>
    /// <param name = "xMaximum">The x maximum.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateMedianLine(string id, double xMinimum, double xMaximum, double[] values, ChartRenderStyle style)
    {
        return CreateHorizontalReferenceLine(id, xMinimum, xMaximum, ChartStatisticsOperations.Median(values), style);
    }

    /// <summary>
    /// Creates the requested quantile band from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xMinimum">The x minimum.</param>
    /// <param name = "xMaximum">The x maximum.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "lowerQuantile">The lower quantile.</param>
    /// <param name = "upperQuantile">The upper quantile.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public static ChartCartesianSeries CreateQuantileBand(string id, double xMinimum, double xMaximum, double[] values, double lowerQuantile, double upperQuantile, ChartRenderStyle style)
    {
        ChartStatisticsOperations.ValidateFiniteValues(values);
        ValidateQuantile(lowerQuantile, "lowerQuantile");
        ValidateQuantile(upperQuantile, "upperQuantile");
        if (lowerQuantile >= upperQuantile)
        {
            throw new System.ArgumentException("Lower quantile must be less than upper quantile.");
        }

        double lower = Quantile(values, lowerQuantile);
        double upper = Quantile(values, upperQuantile);
        return CreateHorizontalReferenceBand(id, xMinimum, xMaximum, lower, upper, style);
    }

    /// <summary>
    /// Applies the fit linear trendline operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <returns>The resulting linear trendline.</returns>
    /// <exception cref = "System.InvalidOperationException">Thrown when the current object state cannot perform this operation.</exception>
    public static ChartLinearTrendline FitLinearTrendline(double[] xValues, double[] yValues)
    {
        ChartStatisticsOperations.ValidateFinitePairs(xValues, yValues);
        double xMean = ChartStatisticsOperations.Mean(xValues);
        double yMean = ChartStatisticsOperations.Mean(yValues);
        double numerator = 0.0;
        double denominator = 0.0;
        int index = 0;
        while (index < xValues.Length)
        {
            double xDelta = xValues[index] - xMean;
            double yDelta = yValues[index] - yMean;
            numerator = numerator + xDelta * yDelta;
            denominator = denominator + xDelta * xDelta;
            index = index + 1;
        }

        if (denominator == 0.0)
        {
            throw new System.InvalidOperationException("Trendline is undefined for zero-variance x values.");
        }

        double slope = numerator / denominator;
        double intercept = yMean - slope * xMean;
        double rSquared = TrendlineRSquared(xValues, yValues, slope, intercept, yMean);
        return new ChartLinearTrendline(slope, intercept, rSquared);
    }

    internal static ChartPolynomialTrendline FitPolynomialTrendline(double[] xValues, double[] yValues, int degree)
    {
        ChartStatisticsOperations.ValidateFinitePairs(xValues, yValues);
        if (degree <= 0)
        {
            throw new System.ArgumentOutOfRangeException("degree");
        }

        if (degree >= xValues.Length)
        {
            throw new System.ArgumentException("Polynomial degree must be less than the value count.");
        }

        int size = degree + 1;
        int stride = size + 1;
        double[] augmented = new double[size * stride];
        int row = 0;
        while (row < size)
        {
            int column = 0;
            while (column < size)
            {
                double total = 0.0;
                int index = 0;
                while (index < xValues.Length)
                {
                    total = total + System.Math.Pow(xValues[index], row + column);
                    index = index + 1;
                }

                augmented[row * stride + column] = total;
                column = column + 1;
            }

            double right = 0.0;
            int valueIndex = 0;
            while (valueIndex < xValues.Length)
            {
                right = right + yValues[valueIndex] * System.Math.Pow(xValues[valueIndex], row);
                valueIndex = valueIndex + 1;
            }

            augmented[row * stride + size] = right;
            row = row + 1;
        }

        SolveAugmentedSystem(augmented, size, stride);
        double[] coefficients = new double[size];
        row = 0;
        while (row < size)
        {
            coefficients[row] = augmented[row * stride + size];
            row = row + 1;
        }

        double rSquared = PolynomialRSquared(xValues, yValues, coefficients);
        return new ChartPolynomialTrendline(coefficients, rSquared);
    }

    internal static void SolveAugmentedSystem(double[] augmented, int size, int stride)
    {
        int pivot = 0;
        while (pivot < size)
        {
            int selected = pivot;
            double selectedMagnitude = System.Math.Abs(augmented[pivot * stride + pivot]);
            int row = pivot + 1;
            while (row < size)
            {
                double magnitude = System.Math.Abs(augmented[row * stride + pivot]);
                if (magnitude > selectedMagnitude)
                {
                    selected = row;
                    selectedMagnitude = magnitude;
                }

                row = row + 1;
            }

            if (selectedMagnitude <= 0.000000000001)
            {
                throw new System.InvalidOperationException("Polynomial trendline system is singular.");
            }

            if (selected != pivot)
            {
                int column = pivot;
                while (column <= size)
                {
                    double swap = augmented[pivot * stride + column];
                    augmented[pivot * stride + column] = augmented[selected * stride + column];
                    augmented[selected * stride + column] = swap;
                    column = column + 1;
                }
            }

            double divisor = augmented[pivot * stride + pivot];
            int normalizedColumn = pivot;
            while (normalizedColumn <= size)
            {
                augmented[pivot * stride + normalizedColumn] = augmented[pivot * stride + normalizedColumn] / divisor;
                normalizedColumn = normalizedColumn + 1;
            }

            row = 0;
            while (row < size)
            {
                if (row != pivot)
                {
                    double factor = augmented[row * stride + pivot];
                    int column = pivot;
                    while (column <= size)
                    {
                        augmented[row * stride + column] = augmented[row * stride + column] - factor * augmented[pivot * stride + column];
                        column = column + 1;
                    }
                }

                row = row + 1;
            }

            pivot = pivot + 1;
        }
    }

    internal static double PolynomialRSquared(double[] xValues, double[] yValues, double[] coefficients)
    {
        ChartStatisticsOperations.ValidateFinitePairs(xValues, yValues);
        ChartStatisticsOperations.ValidateFiniteValues(coefficients);
        double mean = ChartStatisticsOperations.Mean(yValues);
        double total = 0.0;
        double residual = 0.0;
        int index = 0;
        while (index < xValues.Length)
        {
            double predicted = 0.0;
            int coefficient = coefficients.Length - 1;
            while (coefficient >= 0)
            {
                predicted = predicted * xValues[index] + coefficients[coefficient];
                coefficient = coefficient - 1;
            }

            double totalDelta = yValues[index] - mean;
            double residualDelta = yValues[index] - predicted;
            total = total + totalDelta * totalDelta;
            residual = residual + residualDelta * residualDelta;
            index = index + 1;
        }

        if (total == 0.0)
        {
            return 1.0;
        }

        return 1.0 - residual / total;
    }

    internal static string FormatLinearTrendlineEquation(ChartLinearTrendline trendline, string dependentVariable, int decimalPlaces, bool includeRSquared)
    {
        if (trendline == null)
        {
            throw new System.ArgumentNullException("trendline");
        }

        return FormatTrendlineEquation(new double[] { trendline.Intercept, trendline.Slope }, trendline.RSquared, dependentVariable, decimalPlaces, includeRSquared);
    }

    internal static string FormatPolynomialTrendlineEquation(ChartPolynomialTrendline trendline, string dependentVariable, int decimalPlaces, bool includeRSquared)
    {
        if (trendline == null)
        {
            throw new System.ArgumentNullException("trendline");
        }

        return FormatTrendlineEquation(trendline.Coefficients, trendline.RSquared, dependentVariable, decimalPlaces, includeRSquared);
    }

    internal static string FormatTrendlineEquation(double[] coefficients, double rSquared, string dependentVariable, int decimalPlaces, bool includeRSquared)
    {
        ChartStatisticsOperations.ValidateFiniteValues(coefficients);
        ChartTextBlock.ValidateText(dependentVariable, "dependentVariable");
        if (decimalPlaces < 0 || decimalPlaces > 15)
        {
            throw new System.ArgumentOutOfRangeException("decimalPlaces");
        }

        string equation = dependentVariable + " = ";
        int power = coefficients.Length - 1;
        bool wroteTerm = false;
        while (power >= 0)
        {
            double coefficient = coefficients[power];
            double rounded = System.Math.Round(coefficient, decimalPlaces);
            if (rounded != 0.0 || !wroteTerm && power == 0)
            {
                if (wroteTerm)
                {
                    if (rounded < 0.0)
                    {
                        equation = equation + " - ";
                    }
                    else
                    {
                        equation = equation + " + ";
                    }
                }
                else if (rounded < 0.0)
                {
                    equation = equation + "-";
                }

                equation = equation + FormatEquationNumber(System.Math.Abs(rounded), decimalPlaces);
                if (power > 0)
                {
                    equation = equation + "x";
                    if (power > 1)
                    {
                        equation = equation + "^" + power.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }
                }

                wroteTerm = true;
            }

            power = power - 1;
        }

        if (includeRSquared)
        {
            equation = equation + "; R^2 = " + FormatEquationNumber(System.Math.Round(rSquared, decimalPlaces), decimalPlaces);
        }

        return equation;
    }

    internal static string FormatEquationNumber(double value, int decimalPlaces)
    {
        string format = "F" + decimalPlaces.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return value.ToString(format, System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static ChartCartesianSeries CreateLinearTrendlineLabel(string id, ChartLinearTrendline trendline, string dependentVariable, int decimalPlaces, bool includeRSquared, double x, double y, ChartRenderStyle style)
    {
        return CreateTrendlineLabel(id, FormatLinearTrendlineEquation(trendline, dependentVariable, decimalPlaces, includeRSquared), x, y, style);
    }

    internal static ChartCartesianSeries CreatePolynomialTrendlineLabel(string id, ChartPolynomialTrendline trendline, string dependentVariable, int decimalPlaces, bool includeRSquared, double x, double y, ChartRenderStyle style)
    {
        return CreateTrendlineLabel(id, FormatPolynomialTrendlineEquation(trendline, dependentVariable, decimalPlaces, includeRSquared), x, y, style);
    }

    internal static ChartCartesianSeries CreateTrendlineLabel(string id, string text, double x, double y, ChartRenderStyle style)
    {
        ChartPlot.ValidateId(id);
        ChartTextBlock.ValidateText(text, "text");
        ChartLinearScale.ValidateFinite(x, "x");
        ChartLinearScale.ValidateFinite(y, "y");
        ChartLineAreaChartsOperations.ValidateStyle(style);
        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Mixed);
        series.AddText(id + "-label", text, x, y, style);
        return series;
    }

    /// <summary>
    /// Applies the trendline r squared operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "slope">The slope.</param>
    /// <param name = "intercept">The intercept.</param>
    /// <param name = "yMean">The y mean.</param>
    /// <returns>The resulting double.</returns>
    public static double TrendlineRSquared(double[] xValues, double[] yValues, double slope, double intercept, double yMean)
    {
        ChartStatisticsOperations.ValidateFinitePairs(xValues, yValues);
        ChartLinearScale.ValidateFinite(slope, "slope");
        ChartLinearScale.ValidateFinite(intercept, "intercept");
        ChartLinearScale.ValidateFinite(yMean, "yMean");
        double total = 0.0;
        double residual = 0.0;
        int index = 0;
        while (index < xValues.Length)
        {
            double fitted = slope * xValues[index] + intercept;
            double totalDelta = yValues[index] - yMean;
            double residualDelta = yValues[index] - fitted;
            total = total + totalDelta * totalDelta;
            residual = residual + residualDelta * residualDelta;
            index = index + 1;
        }

        if (total == 0.0)
        {
            return 1.0;
        }

        return 1.0 - residual / total;
    }

    /// <summary>
    /// Applies the quantile operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "quantile">The quantile.</param>
    /// <returns>The resulting double.</returns>
    public static double Quantile(double[] values, double quantile)
    {
        ChartStatisticsOperations.ValidateFiniteValues(values);
        ValidateQuantile(quantile, "quantile");
        double[] sorted = ChartDataMatrix.CopyDoubleArray(values);
        System.Array.Sort(sorted);
        return ChartBinnerOperations.Percentile(sorted, quantile);
    }

    internal static void ValidateLowerUpper(double[] lowerValues, double[] upperValues)
    {
        int index = 0;
        while (index < lowerValues.Length)
        {
            if (lowerValues[index] > upperValues[index])
            {
                throw new System.ArgumentException("Lower values must be less than or equal to upper values.");
            }

            index = index + 1;
        }
    }

    internal static void ValidateRange(double minimum, double maximum, string name)
    {
        ChartLinearScale.ValidateFinite(minimum, name + "Minimum");
        ChartLinearScale.ValidateFinite(maximum, name + "Maximum");
        if (minimum == maximum)
        {
            throw new System.ArgumentException("Reference range cannot be zero length.");
        }
    }

    internal static void ValidateQuantile(double quantile, string name)
    {
        ChartLinearScale.ValidateFinite(quantile, name);
        if (quantile < 0.0 || quantile > 1.0)
        {
            throw new System.ArgumentOutOfRangeException(name);
        }
    }
}
