namespace chart;
/// <summary>Creates and converts chart path command values.</summary>
public class ChartPathCommandFactory : IChartPathCommandFactory
{
    /// <summary>Initializes a stateless chart path command factory facade.</summary>
    public ChartPathCommandFactory()
    {
    }

    /// <summary>
    /// Creates a command that starts a new subpath at a point.
    /// </summary>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <returns>The resulting path command.</returns>
    public ChartPathCommand MoveTo(double x, double y)
    {
        return ChartPathCommand.MoveTo(x, y);
    }

    /// <summary>
    /// Creates a command that adds a straight segment to a point.
    /// </summary>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <returns>The resulting path command.</returns>
    public ChartPathCommand LineTo(double x, double y)
    {
        return ChartPathCommand.LineTo(x, y);
    }

    /// <summary>
    /// Creates a quadratic Bézier command with one control point and an endpoint.
    /// </summary>
    /// <param name = "controlX">The control x.</param>
    /// <param name = "controlY">The control y.</param>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <returns>The resulting path command.</returns>
    public ChartPathCommand QuadraticBezierTo(double controlX, double controlY, double x, double y)
    {
        return ChartPathCommand.QuadraticBezierTo(controlX, controlY, x, y);
    }

    /// <summary>
    /// Creates a cubic Bézier command with two control points and an endpoint.
    /// </summary>
    /// <param name = "control1X">The control1 x.</param>
    /// <param name = "control1Y">The control1 y.</param>
    /// <param name = "control2X">The control2 x.</param>
    /// <param name = "control2Y">The control2 y.</param>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <returns>The resulting path command.</returns>
    public ChartPathCommand CubicBezierTo(double control1X, double control1Y, double control2X, double control2Y, double x, double y)
    {
        return ChartPathCommand.CubicBezierTo(control1X, control1Y, control2X, control2Y, x, y);
    }

    /// <summary>
    /// Creates a command that closes the current subpath back to its starting point.
    /// </summary>
    /// <returns>The resulting path command.</returns>
    public ChartPathCommand Close()
    {
        return ChartPathCommand.Close();
    }
}
