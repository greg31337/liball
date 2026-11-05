namespace odf;

/// <summary>Describes a database-range subtotal grouping rule.</summary>
public class OdfSubtotalRule
{
    /// <summary>The zero-based field number used to group rows.</summary>
    public long GroupByFieldNumber;
    /// <summary>The calculated subtotal fields.</summary>
    public OdfSubtotalField[] Fields = [];
    /// <summary>The underlying subtotal-rule element.</summary>
    public OdfNode? Node;
}
