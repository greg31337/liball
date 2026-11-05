namespace odf;

/// <summary>
/// Describes an OpenDocument font-face declaration and its resource sources.
/// </summary>
public class OdfFontFace
{
    /// <summary>
    /// The declaration name referenced by text properties.
    /// </summary>
    public string Name = "";
    /// <summary>
    /// The SVG font-family lexical value.
    /// </summary>
    public string FontFamily = "";
    /// <summary>
    /// The SVG font-style lexical value.
    /// </summary>
    public string FontStyle = "";
    /// <summary>
    /// The SVG font-variant lexical value.
    /// </summary>
    public string FontVariant = "";
    /// <summary>
    /// The SVG font-weight lexical value.
    /// </summary>
    public string FontWeight = "";
    /// <summary>
    /// The SVG font-stretch lexical value.
    /// </summary>
    public string FontStretch = "";
    /// <summary>
    /// The SVG font-size lexical value.
    /// </summary>
    public string FontSize = "";
    /// <summary>
    /// The SVG Unicode-range lexical value.
    /// </summary>
    public string UnicodeRange = "";
    /// <summary>
    /// The SVG units-per-em lexical value.
    /// </summary>
    public string UnitsPerEm = "";
    /// <summary>
    /// The SVG PANOSE-1 classification.
    /// </summary>
    public string Panose1 = "";
    /// <summary>
    /// The SVG vertical-stem lexical value.
    /// </summary>
    public string StemVertical = "";
    /// <summary>
    /// The SVG horizontal-stem lexical value.
    /// </summary>
    public string StemHorizontal = "";
    /// <summary>
    /// The SVG slope lexical value.
    /// </summary>
    public string Slope = "";
    /// <summary>
    /// The SVG cap-height lexical value.
    /// </summary>
    public string CapHeight = "";
    /// <summary>
    /// The SVG x-height lexical value.
    /// </summary>
    public string XHeight = "";
    /// <summary>
    /// The SVG accent-height lexical value.
    /// </summary>
    public string AccentHeight = "";
    /// <summary>
    /// The SVG ascent lexical value.
    /// </summary>
    public string Ascent = "";
    /// <summary>
    /// The SVG descent lexical value.
    /// </summary>
    public string Descent = "";
    /// <summary>
    /// The SVG widths lexical value.
    /// </summary>
    public string Widths = "";
    /// <summary>
    /// The SVG bounding-box lexical value.
    /// </summary>
    public string BoundingBox = "";
    /// <summary>
    /// The SVG ideographic baseline lexical value.
    /// </summary>
    public string Ideographic = "";
    /// <summary>
    /// The SVG alphabetic baseline lexical value.
    /// </summary>
    public string Alphabetic = "";
    /// <summary>
    /// The SVG mathematical baseline lexical value.
    /// </summary>
    public string Mathematical = "";
    /// <summary>
    /// The SVG hanging baseline lexical value.
    /// </summary>
    public string Hanging = "";
    /// <summary>
    /// The SVG vertical ideographic baseline lexical value.
    /// </summary>
    public string VerticalIdeographic = "";
    /// <summary>
    /// The SVG vertical alphabetic baseline lexical value.
    /// </summary>
    public string VerticalAlphabetic = "";
    /// <summary>
    /// The SVG vertical mathematical baseline lexical value.
    /// </summary>
    public string VerticalMathematical = "";
    /// <summary>
    /// The SVG vertical hanging baseline lexical value.
    /// </summary>
    public string VerticalHanging = "";
    /// <summary>
    /// The SVG underline-position lexical value.
    /// </summary>
    public string UnderlinePosition = "";
    /// <summary>
    /// The SVG underline-thickness lexical value.
    /// </summary>
    public string UnderlineThickness = "";
    /// <summary>
    /// The SVG strikethrough-position lexical value.
    /// </summary>
    public string StrikethroughPosition = "";
    /// <summary>
    /// The SVG strikethrough-thickness lexical value.
    /// </summary>
    public string StrikethroughThickness = "";
    /// <summary>
    /// The SVG overline-position lexical value.
    /// </summary>
    public string OverlinePosition = "";
    /// <summary>
    /// The SVG overline-thickness lexical value.
    /// </summary>
    public string OverlineThickness = "";
    /// <summary>
    /// The style font-adornments lexical value.
    /// </summary>
    public string FontAdornments = "";
    /// <summary>
    /// The generic font-family classification.
    /// </summary>
    public string GenericFamily = "";
    /// <summary>
    /// The font pitch classification.
    /// </summary>
    public string Pitch = "";
    /// <summary>
    /// The declared font character set.
    /// </summary>
    public string Charset = "";
    /// <summary>
    /// The XML part containing the declaration, or an empty string for a flat document.
    /// </summary>
    public string PartPath = "";
    /// <summary>
    /// The declared font sources in source order.
    /// </summary>
    public OdfFontFaceSource[] Sources = [];
    /// <summary>
    /// The underlying font-face XML element, when loaded or written.
    /// </summary>
    public OdfNode? Node;
    internal OdfNode? OriginalNode;
    internal OdfNode[] OriginalSourceNodes = [];
    internal string OriginalName = "";
    internal string OriginalFontFamily = "";
    internal string OriginalFontStyle = "";
    internal string OriginalFontVariant = "";
    internal string OriginalFontWeight = "";
    internal string OriginalFontStretch = "";
    internal string OriginalFontSize = "";
    internal string OriginalUnicodeRange = "";
    internal string OriginalUnitsPerEm = "";
    internal string OriginalPanose1 = "";
    internal string OriginalStemVertical = "";
    internal string OriginalStemHorizontal = "";
    internal string OriginalSlope = "";
    internal string OriginalCapHeight = "";
    internal string OriginalXHeight = "";
    internal string OriginalAccentHeight = "";
    internal string OriginalAscent = "";
    internal string OriginalDescent = "";
    internal string OriginalWidths = "";
    internal string OriginalBoundingBox = "";
    internal string OriginalIdeographic = "";
    internal string OriginalAlphabetic = "";
    internal string OriginalMathematical = "";
    internal string OriginalHanging = "";
    internal string OriginalVerticalIdeographic = "";
    internal string OriginalVerticalAlphabetic = "";
    internal string OriginalVerticalMathematical = "";
    internal string OriginalVerticalHanging = "";
    internal string OriginalUnderlinePosition = "";
    internal string OriginalUnderlineThickness = "";
    internal string OriginalStrikethroughPosition = "";
    internal string OriginalStrikethroughThickness = "";
    internal string OriginalOverlinePosition = "";
    internal string OriginalOverlineThickness = "";
    internal string OriginalFontAdornments = "";
    internal string OriginalGenericFamily = "";
    internal string OriginalPitch = "";
    internal string OriginalCharset = "";
    internal string OriginalPartPath = "";
}
