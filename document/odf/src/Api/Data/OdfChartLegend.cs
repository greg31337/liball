namespace odf;

/// <summary>Describes a chart legend.</summary>
public class OdfChartLegend
{
    /// <summary>The legend position token.</summary>
    public string Position = "";
    /// <summary>The legend alignment token.</summary>
    public string Alignment = "";
    /// <summary>The legend expansion token.</summary>
    public string Expansion = "";
    /// <summary>The custom expansion aspect ratio.</summary>
    public string ExpansionAspectRatio = "";
    /// <summary>The horizontal position.</summary>
    public string X = "";
    /// <summary>The vertical position.</summary>
    public string Y = "";
    /// <summary>The referenced chart style name.</summary>
    public string StyleName = "";
    /// <summary>The optional legend text.</summary>
    public string Text = "";
    /// <summary>The underlying legend element.</summary>
    public OdfNode? Node;
    internal string OriginalPosition = "";
    internal string OriginalAlignment = "";
    internal string OriginalExpansion = "";
    internal string OriginalExpansionAspectRatio = "";
    internal string OriginalX = "";
    internal string OriginalY = "";
    internal string OriginalStyleName = "";
    internal string OriginalText = "";
}
