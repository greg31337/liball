
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlMarkdownUnknownElementsTests
{
    [Test]
    public void UnwrapIsTheDefaultAndStillConvertsDescendants()
    {
        HtmlNode fragment = ParseFragment(
            "<x-panel>Alpha <strong>bold</strong>"
            + "<p>Second</p></x-panel>");

        string markdown = Render(fragment,
            HtmlMarkdownOptions.CreateCommonMark());

        Assert.That(markdown, Is.EqualTo("Alpha **bold**\n\nSecond"));
    }

    [Test]
    public void DropOnlyRemovesUnknownAndForeignElementSubtrees()
    {
        HtmlNode fragment = ParseFragment(
            "<p>Known <span>content</span></p>"
            + "<x-panel>unknown</x-panel>"
            + "<svg><text>foreign</text></svg><p>After</p>");
        HtmlMarkdownOptions options =
            HtmlMarkdownOptions.CreateCommonMark();
        options.SetUnknownElementAction(
            HtmlMarkdownUnknownElementAction.Drop);

        string markdown = Render(fragment, options);

        Assert.That(markdown, Is.EqualTo("Known content\n\nAfter"));
    }

    [Test]
    public void PreserveAndEscapeUseDeterministicCanonicalHtml()
    {
        HtmlNode fragment = ParseFragment(
            "<x-panel data-v='a&amp;b'>A &amp; <em>B</em></x-panel>");
        HtmlMarkdownOptions preserve =
            HtmlMarkdownOptions.CreateCommonMark();
        preserve.SetEnableTrustedRawHtml(true);
        preserve.SetUnknownElementAction(
            HtmlMarkdownUnknownElementAction.PreserveHtml);
        HtmlMarkdownOptions escape =
            HtmlMarkdownOptions.CreateCommonMark();
        escape.SetUnknownElementAction(
            HtmlMarkdownUnknownElementAction.EscapeHtml);
        HtmlMarkdownOptions trustedEscape = escape.Copy();
        trustedEscape.SetEnableTrustedRawHtml(true);

        string preserved = Render(fragment, preserve);
        string escaped = Render(fragment, escape);
        string escapedWithTrust = Render(fragment, trustedEscape);

        Assert.Multiple(() =>
        {
            Assert.That(preserved, Is.EqualTo(
                "<x-panel data-v=\"a&amp;b\">A &amp; <em>B</em>"
                + "</x-panel>"));
            Assert.That(escaped, Is.EqualTo(
                "&lt;x-panel data-v=&quot;a&amp;amp;b&quot;&gt;"
                + "A &amp;amp; &lt;em&gt;B&lt;/em&gt;"
                + "&lt;/x-panel&gt;"));
            Assert.That(escapedWithTrust, Is.EqualTo(escaped));
        });
    }

    [Test]
    public void TextUsesVisiblePlainTextAndMarkdownEscaping()
    {
        HtmlNode fragment = ParseFragment(
            "<x-panel>One <strong>*two*</strong><br>three"
            + "<span hidden>secret</span><script>bad()</script>"
            + "<div>four</div></x-panel>");
        HtmlMarkdownOptions options =
            HtmlMarkdownOptions.CreateCommonMark();
        options.SetUnknownElementAction(
            HtmlMarkdownUnknownElementAction.Text);

        string markdown = Render(fragment, options);

        Assert.That(markdown, Is.EqualTo("One \\*two\\* three four"));
    }

    [Test]
    public void ErrorNamesTheFirstUnknownElement()
    {
        HtmlNode fragment = ParseFragment(
            "<p>Before</p><x-panel>value</x-panel>");
        HtmlMarkdownOptions options =
            HtmlMarkdownOptions.CreateCommonMark();
        options.SetUnknownElementAction(
            HtmlMarkdownUnknownElementAction.Error);

        InvalidOperationException exception =
            Assert.Catch<InvalidOperationException>(() =>
                Render(fragment, options));

        Assert.That(exception.Message, Is.EqualTo(
            "Unknown element 'x-panel' cannot be converted to Markdown."));
    }

    [Test]
    public void LastMatchingCustomRuleOverridesEarlierRulesAndStandardRules()
    {
        HtmlNode fragment = ParseFragment(
            "<p class=rule>kept</p>"
            + "<p class='rule remove'>gone</p>"
            + "<p id=restore class='rule remove'><em>normal</em></p>"
            + "<x-panel class=rule>custom</x-panel>"
            + "<x-panel id=unknown-restore class='rule remove'>"
            + "fallback</x-panel>");
        HtmlMarkdownOptions options =
            HtmlMarkdownOptions.CreateCommonMark();
        options.SetEnableTrustedRawHtml(true);
        options.SetUnknownElementAction(
            HtmlMarkdownUnknownElementAction.Drop);
        options.Keep(".rule");
        options.Remove(".remove");
        options.UseDefault("#restore, #unknown-restore");

        string markdown = Render(fragment, options);

        Assert.That(markdown, Is.EqualTo(
            "<p class=\"rule\">kept</p>\n\n*normal*\n\n"
            + "<x-panel class=\"rule\">custom</x-panel>"));
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
