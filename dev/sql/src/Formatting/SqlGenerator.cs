using System;

namespace sql;

/// <summary>Generates SQL text from syntax-tree nodes using configurable formatting and script-output rules.</summary>
/// <remarks>The generator emits a normalized representation of the supplied tree. It does not parse or validate SQL. Required nodes and options must not be <c>null</c>.</remarks>
internal class SqlGenerator
{
    internal SqlWriter Writer;
    internal SqlFormatOptions Options;
    internal SqlDialectOptions WriteDialect;
    internal bool MapDialectSyntax;

    internal static SqlGenerator CreateDefault()
    {
        return new SqlGenerator(SqlFormatOptions.CreateDefault());
    }

    internal static SqlGenerator Create(SqlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        SqlGenerator generator = new SqlGenerator(writer.Options);
        generator.Writer = writer;
        return generator;
    }

    internal SqlGenerator(SqlFormatOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.Validate();
        Options = options.Copy();
        Writer = new SqlWriter(Options);
        WriteDialect = Options.WriteDialect;
        MapDialectSyntax = false;
    }

    internal static SqlGenerator Create(SqlFormatOptions options)
    {
        return new SqlGenerator(options);
    }

    internal static SqlGenerator CreateForGeneration(SqlGenerateOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.Validate();
        SqlGenerator generator = new SqlGenerator(options.FormatOptions);
        generator.WriteDialect = options.WriteDialect;
        return generator;
    }

    internal static SqlGenerator CreateForTranspilation(SqlGenerateOptions options)
    {
        SqlGenerator generator = CreateForGeneration(options);
        generator.MapDialectSyntax = true;
        return generator;
    }

    /// <summary>Generates SQL text for the supplied syntax-tree value.</summary>
    /// <param name="expression">The expression syntax tree to process.</param>
    /// <returns>The generated or formatted SQL text.</returns>
    internal static string GenerateExpression(SqlExpression expression)
    {
        SqlGenerator generator = CreateDefault();
        generator.WriteExpression(expression);
        return generator.ToSql();
    }

    /// <summary>Generates SQL text for the supplied syntax-tree value.</summary>
    /// <param name="expression">The expression syntax tree to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The generated or formatted SQL text.</returns>
    internal static string GenerateExpression(SqlExpression expression, SqlFormatOptions options)
    {
        SqlGenerator generator = new SqlGenerator(options);
        generator.WriteExpression(expression);
        return generator.ToSql();
    }

    internal static string GenerateExpressionWithGenerateOptions(SqlExpression expression, SqlGenerateOptions options)
    {
        SqlGenerator generator = CreateForGeneration(options);
        generator.WriteExpression(expression);
        return generator.ToSql();
    }

    internal static string GenerateExpressionForTranspilation(SqlExpression expression, SqlGenerateOptions options)
    {
        SqlGenerator generator = CreateForTranspilation(options);
        generator.WriteExpression(expression);
        return generator.ToSql();
    }

    /// <summary>Generates SQL text for the supplied syntax-tree value.</summary>
    /// <param name="query">The query syntax tree to process.</param>
    /// <returns>The generated or formatted SQL text.</returns>
    internal static string GenerateQuery(SqlQuery query)
    {
        SqlGenerator generator = CreateDefault();
        generator.WriteQuery(query);
        return generator.ToSql();
    }

    /// <summary>Generates SQL text for the supplied syntax-tree value.</summary>
    /// <param name="query">The query syntax tree to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The generated or formatted SQL text.</returns>
    internal static string GenerateQuery(SqlQuery query, SqlFormatOptions options)
    {
        SqlGenerator generator = new SqlGenerator(options);
        generator.WriteQuery(query);
        return generator.ToSql();
    }

    internal static string GenerateQueryWithGenerateOptions(SqlQuery query, SqlGenerateOptions options)
    {
        SqlGenerator generator = CreateForGeneration(options);
        generator.WriteQuery(query);
        return generator.ToSql();
    }

    /// <summary>Generates SQL text for the supplied syntax-tree value.</summary>
    /// <param name="statement">The statement syntax tree to process.</param>
    /// <returns>The generated or formatted SQL text.</returns>
    internal static string GenerateStatement(SqlStatement statement)
    {
        SqlGenerator generator = CreateDefault();
        generator.WriteStatement(statement);
        return generator.ToSql();
    }

    /// <summary>Generates SQL text for the supplied syntax-tree value.</summary>
    /// <param name="statement">The statement syntax tree to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The generated or formatted SQL text.</returns>
    internal static string GenerateStatement(SqlStatement statement, SqlFormatOptions options)
    {
        SqlGenerator generator = new SqlGenerator(options);
        generator.WriteStatement(statement);
        return generator.ToSql();
    }

    internal static string GenerateStatementWithGenerateOptions(SqlStatement statement, SqlGenerateOptions options)
    {
        SqlGenerator generator = CreateForGeneration(options);
        generator.WriteStatement(statement);
        return generator.ToSql();
    }

    /// <summary>Generates SQL text for the supplied syntax-tree value.</summary>
    /// <param name="script">The script syntax tree to process.</param>
    /// <returns>The generated or formatted SQL text.</returns>
    internal static string GenerateScript(SqlScript script)
    {
        SqlGenerateOptions options = SqlGenerateOptions.CreateDefault();
        return GenerateScript(script, options);
    }

    /// <summary>Generates SQL text for the supplied syntax-tree value.</summary>
    /// <param name="script">The script syntax tree to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The generated or formatted SQL text.</returns>
    /// <exception cref="ArgumentNullException">options is <c>null</c>.</exception>
    internal static string GenerateScript(SqlScript script, SqlFormatOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        SqlGenerateOptions generateOptions = new SqlGenerateOptions();
        generateOptions.FormatOptions = options.Copy();
        return GenerateScript(script, generateOptions);
    }

    /// <summary>Generates SQL text for the supplied syntax-tree value.</summary>
    /// <param name="script">The script syntax tree to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The generated or formatted SQL text.</returns>
    /// <exception cref="ArgumentNullException">options is <c>null</c>.</exception>
    internal static string GenerateScript(SqlScript script, SqlGenerateOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.Validate();
        SqlGenerator generator = CreateForGeneration(options);
        generator.WriteScript(script, options);
        return generator.ToSql();
    }

    internal static string GenerateScriptForTranspilation(SqlScript script, SqlGenerateOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.Validate();
        SqlGenerator generator = CreateForTranspilation(options);
        generator.WriteScript(script, options);
        return generator.ToSql();
    }

    /// <summary>Generates SQL text for the supplied syntax-tree value.</summary>
    /// <param name="statement">The statement syntax tree to process.</param>
    /// <returns>The generated or formatted SQL text.</returns>
    internal static string GenerateInsert(SqlInsertStatement statement)
    {
        SqlGenerator generator = SqlGenerator.CreateDefault();
        generator.WriteInsertStatement(statement);
        return generator.ToSql();
    }

    /// <summary>Generates SQL text for the supplied syntax-tree value.</summary>
    /// <param name="statement">The statement syntax tree to process.</param>
    /// <returns>The generated or formatted SQL text.</returns>
    internal static string GenerateUpdate(SqlUpdateStatement statement)
    {
        SqlGenerator generator = SqlGenerator.CreateDefault();
        generator.WriteUpdateStatement(statement);
        return generator.ToSql();
    }

    /// <summary>Generates SQL text for the supplied syntax-tree value.</summary>
    /// <param name="statement">The statement syntax tree to process.</param>
    /// <returns>The generated or formatted SQL text.</returns>
    internal static string GenerateDelete(SqlDeleteStatement statement)
    {
        SqlGenerator generator = SqlGenerator.CreateDefault();
        generator.WriteDeleteStatement(statement);
        return generator.ToSql();
    }

    internal static string GenerateMerge(SqlMergeStatement statement)
    {
        SqlGenerator generator = SqlGenerator.CreateDefault();
        generator.WriteMergeStatement(statement);
        return generator.ToSql();
    }

    internal static string GenerateUnsupportedSyntax(SqlUnsupportedSyntax syntax)
    {
        SqlGenerator generator = SqlGenerator.CreateDefault();
        generator.WriteUnsupportedSyntax(syntax);
        return generator.ToSql();
    }

    internal static string GenerateResultColumn(SqlResultColumn column)
    {
        SqlGenerator generator = SqlGenerator.CreateDefault();
        generator.WriteResultColumn(column);
        return generator.ToSql();
    }

    internal static string GenerateCreateTable(SqlTableDefinition definition)
    {
        SqlGenerator generator = SqlGenerator.CreateDefault();
        generator.WriteCreateTable(definition);
        return generator.ToSql();
    }

    internal static string GenerateAlterTable(SqlAlterTableDefinition definition)
    {
        SqlGenerator generator = SqlGenerator.CreateDefault();
        generator.WriteAlterTable(definition);
        return generator.ToSql();
    }

    internal static string GenerateDropTable(SqlDropTableDefinition definition)
    {
        SqlGenerator generator = SqlGenerator.CreateDefault();
        generator.WriteDropTable(definition);
        return generator.ToSql();
    }

    internal static string GenerateCreateIndex(SqlIndexDefinition definition)
    {
        SqlGenerator generator = SqlGenerator.CreateDefault();
        generator.WriteCreateIndex(definition);
        return generator.ToSql();
    }

    internal static string GenerateDropIndex(SqlDropIndexDefinition definition)
    {
        SqlGenerator generator = SqlGenerator.CreateDefault();
        generator.WriteDropIndex(definition);
        return generator.ToSql();
    }

    internal static string GenerateCreateView(SqlViewDefinition definition)
    {
        SqlGenerator generator = SqlGenerator.CreateDefault();
        generator.WriteCreateView(definition);
        return generator.ToSql();
    }

    internal static string GenerateDropView(SqlDropViewDefinition definition)
    {
        SqlGenerator generator = SqlGenerator.CreateDefault();
        generator.WriteDropView(definition);
        return generator.ToSql();
    }

    internal static string GenerateCreateVirtualTable(SqlVirtualTableDefinition definition)
    {
        SqlGenerator generator = SqlGenerator.CreateDefault();
        generator.WriteCreateVirtualTable(definition);
        return generator.ToSql();
    }

    internal static string GenerateCreateTrigger(SqlTriggerDefinition definition)
    {
        SqlGenerator generator = SqlGenerator.CreateDefault();
        generator.WriteCreateTrigger(definition);
        return generator.ToSql();
    }

    internal static string GenerateDropTrigger(SqlDropTriggerDefinition definition)
    {
        SqlGenerator generator = SqlGenerator.CreateDefault();
        generator.WriteDropTrigger(definition);
        return generator.ToSql();
    }

    internal static string GeneratePragma(SqlPragmaStatement statement)
    {
        SqlGenerator generator = SqlGenerator.CreateDefault();
        generator.WritePragma(statement);
        return generator.ToSql();
    }

    internal static string GenerateAnalyze(SqlAnalyzeStatement statement)
    {
        SqlGenerator generator = SqlGenerator.CreateDefault();
        generator.WriteAnalyze(statement);
        return generator.ToSql();
    }

    internal static string GenerateVacuum(SqlVacuumStatement statement)
    {
        SqlGenerator generator = SqlGenerator.CreateDefault();
        generator.WriteVacuum(statement);
        return generator.ToSql();
    }

    internal static string GenerateAttach(SqlAttachStatement statement)
    {
        SqlGenerator generator = SqlGenerator.CreateDefault();
        generator.WriteAttach(statement);
        return generator.ToSql();
    }

    internal static string GenerateDetach(SqlDetachStatement statement)
    {
        SqlGenerator generator = SqlGenerator.CreateDefault();
        generator.WriteDetach(statement);
        return generator.ToSql();
    }

    internal static string GenerateReindex(SqlReindexStatement statement)
    {
        SqlGenerator generator = SqlGenerator.CreateDefault();
        generator.WriteReindex(statement);
        return generator.ToSql();
    }

    internal static string GenerateTransaction(SqlTransactionStatement statement)
    {
        SqlGenerator generator = SqlGenerator.CreateDefault();
        generator.WriteTransaction(statement);
        return generator.ToSql();
    }

    internal static string GenerateSavepoint(SqlSavepointStatement statement)
    {
        SqlGenerator generator = SqlGenerator.CreateDefault();
        generator.WriteSavepoint(statement);
        return generator.ToSql();
    }

    internal static string GenerateRelease(SqlReleaseStatement statement)
    {
        SqlGenerator generator = SqlGenerator.CreateDefault();
        generator.WriteRelease(statement);
        return generator.ToSql();
    }

    internal static string GenerateExplain(SqlExplainStatement statement)
    {
        SqlGenerator generator = SqlGenerator.CreateDefault();
        generator.WriteExplain(statement);
        return generator.ToSql();
    }

    internal string ToSql()
    {
        return Writer.ToSql();
    }

