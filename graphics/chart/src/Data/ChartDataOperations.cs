namespace chart;

/// <summary>
/// Provides operations for data operations.
/// </summary>
internal static class ChartDataOperationsOperations
{
    /// <summary>
    /// Groups by category using the supplied data and configuration.
    /// </summary>
    /// <param name="categoryColumn">The category column.</param>
    /// <returns>The resulting data group array.</returns>
    public static ChartDataGroup[] GroupByCategory(ChartDataColumn categoryColumn)
    {
        categoryColumn.EnsureKind(ChartDataKind.Category);

        ChartDataGroup[] groups = new ChartDataGroup[4];
        int groupCount = 0;
        int index = 0;
        while (index < categoryColumn.Length)
        {
            string? key = categoryColumn.GetCategory(index);
            if (key == null)
            {
                key = "";
            }

            int groupIndex = FindGroupIndex(groups, groupCount, key);
            if (groupIndex < 0)
            {
                groups = EnsureGroupCapacity(groups, groupCount + 1);
                groups[groupCount] = new ChartDataGroup(key);
                groupIndex = groupCount;
                groupCount = groupCount + 1;
            }

            groups[groupIndex].AddIndex(index);
            index = index + 1;
        }

        ChartDataGroup[] result = new ChartDataGroup[groupCount];
        System.Array.Copy(groups, result, groupCount);
        return result;
    }

    /// <summary>
    /// Sorts indices by number using the supplied data and configuration.
    /// </summary>
    /// <param name="column">The column.</param>
    /// <param name="ascending"><c>true</c> to use ascending order; <c>false</c> for descending order.</param>
    /// <returns>The resulting int array.</returns>
    public static int[] SortIndicesByNumber(ChartDataColumn column, bool ascending)
    {
        column.EnsureKind(ChartDataKind.Number);
        ChartStatisticsOperations.ValidateFiniteValues(column.NumberValues);

        int[] indices = SequentialIndices(column.Length);
        int index = 1;
        while (index < indices.Length)
        {
            int value = indices[index];
            int scan = index - 1;
            while (scan >= 0 && CompareNumbers(column.GetNumber(indices[scan]), column.GetNumber(value), ascending) > 0)
            {
                indices[scan + 1] = indices[scan];
                scan = scan - 1;
            }

            indices[scan + 1] = value;
            index = index + 1;
        }

        return indices;
    }

    /// <summary>
    /// Sorts indices by text using the supplied data and configuration.
    /// </summary>
    /// <param name="column">The column.</param>
    /// <param name="ascending"><c>true</c> to use ascending order; <c>false</c> for descending order.</param>
    /// <returns>The resulting int array.</returns>
    /// <exception cref="System.InvalidOperationException">Thrown when the current object state cannot perform this operation.</exception>
    public static int[] SortIndicesByText(ChartDataColumn column, bool ascending)
    {
        if (column.Kind != ChartDataKind.Text && column.Kind != ChartDataKind.Category)
        {
            throw new System.InvalidOperationException("Column must contain text or category data.");
        }

        int[] indices = SequentialIndices(column.Length);
        int index = 1;
        while (index < indices.Length)
        {
            int value = indices[index];
            int scan = index - 1;
            while (scan >= 0 && CompareText(GetTextValue(column, indices[scan]), GetTextValue(column, value), ascending) > 0)
            {
                indices[scan + 1] = indices[scan];
                scan = scan - 1;
            }

            indices[scan + 1] = value;
            index = index + 1;
        }

        return indices;
    }

    /// <summary>Sorts text or category rows by an explicit stable value order.</summary>
    /// <param name="column">The text or category column.</param>
    /// <param name="customOrder">The values in their requested order.</param>
    /// <returns>The stable source indices with unlisted values placed last.</returns>
    public static int[] SortIndicesByCustomOrder(ChartDataColumn column, string? [] customOrder)
    {
        if (column.Kind != ChartDataKind.Text && column.Kind != ChartDataKind.Category)
        {
            throw new System.InvalidOperationException("Column must contain text or category data.");
        }

        ValidateCustomOrder(customOrder);
        int[] indices = SequentialIndices(column.Length);
        int index = 1;
        while (index < indices.Length)
        {
            int value = indices[index];
            int valueRank = CustomOrderRank(GetTextValue(column, value), customOrder);
            int scan = index - 1;
            while (scan >= 0 && CustomOrderRank(GetTextValue(column, indices[scan]), customOrder) > valueRank)
            {
                indices[scan + 1] = indices[scan];
                scan = scan - 1;
            }

            indices[scan + 1] = value;
            index = index + 1;
        }

        return indices;
    }

