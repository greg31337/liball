namespace odf;

/// <summary>Describes an OpenDocument data-pilot or pivot table.</summary>
public class OdfPivotTable
{
    /// <summary>The pivot table name.</summary>
    public string Name = "";
    /// <summary>Application-specific preserved data.</summary>
    public string ApplicationData = "";
    /// <summary>The grand-total display mode.</summary>
    public string GrandTotal = "both";
    /// <summary>Whether empty source rows are ignored.</summary>
    public bool IgnoreEmptyRows;
    /// <summary>Whether category labels are inferred from empty source cells.</summary>
    public bool IdentifyCategories;
    /// <summary>The pivot output cell range.</summary>
    public string TargetRangeAddress = "";
    /// <summary>The pivot button cell ranges.</summary>
    public string ButtonRangeAddresses = "";
    /// <summary>Whether the pivot filter button is shown.</summary>
    public bool ShowFilterButton = true;
    /// <summary>Whether double-clicking an output cell drills into source rows.</summary>
    public bool DrillDownOnDoubleClick = true;
    /// <summary>The source element kind, such as cell-range, table, query, SQL, or service.</summary>
    public string SourceKind = "";
    /// <summary>The source range address for a cell-range source.</summary>
    public string SourceRangeAddress = "";
    /// <summary>The optional named source range.</summary>
    public string SourceName = "";
    /// <summary>The filters applied to a cell-range source.</summary>
    public OdfFilterRule[] FilterRules = [];
    /// <summary>The pivot fields in source order.</summary>
    public OdfPivotField[] Fields = [];
    /// <summary>The underlying data-pilot-table element.</summary>
    public OdfNode? Node;
    /// <summary>The underlying source element, when present.</summary>
    public OdfNode? SourceNode;
}
