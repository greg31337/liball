namespace chart;
/// <summary>
/// Represents a cartesian series used by the chart model or rendering pipeline.
/// </summary>
public class ChartCartesianSeries : IChartCartesianSeries
{
    internal string Id;
    internal ChartCartesianSeriesKind Kind;
    internal int Order;
    internal int Sequence;
    internal bool ClipToPlotArea;
    internal ChartRenderCommand[] Commands;
    internal int CommandCount;
    /// <summary>
    /// Initializes a cartesian series from the supplied id and kind.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "kind">The kind.</param>
    public ChartCartesianSeries(string id, ChartCartesianSeriesKind kind)
    {
        ChartPlot.ValidateId(id);
        ValidateKind(kind);
        Id = id;
        Kind = kind;
        Order = 0;
        Sequence = 0;
        ClipToPlotArea = true;
        Commands = new ChartRenderCommand[4];
        CommandCount = 0;
    }

    /// <summary>
    /// Sets the order configuration.
    /// </summary>
    /// <param name = "order">The order.</param>
    public void SetOrder(int order)
    {
        Order = order;
    }

    /// <summary>
    /// Sets the clip to plot area configuration.
    /// </summary>
    /// <param name = "clipToPlotArea">The clip to plot area.</param>
    public void SetClipToPlotArea(bool clipToPlotArea)
    {
        ClipToPlotArea = clipToPlotArea;
    }

    /// <summary>
    /// Adds the supplied command to this object.
    /// </summary>
    /// <param name = "command">The command.</param>
    /// <returns>The resulting render command.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartRenderCommand AddCommand(ChartRenderCommand command)
    {
        if (command == null)
        {
            throw new System.ArgumentNullException("command");
        }

        EnsureCapacity(CommandCount + 1);
        Commands[CommandCount] = command.Copy();
        CommandCount = CommandCount + 1;
        return Commands[CommandCount - 1];
    }

    /// <summary>
    /// Adds the supplied line to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "x1">The x1.</param>
    /// <param name = "y1">The y1.</param>
    /// <param name = "x2">The x2.</param>
    /// <param name = "y2">The y2.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    public ChartRenderCommand AddLine(string id, double x1, double y1, double x2, double y2, ChartRenderStyle style)
    {
        return AddCommand(ChartRenderCommand.CreateLine(id, x1, y1, x2, y2, style));
    }

    /// <summary>
    /// Adds the supplied polyline to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    public ChartRenderCommand AddPolyline(string id, double[] xValues, double[] yValues, ChartRenderStyle style)
    {
        ChartCartesianTransform.ValidateValueArrays(xValues, yValues, 2);
        return AddCommand(ChartRenderCommand.CreatePolyline(id, CreateDataPoints(xValues, yValues), style));
    }

    /// <summary>
    /// Adds the supplied polygon to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    public ChartRenderCommand AddPolygon(string id, double[] xValues, double[] yValues, ChartRenderStyle style)
    {
        ChartCartesianTransform.ValidateValueArrays(xValues, yValues, 3);
        return AddCommand(ChartRenderCommand.CreatePolygon(id, CreateDataPoints(xValues, yValues), style));
    }

    /// <summary>
    /// Adds the supplied marker to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "shape">The shape.</param>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <param name = "size">The size.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    public ChartRenderCommand AddMarker(string id, ChartMarkerShape shape, double x, double y, double size, ChartRenderStyle style)
    {
        return AddCommand(ChartRenderCommand.CreateMarker(id, shape, x, y, size, style));
    }

    /// <summary>Adds a custom path marker in data coordinates.</summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "markerPath">The normalized custom marker path.</param>
    /// <param name = "x">The horizontal data value.</param>
    /// <param name = "y">The vertical data value.</param>
    /// <param name = "size">The positive rendered marker size.</param>
    /// <param name = "style">The rendering style.</param>
    /// <returns>The retained custom marker command.</returns>
    public ChartRenderCommand AddCustomMarker(string id, ChartPath markerPath, double x, double y, double size, ChartRenderStyle style)
    {
        return AddCommand(ChartRenderCommand.CreateCustomMarker(id, markerPath, x, y, size, style));
    }

