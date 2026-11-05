namespace odf;

/// <summary>Describes one subtotal calculation in a pivot level.</summary>
public class OdfPivotSubtotal
{
    /// <summary>The aggregation function.</summary>
    public string Function = "auto";
    /// <summary>The underlying data-pilot-subtotal element.</summary>
    public OdfNode? Node;
}
