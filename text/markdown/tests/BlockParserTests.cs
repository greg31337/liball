using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class BlockParserTests
{
    [Test]
    public void BlockRule_InvokesHandler()
    {
        int calls = 0;
        MarkdownBlockRule rule = new MarkdownBlockRule(
            "test",
            context =>
            {
                calls = calls + 1;
                return false;
            });

        bool matched = rule.TryParse(MarkdownBlockParseContext.Create("", MarkdownPipeline.CommonMarkPreset()));

        Assert.That(matched, Is.False);
        Assert.That(calls, Is.EqualTo(1));
        Assert.That(rule.Name, Is.EqualTo("test"));
    }

    [Test]
    public void BlockParser_WithNoRulesAdvancesThroughAllLines()
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create("a\nb", MarkdownPipeline.ZeroPreset());

        BlockParser.Run(context);

        Assert.That(context.LineIndex, Is.EqualTo(2));
        Assert.That(context.HasCurrentLine(), Is.False);
        Assert.That(context.ParseContext.Document.Root.GetChildCount(), Is.EqualTo(0));
    }

    [Test]
    public void BlockParser_AppliesOrderedRulesUntilOneMatches()
    {
        int firstCalls = 0;
        int secondCalls = 0;
        MarkdownBlockRule first = new MarkdownBlockRule(
            "first",
            context =>
            {
                firstCalls = firstCalls + 1;
                return false;
            });
        MarkdownBlockRule second = new MarkdownBlockRule(
            "second",
            context =>
            {
                MarkdownLine line = context.GetCurrentLine()!;
                MarkdownNode paragraph = MarkdownNode.Create(MarkdownNodeKind.Paragraph, line.ContentSpan);
                paragraph.SetLiteral(line.Text);
                context.ParseContext.Document.Root.AppendChild(paragraph);
                context.AdvanceLine();
                secondCalls = secondCalls + 1;
                return true;
            });
        MarkdownPipeline pipeline = new MarkdownPipelineBuilder()
            .AddNamedBlockRule("first", first)
            .AddNamedBlockRule("second", second)
            .Build();

        MarkdownDocument document = MarkdownParser.Parse("a\nb", pipeline);

        Assert.That(firstCalls, Is.EqualTo(2));
        Assert.That(secondCalls, Is.EqualTo(2));
        Assert.That(document.Root.GetChildCount(), Is.EqualTo(2));
        Assert.That(document.Root.GetChild(0).Literal, Is.EqualTo("a"));
        Assert.That(document.Root.GetChild(1).Literal, Is.EqualTo("b"));
    }

    [Test]
    public void BlockParser_AddsDiagnosticWhenRuleDoesNotAdvance()
    {
        MarkdownBlockRule rule = new MarkdownBlockRule(
            "stuck",
            context =>
            {
                return true;
            });
        MarkdownPipeline pipeline = new MarkdownPipelineBuilder()
            .AddNamedBlockRule("stuck", rule)
            .Build();

        MarkdownDocument document = MarkdownParser.Parse("a", pipeline);

        Assert.That(document.HasErrors(), Is.True);
        Assert.That(document.GetDiagnostics()[0].Code, Is.EqualTo(BlockParser.NoProgressDiagnosticCode));
    }

    [Test]
    public void BlockParser_AddsDiagnosticForInvalidRegisteredRuleObjects()
    {
        MarkdownRuleList blockRules = MarkdownPipeline.CommonMarkPreset().GetBlockRules();
        blockRules.Insert(0, "invalid", new object());
        MarkdownPipeline pipeline = MarkdownPipeline.Create(
            MarkdownOptions.CommonMarkPreset(),
            blockRules,
            new MarkdownRuleList(),
            new MarkdownRuleList(),
            new MarkdownRuleList());

        MarkdownDocument document = MarkdownParser.Parse("a", pipeline);

        Assert.That(document.HasErrors(), Is.True);
        Assert.That(document.GetDiagnostics()[0].Code, Is.EqualTo(BlockParser.InvalidRuleDiagnosticCode));
    }

    [Test]
    public void BlockParser_GetCurrentSpanFallsBackToDocumentSpan()
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create("", MarkdownPipeline.CommonMarkPreset());

        Assert.That(BlockParser.GetCurrentSpan(context).IsEmpty(), Is.True);
    }

    [Test]
    public void BlockParser_RejectsInvalidArguments()
    {
        Assert.That(() => new MarkdownBlockRule(null!, context => false), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new MarkdownBlockRule("", context => false), Throws.TypeOf<ArgumentException>());
        Assert.That(() => new MarkdownBlockRule("rule", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new MarkdownBlockRule("rule", context => false).TryParse(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => BlockParser.Parse(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => BlockParser.Run(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => BlockParser.TryApplyRules(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => BlockParser.GetCurrentSpan(null!), Throws.TypeOf<ArgumentNullException>());
    }
}
