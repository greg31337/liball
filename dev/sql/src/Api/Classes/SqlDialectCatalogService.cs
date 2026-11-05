namespace sql;

/// <summary>Provides access to the library's built-in SQL dialects.</summary>
public class SqlDialectCatalogService : ISqlDialectCatalog
{
    /// <summary>Initializes a dialect catalog service.</summary>
    public SqlDialectCatalogService()
    {
    }

    /// <summary>Returns a descriptor for a built-in dialect.</summary>
    /// <param name="kind">The dialect kind.</param>
    /// <returns>The dialect descriptor.</returns>
    public ISqlDialect GetDialect(SqlDialectKind kind)
    {
        return SqlDialectCatalog.GetDialect(kind);
    }

    /// <summary>Returns the display name for a built-in dialect.</summary>
    /// <param name="kind">The dialect kind.</param>
    /// <returns>The dialect display name.</returns>
    public string GetName(SqlDialectKind kind)
    {
        return SqlDialectCatalog.GetName(kind);
    }

    /// <summary>Creates independent options for a built-in dialect.</summary>
    /// <param name="kind">The dialect kind.</param>
    /// <returns>The dialect options.</returns>
    public SqlDialectOptions CreateOptions(SqlDialectKind kind)
    {
        return SqlDialectCatalog.CreateOptions(kind);
    }
}
