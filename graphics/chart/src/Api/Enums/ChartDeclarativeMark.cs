namespace chart;
/// <summary>
/// Specifies the supported declarative mark values.
/// </summary>
public enum ChartDeclarativeMark
{
    /// <summary>
    /// Uses the line option for the declarative mark.
    /// </summary>
    Line = 0,
    /// <summary>
    /// Uses the area option for the declarative mark.
    /// </summary>
    Area = 1,
    /// <summary>
    /// Uses the scatter option for the declarative mark.
    /// </summary>
    Scatter = 2,
    /// <summary>
    /// Uses the bubble option for the declarative mark.
    /// </summary>
    Bubble = 3,
    /// <summary>
    /// Uses the vertical bar option for the declarative mark.
    /// </summary>
    VerticalBar = 4,
    /// <summary>
    /// Uses the horizontal bar option for the declarative mark.
    /// </summary>
    HorizontalBar = 5,
    /// <summary>
    /// Uses the range bar option for the declarative mark.
    /// </summary>
    RangeBar = 6,
    /// <summary>Uses a general point mark with the configured shape.</summary>
    Point = 7,
    /// <summary>Uses a circular point mark.</summary>
    Circle = 8,
    /// <summary>Uses a square point mark.</summary>
    Square = 9,
    /// <summary>Uses rectangles bounded by x, y, x2, and y2.</summary>
    Rect = 10,
    /// <summary>Uses rules bounded by x, y, x2, and y2.</summary>
    Rule = 11,
    /// <summary>Uses positioned text from the text channel.</summary>
    Text = 12,
    /// <summary>Uses centered vertical tick marks.</summary>
    Tick = 13,
    /// <summary>Uses connected line segments whose widths can vary by row.</summary>
    Trail = 14,
    /// <summary>Uses middle-step line interpolation.</summary>
    StepLine = 15,
    /// <summary>Uses spline line interpolation.</summary>
    SplineLine = 16,
    /// <summary>Uses an area bounded by y and y2.</summary>
    RangeArea = 17,
    /// <summary>Uses stems and point markers from a baseline.</summary>
    Lollipop = 18,
    /// <summary>Uses stem lines from a baseline.</summary>
    Stem = 19
}
