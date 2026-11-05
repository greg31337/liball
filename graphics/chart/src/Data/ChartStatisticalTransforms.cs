namespace chart;
internal static class ChartStatisticalTransformsOperations
{
    /// <summary>
    /// Applies the linear regression operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <returns>The resulting linear trendline.</returns>
    public static ChartLinearTrendline LinearRegression(double[] xValues, double[] yValues)
    {
        return ChartOverlayChartsOperations.FitLinearTrendline(xValues, yValues);
    }

    internal static ChartPolynomialTrendline PolynomialRegression(double[] xValues, double[] yValues, int degree)
    {
        return ChartOverlayChartsOperations.FitPolynomialTrendline(xValues, yValues, degree);
    }

    /// <summary>
    /// Applies the moving average operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "period">The period.</param>
    /// <returns>The resulting double array.</returns>
    public static double[] MovingAverage(double[] values, int period)
    {
        return ChartFinancialChartsOperations.MovingAverage(values, period);
    }

    /// <summary>
    /// Applies the exponential moving average operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "alpha">The alpha channel from 0 for transparent through 255 for opaque.</param>
    /// <returns>The resulting double array.</returns>
    public static double[] ExponentialMovingAverage(double[] values, double alpha)
    {
        ChartStatisticsOperations.ValidateFiniteValues(values);
        ValidateAlpha(alpha);
        double[] averages = new double[values.Length];
        averages[0] = values[0];
        int index = 1;
        while (index < values.Length)
        {
            averages[index] = alpha * values[index] + (1.0 - alpha) * averages[index - 1];
            index = index + 1;
        }

        return averages;
    }

    /// <summary>
    /// Applies the rolling minimum operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "window">The window.</param>
    /// <returns>The resulting double array.</returns>
    public static double[] RollingMinimum(double[] values, int window)
    {
        ChartStatisticsOperations.ValidateFiniteValues(values);
        ValidateWindow(window, values.Length);
        double[] result = new double[values.Length - window + 1];
        int output = 0;
        while (output < result.Length)
        {
            double minimum = values[output];
            int offset = 1;
            while (offset < window)
            {
                double value = values[output + offset];
                if (value < minimum)
                {
                    minimum = value;
                }

                offset = offset + 1;
            }

            result[output] = minimum;
            output = output + 1;
        }

        return result;
    }

    /// <summary>
    /// Applies the rolling maximum operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "window">The window.</param>
    /// <returns>The resulting double array.</returns>
    public static double[] RollingMaximum(double[] values, int window)
    {
        ChartStatisticsOperations.ValidateFiniteValues(values);
        ValidateWindow(window, values.Length);
        double[] result = new double[values.Length - window + 1];
        int output = 0;
        while (output < result.Length)
        {
            double maximum = values[output];
            int offset = 1;
            while (offset < window)
            {
                double value = values[output + offset];
                if (value > maximum)
                {
                    maximum = value;
                }

                offset = offset + 1;
            }

            result[output] = maximum;
            output = output + 1;
        }

        return result;
    }

    /// <summary>
    /// Applies the cumulative sum operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting double array.</returns>
    public static double[] CumulativeSum(double[] values)
    {
        ChartStatisticsOperations.ValidateFiniteValues(values);
        double[] result = new double[values.Length];
        double total = 0.0;
        int index = 0;
        while (index < values.Length)
        {
            total = total + values[index];
            result[index] = total;
            index = index + 1;
        }

        return result;
    }

    internal static double[] CalculateRolling(double[] values, int window, ChartAggregationKind aggregation, double percentile)
    {
        ChartStatisticsOperations.ValidateFiniteValues(values);
        ValidateWindow(window, values.Length);
        ValidateAggregation(aggregation, percentile);
        double[] result = new double[values.Length - window + 1];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = AggregateRange(values, index, window, aggregation, percentile);
            index = index + 1;
        }

