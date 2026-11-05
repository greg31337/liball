namespace chart;

internal static class ChartDeclarativeViewOperations
{
    internal static ChartDeclarativeView CreateView(ChartDeclarativeLayer[] layers)
    {
        if (layers == null)
        {
            throw new System.ArgumentNullException("layers");
        }

        if (layers.Length == 0)
        {
            throw new System.ArgumentException("A declarative view requires at least one layer.");
        }

        int capacity = 0;
        int layerIndex = 0;
        while (layerIndex < layers.Length)
        {
            ValidateLayer(layers[layerIndex]);
            capacity = capacity + layers[layerIndex].Table.RowCount;
            layerIndex = layerIndex + 1;
        }

        ChartDeclarativeViewBuilder builder = new ChartDeclarativeViewBuilder(capacity);
        layerIndex = 0;
        while (layerIndex < layers.Length)
        {
            AddLayerGuides(layers[layerIndex], layers.Length, builder);
            ProcessLayer(layers[layerIndex], layerIndex, builder);
            layerIndex = layerIndex + 1;
        }

        return builder.Build();
    }

    internal static void ProcessLayer(ChartDeclarativeLayer layer, int layerIndex, ChartDeclarativeViewBuilder builder)
    {
        string[] rowKeys = new string[layer.Table.RowCount];
        string[] columnKeys = new string[layer.Table.RowCount];
        string[] wrapKeys = new string[layer.Table.RowCount];
        int facetCount = 0;
        int row = 0;
        while (row < layer.Table.RowCount)
        {
            string rowKey = FacetValue(layer.Table, layer.Encoding, ChartEncodingChannel.Row, row);
            string columnKey = FacetValue(layer.Table, layer.Encoding, ChartEncodingChannel.Column, row);
            string wrapKey = FacetValue(layer.Table, layer.Encoding, ChartEncodingChannel.Wrap, row);
            int facetIndex = FacetIndex(rowKeys, columnKeys, wrapKeys, facetCount, rowKey, columnKey, wrapKey);
            if (facetIndex < 0)
            {
                rowKeys[facetCount] = rowKey;
                columnKeys[facetCount] = columnKey;
                wrapKeys[facetCount] = wrapKey;
                facetCount = facetCount + 1;
            }

            row = row + 1;
        }

        int facet = 0;
        while (facet < facetCount)
        {
            int[] indices = FacetIndices(layer.Table, layer.Encoding, rowKeys[facet], columnKeys[facet], wrapKeys[facet]);
            ChartDeclarativeData data = ChartDeclarativeChartsOperations.PrepareSubsetData(layer.Table, layer.Encoding, indices);
            ProcessFacet(layer, layerIndex, facet, facetCount, rowKeys[facet], columnKeys[facet], wrapKeys[facet], data, builder);
            facet = facet + 1;
        }
    }

    internal static void ProcessFacet(ChartDeclarativeLayer layer, int layerIndex, int facetIndex, int facetCount, string rowKey, string columnKey, string wrapKey, ChartDeclarativeData facet, ChartDeclarativeViewBuilder builder)
    {
        if (!facet.Encoding.HasChannel(ChartEncodingChannel.Detail))
        {
            if (layer.StackMode != ChartStackMode.None)
            {
                throw new System.ArgumentException("Stacked declarative layers require a detail field.");
            }

            string id = SeriesId(layer.Id, facetIndex, facetCount, 0, 1);
            ChartCartesianSeries series = ChartDeclarativeChartsOperations.CreateSeries(id, layer.Mark, facet.Table, facet.Encoding, layer.Style);
            builder.AppendSeries(series, layerIndex, rowKey, columnKey, wrapKey);
            return;
        }

        ChartEncodingBinding detail = facet.Encoding.BindingFor(ChartEncodingChannel.Detail);
        if (detail.IsConstant)
        {
            throw new System.ArgumentException("Detail grouping requires a text or category field.");
        }

        ChartDataColumn column = facet.Table.GetColumn(detail.FieldName);
        ValidateGroupingColumn(column);
        string[] keys = UniqueColumnKeys(column);
        ChartDeclarativeData[] groups = new ChartDeclarativeData[keys.Length];
        int group = 0;
        while (group < keys.Length)
        {
            int[] indices = GroupIndices(column, keys[group]);
            groups[group] = ChartDeclarativeChartsOperations.PrepareSubsetData(facet.Table, facet.Encoding, indices);
            group = group + 1;
        }

        if (layer.StackMode != ChartStackMode.None)
        {
            CreateStackedGroups(layer, layerIndex, facetIndex, facetCount, rowKey, columnKey, wrapKey, keys, groups, builder);
            return;
        }

        group = 0;
        while (group < groups.Length)
        {
            string id = SeriesId(layer.Id, facetIndex, facetCount, group, groups.Length);
            ChartRenderStyle style = StyleForGroup(layer.Style, keys[group], builder);
            ChartCartesianSeries series = ChartDeclarativeChartsOperations.CreateSeries(id, layer.Mark, groups[group].Table, groups[group].Encoding, style);
            builder.AppendSeries(series, layerIndex, rowKey, columnKey, wrapKey);
            group = group + 1;
        }
    }

