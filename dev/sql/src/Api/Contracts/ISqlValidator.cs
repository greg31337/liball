namespace sql;

/// <summary>Defines semantic validation operations for parsed SQL scripts.</summary>
public interface ISqlValidator
{
    /// <summary>Validates a script against a catalog using default options.</summary>
    /// <param name="script">The script to validate.</param>
    /// <param name="catalog">The catalog used to resolve names.</param>
    /// <returns>The validation result.</returns>
    SqlValidationResult Validate(SqlScript script, SqlCatalog catalog);

    /// <summary>Validates a script against a catalog using explicit options.</summary>
    /// <param name="script">The script to validate.</param>
    /// <param name="catalog">The catalog used to resolve names.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The validation result.</returns>
    SqlValidationResult ValidateWithOptions(SqlScript script, SqlCatalog catalog, SqlValidationOptions options);
}
