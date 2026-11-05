namespace sql;

/// <summary>Defines SQL generation operations for syntax-tree data.</summary>
public interface ISqlGenerator
{
    /// <summary>Generates an expression using default formatting.</summary>
    /// <param name="expression">The expression to generate.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateExpression(SqlExpression expression);

    /// <summary>Generates an expression using explicit formatting.</summary>
    /// <param name="expression">The expression to generate.</param>
    /// <param name="options">The formatting options.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateExpressionWithOptions(SqlExpression expression, SqlFormatOptions options);

    /// <summary>Generates an expression using explicit generation options, including the write dialect.</summary>
    /// <param name="expression">The expression to generate.</param>
    /// <param name="options">The generation options.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateExpressionWithGenerateOptions(SqlExpression expression, SqlGenerateOptions options);

    /// <summary>Generates a query using default formatting.</summary>
    /// <param name="query">The query to generate.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateQuery(SqlQuery query);

    /// <summary>Generates a query using explicit formatting.</summary>
    /// <param name="query">The query to generate.</param>
    /// <param name="options">The formatting options.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateQueryWithOptions(SqlQuery query, SqlFormatOptions options);

    /// <summary>Generates a query using explicit generation options, including the write dialect.</summary>
    /// <param name="query">The query to generate.</param>
    /// <param name="options">The generation options.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateQueryWithGenerateOptions(SqlQuery query, SqlGenerateOptions options);

    /// <summary>Generates a statement using default formatting.</summary>
    /// <param name="statement">The statement to generate.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateStatement(SqlStatement statement);

    /// <summary>Generates a statement using explicit formatting.</summary>
    /// <param name="statement">The statement to generate.</param>
    /// <param name="options">The formatting options.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateStatementWithOptions(SqlStatement statement, SqlFormatOptions options);

    /// <summary>Generates a statement using explicit generation options, including the write dialect.</summary>
    /// <param name="statement">The statement to generate.</param>
    /// <param name="options">The generation options.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateStatementWithGenerateOptions(SqlStatement statement, SqlGenerateOptions options);

    /// <summary>Generates a script using default options.</summary>
    /// <param name="script">The script to generate.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateScript(SqlScript script);

    /// <summary>Generates a script using explicit formatting.</summary>
    /// <param name="script">The script to generate.</param>
    /// <param name="options">The formatting options.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateScriptWithFormatOptions(SqlScript script, SqlFormatOptions options);

    /// <summary>Generates a script using explicit generation options.</summary>
    /// <param name="script">The script to generate.</param>
    /// <param name="options">The generation options.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateScriptWithOptions(SqlScript script, SqlGenerateOptions options);

    /// <summary>Generates an insert statement.</summary>
    /// <param name="statement">The statement to generate.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateInsert(SqlInsertStatement statement);

    /// <summary>Generates an update statement.</summary>
    /// <param name="statement">The statement to generate.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateUpdate(SqlUpdateStatement statement);

    /// <summary>Generates a delete statement.</summary>
    /// <param name="statement">The statement to generate.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateDelete(SqlDeleteStatement statement);

    /// <summary>Generates a merge statement.</summary>
    /// <param name="statement">The statement to generate.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateMerge(SqlMergeStatement statement);

    /// <summary>Generates the exact text retained by an unsupported syntax marker.</summary>
    /// <param name="syntax">The unsupported syntax marker to generate.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateUnsupportedSyntax(SqlUnsupportedSyntax syntax);

    /// <summary>Generates a result column.</summary>
    /// <param name="column">The result column to generate.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateResultColumn(SqlResultColumn column);

    /// <summary>Generates a create table definition.</summary>
    /// <param name="definition">The table definition to generate.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateCreateTable(SqlTableDefinition definition);

    /// <summary>Generates an alter table definition.</summary>
    /// <param name="definition">The alter table definition to generate.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateAlterTable(SqlAlterTableDefinition definition);

    /// <summary>Generates a drop table definition.</summary>
    /// <param name="definition">The drop table definition to generate.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateDropTable(SqlDropTableDefinition definition);

    /// <summary>Generates a create index definition.</summary>
    /// <param name="definition">The index definition to generate.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateCreateIndex(SqlIndexDefinition definition);

    /// <summary>Generates a drop index definition.</summary>
    /// <param name="definition">The drop index definition to generate.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateDropIndex(SqlDropIndexDefinition definition);

    /// <summary>Generates a create view definition.</summary>
    /// <param name="definition">The view definition to generate.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateCreateView(SqlViewDefinition definition);

    /// <summary>Generates a drop view definition.</summary>
    /// <param name="definition">The drop view definition to generate.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateDropView(SqlDropViewDefinition definition);

    /// <summary>Generates a create virtual table definition.</summary>
    /// <param name="definition">The virtual table definition to generate.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateCreateVirtualTable(SqlVirtualTableDefinition definition);

    /// <summary>Generates a create trigger definition.</summary>
    /// <param name="definition">The trigger definition to generate.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateCreateTrigger(SqlTriggerDefinition definition);

    /// <summary>Generates a drop trigger definition.</summary>
    /// <param name="definition">The drop trigger definition to generate.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateDropTrigger(SqlDropTriggerDefinition definition);

    /// <summary>Generates a pragma statement.</summary>
    /// <param name="statement">The pragma statement to generate.</param>
    /// <returns>The generated SQL.</returns>
    string GeneratePragma(SqlPragmaStatement statement);

    /// <summary>Generates an analyze statement.</summary>
    /// <param name="statement">The analyze statement to generate.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateAnalyze(SqlAnalyzeStatement statement);

    /// <summary>Generates a vacuum statement.</summary>
    /// <param name="statement">The vacuum statement to generate.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateVacuum(SqlVacuumStatement statement);

    /// <summary>Generates an attach statement.</summary>
    /// <param name="statement">The attach statement to generate.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateAttach(SqlAttachStatement statement);

    /// <summary>Generates a detach statement.</summary>
    /// <param name="statement">The detach statement to generate.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateDetach(SqlDetachStatement statement);

    /// <summary>Generates a reindex statement.</summary>
    /// <param name="statement">The reindex statement to generate.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateReindex(SqlReindexStatement statement);

    /// <summary>Generates a transaction statement.</summary>
    /// <param name="statement">The transaction statement to generate.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateTransaction(SqlTransactionStatement statement);

    /// <summary>Generates a savepoint statement.</summary>
    /// <param name="statement">The savepoint statement to generate.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateSavepoint(SqlSavepointStatement statement);

    /// <summary>Generates a release statement.</summary>
    /// <param name="statement">The release statement to generate.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateRelease(SqlReleaseStatement statement);

    /// <summary>Generates an explain statement.</summary>
    /// <param name="statement">The explain statement to generate.</param>
    /// <returns>The generated SQL.</returns>
    string GenerateExplain(SqlExplainStatement statement);
}
