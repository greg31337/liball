using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class ExpressionCallParserTests
{
    [Test]
    public void Parse_FunctionCall_BuildsCallExpressionWithArguments()
    {
        TemplateExpressionSyntax expression = ParseOutputExpression("format(user.name, 12, \"x\")");

        AssertCall(expression, TemplateExpressionCallKind.Function, "format", "format(user.name,12,\"x\")", 3);
        Assert.That(expression.Arguments[0].ExpressionKind, Is.EqualTo(TemplateExpressionSyntaxKind.Path));
        Assert.That(expression.Arguments[0].Text, Is.EqualTo("user.name"));
        AssertLiteral(expression.Arguments[1], TemplateLiteralSyntaxKind.Number, "12");
        AssertLiteral(expression.Arguments[2], TemplateLiteralSyntaxKind.String, "\"x\"");
    }

    [Test]
    public void Parse_FilterPipe_BuildsNestedFilterCalls()
    {
        TemplateExpressionSyntax expression = ParseOutputExpression("name | upper | replace(\"A\", \"B\")");

        AssertCall(expression, TemplateExpressionCallKind.Filter, "replace", "name|upper|replace(\"A\",\"B\")", 3);
        AssertCall(expression.Arguments[0], TemplateExpressionCallKind.Filter, "upper", "name|upper", 1);
        Assert.That(expression.Arguments[0].Arguments[0].ExpressionKind, Is.EqualTo(TemplateExpressionSyntaxKind.Variable));
        Assert.That(expression.Arguments[0].Arguments[0].Text, Is.EqualTo("name"));
        AssertLiteral(expression.Arguments[1], TemplateLiteralSyntaxKind.String, "\"A\"");
        AssertLiteral(expression.Arguments[2], TemplateLiteralSyntaxKind.String, "\"B\"");
    }

    [Test]
    public void Parse_TestExpression_BuildsTestCallWithSubject()
    {
        TemplateExpressionSyntax expression = ParseOutputExpression("value is defined");

        AssertCall(expression, TemplateExpressionCallKind.Test, "defined", "valueisdefined", 1);
        Assert.That(expression.Arguments[0].ExpressionKind, Is.EqualTo(TemplateExpressionSyntaxKind.Variable));
        Assert.That(expression.Arguments[0].Text, Is.EqualTo("value"));
    }

    [Test]
    public void Parse_NegatedTestWithArguments_BuildsTestCall()
    {
        TemplateExpressionSyntax expression = ParseOutputExpression("score is not greaterthan(10)");

        AssertCall(expression, TemplateExpressionCallKind.Test, "not greaterthan", "scoreisnotgreaterthan(10)", 2);
        Assert.That(expression.Arguments[0].Text, Is.EqualTo("score"));
        AssertLiteral(expression.Arguments[1], TemplateLiteralSyntaxKind.Number, "10");
    }

    [Test]
    public void Parse_CompoundTestNames_IncludeSecondNameToken()
    {
        TemplateExpressionSyntax divisible = ParseOutputExpression("score is divisible by 3");
        TemplateExpressionSyntax equal = ParseOutputExpression("score is not equal to 10");

        AssertCall(divisible, TemplateExpressionCallKind.Test, "divisible by", "scoreisdivisibleby3", 2);
        Assert.That(divisible.Arguments[0].Text, Is.EqualTo("score"));
        AssertLiteral(divisible.Arguments[1], TemplateLiteralSyntaxKind.Number, "3");
        AssertCall(equal, TemplateExpressionCallKind.Test, "not equal to", "scoreisnotequalto10", 2);
        Assert.That(equal.Arguments[0].Text, Is.EqualTo("score"));
        AssertLiteral(equal.Arguments[1], TemplateLiteralSyntaxKind.Number, "10");
    }

    [Test]
    public void Parse_Subexpression_BuildsSubexpressionCall()
    {
        TemplateExpressionSyntax expression = ParseOutputExpression("(concat first last)");

        AssertCall(expression, TemplateExpressionCallKind.Subexpression, "concat", "(concatfirstlast)", 2);
        Assert.That(expression.Arguments[0].Text, Is.EqualTo("first"));
        Assert.That(expression.Arguments[1].Text, Is.EqualTo("last"));
    }

    [Test]
    public void Parse_ReturnStatementArgument_ParsesFunctionCallAsSingleExpression()
    {
        TemplateParseResult result = TemplateParser.ParseNative("{% return format(name, 1) %}");

        TemplateStatementSyntax statement = result.Document.Children[0].Statement!;

        Assert.That(result.HasDiagnostics(), Is.False);
        Assert.That(statement.Arguments.Count, Is.EqualTo(1));
        AssertCall(statement.Arguments[0], TemplateExpressionCallKind.Function, "format", "format(name,1)", 2);
    }

    static TemplateExpressionSyntax ParseOutputExpression(string expressionText)
    {
        TemplateParseResult result = TemplateParser.ParseNative("{{ " + expressionText + " }}");

        Assert.That(result.HasDiagnostics(), Is.False);
        return result.Document.Children[0].Output!.Expression;
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