    internal static void CreateStackedGroups(ChartDeclarativeLayer layer, int layerIndex, int facetIndex, int facetCount, string rowKey, string columnKey, string wrapKey, string[] keys, ChartDeclarativeData[] groups, ChartDeclarativeViewBuilder builder)
    {
        if (layer.Mark != ChartDeclarativeMark.Area && layer.Mark != ChartDeclarativeMark.VerticalBar && layer.Mark != ChartDeclarativeMark.HorizontalBar)
        {
            throw new System.ArgumentException("Stacking is supported for area, vertical bar, and horizontal bar marks.");
        }

        int group = 0;
        while (group < groups.Length)
        {
            groups[group] = ChartDeclarativeChartsOperations.PrepareOrderedData(groups[group].Table, groups[group].Encoding);
            group = group + 1;
        }

        int rowCount = groups[0].Table.RowCount;
        ChartDataMatrix values = new ChartDataMatrix(rowCount, groups.Length);
        double[][] positions = new double[groups.Length][];
        group = 0;
        while (group < groups.Length)
        {
            if (groups[group].Table.RowCount != rowCount)
            {
                throw new System.ArgumentException("Stacked detail groups must contain matching row counts.");
            }

            double[] groupValues;
            if (layer.Mark == ChartDeclarativeMark.HorizontalBar)
            {
                groupValues = ChartDeclarativeChartsOperations.NumberValues(groups[group].Table, groups[group].Encoding, ChartEncodingChannel.X);
                positions[group] = ChartDeclarativeChartsOperations.NumberValues(groups[group].Table, groups[group].Encoding, ChartEncodingChannel.Y);
            }
            else
            {
                groupValues = ChartDeclarativeChartsOperations.NumberValues(groups[group].Table, groups[group].Encoding, ChartEncodingChannel.Y);
                positions[group] = ChartDeclarativeChartsOperations.NumberValues(groups[group].Table, groups[group].Encoding, ChartEncodingChannel.X);
            }

            int row = 0;
            while (row < rowCount)
            {
                if (group > 0 && positions[group][row] != positions[0][row])
                {
                    throw new System.ArgumentException("Stacked detail groups must use matching ordered positions.");
                }

                values.SetValue(row, group, groupValues[row]);
                row = row + 1;
            }

            group = group + 1;
        }

        ChartStackResult stack = ChartStackerOperations.Stack(values, layer.StackMode);
        group = 0;
        while (group < groups.Length)
        {
            string id = SeriesId(layer.Id, facetIndex, facetCount, group, groups.Length);
            ChartRenderStyle style = StyleForGroup(layer.Style, keys[group], builder);
            ChartCartesianSeries series = CreateStackedSeries(id, layer.Mark, groups[group], positions[group], stack, group, style);
            builder.AppendSeries(series, layerIndex, rowKey, columnKey, wrapKey);
            group = group + 1;
        }
    }

    internal static ChartCartesianSeries CreateStackedSeries(string id, ChartDeclarativeMark mark, ChartDeclarativeData group, double[] positions, ChartStackResult stack, int stackColumn, ChartRenderStyle style)
    {
        double[] starts = new double[positions.Length];
        double[] ends = new double[positions.Length];
        int row = 0;
        while (row < positions.Length)
        {
            starts[row] = stack.GetStart(row, stackColumn);
            ends[row] = stack.GetEnd(row, stackColumn);
            row = row + 1;
        }

        if (mark == ChartDeclarativeMark.Area)
        {
            ChartCartesianSeries area = ChartLineAreaChartsOperations.CreateAreaSeries(id, positions, ends, starts, style);
            return ChartVisualEncodingOperations.FinishSeries(area, group.Table, group.Encoding, style, positions, ends);
        }

        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Bar);
        double size = ChartDeclarativeChartsOperations.NumberConstantOrDefault(group.Encoding, ChartEncodingChannel.Size, 0.8);
        row = 0;
        while (row < positions.Length)
        {
            ChartRenderStyle rowStyle = ChartVisualEncodingOperations.StyleForRow(style, group.Table, group.Encoding, row);
            string suffix = row.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (mark == ChartDeclarativeMark.HorizontalBar)
            {
                ChartBarChartsOperations.AddHorizontalBar(series, id + "-bar-" + suffix, starts[row], ends[row], positions[row], size, rowStyle);
            }
            else
            {
                ChartBarChartsOperations.AddVerticalBar(series, id + "-bar-" + suffix, positions[row], starts[row], ends[row], size, rowStyle);
            }

            row = row + 1;
        }

