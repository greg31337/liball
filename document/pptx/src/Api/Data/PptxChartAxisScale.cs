namespace pptx;

/// <summary>
/// Represents editable numeric scale settings for a chart axis.
/// </summary>
public class PptxChartAxisScale
{
    /// <summary>
    /// Stores the minimum scale value, or an empty string for automatic scaling.
    /// </summary>
    public string Minimum;
    /// <summary>
    /// Stores the maximum scale value, or an empty string for automatic scaling.
    /// </summary>
    public string Maximum;
    /// <summary>
    /// Stores the major unit, or an empty string for automatic spacing.
    /// </summary>
    public string MajorUnit;
    /// <summary>
    /// Stores the minor unit, or an empty string for automatic spacing.
    /// </summary>
    public string MinorUnit;
    /// <summary>
    /// Stores the logarithm base, or an empty string for a linear scale.
    /// </summary>
    public string LogBase;
    /// <summary>
    /// Stores the axis crossing value, or an empty string for automatic crossing.
    /// </summary>
    public string CrossAt;
    /// <summary>
    /// Indicates whether the scale direction is reversed.
    /// </summary>
    public bool Reverse;

    /// <summary>
    /// Initializes automatic, forward axis scaling.
    /// </summary>
    public PptxChartAxisScale()
    {
        Minimum = "";
        Maximum = "";
        MajorUnit = "";
        MinorUnit = "";
        LogBase = "";
        CrossAt = "";
        Reverse = false;
    }
}
