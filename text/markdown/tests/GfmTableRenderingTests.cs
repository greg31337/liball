using System.IO;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class GfmTableRenderingTests
{
    [Test]
    public void HtmlRenderer_RendersGfmTables()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "| Left | Center | Right |\n" +
            "| :--- | :---: | ---: |\n" +
            "| a | *b* | c |",
            MarkdownPipeline.GfmPreset());

        string html = HtmlRenderer.Render(document, MarkdownPipeline.GfmPreset());

        Assert.That(html, Is.EqualTo(
            "<table>\n" +
            "<thead>\n" +
            "<tr>\n" +
            "<th align=\"left\">Left</th>\n" +
            "<th align=\"center\">Center</th>\n" +
            "<th align=\"right\">Right</th>\n" +
            "</tr>\n" +
            "</thead>\n" +
            "<tbody>\n" +
            "<tr>\n" +
            "<td align=\"left\">a</td>\n" +
            "<td align=\"center\"><em>b</em></td>\n" +
            "<td align=\"right\">c</td>\n" +
            "</tr>\n" +
            "</tbody>\n" +
            "</table>\n"));
    }

    [Test]
    public void MarkdownRenderer_RendersGfmTables()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "| Left | Center | Right |\n" +
            "| :--- | :---: | ---: |\n" +
            "| a | *b* | c |",
            MarkdownPipeline.GfmPreset());

        string markdown = MarkdownRenderer.Render(document, MarkdownPipeline.GfmPreset());

        Assert.That(markdown, Is.EqualTo(
            "| Left | Center | Right |\n" +
            "| :--- | :---: | ---: |\n" +
            "| a | *b* | c |\n"));
    }

    [Test]
    public void TableRenderers_EscapePipeContent()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "| A | B |\n" +
            "| --- | --- |\n" +
            "| a \\| b | c |",
            MarkdownPipeline.GfmPreset());

        string html = HtmlRenderer.Render(document, MarkdownPipeline.GfmPreset());
        string markdown = MarkdownRenderer.Render(document, MarkdownPipeline.GfmPreset());

        Assert.That(html, Does.Contain("<td>a | b</td>"));
        Assert.That(markdown, Is.EqualTo(
            "| A | B |\n" +
            "| --- | --- |\n" +
            "| a \\| b | c |\n"));
    }

    [Test]
    public void TableRenderers_HandleManuallyConstructedLiteralCells()
    {
        MarkdownDocument document = new MarkdownDocument();
        MarkdownNode table = MarkdownNode.Create(MarkdownNodeKind.Table);
        MarkdownNode head = MarkdownNode.Create(MarkdownNodeKind.TableHead);
        MarkdownNode body = MarkdownNode.Create(MarkdownNodeKind.TableBody);
        MarkdownNode headerRow = MarkdownNode.Create(MarkdownNodeKind.TableRow);
        MarkdownNode headerCell = MarkdownNode.Create(MarkdownNodeKind.TableCell);
        MarkdownNode bodyRow = MarkdownNode.Create(MarkdownNodeKind.TableRow);
        MarkdownNode bodyCell = MarkdownNode.Create(MarkdownNodeKind.TableCell);

        headerCell.SetLiteral("A | B");
        bodyCell.SetLiteral("x < y");
        headerRow.AppendChild(headerCell);
        bodyRow.AppendChild(bodyCell);
        head.AppendChild(headerRow);
        body.AppendChild(bodyRow);
        table.AppendChild(head);
        table.AppendChild(body);
        document.Root.AppendChild(table);

        Assert.That(HtmlRenderer.Render(document), Is.EqualTo(
            "<table>\n" +
            "<thead>\n" +
            "<tr>\n" +
            "<th>A | B</th>\n" +
            "</tr>\n" +
            "</thead>\n" +
            "<tbody>\n" +
            "<tr>\n" +
            "<td>x &lt; y</td>\n" +
            "</tr>\n" +
            "</tbody>\n" +
            "</table>\n"));
        Assert.That(MarkdownRenderer.Render(document), Is.EqualTo(
            "| A \\| B |\n" +
            "| --- |\n" +
            "| x \\< y |\n"));
    }

    [Test]
    public void TableRendering_RejectsInvalidArguments()
    {
        StringWriter htmlWriter = new StringWriter();
        HtmlRenderContext htmlContext = new HtmlRenderContext(new HtmlWriter(htmlWriter), MarkdownPipeline.GfmPreset());
        StringWriter markdownWriter = new StringWriter();
        MarkdownRenderContext markdownContext = new MarkdownRenderContext(new MarkdownWriter(markdownWriter), MarkdownPipeline.GfmPreset());
        MarkdownNode table = MarkdownNode.Create(MarkdownNodeKind.Table);

        Assert.That(() => HtmlRenderer.RenderTable(null!, table), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.RenderTable(htmlContext, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.RenderTableCell(null!, table), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.RenderTableCell(htmlContext, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => HtmlRenderer.GetTableCellTagName(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.RenderTable(null!, table), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.RenderTable(markdownContext, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.RenderTableCell(null!, table), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.RenderTableCell(markdownContext, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.RenderTableDelimiterRow(null!, table), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.RenderTableDelimiterRow(markdownContext, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.GetTableHeaderRow(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.GetTableBody(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.GetTableDelimiterCell(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.GetTableCellMarkdown(null!, table), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.GetTableCellMarkdown(markdownContext, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownRenderer.EscapeTableCellContent(null!), Throws.TypeOf<ArgumentNullException>());
    }
}
