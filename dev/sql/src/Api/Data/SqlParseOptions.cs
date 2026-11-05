using System;

namespace sql;

/// <summary>Controls parse behavior.</summary>
public class SqlParseOptions
{
    /// <summary>The dialect rules used by the lexer and parser.</summary>
    public SqlDialectOptions Dialect;
    /// <summary>Controls whether recovery is enabled.</summary>
    public bool EnableRecovery;
    /// <summary>The maximum recovery steps allowed before processing stops.</summary>
    public int MaxRecoverySteps;
    /// <summary>The maximum diagnostics allowed before processing stops.</summary>
    public int MaxDiagnostics;
    /// <summary>Controls whether parsing stops after the first error diagnostic.</summary>
    public bool StopOnFirstError;
    /// <summary>Controls whether a script may contain more than one statement.</summary>
    public bool AllowMultipleStatements;
    /// <summary>Controls whether extra semicolons produce empty statement nodes instead of errors.</summary>
    public bool AllowEmptyStatements;
    /// <summary>Controls whether parsed tokens are retained in result objects.</summary>
    public bool PreserveTokens;
    /// <summary>Controls whether whitespace and comments are attached to retained tokens.</summary>
    public bool PreserveTrivia;

    /// <summary>Initializes parse options with the library defaults.</summary>
    public SqlParseOptions()
    {
        Dialect = GenericDialect.CreateOptions();
        EnableRecovery = true;
        MaxRecoverySteps = 1000;
        MaxDiagnostics = 100;
        StopOnFirstError = false;
        AllowMultipleStatements = true;
        AllowEmptyStatements = true;
        PreserveTokens = true;
        PreserveTrivia = true;
    }

    /// <summary>Creates parse options for the supplied dialect options.</summary>
    /// <param name="dialect">The dialect to use.</param>
    /// <exception cref="ArgumentNullException">dialect is <c>null</c>.</exception>
    /// <returns>The resulting parse options.</returns>
    internal static SqlParseOptions Create(SqlDialectOptions dialect)
    {
        if (dialect == null)
        {
            throw new ArgumentNullException("dialect");
        }

        SqlParseOptions options = new SqlParseOptions();
        options.Dialect = dialect;
        return options;
    }

    /// <summary>Creates a new value initialized with the library's default settings.</summary>
    /// <returns>The resulting parse options.</returns>
    internal static SqlParseOptions CreateDefault()
    {
        return new SqlParseOptions();
    }

    /// <summary>Creates a new value initialized for the SQLite dialect.</summary>
    /// <returns>The resulting parse options.</returns>
    internal static SqlParseOptions CreateSqlite()
    {
        return Create(SqliteDialect.CreateOptions());
    }

    /// <summary>Creates parse options from the supplied dialect descriptor.</summary>
    /// <param name="dialect">The dialect descriptor to use.</param>
    /// <returns>The resulting parse options.</returns>
    /// <exception cref="ArgumentNullException">dialect is <c>null</c> or returns <c>null</c> options.</exception>
    internal static SqlParseOptions CreateForDialect(ISqlDialect dialect)
    {
        if (dialect == null)
        {
            throw new ArgumentNullException("dialect");
        }

        SqlDialectOptions options = dialect.CreateOptions();
        if (options == null)
        {
            throw new ArgumentNullException("dialect");
        }

        return Create(options);
    }

    /// <summary>Returns the dialect kind associated with this value.</summary>
    /// <returns>The resulting dialect kind.</returns>
    internal SqlDialectKind GetDialectKind()
    {
        Validate();
        return Dialect.Kind;
    }

    /// <summary>Returns whether parsing should attempt recovery after a syntax error.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool ShouldRecover()
    {
        Validate();
        return EnableRecovery && MaxRecoverySteps > 0 && !StopOnFirstError;
    }

    /// <summary>Returns whether the configured diagnostic limit has been reached.</summary>
    /// <param name="diagnosticCount">The diagnostic count to use.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">diagnosticCount is outside the supported range or otherwise invalid.</exception>
    internal bool ShouldStopAfterDiagnosticCount(int diagnosticCount)
    {
        Validate();
        if (diagnosticCount < 0)
        {
            throw new ArgumentOutOfRangeException("diagnosticCount");
        }

        if (MaxDiagnostics == 0)
        {
            return true;
        }

        return diagnosticCount >= MaxDiagnostics;
    }

    /// <summary>Creates an independent copy of this value.</summary>
    /// <returns>The resulting parse options.</returns>
    internal SqlParseOptions Copy()
    {
        Validate();
        SqlParseOptions copy = Create(Dialect);

        copy.EnableRecovery = EnableRecovery;
        copy.MaxRecoverySteps = MaxRecoverySteps;
        copy.MaxDiagnostics = MaxDiagnostics;
        copy.StopOnFirstError = StopOnFirstError;
        copy.AllowMultipleStatements = AllowMultipleStatements;
        copy.AllowEmptyStatements = AllowEmptyStatements;
        copy.PreserveTokens = PreserveTokens;
        copy.PreserveTrivia = PreserveTrivia;

        return copy;
    }

    /// <summary>Checks that the dialect and numeric limits are valid before parsing starts.</summary>
    /// <exception cref="ArgumentNullException">Dialect is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">MaxRecoverySteps is outside the supported range or otherwise invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">MaxDiagnostics is outside the supported range or otherwise invalid.</exception>
    internal void Validate()
    {
        if (Dialect == null)
        {
            throw new ArgumentNullException("Dialect");
        }

        Dialect.Validate();

        if (MaxRecoverySteps < 0)
        {
            throw new ArgumentOutOfRangeException("MaxRecoverySteps");
        }

        if (MaxDiagnostics < 0)
        {
            throw new ArgumentOutOfRangeException("MaxDiagnostics");
        }
    }
}
