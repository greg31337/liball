using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class ParserTests
{
    [Test]
    public void ParseNative_TextAndOutput_BuildsDocumentChildren()
    {
        TemplateParseResult result = TemplateParser.ParseNative("Hello {{ user.name }}!");

        Assert.That(result.HasDiagnostics(), Is.False);
        Assert.That(result.Document.Children.Count, Is.EqualTo(3));
        Assert.That(result.Document.Children[0].Kind, Is.EqualTo(TemplateSyntaxKind.Text));
        Assert.That(result.Document.Children[0].Text!.Text, Is.EqualTo("Hello "));
        Assert.That(result.Document.Children[1].Kind, Is.EqualTo(TemplateSyntaxKind.Output));
        Assert.That(result.Document.Children[1].Output!.Expression.Text, Is.EqualTo("user.name"));
        Assert.That(result.Document.Children[1].Output!.Expression.ExpressionKind, Is.EqualTo(TemplateExpressionSyntaxKind.Path));
        Assert.That(result.Document.Children[1].Output!.Expression.PathKind, Is.EqualTo(TemplatePathSyntaxKind.Variable));
        Assert.That(result.Document.Children[1].Output!.Expression.PathSegments.Count, Is.EqualTo(2));
        Assert.That(result.Document.Children[1].Output!.Escape, Is.True);
        Assert.That(result.Document.Children[1].Output!.Raw, Is.False);
        Assert.That(result.Document.Children[2].Kind, Is.EqualTo(TemplateSyntaxKind.Text));
        Assert.That(result.Document.Children[2].Text!.Text, Is.EqualTo("!"));
    }

    [Test]
    public void ParseNative_CommentsStatementsAndRawBlocks_BuildExpectedNodes()
    {
        TemplateParseResult result = TemplateParser.ParseNative("{# note #}{% if ok %}{% raw %}{{ value }}{% endraw %}{% endif %}");

        Assert.That(result.HasDiagnostics(), Is.False);
        Assert.That(result.Document.Children.Count, Is.EqualTo(2));
        Assert.That(result.Document.Children[0].Kind, Is.EqualTo(TemplateSyntaxKind.Comment));
        Assert.That(result.Document.Children[0].Comment!.Text, Is.EqualTo(" note "));

        Assert.That(result.Document.Children[1].Kind, Is.EqualTo(TemplateSyntaxKind.Block));
        Assert.That(result.Document.Children[1].Block!.Name, Is.EqualTo("if"));
        Assert.That(result.Document.Children[1].Block!.StartStatement.Arguments.Count, Is.EqualTo(1));
        Assert.That(result.Document.Children[1].Block!.StartStatement.Arguments[0].Text, Is.EqualTo("ok"));
        Assert.That(result.Document.Children[1].Block!.StartStatement.Arguments[0].ExpressionKind, Is.EqualTo(TemplateExpressionSyntaxKind.Variable));
        Assert.That(result.Document.Children[1].Block!.HasEndStatement(), Is.True);

        Assert.That(result.Document.Children[1].Block!.Children.Count, Is.EqualTo(1));
        Assert.That(result.Document.Children[1].Block!.Children[0].Kind, Is.EqualTo(TemplateSyntaxKind.Raw));
        Assert.That(result.Document.Children[1].Block!.Children[0].Raw!.Text, Is.EqualTo("{{ value }}"));
    }

    [Test]
    public void ParseNative_LiteralsInOutputAndStatement_AreMarkedAsLiteralExpressions()
    {
        TemplateParseResult output = TemplateParser.ParseNative("{{ true }}");
        TemplateParseResult statement = TemplateParser.ParseNative("{% set true 12 \"x\" %}");

        Assert.That(output.Document.Children[0].Output!.Expression.Text, Is.EqualTo("true"));
        Assert.That(output.Document.Children[0].Output!.Expression.ExpressionKind, Is.EqualTo(TemplateExpressionSyntaxKind.Literal));
        Assert.That(output.Document.Children[0].Output!.Expression.LiteralKind, Is.EqualTo(TemplateLiteralSyntaxKind.Boolean));
        Assert.That(statement.Document.Children[0].Statement!.Name, Is.EqualTo("set"));
        Assert.That(statement.Document.Children[0].Statement!.Arguments.Count, Is.EqualTo(3));
        Assert.That(statement.Document.Children[0].Statement!.Arguments[0].ExpressionKind, Is.EqualTo(TemplateExpressionSyntaxKind.Literal));
        Assert.That(statement.Document.Children[0].Statement!.Arguments[0].LiteralKind, Is.EqualTo(TemplateLiteralSyntaxKind.Boolean));
        Assert.That(statement.Document.Children[0].Statement!.Arguments[1].ExpressionKind, Is.EqualTo(TemplateExpressionSyntaxKind.Literal));
        Assert.That(statement.Document.Children[0].Statement!.Arguments[1].LiteralKind, Is.EqualTo(TemplateLiteralSyntaxKind.Number));
        Assert.That(statement.Document.Children[0].Statement!.Arguments[2].ExpressionKind, Is.EqualTo(TemplateExpressionSyntaxKind.Literal));
        Assert.That(statement.Document.Children[0].Statement!.Arguments[2].LiteralKind, Is.EqualTo(TemplateLiteralSyntaxKind.String));
    }

    [Test]
    public void Parse_CopiesLexerDiagnosticsToParseResultAndDocument()
    {
        TemplateParseResult result = TemplateParser.ParseNative("Hello {{ missing");

        Assert.That(result.HasDiagnostics(), Is.True);
        Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
        Assert.That(result.Diagnostics[0].Code, Is.EqualTo("LEX001"));
        Assert.That(result.Document.HasDiagnostics(), Is.True);
        Assert.That(result.Document.Diagnostics.Count, Is.EqualTo(1));
        Assert.That(result.Document.Diagnostics[0], Is.SameAs(result.Diagnostics[0]));
        Assert.That(result.Document.Children.Count, Is.EqualTo(2));
        Assert.That(result.Document.Children[1].Kind, Is.EqualTo(TemplateSyntaxKind.Output));
    }

    [Test]
    public void Parse_UnexpectedToken_BuildsErrorNodeAndDiagnostic()
    {
        TemplateSource source = TemplateSource.FromString("@");
        TemplateLexerResult lexerResult = new TemplateLexerResult();
        lexerResult.Tokens.Add(new TemplateToken(TemplateTokenKind.Unknown, source.CreateSpan(0, 1), "@"));
        lexerResult.Tokens.Add(new TemplateToken(TemplateTokenKind.EndOfFile, source.CreateSpan(1, 0), ""));
        TemplateParser parser = new TemplateParser(lexerResult);

        TemplateParseResult result = parser.Parse();

        Assert.That(result.HasDiagnostics(), Is.True);
        Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
        Assert.That(result.Diagnostics[0].Code, Is.EqualTo("PARSE001"));
        Assert.That(result.Document.Children.Count, Is.EqualTo(1));
        Assert.That(result.Document.Children[0].Kind, Is.EqualTo(TemplateSyntaxKind.Error));
        Assert.That(result.Document.Children[0].Error!.Message, Does.Contain("Unexpected token"));
    }

    [Test]
    public void Parse_BlockWithElse_BuildsBlockChildrenAndElseChildren()
    {
        TemplateParseResult result = TemplateParser.ParseNative("{% if ok %}yes{% else %}no{% endif %}");

        Assert.That(result.HasDiagnostics(), Is.False);
        Assert.That(result.Document.Children.Count, Is.EqualTo(1));
        Assert.That(result.Document.Children[0].Kind, Is.EqualTo(TemplateSyntaxKind.Block));
        TemplateBlockSyntax block = result.Document.Children[0].Block!;
        Assert.That(block.Name, Is.EqualTo("if"));
        Assert.That(block.Children.Count, Is.EqualTo(1));
        Assert.That(block.Children[0].Text!.Text, Is.EqualTo("yes"));
        Assert.That(block.ElseChildren.Count, Is.EqualTo(2));
        Assert.That(block.ElseChildren[0].Statement!.Name, Is.EqualTo("else"));
        Assert.That(block.ElseChildren[1].Text!.Text, Is.EqualTo("no"));
        Assert.That(block.EndStatement!.Name, Is.EqualTo("endif"));
    }

    [Test]
    public void Parse_ForBlockWithEmpty_BuildsEmptyBranch()
    {
        TemplateParseResult result = TemplateParser.ParseNative("{% for item in items %}yes{% empty %}no{% endfor %}");

        Assert.That(result.HasDiagnostics(), Is.False);
        TemplateBlockSyntax block = result.Document.Children[0].Block!;
        Assert.That(block.Name, Is.EqualTo("for"));
        Assert.That(block.Children.Count, Is.EqualTo(1));
        Assert.That(block.ElseChildren.Count, Is.EqualTo(2));
        Assert.That(block.ElseChildren[0].Statement!.Name, Is.EqualTo("empty"));
        Assert.That(block.ElseChildren[1].Text!.Text, Is.EqualTo("no"));
    }

    [Test]
    public void Parse_ExtendsAndBlock_BuildsStatementAndNamedBlock()
    {
        TemplateParseResult result = TemplateParser.ParseNative("{% extends \"layout\" %}{% block body %}Body{% endblock %}");

        Assert.That(result.HasDiagnostics(), Is.False);
        Assert.That(result.Document.Children.Count, Is.EqualTo(2));
        Assert.That(result.Document.Children[0].Statement!.Name, Is.EqualTo("extends"));
        TemplateBlockSyntax block = result.Document.Children[1].Block!;
        Assert.That(block.Name, Is.EqualTo("block"));
        Assert.That(block.StartStatement.Arguments[0].Text, Is.EqualTo("body"));
        Assert.That(block.Children[0].Text!.Text, Is.EqualTo("Body"));
        Assert.That(block.EndStatement!.Name, Is.EqualTo("endblock"));
    }

    [Test]
    public void Parse_MacroBlock_BuildsNamedMacroBlock()
    {
        TemplateParseResult result = TemplateParser.ParseNative("{% macro greet person punctuation %}Hello{% endmacro %}");

        Assert.That(result.HasDiagnostics(), Is.False);
        TemplateBlockSyntax block = result.Document.Children[0].Block!;
        Assert.That(block.Name, Is.EqualTo("macro"));
        Assert.That(block.StartStatement.Arguments[0].Text, Is.EqualTo("greet"));
        Assert.That(block.StartStatement.Arguments[1].Text, Is.EqualTo("person"));
        Assert.That(block.StartStatement.Arguments[2].Text, Is.EqualTo("punctuation"));
        Assert.That(block.EndStatement!.Name, Is.EqualTo("endmacro"));
    }

    [Test]
    public void Parse_ImportStatement_BuildsStandaloneStatement()
    {
        TemplateParseResult result = TemplateParser.ParseNative("{% import \"macros\" %}{{ greet(user) }}");

        Assert.That(result.HasDiagnostics(), Is.False);
        Assert.That(result.Document.Children[0].Statement!.Name, Is.EqualTo("import"));
        Assert.That(result.Document.Children[0].Statement!.Arguments[0].Text, Is.EqualTo("\"macros\""));
    }

    [Test]
    public void Parse_FilterAndCallBlocks_BuildReusableContentBlocks()
    {
        TemplateParseResult filter = TemplateParser.ParseNative("{% filter upper %}Body{% endfilter %}");
        TemplateParseResult call = TemplateParser.ParseNative("{% call panel(\"Title\") %}Body{% endcall %}");

        Assert.That(filter.HasDiagnostics(), Is.False);
        Assert.That(filter.Document.Children[0].Block!.Name, Is.EqualTo("filter"));
        Assert.That(filter.Document.Children[0].Block!.StartStatement.Arguments[0].Text, Is.EqualTo("upper"));
        Assert.That(call.HasDiagnostics(), Is.False);
        Assert.That(call.Document.Children[0].Block!.Name, Is.EqualTo("call"));
        Assert.That(call.Document.Children[0].Block!.StartStatement.Arguments[0].Text, Is.EqualTo("panel(\"Title\")"));
    }

    [Test]
    public void Parse_UnclosedBlock_ReportsUnexpectedEofAndKeepsBlock()
    {
        TemplateParseResult result = TemplateParser.ParseNative("{% if ok %}yes");

        Assert.That(result.HasDiagnostics(), Is.True);
        Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
        Assert.That(result.Diagnostics[0].Code, Is.EqualTo("PARSE004"));
        Assert.That(result.Document.Children.Count, Is.EqualTo(1));
        Assert.That(result.Document.Children[0].Kind, Is.EqualTo(TemplateSyntaxKind.Block));
        Assert.That(result.Document.Children[0].Block!.Name, Is.EqualTo("if"));
        Assert.That(result.Document.Children[0].Block!.HasEndStatement(), Is.False);
    }

    [Test]
    public void Parse_MismatchedEndTag_ReportsDiagnosticAndClosesCurrentBlock()
    {
        TemplateParseResult result = TemplateParser.ParseNative("{% if ok %}yes{% endfor %}");

        Assert.That(result.HasDiagnostics(), Is.True);
        Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
        Assert.That(result.Diagnostics[0].Code, Is.EqualTo("PARSE003"));
        Assert.That(result.Diagnostics[0].Message, Does.Contain("Expected 'endif'"));
        Assert.That(result.Document.Children[0].Block!.EndStatement!.Name, Is.EqualTo("endfor"));
    }

    [Test]
    public void Parse_UnknownTag_BuildsErrorNodeAndDiagnostic()
    {
        TemplateParseResult result = TemplateParser.ParseNative("{% frob value %}");

        Assert.That(result.HasDiagnostics(), Is.True);
        Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
        Assert.That(result.Diagnostics[0].Code, Is.EqualTo("PARSE006"));
        Assert.That(result.Document.Children.Count, Is.EqualTo(1));
        Assert.That(result.Document.Children[0].Kind, Is.EqualTo(TemplateSyntaxKind.Error));
    }

    [Test]
    public void Parse_StrayEndTag_BuildsErrorNodeAndDiagnostic()
    {
        TemplateParseResult result = TemplateParser.ParseNative("{% endif %}");

        Assert.That(result.HasDiagnostics(), Is.True);
        Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
        Assert.That(result.Diagnostics[0].Code, Is.EqualTo("PARSE002"));
        Assert.That(result.Document.Children[0].Kind, Is.EqualTo(TemplateSyntaxKind.Error));
    }

    [Test]
    public void Parse_InvalidBranchOutsideBlock_BuildsErrorNodeAndDiagnostic()
    {
        TemplateParseResult result = TemplateParser.ParseNative("{% else %}");

        Assert.That(result.HasDiagnostics(), Is.True);
        Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
        Assert.That(result.Diagnostics[0].Code, Is.EqualTo("PARSE005"));
        Assert.That(result.Document.Children[0].Kind, Is.EqualTo(TemplateSyntaxKind.Error));
    }

    [Test]
    public void Parse_InvalidBranchInsideBlock_ReportsDiagnosticAndContinuesToMatchingEnd()
    {
        TemplateParseResult result = TemplateParser.ParseNative("{% if ok %}{% when x %}yes{% endif %}");

        Assert.That(result.HasDiagnostics(), Is.True);
        Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
        Assert.That(result.Diagnostics[0].Code, Is.EqualTo("PARSE005"));
        TemplateBlockSyntax block = result.Document.Children[0].Block!;
        Assert.That(block.HasEndStatement(), Is.True);
        Assert.That(block.EndStatement!.Name, Is.EqualTo("endif"));
        Assert.That(block.Children[0].Kind, Is.EqualTo(TemplateSyntaxKind.Error));
        Assert.That(block.Children[1].Text!.Text, Is.EqualTo("yes"));
    }

    [Test]
    public void Constructor_RejectsMissingOrEmptyLexerResults()
    {
        TemplateLexerResult empty = new TemplateLexerResult();

        Assert.Throws<ArgumentNullException>(() => new TemplateParser(null!));
        Assert.Throws<ArgumentException>(() => new TemplateParser(empty));
    }
}
