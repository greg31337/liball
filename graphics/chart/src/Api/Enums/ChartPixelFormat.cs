namespace chart;
/// <summary>
/// Specifies the supported pixel format values.
/// </summary>
public enum ChartPixelFormat
{
    /// <summary>
    /// Uses the rgba32 option for the pixel format.
    /// </summary>
    Rgba32 = 0,
    /// <summary>
    /// Uses the bgra32 option for the pixel format.
    /// </summary>
    Bgra32 = 1,
    /// <summary>
    /// Uses the rgb24 option for the pixel format.
    /// </summary>
    Rgb24 = 2,
    /// <summary>
    /// Uses the grayscale8 option for the pixel format.
    /// </summary>
    Grayscale8 = 3,
    /// <summary>
    /// Uses the alpha8 option for the pixel format.
    /// </summary>
    Alpha8 = 4,
    /// <summary>
    /// Uses the indexed8 option for the pixel format.
    /// </summary>
    Indexed8 = 5
}
