using System;

namespace sql;

/// <summary>Controls safe SQL syntax mapping between explicit read and write dialects.</summary>
public class SqlTranspileOptions
{
    /// <summary>The dialect used to produce the input AST.</summary>
    public SqlDialectOptions ReadDialect;
    /// <summary>The dialect targeted by generated SQL and the transformed AST.</summary>
    public SqlDialectOptions WriteDialect;
    /// <summary>The formatting applied to transpiled SQL.</summary>
    public SqlFormatOptions FormatOptions;
    /// <summary>Controls whether statement terminators are included in transpiled SQL.</summary>
    public bool IncludeSemicolons;

    /// <summary>Initializes transpilation options with generic read and write dialects.</summary>
    public SqlTranspileOptions()
    {
        ReadDialect = GenericDialect.CreateOptions();
        WriteDialect = GenericDialect.CreateOptions();
        FormatOptions = new SqlFormatOptions();
        FormatOptions.KeywordCase = SqlKeywordCase.Upper;
        FormatOptions.IdentifierCase = SqlIdentifierCase.Preserve;
        FormatOptions.Mode = SqlFormatMode.Compact;
        FormatOptions.PreserveComments = false;
        IncludeSemicolons = true;
    }

    internal void Validate()
    {
        if (ReadDialect == null)
        {
            throw new ArgumentNullException("ReadDialect");
        }

        if (WriteDialect == null)
        {
            throw new ArgumentNullException("WriteDialect");
        }

        if (FormatOptions == null)
        {
            throw new ArgumentNullException("FormatOptions");
        }

        ReadDialect.Validate();
        WriteDialect.Validate();
        FormatOptions.Validate();
    }
}
