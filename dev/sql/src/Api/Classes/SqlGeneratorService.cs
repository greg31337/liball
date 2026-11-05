namespace sql;

/// <summary>Generates SQL text from syntax-tree data.</summary>
public class SqlGeneratorService : ISqlGenerator
{
    /// <summary>Initializes a SQL generator.</summary>
    public SqlGeneratorService()
    {
    }

    /// <summary>Generates an expression using default formatting.</summary>
    /// <param name="expression">The expression to generate.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateExpression(SqlExpression expression)
    {
        return SqlGenerator.GenerateExpression(expression);
    }

    /// <summary>Generates an expression using explicit formatting.</summary>
    /// <param name="expression">The expression to generate.</param>
    /// <param name="options">The formatting options.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateExpressionWithOptions(SqlExpression expression, SqlFormatOptions options)
    {
        return SqlGenerator.GenerateExpression(expression, options);
    }

    /// <summary>Generates an expression using explicit generation options, including the write dialect.</summary>
    /// <param name="expression">The expression to generate.</param>
    /// <param name="options">The generation options.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateExpressionWithGenerateOptions(SqlExpression expression, SqlGenerateOptions options)
    {
        return SqlGenerator.GenerateExpressionWithGenerateOptions(expression, options);
    }

    /// <summary>Generates a query using default formatting.</summary>
    /// <param name="query">The query to generate.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateQuery(SqlQuery query)
    {
        return SqlGenerator.GenerateQuery(query);
    }

    /// <summary>Generates a query using explicit formatting.</summary>
    /// <param name="query">The query to generate.</param>
    /// <param name="options">The formatting options.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateQueryWithOptions(SqlQuery query, SqlFormatOptions options)
    {
        return SqlGenerator.GenerateQuery(query, options);
    }

    /// <summary>Generates a query using explicit generation options, including the write dialect.</summary>
    /// <param name="query">The query to generate.</param>
    /// <param name="options">The generation options.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateQueryWithGenerateOptions(SqlQuery query, SqlGenerateOptions options)
    {
        return SqlGenerator.GenerateQueryWithGenerateOptions(query, options);
    }

    /// <summary>Generates a statement using default formatting.</summary>
    /// <param name="statement">The statement to generate.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateStatement(SqlStatement statement)
    {
        return SqlGenerator.GenerateStatement(statement);
    }

    /// <summary>Generates a statement using explicit formatting.</summary>
    /// <param name="statement">The statement to generate.</param>
    /// <param name="options">The formatting options.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateStatementWithOptions(SqlStatement statement, SqlFormatOptions options)
    {
        return SqlGenerator.GenerateStatement(statement, options);
    }

    /// <summary>Generates a statement using explicit generation options, including the write dialect.</summary>
    /// <param name="statement">The statement to generate.</param>
    /// <param name="options">The generation options.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateStatementWithGenerateOptions(SqlStatement statement, SqlGenerateOptions options)
    {
        return SqlGenerator.GenerateStatementWithGenerateOptions(statement, options);
    }

    /// <summary>Generates a script using default options.</summary>
    /// <param name="script">The script to generate.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateScript(SqlScript script)
    {
        return SqlGenerator.GenerateScript(script);
    }

    /// <summary>Generates a script using explicit formatting.</summary>
    /// <param name="script">The script to generate.</param>
    /// <param name="options">The formatting options.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateScriptWithFormatOptions(SqlScript script, SqlFormatOptions options)
    {
        return SqlGenerator.GenerateScript(script, options);
    }

    /// <summary>Generates a script using explicit generation options.</summary>
    /// <param name="script">The script to generate.</param>
    /// <param name="options">The generation options.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateScriptWithOptions(SqlScript script, SqlGenerateOptions options)
    {
        return SqlGenerator.GenerateScript(script, options);
    }

    /// <summary>Generates an insert statement.</summary>
    /// <param name="statement">The statement to generate.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateInsert(SqlInsertStatement statement)
    {
        return SqlGenerator.GenerateInsert(statement);
    }

    /// <summary>Generates an update statement.</summary>
    /// <param name="statement">The statement to generate.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateUpdate(SqlUpdateStatement statement)
    {
        return SqlGenerator.GenerateUpdate(statement);
    }

    /// <summary>Generates a delete statement.</summary>
    /// <param name="statement">The statement to generate.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateDelete(SqlDeleteStatement statement)
    {
        return SqlGenerator.GenerateDelete(statement);
    }

    /// <summary>Generates a merge statement.</summary>
    /// <param name="statement">The statement to generate.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateMerge(SqlMergeStatement statement)
    {
        return SqlGenerator.GenerateMerge(statement);
    }

    /// <summary>Generates the exact text retained by an unsupported syntax marker.</summary>
    /// <param name="syntax">The unsupported syntax marker to generate.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateUnsupportedSyntax(SqlUnsupportedSyntax syntax)
    {
        return SqlGenerator.GenerateUnsupportedSyntax(syntax);
    }

