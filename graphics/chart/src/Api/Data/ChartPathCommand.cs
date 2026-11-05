namespace chart;
/// <summary>
/// Represents a path command used by the chart model or rendering pipeline.
/// </summary>
public class ChartPathCommand
{
    /// <summary>
    /// The kind used by this path command.
    /// </summary>
    public ChartPathCommandKind Kind;
    /// <summary>
    /// The first horizontal coordinate used by the command.
    /// </summary>
    public double X1;
    /// <summary>
    /// The first vertical coordinate used by the command.
    /// </summary>
    public double Y1;
    /// <summary>
    /// The second horizontal coordinate used by the command.
    /// </summary>
    public double X2;
    /// <summary>
    /// The second vertical coordinate used by the command.
    /// </summary>
    public double Y2;
    /// <summary>
    /// The x3 used by this path command.
    /// </summary>
    public double X3;
    /// <summary>
    /// The y3 used by this path command.
    /// </summary>
    public double Y3;
    /// <summary>
    /// Initializes a path command from the supplied kind, x1, y1, x2, y2, x3, and y3.
    /// </summary>
    /// <param name = "kind">The kind.</param>
    /// <param name = "x1">The x1.</param>
    /// <param name = "y1">The y1.</param>
    /// <param name = "x2">The x2.</param>
    /// <param name = "y2">The y2.</param>
    /// <param name = "x3">The x3.</param>
    /// <param name = "y3">The y3.</param>
    public ChartPathCommand(ChartPathCommandKind kind, double x1, double y1, double x2, double y2, double x3, double y3)
    {
        Kind = kind;
        X1 = x1;
        Y1 = y1;
        X2 = x2;
        Y2 = y2;
        X3 = x3;
        Y3 = y3;
    }

    internal static ChartPathCommand MoveTo(double x, double y)
    {
        return new ChartPathCommand(ChartPathCommandKind.MoveTo, x, y, 0.0, 0.0, 0.0, 0.0);
    }

    internal static ChartPathCommand LineTo(double x, double y)
    {
        return new ChartPathCommand(ChartPathCommandKind.LineTo, x, y, 0.0, 0.0, 0.0, 0.0);
    }

    internal static ChartPathCommand QuadraticBezierTo(double controlX, double controlY, double x, double y)
    {
        return new ChartPathCommand(ChartPathCommandKind.QuadraticBezierTo, controlX, controlY, x, y, 0.0, 0.0);
    }

    internal static ChartPathCommand CubicBezierTo(double control1X, double control1Y, double control2X, double control2Y, double x, double y)
    {
        return new ChartPathCommand(ChartPathCommandKind.CubicBezierTo, control1X, control1Y, control2X, control2Y, x, y);
    }

    internal static ChartPathCommand Close()
    {
        return new ChartPathCommand(ChartPathCommandKind.Close, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0);
    }
}
