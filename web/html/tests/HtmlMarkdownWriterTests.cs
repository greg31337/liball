
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlMarkdownWriterTests
{
    [Test]
    public void ParagraphsAndContainersCleanUpWhitespaceAndBlankLines()
    {
        HtmlNode fragment = ParseFragment(
            " loose <p> alpha   beta <span> gamma </span> </p>"
            + "<div> delta <p>epsilon</p> zeta </div>");
        HtmlMarkdownWriter writer = CreateWriter(
            HtmlMarkdownOptions.CreateCommonMark());

        writer.WriteNode(fragment);

        Assert.That(writer.Complete(), Is.EqualTo(
            "loose\n\nalpha beta gamma\n\ndelta\n\nepsilon\n\nzeta"));
    }

    [Test]
    public void AtxAndSetextHeadingStylesProduceStableBlocks()
    {
        HtmlNode fragment = ParseFragment(
            "<h1># One <em>em</em></h1><h2>Two</h2><h3>Three</h3>");
        HtmlMarkdownWriter atx = CreateWriter(
            HtmlMarkdownOptions.CreateCommonMark());
        HtmlMarkdownOptions setextOptions =
            HtmlMarkdownOptions.CreateCommonMark();
        setextOptions.SetHeadingStyle(HtmlMarkdownHeadingStyle.Setext);
        setextOptions.SetNewLine("\r\n");
        HtmlMarkdownWriter setext = CreateWriter(setextOptions);

        atx.WriteNode(fragment);
        setext.WriteNode(fragment);

        Assert.Multiple(() =>
        {
            Assert.That(atx.Complete(), Is.EqualTo(
                "# # One *em*\n\n## Two\n\n### Three"));
            Assert.That(setext.Complete(), Is.EqualTo(
                "\\# One *em*\r\n===\r\n\r\n"
                + "Two\r\n---\r\n\r\n### Three"));
        });
    }

    [Test]
    public void EmphasisAndStrongTrimInteriorEdgesAndSuppressEmptyMarkers()
    {
        HtmlNode fragment = ParseFragment(
            "<p>before <em> alpha <strong>bold</strong> </em> after "
            + "<strong><em>both</em></strong> <em> </em>end "
            + "<i>a*b</i></p>");
        HtmlMarkdownWriter writer = CreateWriter(
            HtmlMarkdownOptions.CreateCommonMark());

        writer.WriteNode(fragment);

        Assert.That(writer.Complete(), Is.EqualTo(
            "before *alpha **bold*** after ***both*** end *a\\*b*"));
    }

    [Test]
    public void StrikethroughIsSemanticOnlyInEnabledProfiles()
    {
        HtmlNode fragment = ParseFragment(
            "<p>before <del> old </del> <s>new</s> "
            + "<strike>legacy</strike> after</p>");
        HtmlMarkdownWriter common = CreateWriter(
            HtmlMarkdownOptions.CreateCommonMark());
        HtmlMarkdownWriter github = CreateWriter(
            HtmlMarkdownOptions.CreateGitHubFlavored());

        common.WriteNode(fragment);
        github.WriteNode(fragment);

        Assert.Multiple(() =>
        {
            Assert.That(common.Complete(),
                Is.EqualTo("before old new legacy after"));
            Assert.That(github.Complete(), Is.EqualTo(
                "before ~~old~~ ~~new~~ ~~legacy~~ after"));
        });
    }

    [Test]
    public void HtmlBreaksBecomeHardBreaksAndTrailingBreaksAreCleaned()
    {
        HtmlNode fragment = ParseFragment(
            "<p>one<br>two <br> three<br></p><p>after</p>");
        HtmlMarkdownOptions options =
            HtmlMarkdownOptions.CreateCommonMark();
        options.SetNewLine("\r\n");
        HtmlMarkdownWriter writer = CreateWriter(options);

        writer.WriteNode(fragment);

        Assert.That(writer.Complete(), Is.EqualTo(
            "one  \r\ntwo  \r\nthree\r\n\r\nafter"));
    }

    [Test]
    public void ExplicitSoftAndHardBreakRequestsRemainDistinct()
    {
        HtmlMarkdownWriter writer = CreateWriter(
            HtmlMarkdownOptions.CreateCommonMark());

        writer.WriteText("one");
        writer.WriteSoftBreak();
        writer.WriteText("two");
        writer.WriteHardBreak();
        writer.WriteText("three");

        Assert.That(writer.Complete(),
            Is.EqualTo("one\ntwo  \nthree"));
    }

    [Test]
    public void ThematicBreaksSeparateSurroundingBlocksAndEmptyBlocksVanish()
    {
        HtmlNode fragment = ParseFragment(
            "<p>before</p><div> </div><hr><p></p><p>after</p>");
        HtmlMarkdownWriter writer = CreateWriter(
            HtmlMarkdownOptions.CreateCommonMark());

        writer.WriteNode(fragment);

        Assert.That(writer.Complete(),
            Is.EqualTo("before\n\n---\n\nafter"));
    }

    [Test]
    public void BlockLineStartsAreEscapedAndHiddenContentIsExcluded()
    {
        HtmlNode fragment = ParseFragment(
            "<div># heading</div><div>1. list</div>"
            + "<p>&lt;tag&gt; 東京 <span hidden>secret</span>"
            + "<script>bad()</script></p>");
        HtmlMarkdownWriter writer = CreateWriter(
            HtmlMarkdownOptions.CreateCommonMark());

        writer.WriteNode(fragment);

        Assert.That(writer.Complete(), Is.EqualTo(
            "\\# heading\n\n1\\. list\n\n\\<tag> 東京"));
    }

    [Test]
    public void WriterHonorsPreCancelledTokensAndValidatesArguments()
    {
        using CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();
        HtmlCompiledMarkdownOptions options =
            HtmlMarkdownOptions.CreateCommonMark().Compile();
        HtmlMarkdownWriter cancelled = HtmlMarkdownWriter.Create(options,
            source.Token);
        HtmlMarkdownWriter writer = HtmlMarkdownWriter.Create(options,
            CancellationToken.None);
        HtmlNode fragment = ParseFragment("<p>text</p>");

        Assert.Multiple(() =>
        {
            Assert.Throws<OperationCanceledException>(() =>
                cancelled.WriteNode(fragment));
            Assert.Throws<ArgumentNullException>(() =>
                writer.WriteNode(null));
            Assert.Throws<ArgumentNullException>(() =>
                writer.WriteText(null));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlMarkdownWriter.Create(null, CancellationToken.None));
        });
    }

    static HtmlMarkdownWriter CreateWriter(HtmlMarkdownOptions options)
    {
        return HtmlMarkdownWriter.Create(options.Compile(),
            CancellationToken.None);
    }

    static HtmlNode ParseFragment(string html)
    {
        return Html.ParseFragment(html, "body").GetFragment();
    }
}
