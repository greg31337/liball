namespace template.Tests;

[TestFixture]
public class ParserSyntaxModelTests
{
    [Test]
    public void Parse_FunctionCall_SeparatesNamedArgumentsWithExactSpans()
    {
        TemplateParseResult result = TemplateParser.ParseNative("{{ format(value, width = 3, culture=locale) }}");
        TemplateExpressionSyntax expression = result.Document.Children[0].Output!.Expression;

        Assert.That(result.HasDiagnostics(), Is.False);
        Assert.That(expression.Arguments.Count, Is.EqualTo(1));
        Assert.That(expression.Arguments[0].Text, Is.EqualTo("value"));
        Assert.That(expression.NamedArguments.Count, Is.EqualTo(2));
        Assert.That(expression.HashArguments, Is.Empty);
        Assert.That(expression.NamedArguments[0].Name, Is.EqualTo("width"));
        Assert.That(expression.NamedArguments[0].Value.Text, Is.EqualTo("3"));
        Assert.That(expression.NamedArguments[0].Span.GetText(), Is.EqualTo("width = 3"));
        Assert.That(expression.NamedArguments[0].NameSpan.GetText(), Is.EqualTo("width"));
        Assert.That(expression.NamedArguments[0].EqualsSpan.GetText(), Is.EqualTo("="));
        Assert.That(expression.NamedArguments[1].Span.GetText(), Is.EqualTo("culture=locale"));
    }

    [Test]
    public void Parse_WhitespaceHelper_SeparatesHashArguments()
    {
        TemplateParseResult result = TemplateParser.ParseNative("{{ helper value tone=\"loud\" enabled=true }}");
        TemplateExpressionSyntax expression = result.Document.Children[0].Output!.Expression;

        Assert.That(result.HasDiagnostics(), Is.False);
        Assert.That(expression.ExpressionKind, Is.EqualTo(TemplateExpressionSyntaxKind.Call));
        Assert.That(expression.CallKind, Is.EqualTo(TemplateExpressionCallKind.Subexpression));
        Assert.That(expression.Name, Is.EqualTo("helper"));
        Assert.That(expression.Arguments.Count, Is.EqualTo(1));
        Assert.That(expression.Arguments[0].Text, Is.EqualTo("value"));
        Assert.That(expression.HashArguments.Count, Is.EqualTo(2));
        Assert.That(expression.HashArguments[0].Hash, Is.True);
        Assert.That(expression.HashArguments[0].Name, Is.EqualTo("tone"));
        Assert.That(expression.HashArguments[0].Value.Text, Is.EqualTo("\"loud\""));
        Assert.That(expression.HashArguments[1].Name, Is.EqualTo("enabled"));
    }

    [Test]
    public void Parse_HandlebarsBlockStatement_SeparatesHashArguments()
    {
        TemplateParseResult result = TemplateParser.ParseNative("{% handlebarsBlock helper tone=\"loud\" %}x{% endhandlebarsBlock %}");
        TemplateStatementSyntax statement = result.Document.Children[0].Block!.StartStatement;

        Assert.That(result.HasDiagnostics(), Is.False);
        Assert.That(statement.Arguments.Count, Is.EqualTo(1));
        Assert.That(statement.Arguments[0].Text, Is.EqualTo("helper"));
        Assert.That(statement.NamedArguments, Is.Empty);
        Assert.That(statement.HashArguments.Count, Is.EqualTo(1));
        Assert.That(statement.HashArguments[0].Hash, Is.True);
        Assert.That(statement.HashArguments[0].Span.GetText(), Is.EqualTo("tone=\"loud\""));
    }

