namespace chart;
/// <summary>Specifies the ordering direction applied to text elements before layout.</summary>
public enum ChartTextDirection
{
    /// <summary>Preserves left-to-right text-element order.</summary>
    LeftToRight = 0,
    /// <summary>Reverses text elements within each line for deterministic right-to-left ordering without script shaping.</summary>
    RightToLeft = 1
}
