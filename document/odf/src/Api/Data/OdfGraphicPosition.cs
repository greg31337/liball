namespace odf;

/// <summary>
/// Describes graphic positioning, alignment, mirroring, and clipping.
/// </summary>
public class OdfGraphicPosition
{
    /// <summary>The horizontal position mode.</summary>
    public string Horizontal = "";
    /// <summary>The horizontal positioning reference.</summary>
    public string HorizontalRelation = "";
    /// <summary>The vertical position mode.</summary>
    public string Vertical = "";
    /// <summary>The vertical positioning reference.</summary>
    public string VerticalRelation = "";
    /// <summary>The style-level horizontal coordinate.</summary>
    public string X = "";
    /// <summary>The style-level vertical coordinate.</summary>
    public string Y = "";
    /// <summary>The text-area horizontal alignment.</summary>
    public string TextAreaHorizontalAlignment = "";
    /// <summary>The text-area vertical alignment.</summary>
    public string TextAreaVerticalAlignment = "";
    /// <summary>The text wrapping option within the graphic.</summary>
    public string TextWrapOption = "";
    /// <summary>The preserved flow-with-text boolean.</summary>
    public string FlowWithTextValue = "";
    /// <summary>The image mirroring mode.</summary>
    public string Mirror = "";
    /// <summary>The image crop or clip rectangle.</summary>
    public string Clip = "";
}
