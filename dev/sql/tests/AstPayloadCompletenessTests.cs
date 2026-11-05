using NUnit.Framework;

namespace sql.Tests;

[TestFixture]
public class AstPayloadCompletenessTests
{
    [Test]
    public void ExpressionPayloads_StoreEveryTypedRelationship()
    {
        SqlExpression left = Expression();
        SqlExpression right = Expression();
        SqlExpression escape = Expression();
        SqlName name = Name("payload");
        SqlQuery query = Query();
        SqlOrderingTerm ordering = Ordering(left);
        SqlWindowDefinition window = new SqlWindowDefinition(name, name, new SqlExpression[] { left }, new SqlOrderingTerm[] { ordering }, null, Span());

        SqlCollateExpressionData collate = new SqlCollateExpressionData(left, name);
        SqlPatternExpressionData pattern = new SqlPatternExpressionData(left, right, escape, "ILIKE", true);
        SqlDistinctnessExpressionData distinctness = new SqlDistinctnessExpressionData(left, right, SqlDistinctnessKind.IsNotDistinctFrom);
        SqlInExpressionData inData = new SqlInExpressionData(left, true, SqlInTargetKind.Function, new SqlExpression[] { right }, query, name, new SqlExpression[] { escape });
        SqlExtractExpressionData extract = new SqlExtractExpressionData("YEAR", left);
        SqlCastExpressionData cast = new SqlCastExpressionData(left, "integer", SqlCastSyntaxKind.PostgreSql);
        SqlArrayExpressionData array = new SqlArrayExpressionData(new SqlExpression[] { left, right }, query);
        SqlSubscriptExpressionData subscript = new SqlSubscriptExpressionData(left, right, escape, true);
        SqlOperatorExpressionData customOperator = new SqlOperatorExpressionData(left, right, "#>", 50, SqlOperatorExpressionKind.Json);
        SqlWildcardExpressionData wildcard = new SqlWildcardExpressionData(name);
        SqlFunctionInvocationData function = new SqlFunctionInvocationData(name, new SqlExpression[] { left }, true, new SqlOrderingTerm[] { ordering }, right, name, window);

        Assert.That(collate.Operand, Is.SameAs(left));
        Assert.That(collate.Collation, Is.SameAs(name));
        Assert.That(pattern.Value, Is.SameAs(left));
        Assert.That(pattern.Pattern, Is.SameAs(right));
        Assert.That(pattern.Escape, Is.SameAs(escape));
        Assert.That(pattern.OperatorText, Is.EqualTo("ILIKE"));
        Assert.That(pattern.IsNegated, Is.True);
        Assert.That(distinctness.Left, Is.SameAs(left));
        Assert.That(distinctness.Right, Is.SameAs(right));
        Assert.That(distinctness.Kind, Is.EqualTo(SqlDistinctnessKind.IsNotDistinctFrom));
        Assert.That(inData.Operand, Is.SameAs(left));
        Assert.That(inData.IsNegated, Is.True);
        Assert.That(inData.TargetKind, Is.EqualTo(SqlInTargetKind.Function));
        Assert.That(inData.Values[0], Is.SameAs(right));
        Assert.That(inData.Query, Is.SameAs(query));
        Assert.That(inData.TargetName, Is.SameAs(name));
        Assert.That(inData.FunctionArguments[0], Is.SameAs(escape));
        Assert.That(extract.Field, Is.EqualTo("YEAR"));
        Assert.That(extract.Source, Is.SameAs(left));
        Assert.That(cast.Operand, Is.SameAs(left));
        Assert.That(cast.TypeName, Is.EqualTo("integer"));
        Assert.That(cast.SyntaxKind, Is.EqualTo(SqlCastSyntaxKind.PostgreSql));
        Assert.That(array.Elements, Is.EqualTo(new SqlExpression[] { left, right }));
        Assert.That(array.Query, Is.SameAs(query));
        Assert.That(subscript.Target, Is.SameAs(left));
        Assert.That(subscript.LowerBound, Is.SameAs(right));
        Assert.That(subscript.UpperBound, Is.SameAs(escape));
        Assert.That(subscript.IsSlice, Is.True);
        Assert.That(customOperator.Left, Is.SameAs(left));
        Assert.That(customOperator.Right, Is.SameAs(right));
        Assert.That(customOperator.OperatorText, Is.EqualTo("#>"));
        Assert.That(customOperator.Precedence, Is.EqualTo(50));
        Assert.That(customOperator.Kind, Is.EqualTo(SqlOperatorExpressionKind.Json));
        Assert.That(wildcard.Qualifier, Is.SameAs(name));
        Assert.That(function.Name, Is.SameAs(name));
        Assert.That(function.Arguments[0], Is.SameAs(left));
        Assert.That(function.IsDistinct, Is.True);
        Assert.That(function.OrderBy[0], Is.SameAs(ordering));
        Assert.That(function.Filter, Is.SameAs(right));
        Assert.That(function.WindowName, Is.SameAs(name));
        Assert.That(function.WindowDefinition, Is.SameAs(window));
    }

