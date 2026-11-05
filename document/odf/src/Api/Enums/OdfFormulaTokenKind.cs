namespace odf;

/// <summary>
/// Identifies an OpenFormula lexical token.
/// </summary>
public enum OdfFormulaTokenKind
{
    /// <summary>
    /// The token kind is absent or unrecognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// The end of the formula input.
    /// </summary>
    End = 1,
    /// <summary>
    /// Significant or preserved whitespace.
    /// </summary>
    Whitespace = 2,
    /// <summary>
    /// A numeric literal.
    /// </summary>
    Number = 3,
    /// <summary>
    /// A percentage literal.
    /// </summary>
    Percentage = 4,
    /// <summary>
    /// A string literal.
    /// </summary>
    String = 5,
    /// <summary>
    /// A Boolean literal.
    /// </summary>
    Boolean = 6,
    /// <summary>
    /// An error literal.
    /// </summary>
    Error = 7,
    /// <summary>
    /// An identifier.
    /// </summary>
    Identifier = 8,
    /// <summary>
    /// A function name.
    /// </summary>
    Function = 9,
    /// <summary>
    /// A cell reference.
    /// </summary>
    CellReference = 10,
    /// <summary>
    /// A cell-range reference.
    /// </summary>
    RangeReference = 11,
    /// <summary>
    /// An external reference.
    /// </summary>
    ExternalReference = 12,
    /// <summary>
    /// A named expression or named range.
    /// </summary>
    NamedExpression = 13,
    /// <summary>
    /// A left parenthesis.
    /// </summary>
    LeftParenthesis = 14,
    /// <summary>
    /// A right parenthesis.
    /// </summary>
    RightParenthesis = 15,
    /// <summary>
    /// A left array brace.
    /// </summary>
    LeftBrace = 16,
    /// <summary>
    /// A right array brace.
    /// </summary>
    RightBrace = 17,
    /// <summary>
    /// A function-argument separator.
    /// </summary>
    Separator = 18,
    /// <summary>
    /// An array row separator.
    /// </summary>
    ArrayRowSeparator = 19,
    /// <summary>
    /// A formula namespace prefix.
    /// </summary>
    Prefix = 20,
    /// <summary>
    /// An operator.
    /// </summary>
    Operator = 21
}
