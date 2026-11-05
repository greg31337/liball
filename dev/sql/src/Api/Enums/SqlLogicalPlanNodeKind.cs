namespace sql;

/// <summary>Specifies a relational logical-plan operation.</summary>
public enum SqlLogicalPlanNodeKind
{
    /// <summary>Represents syntax whose relational meaning could not be established safely.</summary>
    Unknown = 0,
    /// <summary>Represents the ordered statements in a script.</summary>
    Script = 1,
    /// <summary>Represents projection of result expressions.</summary>
    Project = 2,
    /// <summary>Represents a catalog-resolved table or view scan.</summary>
    Scan = 3,
    /// <summary>Represents literal rows or a query without an input relation.</summary>
    Values = 4,
    /// <summary>Represents predicate filtering.</summary>
    Filter = 5,
    /// <summary>Represents a relational join.</summary>
    Join = 6,
    /// <summary>Represents grouping or aggregation.</summary>
    Aggregate = 7,
    /// <summary>Represents result ordering.</summary>
    Sort = 8,
    /// <summary>Represents row limiting or offsetting.</summary>
    Limit = 9,
    /// <summary>Represents a compound query set operation.</summary>
    SetOperation = 10,
    /// <summary>Represents insertion into a target relation.</summary>
    Insert = 11,
    /// <summary>Represents update of a target relation.</summary>
    Update = 12,
    /// <summary>Represents deletion from a target relation.</summary>
    Delete = 13,
    /// <summary>Represents conditional merge into a target relation.</summary>
    Merge = 14
}
