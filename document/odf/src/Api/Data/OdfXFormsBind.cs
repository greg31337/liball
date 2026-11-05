namespace odf;

/// <summary>
/// Describes an XForms node-set binding and its declarative model-item expressions.
/// </summary>
public class OdfXFormsBind
{
    /// <summary>The bind identifier.</summary>
    public string Id = "";
    /// <summary>The node-set XPath expression.</summary>
    public string NodeSet = "";
    /// <summary>The model-item datatype name.</summary>
    public string Type = "";
    /// <summary>The read-only expression.</summary>
    public string ReadOnlyExpression = "";
    /// <summary>The relevance expression.</summary>
    public string RelevantExpression = "";
    /// <summary>The required-value expression.</summary>
    public string RequiredExpression = "";
    /// <summary>The constraint expression.</summary>
    public string ConstraintExpression = "";
    /// <summary>The calculated-value expression.</summary>
    public string CalculateExpression = "";
    /// <summary>The privacy-preference datatype token.</summary>
    public string P3PType = "";
    /// <summary>The containing bind, when this bind is nested.</summary>
    public OdfXFormsBind? Parent;
    /// <summary>The nested binds in document order.</summary>
    public OdfXFormsBind[] Children = [];
    /// <summary>All qualified bind attributes.</summary>
    public OdfAttribute[] Attributes = [];
    /// <summary>The underlying XForms bind element.</summary>
    public OdfNode? Node;
    internal string OriginalId = "";
    internal string OriginalNodeSet = "";
    internal string OriginalType = "";
    internal string OriginalReadOnlyExpression = "";
    internal string OriginalRelevantExpression = "";
    internal string OriginalRequiredExpression = "";
    internal string OriginalConstraintExpression = "";
    internal string OriginalCalculateExpression = "";
    internal string OriginalP3PType = "";
}
