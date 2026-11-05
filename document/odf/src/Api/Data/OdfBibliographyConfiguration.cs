namespace odf;

/// <summary>Describes global bibliography display and sorting configuration.</summary>
public class OdfBibliographyConfiguration
{
    /// <summary>The entry prefix.</summary>
    public string Prefix = "";
    /// <summary>The entry suffix.</summary>
    public string Suffix = "";
    /// <summary>The preserved numbered-entries value.</summary>
    public string NumberedEntries = "";
    /// <summary>The preserved sort-by-position value.</summary>
    public string SortByPosition = "";
    /// <summary>The sorting algorithm.</summary>
    public string SortAlgorithm = "";
    /// <summary>The sorting language.</summary>
    public string Language = "";
    /// <summary>The sorting country.</summary>
    public string Country = "";
    /// <summary>The ordered sorting keys.</summary>
    public OdfBibliographySortKey[] SortKeys = [];
    /// <summary>All qualified configuration attributes.</summary>
    public OdfAttribute[] Attributes = [];
    /// <summary>The XML part containing the configuration.</summary>
    public string PartPath = "";
    /// <summary>The underlying bibliography-configuration element.</summary>
    public OdfNode? Node;
}
