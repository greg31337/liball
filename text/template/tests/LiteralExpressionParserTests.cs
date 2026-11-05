using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class LiteralExpressionParserTests
{
    [Test]
    public void Parse_ScalarLiteralOutputs_AssignSpecificLiteralKinds()
    {
        AssertOutputLiteral("\"x\"", TemplateLiteralSyntaxKind.String);
        AssertOutputLiteral("12.5", TemplateLiteralSyntaxKind.Number);
        AssertOutputLiteral("true", TemplateLiteralSyntaxKind.Boolean);
        AssertOutputLiteral("false", TemplateLiteralSyntaxKind.Boolean);
        AssertOutputLiteral("null", TemplateLiteralSyntaxKind.Null);
        AssertOutputLiteral("empty", TemplateLiteralSyntaxKind.Empty);
    }

    [Test]
    public void Parse_ArrayLiteral_BuildsLiteralExpressionWithElementChildren()
    {
        TemplateParseResult result = TemplateParser.ParseNative("{{ [1, \"x\", true, null, empty] }}");

        TemplateExpressionSyntax expression = result.Document.Children[0].Output!.Expression;

        Assert.That(result.HasDiagnostics(), Is.False);
        AssertLiteral(expression, TemplateLiteralSyntaxKind.Array, "[1,\"x\",true,null,empty]");
        Assert.That(expression.Children.Count, Is.EqualTo(5));
        AssertLiteral(expression.Children[0], TemplateLiteralSyntaxKind.Number, "1");
        AssertLiteral(expression.Children[1], TemplateLiteralSyntaxKind.String, "\"x\"");
        AssertLiteral(expression.Children[2], TemplateLiteralSyntaxKind.Boolean, "true");
        AssertLiteral(expression.Children[3], TemplateLiteralSyntaxKind.Null, "null");
        AssertLiteral(expression.Children[4], TemplateLiteralSyntaxKind.Empty, "empty");
    }

    [Test]
    public void Parse_ObjectLiteral_BuildsLiteralExpressionWithKeyValueChildren()
    {
        TemplateParseResult result = TemplateParser.ParseNative("{{ {\"name\": \"Ada\", \"flags\": [true, false]} }}");

        TemplateExpressionSyntax expression = result.Document.Children[0].Output!.Expression;

        Assert.That(result.HasDiagnostics(), Is.False);
        AssertLiteral(expression, TemplateLiteralSyntaxKind.Object, "{\"name\":\"Ada\",\"flags\":[true,false]}");
        Assert.That(expression.Children.Count, Is.EqualTo(4));
        AssertLiteral(expression.Children[0], TemplateLiteralSyntaxKind.String, "\"name\"");
        AssertLiteral(expression.Children[1], TemplateLiteralSyntaxKind.String, "\"Ada\"");
        AssertLiteral(expression.Children[2], TemplateLiteralSyntaxKind.String, "\"flags\"");
        AssertLiteral(expression.Children[3], TemplateLiteralSyntaxKind.Array, "[true,false]");
        Assert.That(expression.Children[3].Children.Count, Is.EqualTo(2));
        AssertLiteral(expression.Children[3].Children[0], TemplateLiteralSyntaxKind.Boolean, "true");
        AssertLiteral(expression.Children[3].Children[1], TemplateLiteralSyntaxKind.Boolean, "false");
    }

    [Test]
    public void Parse_RangeLiteral_BuildsLiteralExpressionWithEndpointChildren()
    {
        TemplateParseResult result = TemplateParser.ParseNative("{{ 1..3 }}");

        TemplateExpressionSyntax expression = result.Document.Children[0].Output!.Expression;

        Assert.That(result.HasDiagnostics(), Is.False);
        AssertLiteral(expression, TemplateLiteralSyntaxKind.Range, "1..3");
        Assert.That(expression.Children.Count, Is.EqualTo(2));
        AssertLiteral(expression.Children[0], TemplateLiteralSyntaxKind.Number, "1");
        AssertLiteral(expression.Children[1], TemplateLiteralSyntaxKind.Number, "3");
    }

    [Test]
    public void Parse_CompoundLiteralStatementArguments_GroupAsSingleArguments()
    {
        TemplateParseResult result = TemplateParser.ParseNative("{% set [1, 2] {\"a\": true} 1..3 %}");

        TemplateStatementSyntax statement = result.Document.Children[0].Statement!;

        Assert.That(result.HasDiagnostics(), Is.False);
        Assert.That(statement.Arguments.Count, Is.EqualTo(3));
        AssertLiteral(statement.Arguments[0], TemplateLiteralSyntaxKind.Array, "[1,2]");
        AssertLiteral(statement.Arguments[1], TemplateLiteralSyntaxKind.Object, "{\"a\":true}");
        AssertLiteral(statement.Arguments[2], TemplateLiteralSyntaxKind.Range, "1..3");
    }

    static void AssertOutputLiteral(string text, TemplateLiteralSyntaxKind literalKind)
    {
        TemplateParseResult result = TemplateParser.ParseNative("{{ " + text + " }}");
        TemplateExpressionSyntax expression = result.Document.Children[0].Output!.Expression;

        Assert.That(result.HasDiagnostics(), Is.False);
        AssertLiteral(expression, literalKind, text);
    }

    static void AssertLiteral(TemplateExpressionSyntax expression, TemplateLiteralSyntaxKind literalKind, string text)
    {
        Assert.That(expression.ExpressionKind, Is.EqualTo(TemplateExpressionSyntaxKind.Literal));
        Assert.That(expression.LiteralKind, Is.EqualTo(literalKind));
        Assert.That(expression.Text, Is.EqualTo(text));
    }
}
