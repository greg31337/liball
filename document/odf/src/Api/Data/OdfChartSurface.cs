namespace odf;

/// <summary>Describes a chart wall or floor surface.</summary>
public class OdfChartSurface
{
    /// <summary>The surface width.</summary>
    public string Width = "";
    /// <summary>The referenced chart style name.</summary>
    public string StyleName = "";
    /// <summary>The underlying wall or floor element.</summary>
    public OdfNode? Node;
    internal string OriginalWidth = "";
    internal string OriginalStyleName = "";
}
