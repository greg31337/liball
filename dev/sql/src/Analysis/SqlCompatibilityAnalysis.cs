using System;
using System.Collections.Generic;

namespace sql;

/// <summary>Analyzes syntax-tree data against an immutable target-dialect policy.</summary>
internal static class SqlCompatibilityAnalysis
{
    internal static SqlCompatibilityResult Analyze(SqlScript script, ISqlDialectPolicy targetDialect)
    {
        if (script == null)
        {
            throw new ArgumentNullException("script");
        }

        if (targetDialect == null)
        {
            throw new ArgumentNullException("targetDialect");
        }

        SqlAstWalker walker = new SqlAstWalker();
        walker.WalkScript(script);
        SqlCompatibilityAnalysisContext context = new SqlCompatibilityAnalysisContext(script, targetDialect);
        context.AnalyzeWalker(walker);
        return context.CreateResult();
    }
}

internal class SqlCompatibilityAnalysisContext
{
    internal SqlScript Script;
    internal ISqlDialectPolicy TargetDialect;
    internal SqlDialectKind TargetKind;
    internal SqlNameQuoteStyle[] IdentifierQuoteStyles;
    internal SqlStringQuoteStyle[] StringQuoteStyles;
    internal SqlDialectLiteralPolicyKind[] LiteralPolicies;
    internal SqlOperatorPolicy[] Operators;
    internal List<SqlCompatibilityIssue> Issues;
    internal SqlDiagnosticBag Diagnostics;
    internal SqlSourceText SourceText;

    internal SqlCompatibilityAnalysisContext(SqlScript script, ISqlDialectPolicy targetDialect)
    {
        Script = script;
        TargetDialect = targetDialect;
        TargetKind = targetDialect.GetKind();
        IdentifierQuoteStyles = targetDialect.GetIdentifierQuoteStyles();
        StringQuoteStyles = targetDialect.GetStringQuoteStyles();
        LiteralPolicies = targetDialect.GetLiteralPolicies();
        Operators = targetDialect.GetOperators();
        Issues = new List<SqlCompatibilityIssue>();
        Diagnostics = new SqlDiagnosticBag();
        SourceText = new SqlSourceText(script.SourceText);
    }

    internal void AnalyzeWalker(SqlAstWalker walker)
    {
        AnalyzeStatements(walker.Statements);
        AnalyzeQueries(walker.Queries);
        AnalyzeSelectCores(walker.SelectCores);
        AnalyzeTableReferences(walker.TableReferences);
        AnalyzeJoins(walker.Joins);
        AnalyzeReturningClauses(walker.ReturningClauses);
        AnalyzeUpsertClauses(walker.UpsertClauses);
        AnalyzeExpressions(walker.Expressions);
        AnalyzeLiterals(walker.Literals);
        AnalyzeNames(walker.Names);
        AnalyzeUnsupportedSyntax(walker.UnsupportedSyntax);
    }

    internal void AnalyzeStatements(List<SqlStatement> statements)
    {
        int index = 0;
        while (index < statements.Count)
        {
            SqlStatement statement = statements[index];
            if (!TargetDialect.SupportsStatement(statement.Kind))
            {
                AddIssue(SqlCompatibilityIssueKind.UnsupportedStatement, statement.Kind.ToString() + " statement", statement.Span);
            }

            index = index + 1;
        }
    }

    internal void AnalyzeQueries(List<SqlQuery> queries)
    {
        int queryIndex = 0;
        while (queryIndex < queries.Count)
        {
            SqlQuery query = queries[queryIndex];
            if (query.CommonTableExpressions.Length > 0 && !TargetDialect.SupportsClause(SqlDialectClauseKind.CommonTableExpression))
            {
                AddIssue(SqlCompatibilityIssueKind.UnsupportedClause, "common table expression clause", query.CommonTableExpressions[0].Span);
            }

            AnalyzeUnsupportedClauses(query.UnsupportedClauses);
            queryIndex = queryIndex + 1;
        }
    }

    internal void AnalyzeSelectCores(List<SqlSelectCore> selectCores)
    {
        int coreIndex = 0;
        while (coreIndex < selectCores.Count)
        {
            SqlSelectCore core = selectCores[coreIndex];
            if (core.Windows.Length > 0 && !TargetDialect.SupportsClause(SqlDialectClauseKind.Window))
            {
                AddIssue(SqlCompatibilityIssueKind.UnsupportedClause, "window clause", core.Windows[0].Span);
            }

            AnalyzeUnsupportedClauses(core.UnsupportedClauses);
            coreIndex = coreIndex + 1;
        }
    }

