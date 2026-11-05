namespace odf;

/// <summary>
/// Describes the semantic meaning of an OpenFormula reference expression.
/// </summary>
public class OdfFormulaReference
{
    /// <summary>
    /// The reference expression kind.
    /// </summary>
    public OdfFormulaNodeKind Kind;
    /// <summary>
    /// The original lexical representation.
    /// </summary>
    public string LexicalValue = "";
    /// <summary>
    /// The external document source, or an empty string.
    /// </summary>
    public string Source = "";
    /// <summary>
    /// The named expression or label name, or an empty string.
    /// </summary>
    public string Name = "";
    /// <summary>
    /// The sheet qualifying a named expression, or an empty string.
    /// </summary>
    public string NameSheet = "";
    /// <summary>
    /// The first cell or range endpoint.
    /// </summary>
    public OdfFormulaReferenceEndpoint Start = new OdfFormulaReferenceEndpoint();
    /// <summary>
    /// The last range endpoint.
    /// </summary>
    public OdfFormulaReferenceEndpoint End = new OdfFormulaReferenceEndpoint();
    /// <summary>
    /// The operands of a range, intersection, or reference-concatenation operator.
    /// </summary>
    public OdfFormulaReference[] Items = [];
    /// <summary>
    /// Whether an external document source is present.
    /// </summary>
    public bool HasSource;
    /// <summary>
    /// Whether a second range endpoint is present.
    /// </summary>
    public bool HasEnd;
    /// <summary>
    /// Whether the range crosses explicitly named sheets.
    /// </summary>
    public bool IsThreeDimensional;
    /// <summary>
    /// Whether the named expression uses the explicit <c>$$</c> form.
    /// </summary>
    public bool IsExplicitName;
    /// <summary>
    /// Whether the name is a quoted row or column label.
    /// </summary>
    public bool IsLabel;
    /// <summary>
    /// Whether the reference is the <c>#REF!</c> invalid-reference value.
    /// </summary>
    public bool IsReferenceError;
}
