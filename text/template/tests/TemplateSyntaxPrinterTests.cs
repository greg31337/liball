using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class TemplateSyntaxPrinterTests
{
    [Test]
    public void Print_Document_BuildsStableSnapshot()
    {
        TemplateParseResult result = TemplateParser.ParseNative("Hi {{ name }}");

        string snapshot = TemplateSyntaxPrinter.Print(result.Document);

        Assert.That(snapshot, Is.EqualTo(
            "Document [0..13) source=\"(anonymous)\"\n" +
            "  Text [0..3) text=\"Hi \"\n" +
            "  Output [3..13) raw=false escape=true\n" +
            "    Expression [6..10) kind=Variable path=Variable text=\"name\"\n" +
            "      PathSegment Name text=\"name\"\n"));
    }

    [Test]
    public void Print_BlockWithElse_BuildsNestedSnapshot()
    {
        TemplateParseResult result = TemplateParser.ParseNative("{% if ok %}yes{% else %}no{% endif %}");

        string snapshot = TemplateSyntaxPrinter.Print(result.Document);

        Assert.That(snapshot, Is.EqualTo(
            "Document [0..37) source=\"(anonymous)\"\n" +
            "  Block [0..37) name=\"if\"\n" +
            "    Start\n" +
            "      Statement [0..11) name=\"if\"\n" +
            "        Argument 0\n" +
            "          Expression [6..8) kind=Variable path=Variable text=\"ok\"\n" +
            "            PathSegment Name text=\"ok\"\n" +
            "    Children\n" +
            "      Text [11..14) text=\"yes\"\n" +
            "    ElseChildren\n" +
            "      Statement [14..24) name=\"else\"\n" +
            "      Text [24..26) text=\"no\"\n" +
            "    End\n" +
            "      Statement [26..37) name=\"endif\"\n"));
    }

    [Test]
    public void Print_DiagnosticsAndErrors_BuildsDebugSnapshot()
    {
        TemplateParseResult result = TemplateParser.ParseNative("{% unknown %}\n");

        string snapshot = TemplateSyntaxPrinter.Print(result.Document);

        Assert.That(snapshot, Is.EqualTo(
            "Document [0..14) source=\"(anonymous)\"\n" +
            "  Diagnostic PARSE006 Error [0..13) message=\"Unknown tag 'unknown'.\" hint=\"Register the tag or check its spelling.\"\n" +
            "  Error [0..13) code=PARSE006 message=\"Unknown tag 'unknown'.\"\n" +
            "  Text [13..14) text=\"\\n\"\n"));
    }

    [Test]
    public void PrintExpression_IncludesChildrenAndEscapesText()
    {
        TemplateSource source = TemplateSource.FromString("\"a\"\nchild");
        TemplateExpressionSyntax expression = new TemplateExpressionSyntax(source.CreateSpan(0, 3), TemplateExpressionSyntaxKind.Group, "\"a\"");
        expression.AddChild(new TemplateExpressionSyntax(source.CreateSpan(4, 5), TemplateExpressionSyntaxKind.Variable, "child"));

        string snapshot = TemplateSyntaxPrinter.PrintExpression(expression);

        Assert.That(snapshot, Is.EqualTo(
            "Expression [0..3) kind=Group text=\"\\\"a\\\"\"\n" +
            "  ChildExpression 0\n" +
            "    Expression [4..9) kind=Variable text=\"child\"\n"));
    }

    [Test]
    public void PrintExpression_IncludesOperatorKind()
    {
        TemplateExpressionSyntax expression = TemplateParser.ParseNative("{{ 1 + 2 }}").Document.Children[0].Output!.Expression;

        string snapshot = TemplateSyntaxPrinter.PrintExpression(expression);

        Assert.That(snapshot, Is.EqualTo(
            "Expression [3..8) kind=Operator operator=Add text=\"1+2\"\n" +
            "  ChildExpression 0\n" +
            "    Expression [3..4) kind=Literal literal=Number text=\"1\"\n" +
            "  ChildExpression 1\n" +
            "    Expression [7..8) kind=Literal literal=Number text=\"2\"\n"));
    }

    [Test]
    public void PrintExpression_IncludesCallKindNameAndArguments()
    {
        TemplateExpressionSyntax expression = TemplateParser.ParseNative("{{ format(name, 1) }}").Document.Children[0].Output!.Expression;

        string snapshot = TemplateSyntaxPrinter.PrintExpression(expression);

        Assert.That(snapshot, Is.EqualTo(
            "Expression [3..18) kind=Call call=Function name=\"format\" text=\"format(name,1)\"\n" +
            "  CallArgument 0\n" +
            "    Expression [10..14) kind=Variable path=Variable text=\"name\"\n" +
            "      PathSegment Name text=\"name\"\n" +
            "  CallArgument 1\n" +
            "    Expression [16..17) kind=Literal literal=Number text=\"1\"\n"));
    }

    [Test]
    public void Print_RejectsNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => TemplateSyntaxPrinter.Print((TemplateDocumentSyntax)null!));
        Assert.Throws<ArgumentNullException>(() => TemplateSyntaxPrinter.Print((TemplateSyntaxNode)null!));
        Assert.Throws<ArgumentNullException>(() => TemplateSyntaxPrinter.PrintExpression(null!));
    }
}