    /// <summary>
    /// Filters number range using the supplied data and configuration.
    /// </summary>
    /// <param name="column">The column.</param>
    /// <param name="minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name="maximum">The inclusive upper end of the numeric domain.</param>
    /// <returns>The resulting int array.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public static int[] FilterNumberRange(ChartDataColumn column, double minimum, double maximum)
    {
        column.EnsureKind(ChartDataKind.Number);
        if (double.IsNaN(minimum) || double.IsInfinity(minimum) || double.IsNaN(maximum) || double.IsInfinity(maximum) || maximum < minimum)
        {
            throw new System.ArgumentException("Numeric range is invalid.");
        }

        int[] indices = new int[column.Length];
        int count = 0;
        int index = 0;
        while (index < column.Length)
        {
            double value = column.GetNumber(index);
            if (value >= minimum && value <= maximum)
            {
                indices[count] = index;
                count = count + 1;
            }

            index = index + 1;
        }

        return CopyIndexBuffer(indices, count);
    }

    /// <summary>
    /// Filters category in using the supplied data and configuration.
    /// </summary>
    /// <param name="column">The column.</param>
    /// <param name="categories">The categories.</param>
    /// <returns>The resulting int array.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static int[] FilterCategoryIn(ChartDataColumn column, string?[] categories)
    {
        column.EnsureKind(ChartDataKind.Category);
        if (categories == null)
        {
            throw new System.ArgumentNullException("categories");
        }

        int[] indices = new int[column.Length];
        int count = 0;
        int index = 0;
        while (index < column.Length)
        {
            if (ChartDataColumn.ContainsString(categories, categories.Length, column.GetCategory(index)))
            {
                indices[count] = index;
                count = count + 1;
            }

            index = index + 1;
        }

        return CopyIndexBuffer(indices, count);
    }

    /// <summary>
    /// Filters text contains using the supplied data and configuration.
    /// </summary>
    /// <param name="column">The column.</param>
    /// <param name="text">The text to measure, render, or encode.</param>
    /// <returns>The resulting int array.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <exception cref="System.InvalidOperationException">Thrown when the current object state cannot perform this operation.</exception>
    public static int[] FilterTextContains(ChartDataColumn column, string text)
    {
        if (text == null)
        {
            throw new System.ArgumentNullException("text");
        }

        if (column.Kind != ChartDataKind.Text && column.Kind != ChartDataKind.Category)
        {
            throw new System.InvalidOperationException("Column must contain text or category data.");
        }

        int[] indices = new int[column.Length];
        int count = 0;
        int index = 0;
        while (index < column.Length)
        {
            string? value = GetTextValue(column, index);
            if (value != null && value.IndexOf(text, System.StringComparison.Ordinal) >= 0)
            {
                indices[count] = index;
                count = count + 1;
            }

            index = index + 1;
        }

        return CopyIndexBuffer(indices, count);
    }

    /// <summary>
    /// Filters missing categories using the supplied data and configuration.
    /// </summary>
    /// <param name="column">The column.</param>
    /// <returns>The resulting int array.</returns>
    public static int[] FilterMissingCategories(ChartDataColumn column)
    {
        column.EnsureKind(ChartDataKind.Category);

        int[] indices = new int[column.Length];
        int count = 0;
        int index = 0;
        while (index < column.Length)
        {
            string? value = column.GetCategory(index);
            if (value == null || value.Length == 0)
            {
                indices[count] = index;
                count = count + 1;
            }

            index = index + 1;
        }

        return CopyIndexBuffer(indices, count);
    }

    /// <summary>
    /// Aggregates number using the supplied data and configuration.
    /// </summary>
    /// <param name="column">The column.</param>
    /// <param name="indices">The indices.</param>
    /// <param name="aggregation">The aggregation.</param>
    /// <returns>The resulting double.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref="System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static double AggregateNumber(ChartDataColumn column, int[] indices, ChartAggregationKind aggregation)
    {
        column.EnsureKind(ChartDataKind.Number);
        if (indices == null)
        {
            throw new System.ArgumentNullException("indices");
        }

        if (aggregation == ChartAggregationKind.Count)
        {
            return indices.Length;
        }

        if (indices.Length == 0)
        {
            throw new System.ArgumentException("Cannot aggregate an empty selection.");
        }

        if (aggregation == ChartAggregationKind.First)
        {
            return column.GetNumber(indices[0]);
        }

        if (aggregation == ChartAggregationKind.Last)
        {
            return column.GetNumber(indices[indices.Length - 1]);
        }

        double[] values = CopySelectedNumbers(column, indices);
        if (aggregation == ChartAggregationKind.Sum)
        {
            return SumValues(values);
        }

        if (aggregation == ChartAggregationKind.Mean)
        {
            return ChartStatisticsOperations.Mean(values);
        }

        if (aggregation == ChartAggregationKind.Median)
        {
            return ChartStatisticsOperations.Median(values);
        }

        if (aggregation == ChartAggregationKind.Min)
        {
            return ChartStatisticsOperations.Min(values);
        }

        if (aggregation == ChartAggregationKind.Max)
        {
            return ChartStatisticsOperations.Max(values);
        }

        if (aggregation == ChartAggregationKind.Variance)
        {
            return ChartStatisticsOperations.Variance(values);
        }

        if (aggregation == ChartAggregationKind.StandardDeviation)
        {
            return ChartStatisticsOperations.StandardDeviation(values);
        }

        if (aggregation == ChartAggregationKind.Percentile)
        {
            return ChartStatisticsOperations.Percentile(values, 50.0);
        }

        throw new System.ArgumentException("Unsupported numeric aggregation.");
    }

