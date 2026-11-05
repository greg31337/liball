namespace odf;

/// <summary>Describes a chart plot area and its structural children.</summary>
public class OdfChartPlotArea
{
    /// <summary>The horizontal position.</summary>
    public string X = "";
    /// <summary>The vertical position.</summary>
    public string Y = "";
    /// <summary>The plot-area width.</summary>
    public string Width = "";
    /// <summary>The plot-area height.</summary>
    public string Height = "";
    /// <summary>The referenced chart style name.</summary>
    public string StyleName = "";
    /// <summary>The plot-area source cell range.</summary>
    public string CellRangeAddress = "";
    /// <summary>The preserved data-source label token.</summary>
    public string DataSourceHasLabels = "";
    /// <summary>The effective plot-area chart-style metadata.</summary>
    public OdfChartStyleMetadata StyleMetadata = new OdfChartStyleMetadata();
    /// <summary>The chart axes in source order.</summary>
    public OdfChartAxis[] Axes = [];
    /// <summary>The data series in source order.</summary>
    public OdfChartSeries[] Series = [];
    /// <summary>The chart wall, when present.</summary>
    public OdfChartSurface? Wall;
    /// <summary>The chart floor, when present.</summary>
    public OdfChartSurface? Floor;
    /// <summary>The stock gain marker, when present.</summary>
    public OdfChartStockComponent? StockGainMarker;
    /// <summary>The stock loss marker, when present.</summary>
    public OdfChartStockComponent? StockLossMarker;
    /// <summary>The stock range line, when present.</summary>
    public OdfChartStockComponent? StockRangeLine;
    /// <summary>The parsed plot source ranges.</summary>
    public OdfCellRange[] SourceRanges = [];
    /// <summary>Whether the plot source-range list is absent or valid.</summary>
    public bool IsSourceRangeValid = true;
    /// <summary>The plot-area camera, lighting, and transform metadata.</summary>
    public OdfThreeDimensionalGeometry ThreeDimensional =
        new OdfThreeDimensionalGeometry();
    /// <summary>The underlying plot-area element.</summary>
    public OdfNode? Node;
    internal string OriginalX = "";
    internal string OriginalY = "";
    internal string OriginalWidth = "";
    internal string OriginalHeight = "";
    internal string OriginalStyleName = "";
    internal string OriginalCellRangeAddress = "";
    internal string OriginalDataSourceHasLabels = "";
}
