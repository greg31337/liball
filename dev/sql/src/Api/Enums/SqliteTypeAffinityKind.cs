namespace sql;

/// <summary>Specifies SQLite's storage-class affinity for a declared or inferred value.</summary>
public enum SqliteTypeAffinityKind
{
    /// <summary>The affinity cannot be determined safely.</summary>
    Unknown = 0,
    /// <summary>The value has BLOB affinity and receives no preferred storage-class conversion.</summary>
    Blob = 1,
    /// <summary>The value has TEXT affinity.</summary>
    Text = 2,
    /// <summary>The value has NUMERIC affinity.</summary>
    Numeric = 3,
    /// <summary>The value has INTEGER affinity.</summary>
    Integer = 4,
    /// <summary>The value has REAL affinity.</summary>
    Real = 5
}
