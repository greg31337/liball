namespace chart;

/// <summary>Provides deterministic table join operations.</summary>
internal static class ChartDataJoinOperations
{
    /// <summary>Joins two tables by columns containing equal keys.</summary>
    /// <param name="left">The left table.</param>
    /// <param name="right">The right table.</param>
    /// <param name="leftKeyName">The left key column name.</param>
    /// <param name="rightKeyName">The right key column name.</param>
    /// <param name="kind">The rows retained by the join.</param>
    /// <returns>A new table containing the joined rows.</returns>
    public static ChartDataTable JoinTablesByKey(ChartDataTable left, ChartDataTable right, string leftKeyName, string rightKeyName, ChartJoinKind kind)
    {
        ValidateJoinTables(left, right, kind);
        ChartDataColumn leftKey = left.GetColumn(leftKeyName);
        ChartDataColumn rightKey = right.GetColumn(rightKeyName);
        ValidateJoinKeys(leftKey, rightKey);

        if (kind == ChartJoinKind.Right)
        {
            return BuildRightJoin(left, right, leftKey, rightKey);
        }

        return BuildLeftOrderedJoin(left, right, leftKey, rightKey, kind);
    }

    internal static ChartDataTable BuildLeftOrderedJoin(ChartDataTable left, ChartDataTable right, ChartDataColumn leftKey, ChartDataColumn rightKey, ChartJoinKind kind)
    {
        int rowCount = CountLeftOrderedRows(leftKey, rightKey, kind);
        int[] leftRows = new int[rowCount];
        int[] rightRows = new int[rowCount];
        int position = 0;
        int leftRow = 0;
        while (leftRow < left.RowCount)
        {
            bool matched = false;
            int rightRow = 0;
            while (rightRow < right.RowCount)
            {
                if (JoinKeysEqual(leftKey, leftRow, rightKey, rightRow))
                {
                    leftRows[position] = leftRow;
                    rightRows[position] = rightRow;
                    position = position + 1;
                    matched = true;
                }

                rightRow = rightRow + 1;
            }

            if (!matched && (kind == ChartJoinKind.Left || kind == ChartJoinKind.Full))
            {
                leftRows[position] = leftRow;
                rightRows[position] = -1;
                position = position + 1;
            }

            leftRow = leftRow + 1;
        }

        if (kind == ChartJoinKind.Full)
        {
            int rightRow = 0;
            while (rightRow < right.RowCount)
            {
                if (!RightRowHasMatch(leftKey, rightKey, rightRow))
                {
                    leftRows[position] = -1;
                    rightRows[position] = rightRow;
                    position = position + 1;
                }

                rightRow = rightRow + 1;
            }
        }

        return CreateJoinedTable(left, right, leftRows, rightRows);
    }

    internal static ChartDataTable BuildRightJoin(ChartDataTable left, ChartDataTable right, ChartDataColumn leftKey, ChartDataColumn rightKey)
    {
        int rowCount = CountRightRows(leftKey, rightKey);
        int[] leftRows = new int[rowCount];
        int[] rightRows = new int[rowCount];
        int position = 0;
        int rightRow = 0;
        while (rightRow < right.RowCount)
        {
            bool matched = false;
            int leftRow = 0;
            while (leftRow < left.RowCount)
            {
                if (JoinKeysEqual(leftKey, leftRow, rightKey, rightRow))
                {
                    leftRows[position] = leftRow;
                    rightRows[position] = rightRow;
                    position = position + 1;
                    matched = true;
                }

                leftRow = leftRow + 1;
            }

            if (!matched)
            {
                leftRows[position] = -1;
                rightRows[position] = rightRow;
                position = position + 1;
            }

            rightRow = rightRow + 1;
        }

        return CreateJoinedTable(left, right, leftRows, rightRows);
    }

