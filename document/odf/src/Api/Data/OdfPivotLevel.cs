namespace odf;

/// <summary>Describes member, subtotal, sorting, display, and layout settings for a pivot field level.</summary>
public class OdfPivotLevel
{
    /// <summary>Whether empty members are shown.</summary>
    public bool ShowEmpty;
    /// <summary>The subtotal calculations for this level.</summary>
    public OdfPivotSubtotal[] Subtotals = [];
    /// <summary>The explicit members for this level.</summary>
    public OdfPivotMember[] Members = [];
    /// <summary>Whether top or bottom member display limiting is configured.</summary>
    public bool HasDisplayInfo;
    /// <summary>Whether member display limiting is enabled.</summary>
    public bool DisplayInfoEnabled;
    /// <summary>The data field used to rank displayed members.</summary>
    public string DisplayDataField = "";
    /// <summary>The maximum displayed member count.</summary>
    public long DisplayMemberCount;
    /// <summary>Whether displayed members are selected from the top or bottom.</summary>
    public string DisplayMemberMode = "from-top";
    /// <summary>Whether explicit member sorting is configured.</summary>
    public bool HasSortInfo;
    /// <summary>The member sort mode.</summary>
    public string SortMode = "none";
    /// <summary>The data field used when sorting by data.</summary>
    public string SortDataField = "";
    /// <summary>The member sort order.</summary>
    public string SortOrder = "ascending";
    /// <summary>Whether explicit level layout is configured.</summary>
    public bool HasLayoutInfo;
    /// <summary>The level layout mode.</summary>
    public string LayoutMode = "tabular-layout";
    /// <summary>Whether empty lines are inserted between level groups.</summary>
    public bool AddEmptyLines;
    /// <summary>The underlying data-pilot-level element.</summary>
    public OdfNode? Node;
}