    internal void AnalyzeTableReferences(List<SqlTableReference> tableReferences)
    {
        int index = 0;
        while (index < tableReferences.Count)
        {
            AnalyzeUnsupportedClauses(tableReferences[index].UnsupportedClauses);
            index = index + 1;
        }
    }

    internal void AnalyzeJoins(List<SqlJoin> joins)
    {
        int index = 0;
        while (index < joins.Count)
        {
            AnalyzeUnsupportedClauses(joins[index].UnsupportedClauses);
            index = index + 1;
        }
    }

    internal void AnalyzeUnsupportedClauses(SqlUnsupportedQueryClause[] clauses)
    {
        int index = 0;
        while (index < clauses.Length)
        {
            SqlUnsupportedQueryClause clause = clauses[index];
            if (!SupportsPreservedClause(clause.Kind))
            {
                string feature = clause.Kind == SqlUnsupportedQueryClauseKind.Other ? clause.SourceText : clause.Kind.ToString() + " clause";
                AddIssue(SqlCompatibilityIssueKind.UnsupportedClause, feature, clause.Span);
            }

            index = index + 1;
        }
    }

    internal bool SupportsPreservedClause(SqlUnsupportedQueryClauseKind kind)
    {
        if (TargetKind == SqlDialectKind.Generic)
        {
            return true;
        }

        if (kind == SqlUnsupportedQueryClauseKind.Qualify)
        {
            return TargetKind == SqlDialectKind.DuckDb || TargetKind == SqlDialectKind.BigQuery || TargetKind == SqlDialectKind.Snowflake || TargetKind == SqlDialectKind.Databricks;
        }

        if (kind == SqlUnsupportedQueryClauseKind.Pivot || kind == SqlUnsupportedQueryClauseKind.Unpivot)
        {
            return TargetKind == SqlDialectKind.SqlServer || TargetKind == SqlDialectKind.Oracle || TargetKind == SqlDialectKind.BigQuery || TargetKind == SqlDialectKind.Snowflake || TargetKind == SqlDialectKind.Databricks;
        }

        if (kind == SqlUnsupportedQueryClauseKind.ConnectBy || kind == SqlUnsupportedQueryClauseKind.Model)
        {
            return TargetKind == SqlDialectKind.Oracle;
        }

        return false;
    }

    internal void AnalyzeReturningClauses(List<SqlReturningClause> clauses)
    {
        if (TargetDialect.SupportsClause(SqlDialectClauseKind.Returning))
        {
            return;
        }

        int index = 0;
        while (index < clauses.Count)
        {
            AddIssue(SqlCompatibilityIssueKind.UnsupportedClause, "RETURNING clause", clauses[index].Span);
            index = index + 1;
        }
    }

    internal void AnalyzeUpsertClauses(List<SqlUpsertClause> clauses)
    {
        if (TargetDialect.SupportsClause(SqlDialectClauseKind.Upsert))
        {
            return;
        }

        int index = 0;
        while (index < clauses.Count)
        {
            AddIssue(SqlCompatibilityIssueKind.UnsupportedClause, "upsert clause", clauses[index].Span);
            index = index + 1;
        }
    }

    internal void AnalyzeExpressions(List<SqlExpression> expressions)
    {
        int index = 0;
        while (index < expressions.Count)
        {
            SqlExpression expression = expressions[index];
            AnalyzeExpressionForm(expression);
            AnalyzeExpressionOperator(expression);
            index = index + 1;
        }
    }

    internal void AnalyzeExpressionForm(SqlExpression expression)
    {
        if (expression.Kind == SqlExpressionKind.Unknown)
        {
            AddIssue(SqlCompatibilityIssueKind.UnsupportedExpression, "unknown expression", expression.Span);
            return;
        }

        if (expression.Kind == SqlExpressionKind.Window && !TargetDialect.SupportsClause(SqlDialectClauseKind.Window))
        {
            AddIssue(SqlCompatibilityIssueKind.UnsupportedExpression, "window expression", expression.Span);
            return;
        }

        if (expression.CastData != null && expression.CastData.SyntaxKind == SqlCastSyntaxKind.PostgreSql && !SupportsPostgreSqlCast())
        {
            AddIssue(SqlCompatibilityIssueKind.UnsupportedExpression, "PostgreSQL cast expression", expression.Span);
            return;
        }

        if ((expression.Kind == SqlExpressionKind.Array || expression.Kind == SqlExpressionKind.Subscript) && !SupportsArrayExpression())
        {
            string feature = expression.Kind == SqlExpressionKind.Array ? "array expression" : "subscript expression";
            AddIssue(SqlCompatibilityIssueKind.UnsupportedExpression, feature, expression.Span);
        }
    }

