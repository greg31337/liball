namespace chart;

/// <summary>Applies declarative position, style, label, order, and facet channels.</summary>
internal static class ChartVisualEncodingOperations
{
    internal static double[] PositionXValues(ChartDataTable table, ChartVisualEncoding encoding)
    {
        if (encoding.HasChannel(ChartEncodingChannel.X))
        {
            return ChartDeclarativeChartsOperations.NumberValues(table, encoding, ChartEncodingChannel.X);
        }

        if (encoding.HasChannel(ChartEncodingChannel.Longitude))
        {
            return ChartDeclarativeChartsOperations.NumberValues(table, encoding, ChartEncodingChannel.Longitude);
        }

        RequirePolarChannels(encoding);
        double[] radius = ChartDeclarativeChartsOperations.NumberValues(table, encoding, ChartEncodingChannel.Radius);
        double[] angle = ChartDeclarativeChartsOperations.NumberValues(table, encoding, ChartEncodingChannel.Angle);
        double[] values = new double[radius.Length];
        int index = 0;
        while (index < values.Length)
        {
            ValidateRadius(radius[index]);
            values[index] = radius[index] * System.Math.Cos(angle[index]);
            index = index + 1;
        }

        return values;
    }

    internal static double[] PositionYValues(ChartDataTable table, ChartVisualEncoding encoding)
    {
        if (encoding.HasChannel(ChartEncodingChannel.Y))
        {
            return ChartDeclarativeChartsOperations.NumberValues(table, encoding, ChartEncodingChannel.Y);
        }

        if (encoding.HasChannel(ChartEncodingChannel.Latitude))
        {
            return ChartDeclarativeChartsOperations.NumberValues(table, encoding, ChartEncodingChannel.Latitude);
        }

        RequirePolarChannels(encoding);
        double[] radius = ChartDeclarativeChartsOperations.NumberValues(table, encoding, ChartEncodingChannel.Radius);
        double[] angle = ChartDeclarativeChartsOperations.NumberValues(table, encoding, ChartEncodingChannel.Angle);
        double[] values = new double[radius.Length];
        int index = 0;
        while (index < values.Length)
        {
            ValidateRadius(radius[index]);
            values[index] = radius[index] * System.Math.Sin(angle[index]);
            index = index + 1;
        }

        return values;
    }

    internal static ChartCartesianSeries FinishSeries(ChartCartesianSeries series, ChartDataTable table, ChartVisualEncoding encoding, ChartRenderStyle labelStyle, double[] xValues, double[] yValues)
    {
        ApplyCommandChannels(series, table, encoding);
        ApplyZIndex(series, table, encoding);
        AddValueLabels(series, table, encoding, labelStyle, xValues, yValues);
        return series;
    }

    internal static ChartCartesianSeries CreateEncodedLine(string id, ChartDataTable table, ChartVisualEncoding encoding, ChartRenderStyle style, double[] xValues, double[] yValues)
    {
        if (!RequiresSegmentLines(encoding))
        {
            ChartCartesianSeries series = ChartLineAreaChartsOperations.CreateLineSeries(id, xValues, yValues, style);
            return FinishSeries(series, table, encoding, style, xValues, yValues);
        }

        ChartCartesianSeries result = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Line);
        int index = 0;
        while (index < xValues.Length - 1)
        {
            ChartRenderStyle rowStyle = StyleForRow(style, table, encoding, index);
            double dash = 0.0;
            if (encoding.HasEffectiveChannel(ChartEncodingChannel.Dash))
            {
                dash = NumberForRow(table, encoding, ChartEncodingChannel.Dash, index);
                ChartRenderStyle.ValidatePositive(dash, "dash");
            }

            AddLineSegment(result, id, index, xValues[index], yValues[index], xValues[index + 1], yValues[index + 1], dash, rowStyle);
            index = index + 1;
        }

