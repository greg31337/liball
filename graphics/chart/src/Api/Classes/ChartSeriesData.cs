namespace chart;
/// <summary>
/// Stores optional, row-aligned arrays for common chart series channels.
/// </summary>
/// <remarks>
/// Every present channel has the fixed row count supplied to the constructor. Numeric color values are scalar values intended for color encoding.
/// </remarks>
public class ChartSeriesData : IChartSeriesData
{
    internal int RowCount;
    internal double[] XValues;
    internal double[] YValues;
    internal double[] ZValues;
    internal double[] OpenValues;
    internal double[] HighValues;
    internal double[] LowValues;
    internal double[] CloseValues;
    internal double[] SizeValues;
    internal double[] ColorValues;
    internal string? [] LabelValues;
    internal string? [] GroupValues;
    internal int Version;
    /// <summary>
    /// Initializes empty series data for a fixed positive number of rows.
    /// </summary>
    /// <param name = "rowCount">The positive row count shared by every channel that is set.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when the row count is not positive.</exception>
    public ChartSeriesData(int rowCount)
    {
        if (rowCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException("rowCount");
        }

        RowCount = rowCount;
        XValues = new double[0];
        YValues = new double[0];
        ZValues = new double[0];
        OpenValues = new double[0];
        HighValues = new double[0];
        LowValues = new double[0];
        CloseValues = new double[0];
        SizeValues = new double[0];
        ColorValues = new double[0];
        LabelValues = new string? [0];
        GroupValues = new string? [0];
        Version = 0;
    }

    /// <summary>Sets the x values.</summary>
    /// <param name = "values">The values, whose length must equal the row count.</param>
    public void SetXValues(double[] values)
    {
        XValues = CopyNumberChannel(values, "xValues");
        Invalidate();
    }

    /// <summary>Sets the y values.</summary>
    /// <param name = "values">The values, whose length must equal the row count.</param>
    public void SetYValues(double[] values)
    {
        YValues = CopyNumberChannel(values, "yValues");
        Invalidate();
    }

    /// <summary>Sets the z values.</summary>
    /// <param name = "values">The values, whose length must equal the row count.</param>
    public void SetZValues(double[] values)
    {
        ZValues = CopyNumberChannel(values, "zValues");
        Invalidate();
    }

    /// <summary>Sets the open values.</summary>
    /// <param name = "values">The values, whose length must equal the row count.</param>
    public void SetOpenValues(double[] values)
    {
        OpenValues = CopyNumberChannel(values, "openValues");
        Invalidate();
    }

    /// <summary>Sets the high values.</summary>
    /// <param name = "values">The values, whose length must equal the row count.</param>
    public void SetHighValues(double[] values)
    {
        HighValues = CopyNumberChannel(values, "highValues");
        Invalidate();
    }

    /// <summary>Sets the low values.</summary>
    /// <param name = "values">The values, whose length must equal the row count.</param>
    public void SetLowValues(double[] values)
    {
        LowValues = CopyNumberChannel(values, "lowValues");
        Invalidate();
    }

    /// <summary>Sets the close values.</summary>
    /// <param name = "values">The values, whose length must equal the row count.</param>
    public void SetCloseValues(double[] values)
    {
        CloseValues = CopyNumberChannel(values, "closeValues");
        Invalidate();
    }

    /// <summary>Sets the size values.</summary>
    /// <param name = "values">The values, whose length must equal the row count.</param>
    public void SetSizeValues(double[] values)
    {
        SizeValues = CopyNumberChannel(values, "sizeValues");
        Invalidate();
    }

    /// <summary>Sets the numeric color values.</summary>
    /// <param name = "values">The values, whose length must equal the row count.</param>
    public void SetColorValues(double[] values)
    {
        ColorValues = CopyNumberChannel(values, "colorValues");
        Invalidate();
    }

    /// <summary>Sets the label values.</summary>
    /// <param name = "values">The values, whose length must equal the row count.</param>
    public void SetLabelValues(string? [] values)
    {
        LabelValues = CopyTextChannel(values, "labelValues");
        Invalidate();
    }

    /// <summary>Sets the group values.</summary>
    /// <param name = "values">The values, whose length must equal the row count.</param>
    public void SetGroupValues(string? [] values)
    {
        GroupValues = CopyTextChannel(values, "groupValues");
        Invalidate();
    }

    /// <summary>Returns an independent copy of the x values.</summary>
    /// <returns>The copied values, or an empty array when the channel is absent.</returns>
    public double[] CopyXValues()
    {
        return ChartDataMatrix.CopyDoubleArray(XValues);
    }

