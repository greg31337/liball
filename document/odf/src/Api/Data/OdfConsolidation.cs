namespace odf;

/// <summary>Describes a consolidation of source ranges into one target cell.</summary>
public class OdfConsolidation
{
    /// <summary>The consolidation aggregation function.</summary>
    public string Function = "sum";
    /// <summary>The space-separated source cell range addresses.</summary>
    public string SourceRangeAddresses = "";
    /// <summary>The target cell address.</summary>
    public string TargetCellAddress = "";
    /// <summary>The preserved use-label token.</summary>
    public string UseLabel = "none";
    /// <summary>The underlying consolidation element.</summary>
    public OdfNode? Node;
}
