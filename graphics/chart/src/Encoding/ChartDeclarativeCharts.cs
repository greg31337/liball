namespace chart;
internal static class ChartDeclarativeChartsOperations
{
    /// <summary>
    /// Creates the requested series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "mark">The mark.</param>
    /// <param name = "table">The column-oriented data table that supplies encoded fields.</param>
    /// <param name = "encoding">The mapping from data fields or constants to visual channels.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateSeries(string id, ChartDeclarativeMark mark, ChartDataTable table, ChartVisualEncoding encoding, ChartRenderStyle style)
    {
        ValidateMark(mark);
        if (mark == ChartDeclarativeMark.Line)
        {
            return CreateLineSeries(id, table, encoding, style);
        }

        if (mark == ChartDeclarativeMark.Area)
        {
            return CreateAreaSeries(id, table, encoding, style);
        }

        if (mark == ChartDeclarativeMark.Scatter)
        {
            return CreateScatterSeries(id, table, encoding, style);
        }

        if (mark == ChartDeclarativeMark.Bubble)
        {
            return CreateBubbleSeries(id, table, encoding, 4.0, 12.0, style);
        }

        if (mark == ChartDeclarativeMark.VerticalBar)
        {
            return CreateVerticalBarSeries(id, table, encoding, style);
        }

        if (mark == ChartDeclarativeMark.HorizontalBar)
        {
            return CreateHorizontalBarSeries(id, table, encoding, style);
        }

        if (mark == ChartDeclarativeMark.RangeBar)
        {
            return CreateRangeBarSeries(id, table, encoding, style);
        }

        if (mark == ChartDeclarativeMark.Point)
        {
            return CreateScatterSeries(id, table, encoding, style);
        }

        if (mark == ChartDeclarativeMark.Circle)
        {
            return CreateFixedPointSeries(id, table, encoding, ChartMarkerShape.Circle, style);
        }

        if (mark == ChartDeclarativeMark.Square)
        {
            return CreateFixedPointSeries(id, table, encoding, ChartMarkerShape.Square, style);
        }

        if (mark == ChartDeclarativeMark.Rect)
        {
            return CreateRectSeries(id, table, encoding, style);
        }

        if (mark == ChartDeclarativeMark.Rule)
        {
            return CreateRuleSeries(id, table, encoding, style);
        }

        if (mark == ChartDeclarativeMark.Text)
        {
            return CreateTextSeries(id, table, encoding, style);
        }

        if (mark == ChartDeclarativeMark.Tick)
        {
            return CreateTickSeries(id, table, encoding, style);
        }

        if (mark == ChartDeclarativeMark.Trail)
        {
            return CreateTrailSeries(id, table, encoding, style);
        }

        if (mark == ChartDeclarativeMark.StepLine)
        {
            return CreateStepLineSeries(id, table, encoding, style);
        }

        if (mark == ChartDeclarativeMark.SplineLine)
        {
            return CreateSplineLineSeries(id, table, encoding, style);
        }

        if (mark == ChartDeclarativeMark.RangeArea)
        {
            return CreateRangeAreaSeries(id, table, encoding, style);
        }

        if (mark == ChartDeclarativeMark.Lollipop)
        {
            return CreateLollipopSeries(id, table, encoding, style);
        }

        return CreateStemSeries(id, table, encoding, style);
    }

    /// <summary>
    /// Creates the requested line series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "table">The column-oriented data table that supplies encoded fields.</param>
    /// <param name = "encoding">The mapping from data fields or constants to visual channels.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateLineSeries(string id, ChartDataTable table, ChartVisualEncoding encoding, ChartRenderStyle style)
    {
        ValidateCommon(id, table, encoding, style);
        ChartDeclarativeData data = PrepareOrderedData(table, encoding);
        table = data.Table;
        encoding = data.Encoding;
        double[] xValues = ChartVisualEncodingOperations.PositionXValues(table, encoding);
        double[] yValues = ChartVisualEncodingOperations.PositionYValues(table, encoding);
        return ChartVisualEncodingOperations.CreateEncodedLine(id, table, encoding, style, xValues, yValues);
    }

