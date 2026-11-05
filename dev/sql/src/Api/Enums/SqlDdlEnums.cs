namespace sql;

/// <summary>Specifies a SQLite foreign-key action.</summary>
public enum SqlForeignKeyActionKind
{
    /// <summary>No action clause was specified.</summary>
    None = 0,
    /// <summary>Represents <c>NO ACTION</c>.</summary>
    NoAction = 1,
    /// <summary>Represents <c>RESTRICT</c>.</summary>
    Restrict = 2,
    /// <summary>Represents <c>SET NULL</c>.</summary>
    SetNull = 3,
    /// <summary>Represents <c>SET DEFAULT</c>.</summary>
    SetDefault = 4,
    /// <summary>Represents <c>CASCADE</c>.</summary>
    Cascade = 5
}

/// <summary>Specifies whether a SQLite foreign key is explicitly deferrable.</summary>
public enum SqlForeignKeyDeferrabilityKind
{
    /// <summary>No deferrability clause was specified.</summary>
    None = 0,
    /// <summary>Represents <c>DEFERRABLE</c>.</summary>
    Deferrable = 1,
    /// <summary>Represents <c>NOT DEFERRABLE</c>.</summary>
    NotDeferrable = 2
}

/// <summary>Specifies the initial timing of a SQLite foreign key.</summary>
public enum SqlForeignKeyInitialTimingKind
{
    /// <summary>No initial timing clause was specified.</summary>
    None = 0,
    /// <summary>Represents <c>INITIALLY DEFERRED</c>.</summary>
    Deferred = 1,
    /// <summary>Represents <c>INITIALLY IMMEDIATE</c>.</summary>
    Immediate = 2
}

/// <summary>Specifies a clause in a SQLite foreign-key reference.</summary>
public enum SqlForeignKeyClauseKind
{
    /// <summary>No recognized value is available.</summary>
    Unknown = 0,
    /// <summary>Represents an <c>ON DELETE</c> clause.</summary>
    OnDelete = 1,
    /// <summary>Represents an <c>ON UPDATE</c> clause.</summary>
    OnUpdate = 2,
    /// <summary>Represents a <c>MATCH</c> clause.</summary>
    Match = 3,
    /// <summary>Represents a deferrability clause.</summary>
    Deferrability = 4
}

/// <summary>Specifies a SQLite table option.</summary>
public enum SqlTableOptionKind
{
    /// <summary>No recognized value is available.</summary>
    Unknown = 0,
    /// <summary>Represents <c>WITHOUT ROWID</c>.</summary>
    WithoutRowId = 1,
    /// <summary>Represents <c>STRICT</c>.</summary>
    Strict = 2
}