        return result;
    }

    internal static double[] CalculateCumulative(double[] values, ChartAggregationKind aggregation, double percentile)
    {
        ChartStatisticsOperations.ValidateFiniteValues(values);
        ValidateAggregation(aggregation, percentile);
        double[] result = new double[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = AggregateRange(values, 0, index + 1, aggregation, percentile);
            index = index + 1;
        }

        return result;
    }

    internal static double AggregateRange(double[] values, int start, int count, ChartAggregationKind aggregation, double percentile)
    {
        if (aggregation == ChartAggregationKind.Count)
        {
            return count;
        }

        if (aggregation == ChartAggregationKind.First)
        {
            return values[start];
        }

        if (aggregation == ChartAggregationKind.Last)
        {
            return values[start + count - 1];
        }

        if (aggregation == ChartAggregationKind.Min)
        {
            double minimum = values[start];
            int offset = 1;
            while (offset < count)
            {
                if (values[start + offset] < minimum)
                {
                    minimum = values[start + offset];
                }

                offset = offset + 1;
            }

            return minimum;
        }

        if (aggregation == ChartAggregationKind.Max)
        {
            double maximum = values[start];
            int offset = 1;
            while (offset < count)
            {
                if (values[start + offset] > maximum)
                {
                    maximum = values[start + offset];
                }

                offset = offset + 1;
            }

            return maximum;
        }

        double sum = 0.0;
        int index = 0;
        while (index < count)
        {
            sum = sum + values[start + index];
            index = index + 1;
        }

        if (aggregation == ChartAggregationKind.Sum)
        {
            return sum;
        }

        double mean = sum / count;
        if (aggregation == ChartAggregationKind.Mean)
        {
            return mean;
        }

        if (aggregation == ChartAggregationKind.Variance || aggregation == ChartAggregationKind.StandardDeviation)
        {
            double squared = 0.0;
            index = 0;
            while (index < count)
            {
                double delta = values[start + index] - mean;
                squared = squared + delta * delta;
                index = index + 1;
            }

            double variance = squared / count;
            if (aggregation == ChartAggregationKind.StandardDeviation)
            {
                return System.Math.Sqrt(variance);
            }

            return variance;
        }

        double[] selected = new double[count];
        System.Array.Copy(values, start, selected, 0, count);
        if (aggregation == ChartAggregationKind.Median)
        {
            return ChartStatisticsOperations.Median(selected);
        }

        return ChartStatisticsOperations.Percentile(selected, percentile);
    }

    internal static void ValidateAggregation(ChartAggregationKind aggregation, double percentile)
    {
        if (aggregation != ChartAggregationKind.Count && aggregation != ChartAggregationKind.Sum && aggregation != ChartAggregationKind.Mean && aggregation != ChartAggregationKind.Median && aggregation != ChartAggregationKind.Min && aggregation != ChartAggregationKind.Max && aggregation != ChartAggregationKind.First && aggregation != ChartAggregationKind.Last && aggregation != ChartAggregationKind.Variance && aggregation != ChartAggregationKind.StandardDeviation && aggregation != ChartAggregationKind.Percentile)
        {
            throw new System.ArgumentOutOfRangeException("aggregation");
        }

        if (aggregation == ChartAggregationKind.Percentile)
        {
            ChartLinearScale.ValidateFinite(percentile, "percentile");
            if (percentile < 0.0 || percentile > 100.0)
            {
                throw new System.ArgumentOutOfRangeException("percentile");
            }
        }
    }

    /// <summary>
    /// Applies the pareto operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "labels">The display labels in the same order as their corresponding values.</param>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting pareto result.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public static ChartParetoResult Pareto(string[] labels, double[] values)
    {
        ChartCompactChartsOperations.ValidateLabels(labels, 1);
        ChartStatisticsOperations.ValidateFiniteValues(values);
        if (labels.Length != values.Length)
        {
            throw new System.ArgumentException("Label count must match value count.");
        }

        ValidateNonNegative(values);
        double total = Total(values);
        if (total <= 0.0)
        {
            throw new System.ArgumentException("Pareto values must have a positive total.");
        }

        string[] sortedLabels = ChartDataColumn.CopyStringArray(labels);
        double[] sortedValues = ChartDataMatrix.CopyDoubleArray(values);
        SortPareto(sortedLabels, sortedValues);
        double[] cumulative = new double[sortedValues.Length];
        double running = 0.0;
        int index = 0;
        while (index < sortedValues.Length)
        {
            running = running + sortedValues[index];
            cumulative[index] = running / total;
            index = index + 1;
        }

        return new ChartParetoResult(sortedLabels, sortedValues, cumulative);
    }

    /// <summary>
    /// Applies the correlation matrix operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting data matrix.</returns>
    public static ChartDataMatrix CorrelationMatrix(ChartDataMatrix values)
    {
        ValidateMatrix(values);
        ChartDataMatrix matrix = new ChartDataMatrix(values.Columns, values.Columns);
        int row = 0;
        while (row < values.Columns)
        {
            int column = 0;
            while (column < values.Columns)
            {
                double correlation = 1.0;
                if (row != column)
                {
                    correlation = ChartStatisticsOperations.Correlation(ChartLineAreaChartsOperations.ColumnValues(values, row), ChartLineAreaChartsOperations.ColumnValues(values, column));
                }

                matrix.SetValue(row, column, correlation);
                column = column + 1;
            }

            row = row + 1;
        }

        return matrix;
    }

    internal static void SortPareto(string[] labels, double[] values)
    {
        ChartCompactChartsOperations.ValidateLabels(labels, 1);
        ChartStatisticsOperations.ValidateFiniteValues(values);
        if (labels.Length != values.Length)
        {
            throw new System.ArgumentException("Label count must match value count.");
        }

        int outer = 0;
        while (outer < values.Length)
        {
            int selected = outer;
            int inner = outer + 1;
            while (inner < values.Length)
            {
                if (values[inner] > values[selected])
                {
                    selected = inner;
                }

                inner = inner + 1;
            }

            if (selected != outer)
            {
                double value = values[outer];
                values[outer] = values[selected];
                values[selected] = value;
                string label = labels[outer];
                labels[outer] = labels[selected];
                labels[selected] = label;
            }

            outer = outer + 1;
        }
    }

    /// <summary>
    /// Converts this value to tal.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The converted double.</returns>
    public static double Total(double[] values)
    {
        ChartStatisticsOperations.ValidateFiniteValues(values);
        double total = 0.0;
        int index = 0;
        while (index < values.Length)
        {
            total = total + values[index];
            index = index + 1;
        }

        return total;
    }

    internal static void ValidateAlpha(double alpha)
    {
        ChartLinearScale.ValidateFinite(alpha, "alpha");
        if (alpha <= 0.0 || alpha > 1.0)
        {
            throw new System.ArgumentOutOfRangeException("alpha");
        }
    }

    internal static void ValidateWindow(int window, int valueCount)
    {
        if (window <= 0)
        {
            throw new System.ArgumentOutOfRangeException("window");
        }

        if (valueCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException("valueCount");
        }

        if (window > valueCount)
        {
            throw new System.ArgumentException("Window cannot exceed value count.");
        }
    }

    internal static void ValidateNonNegative(double[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] < 0.0)
            {
                throw new System.ArgumentOutOfRangeException("values");
            }

            index = index + 1;
        }
    }

    internal static void ValidateMatrix(ChartDataMatrix values)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        if (values.Rows < 2 || values.Columns < 2)
        {
            throw new System.ArgumentException("Correlation matrix requires at least two rows and two columns.");
        }

        int index = 0;
        while (index < values.Values.Length)
        {
            ChartLinearScale.ValidateFinite(values.Values[index], "values");
            index = index + 1;
        }
    }
}
