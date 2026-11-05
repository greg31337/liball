namespace odf;

/// <summary>Describes a stock-chart gain marker, loss marker, or range line.</summary>
public class OdfChartStockComponent
{
    /// <summary>The preserved stock component element name.</summary>
    public string ElementName = "";
    /// <summary>The referenced chart style name.</summary>
    public string StyleName = "";
    /// <summary>The effective chart-style metadata.</summary>
    public OdfChartStyleMetadata StyleMetadata = new OdfChartStyleMetadata();
    /// <summary>The underlying stock component element.</summary>
    public OdfNode? Node;
    internal string OriginalStyleName = "";
}
