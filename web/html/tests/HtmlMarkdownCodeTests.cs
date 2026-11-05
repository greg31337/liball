
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlMarkdownCodeTests
{
    [Test]
    public void InlineCodeSpansChooseDelimitersBeyondContentRuns()
    {
        HtmlNode fragment = ParseFragment(
            "<p>Use <code>a*b</code>, <code>x`y``z</code>, and "
            + "<code>`edge`</code>.</p>");

        string markdown = Render(fragment,
            HtmlMarkdownOptions.CreateCommonMark());

        Assert.That(markdown, Is.EqualTo(
            "Use `a*b`, ```x`y``z```, and `` `edge` ``."));
    }

    [Test]
    public void InlineCodeCollapsesHtmlWhitespaceWithoutEscapingContent()
    {
        HtmlNode fragment = ParseFragment(
            "<p>before<code>  &lt;x&gt;   a*b<br><em>next</em>  "
            + "</code>after</p>");

        string markdown = Render(fragment,
            HtmlMarkdownOptions.CreateCommonMark());

        Assert.That(markdown, Is.EqualTo(
            "before `<x> a*b next` after"));
    }

    [Test]
    public void FencedBlocksExtractLanguageAndPreserveWhitespace()
    {
        HtmlNode fragment = ParseFragment(
            "<pre><code class='extra language-csharp'>"
            + "  first\t\r\n\nlast  \n</code></pre>");

        string markdown = Render(fragment,
            HtmlMarkdownOptions.CreateCommonMark());

        Assert.That(markdown, Is.EqualTo(
            "```csharp\n  first\t\n\nlast  \n```"));
    }

    [Test]
    public void FencedBlocksChooseTheShortestCollisionFreeFence()
    {
        HtmlNode tildeFragment = ParseFragment(
            "<pre>before ``` after\n~~</pre>");
        HtmlNode backtickFragment = ParseFragment(
            "<pre>~~~~\ncode</pre>");

        string tilde = Render(tildeFragment,
            HtmlMarkdownOptions.CreateCommonMark());
        string backtick = Render(backtickFragment,
            HtmlMarkdownOptions.CreateCommonMark());

        Assert.Multiple(() =>
        {
            Assert.That(tilde, Is.EqualTo(
                "~~~\nbefore ``` after\n~~\n~~~"));
            Assert.That(backtick, Is.EqualTo(
                "```\n~~~~\ncode\n```"));
        });
    }

    [Test]
    public void ExplicitInfoUsesTildesWhenItContainsBackticks()
    {
        HtmlNode fragment = ParseFragment(
            "<pre><code data-info=' js   title=`demo` '>x</code></pre>");

        string markdown = Render(fragment,
            HtmlMarkdownOptions.CreateCommonMark());

        Assert.That(markdown, Is.EqualTo(
            "~~~js title=`demo`\nx\n~~~"));
    }

    [Test]
    public void CodeMetadataPrecedesPreMetadataAndSupportsLangClasses()
    {
        HtmlNode codeMetadata = ParseFragment(
            "<pre data-language=python><code class=lang-rust>x</code></pre>");
        HtmlNode preMetadata = ParseFragment(
            "<pre class=language-ruby><code>x</code></pre>");

        Assert.Multiple(() =>
        {
            Assert.That(Render(codeMetadata,
                HtmlMarkdownOptions.CreateCommonMark()), Is.EqualTo(
                    "```rust\nx\n```"));
            Assert.That(Render(preMetadata,
                HtmlMarkdownOptions.CreateCommonMark()), Is.EqualTo(
                    "```ruby\nx\n```"));
        });
    }

    [Test]
    public void FencedBlocksUseConfiguredNewlinesAndVisibleCodeText()
    {
        HtmlNode fragment = ParseFragment(
            "<pre data-language=python>  one<br>\ttwo"
            + "<span hidden>secret</span></pre>");
        HtmlMarkdownOptions options =
            HtmlMarkdownOptions.CreateCommonMark();
        options.SetNewLine("\r\n");

        string markdown = Render(fragment, options);

        Assert.That(markdown, Is.EqualTo(
            "```python\r\n  one\r\n\ttwo\r\n```"));
    }

    [Test]
    public void EmptyPreformattedBlocksStillProduceValidFences()
    {
        HtmlNode fragment = ParseFragment("<pre></pre>");

        string markdown = Render(fragment,
            HtmlMarkdownOptions.CreateCommonMark());

        Assert.That(markdown, Is.EqualTo("```\n```"));
    }

    [Test]
    public void FencedBlocksRetainContentIndentInsideListsAndQuotes()
    {
        HtmlNode list = ParseFragment(
            "<ul><li><p>Example:</p><pre> one\ntwo</pre></li></ul>");
        HtmlNode quote = ParseFragment(
            "<blockquote><pre> x\n</pre></blockquote>");

        Assert.Multiple(() =>
        {
            Assert.That(Render(list,
                HtmlMarkdownOptions.CreateCommonMark()), Is.EqualTo(
                    "- Example:\n\n  ```\n   one\n  two\n  ```"));
            Assert.That(Render(quote,
                HtmlMarkdownOptions.CreateCommonMark()), Is.EqualTo(
                    "> ```\n>  x\n> ```"));
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