    [Test]
    public void ExpressionPayloads_RejectEveryRequiredNullAndInvalidPrecedence()
    {
        SqlExpression expression = Expression();
        SqlName name = Name("x");

        Assert.Throws<ArgumentNullException>(() => new SqlCollateExpressionData(null!, name));
        Assert.Throws<ArgumentNullException>(() => new SqlCollateExpressionData(expression, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlPatternExpressionData(null!, expression, null, "LIKE", false));
        Assert.Throws<ArgumentNullException>(() => new SqlPatternExpressionData(expression, null!, null, "LIKE", false));
        Assert.Throws<ArgumentNullException>(() => new SqlPatternExpressionData(expression, expression, null, null!, false));
        Assert.Throws<ArgumentNullException>(() => new SqlDistinctnessExpressionData(null!, expression, SqlDistinctnessKind.IsDistinctFrom));
        Assert.Throws<ArgumentNullException>(() => new SqlDistinctnessExpressionData(expression, null!, SqlDistinctnessKind.IsDistinctFrom));
        Assert.Throws<ArgumentNullException>(() => new SqlInExpressionData(null!, false, SqlInTargetKind.List, new SqlExpression[0], null, null, new SqlExpression[0]));
        Assert.Throws<ArgumentNullException>(() => new SqlInExpressionData(expression, false, SqlInTargetKind.List, null!, null, null, new SqlExpression[0]));
        Assert.Throws<ArgumentNullException>(() => new SqlInExpressionData(expression, false, SqlInTargetKind.List, new SqlExpression[0], null, null, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlExtractExpressionData(null!, expression));
        Assert.Throws<ArgumentNullException>(() => new SqlExtractExpressionData("YEAR", null!));
        Assert.Throws<ArgumentNullException>(() => new SqlCastExpressionData(null!, "integer", SqlCastSyntaxKind.CastFunction));
        Assert.Throws<ArgumentNullException>(() => new SqlCastExpressionData(expression, null!, SqlCastSyntaxKind.CastFunction));
        Assert.Throws<ArgumentNullException>(() => new SqlArrayExpressionData(null!, null));
        Assert.Throws<ArgumentNullException>(() => new SqlSubscriptExpressionData(null!, null, null, false));
        Assert.Throws<ArgumentNullException>(() => new SqlOperatorExpressionData(null!, expression, "+", 1, SqlOperatorExpressionKind.Custom));
        Assert.Throws<ArgumentNullException>(() => new SqlOperatorExpressionData(expression, null!, "+", 1, SqlOperatorExpressionKind.Custom));
        Assert.Throws<ArgumentNullException>(() => new SqlOperatorExpressionData(expression, expression, null!, 1, SqlOperatorExpressionKind.Custom));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SqlOperatorExpressionData(expression, expression, "+", 0, SqlOperatorExpressionKind.Custom));
        Assert.Throws<ArgumentNullException>(() => new SqlFunctionInvocationData(null!, new SqlExpression[0], false, new SqlOrderingTerm[0], null, null, null));
        Assert.Throws<ArgumentNullException>(() => new SqlFunctionInvocationData(name, null!, false, new SqlOrderingTerm[0], null, null, null));
        Assert.Throws<ArgumentNullException>(() => new SqlFunctionInvocationData(name, new SqlExpression[0], false, null!, null, null, null));
    }

    [Test]
    public void QueryPayloads_StoreEveryTypedRelationship()
    {
        SqlExpression expression = Expression();
        SqlSelectCore core = Core();
        SqlTextSpan span = Span();
        SqlToken token = new SqlToken(SqlTokenKind.Keyword, span, "");
        SqlName name = Name("source");
        SqlQuery query = Query();

        SqlCompoundQueryPart compound = new SqlCompoundQueryPart(SqlCompoundOperatorKind.UnionAll, core, span);
        SqlTopClause top = new SqlTopClause(expression, true, true, true, span);
        SqlOffsetFetchClause offsetFetch = new SqlOffsetFetchClause(expression, expression, SqlFetchDirectionKind.Next, true, span);
        SqlUnsupportedQueryClause unsupported = new SqlUnsupportedQueryClause(SqlUnsupportedQueryClauseKind.Qualify, "", span, new SqlToken[] { token });
        SqlTableSourceData source = new SqlTableSourceData(name, new SqlExpression[] { expression }, new SqlExpression[][] { new SqlExpression[] { expression } }, query, true);

        Assert.That(compound.OperatorKind, Is.EqualTo(SqlCompoundOperatorKind.UnionAll));
        Assert.That(compound.Core, Is.SameAs(core));
        Assert.That(compound.Span, Is.SameAs(span));
        Assert.That(top.Quantity, Is.SameAs(expression));
        Assert.That(top.IsPercent, Is.True);
        Assert.That(top.WithTies, Is.True);
        Assert.That(top.IsParenthesized, Is.True);
        Assert.That(top.Span, Is.SameAs(span));
        Assert.That(offsetFetch.Offset, Is.SameAs(expression));
        Assert.That(offsetFetch.Fetch, Is.SameAs(expression));
        Assert.That(offsetFetch.Direction, Is.EqualTo(SqlFetchDirectionKind.Next));
        Assert.That(offsetFetch.WithTies, Is.True);
        Assert.That(offsetFetch.Span, Is.SameAs(span));
        Assert.That(unsupported.Kind, Is.EqualTo(SqlUnsupportedQueryClauseKind.Qualify));
        Assert.That(unsupported.SourceText, Is.Empty);
        Assert.That(unsupported.Span, Is.SameAs(span));
        Assert.That(unsupported.Tokens[0], Is.SameAs(token));
        Assert.That(source.Name, Is.SameAs(name));
        Assert.That(source.FunctionArguments[0], Is.SameAs(expression));
        Assert.That(source.Values[0][0], Is.SameAs(expression));
        Assert.That(source.Query, Is.SameAs(query));
        Assert.That(source.IsLateral, Is.True);
    }

    [Test]
    public void QueryPayloads_RejectEveryRequiredNull()
    {
        SqlExpression expression = Expression();
        SqlSelectCore core = Core();
        SqlTextSpan span = Span();

        Assert.Throws<ArgumentNullException>(() => new SqlCompoundQueryPart(SqlCompoundOperatorKind.Union, null!, span));
        Assert.Throws<ArgumentNullException>(() => new SqlCompoundQueryPart(SqlCompoundOperatorKind.Union, core, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlTopClause(null!, false, false, false, span));
        Assert.Throws<ArgumentNullException>(() => new SqlTopClause(expression, false, false, false, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlOffsetFetchClause(null, null, SqlFetchDirectionKind.None, false, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlUnsupportedQueryClause(SqlUnsupportedQueryClauseKind.None, null!, span, new SqlToken[0]));
        Assert.Throws<ArgumentNullException>(() => new SqlUnsupportedQueryClause(SqlUnsupportedQueryClauseKind.None, "", null!, new SqlToken[0]));
        Assert.Throws<ArgumentNullException>(() => new SqlUnsupportedQueryClause(SqlUnsupportedQueryClauseKind.None, "", span, null!));
        Assert.Throws<ArgumentNullException>(() => new SqlTableSourceData(null, null!, new SqlExpression[0][], null, false));
        Assert.Throws<ArgumentNullException>(() => new SqlTableSourceData(null, new SqlExpression[0], null!, null, false));
    }

    static SqlTextSpan Span()
    {
        return new SqlTextSpan(0, 0);
    }

    static SqlExpression Expression()
    {
        return new SqlExpression(SqlExpressionKind.Unknown, Span(), new SqlToken[0], new SqlExpression[0]);
    }

    static SqlName Name(string text)
    {
        SqlTextSpan span = new SqlTextSpan(0, text.Length);
        return new SqlName(new SqlNamePart[] { new SqlNamePart(text, text, span, SqlNameQuoteStyle.None) }, span);
    }

    static SqlOrderingTerm Ordering(SqlExpression expression)
    {
        return new SqlOrderingTerm(expression, null, SqlOrderingDirection.None, SqlNullOrdering.None, Span());
    }

    static SqlSelectCore Core()
    {
        return new SqlSelectCore(false, false, new SqlResultColumn[0], new SqlTableReference[0], null, new SqlExpression[0], null, new SqlWindowDefinition[0], Span());
    }

    static SqlQuery Query()
    {
        return new SqlQuery(new SqlCommonTableExpression[0], false, new SqlSelectCore[0], new SqlCompoundOperatorKind[0], new SqlOrderingTerm[0], null, Span());
    }
}
