namespace sql;

/// <summary>Describes a SQL dialect and creates independent options for parsing with it.</summary>
/// <remarks>Implement this interface to supply dialect options through APIs that accept a dialect descriptor.</remarks>
public interface ISqlDialect
{
    /// <summary>Returns the dialect kind represented by this descriptor.</summary>
    /// <returns>The resulting dialect kind.</returns>
    SqlDialectKind GetKind();

    /// <summary>Returns the display name represented by this descriptor.</summary>
    /// <returns>The resulting string.</returns>
    string GetName();

    /// <summary>Creates an independent set of options for this dialect.</summary>
    /// <returns>The resulting dialect options.</returns>
    SqlDialectOptions CreateOptions();

    /// <summary>Creates an immutable snapshot of this dialect's syntax policies.</summary>
    /// <returns>The resulting dialect policy.</returns>
    ISqlDialectPolicy CreatePolicy();
}