    /// <summary>Returns an independent copy of the y values.</summary>
    /// <returns>The copied values, or an empty array when the channel is absent.</returns>
    public double[] CopyYValues()
    {
        return ChartDataMatrix.CopyDoubleArray(YValues);
    }

    /// <summary>Returns an independent copy of the z values.</summary>
    /// <returns>The copied values, or an empty array when the channel is absent.</returns>
    public double[] CopyZValues()
    {
        return ChartDataMatrix.CopyDoubleArray(ZValues);
    }

    /// <summary>Returns an independent copy of the open values.</summary>
    /// <returns>The copied values, or an empty array when the channel is absent.</returns>
    public double[] CopyOpenValues()
    {
        return ChartDataMatrix.CopyDoubleArray(OpenValues);
    }

    /// <summary>Returns an independent copy of the high values.</summary>
    /// <returns>The copied values, or an empty array when the channel is absent.</returns>
    public double[] CopyHighValues()
    {
        return ChartDataMatrix.CopyDoubleArray(HighValues);
    }

    /// <summary>Returns an independent copy of the low values.</summary>
    /// <returns>The copied values, or an empty array when the channel is absent.</returns>
    public double[] CopyLowValues()
    {
        return ChartDataMatrix.CopyDoubleArray(LowValues);
    }

    /// <summary>Returns an independent copy of the close values.</summary>
    /// <returns>The copied values, or an empty array when the channel is absent.</returns>
    public double[] CopyCloseValues()
    {
        return ChartDataMatrix.CopyDoubleArray(CloseValues);
    }

    /// <summary>Returns an independent copy of the size values.</summary>
    /// <returns>The copied values, or an empty array when the channel is absent.</returns>
    public double[] CopySizeValues()
    {
        return ChartDataMatrix.CopyDoubleArray(SizeValues);
    }

    /// <summary>Returns an independent copy of the numeric color values.</summary>
    /// <returns>The copied values, or an empty array when the channel is absent.</returns>
    public double[] CopyColorValues()
    {
        return ChartDataMatrix.CopyDoubleArray(ColorValues);
    }

    /// <summary>Returns an independent copy of the label values.</summary>
    /// <returns>The copied values, or an empty array when the channel is absent.</returns>
    public string? [] CopyLabelValues()
    {
        return ChartDataColumn.CopyNullableStringArray(LabelValues);
    }

    /// <summary>Returns an independent copy of the group values.</summary>
    /// <returns>The copied values, or an empty array when the channel is absent.</returns>
    public string? [] CopyGroupValues()
    {
        return ChartDataColumn.CopyNullableStringArray(GroupValues);
    }

    /// <summary>Converts all present channels to a column-oriented table.</summary>
    /// <returns>A new independent data table.</returns>
    public ChartDataTable ToDataTable()
    {
        ChartDataTable table = new ChartDataTable();
        AddNumberColumnWhenPresent(table, "x", XValues);
        AddNumberColumnWhenPresent(table, "y", YValues);
        AddNumberColumnWhenPresent(table, "z", ZValues);
        AddNumberColumnWhenPresent(table, "open", OpenValues);
        AddNumberColumnWhenPresent(table, "high", HighValues);
        AddNumberColumnWhenPresent(table, "low", LowValues);
        AddNumberColumnWhenPresent(table, "close", CloseValues);
        AddNumberColumnWhenPresent(table, "size", SizeValues);
        AddNumberColumnWhenPresent(table, "color", ColorValues);
        if (LabelValues.Length > 0)
        {
            table.AddTextColumn("label", LabelValues);
        }

        if (GroupValues.Length > 0)
        {
            table.AddCategoryColumn("group", GroupValues);
        }

        return table;
    }

    /// <summary>Validates the present channels using the table validation policies.</summary>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult Validate()
    {
        return ChartDataValidatorOperations.ValidateTable(ToDataTable());
    }

    /// <summary>Creates a line series from the x and y channels.</summary>
    /// <param name = "id">The stable series identifier.</param>
    /// <param name = "style">The rendering style.</param>
    /// <returns>The newly created series.</returns>
    public ChartCartesianSeries CreateLineSeries(string id, ChartRenderStyle style)
    {
        RequireChannel(XValues, "x");
        RequireChannel(YValues, "y");
        return ChartLineAreaChartsOperations.CreateLineSeries(id, XValues, YValues, style);
    }

    /// <summary>Creates a scatter series from the x and y channels.</summary>
    /// <param name = "id">The stable series identifier.</param>
    /// <param name = "shape">The marker shape.</param>
    /// <param name = "markerSize">The marker size.</param>
    /// <param name = "style">The rendering style.</param>
    /// <returns>The newly created series.</returns>
    public ChartCartesianSeries CreateScatterSeries(string id, ChartMarkerShape shape, double markerSize, ChartRenderStyle style)
    {
        RequireChannel(XValues, "x");
        RequireChannel(YValues, "y");
        return ChartScatterChartsOperations.CreateScatterSeries(id, XValues, YValues, shape, markerSize, style);
    }

