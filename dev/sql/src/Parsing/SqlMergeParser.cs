using System;
using System.Collections.Generic;

namespace sql;

internal class SqlMergeParser
{
    internal SqlParserContext Context;

    internal SqlMergeParser(SqlParserContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        Context = context;
    }

    internal SqlMergeStatement ParseMergeStatement()
    {
        SkipTrivia();
        SqlToken first = Context.Peek();
        if (IsKeyword(Context.Peek(), "MERGE"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLMERGE001", "Expected MERGE.", Context.Peek().Span);
            Context.Consume();
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "INTO"))
        {
            Context.Consume();
        }

        SqlName tableName = ParseName("SQLMERGE002", "Expected MERGE target table.");
        SqlName? alias = ParseOptionalTargetAlias();
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "USING"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLMERGE003", "Expected USING in MERGE statement.", Context.Peek().Span);
        }

        SqlQueryParser queryParser = new SqlQueryParser(Context);
        SqlTableReference source = queryParser.ParseTableReference();
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "ON"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLMERGE004", "Expected ON in MERGE statement.", Context.Peek().Span);
        }

        SqlExpression condition = ParseExpressionUntil(new string[] { "WHEN" }, "SQLMERGE005", "Expected MERGE match condition.", false);
        List<SqlMergeWhenClause> clauses = new List<SqlMergeWhenClause>();
        SkipTrivia();
        while (IsKeyword(Context.Peek(), "WHEN"))
        {
            clauses.Add(ParseWhenClause());
            SkipTrivia();
        }

        SqlReturningClause? returning = ParseOptionalReturningClause();

        if (clauses.Count == 0)
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLMERGE006", "Expected at least one WHEN clause in MERGE statement.", Context.Peek().Span);
        }

        int end = condition.Span.GetEnd();
        if (clauses.Count > 0)
        {
            end = clauses[clauses.Count - 1].Span.GetEnd();
        }

        if (returning != null)
        {
            end = returning.Span.GetEnd();
        }

        return new SqlMergeStatement(tableName, alias, source, condition, clauses.ToArray(), returning, SqlTextSpan.FromBounds(first.Span.Start, end));
    }

    internal SqlMergeWhenClause ParseWhenClause()
    {
        SqlToken whenToken = Context.Consume();
        SqlMergeMatchKind matchKind = SqlMergeMatchKind.Unknown;
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "MATCHED"))
        {
            Context.Consume();
            matchKind = SqlMergeMatchKind.Matched;
        }
        else if (IsKeyword(Context.Peek(), "NOT"))
        {
            Context.Consume();
            SkipTrivia();
            if (IsKeyword(Context.Peek(), "MATCHED"))
            {
                Context.Consume();
                matchKind = SqlMergeMatchKind.NotMatchedByTarget;
            }
            else
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLMERGE007", "Expected MATCHED after WHEN NOT.", Context.Peek().Span);
            }

            SkipTrivia();
            if (IsKeyword(Context.Peek(), "BY"))
            {
                Context.Consume();
                SkipTrivia();
                if (IsKeyword(Context.Peek(), "SOURCE"))
                {
                    Context.Consume();
                    matchKind = SqlMergeMatchKind.NotMatchedBySource;
                }
                else if (IsKeyword(Context.Peek(), "TARGET"))
                {
                    Context.Consume();
                    matchKind = SqlMergeMatchKind.NotMatchedByTarget;
                }
                else
                {
                    Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLMERGE008", "Expected SOURCE or TARGET after BY.", Context.Peek().Span);
                }
            }
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLMERGE009", "Expected MATCHED or NOT MATCHED after WHEN.", Context.Peek().Span);
        }

        SqlExpression? condition = null;
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "AND"))
        {
            Context.Consume();
            condition = ParseExpressionUntil(new string[] { "THEN" }, "SQLMERGE010", "Expected MERGE clause condition.", false);
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "THEN"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLMERGE011", "Expected THEN in MERGE clause.", Context.Peek().Span);
        }

        SqlMergeActionKind actionKind = SqlMergeActionKind.Unknown;
        SqlAssignment[] assignments = new SqlAssignment[0];
        SqlName[] insertColumns = new SqlName[0];
        SqlExpression[] insertValues = new SqlExpression[0];
        int end = Context.PeekAtOffset(-1).Span.GetEnd();
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "UPDATE"))
        {
            actionKind = SqlMergeActionKind.Update;
            end = Context.Consume().Span.GetEnd();
            SkipTrivia();
            if (IsKeyword(Context.Peek(), "SET"))
            {
                Context.Consume();
            }
            else
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLMERGE012", "Expected SET in MERGE update action.", Context.Peek().Span);
            }

            assignments = ParseMergeAssignments();
            if (assignments.Length > 0)
            {
                end = assignments[assignments.Length - 1].Span.GetEnd();
            }
        }
        else if (IsKeyword(Context.Peek(), "DELETE"))
        {
            actionKind = SqlMergeActionKind.Delete;
            end = Context.Consume().Span.GetEnd();
        }
        else if (IsKeyword(Context.Peek(), "INSERT"))
        {
            actionKind = SqlMergeActionKind.Insert;
            end = Context.Consume().Span.GetEnd();
            insertColumns = ParseOptionalInsertColumns();
            SkipTrivia();
            if (IsKeyword(Context.Peek(), "VALUES"))
            {
                Context.Consume();
                SkipTrivia();
                if (Context.Peek().Kind == SqlTokenKind.OpenParen)
                {
                    SqlQueryParser queryParser = new SqlQueryParser(Context);
                    insertValues = queryParser.ParseValuesRow();
                    end = Context.PeekAtOffset(-1).Span.GetEnd();
                }
                else
                {
                    Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLMERGE013", "Expected value list in MERGE insert action.", Context.Peek().Span);
                }
            }
            else
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLMERGE014", "Expected VALUES in MERGE insert action.", Context.Peek().Span);
            }
        }
        else if (IsKeyword(Context.Peek(), "DO") && IsKeyword(PeekNonTrivia(1), "NOTHING"))
        {
            actionKind = SqlMergeActionKind.DoNothing;
            Context.Consume();
            SkipTrivia();
            end = Context.Consume().Span.GetEnd();
        }
        else if (IsKeyword(Context.Peek(), "NOTHING"))
        {
            actionKind = SqlMergeActionKind.DoNothing;
            end = Context.Consume().Span.GetEnd();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLMERGE015", "Expected UPDATE, DELETE, INSERT, or DO NOTHING in MERGE clause.", Context.Peek().Span);
        }

        return new SqlMergeWhenClause(matchKind, condition, actionKind, assignments, insertColumns, insertValues, SqlTextSpan.FromBounds(whenToken.Span.Start, end));
    }

    internal SqlAssignment[] ParseMergeAssignments()
    {
        List<SqlAssignment> assignments = new List<SqlAssignment>();
        SkipTrivia();
        while (!Context.IsAtEnd() && !IsKeyword(Context.Peek(), "WHEN") && !IsKeyword(Context.Peek(), "RETURNING"))
        {
            SqlName[] columns = ParseAssignmentColumns();
            SkipTrivia();
            if (Context.Peek().Kind == SqlTokenKind.Equal || Context.Peek().Kind == SqlTokenKind.DoubleEqual)
            {
                Context.Consume();
            }
            else
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLMERGE016", "Expected equals sign in MERGE assignment.", Context.Peek().Span);
            }

            SqlExpression value = ParseExpressionUntil(new string[] { "WHEN", "RETURNING" }, "SQLMERGE017", "Expected MERGE assignment expression.", true);
            assignments.Add(new SqlAssignment(columns, value, SqlTextSpan.FromBounds(columns[0].Span.Start, value.Span.GetEnd())));
            SkipTrivia();
            if (Context.Peek().Kind == SqlTokenKind.Comma)
            {
                Context.Consume();
                SkipTrivia();
                continue;
            }

            break;
        }

        if (assignments.Count == 0)
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLMERGE018", "Expected MERGE update assignment.", Context.Peek().Span);
        }

        return assignments.ToArray();
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

        List<SqlResultColumn> columns = new List<SqlResultColumn>();
        int end = returningToken.Span.GetEnd();
        SkipTrivia();
        while (Context.Peek().Kind != SqlTokenKind.EndOfFile && Context.Peek().Kind != SqlTokenKind.Semicolon)
        {
            SqlQueryParser parser = new SqlQueryParser(Context);
            SqlResultColumn column = parser.ParseResultColumn();
            columns.Add(column);
            end = column.Span.GetEnd();
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
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLMERGE023", "Expected RETURNING expression.", Context.Peek().Span);
        }

        return new SqlReturningClause(columns.ToArray(), SqlTextSpan.FromBounds(returningToken.Span.Start, end));
    }

    internal SqlName[] ParseAssignmentColumns()
    {
        SkipTrivia();
        if (Context.Peek().Kind != SqlTokenKind.OpenParen)
        {
            return new SqlName[] { ParseName("SQLMERGE019", "Expected MERGE assignment column.") };
        }

        Context.Consume();
        SqlName[] columns = ParseNameListUntilClose("SQLMERGE019", "Expected MERGE assignment column.");
        if (columns.Length == 0)
        {
            return new SqlName[] { ParseName("SQLMERGE019", "Expected MERGE assignment column.") };
        }

        return columns;
    }

    internal SqlName[] ParseOptionalInsertColumns()
    {
        SkipTrivia();
        if (Context.Peek().Kind != SqlTokenKind.OpenParen)
        {
            return new SqlName[0];
        }

        Context.Consume();
        return ParseNameListUntilClose("SQLMERGE020", "Expected MERGE insert column.");
    }

    internal SqlName[] ParseNameListUntilClose(string diagnosticCode, string diagnosticMessage)
    {
        List<SqlName> names = new List<SqlName>();
        SkipTrivia();
        while (Context.Peek().Kind != SqlTokenKind.CloseParen && Context.Peek().Kind != SqlTokenKind.EndOfFile)
        {
            names.Add(ParseName(diagnosticCode, diagnosticMessage));
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
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLMERGE021", "Expected closing parenthesis in MERGE column list.", Context.Peek().Span);
        }

        return names.ToArray();
    }

    internal SqlName? ParseOptionalTargetAlias()
    {
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "AS"))
        {
            Context.Consume();
            return ParseName("SQLMERGE022", "Expected MERGE target alias.");
        }

        if (IsIdentifierLike(Context.Peek()) && !IsKeyword(Context.Peek(), "USING"))
        {
            SqlToken token = Context.Consume();
            return CreateSinglePartName(token);
        }

        return null;
    }

    internal SqlExpression ParseExpressionUntil(string[] terminatorKeywords, string diagnosticCode, string diagnosticMessage, bool stopOnComma)
    {
        List<SqlToken> tokens = new List<SqlToken>();
        SkipTrivia();
        int end = Context.Peek().Span.Start;
        int depth = 0;
        int caseDepth = 0;
        while (Context.Peek().Kind != SqlTokenKind.EndOfFile && Context.Peek().Kind != SqlTokenKind.Semicolon)
        {
            if (depth == 0 && caseDepth == 0 && IsAnyKeyword(Context.Peek(), terminatorKeywords))
            {
                break;
            }

            if (depth == 0 && caseDepth == 0 && stopOnComma && Context.Peek().Kind == SqlTokenKind.Comma)
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
            else if (token.Kind == SqlTokenKind.CloseParen)
            {
                depth = depth - 1;
            }

            if (IsKeyword(token, "CASE"))
            {
                caseDepth = caseDepth + 1;
            }
            else if (IsKeyword(token, "END") && caseDepth > 0)
            {
                caseDepth = caseDepth - 1;
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
        SqlNamePart first = parts[0];
        SqlNamePart last = parts[parts.Count - 1];
        return new SqlName(parts.ToArray(), SqlTextSpan.FromBounds(first.Span.Start, last.Span.GetEnd()));
    }

    internal SqlName CreateSinglePartName(SqlToken token)
    {
        SqlNamePart part = SqlNamePart.Create(GetIdentifierText(token), token.Text, token.Span, GetQuoteStyle(token), token);
        return new SqlName(new SqlNamePart[] { part }, token.Span);
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

    internal SqlToken PeekNonTrivia(int nonTriviaOffset)
    {
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

    internal bool IsAnyKeyword(SqlToken token, string[] keywords)
    {
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

    internal void SkipTrivia()
    {
        while (IsTrivia(Context.Peek().Kind))
        {
            Context.Consume();
        }
    }

    internal bool IsTrivia(SqlTokenKind kind)
    {
        return kind == SqlTokenKind.Whitespace || kind == SqlTokenKind.LineComment || kind == SqlTokenKind.BlockComment;
    }

    internal string GetIdentifierText(SqlToken token)
    {
        SqlNameQuoteStyle quoteStyle = GetQuoteStyle(token);
        if (quoteStyle == SqlNameQuoteStyle.None)
        {
            return token.Text;
        }

        string text = token.Text.Substring(1, token.Text.Length - 2);
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

        return SqlNameQuoteStyle.None;
    }
}
