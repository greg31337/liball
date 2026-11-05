using System;

namespace sql;

/// <summary>Controls generation and reparsing of a normalized SQL syntax tree.</summary>
public class SqlNormalizationOptions
{
    /// <summary>The dialect used to generate and reparse normalized SQL.</summary>
    public SqlDialectOptions Dialect;
    /// <summary>The casing applied to generated keywords.</summary>
    public SqlKeywordCase KeywordCase;
    /// <summary>The casing applied to unquoted identifiers.</summary>
    public SqlIdentifierCase IdentifierCase;
    /// <summary>The layout used for normalized SQL.</summary>
    public SqlFormatMode Mode;
    /// <summary>Controls whether comments are retained in normalized SQL.</summary>
    public bool PreserveComments;
    /// <summary>Controls whether statement terminators are retained in normalized SQL.</summary>
    public bool IncludeSemicolons;

    /// <summary>Initializes normalization options with deterministic compact defaults.</summary>
    public SqlNormalizationOptions()
    {
        Dialect = SqliteDialect.CreateOptions();
        KeywordCase = SqlKeywordCase.Upper;
        IdentifierCase = SqlIdentifierCase.Preserve;
        Mode = SqlFormatMode.Compact;
        PreserveComments = false;
        IncludeSemicolons = true;
    }

    internal void Validate()
    {
        if (Dialect == null)
        {
            throw new ArgumentNullException("Dialect");
        }

        SqlFormatOptions formatOptions = new SqlFormatOptions();
        formatOptions.KeywordCase = KeywordCase;
        formatOptions.IdentifierCase = IdentifierCase;
        formatOptions.Mode = Mode;
        formatOptions.KeywordDialect = Dialect;
        formatOptions.ReadDialect = Dialect;
        formatOptions.WriteDialect = Dialect;
        formatOptions.Validate();
    }
}
