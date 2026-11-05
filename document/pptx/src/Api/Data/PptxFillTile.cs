namespace pptx;

/// <summary>
/// Represents the placement and scaling settings used to tile a picture fill.
/// </summary>
public class PptxFillTile
{
    /// <summary>
    /// Stores the horizontal tile offset in English Metric Units (EMUs).
    /// </summary>
    public long OffsetX;
    /// <summary>
    /// Stores the vertical tile offset in English Metric Units (EMUs).
    /// </summary>
    public long OffsetY;
    /// <summary>
    /// Stores the horizontal scale in thousandths of one percent.
    /// </summary>
    public int ScaleX;
    /// <summary>
    /// Stores the vertical scale in thousandths of one percent.
    /// </summary>
    public int ScaleY;
    /// <summary>
    /// Stores the DrawingML tile alignment value.
    /// </summary>
    public string Alignment;
    /// <summary>
    /// Stores the DrawingML tile flip value.
    /// </summary>
    public string Flip;

    /// <summary>
    /// Initializes tile settings with full scale and no offset.
    /// </summary>
    public PptxFillTile()
    {
        OffsetX = 0L;
        OffsetY = 0L;
        ScaleX = 100000;
        ScaleY = 100000;
        Alignment = "";
        Flip = "";
    }
}
