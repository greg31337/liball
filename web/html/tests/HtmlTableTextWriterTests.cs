
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlTableTextWriterTests
{
    [Test]
    public void ReadingOrderIncludesCaptionHeadersAndCorrectedBodyRows()
    {
        HtmlNode fragment = ParseFragment(
            "<table><caption>Inventory</caption><thead>"
            + "<tr><th>Name</th><th>Qty</th></tr></thead><tbody>"
            + "<tr><td>Green <strong>tea</strong></td><td>2</td></tr>"
            + "</tbody></table><p>After</p>");

        string text = Write(fragment, HtmlTextTableStyle.ReadingOrder);

        Assert.That(text, Is.EqualTo(
            "Inventory\nName Qty\nGreen tea 2\n\nAfter"));
    }

    [Test]
    public void TsvExpandsRowAndColumnSpansIntoBlankLogicalCells()
    {
        HtmlNode fragment = ParseFragment(
            "<table><tr><th>A</th><th>B</th><th>C</th></tr>"
            + "<tr><td rowspan='2'>R</td><td colspan='2'>X</td></tr>"
            + "<tr><td>Y</td><td>Z</td></tr></table>");

        string text = Write(fragment, HtmlTextTableStyle.Tsv);

        Assert.That(text, Is.EqualTo(
            "A\tB\tC\nR\tX\t\n\tY\tZ"));
    }

    [Test]
    public void RowSpanZeroExtendsThroughTheRemainingRows()
    {
        HtmlNode fragment = ParseFragment(
            "<table><tr><td rowspan='0'>All</td><td>One</td></tr>"
            + "<tr><td>Two</td></tr><tr><td>Three</td></tr></table>");

        string text = Write(fragment, HtmlTextTableStyle.Tsv);

        Assert.That(text, Is.EqualTo(
            "All\tOne\n\tTwo\n\tThree"));
    }

    [Test]
    public void GridAlignsColumnsAndSeparatesAHeaderRow()
    {
        HtmlNode fragment = ParseFragment(
            "<table><tr><th>Name</th><th>Qty</th></tr>"
            + "<tr><td>Tea</td><td>2</td></tr>"
            + "<tr><td>Coffee</td><td>14</td></tr></table>");

        string text = Write(fragment, HtmlTextTableStyle.Grid);

        Assert.That(text, Is.EqualTo(
            "Name   | Qty\n-------+----\nTea    | 2\nCoffee | 14"));
    }

    [Test]
    public void OmissionModeDropsTheWholeTableIncludingItsCaption()
    {
        HtmlNode fragment = ParseFragment(
            "<p>Before</p><table><caption>Secret caption</caption>"
            + "<tr><td>Secret cell</td></tr></table><p>After</p>");

        string text = Write(fragment, HtmlTextTableStyle.Omit);

        Assert.That(text, Is.EqualTo("Before\n\nAfter"));
    }

    [Test]
    public void NestedRowsDoNotBecomeRowsOfTheOuterGrid()
    {
        HtmlNode fragment = ParseFragment(
            "<table><tr><td>Outer<table><tr><td>Inner</td></tr></table>"
            + "</td><td>End</td></tr></table>");

        string text = Write(fragment, HtmlTextTableStyle.Tsv);

        Assert.That(text, Is.EqualTo("Outer Inner\tEnd"));
    }

    [Test]
    public void TableCellExpansionStopsAtTheSharedCellBudget()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumTableCells(3);
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        options.SetTableStyle(HtmlTextTableStyle.Tsv);
        options.SetLimits(limits);
        HtmlCompiledTextOptions compiled = options.Compile();
        HtmlTextOperationContext context = new HtmlTextOperationContext(
            compiled, CancellationToken.None, null);
        HtmlTextWriter writer = new HtmlTextWriter(compiled, context,
            CancellationToken.None);

        writer.WriteNode(ParseFragment(
            "<table><tr><td>A</td><td>B</td></tr>"
            + "<tr><td>C</td><td>D</td></tr></table>"));

        Assert.Multiple(() =>
        {
            Assert.That(writer.Complete(), Is.EqualTo("A\tB\nC"));
            Assert.That(context.WasTruncated(), Is.True);
        });
    }

    [Test]
    public void CellTextOmitsHiddenAndExecutableContentAndUsesImageLabels()
    {
        HtmlNode fragment = ParseFragment(
            "<table><caption hidden>Hidden caption</caption>"
            + "<tr hidden><td>Hidden row</td></tr><tr><td>Shown"
            + "<span hidden>hidden</span><script>bad()</script>"
            + "<img alt='chart'></td></tr></table>");

        string text = Write(fragment, HtmlTextTableStyle.ReadingOrder);

        Assert.That(text, Is.EqualTo("Shownchart"));
    }

    [Test]
    public void SelectorRulesApplyWithinCaptionsRowsAndCells()
    {
        HtmlNode fragment = ParseFragment(
            "<table><caption><span class='replace'>old</span></caption>"
            + "<tr class='whole'><td>old row</td></tr><tr>"
            + "<td>keep <span class='drop'>secret</span>"
            + "<script>included()</script></td></tr></table>");
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        options.SetTableStyle(HtmlTextTableStyle.ReadingOrder);
        options.AddReplacementSelector(".replace", "New");
        options.AddReplacementSelector(".whole", "Whole row");
        options.AddExcludedSelector(".drop");
        options.AddIncludedSelector("script");

        string text = Write(fragment, options);

        Assert.That(text, Is.EqualTo(
            "New\nWhole row\nkeep included()"));
    }

    static string Write(HtmlNode node, HtmlTextTableStyle style)
    {
        HtmlTextOptions options = HtmlTextOptions.CreateDefault();
        options.SetTableStyle(style);
        return Write(node, options);
    }

    static string Write(HtmlNode node, HtmlTextOptions options)
    {
        HtmlCompiledTextOptions compiled = options.Compile();
        HtmlTextOperationContext context = new HtmlTextOperationContext(
            compiled, CancellationToken.None, null);
        HtmlTextWriter writer = new HtmlTextWriter(compiled, context,
            CancellationToken.None);
        writer.WriteNode(node);
        return writer.Complete();
    }

    static HtmlNode ParseFragment(string html)
    {
        return Html.ParseFragment(html, "body").GetFragment();
    }
}
