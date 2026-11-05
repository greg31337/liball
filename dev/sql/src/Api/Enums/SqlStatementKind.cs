namespace sql;

/// <summary>Specifies the supported statement kind.</summary>
public enum SqlStatementKind
{
    /// <summary>No recognized value is available.</summary>
    Unknown = 0,
    /// <summary>Represents empty.</summary>
    Empty = 1,
    /// <summary>Represents select.</summary>
    Select = 2,
    /// <summary>Represents insert.</summary>
    Insert = 3,
    /// <summary>Represents update.</summary>
    Update = 4,
    /// <summary>Represents delete.</summary>
    Delete = 5,
    /// <summary>Represents create table.</summary>
    CreateTable = 6,
    /// <summary>Represents alter table.</summary>
    AlterTable = 7,
    /// <summary>Represents drop table.</summary>
    DropTable = 8,
    /// <summary>Represents create index.</summary>
    CreateIndex = 9,
    /// <summary>Represents drop index.</summary>
    DropIndex = 10,
    /// <summary>Represents create view.</summary>
    CreateView = 11,
    /// <summary>Represents drop view.</summary>
    DropView = 12,
    /// <summary>Represents create trigger.</summary>
    CreateTrigger = 13,
    /// <summary>Represents drop trigger.</summary>
    DropTrigger = 14,
    /// <summary>Represents create virtual table.</summary>
    CreateVirtualTable = 15,
    /// <summary>Represents pragma.</summary>
    Pragma = 16,
    /// <summary>Represents analyze.</summary>
    Analyze = 17,
    /// <summary>Represents vacuum.</summary>
    Vacuum = 18,
    /// <summary>Represents attach.</summary>
    Attach = 19,
    /// <summary>Represents detach.</summary>
    Detach = 20,
    /// <summary>Represents reindex.</summary>
    Reindex = 21,
    /// <summary>Represents begin.</summary>
    Begin = 22,
    /// <summary>Represents commit.</summary>
    Commit = 23,
    /// <summary>Represents rollback.</summary>
    Rollback = 24,
    /// <summary>Represents savepoint.</summary>
    Savepoint = 25,
    /// <summary>Represents release.</summary>
    Release = 26,
    /// <summary>Represents explain.</summary>
    Explain = 27,
    /// <summary>Represents merge.</summary>
    Merge = 28,
    /// <summary>Represents end.</summary>
    End = 29
}
