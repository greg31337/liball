namespace chart;
/// <summary>
/// Represents a cartesian transform used by the chart model or rendering pipeline.
/// </summary>
public class ChartCartesianTransform : IChartCartesianTransform
{
    internal ChartRect PlotArea;
    internal ChartRange XDomain;
    internal ChartRange YDomain;
    internal double[] XBreakMinimums;
    internal double[] XBreakMaximums;
    internal int XBreakCount;
    internal double[] YBreakMinimums;
    internal double[] YBreakMaximums;
    internal int YBreakCount;
    internal ChartRect UnconstrainedPlotArea;
    /// <summary>
    /// Initializes a cartesian transform from the supplied plot area, x domain, and y domain.
    /// </summary>
    /// <param name = "plotArea">The rectangle available for plotting data.</param>
    /// <param name = "xDomain">The x domain.</param>
    /// <param name = "yDomain">The y domain.</param>
    public ChartCartesianTransform(ChartRect plotArea, ChartRange xDomain, ChartRange yDomain)
    {
        ChartAxis.ValidatePlotArea(plotArea);
        ChartAxis.ValidateDomain(xDomain);
        ChartAxis.ValidateDomain(yDomain);
        PlotArea = ChartPlot.CopyRect(plotArea);
        XDomain = new ChartRange(xDomain.Minimum, xDomain.Maximum);
        YDomain = new ChartRange(yDomain.Minimum, yDomain.Maximum);
        XBreakMinimums = new double[0];
        XBreakMaximums = new double[0];
        XBreakCount = 0;
        YBreakMinimums = new double[0];
        YBreakMaximums = new double[0];
        YBreakCount = 0;
        UnconstrainedPlotArea = ChartPlot.CopyRect(plotArea);
    }

    internal static ChartCartesianTransform FromAxes(ChartAxis xAxis, ChartAxis yAxis)
    {
        if (xAxis == null)
        {
            throw new System.ArgumentNullException("xAxis");
        }

        if (yAxis == null)
        {
            throw new System.ArgumentNullException("yAxis");
        }

        if (xAxis.Kind != ChartAxisKind.X || yAxis.Kind != ChartAxisKind.Y)
        {
            throw new System.ArgumentException("Cartesian transforms require x and y axes.");
        }

        ChartCartesianTransform transform = new ChartCartesianTransform(xAxis.PlotArea, xAxis.Domain, yAxis.Domain);
        transform.XBreakMinimums = CopyBreakValues(xAxis.BreakMinimums, xAxis.BreakCount);
        transform.XBreakMaximums = CopyBreakValues(xAxis.BreakMaximums, xAxis.BreakCount);
        transform.XBreakCount = xAxis.BreakCount;
        transform.YBreakMinimums = CopyBreakValues(yAxis.BreakMinimums, yAxis.BreakCount);
        transform.YBreakMaximums = CopyBreakValues(yAxis.BreakMaximums, yAxis.BreakCount);
        transform.YBreakCount = yAxis.BreakCount;
        return transform;
    }

