using System;
using System.Collections.Generic;

namespace sql;

internal class SqlStatementParser
{
    internal SqlParserContext Context;

    internal SqlStatementParser(SqlParserContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        Context = context;
    }

    internal static SqlParseResult ParseScript(string text)
    {
        return ParseScript(text, new SqlParseOptions());
    }

    internal static SqlParseResult ParseScript(string text, SqlParseOptions options)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.Validate();
        SqlSourceText sourceText = new SqlSourceText(text);
        SqlLexer lexer = new SqlLexer(sourceText, options.Dialect);
        SqlToken[] tokens = lexer.LexTokens();
        SqlDiagnosticBag diagnostics = new SqlDiagnosticBag();
        diagnostics.Merge(lexer.Diagnostics);
        SqlTokenStream stream = SqlTokenStream.Create(tokens, diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, options, sourceText);
        SqlStatementParser parser = new SqlStatementParser(context);
        SqlScript script = parser.ParseScript();

        return SqlParseResult.Create(script, tokens, diagnostics, sourceText, options);
    }

    internal SqlParseResult ParseResult()
    {
        SqlScript script = ParseScript();
        return SqlParseResult.Create(script, Context.Stream.Tokens, Context.Diagnostics, Context.SourceText, Context.Options);
    }

    internal SqlScript ParseScript()
    {
        List<SqlStatement> statements = new List<SqlStatement>();

        while (!Context.IsAtEnd())
        {
            SkipTrivia();
            if (Context.IsAtEnd())
            {
                break;
            }

            if (Context.Peek().Kind == SqlTokenKind.Semicolon)
            {
                AddEmptyStatement(statements);
            }
            else
            {
                AddStatement(statements, ParseNonEmptyStatement());
            }
        }

        SqlScript script = new SqlScript(statements.ToArray(), GetScriptSpan());
        PopulateScriptFidelity(script);
        return script;
    }

    internal SqlStatement ParseStatement()
    {
        SkipTrivia();
        if (Context.IsAtEnd())
        {
            SqlToken token = Context.Peek();
            return new SqlStatement(SqlStatementKind.Empty, token.Span, new SqlToken[0]);
        }

        if (Context.Peek().Kind == SqlTokenKind.Semicolon)
        {
            SqlToken semicolon = Context.Consume();
            return new SqlStatement(SqlStatementKind.Empty, semicolon.Span, new SqlToken[] { semicolon });
        }

        return ParseNonEmptyStatement();
    }

    internal void AddEmptyStatement(List<SqlStatement> statements)
    {
        SqlStatement statement = ParseStatement();
        if (!Context.Options.AllowEmptyStatements)
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLPARSE002", "Empty statements are not allowed.", statement.Span);
        }

        AddStatement(statements, statement);
    }

    internal void AddStatement(List<SqlStatement> statements, SqlStatement statement)
    {
        if (!Context.Options.AllowMultipleStatements && statements.Count > 0)
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLPARSE001", "Multiple statements are not allowed.", statement.Span);
        }

        statements.Add(statement);
    }

    internal SqlStatement ParseNonEmptyStatement()
    {
        List<SqlToken> tokens = new List<SqlToken>();
        SqlToken first = Context.Peek();
        SqlToken last = first;

        if (IsCreateTriggerStart())
        {
            CollectCreateTriggerTokens(tokens);
            last = tokens[tokens.Count - 1];
        }
        else
        {
            while (!Context.IsAtEnd() && Context.Peek().Kind != SqlTokenKind.Semicolon)
            {
                last = Context.Consume();
                tokens.Add(last);
            }

            if (Context.Peek().Kind == SqlTokenKind.Semicolon)
            {
                last = Context.Consume();
                tokens.Add(last);
            }
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(first.Span.Start, last.Span.GetEnd());
        SqlStatementKind kind = ClassifyStatement(tokens);
        if (kind != SqlStatementKind.Unknown && !Context.Options.Dialect.SupportsStatement(kind))
        {
            Context.AddUnsupportedFeature(kind.ToString() + " statement", span);
        }

        SqlStatement statement = new SqlStatement(kind, span, tokens.ToArray());
        if (kind == SqlStatementKind.Merge)
        {
            statement.Merge = ParseMergeStatement(tokens);
            return statement;
        }

        if (kind == SqlStatementKind.Select)
        {
            statement.Query = ParseSelectQuery(tokens);
        }
        else
        {
            if (kind == SqlStatementKind.Explain)
            {
                SqlExplainStatement explain = ParseExplain(tokens);
                statement.Explain = explain;
                if (explain.Statement != null)
                {
                    statement.Query = explain.Statement.Query;
                }
            }
            else
            {
                if (kind == SqlStatementKind.Pragma)
                {
                    statement.Pragma = ParsePragma(tokens);
                }
                else
                {
                    if (kind == SqlStatementKind.Analyze)
                    {
                        statement.Analyze = ParseAnalyze(tokens);
                    }
                    else
                    {
                        if (kind == SqlStatementKind.Vacuum)
                        {
                            statement.Vacuum = ParseVacuum(tokens);
                        }
                        else
                        {
                            if (kind == SqlStatementKind.Attach)
                            {
                                statement.Attach = ParseAttach(tokens);
                            }
                            else
                            {
                                if (kind == SqlStatementKind.Detach)
                                {
                                    statement.Detach = ParseDetach(tokens);
                                }
                                else
                                {
                                    if (kind == SqlStatementKind.Reindex)
                                    {
                                        statement.Reindex = ParseReindex(tokens);
                                    }
                                    else
                                    {
                                        if (kind == SqlStatementKind.Begin || kind == SqlStatementKind.Commit || kind == SqlStatementKind.End || kind == SqlStatementKind.Rollback)
                                        {
                                            statement.Transaction = ParseTransaction(tokens);
                                        }
                                        else
                                        {
                                            if (kind == SqlStatementKind.Savepoint)
                                            {
                                                statement.Savepoint = ParseSavepoint(tokens);
                                            }
                                            else
                                            {
                                                if (kind == SqlStatementKind.Release)
                                                {
                                                    statement.Release = ParseRelease(tokens);
                                                }
                                                else
                                                {
                                                    if (kind == SqlStatementKind.Insert)
                                                    {
                                                        statement.Insert = ParseInsertStatement(tokens);
                                                    }
                                                    else
                                                    {
                                                        if (kind == SqlStatementKind.Update)
                                                        {
                                                            statement.Update = ParseUpdateStatement(tokens);
                                                        }
                                                        else
                                                        {
                                                            if (kind == SqlStatementKind.Delete)
                                                            {
                                                                statement.Delete = ParseDeleteStatement(tokens);
                                                            }
                                                            else
                                                            {
                                                                if (kind == SqlStatementKind.CreateTable)
                                                                {
                                                                    statement.CreateTable = ParseCreateTable(tokens);
                                                                }
                                                                else
                                                                {
                                                                    if (kind == SqlStatementKind.AlterTable)
                                                                    {
                                                                        statement.AlterTable = ParseAlterTable(tokens);
                                                                    }
                                                                    else
                                                                    {
                                                                        if (kind == SqlStatementKind.DropTable)
                                                                        {
                                                                            statement.DropTable = ParseDropTable(tokens);
                                                                        }
                                                                        else
                                                                        {
                                                                            if (kind == SqlStatementKind.CreateIndex)
                                                                            {
                                                                                statement.CreateIndex = ParseCreateIndex(tokens);
                                                                            }
                                                                            else
                                                                            {
                                                                                if (kind == SqlStatementKind.DropIndex)
                                                                                {
                                                                                    statement.DropIndex = ParseDropIndex(tokens);
                                                                                }
                                                                                else
                                                                                {
                                                                                    if (kind == SqlStatementKind.CreateView)
                                                                                    {
                                                                                        statement.CreateView = ParseCreateView(tokens);
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        if (kind == SqlStatementKind.DropView)
                                                                                        {
                                                                                            statement.DropView = ParseDropView(tokens);
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            if (kind == SqlStatementKind.CreateTrigger)
                                                                                            {
                                                                                                statement.CreateTrigger = ParseCreateTrigger(tokens);
                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                if (kind == SqlStatementKind.DropTrigger)
                                                                                                {
                                                                                                    statement.DropTrigger = ParseDropTrigger(tokens);
                                                                                                }
                                                                                                else
                                                                                                {
                                                                                                    if (kind == SqlStatementKind.CreateVirtualTable)
                                                                                                    {
                                                                                                        statement.CreateVirtualTable = ParseCreateVirtualTable(tokens);
                                                                                                    }
                                                                                                }
                                                                                            }
                                                                                        }
                                                                                    }
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return statement;
    }

    internal void CollectCreateTriggerTokens(List<SqlToken> tokens)
    {
        bool foundEnd = false;
        while (!Context.IsAtEnd())
        {
            SqlToken token = Context.Consume();
            tokens.Add(token);
            if (IsKeyword(token, "END"))
            {
                foundEnd = true;
            }

            if (foundEnd)
            {
                if (Context.Peek().Kind == SqlTokenKind.Semicolon)
                {
                    tokens.Add(Context.Consume());
                    break;
                }

                if (Context.Peek().Kind == SqlTokenKind.EndOfFile)
                {
                    break;
                }
            }
        }
    }

    internal bool IsCreateTriggerStart()
    {
        string first = GetContextKeywordAt(0);
        string second = GetContextKeywordAt(1);
        string third = GetContextKeywordAt(2);
        if (first != "CREATE")
        {
            return false;
        }

        if (second == "TEMP" || second == "TEMPORARY")
        {
            return third == "TRIGGER";
        }

        return second == "TRIGGER";
    }

    internal string GetContextKeywordAt(int ordinal)
    {
        int seen = 0;
        int offset = 0;
        while (Context.PeekAtOffset(offset).Kind != SqlTokenKind.EndOfFile)
        {
            SqlToken token = Context.PeekAtOffset(offset);
            if (token.Kind != SqlTokenKind.Whitespace && token.Kind != SqlTokenKind.LineComment && token.Kind != SqlTokenKind.BlockComment)
            {
                if (seen == ordinal)
                {
                    string? canonical = Context.Options.Dialect.GetCanonicalKeyword(token.Text);
                    if (canonical == null)
                    {
                        canonical = token.Text.ToUpperInvariant();
                    }

                    return canonical;
                }

                seen = seen + 1;
            }

            offset = offset + 1;
        }

        return "";
    }

    internal bool IsKeyword(SqlToken token, string text)
    {
        string? canonical = Context.Options.Dialect.GetCanonicalKeyword(token.Text);
        if (canonical == null)
        {
            canonical = token.Text.ToUpperInvariant();
        }

        return canonical == text;
    }

    internal SqlTableDefinition ParseCreateTable(List<SqlToken> tokens)
    {
        SqlToken[] createTokens = CreateParseTokens(tokens);
        SqlTokenStream stream = SqlTokenStream.Create(createTokens, Context.Diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, Context.Options, Context.SourceText);
        SqlCreateTableParser parser = new SqlCreateTableParser(context);
        return parser.ParseCreateTable();
    }

    internal SqlPragmaStatement ParsePragma(List<SqlToken> tokens)
    {
        SqlToken[] pragmaTokens = CreateParseTokens(tokens);
        SqlTokenStream stream = SqlTokenStream.Create(pragmaTokens, Context.Diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, Context.Options, Context.SourceText);
        SqlPragmaParser parser = new SqlPragmaParser(context);
        return parser.ParsePragma();
    }

    internal SqlExplainStatement ParseExplain(List<SqlToken> tokens)
    {
        SqlToken[] explainTokens = CreateParseTokens(tokens);
        SqlTokenStream stream = SqlTokenStream.Create(explainTokens, Context.Diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, Context.Options, Context.SourceText);
        SqlExplainParser parser = new SqlExplainParser(context);
        return parser.ParseExplain();
    }

    internal SqlAnalyzeStatement ParseAnalyze(List<SqlToken> tokens)
    {
        SqlToken[] analyzeTokens = CreateParseTokens(tokens);
        SqlTokenStream stream = SqlTokenStream.Create(analyzeTokens, Context.Diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, Context.Options, Context.SourceText);
        SqlAnalyzeParser parser = new SqlAnalyzeParser(context);
        return parser.ParseAnalyze();
    }

    internal SqlVacuumStatement ParseVacuum(List<SqlToken> tokens)
    {
        SqlToken[] vacuumTokens = CreateParseTokens(tokens);
        SqlTokenStream stream = SqlTokenStream.Create(vacuumTokens, Context.Diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, Context.Options, Context.SourceText);
        SqlVacuumParser parser = new SqlVacuumParser(context);
        return parser.ParseVacuum();
    }

    internal SqlAttachStatement ParseAttach(List<SqlToken> tokens)
    {
        SqlToken[] attachTokens = CreateParseTokens(tokens);
        SqlTokenStream stream = SqlTokenStream.Create(attachTokens, Context.Diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, Context.Options, Context.SourceText);
        SqlAttachParser parser = new SqlAttachParser(context);
        return parser.ParseAttach();
    }

    internal SqlDetachStatement ParseDetach(List<SqlToken> tokens)
    {
        SqlToken[] detachTokens = CreateParseTokens(tokens);
        SqlTokenStream stream = SqlTokenStream.Create(detachTokens, Context.Diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, Context.Options, Context.SourceText);
        SqlDetachParser parser = new SqlDetachParser(context);
        return parser.ParseDetach();
    }

    internal SqlReindexStatement ParseReindex(List<SqlToken> tokens)
    {
        SqlToken[] reindexTokens = CreateParseTokens(tokens);
        SqlTokenStream stream = SqlTokenStream.Create(reindexTokens, Context.Diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, Context.Options, Context.SourceText);
        SqlReindexParser parser = new SqlReindexParser(context);
        return parser.ParseReindex();
    }

    internal SqlTransactionStatement ParseTransaction(List<SqlToken> tokens)
    {
        SqlToken[] transactionTokens = CreateParseTokens(tokens);
        SqlTokenStream stream = SqlTokenStream.Create(transactionTokens, Context.Diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, Context.Options, Context.SourceText);
        SqlTransactionParser parser = new SqlTransactionParser(context);
        return parser.ParseTransaction();
    }

    internal SqlSavepointStatement ParseSavepoint(List<SqlToken> tokens)
    {
        SqlToken[] savepointTokens = CreateParseTokens(tokens);
        SqlTokenStream stream = SqlTokenStream.Create(savepointTokens, Context.Diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, Context.Options, Context.SourceText);
        SqlSavepointParser parser = new SqlSavepointParser(context);
        return parser.ParseSavepoint();
    }

    internal SqlReleaseStatement ParseRelease(List<SqlToken> tokens)
    {
        SqlToken[] releaseTokens = CreateParseTokens(tokens);
        SqlTokenStream stream = SqlTokenStream.Create(releaseTokens, Context.Diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, Context.Options, Context.SourceText);
        SqlReleaseParser parser = new SqlReleaseParser(context);
        return parser.ParseRelease();
    }

    internal SqlIndexDefinition ParseCreateIndex(List<SqlToken> tokens)
    {
        SqlToken[] createTokens = CreateParseTokens(tokens);
        SqlTokenStream stream = SqlTokenStream.Create(createTokens, Context.Diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, Context.Options, Context.SourceText);
        SqlCreateIndexParser parser = new SqlCreateIndexParser(context);
        return parser.ParseCreateIndex();
    }

    internal SqlViewDefinition ParseCreateView(List<SqlToken> tokens)
    {
        SqlToken[] createTokens = CreateParseTokens(tokens);
        SqlTokenStream stream = SqlTokenStream.Create(createTokens, Context.Diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, Context.Options, Context.SourceText);
        SqlCreateViewParser parser = new SqlCreateViewParser(context);
        return parser.ParseCreateView();
    }

    internal SqlVirtualTableDefinition ParseCreateVirtualTable(List<SqlToken> tokens)
    {
        SqlToken[] createTokens = CreateParseTokens(tokens);
        SqlTokenStream stream = SqlTokenStream.Create(createTokens, Context.Diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, Context.Options, Context.SourceText);
        SqlCreateVirtualTableParser parser = new SqlCreateVirtualTableParser(context);
        return parser.ParseCreateVirtualTable();
    }

    internal SqlTriggerDefinition ParseCreateTrigger(List<SqlToken> tokens)
    {
        SqlToken[] createTokens = CreateParseTokens(tokens);
        SqlTokenStream stream = SqlTokenStream.Create(createTokens, Context.Diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, Context.Options, Context.SourceText);
        SqlCreateTriggerParser parser = new SqlCreateTriggerParser(context);
        return parser.ParseCreateTrigger();
    }

    internal SqlDropViewDefinition ParseDropView(List<SqlToken> tokens)
    {
        SqlToken[] dropTokens = CreateParseTokens(tokens);
        SqlTokenStream stream = SqlTokenStream.Create(dropTokens, Context.Diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, Context.Options, Context.SourceText);
        SqlDropViewParser parser = new SqlDropViewParser(context);
        return parser.ParseDropView();
    }

    internal SqlDropTriggerDefinition ParseDropTrigger(List<SqlToken> tokens)
    {
        SqlToken[] dropTokens = CreateParseTokens(tokens);
        SqlTokenStream stream = SqlTokenStream.Create(dropTokens, Context.Diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, Context.Options, Context.SourceText);
        SqlDropTriggerParser parser = new SqlDropTriggerParser(context);
        return parser.ParseDropTrigger();
    }

    internal SqlDropIndexDefinition ParseDropIndex(List<SqlToken> tokens)
    {
        SqlToken[] dropTokens = CreateParseTokens(tokens);
        SqlTokenStream stream = SqlTokenStream.Create(dropTokens, Context.Diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, Context.Options, Context.SourceText);
        SqlDropIndexParser parser = new SqlDropIndexParser(context);
        return parser.ParseDropIndex();
    }

    internal SqlAlterTableDefinition ParseAlterTable(List<SqlToken> tokens)
    {
        SqlToken[] alterTokens = CreateParseTokens(tokens);
        SqlTokenStream stream = SqlTokenStream.Create(alterTokens, Context.Diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, Context.Options, Context.SourceText);
        SqlAlterTableParser parser = new SqlAlterTableParser(context);
        return parser.ParseAlterTable();
    }

    internal SqlDropTableDefinition ParseDropTable(List<SqlToken> tokens)
    {
        SqlToken[] dropTokens = CreateParseTokens(tokens);
        SqlTokenStream stream = SqlTokenStream.Create(dropTokens, Context.Diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, Context.Options, Context.SourceText);
        SqlDropTableParser parser = new SqlDropTableParser(context);
        return parser.ParseDropTable();
    }

    internal SqlDeleteStatement ParseDeleteStatement(List<SqlToken> tokens)
    {
        SqlToken[] deleteTokens = CreateParseTokens(tokens);
        SqlTokenStream stream = SqlTokenStream.Create(deleteTokens, Context.Diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, Context.Options, Context.SourceText);
        SqlDeleteParser parser = new SqlDeleteParser(context);
        return parser.ParseDeleteStatement();
    }

    internal SqlMergeStatement ParseMergeStatement(List<SqlToken> tokens)
    {
        SqlToken[] mergeTokens = CreateParseTokens(tokens);
        SqlTokenStream stream = SqlTokenStream.Create(mergeTokens, Context.Diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, Context.Options, Context.SourceText);
        SqlMergeParser parser = new SqlMergeParser(context);
        return parser.ParseMergeStatement();
    }

    internal SqlUpdateStatement ParseUpdateStatement(List<SqlToken> tokens)
    {
        SqlToken[] updateTokens = CreateParseTokens(tokens);
        SqlTokenStream stream = SqlTokenStream.Create(updateTokens, Context.Diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, Context.Options, Context.SourceText);
        SqlUpdateParser parser = new SqlUpdateParser(context);
        return parser.ParseUpdateStatement();
    }

    internal SqlInsertStatement ParseInsertStatement(List<SqlToken> tokens)
    {
        SqlToken[] insertTokens = CreateParseTokens(tokens);
        SqlTokenStream stream = SqlTokenStream.Create(insertTokens, Context.Diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, Context.Options, Context.SourceText);
        SqlInsertParser parser = new SqlInsertParser(context);
        return parser.ParseInsertStatement();
    }

    internal SqlQuery ParseSelectQuery(List<SqlToken> tokens)
    {
        SqlToken[] queryTokens = CreateParseTokens(tokens);
        SqlTokenStream stream = SqlTokenStream.Create(queryTokens, Context.Diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, Context.Options, Context.SourceText);
        SqlQueryParser parser = new SqlQueryParser(context);
        return parser.ParseQuery();
    }

    internal SqlToken[] CreateParseTokens(List<SqlToken> tokens)
    {
        int count = tokens.Count;
        if (count > 0 && tokens[count - 1].Kind == SqlTokenKind.Semicolon)
        {
            count = count - 1;
        }

        SqlToken[] result = new SqlToken[count + 1];
        int index = 0;
        while (index < count)
        {
            result[index] = tokens[index];
            index = index + 1;
        }

        int end = 0;
        if (count > 0)
        {
            end = tokens[count - 1].Span.GetEnd();
        }

        result[count] = new SqlToken(SqlTokenKind.EndOfFile, new SqlTextSpan(end, 0), "");
        return result;
    }

    internal SqlStatementKind ClassifyStatement(List<SqlToken> tokens)
    {
        string first = GetKeywordAt(tokens, 0);

        if (first == "SELECT" || first == "WITH" || first == "VALUES")
        {
            return SqlStatementKind.Select;
        }

        if (first == "INSERT" || first == "REPLACE")
        {
            return SqlStatementKind.Insert;
        }

        if (first == "UPDATE")
        {
            return SqlStatementKind.Update;
        }

        if (first == "DELETE")
        {
            return SqlStatementKind.Delete;
        }

        if (first == "MERGE")
        {
            return SqlStatementKind.Merge;
        }

        if (first == "CREATE")
        {
            return ClassifyCreateStatement(tokens);
        }

        if (first == "ALTER")
        {
            return SqlStatementKind.AlterTable;
        }

        if (first == "DROP")
        {
            return ClassifyDropStatement(tokens);
        }

        if (first == "PRAGMA")
        {
            return SqlStatementKind.Pragma;
        }

        if (first == "ANALYZE")
        {
            return SqlStatementKind.Analyze;
        }

        if (first == "VACUUM")
        {
            return SqlStatementKind.Vacuum;
        }

        if (first == "ATTACH")
        {
            return SqlStatementKind.Attach;
        }

        if (first == "DETACH")
        {
            return SqlStatementKind.Detach;
        }

        if (first == "REINDEX")
        {
            return SqlStatementKind.Reindex;
        }

        if (first == "BEGIN")
        {
            return SqlStatementKind.Begin;
        }

        if (first == "COMMIT")
        {
            return SqlStatementKind.Commit;
        }

        if (first == "END")
        {
            return SqlStatementKind.End;
        }

        if (first == "ROLLBACK")
        {
            return SqlStatementKind.Rollback;
        }

        if (first == "SAVEPOINT")
        {
            return SqlStatementKind.Savepoint;
        }

        if (first == "RELEASE")
        {
            return SqlStatementKind.Release;
        }

        if (first == "EXPLAIN")
        {
            return SqlStatementKind.Explain;
        }

        return SqlStatementKind.Unknown;
    }

    internal SqlStatementKind ClassifyCreateStatement(List<SqlToken> tokens)
    {
        int index = 1;
        string keyword = GetKeywordAt(tokens, index);
        if (keyword == "TEMP" || keyword == "TEMPORARY")
        {
            index = index + 1;
            keyword = GetKeywordAt(tokens, index);
        }

        if (keyword == "VIRTUAL" && GetKeywordAt(tokens, index + 1) == "TABLE")
        {
            return SqlStatementKind.CreateVirtualTable;
        }

        if (keyword == "TABLE")
        {
            return SqlStatementKind.CreateTable;
        }

        if (keyword == "INDEX" || keyword == "UNIQUE")
        {
            return SqlStatementKind.CreateIndex;
        }

        if (keyword == "VIEW")
        {
            return SqlStatementKind.CreateView;
        }

        if (keyword == "TRIGGER")
        {
            return SqlStatementKind.CreateTrigger;
        }

        return SqlStatementKind.Unknown;
    }

    internal SqlStatementKind ClassifyDropStatement(List<SqlToken> tokens)
    {
        string keyword = GetKeywordAt(tokens, 1);

        if (keyword == "TABLE")
        {
            return SqlStatementKind.DropTable;
        }

        if (keyword == "INDEX")
        {
            return SqlStatementKind.DropIndex;
        }

        if (keyword == "VIEW")
        {
            return SqlStatementKind.DropView;
        }

        if (keyword == "TRIGGER")
        {
            return SqlStatementKind.DropTrigger;
        }

        return SqlStatementKind.Unknown;
    }

    internal string GetKeywordAt(List<SqlToken> tokens, int ordinal)
    {
        int seen = 0;
        int index = 0;
        while (index < tokens.Count)
        {
            if (!IsTriviaToken(tokens[index].Kind))
            {
                if (seen == ordinal)
                {
                    string? canonical = Context.Options.Dialect.GetCanonicalKeyword(tokens[index].Text);
                    if (canonical != null)
                    {
                        return canonical;
                    }

                    return tokens[index].Text.ToUpperInvariant();
                }

                seen = seen + 1;
            }

            index = index + 1;
        }

        return "";
    }

    internal void SkipTrivia()
    {
        while (!Context.IsAtEnd() && IsTriviaToken(Context.Peek().Kind))
        {
            Context.Consume();
        }
    }

    internal SqlTextSpan GetScriptSpan()
    {
        if (Context.SourceText != null)
        {
            return Context.SourceText.FullSpan;
        }

        SqlToken[] tokens = Context.Stream.Tokens;
        if (tokens.Length == 0)
        {
            return new SqlTextSpan(0, 0);
        }

        return SqlTextSpan.FromBounds(tokens[0].Span.Start, tokens[tokens.Length - 1].Span.Start);
    }

    internal void PopulateScriptFidelity(SqlScript script)
    {
        SqlToken[] tokens = Context.Stream.Tokens;
        script.Tokens = tokens;
        int tokenCount = tokens.Length;
        if (tokenCount > 0 && tokens[tokenCount - 1].Kind == SqlTokenKind.EndOfFile)
        {
            tokenCount = tokenCount - 1;
        }

        script.TokenRange = new SqlTokenRange(0, tokenCount);
        if (Context.SourceText != null)
        {
            script.SourceText = Context.SourceText.GetText(script.Span);
        }

        if (tokens.Length > 0)
        {
            SqlToken first = GetFirstNonTriviaToken(tokens);
            script.LeadingTrivia = first.LeadingTrivia;
            script.TrailingTrivia = tokens[tokens.Length - 1].LeadingTrivia;
        }

        List<SqlUnsupportedSyntax> unsupported = new List<SqlUnsupportedSyntax>();
        int searchStart = 0;
        int index = 0;
        while (index < script.Statements.Length)
        {
            SqlStatement statement = script.Statements[index];
            searchStart = PopulateStatementFidelity(statement, tokens, searchStart);
            int unsupportedIndex = 0;
            while (unsupportedIndex < statement.UnsupportedSyntax.Length)
            {
                unsupported.Add(statement.UnsupportedSyntax[unsupportedIndex]);
                unsupportedIndex = unsupportedIndex + 1;
            }

            index = index + 1;
        }

        script.UnsupportedSyntax = unsupported.ToArray();
    }

    internal int PopulateStatementFidelity(SqlStatement statement, SqlToken[] scriptTokens, int searchStart)
    {
        int start = searchStart;
        if (statement.Tokens.Length > 0)
        {
            start = FindTokenIndex(scriptTokens, statement.Tokens[0], searchStart);
        }

        statement.TokenRange = new SqlTokenRange(start, statement.Tokens.Length);
        if (statement.Tokens.Length > 0)
        {
            SqlToken first = GetFirstNonTriviaToken(statement.Tokens);
            SqlToken last = GetLastNonTriviaToken(statement.Tokens);
            statement.LeadingTrivia = first.LeadingTrivia;
            statement.TrailingTrivia = last.TrailingTrivia;
            if (last.Kind == SqlTokenKind.Semicolon)
            {
                statement.Terminator = last;
            }
        }

        statement.SourceText = GetSourceText(statement.Span, statement.Tokens);
        statement.QuotedTokens = GetQuotedTokens(statement.Tokens);
        statement.UnsupportedSyntax = GetUnsupportedSyntax(statement);
        return start + statement.Tokens.Length;
    }

    internal int FindTokenIndex(SqlToken[] tokens, SqlToken target, int start)
    {
        int index = start;
        while (index < tokens.Length)
        {
            if (tokens[index] == target)
            {
                return index;
            }

            index = index + 1;
        }

        return start;
    }

    internal SqlToken GetFirstNonTriviaToken(SqlToken[] tokens)
    {
        int index = 0;
        while (index < tokens.Length && IsTriviaToken(tokens[index].Kind))
        {
            index = index + 1;
        }

        if (index >= tokens.Length)
        {
            return tokens[tokens.Length - 1];
        }

        return tokens[index];
    }

    internal SqlToken GetLastNonTriviaToken(SqlToken[] tokens)
    {
        int index = tokens.Length - 1;
        while (index > 0 && IsTriviaToken(tokens[index].Kind))
        {
            index = index - 1;
        }

        return tokens[index];
    }

    internal SqlToken[] GetQuotedTokens(SqlToken[] tokens)
    {
        List<SqlToken> quoted = new List<SqlToken>();
        int index = 0;
        while (index < tokens.Length)
        {
            SqlToken token = tokens[index];
            if (token.IdentifierQuoteStyle != SqlNameQuoteStyle.None || token.StringQuoteStyle != SqlStringQuoteStyle.None || token.QuoteDelimiter.Length > 0)
            {
                quoted.Add(token);
            }

            index = index + 1;
        }

        return quoted.ToArray();
    }

    internal SqlUnsupportedSyntax[] GetUnsupportedSyntax(SqlStatement statement)
    {
        List<SqlUnsupportedSyntax> markers = new List<SqlUnsupportedSyntax>();
        if (statement.Kind == SqlStatementKind.Unknown)
        {
            markers.Add(new SqlUnsupportedSyntax(SqlUnsupportedSyntaxKind.UnknownStatement, "Unknown statement", statement.SourceText, statement.Span, statement.Tokens));
        }

        int tokenIndex = 0;
        while (tokenIndex < statement.Tokens.Length)
        {
            if (statement.Tokens[tokenIndex].Kind == SqlTokenKind.BadToken)
            {
                SqlToken token = statement.Tokens[tokenIndex];
                markers.Add(new SqlUnsupportedSyntax(SqlUnsupportedSyntaxKind.SkippedText, "Skipped source text", token.Text, token.Span, new SqlToken[] { token }));
            }

            tokenIndex = tokenIndex + 1;
        }

        int diagnosticIndex = 0;
        while (diagnosticIndex < Context.Diagnostics.GetCount())
        {
            SqlDiagnostic diagnostic = Context.Diagnostics.GetAt(diagnosticIndex);
            if (diagnostic.Code == "SQLDIALECT001" && statement.Span.ContainsSpan(diagnostic.Span))
            {
                SqlToken[] markerTokens = GetTokensInSpan(statement.Tokens, diagnostic.Span);
                string sourceText = GetSourceText(diagnostic.Span, markerTokens);
                markers.Add(new SqlUnsupportedSyntax(SqlUnsupportedSyntaxKind.DialectFeature, diagnostic.Message, sourceText, diagnostic.Span, markerTokens));
            }

            diagnosticIndex = diagnosticIndex + 1;
        }

        return markers.ToArray();
    }

    internal SqlToken[] GetTokensInSpan(SqlToken[] tokens, SqlTextSpan span)
    {
        List<SqlToken> result = new List<SqlToken>();
        int index = 0;
        while (index < tokens.Length)
        {
            if (span.ContainsSpan(tokens[index].Span) && tokens[index].Kind != SqlTokenKind.EndOfFile)
            {
                result.Add(tokens[index]);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal string GetSourceText(SqlTextSpan span, SqlToken[] tokens)
    {
        if (Context.SourceText != null)
        {
            return Context.SourceText.GetText(span);
        }

        string text = "";
        int index = 0;
        while (index < tokens.Length)
        {
            text = text + tokens[index].Text;
            index = index + 1;
        }

        return text;
    }

    internal static bool IsTriviaToken(SqlTokenKind kind)
    {
        return kind == SqlTokenKind.Whitespace || kind == SqlTokenKind.LineComment || kind == SqlTokenKind.BlockComment;
    }
}
