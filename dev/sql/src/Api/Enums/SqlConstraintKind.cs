namespace sql;

/// <summary>Specifies the supported constraint kind.</summary>
public enum SqlConstraintKind
{
    /// <summary>No recognized value is available.</summary>
    Unknown = 0,
    /// <summary>Represents primary key.</summary>
    PrimaryKey = 1,
    /// <summary>Represents not null.</summary>
    NotNull = 2,
    /// <summary>Represents unique.</summary>
    Unique = 3,
    /// <summary>Represents check.</summary>
    Check = 4,
    /// <summary>Represents default.</summary>
    Default = 5,
    /// <summary>Represents collate.</summary>
    Collate = 6,
    /// <summary>Represents references.</summary>
    References = 7,
    /// <summary>Represents generated.</summary>
    Generated = 8,
    /// <summary>Represents foreign key.</summary>
    ForeignKey = 9
}
