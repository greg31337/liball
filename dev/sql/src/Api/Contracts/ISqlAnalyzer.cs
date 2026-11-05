namespace sql;

/// <summary>Defines read-only searches and summaries for SQL syntax trees.</summary>
public interface ISqlAnalyzer
{
    /// <summary>Finds referenced tables in a script.</summary>
    /// <param name="script">The script to search.</param>
    /// <returns>The referenced names in encounter order.</returns>
    SqlName[] FindTablesInScript(SqlScript? script);

    /// <summary>Finds referenced tables in a statement.</summary>
    /// <param name="statement">The statement to search.</param>
    /// <returns>The referenced names in encounter order.</returns>
    SqlName[] FindTablesInStatement(SqlStatement? statement);

    /// <summary>Finds referenced tables in a query.</summary>
    /// <param name="query">The query to search.</param>
    /// <returns>The referenced names in encounter order.</returns>
    SqlName[] FindTablesInQuery(SqlQuery? query);

    /// <summary>Finds referenced tables in an expression.</summary>
    /// <param name="expression">The expression to search.</param>
    /// <returns>The referenced names in encounter order.</returns>
    SqlName[] FindTablesInExpression(SqlExpression? expression);

    /// <summary>Finds referenced columns in a script.</summary>
    /// <param name="script">The script to search.</param>
    /// <returns>The referenced names in encounter order.</returns>
    SqlName[] FindColumnsInScript(SqlScript? script);

    /// <summary>Finds referenced columns in a statement.</summary>
    /// <param name="statement">The statement to search.</param>
    /// <returns>The referenced names in encounter order.</returns>
    SqlName[] FindColumnsInStatement(SqlStatement? statement);

    /// <summary>Finds referenced columns in a query.</summary>
    /// <param name="query">The query to search.</param>
    /// <returns>The referenced names in encounter order.</returns>
    SqlName[] FindColumnsInQuery(SqlQuery? query);

    /// <summary>Finds referenced columns in an expression.</summary>
    /// <param name="expression">The expression to search.</param>
    /// <returns>The referenced names in encounter order.</returns>
    SqlName[] FindColumnsInExpression(SqlExpression? expression);

    /// <summary>Finds parameters in a script.</summary>
    /// <param name="script">The script to search.</param>
    /// <returns>The matching expressions in encounter order.</returns>
    SqlExpression[] FindParametersInScript(SqlScript? script);

    /// <summary>Finds parameters in a statement.</summary>
    /// <param name="statement">The statement to search.</param>
    /// <returns>The matching expressions in encounter order.</returns>
    SqlExpression[] FindParametersInStatement(SqlStatement? statement);

    /// <summary>Finds parameters in a query.</summary>
    /// <param name="query">The query to search.</param>
    /// <returns>The matching expressions in encounter order.</returns>
    SqlExpression[] FindParametersInQuery(SqlQuery? query);

    /// <summary>Finds parameters in an expression.</summary>
    /// <param name="expression">The expression to search.</param>
    /// <returns>The matching expressions in encounter order.</returns>
    SqlExpression[] FindParametersInExpression(SqlExpression? expression);

    /// <summary>Finds function calls in a script.</summary>
    /// <param name="script">The script to search.</param>
    /// <returns>The matching expressions in encounter order.</returns>
    SqlExpression[] FindFunctionsInScript(SqlScript? script);

    /// <summary>Finds function calls in a statement.</summary>
    /// <param name="statement">The statement to search.</param>
    /// <returns>The matching expressions in encounter order.</returns>
    SqlExpression[] FindFunctionsInStatement(SqlStatement? statement);

    /// <summary>Finds function calls in a query.</summary>
    /// <param name="query">The query to search.</param>
    /// <returns>The matching expressions in encounter order.</returns>
    SqlExpression[] FindFunctionsInQuery(SqlQuery? query);

    /// <summary>Finds function calls in an expression.</summary>
    /// <param name="expression">The expression to search.</param>
    /// <returns>The matching expressions in encounter order.</returns>
    SqlExpression[] FindFunctionsInExpression(SqlExpression? expression);

    /// <summary>Builds a high-level read/write summary for a statement.</summary>
    /// <param name="statement">The statement to summarize.</param>
    /// <returns>The statement summary.</returns>
    SqlStatementSummary GetStatementSummary(SqlStatement? statement);

    /// <summary>Analyzes a script for syntax unavailable in a selected target dialect without changing the script.</summary>
    /// <param name="script">The script to analyze.</param>
    /// <param name="targetDialect">The immutable target-dialect policy.</param>
    /// <returns>The compatibility result.</returns>
    SqlCompatibilityResult AnalyzeDialectCompatibility(SqlScript script, ISqlDialectPolicy targetDialect);

    /// <summary>Maps query outputs and DML assignments to source columns without changing the script.</summary>
    /// <param name="script">The script to analyze.</param>
    /// <param name="catalog">The catalog used to resolve relations and columns.</param>
    /// <returns>The column-lineage result.</returns>
    SqlColumnLineageResult AnalyzeColumnLineage(SqlScript script, SqlCatalog catalog);

    /// <summary>Infers SQLite type affinities for syntax and resolved columns throughout a script.</summary>
    /// <param name="script">The script to analyze.</param>
    /// <param name="catalog">The catalog used to resolve columns and functions.</param>
    /// <returns>The affinity-analysis result.</returns>
    SqliteTypeAffinityResult AnalyzeSqliteTypeAffinities(SqlScript script, SqlCatalog catalog);

    /// <summary>Returns the affinity implied by a literal.</summary>
    /// <param name="literal">The literal to inspect.</param>
    /// <returns>The inferred affinity, or unknown.</returns>
    SqliteTypeAffinityKind GetSqliteLiteralAffinity(SqlLiteral literal);

    /// <summary>Returns an explicit or declared-type-derived catalog-column affinity.</summary>
    /// <param name="column">The catalog column to inspect.</param>
    /// <returns>The inferred affinity, or unknown.</returns>
    SqliteTypeAffinityKind GetSqliteCatalogColumnAffinity(SqlCatalogColumn column);

    /// <summary>Conservatively infers an expression affinity using catalog metadata.</summary>
    /// <param name="expression">The expression to inspect.</param>
    /// <param name="catalog">The catalog used to resolve names and functions.</param>
    /// <returns>The inferred affinity, or unknown.</returns>
    SqliteTypeAffinityKind InferSqliteExpressionAffinity(SqlExpression expression, SqlCatalog catalog);

    /// <summary>Conservatively infers a result-column affinity using catalog metadata.</summary>
    /// <param name="resultColumn">The result column to inspect.</param>
    /// <param name="catalog">The catalog used to resolve names and functions.</param>
    /// <returns>The inferred affinity, or unknown.</returns>
    SqliteTypeAffinityKind InferSqliteResultColumnAffinity(SqlResultColumn resultColumn, SqlCatalog catalog);

    /// <summary>Conservatively infers the value affinity of a DML assignment.</summary>
    /// <param name="assignment">The assignment to inspect.</param>
    /// <param name="catalog">The catalog used to resolve names and functions.</param>
    /// <returns>The inferred affinity, or unknown.</returns>
    SqliteTypeAffinityKind InferSqliteAssignmentAffinity(SqlAssignment assignment, SqlCatalog catalog);
}
