using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfReportTests
{
    [Test]
    public void Report_BuildRendersTitleSectionsKpisTablesAndFurniture()
    {
        PdfReport report = new PdfReport("Operations Review");
        PdfReportTable table = new PdfReportTable(new string[] { "Metric", "Value" });

        table.AddRow(new string[] { "Revenue", "$42M" });
        report.AddTitle("Operations Review", "Quarterly performance summary");
        report.AddSection("Executive Summary", "Revenue improved while delivery risk remained stable.");
        report.AddKpi("Gross Margin", "62%");
        report.AddTable(table);
        PdfDocument document = report.Build();

        string content = document.Pages[0].Canvas.ToPdfString();
        Assert.That(document.Pages.Count, Is.EqualTo(1));
        Assert.That(content, Does.Contain("(Operations Review) Tj"));
        Assert.That(content, Does.Contain("(Quarterly performance summary) Tj"));
        Assert.That(content, Does.Contain("(Executive Summary) Tj"));
        Assert.That(content, Does.Contain("(Revenue improved while delivery risk remained stable.) Tj"));
        Assert.That(content, Does.Contain("(Gross Margin) Tj"));
        Assert.That(content, Does.Contain("(62%) Tj"));
        Assert.That(content, Does.Contain("(Metric) Tj"));
        Assert.That(content, Does.Contain("($42M) Tj"));
        Assert.That(content, Does.Contain("(Page 1 of 1) Tj"));
    }

    [Test]
    public void Report_AddFigureRendersImageAndCaption()
    {
        PdfReport report = new PdfReport("Image Report");
        PdfImage image = report.Document.AddImage(2, 2, new byte[] { 255, 0, 0, 0, 255, 0, 0, 0, 255, 255, 255, 255 });

        report.AddFigure(image, "Figure 1. Sample image.");
        PdfDocument document = report.Build();

        string content = document.Pages[0].Canvas.ToPdfString();
        Assert.That(content, Does.Contain("/Im1 Do"));
        Assert.That(content, Does.Contain("(Figure 1. Sample image.) Tj"));
    }

    [Test]
    public void ReportTableCopiesHeadersRowsAndColumnWidths()
    {
        string[] headers = new string[] { "A", "B" };
        string[] row = new string[] { "one", "two" };
        double[] widths = new double[] { 30, 40 };
        PdfReportTable table = new PdfReportTable(headers);

        table.AddRow(row);
        table.SetColumnWidths(widths);
        headers[0] = "changed";
        row[0] = "changed";
        widths[0] = 90;

        PdfTableBlock block = table.CreateBlock(100, HeaderStyle(), BodyStyle());
        Assert.That(table.Headers[0], Is.EqualTo("A"));
        Assert.That(table.Rows[0][0], Is.EqualTo("one"));
        Assert.That(table.ColumnWidths[0], Is.EqualTo(30));
        Assert.That(block.ColumnWidths[0], Is.EqualTo(30));
        Assert.That(block.ColumnWidths[1], Is.EqualTo(40));
    }

    [Test]
    public void ReportKpiAndSectionCreateLayoutBlocks()
    {
        PdfKpi kpi = new PdfKpi("Pipeline", "$12M");
        PdfReportSection section = new PdfReportSection("Outlook", "Demand remains healthy.");

        PdfTableBlock kpiBlock = kpi.CreateBlock(120, HeaderStyle(), BodyStyle());
        PdfStackBlock sectionBlock = section.CreateBlock(HeaderStyle(), BodyStyle());

        Assert.That(kpiBlock.GetRowCount(), Is.EqualTo(2));
        Assert.That(sectionBlock.Items.Count, Is.EqualTo(2));
    }

    [Test]
    public void ReportHelpersRejectInvalidInputs()
    {
        PdfReport report = new PdfReport("Validation");
        PdfReportTable table = new PdfReportTable(new string[] { "A" });
        PdfImage image = report.Document.AddImage(1, 1, new byte[] { 0, 0, 0 });

        Assert.Throws<ArgumentNullException>(() => new PdfReport(null!));
        Assert.Throws<ArgumentNullException>(() => report.AddTitle(null!, "x"));
        Assert.Throws<ArgumentNullException>(() => report.AddTitle("x", null!));
        Assert.Throws<ArgumentNullException>(() => report.AddSection(null!, "x"));
        Assert.Throws<ArgumentNullException>(() => report.AddSection("x", null!));
        Assert.Throws<ArgumentNullException>(() => report.AddParagraph(null!));
        Assert.Throws<ArgumentNullException>(() => report.AddKpi(null!, "x"));
        Assert.Throws<ArgumentNullException>(() => report.AddKpi("x", null!));
        Assert.Throws<ArgumentNullException>(() => report.AddTable(null!));
        Assert.Throws<ArgumentNullException>(() => report.AddFigure(null!, "caption"));
        Assert.Throws<ArgumentNullException>(() => report.AddFigure(image, null!));
        Assert.Throws<ArgumentNullException>(() => new PdfReportSection(null!, "x"));
        Assert.Throws<ArgumentNullException>(() => new PdfReportSection("x", null!));
        Assert.Throws<ArgumentNullException>(() => new PdfKpi(null!, "x"));
        Assert.Throws<ArgumentNullException>(() => new PdfKpi("x", null!));
        Assert.Throws<ArgumentNullException>(() => new PdfReportTable(null!));
        Assert.Throws<ArgumentException>(() => new PdfReportTable(new string[0]));
        Assert.Throws<ArgumentNullException>(() => new PdfReportTable(new string[] { null! }));
        Assert.Throws<ArgumentNullException>(() => table.AddRow(null!));
        Assert.Throws<ArgumentException>(() => table.AddRow(new string[] { "x", "y" }));
        Assert.Throws<ArgumentNullException>(() => table.AddRow(new string[] { null! }));
        Assert.Throws<ArgumentNullException>(() => table.SetColumnWidths(null!));
        Assert.Throws<ArgumentException>(() => table.SetColumnWidths(new double[] { 10, 20 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => table.SetColumnWidths(new double[] { 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => table.CreateBlock(0, HeaderStyle(), BodyStyle()));
        Assert.Throws<ArgumentNullException>(() => table.CreateBlock(100, null!, BodyStyle()));
        Assert.Throws<ArgumentNullException>(() => table.CreateBlock(100, HeaderStyle(), null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfImageBlock(image, 0, 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PdfImageBlock(image, 10, 0));
        Assert.Throws<ArgumentNullException>(() => new PdfImageBlock(null!, 10, 10));
        Assert.Throws<ArgumentNullException>(() => PdfLayoutItem.FromImageBlock(null!));
        Assert.Throws<ArgumentNullException>(() => report.Layout.AddImageBlock(null!));
    }

    [Test]
    public void ReportRejectsChangesAfterBuild()
    {
        PdfReport report = new PdfReport("Built");

        report.Build();

        Assert.Throws<InvalidOperationException>(() => report.AddParagraph("late"));
    }

    static PdfParagraphStyle HeaderStyle()
    {
        return PdfParagraphStyle.Create(PdfTextStyle.Create(10, PdfFont.Bold), 12, 0, 0, 0);
    }

    static PdfParagraphStyle BodyStyle()
    {
        return PdfParagraphStyle.Create(PdfTextStyle.Create(10, PdfFont.Regular), 12, 0, 0, 0);
    }
}
