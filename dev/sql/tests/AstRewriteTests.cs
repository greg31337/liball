using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class AstRewriteTests
{
    [Test]
    public void ReplaceResultStatementAt_PreservesUntouchedTreeAndMetadataByDefault()
    {
        SqlParseResult original = SqlParser.ParseScript("-- lead\nselect 1; select #", SqlParseOptions.CreateSqlite());
        SqlStatement replacement = SqlParser.ParseStatement("select 2", SqlParseOptions.CreateSqlite()).Statement;
        SqlStatement untouched = original.Script.Statements[0];
        SqlStatement replaced = original.Script.Statements[1];
        ISqlAstRewriter rewriter = new SqlAstRewriteService();

        SqlParseResult rewritten = rewriter.ReplaceResultStatementAt(original, 1, replacement, new SqlRewriteOptions());

        Assert.That(rewritten, Is.Not.SameAs(original));
        Assert.That(rewritten.Script, Is.Not.SameAs(original.Script));
        Assert.That(rewritten.Script.Statements, Is.Not.SameAs(original.Script.Statements));
        Assert.That(rewritten.Script.Statements[0], Is.SameAs(untouched));
        Assert.That(rewritten.Script.Statements[1], Is.SameAs(replacement));
        Assert.That(original.Script.Statements[1], Is.SameAs(replaced));
        Assert.That(rewritten.Script.Span, Is.SameAs(original.Script.Span));
        Assert.That(rewritten.Script.Tokens, Is.SameAs(original.Script.Tokens));
        Assert.That(rewritten.Script.LeadingTrivia, Is.SameAs(original.Script.LeadingTrivia));
        Assert.That(rewritten.Script.SourceText, Is.EqualTo(original.Script.SourceText));
        Assert.That(rewritten.Tokens, Is.SameAs(original.Tokens));
        Assert.That(rewritten.Diagnostics, Is.SameAs(original.Diagnostics));
        Assert.That(rewritten.SourceText, Is.SameAs(original.SourceText));
        Assert.That(rewritten.Options, Is.SameAs(original.Options));
    }

    [Test]
    public void ReplaceResultStatementAt_CanDiscardEachMetadataCategory()
    {
        SqlParseResult original = SqlParser.ParseScript("-- lead\nselect #;", SqlParseOptions.CreateSqlite());
        SqlStatement replacement = SqlParser.ParseStatement("select 2", SqlParseOptions.CreateSqlite()).Statement;
        SqlRewriteOptions options = new SqlRewriteOptions();
        options.PreserveSpans = false;
        options.PreserveTokens = false;
        options.PreserveTrivia = false;
        options.PreserveSourceText = false;
        options.PreserveDiagnostics = false;

        SqlParseResult rewritten = new SqlAstRewriteService().ReplaceResultStatementAt(original, 0, replacement, options);

        Assert.That(rewritten.Script.Span.Start, Is.EqualTo(0));
        Assert.That(rewritten.Script.Span.Length, Is.EqualTo(0));
        Assert.That(rewritten.Script.Tokens, Is.Empty);
        Assert.That(rewritten.Script.TokenRange.Length, Is.EqualTo(0));
        Assert.That(rewritten.Script.LeadingTrivia, Is.Empty);
        Assert.That(rewritten.Script.TrailingTrivia, Is.Empty);
        Assert.That(rewritten.Script.SourceText, Is.Empty);
        Assert.That(rewritten.Tokens, Is.Empty);
        Assert.That(rewritten.Diagnostics, Is.Not.SameAs(original.Diagnostics));
        Assert.That(rewritten.Diagnostics.IsEmpty(), Is.True);
        Assert.That(rewritten.SourceText, Is.Null);
    }

    [Test]
    public void ReplaceStatementPayload_UsesNewPayloadAndOriginalMetadata()
    {
        SqlStatement original = SqlParser.ParseStatement("select 'old'", SqlParseOptions.CreateSqlite()).Statement;
        SqlStatement payloadSource = SqlParser.ParseStatement("pragma cache_size", SqlParseOptions.CreateSqlite()).Statement;
        SqlAstRewriteService rewriter = new SqlAstRewriteService();

        SqlStatement rewritten = rewriter.ReplaceStatementPayload(original, payloadSource, new SqlRewriteOptions());

        Assert.That(rewritten, Is.Not.SameAs(original));
        Assert.That(rewritten.Kind, Is.EqualTo(payloadSource.Kind));
        Assert.That(rewritten.Pragma, Is.SameAs(payloadSource.Pragma));
        Assert.That(rewritten.Query, Is.Null);
        Assert.That(rewritten.Span, Is.SameAs(original.Span));
        Assert.That(rewritten.Tokens, Is.SameAs(original.Tokens));
        Assert.That(rewritten.TokenRange, Is.SameAs(original.TokenRange));
        Assert.That(rewritten.SourceText, Is.EqualTo(original.SourceText));
        Assert.That(original.Query, Is.Not.Null);
    }

    [Test]
    public void ReplaceQuerySelectCoreAt_UpdatesCompoundMirrorOnlyAtRequestedIndex()
    {
        SqlQuery original = ParseQuery("select 1 union all select 2 except select 3");
        SqlSelectCore replacement = ParseQuery("select 4").SelectCores[0];
        SqlSelectCore untouchedFirst = original.SelectCores[0];
        SqlSelectCore untouchedLast = original.SelectCores[2];

        SqlQuery rewritten = new SqlAstRewriteService().ReplaceQuerySelectCoreAt(original, 1, replacement, new SqlRewriteOptions());

        Assert.That(rewritten, Is.Not.SameAs(original));
        Assert.That(rewritten.SelectCores, Is.Not.SameAs(original.SelectCores));
        Assert.That(rewritten.SelectCores[0], Is.SameAs(untouchedFirst));
        Assert.That(rewritten.SelectCores[1], Is.SameAs(replacement));
        Assert.That(rewritten.SelectCores[2], Is.SameAs(untouchedLast));
        Assert.That(rewritten.CompoundParts, Is.Not.SameAs(original.CompoundParts));
        Assert.That(rewritten.CompoundParts[0].Core, Is.SameAs(replacement));
        Assert.That(rewritten.CompoundParts[1], Is.SameAs(original.CompoundParts[1]));
        Assert.That(rewritten.CommonTableExpressions, Is.SameAs(original.CommonTableExpressions));
        Assert.That(rewritten.OrderBy, Is.SameAs(original.OrderBy));
        Assert.That(rewritten.Span, Is.SameAs(original.Span));
        Assert.That(original.SelectCores[1], Is.Not.SameAs(replacement));
    }

    [Test]
    public void TableRewriteOperations_UpdateMirrorsAndPreserveOtherChildren()
    {
        SqlTextSpan span = new SqlTextSpan(4, 20);
        SqlQuery existingQuery = ParseQuery("select 1");
        SqlQuery replacementQuery = ParseQuery("select 2");
        SqlTableReference leaf = new SqlTableReference(SqlTableReferenceKind.Named, span, null, null, null, null, new SqlJoin[0], null, new SqlName[0]);
        SqlJoin oldJoin = new SqlJoin(SqlJoinKind.Inner, false, false, leaf, SqlJoinConstraintKind.None, null, new SqlName[0], span);
        SqlJoin newJoin = new SqlJoin(SqlJoinKind.Cross, false, false, leaf, SqlJoinConstraintKind.None, null, new SqlName[0], span);
        SqlTableReference table = new SqlTableReference(SqlTableReferenceKind.Subquery, span, null, existingQuery, null, leaf, new SqlJoin[] { oldJoin }, null, new SqlName[0]);
        table.SourceData = new SqlTableSourceData(null, new SqlExpression[0], new SqlExpression[0][], existingQuery, true);
        SqlAstRewriteService rewriter = new SqlAstRewriteService();

        SqlTableReference queryRewrite = rewriter.ReplaceTableQuery(table, existingQuery, replacementQuery, new SqlRewriteOptions());
        SqlTableReference innerRewrite = rewriter.ReplaceTableInner(table, queryRewrite, new SqlRewriteOptions());
        SqlTableReference joinRewrite = rewriter.ReplaceTableJoinAt(table, 0, newJoin, new SqlRewriteOptions());

        Assert.That(queryRewrite.Query, Is.SameAs(replacementQuery));
        Assert.That(queryRewrite.SourceData, Is.Not.SameAs(table.SourceData));
        Assert.That(queryRewrite.SourceData!.Query, Is.SameAs(replacementQuery));
        Assert.That(queryRewrite.SourceData.Values, Is.SameAs(table.SourceData!.Values));
        Assert.That(queryRewrite.Inner, Is.SameAs(leaf));
        Assert.That(queryRewrite.Joins, Is.SameAs(table.Joins));
        Assert.That(innerRewrite.Inner, Is.SameAs(queryRewrite));
        Assert.That(innerRewrite.Query, Is.SameAs(existingQuery));
        Assert.That(joinRewrite.Joins, Is.Not.SameAs(table.Joins));
        Assert.That(joinRewrite.Joins[0], Is.SameAs(newJoin));
        Assert.That(joinRewrite.Query, Is.SameAs(existingQuery));
        Assert.That(table.Query, Is.SameAs(existingQuery));
    }

    [Test]
    public void ReplaceTableFunction_UpdatesTypedFunctionSourceMirror()
    {
        SqlTextSpan span = new SqlTextSpan(0, 1);
        SqlName name = new SqlName(new SqlNamePart[0], span);
        SqlExpression argument = new SqlExpression(SqlExpressionKind.Literal, span, new SqlToken[0], new SqlExpression[0]);
        SqlExpression originalFunction = new SqlExpression(SqlExpressionKind.FunctionCall, span, new SqlToken[0], new SqlExpression[] { argument });
        originalFunction.FunctionData = new SqlFunctionInvocationData(name, originalFunction.Arguments, false, new SqlOrderingTerm[0], null, null, null);
        SqlExpression replacementFunction = new SqlExpression(SqlExpressionKind.FunctionCall, span, new SqlToken[0], new SqlExpression[0]);
        replacementFunction.FunctionData = new SqlFunctionInvocationData(name, replacementFunction.Arguments, false, new SqlOrderingTerm[0], null, null, null);
        SqlTableReference table = new SqlTableReference(SqlTableReferenceKind.Function, span, null, null, originalFunction, null, new SqlJoin[0], null, new SqlName[0]);
        table.SourceData = new SqlTableSourceData(name, originalFunction.Arguments, new SqlExpression[0][], null, false);

        SqlTableReference rewritten = new SqlAstRewriteService().ReplaceTableFunction(table, replacementFunction, new SqlRewriteOptions());

        Assert.That(rewritten.FunctionExpression, Is.SameAs(replacementFunction));
        Assert.That(rewritten.SourceData, Is.Not.SameAs(table.SourceData));
        Assert.That(rewritten.SourceData!.Name, Is.SameAs(replacementFunction.FunctionData.Name));
        Assert.That(rewritten.SourceData.FunctionArguments, Is.SameAs(replacementFunction.FunctionData.Arguments));
        Assert.That(table.FunctionExpression, Is.SameAs(originalFunction));
    }

    [Test]
    public void ReplaceExpressionChild_UpdatesEveryMatchingDirectFieldAndTypedMirror()
    {
        SqlTextSpan span = new SqlTextSpan(2, 8);
        SqlToken token = new SqlToken(SqlTokenKind.Identifier, new SqlTextSpan(2, 5), "value");
        SqlExpression existing = new SqlExpression(SqlExpressionKind.Name, span, new SqlToken[0], new SqlExpression[0]);
        SqlExpression replacement = new SqlExpression(SqlExpressionKind.Literal, span, new SqlToken[0], new SqlExpression[0]);
        SqlExpression untouched = new SqlExpression(SqlExpressionKind.Name, span, new SqlToken[0], new SqlExpression[0]);
        SqlName name = new SqlName(new SqlNamePart[0], span);
        SqlExpression root = new SqlExpression(SqlExpressionKind.FunctionCall, span, new SqlToken[] { token }, new SqlExpression[] { existing, untouched });
        root.Left = existing;
        root.Right = untouched;
        root.Filter = existing;
        root.PatternData = new SqlPatternExpressionData(existing, untouched, existing, "like", false);
        root.InData = new SqlInExpressionData(existing, false, SqlInTargetKind.List, new SqlExpression[] { untouched, existing }, null, null, new SqlExpression[] { existing });
        root.FunctionData = new SqlFunctionInvocationData(name, new SqlExpression[] { existing, untouched }, false, new SqlOrderingTerm[0], existing, null, null);

        SqlExpression rewritten = new SqlAstRewriteService().ReplaceExpressionChild(root, existing, replacement, new SqlRewriteOptions());

        Assert.That(rewritten.Arguments, Is.Not.SameAs(root.Arguments));
        Assert.That(rewritten.Arguments[0], Is.SameAs(replacement));
        Assert.That(rewritten.Arguments[1], Is.SameAs(untouched));
        Assert.That(rewritten.Left, Is.SameAs(replacement));
        Assert.That(rewritten.Right, Is.SameAs(untouched));
        Assert.That(rewritten.Filter, Is.SameAs(replacement));
        Assert.That(rewritten.PatternData!.Value, Is.SameAs(replacement));
        Assert.That(rewritten.PatternData.Escape, Is.SameAs(replacement));
        Assert.That(rewritten.InData!.Operand, Is.SameAs(replacement));
        Assert.That(rewritten.InData.Values[1], Is.SameAs(replacement));
        Assert.That(rewritten.InData.FunctionArguments[0], Is.SameAs(replacement));
        Assert.That(rewritten.FunctionData!.Arguments[0], Is.SameAs(replacement));
        Assert.That(rewritten.FunctionData.Filter, Is.SameAs(replacement));
        Assert.That(rewritten.Span, Is.SameAs(root.Span));
        Assert.That(rewritten.Tokens, Is.SameAs(root.Tokens));
        Assert.That(root.Left, Is.SameAs(existing));
        Assert.That(root.PatternData.Value, Is.SameAs(existing));
    }

    [Test]
    public void ReplaceExpressionChild_UpdatesAllRemainingDirectFieldsAndTypedPayloads()
    {
        SqlTextSpan span = new SqlTextSpan(0, 1);
        SqlExpression existing = new SqlExpression(SqlExpressionKind.Name, span, new SqlToken[0], new SqlExpression[0]);
        SqlExpression replacement = new SqlExpression(SqlExpressionKind.Literal, span, new SqlToken[0], new SqlExpression[0]);
        SqlExpression untouched = new SqlExpression(SqlExpressionKind.Name, span, new SqlToken[0], new SqlExpression[0]);
        SqlName collation = new SqlName(new SqlNamePart[0], span);
        SqlExpression root = new SqlExpression(SqlExpressionKind.Case, span, new SqlToken[0], new SqlExpression[0]);
        root.Operand = existing;
        root.Alternate = existing;
        root.CollateData = new SqlCollateExpressionData(existing, collation);
        root.DistinctnessData = new SqlDistinctnessExpressionData(existing, untouched, SqlDistinctnessKind.IsDistinctFrom);
        root.ExtractData = new SqlExtractExpressionData("year", existing);
        root.CastData = new SqlCastExpressionData(existing, "integer", SqlCastSyntaxKind.CastFunction);
        root.ArrayData = new SqlArrayExpressionData(new SqlExpression[] { untouched, existing }, null);
        root.SubscriptData = new SqlSubscriptExpressionData(existing, existing, untouched, true);
        root.OperatorData = new SqlOperatorExpressionData(existing, untouched, "->", 5, SqlOperatorExpressionKind.Json);

        SqlExpression rewritten = new SqlAstRewriteService().ReplaceExpressionChild(root, existing, replacement, new SqlRewriteOptions());

        Assert.That(rewritten.Operand, Is.SameAs(replacement));
        Assert.That(rewritten.Alternate, Is.SameAs(replacement));
        Assert.That(rewritten.CollateData!.Operand, Is.SameAs(replacement));
        Assert.That(rewritten.DistinctnessData!.Left, Is.SameAs(replacement));
        Assert.That(rewritten.DistinctnessData.Right, Is.SameAs(untouched));
        Assert.That(rewritten.ExtractData!.Source, Is.SameAs(replacement));
        Assert.That(rewritten.CastData!.Operand, Is.SameAs(replacement));
        Assert.That(rewritten.ArrayData!.Elements[0], Is.SameAs(untouched));
        Assert.That(rewritten.ArrayData.Elements[1], Is.SameAs(replacement));
        Assert.That(rewritten.SubscriptData!.Target, Is.SameAs(replacement));
        Assert.That(rewritten.SubscriptData.LowerBound, Is.SameAs(replacement));
        Assert.That(rewritten.SubscriptData.UpperBound, Is.SameAs(untouched));
        Assert.That(rewritten.OperatorData!.Left, Is.SameAs(replacement));
        Assert.That(rewritten.OperatorData.Right, Is.SameAs(untouched));
        Assert.That(root.Operand, Is.SameAs(existing));
        Assert.That(root.CollateData.Operand, Is.SameAs(existing));
        Assert.That(root.ArrayData.Elements[1], Is.SameAs(existing));
    }

    [Test]
    public void RewriteOptions_AreAppliedToEveryCopiedRootWithoutMutatingInputs()
    {
        SqlParseResult parseResult = SqlParser.ParseScript("-- lead\nselect 1 + 2 union select 3;", SqlParseOptions.CreateSqlite());
        SqlScript script = parseResult.Script;
        SqlStatement statement = script.Statements[0];
        SqlQuery query = statement.Query!;
        SqlTableReference table = SqlParser.ParseStatement("select 1 from (items)", SqlParseOptions.CreateSqlite()).Statement.Query!.SelectCores[0].From[0];
        SqlExpression expression = query.SelectCores[0].ResultColumns[0].Expression!;
        SqlRewriteOptions options = new SqlRewriteOptions();
        options.PreserveSpans = false;
        options.PreserveTokens = false;
        options.PreserveTrivia = false;
        options.PreserveSourceText = false;
        SqlAstRewriteService rewriter = new SqlAstRewriteService();

        SqlScript rewrittenScript = rewriter.ReplaceScriptStatementAt(script, 0, statement, options);
        SqlStatement rewrittenStatement = rewriter.ReplaceStatementPayload(statement, statement, options);
        SqlQuery rewrittenQuery = rewriter.ReplaceQuerySelectCoreAt(query, 1, query.SelectCores[0], options);
        SqlTableReference rewrittenTable = rewriter.ReplaceTableInner(table, table.Inner!, options);
        SqlExpression rewrittenExpression = rewriter.ReplaceExpressionChild(expression, expression.Left!, expression.Left!, options);

        Assert.That(rewrittenScript.Span.Length, Is.EqualTo(0));
        Assert.That(rewrittenScript.Tokens, Is.Empty);
        Assert.That(rewrittenScript.LeadingTrivia, Is.Empty);
        Assert.That(rewrittenScript.SourceText, Is.Empty);
        Assert.That(rewrittenStatement.Span.Length, Is.EqualTo(0));
        Assert.That(rewrittenStatement.Tokens, Is.Empty);
        Assert.That(rewrittenStatement.SourceText, Is.Empty);
        Assert.That(rewrittenQuery.Span.Length, Is.EqualTo(0));
        Assert.That(rewrittenTable.Span.Length, Is.EqualTo(0));
        Assert.That(rewrittenExpression.Span.Length, Is.EqualTo(0));
        Assert.That(rewrittenExpression.Tokens, Is.Empty);
        Assert.That(script.Span.Length, Is.GreaterThan(0));
        Assert.That(statement.Tokens, Is.Not.Empty);
        Assert.That(query.Span.Length, Is.GreaterThan(0));
        Assert.That(table.Span.Length, Is.GreaterThan(0));
        Assert.That(expression.Tokens, Is.Not.Empty);
    }

    [Test]
    public void ReplaceExpressionQuery_UpdatesEveryMatchingTypedQueryMirror()
    {
        SqlTextSpan span = new SqlTextSpan(0, 1);
        SqlQuery existing = ParseQuery("select 1");
        SqlQuery replacement = ParseQuery("select 2");
        SqlExpression operand = new SqlExpression(SqlExpressionKind.Literal, span, new SqlToken[0], new SqlExpression[0]);
        SqlExpression root = new SqlExpression(SqlExpressionKind.Subquery, span, new SqlToken[0], new SqlExpression[0]);
        root.Query = existing;
        root.InData = new SqlInExpressionData(operand, false, SqlInTargetKind.Subquery, new SqlExpression[0], existing, null, new SqlExpression[0]);
        root.ArrayData = new SqlArrayExpressionData(new SqlExpression[] { operand }, existing);

        SqlExpression rewritten = new SqlAstRewriteService().ReplaceExpressionQuery(root, existing, replacement, new SqlRewriteOptions());

        Assert.That(rewritten.Query, Is.SameAs(replacement));
        Assert.That(rewritten.InData!.Query, Is.SameAs(replacement));
        Assert.That(rewritten.ArrayData!.Query, Is.SameAs(replacement));
        Assert.That(rewritten.InData.Operand, Is.SameAs(operand));
        Assert.That(rewritten.ArrayData.Elements, Is.SameAs(root.ArrayData.Elements));
        Assert.That(root.Query, Is.SameAs(existing));
    }

    [Test]
    public void RewriteOperations_RejectInvalidTargets()
    {
        SqlAstRewriteService rewriter = new SqlAstRewriteService();
        SqlParseResult result = SqlParser.ParseScript("select 1", SqlParseOptions.CreateSqlite());
        SqlExpression root = result.Script.Statements[0].Query!.SelectCores[0].ResultColumns[0].Expression!;
        SqlExpression other = new SqlExpression(SqlExpressionKind.Literal, new SqlTextSpan(0, 0), new SqlToken[0], new SqlExpression[0]);

        Assert.Throws<ArgumentOutOfRangeException>(() => rewriter.ReplaceScriptStatementAt(result.Script, 1, result.Script.Statements[0], new SqlRewriteOptions()));
        Assert.Throws<ArgumentException>(() => rewriter.ReplaceExpressionChild(root, other, root, new SqlRewriteOptions()));
        Assert.Throws<ArgumentNullException>(() => rewriter.ReplaceStatementPayload(result.Script.Statements[0], result.Script.Statements[0], null!));
    }

    static SqlQuery ParseQuery(string text)
    {
        return new SqlParserService().ParseQuery(text, SqlParseOptions.CreateSqlite()).Query;
    }
}