    internal bool SupportsPostgreSqlCast()
    {
        return TargetKind == SqlDialectKind.Generic || TargetKind == SqlDialectKind.PostgreSql || TargetKind == SqlDialectKind.DuckDb;
    }

    internal bool SupportsArrayExpression()
    {
        return TargetKind == SqlDialectKind.Generic || TargetKind == SqlDialectKind.PostgreSql || TargetKind == SqlDialectKind.DuckDb || TargetKind == SqlDialectKind.BigQuery || TargetKind == SqlDialectKind.Snowflake || TargetKind == SqlDialectKind.Databricks;
    }

    internal void AnalyzeExpressionOperator(SqlExpression expression)
    {
        if (expression.OperatorData != null)
        {
            if (expression.OperatorData.Kind == SqlOperatorExpressionKind.Custom && TargetDialect.GetCustomOperatorPrecedence(expression.OperatorData.OperatorText) == 0)
            {
                AddOperatorIssue(expression.OperatorData.OperatorText, expression);
                return;
            }

            if (expression.OperatorData.Kind == SqlOperatorExpressionKind.Json && !SupportsJsonOperator(expression.OperatorData.OperatorText))
            {
                AddOperatorIssue(expression.OperatorData.OperatorText, expression);
                return;
            }
        }

        if (expression.UnaryOperator != SqlUnaryOperatorKind.None && TargetDialect.GetUnaryPrecedence(expression.UnaryOperator) == 0)
        {
            AddOperatorIssue(GetOperatorText(expression, expression.UnaryOperator.ToString()), expression);
            return;
        }

        if (expression.BinaryOperator != SqlBinaryOperatorKind.None && TargetDialect.GetBinaryPrecedence(expression.BinaryOperator) == 0)
        {
            AddOperatorIssue(GetOperatorText(expression, expression.BinaryOperator.ToString()), expression);
            return;
        }

        if ((expression.Kind == SqlExpressionKind.Collate || expression.Kind == SqlExpressionKind.Is || expression.Kind == SqlExpressionKind.Like || expression.Kind == SqlExpressionKind.Between || expression.Kind == SqlExpressionKind.In) && expression.OperatorText != null && !ContainsOperatorText(expression.OperatorText))
        {
            AddOperatorIssue(expression.OperatorText, expression);
        }
    }

    internal bool SupportsJsonOperator(string operatorText)
    {
        if (TargetDialect.GetCustomOperatorPrecedence(operatorText) > 0)
        {
            return true;
        }

        if (operatorText == "->" || operatorText == "->>")
        {
            return TargetKind == SqlDialectKind.Generic || TargetKind == SqlDialectKind.SQLite || TargetKind == SqlDialectKind.PostgreSql || TargetKind == SqlDialectKind.MySql || TargetKind == SqlDialectKind.MariaDb;
        }

        return false;
    }

