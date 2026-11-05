namespace odf;

/// <summary>Describes a database range together with sorting, filtering, and subtotal rules.</summary>
public class OdfDatabaseRange
{
    /// <summary>The database-range name.</summary>
    public string Name = "";
    /// <summary>The target cell range address.</summary>
    public string TargetRangeAddress = "";
    /// <summary>Whether the first row or column contains labels.</summary>
    public bool ContainsHeader = true;
    /// <summary>Whether filter buttons are displayed.</summary>
    public bool DisplayFilterButtons;
    /// <summary>Whether styles are retained when the range updates.</summary>
    public bool KeepStyles = true;
    /// <summary>Whether the range size is retained when it updates.</summary>
    public bool KeepSize;
    /// <summary>Whether imported database data is stored in the document.</summary>
    public bool HasPersistentData = true;
    /// <summary>Whether sort comparisons are case-sensitive.</summary>
    public bool SortCaseSensitive;
    /// <summary>The optional sort target range address.</summary>
    public string SortTargetRangeAddress = "";
    /// <summary>The sort keys in evaluation order.</summary>
    public OdfSortRule[] SortRules = [];
    /// <summary>The filter conditions in document order.</summary>
    public OdfFilterRule[] FilterRules = [];
    /// <summary>The subtotal grouping rules.</summary>
    public OdfSubtotalRule[] SubtotalRules = [];
    /// <summary>The underlying database-range element.</summary>
    public OdfNode? Node;
}
