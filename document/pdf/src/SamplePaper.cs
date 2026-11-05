using System;

namespace pdf;

internal static class SamplePaper
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
        PdfPaper paper = new PdfPaper("Compact PDF Helper Architecture");
        PdfImage figure = paper.Document.AddImage(220, 120, CreateFigureImage(220, 120));
        PdfReportTable table = CreateSummaryTable();

        paper.AddAuthor("Ada Lovelace", "Analytical Institute");
        paper.AddAuthor("Grace Hopper", "Compiler Laboratory");
        paper.SetAbstract("This sample paper demonstrates title material, abstract text, sections, figures, tables, citations, bibliography entries, and appendix content generated with paper helper classes.");
        paper.AddKeyword("PDF helpers");
        paper.AddKeyword("document layout");
        paper.AddSection("Introduction");
        paper.AddParagraph("The paper helper composes academic content using the same layout engine as reports and invoices. It keeps title material, figures, tables, citations, and bibliography content on a consistent document template.");
        paper.AddSection("Layout Model");
        paper.AddParagraph("The layout model separates measurement from drawing and uses reusable blocks for paragraphs, figures, and tables. Citation markers can be collected and emitted near the end of the paper.");
        paper.AddFigure(figure, "Figure 1. Synthetic layout pipeline diagram.");
        paper.AddTable(table, "Table 1. Helper responsibilities.");
        paper.AddSection("Discussion");
        paper.AddParagraph("The helper layer is intentionally small, but it demonstrates how domain-specific document builders can sit above generic PDF primitives.");
        paper.AddCitation(new PdfCitation("L1", "Lovelace described structured algorithmic notes."));
        paper.AddCitation(new PdfCitation("H1", "Hopper emphasized readable programming systems."));
        paper.AddBibliographyEntry(new PdfBibliographyEntry("L1", "Lovelace, A.", "Notes on the Analytical Engine", "Scientific Memoirs", "1843"));
        paper.AddBibliographyEntry(new PdfBibliographyEntry("H1", "Hopper, G.", "The Education of a Computer", "Proceedings ACM", "1952"));
        paper.AddAppendix("Implementation Notes", "The appendix records that all sample content is generated locally with standard fonts, generated RGB image data, and deterministic helper inputs.");

        return paper.Build();
    }

    internal static PdfReportTable CreateSummaryTable()
    {
        PdfReportTable table = new PdfReportTable(new string[] { "Helper", "Responsibility" });
        table.AddRow(new string[] { "PdfPaper", "Document template and page furniture" });
        table.AddRow(new string[] { "PdfCitation", "Citation marker formatting" });
        table.AddRow(new string[] { "PdfBibliographyEntry", "Reference list formatting" });
        table.AddRow(new string[] { "PdfReportTable", "Reusable tabular layout" });
        return table;
    }

    internal static byte[] CreateFigureImage(int width, int height)
    {
        byte[] pixels = new byte[width * height * 3];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int offset = (y * width + x) * 3;
                bool inTopBand = y > 18 && y < 42 && x > 18 && x < width - 18;
                bool inMiddleBand = y > 54 && y < 78 && x > 36 && x < width - 36;
                bool inBottomBand = y > 90 && y < 108 && x > 58 && x < width - 58;

                if (inTopBand)
                {
                    pixels[offset] = 44;
                    pixels[offset + 1] = 105;
                    pixels[offset + 2] = 141;
                }
                else if (inMiddleBand)
                {
                    pixels[offset] = 82;
                    pixels[offset + 1] = 138;
                    pixels[offset + 2] = 92;
                }
                else if (inBottomBand)
                {
                    pixels[offset] = 153;
                    pixels[offset + 1] = 109;
                    pixels[offset + 2] = 54;
                }
                else
                {
                    pixels[offset] = 240;
                    pixels[offset + 1] = 243;
                    pixels[offset + 2] = 246;
                }
            }
        }

        return pixels;
    }
}
