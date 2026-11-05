
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlMarkdownLinksTests
{
    [Test]
    public void InlineLinksResolveBasesAndEscapeLabelsTargetsAndTitles()
    {
        HtmlNode body = ParseBodyWithBase(
            "<p><a href='guide(a)' title='say &quot;hi&quot; \\ ok'>"
            + "Guide [new]</a></p>",
            "https://example.test/docs/page.html");

        string markdown = Render(body,
            HtmlMarkdownOptions.CreateCommonMark());

        Assert.That(markdown, Is.EqualTo(
            "[Guide \\[new\\]](https://example.test/docs/guide\\(a\\) "
            + "\"say \\\"hi\\\" \\\\ ok\")"));
    }

    [Test]
    public void DocumentBaseElementOverridesTheFallbackBase()
    {
        HtmlParseOptions parseOptions = HtmlParseOptions.CreateDefault();
        parseOptions.SetBaseUrl("https://example.test/fallback/page.html");
        HtmlDocument document = Html.Parse(
            "<html><head><base href='/docs/'></head><body>"
            + "<a href=guide>Guide</a></body></html>", parseOptions,
            CancellationToken.None).GetDocument();

        string markdown = Render(document.GetBody(),
            HtmlMarkdownOptions.CreateCommonMark());

        Assert.That(markdown, Is.EqualTo(
            "[Guide](https://example.test/docs/guide)"));
    }

    [Test]
    public void ValidRelativeTargetsRemainRelativeWithoutADocumentBase()
    {
        HtmlNode fragment = ParseFragment(
            "<p><a href='../guide(a)'>Guide</a> "
            + "<a href='#part'>Part</a></p>");

        string markdown = Render(fragment,
            HtmlMarkdownOptions.CreateCommonMark());

        Assert.That(markdown, Is.EqualTo(
            "[Guide](../guide\\(a\\)) [Part](#part)"));
    }

    [Test]
    public void EmptyLinksDoNotInventVisibleLabelText()
    {
        HtmlNode fragment = ParseFragment(
            "<p>before<a href='https://example.test/'></a>after</p>");

        string markdown = Render(fragment,
            HtmlMarkdownOptions.CreateCommonMark());

        Assert.That(markdown,
            Is.EqualTo("before[](https://example.test/)after"));
    }

    [Test]
    public void UnsafeAndCredentialBearingLinksFallBackToTheirLabels()
    {
        HtmlNode fragment = ParseFragment(
            "<p><a href='java&#x0A;script:alert(1)'><strong>Script</strong>"
            + "</a> <a href='data:text/html,bad'>Data</a> "
            + "<a href='https://user:secret@example.test/'>Credentials</a> "
            + "<a>Missing</a></p>");

        string markdown = Render(fragment,
            HtmlMarkdownOptions.CreateCommonMark());

        Assert.That(markdown,
            Is.EqualTo("**Script** Data Credentials Missing"));
    }

    [Test]
    public void GitHubProfileUsesSafeUriAndEmailAutolinks()
    {
        HtmlNode fragment = ParseFragment(
            "<p><a href='https://example.test/a'>https://example.test/a</a> "
            + "<a href='mailto:test@example.com'>test@example.com</a> "
            + "<a href='https://example.test/titled' title=Site>"
            + "https://example.test/titled</a></p>");
        string common = Render(fragment,
            HtmlMarkdownOptions.CreateCommonMark());
        string github = Render(fragment,
            HtmlMarkdownOptions.CreateGitHubFlavored());

        Assert.Multiple(() =>
        {
            Assert.That(common, Is.EqualTo(
                "[https://example.test/a](https://example.test/a) "
                + "[test@example.com](mailto:test@example.com) "
                + "[https://example.test/titled]"
                + "(https://example.test/titled \"Site\")"));
            Assert.That(github, Is.EqualTo(
                "<https://example.test/a> <test@example.com> "
                + "[https://example.test/titled]"
                + "(https://example.test/titled \"Site\")"));
        });
    }

    [Test]
    public void GfmExtendedWwwAutolinksRequireMatchingSimpleHttpDomains()
    {
        HtmlNode fragment = ParseFragment(
            "<p><a href='http://www.example.com'>www.example.com</a> "
            + "<a href='https://www.example.com'>www.example.com</a> "
            + "<a href='http://www.example.com/path'>www.example.com</a> "
            + "<a href='http://www.example.com' title=Site>"
            + "www.example.com</a></p>");

        string markdown = Render(fragment,
            HtmlMarkdownOptions.CreateGitHubFlavored());

        Assert.That(markdown, Is.EqualTo(
            "www.example.com "
            + "[www.example.com](https://www.example.com/) "
            + "[www.example.com](http://www.example.com/path) "
            + "[www.example.com](http://www.example.com/ \"Site\")"));
    }

    [Test]
    public void NumericReferencesCoalesceAcrossQuotesListsAndImages()
    {
        HtmlNode body = ParseBodyWithBase(
            "<blockquote><a href=guide>Quote</a></blockquote>"
            + "<ul><li><a href='./guide'>List</a></li>"
            + "<li><img alt=Chart src=chart.png title=Diagram></li></ul>",
            "https://example.test/docs/page.html");
        HtmlMarkdownOptions options =
            HtmlMarkdownOptions.CreateCommonMark();
        options.SetLinkStyle(HtmlMarkdownLinkStyle.Reference);

        string markdown = Render(body, options);

        Assert.That(markdown, Is.EqualTo(
            "> [Quote][1]\n\n- [List][1]\n- ![Chart][2]\n\n"
            + "[1]: https://example.test/docs/guide\n"
            + "[2]: https://example.test/docs/chart.png \"Diagram\""));
    }

    [Test]
    public void CollapsedReferencesCoalesceTargetsAndDisambiguateLabels()
    {
        HtmlNode fragment = ParseFragment(
            "<p><a href='https://one.test/'>Docs</a> "
            + "<a href='https://one.test/'>More</a> "
            + "<a href='https://two.test/'>Docs</a> "
            + "<img alt=Logo src='https://image.test/logo.png'></p>");
        HtmlMarkdownOptions options =
            HtmlMarkdownOptions.CreateCommonMark();
        options.SetLinkStyle(HtmlMarkdownLinkStyle.CollapsedReference);

        string markdown = Render(fragment, options);

        Assert.That(markdown, Is.EqualTo(
            "[Docs][] [More][Docs] [Docs][link-2] ![Logo][]\n\n"
            + "[Docs]: https://one.test/\n"
            + "[link-2]: https://two.test/\n"
            + "[Logo]: https://image.test/logo.png"));
    }

    [Test]
    public void ImagesUseAltTextAndFallBackSafelyForUnsafeSources()
    {
        HtmlNode body = ParseBodyWithBase(
            "<p><img alt='A [chart]' src='images/chart(1).png' "
            + "title='say &quot;hi&quot;'> "
            + "<img alt='Bad *image*' src='javascript:bad'> "
            + "<img alt='' src=decorative.png></p>",
            "https://example.test/docs/page.html");

        string markdown = Render(body,
            HtmlMarkdownOptions.CreateCommonMark());

        Assert.That(markdown, Is.EqualTo(
            "![A \\[chart\\]](https://example.test/docs/images/"
            + "chart\\(1\\).png \"say \\\"hi\\\"\") "
            + "Bad \\*image\\* "
            + "![](https://example.test/docs/decorative.png)"));
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

    static HtmlNode ParseBodyWithBase(string html, string baseUrl)
    {
        HtmlParseOptions options = HtmlParseOptions.CreateDefault();
        options.SetBaseUrl(baseUrl);
        HtmlDocument document = Html.Parse(html, options,
            CancellationToken.None).GetDocument();
        return document.GetBody();
    }
}
