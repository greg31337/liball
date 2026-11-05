namespace odf;

/// <summary>
/// Describes graphic fill, bitmap-fill, and opacity properties.
/// </summary>
public class OdfGraphicFill
{
    /// <summary>The fill mode.</summary>
    public string Mode = "";
    /// <summary>The primary fill color.</summary>
    public string Color = "";
    /// <summary>The secondary fill color.</summary>
    public string SecondaryColor = "";
    /// <summary>The referenced gradient name.</summary>
    public string GradientName = "";
    /// <summary>The lexical gradient step count.</summary>
    public string GradientStepCount = "";
    /// <summary>The referenced hatch name.</summary>
    public string HatchName = "";
    /// <summary>The preserved hatch-solid boolean.</summary>
    public string HatchSolidValue = "";
    /// <summary>The referenced bitmap fill-image name.</summary>
    public string ImageName = "";
    /// <summary>The bitmap repeat mode.</summary>
    public string ImageRepeat = "";
    /// <summary>The bitmap fill width.</summary>
    public string ImageWidth = "";
    /// <summary>The bitmap fill height.</summary>
    public string ImageHeight = "";
    /// <summary>The bitmap reference point.</summary>
    public string ImageReferencePoint = "";
    /// <summary>The horizontal bitmap reference-point offset.</summary>
    public string ImageReferencePointX = "";
    /// <summary>The vertical bitmap reference-point offset.</summary>
    public string ImageReferencePointY = "";
    /// <summary>The bitmap tile-repeat offset.</summary>
    public string TileRepeatOffset = "";
    /// <summary>The fill opacity.</summary>
    public string Opacity = "";
    /// <summary>The referenced opacity-gradient name.</summary>
    public string OpacityName = "";
    /// <summary>The SVG fill rule.</summary>
    public string Rule = "";
}
