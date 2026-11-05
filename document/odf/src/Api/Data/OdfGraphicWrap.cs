namespace odf;

/// <summary>
/// Describes text wrapping, contour, and run-through behavior.
/// </summary>
public class OdfGraphicWrap
{
    /// <summary>The wrapping mode.</summary>
    public string Mode = "";
    /// <summary>The dynamic wrapping threshold.</summary>
    public string DynamicThreshold = "";
    /// <summary>The lexical wrapped-paragraph limit.</summary>
    public string ParagraphCount = "";
    /// <summary>The preserved contour-wrapping boolean.</summary>
    public string ContourValue = "";
    /// <summary>The contour wrapping mode.</summary>
    public string ContourMode = "";
    /// <summary>The foreground or background run-through mode.</summary>
    public string RunThrough = "";
    /// <summary>How wrapping influences object positioning.</summary>
    public string InfluenceOnPosition = "";
}
