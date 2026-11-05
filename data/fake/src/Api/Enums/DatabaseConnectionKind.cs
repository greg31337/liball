namespace fake;

/// <summary>Selects the database family used for generated connection-string-shaped text.</summary>
public enum DatabaseConnectionKind
{
    /// <summary>Uses PostgreSQL-style connection-string keys.</summary>
    PostgreSql = 0,

    /// <summary>Uses MySQL-style connection-string keys.</summary>
    MySql = 1,

    /// <summary>Uses Microsoft SQL Server-style connection-string keys.</summary>
    SqlServer = 2,

    /// <summary>Uses an in-memory SQLite connection-string shape.</summary>
    Sqlite = 3
}
