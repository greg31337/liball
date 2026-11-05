namespace odf;

/// <summary>Describes one database-range filter condition.</summary>
public class OdfFilterRule
{
    /// <summary>The zero-based field number tested by the condition.</summary>
    public long FieldNumber;
    /// <summary>The filter operator token.</summary>
    public string Operator = "=";
    /// <summary>The comparison value.</summary>
    public string Value = "";
    /// <summary>The preserved automatic, text, or number data-type token.</summary>
    public string DataType = "automatic";
    /// <summary>Whether text comparison is case-sensitive.</summary>
    public bool CaseSensitive;
    /// <summary>The logical connector inherited from the containing filter group.</summary>
    public string Connector = "";
    /// <summary>The number of containing filter-and or filter-or groups.</summary>
    public int GroupDepth;
    /// <summary>The underlying filter-condition element.</summary>
    public OdfNode? Node;
}
