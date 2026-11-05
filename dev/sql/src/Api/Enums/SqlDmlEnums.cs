namespace sql;

/// <summary>Specifies the syntax introducing an upsert clause.</summary>
public enum SqlUpsertSyntaxKind
{
    /// <summary>No recognized value is available.</summary>
    Unknown = 0,
    /// <summary>Represents <c>ON CONFLICT</c>.</summary>
    OnConflict = 1,
    /// <summary>Represents <c>ON DUPLICATE KEY UPDATE</c>.</summary>
    OnDuplicateKey = 2
}

/// <summary>Specifies the form of an upsert conflict target.</summary>
public enum SqlConflictTargetKind
{
    /// <summary>No explicit target was written.</summary>
    None = 0,
    /// <summary>Represents an indexed-column target.</summary>
    IndexedColumns = 1,
    /// <summary>Represents a named constraint target.</summary>
    Constraint = 2,
    /// <summary>Represents the implicit duplicate-key target.</summary>
    DuplicateKey = 3
}

/// <summary>Specifies which source-target relationship activates a merge clause.</summary>
public enum SqlMergeMatchKind
{
    /// <summary>No recognized value is available.</summary>
    Unknown = 0,
    /// <summary>Represents <c>WHEN MATCHED</c>.</summary>
    Matched = 1,
    /// <summary>Represents <c>WHEN NOT MATCHED</c> or <c>WHEN NOT MATCHED BY TARGET</c>.</summary>
    NotMatchedByTarget = 2,
    /// <summary>Represents <c>WHEN NOT MATCHED BY SOURCE</c>.</summary>
    NotMatchedBySource = 3
}

/// <summary>Specifies the action performed by a merge clause.</summary>
public enum SqlMergeActionKind
{
    /// <summary>No recognized value is available.</summary>
    Unknown = 0,
    /// <summary>Represents an update action.</summary>
    Update = 1,
    /// <summary>Represents a delete action.</summary>
    Delete = 2,
    /// <summary>Represents an insert action.</summary>
    Insert = 3,
    /// <summary>Represents an action that makes no change.</summary>
    DoNothing = 4
}
