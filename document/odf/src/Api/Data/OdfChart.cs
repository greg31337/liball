namespace odf;

/// <summary>
/// Describes the kind of a chart.
/// </summary>
public class OdfChart
{
    /// <summary>
    /// The chart kind.
    /// </summary>
    public OdfChartKind Kind;
    /// <summary>The preserved chart class token.</summary>
    public string ClassName = "";
    /// <summary>The chart style name.</summary>
    public string StyleName = "";
    /// <summary>The chart data-source reference.</summary>
    public string Href = "";
    /// <summary>The preserved XLink type.</summary>
    public string XLinkType = "";
    /// <summary>The chart width.</summary>
    public string Width = "";
    /// <summary>The chart height.</summary>
    public string Height = "";
    /// <summary>The chart title, when present.</summary>
    public OdfChartText? Title;
    /// <summary>The chart subtitle, when present.</summary>
    public OdfChartText? Subtitle;
    /// <summary>The chart footer, when present.</summary>
    public OdfChartText? Footer;
    /// <summary>The chart legend, when present.</summary>
    public OdfChartLegend? Legend;
    /// <summary>The chart plot area.</summary>
    public OdfChartPlotArea? PlotArea;
    /// <summary>The embedded chart data table, when present.</summary>
    public OdfTable? DataTable;
    /// <summary>The effective chart-style variant metadata.</summary>
    public OdfChartStyleMetadata StyleMetadata = new OdfChartStyleMetadata();
    /// <summary>The embedded object containing this chart, when applicable.</summary>
    public OdfEmbeddedObject? EmbeddedObject;
    /// <summary>The package part containing the chart.</summary>
    public string PartPath = "";
    /// <summary>The underlying chart element when materialized.</summary>
    public OdfNode? Node;
    internal string OriginalClassName = "";
    internal string OriginalStyleName = "";
    internal string OriginalHref = "";
    internal string OriginalXLinkType = "";
    internal string OriginalWidth = "";
    internal string OriginalHeight = "";
}
