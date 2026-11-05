namespace odf;

/// <summary>Describes one chart-series domain range.</summary>
public class OdfChartDomain
{
    /// <summary>The domain cell-range address.</summary>
    public string CellRangeAddress = "";
    /// <summary>The parsed domain ranges.</summary>
    public OdfCellRange[] Ranges = [];
    /// <summary>Whether the domain range list is absent or valid.</summary>
    public bool IsRangeValid = true;
    /// <summary>The underlying domain element.</summary>
    public OdfNode? Node;
    internal string OriginalCellRangeAddress = "";
}