    /// <summary>
    /// Maps x through the configured input and output domains.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting double.</returns>
    public double MapX(double value)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        double amount = ChartAxisBreakOperations.Normalize(value, XDomain, XBreakMinimums, XBreakMaximums, XBreakCount);
        return PlotArea.Left() + PlotArea.Width * amount;
    }

    /// <summary>
    /// Maps y through the configured input and output domains.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting double.</returns>
    public double MapY(double value)
    {
        ChartLinearScale.ValidateFinite(value, "value");
        double amount = ChartAxisBreakOperations.Normalize(value, YDomain, YBreakMinimums, YBreakMaximums, YBreakCount);
        return PlotArea.Bottom() - PlotArea.Height * amount;
    }

    /// <summary>
    /// Maps point through the configured input and output domains.
    /// </summary>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <returns>The resulting point.</returns>
    public ChartPoint MapPoint(double x, double y)
    {
        return new ChartPoint(MapX(x), MapY(y));
    }

    /// <summary>
    /// Maps points through the configured input and output domains.
    /// </summary>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <returns>The resulting point array.</returns>
    public ChartPoint[] MapPoints(double[] xValues, double[] yValues)
    {
        ValidateValueArrays(xValues, yValues, 1);
        ChartPoint[] points = new ChartPoint[xValues.Length];
        int index = 0;
        while (index < xValues.Length)
        {
            points[index] = MapPoint(xValues[index], yValues[index]);
            index = index + 1;
        }

        return points;
    }

    /// <summary>
    /// Applies the invert x operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "x">The x.</param>
    /// <returns>The resulting double.</returns>
    public double InvertX(double x)
    {
        ChartLinearScale.ValidateFinite(x, "x");
        double amount = (x - PlotArea.Left()) / PlotArea.Width;
        return ChartAxisBreakOperations.Denormalize(amount, XDomain, XBreakMinimums, XBreakMaximums, XBreakCount);
    }

    /// <summary>
    /// Applies the invert y operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "y">The y.</param>
    /// <returns>The resulting double.</returns>
    public double InvertY(double y)
    {
        ChartLinearScale.ValidateFinite(y, "y");
        double amount = (PlotArea.Bottom() - y) / PlotArea.Height;
        return ChartAxisBreakOperations.Denormalize(amount, YDomain, YBreakMinimums, YBreakMaximums, YBreakCount);
    }

    /// <summary>
    /// Determines whether this value contains data point.
    /// </summary>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public bool ContainsDataPoint(double x, double y)
    {
        ChartLinearScale.ValidateFinite(x, "x");
        ChartLinearScale.ValidateFinite(y, "y");
        return XDomain.Contains(x) && YDomain.Contains(y) && ChartAxisBreakOperations.IsVisible(x, XBreakMinimums, XBreakMaximums, XBreakCount) && ChartAxisBreakOperations.IsVisible(y, YBreakMinimums, YBreakMaximums, YBreakCount);
    }

    /// <summary>
    /// Determines whether this value contains plot point.
    /// </summary>
    /// <param name = "point">The point.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public bool ContainsPlotPoint(ChartPoint point)
    {
        if (point == null)
        {
            throw new System.ArgumentNullException("point");
        }

        return PlotArea.ContainsPoint(point);
    }

    internal ChartCartesianLineClip ClipDataLine(double x1, double y1, double x2, double y2)
    {
        return ClipPlotLine(MapPoint(x1, y1), MapPoint(x2, y2));
    }

    internal ChartCartesianLineClip ClipPlotLine(ChartPoint start, ChartPoint end)
    {
        if (start == null)
        {
            throw new System.ArgumentNullException("start");
        }

        if (end == null)
        {
            throw new System.ArgumentNullException("end");
        }

        double x1 = start.X;
        double y1 = start.Y;
        double x2 = end.X;
        double y2 = end.Y;
        int code1 = OutCode(x1, y1, PlotArea);
        int code2 = OutCode(x2, y2, PlotArea);
        while (true)
        {
            if ((code1 | code2) == 0)
            {
                return new ChartCartesianLineClip(true, new ChartPoint(x1, y1), new ChartPoint(x2, y2));
            }

            if ((code1 & code2) != 0)
            {
                return new ChartCartesianLineClip(false, new ChartPoint(x1, y1), new ChartPoint(x2, y2));
            }

            int codeOut = code1;
            if (codeOut == 0)
            {
                codeOut = code2;
            }

            ChartPoint intersection = IntersectLineOutCode(x1, y1, x2, y2, codeOut, PlotArea);
            if (codeOut == code1)
            {
                x1 = intersection.X;
                y1 = intersection.Y;
                code1 = OutCode(x1, y1, PlotArea);
            }
            else
            {
                x2 = intersection.X;
                y2 = intersection.Y;
                code2 = OutCode(x2, y2, PlotArea);
            }
        }
    }

    /// <summary>
    /// Applies the clip plot polygon operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "points">The points.</param>
    /// <returns>The resulting point array.</returns>
    public ChartPoint[] ClipPlotPolygon(ChartPoint[] points)
    {
        ChartRenderCommand.ValidatePoints(points, 3);
        ChartPoint[] output = ChartRenderCommand.CopyPoints(points);
        output = ClipPolygonEdge(output, PlotArea, 0);
        output = ClipPolygonEdge(output, PlotArea, 1);
        output = ClipPolygonEdge(output, PlotArea, 2);
        output = ClipPolygonEdge(output, PlotArea, 3);
        return output;
    }

    internal static ChartPoint[] ClipPolygonEdge(ChartPoint[] points, ChartRect rect, int edge)
    {
        if (points == null)
        {
            throw new System.ArgumentNullException("points");
        }

        ChartAxis.ValidatePlotArea(rect);
        System.Collections.Generic.List<ChartPoint> output = new System.Collections.Generic.List<ChartPoint>();
        if (points.Length == 0)
        {
            return output.ToArray();
        }

        ChartPoint previous = points[points.Length - 1];
        bool previousInside = IsInsideEdge(previous, rect, edge);
        int index = 0;
        while (index < points.Length)
        {
            ChartPoint current = points[index];
            bool currentInside = IsInsideEdge(current, rect, edge);
            if (currentInside)
            {
                if (!previousInside)
                {
                    output.Add(IntersectPolygonEdge(previous, current, rect, edge));
                }

                output.Add(current.Copy());
            }
            else if (previousInside)
            {
                output.Add(IntersectPolygonEdge(previous, current, rect, edge));
            }

            previous = current;
            previousInside = currentInside;
            index = index + 1;
        }

        return output.ToArray();
    }

    internal static bool IsInsideEdge(ChartPoint point, ChartRect rect, int edge)
    {
        if (point == null)
        {
            throw new System.ArgumentNullException("point");
        }

        if (edge == 0)
        {
            return point.X >= rect.Left();
        }

        if (edge == 1)
        {
            return point.X <= rect.Right();
        }

        if (edge == 2)
        {
            return point.Y >= rect.Top();
        }

        if (edge == 3)
        {
            return point.Y <= rect.Bottom();
        }

        throw new System.ArgumentOutOfRangeException("edge");
    }

    internal static ChartPoint IntersectPolygonEdge(ChartPoint start, ChartPoint end, ChartRect rect, int edge)
    {
        if (start == null)
        {
            throw new System.ArgumentNullException("start");
        }

        if (end == null)
        {
            throw new System.ArgumentNullException("end");
        }

        double dx = end.X - start.X;
        double dy = end.Y - start.Y;
        if (edge == 0)
        {
            return new ChartPoint(rect.Left(), start.Y + dy * (rect.Left() - start.X) / dx);
        }

        if (edge == 1)
        {
            return new ChartPoint(rect.Right(), start.Y + dy * (rect.Right() - start.X) / dx);
        }

        if (edge == 2)
        {
            return new ChartPoint(start.X + dx * (rect.Top() - start.Y) / dy, rect.Top());
        }

        if (edge == 3)
        {
            return new ChartPoint(start.X + dx * (rect.Bottom() - start.Y) / dy, rect.Bottom());
        }

        throw new System.ArgumentOutOfRangeException("edge");
    }

    internal static ChartPoint IntersectLineOutCode(double x1, double y1, double x2, double y2, int codeOut, ChartRect rect)
    {
        double x = 0.0;
        double y = 0.0;
        if ((codeOut & 4) != 0)
        {
            x = x1 + (x2 - x1) * (rect.Top() - y1) / (y2 - y1);
            y = rect.Top();
        }
        else if ((codeOut & 8) != 0)
        {
            x = x1 + (x2 - x1) * (rect.Bottom() - y1) / (y2 - y1);
            y = rect.Bottom();
        }
        else if ((codeOut & 2) != 0)
        {
            y = y1 + (y2 - y1) * (rect.Right() - x1) / (x2 - x1);
            x = rect.Right();
        }
        else if ((codeOut & 1) != 0)
        {
            y = y1 + (y2 - y1) * (rect.Left() - x1) / (x2 - x1);
            x = rect.Left();
        }

        return new ChartPoint(x, y);
    }

    internal static int OutCode(double x, double y, ChartRect rect)
    {
        int code = 0;
        if (x < rect.Left())
        {
            code = code | 1;
        }
        else if (x > rect.Right())
        {
            code = code | 2;
        }

        if (y < rect.Top())
        {
            code = code | 4;
        }
        else if (y > rect.Bottom())
        {
            code = code | 8;
        }

        return code;
    }

    internal static void ValidateValueArrays(double[] xValues, double[] yValues, int minimumCount)
    {
        if (xValues == null)
        {
            throw new System.ArgumentNullException("xValues");
        }

        if (yValues == null)
        {
            throw new System.ArgumentNullException("yValues");
        }

        if (xValues.Length != yValues.Length || xValues.Length < minimumCount)
        {
            throw new System.ArgumentException("Cartesian coordinate arrays must have the same required length.");
        }

        int index = 0;
        while (index < xValues.Length)
        {
            ChartLinearScale.ValidateFinite(xValues[index], "xValues");
            ChartLinearScale.ValidateFinite(yValues[index], "yValues");
            index = index + 1;
        }
    }

    internal static double[] CopyBreakValues(double[] values, int count)
    {
        double[] copy = new double[count];
        System.Array.Copy(values, copy, count);
        return copy;
    }

    /// <summary>Constrains the plot area to a fixed ratio of horizontal to vertical data scale.</summary>
    /// <param name = "ratio">The positive horizontal-to-vertical data scale ratio.</param>
    public void SetFixedAspectRatio(double ratio)
    {
        ChartRenderStyle.ValidatePositive(ratio, "ratio");
        double xLength = ChartAxisBreakOperations.EffectiveLength(XDomain, XBreakMinimums, XBreakMaximums, XBreakCount);
        double yLength = ChartAxisBreakOperations.EffectiveLength(YDomain, YBreakMinimums, YBreakMaximums, YBreakCount);
        double desired = ratio * xLength / yLength;
        double available = UnconstrainedPlotArea.Width / UnconstrainedPlotArea.Height;
        if (available > desired)
        {
            double width = UnconstrainedPlotArea.Height * desired;
            double x = UnconstrainedPlotArea.X + (UnconstrainedPlotArea.Width - width) * 0.5;
            PlotArea = new ChartRect(x, UnconstrainedPlotArea.Y, width, UnconstrainedPlotArea.Height);
        }
        else
        {
            double height = UnconstrainedPlotArea.Width / desired;
            double y = UnconstrainedPlotArea.Y + (UnconstrainedPlotArea.Height - height) * 0.5;
            PlotArea = new ChartRect(UnconstrainedPlotArea.X, y, UnconstrainedPlotArea.Width, height);
        }
    }

    /// <summary>Restores the unconstrained plot area.</summary>
    public void ClearFixedAspectRatio()
    {
        PlotArea = ChartPlot.CopyRect(UnconstrainedPlotArea);
    }

    /// <summary>Returns the current plot area.</summary>
    /// <returns>The current field value.</returns>
    public ChartRect GetPlotArea()
    {
        return PlotArea;
    }

    /// <summary>Replaces the current plot area.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetPlotArea(ChartRect value)
    {
        ChartAxis.ValidatePlotArea(value);
        PlotArea = ChartPlot.CopyRect(value);
        UnconstrainedPlotArea = ChartPlot.CopyRect(value);
    }

    /// <summary>Returns the current x domain.</summary>
    /// <returns>The current field value.</returns>
    public ChartRange GetXDomain()
    {
        return XDomain;
    }

    /// <summary>Replaces the current x domain.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetXDomain(ChartRange value)
    {
        XDomain = value;
    }

    /// <summary>Returns the current y domain.</summary>
    /// <returns>The current field value.</returns>
    public ChartRange GetYDomain()
    {
        return YDomain;
    }

    /// <summary>Replaces the current y domain.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetYDomain(ChartRange value)
    {
        YDomain = value;
    }
}