    /// <summary>Generates a result column.</summary>
    /// <param name="column">The result column to generate.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateResultColumn(SqlResultColumn column)
    {
        return SqlGenerator.GenerateResultColumn(column);
    }

    /// <summary>Generates a create table definition.</summary>
    /// <param name="definition">The table definition to generate.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateCreateTable(SqlTableDefinition definition)
    {
        return SqlGenerator.GenerateCreateTable(definition);
    }

    /// <summary>Generates an alter table definition.</summary>
    /// <param name="definition">The alter table definition to generate.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateAlterTable(SqlAlterTableDefinition definition)
    {
        return SqlGenerator.GenerateAlterTable(definition);
    }

    /// <summary>Generates a drop table definition.</summary>
    /// <param name="definition">The drop table definition to generate.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateDropTable(SqlDropTableDefinition definition)
    {
        return SqlGenerator.GenerateDropTable(definition);
    }

    /// <summary>Generates a create index definition.</summary>
    /// <param name="definition">The index definition to generate.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateCreateIndex(SqlIndexDefinition definition)
    {
        return SqlGenerator.GenerateCreateIndex(definition);
    }

    /// <summary>Generates a drop index definition.</summary>
    /// <param name="definition">The drop index definition to generate.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateDropIndex(SqlDropIndexDefinition definition)
    {
        return SqlGenerator.GenerateDropIndex(definition);
    }

    /// <summary>Generates a create view definition.</summary>
    /// <param name="definition">The view definition to generate.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateCreateView(SqlViewDefinition definition)
    {
        return SqlGenerator.GenerateCreateView(definition);
    }

    /// <summary>Generates a drop view definition.</summary>
    /// <param name="definition">The drop view definition to generate.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateDropView(SqlDropViewDefinition definition)
    {
        return SqlGenerator.GenerateDropView(definition);
    }

    /// <summary>Generates a create virtual table definition.</summary>
    /// <param name="definition">The virtual table definition to generate.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateCreateVirtualTable(SqlVirtualTableDefinition definition)
    {
        return SqlGenerator.GenerateCreateVirtualTable(definition);
    }

    /// <summary>Generates a create trigger definition.</summary>
    /// <param name="definition">The trigger definition to generate.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateCreateTrigger(SqlTriggerDefinition definition)
    {
        return SqlGenerator.GenerateCreateTrigger(definition);
    }

    /// <summary>Generates a drop trigger definition.</summary>
    /// <param name="definition">The drop trigger definition to generate.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateDropTrigger(SqlDropTriggerDefinition definition)
    {
        return SqlGenerator.GenerateDropTrigger(definition);
    }

    /// <summary>Generates a pragma statement.</summary>
    /// <param name="statement">The pragma statement to generate.</param>
    /// <returns>The generated SQL.</returns>
    public string GeneratePragma(SqlPragmaStatement statement)
    {
        return SqlGenerator.GeneratePragma(statement);
    }

    /// <summary>Generates an analyze statement.</summary>
    /// <param name="statement">The analyze statement to generate.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateAnalyze(SqlAnalyzeStatement statement)
    {
        return SqlGenerator.GenerateAnalyze(statement);
    }

    /// <summary>Generates a vacuum statement.</summary>
    /// <param name="statement">The vacuum statement to generate.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateVacuum(SqlVacuumStatement statement)
    {
        return SqlGenerator.GenerateVacuum(statement);
    }

    /// <summary>Generates an attach statement.</summary>
    /// <param name="statement">The attach statement to generate.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateAttach(SqlAttachStatement statement)
    {
        return SqlGenerator.GenerateAttach(statement);
    }

    /// <summary>Generates a detach statement.</summary>
    /// <param name="statement">The detach statement to generate.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateDetach(SqlDetachStatement statement)
    {
        return SqlGenerator.GenerateDetach(statement);
    }

    /// <summary>Generates a reindex statement.</summary>
    /// <param name="statement">The reindex statement to generate.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateReindex(SqlReindexStatement statement)
    {
        return SqlGenerator.GenerateReindex(statement);
    }

    /// <summary>Generates a transaction statement.</summary>
    /// <param name="statement">The transaction statement to generate.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateTransaction(SqlTransactionStatement statement)
    {
        return SqlGenerator.GenerateTransaction(statement);
    }

    /// <summary>Generates a savepoint statement.</summary>
    /// <param name="statement">The savepoint statement to generate.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateSavepoint(SqlSavepointStatement statement)
    {
        return SqlGenerator.GenerateSavepoint(statement);
    }

    /// <summary>Generates a release statement.</summary>
    /// <param name="statement">The release statement to generate.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateRelease(SqlReleaseStatement statement)
    {
        return SqlGenerator.GenerateRelease(statement);
    }

    /// <summary>Generates an explain statement.</summary>
    /// <param name="statement">The explain statement to generate.</param>
    /// <returns>The generated SQL.</returns>
    public string GenerateExplain(SqlExplainStatement statement)
    {
        return SqlGenerator.GenerateExplain(statement);
    }
}