    /// <summary>Creates a bubble series from the x, y, and size channels.</summary>
    /// <param name = "id">The stable series identifier.</param>
    /// <param name = "minimumSize">The minimum rendered marker size.</param>
    /// <param name = "maximumSize">The maximum rendered marker size.</param>
    /// <param name = "style">The rendering style.</param>
    /// <returns>The newly created series.</returns>
    public ChartCartesianSeries CreateBubbleSeries(string id, double minimumSize, double maximumSize, ChartRenderStyle style)
    {
        RequireChannel(XValues, "x");
        RequireChannel(YValues, "y");
        RequireChannel(SizeValues, "size");
        return ChartScatterChartsOperations.CreateBubbleSeries(id, XValues, YValues, SizeValues, minimumSize, maximumSize, style);
    }

    /// <summary>Creates a range-bar series from the x, low, and high channels.</summary>
    /// <param name = "id">The stable series identifier.</param>
    /// <param name = "barWidth">The bar width.</param>
    /// <param name = "style">The rendering style.</param>
    /// <returns>The newly created series.</returns>
    public ChartCartesianSeries CreateRangeBarSeries(string id, double barWidth, ChartRenderStyle style)
    {
        RequireChannel(XValues, "x");
        RequireChannel(LowValues, "low");
        RequireChannel(HighValues, "high");
        return ChartBarChartsOperations.CreateRangeBarSeries(id, XValues, LowValues, HighValues, barWidth, style);
    }

    /// <summary>Creates a candlestick series from the x, open, high, low, and close channels.</summary>
    /// <param name = "id">The stable series identifier.</param>
    /// <param name = "bodyWidth">The candle body width.</param>
    /// <param name = "upStyle">The style for rising candles.</param>
    /// <param name = "downStyle">The style for falling candles.</param>
    /// <param name = "wickStyle">The wick style.</param>
    /// <returns>The newly created series.</returns>
    public ChartCartesianSeries CreateCandlestickSeries(string id, double bodyWidth, ChartRenderStyle upStyle, ChartRenderStyle downStyle, ChartRenderStyle wickStyle)
    {
        RequireChannel(XValues, "x");
        RequireChannel(OpenValues, "open");
        RequireChannel(HighValues, "high");
        RequireChannel(LowValues, "low");
        RequireChannel(CloseValues, "close");
        return ChartFinancialChartsOperations.CreateCandlestickSeries(id, XValues, OpenValues, HighValues, LowValues, CloseValues, bodyWidth, upStyle, downStyle, wickStyle);
    }

    /// <summary>Creates a deep copy that can be changed independently.</summary>
    /// <returns>The copied series data.</returns>
    public ChartSeriesData Copy()
    {
        ChartSeriesData copy = new ChartSeriesData(RowCount);
        CopyChannelWhenPresent(copy);
        copy.Version = Version;
        return copy;
    }

    /// <summary>Appends rows whose present channels match this series data.</summary>
    /// <param name = "data">The series rows to append.</param>
    public void AppendData(ChartSeriesData data)
    {
        ValidateStreamingSchema(data);
        ChartDataTable table = ToDataTable();
        table.AppendRows(data.ToDataTable());
        ApplyStreamingTable(table);
        Invalidate();
    }

    /// <summary>Replaces existing rows whose present channels match this series data.</summary>
    /// <param name = "startRow">The first row to replace.</param>
    /// <param name = "data">The replacement series rows.</param>
    public void UpdateData(int startRow, ChartSeriesData data)
    {
        ValidateStreamingSchema(data);
        ChartDataTable table = ToDataTable();
        table.UpdateRows(startRow, data.ToDataTable());
        ApplyStreamingTable(table);
        Invalidate();
    }

    /// <summary>Explicitly marks this series data as changed.</summary>
    public void Invalidate()
    {
        if (Version == int.MaxValue)
        {
            Version = 1;
        }
        else
        {
            Version = Version + 1;
        }
    }

    /// <summary>Returns the mutation version used by streaming renderers.</summary>
    /// <returns>The current mutation version.</returns>
    public int GetVersion()
    {
        return Version;
    }

    /// <summary>Returns the row count shared by present channels.</summary>
    /// <returns>The row count.</returns>
    public int GetRowCount()
    {
        return RowCount;
    }

    internal void ValidateStreamingSchema(ChartSeriesData data)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (!HasSameStreamingChannels(data))
        {
            throw new System.ArgumentException("Streaming series data must have the same present channels.");
        }

