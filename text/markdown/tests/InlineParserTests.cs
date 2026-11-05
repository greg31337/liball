using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class InlineParserTests
{
    [Test]
    public void InlineRule_InvokesHandler()
    {
        int calls = 0;
        MarkdownInlineRule rule = new MarkdownInlineRule(
            "test",
            context =>
            {
                calls = calls + 1;
                return false;
            });
        MarkdownInlineParseContext context = CreateInlineContext("text", MarkdownPipeline.CommonMarkPreset());

        bool matched = rule.TryParse(context);

        Assert.That(matched, Is.False);
        Assert.That(calls, Is.EqualTo(1));
        Assert.That(rule.Name, Is.EqualTo("test"));
    }

    [Test]
    public void InlineContext_TracksSourcePositionAndAppendsInlineNodes()
    {
        MarkdownInlineParseContext context = CreateInlineContext("abc", MarkdownPipeline.CommonMarkPreset());
        MarkdownNode text = MarkdownNode.Create(MarkdownNodeKind.Text);
        text.SetLiteral("a");

        Assert.That(context.HasCurrentCharacter(), Is.True);
        Assert.That(context.GetCurrentCharacter(), Is.EqualTo('a'));
        Assert.That(context.GetRemainingText(), Is.EqualTo("abc"));

        context.AppendInline(text);
        context.Advance();

        Assert.That(context.Position, Is.EqualTo(1));
        Assert.That(context.GetCurrentCharacter(), Is.EqualTo('b'));
        Assert.That(context.GetRemainingText(), Is.EqualTo("bc"));
        Assert.That(context.Container.GetChild(0), Is.SameAs(text));

        context.AdvanceBy(2);

        Assert.That(context.HasCurrentCharacter(), Is.False);
        Assert.That(context.GetCurrentCharacter(), Is.EqualTo('\0'));
        Assert.That(context.GetRemainingText(), Is.EqualTo(""));
    }

    [Test]
    public void InlineParser_WithNoRulesAdvancesThroughSource()
    {
        MarkdownInlineParseContext context = CreateInlineContext("abc", MarkdownPipeline.ZeroPreset());

        InlineParser.Run(context);

        Assert.That(context.Position, Is.EqualTo(3));
        Assert.That(context.Container.GetChildCount(), Is.EqualTo(0));
        Assert.That(context.ParseContext.Document.HasDiagnostics(), Is.False);
    }

    [Test]
    public void InlineParser_AppliesOrderedRulesUntilOneMatches()
    {
        int firstCalls = 0;
        int secondCalls = 0;
        MarkdownInlineRule first = new MarkdownInlineRule(
            "first",
            context =>
            {
                firstCalls = firstCalls + 1;
                return false;
            });
        MarkdownInlineRule second = new MarkdownInlineRule(
            "second",
            context =>
            {
                AppendCurrentCharacter(context);
                secondCalls = secondCalls + 1;
                return true;
            });
        MarkdownPipeline pipeline = new MarkdownPipelineBuilder()
            .AddNamedInlineRule("first", first)
            .AddNamedInlineRule("second", second)
            .Build();
        MarkdownInlineParseContext context = CreateInlineContext("ab", pipeline);

        InlineParser.Run(context);

        Assert.That(firstCalls, Is.EqualTo(2));
        Assert.That(secondCalls, Is.EqualTo(2));
        Assert.That(context.Container.GetChildCount(), Is.EqualTo(2));
        Assert.That(context.Container.GetChild(0).Literal, Is.EqualTo("a"));
        Assert.That(context.Container.GetChild(1).Literal, Is.EqualTo("b"));
    }

    [Test]
    public void MarkdownParser_RunsInlineParserAfterBlockParser()
    {
        MarkdownDocument document = MarkdownParser.Parse("# Hi");
        MarkdownNode heading = document.Root.GetChild(0);

        Assert.That(heading.Kind, Is.EqualTo(MarkdownNodeKind.Heading));
        Assert.That(heading.Literal, Is.EqualTo("Hi"));
        Assert.That(heading.GetChildCount(), Is.EqualTo(1));
        Assert.That(heading.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Text));
        Assert.That(heading.GetChild(0).Literal, Is.EqualTo("Hi"));
    }

    [Test]
    public void InlineParser_TraversesNestedInlineContainers()
    {
        MarkdownDocument document = MarkdownParser.Parse("- one");
        MarkdownNode paragraph = document.Root.GetChild(0).GetChild(0).GetChild(0);

        Assert.That(paragraph.Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(paragraph.GetChildCount(), Is.EqualTo(1));
        Assert.That(paragraph.GetChild(0).Literal, Is.EqualTo("one"));
    }

    [Test]
    public void InlineParser_AddsDiagnosticWhenRuleDoesNotAdvance()
    {
        MarkdownInlineRule rule = new MarkdownInlineRule(
            "stuck",
            context =>
            {
                return true;
            });
        MarkdownPipeline pipeline = new MarkdownPipelineBuilder()
            .AddNamedInlineRule("stuck", rule)
            .Build();
        MarkdownInlineParseContext context = CreateInlineContext("a", pipeline);

        InlineParser.Run(context);

        Assert.That(context.ParseContext.Document.HasErrors(), Is.True);
        Assert.That(context.ParseContext.Document.GetDiagnostics()[0].Code, Is.EqualTo(InlineParser.NoProgressDiagnosticCode));
        Assert.That(context.Position, Is.EqualTo(1));
    }

    [Test]
    public void InlineParser_AddsDiagnosticForInvalidRegisteredRuleObjects()
    {
        MarkdownRuleList inlineRules = new MarkdownRuleList();
        inlineRules.Add("invalid", new object());
        MarkdownPipeline pipeline = MarkdownPipeline.Create(
            MarkdownOptions.CommonMarkPreset(),
            new MarkdownRuleList(),
            inlineRules,
            new MarkdownRuleList(),
            new MarkdownRuleList());
        MarkdownInlineParseContext context = CreateInlineContext("a", pipeline);

        InlineParser.Run(context);

        Assert.That(context.ParseContext.Document.HasErrors(), Is.True);
        Assert.That(context.ParseContext.Document.GetDiagnostics()[0].Code, Is.EqualTo(InlineParser.InvalidRuleDiagnosticCode));
        Assert.That(context.Position, Is.EqualTo(1));
    }

    [Test]
    public void InlineParser_ClassifiesInlineContainers()
    {
        Assert.That(InlineParser.IsInlineContainer(MarkdownNode.Create(MarkdownNodeKind.Paragraph)), Is.True);
        Assert.That(InlineParser.IsInlineContainer(MarkdownNode.Create(MarkdownNodeKind.Heading)), Is.True);
        Assert.That(InlineParser.IsInlineContainer(MarkdownNode.Create(MarkdownNodeKind.TableCell)), Is.True);
        Assert.That(InlineParser.IsInlineContainer(MarkdownNode.Create(MarkdownNodeKind.FencedCodeBlock)), Is.False);
        Assert.That(InlineParser.IsInlineContainer(MarkdownNode.Create(MarkdownNodeKind.Text)), Is.False);
    }

    [Test]
    public void InlineContext_TracksWhenContainerIsInsideLink()
    {
        MarkdownParseContext parseContext = MarkdownParser.CreateContext("", MarkdownPipeline.CommonMarkPreset());
        MarkdownNode link = MarkdownNode.Create(MarkdownNodeKind.Link);
        MarkdownNode emphasis = MarkdownNode.Create(MarkdownNodeKind.Emphasis);
        link.AppendChild(emphasis);

        MarkdownInlineParseContext linkContext = MarkdownInlineParseContext.Create(parseContext, link, "text");
        MarkdownInlineParseContext childContext = MarkdownInlineParseContext.Create(parseContext, emphasis, "text");

        Assert.That(linkContext.InLink, Is.True);
        Assert.That(childContext.InLink, Is.True);
        Assert.That(MarkdownInlineParseContext.IsInLinkContainer(emphasis), Is.True);
    }

    [Test]
    public void InlineParser_RejectsInvalidArguments()
    {
        MarkdownParseContext parseContext = MarkdownParser.CreateContext("", MarkdownPipeline.CommonMarkPreset());
        MarkdownNode paragraph = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        MarkdownInlineParseContext context = MarkdownInlineParseContext.Create(parseContext, paragraph, "text");

        Assert.That(() => new MarkdownInlineRule(null!, inlineContext => false), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new MarkdownInlineRule("", inlineContext => false), Throws.TypeOf<ArgumentException>());
        Assert.That(() => new MarkdownInlineRule("rule", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new MarkdownInlineRule("rule", inlineContext => false).TryParse(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownInlineParseContext.Create(null!, paragraph), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownInlineParseContext.Create(parseContext, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownInlineParseContext.Create(null!, paragraph, ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownInlineParseContext.Create(parseContext, null!, ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownInlineParseContext.Create(parseContext, paragraph, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownInlineParseContext.IsInLinkContainer(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => context.AdvanceBy(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => context.AppendInline(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => context.AppendInline(MarkdownNode.Create(MarkdownNodeKind.Paragraph)), Throws.TypeOf<ArgumentException>());
        Assert.That(() => InlineParser.Parse(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => InlineParser.ParseNode(null!, paragraph), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => InlineParser.ParseNode(parseContext, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => InlineParser.Run(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => InlineParser.TryApplyRules(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => InlineParser.IsInlineContainer(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => InlineParser.GetCurrentSpan(null!), Throws.TypeOf<ArgumentNullException>());
    }

    static MarkdownInlineParseContext CreateInlineContext(string source, MarkdownPipeline pipeline)
    {
        MarkdownParseContext parseContext = MarkdownParser.CreateContext("", pipeline);
        MarkdownNode paragraph = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        paragraph.SetLiteral(source);
        parseContext.Document.Root.AppendChild(paragraph);
        return MarkdownInlineParseContext.Create(parseContext, paragraph);
    }

    static void AppendCurrentCharacter(MarkdownInlineParseContext context)
    {
        MarkdownNode text = MarkdownNode.Create(MarkdownNodeKind.Text);
        text.SetLiteral(context.GetCurrentCharacter().ToString());
        context.AppendInline(text);
        context.Advance();
    }
}
