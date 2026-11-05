namespace sql;

/// <summary>Specifies the supported dialect kind.</summary>
public enum SqlDialectKind
{
    /// <summary>Represents generic.</summary>
    Generic = 0,
    /// <summary>Represents sq lite.</summary>
    SQLite = 1,
    /// <summary>Represents postgre sql.</summary>
    PostgreSql = 2,
    /// <summary>Represents my sql.</summary>
    MySql = 3,
    /// <summary>Represents maria db.</summary>
    MariaDb = 4,
    /// <summary>Represents sql server.</summary>
    SqlServer = 5,
    /// <summary>Represents oracle.</summary>
    Oracle = 6,
    /// <summary>Represents duck db.</summary>
    DuckDb = 7,
    /// <summary>Represents big query.</summary>
    BigQuery = 8,
    /// <summary>Represents snowflake.</summary>
    Snowflake = 9,
    /// <summary>Represents redshift.</summary>
    Redshift = 10,
    /// <summary>Represents databricks.</summary>
    Databricks = 11,
    /// <summary>Represents ansi.</summary>
    Ansi = 12
}
