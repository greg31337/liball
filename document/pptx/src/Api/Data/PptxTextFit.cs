namespace pptx;

/// <summary>
/// Reports a dependency-free estimate of text-frame fitting.
/// </summary>
public class PptxTextFit
{
    /// <summary>
    /// Stores the number of measured text characters.
    /// </summary>
    public int CharacterCount;
    /// <summary>
    /// Stores the estimated rendered line count after wrapping.
    /// </summary>
    public int LineCount;
    /// <summary>
    /// Stores the available width after text-frame margins in English Metric Units.
    /// </summary>
    public long AvailableWidth;
    /// <summary>
    /// Stores the available height after text-frame margins in English Metric Units.
    /// </summary>
    public long AvailableHeight;
    /// <summary>
    /// Stores the estimated text width in English Metric Units.
    /// </summary>
    public long EstimatedWidth;
    /// <summary>
    /// Stores the estimated text height in English Metric Units.
    /// </summary>
    public long EstimatedHeight;
    /// <summary>
    /// Indicates estimated horizontal overflow.
    /// </summary>
    public bool HorizontalOverflow;
    /// <summary>
    /// Indicates estimated vertical overflow.
    /// </summary>
    public bool VerticalOverflow;
    /// <summary>
    /// Indicates whether the current estimated text fits in both dimensions.
    /// </summary>
    public bool Fits;
    /// <summary>
    /// Stores the active autofit mode.
    /// </summary>
    public string Autofit;
    /// <summary>
    /// Indicates whether an active autofit mode can respond to estimated overflow.
    /// </summary>
    public bool CanAutofit;

    /// <summary>
    /// Initializes an empty fitting estimate.
    /// </summary>
    public PptxTextFit()
    {
        CharacterCount = 0;
        LineCount = 0;
        AvailableWidth = 0L;
        AvailableHeight = 0L;
        EstimatedWidth = 0L;
        EstimatedHeight = 0L;
        HorizontalOverflow = false;
        VerticalOverflow = false;
        Fits = true;
        Autofit = "";
        CanAutofit = false;
    }
}
