namespace odf;

/// <summary>Describes one calculated field in a subtotal rule.</summary>
public class OdfSubtotalField
{
    /// <summary>The zero-based field number to subtotal.</summary>
    public long FieldNumber;
    /// <summary>The subtotal aggregation function token.</summary>
    public string Function = "sum";
    /// <summary>The underlying subtotal-field element.</summary>
    public OdfNode? Node;
}
