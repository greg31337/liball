namespace pptx;

/// <summary>
/// Represents a top or bottom bevel applied to a three-dimensional shape.
/// </summary>
public class PptxBevel
{
    /// <summary>
    /// Indicates whether the bevel is enabled.
    /// </summary>
    public bool Enabled;
    /// <summary>
    /// Stores bevel width in English Metric Units (EMUs).
    /// </summary>
    public long Width;
    /// <summary>
    /// Stores bevel height in English Metric Units (EMUs).
    /// </summary>
    public long Height;
    /// <summary>
    /// Stores the DrawingML bevel preset.
    /// </summary>
    public string Preset;

    /// <summary>
    /// Initializes a disabled bevel.
    /// </summary>
    public PptxBevel()
    {
        Enabled = false;
        Width = 0L;
        Height = 0L;
        Preset = "";
    }
}
