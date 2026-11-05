namespace odf;

/// <summary>Describes the category source for a chart axis.</summary>
public class OdfChartCategories
{
    /// <summary>The category cell-range address.</summary>
    public string CellRangeAddress = "";
    /// <summary>The parsed category ranges.</summary>
    public OdfCellRange[] Ranges = [];
    /// <summary>Whether the category range list is absent or valid.</summary>
    public bool IsRangeValid = true;
    /// <summary>The underlying categories element.</summary>
    public OdfNode? Node;
    internal string OriginalCellRangeAddress = "";
}
