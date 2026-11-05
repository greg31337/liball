using System;

namespace sql;

/// <summary>Contains target-dialect SQL, its AST, safe mappings, and explicit unsupported syntax.</summary>
public class SqlTranspileResult
{
    /// <summary>The unchanged input syntax tree.</summary>
    public SqlScript InputScript;
    /// <summary>The syntax tree parsed from target-dialect SQL.</summary>
    public SqlScript Script;
    /// <summary>The generated target-dialect SQL.</summary>
    public string Sql;
    /// <summary>The read dialect supplied to the operation.</summary>
    public SqlDialectOptions ReadDialect;
    /// <summary>The write dialect supplied to the operation.</summary>
    public SqlDialectOptions WriteDialect;
    /// <summary>The safe syntax mappings applied by the operation.</summary>
    public SqlTranspileMapping[] Mappings;
    /// <summary>The source markers for syntax that has no lossless target-dialect mapping.</summary>
    public SqlUnsupportedSyntax[] UnsupportedSyntax;
    /// <summary>The compatibility and target-dialect reparse diagnostics.</summary>
    public SqlDiagnosticBag Diagnostics;
    /// <summary>Indicates that every source construct was represented without a lossy mapping.</summary>
    public bool IsLossless;

    /// <summary>Initializes a dialect transpilation result.</summary>
    /// <param name="inputScript">The unchanged input script.</param>
    /// <param name="script">The target-dialect script.</param>
    /// <param name="sql">The generated target-dialect SQL.</param>
    /// <param name="readDialect">The read dialect.</param>
    /// <param name="writeDialect">The write dialect.</param>
    /// <param name="mappings">The safe mappings.</param>
    /// <param name="unsupportedSyntax">The unsupported syntax markers.</param>
    /// <param name="diagnostics">The operation diagnostics.</param>
    /// <param name="isLossless">Whether all syntax was mapped losslessly.</param>
    /// <exception cref="ArgumentNullException">Any required reference or an array item is <c>null</c>.</exception>
    public SqlTranspileResult(SqlScript inputScript, SqlScript script, string sql, SqlDialectOptions readDialect, SqlDialectOptions writeDialect, SqlTranspileMapping[] mappings, SqlUnsupportedSyntax[] unsupportedSyntax, SqlDiagnosticBag diagnostics, bool isLossless)
    {
        InputScript = inputScript ?? throw new ArgumentNullException("inputScript");
        Script = script ?? throw new ArgumentNullException("script");
        Sql = sql ?? throw new ArgumentNullException("sql");
        ReadDialect = readDialect ?? throw new ArgumentNullException("readDialect");
        WriteDialect = writeDialect ?? throw new ArgumentNullException("writeDialect");
        ValidateMappings(mappings);
        ValidateUnsupportedSyntax(unsupportedSyntax);
        Mappings = mappings;
        UnsupportedSyntax = unsupportedSyntax;
        Diagnostics = diagnostics ?? throw new ArgumentNullException("diagnostics");
        IsLossless = isLossless;
    }

    internal static void ValidateMappings(SqlTranspileMapping[] mappings)
    {
        if (mappings == null)
        {
            throw new ArgumentNullException("mappings");
        }

        int index = 0;
        while (index < mappings.Length)
        {
            if (mappings[index] == null)
            {
                throw new ArgumentNullException("mappings");
            }

            index = index + 1;
        }
    }

    internal static void ValidateUnsupportedSyntax(SqlUnsupportedSyntax[] unsupportedSyntax)
    {
        if (unsupportedSyntax == null)
        {
            throw new ArgumentNullException("unsupportedSyntax");
        }

        int index = 0;
        while (index < unsupportedSyntax.Length)
        {
            if (unsupportedSyntax[index] == null)
            {
                throw new ArgumentNullException("unsupportedSyntax");
            }

            index = index + 1;
        }
    }
}
