namespace pptx;

/// <summary>
/// Represents font data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxFont
{
    /// <summary>
    /// Stores the family associated with this object.
    /// </summary>
    public string Family;
    /// <summary>
    /// Stores the east asian family associated with this object.
    /// </summary>
    public string EastAsianFamily;
    /// <summary>
    /// Stores the complex script family associated with this object.
    /// </summary>
    public string ComplexScriptFamily;
    /// <summary>
    /// Stores the language associated with this object.
    /// </summary>
    public string Language;
    /// <summary>
    /// Stores the alternative language associated with this object.
    /// </summary>
    public string AlternativeLanguage;
    /// <summary>
    /// Stores the size points value associated with this object.
    /// </summary>
    public double SizePoints;
    /// <summary>
    /// Indicates whether the bold setting is enabled.
    /// </summary>
    public bool Bold;
    /// <summary>
    /// Indicates whether the italic setting is enabled.
    /// </summary>
    public bool Italic;
    /// <summary>
    /// Indicates whether the underline setting is enabled.
    /// </summary>
    public bool Underline;
    /// <summary>
    /// Indicates whether the strike setting is enabled.
    /// </summary>
    public bool Strike;
    /// <summary>
    /// Stores the baseline value associated with this object.
    /// </summary>
    public int Baseline;
    /// <summary>
    /// Stores <c>none</c>, <c>small</c>, or <c>all</c> capitalization.
    /// </summary>
    public string Capitalization;
    /// <summary>
    /// Stores the minimum kerning size in hundredths of one point.
    /// </summary>
    public int Kerning;
    /// <summary>
    /// Stores signed character spacing in hundredths of one point.
    /// </summary>
    public int Spacing;
    /// <summary>
    /// Stores the underline style, or an empty inherited value.
    /// </summary>
    public string UnderlineStyle;
    /// <summary>
    /// Stores the strike style, or an empty inherited value.
    /// </summary>
    public string StrikeStyle;
    /// <summary>
    /// Stores the symbol typeface.
    /// </summary>
    public string SymbolFamily;
    /// <summary>
    /// Stores the fill color value associated with this object.
    /// </summary>
    public PptxColor FillColor;
    /// <summary>
    /// Stores the complete direct text fill.
    /// </summary>
    public PptxFill Fill;
    /// <summary>
    /// Stores the direct highlight color.
    /// </summary>
    public PptxColor HighlightColor;

    /// <summary>
    /// Initializes a new font with safe defaults or the supplied values.
    /// </summary>
    public PptxFont()
    {
        Family = "";
        EastAsianFamily = "";
        ComplexScriptFamily = "";
        Language = "";
        AlternativeLanguage = "";
        SizePoints = 0.0;
        Bold = false;
        Italic = false;
        Underline = false;
        Strike = false;
        Baseline = 0;
        Capitalization = "";
        Kerning = 0;
        Spacing = 0;
        UnderlineStyle = "";
        StrikeStyle = "";
        SymbolFamily = "";
        FillColor = new PptxColor();
        Fill = new PptxFill();
        HighlightColor = new PptxColor();
    }
}
