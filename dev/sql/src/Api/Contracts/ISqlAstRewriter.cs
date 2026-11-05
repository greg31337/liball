namespace sql;

/// <summary>Defines copy-on-write operations for replacing direct AST children.</summary>
public interface ISqlAstRewriter
{
    /// <summary>Replaces one statement in a script.</summary>
    /// <param name="script">The source script.</param>
    /// <param name="index">The zero-based statement index.</param>
    /// <param name="replacement">The replacement statement.</param>
    /// <param name="options">The metadata-preservation options.</param>
    /// <returns>A rewritten script.</returns>
    SqlScript ReplaceScriptStatementAt(SqlScript script, int index, SqlStatement replacement, SqlRewriteOptions options);

    /// <summary>Replaces one statement in a parse result while applying diagnostic and source preservation options.</summary>
    /// <param name="result">The parse result to rewrite.</param>
    /// <param name="index">The zero-based statement index.</param>
    /// <param name="replacement">The replacement statement.</param>
    /// <param name="options">The metadata-preservation options.</param>
    /// <returns>A rewritten parse result.</returns>
    SqlParseResult ReplaceResultStatementAt(SqlParseResult result, int index, SqlStatement replacement, SqlRewriteOptions options);

    /// <summary>Replaces a statement's direct typed payloads using another statement as the payload source.</summary>
    /// <param name="statement">The statement root whose metadata is preserved.</param>
    /// <param name="payloadSource">The statement supplying the discriminator and direct payload fields.</param>
    /// <param name="options">The metadata-preservation options.</param>
    /// <returns>A rewritten statement.</returns>
    SqlStatement ReplaceStatementPayload(SqlStatement statement, SqlStatement payloadSource, SqlRewriteOptions options);

    /// <summary>Replaces one select core in a query.</summary>
    /// <param name="query">The query root.</param>
    /// <param name="index">The zero-based select-core index.</param>
    /// <param name="replacement">The replacement select core.</param>
    /// <param name="options">The metadata-preservation options.</param>
    /// <returns>A rewritten query.</returns>
    SqlQuery ReplaceQuerySelectCoreAt(SqlQuery query, int index, SqlSelectCore replacement, SqlRewriteOptions options);

    /// <summary>Replaces a direct query child of a table reference.</summary>
    /// <param name="table">The table-reference root.</param>
    /// <param name="existingQuery">The direct query child to replace.</param>
    /// <param name="replacement">The replacement query.</param>
    /// <param name="options">The metadata-preservation options.</param>
    /// <returns>A rewritten table reference.</returns>
    SqlTableReference ReplaceTableQuery(SqlTableReference table, SqlQuery existingQuery, SqlQuery replacement, SqlRewriteOptions options);

    /// <summary>Replaces the direct inner table-reference child.</summary>
    /// <param name="table">The table-reference root.</param>
    /// <param name="replacement">The replacement inner table reference.</param>
    /// <param name="options">The metadata-preservation options.</param>
    /// <returns>A rewritten table reference.</returns>
    SqlTableReference ReplaceTableInner(SqlTableReference table, SqlTableReference replacement, SqlRewriteOptions options);

    /// <summary>Replaces the direct function-expression child of a table reference.</summary>
    /// <param name="table">The table-reference root.</param>
    /// <param name="replacement">The replacement function expression.</param>
    /// <param name="options">The metadata-preservation options.</param>
    /// <returns>A rewritten table reference.</returns>
    SqlTableReference ReplaceTableFunction(SqlTableReference table, SqlExpression replacement, SqlRewriteOptions options);

    /// <summary>Replaces one direct join child of a table reference.</summary>
    /// <param name="table">The table-reference root.</param>
    /// <param name="index">The zero-based join index.</param>
    /// <param name="replacement">The replacement join.</param>
    /// <param name="options">The metadata-preservation options.</param>
    /// <returns>A rewritten table reference.</returns>
    SqlTableReference ReplaceTableJoinAt(SqlTableReference table, int index, SqlJoin replacement, SqlRewriteOptions options);

    /// <summary>Replaces every direct expression-child reference equal to the supplied existing child.</summary>
    /// <param name="expression">The expression root.</param>
    /// <param name="existingChild">The direct expression child to replace.</param>
    /// <param name="replacement">The replacement expression.</param>
    /// <param name="options">The metadata-preservation options.</param>
    /// <returns>A rewritten expression.</returns>
    SqlExpression ReplaceExpressionChild(SqlExpression expression, SqlExpression existingChild, SqlExpression replacement, SqlRewriteOptions options);

    /// <summary>Replaces every direct query-child reference equal to the supplied existing query.</summary>
    /// <param name="expression">The expression root.</param>
    /// <param name="existingQuery">The direct query child to replace.</param>
    /// <param name="replacement">The replacement query.</param>
    /// <param name="options">The metadata-preservation options.</param>
    /// <returns>A rewritten expression.</returns>
    SqlExpression ReplaceExpressionQuery(SqlExpression expression, SqlQuery existingQuery, SqlQuery replacement, SqlRewriteOptions options);
}