        ChartVisualEncodingOperations.ApplyZIndex(series, group.Table, group.Encoding);
        double[] x = positions;
        double[] y = ends;
        if (mark == ChartDeclarativeMark.HorizontalBar)
        {
            x = ends;
            y = positions;
        }

        ChartVisualEncodingOperations.AddValueLabels(series, group.Table, group.Encoding, style, x, y);
        return series;
    }

    internal static void AddLayerGuides(ChartDeclarativeLayer layer, int layerCount, ChartDeclarativeViewBuilder builder)
    {
        ChartEncodingChannel channel = ChartEncodingChannel.Detail;
        bool hasGuide = layer.Encoding.HasChannel(channel);
        if (!hasGuide)
        {
            channel = ChartEncodingChannel.Color;
            hasGuide = layer.Encoding.HasChannel(channel);
        }

        if (hasGuide)
        {
            ChartEncodingBinding binding = layer.Encoding.BindingFor(channel);
            if (!binding.IsConstant)
            {
                ChartDataColumn column = layer.Table.GetColumn(binding.FieldName);
                if (column.Kind == ChartDataKind.Text || column.Kind == ChartDataKind.Category)
                {
                    string[] keys = UniqueColumnKeys(column);
                    int index = 0;
                    while (index < keys.Length)
                    {
                        builder.AddGuide(keys[index]);
                        index = index + 1;
                    }

                    return;
                }
            }
        }

        if (layerCount > 1)
        {
            builder.AddGuide(layer.Id);
        }
    }

    internal static ChartRenderStyle StyleForGroup(ChartRenderStyle source, string key, ChartDeclarativeViewBuilder builder)
    {
        ChartRenderStyle style = source.Copy();
        ChartColor? color = builder.GuideColor(key);
        if (color == null)
        {
            return style;
        }

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

        return style;
    }

    internal static string FacetValue(ChartDataTable table, ChartVisualEncoding encoding, ChartEncodingChannel channel, int row)
    {
        if (!encoding.HasChannel(channel))
        {
            return "";
        }

        ChartEncodingBinding binding = encoding.BindingFor(channel);
        if (binding.IsConstant)
        {
            throw new System.ArgumentException("Facet channels require text or category fields.");
        }

        ChartDataColumn column = table.GetColumn(binding.FieldName);
        ValidateGroupingColumn(column);
        string? value = ChartVisualEncodingOperations.TextForColumnRow(column, row);
        if (value == null)
        {
            return "";
        }

        return value;
    }

    internal static int FacetIndex(string[] rows, string[] columns, string[] wraps, int count, string row, string column, string wrap)
    {
        int index = 0;
        while (index < count)
        {
            if (rows[index] == row && columns[index] == column && wraps[index] == wrap)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static int[] FacetIndices(ChartDataTable table, ChartVisualEncoding encoding, string rowKey, string columnKey, string wrapKey)
    {
        int[] buffer = new int[table.RowCount];
        int count = 0;
        int row = 0;
        while (row < table.RowCount)
        {
            if (FacetValue(table, encoding, ChartEncodingChannel.Row, row) == rowKey && FacetValue(table, encoding, ChartEncodingChannel.Column, row) == columnKey && FacetValue(table, encoding, ChartEncodingChannel.Wrap, row) == wrapKey)
            {
                buffer[count] = row;
                count = count + 1;
            }

            row = row + 1;
        }

        return ChartDataOperationsOperations.CopyIndexBuffer(buffer, count);
    }

    internal static string[] UniqueColumnKeys(ChartDataColumn column)
    {
        ValidateGroupingColumn(column);
        string[] buffer = new string[column.Length];
        int count = 0;
        int row = 0;
        while (row < column.Length)
        {
            string? value = ChartVisualEncodingOperations.TextForColumnRow(column, row);
            string key = "";
            if (value != null)
            {
                key = value;
            }

            if (!ContainsKey(buffer, count, key))
            {
                buffer[count] = key;
                count = count + 1;
            }

            row = row + 1;
        }

        string[] result = new string[count];
        System.Array.Copy(buffer, result, count);
        return result;
    }

    internal static int[] GroupIndices(ChartDataColumn column, string key)
    {
        int[] buffer = new int[column.Length];
        int count = 0;
        int row = 0;
        while (row < column.Length)
        {
            string? value = ChartVisualEncodingOperations.TextForColumnRow(column, row);
            string current = "";
            if (value != null)
            {
                current = value;
            }

            if (current == key)
            {
                buffer[count] = row;
                count = count + 1;
            }

            row = row + 1;
        }

        return ChartDataOperationsOperations.CopyIndexBuffer(buffer, count);
    }

    internal static bool ContainsKey(string[] values, int count, string value)
    {
        int index = 0;
        while (index < count)
        {
            if (values[index] == value)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static string SeriesId(string prefix, int facetIndex, int facetCount, int groupIndex, int groupCount)
    {
        string id = prefix;
        if (facetCount > 1)
        {
            id = id + "-facet-" + facetIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (groupCount > 1)
        {
            id = id + "-group-" + groupIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return id;
    }

    internal static void ValidateGroupingColumn(ChartDataColumn column)
    {
        if (column.Kind != ChartDataKind.Text && column.Kind != ChartDataKind.Category)
        {
            throw new System.ArgumentException("Grouping and facet fields must contain text or category data.");
        }
    }

    internal static void ValidateLayer(ChartDeclarativeLayer layer)
    {
        if (layer == null)
        {
            throw new System.ArgumentException("Declarative layers cannot contain null entries.");
        }

        ChartPlot.ValidateId(layer.Id);
        ChartDeclarativeChartsOperations.ValidateMark(layer.Mark);
        ChartDeclarativeChartsOperations.ValidateCommon(layer.Id, layer.Table, layer.Encoding, layer.Style);
        int mode = (int)layer.StackMode;
        if (mode < 0 || mode > (int)ChartStackMode.Streamgraph)
        {
            throw new System.ArgumentOutOfRangeException("StackMode");
        }
    }
}

internal class ChartDeclarativeViewBuilder
{
    internal ChartCartesianSeries[] Series;
    internal int[] LayerIndices;
    internal string[] RowKeys;
    internal string[] ColumnKeys;
    internal string[] WrapKeys;
    internal int SeriesCount;
    internal string[] GuideLabels;
    internal ChartColor[] GuideColors;
    internal int GuideCount;

    internal ChartDeclarativeViewBuilder(int capacity)
    {
        Series = new ChartCartesianSeries[capacity];
        LayerIndices = new int[capacity];
        RowKeys = new string[capacity];
        ColumnKeys = new string[capacity];
        WrapKeys = new string[capacity];
        SeriesCount = 0;
        GuideLabels = new string[capacity];
        GuideColors = new ChartColor[capacity];
        GuideCount = 0;
    }

    internal void AppendSeries(ChartCartesianSeries series, int layerIndex, string rowKey, string columnKey, string wrapKey)
    {
        Series[SeriesCount] = series;
        LayerIndices[SeriesCount] = layerIndex;
        RowKeys[SeriesCount] = rowKey;
        ColumnKeys[SeriesCount] = columnKey;
        WrapKeys[SeriesCount] = wrapKey;
        SeriesCount = SeriesCount + 1;
    }

    internal void AddGuide(string label)
    {
        int index = GuideIndex(label);
        if (index >= 0)
        {
            return;
        }

        GuideLabels[GuideCount] = label;
        GuideColors[GuideCount] = ChartPalettesOperations.Categorical().GetColor(GuideCount);
        GuideCount = GuideCount + 1;
    }

    internal int GuideIndex(string label)
    {
        int index = 0;
        while (index < GuideCount)
        {
            if (GuideLabels[index] == label)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal ChartColor? GuideColor(string label)
    {
        int index = GuideIndex(label);
        if (index < 0)
        {
            return null;
        }

        return GuideColors[index];
    }

    internal ChartDeclarativeView Build()
    {
        ChartDeclarativeView view = new ChartDeclarativeView();
        view.Series = new ChartCartesianSeries[SeriesCount];
        view.LayerIndices = new int[SeriesCount];
        view.RowFacetKeys = new string[SeriesCount];
        view.ColumnFacetKeys = new string[SeriesCount];
        view.WrapFacetKeys = new string[SeriesCount];
        System.Array.Copy(Series, view.Series, SeriesCount);
        System.Array.Copy(LayerIndices, view.LayerIndices, SeriesCount);
        System.Array.Copy(RowKeys, view.RowFacetKeys, SeriesCount);
        System.Array.Copy(ColumnKeys, view.ColumnFacetKeys, SeriesCount);
        System.Array.Copy(WrapKeys, view.WrapFacetKeys, SeriesCount);
        view.GuideLabels = new string[GuideCount];
        view.GuideColors = new ChartColor[GuideCount];
        System.Array.Copy(GuideLabels, view.GuideLabels, GuideCount);
        int index = 0;
        while (index < GuideCount)
        {
            view.GuideColors[index] = GuideColors[index].Copy();
            index = index + 1;
        }

        return view;
    }
}
