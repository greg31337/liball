namespace chart;
/// <summary>
/// Specifies how sibling values divide treemap rectangles.
/// </summary>
public enum ChartTreemapLayoutKind
{
    /// <summary>
    /// Stacks sibling rectangles from top to bottom.
    /// </summary>
    Slice = 0,
    /// <summary>
    /// Places sibling rectangles from left to right.
    /// </summary>
    Dice = 1,
    /// <summary>
    /// Alternates dice and slice placement at successive hierarchy depths.
    /// </summary>
    SliceDice = 2,
    /// <summary>
    /// Groups siblings into rows chosen to reduce the worst rectangle aspect ratio.
    /// </summary>
    Squarified = 3
}
