namespace sql;

/// <summary>Specifies the supported insert source kind.</summary>
public enum SqlInsertSourceKind
{
    /// <summary>No recognized value is available.</summary>
    Unknown = 0,
    /// <summary>Represents values.</summary>
    Values = 1,
    /// <summary>Represents select.</summary>
    Select = 2,
    /// <summary>Represents default values.</summary>
    DefaultValues = 3
}

/// <summary>Specifies the supported upsert action kind.</summary>
public enum SqlUpsertActionKind
{
    /// <summary>No recognized value is available.</summary>
    Unknown = 0,
    /// <summary>Represents do nothing.</summary>
    DoNothing = 1,
    /// <summary>Represents do update.</summary>
    DoUpdate = 2
}

/// <summary>Specifies the supported pragma value kind.</summary>
public enum SqlPragmaValueKind
{
    /// <summary>No recognized value is available.</summary>
    None = 0,
    /// <summary>Represents equals.</summary>
    Equals = 1,
    /// <summary>Represents parenthesized.</summary>
    Parenthesized = 2
}

/// <summary>Specifies the supported transaction action kind.</summary>
public enum SqlTransactionActionKind
{
    /// <summary>No recognized value is available.</summary>
    Unknown = 0,
    /// <summary>Represents begin.</summary>
    Begin = 1,
    /// <summary>Represents commit.</summary>
    Commit = 2,
    /// <summary>Represents end.</summary>
    End = 3,
    /// <summary>Represents rollback.</summary>
    Rollback = 4
}

/// <summary>Specifies the supported transaction mode.</summary>
public enum SqlTransactionMode
{
    /// <summary>No recognized value is available.</summary>
    None = 0,
    /// <summary>Represents deferred.</summary>
    Deferred = 1,
    /// <summary>Represents immediate.</summary>
    Immediate = 2,
    /// <summary>Represents exclusive.</summary>
    Exclusive = 3
}

/// <summary>Specifies the supported compound operator kind.</summary>
public enum SqlCompoundOperatorKind
{
    /// <summary>No recognized value is available.</summary>
    None = 0,
    /// <summary>Represents union.</summary>
    Union = 1,
    /// <summary>Represents union all.</summary>
    UnionAll = 2,
    /// <summary>Represents intersect.</summary>
    Intersect = 3,
    /// <summary>Represents except.</summary>
    Except = 4
}

/// <summary>Specifies the supported cte materialization kind.</summary>
public enum SqlCteMaterializationKind
{
    /// <summary>No recognized value is available.</summary>
    None = 0,
    /// <summary>Represents materialized.</summary>
    Materialized = 1,
    /// <summary>Represents not materialized.</summary>
    NotMaterialized = 2
}

/// <summary>Specifies the supported join constraint kind.</summary>
public enum SqlJoinConstraintKind
{
    /// <summary>No recognized value is available.</summary>
    None = 0,
    /// <summary>Represents on.</summary>
    On = 1,
    /// <summary>Represents using.</summary>
    Using = 2
}

/// <summary>Specifies the supported ordering direction.</summary>
public enum SqlOrderingDirection
{
    /// <summary>No recognized value is available.</summary>
    None = 0,
    /// <summary>Represents asc.</summary>
    Asc = 1,
    /// <summary>Represents desc.</summary>
    Desc = 2
}

/// <summary>Specifies the supported null ordering.</summary>
public enum SqlNullOrdering
{
    /// <summary>No recognized value is available.</summary>
    None = 0,
    /// <summary>Represents first.</summary>
    First = 1,
    /// <summary>Represents last.</summary>
    Last = 2
}

/// <summary>Specifies the supported window frame kind.</summary>
public enum SqlWindowFrameKind
{
    /// <summary>No recognized value is available.</summary>
    None = 0,
    /// <summary>Represents rows.</summary>
    Rows = 1,
    /// <summary>Represents range.</summary>
    Range = 2,
    /// <summary>Represents groups.</summary>
    Groups = 3
}

/// <summary>Specifies the supported window frame boundary kind.</summary>
public enum SqlWindowFrameBoundaryKind
{
    /// <summary>No recognized value is available.</summary>
    None = 0,
    /// <summary>Represents unbounded preceding.</summary>
    UnboundedPreceding = 1,
    /// <summary>Represents preceding.</summary>
    Preceding = 2,
    /// <summary>Represents current row.</summary>
    CurrentRow = 3,
    /// <summary>Represents following.</summary>
    Following = 4,
    /// <summary>Represents unbounded following.</summary>
    UnboundedFollowing = 5
}

/// <summary>Specifies the supported window frame exclusion kind.</summary>
public enum SqlWindowFrameExclusionKind
{
    /// <summary>No recognized value is available.</summary>
    None = 0,
    /// <summary>Represents current row.</summary>
    CurrentRow = 1,
    /// <summary>Represents group.</summary>
    Group = 2,
    /// <summary>Represents ties.</summary>
    Ties = 3,
    /// <summary>Represents no others.</summary>
    NoOthers = 4
}

/// <summary>Specifies the supported generated column kind.</summary>
public enum SqlGeneratedColumnKind
{
    /// <summary>No recognized value is available.</summary>
    None = 0,
    /// <summary>Represents virtual.</summary>
    Virtual = 1,
    /// <summary>Represents stored.</summary>
    Stored = 2
}

/// <summary>Specifies the supported alter table action kind.</summary>
public enum SqlAlterTableActionKind
{
    /// <summary>No recognized value is available.</summary>
    Unknown = 0,
    /// <summary>Represents rename table.</summary>
    RenameTable = 1,
    /// <summary>Represents rename column.</summary>
    RenameColumn = 2,
    /// <summary>Represents add column.</summary>
    AddColumn = 3,
    /// <summary>Represents drop column.</summary>
    DropColumn = 4
}

/// <summary>Specifies the supported table reference kind.</summary>
public enum SqlTableReferenceKind
{
    /// <summary>No recognized value is available.</summary>
    Unknown = 0,
    /// <summary>Represents named.</summary>
    Named = 1,
    /// <summary>Represents subquery.</summary>
    Subquery = 2,
    /// <summary>Represents function.</summary>
    Function = 3,
    /// <summary>Represents join.</summary>
    Join = 4,
    /// <summary>Represents parenthesized.</summary>
    Parenthesized = 5,
    /// <summary>Represents values.</summary>
    Values = 6
}

/// <summary>Specifies the supported trigger timing.</summary>
public enum SqlTriggerTiming
{
    /// <summary>No recognized value is available.</summary>
    Unknown = 0,
    /// <summary>Represents before.</summary>
    Before = 1,
    /// <summary>Represents after.</summary>
    After = 2,
    /// <summary>Represents instead of.</summary>
    InsteadOf = 3
}

/// <summary>Specifies the supported trigger event.</summary>
public enum SqlTriggerEvent
{
    /// <summary>No recognized value is available.</summary>
    Unknown = 0,
    /// <summary>Represents insert.</summary>
    Insert = 1,
    /// <summary>Represents update.</summary>
    Update = 2,
    /// <summary>Represents delete.</summary>
    Delete = 3
}
