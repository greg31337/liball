namespace sql;

/// <summary>Specifies the syntax or catalog value described by a SQLite affinity entry.</summary>
public enum SqliteTypeAffinityTargetKind
{
    /// <summary>No recognized target is available.</summary>
    Unknown = 0,
    /// <summary>Represents a literal node.</summary>
    Literal = 1,
    /// <summary>Represents a catalog column.</summary>
    CatalogColumn = 2,
    /// <summary>Represents an expression node.</summary>
    Expression = 3,
    /// <summary>Represents a query or <c>RETURNING</c> result column.</summary>
    ResultColumn = 4,
    /// <summary>Represents a DML assignment or inserted value.</summary>
    Assignment = 5
}
