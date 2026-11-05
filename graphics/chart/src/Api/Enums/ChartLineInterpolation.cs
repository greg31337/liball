namespace chart;
/// <summary>
/// Specifies the supported line interpolation values.
/// </summary>
public enum ChartLineInterpolation
{
    /// <summary>
    /// Uses the linear option for the line interpolation.
    /// </summary>
    Linear = 0,
    /// <summary>
    /// Uses the step before option for the line interpolation.
    /// </summary>
    StepBefore = 1,
    /// <summary>
    /// Uses the step after option for the line interpolation.
    /// </summary>
    StepAfter = 2,
    /// <summary>
    /// Uses the step middle option for the line interpolation.
    /// </summary>
    StepMiddle = 3,
    /// <summary>
    /// Uses the spline option for the line interpolation.
    /// </summary>
    Spline = 4,
    /// <summary>
    /// Uses a Catmull-Rom cubic spline through every input point.
    /// </summary>
    CatmullRom = 5,
    /// <summary>
    /// Uses a shape-preserving monotone cubic spline for increasing horizontal values.
    /// </summary>
    MonotoneCubic = 6,
    /// <summary>
    /// Uses a twice-continuously-differentiable natural cubic spline for increasing horizontal values.
    /// </summary>
    NaturalCubic = 7
}
