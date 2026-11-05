namespace chart;

/// <summary>
/// Provides operations for stacker.
/// </summary>
internal static class ChartStackerOperations
{
    /// <summary>
    /// Stacks the configured value using the supplied data and configuration.
    /// </summary>
    /// <param name="values">The input values to process.</param>
    /// <param name="mode">The mode.</param>
    /// <returns>The resulting stack result.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public static ChartStackResult Stack(ChartDataMatrix values, ChartStackMode mode)
    {
        if (mode == ChartStackMode.None)
        {
            return StackNone(values);
        }

        if (mode == ChartStackMode.Zero)
        {
            return StackNormal(values);
        }

        if (mode == ChartStackMode.Percent)
        {
            return StackPercent(values);
        }

        if (mode == ChartStackMode.Diverging)
        {
            return StackDiverging(values);
        }

        if (mode == ChartStackMode.Streamgraph)
        {
            return StackStreamgraph(values);
        }

        throw new System.ArgumentException("Unsupported stack mode.");
    }

    /// <summary>
    /// Stacks none using the supplied data and configuration.
    /// </summary>
    /// <param name="values">The input values to process.</param>
    /// <returns>The resulting stack result.</returns>
    public static ChartStackResult StackNone(ChartDataMatrix values)
    {
        ValidateValues(values);

        ChartDataMatrix starts = new ChartDataMatrix(values.Rows, values.Columns);
        ChartDataMatrix ends = new ChartDataMatrix(values.Rows, values.Columns);
        int row = 0;
        while (row < values.Rows)
        {
            int column = 0;
            while (column < values.Columns)
            {
                starts.SetValue(row, column, 0.0);
                ends.SetValue(row, column, values.GetValue(row, column));
                column = column + 1;
            }

            row = row + 1;
        }

        return new ChartStackResult(starts, ends, ChartStackMode.None);
    }

    /// <summary>
    /// Stacks normal using the supplied data and configuration.
    /// </summary>
    /// <param name="values">The input values to process.</param>
    /// <returns>The resulting stack result.</returns>
    public static ChartStackResult StackNormal(ChartDataMatrix values)
    {
        ValidateValues(values);

        ChartDataMatrix starts = new ChartDataMatrix(values.Rows, values.Columns);
        ChartDataMatrix ends = new ChartDataMatrix(values.Rows, values.Columns);
        int row = 0;
        while (row < values.Rows)
        {
            double baseline = 0.0;
            int column = 0;
            while (column < values.Columns)
            {
                double value = values.GetValue(row, column);
                starts.SetValue(row, column, baseline);
                baseline = baseline + value;
                ends.SetValue(row, column, baseline);
                column = column + 1;
            }

            row = row + 1;
        }

        return new ChartStackResult(starts, ends, ChartStackMode.Zero);
    }

    /// <summary>
    /// Stacks percent using the supplied data and configuration.
    /// </summary>
    /// <param name="values">The input values to process.</param>
    /// <returns>The resulting stack result.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public static ChartStackResult StackPercent(ChartDataMatrix values)
    {
        ValidateValues(values);

        ChartDataMatrix starts = new ChartDataMatrix(values.Rows, values.Columns);
        ChartDataMatrix ends = new ChartDataMatrix(values.Rows, values.Columns);
        int row = 0;
        while (row < values.Rows)
        {
            double total = RowTotal(values, row);
            if (total == 0.0)
            {
                throw new System.ArgumentException("Percent stack rows must have a nonzero total.");
            }

            double baseline = 0.0;
            int column = 0;
            while (column < values.Columns)
            {
                double value = values.GetValue(row, column) / total;
                starts.SetValue(row, column, baseline);
                baseline = baseline + value;
                ends.SetValue(row, column, baseline);
                column = column + 1;
            }

            row = row + 1;
        }

        return new ChartStackResult(starts, ends, ChartStackMode.Percent);
    }

    /// <summary>
    /// Stacks diverging using the supplied data and configuration.
    /// </summary>
    /// <param name="values">The input values to process.</param>
    /// <returns>The resulting stack result.</returns>
    public static ChartStackResult StackDiverging(ChartDataMatrix values)
    {
        ValidateValues(values);

        ChartDataMatrix starts = new ChartDataMatrix(values.Rows, values.Columns);
        ChartDataMatrix ends = new ChartDataMatrix(values.Rows, values.Columns);
        int row = 0;
        while (row < values.Rows)
        {
            double positive = 0.0;
            double negative = 0.0;
            int column = 0;
            while (column < values.Columns)
            {
                double value = values.GetValue(row, column);
                if (value >= 0.0)
                {
                    starts.SetValue(row, column, positive);
                    positive = positive + value;
                    ends.SetValue(row, column, positive);
                }
                else
                {
                    starts.SetValue(row, column, negative);
                    negative = negative + value;
                    ends.SetValue(row, column, negative);
                }

                column = column + 1;
            }

            row = row + 1;
        }

        return new ChartStackResult(starts, ends, ChartStackMode.Diverging);
    }

    /// <summary>
    /// Stacks streamgraph using the supplied data and configuration.
    /// </summary>
    /// <param name="values">The input values to process.</param>
    /// <returns>The resulting stack result.</returns>
    public static ChartStackResult StackStreamgraph(ChartDataMatrix values)
    {
        ValidateValues(values);

        ChartDataMatrix starts = new ChartDataMatrix(values.Rows, values.Columns);
        ChartDataMatrix ends = new ChartDataMatrix(values.Rows, values.Columns);
        int row = 0;
        while (row < values.Rows)
        {
            double baseline = 0.0 - RowTotal(values, row) / 2.0;
            int column = 0;
            while (column < values.Columns)
            {
                double value = values.GetValue(row, column);
                starts.SetValue(row, column, baseline);
                baseline = baseline + value;
                ends.SetValue(row, column, baseline);
                column = column + 1;
            }

            row = row + 1;
        }

        return new ChartStackResult(starts, ends, ChartStackMode.Streamgraph);
    }

    internal static double RowTotal(ChartDataMatrix values, int row)
    {
        double total = 0.0;
        int column = 0;
        while (column < values.Columns)
        {
            total = total + values.GetValue(row, column);
            column = column + 1;
        }

        return total;
    }

    internal static void ValidateValues(ChartDataMatrix values)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        if (values.Rows <= 0 || values.Columns <= 0 || values.Values == null || values.Values.Length != (long)values.Rows * values.Columns)
        {
            throw new System.ArgumentException("Stack matrix dimensions are invalid.");
        }

        int index = 0;
        while (index < values.Values.Length)
        {
            if (double.IsNaN(values.Values[index]) || double.IsInfinity(values.Values[index]))
            {
                throw new System.ArgumentException("Stack values must be finite.");
            }

            index = index + 1;
        }
    }
}
