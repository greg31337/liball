namespace chart;
/// <summary>
/// Represents a path used by the chart model or rendering pipeline.
/// </summary>
public class ChartPath : IChartPath
{
    internal System.Collections.Generic.List<ChartPathCommand> Commands;
    /// <summary>
    /// Initializes an empty path with library defaults.
    /// </summary>
    public ChartPath()
    {
        Commands = new System.Collections.Generic.List<ChartPathCommand>();
    }

    /// <summary>
    /// Starts a new subpath at the specified point.
    /// </summary>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    public void MoveTo(double x, double y)
    {
        Commands.Add(ChartPathCommand.MoveTo(x, y));
    }

    /// <summary>
    /// Adds a straight segment from the current point to the specified point.
    /// </summary>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    public void LineTo(double x, double y)
    {
        Commands.Add(ChartPathCommand.LineTo(x, y));
    }

    /// <summary>
    /// Adds a quadratic Bézier segment from the current point through one control point.
    /// </summary>
    /// <param name = "controlX">The control x.</param>
    /// <param name = "controlY">The control y.</param>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    public void QuadraticBezierTo(double controlX, double controlY, double x, double y)
    {
        Commands.Add(ChartPathCommand.QuadraticBezierTo(controlX, controlY, x, y));
    }

    /// <summary>
    /// Adds a cubic Bézier segment from the current point through two control points.
    /// </summary>
    /// <param name = "control1X">The control1 x.</param>
    /// <param name = "control1Y">The control1 y.</param>
    /// <param name = "control2X">The control2 x.</param>
    /// <param name = "control2Y">The control2 y.</param>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    public void CubicBezierTo(double control1X, double control1Y, double control2X, double control2Y, double x, double y)
    {
        Commands.Add(ChartPathCommand.CubicBezierTo(control1X, control1Y, control2X, control2Y, x, y));
    }

    /// <summary>
    /// Closes the current subpath by connecting it to its starting point.
    /// </summary>
    public void Close()
    {
        Commands.Add(ChartPathCommand.Close());
    }

    /// <summary>
    /// Approximates curves with straight segments and returns one polyline per subpath.
    /// </summary>
    /// <param name = "tolerance">The maximum curve-flatness error in chart units; it must be positive.</param>
    /// <returns>The resulting path polyline array.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartPathPolyline[] Flatten(double tolerance)
    {
        if (tolerance <= 0.0 || double.IsNaN(tolerance) || double.IsInfinity(tolerance))
        {
            throw new System.ArgumentOutOfRangeException("tolerance");
        }

        System.Collections.Generic.List<ChartPathPolyline> polylines = new System.Collections.Generic.List<ChartPathPolyline>();
        System.Collections.Generic.List<ChartPoint> currentPoints = new System.Collections.Generic.List<ChartPoint>();
        ChartPoint? currentPoint = null;
        ChartPoint? startPoint = null;
        double toleranceSquared = tolerance * tolerance;
        int index = 0;
        while (index < Commands.Count)
        {
            ChartPathCommand command = Commands[index];
            if (command.Kind == ChartPathCommandKind.MoveTo)
            {
                FinishOpenPolyline(polylines, currentPoints);
                currentPoints = new System.Collections.Generic.List<ChartPoint>();
                currentPoint = new ChartPoint(command.X1, command.Y1);
                startPoint = currentPoint.Copy();
                currentPoints.Add(currentPoint.Copy());
            }
            else if (command.Kind == ChartPathCommandKind.LineTo)
            {
                if (currentPoint == null)
                {
                    currentPoint = new ChartPoint(command.X1, command.Y1);
                    startPoint = currentPoint.Copy();
                    currentPoints.Add(currentPoint.Copy());
                }
                else
                {
                    currentPoint = new ChartPoint(command.X1, command.Y1);
                    currentPoints.Add(currentPoint.Copy());
                }
            }
            else if (command.Kind == ChartPathCommandKind.QuadraticBezierTo)
            {
                if (currentPoint == null)
                {
                    currentPoint = new ChartPoint(command.X2, command.Y2);
                    startPoint = currentPoint.Copy();
                    currentPoints.Add(currentPoint.Copy());
                }
                else
                {
                    ChartPoint control = new ChartPoint(command.X1, command.Y1);
                    ChartPoint end = new ChartPoint(command.X2, command.Y2);
                    FlattenQuadraticRecursive(currentPoint, control, end, toleranceSquared, 0, currentPoints);
                    currentPoint = end.Copy();
                }
            }
            else if (command.Kind == ChartPathCommandKind.CubicBezierTo)
            {
                if (currentPoint == null)
                {
                    currentPoint = new ChartPoint(command.X3, command.Y3);
                    startPoint = currentPoint.Copy();
                    currentPoints.Add(currentPoint.Copy());
                }
                else
                {
                    ChartPoint control1 = new ChartPoint(command.X1, command.Y1);
                    ChartPoint control2 = new ChartPoint(command.X2, command.Y2);
                    ChartPoint end = new ChartPoint(command.X3, command.Y3);
                    FlattenCubicRecursive(currentPoint, control1, control2, end, toleranceSquared, 0, currentPoints);
                    currentPoint = end.Copy();
                }
            }
            else if (command.Kind == ChartPathCommandKind.Close)
            {
                if (currentPoints.Count > 0 && startPoint != null)
                {
                    if (!PointsEqual(currentPoint, startPoint))
                    {
                        currentPoints.Add(startPoint.Copy());
                    }

                    polylines.Add(new ChartPathPolyline(currentPoints.ToArray(), true));
                    currentPoints = new System.Collections.Generic.List<ChartPoint>();
                    currentPoint = null;
                    startPoint = null;
                }
            }

            index = index + 1;
        }

        FinishOpenPolyline(polylines, currentPoints);
        return polylines.ToArray();
    }

