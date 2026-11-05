namespace odf;

/// <summary>
/// Describes graphic stroke and line-rendering properties.
/// </summary>
public class OdfGraphicStroke
{
    /// <summary>The stroke mode.</summary>
    public string Mode = "";
    /// <summary>The referenced stroke-dash name.</summary>
    public string DashName = "";
    /// <summary>The lexical list of fallback stroke-dash names.</summary>
    public string DashNames = "";
    /// <summary>The stroke width.</summary>
    public string Width = "";
    /// <summary>The stroke color.</summary>
    public string Color = "";
    /// <summary>The stroke opacity.</summary>
    public string Opacity = "";
    /// <summary>The stroke line-join mode.</summary>
    public string LineJoin = "";
    /// <summary>The stroke line-cap mode.</summary>
    public string LineCap = "";
}
