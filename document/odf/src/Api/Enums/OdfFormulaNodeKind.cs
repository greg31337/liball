namespace odf;

/// <summary>
/// Identifies an OpenFormula syntax-tree node.
/// </summary>
public enum OdfFormulaNodeKind
{
    /// <summary>
    /// The node kind is absent or unrecognized.
    /// </summary>
    Unknown = 0,
    /// <summary>
    /// A scalar literal.
    /// </summary>
    Literal = 1,
    /// <summary>
    /// An error literal.
    /// </summary>
    Error = 2,
    /// <summary>
    /// A named expression or identifier.
    /// </summary>
    Name = 3,
    /// <summary>
    /// A cell reference.
    /// </summary>
    Reference = 4,
    /// <summary>
    /// A range reference.
    /// </summary>
    Range = 5,
    /// <summary>
    /// An external document reference.
    /// </summary>
    ExternalReference = 6,
    /// <summary>
    /// A unary operation.
    /// </summary>
    UnaryOperation = 7,
    /// <summary>
    /// A binary operation.
    /// </summary>
    BinaryOperation = 8,
    /// <summary>
    /// A function call.
    /// </summary>
    FunctionCall = 9,
    /// <summary>
    /// An array value.
    /// </summary>
    Array = 10,
    /// <summary>
    /// A reference intersection.
    /// </summary>
    Intersection = 11,
    /// <summary>
    /// A reference concatenation.
    /// </summary>
    ReferenceConcatenation = 12
}
