namespace pptx;

/// <summary>
/// Describes typed DrawingML body properties for a text frame.
/// </summary>
public class PptxTextFrameBody
{
    /// <summary>
    /// Identifies text frames without automatic fitting.
    /// </summary>
    public const string AutofitNone = "none";
    /// <summary>
    /// Identifies text frames that reduce text to fit their shape.
    /// </summary>
    public const string AutofitNormal = "normal";
    /// <summary>
    /// Identifies text frames whose shape grows to fit their text.
    /// </summary>
    public const string AutofitShape = "shape";
    /// <summary>
    /// Stores the text insets in English Metric Units.
    /// </summary>
    public PptxMargins Margins;
    /// <summary>
    /// Stores the vertical anchor value such as <c>t</c>, <c>ctr</c>, or <c>b</c>.
    /// </summary>
    public string VerticalAlignment;
    /// <summary>
    /// Indicates whether text is centered horizontally around the anchor point.
    /// </summary>
    public bool CenteredAnchor;
    /// <summary>
    /// Stores <c>none</c>, <c>normal</c>, <c>shape</c>, or an empty inherited value.
    /// </summary>
    public string Autofit;
    /// <summary>
    /// Stores the normal-autofit font scale in thousandths of one percent.
    /// </summary>
    public int AutofitFontScale;
    /// <summary>
    /// Stores the normal-autofit line-spacing reduction in thousandths of one percent.
    /// </summary>
    public int AutofitLineSpacingReduction;
    /// <summary>
    /// Stores the wrapping value, normally <c>square</c> or <c>none</c>.
    /// </summary>
    public string Wrapping;
    /// <summary>
    /// Stores the horizontal overflow behavior.
    /// </summary>
    public string HorizontalOverflow;
    /// <summary>
    /// Stores the vertical overflow behavior.
    /// </summary>
    public string VerticalOverflow;
    /// <summary>
    /// Stores the vertical text mode.
    /// </summary>
    public string VerticalText;
    /// <summary>
    /// Stores text rotation in DrawingML angle units.
    /// </summary>
    public int Rotation;
    /// <summary>
    /// Indicates whether glyphs remain upright when the shape rotates.
    /// </summary>
    public bool Upright;

    /// <summary>
    /// Initializes body properties using DrawingML defaults.
    /// </summary>
    public PptxTextFrameBody()
    {
        Margins = new PptxMargins(91440L, 45720L, 91440L, 45720L);
        VerticalAlignment = "t";
        CenteredAnchor = false;
        Autofit = "";
        AutofitFontScale = 100000;
        AutofitLineSpacingReduction = 0;
        Wrapping = "square";
        HorizontalOverflow = "overflow";
        VerticalOverflow = "overflow";
        VerticalText = "horz";
        Rotation = 0;
        Upright = false;
    }
}
