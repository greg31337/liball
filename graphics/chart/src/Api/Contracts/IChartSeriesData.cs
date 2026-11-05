namespace chart;
/// <summary>Defines array-oriented chart series data and its conversion operations.</summary>
public interface IChartSeriesData
{
    /// <summary>Sets the x values.</summary>
    /// <param name = "values">The values, whose length must equal the row count.</param>
    void SetXValues(double[] values);
    /// <summary>Sets the y values.</summary>
    /// <param name = "values">The values, whose length must equal the row count.</param>
    void SetYValues(double[] values);
    /// <summary>Sets the z values.</summary>
    /// <param name = "values">The values, whose length must equal the row count.</param>
    void SetZValues(double[] values);
    /// <summary>Sets the open values.</summary>
    /// <param name = "values">The values, whose length must equal the row count.</param>
    void SetOpenValues(double[] values);
    /// <summary>Sets the high values.</summary>
    /// <param name = "values">The values, whose length must equal the row count.</param>
    void SetHighValues(double[] values);
    /// <summary>Sets the low values.</summary>
    /// <param name = "values">The values, whose length must equal the row count.</param>
    void SetLowValues(double[] values);
    /// <summary>Sets the close values.</summary>
    /// <param name = "values">The values, whose length must equal the row count.</param>
    void SetCloseValues(double[] values);
    /// <summary>Sets the size values.</summary>
    /// <param name = "values">The values, whose length must equal the row count.</param>
    void SetSizeValues(double[] values);
    /// <summary>Sets the numeric color values.</summary>
    /// <param name = "values">The values, whose length must equal the row count.</param>
    void SetColorValues(double[] values);
    /// <summary>Sets the label values.</summary>
    /// <param name = "values">The values, whose length must equal the row count.</param>
    void SetLabelValues(string? [] values);
    /// <summary>Sets the group values.</summary>
    /// <param name = "values">The values, whose length must equal the row count.</param>
    void SetGroupValues(string? [] values);
    /// <summary>Returns an independent copy of the x values.</summary>
    /// <returns>The copied values, or an empty array when the channel is absent.</returns>
    double[] CopyXValues();
    /// <summary>Returns an independent copy of the y values.</summary>
    /// <returns>The copied values, or an empty array when the channel is absent.</returns>
    double[] CopyYValues();
    /// <summary>Returns an independent copy of the z values.</summary>
    /// <returns>The copied values, or an empty array when the channel is absent.</returns>
    double[] CopyZValues();
    /// <summary>Returns an independent copy of the open values.</summary>
    /// <returns>The copied values, or an empty array when the channel is absent.</returns>
    double[] CopyOpenValues();
    /// <summary>Returns an independent copy of the high values.</summary>
    /// <returns>The copied values, or an empty array when the channel is absent.</returns>
    double[] CopyHighValues();
    /// <summary>Returns an independent copy of the low values.</summary>
    /// <returns>The copied values, or an empty array when the channel is absent.</returns>
    double[] CopyLowValues();
    /// <summary>Returns an independent copy of the close values.</summary>
    /// <returns>The copied values, or an empty array when the channel is absent.</returns>
    double[] CopyCloseValues();
    /// <summary>Returns an independent copy of the size values.</summary>
    /// <returns>The copied values, or an empty array when the channel is absent.</returns>
    double[] CopySizeValues();
    /// <summary>Returns an independent copy of the numeric color values.</summary>
    /// <returns>The copied values, or an empty array when the channel is absent.</returns>
    double[] CopyColorValues();
    /// <summary>Returns an independent copy of the label values.</summary>
    /// <returns>The copied values, or an empty array when the channel is absent.</returns>
    string? [] CopyLabelValues();
    /// <summary>Returns an independent copy of the group values.</summary>
    /// <returns>The copied values, or an empty array when the channel is absent.</returns>
    string? [] CopyGroupValues();
    /// <summary>Converts all present channels to a column-oriented table.</summary>
    /// <returns>A new independent data table.</returns>
    ChartDataTable ToDataTable();
    /// <summary>Validates the present channels using the table validation policies.</summary>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult Validate();
    /// <summary>Creates a line series from the x and y channels.</summary>
    /// <param name = "id">The stable series identifier.</param>
    /// <param name = "style">The rendering style.</param>
    /// <returns>The newly created series.</returns>
    ChartCartesianSeries CreateLineSeries(string id, ChartRenderStyle style);
    /// <summary>Creates a scatter series from the x and y channels.</summary>
    /// <param name = "id">The stable series identifier.</param>
    /// <param name = "shape">The marker shape.</param>
    /// <param name = "markerSize">The marker size.</param>
    /// <param name = "style">The rendering style.</param>
    /// <returns>The newly created series.</returns>
    ChartCartesianSeries CreateScatterSeries(string id, ChartMarkerShape shape, double markerSize, ChartRenderStyle style);
    /// <summary>Creates a bubble series from the x, y, and size channels.</summary>
    /// <param name = "id">The stable series identifier.</param>
    /// <param name = "minimumSize">The minimum rendered marker size.</param>
    /// <param name = "maximumSize">The maximum rendered marker size.</param>
    /// <param name = "style">The rendering style.</param>
    /// <returns>The newly created series.</returns>
    ChartCartesianSeries CreateBubbleSeries(string id, double minimumSize, double maximumSize, ChartRenderStyle style);
    /// <summary>Creates a range-bar series from the x, low, and high channels.</summary>
    /// <param name = "id">The stable series identifier.</param>
    /// <param name = "barWidth">The bar width.</param>
    /// <param name = "style">The rendering style.</param>
    /// <returns>The newly created series.</returns>
    ChartCartesianSeries CreateRangeBarSeries(string id, double barWidth, ChartRenderStyle style);
    /// <summary>Creates a candlestick series from the x, open, high, low, and close channels.</summary>
    /// <param name = "id">The stable series identifier.</param>
    /// <param name = "bodyWidth">The candle body width.</param>
    /// <param name = "upStyle">The style for rising candles.</param>
    /// <param name = "downStyle">The style for falling candles.</param>
    /// <param name = "wickStyle">The wick style.</param>
    /// <returns>The newly created series.</returns>
    ChartCartesianSeries CreateCandlestickSeries(string id, double bodyWidth, ChartRenderStyle upStyle, ChartRenderStyle downStyle, ChartRenderStyle wickStyle);
    /// <summary>Creates a deep copy that can be changed independently.</summary>
    /// <returns>The copied series data.</returns>
    ChartSeriesData Copy();
    /// <summary>Appends rows whose present channels match this series data.</summary>
    /// <param name = "data">The series rows to append.</param>
    void AppendData(ChartSeriesData data);
    /// <summary>Replaces existing rows whose present channels match this series data.</summary>
    /// <param name = "startRow">The first row to replace.</param>
    /// <param name = "data">The replacement series rows.</param>
    void UpdateData(int startRow, ChartSeriesData data);
    /// <summary>Explicitly marks this series data as changed.</summary>
    void Invalidate();
    /// <summary>Returns the mutation version used by streaming renderers.</summary>
    /// <returns>The current mutation version.</returns>
    int GetVersion();
    /// <summary>Returns the row count shared by present channels.</summary>
    /// <returns>The row count.</returns>
    int GetRowCount();
}
