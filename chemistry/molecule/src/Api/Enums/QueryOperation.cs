namespace molecule;


/// <summary>
/// Defines the supported values for query operation.
/// </summary>
public enum QueryOperation
{
    /// <summary>
    /// Specifies none.
    /// </summary>
    None = 0,
    /// <summary>
    /// Specifies equals.
    /// </summary>
    Equals = 1,
    /// <summary>
    /// Specifies not equals.
    /// </summary>
    NotEquals = 2,
    /// <summary>
    /// Specifies in list.
    /// </summary>
    InList = 3,
    /// <summary>
    /// Specifies not in list.
    /// </summary>
    NotInList = 4,
    /// <summary>
    /// Specifies less than.
    /// </summary>
    LessThan = 5,
    /// <summary>
    /// Specifies less than or equal.
    /// </summary>
    LessThanOrEqual = 6,
    /// <summary>
    /// Specifies greater than.
    /// </summary>
    GreaterThan = 7,
    /// <summary>
    /// Specifies greater than or equal.
    /// </summary>
    GreaterThanOrEqual = 8,
    /// <summary>
    /// Specifies and.
    /// </summary>
    And = 9,
    /// <summary>
    /// Specifies or.
    /// </summary>
    Or = 10,
    /// <summary>
    /// Specifies not.
    /// </summary>
    Not = 11,
    /// <summary>
    /// Specifies recursive.
    /// </summary>
    Recursive = 12
}

