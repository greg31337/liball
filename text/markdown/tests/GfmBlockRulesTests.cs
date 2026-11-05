using System.Collections.Generic;
using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class GfmBlockRulesTests
{
    [Test]
    public void TableRule_ParsesGfmTableStructureAndAlignment()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "| Left | Center | Right |\n" +
            "| :--- | :---: | ---: |\n" +
            "| a | *b* | c |\n" +
            "| short |\n" +
            "\n" +
            "after",
            MarkdownPipeline.GfmPreset());

        MarkdownNode table = document.Root.GetChild(0);
        MarkdownNode head = table.GetChild(0);
        MarkdownNode body = table.GetChild(1);
        MarkdownNode headerRow = head.GetChild(0);
        MarkdownNode firstBodyRow = body.GetChild(0);
        MarkdownNode secondBodyRow = body.GetChild(1);

        Assert.That(table.Kind, Is.EqualTo(MarkdownNodeKind.Table));
        Assert.That(table.GetAttributeValue("columns"), Is.EqualTo("3"));
        Assert.That(head.Kind, Is.EqualTo(MarkdownNodeKind.TableHead));
        Assert.That(body.Kind, Is.EqualTo(MarkdownNodeKind.TableBody));
        Assert.That(headerRow.GetChild(0).Literal, Is.EqualTo("Left"));
        Assert.That(headerRow.GetChild(0).GetAttributeValue("align"), Is.EqualTo("left"));
        Assert.That(headerRow.GetChild(1).GetAttributeValue("align"), Is.EqualTo("center"));
        Assert.That(headerRow.GetChild(2).GetAttributeValue("align"), Is.EqualTo("right"));
        Assert.That(firstBodyRow.GetChild(0).Literal, Is.EqualTo("a"));
        Assert.That(firstBodyRow.GetChild(1).Literal, Is.EqualTo("*b*"));
        Assert.That(firstBodyRow.GetChild(1).GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Emphasis));
        Assert.That(secondBodyRow.GetChild(0).Literal, Is.EqualTo("short"));
        Assert.That(secondBodyRow.GetChild(1).Literal, Is.EqualTo(""));
        Assert.That(secondBodyRow.GetChild(2).Literal, Is.EqualTo(""));
        Assert.That(document.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
    }

    [Test]
    public void TableRule_DoesNotParseWithoutGfmTableFeature()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "| A | B |\n" +
            "| --- | --- |\n" +
            "| 1 | 2 |",
            MarkdownPipeline.CommonMarkPreset());

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(1));
        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
    }

    [Test]
    public void TableRule_IgnoresInvalidDelimiterRows()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "| A | B |\n" +
            "| : | --- |\n" +
            "| 1 | 2 |",
            MarkdownPipeline.GfmPreset());

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(1));
        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
    }

    [Test]
    public void TableRule_SplitsEscapedPipesAsCellContent()
    {
        MarkdownDocument document = MarkdownParser.Parse(
            "| A | B |\n" +
            "| --- | --- |\n" +
            "| a \\| b | c |",
            MarkdownPipeline.GfmPreset());

        MarkdownNode firstCell = document.Root.GetChild(0).GetChild(1).GetChild(0).GetChild(0);
        MarkdownNode secondCell = document.Root.GetChild(0).GetChild(1).GetChild(0).GetChild(1);

        Assert.That(firstCell.Literal, Is.EqualTo("a | b"));
        Assert.That(firstCell.GetChildCount(), Is.EqualTo(1));
        Assert.That(firstCell.GetChild(0).Literal, Is.EqualTo("a | b"));
        Assert.That(secondCell.Literal, Is.EqualTo("c"));
    }

    [Test]
    public void TableRule_IsRegisteredBeforeParagraphInGfmPipelines()
    {
        MarkdownRuleList rules = MarkdownPipeline.GfmPreset().GetBlockRules();

        Assert.That(rules.Contains(GfmBlockRules.TableRuleName), Is.True);
        Assert.That(rules.IndexOf(GfmBlockRules.TableRuleName), Is.LessThan(rules.IndexOf(CommonMarkBlockRules.ParagraphRuleName)));
        Assert.That(MarkdownPipeline.CommonMarkPreset().GetBlockRules().Contains(GfmBlockRules.TableRuleName), Is.False);
    }

    [Test]
    public void TableRule_HelperMethodsClassifyRowsAndCells()
    {
        List<string> split = GfmBlockRules.SplitTableRow("| a \\| b | c |");
        List<string> body = new List<string>();
        body.Add("x");

        Assert.That(GfmBlockRules.LooksLikeTableRow("a | b"), Is.True);
        Assert.That(GfmBlockRules.ContainsUnescapedPipe("a \\| b"), Is.False);
        Assert.That(split.Count, Is.EqualTo(2));
        Assert.That(split[0], Is.EqualTo("a | b"));
        Assert.That(split[1], Is.EqualTo("c"));
        Assert.That(GfmBlockRules.IsDelimiterCell(":---:"), Is.True);
        Assert.That(GfmBlockRules.IsDelimiterCell("-"), Is.True);
        Assert.That(GfmBlockRules.IsDelimiterCell(":"), Is.False);
        Assert.That(GfmBlockRules.GetDelimiterAlignment(":---"), Is.EqualTo("left"));
        Assert.That(GfmBlockRules.GetDelimiterAlignment("---:"), Is.EqualTo("right"));
        Assert.That(GfmBlockRules.GetDelimiterAlignment(":---:"), Is.EqualTo("center"));
        Assert.That(GfmBlockRules.NormalizeBodyCells(body, 2).Count, Is.EqualTo(2));
    }

    [Test]
    public void TableRule_RejectsInvalidArguments()
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create("", MarkdownPipeline.GfmPreset());
        MarkdownLine line = new MarkdownLine();
        List<string> cells = new List<string>();

        cells.Add("---");

        Assert.That(() => GfmBlockRules.RegisterGfmBlockRules(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmBlockRules.TryParseTable(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmBlockRules.CreateTableRow(null!, cells, cells), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmBlockRules.CreateTableRow(line, null!, cells), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmBlockRules.CreateTableRow(line, cells, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmBlockRules.NormalizeBodyCells(null!, 1), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmBlockRules.NormalizeBodyCells(cells, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => GfmBlockRules.LooksLikeTableRow(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmBlockRules.SplitTableRow(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmBlockRules.IsDelimiterRow(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmBlockRules.IsDelimiterCell(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmBlockRules.GetDelimiterAlignment(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => GfmBlockRules.ContainsUnescapedPipe(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(GfmBlockRules.TryParseTable(context), Is.False);
    }
}
