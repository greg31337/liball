namespace odf;

/// <summary>Describes one source, row, column, page, data, or hidden pivot field.</summary>
public class OdfPivotField
{
    /// <summary>The source field name.</summary>
    public string SourceFieldName = "";
    /// <summary>The field orientation.</summary>
    public string Orientation = "hidden";
    /// <summary>The selected member for a page field.</summary>
    public string SelectedPage = "";
    /// <summary>Whether this is the synthetic data-layout field.</summary>
    public bool IsDataLayoutField;
    /// <summary>The aggregation function for a data field.</summary>
    public string Function = "auto";
    /// <summary>The selected hierarchy number.</summary>
    public long UsedHierarchy;
    /// <summary>The field level settings, when present.</summary>
    public OdfPivotLevel? Level;
    /// <summary>The underlying data-pilot-field element.</summary>
    public OdfNode? Node;
}
