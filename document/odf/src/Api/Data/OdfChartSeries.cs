namespace odf;

/// <summary>Describes one chart data series.</summary>
public class OdfChartSeries
{
    /// <summary>The standard series chart kind.</summary>
    public OdfChartKind Kind;
    /// <summary>The preserved series chart-class token.</summary>
    public string ClassName = "";
    /// <summary>The values cell-range address.</summary>
    public string ValuesCellRangeAddress = "";
    /// <summary>The label cell address.</summary>
    public string LabelCellAddress = "";
    /// <summary>The attached axis name.</summary>
    public string AttachedAxis = "";
    /// <summary>The referenced chart style name.</summary>
    public string StyleName = "";
    /// <summary>The effective chart-style metadata.</summary>
    public OdfChartStyleMetadata StyleMetadata = new OdfChartStyleMetadata();
    /// <summary>The series domains in source order.</summary>
    public OdfChartDomain[] Domains = [];
    /// <summary>The parsed values source ranges.</summary>
    public OdfCellRange[] ValueRanges = [];
    /// <summary>Whether the values source-range list is absent or valid.</summary>
    public bool IsValueRangeValid = true;
    /// <summary>The parsed label source ranges.</summary>
    public OdfCellRange[] LabelRanges = [];
    /// <summary>Whether the label source-range list is absent or valid.</summary>
    public bool IsLabelRangeValid = true;
    /// <summary>The compact data-point runs in source order.</summary>
    public OdfChartDataPoint[] DataPoints = [];
    /// <summary>The series error indicators in source order.</summary>
    public OdfChartErrorIndicator[] ErrorIndicators = [];
    /// <summary>The series regression curves in source order.</summary>
    public OdfChartRegressionCurve[] RegressionCurves = [];
    /// <summary>The series data label, when present.</summary>
    public OdfChartDataLabel? Label;
    /// <summary>The containing chart.</summary>
    public OdfChart? Chart;
    /// <summary>The underlying series element.</summary>
    public OdfNode? Node;
    internal string OriginalClassName = "";
    internal string OriginalValuesCellRangeAddress = "";
    internal string OriginalLabelCellAddress = "";
    internal string OriginalAttachedAxis = "";
    internal string OriginalStyleName = "";
}