    /// <summary>Returns an R-7 percentile of the selected numeric rows.</summary>
    /// <param name="column">The numeric column.</param>
    /// <param name="indices">The selected source indices.</param>
    /// <param name="percentile">The percentile from zero through 100.</param>
    /// <returns>The interpolated percentile.</returns>
    public static double AggregatePercentile(ChartDataColumn column, int[] indices, double percentile)
    {
        column.EnsureKind(ChartDataKind.Number);
        if (indices == null)
        {
            throw new System.ArgumentNullException("indices");
        }

        if (indices.Length == 0)
        {
            throw new System.ArgumentException("Cannot aggregate an empty selection.");
        }

        return ChartStatisticsOperations.Percentile(CopySelectedNumbers(column, indices), percentile);
    }

    /// <summary>Joins two tables by columns containing equal keys.</summary>
    /// <param name="left">The left table.</param>
    /// <param name="right">The right table.</param>
    /// <param name="leftKeyName">The left key column name.</param>
    /// <param name="rightKeyName">The right key column name.</param>
    /// <param name="kind">The rows retained by the join.</param>
    /// <returns>A new table containing the joined rows.</returns>
    public static ChartDataTable JoinTablesByKey(ChartDataTable left, ChartDataTable right, string leftKeyName, string rightKeyName, ChartJoinKind kind)
    {
        return ChartDataJoinOperations.JoinTablesByKey(left, right, leftKeyName, rightKeyName, kind);
    }

    /// <summary>Calculates and adds a numeric column to a table.</summary>
    /// <param name="table">The table to update.</param>
    /// <param name="name">The new column name.</param>
    /// <param name="calculation">The row calculation.</param>
    /// <returns>The added numeric column.</returns>
    public static ChartDataColumn AddCalculatedNumberColumn(ChartDataTable table, string name, IChartNumberCalculation calculation)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        if (calculation == null)
        {
            throw new System.ArgumentNullException("calculation");
        }

        ChartDataColumn.ValidateName(name);
        if (table.ContainsColumn(name))
        {
            throw new System.ArgumentException("A column with the same name already exists.");
        }

        double[] values = new double[table.RowCount];
        int row = 0;
        while (row < values.Length)
        {
            values[row] = calculation.Calculate(table, row);
            row = row + 1;
        }

