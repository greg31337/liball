using System;
using System.Collections.Generic;

namespace sql;

internal class SqlQueryParser
{
    internal SqlParserContext Context;

    internal SqlQueryParser(SqlParserContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        Context = context;
    }

    internal SqlQuery ParseQuery()
    {
        SqlCommonTableExpression[] commonTableExpressions = new SqlCommonTableExpression[0];
        bool isRecursive = false;
        int start = Context.Peek().Span.Start;
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "WITH"))
        {
            SqlToken withToken = Context.Consume();
            if (!Context.Options.Dialect.AllowCommonTableExpressions)
            {
                Context.AddUnsupportedFeature("common table expressions", withToken.Span);
            }

            start = withToken.Span.Start;
            SkipTrivia();
            if (IsKeyword(Context.Peek(), "RECURSIVE"))
            {
                Context.Consume();
                isRecursive = true;
            }

            commonTableExpressions = ParseCommonTableExpressionList();
        }

        SqlSelectCore core = ParseQueryCore();
        List<SqlSelectCore> cores = new List<SqlSelectCore>();
        List<SqlCompoundOperatorKind> compoundOperators = new List<SqlCompoundOperatorKind>();
        List<SqlCompoundQueryPart> compoundParts = new List<SqlCompoundQueryPart>();
        cores.Add(core);
        core.UnsupportedClauses = ParseUnsupportedCoreClauses();
        if (core.UnsupportedClauses.Length > 0)
        {
            core.Span = SqlTextSpan.FromBounds(core.Span.Start, core.UnsupportedClauses[core.UnsupportedClauses.Length - 1].Span.GetEnd());
        }
        SqlOrderingTerm[] orderBy = new SqlOrderingTerm[0];
        SqlLimitClause? limit = null;
        int end = core.Span.GetEnd();
        SkipTrivia();
        while (IsCompoundOperatorStart(Context.Peek()))
        {
            int compoundStart = Context.Peek().Span.Start;
            SqlCompoundOperatorKind compoundOperator = ParseCompoundOperator();
            compoundOperators.Add(compoundOperator);
            SqlSelectCore nextCore = ParseQueryCore();
            nextCore.UnsupportedClauses = ParseUnsupportedCoreClauses();
            if (nextCore.UnsupportedClauses.Length > 0)
            {
                nextCore.Span = SqlTextSpan.FromBounds(nextCore.Span.Start, nextCore.UnsupportedClauses[nextCore.UnsupportedClauses.Length - 1].Span.GetEnd());
            }
            cores.Add(nextCore);
            compoundParts.Add(new SqlCompoundQueryPart(compoundOperator, nextCore, SqlTextSpan.FromBounds(compoundStart, nextCore.Span.GetEnd())));
            end = nextCore.Span.GetEnd();
            SkipTrivia();
        }

        if (IsKeyword(Context.Peek(), "ORDER"))
        {
            orderBy = ParseOrderByClause();
            if (orderBy.Length > 0)
            {
                end = orderBy[orderBy.Length - 1].Span.GetEnd();
            }
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "LIMIT"))
        {
            limit = ParseLimitClause();
            end = limit.Span.GetEnd();
        }

        SqlOffsetFetchClause? offsetFetch = null;
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "OFFSET") || IsKeyword(Context.Peek(), "FETCH"))
        {
            offsetFetch = ParseOffsetFetchClause();
            end = offsetFetch.Span.GetEnd();
        }

        SqlUnsupportedQueryClause[] unsupportedClauses = ParseUnsupportedQueryTail();
        if (unsupportedClauses.Length > 0)
        {
            end = unsupportedClauses[unsupportedClauses.Length - 1].Span.GetEnd();
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(start, end);
        SqlQuery query = SqlQuery.Create(commonTableExpressions, isRecursive, cores.ToArray(), compoundOperators.ToArray(), orderBy, limit, span);
        query.CompoundParts = compoundParts.ToArray();
        query.OffsetFetch = offsetFetch;
        query.UnsupportedClauses = unsupportedClauses;
        return query;
    }

    internal SqlCommonTableExpression[] ParseCommonTableExpressionList()
    {
        List<SqlCommonTableExpression> expressions = new List<SqlCommonTableExpression>();
        SkipTrivia();
        while (!Context.IsAtEnd())
        {
            if (!IsIdentifierLike(Context.Peek()))
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY022", "Expected common table expression name.", Context.Peek().Span);
                break;
            }

            SqlCommonTableExpression expression = ParseCommonTableExpression();
            expressions.Add(expression);
            SkipTrivia();
            if (Context.Peek().Kind == SqlTokenKind.Comma)
            {
                Context.Consume();
                SkipTrivia();
                continue;
            }

            break;
        }

        if (expressions.Count == 0)
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY022", "Expected common table expression name.", Context.Peek().Span);
        }

        return expressions.ToArray();
    }

    internal SqlCommonTableExpression ParseCommonTableExpression()
    {
        SqlToken nameToken = Context.Consume();
        SqlName name = CreateSinglePartName(nameToken);
        SqlName[] columnNames = new SqlName[0];
        SkipTrivia();
        if (Context.Peek().Kind == SqlTokenKind.OpenParen)
        {
            columnNames = ParseColumnAliasList();
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "AS"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY023", "Expected AS in common table expression.", Context.Peek().Span);
        }

        SqlCteMaterializationKind materialization = SqlCteMaterializationKind.None;
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "MATERIALIZED"))
        {
            Context.Consume();
            materialization = SqlCteMaterializationKind.Materialized;
        }
        else
        {
            if (IsKeyword(Context.Peek(), "NOT"))
            {
                Context.Consume();
                SkipTrivia();
                if (IsKeyword(Context.Peek(), "MATERIALIZED"))
                {
                    Context.Consume();
                    materialization = SqlCteMaterializationKind.NotMaterialized;
                }
                else
                {
                    Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY024", "Expected MATERIALIZED after NOT.", Context.Peek().Span);
                }
            }
        }

        SkipTrivia();
        SqlQuery query;
        int end = name.Span.GetEnd();
        if (Context.Peek().Kind == SqlTokenKind.OpenParen)
        {
            SqlToken open = Context.Consume();
            query = ParseParenthesizedQuery(open);
            end = Context.PeekAtOffset(-1).Span.GetEnd();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY025", "Expected common table expression query.", Context.Peek().Span);
            query = SqlQuery.Create(new SqlSelectCore[0], Context.Peek().Span);
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(name.Span.Start, end);
        return new SqlCommonTableExpression(name, columnNames, materialization, query, span);
    }

    internal SqlSelectCore ParseQueryCore()
    {
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "VALUES"))
        {
            return ParseValuesCore();
        }

        return ParseSelectCore();
    }

    internal SqlSelectCore ParseValuesCore()
    {
        SqlToken valuesToken = Context.Consume();
        List<SqlExpression[]> rows = new List<SqlExpression[]>();
        int end = valuesToken.Span.GetEnd();
        SkipTrivia();
        while (Context.Peek().Kind == SqlTokenKind.OpenParen)
        {
            SqlExpression[] row = ParseValuesRow();
            rows.Add(row);
            end = Context.PeekAtOffset(-1).Span.GetEnd();
            SkipTrivia();
            if (Context.Peek().Kind == SqlTokenKind.Comma)
            {
                Context.Consume();
                SkipTrivia();
                continue;
            }

            break;
        }

        if (rows.Count == 0)
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY026", "Expected VALUES row.", Context.Peek().Span);
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(valuesToken.Span.Start, end);
        SqlSelectCore core = SqlSelectCore.Create(new SqlResultColumn[0], span);
        core.IsValues = true;
        core.Values = rows.ToArray();
        return core;
    }

    internal SqlExpression[] ParseValuesRow()
    {
        Context.Consume();
        List<SqlExpression> values = new List<SqlExpression>();
        SkipTrivia();
        while (Context.Peek().Kind != SqlTokenKind.CloseParen && Context.Peek().Kind != SqlTokenKind.EndOfFile)
        {
            SqlExpressionParser expressionParser = new SqlExpressionParser(Context);
            SqlExpression expression = expressionParser.ParseExpression();
            values.Add(expression);
            SkipTrivia();
            if (Context.Peek().Kind == SqlTokenKind.Comma)
            {
                Context.Consume();
                SkipTrivia();
                continue;
            }

            if (RecoverMissingCommaBeforeValuesExpression())
            {
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
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY027", "Expected closing parenthesis in VALUES row.", Context.Peek().Span);
        }

        return values.ToArray();
    }

    internal SqlCompoundOperatorKind ParseCompoundOperator()
    {
        if (IsKeyword(Context.Peek(), "UNION"))
        {
            Context.Consume();
            SkipTrivia();
            if (IsKeyword(Context.Peek(), "ALL"))
            {
                Context.Consume();
                return SqlCompoundOperatorKind.UnionAll;
            }

            return SqlCompoundOperatorKind.Union;
        }

        if (IsKeyword(Context.Peek(), "INTERSECT"))
        {
            Context.Consume();
            return SqlCompoundOperatorKind.Intersect;
        }

        if (IsKeyword(Context.Peek(), "EXCEPT"))
        {
            Context.Consume();
            return SqlCompoundOperatorKind.Except;
        }

        Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY021", "Expected compound operator.", Context.Peek().Span);
        return SqlCompoundOperatorKind.None;
    }

    internal SqlSelectCore ParseSelectCore()
    {
        SkipTrivia();
        SqlToken first = Context.Peek();
        if (IsKeyword(Context.Peek(), "SELECT"))
        {
            first = Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY001", "Expected SELECT.", Context.Peek().Span);
        }

        bool isDistinct = false;
        bool isAll = false;
        SqlTopClause? top = null;
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "DISTINCT"))
        {
            Context.Consume();
            isDistinct = true;
        }
        else
        {
            if (IsKeyword(Context.Peek(), "ALL"))
            {
                Context.Consume();
                isAll = true;
            }
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "TOP"))
        {
            top = ParseTopClause();
        }

        List<SqlResultColumn> resultColumns = new List<SqlResultColumn>();
        int end = first.Span.GetEnd();
        SkipTrivia();
        while (!Context.IsAtEnd() && !IsSelectCoreClauseStart(Context.Peek()))
        {
            SqlResultColumn resultColumn = ParseResultColumn();
            resultColumns.Add(resultColumn);
            end = resultColumn.Span.GetEnd();
            SkipTrivia();
            if (Context.Peek().Kind == SqlTokenKind.Comma)
            {
                SqlToken comma = Context.Consume();
                end = comma.Span.GetEnd();
                SkipTrivia();
                continue;
            }

            if (RecoverMissingCommaBeforeResultColumn())
            {
                continue;
            }

            break;
        }

        if (resultColumns.Count == 0)
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY002", "Expected result column in SELECT core.", Context.Peek().Span);
        }

        SqlTableReference[] from = new SqlTableReference[0];
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "FROM"))
        {
            Context.Consume();
            from = ParseFromList();
            if (from.Length > 0)
            {
                end = from[from.Length - 1].Span.GetEnd();
            }
        }

        SqlExpression? where = null;
        SqlExpression[] groupBy = new SqlExpression[0];
        SqlExpression? having = null;
        SqlWindowDefinition[] windows = new SqlWindowDefinition[0];
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "WHERE"))
        {
            Context.Consume();
            SqlExpressionParser expressionParser = new SqlExpressionParser(Context);
            where = expressionParser.ParseExpression();
            end = where.Span.GetEnd();
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "GROUP"))
        {
            groupBy = ParseGroupByList();
            if (groupBy.Length > 0)
            {
                end = groupBy[groupBy.Length - 1].Span.GetEnd();
            }
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "HAVING"))
        {
            Context.Consume();
            SqlExpressionParser expressionParser = new SqlExpressionParser(Context);
            having = expressionParser.ParseExpression();
            end = having.Span.GetEnd();
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "WINDOW"))
        {
            if (!Context.Options.Dialect.AllowWindowFunctions)
            {
                Context.AddUnsupportedFeature("window functions", Context.Peek().Span);
            }

            windows = ParseWindowClause();
            if (windows.Length > 0)
            {
                end = windows[windows.Length - 1].Span.GetEnd();
            }
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(first.Span.Start, end);
        SqlSelectCore core = SqlSelectCore.Create(isDistinct, isAll, resultColumns.ToArray(), from, where, groupBy, having, windows, span);
        core.Top = top;
        return core;
    }

    internal SqlExpression[] ParseGroupByList()
    {
        Context.Consume();
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "BY"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY012", "Expected BY after GROUP.", Context.Peek().Span);
        }

        List<SqlExpression> expressions = new List<SqlExpression>();
        SkipTrivia();
        while (!Context.IsAtEnd() && !IsGroupByTerminator(Context.Peek()))
        {
            SqlExpressionParser expressionParser = new SqlExpressionParser(Context);
            SqlExpression expression = expressionParser.ParseExpression();
            expressions.Add(expression);
            SkipTrivia();
            if (Context.Peek().Kind == SqlTokenKind.Comma)
            {
                Context.Consume();
                SkipTrivia();
                continue;
            }

            break;
        }

        if (expressions.Count == 0)
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY013", "Expected GROUP BY expression.", Context.Peek().Span);
        }

        return expressions.ToArray();
    }

    internal SqlOrderingTerm[] ParseOrderByClause()
    {
        Context.Consume();
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "BY"))
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY017", "Expected BY after ORDER.", Context.Peek().Span);
        }

        List<SqlOrderingTerm> terms = new List<SqlOrderingTerm>();
        SkipTrivia();
        while (!Context.IsAtEnd() && !IsOrderByTerminator(Context.Peek()))
        {
            SqlOrderingTerm term = ParseOrderingTerm();
            terms.Add(term);
            SkipTrivia();
            if (Context.Peek().Kind == SqlTokenKind.Comma)
            {
                Context.Consume();
                SkipTrivia();
                continue;
            }

            break;
        }

        if (terms.Count == 0)
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY018", "Expected ORDER BY expression.", Context.Peek().Span);
        }

        return terms.ToArray();
    }

    internal SqlLimitClause ParseLimitClause()
    {
        SqlToken limitToken = Context.Consume();
        SkipTrivia();
        SqlExpressionParser expressionParser = new SqlExpressionParser(Context);
        SqlExpression limitExpression = expressionParser.ParseExpression();
        SqlExpression? offsetExpression = null;
        int end = limitExpression.Span.GetEnd();
        SkipTrivia();
        if (Context.Peek().Kind == SqlTokenKind.Comma)
        {
            Context.Consume();
            SqlExpression oldLimit = limitExpression;
            SqlExpressionParser limitParser = new SqlExpressionParser(Context);
            limitExpression = limitParser.ParseExpression();
            offsetExpression = oldLimit;
            end = limitExpression.Span.GetEnd();
        }
        else
        {
            if (IsKeyword(Context.Peek(), "OFFSET"))
            {
                Context.Consume();
                SqlExpressionParser offsetParser = new SqlExpressionParser(Context);
                offsetExpression = offsetParser.ParseExpression();
                end = offsetExpression.Span.GetEnd();
            }
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(limitToken.Span.Start, end);
        return new SqlLimitClause(limitExpression, offsetExpression, span);
    }

    internal SqlTopClause ParseTopClause()
    {
        SqlToken topToken = Context.Consume();
        if (Context.Options.Dialect.Kind != SqlDialectKind.SqlServer)
        {
            Context.AddUnsupportedFeature("TOP", topToken.Span);
        }

        SkipTrivia();
        bool parenthesized = false;
        if (Context.Peek().Kind == SqlTokenKind.OpenParen)
        {
            Context.Consume();
            parenthesized = true;
            SkipTrivia();
        }

        SqlExpressionParser expressionParser = new SqlExpressionParser(Context);
        SqlExpression quantity = expressionParser.ParseExpression();
        int end = quantity.Span.GetEnd();
        SkipTrivia();
        if (parenthesized)
        {
            if (Context.Peek().Kind == SqlTokenKind.CloseParen)
            {
                end = Context.Consume().Span.GetEnd();
            }
            else
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY033", "Expected closing parenthesis in TOP clause.", Context.Peek().Span);
            }
        }

        bool isPercent = false;
        bool withTies = false;
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "PERCENT"))
        {
            isPercent = true;
            end = Context.Consume().Span.GetEnd();
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "WITH") && IsKeyword(PeekNonTrivia(1), "TIES"))
        {
            Context.Consume();
            SkipTrivia();
            end = Context.Consume().Span.GetEnd();
            withTies = true;
        }

        return new SqlTopClause(quantity, isPercent, withTies, parenthesized, SqlTextSpan.FromBounds(topToken.Span.Start, end));
    }

    internal SqlOffsetFetchClause ParseOffsetFetchClause()
    {
        SqlExpression? offset = null;
        SqlExpression? fetch = null;
        SqlFetchDirectionKind direction = SqlFetchDirectionKind.None;
        bool withTies = false;
        int start = Context.Peek().Span.Start;
        int end = start;
        if (IsKeyword(Context.Peek(), "OFFSET"))
        {
            SqlToken offsetToken = Context.Consume();
            end = offsetToken.Span.GetEnd();
            SkipTrivia();
            SqlExpressionParser offsetParser = new SqlExpressionParser(Context);
            offset = offsetParser.ParseExpression();
            end = offset.Span.GetEnd();
            SkipTrivia();
            if (IsKeyword(Context.Peek(), "ROW") || IsKeyword(Context.Peek(), "ROWS"))
            {
                end = Context.Consume().Span.GetEnd();
            }
            else
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY034", "Expected ROW or ROWS after OFFSET quantity.", Context.Peek().Span);
            }
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "FETCH"))
        {
            SqlToken fetchToken = Context.Consume();
            end = fetchToken.Span.GetEnd();
            SkipTrivia();
            if (IsKeyword(Context.Peek(), "FIRST"))
            {
                direction = SqlFetchDirectionKind.First;
                end = Context.Consume().Span.GetEnd();
            }
            else if (IsKeyword(Context.Peek(), "NEXT"))
            {
                direction = SqlFetchDirectionKind.Next;
                end = Context.Consume().Span.GetEnd();
            }
            else
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY035", "Expected FIRST or NEXT after FETCH.", Context.Peek().Span);
            }

            SkipTrivia();
            if (!IsKeyword(Context.Peek(), "ROW") && !IsKeyword(Context.Peek(), "ROWS"))
            {
                SqlExpressionParser fetchParser = new SqlExpressionParser(Context);
                fetch = fetchParser.ParseExpression();
                end = fetch.Span.GetEnd();
                SkipTrivia();
            }

            if (IsKeyword(Context.Peek(), "ROW") || IsKeyword(Context.Peek(), "ROWS"))
            {
                end = Context.Consume().Span.GetEnd();
            }
            else
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY036", "Expected ROW or ROWS in FETCH clause.", Context.Peek().Span);
            }

            SkipTrivia();
            if (IsKeyword(Context.Peek(), "ONLY"))
            {
                end = Context.Consume().Span.GetEnd();
            }
            else if (IsKeyword(Context.Peek(), "WITH") && IsKeyword(PeekNonTrivia(1), "TIES"))
            {
                Context.Consume();
                SkipTrivia();
                end = Context.Consume().Span.GetEnd();
                withTies = true;
            }
            else
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY037", "Expected ONLY or WITH TIES in FETCH clause.", Context.Peek().Span);
            }
        }

        return new SqlOffsetFetchClause(offset, fetch, direction, withTies, SqlTextSpan.FromBounds(start, end));
    }

    internal SqlUnsupportedQueryClause[] ParseUnsupportedCoreClauses()
    {
        List<SqlUnsupportedQueryClause> clauses = new List<SqlUnsupportedQueryClause>();
        SkipTrivia();
        while (IsUnsupportedQueryClauseStart(Context.Peek()))
        {
            clauses.Add(ParseUnsupportedQueryClause());
            SkipTrivia();
        }

        return clauses.ToArray();
    }

    internal SqlUnsupportedQueryClause[] ParseUnsupportedQueryTail()
    {
        return ParseUnsupportedCoreClauses();
    }

    internal SqlUnsupportedQueryClause ParseUnsupportedQueryClause()
    {
        SqlToken first = Context.Peek();
        SqlUnsupportedQueryClauseKind kind = GetUnsupportedQueryClauseKind(first);
        List<SqlToken> tokens = new List<SqlToken>();
        SqlToken last = first;
        int depth = 0;
        while (Context.Peek().Kind != SqlTokenKind.EndOfFile && Context.Peek().Kind != SqlTokenKind.Semicolon && !(Context.Peek().Kind == SqlTokenKind.CloseParen && depth == 0) && !(depth == 0 && IsUnsupportedClauseBoundary(Context.Peek(), first)))
        {
            if (depth == 0 && IsTrivia(Context.Peek().Kind))
            {
                SqlToken next = PeekNonTrivia(0);
                if (next.Kind == SqlTokenKind.EndOfFile || next.Kind == SqlTokenKind.Semicolon || next.Kind == SqlTokenKind.CloseParen || IsUnsupportedClauseBoundary(next, first))
                {
                    break;
                }
            }

            SqlToken token = Context.Consume();
            tokens.Add(token);
            if (!IsTrivia(token.Kind))
            {
                last = token;
            }

            if (token.Kind == SqlTokenKind.OpenParen)
            {
                depth = depth + 1;
            }
            else if (token.Kind == SqlTokenKind.CloseParen && depth > 0)
            {
                depth = depth - 1;
            }
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(first.Span.Start, last.Span.GetEnd());
        Context.AddUnsupportedFeature(first.Text, span);
        return new SqlUnsupportedQueryClause(kind, GetSourceText(span), span, tokens.ToArray());
    }

    internal bool IsUnsupportedClauseBoundary(SqlToken token, SqlToken first)
    {
        if (token == first)
        {
            return false;
        }

        return IsCompoundOperatorStart(token) || IsKeyword(token, "ORDER") || IsKeyword(token, "LIMIT") || IsKeyword(token, "OFFSET") || IsKeyword(token, "FETCH") || IsUnsupportedQueryClauseStart(token);
    }

    internal bool IsUnsupportedQueryClauseStart(SqlToken token)
    {
        return IsKeyword(token, "QUALIFY") || IsKeyword(token, "SAMPLE") || IsKeyword(token, "TABLESAMPLE") || IsKeyword(token, "PIVOT") || IsKeyword(token, "UNPIVOT") || IsKeyword(token, "CONNECT") || IsKeyword(token, "MODEL");
    }

    internal SqlUnsupportedQueryClauseKind GetUnsupportedQueryClauseKind(SqlToken token)
    {
        if (IsKeyword(token, "QUALIFY"))
        {
            return SqlUnsupportedQueryClauseKind.Qualify;
        }

        if (IsKeyword(token, "SAMPLE") || IsKeyword(token, "TABLESAMPLE"))
        {
            return SqlUnsupportedQueryClauseKind.Sample;
        }

        if (IsKeyword(token, "PIVOT"))
        {
            return SqlUnsupportedQueryClauseKind.Pivot;
        }

        if (IsKeyword(token, "UNPIVOT"))
        {
            return SqlUnsupportedQueryClauseKind.Unpivot;
        }

        if (IsKeyword(token, "CONNECT"))
        {
            return SqlUnsupportedQueryClauseKind.ConnectBy;
        }

        if (IsKeyword(token, "MODEL"))
        {
            return SqlUnsupportedQueryClauseKind.Model;
        }

        return SqlUnsupportedQueryClauseKind.Other;
    }

    internal SqlOrderingTerm ParseOrderingTerm()
    {
        SqlExpressionParser expressionParser = new SqlExpressionParser(Context);
        SqlExpression expression = expressionParser.ParseExpression();
        SqlName? collation = null;
        SqlOrderingDirection direction = SqlOrderingDirection.None;
        SqlNullOrdering nullOrdering = SqlNullOrdering.None;
        int end = expression.Span.GetEnd();
        if (expression.Kind == SqlExpressionKind.Collate)
        {
            collation = expression.Name;
            if (expression.Operand != null)
            {
                expression = expression.Operand;
            }
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "COLLATE"))
        {
            Context.Consume();
            SkipTrivia();
            if (IsIdentifierLike(Context.Peek()))
            {
                SqlToken collationToken = Context.Consume();
                collation = CreateSinglePartName(collationToken);
                end = collation.Span.GetEnd();
            }
            else
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY019", "Expected collation name.", Context.Peek().Span);
            }
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "ASC"))
        {
            SqlToken token = Context.Consume();
            direction = SqlOrderingDirection.Asc;
            end = token.Span.GetEnd();
        }
        else
        {
            if (IsKeyword(Context.Peek(), "DESC"))
            {
                SqlToken token = Context.Consume();
                direction = SqlOrderingDirection.Desc;
                end = token.Span.GetEnd();
            }
        }

        SkipTrivia();
        if (IsKeyword(Context.Peek(), "NULLS"))
        {
            SqlToken nulls = Context.Consume();
            end = nulls.Span.GetEnd();
            SkipTrivia();
            if (IsKeyword(Context.Peek(), "FIRST"))
            {
                SqlToken first = Context.Consume();
                nullOrdering = SqlNullOrdering.First;
                end = first.Span.GetEnd();
            }
            else
            {
                if (IsKeyword(Context.Peek(), "LAST"))
                {
                    SqlToken last = Context.Consume();
                    nullOrdering = SqlNullOrdering.Last;
                    end = last.Span.GetEnd();
                }
                else
                {
                    Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY020", "Expected FIRST or LAST after NULLS.", Context.Peek().Span);
                }
            }
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(expression.Span.Start, end);
        return new SqlOrderingTerm(expression, collation, direction, nullOrdering, span);
    }

    internal SqlWindowDefinition[] ParseWindowClause()
    {
        Context.Consume();
        List<SqlWindowDefinition> windows = new List<SqlWindowDefinition>();
        SkipTrivia();
        while (!Context.IsAtEnd() && !IsWindowClauseTerminator(Context.Peek()))
        {
            if (!IsIdentifierLike(Context.Peek()))
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY014", "Expected window name.", Context.Peek().Span);
                break;
            }

            SqlToken nameToken = Context.Consume();
            SqlName name = CreateSinglePartName(nameToken);
            SkipTrivia();
            if (IsKeyword(Context.Peek(), "AS"))
            {
                Context.Consume();
            }
            else
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY015", "Expected AS in WINDOW clause.", Context.Peek().Span);
            }

            SkipTrivia();
            SqlWindowDefinition window;
            if (Context.Peek().Kind == SqlTokenKind.OpenParen)
            {
                SqlExpressionParser expressionParser = new SqlExpressionParser(Context);
                window = expressionParser.ParseInlineWindowDefinition();
                window.Name = name;
                window.Span = SqlTextSpan.FromBounds(name.Span.Start, window.Span.GetEnd());
            }
            else
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY016", "Expected window definition.", Context.Peek().Span);
                window = new SqlWindowDefinition(name, null, new SqlExpression[0], new SqlOrderingTerm[0], null, name.Span);
            }

            windows.Add(window);
            SkipTrivia();
            if (Context.Peek().Kind == SqlTokenKind.Comma)
            {
                Context.Consume();
                SkipTrivia();
                continue;
            }

            break;
        }

        if (windows.Count == 0)
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY014", "Expected window name.", Context.Peek().Span);
        }

        return windows.ToArray();
    }

    internal SqlTableReference[] ParseFromList()
    {
        List<SqlTableReference> tables = new List<SqlTableReference>();
        SkipTrivia();
        while (!Context.IsAtEnd() && !IsTableReferenceTerminator(Context.Peek()))
        {
            SqlTableReference table = ParseTableReference();
            tables.Add(table);
            SkipTrivia();
            if (Context.Peek().Kind == SqlTokenKind.Comma)
            {
                Context.Consume();
                SkipTrivia();
                continue;
            }

            if (RecoverMissingCommaBeforeTableReference())
            {
                continue;
            }

            break;
        }

        if (tables.Count == 0)
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY004", "Expected table reference after FROM.", Context.Peek().Span);
        }

        return tables.ToArray();
    }

    internal SqlTableReference ParseTableReference()
    {
        SqlTableReference table = ParseTableReferenceCore();
        ParseJoinSuffixes(table);
        return table;
    }

    internal SqlTableReference ParseTableReferenceCore()
    {
        SkipTrivia();
        if (IsTableReferenceTerminator(Context.Peek()))
        {
            SqlToken terminator = Context.Consume();
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY005", "Expected table reference.", terminator.Span);
            return SqlTableReference.Create(SqlTableReferenceKind.Unknown, terminator.Span);
        }

        if (Context.Peek().Kind == SqlTokenKind.OpenParen)
        {
            return ParseParenthesizedTableReference();
        }

        if (IsKeyword(Context.Peek(), "LATERAL"))
        {
            SqlToken lateral = Context.Consume();
            SqlTableReference lateralTable = ParseTableReferenceCore();
            if (lateralTable.SourceData == null)
            {
                lateralTable.SourceData = CreateTableSourceData(lateralTable);
            }

            lateralTable.SourceData.IsLateral = true;
            lateralTable.Span = SqlTextSpan.FromBounds(lateral.Span.Start, lateralTable.Span.GetEnd());
            return lateralTable;
        }

        if (IsIdentifierLike(Context.Peek()))
        {
            SqlExpressionParser expressionParser = new SqlExpressionParser(Context);
            SqlExpression expression = expressionParser.ParseExpression();
            SqlTableReferenceKind kind = SqlTableReferenceKind.Unknown;
            SqlName? name = null;
            SqlExpression? functionExpression = null;
            if (expression.Kind == SqlExpressionKind.FunctionCall)
            {
                kind = SqlTableReferenceKind.Function;
                functionExpression = expression;
            }
            else
            {
                if (expression.Kind == SqlExpressionKind.Name)
                {
                    kind = SqlTableReferenceKind.Named;
                    name = expression.Name;
                }
            }

            SqlTableReference table = SqlTableReference.Create(kind, expression.Span, name, null, functionExpression, null, new SqlJoin[0], null, new SqlName[0]);
            table.SourceData = CreateTableSourceData(table);
            ParseTableAlias(table);
            return table;
        }

        SqlToken token = Context.Consume();
        Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY005", "Expected table reference.", token.Span);
        return SqlTableReference.Create(SqlTableReferenceKind.Unknown, token.Span);
    }

    internal void ParseJoinSuffixes(SqlTableReference table)
    {
        List<SqlJoin> joins = new List<SqlJoin>();
        AddJoins(joins, table.Joins);
        SkipTrivia();
        while (IsJoinOperatorStart(Context.Peek()))
        {
            SqlJoin join = ParseJoin();
            joins.Add(join);
            table.Span = SqlTextSpan.FromBounds(table.Span.Start, join.Span.GetEnd());
            SkipTrivia();
        }

        table.Joins = joins.ToArray();
    }

    internal SqlJoin ParseJoin()
    {
        SqlToken first = Context.Peek();
        List<SqlJoinModifierKind> modifiers = new List<SqlJoinModifierKind>();
        SqlJoinKind kind = SqlJoinKind.Unknown;
        bool isNatural = false;
        bool isOuter = false;
        bool sawJoin = false;

        while (!Context.IsAtEnd())
        {
            SkipTrivia();
            if (IsKeyword(Context.Peek(), "NATURAL"))
            {
                Context.Consume();
                isNatural = true;
                modifiers.Add(SqlJoinModifierKind.Natural);
            }
            else
            {
                if (IsKeyword(Context.Peek(), "OUTER"))
                {
                    Context.Consume();
                    isOuter = true;
                    modifiers.Add(SqlJoinModifierKind.Outer);
                }
                else
                {
                    if (IsKeyword(Context.Peek(), "LEFT"))
                    {
                        Context.Consume();
                        kind = SqlJoinKind.Left;
                        modifiers.Add(SqlJoinModifierKind.Left);
                    }
                    else
                    {
                        if (IsKeyword(Context.Peek(), "RIGHT"))
                        {
                            Context.Consume();
                            kind = SqlJoinKind.Right;
                            modifiers.Add(SqlJoinModifierKind.Right);
                        }
                        else
                        {
                            if (IsKeyword(Context.Peek(), "FULL"))
                            {
                                Context.Consume();
                                kind = SqlJoinKind.Full;
                                modifiers.Add(SqlJoinModifierKind.Full);
                            }
                            else
                            {
                                if (IsKeyword(Context.Peek(), "CROSS"))
                                {
                                    Context.Consume();
                                    kind = SqlJoinKind.Cross;
                                    modifiers.Add(SqlJoinModifierKind.Cross);
                                }
                                else
                                {
                                    if (IsKeyword(Context.Peek(), "INNER"))
                                    {
                                        Context.Consume();
                                        kind = SqlJoinKind.Inner;
                                        modifiers.Add(SqlJoinModifierKind.Inner);
                                    }
                                    else
                                    {
                                        if (IsKeyword(Context.Peek(), "SEMI"))
                                        {
                                            Context.Consume();
                                            kind = SqlJoinKind.Semi;
                                            modifiers.Add(SqlJoinModifierKind.Semi);
                                            continue;
                                        }
                                        else if (IsKeyword(Context.Peek(), "ANTI"))
                                        {
                                            Context.Consume();
                                            kind = SqlJoinKind.Anti;
                                            modifiers.Add(SqlJoinModifierKind.Anti);
                                            continue;
                                        }
                                        else if (IsKeyword(Context.Peek(), "ASOF"))
                                        {
                                            Context.Consume();
                                            kind = SqlJoinKind.AsOf;
                                            modifiers.Add(SqlJoinModifierKind.AsOf);
                                            continue;
                                        }
                                        else if (IsKeyword(Context.Peek(), "APPLY"))
                                        {
                                            Context.Consume();
                                            modifiers.Add(SqlJoinModifierKind.Apply);
                                            if (isOuter)
                                            {
                                                kind = SqlJoinKind.OuterApply;
                                            }
                                            else
                                            {
                                                kind = SqlJoinKind.CrossApply;
                                            }

                                            sawJoin = true;
                                            break;
                                        }
                                        else if (IsKeyword(Context.Peek(), "STRAIGHT_JOIN"))
                                        {
                                            Context.Consume();
                                            kind = SqlJoinKind.Straight;
                                            modifiers.Add(SqlJoinModifierKind.Straight);
                                            sawJoin = true;
                                            break;
                                        }
                                        else if (IsKeyword(Context.Peek(), "JOIN"))
                                        {
                                            Context.Consume();
                                            sawJoin = true;
                                            break;
                                        }

                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        if (!sawJoin)
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY009", "Expected JOIN.", Context.Peek().Span);
        }

        if (kind == SqlJoinKind.Unknown)
        {
            if (isNatural)
            {
                kind = SqlJoinKind.Natural;
            }
            else
            {
                kind = SqlJoinKind.Inner;
            }
        }

        SqlTableReference right = ParseTableReferenceCore();
        SqlJoinConstraintKind constraintKind = SqlJoinConstraintKind.None;
        SqlExpression? onExpression = null;
        SqlName[] usingColumns = new SqlName[0];
        int end = right.Span.GetEnd();
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "ON"))
        {
            Context.Consume();
            SqlExpressionParser expressionParser = new SqlExpressionParser(Context);
            onExpression = expressionParser.ParseExpression();
            constraintKind = SqlJoinConstraintKind.On;
            end = onExpression.Span.GetEnd();
        }
        else
        {
            if (IsKeyword(Context.Peek(), "USING"))
            {
                Context.Consume();
                usingColumns = ParseUsingColumnList();
                constraintKind = SqlJoinConstraintKind.Using;
                if (usingColumns.Length > 0)
                {
                    end = Context.PeekAtOffset(-1).Span.GetEnd();
                }
            }
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(first.Span.Start, end);
        SqlJoin join = new SqlJoin(kind, isNatural, isOuter, right, constraintKind, onExpression, usingColumns, span);
        join.Modifiers = modifiers.ToArray();
        return join;
    }

    internal SqlTableReference ParseParenthesizedTableReference()
    {
        SqlToken open = Context.Consume();
        SkipTrivia();
        SqlTableReference table;
        if (IsSubqueryStart(Context.Peek()))
        {
            SqlQuery query = ParseParenthesizedQuery(open);
            SqlTextSpan span = SqlTextSpan.FromBounds(open.Span.Start, Context.PeekAtOffset(-1).Span.GetEnd());
            SqlTableReferenceKind kind = SqlTableReferenceKind.Subquery;
            SqlExpression[][] values = new SqlExpression[0][];
            if (query.SelectCores.Length == 1 && query.SelectCores[0].IsValues)
            {
                kind = SqlTableReferenceKind.Values;
                values = query.SelectCores[0].Values;
            }

            table = SqlTableReference.Create(kind, span, null, query, null, null, new SqlJoin[0], null, new SqlName[0]);
            table.SourceData = new SqlTableSourceData(null, new SqlExpression[0], values, query, false);
        }
        else
        {
            SqlTableReference inner = ParseTableReference();
            int end = inner.Span.GetEnd();
            SkipTrivia();
            if (Context.Peek().Kind == SqlTokenKind.CloseParen)
            {
                SqlToken close = Context.Consume();
                end = close.Span.GetEnd();
            }
            else
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY006", "Expected closing parenthesis in table reference.", Context.Peek().Span);
            }

            SqlTextSpan span = SqlTextSpan.FromBounds(open.Span.Start, end);
            table = SqlTableReference.Create(SqlTableReferenceKind.Parenthesized, span, null, null, null, inner, new SqlJoin[0], null, new SqlName[0]);
        }

        ParseTableAlias(table);
        return table;
    }

    internal SqlQuery ParseParenthesizedQuery(SqlToken open)
    {
        List<SqlToken> tokens = new List<SqlToken>();
        SqlToken last = open;
        int depth = 0;
        while (Context.Peek().Kind != SqlTokenKind.EndOfFile)
        {
            if (Context.Peek().Kind == SqlTokenKind.CloseParen && depth == 0)
            {
                break;
            }

            SqlToken token = Context.Consume();
            tokens.Add(token);
            last = token;
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

        int end = last.Span.GetEnd();
        if (Context.Peek().Kind == SqlTokenKind.CloseParen)
        {
            SqlToken close = Context.Consume();
            end = close.Span.GetEnd();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY006", "Expected closing parenthesis in table reference.", Context.Peek().Span);
        }

        SqlToken[] queryTokens = CreateParseTokens(tokens, end);
        SqlTokenStream stream = SqlTokenStream.Create(queryTokens, Context.Diagnostics);
        SqlParserContext context = SqlParserContext.Create(stream, Context.Options, Context.SourceText);
        SqlQueryParser parser = new SqlQueryParser(context);
        return parser.ParseQuery();
    }

    internal void ParseTableAlias(SqlTableReference table)
    {
        int end = table.Span.GetEnd();
        SkipTrivia();
        bool hasAs = false;
        if (IsKeyword(Context.Peek(), "AS"))
        {
            Context.Consume();
            hasAs = true;
            SkipTrivia();
        }

        if (IsIdentifierLike(Context.Peek()) && !IsTableReferenceTerminator(Context.Peek()))
        {
            SqlToken aliasToken = Context.Consume();
            table.Alias = CreateSinglePartName(aliasToken);
            end = table.Alias.Span.GetEnd();
            SkipTrivia();
            if (Context.Peek().Kind == SqlTokenKind.OpenParen)
            {
                table.ColumnAliases = ParseColumnAliasList();
                if (table.ColumnAliases.Length > 0)
                {
                    end = Context.PeekAtOffset(-1).Span.GetEnd();
                }
            }
        }
        else
        {
            if (hasAs)
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY008", "Expected table alias.", Context.Peek().Span);
            }
        }

        table.Span = SqlTextSpan.FromBounds(table.Span.Start, end);
    }

    internal SqlName[] ParseColumnAliasList()
    {
        List<SqlName> aliases = new List<SqlName>();
        Context.Consume();
        SkipTrivia();
        while (IsIdentifierLike(Context.Peek()))
        {
            SqlToken token = Context.Consume();
            aliases.Add(CreateSinglePartName(token));
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
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY007", "Expected closing parenthesis in column alias list.", Context.Peek().Span);
        }

        return aliases.ToArray();
    }

    internal SqlName[] ParseUsingColumnList()
    {
        List<SqlName> columns = new List<SqlName>();
        SkipTrivia();
        if (Context.Peek().Kind == SqlTokenKind.OpenParen)
        {
            Context.Consume();
        }
        else
        {
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY010", "Expected opening parenthesis after USING.", Context.Peek().Span);
        }

        SkipTrivia();
        while (IsIdentifierLike(Context.Peek()))
        {
            SqlToken token = Context.Consume();
            columns.Add(CreateSinglePartName(token));
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
            Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY011", "Expected closing parenthesis in USING clause.", Context.Peek().Span);
        }

        return columns.ToArray();
    }

    internal SqlResultColumn ParseResultColumn()
    {
        SkipTrivia();
        SqlExpressionParser expressionParser = new SqlExpressionParser(Context);
        SqlExpression expression = expressionParser.ParseExpression();
        SqlName? qualifier = null;
        bool isWildcard = false;
        if (expression.Kind == SqlExpressionKind.Wildcard)
        {
            isWildcard = true;
            qualifier = expression.Name;
        }

        SqlName? alias = null;
        int end = expression.Span.GetEnd();
        List<SqlToken> tokens = new List<SqlToken>();
        AddTokens(tokens, expression.Tokens);
        SkipTrivia();
        if (IsKeyword(Context.Peek(), "AS"))
        {
            tokens.Add(Context.Consume());
            SkipTrivia();
            if (IsIdentifierLike(Context.Peek()))
            {
                SqlToken aliasToken = Context.Consume();
                tokens.Add(aliasToken);
                alias = CreateSinglePartName(aliasToken);
                end = alias.Span.GetEnd();
            }
            else
            {
                Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY003", "Expected result column alias.", Context.Peek().Span);
            }
        }
        else
        {
            if (!isWildcard && IsIdentifierLike(Context.Peek()) && !IsSelectCoreClauseStart(Context.Peek()))
            {
                SqlToken aliasToken = Context.Consume();
                tokens.Add(aliasToken);
                alias = CreateSinglePartName(aliasToken);
                end = alias.Span.GetEnd();
            }
        }

        SqlTextSpan span = SqlTextSpan.FromBounds(expression.Span.Start, end);
        return new SqlResultColumn(expression, isWildcard, qualifier, alias, span, tokens.ToArray());
    }

    internal bool IsSelectCoreClauseStart(SqlToken token)
    {
        if (token == null)
        {
            throw new ArgumentNullException("token");
        }

        return IsKeyword(token, "FROM") || IsKeyword(token, "WHERE") || IsKeyword(token, "GROUP") || IsKeyword(token, "HAVING") || IsKeyword(token, "WINDOW") || IsKeyword(token, "ORDER") || IsKeyword(token, "LIMIT") || IsKeyword(token, "OFFSET") || IsKeyword(token, "FETCH") || IsKeyword(token, "UNION") || IsKeyword(token, "INTERSECT") || IsKeyword(token, "EXCEPT") || IsUnsupportedQueryClauseStart(token) || token.Kind == SqlTokenKind.Semicolon || token.Kind == SqlTokenKind.CloseParen;
    }

    internal bool IsTableReferenceTerminator(SqlToken token)
    {
        if (token == null)
        {
            throw new ArgumentNullException("token");
        }

        return IsSelectCoreClauseStart(token) || IsJoinOperatorStart(token) || IsKeyword(token, "ON") || IsKeyword(token, "USING") || token.Kind == SqlTokenKind.Comma || token.Kind == SqlTokenKind.EndOfFile;
    }

    internal bool IsGroupByTerminator(SqlToken token)
    {
        return IsKeyword(token, "HAVING") || IsKeyword(token, "WINDOW") || IsKeyword(token, "ORDER") || IsKeyword(token, "LIMIT") || IsKeyword(token, "OFFSET") || IsKeyword(token, "FETCH") || IsKeyword(token, "UNION") || IsKeyword(token, "INTERSECT") || IsKeyword(token, "EXCEPT") || IsUnsupportedQueryClauseStart(token) || token.Kind == SqlTokenKind.Semicolon || token.Kind == SqlTokenKind.CloseParen || token.Kind == SqlTokenKind.EndOfFile;
    }

    internal bool IsWindowClauseTerminator(SqlToken token)
    {
        return IsKeyword(token, "ORDER") || IsKeyword(token, "LIMIT") || IsKeyword(token, "OFFSET") || IsKeyword(token, "FETCH") || IsKeyword(token, "UNION") || IsKeyword(token, "INTERSECT") || IsKeyword(token, "EXCEPT") || IsUnsupportedQueryClauseStart(token) || token.Kind == SqlTokenKind.Semicolon || token.Kind == SqlTokenKind.CloseParen || token.Kind == SqlTokenKind.EndOfFile;
    }

    internal bool IsOrderByTerminator(SqlToken token)
    {
        return IsKeyword(token, "LIMIT") || IsKeyword(token, "OFFSET") || IsKeyword(token, "FETCH") || IsKeyword(token, "UNION") || IsKeyword(token, "INTERSECT") || IsKeyword(token, "EXCEPT") || IsUnsupportedQueryClauseStart(token) || token.Kind == SqlTokenKind.Semicolon || token.Kind == SqlTokenKind.CloseParen || token.Kind == SqlTokenKind.EndOfFile;
    }

    internal bool IsCompoundOperatorStart(SqlToken token)
    {
        return IsKeyword(token, "UNION") || IsKeyword(token, "INTERSECT") || IsKeyword(token, "EXCEPT");
    }

    internal bool IsJoinOperatorStart(SqlToken token)
    {
        return IsKeyword(token, "JOIN") || IsKeyword(token, "INNER") || IsKeyword(token, "LEFT") || IsKeyword(token, "RIGHT") || IsKeyword(token, "FULL") || IsKeyword(token, "CROSS") || IsKeyword(token, "NATURAL") || IsKeyword(token, "OUTER") || IsKeyword(token, "SEMI") || IsKeyword(token, "ANTI") || IsKeyword(token, "ASOF") || IsKeyword(token, "APPLY") || IsKeyword(token, "STRAIGHT_JOIN");
    }

    internal bool RecoverMissingCommaBeforeResultColumn()
    {
        if (!Context.CanRecover())
        {
            return false;
        }

        if (IsSelectCoreClauseStart(Context.Peek()) || !IsExpressionStart(Context.Peek()))
        {
            return false;
        }

        Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY030", "Expected comma between result columns.", Context.Peek().Span);
        return Context.RecordRecoveryStep();
    }

    internal bool RecoverMissingCommaBeforeValuesExpression()
    {
        if (!Context.CanRecover())
        {
            return false;
        }

        if (!IsExpressionStart(Context.Peek()))
        {
            return false;
        }

        Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY031", "Expected comma between VALUES expressions.", Context.Peek().Span);
        return Context.RecordRecoveryStep();
    }

    internal bool RecoverMissingCommaBeforeTableReference()
    {
        if (!Context.CanRecover())
        {
            return false;
        }

        if (IsTableReferenceTerminator(Context.Peek()) || !IsTableReferenceStart(Context.Peek()))
        {
            return false;
        }

        Context.AddDiagnostic(SqlDiagnosticSeverity.Error, "SQLQUERY032", "Expected comma between table references.", Context.Peek().Span);
        return Context.RecordRecoveryStep();
    }

    internal bool IsExpressionStart(SqlToken token)
    {
        return token.Kind == SqlTokenKind.Identifier || token.Kind == SqlTokenKind.Keyword || token.Kind == SqlTokenKind.NumericLiteral || token.Kind == SqlTokenKind.StringLiteral || token.Kind == SqlTokenKind.BlobLiteral || token.Kind == SqlTokenKind.Parameter || token.Kind == SqlTokenKind.OpenParen || token.Kind == SqlTokenKind.Star || token.Kind == SqlTokenKind.Plus || token.Kind == SqlTokenKind.Minus || token.Kind == SqlTokenKind.Tilde;
    }

    internal bool IsTableReferenceStart(SqlToken token)
    {
        return IsIdentifierLike(token) || token.Kind == SqlTokenKind.OpenParen;
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

    internal bool IsSubqueryStart(SqlToken token)
    {
        return IsKeyword(token, "SELECT") || IsKeyword(token, "WITH") || IsKeyword(token, "VALUES");
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

    internal SqlName CreateSinglePartName(SqlToken token)
    {
        SqlNamePart part = SqlNamePart.Create(GetIdentifierText(token), token.Text, token.Span, GetQuoteStyle(token), token);
        return new SqlName(new SqlNamePart[] { part }, token.Span);
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

    internal SqlTableSourceData CreateTableSourceData(SqlTableReference table)
    {
        SqlName? name = table.Name;
        SqlExpression[] arguments = new SqlExpression[0];
        if (table.FunctionExpression != null)
        {
            name = table.FunctionExpression.Name;
            arguments = table.FunctionExpression.Arguments;
            if (table.FunctionExpression.FunctionData != null)
            {
                name = table.FunctionExpression.FunctionData.Name;
                arguments = table.FunctionExpression.FunctionData.Arguments;
            }
        }

        return new SqlTableSourceData(name, arguments, new SqlExpression[0][], table.Query, false);
    }

    internal string GetSourceText(SqlTextSpan span)
    {
        if (Context.SourceText != null)
        {
            return Context.SourceText.GetText(span);
        }

        return "";
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

    internal void AddTokens(List<SqlToken> target, SqlToken[] tokens)
    {
        int index = 0;
        while (index < tokens.Length)
        {
            target.Add(tokens[index]);
            index = index + 1;
        }
    }

    internal void AddJoins(List<SqlJoin> target, SqlJoin[] joins)
    {
        int index = 0;
        while (index < joins.Length)
        {
            target.Add(joins[index]);
            index = index + 1;
        }
    }
}
