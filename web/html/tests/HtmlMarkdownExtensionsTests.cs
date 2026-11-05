
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlMarkdownExtensionsTests
{
    [Test]
    public void DefinitionListsAreOptionalAndIndentContinuationBlocks()
    {
        HtmlNode fragment = ParseFragment(
            "<dl><dt><em>Term</em></dt><dd>First</dd>"
            + "<dd><p>Second</p><p>More</p></dd></dl><p>After</p>");
        string common = Render(fragment,
            HtmlMarkdownOptions.CreateCommonMark());
        string readable = Render(fragment,
            HtmlMarkdownOptions.CreateReadable());

        Assert.Multiple(() =>
        {
            Assert.That(common, Is.EqualTo(
                "*Term*\n\nFirst\n\nSecond\n\nMore\n\nAfter"));
            Assert.That(readable, Is.EqualTo(
                "*Term*\n: First\n: Second\n\n  More\n\nAfter"));
        });
    }

    [Test]
    public void SubscriptAndSuperscriptEscapeExtensionDelimitersAndSpaces()
    {
        HtmlNode fragment = ParseFragment(
            "<p>H<sub>2</sub>O, x<sup>a b^c</sup>, "
            + "and <sub>a~b</sub>.</p>");
        string common = Render(fragment,
            HtmlMarkdownOptions.CreateCommonMark());
        string readable = Render(fragment,
            HtmlMarkdownOptions.CreateReadable());

        Assert.Multiple(() =>
        {
            Assert.That(common, Is.EqualTo("H2O, xa b^c, and a~b."));
            Assert.That(readable, Is.EqualTo(
                "H~2~O, x^a\\ b\\^c^, and ~a\\~b~."));
        });
    }

    [Test]
    public void SemanticFootnotesRenderDefinitionsAndDropBacklinks()
    {
        HtmlNode fragment = ParseFragment(
            "<p>Text<a role=doc-noteref href='#note-1'><sup>1</sup></a>."
            + "</p><section role=doc-endnotes><ol>"
            + "<li id=note-1 role=doc-endnote>"
            + "<p>First <em>note</em>.</p><p>More.</p>"
            + "<a role=doc-backlink href=#ref>back</a>"
            + "</li></ol></section>");
        HtmlMarkdownOptions options =
            HtmlMarkdownOptions.CreateCommonMark();
        options.SetEnableFootnotes(true);
        options.SetEnableSuperscript(true);

        string markdown = Render(fragment, options);

        Assert.That(markdown, Is.EqualTo(
            "Text[^note-1].\n\n"
            + "[^note-1]: First *note*.\n\n    More."));
    }

    [Test]
    public void SemanticFootnoteListsWorkWithoutAnEndnotesContainer()
    {
        HtmlNode fragment = ParseFragment(
            "<p>A<a role=doc-noteref href='#fn:one'>1</a>.</p>"
            + "<ol><li id='fn:one' role=doc-footnote>Definition</li></ol>");
        HtmlMarkdownOptions options =
            HtmlMarkdownOptions.CreateCommonMark();
        options.SetEnableFootnotes(true);

        string markdown = Render(fragment, options);

        Assert.That(markdown, Is.EqualTo(
            "A[^fn-one].\n\n[^fn-one]: Definition"));
    }

    [Test]
    public void MixedOrdinaryAndFootnoteItemsRemainAnOrderedList()
    {
        HtmlNode fragment = ParseFragment(
            "<ol><li>Ordinary</li>"
            + "<li id=note role=doc-endnote>Note</li></ol>");
        HtmlMarkdownOptions options =
            HtmlMarkdownOptions.CreateCommonMark();
        options.SetEnableFootnotes(true);

        string markdown = Render(fragment, options);

        Assert.That(markdown, Is.EqualTo("1. Ordinary\n2. Note"));
    }

    [Test]
    public void EndnotesContainersInferIdentifiedListItems()
    {
        HtmlNode fragment = ParseFragment(
            "<p>A<a role=doc-noteref href=#note>1</a>.</p>"
            + "<section role=doc-endnotes><ol>"
            + "<li id=note>Inferred definition</li></ol></section>");
        HtmlMarkdownOptions options =
            HtmlMarkdownOptions.CreateCommonMark();
        options.SetEnableFootnotes(true);

        string markdown = Render(fragment, options);

        Assert.That(markdown, Is.EqualTo(
            "A[^note].\n\n[^note]: Inferred definition"));
    }

    [Test]
    public void TrustedRawExtensionsRequireExplicitTrust()
    {
        HtmlNode fragment = ParseFragment(
            "<p>Press <kbd title=Key>Ctrl</kbd> <mark>now</mark>.</p>"
            + "<details open><summary>More</summary>"
            + "<p>Body &amp; text</p></details>");
        HtmlMarkdownOptions untrusted =
            HtmlMarkdownOptions.CreateCommonMark();
        HtmlMarkdownOptions trusted = untrusted.Copy();
        trusted.SetEnableTrustedRawHtml(true);

        string safe = Render(fragment, untrusted);
        string raw = Render(fragment, trusted);

        Assert.Multiple(() =>
        {
            Assert.That(safe, Is.EqualTo(
                "Press Ctrl now.\n\nMore\n\nBody \\& text"));
            Assert.That(safe, Does.Not.Contain("<kbd"));
            Assert.That(safe, Does.Not.Contain("<details"));
            Assert.That(raw, Is.EqualTo(
                "Press <kbd title=\"Key\">Ctrl</kbd> "
                + "<mark>now</mark>.\n\n"
                + "<details open><summary>More</summary>"
                + "<p>Body &amp; text</p></details>"));
        });
    }

    static string Render(HtmlNode node, HtmlMarkdownOptions options)
    {
        HtmlMarkdownWriter writer = HtmlMarkdownWriter.Create(
            options.Compile(), CancellationToken.None);
        writer.WriteNode(node);
        return writer.Complete();
    }

    static HtmlNode ParseFragment(string html)
    {
        return Html.ParseFragment(html, "body").GetFragment();
    }
}