    /// <summary>
    /// Adds the supplied text to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "text">The text to measure, render, or encode.</param>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting render command.</returns>
    public ChartRenderCommand AddText(string id, string text, double x, double y, ChartRenderStyle style)
    {
        return AddCommand(ChartRenderCommand.CreateText(id, text, x, y, style));
    }

    /// <summary>
    /// Returns the requested command.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting render command.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartRenderCommand CommandAt(int index)
    {
        if (index < 0 || index >= CommandCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Commands[index];
    }

    /// <summary>Replaces the style of one retained point or command without changing the series default.</summary>
    /// <param name = "index">The zero-based command index.</param>
    /// <param name = "style">The point-specific rendering style.</param>
    public void SetPointStyle(int index, ChartRenderStyle style)
    {
        if (index < 0 || index >= CommandCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        if (style == null)
        {
            throw new System.ArgumentNullException("style");
        }

        Commands[index].Style = style.Copy();
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartCartesianSeries Copy()
    {
        ChartCartesianSeries series = new ChartCartesianSeries(Id, Kind);
        series.Order = Order;
        series.Sequence = Sequence;
        series.ClipToPlotArea = ClipToPlotArea;
        int index = 0;
        while (index < CommandCount)
        {
            series.AddCommand(Commands[index]);
            index = index + 1;
        }

        return series;
    }

    internal void EnsureCapacity(int capacity)
    {
        if (capacity <= Commands.Length)
        {
            return;
        }

        int newCapacity = ChartArrayUtilities.GrowthCapacity(Commands.Length, capacity);
        ChartRenderCommand[] commands = new ChartRenderCommand[newCapacity];
        System.Array.Copy(Commands, commands, CommandCount);
        Commands = commands;
    }

    internal static ChartPoint[] CreateDataPoints(double[] xValues, double[] yValues)
    {
        ChartCartesianTransform.ValidateValueArrays(xValues, yValues, 1);
        ChartPoint[] points = new ChartPoint[xValues.Length];
        int index = 0;
        while (index < xValues.Length)
        {
            points[index] = new ChartPoint(xValues[index], yValues[index]);
            index = index + 1;
        }

        return points;
    }

    internal static void ValidateKind(ChartCartesianSeriesKind kind)
    {
        int value = (int)kind;
        if (value < 0 || value > 4)
        {
            throw new System.ArgumentOutOfRangeException("kind");
        }
    }

    /// <summary>Returns the current id.</summary>
    /// <returns>The current field value.</returns>
    public string GetId()
    {
        return Id;
    }

    /// <summary>Replaces the current id.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetId(string value)
    {
        Id = value;
    }

    /// <summary>Returns the current kind.</summary>
    /// <returns>The current field value.</returns>
    public ChartCartesianSeriesKind GetKind()
    {
        return Kind;
    }

    /// <summary>Replaces the current kind.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetKind(ChartCartesianSeriesKind value)
    {
        Kind = value;
    }

    /// <summary>Returns the current order.</summary>
    /// <returns>The current field value.</returns>
    public int GetOrder()
    {
        return Order;
    }

    /// <summary>Returns the current sequence.</summary>
    /// <returns>The current field value.</returns>
    public int GetSequence()
    {
        return Sequence;
    }

    /// <summary>Replaces the current sequence.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetSequence(int value)
    {
        Sequence = value;
    }

    /// <summary>Returns the current clip to plot area.</summary>
    /// <returns>The current field value.</returns>
    public bool GetClipToPlotArea()
    {
        return ClipToPlotArea;
    }

    /// <summary>Returns the current commands.</summary>
    /// <returns>The current field value.</returns>
    public ChartRenderCommand[] GetCommands()
    {
        return Commands;
    }

    /// <summary>Replaces the current commands.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetCommands(ChartRenderCommand[] value)
    {
        Commands = value;
    }

    /// <summary>Returns the current command count.</summary>
    /// <returns>The current field value.</returns>
    public int GetCommandCount()
    {
        return CommandCount;
    }

    /// <summary>Replaces the current command count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetCommandCount(int value)
    {
        CommandCount = value;
    }
}