    [Test]
    public void Parse_BlockParameters_StoresAliasesOutsidePositionalArguments()
    {
        TemplateParseResult result = TemplateParser.ParseNative("{% handlebarsEach people as |person index| %}x{% endhandlebarsEach %}");
        TemplateStatementSyntax statement = result.Document.Children[0].Block!.StartStatement;

        Assert.That(result.HasDiagnostics(), Is.False);
        Assert.That(statement.Arguments.Count, Is.EqualTo(1));
        Assert.That(statement.Arguments[0].Text, Is.EqualTo("people"));
        Assert.That(statement.BlockParameters.Count, Is.EqualTo(2));
        Assert.That(statement.BlockParameters[0].Name, Is.EqualTo("person"));
        Assert.That(statement.BlockParameters[0].Span.GetText(), Is.EqualTo("person"));
        Assert.That(statement.BlockParameters[1].Name, Is.EqualTo("index"));
    }

    [Test]
    public void Parse_BranchChain_PreservesEachBranchAndLegacyFlattenedView()
    {
        string text = "{% if first %}A{% elseif second %}B{% elseif third %}C{% else %}D{% endif %}";
        TemplateParseResult result = TemplateParser.ParseNative(text);
        TemplateBlockSyntax block = result.Document.Children[0].Block!;

        Assert.That(result.HasDiagnostics(), Is.False);
        Assert.That(block.Branches.Count, Is.EqualTo(3));
        Assert.That(block.Branches[0].Statement.Name, Is.EqualTo("elseif"));
        Assert.That(block.Branches[0].Children[0].Text!.Text, Is.EqualTo("B"));
        Assert.That(block.Branches[0].Span.GetText(), Is.EqualTo("{% elseif second %}B"));
        Assert.That(block.Branches[1].Statement.Name, Is.EqualTo("elseif"));
        Assert.That(block.Branches[1].Children[0].Text!.Text, Is.EqualTo("C"));
        Assert.That(block.Branches[2].Statement.Name, Is.EqualTo("else"));
        Assert.That(block.Branches[2].Children[0].Text!.Text, Is.EqualTo("D"));
        Assert.That(block.Branches[2].Span.GetText(), Is.EqualTo("{% else %}D"));
        Assert.That(block.ElseChildren.Count, Is.EqualTo(6));
    }

    [Test]
    public void Parse_CompositionNames_DistinguishesStaticAndDynamicNames()
    {
        TemplateParseResult result = TemplateParser.ParseNative("{% partial \"card\" %}{% partial (partialName) %}{% include selected %}");
        TemplateStatementSyntax staticPartial = result.Document.Children[0].Statement!;
        TemplateStatementSyntax groupedDynamic = result.Document.Children[1].Statement!;
        TemplateStatementSyntax directDynamic = result.Document.Children[2].Statement!;

        Assert.That(result.HasDiagnostics(), Is.False);
        Assert.That(staticPartial.DynamicName, Is.Null);
        Assert.That(groupedDynamic.DynamicName, Is.Not.Null);
        Assert.That(groupedDynamic.DynamicName!.Span.GetText(), Is.EqualTo("(partialName)"));
        Assert.That(groupedDynamic.DynamicName.Expression.Text, Is.EqualTo("partialName"));
        Assert.That(directDynamic.DynamicName, Is.Not.Null);
        Assert.That(directDynamic.DynamicName!.Expression.Text, Is.EqualTo("selected"));
    }

    [Test]
    public void Parse_MalformedNamedArgument_ReportsPreciseRecoveryNodeAndContinues()
    {
        TemplateParseResult result = TemplateParser.ParseNative("{{ format(name, width=) }} tail");
        TemplateExpressionSyntax expression = result.Document.Children[0].Output!.Expression;

        Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
        Assert.That(result.Diagnostics[0].Code, Is.EqualTo("PARSE008"));
        Assert.That(result.Diagnostics[0].Span!.GetText(), Is.EqualTo("width="));
        Assert.That(expression.RecoveryNodes.Count, Is.EqualTo(1));
        Assert.That(expression.RecoveryNodes[0].Text, Is.EqualTo("width="));
        Assert.That(expression.RecoveryNodes[0].RecoveredTokens.Count, Is.EqualTo(2));
        Assert.That(expression.NamedArguments[0].Value.Span.Length, Is.EqualTo(0));
        Assert.That(result.Document.Children[1].Text!.Text, Is.EqualTo(" tail"));
    }

