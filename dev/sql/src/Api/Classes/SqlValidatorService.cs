namespace sql;

/// <summary>Validates parsed SQL scripts against catalog data.</summary>
public class SqlValidatorService : ISqlValidator
{
    /// <summary>Initializes a SQL validator.</summary>
    public SqlValidatorService()
    {
    }

    /// <summary>Validates a script against a catalog using default options.</summary>
    /// <param name="script">The script to validate.</param>
    /// <param name="catalog">The catalog used to resolve names.</param>
    /// <returns>The validation result.</returns>
    public SqlValidationResult Validate(SqlScript script, SqlCatalog catalog)
    {
        return SqlValidation.Validate(script, catalog);
    }

    /// <summary>Validates a script against a catalog using explicit options.</summary>
    /// <param name="script">The script to validate.</param>
    /// <param name="catalog">The catalog used to resolve names.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The validation result.</returns>
    public SqlValidationResult ValidateWithOptions(SqlScript script, SqlCatalog catalog, SqlValidationOptions options)
    {
        return SqlValidation.Validate(script, catalog, options);
    }
}
