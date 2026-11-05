namespace odf;

/// <summary>
/// Describes an OpenFormula syntax-tree node.
/// </summary>
public class OdfFormulaNode
{
    /// <summary>
    /// The syntax-tree node kind.
    /// </summary>
    public OdfFormulaNodeKind Kind;
    /// <summary>
    /// The source text associated with the node, such as a literal, name, or operator.
    /// </summary>
    public string Text = "";
    /// <summary>
    /// The decoded literal value when distinct from <see cref="Text"/>.
    /// </summary>
    public string Value = "";
    /// <summary>
    /// The lexical token kind that produced a leaf node.
    /// </summary>
    public OdfFormulaTokenKind TokenKind;
    /// <summary>
    /// The node operands or contained expressions in source order.
    /// </summary>
    public OdfFormulaNode[] Children = [];
    /// <summary>
    /// The separators between function arguments or inline-array values.
    /// </summary>
    public string[] Separators = [];
    /// <summary>
    /// The structured reference meaning for a reference or name node.
    /// </summary>
    public OdfFormulaReference? Reference;
    /// <summary>
    /// Standardized function metadata for a recognized function call.
    /// </summary>
    public OdfFormulaFunctionInfo? Function;
    /// <summary>
    /// The zero-based UTF-16 source offset.
    /// </summary>
    public int Start;
    /// <summary>
    /// The node length in UTF-16 code units.
    /// </summary>
    public int Length;
    internal OdfFormulaNodeKind OriginalKind;
    internal string OriginalText = "";
    internal string OriginalValue = "";
    internal OdfFormulaTokenKind OriginalTokenKind;
    internal int OriginalChildCount;
    internal string[] OriginalSeparators = [];
}