    [Test]
    public void Parse_UnknownStatement_RetainsExactSourceAndRecoveredTokens()
    {
        TemplateParseResult result = TemplateParser.ParseNative("before{% unknown value %}after");
        TemplateErrorSyntax error = result.Document.Children[1].Error!;

        Assert.That(result.Diagnostics[0].Code, Is.EqualTo("PARSE006"));
        Assert.That(error.Text, Is.EqualTo("{% unknown value %}"));
        Assert.That(error.Span.GetText(), Is.EqualTo(error.Text));
        Assert.That(error.RecoveredTokens.Count, Is.EqualTo(4));
        Assert.That(error.RecoveredTokens[0].Kind, Is.EqualTo(TemplateTokenKind.StatementStartDelimiter));
        Assert.That(error.RecoveredTokens[3].Kind, Is.EqualTo(TemplateTokenKind.StatementEndDelimiter));
        Assert.That(result.Document.Children[2].Text!.Text, Is.EqualTo("after"));
    }

    [Test]
    public void Parse_LexicalAndPathSpans_PreserveOriginalSourceSlices()
    {
        TemplateParseResult result = TemplateParser.ParseNative("{% include  user?.cards[ index ] %}");
        TemplateStatementSyntax statement = result.Document.Children[0].Statement!;
        TemplateExpressionSyntax expression = statement.Arguments[0];

        Assert.That(statement.NameSpan.GetText(), Is.EqualTo("include"));
        Assert.That(statement.ContentSpan.GetText(), Is.EqualTo("include  user?.cards[ index ]"));
        Assert.That(statement.Span.GetText(), Is.EqualTo("{% include  user?.cards[ index ] %}"));
        Assert.That(expression.Span.GetText(), Is.EqualTo("user?.cards[ index ]"));
        Assert.That(expression.PathSegments.Count, Is.EqualTo(3));
        Assert.That(expression.PathSegments[0].Span!.GetText(), Is.EqualTo("user"));
        Assert.That(expression.PathSegments[1].Span!.GetText(), Is.EqualTo("?.cards"));
        Assert.That(expression.PathSegments[2].Span!.GetText(), Is.EqualTo("[ index ]"));
    }

    [Test]
    public void Parse_UnterminatedBlockParameters_ReportsRecoveryAndStillBuildsBlock()
    {
        TemplateParseResult result = TemplateParser.ParseNative("{% handlebarsEach people as |person %}x{% endhandlebarsEach %}");
        TemplateStatementSyntax statement = result.Document.Children[0].Block!.StartStatement;

        Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
        Assert.That(result.Diagnostics[0].Code, Is.EqualTo("PARSE009"));
        Assert.That(statement.BlockParameters.Count, Is.EqualTo(1));
        Assert.That(statement.RecoveryNodes.Count, Is.EqualTo(1));
        Assert.That(statement.RecoveryNodes[0].Text, Is.EqualTo("as |person"));
        Assert.That(result.Document.Children[0].Block!.EndStatement!.Name, Is.EqualTo("endhandlebarsEach"));
    }

    [Test]
    public void Parse_MalformedBlockEnd_RetainsClosingTagAsRecoverySource()
    {
        TemplateParseResult result = TemplateParser.ParseNative("{% if ok %}x{% endfor %}");
        TemplateBlockSyntax block = result.Document.Children[0].Block!;

        Assert.That(result.Diagnostics.Count, Is.EqualTo(1));
        Assert.That(result.Diagnostics[0].Code, Is.EqualTo("PARSE003"));
        Assert.That(block.RecoveryNodes.Count, Is.EqualTo(1));
        Assert.That(block.RecoveryNodes[0].Text, Is.EqualTo("{% endfor %}"));
        Assert.That(block.RecoveryNodes[0].RecoveredTokens.Count, Is.EqualTo(3));
    }
}
