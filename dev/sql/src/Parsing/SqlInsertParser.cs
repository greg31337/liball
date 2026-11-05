using System;
using System.Collections.Generic;

namespace sql;

internal class SqlInsertParser
{
    internal SqlParserContext Context;

    internal SqlInsertParser(SqlParserContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        Context = context;
    }

    internal SqlInsertStatement ParseInsertStatement()
    {
        SkipTrivia();
        SqlToken first = Context.Peek();
        bool isReplaceKeyword = false;
        SqlConflictActionKind conflictAction = SqlConflictActionKind.None;
        if (IsKeyword(Context.Peek(), "REPLACE"))
        {
            Context.Consume();
            isReplaceKeyword = true;
            conflictAction = SqlConflictActionKind.Replace;
        }
        else
        {
            if (IsKeyword(Context.Peek(), "INSERT"))
            {
                Context.Consume();
                conflictAction = ParseOptionalConflictAction();
            }
            else
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLINSERT001", "Expected INSERT.", Context.Peek().Span);
                Context.Consume();
            }
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "INTO"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLINSERT002", "Expected INTO in INSERT statement.", Context.Peek().Span);
        }

        SkipTrivia();
        SqlName tableName = ParseName("SQLINSERT004", "Expected table name in INSERT statement.");
        SqlName? alias = ParseOptionalAlias();
        SqlName[] columnNames = ParseOptionalColumnNames();
        SqlInsertSourceKind sourceKind = SqlInsertSourceKind.Unknown;
        SqlExpression[][] values = new SqlExpression[0][];
        SqlQuery? sourceQuery = null;
        SqlUpsertClause[] upsertClauses = new SqlUpsertClause[0];
        SqlReturningClause? returning = null;
        int end = tableName.Span.GetEnd();
        if (alias != null)
        {
            end = alias.Span.GetEnd();
        }

