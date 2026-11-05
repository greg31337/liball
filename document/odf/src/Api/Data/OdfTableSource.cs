namespace odf;

/// <summary>Describes an external table used as the source of a linked table.</summary>
public class OdfTableSource
{
    /// <summary>Whether the table has an explicit table-source element.</summary>
    public bool HasSource;
    /// <summary>The IRI of the source document.</summary>
    public string Href = "";
    /// <summary>The XLink type, which is <c>simple</c> for an ODF table source.</summary>
    public string XLinkType = "simple";
    /// <summary>The optional XLink activation behavior.</summary>
    public string XLinkActuate = "";
    /// <summary>The name of the table in the source document.</summary>
    public string TableName = "";
    /// <summary>Whether formulas and styles or only calculated results are copied.</summary>
    public string Mode = "copy-all";
    /// <summary>The application-defined import filter name.</summary>
    public string FilterName = "";
    /// <summary>The application-defined import filter options.</summary>
    public string FilterOptions = "";
    /// <summary>The lexical duration between source refreshes.</summary>
    public string RefreshDelay = "";
    /// <summary>The underlying table-source element, when present.</summary>
    public OdfNode? Node;
    internal bool OriginalHasSource;
    internal string OriginalHref = "";
    internal string OriginalXLinkType = "simple";
    internal string OriginalXLinkActuate = "";
    internal string OriginalTableName = "";
    internal string OriginalMode = "copy-all";
    internal string OriginalFilterName = "";
    internal string OriginalFilterOptions = "";
    internal string OriginalRefreshDelay = "";
}
