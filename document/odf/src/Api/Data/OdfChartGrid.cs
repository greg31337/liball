namespace odf;

/// <summary>Describes a major or minor chart-axis grid.</summary>
public class OdfChartGrid
{
    /// <summary>The preserved grid class token.</summary>
    public string ClassName = "";
    /// <summary>The referenced chart style name.</summary>
    public string StyleName = "";
    /// <summary>The underlying grid element.</summary>
    public OdfNode? Node;
    internal string OriginalClassName = "";
    internal string OriginalStyleName = "";
}
