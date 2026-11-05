using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class GfmBareAutolinkTests
{
    [Test]
    public void BareAutolinkRule_RendersUrlsWwwAndEmailsInGfm()
    {
        MarkdownPipeline pipeline = MarkdownPipeline.GfmPreset();

        string html = Markdown.ToHtml("Visit https://example.com, www.example.org and user@example.com.", pipeline);

        Assert.That(html, Is.EqualTo(
            "<p>Visit <a href=\"https://example.com\">https://example.com</a>, " +
            "<a href=\"http://www.example.org\">www.example.org</a> and " +
            "<a href=\"mailto:user@example.com\">user@example.com</a>.</p>\n"));
    }

    [Test]
    public void BareAutolinkRule_CreatesLinkNodes()
    {
        MarkdownDocument document = MarkdownParser.Parse("see https://example.com/path?q=1 now", MarkdownPipeline.GfmPreset());
        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownNode link = paragraph.GetChild(1);

        Assert.That(paragraph.GetChild(0).Literal, Is.EqualTo("see "));
        Assert.That(link.Kind, Is.EqualTo(MarkdownNodeKind.Link));
        Assert.That(link.GetAttributeValue("destination"), Is.EqualTo("https://example.com/path?q=1"));
        Assert.That(link.GetChild(0).Literal, Is.EqualTo("https://example.com/path?q=1"));
        Assert.That(paragraph.GetChild(2).Literal, Is.EqualTo(" now"));
    }

    [Test]
    public void BareAutolinkRule_DoesNotParseWithoutGfmFeature()
    {
        MarkdownDocument document = MarkdownParser.Parse("see https://example.com and user@example.com", MarkdownPipeline.CommonMarkPreset());

        Assert.That(ContainsInlineKind(document.Root, MarkdownNodeKind.Link), Is.False);
        Assert.That(ConcatenateInlineLiterals(document.Root.GetChild(0)), Is.EqualTo("see https://example.com and user@example.com"));
    }

    [Test]
    public void BareAutolinkRule_DoesNotCreateNestedLinks()
    {
        MarkdownDocument document = MarkdownParser.Parse("[https://inner.example](https://outer.example)", MarkdownPipeline.GfmPreset());
        MarkdownNode link = document.Root.GetChild(0).GetChild(0);

        Assert.That(link.Kind, Is.EqualTo(MarkdownNodeKind.Link));
        Assert.That(link.GetAttributeValue("destination"), Is.EqualTo("https://outer.example"));
        Assert.That(ContainsInlineKind(link, MarkdownNodeKind.Link), Is.False);
        Assert.That(ConcatenateInlineLiterals(link), Is.EqualTo("https://inner.example"));
    }

    [Test]
    public void BareAutolinkRule_TrimsTrailingPunctuationAndUnmatchedParens()
    {
        MarkdownPipeline pipeline = MarkdownPipeline.GfmPreset();

        Assert.That(Markdown.ToHtml("(https://example.com/foo).", pipeline), Is.EqualTo(
            "<p>(<a href=\"https://example.com/foo\">https://example.com/foo</a>).</p>\n"));
        Assert.That(Markdown.ToHtml("https://example.com/a_(b)", pipeline), Is.EqualTo(
            "<p><a href=\"https://example.com/a_(b)\">https://example.com/a_(b)</a></p>\n"));
        Assert.That(Markdown.ToHtml("www.example.com?a=one&hl;", pipeline), Is.EqualTo(
            "<p><a href=\"http://www.example.com?a=one\">www.example.com?a=one</a>&amp;hl;</p>\n"));
    }

    [Test]
    public void BareAutolinkRule_HandlesGfmEmailAndProtocolForms()
    {
        MarkdownPipeline pipeline = MarkdownPipelineBuilder.GfmPreset()
            .AllowUnsafeLinks()
            .Build();

        Assert.That(Markdown.ToHtml("a.b-c_d@a.b.", pipeline), Is.EqualTo(
            "<p><a href=\"mailto:a.b-c_d@a.b\">a.b-c_d@a.b</a>.</p>\n"));
        Assert.That(Markdown.ToHtml("mailto:a.b-c_d@a.b/", pipeline), Is.EqualTo(
            "<p><a href=\"mailto:a.b-c_d@a.b\">mailto:a.b-c_d@a.b</a>/</p>\n"));
        Assert.That(Markdown.ToHtml("xmpp:foo@bar.baz/txt/bin", pipeline), Is.EqualTo(
            "<p><a href=\"xmpp:foo@bar.baz/txt\">xmpp:foo@bar.baz/txt</a>/bin</p>\n"));
        Assert.That(Markdown.ToHtml("a@b.c-", pipeline), Is.EqualTo("<p>a@b.c-</p>\n"));
    }

    [Test]
    public void BareAutolinkRule_HelperMethodsClassifyMatches()
    {
        MarkdownBareAutolinkMatch url = GfmInlineRules.FindBareUrlAutolink("See HTTP://Example.com.", 4);
        MarkdownBareAutolinkMatch www = GfmInlineRules.FindBareUrlAutolink("www.example.com", 0);
        MarkdownBareAutolinkMatch email = GfmInlineRules.FindBareEmailAutolink("user.name+tag@example.co.uk,", 0);
        MarkdownBareAutolinkMatch protocol = GfmInlineRules.FindBareProtocolAutolink("xmpp:user@example.com/chat", 0);

        Assert.That(url.Success, Is.True);
        Assert.That(url.Text, Is.EqualTo("HTTP://Example.com"));
        Assert.That(url.Destination, Is.EqualTo("HTTP://Example.com"));
        Assert.That(www.Destination, Is.EqualTo("http://www.example.com"));
        Assert.That(email.Success, Is.True);
        Assert.That(email.Text, Is.EqualTo("user.name+tag@example.co.uk"));
        Assert.That(email.Destination, Is.EqualTo("mailto:user.name+tag@example.co.uk"));
        Assert.That(protocol.Destination, Is.EqualTo("xmpp:user@example.com/chat"));
        Assert.That(GfmInlineRules.IsGfmBareEmailAddress("a_b@c.d"), Is.True);
        Assert.That(GfmInlineRules.IsGfmBareEmailAddress("a@b.c_"), Is.False);
        Assert.That(GfmInlineRules.IsPotentialBareAutolinkStart("go https://example.com", 3), Is.True);
        Assert.That(GfmInlineRules.IsPotentialBareAutolinkStart("go user@example.com", 3), Is.True);
        Assert.That(GfmInlineRules.HasBareAutolinkBoundary("go https://example.com", 3), Is.True);
        Assert.That(GfmInlineRules.HasBareAutolinkBoundary("xhttps://example.com", 1), Is.False);
        Assert.That(GfmInlineRules.TrimBareUrlEnd("https://example.com/foo).", 0, 25), Is.EqualTo(23));
        Assert.That(GfmInlineRules.HasMoreClosingParensThanOpening("a(b))", 0, 5), Is.True);
    }

    [Test]
    public void BareAutolinkRule_CanBeEnabledWithoutStrikethrough()
    {
        MarkdownOptions options = MarkdownOptions.CommonMarkPreset();
        options.Features.GfmBareAutolinks = true;
        MarkdownPipeline pipeline = MarkdownPipeline.Create(options);

        Assert.That(pipeline.GetInlineRules().Contains(GfmInlineRules.BareAutolinkRuleName), Is.True);
        Assert.That(pipeline.GetInlineRules().Contains(GfmInlineRules.StrikethroughRuleName), Is.False);
        Assert.That(Markdown.ToHtml("see https://example.com", pipeline), Is.EqualTo("<p>see <a href=\"https://example.com\">https://example.com</a></p>\n"));
    }

    [Test]
    public void BareAutolinkRule_RejectsInvalidArguments()
    {
        MarkdownBareAutolinkMatch defaultMatch = new MarkdownBareAutolinkMatch();

        Assert.That(defaultMatch.Success, Is.False);
        Assert.That(defaultMatch.EndIndex, Is.EqualTo(-1));
        Assert.That(() => GfmInlineRules.RegisterGfmBareAutolinkRule(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmInlineRules.TryParseBareAutolink(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmInlineRules.FindBareAutolink(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmInlineRules.FindBareAutolink("", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => GfmInlineRules.FindBareUrlAutolink(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmInlineRules.FindBareEmailAutolink(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmInlineRules.FindBareProtocolAutolink(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmInlineRules.IsGfmBareEmailAddress(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmInlineRules.IsPotentialBareAutolinkStart(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmInlineRules.CouldStartBareEmail(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmInlineRules.HasBareAutolinkBoundary(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmInlineRules.FindBareUrlEnd(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmInlineRules.TrimBareUrlEnd(null!, 0, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmInlineRules.HasMoreClosingParensThanOpening(null!, 0, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmInlineRules.StartsWithIgnoreCaseAt(null!, 0, "x"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmInlineRules.StartsWithIgnoreCaseAt("x", 0, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownBareAutolinkMatch.Create(true, 0, null!, "x"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownBareAutolinkMatch.Create(true, 0, "x", null!), Throws.TypeOf<ArgumentNullException>());
    }

    public static bool ContainsInlineKind(MarkdownNode node, MarkdownNodeKind kind)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        int index = 0;
        while (index < node.GetChildCount())
        {
            MarkdownNode child = node.GetChild(index);
            if (child.Kind == kind || ContainsInlineKind(child, kind))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    public static string ConcatenateInlineLiterals(MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (node.GetChildCount() == 0)
        {
            return node.Literal;
        }

        string text = "";
        int index = 0;
        while (index < node.GetChildCount())
        {
            text = text + ConcatenateInlineLiterals(node.GetChild(index));
            index = index + 1;
        }

        return text;
    }
}
