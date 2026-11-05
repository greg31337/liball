using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class PathExpressionParserTests
{
    [Test]
    public void Parse_VariableExpression_BuildsVariablePathSegment()
    {
        TemplateExpressionSyntax expression = ParseOutputExpression("user");

        Assert.That(expression.ExpressionKind, Is.EqualTo(TemplateExpressionSyntaxKind.Variable));
        Assert.That(expression.PathKind, Is.EqualTo(TemplatePathSyntaxKind.Variable));
        Assert.That(expression.PathSegments.Count, Is.EqualTo(1));
        AssertSegment(expression.PathSegments[0], TemplatePathSegmentSyntaxKind.Name, "user");
    }

    [Test]
    public void Parse_DottedPathAndIndexers_BuildsPathSegments()
    {
        TemplateExpressionSyntax expression = ParseOutputExpression("user.profile[\"name\"][0]");

        Assert.That(expression.ExpressionKind, Is.EqualTo(TemplateExpressionSyntaxKind.Path));
        Assert.That(expression.PathKind, Is.EqualTo(TemplatePathSyntaxKind.Variable));
        Assert.That(expression.Text, Is.EqualTo("user.profile[\"name\"][0]"));
        Assert.That(expression.PathSegments.Count, Is.EqualTo(4));
        AssertSegment(expression.PathSegments[0], TemplatePathSegmentSyntaxKind.Name, "user");
        AssertSegment(expression.PathSegments[1], TemplatePathSegmentSyntaxKind.Name, "profile");
        AssertIndexerSegment(expression.PathSegments[2], "[\"name\"]", TemplateLiteralSyntaxKind.String, "\"name\"");
        AssertIndexerSegment(expression.PathSegments[3], "[0]", TemplateLiteralSyntaxKind.Number, "0");
    }

    [Test]
    public void Parse_RootCurrentParentAndLocalPaths_AssignsPathKinds()
    {
        TemplateExpressionSyntax root = ParseOutputExpression("$.site.title");
        TemplateExpressionSyntax current = ParseOutputExpression("this.name");
        TemplateExpressionSyntax parent = ParseOutputExpression("../title");
        TemplateExpressionSyntax local = ParseOutputExpression("@index");

        Assert.That(root.PathKind, Is.EqualTo(TemplatePathSyntaxKind.Root));
        Assert.That(root.PathSegments.Count, Is.EqualTo(2));
        AssertSegment(root.PathSegments[0], TemplatePathSegmentSyntaxKind.Name, "site");
        AssertSegment(root.PathSegments[1], TemplatePathSegmentSyntaxKind.Name, "title");

        Assert.That(current.PathKind, Is.EqualTo(TemplatePathSyntaxKind.Current));
        Assert.That(current.PathSegments.Count, Is.EqualTo(1));
        AssertSegment(current.PathSegments[0], TemplatePathSegmentSyntaxKind.Name, "name");

        Assert.That(parent.PathKind, Is.EqualTo(TemplatePathSyntaxKind.Parent));
        Assert.That(parent.PathSegments.Count, Is.EqualTo(1));
        AssertSegment(parent.PathSegments[0], TemplatePathSegmentSyntaxKind.Name, "title");

        Assert.That(local.PathKind, Is.EqualTo(TemplatePathSyntaxKind.Local));
        Assert.That(local.PathSegments.Count, Is.EqualTo(1));
        AssertSegment(local.PathSegments[0], TemplatePathSegmentSyntaxKind.Name, "index");
    }

    [Test]
    public void Parse_StatementArguments_GroupPathAndIndexerAsSingleArgument()
    {
        TemplateParseResult result = TemplateParser.ParseNative("{% set user.items[0].name $.root %}");

        TemplateStatementSyntax statement = result.Document.Children[0].Statement!;

        Assert.That(result.HasDiagnostics(), Is.False);
        Assert.That(statement.Arguments.Count, Is.EqualTo(2));
        Assert.That(statement.Arguments[0].ExpressionKind, Is.EqualTo(TemplateExpressionSyntaxKind.Path));
        Assert.That(statement.Arguments[0].Text, Is.EqualTo("user.items[0].name"));
        Assert.That(statement.Arguments[0].PathSegments.Count, Is.EqualTo(4));
        Assert.That(statement.Arguments[1].PathKind, Is.EqualTo(TemplatePathSyntaxKind.Root));
        Assert.That(statement.Arguments[1].PathSegments.Count, Is.EqualTo(1));
    }

    [Test]
    public void Lex_RootAndLocalPrefixes_ProduceOperatorTokens()
    {
        TemplateLexerResult result = TemplateLexer.LexNative("{{ $.name @index }}");

        Assert.That(result.HasDiagnostics(), Is.False);
        Assert.That(result.Tokens[1].Kind, Is.EqualTo(TemplateTokenKind.Operator));
        Assert.That(result.Tokens[1].Text, Is.EqualTo("$"));
        Assert.That(result.Tokens[4].Kind, Is.EqualTo(TemplateTokenKind.Operator));
        Assert.That(result.Tokens[4].Text, Is.EqualTo("@"));
    }

    static TemplateExpressionSyntax ParseOutputExpression(string expressionText)
    {
        TemplateParseResult result = TemplateParser.ParseNative("{{ " + expressionText + " }}");

        Assert.That(result.HasDiagnostics(), Is.False);
        return result.Document.Children[0].Output!.Expression;
    }

    static void AssertSegment(TemplatePathSegmentSyntax segment, TemplatePathSegmentSyntaxKind segmentKind, string text)
    {
        Assert.That(segment.SegmentKind, Is.EqualTo(segmentKind));
        Assert.That(segment.Text, Is.EqualTo(text));
        Assert.That(segment.Expression, Is.Null);
    }

    static void AssertIndexerSegment(TemplatePathSegmentSyntax segment, string text, TemplateLiteralSyntaxKind literalKind, string expressionText)
    {
        Assert.That(segment.SegmentKind, Is.EqualTo(TemplatePathSegmentSyntaxKind.Indexer));
        Assert.That(segment.Text, Is.EqualTo(text));
        Assert.That(segment.Expression, Is.Not.Null);
        Assert.That(segment.Expression!.ExpressionKind, Is.EqualTo(TemplateExpressionSyntaxKind.Literal));
        Assert.That(segment.Expression!.LiteralKind, Is.EqualTo(literalKind));
        Assert.That(segment.Expression!.Text, Is.EqualTo(expressionText));
    }
}
