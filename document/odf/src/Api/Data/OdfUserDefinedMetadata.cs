namespace odf;

/// <summary>
/// Represents one typed user-defined metadata value.
/// </summary>
public class OdfUserDefinedMetadata
{
    /// <summary>The user-defined field name.</summary>
    public string Name = "";
    /// <summary>The exact <c>meta:value-type</c> token.</summary>
    public string ValueType = "";
    /// <summary>The semantic value kind.</summary>
    public OdfValueKind Kind;
    /// <summary>The preserved lexical value.</summary>
    public string LexicalValue = "";
    /// <summary>The string value.</summary>
    public string StringValue = "";
    /// <summary>The numeric value for numeric, percentage, and currency values.</summary>
    public double NumericValue;
    /// <summary>The ISO currency code for a currency value.</summary>
    public string Currency = "";
    /// <summary>The preserved date or date-time value.</summary>
    public string DateValue = "";
    /// <summary>The preserved duration value.</summary>
    public string DurationValue = "";
    /// <summary>The Boolean value.</summary>
    public bool BooleanValue;
    /// <summary>Whether the lexical value is valid for its declared type.</summary>
    public bool IsValid;
    internal OdfNode? Node;
    internal OdfValueKind OriginalKind;
    internal string OriginalValueType = "";
    internal string OriginalLexicalValue = "";
    internal string OriginalStringValue = "";
    internal double OriginalNumericValue;
    internal string OriginalCurrency = "";
    internal string OriginalDateValue = "";
    internal string OriginalDurationValue = "";
    internal bool OriginalBooleanValue;
}