    internal static void FinishOpenPolyline(System.Collections.Generic.List<ChartPathPolyline> polylines, System.Collections.Generic.List<ChartPoint> currentPoints)
    {
        if (currentPoints.Count > 0)
        {
            polylines.Add(new ChartPathPolyline(currentPoints.ToArray(), false));
        }
    }

    internal static void FlattenQuadraticRecursive(ChartPoint start, ChartPoint control, ChartPoint end, double toleranceSquared, int depth, System.Collections.Generic.List<ChartPoint> output)
    {
        if (depth >= 16 || QuadraticFlatnessSquared(start, control, end) <= toleranceSquared)
        {
            output.Add(end.Copy());
            return;
        }

        ChartPoint startControl = ChartPoint.Lerp(start, control, 0.5);
        ChartPoint controlEnd = ChartPoint.Lerp(control, end, 0.5);
        ChartPoint middle = ChartPoint.Lerp(startControl, controlEnd, 0.5);
        FlattenQuadraticRecursive(start, startControl, middle, toleranceSquared, depth + 1, output);
        FlattenQuadraticRecursive(middle, controlEnd, end, toleranceSquared, depth + 1, output);
    }

    internal static void FlattenCubicRecursive(ChartPoint start, ChartPoint control1, ChartPoint control2, ChartPoint end, double toleranceSquared, int depth, System.Collections.Generic.List<ChartPoint> output)
    {
        if (depth >= 16 || CubicFlatnessSquared(start, control1, control2, end) <= toleranceSquared)
        {
            output.Add(end.Copy());
            return;
        }

        ChartPoint startControl1 = ChartPoint.Lerp(start, control1, 0.5);
        ChartPoint control1Control2 = ChartPoint.Lerp(control1, control2, 0.5);
        ChartPoint control2End = ChartPoint.Lerp(control2, end, 0.5);
        ChartPoint leftControl = ChartPoint.Lerp(startControl1, control1Control2, 0.5);
        ChartPoint rightControl = ChartPoint.Lerp(control1Control2, control2End, 0.5);
        ChartPoint middle = ChartPoint.Lerp(leftControl, rightControl, 0.5);
        FlattenCubicRecursive(start, startControl1, leftControl, middle, toleranceSquared, depth + 1, output);
        FlattenCubicRecursive(middle, rightControl, control2End, end, toleranceSquared, depth + 1, output);
    }

    internal static double QuadraticFlatnessSquared(ChartPoint start, ChartPoint control, ChartPoint end)
    {
        return DistancePointToLineSquared(control, start, end);
    }

    internal static double CubicFlatnessSquared(ChartPoint start, ChartPoint control1, ChartPoint control2, ChartPoint end)
    {
        double first = DistancePointToLineSquared(control1, start, end);
        double second = DistancePointToLineSquared(control2, start, end);
        return System.Math.Max(first, second);
    }

    internal static double DistancePointToLineSquared(ChartPoint point, ChartPoint lineStart, ChartPoint lineEnd)
    {
        double dx = lineEnd.X - lineStart.X;
        double dy = lineEnd.Y - lineStart.Y;
        double lengthSquared = dx * dx + dy * dy;
        if (lengthSquared == 0.0)
        {
            return ChartRasterCanvas.DistanceSquared(point.X, point.Y, lineStart.X, lineStart.Y);
        }

        double t = ((point.X - lineStart.X) * dx + (point.Y - lineStart.Y) * dy) / lengthSquared;
        double nearestX = lineStart.X + dx * t;
        double nearestY = lineStart.Y + dy * t;
        return ChartRasterCanvas.DistanceSquared(point.X, point.Y, nearestX, nearestY);
    }

    internal static bool PointsEqual(ChartPoint? first, ChartPoint? second)
    {
        if (first == null || second == null)
        {
            return false;
        }

        return first.X == second.X && first.Y == second.Y;
    }

    /// <summary>Returns the current commands.</summary>
    /// <returns>The current field value.</returns>
    public System.Collections.Generic.List<ChartPathCommand> GetCommands()
    {
        return Commands;
    }

    /// <summary>Replaces the current commands.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetCommands(System.Collections.Generic.List<ChartPathCommand> value)
    {
        Commands = value;
    }
}
