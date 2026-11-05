namespace sql;

/// <summary>Specifies an explicitly written join modifier.</summary>
public enum SqlJoinModifierKind
{
    /// <summary>No recognized value is available.</summary>
    None = 0,
    /// <summary>Represents <c>NATURAL</c>.</summary>
    Natural = 1,
    /// <summary>Represents <c>INNER</c>.</summary>
    Inner = 2,
    /// <summary>Represents <c>LEFT</c>.</summary>
    Left = 3,
    /// <summary>Represents <c>RIGHT</c>.</summary>
    Right = 4,
    /// <summary>Represents <c>FULL</c>.</summary>
    Full = 5,
    /// <summary>Represents <c>CROSS</c>.</summary>
    Cross = 6,
    /// <summary>Represents <c>OUTER</c>.</summary>
    Outer = 7,
    /// <summary>Represents <c>SEMI</c>.</summary>
    Semi = 8,
    /// <summary>Represents <c>ANTI</c>.</summary>
    Anti = 9,
    /// <summary>Represents <c>APPLY</c>.</summary>
    Apply = 10,
    /// <summary>Represents <c>ASOF</c>.</summary>
    AsOf = 11,
    /// <summary>Represents <c>STRAIGHT_JOIN</c>.</summary>
    Straight = 12
}

/// <summary>Specifies whether a fetch clause uses <c>FIRST</c> or <c>NEXT</c>.</summary>
public enum SqlFetchDirectionKind
{
    /// <summary>No recognized value is available.</summary>
    None = 0,
    /// <summary>Represents <c>FIRST</c>.</summary>
    First = 1,
    /// <summary>Represents <c>NEXT</c>.</summary>
    Next = 2
}

/// <summary>Classifies a preserved query clause whose semantics are not modeled.</summary>
public enum SqlUnsupportedQueryClauseKind
{
    /// <summary>No recognized value is available.</summary>
    None = 0,
    /// <summary>Represents a <c>QUALIFY</c> clause.</summary>
    Qualify = 1,
    /// <summary>Represents a sampling clause.</summary>
    Sample = 2,
    /// <summary>Represents a pivot clause.</summary>
    Pivot = 3,
    /// <summary>Represents an unpivot clause.</summary>
    Unpivot = 4,
    /// <summary>Represents an Oracle <c>CONNECT BY</c> clause.</summary>
    ConnectBy = 5,
    /// <summary>Represents an Oracle <c>MODEL</c> clause.</summary>
    Model = 6,
    /// <summary>Represents another preserved dialect-specific clause.</summary>
    Other = 7
}
