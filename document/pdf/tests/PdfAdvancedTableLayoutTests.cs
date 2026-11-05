using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfAdvancedTableLayoutTests
{
    [Test]
    public void TableResolvesFixedProportionalAutoMinimumAndMaximumColumns()
    {
        PdfTableColumnDefinition[] definitions = new PdfTableColumnDefinition[]
        {
            new PdfTableColumnDefinition(PdfTableColumnSizing.Fixed, 20),
            new PdfTableColumnDefinition(PdfTableColumnSizing.Proportional, 1),
            new PdfTableColumnDefinition(PdfTableColumnSizing.Proportional, 3),
            new PdfTableColumnDefinition(PdfTableColumnSizing.Auto, 0),
            new PdfTableColumnDefinition(PdfTableColumnSizing.Minimum, 25),
            new PdfTableColumnDefinition(PdfTableColumnSizing.Maximum, 12)
        };
        PdfTableBlock table = new PdfTableBlock(new double[] { 1 });
        table.SetColumnDefinitions(definitions);
        definitions[0].Value = 90;
        PdfTableRow row = table.AddRow();
        row.AddBlock(PdfBlock.WithSize(10, 10));
        row.AddBlock(PdfBlock.WithSize(5, 10));
        row.AddBlock(PdfBlock.WithSize(5, 10));
        row.AddBlock(PdfBlock.WithSize(15, 10));
        row.AddBlock(PdfBlock.WithSize(10, 10));
        row.AddBlock(PdfBlock.WithSize(10, 10));

        PdfLayoutResult result = table.Measure(new PdfLayoutContext(120, 50));

        Assert.That(result.Fits, Is.True);
        Assert.That(result.Size.Width, Is.EqualTo(120).Within(0.000001));
        Assert.That(table.GetResolvedColumnWidth(0), Is.EqualTo(20));
        Assert.That(table.GetResolvedColumnWidth(1), Is.EqualTo(12.5).Within(0.000001));
        Assert.That(table.GetResolvedColumnWidth(2), Is.EqualTo(37.5).Within(0.000001));
        Assert.That(table.GetResolvedColumnWidth(3), Is.EqualTo(15));
        Assert.That(table.GetResolvedColumnWidth(4), Is.EqualTo(25));
        Assert.That(table.GetResolvedColumnWidth(5), Is.EqualTo(10));

        PdfTableBlock capped = new PdfTableBlock(new double[] { 1 });
        capped.SetColumnDefinitions(new PdfTableColumnDefinition[]
        {
            new PdfTableColumnDefinition(PdfTableColumnSizing.Maximum, 12)
        });
        capped.AddRow().AddBlock(PdfBlock.WithSize(30, 10));
        Assert.That(capped.Measure(new PdfLayoutContext(100, 50)).Fits, Is.False);
        Assert.That(capped.GetResolvedColumnWidth(0), Is.EqualTo(12));

        PdfTableBlock autoText = new PdfTableBlock(new double[] { 1 });
        autoText.SetColumnDefinitions(new PdfTableColumnDefinition[]
        {
            new PdfTableColumnDefinition(PdfTableColumnSizing.Auto, 0)
        });
        autoText.AddRow().AddTextBlock(new PdfTextBlock("abc", CreateParagraphStyle()));
        autoText.Measure(new PdfLayoutContext(100, 50));
        Assert.That(autoText.GetResolvedColumnWidth(0), Is.EqualTo(18).Within(0.001));
    }

    [Test]
    public void TableRowsSupportExactAndContentDrivenHeights()
    {
        PdfTableBlock table = new PdfTableBlock(new double[] { 40 });
        PdfTableRow row = table.AddRow();
        PdfTableCell cell = row.AddBlock(PdfBlock.WithSize(20, 20));
        PdfElementStyle cellStyle = new PdfElementStyle();
        cellStyle.HasBackground = true;
        cellStyle.BackgroundColor = PdfColor.Gray(0.3);
        cell.SetStyle(cellStyle);
        row.SetHeight(30);

        PdfLayoutResult exact = table.Measure(new PdfLayoutContext(100, 100));
        PdfPage page = new PdfPage(100, 100);
        table.Draw(new PdfLayoutDrawContext(page, 10, 20, 40, 60));
        row.ClearHeight();
        PdfLayoutResult contentDriven = table.Measure(new PdfLayoutContext(100, 100));
        row.SetHeight(10);
        PdfLayoutResult tooShort = table.Measure(new PdfLayoutContext(100, 100));

        Assert.That(exact.Size.Height, Is.EqualTo(30));
        Assert.That(exact.Fits, Is.True);
        Assert.That(page.GetCanvas().ToPdfString(), Does.Contain("10 20 40 30 re f"));
        Assert.That(contentDriven.Size.Height, Is.EqualTo(20));
        Assert.That(contentDriven.Fits, Is.True);
        Assert.That(tooShort.Size.Height, Is.EqualTo(10));
        Assert.That(tooShort.Fits, Is.False);
    }

    [Test]
    public void ColumnSpansMeasureAndDrawAcrossCombinedColumnWidths()
    {
        PdfTableBlock table = new PdfTableBlock(new double[] { 30, 30, 30 });
        PdfTableRow row = table.AddRow();
        PdfTableCell wide = row.AddBlock(PdfBlock.WithSize(50, 10));
        wide.SetColumnSpan(2);
        wide.SetBackground(PdfColor.Gray(0.2));
        PdfTableCell trailing = row.AddBlock(PdfBlock.WithSize(20, 10));
        trailing.SetBackground(PdfColor.Gray(0.4));
        PdfPage page = new PdfPage(120, 80);

        PdfLayoutResult result = table.Draw(new PdfLayoutDrawContext(page, 10, 20, 90, 40));

        string content = page.GetCanvas().ToPdfString();
        Assert.That(result.Fits, Is.True);
        Assert.That(result.Size.Width, Is.EqualTo(90));
        Assert.That(result.Size.Height, Is.EqualTo(10));
        Assert.That(wide.GetColumnSpan(), Is.EqualTo(2));
        Assert.That(content, Does.Contain("10 20 60 10 re f"));
        Assert.That(content, Does.Contain("70 20 30 10 re f"));
    }

    [Test]
    public void RowSpansDistributeContentHeightAndDrawOneCombinedCell()
    {
        PdfTableBlock table = new PdfTableBlock(new double[] { 40, 40 });
        PdfTableRow first = table.AddRow();
        PdfTableRow second = table.AddRow();
        PdfTableCell spanning = first.AddBlock(PdfBlock.WithSize(20, 30));
        spanning.SetRowSpan(2);
        spanning.SetBackground(PdfColor.Gray(0.2));
        PdfTableCell firstTrailing = first.AddBlock(PdfBlock.WithSize(20, 5));
        firstTrailing.SetBackground(PdfColor.Gray(0.4));
        PdfTableCell secondTrailing = second.AddBlock(PdfBlock.WithSize(20, 15));
        secondTrailing.SetBackground(PdfColor.Gray(0.6));
        PdfPage page = new PdfPage(120, 100);

        PdfLayoutResult result = table.Draw(new PdfLayoutDrawContext(page, 10, 20, 80, 60));

        string content = page.GetCanvas().ToPdfString();
        Assert.That(result.Fits, Is.True);
        Assert.That(result.Size.Height, Is.EqualTo(30));
        Assert.That(spanning.GetRowSpan(), Is.EqualTo(2));
        Assert.That(content, Does.Contain("10 20 40 30 re f"));
        Assert.That(content, Does.Contain("50 40 40 10 re f"));
        Assert.That(content, Does.Contain("50 20 40 20 re f"));
    }

    [Test]
    public void PaginationKeepsRowSpanGroupsTogetherUnlessBreaksAreAllowed()
    {
        PdfDocument preventedDocument = new PdfDocument();
        PdfLayoutDocument preventedLayout = CreateShortLayout(preventedDocument);
        PdfTableBlock prevented = CreateSpanningTextTable();
        prevented.SetRowSplitPolicy(PdfTableRowSplitPolicy.Prevent);
        preventedLayout.AddBlock(PdfBlock.WithSize(20, 32));
        preventedLayout.AddTableBlock(prevented);
        preventedLayout.Render();

        Assert.That(preventedDocument.GetPageCount(), Is.EqualTo(2));
        Assert.That(preventedDocument.GetPage(0).GetCanvas().ToPdfString(), Does.Not.Contain("(SPAN) Tj"));
        Assert.That(preventedDocument.GetPage(1).GetCanvas().ToPdfString(), Does.Contain("(SPAN) Tj"));
        Assert.That(preventedDocument.GetPage(1).GetCanvas().ToPdfString(), Does.Contain("(ONE) Tj"));
        Assert.That(preventedDocument.GetPage(1).GetCanvas().ToPdfString(), Does.Contain("(TWO) Tj"));

        PdfDocument allowedDocument = new PdfDocument();
        PdfLayoutDocument allowedLayout = CreateShortLayout(allowedDocument);
        PdfTableBlock allowed = CreateSpanningTextTable();
        allowed.SetRowSplitPolicy(PdfTableRowSplitPolicy.Allow);
        allowedLayout.AddBlock(PdfBlock.WithSize(20, 32));
        allowedLayout.AddTableBlock(allowed);
        allowedLayout.Render();

        Assert.That(allowedDocument.GetPageCount(), Is.EqualTo(2));
        Assert.That(allowedDocument.GetPage(0).GetCanvas().ToPdfString(), Does.Contain("(SPAN) Tj"));
        Assert.That(allowedDocument.GetPage(0).GetCanvas().ToPdfString(), Does.Contain("(ONE) Tj"));
        Assert.That(allowedDocument.GetPage(0).GetCanvas().ToPdfString(), Does.Not.Contain("(TWO) Tj"));
        Assert.That(allowedDocument.GetPage(1).GetCanvas().ToPdfString(), Does.Contain("(TWO) Tj"));
    }

    [Test]
    public void SpannedHeadersAndFootersRepeatAcrossEveryTablePage()
    {
        PdfDocument document = new PdfDocument();
        PdfLayoutDocument layout = CreateShortLayout(document);
        PdfTableBlock table = new PdfTableBlock(new double[] { 30, 30 });
        PdfParagraphStyle paragraph = CreateParagraphStyle();
        PdfTableRow firstHeader = table.AddHeaderRow();
        PdfTableCell spanningHeader = firstHeader.AddTextBlock(new PdfTextBlock("HEAD", paragraph));
        spanningHeader.SetRowSpan(2);
        firstHeader.AddTextBlock(new PdfTextBlock("H1", paragraph));
        table.AddHeaderRow().AddTextBlock(new PdfTextBlock("H2", paragraph));
        PdfTableCell footer = table.AddFooterRow().AddTextBlock(new PdfTextBlock("FOOT", paragraph));
        footer.SetColumnSpan(2);
        AddSpannedTextRow(table, "BODY1", paragraph);
        AddSpannedTextRow(table, "BODY2", paragraph);
        AddSpannedTextRow(table, "BODY3", paragraph);
        layout.AddTableBlock(table);

        layout.Render();

        Assert.That(document.GetPageCount(), Is.EqualTo(3));
        for (int index = 0; index < document.GetPageCount(); index++)
        {
            string content = document.GetPage(index).GetCanvas().ToPdfString();
            Assert.That(content, Does.Contain("(HEAD) Tj"));
            Assert.That(content, Does.Contain("(H1) Tj"));
            Assert.That(content, Does.Contain("(H2) Tj"));
            Assert.That(content, Does.Contain("(FOOT) Tj"));
            Assert.That(content, Does.Contain("(BODY" + (index + 1) + ") Tj"));
        }
    }

    [Test]
    public void AdvancedTableConfigurationRejectsInvalidDefinitionsHeightsAndSpans()
    {
        PdfTableBlock table = new PdfTableBlock(new double[] { 20, 20 });
        PdfTableRow row = table.AddRow();
        PdfTableCell cell = row.AddBlock(PdfBlock.WithSize(10, 10));

        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfTableColumnDefinition(PdfTableColumnSizing.Auto, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfTableColumnDefinition((PdfTableColumnSizing)99, 1));
        Assert.Throws<ArgumentNullException>(() => table.SetColumnDefinitions(null!));
        Assert.Throws<ArgumentException>(() => table.SetColumnDefinitions(new PdfTableColumnDefinition[0]));
        Assert.Throws<ArgumentNullException>(() => table.SetColumnDefinitions(new PdfTableColumnDefinition[] { null! }));
        Assert.Throws<ArgumentOutOfRangeException>(() => table.SetRowSplitPolicy((PdfTableRowSplitPolicy)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => table.GetResolvedColumnWidth(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => row.SetHeight(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => row.SetHeight(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => cell.SetColumnSpan(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => cell.SetRowSpan(0));

        cell.SetColumnSpan(3);
        Assert.Throws<ArgumentException>(() => table.Measure(new PdfLayoutContext(100, 100)));
        PdfTableBlock rowOverflow = new PdfTableBlock(new double[] { 20 });
        PdfTableCell rowOverflowCell = rowOverflow.AddRow().AddBlock(PdfBlock.WithSize(10, 10));
        rowOverflowCell.SetRowSpan(2);
        Assert.Throws<ArgumentException>(() => rowOverflow.Measure(new PdfLayoutContext(100, 100)));
    }

    static PdfLayoutDocument CreateShortLayout(PdfDocument document)
    {
        return new PdfLayoutDocument(document, new PdfPageDimensions(100, 60), new PdfPageMargins(10, 5, 10, 5));
    }

    static PdfTableBlock CreateSpanningTextTable()
    {
        PdfParagraphStyle paragraph = CreateParagraphStyle();
        PdfTableBlock table = new PdfTableBlock(new double[] { 30, 30 });
        PdfTableRow first = table.AddRow();
        PdfTableCell spanning = first.AddTextBlock(new PdfTextBlock("SPAN", paragraph));
        spanning.SetRowSpan(2);
        first.AddTextBlock(new PdfTextBlock("ONE", paragraph));
        table.AddRow().AddTextBlock(new PdfTextBlock("TWO", paragraph));
        return table;
    }

    static PdfParagraphStyle CreateParagraphStyle()
    {
        PdfTextStyle text = PdfTextStyle.Create(10, PdfFont.Courier);
        return PdfParagraphStyle.Create(text, 12, 0, 0, 0);
    }

    static void AddSpannedTextRow(PdfTableBlock table, string text, PdfParagraphStyle paragraph)
    {
        PdfTableCell cell = table.AddRow().AddTextBlock(new PdfTextBlock(text, paragraph));
        cell.SetColumnSpan(2);
    }
}
