namespace odf;

/// <summary>Describes a named cell range or named formula expression.</summary>
public class OdfNamedExpression
{
    /// <summary>The expression name.</summary>
    public string Name = "";
    /// <summary>The formula expression for a named expression.</summary>
    public string Expression = "";
    /// <summary>The base cell address used for relative references.</summary>
    public string BaseCellAddress = "";
    /// <summary>The cell range address for a named range.</summary>
    public string RangeAddress = "";
    /// <summary>The preserved range-usable-as token list.</summary>
    public string RangeUsableAs = "";
    /// <summary>Whether this declaration is a named range rather than an expression.</summary>
    public bool IsRange;
    /// <summary>The underlying named-range or named-expression element.</summary>
    public OdfNode? Node;
}
