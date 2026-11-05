using System;

namespace sql;

/// <summary>Controls creation of a stable SQL representation for AST comparison.</summary>
public class SqlCanonicalizationOptions
{
    /// <summary>The dialect used to generate and reparse canonical SQL.</summary>
    public SqlDialectOptions Dialect;
    /// <summary>The casing applied to generated keywords.</summary>
    public SqlKeywordCase KeywordCase;
    /// <summary>The casing applied to unquoted identifiers.</summary>
    public SqlIdentifierCase IdentifierCase;
    /// <summary>Controls whether comments participate in the canonical representation.</summary>
    public bool PreserveComments;
    /// <summary>Controls whether statement terminators participate in the canonical representation.</summary>
    public bool IncludeSemicolons;

    /// <summary>Initializes canonicalization options with stable minimal-output defaults.</summary>
    public SqlCanonicalizationOptions()
    {
        Dialect = SqliteDialect.CreateOptions();
        KeywordCase = SqlKeywordCase.Upper;
        IdentifierCase = SqlIdentifierCase.Lower;
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
        formatOptions.Mode = SqlFormatMode.Minimal;
        formatOptions.KeywordDialect = Dialect;
        formatOptions.ReadDialect = Dialect;
        formatOptions.WriteDialect = Dialect;
        formatOptions.Validate();
    }
}
