namespace odf;

/// <summary>
/// Describes a conditional mapping from one data style to another.
/// </summary>
public class OdfDataStyleMap
{
    /// <summary>
    /// The preserved map condition expression.
    /// </summary>
    public string Condition = "";
    /// <summary>
    /// The data-style name applied when the condition is true.
    /// </summary>
    public string ApplyStyleName = "";
    /// <summary>
    /// The optional base cell address used by the condition.
    /// </summary>
    public string BaseCellAddress = "";
    /// <summary>
    /// All qualified map attributes, including extension attributes.
    /// </summary>
    public OdfStyleProperty[] Attributes = [];
    /// <summary>
    /// The underlying map element, when loaded or written.
    /// </summary>
    public OdfNode? Node;
    internal OdfNode? OriginalNode;
    internal OdfAttribute[] OriginalAttributes = [];
    internal string OriginalCondition = "";
    internal string OriginalApplyStyleName = "";
    internal string OriginalBaseCellAddress = "";
}