        return table.AddNumberColumn(name, values);
    }

    /// <summary>Calculates and adds a text column to a table.</summary>
    /// <param name="table">The table to update.</param>
    /// <param name="name">The new column name.</param>
    /// <param name="calculation">The row calculation.</param>
    /// <returns>The added text column.</returns>
    public static ChartDataColumn AddCalculatedTextColumn(ChartDataTable table, string name, IChartTextCalculation calculation)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        if (calculation == null)
        {
            throw new System.ArgumentNullException("calculation");
        }

        ChartDataColumn.ValidateName(name);
        if (table.ContainsColumn(name))
        {
            throw new System.ArgumentException("A column with the same name already exists.");
        }

        string? [] values = new string? [table.RowCount];
        int row = 0;
        while (row < values.Length)
        {
            values[row] = calculation.Calculate(table, row);
            row = row + 1;
        }

        return table.AddTextColumn(name, values);
    }

    /// <summary>
    /// Pivots category number using the supplied data and configuration.
    /// </summary>
    /// <param name="rowCategories">The row categories.</param>
    /// <param name="columnCategories">The column categories.</param>
    /// <param name="values">The input values to process.</param>
    /// <param name="aggregation">The aggregation.</param>
    /// <returns>The resulting pivot table.</returns>
    public static ChartPivotTable PivotCategoryNumber(ChartDataColumn rowCategories, ChartDataColumn columnCategories, ChartDataColumn values, ChartAggregationKind aggregation)
    {
        rowCategories.EnsureKind(ChartDataKind.Category);
        columnCategories.EnsureKind(ChartDataKind.Category);
        values.EnsureKind(ChartDataKind.Number);
        ValidateSameLength(rowCategories, columnCategories);
        ValidateSameLength(rowCategories, values);

        string?[] rowKeys = rowCategories.CopyCategoryOrder();
        string?[] columnKeys = columnCategories.CopyCategoryOrder();
        ChartDataMatrix matrix = new ChartDataMatrix(rowKeys.Length, columnKeys.Length);
        int rowIndex = 0;
        while (rowIndex < rowKeys.Length)
        {
            int columnIndex = 0;
            while (columnIndex < columnKeys.Length)
            {
                int[] indices = MatchingCategoryPairIndices(rowCategories, columnCategories, rowKeys[rowIndex], columnKeys[columnIndex]);
                double value = double.NaN;
                if (indices.Length > 0)
                {
                    value = AggregateNumber(values, indices, aggregation);
                }

                matrix.SetValue(rowIndex, columnIndex, value);
                columnIndex = columnIndex + 1;
            }

            rowIndex = rowIndex + 1;
        }

        return new ChartPivotTable(rowKeys, columnKeys, matrix);
    }

    internal static int[] MatchingCategoryPairIndices(ChartDataColumn rowCategories, ChartDataColumn columnCategories, string? rowKey, string? columnKey)
    {
        int[] indices = new int[rowCategories.Length];
        int count = 0;
        int index = 0;
        while (index < rowCategories.Length)
        {
            if (rowCategories.GetCategory(index) == rowKey && columnCategories.GetCategory(index) == columnKey)
            {
                indices[count] = index;
                count = count + 1;
            }

            index = index + 1;
        }

        return CopyIndexBuffer(indices, count);
    }

    internal static void ValidateSameLength(ChartDataColumn first, ChartDataColumn second)
    {
        if (first.Length != second.Length)
        {
            throw new System.ArgumentException("Columns must have the same length.");
        }
    }

    internal static double[] CopySelectedNumbers(ChartDataColumn column, int[] indices)
    {
        double[] values = new double[indices.Length];
        int index = 0;
        while (index < indices.Length)
        {
            values[index] = column.GetNumber(indices[index]);
            index = index + 1;
        }

        return values;
    }

    internal static int[] SequentialIndices(int length)
    {
        if (length < 0)
        {
            throw new System.ArgumentOutOfRangeException("length");
        }

        int[] indices = new int[length];
        int index = 0;
        while (index < length)
        {
            indices[index] = index;
            index = index + 1;
        }

        return indices;
    }

    internal static int CompareNumbers(double left, double right, bool ascending)
    {
        int result = 0;
        if (left < right)
        {
            result = -1;
        }
        else if (left > right)
        {
            result = 1;
        }

        if (!ascending)
        {
            result = 0 - result;
        }

        return result;
    }

    internal static int CompareText(string? left, string? right, bool ascending)
    {
        int result = System.String.CompareOrdinal(left, right);
        if (!ascending)
        {
            result = 0 - result;
        }

        return result;
    }

    internal static void ValidateCustomOrder(string? [] customOrder)
    {
        if (customOrder == null)
        {
            throw new System.ArgumentNullException("customOrder");
        }

        int index = 0;
        while (index < customOrder.Length)
        {
            int earlier = 0;
            while (earlier < index)
            {
                if (customOrder[earlier] == customOrder[index])
                {
                    throw new System.ArgumentException("Custom order values must be unique.");
                }

                earlier = earlier + 1;
            }

            index = index + 1;
        }
    }

    internal static int CustomOrderRank(string? value, string? [] customOrder)
    {
        int index = 0;
        while (index < customOrder.Length)
        {
            if (customOrder[index] == value)
            {
                return index;
            }

            index = index + 1;
        }

        return customOrder.Length;
    }

    internal static string? GetTextValue(ChartDataColumn column, int index)
    {
        if (column.Kind == ChartDataKind.Category)
        {
            return column.GetCategory(index);
        }

        return column.GetText(index);
    }

    internal static double SumValues(double[] values)
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

    internal static int FindGroupIndex(ChartDataGroup[] groups, int groupCount, string key)
    {
        int index = 0;
        while (index < groupCount)
        {
            if (groups[index].Key == key)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static ChartDataGroup[] EnsureGroupCapacity(ChartDataGroup[] groups, int capacity)
    {
        if (capacity <= groups.Length)
        {
            return groups;
        }

        int newCapacity = ChartArrayUtilities.GrowthCapacity(groups.Length, capacity);

        ChartDataGroup[] copy = new ChartDataGroup[newCapacity];
        System.Array.Copy(groups, copy, groups.Length);
        return copy;
    }

    internal static int[] CopyIndexBuffer(int[] indices, int count)
    {
        int[] copy = new int[count];
        System.Array.Copy(indices, copy, count);
        return copy;
    }
}
