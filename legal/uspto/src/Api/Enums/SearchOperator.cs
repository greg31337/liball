namespace uspto;

/// <summary>
/// Defines operators used by structured remote and local search predicates.
/// </summary>
public enum SearchOperator
{
    /// <summary>No search operator has been selected.</summary>
    None = 0,

    /// <summary>Tests values for equality.</summary>
    Equal = 1,

    /// <summary>Tests values for inequality.</summary>
    NotEqual = 2,

    /// <summary>Tests whether the left value is less than the right value.</summary>
    LessThan = 3,

    /// <summary>Tests whether the left value is less than or equal to the right value.</summary>
    LessThanOrEqual = 4,

    /// <summary>Tests whether the left value is greater than the right value.</summary>
    GreaterThan = 5,

    /// <summary>Tests whether the left value is greater than or equal to the right value.</summary>
    GreaterThanOrEqual = 6,

    /// <summary>Tests whether text contains a value.</summary>
    Contains = 7,

    /// <summary>Tests whether a value is present in a supplied set.</summary>
    In = 8,

    /// <summary>Requires all child predicates to match.</summary>
    And = 9,

    /// <summary>Requires at least one child predicate to match.</summary>
    Or = 10,

    /// <summary>Negates a child predicate.</summary>
    Not = 11
}
