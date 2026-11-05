namespace jmap;

/// <summary>
/// Defines the operators used to combine JMAP filter conditions.
/// </summary>
public enum JmapFilterOperator
{
    /// <summary>All child conditions must match.</summary>
    And = 0,

    /// <summary>At least one child condition must match.</summary>
    Or = 1,

    /// <summary>The single child condition must not match.</summary>
    Not = 2
}