    internal static int CountLeftOrderedRows(ChartDataColumn leftKey, ChartDataColumn rightKey, ChartJoinKind kind)
    {
        long count = 0;
        int leftRow = 0;
        while (leftRow < leftKey.Length)
        {
            int matches = 0;
            int rightRow = 0;
            while (rightRow < rightKey.Length)
            {
                if (JoinKeysEqual(leftKey, leftRow, rightKey, rightRow))
                {
                    matches = matches + 1;
                }

                rightRow = rightRow + 1;
            }

            if (matches > 0)
            {
                count = count + matches;
            }
            else if (kind == ChartJoinKind.Left || kind == ChartJoinKind.Full)
            {
                count = count + 1;
            }

            leftRow = leftRow + 1;
        }

        if (kind == ChartJoinKind.Full)
        {
            int rightRow = 0;
            while (rightRow < rightKey.Length)
            {
                if (!RightRowHasMatch(leftKey, rightKey, rightRow))
                {
                    count = count + 1;
                }

                rightRow = rightRow + 1;
            }
        }

        return ValidateJoinRowCount(count);
    }

    internal static int CountRightRows(ChartDataColumn leftKey, ChartDataColumn rightKey)
    {
        long count = 0;
        int rightRow = 0;
        while (rightRow < rightKey.Length)
        {
            int matches = 0;
            int leftRow = 0;
            while (leftRow < leftKey.Length)
            {
                if (JoinKeysEqual(leftKey, leftRow, rightKey, rightRow))
                {
                    matches = matches + 1;
                }

                leftRow = leftRow + 1;
            }

            if (matches > 0)
            {
                count = count + matches;
            }
            else
            {
                count = count + 1;
            }

            rightRow = rightRow + 1;
        }

        return ValidateJoinRowCount(count);
    }

    internal static bool RightRowHasMatch(ChartDataColumn leftKey, ChartDataColumn rightKey, int rightRow)
    {
        int leftRow = 0;
        while (leftRow < leftKey.Length)
        {
            if (JoinKeysEqual(leftKey, leftRow, rightKey, rightRow))
            {
                return true;
            }

            leftRow = leftRow + 1;
        }

        return false;
    }

    internal static bool JoinKeysEqual(ChartDataColumn leftKey, int leftRow, ChartDataColumn rightKey, int rightRow)
    {
        if (leftKey.Kind == ChartDataKind.Number)
        {
            double leftValue = leftKey.GetNumber(leftRow);
            double rightValue = rightKey.GetNumber(rightRow);
            return !double.IsNaN(leftValue) && !double.IsNaN(rightValue) && leftValue == rightValue;
        }

        if (leftKey.Kind == ChartDataKind.DateTime)
        {
            return leftKey.GetDateTime(leftRow) == rightKey.GetDateTime(rightRow);
        }

        string? leftText = GetJoinText(leftKey, leftRow);
        string? rightText = GetJoinText(rightKey, rightRow);
        return leftText != null && rightText != null && leftText == rightText;
    }

    internal static string? GetJoinText(ChartDataColumn key, int row)
    {
        if (key.Kind == ChartDataKind.Text)
        {
            return key.GetText(row);
        }

        return key.GetCategory(row);
    }

    internal static ChartDataTable CreateJoinedTable(ChartDataTable left, ChartDataTable right, int[] leftRows, int[] rightRows)
    {
        ChartDataTable result = new ChartDataTable();
        AddJoinColumns(result, left, leftRows, false);
        AddJoinColumns(result, right, rightRows, true);
        return result;
    }

