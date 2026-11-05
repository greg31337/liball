namespace odf;

/// <summary>
/// Represents one preserved lexical form-property value.
/// </summary>
public class OdfFormPropertyValue
{
    /// <summary>The preserved lexical value.</summary>
    public string Value = "";
    /// <summary>The currency code associated with a currency value.</summary>
    public string Currency = "";
    /// <summary>The underlying property or list-value element.</summary>
    public OdfNode? Node;
    internal string OriginalValue = "";
    internal string OriginalCurrency = "";
}
