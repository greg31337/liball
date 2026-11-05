using System.Text;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfAdvancedReportTests
{
    [Test]
    public void Report_BuildsCoverSummaryKpiGridGroupedTableChartsFiguresAndAppendix()
    {
        PdfReport report = new PdfReport("Board Review");
        Assert.That(report.GetDocument(), Is.SameAs(report.Document));
        report.SetReportPageFurniture("Board Review | FY 2026", "Confidential");
        report.AddCoverPage("Board Review", "Operating performance", "Example Corp", "FY 2026");
        report.AddExecutiveSummary("Revenue and margin remain ahead of plan.");
        report.AddSubsection("Regional Detail", "North America led year-over-year growth.");
        report.AddKpiLayout(new PdfKpi[]
        {
            new PdfKpi("Revenue", "$42M"),
            new PdfKpi("Margin", "62%"),
            new PdfKpi("Retention", "94%")
        }, 3);

        PdfReportTable table = new PdfReportTable(new string[] { "Metric", "Value" });
        table.AddGroupRow("Commercial");
        table.AddRow(new string[] { "Bookings", "$18M" });
        table.AddTotalsRow(new string[] { "Total", "$18M" });
        report.AddTable(table);
        report.AddChartFigure("Revenue trend", "Figure 1. Quarterly revenue trend.", 100);
        PdfImage image = report.GetDocument().AddImage(2, 1, new byte[] { 255, 0, 0, 0, 0, 255 });
        report.AddFigure(image, "Figure 2. Regional mix.");
        report.AddAppendix("Methodology", "Metrics use the approved finance definitions.");

        PdfDocument document = report.Build();
        string firstPage = document.Pages[0].Canvas.ToPdfString();
        string allContent = GetAllContent(document);

        Assert.That(document.Pages.Count, Is.GreaterThanOrEqualTo(3));
        Assert.That(firstPage, Does.Contain("(Board Review) Tj"));
        Assert.That(firstPage, Does.Contain("(Operating performance) Tj"));
        Assert.That(firstPage, Does.Contain("(Example Corp) Tj"));
        Assert.That(firstPage, Does.Not.Contain("(Board Review | FY 2026) Tj"));
        Assert.That(firstPage, Does.Not.Contain("(Confidential | Page 1"));
        Assert.That(allContent, Does.Contain("(Executive Summary) Tj"));
        Assert.That(allContent, Does.Contain("(Regional Detail) Tj"));
        Assert.That(allContent, Does.Contain("(Revenue) Tj"));
        Assert.That(allContent, Does.Contain("($42M) Tj"));
        Assert.That(allContent, Does.Contain("(Commercial) Tj"));
        Assert.That(allContent, Does.Contain("(Total) Tj"));
        Assert.That(allContent, Does.Contain("(Revenue trend) Tj"));
        Assert.That(allContent, Does.Contain("(Figure 1. Quarterly revenue trend.) Tj"));
        Assert.That(allContent, Does.Contain("/Im1 Do"));
        Assert.That(allContent, Does.Contain("(Figure 2. Regional mix.) Tj"));
        Assert.That(allContent, Does.Contain("(Appendix: Methodology) Tj"));
        Assert.That(allContent, Does.Contain("(Board Review | FY 2026) Tj"));
        Assert.That(allContent, Does.Contain("(Confidential | Page "));
        Assert.That(HasAccessibilityRole(document, "Note"), Is.True);
        Assert.That(HasAccessibilityRole(document, "Figure"), Is.True);
        Assert.That(HasAccessibilityRole(document, "Caption"), Is.True);
    }

    [Test]
    public void ReportTable_CreatesSpanningGroupRowsAndEmphasizedTotalsRows()
    {
        PdfReportTable table = new PdfReportTable(new string[] { "A", "B", "C" });
        table.AddGroupRow("Group One");
        table.AddRow(new string[] { "one", "two", "three" });
        table.AddTotalsRow(new string[] { "Total", string.Empty, "three" });

        PdfTableBlock block = table.CreateBlock(300, HeaderStyle(), BodyStyle());

        Assert.That(block.GetHeaderRowCount(), Is.EqualTo(1));
        Assert.That(block.GetRowCount(), Is.EqualTo(3));
        Assert.That(block.Rows[0].Cells.Count, Is.EqualTo(1));
        Assert.That(block.Rows[0].Cells[0].GetColumnSpan(), Is.EqualTo(3));
        Assert.That(block.Rows[0].Cells[0].HasBackground, Is.True);
        Assert.That(block.Rows[2].Cells.Count, Is.EqualTo(3));
        Assert.That(block.Rows[2].Cells[0].HasBackground, Is.True);
    }

    [Test]
    public void CoverOnlyReport_DoesNotCreateAnEmptyTrailingPage()
    {
        PdfReport report = new PdfReport("Cover");
        report.AddCoverPage("Cover", string.Empty, string.Empty, string.Empty);

        PdfDocument document = report.Build();

        Assert.That(document.Pages.Count, Is.EqualTo(1));
        Assert.That(document.Pages[0].Canvas.ToPdfString(), Does.Contain("(Cover) Tj"));
    }

    [Test]
    public void AdvancedReportApis_ValidateInputsAndRejectLateChanges()
    {
        PdfReport report = new PdfReport("Validation");
        PdfReportTable table = new PdfReportTable(new string[] { "A", "B" });

        Assert.Throws<ArgumentNullException>(() => report.AddCoverPage(null!, "", "", ""));
        Assert.Throws<ArgumentException>(() => report.AddCoverPage("", "", "", ""));
        Assert.Throws<ArgumentNullException>(() => report.AddExecutiveSummary(null!));
        Assert.Throws<ArgumentNullException>(() => report.AddSubsection(null!, "text"));
        Assert.Throws<ArgumentNullException>(() => report.AddKpiLayout(null!, 1));
        Assert.Throws<ArgumentException>(() => report.AddKpiLayout(new PdfKpi[0], 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => report.AddKpiLayout(new PdfKpi[] { new PdfKpi("A", "1") }, 2));
        Assert.Throws<ArgumentNullException>(() => report.AddKpiLayout(new PdfKpi[] { null! }, 1));
        Assert.Throws<ArgumentException>(() => report.AddChartFigure("", "caption", 10));
        Assert.Throws<ArgumentOutOfRangeException>(() => report.AddChartFigure("chart", "caption", 0));
        Assert.Throws<ArgumentException>(() => report.AddAppendix("", "text"));
        Assert.Throws<ArgumentNullException>(() => report.SetReportPageFurniture(null!, "footer"));
        Assert.Throws<ArgumentNullException>(() => table.AddGroupRow(null!));
        Assert.Throws<ArgumentException>(() => table.AddGroupRow(""));
        Assert.Throws<ArgumentNullException>(() => table.AddTotalsRow(null!));
        Assert.Throws<ArgumentException>(() => table.AddTotalsRow(new string[] { "one" }));
        Assert.Throws<ArgumentNullException>(() => table.AddTotalsRow(new string[] { "one", null! }));

        report.AddParagraph("content");
        Assert.Throws<InvalidOperationException>(() => report.AddCoverPage("Late", "", "", ""));
        report.Build();
        Assert.Throws<InvalidOperationException>(() => report.AddExecutiveSummary("late"));
        Assert.Throws<InvalidOperationException>(() => report.SetReportPageFurniture("late", "late"));
    }

    static string GetAllContent(PdfDocument document)
    {
        StringBuilder content = new StringBuilder();
        foreach (PdfPage page in document.Pages)
        {
            content.Append(page.Canvas.ToPdfString());
        }

        return content.ToString();
    }

    static bool HasAccessibilityRole(PdfDocument document, string role)
    {
        foreach (PdfAccessibilityNode node in document.AccessibilityNodes)
        {
            if (node.Role == role)
            {
                return true;
            }
        }

        return false;
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