    /// <summary>
    /// Creates the requested area series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "table">The column-oriented data table that supplies encoded fields.</param>
    /// <param name = "encoding">The mapping from data fields or constants to visual channels.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateAreaSeries(string id, ChartDataTable table, ChartVisualEncoding encoding, ChartRenderStyle style)
    {
        ValidateCommon(id, table, encoding, style);
        ChartDeclarativeData data = PrepareOrderedData(table, encoding);
        table = data.Table;
        encoding = data.Encoding;
        double[] xValues = ChartVisualEncodingOperations.PositionXValues(table, encoding);
        double[] upperValues = ChartVisualEncodingOperations.PositionYValues(table, encoding);
        double[] lowerValues = NumberValuesOrConstant(table, encoding, ChartEncodingChannel.Y2, 0.0);
        ChartCartesianSeries series = ChartLineAreaChartsOperations.CreateAreaSeries(id, xValues, upperValues, lowerValues, style);
        return ChartVisualEncodingOperations.FinishSeries(series, table, encoding, style, xValues, upperValues);
    }

    /// <summary>
    /// Creates the requested scatter series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "table">The column-oriented data table that supplies encoded fields.</param>
    /// <param name = "encoding">The mapping from data fields or constants to visual channels.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateScatterSeries(string id, ChartDataTable table, ChartVisualEncoding encoding, ChartRenderStyle style)
    {
        ValidateCommon(id, table, encoding, style);
        ChartDeclarativeData data = PrepareOrderedData(table, encoding);
        table = data.Table;
        encoding = data.Encoding;
        double[] xValues = ChartVisualEncodingOperations.PositionXValues(table, encoding);
        double[] yValues = ChartVisualEncodingOperations.PositionYValues(table, encoding);
        double markerSize = NumberConstantOrDefault(encoding, ChartEncodingChannel.Size, 6.0);
        ChartMarkerShape shape = ShapeConstantOrDefault(encoding, ChartEncodingChannel.Shape, ChartMarkerShape.Circle);
        ChartCartesianSeries series = ChartScatterChartsOperations.CreateScatterSeries(id, xValues, yValues, shape, markerSize, style);
        return ChartVisualEncodingOperations.FinishSeries(series, table, encoding, style, xValues, yValues);
    }

    /// <summary>
    /// Creates the requested bubble series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "table">The column-oriented data table that supplies encoded fields.</param>
    /// <param name = "encoding">The mapping from data fields or constants to visual channels.</param>
    /// <param name = "minimumSize">The minimum size.</param>
    /// <param name = "maximumSize">The maximum size.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateBubbleSeries(string id, ChartDataTable table, ChartVisualEncoding encoding, double minimumSize, double maximumSize, ChartRenderStyle style)
    {
        ValidateCommon(id, table, encoding, style);
        ChartDeclarativeData data = PrepareOrderedData(table, encoding);
        table = data.Table;
        encoding = data.Encoding;
        RequireChannel(encoding, ChartEncodingChannel.Size);
        double[] xValues = ChartVisualEncodingOperations.PositionXValues(table, encoding);
        double[] yValues = ChartVisualEncodingOperations.PositionYValues(table, encoding);
        double[] sizeValues = NumberValues(table, encoding, ChartEncodingChannel.Size);
        ChartCartesianSeries series = ChartScatterChartsOperations.CreateBubbleSeries(id, xValues, yValues, sizeValues, minimumSize, maximumSize, style);
        return ChartVisualEncodingOperations.FinishSeries(series, table, encoding, style, xValues, yValues);
    }

    /// <summary>
    /// Creates the requested vertical bar series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "table">The column-oriented data table that supplies encoded fields.</param>
    /// <param name = "encoding">The mapping from data fields or constants to visual channels.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateVerticalBarSeries(string id, ChartDataTable table, ChartVisualEncoding encoding, ChartRenderStyle style)
    {
        ValidateCommon(id, table, encoding, style);
        ChartDeclarativeData data = PrepareOrderedData(table, encoding);
        table = data.Table;
        encoding = data.Encoding;
        RequireChannel(encoding, ChartEncodingChannel.X);
        RequireChannel(encoding, ChartEncodingChannel.Y);
        double[] xValues = NumberValues(table, encoding, ChartEncodingChannel.X);
        double[] yValues = NumberValues(table, encoding, ChartEncodingChannel.Y);
        double barWidth = NumberConstantOrDefault(encoding, ChartEncodingChannel.Size, 0.8);
        double baseline = NumberConstantOrDefault(encoding, ChartEncodingChannel.Y2, 0.0);
        ChartCartesianSeries series = ChartBarChartsOperations.CreateVerticalBarSeries(id, xValues, yValues, barWidth, baseline, style);
        return ChartVisualEncodingOperations.FinishSeries(series, table, encoding, style, xValues, yValues);
    }