        ApplyZIndex(result, table, encoding);
        AddValueLabels(result, table, encoding, style, xValues, yValues);
        return result;
    }

    internal static void AddLineSegment(ChartCartesianSeries series, string id, int row, double x1, double y1, double x2, double y2, double dash, ChartRenderStyle style)
    {
        if (dash <= 0.0)
        {
            series.AddLine(id + "-segment-" + row.ToString(System.Globalization.CultureInfo.InvariantCulture), x1, y1, x2, y2, style);
            return;
        }

        double dx = x2 - x1;
        double dy = y2 - y1;
        double length = System.Math.Sqrt(dx * dx + dy * dy);
        if (length == 0.0)
        {
            return;
        }

        if (System.Math.Ceiling(length / (dash + dash)) > 100000.0)
        {
            throw new System.ArgumentException("Dash encoding would create too many retained commands.");
        }

        double position = 0.0;
        int dashIndex = 0;
        while (position < length)
        {
            double end = position + dash;
            if (end > length)
            {
                end = length;
            }

            double startAmount = position / length;
            double endAmount = end / length;
            string suffix = row.ToString(System.Globalization.CultureInfo.InvariantCulture) + "-dash-" + dashIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
            series.AddLine(id + "-" + suffix, x1 + dx * startAmount, y1 + dy * startAmount, x1 + dx * endAmount, y1 + dy * endAmount, style);
            position = position + dash + dash;
            dashIndex = dashIndex + 1;
        }
    }

    internal static void ApplyCommandChannels(ChartCartesianSeries series, ChartDataTable table, ChartVisualEncoding encoding)
    {
        int initialCount = series.CommandCount;
        int index = 0;
        while (index < initialCount)
        {
            int row = index;
            if (row >= table.RowCount)
            {
                row = table.RowCount - 1;
            }

            series.Commands[index].Style = StyleForRow(series.Commands[index].Style, table, encoding, row);
            if (series.Commands[index].Kind == ChartRenderCommandKind.Marker && encoding.Selection != null && encoding.HasConditionalEncoding(ChartEncodingChannel.Size))
            {
                double size = NumberForRow(table, encoding, ChartEncodingChannel.Size, row);
                ChartRenderStyle.ValidatePositive(size, "size");
                series.Commands[index].MarkerSize = size;
            }

            if (series.Commands[index].Kind == ChartRenderCommandKind.Marker && encoding.Selection != null && encoding.HasConditionalEncoding(ChartEncodingChannel.Shape))
            {
                series.Commands[index].MarkerShape = ShapeForRow(encoding, row);
            }

            index = index + 1;
        }
    }

    internal static ChartRenderStyle StyleForRow(ChartRenderStyle source, ChartDataTable table, ChartVisualEncoding encoding, int row)
    {
        ChartRenderStyle style = source.Copy();
        if (encoding.HasEffectiveChannel(ChartEncodingChannel.Color))
        {
            ChartColor color = ColorForRow(table, encoding, ChartEncodingChannel.Color, row);
            if (style.HasFill && !style.FillNone)
            {
                style.SetFill(color);
            }

            if (style.HasStroke && !style.StrokeNone)
            {
                style.SetStroke(color, style.StrokeWidth);
            }

            if ((!style.HasFill || style.FillNone) && (!style.HasStroke || style.StrokeNone))
            {
                style.SetFill(color);
            }
        }

        if (encoding.HasEffectiveChannel(ChartEncodingChannel.FillColor))
        {
            style.SetFill(ColorForRow(table, encoding, ChartEncodingChannel.FillColor, row));
        }

        if (encoding.HasEffectiveChannel(ChartEncodingChannel.StrokeColor))
        {
            style.SetStroke(ColorForRow(table, encoding, ChartEncodingChannel.StrokeColor, row), style.StrokeWidth);
        }

        if (encoding.HasEffectiveChannel(ChartEncodingChannel.StrokeWidth))
        {
            double width = NumberForRow(table, encoding, ChartEncodingChannel.StrokeWidth, row);
            ChartRenderStyle.ValidatePositive(width, "strokeWidth");
            style.SetStroke(style.StrokeColor, width);
        }

        if (encoding.HasEffectiveChannel(ChartEncodingChannel.Opacity))
        {
            style.SetOpacity(NumberForRow(table, encoding, ChartEncodingChannel.Opacity, row));
        }

        return style;
    }

    internal static ChartColor ColorForRow(ChartDataTable table, ChartVisualEncoding encoding, ChartEncodingChannel channel, int row)
    {
        ChartEncodingBinding binding = encoding.BindingForRow(channel, row);
        if (binding.IsConstant)
        {
            return binding.ColorValue.Copy();
        }

        ChartDataColumn column = table.GetColumn(binding.FieldName);
        if (column.Kind == ChartDataKind.Number)
        {
            double[] values = column.CopyNumberValues();
            ChartStatisticsOperations.ValidateFiniteValues(values);
            double minimum = ChartStatisticsOperations.Min(values);
            double maximum = ChartStatisticsOperations.Max(values);
            double amount = 0.5;
            if (maximum > minimum)
            {
                amount = (values[row] - minimum) / (maximum - minimum);
            }

            byte shade = ChartColor.RoundToByte(amount * 255.0);
            return new ChartColor(shade, shade, shade, 255);
        }

        string? value = TextForColumnRow(column, row);
        int rank = TextRank(column, row, value);
        return ChartPalettesOperations.Categorical().GetColor(rank);
    }

    internal static int TextRank(ChartDataColumn column, int row, string? value)
    {
        string? [] seen = new string? [row + 1];
        int count = 0;
        int index = 0;
        while (index <= row)
        {
            string? current = TextForColumnRow(column, index);
            if (!ChartDataColumn.ContainsString(seen, count, current))
            {
                seen[count] = current;
                count = count + 1;
            }

            index = index + 1;
        }

        int rank = 0;
        while (rank < count)
        {
            if (seen[rank] == value)
            {
                return rank;
            }

            rank = rank + 1;
        }

        return 0;
    }

    internal static double NumberForRow(ChartDataTable table, ChartVisualEncoding encoding, ChartEncodingChannel channel, int row)
    {
        ChartEncodingBinding binding = encoding.BindingForRow(channel, row);
        if (binding.IsConstant)
        {
            if (binding.FieldKind != ChartDataKind.Number)
            {
                throw new System.ArgumentException("Encoded channel must be numeric.");
            }

            return binding.NumberValue;
        }

        if (binding.FieldKind != ChartDataKind.Number)
        {
            throw new System.ArgumentException("Encoded field must be numeric.");
        }

        return table.GetColumn(binding.FieldName).GetNumber(row);
    }

    internal static void ApplyZIndex(ChartCartesianSeries series, ChartDataTable table, ChartVisualEncoding encoding)
    {
        if (!encoding.HasEffectiveChannel(ChartEncodingChannel.ZIndex))
        {
            return;
        }

        double value = NumberForRow(table, encoding, ChartEncodingChannel.ZIndex, 0);
        if (value < int.MinValue || value > int.MaxValue)
        {
            throw new System.ArgumentOutOfRangeException("zIndex");
        }

        series.SetOrder((int)value);
    }

    internal static void AddValueLabels(ChartCartesianSeries series, ChartDataTable table, ChartVisualEncoding encoding, ChartRenderStyle sourceStyle, double[] xValues, double[] yValues)
    {
        if (!encoding.HasEffectiveChannel(ChartEncodingChannel.ValueLabel))
        {
            return;
        }

        ChartRenderStyle labelStyle = sourceStyle.Copy();
        if (!labelStyle.HasFill || labelStyle.FillNone)
        {
            labelStyle.SetFill(ChartColor.Black());
        }

        int row = 0;
        while (row < table.RowCount)
        {
            string? text = LabelForRow(table, encoding, row);
            if (text != null && text.Length > 0)
            {
                string suffix = row.ToString(System.Globalization.CultureInfo.InvariantCulture);
                series.AddText(series.Id + "-value-label-" + suffix, text, xValues[row], yValues[row], labelStyle);
            }

            row = row + 1;
        }
    }

    internal static string? LabelForRow(ChartDataTable table, ChartVisualEncoding encoding, int row)
    {
        ChartEncodingBinding binding = encoding.BindingForRow(ChartEncodingChannel.ValueLabel, row);
        if (binding.IsConstant)
        {
            return binding.TextValue;
        }

        ChartDataColumn column = table.GetColumn(binding.FieldName);
        if (column.Kind == ChartDataKind.Number)
        {
            return column.GetNumber(row).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return TextForColumnRow(column, row);
    }

    internal static string? TextForColumnRow(ChartDataColumn column, int row)
    {
        if (column.Kind == ChartDataKind.Text)
        {
            return column.GetText(row);
        }

        if (column.Kind == ChartDataKind.Category)
        {
            return column.GetCategory(row);
        }

        throw new System.ArgumentException("Encoded text field must contain text or category data.");
    }

    internal static bool RequiresSegmentLines(ChartVisualEncoding encoding)
    {
        if (encoding.HasEffectiveChannel(ChartEncodingChannel.Dash))
        {
            return true;
        }

        return HasFieldBinding(encoding, ChartEncodingChannel.Color)
            || HasFieldBinding(encoding, ChartEncodingChannel.FillColor)
            || HasFieldBinding(encoding, ChartEncodingChannel.StrokeColor)
            || HasFieldBinding(encoding, ChartEncodingChannel.StrokeWidth)
            || HasFieldBinding(encoding, ChartEncodingChannel.Opacity);
    }

    internal static bool HasFieldBinding(ChartVisualEncoding encoding, ChartEncodingChannel channel)
    {
        if (encoding.Selection != null && encoding.HasConditionalEncoding(channel))
        {
            return true;
        }

        return encoding.HasChannel(channel) && !encoding.BindingFor(channel).IsConstant;
    }

    internal static ChartMarkerShape ShapeForRow(ChartVisualEncoding encoding, int row)
    {
        ChartEncodingBinding binding = encoding.BindingForRow(ChartEncodingChannel.Shape, row);
        if (!binding.IsConstant)
        {
            throw new System.ArgumentException("Conditional shape encoding requires a shape constant.");
        }

        ChartRenderCommand.ValidateMarkerShape(binding.ShapeValue);
        return binding.ShapeValue;
    }

    internal static void RequirePolarChannels(ChartVisualEncoding encoding)
    {
        if (!encoding.HasChannel(ChartEncodingChannel.Radius) || !encoding.HasChannel(ChartEncodingChannel.Angle))
        {
            throw new System.ArgumentException("Position encoding requires x and y, longitude and latitude, or radius and angle channels.");
        }
    }

    internal static void ValidateRadius(double radius)
    {
        ChartLinearScale.ValidateFinite(radius, "radius");
        if (radius < 0.0)
        {
            throw new System.ArgumentOutOfRangeException("radius");
        }
    }

    internal static ChartCartesianSeries[] CreateWrapFacetSeries(string idPrefix, ChartDeclarativeMark mark, ChartDataTable table, ChartVisualEncoding encoding, ChartRenderStyle style)
    {
        ChartDeclarativeChartsOperations.ValidateCommon(idPrefix, table, encoding, style);
        if (!encoding.HasChannel(ChartEncodingChannel.Wrap))
        {
            throw new System.ArgumentException("Wrap facet channel is missing.");
        }

        ChartEncodingBinding binding = encoding.BindingFor(ChartEncodingChannel.Wrap);
        if (binding.IsConstant)
        {
            throw new System.ArgumentException("Wrap facet channel must use a field.");
        }

        ChartDataColumn column = table.GetColumn(binding.FieldName);
        if (column.Kind != ChartDataKind.Text && column.Kind != ChartDataKind.Category)
        {
            throw new System.ArgumentException("Wrap facet field must contain text or category data.");
        }

        string? [] keys = new string? [table.RowCount];
        int keyCount = 0;
        int row = 0;
        while (row < table.RowCount)
        {
            string? key = TextForColumnRow(column, row);
            if (!ChartDataColumn.ContainsString(keys, keyCount, key))
            {
                keys[keyCount] = key;
                keyCount = keyCount + 1;
            }

            row = row + 1;
        }

        ChartCartesianSeries[] series = new ChartCartesianSeries[keyCount];
        int keyIndex = 0;
        while (keyIndex < keyCount)
        {
            int[] indices = IndicesForFacet(column, keys[keyIndex]);
            ChartDeclarativeData facet = ChartDeclarativeChartsOperations.PrepareSubsetData(table, encoding, indices);
            string suffix = keyIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
            series[keyIndex] = ChartDeclarativeChartsOperations.CreateSeries(idPrefix + "-wrap-" + suffix, mark, facet.Table, facet.Encoding, style);
            keyIndex = keyIndex + 1;
        }

        return series;
    }

    internal static int[] IndicesForFacet(ChartDataColumn column, string? key)
    {
        int[] buffer = new int[column.Length];
        int count = 0;
        int row = 0;
        while (row < column.Length)
        {
            if (TextForColumnRow(column, row) == key)
            {
                buffer[count] = row;
                count = count + 1;
            }

            row = row + 1;
        }

        return ChartDataOperationsOperations.CopyIndexBuffer(buffer, count);
    }

    internal static ChartDataTable CopyFacetRows(ChartDataTable table, int[] indices)
    {
        ChartDataTable facet = new ChartDataTable();
        int columnIndex = 0;
        while (columnIndex < table.ColumnCount)
        {
            ChartDataColumn column = table.Columns[columnIndex];
            if (column.Kind == ChartDataKind.Number)
            {
                facet.AddNumberColumn(column.Name, SelectFacetNumbers(column, indices));
            }
            else if (column.Kind == ChartDataKind.Text)
            {
                facet.AddTextColumn(column.Name, SelectFacetText(column, indices));
            }
            else if (column.Kind == ChartDataKind.DateTime)
            {
                facet.AddDateTimeColumn(column.Name, SelectFacetDates(column, indices));
            }
            else if (column.Kind == ChartDataKind.Category)
            {
                facet.AddCategoryColumn(column.Name, SelectFacetCategories(column, indices));
            }
            else
            {
                throw new System.InvalidOperationException("Matrix columns cannot be faceted by row.");
            }

            columnIndex = columnIndex + 1;
        }

        return facet;
    }

    internal static double[] SelectFacetNumbers(ChartDataColumn column, int[] indices)
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

    internal static string? [] SelectFacetText(ChartDataColumn column, int[] indices)
    {
        string? [] values = new string? [indices.Length];
        int index = 0;
        while (index < indices.Length)
        {
            values[index] = column.GetText(indices[index]);
            index = index + 1;
        }

        return values;
    }

    internal static System.DateTime[] SelectFacetDates(ChartDataColumn column, int[] indices)
    {
        System.DateTime[] values = new System.DateTime[indices.Length];
        int index = 0;
        while (index < indices.Length)
        {
            values[index] = column.GetDateTime(indices[index]);
            index = index + 1;
        }

        return values;
    }

    internal static string? [] SelectFacetCategories(ChartDataColumn column, int[] indices)
    {
        string? [] values = new string? [indices.Length];
        int index = 0;
        while (index < indices.Length)
        {
            values[index] = column.GetCategory(indices[index]);
            index = index + 1;
        }

        return values;
    }
}
