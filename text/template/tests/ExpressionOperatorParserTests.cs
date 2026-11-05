using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class ExpressionOperatorParserTests
{
    [Test]
    public void Parse_UnaryOperators_BuildOperatorExpressions()
    {
        TemplateExpressionSyntax notExpression = ParseOutputExpression("not active");
        TemplateExpressionSyntax negateExpression = ParseOutputExpression("-count");

        AssertOperator(notExpression, TemplateExpressionOperatorKind.UnaryNot, "notactive", 1);
        Assert.That(notExpression.Children[0].ExpressionKind, Is.EqualTo(TemplateExpressionSyntaxKind.Variable));
        Assert.That(notExpression.Children[0].Text, Is.EqualTo("active"));

        AssertOperator(negateExpression, TemplateExpressionOperatorKind.UnaryNegate, "-count", 1);
        Assert.That(negateExpression.Children[0].Text, Is.EqualTo("count"));
    }

    [Test]
    public void Parse_BinaryOperators_RespectPrecedence()
    {
        TemplateExpressionSyntax expression = ParseOutputExpression("1 + 2 * 3 == 7 and ok");

        AssertOperator(expression, TemplateExpressionOperatorKind.And, "1+2*3==7andok", 2);
        AssertOperator(expression.Children[0], TemplateExpressionOperatorKind.Equal, "1+2*3==7", 2);
        AssertOperator(expression.Children[0].Children[0], TemplateExpressionOperatorKind.Add, "1+2*3", 2);
        AssertOperator(expression.Children[0].Children[0].Children[1], TemplateExpressionOperatorKind.Multiply, "2*3", 2);
        Assert.That(expression.Children[1].ExpressionKind, Is.EqualTo(TemplateExpressionSyntaxKind.Variable));
        Assert.That(expression.Children[1].Text, Is.EqualTo("ok"));
    }

    [Test]
    public void Parse_Grouping_OverridesBinaryPrecedence()
    {
        TemplateExpressionSyntax expression = ParseOutputExpression("(1 + 2) * 3");

        AssertOperator(expression, TemplateExpressionOperatorKind.Multiply, "(1+2)*3", 2);
        Assert.That(expression.Children[0].ExpressionKind, Is.EqualTo(TemplateExpressionSyntaxKind.Group));
        Assert.That(expression.Children[0].Children.Count, Is.EqualTo(1));
        AssertOperator(expression.Children[0].Children[0], TemplateExpressionOperatorKind.Add, "1+2", 2);
        AssertLiteral(expression.Children[1], TemplateLiteralSyntaxKind.Number, "3");
    }

    [Test]
    public void Parse_ConditionalExpression_BuildsThreeChildOperator()
    {
        TemplateExpressionSyntax expression = ParseOutputExpression("ok ? \"yes\" : \"no\"");

        AssertOperator(expression, TemplateExpressionOperatorKind.Conditional, "ok?\"yes\":\"no\"", 3);
        Assert.That(expression.Children[0].Text, Is.EqualTo("ok"));
        AssertLiteral(expression.Children[1], TemplateLiteralSyntaxKind.String, "\"yes\"");
        AssertLiteral(expression.Children[2], TemplateLiteralSyntaxKind.String, "\"no\"");
    }

    [Test]
    public void Parse_NullCoalescingExpression_BuildsBinaryOperator()
    {
        TemplateExpressionSyntax expression = ParseOutputExpression("user.name ?? \"guest\"");

        AssertOperator(expression, TemplateExpressionOperatorKind.NullCoalescing, "user.name??\"guest\"", 2);
        Assert.That(expression.Children[0].ExpressionKind, Is.EqualTo(TemplateExpressionSyntaxKind.Path));
        Assert.That(expression.Children[0].Text, Is.EqualTo("user.name"));
        AssertLiteral(expression.Children[1], TemplateLiteralSyntaxKind.String, "\"guest\"");
    }

    [Test]
    public void Parse_ConditionalStatementArgument_ParsesAsSingleExpression()
    {
        TemplateParseResult result = TemplateParser.ParseNative("{% if ok and ready %}yes{% endif %}");

        TemplateStatementSyntax statement = result.Document.Children[0].Block!.StartStatement;

        Assert.That(result.HasDiagnostics(), Is.False);
        Assert.That(statement.Arguments.Count, Is.EqualTo(1));
        AssertOperator(statement.Arguments[0], TemplateExpressionOperatorKind.And, "okandready", 2);
    }

    static TemplateExpressionSyntax ParseOutputExpression(string expressionText)
    {
        TemplateParseResult result = TemplateParser.ParseNative("{{ " + expressionText + " }}");

        Assert.That(result.HasDiagnostics(), Is.False);
        return result.Document.Children[0].Output!.Expression;
    }

    static void AssertOperator(TemplateExpressionSyntax expression, TemplateExpressionOperatorKind operatorKind, string text, int childCount)
    {
        Assert.That(expression.ExpressionKind, Is.EqualTo(TemplateExpressionSyntaxKind.Operator));
        Assert.That(expression.OperatorKind, Is.EqualTo(operatorKind));
        Assert.That(expression.Text, Is.EqualTo(text));
        Assert.That(expression.Children.Count, Is.EqualTo(childCount));
    }

    static void AssertLiteral(TemplateExpressionSyntax expression, TemplateLiteralSyntaxKind literalKind, string text)
    {
        Assert.That(expression.ExpressionKind, Is.EqualTo(TemplateExpressionSyntaxKind.Literal));
        Assert.That(expression.LiteralKind, Is.EqualTo(literalKind));
        Assert.That(expression.Text, Is.EqualTo(text));
    }
}