    internal void WriteScript(SqlScript script, SqlGenerateOptions options)
    {
        if (script == null)
        {
            throw new ArgumentNullException("script");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.Validate();
        if ((options.PreserveSourceText || Options.Mode == SqlFormatMode.RoundTrip) && script.Tokens.Length > 0)
        {
            WritePreservedSourceTokens(script.Tokens);
            return;
        }

        int index = 0;
        while (index < script.Statements.Length)
        {
            if (index > 0)
            {
                Writer.WriteLine();
            }

            WriteScriptStatement(script.Statements[index], options);
            index = index + 1;
        }

        WritePreservedTriviaComments(script.TrailingTrivia);
    }

    internal void WritePreservedSourceTokens(SqlToken[] tokens)
    {
        int index = 0;
        while (index < tokens.Length)
        {
            if (tokens[index].Kind != SqlTokenKind.EndOfFile)
            {
                Writer.WriteRaw(tokens[index].Text);
            }

            index = index + 1;
        }
    }

    internal void WriteExpression(SqlExpression expression)
    {
        if (expression == null)
        {
            throw new ArgumentNullException("expression");
        }

        WriteExpressionWithPrecedence(expression, 0, false);
    }

    internal void WriteQuery(SqlQuery query)
    {
        if (query == null)
        {
            throw new ArgumentNullException("query");
        }

        WriteCommonTableExpressions(query);
        if (query.CompoundParts.Length > 0 && query.SelectCores.Length > 0)
        {
            WriteSelectCore(query.SelectCores[0]);
            int partIndex = 0;
            while (partIndex < query.CompoundParts.Length)
            {
                Writer.Space();
                Writer.Write(GetCompoundOperatorText(query.CompoundParts[partIndex].OperatorKind));
                Writer.Space();
                WriteSelectCore(query.CompoundParts[partIndex].Core);
                partIndex = partIndex + 1;
            }
        }
        else
        {
            int index = 0;
            while (index < query.SelectCores.Length)
            {
                if (index > 0)
                {
                    Writer.Space();
                    Writer.Write(GetCompoundOperatorText(query.CompoundOperators[index - 1]));
                    Writer.Space();
                }

                WriteSelectCore(query.SelectCores[index]);
                index = index + 1;
            }
        }

        if (query.OrderBy.Length > 0)
        {
            Writer.Write(" ORDER BY ");
            WriteOrderingTerms(query.OrderBy);
        }

        if (query.Limit != null)
        {
            Writer.Space();
            WriteLimitClause(query.Limit);
        }

        if (query.OffsetFetch != null)
        {
            Writer.Space();
            WriteOffsetFetchClause(query.OffsetFetch);
        }

        WriteUnsupportedQueryClauses(query.UnsupportedClauses);
    }

    internal void WriteStatement(SqlStatement statement)
    {
        if (statement == null)
        {
            throw new ArgumentNullException("statement");
        }

        WritePreservedComments(statement);

        if (statement.Kind == SqlStatementKind.Select && statement.Query != null)
        {
            WriteQuery(statement.Query);
            return;
        }

        if (statement.Kind == SqlStatementKind.Insert)
        {
            WriteInsertStatement(RequireInsertStatement(statement.Insert));
            return;
        }

        if (statement.Kind == SqlStatementKind.Update)
        {
            WriteUpdateStatement(RequireUpdateStatement(statement.Update));
            return;
        }

        if (statement.Kind == SqlStatementKind.Delete)
        {
            WriteDeleteStatement(RequireDeleteStatement(statement.Delete));
            return;
        }

        if (statement.Kind == SqlStatementKind.Merge)
        {
            WriteMergeStatement(RequireMergeStatement(statement.Merge));
            return;
        }

        if (statement.Kind == SqlStatementKind.CreateTable)
        {
            WriteCreateTable(RequireTableDefinition(statement.CreateTable));
            return;
        }

        if (statement.Kind == SqlStatementKind.AlterTable)
        {
            WriteAlterTable(RequireAlterTableDefinition(statement.AlterTable));
            return;
        }

        if (statement.Kind == SqlStatementKind.DropTable)
        {
            WriteDropTable(RequireDropTableDefinition(statement.DropTable));
            return;
        }

        if (statement.Kind == SqlStatementKind.CreateIndex)
        {
            WriteCreateIndex(RequireIndexDefinition(statement.CreateIndex));
            return;
        }

        if (statement.Kind == SqlStatementKind.DropIndex)
        {
            WriteDropIndex(RequireDropIndexDefinition(statement.DropIndex));
            return;
        }

        if (statement.Kind == SqlStatementKind.CreateView)
        {
            WriteCreateView(RequireViewDefinition(statement.CreateView));
            return;
        }

        if (statement.Kind == SqlStatementKind.DropView)
        {
            WriteDropView(RequireDropViewDefinition(statement.DropView));
            return;
        }

        if (statement.Kind == SqlStatementKind.CreateTrigger)
        {
            WriteCreateTrigger(RequireTriggerDefinition(statement.CreateTrigger));
            return;
        }

        if (statement.Kind == SqlStatementKind.DropTrigger)
        {
            WriteDropTrigger(RequireDropTriggerDefinition(statement.DropTrigger));
            return;
        }

        if (statement.Kind == SqlStatementKind.CreateVirtualTable)
        {
            WriteCreateVirtualTable(RequireVirtualTableDefinition(statement.CreateVirtualTable));
            return;
        }

        if (statement.Kind == SqlStatementKind.Pragma)
        {
            WritePragma(RequirePragmaStatement(statement.Pragma));
            return;
        }

        if (statement.Kind == SqlStatementKind.Analyze)
        {
            WriteAnalyze(RequireAnalyzeStatement(statement.Analyze));
            return;
        }

        if (statement.Kind == SqlStatementKind.Vacuum)
        {
            WriteVacuum(RequireVacuumStatement(statement.Vacuum));
            return;
        }

        if (statement.Kind == SqlStatementKind.Attach)
        {
            WriteAttach(RequireAttachStatement(statement.Attach));
            return;
        }

        if (statement.Kind == SqlStatementKind.Detach)
        {
            WriteDetach(RequireDetachStatement(statement.Detach));
            return;
        }

        if (statement.Kind == SqlStatementKind.Reindex)
        {
            WriteReindex(RequireReindexStatement(statement.Reindex));
            return;
        }

        if (statement.Kind == SqlStatementKind.Begin || statement.Kind == SqlStatementKind.Commit || statement.Kind == SqlStatementKind.End || statement.Kind == SqlStatementKind.Rollback)
        {
            WriteTransaction(RequireTransactionStatement(statement.Transaction));
            return;
        }

        if (statement.Kind == SqlStatementKind.Savepoint)
        {
            WriteSavepoint(RequireSavepointStatement(statement.Savepoint));
            return;
        }

        if (statement.Kind == SqlStatementKind.Release)
        {
            WriteRelease(RequireReleaseStatement(statement.Release));
            return;
        }

        if (statement.Kind == SqlStatementKind.Explain)
        {
            WriteExplain(RequireExplainStatement(statement.Explain));
            return;
        }

        if (statement.Kind == SqlStatementKind.Unknown)
        {
            WriteUnknownStatement(statement);
        }
    }

    internal void WriteUnknownStatement(SqlStatement statement)
    {
        string sourceText = statement.SourceText;
        if (statement.UnsupportedSyntax.Length > 0)
        {
            sourceText = statement.UnsupportedSyntax[0].SourceText;
        }

        if (sourceText.Length > 0)
        {
            if (statement.Terminator != null && sourceText.EndsWith(statement.Terminator.Text, StringComparison.Ordinal))
            {
                sourceText = sourceText.Substring(0, sourceText.Length - statement.Terminator.Text.Length);
            }

            Writer.WriteRaw(sourceText);
            return;
        }

        WriteTokens(statement.Tokens);
    }

    internal void WriteScriptStatement(SqlStatement statement, SqlGenerateOptions options)
    {
        if (statement.Kind == SqlStatementKind.Empty)
        {
            if (options.IncludeSemicolons)
            {
                Writer.Write(";");
            }

            return;
        }

        WriteStatement(statement);
        if (options.IncludeSemicolons)
        {
            Writer.Write(";");
        }
    }

    internal void WriteInsertStatement(SqlInsertStatement statement)
    {
        if (statement == null)
        {
            throw new ArgumentNullException("statement");
        }

        if (statement.IsReplaceKeyword)
        {
            Writer.Write("REPLACE");
        }
        else
        {
            Writer.Write("INSERT");
            WriteConflictAction(statement.ConflictAction);
        }

        Writer.Write(" INTO ");
        WriteName(statement.TableName);
        if (statement.Alias != null)
        {
            Writer.Write(" AS ");
            WriteName(statement.Alias);
        }

        if (statement.ColumnNames.Length > 0)
        {
            Writer.Write(" (");
            WriteNameList(statement.ColumnNames);
            Writer.Write(")");
        }

        Writer.Space();
        if (statement.SourceKind == SqlInsertSourceKind.Values)
        {
            WriteInsertValues(statement.Values);
        }
        else
        {
            if (statement.SourceKind == SqlInsertSourceKind.Select && statement.SourceQuery != null)
            {
                WriteQuery(statement.SourceQuery);
            }
            else
            {
                if (statement.SourceKind == SqlInsertSourceKind.DefaultValues)
                {
                    Writer.Write("DEFAULT VALUES");
                }
            }
        }

        WriteUpsertClauses(statement.UpsertClauses);
        if (statement.Returning != null)
        {
            Writer.Write(" ");
            WriteReturningClause(statement.Returning);
        }
    }

    internal void WriteUpdateStatement(SqlUpdateStatement statement)
    {
        if (statement == null)
        {
            throw new ArgumentNullException("statement");
        }

        Writer.Write("UPDATE");
        WriteConflictAction(statement.ConflictAction);
        Writer.Space();
        WriteName(statement.TableName);
        if (statement.Alias != null)
        {
            Writer.Write(" AS ");
            WriteName(statement.Alias);
        }

        WriteIndexedBy(statement.IndexedBy, statement.IsNotIndexed);
        Writer.Write(" SET ");
        if (statement.Assignments.Length > 0)
        {
            WriteTypedAssignments(statement.Assignments);
        }
        else
        {
            WriteAssignments(statement.AssignmentColumns, statement.AssignmentExpressions);
        }
        if (statement.From.Length > 0)
        {
            Writer.Write(" FROM ");
            WriteTableReferences(statement.From);
        }

        if (statement.Where != null)
        {
            Writer.Write(" WHERE ");
            WriteExpression(statement.Where);
        }

        if (statement.Returning != null)
        {
            Writer.Write(" ");
            WriteReturningClause(statement.Returning);
        }

        if (statement.OrderBy.Length > 0)
        {
            Writer.Write(" ORDER BY ");
            WriteOrderingTerms(statement.OrderBy);
        }

        if (statement.Limit != null)
        {
            Writer.Space();
            WriteLimitClause(statement.Limit);
        }
    }

    internal void WriteDeleteStatement(SqlDeleteStatement statement)
    {
        if (statement == null)
        {
            throw new ArgumentNullException("statement");
        }

        Writer.Write("DELETE FROM ");
        WriteName(statement.TableName);
        if (statement.Alias != null)
        {
            Writer.Write(" AS ");
            WriteName(statement.Alias);
        }

        WriteIndexedBy(statement.IndexedBy, statement.IsNotIndexed);
        if (statement.Where != null)
        {
            Writer.Write(" WHERE ");
            WriteExpression(statement.Where);
        }

        if (statement.Returning != null)
        {
            Writer.Write(" ");
            WriteReturningClause(statement.Returning);
        }

        if (statement.OrderBy.Length > 0)
        {
            Writer.Write(" ORDER BY ");
            WriteOrderingTerms(statement.OrderBy);
        }

        if (statement.Limit != null)
        {
            Writer.Space();
            WriteLimitClause(statement.Limit);
        }
    }

    internal void WriteMergeStatement(SqlMergeStatement statement)
    {
        if (statement == null)
        {
            throw new ArgumentNullException("statement");
        }

        Writer.Write("MERGE INTO ");
        WriteName(statement.TableName);
        if (statement.Alias != null)
        {
            Writer.Write(" AS ");
            WriteName(statement.Alias);
        }

        Writer.Write(" USING ");
        WriteTableReference(statement.Source);
        Writer.Write(" ON ");
        WriteExpression(statement.Condition);
        int index = 0;
        while (index < statement.Clauses.Length)
        {
            Writer.Space();
            WriteMergeWhenClause(statement.Clauses[index]);
            index = index + 1;
        }

        if (statement.Returning != null)
        {
            Writer.Space();
            WriteReturningClause(statement.Returning);
        }
    }

    internal void WriteUnsupportedSyntax(SqlUnsupportedSyntax syntax)
    {
        if (syntax == null)
        {
            throw new ArgumentNullException("syntax");
        }

        if (syntax.SourceText.Length > 0)
        {
            Writer.WriteRaw(syntax.SourceText);
            return;
        }

        WritePreservedSourceTokens(syntax.Tokens);
    }

    internal void WriteMergeWhenClause(SqlMergeWhenClause clause)
    {
        Writer.Write("WHEN ");
        if (clause.MatchKind == SqlMergeMatchKind.Matched)
        {
            Writer.Write("MATCHED");
        }
        else if (clause.MatchKind == SqlMergeMatchKind.NotMatchedBySource)
        {
            Writer.Write("NOT MATCHED BY SOURCE");
        }
        else
        {
            Writer.Write("NOT MATCHED BY TARGET");
        }

        if (clause.Condition != null)
        {
            Writer.Write(" AND ");
            WriteExpression(clause.Condition);
        }

        Writer.Write(" THEN ");
        if (clause.ActionKind == SqlMergeActionKind.Update)
        {
            Writer.Write("UPDATE SET ");
            WriteTypedAssignments(clause.Assignments);
        }
        else if (clause.ActionKind == SqlMergeActionKind.Delete)
        {
            Writer.Write("DELETE");
        }
        else if (clause.ActionKind == SqlMergeActionKind.Insert)
        {
            Writer.Write("INSERT");
            if (clause.InsertColumns.Length > 0)
            {
                Writer.Write(" (");
                WriteNameList(clause.InsertColumns);
                Writer.Write(")");
            }

            Writer.Write(" VALUES (");
            WriteExpressionList(clause.InsertValues);
            Writer.Write(")");
        }
        else if (clause.ActionKind == SqlMergeActionKind.DoNothing)
        {
            Writer.Write("DO NOTHING");
        }
    }

    internal void WriteCreateTable(SqlTableDefinition table)
    {
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        Writer.Write("CREATE");
        if (table.IsTemporary)
        {
            Writer.Write(" TEMP");
        }

        Writer.Write(" TABLE");
        if (table.IfNotExists)
        {
            Writer.Write(" IF NOT EXISTS");
        }

        Writer.Space();
        WriteName(table.Name);
        if (table.Query != null)
        {
            Writer.Write(" AS ");
            WriteQuery(table.Query);
            return;
        }

        Writer.Write(" (");
        WriteColumnAndTableDefinitions(table);
        Writer.Write(")");
        WriteTableOptions(table);
    }

    internal void WriteAlterTable(SqlAlterTableDefinition alter)
    {
        if (alter == null)
        {
            throw new ArgumentNullException("alter");
        }

        Writer.Write("ALTER TABLE ");
        WriteName(alter.TableName);
        if (alter.ActionKind == SqlAlterTableActionKind.RenameTable)
        {
            Writer.Write(" RENAME TO ");
            WriteName(alter.NewTableName);
            return;
        }

        if (alter.ActionKind == SqlAlterTableActionKind.RenameColumn)
        {
            Writer.Write(" RENAME COLUMN ");
            WriteName(alter.ColumnName);
            Writer.Write(" TO ");
            WriteName(alter.NewColumnName);
            return;
        }

        if (alter.ActionKind == SqlAlterTableActionKind.AddColumn)
        {
            Writer.Write(" ADD COLUMN ");
            WriteColumnDefinition(RequireColumnDefinition(alter.ColumnDefinition));
            return;
        }

        if (alter.ActionKind == SqlAlterTableActionKind.DropColumn)
        {
            Writer.Write(" DROP COLUMN ");
            WriteName(alter.ColumnName);
        }
    }

    internal void WriteDropTable(SqlDropTableDefinition drop)
    {
        if (drop == null)
        {
            throw new ArgumentNullException("drop");
        }

        Writer.Write("DROP TABLE");
        WriteIfExists(drop.IfExists);
        Writer.Space();
        WriteName(drop.Name);
    }

    internal void WriteCreateIndex(SqlIndexDefinition indexDefinition)
    {
        if (indexDefinition == null)
        {
            throw new ArgumentNullException("indexDefinition");
        }

        Writer.Write("CREATE");
        if (indexDefinition.IsUnique)
        {
            Writer.Write(" UNIQUE");
        }

        Writer.Write(" INDEX");
        if (indexDefinition.IfNotExists)
        {
            Writer.Write(" IF NOT EXISTS");
        }

        Writer.Space();
        WriteName(indexDefinition.Name);
        Writer.Write(" ON ");
        WriteName(indexDefinition.TableName);
        Writer.Write(" (");
        if (indexDefinition.IndexedColumns.Length > 0)
        {
            WriteOrderingTerms(indexDefinition.IndexedColumns);
        }
        else
        {
            WriteOrderingTerms(indexDefinition.KeyExpressions);
        }

        Writer.Write(")");
        SqlExpression? predicate = indexDefinition.Predicate;
        if (predicate == null)
        {
            predicate = indexDefinition.WhereExpression;
        }

        if (predicate != null)
        {
            Writer.Write(" WHERE ");
            WriteExpression(predicate);
        }
    }

    internal void WriteDropIndex(SqlDropIndexDefinition drop)
    {
        if (drop == null)
        {
            throw new ArgumentNullException("drop");
        }

        Writer.Write("DROP INDEX");
        WriteIfExists(drop.IfExists);
        Writer.Space();
        WriteName(drop.Name);
    }

    internal void WriteCreateView(SqlViewDefinition view)
    {
        if (view == null)
        {
            throw new ArgumentNullException("view");
        }

        Writer.Write("CREATE");
        if (view.IsTemporary)
        {
            Writer.Write(" TEMP");
        }

        Writer.Write(" VIEW");
        if (view.IfNotExists)
        {
            Writer.Write(" IF NOT EXISTS");
        }

        Writer.Space();
        WriteName(view.Name);
        if (view.ColumnNames.Length > 0)
        {
            Writer.Write(" (");
            WriteNameList(view.ColumnNames);
            Writer.Write(")");
        }

        Writer.Write(" AS ");
        WriteQuery(view.Query);
    }

    internal void WriteDropView(SqlDropViewDefinition drop)
    {
        if (drop == null)
        {
            throw new ArgumentNullException("drop");
        }

        Writer.Write("DROP VIEW");
        WriteIfExists(drop.IfExists);
        Writer.Space();
        WriteName(drop.Name);
    }

    internal void WriteCreateVirtualTable(SqlVirtualTableDefinition table)
    {
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        Writer.Write("CREATE VIRTUAL TABLE");
        if (table.IfNotExists)
        {
            Writer.Write(" IF NOT EXISTS");
        }

        Writer.Space();
        WriteName(table.Name);
        Writer.Write(" USING ");
        WriteName(table.ModuleName);
        Writer.Write("(");
        if (table.Arguments.Length > 0)
        {
            WriteVirtualTableArguments(table.Arguments);
        }
        else
        {
            WriteTokens(table.ModuleArguments);
        }

        Writer.Write(")");
    }

    internal void WriteVirtualTableArguments(SqlVirtualTableArgument[] arguments)
    {
        int index = 0;
        while (index < arguments.Length)
        {
            if (index > 0)
            {
                Writer.Write(",");
            }

            Writer.Write(arguments[index].SourceText);
            index = index + 1;
        }
    }

    internal void WriteCreateTrigger(SqlTriggerDefinition trigger)
    {
        if (trigger == null)
        {
            throw new ArgumentNullException("trigger");
        }

        Writer.Write("CREATE");
        if (trigger.IsTemporary)
        {
            Writer.Write(" TEMP");
        }

        Writer.Write(" TRIGGER");
        if (trigger.IfNotExists)
        {
            Writer.Write(" IF NOT EXISTS");
        }

        Writer.Space();
        WriteName(trigger.Name);
        WriteTriggerTiming(trigger.Timing);
        WriteTriggerEvent(trigger);
        Writer.Write(" ON ");
        WriteName(trigger.TargetName);
        if (trigger.ForEachRow)
        {
            Writer.Write(" FOR EACH ROW");
        }

        if (trigger.WhenExpression != null)
        {
            Writer.Write(" WHEN ");
            WriteExpression(trigger.WhenExpression);
        }

        Writer.Write(" BEGIN ");
        WriteTriggerBody(trigger.Body);
        Writer.Write(" END");
    }

    internal void WriteDropTrigger(SqlDropTriggerDefinition drop)
    {
        if (drop == null)
        {
            throw new ArgumentNullException("drop");
        }

        Writer.Write("DROP TRIGGER");
        WriteIfExists(drop.IfExists);
        Writer.Space();
        WriteName(drop.Name);
    }

    internal void WritePragma(SqlPragmaStatement pragma)
    {
        if (pragma == null)
        {
            throw new ArgumentNullException("pragma");
        }

        Writer.Write("PRAGMA ");
        WriteName(pragma.Name);
        if (pragma.Value == null)
        {
            return;
        }

        if (pragma.ValueKind == SqlPragmaValueKind.Equals)
        {
            Writer.Write(" = ");
            WriteExpression(pragma.Value);
            return;
        }

        if (pragma.ValueKind == SqlPragmaValueKind.Parenthesized)
        {
            WritePragmaParenthesizedValue(pragma.Value);
        }
    }

    internal void WriteAnalyze(SqlAnalyzeStatement analyze)
    {
        if (analyze == null)
        {
            throw new ArgumentNullException("analyze");
        }

        Writer.Write("ANALYZE");
        if (analyze.TargetName != null)
        {
            Writer.Space();
            WriteName(analyze.TargetName);
        }
    }

    internal void WriteVacuum(SqlVacuumStatement vacuum)
    {
        if (vacuum == null)
        {
            throw new ArgumentNullException("vacuum");
        }

        Writer.Write("VACUUM");
        if (vacuum.DatabaseName != null)
        {
            Writer.Space();
            WriteName(vacuum.DatabaseName);
        }

        if (vacuum.IntoExpression != null)
        {
            Writer.Write(" INTO ");
            WriteExpression(vacuum.IntoExpression);
        }
    }

    internal void WriteAttach(SqlAttachStatement attach)
    {
        if (attach == null)
        {
            throw new ArgumentNullException("attach");
        }

        Writer.Write("ATTACH");
        if (attach.HasDatabaseKeyword)
        {
            Writer.Write(" DATABASE");
        }

        Writer.Space();
        if (attach.DatabaseExpression != null)
        {
            WriteExpression(attach.DatabaseExpression);
        }

        Writer.Write(" AS ");
        WriteName(attach.SchemaName);
    }

    internal void WriteDetach(SqlDetachStatement detach)
    {
        if (detach == null)
        {
            throw new ArgumentNullException("detach");
        }

        Writer.Write("DETACH");
        if (detach.HasDatabaseKeyword)
        {
            Writer.Write(" DATABASE");
        }

        Writer.Space();
        WriteName(detach.SchemaName);
    }

    internal void WriteReindex(SqlReindexStatement reindex)
    {
        if (reindex == null)
        {
            throw new ArgumentNullException("reindex");
        }

        Writer.Write("REINDEX");
        if (reindex.TargetName != null)
        {
            Writer.Space();
            WriteName(reindex.TargetName);
        }
    }

    internal void WriteTransaction(SqlTransactionStatement transaction)
    {
        if (transaction == null)
        {
            throw new ArgumentNullException("transaction");
        }

        WriteTransactionAction(transaction.ActionKind);
        if (transaction.ActionKind == SqlTransactionActionKind.Begin)
        {
            WriteTransactionMode(transaction.Mode);
            if (transaction.HasTransactionKeyword)
            {
                Writer.Write(" TRANSACTION");
            }

            return;
        }

        if (transaction.HasTransactionKeyword)
        {
            Writer.Write(" TRANSACTION");
        }

        if (transaction.RollbackTarget != null)
        {
            Writer.Write(" TO");
            if (transaction.RollbackTarget.HasSavepointKeyword)
            {
                Writer.Write(" SAVEPOINT");
            }

            Writer.Space();
            WriteName(transaction.RollbackTarget.Name);
        }
        else if (transaction.IsToSavepoint)
        {
            Writer.Write(" TO");
            if (transaction.HasSavepointKeyword)
            {
                Writer.Write(" SAVEPOINT");
            }

            Writer.Space();
            WriteName(transaction.SavepointName);
        }
    }

    internal void WriteSavepoint(SqlSavepointStatement savepoint)
    {
        if (savepoint == null)
        {
            throw new ArgumentNullException("savepoint");
        }

        Writer.Write("SAVEPOINT");
        if (savepoint.Name != null)
        {
            Writer.Space();
            WriteName(savepoint.Name);
        }
    }

    internal void WriteRelease(SqlReleaseStatement release)
    {
        if (release == null)
        {
            throw new ArgumentNullException("release");
        }

        Writer.Write("RELEASE");
        if (release.HasSavepointKeyword)
        {
            Writer.Write(" SAVEPOINT");
        }

        if (release.Name != null)
        {
            Writer.Space();
            WriteName(release.Name);
        }
    }

    internal void WriteExplain(SqlExplainStatement explain)
    {
        if (explain == null)
        {
            throw new ArgumentNullException("explain");
        }

        Writer.Write("EXPLAIN");
        if (explain.Kind == SqlExplainKind.QueryPlan)
        {
            Writer.Write(" QUERY PLAN");
        }

        if (explain.Statement != null)
        {
            Writer.Space();
            WriteStatement(explain.Statement);
        }
    }

    internal void WriteSelectCore(SqlSelectCore core)
    {
        if (core == null)
        {
            throw new ArgumentNullException("core");
        }

        if (core.IsValues)
        {
            WriteValuesCore(core);
            return;
        }

        Writer.Write("SELECT");
        if (core.IsDistinct)
        {
            Writer.Write(" DISTINCT");
        }
        else
        {
            if (core.IsAll)
            {
                Writer.Write(" ALL");
            }
        }

        if (core.Top != null)
        {
            Writer.Space();
            WriteTopClause(core.Top);
        }

        if (core.ResultColumns.Length > 0)
        {
            Writer.Space();
            WriteResultColumns(core.ResultColumns);
            WriteSelectTrailingComma();
        }

        if (core.From.Length > 0)
        {
            Writer.Write(" FROM ");
            WriteTableReferences(core.From);
        }

        if (core.Where != null)
        {
            Writer.Write(" WHERE ");
            WriteExpression(core.Where);
        }

        if (core.GroupBy.Length > 0)
        {
            Writer.Write(" GROUP BY ");
            WriteExpressionList(core.GroupBy);
        }

        if (core.Having != null)
        {
            Writer.Write(" HAVING ");
            WriteExpression(core.Having);
        }

        if (core.Windows.Length > 0)
        {
            Writer.Write(" WINDOW ");
            WriteNamedWindowDefinitions(core.Windows);
        }

        WriteUnsupportedQueryClauses(core.UnsupportedClauses);
    }

    internal void WriteExpressionWithPrecedence(SqlExpression expression, int parentPrecedence, bool isRightChild)
    {
        int precedence = SqlPrecedence.GetExpressionPrecedence(expression);
        bool parenthesize = ShouldParenthesize(expression, precedence, parentPrecedence, isRightChild);
        if (parenthesize)
        {
            Writer.Write("(");
        }

        WriteExpressionCore(expression, precedence);
        if (parenthesize)
        {
            Writer.Write(")");
        }
    }

    internal void WriteCommonTableExpressions(SqlQuery query)
    {
        if (query.CommonTableExpressions.Length == 0)
        {
            return;
        }

        Writer.Write("WITH");
        if (query.IsRecursive)
        {
            Writer.Write(" RECURSIVE");
        }

        Writer.Space();
        int index = 0;
        while (index < query.CommonTableExpressions.Length)
        {
            if (index > 0)
            {
                WriteListSeparator();
            }

            WriteCommonTableExpression(query.CommonTableExpressions[index]);
            index = index + 1;
        }

        Writer.Space();
    }

    internal void WriteCommonTableExpression(SqlCommonTableExpression expression)
    {
        WriteName(expression.Name);
        if (expression.ColumnNames.Length > 0)
        {
            Writer.Write("(");
            WriteNameList(expression.ColumnNames);
            Writer.Write(")");
        }

        Writer.Write(" AS");
        if (expression.Materialization == SqlCteMaterializationKind.Materialized)
        {
            Writer.Write(" MATERIALIZED");
        }
        else
        {
            if (expression.Materialization == SqlCteMaterializationKind.NotMaterialized)
            {
                Writer.Write(" NOT MATERIALIZED");
            }
        }

        Writer.Write(" (");
        WriteQuery(expression.Query);
        Writer.Write(")");
    }

    internal void WriteValuesCore(SqlSelectCore core)
    {
        Writer.Write("VALUES ");
        int rowIndex = 0;
        while (rowIndex < core.Values.Length)
        {
            if (rowIndex > 0)
            {
                WriteListSeparator();
            }

            Writer.Write("(");
            WriteExpressionList(core.Values[rowIndex]);
            Writer.Write(")");
            rowIndex = rowIndex + 1;
        }
    }

    internal void WriteResultColumns(SqlResultColumn[] resultColumns)
    {
        int index = 0;
        while (index < resultColumns.Length)
        {
            if (index > 0)
            {
                WriteListSeparator();
            }

            WriteResultColumn(resultColumns[index]);

            index = index + 1;
        }
    }

    internal void WriteResultColumn(SqlResultColumn resultColumn)
    {
        if (resultColumn.IsWildcard)
        {
            if (resultColumn.Qualifier != null)
            {
                WriteName(resultColumn.Qualifier);
                Writer.Write(".");
            }

            Writer.Write("*");
        }
        else
        {
            WriteExpression(RequireExpression(resultColumn.Expression, "Expression"));
        }

        if (resultColumn.Alias != null)
        {
            Writer.Write(" AS ");
            WriteName(resultColumn.Alias);
        }
    }

    internal void WriteTableReferences(SqlTableReference[] tables)
    {
        int index = 0;
        while (index < tables.Length)
        {
            if (index > 0)
            {
                WriteListSeparator();
            }

            WriteTableReference(tables[index]);
            index = index + 1;
        }
    }

    internal void WriteTableReference(SqlTableReference table)
    {
        if (table.SourceData != null && table.SourceData.IsLateral)
        {
            Writer.Write("LATERAL ");
        }

        if (table.Kind == SqlTableReferenceKind.Named)
        {
            if (table.SourceData != null && table.SourceData.Name != null)
            {
                WriteName(table.SourceData.Name);
            }
            else
            {
                WriteName(table.Name);
            }
        }
        else if (table.Kind == SqlTableReferenceKind.Function)
        {
            if (table.SourceData != null && table.SourceData.Name != null)
            {
                WriteName(table.SourceData.Name);
                Writer.Write("(");
                WriteExpressionList(table.SourceData.FunctionArguments);
                Writer.Write(")");
            }
            else
            {
                WriteExpression(RequireExpression(table.FunctionExpression, "FunctionExpression"));
            }
        }
        else if (table.Kind == SqlTableReferenceKind.Subquery || table.Kind == SqlTableReferenceKind.Values)
        {
            Writer.Write("(");
            if (table.SourceData != null && table.SourceData.Query != null)
            {
                WriteQuery(table.SourceData.Query);
            }
            else if (table.SourceData != null && table.SourceData.Values.Length > 0)
            {
                WriteValuesRows(table.SourceData.Values);
            }
            else
            {
                WriteQuery(RequireQuery(table.Query));
            }

            Writer.Write(")");
        }
        else if (table.Kind == SqlTableReferenceKind.Parenthesized || table.Kind == SqlTableReferenceKind.Join)
        {
            Writer.Write("(");
            WriteTableReference(RequireTableReference(table.Inner));
            Writer.Write(")");
        }

        WriteTableAlias(table);
        WriteUnsupportedQueryClauses(table.UnsupportedClauses);
        WriteJoins(table.Joins);
    }

    internal void WriteValuesRows(SqlExpression[][] rows)
    {
        Writer.Write("VALUES ");
        int rowIndex = 0;
        while (rowIndex < rows.Length)
        {
            if (rowIndex > 0)
            {
                WriteListSeparator();
            }

            Writer.Write("(");
            WriteExpressionList(rows[rowIndex]);
            Writer.Write(")");
            rowIndex = rowIndex + 1;
        }
    }

    internal void WriteTableAlias(SqlTableReference table)
    {
        if (table.Alias == null)
        {
            return;
        }

        Writer.Write(" AS ");
        WriteName(table.Alias);
        if (table.ColumnAliases.Length > 0)
        {
            Writer.Write("(");
            WriteNameList(table.ColumnAliases);
            Writer.Write(")");
        }
    }

    internal void WriteJoins(SqlJoin[] joins)
    {
        int index = 0;
        while (index < joins.Length)
        {
            Writer.Space();
            WriteJoin(joins[index]);
            index = index + 1;
        }
    }

    internal void WriteJoin(SqlJoin join)
    {
        WriteJoinOperator(join);
        Writer.Space();
        WriteTableReference(join.Right);
        if (join.ConstraintKind == SqlJoinConstraintKind.On && join.OnExpression != null)
        {
            Writer.Write(" ON ");
            WriteExpression(join.OnExpression);
        }
        else
        {
            if (join.ConstraintKind == SqlJoinConstraintKind.Using)
            {
                Writer.Write(" USING (");
                WriteNameList(join.UsingColumns);
                Writer.Write(")");
            }
        }

        WriteUnsupportedQueryClauses(join.UnsupportedClauses);
    }

    internal void WriteJoinOperator(SqlJoin join)
    {
        if (join.IsNatural)
        {
            Writer.Write("NATURAL ");
        }

        if (join.Kind == SqlJoinKind.Cross)
        {
            Writer.Write("CROSS JOIN");
            return;
        }

        if (join.Kind == SqlJoinKind.CrossApply)
        {
            Writer.Write("CROSS APPLY");
            return;
        }

        if (join.Kind == SqlJoinKind.OuterApply)
        {
            Writer.Write("OUTER APPLY");
            return;
        }

        if (join.Kind == SqlJoinKind.Semi)
        {
            if (HasJoinModifier(join.Modifiers, SqlJoinModifierKind.Left))
            {
                Writer.Write("LEFT ");
            }
            else if (HasJoinModifier(join.Modifiers, SqlJoinModifierKind.Right))
            {
                Writer.Write("RIGHT ");
            }

            Writer.Write("SEMI JOIN");
            return;
        }

        if (join.Kind == SqlJoinKind.Anti)
        {
            if (HasJoinModifier(join.Modifiers, SqlJoinModifierKind.Left))
            {
                Writer.Write("LEFT ");
            }
            else if (HasJoinModifier(join.Modifiers, SqlJoinModifierKind.Right))
            {
                Writer.Write("RIGHT ");
            }

            Writer.Write("ANTI JOIN");
            return;
        }

        if (join.Kind == SqlJoinKind.AsOf)
        {
            Writer.Write("ASOF JOIN");
            return;
        }

        if (join.Kind == SqlJoinKind.Straight)
        {
            Writer.Write("STRAIGHT_JOIN");
            return;
        }

        if (join.Kind == SqlJoinKind.Left)
        {
            Writer.Write("LEFT");
            if (join.IsOuter)
            {
                Writer.Write(" OUTER");
            }

            Writer.Write(" JOIN");
            return;
        }

        if (join.Kind == SqlJoinKind.Right)
        {
            Writer.Write("RIGHT");
            if (join.IsOuter)
            {
                Writer.Write(" OUTER");
            }

            Writer.Write(" JOIN");
            return;
        }

        if (join.Kind == SqlJoinKind.Full)
        {
            Writer.Write("FULL");
            if (join.IsOuter)
            {
                Writer.Write(" OUTER");
            }

            Writer.Write(" JOIN");
            return;
        }

        if (join.Kind == SqlJoinKind.Natural)
        {
            Writer.Write("JOIN");
            return;
        }

        if (join.Kind == SqlJoinKind.Inner)
        {
            Writer.Write("JOIN");
            return;
        }

        Writer.Write("JOIN");
    }

    internal void WriteNamedWindowDefinitions(SqlWindowDefinition[] windows)
    {
        int index = 0;
        while (index < windows.Length)
        {
            if (index > 0)
            {
                WriteListSeparator();
            }

            WriteNamedWindowDefinition(windows[index]);
            index = index + 1;
        }
    }

    internal void WriteNamedWindowDefinition(SqlWindowDefinition windowDefinition)
    {
        WriteName(windowDefinition.Name);
        Writer.Write(" AS ");
        WriteWindowDefinition(windowDefinition);
    }

    internal void WriteLimitClause(SqlLimitClause limit)
    {
        Writer.Write("LIMIT ");
        WriteExpression(limit.LimitExpression);
        if (limit.OffsetExpression != null)
        {
            Writer.Write(" OFFSET ");
            WriteExpression(limit.OffsetExpression);
        }
    }

    internal void WriteTopClause(SqlTopClause top)
    {
        Writer.Write("TOP ");
        if (top.IsParenthesized)
        {
            Writer.Write("(");
        }

        WriteExpression(top.Quantity);
        if (top.IsParenthesized)
        {
            Writer.Write(")");
        }

        if (top.IsPercent)
        {
            Writer.Write(" PERCENT");
        }

        if (top.WithTies)
        {
            Writer.Write(" WITH TIES");
        }
    }

    internal void WriteOffsetFetchClause(SqlOffsetFetchClause clause)
    {
        if (clause.Offset != null)
        {
            Writer.Write("OFFSET ");
            WriteExpression(clause.Offset);
            Writer.Write(" ROWS");
        }

        if (clause.Direction != SqlFetchDirectionKind.None)
        {
            if (clause.Offset != null)
            {
                Writer.Space();
            }

            Writer.Write("FETCH ");
            if (clause.Direction == SqlFetchDirectionKind.First)
            {
                Writer.Write("FIRST");
            }
            else
            {
                Writer.Write("NEXT");
            }

            if (clause.Fetch != null)
            {
                Writer.Space();
                WriteExpression(clause.Fetch);
            }

            Writer.Write(" ROWS");
            if (clause.WithTies)
            {
                Writer.Write(" WITH TIES");
            }
            else
            {
                Writer.Write(" ONLY");
            }
        }
    }

    internal void WriteUnsupportedQueryClauses(SqlUnsupportedQueryClause[] clauses)
    {
        int index = 0;
        while (index < clauses.Length)
        {
            Writer.Space();
            Writer.Write(clauses[index].SourceText);
            index = index + 1;
        }
    }

    internal bool HasJoinModifier(SqlJoinModifierKind[] modifiers, SqlJoinModifierKind expected)
    {
        int index = 0;
        while (index < modifiers.Length)
        {
            if (modifiers[index] == expected)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void WriteConflictAction(SqlConflictActionKind action)
    {
        if (action == SqlConflictActionKind.None)
        {
            return;
        }

        Writer.Write(" OR ");
        Writer.Write(GetConflictActionText(action));
    }

    internal void WriteIndexedBy(SqlName? indexedBy, bool isNotIndexed)
    {
        if (indexedBy != null)
        {
            Writer.Write(" INDEXED BY ");
            WriteName(indexedBy);
            return;
        }

        if (isNotIndexed)
        {
            Writer.Write(" NOT INDEXED");
        }
    }

    internal void WriteInsertValues(SqlExpression[][] rows)
    {
        Writer.Write("VALUES ");
        int rowIndex = 0;
        while (rowIndex < rows.Length)
        {
            if (rowIndex > 0)
            {
                WriteListSeparator();
            }

            Writer.Write("(");
            WriteExpressionList(rows[rowIndex]);
            Writer.Write(")");
            rowIndex = rowIndex + 1;
        }
    }

    internal void WriteAssignments(SqlName[] columns, SqlExpression[] expressions)
    {
        int index = 0;
        while (index < columns.Length && index < expressions.Length)
        {
            if (index > 0)
            {
                WriteListSeparator();
            }

            WriteName(columns[index]);
            Writer.Write(" = ");
            WriteExpression(expressions[index]);
            index = index + 1;
        }
    }

    internal void WriteTypedAssignments(SqlAssignment[] assignments)
    {
        int index = 0;
        while (index < assignments.Length)
        {
            if (index > 0)
            {
                WriteListSeparator();
            }

            if (assignments[index].Columns.Length > 1)
            {
                Writer.Write("(");
                WriteNameList(assignments[index].Columns);
                Writer.Write(")");
            }
            else
            {
                WriteName(assignments[index].Columns[0]);
            }

            Writer.Write(" = ");
            WriteExpression(assignments[index].Value);
            index = index + 1;
        }
    }

    internal void WriteUpsertClauses(SqlUpsertClause[] clauses)
    {
        int index = 0;
        while (index < clauses.Length)
        {
            Writer.Space();
            WriteUpsertClause(clauses[index]);
            index = index + 1;
        }
    }

    internal void WriteUpsertClause(SqlUpsertClause clause)
    {
        if (clause.SyntaxKind == SqlUpsertSyntaxKind.OnDuplicateKey)
        {
            Writer.Write("ON DUPLICATE KEY UPDATE ");
            WriteTypedAssignments(clause.Assignments);
            return;
        }

        Writer.Write("ON CONFLICT");
        if (clause.ConflictTarget != null && clause.ConflictTarget.Kind == SqlConflictTargetKind.Constraint && clause.ConflictTarget.ConstraintName != null)
        {
            Writer.Write(" ON CONSTRAINT ");
            WriteName(clause.ConflictTarget.ConstraintName);
        }
        else if (clause.ConflictTarget != null && clause.ConflictTarget.Kind == SqlConflictTargetKind.IndexedColumns && clause.ConflictTarget.IndexedColumns.Length > 0)
        {
            Writer.Write("(");
            WriteOrderingTerms(clause.ConflictTarget.IndexedColumns);
            Writer.Write(")");
        }
        else if (clause.ConflictTargets.Length > 0)
        {
            Writer.Write("(");
            WriteOrderingTerms(clause.ConflictTargets);
            Writer.Write(")");
        }

        SqlExpression? conflictPredicate = clause.ConflictWhere;
        if (clause.ConflictTarget != null)
        {
            conflictPredicate = clause.ConflictTarget.Predicate;
        }

        if (conflictPredicate != null)
        {
            Writer.Write(" WHERE ");
            WriteExpression(conflictPredicate);
        }

        if (clause.ActionKind == SqlUpsertActionKind.DoNothing)
        {
            Writer.Write(" DO NOTHING");
            return;
        }

        if (clause.ActionKind == SqlUpsertActionKind.DoUpdate)
        {
            Writer.Write(" DO UPDATE SET ");
            if (clause.Assignments.Length > 0)
            {
                WriteTypedAssignments(clause.Assignments);
            }
            else
            {
                WriteAssignments(clause.UpdateColumns, clause.UpdateExpressions);
            }
            if (clause.UpdateWhere != null)
            {
                Writer.Write(" WHERE ");
                WriteExpression(clause.UpdateWhere);
            }
        }
    }

    internal void WriteReturningClause(SqlReturningClause returning)
    {
        Writer.Write("RETURNING ");
        WriteResultColumns(returning.ResultColumns);
    }

    internal void WriteColumnAndTableDefinitions(SqlTableDefinition table)
    {
        bool wroteItem = false;
        int columnIndex = 0;
        while (columnIndex < table.Columns.Length)
        {
            if (wroteItem)
            {
                WriteListSeparator();
            }

            WriteColumnDefinition(table.Columns[columnIndex]);
            wroteItem = true;
            columnIndex = columnIndex + 1;
        }

        int constraintIndex = 0;
        while (constraintIndex < table.Constraints.Length)
        {
            if (wroteItem)
            {
                WriteListSeparator();
            }

            WriteTableConstraint(table.Constraints[constraintIndex]);
            wroteItem = true;
            constraintIndex = constraintIndex + 1;
        }
    }

    internal void WriteColumnDefinition(SqlColumnDefinition column)
    {
        WriteName(column.Name);
        if (column.DeclaredType != null)
        {
            Writer.Space();
            Writer.Write(column.DeclaredType.SourceText);
        }
        else if (column.SourceTypeName.Length > 0)
        {
            Writer.Space();
            Writer.Write(column.SourceTypeName);
        }
        else if (column.TypeName.Length > 0)
        {
            Writer.Space();
            Writer.Write(column.TypeName);
        }

        int index = 0;
        while (index < column.Constraints.Length)
        {
            Writer.Space();
            WriteColumnConstraint(column.Constraints[index]);
            index = index + 1;
        }

        if (column.DefaultExpression != null && !HasColumnConstraintKind(column.Constraints, SqlConstraintKind.Default))
        {
            Writer.Write(" DEFAULT ");
            WriteExpression(column.DefaultExpression);
        }

        if (column.GeneratedExpression != null && !HasColumnConstraintKind(column.Constraints, SqlConstraintKind.Generated))
        {
            Writer.Write(" GENERATED ALWAYS AS ");
            WriteExpression(column.GeneratedExpression);
            WriteGeneratedKind(column.GeneratedKind);
        }
    }

    internal bool HasColumnConstraintKind(SqlColumnConstraint[] constraints, SqlConstraintKind kind)
    {
        int index = 0;
        while (index < constraints.Length)
        {
            if (constraints[index].Kind == kind)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void WriteColumnConstraint(SqlColumnConstraint constraint)
    {
        WriteOptionalConstraintName(constraint.Name);
        if (constraint.Kind == SqlConstraintKind.PrimaryKey)
        {
            Writer.Write("PRIMARY KEY");
            if (constraint.PrimaryKeyDirection == SqlOrderingDirection.Asc)
            {
                Writer.Write(" ASC");
            }
            else if (constraint.PrimaryKeyDirection == SqlOrderingDirection.Desc)
            {
                Writer.Write(" DESC");
            }

            WriteOnConflict(constraint.ConflictAction);
            if (constraint.IsAutoincrement)
            {
                Writer.Write(" AUTOINCREMENT");
            }

            return;
        }

        if (constraint.Kind == SqlConstraintKind.NotNull)
        {
            Writer.Write("NOT NULL");
            WriteOnConflict(constraint.ConflictAction);
            return;
        }

        if (constraint.Kind == SqlConstraintKind.Unique)
        {
            Writer.Write("UNIQUE");
            WriteOnConflict(constraint.ConflictAction);
            return;
        }

        if (constraint.Kind == SqlConstraintKind.Check)
        {
            Writer.Write("CHECK ");
            WriteExpression(RequireExpression(constraint.Expression, "Expression"));
            return;
        }

        if (constraint.Kind == SqlConstraintKind.Default)
        {
            Writer.Write("DEFAULT ");
            WriteExpression(RequireExpression(constraint.Expression, "Expression"));
            return;
        }

        if (constraint.Kind == SqlConstraintKind.Collate)
        {
            Writer.Write("COLLATE ");
            WriteName(constraint.CollationName);
            return;
        }

        if (constraint.Kind == SqlConstraintKind.References && constraint.References != null)
        {
            WriteForeignKeyReference(constraint.References);
            return;
        }

        if (constraint.Kind == SqlConstraintKind.Generated)
        {
            if (constraint.IsGeneratedAlways)
            {
                Writer.Write("GENERATED ALWAYS ");
            }

            Writer.Write("AS ");
            WriteExpression(RequireExpression(constraint.Expression, "Expression"));
            if (constraint.HasGeneratedStorageKind)
            {
                WriteGeneratedKind(constraint.GeneratedKind);
            }
        }
    }

    internal void WriteTableConstraint(SqlTableConstraint constraint)
    {
        WriteOptionalConstraintName(constraint.Name);
        if (constraint.Kind == SqlConstraintKind.PrimaryKey)
        {
            Writer.Write("PRIMARY KEY (");
            WriteTableIndexedColumns(constraint);
            Writer.Write(")");
            WriteOnConflict(constraint.ConflictAction);
            return;
        }

        if (constraint.Kind == SqlConstraintKind.Unique)
        {
            Writer.Write("UNIQUE (");
            WriteTableIndexedColumns(constraint);
            Writer.Write(")");
            WriteOnConflict(constraint.ConflictAction);
            return;
        }

        if (constraint.Kind == SqlConstraintKind.Check)
        {
            Writer.Write("CHECK ");
            if (constraint.Expressions.Length > 0)
            {
                WriteExpression(constraint.Expressions[0]);
            }

            return;
        }

        if (constraint.Kind == SqlConstraintKind.ForeignKey)
        {
            Writer.Write("FOREIGN KEY (");
            WriteNameList(constraint.Columns);
            Writer.Write(") ");
            if (constraint.References != null)
            {
                WriteForeignKeyReference(constraint.References);
            }
        }
    }

    internal void WriteForeignKeyReference(SqlForeignKeyReference reference)
    {
        Writer.Write("REFERENCES ");
        WriteName(reference.TableName);
        if (reference.ColumnNames.Length > 0)
        {
            Writer.Write("(");
            WriteNameList(reference.ColumnNames);
            Writer.Write(")");
        }

        if (reference.Clauses.Length > 0)
        {
            WriteForeignKeyClauses(reference.Clauses);
            return;
        }

        if (reference.OnDelete != SqlForeignKeyActionKind.None)
        {
            Writer.Write(" ON DELETE ");
            WriteForeignKeyAction(reference.OnDelete);
        }
        else if (reference.OnDeleteAction.Length > 0)
        {
            Writer.Write(" ON DELETE ");
            Writer.Write(reference.OnDeleteAction.ToUpperInvariant());
        }

        if (reference.OnUpdate != SqlForeignKeyActionKind.None)
        {
            Writer.Write(" ON UPDATE ");
            WriteForeignKeyAction(reference.OnUpdate);
        }
        else if (reference.OnUpdateAction.Length > 0)
        {
            Writer.Write(" ON UPDATE ");
            Writer.Write(reference.OnUpdateAction.ToUpperInvariant());
        }

        if (reference.MatchName != null)
        {
            Writer.Write(" MATCH ");
            WriteName(reference.MatchName);
        }

        if (reference.Deferrability == SqlForeignKeyDeferrabilityKind.NotDeferrable)
        {
            Writer.Write(" NOT DEFERRABLE");
        }
        else if (reference.Deferrability == SqlForeignKeyDeferrabilityKind.Deferrable || reference.IsDeferrable)
        {
            Writer.Write(" DEFERRABLE");
        }

        if (reference.InitialTiming == SqlForeignKeyInitialTimingKind.Deferred || reference.IsInitiallyDeferred)
        {
            Writer.Write(" INITIALLY DEFERRED");
        }
        else if (reference.InitialTiming == SqlForeignKeyInitialTimingKind.Immediate)
        {
            Writer.Write(" INITIALLY IMMEDIATE");
        }
    }

    internal void WriteTableIndexedColumns(SqlTableConstraint constraint)
    {
        if (constraint.IndexedColumns.Length > 0)
        {
            WriteOrderingTerms(constraint.IndexedColumns);
            return;
        }

        WriteNameList(constraint.Columns);
    }

    internal void WriteForeignKeyClauses(SqlForeignKeyClause[] clauses)
    {
        int index = 0;
        while (index < clauses.Length)
        {
            SqlForeignKeyClause clause = clauses[index];
            if (clause.Kind == SqlForeignKeyClauseKind.OnDelete)
            {
                Writer.Write(" ON DELETE ");
                WriteForeignKeyAction(clause.Action);
            }
            else if (clause.Kind == SqlForeignKeyClauseKind.OnUpdate)
            {
                Writer.Write(" ON UPDATE ");
                WriteForeignKeyAction(clause.Action);
            }
            else if (clause.Kind == SqlForeignKeyClauseKind.Match)
            {
                Writer.Write(" MATCH ");
                WriteName(clause.MatchName);
            }
            else if (clause.Kind == SqlForeignKeyClauseKind.Deferrability)
            {
                if (clause.Deferrability == SqlForeignKeyDeferrabilityKind.NotDeferrable)
                {
                    Writer.Write(" NOT DEFERRABLE");
                }
                else
                {
                    Writer.Write(" DEFERRABLE");
                }

                if (clause.InitialTiming == SqlForeignKeyInitialTimingKind.Deferred)
                {
                    Writer.Write(" INITIALLY DEFERRED");
                }
                else if (clause.InitialTiming == SqlForeignKeyInitialTimingKind.Immediate)
                {
                    Writer.Write(" INITIALLY IMMEDIATE");
                }
            }

            index = index + 1;
        }
    }

    internal void WriteForeignKeyAction(SqlForeignKeyActionKind action)
    {
        if (action == SqlForeignKeyActionKind.NoAction)
        {
            Writer.Write("NO ACTION");
        }
        else if (action == SqlForeignKeyActionKind.Restrict)
        {
            Writer.Write("RESTRICT");
        }
        else if (action == SqlForeignKeyActionKind.SetNull)
        {
            Writer.Write("SET NULL");
        }
        else if (action == SqlForeignKeyActionKind.SetDefault)
        {
            Writer.Write("SET DEFAULT");
        }
        else if (action == SqlForeignKeyActionKind.Cascade)
        {
            Writer.Write("CASCADE");
        }
    }

    internal void WriteTableOptions(SqlTableDefinition table)
    {
        if (table.Options.Length > 0)
        {
            int index = 0;
            while (index < table.Options.Length)
            {
                if (index > 0)
                {
                    Writer.Write(",");
                }

                if (table.Options[index] == SqlTableOptionKind.Strict)
                {
                    Writer.Write(" STRICT");
                }
                else if (table.Options[index] == SqlTableOptionKind.WithoutRowId)
                {
                    Writer.Write(" WITHOUT ROWID");
                }

                index = index + 1;
            }

            return;
        }

        bool wroteOption = false;
        if (table.IsStrict)
        {
            Writer.Write(" STRICT");
            wroteOption = true;
        }

        if (table.WithoutRowId)
        {
            if (wroteOption)
            {
                Writer.Write(",");
            }

            Writer.Write(" WITHOUT ROWID");
        }
    }

    internal void WriteOptionalConstraintName(SqlName? name)
    {
        if (name == null)
        {
            return;
        }

        Writer.Write("CONSTRAINT ");
        WriteName(name);
        Writer.Space();
    }

    internal void WriteOnConflict(SqlConflictActionKind action)
    {
        if (action == SqlConflictActionKind.None)
        {
            return;
        }

        Writer.Write(" ON CONFLICT ");
        Writer.Write(GetConflictActionText(action));
    }

    internal void WriteGeneratedKind(SqlGeneratedColumnKind kind)
    {
        if (kind == SqlGeneratedColumnKind.Stored)
        {
            Writer.Write(" STORED");
            return;
        }

        if (kind == SqlGeneratedColumnKind.Virtual)
        {
            Writer.Write(" VIRTUAL");
        }
    }

    internal void WriteIfExists(bool ifExists)
    {
        if (ifExists)
        {
            Writer.Write(" IF EXISTS");
        }
    }

    internal void WriteTriggerTiming(SqlTriggerTiming timing)
    {
        if (timing == SqlTriggerTiming.Before)
        {
            Writer.Write(" BEFORE");
            return;
        }

        if (timing == SqlTriggerTiming.After)
        {
            Writer.Write(" AFTER");
            return;
        }

        if (timing == SqlTriggerTiming.InsteadOf)
        {
            Writer.Write(" INSTEAD OF");
        }
    }

    internal void WriteTriggerEvent(SqlTriggerDefinition trigger)
    {
        if (trigger.Event == SqlTriggerEvent.Insert)
        {
            Writer.Write(" INSERT");
            return;
        }

        if (trigger.Event == SqlTriggerEvent.Update)
        {
            Writer.Write(" UPDATE");
            if (trigger.UpdateColumns.Length > 0)
            {
                Writer.Write(" OF ");
                WriteNameList(trigger.UpdateColumns);
            }

            return;
        }

        if (trigger.Event == SqlTriggerEvent.Delete)
        {
            Writer.Write(" DELETE");
        }
    }

    internal void WriteTriggerBody(SqlStatement[] statements)
    {
        int index = 0;
        while (index < statements.Length)
        {
            if (index > 0)
            {
                Writer.Space();
            }

            WriteStatement(statements[index]);
            Writer.Write(";");
            index = index + 1;
        }
    }

    internal void WriteListSeparator()
    {
        if (Options.CommaStyle == SqlCommaStyle.Leading)
        {
            Writer.WriteLine();
            Writer.Write(", ");
            return;
        }

        Writer.Write(", ");
    }

    internal void WritePreservedComments(SqlStatement statement)
    {
        if (!Options.PreserveComments)
        {
            return;
        }

        WritePreservedTriviaComments(statement.LeadingTrivia);
        int index = 0;
        while (index < statement.Tokens.Length)
        {
            if (IsComment(statement.Tokens[index]))
            {
                if (Writer.HasCurrentLine())
                {
                    Writer.WriteLine();
                }

                Writer.WriteRaw(TrimLineEnding(statement.Tokens[index].Text));
                Writer.WriteLine();
            }

            index = index + 1;
        }
    }

    internal void WritePreservedTriviaComments(SqlTrivia[] trivia)
    {
        if (!Options.PreserveComments)
        {
            return;
        }

        int index = 0;
        while (index < trivia.Length)
        {
            if (trivia[index].Kind == SqlTriviaKind.LineComment || trivia[index].Kind == SqlTriviaKind.BlockComment)
            {
                if (Writer.HasCurrentLine())
                {
                    Writer.WriteLine();
                }

                Writer.WriteRaw(TrimLineEnding(trivia[index].Text));
                Writer.WriteLine();
            }

            index = index + 1;
        }
    }

    internal string TrimLineEnding(string text)
    {
        int end = text.Length;
        while (end > 0 && (text[end - 1] == '\n' || text[end - 1] == '\r'))
        {
            end = end - 1;
        }

        if (end == text.Length)
        {
            return text;
        }

        return text.Substring(0, end);
    }

    internal void WritePragmaParenthesizedValue(SqlExpression value)
    {
        if (value.Kind == SqlExpressionKind.Parenthesized)
        {
            WriteExpression(value);
            return;
        }

        Writer.Write("(");
        WriteExpression(value);
        Writer.Write(")");
    }

    internal void WriteTransactionAction(SqlTransactionActionKind kind)
    {
        if (kind == SqlTransactionActionKind.Begin)
        {
            Writer.Write("BEGIN");
            return;
        }

        if (kind == SqlTransactionActionKind.Commit)
        {
            Writer.Write("COMMIT");
            return;
        }

        if (kind == SqlTransactionActionKind.End)
        {
            Writer.Write("END");
            return;
        }

        if (kind == SqlTransactionActionKind.Rollback)
        {
            Writer.Write("ROLLBACK");
        }
    }

    internal void WriteTransactionMode(SqlTransactionMode mode)
    {
        if (mode == SqlTransactionMode.Deferred)
        {
            Writer.Write(" DEFERRED");
            return;
        }

        if (mode == SqlTransactionMode.Immediate)
        {
            Writer.Write(" IMMEDIATE");
            return;
        }

        if (mode == SqlTransactionMode.Exclusive)
        {
            Writer.Write(" EXCLUSIVE");
        }
    }

    internal void WriteExpressionCore(SqlExpression expression, int precedence)
    {
        if (expression.Kind == SqlExpressionKind.Name)
        {
            WriteName(expression.Name);
            return;
        }

        if (expression.Kind == SqlExpressionKind.Literal)
        {
            WriteLiteral(expression.Literal);
            return;
        }

        if (expression.Kind == SqlExpressionKind.Parameter)
        {
            WriteExpressionValue(expression);
            return;
        }

        if (expression.Kind == SqlExpressionKind.Wildcard)
        {
            WriteWildcard(expression);
            return;
        }

        if (expression.Kind == SqlExpressionKind.Unary)
        {
            WriteUnaryExpression(expression, precedence);
            return;
        }

        if (expression.Kind == SqlExpressionKind.Binary)
        {
            WriteBinaryExpression(expression, precedence);
            return;
        }

        if (expression.Kind == SqlExpressionKind.JsonOperator)
        {
            WriteBinaryExpression(expression, precedence);
            return;
        }

        if (expression.Kind == SqlExpressionKind.FunctionCall)
        {
            WriteFunctionCall(expression);
            return;
        }

        if (expression.Kind == SqlExpressionKind.Parenthesized)
        {
            Writer.Write("(");
            WriteExpression(RequireExpression(expression.Operand, "Operand"));
            Writer.Write(")");
            return;
        }

        if (expression.Kind == SqlExpressionKind.RowValue)
        {
            WriteExpressionListInParentheses(expression.Arguments);
            return;
        }

        if (expression.Kind == SqlExpressionKind.Cast)
        {
            WriteCastExpression(expression);
            return;
        }

        if (expression.Kind == SqlExpressionKind.Extract)
        {
            WriteExtractExpression(expression);
            return;
        }

        if (expression.Kind == SqlExpressionKind.Array)
        {
            WriteArrayExpression(expression);
            return;
        }

        if (expression.Kind == SqlExpressionKind.Subscript)
        {
            WriteSubscriptExpression(expression, precedence);
            return;
        }

        if (expression.Kind == SqlExpressionKind.Case)
        {
            WriteCaseExpression(expression);
            return;
        }

        if (expression.Kind == SqlExpressionKind.Exists)
        {
            Writer.Write("EXISTS (");
            if (expression.Query != null && expression.Query.SelectCores.Length > 0)
            {
                WriteQuery(expression.Query);
            }

            Writer.Write(")");
            return;
        }

        if (expression.Kind == SqlExpressionKind.Subquery)
        {
            Writer.Write("(");
            if (expression.Query != null && expression.Query.SelectCores.Length > 0)
            {
                WriteQuery(expression.Query);
            }

            Writer.Write(")");
            return;
        }

        if (expression.Kind == SqlExpressionKind.Between)
        {
            WriteBetweenExpression(expression, precedence);
            return;
        }

        if (expression.Kind == SqlExpressionKind.In)
        {
            WriteInExpression(expression, precedence);
            return;
        }

        if (expression.Kind == SqlExpressionKind.Collate)
        {
            WriteCollateExpression(expression, precedence);
            return;
        }

        if (expression.Kind == SqlExpressionKind.Is)
        {
            WriteIsExpression(expression, precedence);
            return;
        }

        if (expression.Kind == SqlExpressionKind.Distinctness)
        {
            WriteDistinctnessExpression(expression, precedence);
            return;
        }

        if (expression.Kind == SqlExpressionKind.Like)
        {
            WriteLikeExpression(expression, precedence);
            return;
        }

        if (expression.Kind == SqlExpressionKind.Window)
        {
            WriteWindowExpression(expression, precedence);
            return;
        }

        WriteFallbackExpression(expression);
    }

    internal void WriteUnaryExpression(SqlExpression expression, int precedence)
    {
        string operatorText = GetUnaryOperatorText(expression);
        Writer.Write(operatorText);
        if (expression.UnaryOperator == SqlUnaryOperatorKind.Not)
        {
            Writer.Space();
        }

        WriteExpressionWithPrecedence(RequireExpression(expression.Operand, "Operand"), precedence, false);
    }

    internal void WriteBinaryExpression(SqlExpression expression, int precedence)
    {
        if (expression.OperatorData != null)
        {
            WriteExpressionWithPrecedence(expression.OperatorData.Left, precedence, false);
            Writer.Space();
            Writer.Write(expression.OperatorData.OperatorText);
            Writer.Space();
            WriteExpressionWithPrecedence(expression.OperatorData.Right, precedence, true);
            return;
        }

        WriteExpressionWithPrecedence(RequireExpression(expression.Left, "Left"), precedence, false);
        Writer.Space();
        Writer.Write(GetBinaryOperatorText(expression));
        Writer.Space();
        WriteExpressionWithPrecedence(RequireExpression(expression.Right, "Right"), precedence, true);
    }

    internal void WriteFunctionCall(SqlExpression expression)
    {
        if (expression.FunctionData != null)
        {
            WriteFunctionInvocationCore(expression.FunctionData);
            WriteFunctionInvocationWindow(expression.FunctionData);
            return;
        }

        WriteName(expression.Name);
        Writer.Write("(");
        if (expression.IsDistinct)
        {
            Writer.Write("DISTINCT");
            if (expression.Arguments.Length > 0)
            {
                Writer.Space();
            }
        }

        WriteExpressionList(expression.Arguments);
        if (expression.OrderBy.Length > 0)
        {
            if (expression.Arguments.Length > 0 || expression.IsDistinct)
            {
                Writer.Space();
            }

            Writer.Write("ORDER BY ");
            WriteOrderingTerms(expression.OrderBy);
        }

        Writer.Write(")");
        if (expression.Filter != null)
        {
            Writer.Write(" FILTER (WHERE ");
            WriteExpression(expression.Filter);
            Writer.Write(")");
        }
    }

    internal void WriteFunctionInvocationCore(SqlFunctionInvocationData invocation)
    {
        WriteName(invocation.Name);
        Writer.Write("(");
        if (invocation.IsDistinct)
        {
            Writer.Write("DISTINCT");
            if (invocation.Arguments.Length > 0)
            {
                Writer.Space();
            }
        }

        WriteExpressionList(invocation.Arguments);
        if (invocation.OrderBy.Length > 0)
        {
            if (invocation.Arguments.Length > 0 || invocation.IsDistinct)
            {
                Writer.Space();
            }

            Writer.Write("ORDER BY ");
            WriteOrderingTerms(invocation.OrderBy);
        }

        Writer.Write(")");
        if (invocation.Filter != null)
        {
            Writer.Write(" FILTER (WHERE ");
            WriteExpression(invocation.Filter);
            Writer.Write(")");
        }
    }

    internal void WriteFunctionInvocationWindow(SqlFunctionInvocationData invocation)
    {
        if (invocation.WindowDefinition != null)
        {
            Writer.Write(" OVER ");
            WriteWindowDefinition(invocation.WindowDefinition);
        }
        else if (invocation.WindowName != null)
        {
            Writer.Write(" OVER ");
            WriteName(invocation.WindowName);
        }
    }

    internal void WriteCastExpression(SqlExpression expression)
    {
        if (expression.CastData != null && expression.CastData.SyntaxKind == SqlCastSyntaxKind.PostgreSql && (!MapDialectSyntax || SupportsPostgreSqlCastSyntax()))
        {
            WriteExpressionWithPrecedence(expression.CastData.Operand, 95, false);
            Writer.Write("::");
            Writer.Write(expression.CastData.TypeName);
            return;
        }

        if (expression.CastData != null)
        {
            Writer.Write("CAST(");
            WriteExpression(expression.CastData.Operand);
            Writer.Write(" AS ");
            Writer.Write(expression.CastData.TypeName);
            Writer.Write(")");
            return;
        }

        Writer.Write("CAST(");
        WriteExpression(RequireExpression(expression.Operand, "Operand"));
        Writer.Write(" AS ");
        string? typeName = expression.TypeName;
        if (typeName != null)
        {
            Writer.Write(typeName);
        }

        Writer.Write(")");
    }

    internal bool SupportsPostgreSqlCastSyntax()
    {
        return WriteDialect.Kind == SqlDialectKind.Generic || WriteDialect.Kind == SqlDialectKind.PostgreSql || WriteDialect.Kind == SqlDialectKind.DuckDb;
    }

    internal void WriteExtractExpression(SqlExpression expression)
    {
        if (expression.ExtractData == null)
        {
            WriteFallbackExpression(expression);
            return;
        }

        Writer.Write("EXTRACT(");
        Writer.Write(expression.ExtractData.Field);
        Writer.Write(" FROM ");
        WriteExpression(expression.ExtractData.Source);
        Writer.Write(")");
    }

    internal void WriteArrayExpression(SqlExpression expression)
    {
        Writer.Write("ARRAY");
        if (expression.ArrayData != null && expression.ArrayData.Query != null)
        {
            Writer.Write("(");
            WriteQuery(expression.ArrayData.Query);
            Writer.Write(")");
            return;
        }

        Writer.Write("[");
        if (expression.ArrayData != null)
        {
            WriteExpressionList(expression.ArrayData.Elements);
        }

        Writer.Write("]");
    }

    internal void WriteSubscriptExpression(SqlExpression expression, int precedence)
    {
        if (expression.SubscriptData == null)
        {
            WriteFallbackExpression(expression);
            return;
        }

        WriteExpressionWithPrecedence(expression.SubscriptData.Target, precedence, false);
        Writer.Write("[");
        if (expression.SubscriptData.LowerBound != null)
        {
            WriteExpression(expression.SubscriptData.LowerBound);
        }

        if (expression.SubscriptData.IsSlice)
        {
            Writer.Write(":");
            if (expression.SubscriptData.UpperBound != null)
            {
                WriteExpression(expression.SubscriptData.UpperBound);
            }
        }

        Writer.Write("]");
    }

    internal void WriteCaseExpression(SqlExpression expression)
    {
        Writer.Write("CASE");
        if (expression.Operand != null)
        {
            Writer.Space();
            WriteExpression(expression.Operand);
        }

        int index = 0;
        while (index + 1 < expression.Arguments.Length)
        {
            Writer.Write(" WHEN ");
            WriteExpression(expression.Arguments[index]);
            Writer.Write(" THEN ");
            WriteExpression(expression.Arguments[index + 1]);
            index = index + 2;
        }

        if (expression.Alternate != null)
        {
            Writer.Write(" ELSE ");
            WriteExpression(expression.Alternate);
        }

        Writer.Write(" END");
    }

    internal void WriteBetweenExpression(SqlExpression expression, int precedence)
    {
        WriteExpressionWithPrecedence(RequireExpression(expression.Operand, "Operand"), precedence, false);
        Writer.Space();
        if (IsNegated(expression))
        {
            Writer.Write("NOT ");
        }

        Writer.Write("BETWEEN ");
        WriteExpressionWithPrecedence(RequireExpression(expression.Left, "Left"), precedence, false);
        Writer.Write(" AND ");
        WriteExpressionWithPrecedence(RequireExpression(expression.Right, "Right"), precedence, true);
    }

    internal void WriteInExpression(SqlExpression expression, int precedence)
    {
        SqlExpression? operandValue = expression.Operand;
        if (expression.InData != null)
        {
            operandValue = expression.InData.Operand;
        }

        SqlExpression operand = RequireExpression(operandValue, "Operand");
        WriteExpressionWithPrecedence(operand, precedence, false);
        Writer.Space();
        bool negated = IsNegated(expression);
        if (expression.InData != null)
        {
            negated = expression.InData.IsNegated;
        }

        if (negated)
        {
            Writer.Write("NOT ");
        }

        Writer.Write("IN ");
        if (expression.InData != null && (expression.InData.TargetKind == SqlInTargetKind.Table || expression.InData.TargetKind == SqlInTargetKind.Function))
        {
            WriteName(expression.InData.TargetName);
            if (expression.InData.TargetKind == SqlInTargetKind.Function)
            {
                Writer.Write("(");
                WriteExpressionList(expression.InData.FunctionArguments);
                Writer.Write(")");
            }

            return;
        }

        Writer.Write("(");
        if (expression.InData != null && expression.InData.TargetKind == SqlInTargetKind.Subquery && expression.InData.Query != null)
        {
            WriteQuery(expression.InData.Query);
        }
        else if (expression.InData != null && expression.InData.TargetKind == SqlInTargetKind.List)
        {
            WriteExpressionList(expression.InData.Values);
        }
        else if (expression.Query != null && expression.Query.SelectCores.Length > 0)
        {
            WriteQuery(expression.Query);
        }
        else
        {
            WriteExpressionList(expression.Arguments);
        }

        Writer.Write(")");
    }

    internal void WriteDistinctnessExpression(SqlExpression expression, int precedence)
    {
        if (expression.DistinctnessData == null)
        {
            WriteFallbackExpression(expression);
            return;
        }

        WriteExpressionWithPrecedence(expression.DistinctnessData.Left, precedence, false);
        if (expression.DistinctnessData.Kind == SqlDistinctnessKind.IsNotDistinctFrom)
        {
            Writer.Write(" IS NOT DISTINCT FROM ");
        }
        else
        {
            Writer.Write(" IS DISTINCT FROM ");
        }

        WriteExpressionWithPrecedence(expression.DistinctnessData.Right, precedence, true);
    }

    internal void WriteCollateExpression(SqlExpression expression, int precedence)
    {
        if (expression.CollateData != null)
        {
            WriteExpressionWithPrecedence(expression.CollateData.Operand, precedence, false);
            Writer.Write(" COLLATE ");
            WriteName(expression.CollateData.Collation);
            return;
        }

        WriteExpressionWithPrecedence(RequireExpression(expression.Operand, "Operand"), precedence, false);
        Writer.Write(" COLLATE ");
        WriteName(expression.Name);
    }

    internal void WriteIsExpression(SqlExpression expression, int precedence)
    {
        WriteExpressionWithPrecedence(RequireExpression(expression.Left, "Left"), precedence, false);
        Writer.Space();
        if (IsNegated(expression))
        {
            Writer.Write("IS NOT");
        }
        else
        {
            Writer.Write("IS");
        }

        Writer.Space();
        WriteExpressionWithPrecedence(RequireExpression(expression.Right, "Right"), precedence, true);
    }

    internal void WriteLikeExpression(SqlExpression expression, int precedence)
    {
        if (expression.PatternData != null)
        {
            WriteExpressionWithPrecedence(expression.PatternData.Value, precedence, false);
            Writer.Space();
            if (expression.PatternData.IsNegated)
            {
                Writer.Write("NOT ");
            }

            Writer.Write(expression.PatternData.OperatorText.ToUpperInvariant());
            Writer.Space();
            WriteExpressionWithPrecedence(expression.PatternData.Pattern, precedence, true);
            if (expression.PatternData.Escape != null)
            {
                Writer.Write(" ESCAPE ");
                WriteExpression(expression.PatternData.Escape);
            }

            return;
        }

        WriteExpressionWithPrecedence(RequireExpression(expression.Left, "Left"), precedence, false);
        Writer.Space();
        if (IsNegated(expression))
        {
            Writer.Write("NOT ");
        }

        Writer.Write(GetLikeOperatorText(expression));
        Writer.Space();
        WriteExpressionWithPrecedence(RequireExpression(expression.Right, "Right"), precedence, true);
        if (expression.Alternate != null)
        {
            Writer.Write(" ESCAPE ");
            WriteExpression(expression.Alternate);
        }
    }

    internal void WriteWindowExpression(SqlExpression expression, int precedence)
    {
        if (expression.FunctionData != null)
        {
            WriteFunctionInvocationCore(expression.FunctionData);
            if (expression.WindowDefinition != null)
            {
                Writer.Write(" OVER ");
                WriteWindowDefinition(expression.WindowDefinition);
            }
            else
            {
                WriteFunctionInvocationWindow(expression.FunctionData);
            }

            return;
        }

        WriteExpressionWithPrecedence(RequireExpression(expression.Operand, "Operand"), precedence, false);
        Writer.Write(" OVER ");
        WriteWindowDefinition(RequireWindowDefinition(expression.WindowDefinition));
    }

    internal void WriteWindowDefinition(SqlWindowDefinition windowDefinition)
    {
        if (windowDefinition.BaseWindowName != null && windowDefinition.PartitionBy.Length == 0 && windowDefinition.OrderBy.Length == 0 && windowDefinition.Frame == null)
        {
            WriteName(windowDefinition.BaseWindowName);
            return;
        }

        Writer.Write("(");
        bool needsSpace = false;
        if (windowDefinition.BaseWindowName != null)
        {
            WriteName(windowDefinition.BaseWindowName);
            needsSpace = true;
        }

        if (windowDefinition.PartitionBy.Length > 0)
        {
            if (needsSpace)
            {
                Writer.Space();
            }

            Writer.Write("PARTITION BY ");
            WriteExpressionList(windowDefinition.PartitionBy);
            needsSpace = true;
        }

        if (windowDefinition.OrderBy.Length > 0)
        {
            if (needsSpace)
            {
                Writer.Space();
            }

            Writer.Write("ORDER BY ");
            WriteOrderingTerms(windowDefinition.OrderBy);
            needsSpace = true;
        }

        if (windowDefinition.Frame != null)
        {
            if (needsSpace)
            {
                Writer.Space();
            }

            WriteWindowFrame(windowDefinition.Frame);
        }

        Writer.Write(")");
    }

    internal void WriteWindowFrame(SqlWindowFrame frame)
    {
        Writer.Write(GetWindowFrameKindText(frame.Kind));
        Writer.Space();
        if (frame.End != null)
        {
            Writer.Write("BETWEEN ");
            WriteWindowFrameBoundary(frame.Start);
            Writer.Write(" AND ");
            WriteWindowFrameBoundary(frame.End);
        }
        else
        {
            WriteWindowFrameBoundary(frame.Start);
        }

        if (frame.Exclusion != SqlWindowFrameExclusionKind.None)
        {
            Writer.Write(" EXCLUDE ");
            Writer.Write(GetWindowFrameExclusionText(frame.Exclusion));
        }
    }

    internal void WriteWindowFrameBoundary(SqlWindowFrameBoundary boundary)
    {
        if (boundary.Kind == SqlWindowFrameBoundaryKind.UnboundedPreceding)
        {
            Writer.Write("UNBOUNDED PRECEDING");
            return;
        }

        if (boundary.Kind == SqlWindowFrameBoundaryKind.UnboundedFollowing)
        {
            Writer.Write("UNBOUNDED FOLLOWING");
            return;
        }

        if (boundary.Kind == SqlWindowFrameBoundaryKind.CurrentRow)
        {
            Writer.Write("CURRENT ROW");
            return;
        }

        if (boundary.Kind == SqlWindowFrameBoundaryKind.Preceding)
        {
            WriteExpression(RequireExpression(boundary.Expression, "Expression"));
            Writer.Write(" PRECEDING");
            return;
        }

        if (boundary.Kind == SqlWindowFrameBoundaryKind.Following)
        {
            WriteExpression(RequireExpression(boundary.Expression, "Expression"));
            Writer.Write(" FOLLOWING");
            return;
        }
    }

    internal void WriteOrderingTerms(SqlOrderingTerm[] terms)
    {
        int index = 0;
        while (index < terms.Length)
        {
            if (index > 0)
            {
                WriteListSeparator();
            }

            WriteOrderingTerm(terms[index]);
            index = index + 1;
        }
    }

    internal void WriteOrderingTerm(SqlOrderingTerm term)
    {
        WriteExpression(term.Expression);
        if (term.Collation != null)
        {
            Writer.Write(" COLLATE ");
            WriteName(term.Collation);
        }

        if (term.Direction == SqlOrderingDirection.Asc)
        {
            Writer.Write(" ASC");
        }
        else
        {
            if (term.Direction == SqlOrderingDirection.Desc)
            {
                Writer.Write(" DESC");
            }
        }

        if (term.NullOrdering == SqlNullOrdering.First)
        {
            Writer.Write(" NULLS FIRST");
        }
        else
        {
            if (term.NullOrdering == SqlNullOrdering.Last)
            {
                Writer.Write(" NULLS LAST");
            }
        }
    }

    internal void WriteExpressionListInParentheses(SqlExpression[] expressions)
    {
        Writer.Write("(");
        WriteExpressionList(expressions);
        Writer.Write(")");
    }

    internal void WriteExpressionList(SqlExpression[] expressions)
    {
        int index = 0;
        while (index < expressions.Length)
        {
            if (index > 0)
            {
                WriteListSeparator();
            }

            WriteExpression(expressions[index]);
            index = index + 1;
        }
    }

    internal void WriteWildcard(SqlExpression expression)
    {
        SqlName? qualifier = expression.Name;
        if (expression.WildcardData != null)
        {
            qualifier = expression.WildcardData.Qualifier;
        }

        if (qualifier != null)
        {
            WriteName(qualifier);
            Writer.Write(".");
        }

        Writer.Write("*");
    }

    internal void WriteName(SqlName? name)
    {
        if (name == null)
        {
            return;
        }

        int index = 0;
        while (index < name.Parts.Length)
        {
            if (index > 0)
            {
                Writer.WriteRaw(".");
            }

            Writer.WriteRaw(GetFormattedNamePart(name.Parts[index]));
            index = index + 1;
        }
    }

    internal string GetFormattedNamePart(SqlNamePart part)
    {
        if (part.QuoteStyle != SqlNameQuoteStyle.None && IsNameQuoteSupported(part.QuoteStyle))
        {
            return part.SourceText;
        }

        string text = part.Text;
        if (part.QuoteStyle == SqlNameQuoteStyle.None)
        {
            text = ApplyIdentifierCase(text);
        }
        if (part.QuoteStyle != SqlNameQuoteStyle.None || !IsBareIdentifierText(text))
        {
            SqlNameQuoteStyle quoteStyle = GetPreferredNameQuoteStyle();
            if (quoteStyle != SqlNameQuoteStyle.None)
            {
                return QuoteIdentifierText(text, quoteStyle);
            }
        }

        return text;
    }

    internal string ApplyIdentifierCase(string text)
    {
        if (Options.IdentifierCase == SqlIdentifierCase.Upper)
        {
            return text.ToUpperInvariant();
        }

        if (Options.IdentifierCase == SqlIdentifierCase.Lower)
        {
            return text.ToLowerInvariant();
        }

        return text;
    }

    internal bool IsNameQuoteSupported(SqlNameQuoteStyle style)
    {
        if (style == SqlNameQuoteStyle.DoubleQuote)
        {
            return WriteDialect.AllowDoubleQuotedIdentifiers;
        }

        if (style == SqlNameQuoteStyle.Bracket)
        {
            return WriteDialect.AllowBracketQuotedIdentifiers;
        }

        if (style == SqlNameQuoteStyle.Backtick)
        {
            return WriteDialect.AllowBacktickQuotedIdentifiers;
        }

        return false;
    }

    internal SqlNameQuoteStyle GetPreferredNameQuoteStyle()
    {
        if (WriteDialect.AllowDoubleQuotedIdentifiers)
        {
            return SqlNameQuoteStyle.DoubleQuote;
        }

        if (WriteDialect.AllowBacktickQuotedIdentifiers)
        {
            return SqlNameQuoteStyle.Backtick;
        }

        if (WriteDialect.AllowBracketQuotedIdentifiers)
        {
            return SqlNameQuoteStyle.Bracket;
        }

        return SqlNameQuoteStyle.None;
    }

    internal string QuoteIdentifierText(string text, SqlNameQuoteStyle style)
    {
        if (style == SqlNameQuoteStyle.DoubleQuote)
        {
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }

        if (style == SqlNameQuoteStyle.Backtick)
        {
            return "`" + text.Replace("`", "``") + "`";
        }

        if (style == SqlNameQuoteStyle.Bracket)
        {
            return "[" + text.Replace("]", "]]" ) + "]";
        }

        return text;
    }

    internal bool IsBareIdentifierText(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        char first = text[0];
        if (!char.IsLetter(first) && first != '_')
        {
            return false;
        }

        int index = 1;
        while (index < text.Length)
        {
            char current = text[index];
            if (!char.IsLetterOrDigit(current) && current != '_' && !(current == '$' && WriteDialect.AllowDollarInBareIdentifiers))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal void WriteSelectTrailingComma()
    {
        if (Options.TrailingCommaPolicy != SqlTrailingCommaPolicy.WhenSupported)
        {
            return;
        }

        SqlDialectKind kind = WriteDialect.Kind;
        if (kind == SqlDialectKind.BigQuery || kind == SqlDialectKind.DuckDb || kind == SqlDialectKind.Snowflake)
        {
            Writer.WriteRaw(",");
        }
    }

    internal void WriteNameList(SqlName[] names)
    {
        int index = 0;
        while (index < names.Length)
        {
            if (index > 0)
            {
                WriteListSeparator();
            }

            WriteName(names[index]);
            index = index + 1;
        }
    }

    internal void WriteLiteral(SqlLiteral? literal)
    {
        if (literal == null)
        {
            return;
        }

        if (literal.Kind == SqlLiteralKind.Null)
        {
            Writer.Write("NULL");
            return;
        }

        if (literal.Kind == SqlLiteralKind.Boolean)
        {
            Writer.Write(literal.SourceText.ToUpperInvariant());
            return;
        }

        if (literal.Kind == SqlLiteralKind.CurrentDate)
        {
            Writer.Write("CURRENT_DATE");
            return;
        }

        if (literal.Kind == SqlLiteralKind.CurrentTime)
        {
            Writer.Write("CURRENT_TIME");
            return;
        }

        if (literal.Kind == SqlLiteralKind.CurrentTimestamp)
        {
            Writer.Write("CURRENT_TIMESTAMP");
            return;
        }

        Writer.Write(literal.SourceText);
    }

    internal void WriteExpressionValue(SqlExpression expression)
    {
        string? text = expression.Value;
        if (text != null)
        {
            Writer.Write(text);
        }
    }

    internal void WriteFallbackExpression(SqlExpression expression)
    {
        if (expression.Tokens.Length > 0)
        {
            WriteTokens(expression.Tokens);
            return;
        }

        WriteExpressionValue(expression);
    }

    internal void WriteTokens(SqlToken[] tokens)
    {
        int index = 0;
        while (index < tokens.Length)
        {
            if (!IsTrivia(tokens[index]))
            {
                Writer.Write(tokens[index].Text);
            }

            index = index + 1;
        }
    }

    internal bool ShouldParenthesize(SqlExpression expression, int precedence, int parentPrecedence, bool isRightChild)
    {
        if (parentPrecedence == 0)
        {
            return false;
        }

        if (precedence < parentPrecedence)
        {
            return true;
        }

        if (isRightChild && precedence == parentPrecedence && (expression.Kind == SqlExpressionKind.Binary || expression.Kind == SqlExpressionKind.JsonOperator))
        {
            return true;
        }

        return false;
    }

    internal bool IsNegated(SqlExpression expression)
    {
        if (expression.OperatorText != null && expression.OperatorText.ToUpperInvariant() == "NOTNULL")
        {
            return true;
        }

        int index = 0;
        while (index < expression.Tokens.Length)
        {
            if (IsKeywordText(expression.Tokens[index].Text, "NOT"))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal string GetUnaryOperatorText(SqlExpression expression)
    {
        if (expression.UnaryOperator == SqlUnaryOperatorKind.Positive)
        {
            return "+";
        }

        if (expression.UnaryOperator == SqlUnaryOperatorKind.Negative)
        {
            return "-";
        }

        if (expression.UnaryOperator == SqlUnaryOperatorKind.BitwiseNot)
        {
            return "~";
        }

        if (expression.UnaryOperator == SqlUnaryOperatorKind.Not)
        {
            return "NOT";
        }

        if (expression.OperatorText != null)
        {
            return expression.OperatorText;
        }

        return "";
    }

    internal string GetBinaryOperatorText(SqlExpression expression)
    {
        if (expression.BinaryOperator == SqlBinaryOperatorKind.Add)
        {
            return "+";
        }

        if (expression.BinaryOperator == SqlBinaryOperatorKind.Subtract)
        {
            return "-";
        }

        if (expression.BinaryOperator == SqlBinaryOperatorKind.Multiply)
        {
            return "*";
        }

        if (expression.BinaryOperator == SqlBinaryOperatorKind.Divide)
        {
            return "/";
        }

        if (expression.BinaryOperator == SqlBinaryOperatorKind.Modulo)
        {
            return "%";
        }

        if (expression.BinaryOperator == SqlBinaryOperatorKind.Concat)
        {
            return "||";
        }

        if (expression.BinaryOperator == SqlBinaryOperatorKind.Equal)
        {
            return "=";
        }

        if (expression.BinaryOperator == SqlBinaryOperatorKind.NotEqual)
        {
            return "<>";
        }

        if (expression.BinaryOperator == SqlBinaryOperatorKind.Less)
        {
            return "<";
        }

        if (expression.BinaryOperator == SqlBinaryOperatorKind.LessOrEqual)
        {
            return "<=";
        }

        if (expression.BinaryOperator == SqlBinaryOperatorKind.Greater)
        {
            return ">";
        }

        if (expression.BinaryOperator == SqlBinaryOperatorKind.GreaterOrEqual)
        {
            return ">=";
        }

        if (expression.BinaryOperator == SqlBinaryOperatorKind.BitwiseAnd)
        {
            return "&";
        }

        if (expression.BinaryOperator == SqlBinaryOperatorKind.BitwiseOr)
        {
            return "|";
        }

        if (expression.BinaryOperator == SqlBinaryOperatorKind.BitwiseXor)
        {
            return "^";
        }

        if (expression.BinaryOperator == SqlBinaryOperatorKind.ShiftLeft)
        {
            return "<<";
        }

        if (expression.BinaryOperator == SqlBinaryOperatorKind.ShiftRight)
        {
            return ">>";
        }

        if (expression.BinaryOperator == SqlBinaryOperatorKind.And)
        {
            return "AND";
        }

        if (expression.BinaryOperator == SqlBinaryOperatorKind.Or)
        {
            return "OR";
        }

        if (expression.OperatorText != null)
        {
            return expression.OperatorText;
        }

        return "";
    }

    internal string GetLikeOperatorText(SqlExpression expression)
    {
        if (expression.OperatorToken != null)
        {
            return GetKeywordOperatorText(expression.OperatorToken.Text);
        }

        if (expression.OperatorText != null)
        {
            return expression.OperatorText;
        }

        return "LIKE";
    }

    internal string GetKeywordOperatorText(string text)
    {
        if (IsKeywordText(text, "GLOB"))
        {
            return "GLOB";
        }

        if (IsKeywordText(text, "REGEXP"))
        {
            return "REGEXP";
        }

        if (IsKeywordText(text, "MATCH"))
        {
            return "MATCH";
        }

        return "LIKE";
    }

    internal string GetWindowFrameKindText(SqlWindowFrameKind kind)
    {
        if (kind == SqlWindowFrameKind.Rows)
        {
            return "ROWS";
        }

        if (kind == SqlWindowFrameKind.Range)
        {
            return "RANGE";
        }

        if (kind == SqlWindowFrameKind.Groups)
        {
            return "GROUPS";
        }

        return "";
    }

    internal string GetWindowFrameExclusionText(SqlWindowFrameExclusionKind kind)
    {
        if (kind == SqlWindowFrameExclusionKind.CurrentRow)
        {
            return "CURRENT ROW";
        }

        if (kind == SqlWindowFrameExclusionKind.Group)
        {
            return "GROUP";
        }

        if (kind == SqlWindowFrameExclusionKind.Ties)
        {
            return "TIES";
        }

        if (kind == SqlWindowFrameExclusionKind.NoOthers)
        {
            return "NO OTHERS";
        }

        return "";
    }

    internal string GetCompoundOperatorText(SqlCompoundOperatorKind kind)
    {
        if (kind == SqlCompoundOperatorKind.Union)
        {
            return "UNION";
        }

        if (kind == SqlCompoundOperatorKind.UnionAll)
        {
            return "UNION ALL";
        }

        if (kind == SqlCompoundOperatorKind.Intersect)
        {
            return "INTERSECT";
        }

        if (kind == SqlCompoundOperatorKind.Except)
        {
            return "EXCEPT";
        }

        return "";
    }

    internal string GetConflictActionText(SqlConflictActionKind action)
    {
        if (action == SqlConflictActionKind.Rollback)
        {
            return "ROLLBACK";
        }

        if (action == SqlConflictActionKind.Abort)
        {
            return "ABORT";
        }

        if (action == SqlConflictActionKind.Fail)
        {
            return "FAIL";
        }

        if (action == SqlConflictActionKind.Ignore)
        {
            return "IGNORE";
        }

        if (action == SqlConflictActionKind.Replace)
        {
            return "REPLACE";
        }

        return "";
    }

    internal SqlExpression RequireExpression(SqlExpression? expression, string parameterName)
    {
        if (expression == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        return expression;
    }

    internal SqlWindowDefinition RequireWindowDefinition(SqlWindowDefinition? windowDefinition)
    {
        if (windowDefinition == null)
        {
            throw new ArgumentNullException("WindowDefinition");
        }

        return windowDefinition;
    }

    internal SqlQuery RequireQuery(SqlQuery? query)
    {
        if (query == null)
        {
            throw new ArgumentNullException("Query");
        }

        return query;
    }

    internal SqlTableReference RequireTableReference(SqlTableReference? table)
    {
        if (table == null)
        {
            throw new ArgumentNullException("TableReference");
        }

        return table;
    }

    internal SqlInsertStatement RequireInsertStatement(SqlInsertStatement? target)
    {
        if (target == null)
        {
            throw new ArgumentNullException("Target");
        }

        return target;
    }

    internal SqlUpdateStatement RequireUpdateStatement(SqlUpdateStatement? target)
    {
        if (target == null)
        {
            throw new ArgumentNullException("Target");
        }

        return target;
    }

    internal SqlDeleteStatement RequireDeleteStatement(SqlDeleteStatement? target)
    {
        if (target == null)
        {
            throw new ArgumentNullException("Target");
        }

        return target;
    }

    internal SqlMergeStatement RequireMergeStatement(SqlMergeStatement? target)
    {
        if (target == null)
        {
            throw new ArgumentNullException("Target");
        }

        return target;
    }

    internal SqlTableDefinition RequireTableDefinition(SqlTableDefinition? schema)
    {
        if (schema == null)
        {
            throw new ArgumentNullException("Schema");
        }

        return schema;
    }

    internal SqlAlterTableDefinition RequireAlterTableDefinition(SqlAlterTableDefinition? schema)
    {
        if (schema == null)
        {
            throw new ArgumentNullException("Schema");
        }

        return schema;
    }

    internal SqlDropTableDefinition RequireDropTableDefinition(SqlDropTableDefinition? schema)
    {
        if (schema == null)
        {
            throw new ArgumentNullException("Schema");
        }

        return schema;
    }

    internal SqlIndexDefinition RequireIndexDefinition(SqlIndexDefinition? schema)
    {
        if (schema == null)
        {
            throw new ArgumentNullException("Schema");
        }

        return schema;
    }

    internal SqlDropIndexDefinition RequireDropIndexDefinition(SqlDropIndexDefinition? schema)
    {
        if (schema == null)
        {
            throw new ArgumentNullException("Schema");
        }

        return schema;
    }

    internal SqlViewDefinition RequireViewDefinition(SqlViewDefinition? schema)
    {
        if (schema == null)
        {
            throw new ArgumentNullException("Schema");
        }

        return schema;
    }

    internal SqlDropViewDefinition RequireDropViewDefinition(SqlDropViewDefinition? schema)
    {
        if (schema == null)
        {
            throw new ArgumentNullException("Schema");
        }

        return schema;
    }

    internal SqlTriggerDefinition RequireTriggerDefinition(SqlTriggerDefinition? schema)
    {
        if (schema == null)
        {
            throw new ArgumentNullException("Schema");
        }

        return schema;
    }

    internal SqlDropTriggerDefinition RequireDropTriggerDefinition(SqlDropTriggerDefinition? schema)
    {
        if (schema == null)
        {
            throw new ArgumentNullException("Schema");
        }

        return schema;
    }

    internal SqlVirtualTableDefinition RequireVirtualTableDefinition(SqlVirtualTableDefinition? schema)
    {
        if (schema == null)
        {
            throw new ArgumentNullException("Schema");
        }

        return schema;
    }

    internal SqlColumnDefinition RequireColumnDefinition(SqlColumnDefinition? column)
    {
        if (column == null)
        {
            throw new ArgumentNullException("ColumnDefinition");
        }

        return column;
    }

    internal SqlPragmaStatement RequirePragmaStatement(SqlPragmaStatement? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("Expression");
        }

        return value;
    }

    internal SqlAnalyzeStatement RequireAnalyzeStatement(SqlAnalyzeStatement? target)
    {
        if (target == null)
        {
            throw new ArgumentNullException("Target");
        }

        return target;
    }

    internal SqlVacuumStatement RequireVacuumStatement(SqlVacuumStatement? target)
    {
        if (target == null)
        {
            throw new ArgumentNullException("Target");
        }

        return target;
    }

    internal SqlAttachStatement RequireAttachStatement(SqlAttachStatement? target)
    {
        if (target == null)
        {
            throw new ArgumentNullException("Target");
        }

        return target;
    }

    internal SqlDetachStatement RequireDetachStatement(SqlDetachStatement? target)
    {
        if (target == null)
        {
            throw new ArgumentNullException("Target");
        }

        return target;
    }

    internal SqlReindexStatement RequireReindexStatement(SqlReindexStatement? target)
    {
        if (target == null)
        {
            throw new ArgumentNullException("Target");
        }

        return target;
    }

    internal SqlTransactionStatement RequireTransactionStatement(SqlTransactionStatement? target)
    {
        if (target == null)
        {
            throw new ArgumentNullException("Target");
        }

        return target;
    }

    internal SqlSavepointStatement RequireSavepointStatement(SqlSavepointStatement? target)
    {
        if (target == null)
        {
            throw new ArgumentNullException("Target");
        }

        return target;
    }

    internal SqlReleaseStatement RequireReleaseStatement(SqlReleaseStatement? target)
    {
        if (target == null)
        {
            throw new ArgumentNullException("Target");
        }

        return target;
    }

    internal SqlExplainStatement RequireExplainStatement(SqlExplainStatement? target)
    {
        if (target == null)
        {
            throw new ArgumentNullException("Target");
        }

        return target;
    }

    internal bool IsKeywordText(string text, string keyword)
    {
        return string.Equals(text, keyword, StringComparison.OrdinalIgnoreCase);
    }

    internal bool IsTrivia(SqlToken token)
    {
        SqlTokenKind kind = token.Kind;
        return kind == SqlTokenKind.Whitespace || kind == SqlTokenKind.LineComment || kind == SqlTokenKind.BlockComment;
    }

    internal bool IsComment(SqlToken token)
    {
        SqlTokenKind kind = token.Kind;
        return kind == SqlTokenKind.LineComment || kind == SqlTokenKind.BlockComment;
    }
}