        if (columnNames.Length > 0)
        {
            end = columnNames[columnNames.Length - 1].Span.GetEnd();
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "DEFAULT"))
        {
            sourceKind = ParseDefaultValuesSource();
            end = Context.PeekAtOffset(-1).Span.GetEnd();
        }
        else
        {
            if (IsKeyword(Context.Peek(), "VALUES"))
            {
                SqlSelectCore valuesCore = ParseValuesSource();
                sourceKind = SqlInsertSourceKind.Values;
                values = valuesCore.Values;
                end = valuesCore.Span.GetEnd();
            }
            else
            {
                if (IsQueryStart(Context.Peek()))
                {
                    sourceQuery = ParseSelectSource();
                    sourceKind = SqlInsertSourceKind.Select;
                    end = sourceQuery.Span.GetEnd();
                }
                else
                {
                    if (Context.Peek().Kind == SqlTokenKind.EndOfFile)
                    {
                        Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLINSERT008", "Expected INSERT source.", Context.Peek().Span);
                    }
                }
            }
        }

        upsertClauses = ParseUpsertClauses();
        if (upsertClauses.Length > 0)
        {
            end = upsertClauses[upsertClauses.Length - 1].Span.GetEnd();
        }

        returning = ParseOptionalReturningClause();
        if (returning != null)
        {
            end = returning.Span.GetEnd();
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(first.Span.Start, end);
        return new SqlInsertStatement(isReplaceKeyword, conflictAction, tableName, alias, columnNames, sourceKind, values, sourceQuery, upsertClauses, returning, span);
    }

    internal SqlReturningClause? ParseOptionalReturningClause()
    {
        SkipTrivia();
        if (!IsKeyword(Context.Peek(), "RETURNING"))
        {
            return null;
        }

        SqlToken returningToken = Context.Consume();
        if (!Context.Options.Dialect.AllowReturningClause)
        {
            Context.AddUnsupportedFeature("RETURNING clause", returningToken.Span);
        }

        List<SqlResultColumn> resultColumns = new List<SqlResultColumn>();
        int end = returningToken.Span.GetEnd();
        SkipTrivia();
        while (Context.Peek().Kind != SqlTokenKind.EndOfFile && Context.Peek().Kind != SqlTokenKind.Semicolon)
        {
            SqlQueryParser queryParser = new SqlQueryParser(Context);
            SqlResultColumn resultColumn = queryParser.ParseResultColumn();
            resultColumns.Add(resultColumn);
            end = resultColumn.Span.GetEnd();
            SkipTrivia();
            if (Context.Peek().Kind == SqlTokenKind.Comma)
            {
                Context.Consume();
                SkipTrivia();
                continue;
            }

            break;
        }

        if (resultColumns.Count == 0)
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLINSERT020", "Expected RETURNING expression.", Context.Peek().Span);
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(returningToken.Span.Start, end);
        return new SqlReturningClause(resultColumns.ToArray(), span);
    }

    internal SqlUpsertClause[] ParseUpsertClauses()
    {
        List<SqlUpsertClause> clauses = new List<SqlUpsertClause>();
        SkipTrivia();
        while (IsKeyword(Context.Peek(), "ON") && (IsKeyword(PeekNonTrivia(1), "CONFLICT") || IsKeyword(PeekNonTrivia(1), "DUPLICATE")))
        {
            SqlUpsertClause clause = ParseUpsertClause();
            clauses.Add(clause);
            SkipTrivia();
        }

        return clauses.ToArray();
    }

    internal SqlUpsertClause ParseUpsertClause()
    {
        if (IsKeyword(PeekNonTrivia(1), "DUPLICATE"))
        {
            return ParseDuplicateKeyUpsertClause();
        }

        SqlToken onToken = Context.Consume();
        if (!Context.Options.Dialect.AllowUpsertClause)
        {
            Context.AddUnsupportedFeature("UPSERT clause", onToken.Span);
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "CONFLICT"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLINSERT010", "Expected CONFLICT after ON.", Context.Peek().Span);
        }

        SqlOrderingTerm[] conflictTargets = new SqlOrderingTerm[0];
        SqlExpression? conflictWhere = null;
        SqlConflictTarget? conflictTarget = null;
        SkipTrivia();
        if (Context.Peek().Kind == SqlTokenKind.OpenParen)
        {
            int targetStart = Context.Peek().Span.Start;
            conflictTargets = ParseUpsertConflictTargets();
            int targetEnd = Context.PeekAtOffset(-1).Span.GetEnd();
            SkipTrivia();
            if (IsKeyword(Context.Peek(), "WHERE"))
            {
                Context.Consume();
                conflictWhere = ParseExpressionUntil(new string[] { "DO" }, false, "SQLINSERT011", "Expected UPSERT conflict WHERE expression.");
                targetEnd = conflictWhere.Span.GetEnd();
            }

            conflictTarget = new SqlConflictTarget(SqlConflictTargetKind.IndexedColumns, conflictTargets, null, conflictWhere, SqlTextSpan.FromBounds(targetStart, targetEnd));
        }
        else if (IsKeyword(Context.Peek(), "ON") && IsKeyword(PeekNonTrivia(1), "CONSTRAINT"))
        {
            SqlToken targetStart = Context.Consume();
            SkipTrivia();
            Context.Consume();
            SqlName constraintName = ParseName("SQLINSERT021", "Expected constraint name after ON CONSTRAINT.");
            conflictTarget = new SqlConflictTarget(SqlConflictTargetKind.Constraint, new SqlOrderingTerm[0], constraintName, null, SqlTextSpan.FromBounds(targetStart.Span.Start, constraintName.Span.GetEnd()));
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "DO"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLINSERT012", "Expected DO in UPSERT clause.", Context.Peek().Span);
        }

        SqlUpsertActionKind actionKind = SqlUpsertActionKind.Unknown;
        SqlName[] updateColumns = new SqlName[0];
        SqlExpression[] updateExpressions = new SqlExpression[0];
        SqlAssignment[] typedAssignments = new SqlAssignment[0];
        SqlExpression? updateWhere = null;
        int end = Context.PeekAtOffset(-1).Span.GetEnd();
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "NOTHING"))
        {
            SqlToken nothing = Context.Consume();
            actionKind = SqlUpsertActionKind.DoNothing;
            end = nothing.Span.GetEnd();
        }
        else
        {
            if (IsKeyword(Context.Peek(), "UPDATE"))
            {
                Context.Consume();
                actionKind = SqlUpsertActionKind.DoUpdate;
                SkipTrivia();
                if (IsKeyword(Context.Peek(), "SET"))
                {
                    Context.Consume();
                }
                else
                {
                    Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLINSERT013", "Expected SET in UPSERT update action.", Context.Peek().Span);
                }

                SqlUpsertAssignmentList assignments = ParseUpsertAssignments();
                updateColumns = assignments.Columns;
                updateExpressions = assignments.Expressions;
                typedAssignments = assignments.Assignments;
                if (assignments.End > end)
                {
                    end = assignments.End;
                }

                SkipTrivia();
                if (IsKeyword(Context.Peek(), "WHERE"))
                {
                    Context.Consume();
                    updateWhere = ParseExpressionUntil(new string[] { "ON", "RETURNING" }, false, "SQLINSERT017", "Expected UPSERT update WHERE expression.");
                    end = updateWhere.Span.GetEnd();
                }
            }
            else
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLINSERT014", "Expected NOTHING or UPDATE after DO.", Context.Peek().Span);
            }
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(onToken.Span.Start, end);
        SqlUpsertClause clause = new SqlUpsertClause(conflictTargets, conflictWhere, actionKind, updateColumns, updateExpressions, updateWhere, span);
        clause.SyntaxKind = SqlUpsertSyntaxKind.OnConflict;
        clause.ConflictTarget = conflictTarget;
        if (actionKind == SqlUpsertActionKind.DoUpdate)
        {
            clause.Assignments = typedAssignments;
        }

        return clause;
    }

    internal SqlUpsertClause ParseDuplicateKeyUpsertClause()
    {
        SqlToken onToken = Context.Consume();
        if (!Context.Options.Dialect.AllowUpsertClause)
        {
            Context.AddUnsupportedFeature("ON DUPLICATE KEY UPDATE clause", onToken.Span);
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "DUPLICATE"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLINSERT022", "Expected DUPLICATE after ON.", Context.Peek().Span);
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "KEY"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLINSERT023", "Expected KEY after ON DUPLICATE.", Context.Peek().Span);
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "UPDATE"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLINSERT024", "Expected UPDATE after ON DUPLICATE KEY.", Context.Peek().Span);
        }

        SqlUpsertAssignmentList assignments = ParseUpsertAssignments();
        SqlTextSpan span = SqlTextSpan.FromBounds(onToken.Span.Start, assignments.End);
        SqlUpsertClause clause = new SqlUpsertClause(new SqlOrderingTerm[0], null, SqlUpsertActionKind.DoUpdate, assignments.Columns, assignments.Expressions, null, span);
        clause.SyntaxKind = SqlUpsertSyntaxKind.OnDuplicateKey;
        clause.ConflictTarget = new SqlConflictTarget(SqlConflictTargetKind.DuplicateKey, new SqlOrderingTerm[0], null, null, onToken.Span);
        clause.Assignments = assignments.Assignments;
        return clause;
    }

    internal SqlOrderingTerm[] ParseUpsertConflictTargets()
    {
        Context.Consume();
        List<SqlOrderingTerm> targets = new List<SqlOrderingTerm>();
        SkipTrivia();
        while (Context.Peek().Kind != SqlTokenKind.CloseParen && Context.Peek().Kind != SqlTokenKind.EndOfFile)
        {
            SqlQueryParser queryParser = new SqlQueryParser(Context);
            SqlOrderingTerm term = queryParser.ParseOrderingTerm();
            targets.Add(term);
            SkipTrivia();
            if (Context.Peek().Kind == SqlTokenKind.Comma)
            {
                Context.Consume();
                SkipTrivia();
                continue;
            }

            break;
        }

        if (Context.Peek().Kind == SqlTokenKind.CloseParen)
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLINSERT015", "Expected closing parenthesis in UPSERT conflict target.", Context.Peek().Span);
        }

        return targets.ToArray();
    }

    internal SqlUpsertAssignmentList ParseUpsertAssignments()
    {
        List<SqlName> columns = new List<SqlName>();
        List<SqlExpression> expressions = new List<SqlExpression>();
        List<SqlAssignment> assignments = new List<SqlAssignment>();
        int end = Context.PeekAtOffset(-1).Span.GetEnd();
        SkipTrivia();
        while (!Context.IsAtEnd() && !IsUpsertAssignmentTerminator(Context.Peek()))
        {
            SqlName[] assignmentColumns = ParseUpsertAssignmentColumns();
            SqlName column = assignmentColumns[0];
            columns.Add(column);
            SkipTrivia();
            if (Context.Peek().Kind == SqlTokenKind.Equal || Context.Peek().Kind == SqlTokenKind.DoubleEqual)
            {
                Context.Consume();
            }
            else
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLINSERT018", "Expected equals sign in UPSERT update assignment.", Context.Peek().Span);
            }

            SqlExpression expression = ParseExpressionUntil(new string[] { "WHERE", "ON", "RETURNING" }, true, "SQLINSERT019", "Expected UPSERT update expression.");
            expressions.Add(expression);
            assignments.Add(new SqlAssignment(assignmentColumns, expression, SqlTextSpan.FromBounds(column.Span.Start, expression.Span.GetEnd())));
            end = expression.Span.GetEnd();
            SkipTrivia();
            if (Context.Peek().Kind == SqlTokenKind.Comma)
            {
                Context.Consume();
                SkipTrivia();
                continue;
            }

            break;
        }

        if (columns.Count == 0)
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLINSERT016", "Expected UPSERT update column.", Context.Peek().Span);
        }

        return new SqlUpsertAssignmentList(columns.ToArray(), expressions.ToArray(), assignments.ToArray(), end);
    }

    internal SqlName[] ParseUpsertAssignmentColumns()
    {
        SkipTrivia();
        if (Context.Peek().Kind != SqlTokenKind.OpenParen)
        {
            return new SqlName[] { ParseName("SQLINSERT016", "Expected UPSERT update column.") };
        }

        Context.Consume();
        List<SqlName> columns = new List<SqlName>();
        SkipTrivia();
        while (Context.Peek().Kind != SqlTokenKind.CloseParen && Context.Peek().Kind != SqlTokenKind.EndOfFile)
        {
            columns.Add(ParseName("SQLINSERT016", "Expected UPSERT update column."));
            SkipTrivia();
            if (Context.Peek().Kind == SqlTokenKind.Comma)
            {
                Context.Consume();
                SkipTrivia();
                continue;
            }

            break;
        }

        if (Context.Peek().Kind == SqlTokenKind.CloseParen)
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLINSERT025", "Expected closing parenthesis in multi-column UPSERT assignment.", Context.Peek().Span);
        }

        if (columns.Count == 0)
        {
            columns.Add(ParseName("SQLINSERT016", "Expected UPSERT update column."));
        }

        return columns.ToArray();
    }

    internal SqlExpression ParseExpressionUntil(string[] terminatorKeywords, bool stopOnComma, string diagnosticCode, string diagnosticMessage)
    {
        List<SqlToken> tokens = new List<SqlToken>();
        SkipTrivia();
        int start = Context.Peek().Span.Start;
        int end = start;
        int depth = 0;
        while (Context.Peek().Kind != SqlTokenKind.EndOfFile && Context.Peek().Kind != SqlTokenKind.Semicolon)
        {
            if (depth == 0 && stopOnComma && Context.Peek().Kind == SqlTokenKind.Comma)
            {
                break;
            }

            if (depth == 0 && IsAnyKeyword(Context.Peek(), terminatorKeywords))
            {
                break;
            }

            if (depth == 0 && Context.Peek().Kind == SqlTokenKind.CloseParen)
            {
                break;
            }

            SqlToken token = Context.Consume();
            tokens.Add(token);
            end = token.Span.GetEnd();
            if (token.Kind == SqlTokenKind.OpenParen)
            {
                depth = depth + 1;
            }
            else
            {
                if (token.Kind == SqlTokenKind.CloseParen)
                {
                    depth = depth - 1;
                }
            }
        }

        if (tokens.Count == 0)
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, diagnosticCode, diagnosticMessage, Context.Peek().Span);
            return SqlExpression.Create(SqlExpressionKind.Unknown, Context.Peek().Span);
        }

        SqlToken[] expressionTokens = CreateParseTokens(tokens, end);
        SqlTokenStream stream = SqlTokenStream.Create(expressionTokens, Context.Diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, Context.Options, Context.SourceText);
        SqlExpressionParser parser = new SqlExpressionParser(context);
        return parser.ParseExpression();
    }

    internal SqlInsertSourceKind ParseDefaultValuesSource()
    {
        Context.Consume();
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "VALUES"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLINSERT009", "Expected VALUES after DEFAULT.", Context.Peek().Span);
        }

        return SqlInsertSourceKind.DefaultValues;
    }

    internal SqlSelectCore ParseValuesSource()
    {
        SqlQueryParser parser = new SqlQueryParser(Context);
        return parser.ParseValuesCore();
    }

    internal SqlQuery ParseSelectSource()
    {
        SqlQueryParser parser = new SqlQueryParser(Context);
        return parser.ParseQuery();
    }

    internal SqlConflictActionKind ParseOptionalConflictAction()
    {
        SkipTrivia();
        if (!IsKeyword(Context.Peek(), "OR"))
        {
            return SqlConflictActionKind.None;
        }

        Context.Consume();
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "ROLLBACK"))
        {
            Context.Consume();
            return SqlConflictActionKind.Rollback;
        }

        if (IsKeyword(Context.Peek(), "ABORT"))
        {
            Context.Consume();
            return SqlConflictActionKind.Abort;
        }

        if (IsKeyword(Context.Peek(), "FAIL"))
        {
            Context.Consume();
            return SqlConflictActionKind.Fail;
        }

        if (IsKeyword(Context.Peek(), "IGNORE"))
        {
            Context.Consume();
            return SqlConflictActionKind.Ignore;
        }

        if (IsKeyword(Context.Peek(), "REPLACE"))
        {
            Context.Consume();
            return SqlConflictActionKind.Replace;
        }

        Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLINSERT003", "Expected conflict action after OR.", Context.Peek().Span);
        return SqlConflictActionKind.None;
    }

    internal SqlName? ParseOptionalAlias()
    {
        SkipTrivia();
        if (!IsKeyword(Context.Peek(), "AS"))
        {
            return null;
        }

        Context.Consume();
        SkipTrivia();
        if (!IsIdentifierLike(Context.Peek()))
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLINSERT005", "Expected table alias after AS.", Context.Peek().Span);
            return null;
        }

        SqlToken aliasToken = Context.Consume();
        return CreateSinglePartName(aliasToken);
    }

    internal SqlName[] ParseOptionalColumnNames()
    {
        SkipTrivia();
        if (Context.Peek().Kind != SqlTokenKind.OpenParen)
        {
            return new SqlName[0];
        }

        Context.Consume();
        List<SqlName> columnNames = new List<SqlName>();
        SkipTrivia();
        while (Context.Peek().Kind != SqlTokenKind.CloseParen && Context.Peek().Kind != SqlTokenKind.EndOfFile)
        {
            SqlName columnName = ParseName("SQLINSERT006", "Expected column name in INSERT column list.");
            columnNames.Add(columnName);
            SkipTrivia();
            if (Context.Peek().Kind == SqlTokenKind.Comma)
            {
                Context.Consume();
                SkipTrivia();
                continue;
            }

            break;
        }

        if (Context.Peek().Kind == SqlTokenKind.CloseParen)
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLINSERT007", "Expected closing parenthesis in INSERT column list.", Context.Peek().Span);
        }

        return columnNames.ToArray();
    }

    internal SqlName ParseName(string diagnosticCode, string diagnosticMessage)
    {
        List<SqlNamePart> parts = new List<SqlNamePart>();
        SkipTrivia();
        if (!IsIdentifierLike(Context.Peek()))
        {
            SqlToken token = Context.Consume();
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, diagnosticCode, diagnosticMessage, token.Span);
            parts.Add(SqlNamePart.Create(token.Text, token.Text, token.Span, SqlNameQuoteStyle.None, token));
            return CreateName(parts);
        }

        ConsumeNamePart(parts);
        SkipTrivia();
        while (Context.Peek().Kind == SqlTokenKind.Dot)
        {
            Context.Consume();
            SkipTrivia();
            if (!IsIdentifierLike(Context.Peek()))
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, diagnosticCode, diagnosticMessage, Context.Peek().Span);
                break;
            }

            ConsumeNamePart(parts);
            SkipTrivia();
        }

        return CreateName(parts);
    }

    internal void ConsumeNamePart(List<SqlNamePart> parts)
    {
        SqlToken token = Context.Consume();
        parts.Add(SqlNamePart.Create(GetIdentifierText(token), token.Text, token.Span, GetQuoteStyle(token), token));
    }

    internal SqlName CreateName(List<SqlNamePart> parts)
    {
        if (parts.Count == 0)
        {
            return new SqlName(new SqlNamePart[0], new SqlTextSpan(0, 0));
        }

        SqlNamePart first = parts[0];
        SqlNamePart last = parts[parts.Count - 1];
        SqlTextSpan span = SqlTextSpan.FromBounds(first.Span.Start, last.Span.GetEnd());
        return new SqlName(parts.ToArray(), span);
    }

    internal SqlName CreateSinglePartName(SqlToken token)
    {
        SqlNamePart part = SqlNamePart.Create(GetIdentifierText(token), token.Text, token.Span, GetQuoteStyle(token), token);
        return new SqlName(new SqlNamePart[] { part }, token.Span);
    }

    internal void SkipTrivia()
    {
        while (Context.Peek().Kind == SqlTokenKind.Whitespace || Context.Peek().Kind == SqlTokenKind.LineComment || Context.Peek().Kind == SqlTokenKind.BlockComment)
        {
            Context.Consume();
        }
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

    internal bool IsIdentifierLike(SqlToken token)
    {
        return token.Kind == SqlTokenKind.Identifier || token.Kind == SqlTokenKind.Keyword;
    }

    internal bool IsQueryStart(SqlToken token)
    {
        return IsKeyword(token, "SELECT") || IsKeyword(token, "WITH");
    }

    internal bool IsUpsertAssignmentTerminator(SqlToken token)
    {
        return token.Kind == SqlTokenKind.EndOfFile || token.Kind == SqlTokenKind.Semicolon || IsKeyword(token, "WHERE") || IsKeyword(token, "ON") || IsKeyword(token, "RETURNING");
    }

    internal bool IsAnyKeyword(SqlToken token, string[] keywords)
    {
        if (keywords == null)
        {
            throw new ArgumentNullException("keywords");
        }

        int index = 0;
        while (index < keywords.Length)
        {
            if (IsKeyword(token, keywords[index]))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal SqlToken PeekNonTrivia(int nonTriviaOffset)
    {
        if (nonTriviaOffset < 0)
        {
            throw new ArgumentOutOfRangeException("nonTriviaOffset");
        }

        int offset = 0;
        int seen = 0;
        while (true)
        {
            SqlToken token = Context.PeekAtOffset(offset);
            if (!IsTrivia(token.Kind))
            {
                if (seen == nonTriviaOffset)
                {
                    return token;
                }

                seen = seen + 1;
            }

            if (token.Kind == SqlTokenKind.EndOfFile)
            {
                return token;
            }

            offset = offset + 1;
        }
    }

    internal bool IsTrivia(SqlTokenKind kind)
    {
        return kind == SqlTokenKind.Whitespace || kind == SqlTokenKind.LineComment || kind == SqlTokenKind.BlockComment;
    }

    internal SqlToken[] CreateParseTokens(List<SqlToken> tokens, int end)
    {
        SqlToken[] result = new SqlToken[tokens.Count + 1];
        int index = 0;
        while (index < tokens.Count)
        {
            result[index] = tokens[index];
            index = index + 1;
        }

        result[tokens.Count] = new SqlToken(SqlTokenKind.EndOfFile, new SqlTextSpan(end, 0), "");
        return result;
    }

    internal string GetIdentifierText(SqlToken token)
    {
        SqlNameQuoteStyle quoteStyle = GetQuoteStyle(token);
        if (quoteStyle == SqlNameQuoteStyle.None)
        {
            return token.Text;
        }

        string text = StripOuterQuotes(token.Text, quoteStyle);
        if (quoteStyle == SqlNameQuoteStyle.DoubleQuote)
        {
            return text.Replace("\"\"", "\"");
        }

        if (quoteStyle == SqlNameQuoteStyle.Bracket)
        {
            return text.Replace("]]", "]");
        }

        if (quoteStyle == SqlNameQuoteStyle.Backtick)
        {
            return text.Replace("``", "`");
        }

        if (quoteStyle == SqlNameQuoteStyle.SingleQuote)
        {
            return text.Replace("''", "'");
        }

        return text;
    }

    internal SqlNameQuoteStyle GetQuoteStyle(SqlToken token)
    {
        if (token.Text.Length == 0)
        {
            return SqlNameQuoteStyle.None;
        }

        if (token.Text[0] == '"')
        {
            return SqlNameQuoteStyle.DoubleQuote;
        }

        if (token.Text[0] == '[')
        {
            return SqlNameQuoteStyle.Bracket;
        }

        if (token.Text[0] == '`')
        {
            return SqlNameQuoteStyle.Backtick;
        }

        if (token.Text[0] == '\'')
        {
            return SqlNameQuoteStyle.SingleQuote;
        }

        return SqlNameQuoteStyle.None;
    }

    internal string StripOuterQuotes(string text, SqlNameQuoteStyle quoteStyle)
    {
        if (quoteStyle == SqlNameQuoteStyle.None || text.Length == 0)
        {
            return text;
        }

        int start = 1;
        int end = text.Length;
        if (text.Length > 1)
        {
            if (quoteStyle == SqlNameQuoteStyle.DoubleQuote && text[text.Length - 1] == '"')
            {
                end = text.Length - 1;
            }

            if (quoteStyle == SqlNameQuoteStyle.Bracket && text[text.Length - 1] == ']')
            {
                end = text.Length - 1;
            }

            if (quoteStyle == SqlNameQuoteStyle.Backtick && text[text.Length - 1] == '`')
            {
                end = text.Length - 1;
            }

            if (quoteStyle == SqlNameQuoteStyle.SingleQuote && text[text.Length - 1] == '\'')
            {
                end = text.Length - 1;
            }
        }

        if (end < start)
        {
            return "";
        }

        return text.Substring(start, end - start);
    }
}

internal class SqlUpsertAssignmentList
{
    internal SqlName[] Columns;
    internal SqlExpression[] Expressions;
    internal SqlAssignment[] Assignments;
    internal int End;

    internal SqlUpsertAssignmentList(SqlName[] columns, SqlExpression[] expressions, SqlAssignment[] assignments, int end)
    {
        if (columns == null)
        {
            throw new ArgumentNullException("columns");
        }

        if (expressions == null)
        {
            throw new ArgumentNullException("expressions");
        }

        if (assignments == null)
        {
            throw new ArgumentNullException("assignments");
        }

        Columns = columns;
        Expressions = expressions;
        Assignments = assignments;
        End = end;
    }
}