    internal static void AddJoinColumns(ChartDataTable result, ChartDataTable source, int[] sourceRows, bool renameCollisions)
    {
        int columnIndex = 0;
        while (columnIndex < source.ColumnCount)
        {
            ChartDataColumn column = source.Columns[columnIndex];
            string name = JoinColumnName(result, column.Name, renameCollisions);
            if (column.Kind == ChartDataKind.Number)
            {
                result.AddNumberColumn(name, SelectJoinNumbers(column, sourceRows));
            }
            else if (column.Kind == ChartDataKind.Text)
            {
                result.AddTextColumn(name, SelectJoinTexts(column, sourceRows));
            }
            else if (column.Kind == ChartDataKind.DateTime)
            {
                result.AddDateTimeColumn(name, SelectJoinDates(column, sourceRows));
            }
            else if (column.Kind == ChartDataKind.Category)
            {
                result.AddCategoryColumn(name, SelectJoinCategories(column, sourceRows));
            }
            else
            {
                throw new System.InvalidOperationException("Matrix columns cannot be joined.");
            }

            columnIndex = columnIndex + 1;
        }
    }

    internal static string JoinColumnName(ChartDataTable result, string sourceName, bool renameCollisions)
    {
        string name = sourceName;
        if (renameCollisions)
        {
            while (result.ContainsColumn(name))
            {
                name = "right." + name;
            }
        }

        return name;
    }

    internal static double[] SelectJoinNumbers(ChartDataColumn column, int[] rows)
    {
        double[] values = new double[rows.Length];
        int index = 0;
        while (index < rows.Length)
        {
            values[index] = double.NaN;
            if (rows[index] >= 0)
            {
                values[index] = column.GetNumber(rows[index]);
            }

            index = index + 1;
        }

        return values;
    }

    internal static string? [] SelectJoinTexts(ChartDataColumn column, int[] rows)
    {
        string? [] values = new string? [rows.Length];
        int index = 0;
        while (index < rows.Length)
        {
            if (rows[index] >= 0)
            {
                values[index] = column.GetText(rows[index]);
            }

            index = index + 1;
        }

        return values;
    }

    internal static System.DateTime[] SelectJoinDates(ChartDataColumn column, int[] rows)
    {
        System.DateTime[] values = new System.DateTime[rows.Length];
        int index = 0;
        while (index < rows.Length)
        {
            if (rows[index] >= 0)
            {
                values[index] = column.GetDateTime(rows[index]);
            }

            index = index + 1;
        }

        return values;
    }

    internal static string? [] SelectJoinCategories(ChartDataColumn column, int[] rows)
    {
        string? [] values = new string? [rows.Length];
        int index = 0;
        while (index < rows.Length)
        {
            if (rows[index] >= 0)
            {
                values[index] = column.GetCategory(rows[index]);
            }

            index = index + 1;
        }

        return values;
    }

    internal static void ValidateJoinTables(ChartDataTable left, ChartDataTable right, ChartJoinKind kind)
    {
        if (left == null)
        {
            throw new System.ArgumentNullException("left");
        }

        if (right == null)
        {
            throw new System.ArgumentNullException("right");
        }

        if (kind != ChartJoinKind.Inner && kind != ChartJoinKind.Left && kind != ChartJoinKind.Right && kind != ChartJoinKind.Full)
        {
            throw new System.ArgumentException("Join kind is not supported.");
        }

        ValidateJoinColumns(left);
        ValidateJoinColumns(right);
    }

    internal static void ValidateJoinColumns(ChartDataTable table)
    {
        int index = 0;
        while (index < table.ColumnCount)
        {
            if (table.Columns[index].Kind == ChartDataKind.Matrix)
            {
                throw new System.InvalidOperationException("Matrix columns cannot be joined.");
            }

            index = index + 1;
        }
    }

    internal static void ValidateJoinKeys(ChartDataColumn leftKey, ChartDataColumn rightKey)
    {
        if (leftKey.Kind != rightKey.Kind)
        {
            throw new System.ArgumentException("Join key columns must have the same data kind.");
        }

        if (leftKey.Kind == ChartDataKind.Matrix)
        {
            throw new System.InvalidOperationException("Matrix columns cannot be join keys.");
        }
    }

    internal static int ValidateJoinRowCount(long count)
    {
        if (count > int.MaxValue)
        {
            throw new System.ArgumentException("Join result has too many rows.");
        }

        return (int)count;
    }
}
