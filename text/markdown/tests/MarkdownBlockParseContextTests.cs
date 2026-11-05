using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class MarkdownBlockParseContextTests
{
    [Test]
    public void BlockParseContext_CreatesLinesFromParseContext()
    {
        MarkdownParseContext parseContext = MarkdownParser.CreateContext("a\nb", MarkdownPipeline.CommonMarkPreset());
        MarkdownBlockParseContext blockContext = MarkdownBlockParseContext.Create(parseContext);

        Assert.That(blockContext.ParseContext, Is.SameAs(parseContext));
        Assert.That(blockContext.GetLineCount(), Is.EqualTo(2));
        Assert.That(blockContext.LineIndex, Is.EqualTo(0));
        Assert.That(blockContext.HasCurrentLine(), Is.True);
        Assert.That(blockContext.GetCurrentLine()!.Text, Is.EqualTo("a"));
    }

    [Test]
    public void BlockParseContext_AdvancesAndResetsLines()
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create("a\nb", MarkdownPipeline.CommonMarkPreset());

        Assert.That(context.GetCurrentLine()!.Text, Is.EqualTo("a"));
        Assert.That(context.AdvanceLine(), Is.True);
        Assert.That(context.GetCurrentLine()!.Text, Is.EqualTo("b"));
        Assert.That(context.AdvanceLine(), Is.False);
        Assert.That(context.GetCurrentLine(), Is.Null);

        context.ResetLines();

        Assert.That(context.GetCurrentLine()!.Text, Is.EqualTo("a"));
    }

    [Test]
    public void BlockParseContext_UsesNormalizedSource()
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create("a\0", MarkdownPipeline.CommonMarkPreset());

        Assert.That(context.ParseContext.Source[1], Is.EqualTo(MarkdownInputNormalizer.NullReplacementCharacter));
        Assert.That(context.GetCurrentLine()!.Text[1], Is.EqualTo(MarkdownInputNormalizer.NullReplacementCharacter));
    }

    [Test]
    public void BlockParseContext_ManagesOpenContainers()
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create("", MarkdownPipeline.CommonMarkPreset());
        MarkdownNode quote = MarkdownNode.Create(MarkdownNodeKind.BlockQuote);

        context.PushContainer(quote, 0, 0, ">");

        Assert.That(context.GetOpenContainerCount(), Is.EqualTo(1));
        Assert.That(context.GetCurrentContainer()!.Node, Is.SameAs(quote));

        MarkdownContainerState? popped = context.PopContainer();

        Assert.That(popped!.Node, Is.SameAs(quote));
        Assert.That(popped.IsOpen, Is.False);
        Assert.That(context.GetOpenContainerCount(), Is.EqualTo(0));
    }

    [Test]
    public void BlockParseContext_RejectsInvalidArguments()
    {
        Assert.That(() => MarkdownBlockParseContext.Create((MarkdownParseContext)null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownBlockParseContext.Create(null!, MarkdownPipeline.CommonMarkPreset()), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownBlockParseContext.Create("", null!), Throws.TypeOf<ArgumentNullException>());
    }
}
