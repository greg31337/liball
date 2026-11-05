namespace chart;

/// <summary>Provides validated streaming mutations for data tables.</summary>
internal static class ChartStreamingTableOperations
{
    internal static void AppendRows(ChartDataTable table, ChartDataTable rows)
    {
        ValidateStreamingSchema(table, rows);
        if (rows.RowCount > int.MaxValue - table.RowCount)
        {
            throw new System.ArgumentException("Appended table has too many rows.");
        }

        ChartDataColumn[] replacements = new ChartDataColumn[table.ColumnCount];
        int index = 0;
        while (index < table.ColumnCount)
        {
            replacements[index] = CreateAppendedColumn(table.Columns[index], rows.Columns[index]);
            index = index + 1;
        }

        ApplyStreamingColumns(table, replacements, table.RowCount + rows.RowCount);
    }

    internal static void UpdateRows(ChartDataTable table, int startRow, ChartDataTable rows)
    {
        ValidateStreamingSchema(table, rows);
        if (startRow < 0 || startRow > table.RowCount || rows.RowCount > table.RowCount - startRow)
        {
            throw new System.ArgumentOutOfRangeException("startRow");
        }

        ChartDataColumn[] replacements = new ChartDataColumn[table.ColumnCount];
        int index = 0;
        while (index < table.ColumnCount)
        {
            replacements[index] = CreateUpdatedColumn(table.Columns[index], rows.Columns[index], startRow);
            index = index + 1;
        }

        ApplyStreamingColumns(table, replacements, table.RowCount);
    }

    internal static ChartDataColumn CreateAppendedColumn(ChartDataColumn target, ChartDataColumn rows)
    {
        int length = target.Length + rows.Length;
        if (target.Kind == ChartDataKind.Number)
        {
            double[] values = new double[length];
            System.Array.Copy(target.NumberValues, values, target.Length);
            System.Array.Copy(rows.NumberValues, 0, values, target.Length, rows.Length);
            return ChartDataColumn.CreateNumber(target.Name, values);
        }

        if (target.Kind == ChartDataKind.Text)
        {
            string? [] values = new string? [length];
            System.Array.Copy(target.TextValues, values, target.Length);
            System.Array.Copy(rows.TextValues, 0, values, target.Length, rows.Length);
            return ChartDataColumn.CreateText(target.Name, values);
        }

        if (target.Kind == ChartDataKind.DateTime)
        {
            System.DateTime[] values = new System.DateTime[length];
            System.Array.Copy(target.DateTimeValues, values, target.Length);
            System.Array.Copy(rows.DateTimeValues, 0, values, target.Length, rows.Length);
            return ChartDataColumn.CreateDateTime(target.Name, values);
        }

        string? [] categories = new string? [length];
        System.Array.Copy(target.CategoryValues, categories, target.Length);
        System.Array.Copy(rows.CategoryValues, 0, categories, target.Length, rows.Length);
        return ChartDataColumn.CreateCategory(target.Name, categories);
    }

    internal static ChartDataColumn CreateUpdatedColumn(ChartDataColumn target, ChartDataColumn rows, int startRow)
    {
        ChartDataColumn replacement = target.Copy();
        if (target.Kind == ChartDataKind.Number)
        {
            System.Array.Copy(rows.NumberValues, 0, replacement.NumberValues, startRow, rows.Length);
        }
        else if (target.Kind == ChartDataKind.Text)
        {
            System.Array.Copy(rows.TextValues, 0, replacement.TextValues, startRow, rows.Length);
        }
        else if (target.Kind == ChartDataKind.DateTime)
        {
            System.Array.Copy(rows.DateTimeValues, 0, replacement.DateTimeValues, startRow, rows.Length);
        }
        else
        {
            System.Array.Copy(rows.CategoryValues, 0, replacement.CategoryValues, startRow, rows.Length);
            replacement.CategoryOrder = ChartDataColumn.BuildCategoryOrder(replacement.CategoryValues);
        }

        return replacement;
    }

    internal static void ApplyStreamingColumns(ChartDataTable table, ChartDataColumn[] replacements, int rowCount)
    {
        int index = 0;
        while (index < table.ColumnCount)
        {
            ChartDataColumn target = table.Columns[index];
            ChartDataColumn replacement = replacements[index];
            target.Length = replacement.Length;
            target.NumberValues = replacement.NumberValues;
            target.TextValues = replacement.TextValues;
            target.DateTimeValues = replacement.DateTimeValues;
            target.CategoryValues = replacement.CategoryValues;
            target.CategoryOrder = replacement.CategoryOrder;
            index = index + 1;
        }

        table.RowCount = rowCount;
    }

    internal static void ValidateStreamingSchema(ChartDataTable table, ChartDataTable rows)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        if (rows == null)
        {
            throw new System.ArgumentNullException("rows");
        }

        if (table.ColumnCount != rows.ColumnCount)
        {
            throw new System.ArgumentException("Streaming tables must have the same columns.");
        }

        int index = 0;
        while (index < table.ColumnCount)
        {
            ChartDataColumn target = table.Columns[index];
            ChartDataColumn source = rows.Columns[index];
            if (target.Name != source.Name || target.Kind != source.Kind)
            {
                throw new System.ArgumentException("Streaming tables must have the same column names, order, and kinds.");
            }

            if (target.Kind == ChartDataKind.Matrix)
            {
                throw new System.InvalidOperationException("Matrix columns do not support streaming row mutations.");
            }

            index = index + 1;
        }
    }
}
