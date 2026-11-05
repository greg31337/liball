
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlMarkdownTablesTests
{
    [Test]
    public void GitHubTablesNormalizeColumnWidthsAndRows()
    {
        HtmlNode fragment = ParseFragment(
            "<table><thead><tr><th>Name</th><th>Qty</th></tr></thead>"
            + "<tbody><tr><td>Tea</td><td>2</td></tr>"
            + "<tr><td>Coffee</td><td>14</td></tr></tbody></table>");

        string markdown = Render(fragment,
            HtmlMarkdownOptions.CreateGitHubFlavored());

        Assert.That(markdown, Is.EqualTo(
            "| Name   | Qty |\n"
            + "| ------ | --- |\n"
            + "| Tea    | 2   |\n"
            + "| Coffee | 14  |"));
    }

    [Test]
    public void FirstDataRowIsPromotedAndShortRowsArePadded()
    {
        HtmlNode fragment = ParseFragment(
            "<table><tr><td>A</td><td>B</td></tr>"
            + "<tr><td>1</td></tr></table>");

        string markdown = Render(fragment,
            HtmlMarkdownOptions.CreateGitHubFlavored());

        Assert.That(markdown, Is.EqualTo(
            "| A   | B   |\n"
            + "| --- | --- |\n"
            + "| 1   |     |"));
    }

    [Test]
    public void CellAlignmentOverridesColumnAlignment()
    {
        HtmlNode fragment = ParseFragment(
            "<table><colgroup><col align=left>"
            + "<col style='text-align:center'><col></colgroup>"
            + "<tr><th>L</th><th>M</th>"
            + "<th style='text-align:right !important;"
            + "text-align:bogus'>R</th></tr>"
            + "<tr><td>1</td><td>2</td><td>3</td></tr></table>");

        string markdown = Render(fragment,
            HtmlMarkdownOptions.CreateGitHubFlavored());

        Assert.That(markdown, Is.EqualTo(
            "| L    | M     | R    |\n"
            + "| :--- | :---: | ---: |\n"
            + "| 1    | 2     | 3    |"));
    }

    [Test]
    public void CellMarkdownAndPipesAreEscapedWithoutLosingFormatting()
    {
        HtmlNode fragment = ParseFragment(
            "<table><tr><th>A|B</th><th>Code</th></tr>"
            + "<tr><td><em>x|y</em><br>next</td>"
            + "<td><code>x|y</code></td></tr></table>");

        string markdown = Render(fragment,
            HtmlMarkdownOptions.CreateGitHubFlavored());

        Assert.That(markdown, Is.EqualTo(
            "| A\\|B        | Code   |\n"
            + "| ----------- | ------ |\n"
            + "| *x\\|y* next | `x\\|y` |"));
    }

    [Test]
    public void CaptionsPrecedeTablesAndConfiguredNewlinesAreUsed()
    {
        HtmlNode fragment = ParseFragment(
            "<table><caption><strong>Inventory</strong></caption>"
            + "<tr><th>Item</th></tr><tr><td>Tea</td></tr></table>");
        HtmlMarkdownOptions options =
            HtmlMarkdownOptions.CreateGitHubFlavored();
        options.SetNewLine("\r\n");

        string markdown = Render(fragment, options);

        Assert.That(markdown, Is.EqualTo(
            "**Inventory**\r\n\r\n"
            + "| Item |\r\n| ---- |\r\n| Tea  |"));
    }

    [Test]
    public void TableLinksShareDocumentReferenceDefinitions()
    {
        HtmlNode fragment = ParseFragment(
            "<table><tr><th><a href='https://example.test/'>One</a>"
            + "</th></tr><tr><td><a href='https://example.test/'>Two</a>"
            + "</td></tr></table>");
        HtmlMarkdownOptions options =
            HtmlMarkdownOptions.CreateGitHubFlavored();
        options.SetLinkStyle(HtmlMarkdownLinkStyle.Reference);

        string markdown = Render(fragment, options);

        Assert.That(markdown, Is.EqualTo(
            "| [One][1] |\n| -------- |\n| [Two][1] |\n\n"
            + "[1]: https://example.test/"));
    }

    [Test]
    public void DisabledTablesUseSafeHardBreakSeparatedFallback()
    {
        HtmlNode fragment = ParseFragment(
            "<table><caption>Inventory</caption>"
            + "<tr><td>A</td><td>B</td></tr>"
            + "<tr><td>C</td><td>D</td></tr></table>");

        string markdown = Render(fragment,
            HtmlMarkdownOptions.CreateCommonMark());

        Assert.That(markdown, Is.EqualTo(
            "Inventory\n\nA \\| B  \nC \\| D"));
    }

    [Test]
    public void RowAndColumnSpansExplicitlyFallBackFromGfmTables()
    {
        HtmlNode columnSpan = ParseFragment(
            "<table><tr><th>A</th><th>B</th></tr>"
            + "<tr><td colspan=2>X</td></tr></table>");
        HtmlNode rowSpan = ParseFragment(
            "<table><tr><th>A</th><th>B</th></tr>"
            + "<tr><td rowspan=2>R</td><td>1</td></tr>"
            + "<tr><td>2</td></tr></table>");

        string columns = Render(columnSpan,
            HtmlMarkdownOptions.CreateGitHubFlavored());
        string rows = Render(rowSpan,
            HtmlMarkdownOptions.CreateGitHubFlavored());

        Assert.Multiple(() =>
        {
            Assert.That(columns, Is.EqualTo("A \\| B  \nX"));
            Assert.That(rows, Is.EqualTo(
                "A \\| B  \nR \\| 1  \n2"));
            Assert.That(columns, Does.Not.Contain("<table"));
            Assert.That(rows, Does.Not.Contain("<table"));
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
