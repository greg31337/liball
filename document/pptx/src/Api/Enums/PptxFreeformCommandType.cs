namespace pptx;

/// <summary>
/// Identifies a command in a DrawingML freeform path.
/// </summary>
public enum PptxFreeformCommandType
{
    /// <summary>
    /// Moves the current point without drawing a segment.
    /// </summary>
    MoveTo = 0,
    /// <summary>
    /// Draws a straight line to one point.
    /// </summary>
    LineTo = 1,
    /// <summary>
    /// Draws a cubic Bezier curve using two control points and an end point.
    /// </summary>
    CubicBezierTo = 2,
    /// <summary>
    /// Draws a quadratic Bezier curve using one control point and an end point.
    /// </summary>
    QuadraticBezierTo = 3,
    /// <summary>
    /// Closes the current subpath.
    /// </summary>
    Close = 4
}
