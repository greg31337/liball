namespace xml;

/// <summary>
/// Describes the symbols used by an XPath decimal format.
/// </summary>
public class XPathDecimalFormat
{
    /// <summary>The expanded name, or null for the default decimal format.</summary>
    public XmlName? Name;
    /// <summary>The decimal separator character.</summary>
    public string? DecimalSeparator;
    /// <summary>The grouping separator character.</summary>
    public string? GroupingSeparator;
    /// <summary>The infinity representation.</summary>
    public string? Infinity;
    /// <summary>The not-a-number representation.</summary>
    public string? NaN;
    /// <summary>The minus sign character.</summary>
    public string? MinusSign;
    /// <summary>The percent character.</summary>
    public string? Percent;
    /// <summary>The per-mille character.</summary>
    public string? PerMille;
    /// <summary>The zero digit character.</summary>
    public string? ZeroDigit;
    /// <summary>The optional digit character.</summary>
    public string? Digit;
    /// <summary>The pattern separator character.</summary>
    public string? PatternSeparator;
    /// <summary>The exponent separator character.</summary>
    public string? ExponentSeparator;
}
