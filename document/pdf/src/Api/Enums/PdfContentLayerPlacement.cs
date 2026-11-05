namespace pdf;

/// <summary>
/// Specifies where an ordered content layer is drawn relative to normal page content.
/// </summary>
public enum PdfContentLayerPlacement
{
    /// <summary>
    /// Draws the layer behind normal page content.
    /// </summary>
    Background = 0,

    /// <summary>
    /// Draws the layer above normal page content and below foreground layers.
    /// </summary>
    Watermark = 1,

    /// <summary>
    /// Draws the layer above normal page content and watermark layers.
    /// </summary>
    Foreground = 2
}
