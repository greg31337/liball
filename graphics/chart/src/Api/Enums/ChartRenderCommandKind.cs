namespace chart;
/// <summary>
/// Specifies the supported render command kind values.
/// </summary>
public enum ChartRenderCommandKind
{
    /// <summary>
    /// Uses the clear option for the render command kind.
    /// </summary>
    Clear = 0,
    /// <summary>
    /// Uses the line option for the render command kind.
    /// </summary>
    Line = 1,
    /// <summary>
    /// Uses the polyline option for the render command kind.
    /// </summary>
    Polyline = 2,
    /// <summary>
    /// Uses the polygon option for the render command kind.
    /// </summary>
    Polygon = 3,
    /// <summary>
    /// Uses the rect option for the render command kind.
    /// </summary>
    Rect = 4,
    /// <summary>
    /// Uses the circle option for the render command kind.
    /// </summary>
    Circle = 5,
    /// <summary>
    /// Uses the ellipse option for the render command kind.
    /// </summary>
    Ellipse = 6,
    /// <summary>
    /// Uses the path option for the render command kind.
    /// </summary>
    Path = 7,
    /// <summary>
    /// Uses the text option for the render command kind.
    /// </summary>
    Text = 8,
    /// <summary>
    /// Uses the marker option for the render command kind.
    /// </summary>
    Marker = 9,
    /// <summary>
    /// Uses an open circular arc for the render command kind.
    /// </summary>
    Arc = 10,
    /// <summary>
    /// Uses a closed annular wedge for the render command kind.
    /// </summary>
    Wedge = 11,
    /// <summary>
    /// Places a raw bitmap for the render command kind.
    /// </summary>
    Image = 12,
    /// <summary>
    /// Begins a retained group.
    /// </summary>
    BeginGroup = 13,
    /// <summary>
    /// Ends a retained group.
    /// </summary>
    EndGroup = 14,
    /// <summary>
    /// Pushes an affine transform for following commands.
    /// </summary>
    PushTransform = 15,
    /// <summary>
    /// Restores the preceding affine transform.
    /// </summary>
    PopTransform = 16,
    /// <summary>
    /// Pushes a rectangular clip for following commands.
    /// </summary>
    PushClip = 17,
    /// <summary>
    /// Restores the preceding clip region.
    /// </summary>
    PopClip = 18
}
