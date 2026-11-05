using System;

namespace pdf;

internal static class SampleReport
{
    internal static void Generate(string outputPath)
    {
        if (outputPath == null)
        {
            throw new ArgumentNullException("outputPath");
        }

        PdfDocument document = BuildDocument();
        document.Save(outputPath);
    }

    internal static PdfDocument BuildDocument()
    {
        PdfReport report = new PdfReport("Operational Health Report");
        PdfImage image = report.Document.AddImage(260, 140, CreateLandscapeImage(260, 140));
        PdfReportTable table = CreatePortfolioTable();

        report.AddTitle("Operational Health Report", "Sections, KPI cards, figure, detail table, appendix, and page furniture.");
        report.AddSection(
            "Executive Summary",
            "The portfolio remains on track with stronger recurring revenue, stable delivery capacity, and a focused risk register. The report helper classes build this sample from reusable sections, KPI cards, figures, tables, and automatic page furniture.");
        report.AddKpi("Annual Recurring Revenue", "$42M");
        report.AddKpi("Gross Margin", "62%");
        report.AddKpi("Open Delivery Risks", "7");
        report.AddFigure(image, "Figure 1. Generated landscape used as a report figure with caption.");
        report.AddSection(
            "Portfolio Status",
            "The detail table summarizes quarterly initiatives, owners, and current status. It is intentionally long enough to exercise table pagination and repeated page furniture.");
        report.AddTable(table);
        report.AddSection(
            "Appendix: Methodology",
            "Metrics are compiled from operating reviews, delivery plans, and program status updates. Revenue values are rounded for presentation, margin values are point-in-time estimates, and delivery risk counts include only active risks requiring leadership attention.");
        report.AddParagraph("Data refresh cadence: weekly for operational metrics and monthly for financial metrics.");

        return report.Build();
    }

    internal static PdfReportTable CreatePortfolioTable()
    {
        PdfReportTable table = new PdfReportTable(new string[] { "Quarter", "Focus", "Owner", "Status" });
        table.SetColumnWidths(new double[] { 70, 224, 100, 90 });
        AddPortfolioRows(table);
        return table;
    }

    internal static void AddPortfolioRows(PdfReportTable table)
    {
        table.AddRow(new string[] { "Q1", "Define reporting template", "Design", "Done" });
        table.AddRow(new string[] { "Q1", "Create PDF object writer", "Platform", "Done" });
        table.AddRow(new string[] { "Q1", "Add text wrapping", "Platform", "Done" });
        table.AddRow(new string[] { "Q2", "Embed generated image", "Media", "Done" });
        table.AddRow(new string[] { "Q2", "Add page header", "Design", "Done" });
        table.AddRow(new string[] { "Q2", "Add page footer", "Design", "Done" });
        table.AddRow(new string[] { "Q2", "Add page numbering", "Platform", "Done" });
        table.AddRow(new string[] { "Q3", "Split long tables", "Reports", "Ready" });
        table.AddRow(new string[] { "Q3", "Add chart placeholders", "Reports", "Ready" });
        table.AddRow(new string[] { "Q3", "Add table totals", "Reports", "Planned" });
        table.AddRow(new string[] { "Q3", "Add document metadata", "Platform", "Planned" });
        table.AddRow(new string[] { "Q4", "Support custom themes", "Design", "Planned" });
        table.AddRow(new string[] { "Q4", "Support reusable sections", "Reports", "Planned" });
        table.AddRow(new string[] { "Q4", "Add automatic pagination", "Platform", "Planned" });
        table.AddRow(new string[] { "Q4", "Add validation tests", "Quality", "Planned" });
        table.AddRow(new string[] { "Q4", "Package sample templates", "Docs", "Planned" });
        table.AddRow(new string[] { "Q1", "Create invoice helpers", "Apps", "Next" });
        table.AddRow(new string[] { "Q1", "Create paper helpers", "Publishing", "Next" });
    }

    internal static byte[] CreateLandscapeImage(int width, int height)
    {
        byte[] pixels = new byte[width * height * 3];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int offset = (y * width + x) * 3;
                double horizon = height * 0.58;
                double sunDistance = Math.Sqrt(Math.Pow(x - width * 0.76, 2) + Math.Pow(y - height * 0.30, 2));
                double sun = Math.Max(0, 1 - sunDistance / 38);

                if (y < horizon)
                {
                    pixels[offset] = (byte)Math.Clamp(95 + x * 0.24 + sun * 135, 0, 255);
                    pixels[offset + 1] = (byte)Math.Clamp(155 + y * 0.45 + sun * 70, 0, 255);
                    pixels[offset + 2] = (byte)Math.Clamp(198 + y * 0.18 + sun * 20, 0, 255);
                    continue;
                }

                double ridgeOne = height * 0.65 + Math.Sin(x / 24.0) * 11;
                double ridgeTwo = height * 0.76 + Math.Cos(x / 18.0) * 8;

                if (y < ridgeOne)
                {
                    pixels[offset] = 70;
                    pixels[offset + 1] = 117;
                    pixels[offset + 2] = 111;
                }
                else if (y < ridgeTwo)
                {
                    pixels[offset] = 51;
                    pixels[offset + 1] = 91;
                    pixels[offset + 2] = 82;
                }
                else
                {
                    pixels[offset] = 42;
                    pixels[offset + 1] = 71;
                    pixels[offset + 2] = 64;
                }
            }
        }

        return pixels;
    }
}
