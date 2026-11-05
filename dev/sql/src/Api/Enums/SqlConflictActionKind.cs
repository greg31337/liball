namespace sql;

/// <summary>Specifies the supported conflict action kind.</summary>
public enum SqlConflictActionKind
{
    /// <summary>No recognized value is available.</summary>
    None = 0,
    /// <summary>Represents rollback.</summary>
    Rollback = 1,
    /// <summary>Represents abort.</summary>
    Abort = 2,
    /// <summary>Represents fail.</summary>
    Fail = 3,
    /// <summary>Represents ignore.</summary>
    Ignore = 4,
    /// <summary>Represents replace.</summary>
    Replace = 5
}
