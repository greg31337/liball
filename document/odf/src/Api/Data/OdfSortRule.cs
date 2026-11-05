namespace odf;

/// <summary>Describes one database-range sort key.</summary>
public class OdfSortRule
{
    /// <summary>The zero-based field number used as the sort key.</summary>
    public long FieldNumber;
    /// <summary>The preserved automatic, text, or number data-type token.</summary>
    public string DataType = "automatic";
    /// <summary>The ascending or descending order token.</summary>
    public string Order = "ascending";
    /// <summary>The underlying sort-by element.</summary>
    public OdfNode? Node;
}
