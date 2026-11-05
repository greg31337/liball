using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class ExpressionParserCoverageTests
{
    [Test]
    public void Precedence_FilterBindsInsideEquality()
    {
        TemplateExpressionSyntax expression = ParseOutputExpression("name | upper == \"ADA\"");

        AssertOperator(expression, TemplateExpressionOperatorKind.Equal, "name|upper==\"ADA\"", 2);
        AssertCall(expression.Children[0], TemplateExpressionCallKind.Filter, "upper", "name|upper", 1);
        AssertLiteral(expression.Children[1], TemplateLiteralSyntaxKind.String, "\"ADA\"");
    }

    [Test]
    public void Precedence_NullCoalescingCanBeConditionalCondition()
    {
        TemplateExpressionSyntax expression = ParseOutputExpression("a ?? b ? c : d");

        AssertOperator(expression, TemplateExpressionOperatorKind.Conditional, "a??b?c:d", 3);
        AssertOperator(expression.Children[0], TemplateExpressionOperatorKind.NullCoalescing, "a??b", 2);
        Assert.That(expression.Children[1].Text, Is.EqualTo("c"));
        Assert.That(expression.Children[2].Text, Is.EqualTo("d"));
    }

    [Test]
    public void Precedence_FunctionArgumentCanContainSubexpression()
    {
        TemplateExpressionSyntax expression = ParseOutputExpression("format((concat first last), 1 + 2)");

        AssertCall(expression, TemplateExpressionCallKind.Function, "format", "format((concatfirstlast),1+2)", 2);
        AssertCall(expression.Arguments[0], TemplateExpressionCallKind.Subexpression, "concat", "(concatfirstlast)", 2);
        AssertOperator(expression.Arguments[1], TemplateExpressionOperatorKind.Add, "1+2", 2);
    }

    [Test]
    public void InvalidExpressions_ReturnUnknownNodes()
    {
        AssertUnknownExpression("1 +", "1+");
        AssertUnknownExpression("user.", "user.");
        AssertUnknownExpression("name |", "name|");
        AssertUnknownExpression("ok ? \"yes\"", "ok?\"yes\"");
    }

    [Test]
    public void CompatibilitySyntax_ParsesHandlebarsStyleDataParentAndSubexpression()
    {
        TemplateExpressionSyntax parent = ParseOutputExpression("../title");
        TemplateExpressionSyntax local = ParseOutputExpression("@index");
        TemplateExpressionSyntax subexpression = ParseOutputExpression("(concat first last)");

        Assert.That(parent.PathKind, Is.EqualTo(TemplatePathSyntaxKind.Parent));
        Assert.That(parent.PathSegments[0].Text, Is.EqualTo("title"));
        Assert.That(local.PathKind, Is.EqualTo(TemplatePathSyntaxKind.Local));
        Assert.That(local.PathSegments[0].Text, Is.EqualTo("index"));
        AssertCall(subexpression, TemplateExpressionCallKind.Subexpression, "concat", "(concatfirstlast)", 2);
    }

    [Test]
    public void CompatibilitySyntax_ParsesLiquidAndJinjaStyleFiltersAndTests()
    {
        TemplateExpressionSyntax filter = ParseOutputExpression("name | replace(\"a\", \"b\")");
        TemplateExpressionSyntax test = ParseOutputExpression("value is defined");

        AssertCall(filter, TemplateExpressionCallKind.Filter, "replace", "name|replace(\"a\",\"b\")", 3);
        AssertCall(test, TemplateExpressionCallKind.Test, "defined", "valueisdefined", 1);
    }

    static TemplateExpressionSyntax ParseOutputExpression(string expressionText)
    {
        TemplateParseResult result = TemplateParser.ParseNative("{{ " + expressionText + " }}");

        Assert.That(result.HasDiagnostics(), Is.False);
        return result.Document.Children[0].Output!.Expression;
    }

    static void AssertUnknownExpression(string expressionText, string compactText)
    {
        TemplateExpressionSyntax expression = ParseOutputExpression(expressionText);

        Assert.That(expression.ExpressionKind, Is.EqualTo(TemplateExpressionSyntaxKind.Unknown));
        Assert.That(expression.Text, Is.EqualTo(compactText));
    }

    static void AssertOperator(TemplateExpressionSyntax expression, TemplateExpressionOperatorKind operatorKind, string text, int childCount)
    {
        Assert.That(expression.ExpressionKind, Is.EqualTo(TemplateExpressionSyntaxKind.Operator));
        Assert.That(expression.OperatorKind, Is.EqualTo(operatorKind));
        Assert.That(expression.Text, Is.EqualTo(text));
        Assert.That(expression.Children.Count, Is.EqualTo(childCount));
    }

    static void AssertCall(TemplateExpressionSyntax expression, TemplateExpressionCallKind callKind, string name, string text, int argumentCount)
    {
        Assert.That(expression.ExpressionKind, Is.EqualTo(TemplateExpressionSyntaxKind.Call));
        Assert.That(expression.CallKind, Is.EqualTo(callKind));
        Assert.That(expression.Name, Is.EqualTo(name));
        Assert.That(expression.Text, Is.EqualTo(text));
        Assert.That(expression.Arguments.Count, Is.EqualTo(argumentCount));
    }

    static void AssertLiteral(TemplateExpressionSyntax expression, TemplateLiteralSyntaxKind literalKind, string text)
    {
        Assert.That(expression.ExpressionKind, Is.EqualTo(TemplateExpressionSyntaxKind.Literal));
        Assert.That(expression.LiteralKind, Is.EqualTo(literalKind));
        Assert.That(expression.Text, Is.EqualTo(text));
    }
}