    /// <summary>
    /// Creates the requested horizontal bar series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "table">The column-oriented data table that supplies encoded fields.</param>
    /// <param name = "encoding">The mapping from data fields or constants to visual channels.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateHorizontalBarSeries(string id, ChartDataTable table, ChartVisualEncoding encoding, ChartRenderStyle style)
    {
        ValidateCommon(id, table, encoding, style);
        ChartDeclarativeData data = PrepareOrderedData(table, encoding);
        table = data.Table;
        encoding = data.Encoding;
        RequireChannel(encoding, ChartEncodingChannel.X);
        RequireChannel(encoding, ChartEncodingChannel.Y);
        double[] xValues = NumberValues(table, encoding, ChartEncodingChannel.X);
        double[] yValues = NumberValues(table, encoding, ChartEncodingChannel.Y);
        double barHeight = NumberConstantOrDefault(encoding, ChartEncodingChannel.Size, 0.8);
        double baseline = NumberConstantOrDefault(encoding, ChartEncodingChannel.X2, 0.0);
        ChartCartesianSeries series = ChartBarChartsOperations.CreateHorizontalBarSeries(id, yValues, xValues, barHeight, baseline, style);
        return ChartVisualEncodingOperations.FinishSeries(series, table, encoding, style, xValues, yValues);
    }

    /// <summary>
    /// Creates the requested range bar series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "table">The column-oriented data table that supplies encoded fields.</param>
    /// <param name = "encoding">The mapping from data fields or constants to visual channels.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateRangeBarSeries(string id, ChartDataTable table, ChartVisualEncoding encoding, ChartRenderStyle style)
    {
        ValidateCommon(id, table, encoding, style);
        ChartDeclarativeData data = PrepareOrderedData(table, encoding);
        table = data.Table;
        encoding = data.Encoding;
        RequireChannel(encoding, ChartEncodingChannel.X);
        RequireChannel(encoding, ChartEncodingChannel.Y);
        RequireChannel(encoding, ChartEncodingChannel.Y2);
        double[] xValues = NumberValues(table, encoding, ChartEncodingChannel.X);
        double[] lowValues = NumberValues(table, encoding, ChartEncodingChannel.Y);
        double[] highValues = NumberValues(table, encoding, ChartEncodingChannel.Y2);
        double barWidth = NumberConstantOrDefault(encoding, ChartEncodingChannel.Size, 0.8);
        ChartCartesianSeries series = ChartBarChartsOperations.CreateRangeBarSeries(id, xValues, lowValues, highValues, barWidth, style);
        return ChartVisualEncodingOperations.FinishSeries(series, table, encoding, style, xValues, highValues);
    }

    /// <summary>Creates one series per stable value of the wrap-facet channel.</summary>
    /// <param name="idPrefix">The stable series identifier prefix.</param>
    /// <param name="mark">The declarative mark kind.</param>
    /// <param name="table">The source data table.</param>
    /// <param name="encoding">The visual encoding containing a wrap field.</param>
    /// <param name="style">The rendering style.</param>
    /// <returns>The wrapped facet series in first-observed order.</returns>
    public static ChartCartesianSeries[] CreateWrapFacetSeries(string idPrefix, ChartDeclarativeMark mark, ChartDataTable table, ChartVisualEncoding encoding, ChartRenderStyle style)
    {
        ValidateMark(mark);
        return ChartVisualEncodingOperations.CreateWrapFacetSeries(idPrefix, mark, table, encoding, style);
    }

    internal static ChartCartesianSeries CreateFixedPointSeries(string id, ChartDataTable table, ChartVisualEncoding encoding, ChartMarkerShape shape, ChartRenderStyle style)
    {
        ValidateCommon(id, table, encoding, style);
        ChartDeclarativeData data = PrepareOrderedData(table, encoding);
        table = data.Table;
        encoding = data.Encoding;
        double[] xValues = ChartVisualEncodingOperations.PositionXValues(table, encoding);
        double[] yValues = ChartVisualEncodingOperations.PositionYValues(table, encoding);
        double markerSize = NumberConstantOrDefault(encoding, ChartEncodingChannel.Size, 6.0);
        ChartCartesianSeries series = ChartScatterChartsOperations.CreateScatterSeries(id, xValues, yValues, shape, markerSize, style);
        return ChartVisualEncodingOperations.FinishSeries(series, table, encoding, style, xValues, yValues);
    }

