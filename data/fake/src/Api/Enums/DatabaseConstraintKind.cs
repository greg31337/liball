namespace fake;

/// <summary>Selects the convention used for a generated database constraint name.</summary>
public enum DatabaseConstraintKind
{
    /// <summary>Uses a primary-key constraint prefix.</summary>
    PrimaryKey = 0,

    /// <summary>Uses a foreign-key constraint prefix.</summary>
    ForeignKey = 1,

    /// <summary>Uses a unique constraint prefix.</summary>
    Unique = 2,

    /// <summary>Uses a check constraint prefix.</summary>
    Check = 3
}
