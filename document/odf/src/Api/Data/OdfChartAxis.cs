namespace odf;

/// <summary>Describes a chart axis.</summary>
public class OdfChartAxis
{
    /// <summary>The axis dimension token.</summary>
    public string Dimension = "";
    /// <summary>The axis name.</summary>
    public string Name = "";
    /// <summary>The referenced chart style name.</summary>
    public string StyleName = "";
    /// <summary>The axis title, when present.</summary>
    public OdfChartText? Title;
    /// <summary>The category source, when present.</summary>
    public OdfChartCategories? Categories;
    /// <summary>The axis grids in source order.</summary>
    public OdfChartGrid[] Grids = [];
    /// <summary>The underlying axis element.</summary>
    public OdfNode? Node;
    internal string OriginalDimension = "";
    internal string OriginalName = "";
    internal string OriginalStyleName = "";
}
