namespace sql;

/// <summary>Provides read-only searches and summaries for SQL syntax trees.</summary>
public class SqlAnalyzerService : ISqlAnalyzer
{
    /// <summary>Initializes a SQL analyzer.</summary>
    public SqlAnalyzerService()
    {
    }

    /// <summary>Finds referenced tables in a script.</summary>
    /// <param name="script">The script to search.</param>
    /// <returns>The referenced names in encounter order.</returns>
    public SqlName[] FindTablesInScript(SqlScript? script)
    {
        return SqlAnalyzer.FindTables(script);
    }

    /// <summary>Finds referenced tables in a statement.</summary>
    /// <param name="statement">The statement to search.</param>
    /// <returns>The referenced names in encounter order.</returns>
    public SqlName[] FindTablesInStatement(SqlStatement? statement)
    {
        return SqlAnalyzer.FindTables(statement);
    }

    /// <summary>Finds referenced tables in a query.</summary>
    /// <param name="query">The query to search.</param>
    /// <returns>The referenced names in encounter order.</returns>
    public SqlName[] FindTablesInQuery(SqlQuery? query)
    {
        return SqlAnalyzer.FindTables(query);
    }

    /// <summary>Finds referenced tables in an expression.</summary>
    /// <param name="expression">The expression to search.</param>
    /// <returns>The referenced names in encounter order.</returns>
    public SqlName[] FindTablesInExpression(SqlExpression? expression)
    {
        return SqlAnalyzer.FindTables(expression);
    }

    /// <summary>Finds referenced columns in a script.</summary>
    /// <param name="script">The script to search.</param>
    /// <returns>The referenced names in encounter order.</returns>
    public SqlName[] FindColumnsInScript(SqlScript? script)
    {
        return SqlAnalyzer.FindColumns(script);
    }

    /// <summary>Finds referenced columns in a statement.</summary>
    /// <param name="statement">The statement to search.</param>
    /// <returns>The referenced names in encounter order.</returns>
    public SqlName[] FindColumnsInStatement(SqlStatement? statement)
    {
        return SqlAnalyzer.FindColumns(statement);
    }

    /// <summary>Finds referenced columns in a query.</summary>
    /// <param name="query">The query to search.</param>
    /// <returns>The referenced names in encounter order.</returns>
    public SqlName[] FindColumnsInQuery(SqlQuery? query)
    {
        return SqlAnalyzer.FindColumns(query);
    }

    /// <summary>Finds referenced columns in an expression.</summary>
    /// <param name="expression">The expression to search.</param>
    /// <returns>The referenced names in encounter order.</returns>
    public SqlName[] FindColumnsInExpression(SqlExpression? expression)
    {
        return SqlAnalyzer.FindColumns(expression);
    }

    /// <summary>Finds parameters in a script.</summary>
    /// <param name="script">The script to search.</param>
    /// <returns>The matching expressions in encounter order.</returns>
    public SqlExpression[] FindParametersInScript(SqlScript? script)
    {
        return SqlAnalyzer.FindParameters(script);
    }

    /// <summary>Finds parameters in a statement.</summary>
    /// <param name="statement">The statement to search.</param>
    /// <returns>The matching expressions in encounter order.</returns>
    public SqlExpression[] FindParametersInStatement(SqlStatement? statement)
    {
        return SqlAnalyzer.FindParameters(statement);
    }

    /// <summary>Finds parameters in a query.</summary>
    /// <param name="query">The query to search.</param>
    /// <returns>The matching expressions in encounter order.</returns>
    public SqlExpression[] FindParametersInQuery(SqlQuery? query)
    {
        return SqlAnalyzer.FindParameters(query);
    }

    /// <summary>Finds parameters in an expression.</summary>
    /// <param name="expression">The expression to search.</param>
    /// <returns>The matching expressions in encounter order.</returns>
    public SqlExpression[] FindParametersInExpression(SqlExpression? expression)
    {
        return SqlAnalyzer.FindParameters(expression);
    }

    /// <summary>Finds function calls in a script.</summary>
    /// <param name="script">The script to search.</param>
    /// <returns>The matching expressions in encounter order.</returns>
    public SqlExpression[] FindFunctionsInScript(SqlScript? script)
    {
        return SqlAnalyzer.FindFunctions(script);
    }

    /// <summary>Finds function calls in a statement.</summary>
    /// <param name="statement">The statement to search.</param>
    /// <returns>The matching expressions in encounter order.</returns>
    public SqlExpression[] FindFunctionsInStatement(SqlStatement? statement)
    {
        return SqlAnalyzer.FindFunctions(statement);
    }