    internal static ChartCartesianSeries CreateRectSeries(string id, ChartDataTable table, ChartVisualEncoding encoding, ChartRenderStyle style)
    {
        ValidateCommon(id, table, encoding, style);
        RequireChannel(encoding, ChartEncodingChannel.X2);
        RequireChannel(encoding, ChartEncodingChannel.Y2);
        ChartDeclarativeData data = PrepareOrderedData(table, encoding);
        table = data.Table;
        encoding = data.Encoding;
        double[] x = NumberValues(table, encoding, ChartEncodingChannel.X);
        double[] y = NumberValues(table, encoding, ChartEncodingChannel.Y);
        double[] x2 = NumberValues(table, encoding, ChartEncodingChannel.X2);
        double[] y2 = NumberValues(table, encoding, ChartEncodingChannel.Y2);
        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Mixed);
        int row = 0;
        while (row < table.RowCount)
        {
            ChartRenderStyle rowStyle = ChartVisualEncodingOperations.StyleForRow(style, table, encoding, row);
            string suffix = row.ToString(System.Globalization.CultureInfo.InvariantCulture);
            series.AddCommand(ChartRenderCommand.CreatePolygon(id + "-rect-" + suffix, new ChartPoint[] { new ChartPoint(x[row], y[row]), new ChartPoint(x[row], y2[row]), new ChartPoint(x2[row], y2[row]), new ChartPoint(x2[row], y[row]) }, rowStyle));
            row = row + 1;
        }