    internal bool ContainsOperatorText(string operatorText)
    {
        int index = 0;
        while (index < Operators.Length)
        {
            if (string.Equals(Operators[index].Text, operatorText, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void AddOperatorIssue(string operatorText, SqlExpression expression)
    {
        SqlTextSpan span = expression.OperatorToken == null ? expression.Span : expression.OperatorToken.Span;
        AddIssue(SqlCompatibilityIssueKind.UnsupportedOperator, operatorText + " operator", span);
    }

    internal string GetOperatorText(SqlExpression expression, string fallback)
    {
        if (expression.OperatorText != null && expression.OperatorText.Length > 0)
        {
            return expression.OperatorText;
        }

        return fallback;
    }

    internal void AnalyzeLiterals(List<SqlLiteral> literals)
    {
        int index = 0;
        while (index < literals.Count)
        {
            SqlLiteral literal = literals[index];
            SqlDialectLiteralPolicyKind policyKind = GetLiteralPolicy(literal);
            if (!ContainsLiteralPolicy(policyKind))
            {
                AddIssue(SqlCompatibilityIssueKind.UnsupportedLiteral, GetLiteralFeature(policyKind, literal), literal.Span);
            }

            if (literal.Kind == SqlLiteralKind.String)
            {
                SqlStringQuoteStyle quoteStyle = GetStringQuoteStyle(literal);
                if (quoteStyle != SqlStringQuoteStyle.None && !ContainsStringQuoteStyle(quoteStyle))
                {
                    AddIssue(SqlCompatibilityIssueKind.UnsupportedQuoting, quoteStyle.ToString() + " string quoting", literal.Span);
                }
            }

            index = index + 1;
        }
    }

    internal SqlDialectLiteralPolicyKind GetLiteralPolicy(SqlLiteral literal)
    {
        if (literal.Kind == SqlLiteralKind.Null)
        {
            return SqlDialectLiteralPolicyKind.Null;
        }

        if (literal.Kind == SqlLiteralKind.Integer)
        {
            if (literal.SourceText.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return SqlDialectLiteralPolicyKind.HexInteger;
            }

            return SqlDialectLiteralPolicyKind.Integer;
        }

        if (literal.Kind == SqlLiteralKind.Decimal)
        {
            if (literal.SourceText.Contains("e", StringComparison.OrdinalIgnoreCase))
            {
                return SqlDialectLiteralPolicyKind.Exponent;
            }

            return SqlDialectLiteralPolicyKind.Decimal;
        }

        if (literal.Kind == SqlLiteralKind.String)
        {
            return SqlDialectLiteralPolicyKind.String;
        }

        if (literal.Kind == SqlLiteralKind.Blob)
        {
            return SqlDialectLiteralPolicyKind.Blob;
        }

        if (literal.Kind == SqlLiteralKind.Boolean)
        {
            return SqlDialectLiteralPolicyKind.Boolean;
        }

        if (literal.Kind == SqlLiteralKind.CurrentDate)
        {
            return SqlDialectLiteralPolicyKind.CurrentDate;
        }

        if (literal.Kind == SqlLiteralKind.CurrentTime)
        {
            return SqlDialectLiteralPolicyKind.CurrentTime;
        }

        if (literal.Kind == SqlLiteralKind.CurrentTimestamp)
        {
            return SqlDialectLiteralPolicyKind.CurrentTimestamp;
        }

        return SqlDialectLiteralPolicyKind.None;
    }

    internal bool ContainsLiteralPolicy(SqlDialectLiteralPolicyKind kind)
    {
        int index = 0;
        while (index < LiteralPolicies.Length)
        {
            if (LiteralPolicies[index] == kind && kind != SqlDialectLiteralPolicyKind.None)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal string GetLiteralFeature(SqlDialectLiteralPolicyKind kind, SqlLiteral literal)
    {
        if (kind == SqlDialectLiteralPolicyKind.None)
        {
            return literal.Kind.ToString() + " literal";
        }

        return kind.ToString() + " literal";
    }

    internal SqlStringQuoteStyle GetStringQuoteStyle(SqlLiteral literal)
    {
        if (literal.Token != null)
        {
            return literal.Token.StringQuoteStyle;
        }

        if (literal.SourceText.StartsWith("E'", StringComparison.OrdinalIgnoreCase))
        {
            return SqlStringQuoteStyle.EscapedSingleQuote;
        }

        if (literal.SourceText.StartsWith("U&'", StringComparison.OrdinalIgnoreCase))
        {
            return SqlStringQuoteStyle.UnicodeSingleQuote;
        }

        if (literal.SourceText.StartsWith("$", StringComparison.Ordinal))
        {
            return SqlStringQuoteStyle.DollarQuote;
        }

        if (literal.SourceText.StartsWith("\"", StringComparison.Ordinal))
        {
            return SqlStringQuoteStyle.DoubleQuote;
        }

        if (literal.SourceText.StartsWith("'", StringComparison.Ordinal))
        {
            return SqlStringQuoteStyle.SingleQuote;
        }

        return SqlStringQuoteStyle.None;
    }

    internal bool ContainsStringQuoteStyle(SqlStringQuoteStyle style)
    {
        int index = 0;
        while (index < StringQuoteStyles.Length)
        {
            if (StringQuoteStyles[index] == style)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void AnalyzeNames(List<SqlName> names)
    {
        int nameIndex = 0;
        while (nameIndex < names.Count)
        {
            SqlName name = names[nameIndex];
            int partIndex = 0;
            while (partIndex < name.Parts.Length)
            {
                SqlNamePart part = name.Parts[partIndex];
                if (part.QuoteStyle != SqlNameQuoteStyle.None && !ContainsIdentifierQuoteStyle(part.QuoteStyle))
                {
                    AddIssue(SqlCompatibilityIssueKind.UnsupportedQuoting, part.QuoteStyle.ToString() + " identifier quoting", part.Span);
                }

                partIndex = partIndex + 1;
            }

            nameIndex = nameIndex + 1;
        }
    }

    internal bool ContainsIdentifierQuoteStyle(SqlNameQuoteStyle style)
    {
        int index = 0;
        while (index < IdentifierQuoteStyles.Length)
        {
            if (IdentifierQuoteStyles[index] == style)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void AnalyzeUnsupportedSyntax(List<SqlUnsupportedSyntax> syntaxItems)
    {
        int index = 0;
        while (index < syntaxItems.Count)
        {
            SqlUnsupportedSyntax syntax = syntaxItems[index];
            if (ShouldReportUnsupportedSyntax(syntax))
            {
                AddIssue(SqlCompatibilityIssueKind.UnsupportedSyntax, syntax.Description, syntax.Span);
            }

            index = index + 1;
        }
    }

    internal bool ShouldReportUnsupportedSyntax(SqlUnsupportedSyntax syntax)
    {
        if (syntax.Kind == SqlUnsupportedSyntaxKind.SkippedText)
        {
            return true;
        }

        if (syntax.Kind == SqlUnsupportedSyntaxKind.UnknownStatement)
        {
            return false;
        }

        if (syntax.Kind != SqlUnsupportedSyntaxKind.DialectFeature)
        {
            return true;
        }

        if (syntax.Description.Contains("RETURNING", StringComparison.OrdinalIgnoreCase))
        {
            return !TargetDialect.SupportsClause(SqlDialectClauseKind.Returning);
        }

        if (syntax.Description.Contains("UPSERT", StringComparison.OrdinalIgnoreCase))
        {
            return !TargetDialect.SupportsClause(SqlDialectClauseKind.Upsert);
        }

        if (syntax.Description.Contains("window", StringComparison.OrdinalIgnoreCase))
        {
            return !TargetDialect.SupportsClause(SqlDialectClauseKind.Window);
        }

        if (syntax.Description.Contains("common table", StringComparison.OrdinalIgnoreCase))
        {
            return !TargetDialect.SupportsClause(SqlDialectClauseKind.CommonTableExpression);
        }

        if (syntax.Description.Contains("statement", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    internal void AddIssue(SqlCompatibilityIssueKind kind, string feature, SqlTextSpan span)
    {
        if (HasIssue(kind, feature, span))
        {
            return;
        }

        string code = GetDiagnosticCode(kind);
        string message = "Target dialect " + TargetKind.ToString() + " does not support " + feature + ".";
        SqlDiagnostic diagnostic = Diagnostics.AddDiagnosticWithDetails(SqlDiagnosticSeverity.Error, code, message, span, SourceText);
        Issues.Add(new SqlCompatibilityIssue(kind, feature, span, diagnostic));
    }

    internal bool HasIssue(SqlCompatibilityIssueKind kind, string feature, SqlTextSpan span)
    {
        int index = 0;
        while (index < Issues.Count)
        {
            SqlCompatibilityIssue issue = Issues[index];
            if (issue.Kind == kind && issue.Feature == feature && issue.Span.Start == span.Start && issue.Span.Length == span.Length)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal string GetDiagnosticCode(SqlCompatibilityIssueKind kind)
    {
        if (kind == SqlCompatibilityIssueKind.UnsupportedStatement)
        {
            return "SQLCOMP001";
        }

        if (kind == SqlCompatibilityIssueKind.UnsupportedClause)
        {
            return "SQLCOMP002";
        }

        if (kind == SqlCompatibilityIssueKind.UnsupportedExpression)
        {
            return "SQLCOMP003";
        }

        if (kind == SqlCompatibilityIssueKind.UnsupportedLiteral)
        {
            return "SQLCOMP004";
        }

        if (kind == SqlCompatibilityIssueKind.UnsupportedQuoting)
        {
            return "SQLCOMP005";
        }

        if (kind == SqlCompatibilityIssueKind.UnsupportedOperator)
        {
            return "SQLCOMP006";
        }

        return "SQLCOMP007";
    }

    internal SqlCompatibilityResult CreateResult()
    {
        Issues.Sort((left, right) => left.Span.Start.CompareTo(right.Span.Start));
        SqlDiagnosticBag sortedDiagnostics = new SqlDiagnosticBag();
        int index = 0;
        while (index < Issues.Count)
        {
            sortedDiagnostics.Add(Issues[index].Diagnostic);
            index = index + 1;
        }

        return new SqlCompatibilityResult(Script, TargetDialect, Issues.ToArray(), sortedDiagnostics);
    }
}