    /// <summary>Finds function calls in a query.</summary>
    /// <param name="query">The query to search.</param>
    /// <returns>The matching expressions in encounter order.</returns>
    public SqlExpression[] FindFunctionsInQuery(SqlQuery? query)
    {
        return SqlAnalyzer.FindFunctions(query);
    }

    /// <summary>Finds function calls in an expression.</summary>
    /// <param name="expression">The expression to search.</param>
    /// <returns>The matching expressions in encounter order.</returns>
    public SqlExpression[] FindFunctionsInExpression(SqlExpression? expression)
    {
        return SqlAnalyzer.FindFunctions(expression);
    }

    /// <summary>Builds a high-level read/write summary for a statement.</summary>
    /// <param name="statement">The statement to summarize.</param>
    /// <returns>The statement summary.</returns>
    public SqlStatementSummary GetStatementSummary(SqlStatement? statement)
    {
        return SqlAnalyzer.GetStatementSummary(statement);
    }

    /// <summary>Analyzes a script for syntax unavailable in a selected target dialect without changing the script.</summary>
    /// <param name="script">The script to analyze.</param>
    /// <param name="targetDialect">The immutable target-dialect policy.</param>
    /// <returns>The compatibility result.</returns>
    public SqlCompatibilityResult AnalyzeDialectCompatibility(SqlScript script, ISqlDialectPolicy targetDialect)
    {
        return SqlCompatibilityAnalysis.Analyze(script, targetDialect);
    }

    /// <summary>Maps query outputs and DML assignments to source columns without changing the script.</summary>
    /// <param name="script">The script to analyze.</param>
    /// <param name="catalog">The catalog used to resolve relations and columns.</param>
    /// <returns>The column-lineage result.</returns>
    public SqlColumnLineageResult AnalyzeColumnLineage(SqlScript script, SqlCatalog catalog)
    {
        return SqlColumnLineageAnalysis.Analyze(script, catalog);
    }

    /// <summary>Infers SQLite type affinities for syntax and resolved columns throughout a script.</summary>
    /// <param name="script">The script to analyze.</param>
    /// <param name="catalog">The catalog used to resolve columns and functions.</param>
    /// <returns>The affinity-analysis result.</returns>
    public SqliteTypeAffinityResult AnalyzeSqliteTypeAffinities(SqlScript script, SqlCatalog catalog)
    {
        return SqliteTypeAffinityAnalysis.AnalyzeScript(script, catalog);
    }

    /// <summary>Returns the affinity implied by a literal.</summary>
    /// <param name="literal">The literal to inspect.</param>
    /// <returns>The inferred affinity, or unknown.</returns>
    public SqliteTypeAffinityKind GetSqliteLiteralAffinity(SqlLiteral literal)
    {
        return SqliteTypeAffinityAnalysis.GetLiteralAffinity(literal);
    }

    /// <summary>Returns an explicit or declared-type-derived catalog-column affinity.</summary>
    /// <param name="column">The catalog column to inspect.</param>
    /// <returns>The inferred affinity, or unknown.</returns>
    public SqliteTypeAffinityKind GetSqliteCatalogColumnAffinity(SqlCatalogColumn column)
    {
        return SqliteTypeAffinityAnalysis.GetColumnAffinity(column);
    }

    /// <summary>Conservatively infers an expression affinity using catalog metadata.</summary>
    /// <param name="expression">The expression to inspect.</param>
    /// <param name="catalog">The catalog used to resolve names and functions.</param>
    /// <returns>The inferred affinity, or unknown.</returns>
    public SqliteTypeAffinityKind InferSqliteExpressionAffinity(SqlExpression expression, SqlCatalog catalog)
    {
        return SqliteTypeAffinityAnalysis.InferExpression(expression, catalog);
    }

    /// <summary>Conservatively infers a result-column affinity using catalog metadata.</summary>
    /// <param name="resultColumn">The result column to inspect.</param>
    /// <param name="catalog">The catalog used to resolve names and functions.</param>
    /// <returns>The inferred affinity, or unknown.</returns>
    public SqliteTypeAffinityKind InferSqliteResultColumnAffinity(SqlResultColumn resultColumn, SqlCatalog catalog)
    {
        return SqliteTypeAffinityAnalysis.InferResultColumn(resultColumn, catalog);
    }

    /// <summary>Conservatively infers the value affinity of a DML assignment.</summary>
    /// <param name="assignment">The assignment to inspect.</param>
    /// <param name="catalog">The catalog used to resolve names and functions.</param>
    /// <returns>The inferred affinity, or unknown.</returns>
    public SqliteTypeAffinityKind InferSqliteAssignmentAffinity(SqlAssignment assignment, SqlCatalog catalog)
    {
        return SqliteTypeAffinityAnalysis.InferAssignment(assignment, catalog);
    }
}