        ChartVisualEncodingOperations.ApplyZIndex(series, table, encoding);
        ChartVisualEncodingOperations.AddValueLabels(series, table, encoding, style, x, y);
        return series;
    }

    internal static ChartCartesianSeries CreateRuleSeries(string id, ChartDataTable table, ChartVisualEncoding encoding, ChartRenderStyle style)
    {
        ValidateCommon(id, table, encoding, style);
        RequireChannel(encoding, ChartEncodingChannel.X2);
        RequireChannel(encoding, ChartEncodingChannel.Y2);
        ChartDeclarativeData data = PrepareOrderedData(table, encoding);
        table = data.Table;
        encoding = data.Encoding;
        double[] x = NumberValues(table, encoding, ChartEncodingChannel.X);
        double[] y = NumberValues(table, encoding, ChartEncodingChannel.Y);
        double[] x2 = NumberValues(table, encoding, ChartEncodingChannel.X2);
        double[] y2 = NumberValues(table, encoding, ChartEncodingChannel.Y2);
        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Line);
        int row = 0;
        while (row < table.RowCount)
        {
            ChartRenderStyle rowStyle = ChartVisualEncodingOperations.StyleForRow(style, table, encoding, row);
            series.AddLine(id + "-rule-" + row.ToString(System.Globalization.CultureInfo.InvariantCulture), x[row], y[row], x2[row], y2[row], rowStyle);
            row = row + 1;
        }

        ChartVisualEncodingOperations.ApplyZIndex(series, table, encoding);
        ChartVisualEncodingOperations.AddValueLabels(series, table, encoding, style, x, y);
        return series;
    }

    internal static ChartCartesianSeries CreateTextSeries(string id, ChartDataTable table, ChartVisualEncoding encoding, ChartRenderStyle style)
    {
        ValidateCommon(id, table, encoding, style);
        RequireChannel(encoding, ChartEncodingChannel.Text);
        ChartDeclarativeData data = PrepareOrderedData(table, encoding);
        table = data.Table;
        encoding = data.Encoding;
        double[] x = ChartVisualEncodingOperations.PositionXValues(table, encoding);
        double[] y = ChartVisualEncodingOperations.PositionYValues(table, encoding);
        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Mixed);
        int row = 0;
        while (row < table.RowCount)
        {
            string? text = TextForRow(table, encoding, ChartEncodingChannel.Text, row);
            if (text != null && text.Length > 0)
            {
                ChartRenderStyle rowStyle = ChartVisualEncodingOperations.StyleForRow(style, table, encoding, row);
                series.AddText(id + "-text-" + row.ToString(System.Globalization.CultureInfo.InvariantCulture), text, x[row], y[row], rowStyle);
            }

            row = row + 1;
        }

        ChartVisualEncodingOperations.ApplyZIndex(series, table, encoding);
        return series;
    }

    internal static ChartCartesianSeries CreateTickSeries(string id, ChartDataTable table, ChartVisualEncoding encoding, ChartRenderStyle style)
    {
        ValidateCommon(id, table, encoding, style);
        ChartDeclarativeData data = PrepareOrderedData(table, encoding);
        table = data.Table;
        encoding = data.Encoding;
        double[] x = ChartVisualEncodingOperations.PositionXValues(table, encoding);
        double[] y = ChartVisualEncodingOperations.PositionYValues(table, encoding);
        double length = 6.0;
        if (encoding.HasChannel(ChartEncodingChannel.Size) && encoding.BindingFor(ChartEncodingChannel.Size).IsConstant)
        {
            length = NumberConstantOrDefault(encoding, ChartEncodingChannel.Size, 6.0);
        }
        ChartRenderStyle.ValidatePositive(length, "size");
        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Line);
        int row = 0;
        while (row < table.RowCount)
        {
            double rowLength = length;
            if (encoding.HasEffectiveChannel(ChartEncodingChannel.Size))
            {
                rowLength = ChartVisualEncodingOperations.NumberForRow(table, encoding, ChartEncodingChannel.Size, row);
                ChartRenderStyle.ValidatePositive(rowLength, "size");
            }

            ChartRenderStyle rowStyle = ChartVisualEncodingOperations.StyleForRow(style, table, encoding, row);
            series.AddLine(id + "-tick-" + row.ToString(System.Globalization.CultureInfo.InvariantCulture), x[row], y[row] - rowLength * 0.5, x[row], y[row] + rowLength * 0.5, rowStyle);
            row = row + 1;
        }

        ChartVisualEncodingOperations.ApplyZIndex(series, table, encoding);
        return series;
    }

    internal static ChartCartesianSeries CreateTrailSeries(string id, ChartDataTable table, ChartVisualEncoding encoding, ChartRenderStyle style)
    {
        ValidateCommon(id, table, encoding, style);
        ChartDeclarativeData data = PrepareOrderedData(table, encoding);
        table = data.Table;
        encoding = data.Encoding;
        double[] x = ChartVisualEncodingOperations.PositionXValues(table, encoding);
        double[] y = ChartVisualEncodingOperations.PositionYValues(table, encoding);
        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Line);
        int row = 0;
        while (row < table.RowCount - 1)
        {
            ChartRenderStyle rowStyle = ChartVisualEncodingOperations.StyleForRow(style, table, encoding, row);
            if (encoding.HasEffectiveChannel(ChartEncodingChannel.Size))
            {
                double width = ChartVisualEncodingOperations.NumberForRow(table, encoding, ChartEncodingChannel.Size, row);
                ChartRenderStyle.ValidatePositive(width, "size");
                ChartColor stroke = rowStyle.StrokeColor;
                if (!rowStyle.HasStroke || rowStyle.StrokeNone)
                {
                    stroke = rowStyle.FillColor;
                }

                rowStyle.SetStroke(stroke, width);
            }

            series.AddLine(id + "-trail-" + row.ToString(System.Globalization.CultureInfo.InvariantCulture), x[row], y[row], x[row + 1], y[row + 1], rowStyle);
            row = row + 1;
        }

        ChartVisualEncodingOperations.ApplyZIndex(series, table, encoding);
        ChartVisualEncodingOperations.AddValueLabels(series, table, encoding, style, x, y);
        return series;
    }

    internal static ChartCartesianSeries CreateStepLineSeries(string id, ChartDataTable table, ChartVisualEncoding encoding, ChartRenderStyle style)
    {
        ValidateCommon(id, table, encoding, style);
        ChartDeclarativeData data = PrepareOrderedData(table, encoding);
        table = data.Table;
        encoding = data.Encoding;
        double[] x = ChartVisualEncodingOperations.PositionXValues(table, encoding);
        double[] y = ChartVisualEncodingOperations.PositionYValues(table, encoding);
        ChartCartesianSeries series = ChartLineAreaChartsOperations.CreateStepLineSeries(id, x, y, ChartLineInterpolation.StepMiddle, style);
        return ChartVisualEncodingOperations.FinishSeries(series, table, encoding, style, x, y);
    }

    internal static ChartCartesianSeries CreateSplineLineSeries(string id, ChartDataTable table, ChartVisualEncoding encoding, ChartRenderStyle style)
    {
        ValidateCommon(id, table, encoding, style);
        ChartDeclarativeData data = PrepareOrderedData(table, encoding);
        table = data.Table;
        encoding = data.Encoding;
        double[] x = ChartVisualEncodingOperations.PositionXValues(table, encoding);
        double[] y = ChartVisualEncodingOperations.PositionYValues(table, encoding);
        ChartCartesianSeries series = ChartLineAreaChartsOperations.CreateSplineLineSeries(id, x, y, 8, style);
        return ChartVisualEncodingOperations.FinishSeries(series, table, encoding, style, x, y);
    }

    internal static ChartCartesianSeries CreateRangeAreaSeries(string id, ChartDataTable table, ChartVisualEncoding encoding, ChartRenderStyle style)
    {
        ValidateCommon(id, table, encoding, style);
        RequireChannel(encoding, ChartEncodingChannel.Y2);
        ChartDeclarativeData data = PrepareOrderedData(table, encoding);
        table = data.Table;
        encoding = data.Encoding;
        double[] x = ChartVisualEncodingOperations.PositionXValues(table, encoding);
        double[] y = ChartVisualEncodingOperations.PositionYValues(table, encoding);
        double[] y2 = NumberValues(table, encoding, ChartEncodingChannel.Y2);
        ChartCartesianSeries series = ChartLineAreaChartsOperations.CreateAreaSeries(id, x, y, y2, style);
        return ChartVisualEncodingOperations.FinishSeries(series, table, encoding, style, x, y);
    }

    internal static ChartCartesianSeries CreateLollipopSeries(string id, ChartDataTable table, ChartVisualEncoding encoding, ChartRenderStyle style)
    {
        ValidateCommon(id, table, encoding, style);
        ChartDeclarativeData data = PrepareOrderedData(table, encoding);
        table = data.Table;
        encoding = data.Encoding;
        double[] x = ChartVisualEncodingOperations.PositionXValues(table, encoding);
        double[] y = ChartVisualEncodingOperations.PositionYValues(table, encoding);
        double[] baseline = NumberValuesOrConstant(table, encoding, ChartEncodingChannel.Y2, 0.0);
        double markerSize = 6.0;
        if (encoding.HasChannel(ChartEncodingChannel.Size) && encoding.BindingFor(ChartEncodingChannel.Size).IsConstant)
        {
            markerSize = NumberConstantOrDefault(encoding, ChartEncodingChannel.Size, 6.0);
        }
        ChartMarkerShape shape = ShapeConstantOrDefault(encoding, ChartEncodingChannel.Shape, ChartMarkerShape.Circle);
        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Mixed);
        int row = 0;
        while (row < table.RowCount)
        {
            ChartRenderStyle rowStyle = ChartVisualEncodingOperations.StyleForRow(style, table, encoding, row);
            double rowSize = markerSize;
            ChartMarkerShape rowShape = shape;
            if (encoding.HasEffectiveChannel(ChartEncodingChannel.Size))
            {
                rowSize = ChartVisualEncodingOperations.NumberForRow(table, encoding, ChartEncodingChannel.Size, row);
            }

            if (encoding.Selection != null && encoding.HasConditionalEncoding(ChartEncodingChannel.Shape))
            {
                rowShape = ChartVisualEncodingOperations.ShapeForRow(encoding, row);
            }

            string suffix = row.ToString(System.Globalization.CultureInfo.InvariantCulture);
            series.AddLine(id + "-stem-" + suffix, x[row], baseline[row], x[row], y[row], rowStyle);
            series.AddMarker(id + "-point-" + suffix, rowShape, x[row], y[row], rowSize, rowStyle);
            row = row + 1;
        }

        ChartVisualEncodingOperations.ApplyZIndex(series, table, encoding);
        ChartVisualEncodingOperations.AddValueLabels(series, table, encoding, style, x, y);
        return series;
    }

    internal static ChartCartesianSeries CreateStemSeries(string id, ChartDataTable table, ChartVisualEncoding encoding, ChartRenderStyle style)
    {
        ValidateCommon(id, table, encoding, style);
        ChartDeclarativeData data = PrepareOrderedData(table, encoding);
        table = data.Table;
        encoding = data.Encoding;
        double[] x = ChartVisualEncodingOperations.PositionXValues(table, encoding);
        double[] y = ChartVisualEncodingOperations.PositionYValues(table, encoding);
        double[] baseline = NumberValuesOrConstant(table, encoding, ChartEncodingChannel.Y2, 0.0);
        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Line);
        int row = 0;
        while (row < table.RowCount)
        {
            ChartRenderStyle rowStyle = ChartVisualEncodingOperations.StyleForRow(style, table, encoding, row);
            series.AddLine(id + "-stem-" + row.ToString(System.Globalization.CultureInfo.InvariantCulture), x[row], baseline[row], x[row], y[row], rowStyle);
            row = row + 1;
        }

        ChartVisualEncodingOperations.ApplyZIndex(series, table, encoding);
        ChartVisualEncodingOperations.AddValueLabels(series, table, encoding, style, x, y);
        return series;
    }

    internal static string? TextForRow(ChartDataTable table, ChartVisualEncoding encoding, ChartEncodingChannel channel, int row)
    {
        ChartEncodingBinding binding = encoding.BindingForRow(channel, row);
        if (binding.IsConstant)
        {
            return binding.TextValue;
        }

        return ChartVisualEncodingOperations.TextForColumnRow(table.GetColumn(binding.FieldName), row);
    }

    internal static double[] NumberValues(ChartDataTable table, ChartVisualEncoding encoding, ChartEncodingChannel channel)
    {
        ValidateTableEncoding(table, encoding);
        ChartEncodingBinding binding = encoding.BindingFor(channel);
        if (binding.IsConstant)
        {
            if (binding.FieldKind != ChartDataKind.Number)
            {
                throw new System.ArgumentException("Encoded constant must be numeric.");
            }

            return ChartLineAreaChartsOperations.ConstantValues(table.RowCount, binding.NumberValue);
        }

        if (binding.FieldKind != ChartDataKind.Number)
        {
            throw new System.ArgumentException("Encoded field must be numeric.");
        }

        ChartDataColumn column = table.GetColumn(binding.FieldName);
        column.EnsureKind(ChartDataKind.Number);
        return column.CopyNumberValues();
    }

    internal static double[] NumberValuesOrConstant(ChartDataTable table, ChartVisualEncoding encoding, ChartEncodingChannel channel, double fallback)
    {
        ValidateTableEncoding(table, encoding);
        ChartLinearScale.ValidateFinite(fallback, "fallback");
        if (!encoding.HasChannel(channel))
        {
            return ChartLineAreaChartsOperations.ConstantValues(table.RowCount, fallback);
        }

        return NumberValues(table, encoding, channel);
    }

    internal static double NumberConstantOrDefault(ChartVisualEncoding encoding, ChartEncodingChannel channel, double fallback)
    {
        if (encoding == null)
        {
            throw new System.ArgumentNullException("encoding");
        }

        ChartLinearScale.ValidateFinite(fallback, "fallback");
        if (!encoding.HasChannel(channel))
        {
            return fallback;
        }

        ChartEncodingBinding binding = encoding.BindingFor(channel);
        if (!binding.IsConstant || binding.FieldKind != ChartDataKind.Number)
        {
            throw new System.ArgumentException("Encoded channel requires a numeric constant.");
        }

        return binding.NumberValue;
    }

    internal static ChartMarkerShape ShapeConstantOrDefault(ChartVisualEncoding encoding, ChartEncodingChannel channel, ChartMarkerShape fallback)
    {
        if (encoding == null)
        {
            throw new System.ArgumentNullException("encoding");
        }

        ChartRenderCommand.ValidateMarkerShape(fallback);
        if (!encoding.HasChannel(channel))
        {
            return fallback;
        }

        ChartEncodingBinding binding = encoding.BindingFor(channel);
        if (!binding.IsConstant)
        {
            throw new System.ArgumentException("Encoded shape channel requires a shape constant.");
        }

        ChartRenderCommand.ValidateMarkerShape(binding.ShapeValue);
        return binding.ShapeValue;
    }

    internal static ChartDeclarativeData PrepareOrderedData(ChartDataTable table, ChartVisualEncoding encoding)
    {
        if (!encoding.HasChannel(ChartEncodingChannel.Order))
        {
            return new ChartDeclarativeData(table, encoding);
        }

        ChartEncodingBinding binding = encoding.BindingFor(ChartEncodingChannel.Order);
        if (binding.IsConstant)
        {
            return new ChartDeclarativeData(table, encoding);
        }

        ChartDataColumn column = table.GetColumn(binding.FieldName);
        int[] indices = new int[table.RowCount];
        int row = 0;
        while (row < indices.Length)
        {
            indices[row] = row;
            row = row + 1;
        }

        int index = 1;
        while (index < indices.Length)
        {
            int value = indices[index];
            int position = index;
            while (position > 0 && CompareOrderRows(column, value, indices[position - 1]) < 0)
            {
                indices[position] = indices[position - 1];
                position = position - 1;
            }

            indices[position] = value;
            index = index + 1;
        }

        return PrepareSubsetData(table, encoding, indices);
    }

    internal static ChartDeclarativeData PrepareSubsetData(ChartDataTable table, ChartVisualEncoding encoding, int[] indices)
    {
        ChartDataTable subset = ChartVisualEncodingOperations.CopyFacetRows(table, indices);
        ChartVisualEncoding subsetEncoding = CopyEncodingForRows(encoding, indices);
        return new ChartDeclarativeData(subset, subsetEncoding);
    }

    internal static ChartVisualEncoding CopyEncodingForRows(ChartVisualEncoding encoding, int[] indices)
    {
        ChartVisualEncoding copy = encoding.Copy();
        if (encoding.Selection == null)
        {
            return copy;
        }

        ChartSelection selection = new ChartSelection(encoding.Selection.SelectionId, encoding.Selection.LinkGroup);
        int[] selected = new int[indices.Length];
        int[] highlighted = new int[indices.Length];
        int selectedCount = 0;
        int highlightedCount = 0;
        int row = 0;
        while (row < indices.Length)
        {
            ChartSelectionState state = encoding.Selection.StateForRow(indices[row]);
            if (state == ChartSelectionState.Selected || state == ChartSelectionState.SelectedHighlighted)
            {
                selected[selectedCount] = row;
                selectedCount = selectedCount + 1;
            }

            if (state == ChartSelectionState.Highlighted || state == ChartSelectionState.SelectedHighlighted)
            {
                highlighted[highlightedCount] = row;
                highlightedCount = highlightedCount + 1;
            }

            row = row + 1;
        }

        selection.SetSelectedRows(ChartDataOperationsOperations.CopyIndexBuffer(selected, selectedCount));
        selection.SetHighlightedRows(ChartDataOperationsOperations.CopyIndexBuffer(highlighted, highlightedCount));
        copy.Selection = selection;
        return copy;
    }

    internal static int CompareOrderRows(ChartDataColumn column, int left, int right)
    {
        if (column.Kind == ChartDataKind.Number)
        {
            return column.GetNumber(left).CompareTo(column.GetNumber(right));
        }

        if (column.Kind == ChartDataKind.DateTime)
        {
            return column.GetDateTime(left).CompareTo(column.GetDateTime(right));
        }

        string? leftText = ChartVisualEncodingOperations.TextForColumnRow(column, left);
        string? rightText = ChartVisualEncodingOperations.TextForColumnRow(column, right);
        return string.CompareOrdinal(leftText, rightText);
    }

    internal static void ValidateCommon(string id, ChartDataTable table, ChartVisualEncoding encoding, ChartRenderStyle style)
    {
        ChartPlot.ValidateId(id);
        ValidateTableEncoding(table, encoding);
        ChartLineAreaChartsOperations.ValidateStyle(style);
    }

    internal static void ValidateTableEncoding(ChartDataTable table, ChartVisualEncoding encoding)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        if (encoding == null)
        {
            throw new System.ArgumentNullException("encoding");
        }

        if (table.RowCount <= 0)
        {
            throw new System.ArgumentException("Declarative charts require at least one data row.");
        }

        encoding.ValidateAgainstTable(table);
    }

    internal static void RequireChannel(ChartVisualEncoding encoding, ChartEncodingChannel channel)
    {
        if (encoding == null)
        {
            throw new System.ArgumentNullException("encoding");
        }

        if (!encoding.HasChannel(channel))
        {
            throw new System.ArgumentException("Required encoding channel is missing.");
        }
    }

    internal static void ValidateMark(ChartDeclarativeMark mark)
    {
        int value = (int)mark;
        if (value < 0 || value > 19)
        {
            throw new System.ArgumentOutOfRangeException("mark");
        }
    }
}

internal class ChartDeclarativeData
{
    internal ChartDataTable Table;
    internal ChartVisualEncoding Encoding;

    internal ChartDeclarativeData(ChartDataTable table, ChartVisualEncoding encoding)
    {
        Table = table;
        Encoding = encoding;
    }
}
