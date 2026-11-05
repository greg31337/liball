namespace chart;

internal static class ChartAccessibilityOutput
{
    internal static string StructuredText(ChartAccessibilityMetadata metadata)
    {
        if (metadata == null)
        {
            throw new System.ArgumentNullException("metadata");
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        if (metadata.Title.Length > 0)
        {
            builder.Append("Title: ");
            builder.Append(metadata.Title);
        }

        if (metadata.Description.Length > 0)
        {
            AppendTextBreak(builder);
            builder.Append("Description: ");
            builder.Append(metadata.Description);
        }

        if (metadata.SeriesCount > 0)
        {
            AppendTextBreak(builder);
            builder.Append("Series:");
            int seriesIndex = 0;
            while (seriesIndex < metadata.SeriesCount)
            {
                ChartSeriesAccessibility series = metadata.Series[seriesIndex];
                builder.Append("\n- ");
                builder.Append(series.SeriesId);
                builder.Append(": ");
                builder.Append(series.Description);
                if (series.HasNonColorCues)
                {
                    builder.Append("\n  Non-color cues: ");
                    AppendCueText(builder, series);
                }

                int pointIndex = 0;
                while (pointIndex < series.PointCount)
                {
                    ChartPointAccessibility point = series.Points[pointIndex];
                    builder.Append("\n  - ");
                    builder.Append(point.PointId);
                    builder.Append(" (row ");
                    builder.Append(point.RowIndex.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    builder.Append("): ");
                    builder.Append(point.Description);
                    pointIndex = pointIndex + 1;
                }

                seriesIndex = seriesIndex + 1;
            }
        }

        if (metadata.DataTableSidecar != null)
        {
            AppendTextBreak(builder);
            builder.Append("Data table: ");
            builder.Append(metadata.DataTableSidecar.SummaryText());
            if (metadata.DataTableSidecar.Caption.Length > 0)
            {
                builder.Append("; caption=");
                builder.Append(metadata.DataTableSidecar.Caption);
            }
        }

        if (metadata.SelectionSidecar != null)
        {
            string interaction = metadata.SelectionSidecar.MetadataText();
            if (interaction.Length > 0)
            {
                AppendTextBreak(builder);
                builder.Append("Interaction metadata:\n");
                builder.Append(interaction);
            }
        }

        return builder.ToString();
    }

    internal static string JsonAlternative(ChartAccessibilityMetadata metadata)
    {
        if (metadata == null)
        {
            throw new System.ArgumentNullException("metadata");
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append("{\"title\":");
        AppendJsonString(builder, metadata.Title);
        builder.Append(",\"description\":");
        AppendJsonString(builder, metadata.Description);
        builder.Append(",\"series\":[");
        int seriesIndex = 0;
        while (seriesIndex < metadata.SeriesCount)
        {
            if (seriesIndex > 0)
            {
                builder.Append(",");
            }

            AppendSeriesJson(builder, metadata.Series[seriesIndex]);
            seriesIndex = seriesIndex + 1;
        }

        builder.Append("],\"dataTable\":");
        if (metadata.DataTableSidecar == null)
        {
            builder.Append("null");
        }
        else
        {
            AppendTablePlanJson(builder, metadata.DataTableSidecar);
        }

        builder.Append(",\"interactionMetadata\":");
        if (metadata.SelectionSidecar == null || metadata.SelectionSidecar.MetadataText().Length == 0)
        {
            builder.Append("null");
        }
        else
        {
            AppendJsonString(builder, metadata.SelectionSidecar.MetadataText());
        }

        builder.Append("}");
        return builder.ToString();
    }

    internal static string TableCsv(ChartDataTable table)
    {
        return TableDelimited(table, ",");
    }

    internal static string TableTsv(ChartDataTable table)
    {
        return TableDelimited(table, "\t");
    }

    internal static string TableJson(ChartDataTable table)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append("{\"columns\":[");
        int columnIndex = 0;
        while (columnIndex < table.ColumnCount)
        {
            if (columnIndex > 0)
            {
                builder.Append(",");
            }

            ChartDataColumn column = table.Columns[columnIndex];
            builder.Append("{\"name\":");
            AppendJsonString(builder, column.Name);
            builder.Append(",\"kind\":");
            AppendJsonString(builder, column.Kind.ToString());
            builder.Append("}");
            columnIndex = columnIndex + 1;
        }

        builder.Append("],\"rows\":[");
        int row = 0;
        while (row < table.RowCount)
        {
            if (row > 0)
            {
                builder.Append(",");
            }

            builder.Append("[");
            columnIndex = 0;
            while (columnIndex < table.ColumnCount)
            {
                if (columnIndex > 0)
                {
                    builder.Append(",");
                }

                AppendTableJsonValue(builder, table.Columns[columnIndex], row);
                columnIndex = columnIndex + 1;
            }

            builder.Append("]");
            row = row + 1;
        }

        builder.Append("]}");
        return builder.ToString();
    }

    internal static string SeriesSemanticLabel(ChartSeriesAccessibility series)
    {
        string label = series.Description;
        if (!series.HasNonColorCues)
        {
            return label;
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append(label);
        if (builder.Length > 0)
        {
            builder.Append(" ");
        }

        builder.Append("Non-color cues: ");
        AppendCueText(builder, series);
        return builder.ToString();
    }

    internal static string PointSemanticLabel(ChartPointAccessibility point, ChartSeriesAccessibility series)
    {
        if (!series.HasNonColorCues)
        {
            return point.Description;
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append(point.Description);
        if (builder.Length > 0)
        {
            builder.Append(" ");
        }

        builder.Append("Series non-color cues: ");
        AppendCueText(builder, series);
        return builder.ToString();
    }

    internal static string CueDashText(ChartSeriesAccessibility series)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < series.CueDashArray.Length)
        {
            if (index > 0)
            {
                builder.Append(",");
            }

            builder.Append(series.CueDashArray[index].ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            index = index + 1;
        }

        return builder.ToString();
    }

    internal static void AppendTextBreak(System.Text.StringBuilder builder)
    {
        if (builder.Length > 0)
        {
            builder.Append("\n");
        }
    }

    internal static void AppendCueText(System.Text.StringBuilder builder, ChartSeriesAccessibility series)
    {
        builder.Append("shape=");
        builder.Append(series.CueShape.ToString());
        if (series.CueDashArray.Length > 0)
        {
            builder.Append("; dash=");
            builder.Append(CueDashText(series));
        }
        if (series.CueLabel.Length > 0)
        {
            builder.Append("; label=");
            builder.Append(series.CueLabel);
        }
        if (series.CuePatternDescription.Length > 0)
        {
            builder.Append("; pattern=");
            builder.Append(series.CuePatternDescription);
        }
    }

    internal static void AppendSeriesJson(System.Text.StringBuilder builder, ChartSeriesAccessibility series)
    {
        builder.Append("{\"id\":");
        AppendJsonString(builder, series.SeriesId);
        builder.Append(",\"description\":");
        AppendJsonString(builder, series.Description);
        builder.Append(",\"nonColorCues\":");
        if (!series.HasNonColorCues)
        {
            builder.Append("null");
        }
        else
        {
            AppendCueJson(builder, series);
        }

        builder.Append(",\"points\":[");
        int pointIndex = 0;
        while (pointIndex < series.PointCount)
        {
            if (pointIndex > 0)
            {
                builder.Append(",");
            }

            ChartPointAccessibility point = series.Points[pointIndex];
            builder.Append("{\"id\":");
            AppendJsonString(builder, point.PointId);
            builder.Append(",\"rowIndex\":");
            builder.Append(point.RowIndex.ToString(System.Globalization.CultureInfo.InvariantCulture));
            builder.Append(",\"description\":");
            AppendJsonString(builder, point.Description);
            builder.Append("}");
            pointIndex = pointIndex + 1;
        }

        builder.Append("]}");
    }

    internal static void AppendCueJson(System.Text.StringBuilder builder, ChartSeriesAccessibility series)
    {
        builder.Append("{\"shape\":");
        AppendJsonString(builder, series.CueShape.ToString());
        builder.Append(",\"dash\":[");
        int index = 0;
        while (index < series.CueDashArray.Length)
        {
            if (index > 0)
            {
                builder.Append(",");
            }

            builder.Append(series.CueDashArray[index].ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            index = index + 1;
        }

        builder.Append("],\"label\":");
        AppendJsonString(builder, series.CueLabel);
        builder.Append(",\"pattern\":");
        AppendJsonString(builder, series.CuePatternDescription);
        builder.Append("}");
    }

    internal static void AppendTablePlanJson(System.Text.StringBuilder builder, ChartDataTableSidecarPlan plan)
    {
        builder.Append("{\"id\":");
        AppendJsonString(builder, plan.TableId);
        builder.Append(",\"caption\":");
        AppendJsonString(builder, plan.Caption);
        builder.Append(",\"rowCount\":");
        builder.Append(plan.RowCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.Append(",\"columns\":[");
        int index = 0;
        while (index < plan.ColumnCount)
        {
            if (index > 0)
            {
                builder.Append(",");
            }

            builder.Append("{\"name\":");
            AppendJsonString(builder, plan.ColumnNames[index]);
            builder.Append(",\"kind\":");
            AppendJsonString(builder, plan.ColumnKinds[index].ToString());
            builder.Append("}");
            index = index + 1;
        }

        builder.Append("]}");
    }

    internal static string TableDelimited(ChartDataTable table, string separator)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int columnIndex = 0;
        while (columnIndex < table.ColumnCount)
        {
            if (columnIndex > 0)
            {
                builder.Append(separator);
            }

            AppendDelimitedCell(builder, table.Columns[columnIndex].Name, separator);
            columnIndex = columnIndex + 1;
        }

        int row = 0;
        while (row < table.RowCount)
        {
            builder.Append("\n");
            columnIndex = 0;
            while (columnIndex < table.ColumnCount)
            {
                if (columnIndex > 0)
                {
                    builder.Append(separator);
                }

                AppendDelimitedCell(builder, TableCellText(table.Columns[columnIndex], row), separator);
                columnIndex = columnIndex + 1;
            }

            row = row + 1;
        }

        return builder.ToString();
    }

    internal static string TableCellText(ChartDataColumn column, int row)
    {
        if (column.Kind == ChartDataKind.Number)
        {
            double number = column.NumberValues[row];
            if (double.IsNaN(number))
            {
                return "";
            }

            return number.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        }

        if (column.Kind == ChartDataKind.Text)
        {
            string? text = column.TextValues[row];
            return text == null ? "" : text;
        }

        if (column.Kind == ChartDataKind.DateTime)
        {
            return column.DateTimeValues[row].ToString("O", System.Globalization.CultureInfo.InvariantCulture);
        }

        if (column.Kind == ChartDataKind.Category)
        {
            string? category = column.CategoryValues[row];
            return category == null ? "" : category;
        }

        ChartDataMatrix? matrix = column.MatrixValue;
        if (matrix == null)
        {
            return "";
        }

        return matrix.Values[row].ToString("R", System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static void AppendDelimitedCell(System.Text.StringBuilder builder, string value, string separator)
    {
        bool quote = value.IndexOf(separator, System.StringComparison.Ordinal) >= 0 || value.IndexOf('"') >= 0 || value.IndexOf('\n') >= 0 || value.IndexOf('\r') >= 0;
        if (!quote)
        {
            builder.Append(value);
            return;
        }

        builder.Append('"');
        builder.Append(value.Replace("\"", "\"\""));
        builder.Append('"');
    }

    internal static void AppendTableJsonValue(System.Text.StringBuilder builder, ChartDataColumn column, int row)
    {
        if (column.Kind == ChartDataKind.Number)
        {
            AppendJsonDouble(builder, column.NumberValues[row]);
            return;
        }

        if (column.Kind == ChartDataKind.Text)
        {
            string? text = column.TextValues[row];
            if (text == null)
            {
                builder.Append("null");
            }
            else
            {
                AppendJsonString(builder, text);
            }
            return;
        }

        if (column.Kind == ChartDataKind.DateTime)
        {
            AppendJsonString(builder, column.DateTimeValues[row].ToString("O", System.Globalization.CultureInfo.InvariantCulture));
            return;
        }

        if (column.Kind == ChartDataKind.Category)
        {
            string? category = column.CategoryValues[row];
            if (category == null)
            {
                builder.Append("null");
            }
            else
            {
                AppendJsonString(builder, category);
            }
            return;
        }

        ChartDataMatrix? matrix = column.MatrixValue;
        if (matrix == null)
        {
            builder.Append("null");
            return;
        }

        AppendJsonDouble(builder, matrix.Values[row]);
    }

    internal static void AppendJsonDouble(System.Text.StringBuilder builder, double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            builder.Append("null");
            return;
        }

        builder.Append(value.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
    }

    internal static void AppendJsonString(System.Text.StringBuilder builder, string value)
    {
        builder.Append('"');
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '"')
            {
                builder.Append("\\\"");
            }
            else if (character == '\\')
            {
                builder.Append("\\\\");
            }
            else if (character == '\b')
            {
                builder.Append("\\b");
            }
            else if (character == '\f')
            {
                builder.Append("\\f");
            }
            else if (character == '\n')
            {
                builder.Append("\\n");
            }
            else if (character == '\r')
            {
                builder.Append("\\r");
            }
            else if (character == '\t')
            {
                builder.Append("\\t");
            }
            else if (character < 32)
            {
                builder.Append("\\u");
                builder.Append(((int)character).ToString("X4", System.Globalization.CultureInfo.InvariantCulture));
            }
            else
            {
                builder.Append(character);
            }

            index = index + 1;
        }

        builder.Append('"');
    }
}
