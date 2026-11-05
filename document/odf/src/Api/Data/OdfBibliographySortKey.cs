namespace odf;

/// <summary>Describes a bibliography sorting key.</summary>
public class OdfBibliographySortKey
{
    /// <summary>The bibliography data-field key.</summary>
    public string Key = "";
    /// <summary>The preserved ascending-sort value.</summary>
    public string SortAscending = "";
    /// <summary>The underlying sort-key element.</summary>
    public OdfNode? Node;
}
