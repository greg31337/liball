namespace sql;

/// <summary>Specifies the supported join kind.</summary>
public enum SqlJoinKind
{
    /// <summary>No recognized value is available.</summary>
    Unknown = 0,
    /// <summary>Represents comma.</summary>
    Comma = 1,
    /// <summary>Represents cross.</summary>
    Cross = 2,
    /// <summary>Represents inner.</summary>
    Inner = 3,
    /// <summary>Represents left.</summary>
    Left = 4,
    /// <summary>Represents right.</summary>
    Right = 5,
    /// <summary>Represents full.</summary>
    Full = 6,
    /// <summary>Represents natural.</summary>
    Natural = 7,
    /// <summary>Represents a semi join.</summary>
    Semi = 8,
    /// <summary>Represents an anti join.</summary>
    Anti = 9,
    /// <summary>Represents a cross apply join.</summary>
    CrossApply = 10,
    /// <summary>Represents an outer apply join.</summary>
    OuterApply = 11,
    /// <summary>Represents an as-of join.</summary>
    AsOf = 12,
    /// <summary>Represents a straight join.</summary>
    Straight = 13
}
