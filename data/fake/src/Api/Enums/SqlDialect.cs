namespace fake;

/// <summary>Selects the SQL text conventions used by record export.</summary>
public enum SqlDialect
{
    /// <summary>Uses ANSI double-quoted identifiers and standard literals.</summary>
    Ansi = 0,

    /// <summary>Uses PostgreSQL double-quoted identifiers and native boolean and binary literals.</summary>
    PostgreSql = 1,

    /// <summary>Uses MySQL backtick-delimited identifiers and backslash-safe text literals.</summary>
    MySql = 2,

    /// <summary>Uses SQL Server bracket-delimited identifiers and Unicode text literals.</summary>
    SqlServer = 3,

    /// <summary>Uses SQLite double-quoted identifiers and numeric boolean literals.</summary>
    Sqlite = 4
}
