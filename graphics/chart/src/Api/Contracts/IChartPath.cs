namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartPath"/>.</summary>
public interface IChartPath
{
    /// <summary>
    /// Starts a new subpath at the specified point.
    /// </summary>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    void MoveTo(double x, double y);
    /// <summary>
    /// Adds a straight segment from the current point to the specified point.
    /// </summary>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    void LineTo(double x, double y);
    /// <summary>
    /// Adds a quadratic Bézier segment from the current point through one control point.
    /// </summary>
    /// <param name = "controlX">The control x.</param>
    /// <param name = "controlY">The control y.</param>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    void QuadraticBezierTo(double controlX, double controlY, double x, double y);
    /// <summary>
    /// Adds a cubic Bézier segment from the current point through two control points.
    /// </summary>
    /// <param name = "control1X">The control1 x.</param>
    /// <param name = "control1Y">The control1 y.</param>
    /// <param name = "control2X">The control2 x.</param>
    /// <param name = "control2Y">The control2 y.</param>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    void CubicBezierTo(double control1X, double control1Y, double control2X, double control2Y, double x, double y);
    /// <summary>
    /// Closes the current subpath by connecting it to its starting point.
    /// </summary>
    void Close();
    /// <summary>
    /// Approximates curves with straight segments and returns one polyline per subpath.
    /// </summary>
    /// <param name = "tolerance">The maximum curve-flatness error in chart units; it must be positive.</param>
    /// <returns>The resulting path polyline array.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    ChartPathPolyline[] Flatten(double tolerance);
    /// <summary>Returns the current commands.</summary>
    /// <returns>The current field value.</returns>
    System.Collections.Generic.List<ChartPathCommand> GetCommands();
    /// <summary>Replaces the current commands.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetCommands(System.Collections.Generic.List<ChartPathCommand> value);
}
