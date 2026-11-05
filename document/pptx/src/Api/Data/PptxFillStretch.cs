namespace pptx;

/// <summary>
/// Represents the inset rectangle used to stretch a picture fill.
/// </summary>
public class PptxFillStretch
{
    /// <summary>
    /// Stores the left inset in thousandths of one percent.
    /// </summary>
    public int Left;
    /// <summary>
    /// Stores the top inset in thousandths of one percent.
    /// </summary>
    public int Top;
    /// <summary>
    /// Stores the right inset in thousandths of one percent.
    /// </summary>
    public int Right;
    /// <summary>
    /// Stores the bottom inset in thousandths of one percent.
    /// </summary>
    public int Bottom;

    /// <summary>
    /// Initializes stretch settings with no inset.
    /// </summary>
    public PptxFillStretch()
    {
        Left = 0;
        Top = 0;
        Right = 0;
        Bottom = 0;
    }
}