        if (ToDataTable().ColumnCount == 0)
        {
            throw new System.InvalidOperationException("Streaming series data requires at least one present channel.");
        }
    }

    internal bool HasSameStreamingChannels(ChartSeriesData data)
    {
        return (XValues.Length > 0) == (data.XValues.Length > 0)
            && (YValues.Length > 0) == (data.YValues.Length > 0)
            && (ZValues.Length > 0) == (data.ZValues.Length > 0)
            && (OpenValues.Length > 0) == (data.OpenValues.Length > 0)
            && (HighValues.Length > 0) == (data.HighValues.Length > 0)
            && (LowValues.Length > 0) == (data.LowValues.Length > 0)
            && (CloseValues.Length > 0) == (data.CloseValues.Length > 0)
            && (SizeValues.Length > 0) == (data.SizeValues.Length > 0)
            && (ColorValues.Length > 0) == (data.ColorValues.Length > 0)
            && (LabelValues.Length > 0) == (data.LabelValues.Length > 0)
            && (GroupValues.Length > 0) == (data.GroupValues.Length > 0);
    }

    internal void ApplyStreamingTable(ChartDataTable table)
    {
        if (XValues.Length > 0)
        {
            XValues = table.GetColumn("x").CopyNumberValues();
        }

        if (YValues.Length > 0)
        {
            YValues = table.GetColumn("y").CopyNumberValues();
        }

        if (ZValues.Length > 0)
        {
            ZValues = table.GetColumn("z").CopyNumberValues();
        }

        if (OpenValues.Length > 0)
        {
            OpenValues = table.GetColumn("open").CopyNumberValues();
        }

        if (HighValues.Length > 0)
        {
            HighValues = table.GetColumn("high").CopyNumberValues();
        }

        if (LowValues.Length > 0)
        {
            LowValues = table.GetColumn("low").CopyNumberValues();
        }

        if (CloseValues.Length > 0)
        {
            CloseValues = table.GetColumn("close").CopyNumberValues();
        }

        if (SizeValues.Length > 0)
        {
            SizeValues = table.GetColumn("size").CopyNumberValues();
        }

        if (ColorValues.Length > 0)
        {
            ColorValues = table.GetColumn("color").CopyNumberValues();
        }

        if (LabelValues.Length > 0)
        {
            LabelValues = table.GetColumn("label").CopyTextValues();
        }

        if (GroupValues.Length > 0)
        {
            GroupValues = table.GetColumn("group").CopyCategoryValues();
        }

        RowCount = table.RowCount;
    }

    internal double[] CopyNumberChannel(double[] values, string name)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException(name);
        }

        if (values.Length != RowCount)
        {
            throw new System.ArgumentException("Series channel length must match the row count.", name);
        }

        return ChartDataMatrix.CopyDoubleArray(values);
    }

    internal string? [] CopyTextChannel(string? [] values, string name)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException(name);
        }

        if (values.Length != RowCount)
        {
            throw new System.ArgumentException("Series channel length must match the row count.", name);
        }

        return ChartDataColumn.CopyNullableStringArray(values);
    }

    internal static void AddNumberColumnWhenPresent(ChartDataTable table, string name, double[] values)
    {
        if (values.Length > 0)
        {
            table.AddNumberColumn(name, values);
        }
    }

    internal static void RequireChannel(double[] values, string name)
    {
        if (values.Length == 0)
        {
            throw new System.InvalidOperationException("The " + name + " series channel is required.");
        }
    }

    internal void CopyChannelWhenPresent(ChartSeriesData copy)
    {
        if (XValues.Length > 0)
        {
            copy.SetXValues(XValues);
        }

        if (YValues.Length > 0)
        {
            copy.SetYValues(YValues);
        }

        if (ZValues.Length > 0)
        {
            copy.SetZValues(ZValues);
        }

        if (OpenValues.Length > 0)
        {
            copy.SetOpenValues(OpenValues);
        }

        if (HighValues.Length > 0)
        {
            copy.SetHighValues(HighValues);
        }

        if (LowValues.Length > 0)
        {
            copy.SetLowValues(LowValues);
        }

        if (CloseValues.Length > 0)
        {
            copy.SetCloseValues(CloseValues);
        }

        if (SizeValues.Length > 0)
        {
            copy.SetSizeValues(SizeValues);
        }

        if (ColorValues.Length > 0)
        {
            copy.SetColorValues(ColorValues);
        }

        if (LabelValues.Length > 0)
        {
            copy.SetLabelValues(LabelValues);
        }

        if (GroupValues.Length > 0)
        {
            copy.SetGroupValues(GroupValues);
        }
    }
}
