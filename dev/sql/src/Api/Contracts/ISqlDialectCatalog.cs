namespace sql;

/// <summary>Defines access to built-in SQL dialect descriptors and options.</summary>
public interface ISqlDialectCatalog
{
    /// <summary>Returns a descriptor for a built-in dialect.</summary>
    /// <param name="kind">The dialect kind.</param>
    /// <returns>The dialect descriptor.</returns>
    ISqlDialect GetDialect(SqlDialectKind kind);

    /// <summary>Returns the display name for a built-in dialect.</summary>
    /// <param name="kind">The dialect kind.</param>
    /// <returns>The dialect display name.</returns>
    string GetName(SqlDialectKind kind);

    /// <summary>Creates independent options for a built-in dialect.</summary>
    /// <param name="kind">The dialect kind.</param>
    /// <returns>The dialect options.</returns>
    SqlDialectOptions CreateOptions(SqlDialectKind kind);
}
