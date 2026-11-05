namespace odf;

/// <summary>
/// Identifies an OpenFormula evaluation error.
/// </summary>
public enum OdfFormulaError
{
    /// <summary>
    /// No formula error.
    /// </summary>
    None = 0,
    /// <summary>
    /// The requested intersection is empty.
    /// </summary>
    Null = 1,
    /// <summary>
    /// A division-by-zero error.
    /// </summary>
    DivisionByZero = 2,
    /// <summary>
    /// An invalid value or operand type.
    /// </summary>
    Value = 3,
    /// <summary>
    /// An invalid reference.
    /// </summary>
    Reference = 4,
    /// <summary>
    /// An unknown name.
    /// </summary>
    Name = 5,
    /// <summary>
    /// An invalid numeric result.
    /// </summary>
    Number = 6,
    /// <summary>
    /// A requested value is unavailable.
    /// </summary>
    NotAvailable = 7,
    /// <summary>
    /// The formula uses an unsupported function.
    /// </summary>
    UnsupportedFunction = 8,
    /// <summary>
    /// The formula has a circular dependency.
    /// </summary>
    CircularReference = 9
}
