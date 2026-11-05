namespace sql;

/// <summary>Specifies how a lineage target obtains its value.</summary>
public enum SqlColumnLineageOriginKind
{
    /// <summary>The value's origin could not be established.</summary>
    Unknown = 0,
    /// <summary>The value comes directly from one resolved source column.</summary>
    DirectColumn = 1,
    /// <summary>The value is computed by an expression.</summary>
    Expression = 2,
    /// <summary>The value can refer to more than one matching source column.</summary>
    Ambiguous = 3
}

/// <summary>Specifies the syntax target described by a lineage entry.</summary>
public enum SqlColumnLineageTargetKind
{
    /// <summary>No recognized target is available.</summary>
    Unknown = 0,
    /// <summary>Represents a query result column.</summary>
    QueryOutput = 1,
    /// <summary>Represents a <c>RETURNING</c> result column.</summary>
    ReturningOutput = 2,
    /// <summary>Represents a column populated by an insert.</summary>
    InsertAssignment = 3,
    /// <summary>Represents a column changed by an update.</summary>
    UpdateAssignment = 4,
    /// <summary>Represents a column changed by an upsert.</summary>
    UpsertAssignment = 5,
    /// <summary>Represents a column populated or changed by a merge action.</summary>
    MergeAssignment = 6
}
