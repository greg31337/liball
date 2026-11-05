namespace odf;

/// <summary>Describes an explicit chart series or data-point label.</summary>
public class OdfChartDataLabel
{
    /// <summary>The displayed label text.</summary>
    public string Text = "";
    /// <summary>The horizontal label position.</summary>
    public string X = "";
    /// <summary>The vertical label position.</summary>
    public string Y = "";
    /// <summary>The referenced chart style name.</summary>
    public string StyleName = "";
    /// <summary>The effective chart-style metadata.</summary>
    public OdfChartStyleMetadata StyleMetadata = new OdfChartStyleMetadata();
    /// <summary>The underlying data-label element.</summary>
    public OdfNode? Node;
    internal string OriginalText = "";
    internal string OriginalX = "";
    internal string OriginalY = "";
    internal string OriginalStyleName = "";
}
