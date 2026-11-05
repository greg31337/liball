namespace chart;
/// <summary>
/// Specifies the supported path command kind values.
/// </summary>
public enum ChartPathCommandKind
{
    /// <summary>
    /// Uses the move to option for the path command kind.
    /// </summary>
    MoveTo = 0,
    /// <summary>
    /// Uses the line to option for the path command kind.
    /// </summary>
    LineTo = 1,
    /// <summary>
    /// Uses the quadratic bezier to option for the path command kind.
    /// </summary>
    QuadraticBezierTo = 2,
    /// <summary>
    /// Uses the cubic bezier to option for the path command kind.
    /// </summary>
    CubicBezierTo = 3,
    /// <summary>
    /// Uses the close option for the path command kind.
    /// </summary>
    Close = 4
}
