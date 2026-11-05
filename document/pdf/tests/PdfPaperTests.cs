using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfPaperTests
{
    [Test]
    public void Paper_BuildRendersTitleAuthorsAbstractKeywordsSectionsFiguresCitationsAndBibliography()
    {
        PdfPaper paper = new PdfPaper("A Small Paper");
        PdfImage image = paper.Document.AddImage(2, 2, new byte[] { 255, 0, 0, 0, 255, 0, 0, 0, 255, 255, 255, 255 });

        paper.AddAuthor("Ada Lovelace", "Analytical Institute");
        paper.SetAbstract("This paper studies compact PDF helper APIs.");
        paper.AddKeyword("PDF generation");
        paper.AddSection("Introduction");
        paper.AddParagraph("The introduction is intentionally short.");
        paper.AddFigure(image, "Figure 1. A generated figure.");
        paper.AddCitation(new PdfCitation("L1", "Lovelace introduced algorithmic notes."));
        paper.AddBibliographyEntry(new PdfBibliographyEntry("L1", "Lovelace, A.", "Notes", "Scientific Memoirs", "1843"));
        PdfDocument document = paper.Build();

        string content = document.Pages[0].Canvas.ToPdfString();
        Assert.That(document.Pages.Count, Is.EqualTo(1));
        Assert.That(content, Does.Contain("(A Small Paper) Tj"));
        Assert.That(content, Does.Contain("(Ada Lovelace, Analytical Institute) Tj"));
        Assert.That(content, Does.Contain("(Abstract) Tj"));
        Assert.That(content, Does.Contain("(This paper studies compact PDF helper APIs.) Tj"));
        Assert.That(content, Does.Contain("(Keywords: PDF generation) Tj"));
        Assert.That(content, Does.Contain("(Introduction) Tj"));
        Assert.That(content, Does.Contain("(The introduction is intentionally short.) Tj"));
        Assert.That(content, Does.Contain("/Im1 Do"));
        Assert.That(content, Does.Contain("(Figure 1. A generated figure.) Tj"));
        Assert.That(content, Does.Contain("(Citations) Tj"));
        Assert.That(content, Does.Contain("([L1] Lovelace introduced algorithmic notes.) Tj"));
        Assert.That(content, Does.Contain("(Bibliography) Tj"));
        Assert.That(content, Does.Contain("([L1] Lovelace, A. Notes. Scientific Memoirs, 1843.) Tj"));
        Assert.That(content, Does.Contain("(Page 1 of 1) Tj"));
    }

    [Test]
    public void Paper_AddTableAndAppendixRender()
    {
        PdfPaper paper = new PdfPaper("Tables");
        PdfReportTable table = new PdfReportTable(new string[] { "Variable", "Value" });

        table.AddRow(new string[] { "n", "12" });
        paper.AddTable(table, "Table 1. Study variables.");
        paper.AddAppendix("Data", "The appendix includes supporting observations.");
        PdfDocument document = paper.Build();

        string content = document.Pages[0].Canvas.ToPdfString();
        Assert.That(content, Does.Contain("(Variable) Tj"));
        Assert.That(content, Does.Contain("(12) Tj"));
        Assert.That(content, Does.Contain("(Table 1. Study variables.) Tj"));
        Assert.That(content, Does.Contain("(Appendix: Data) Tj"));
        Assert.That(content, Does.Contain("(The appendix includes supporting observations.) Tj"));
    }

    [Test]
    public void PaperCitationAndBibliographyFormatEntries()
    {
        PdfCitation citation = new PdfCitation("S1", "Source text.");
        PdfBibliographyEntry entry = new PdfBibliographyEntry("S1", "Smith, J.", "Paper Title", "Journal", "2026");

        Assert.That(citation.Format(), Is.EqualTo("[S1] Source text."));
        Assert.That(entry.Format(), Is.EqualTo("[S1] Smith, J. Paper Title. Journal, 2026."));
    }

    [Test]
    public void PaperHelpersRejectInvalidInputs()
    {
        PdfPaper paper = new PdfPaper("Validation");
        PdfImage image = paper.Document.AddImage(1, 1, new byte[] { 0, 0, 0 });
        PdfReportTable table = new PdfReportTable(new string[] { "A" });

        Assert.Throws<ArgumentNullException>(() => new PdfPaper(null!));
        Assert.Throws<ArgumentNullException>(() => paper.AddAuthor(null!, "x"));
        Assert.Throws<ArgumentNullException>(() => paper.AddAuthor("x", null!));
        Assert.Throws<ArgumentNullException>(() => paper.SetAbstract(null!));
        Assert.Throws<ArgumentNullException>(() => paper.AddKeyword(null!));
        Assert.Throws<ArgumentNullException>(() => paper.AddSection(null!));
        Assert.Throws<ArgumentNullException>(() => paper.AddParagraph(null!));
        Assert.Throws<ArgumentNullException>(() => paper.AddFigure(null!, "caption"));
        Assert.Throws<ArgumentNullException>(() => paper.AddFigure(image, null!));
        Assert.Throws<ArgumentNullException>(() => paper.AddTable(null!, "caption"));
        Assert.Throws<ArgumentNullException>(() => paper.AddTable(table, null!));
        Assert.Throws<ArgumentNullException>(() => paper.AddCitation(null!));
        Assert.Throws<ArgumentNullException>(() => paper.AddBibliographyEntry(null!));
        Assert.Throws<ArgumentNullException>(() => paper.AddAppendix(null!, "text"));
        Assert.Throws<ArgumentNullException>(() => paper.AddAppendix("A", null!));
        Assert.Throws<ArgumentNullException>(() => new PdfPaperSection(null!));
        Assert.Throws<ArgumentNullException>(() => new PdfPaperSection("x").CreateBlock(null!));
        Assert.Throws<ArgumentNullException>(() => new PdfCitation(null!, "x"));
        Assert.Throws<ArgumentNullException>(() => new PdfCitation("x", null!));
        Assert.Throws<ArgumentNullException>(() => new PdfBibliographyEntry(null!, "a", "t", "s", "y"));
        Assert.Throws<ArgumentNullException>(() => new PdfBibliographyEntry("k", null!, "t", "s", "y"));
        Assert.Throws<ArgumentNullException>(() => new PdfBibliographyEntry("k", "a", null!, "s", "y"));
        Assert.Throws<ArgumentNullException>(() => new PdfBibliographyEntry("k", "a", "t", null!, "y"));
        Assert.Throws<ArgumentNullException>(() => new PdfBibliographyEntry("k", "a", "t", "s", null!));
    }

    [Test]
    public void PaperRejectsChangesAfterBuild()
    {
        PdfPaper paper = new PdfPaper("Built");

        paper.Build();

        Assert.Throws<InvalidOperationException>(() => paper.AddParagraph("late"));
    }

    [Test]
    public void PaperPublishingHelpersRenderAffiliationsHeadingsNotesAndLinkedReferences()
    {
        PdfPaper paper = new PdfPaper("Linked Paper");
        paper.AddAffiliation(new PdfPaperAffiliation("1", "Analytical Institute"));
        paper.AddAuthorWithAffiliations("Ada Lovelace", new string[] { "1" });
        paper.SetRunningHead("COMPACT PDF STUDY");
        paper.SetPageLabels(PdfPageLabelStyle.UpperRoman, "P-", 3);
        paper.AddHeading("Background", PdfPaperHeadingLevel.Level2);
        paper.AddReferencedHeading("methods", "Methods", PdfPaperHeadingLevel.Level3);
        paper.AddParagraph("The method is reproducible.");
        paper.AddCrossReference("methods", "Return to Methods");
        paper.AddFootnote("Measured on the first page.");
        paper.AddEndnote("Supporting material is archived.");
        paper.AddCitation(new PdfCitation("L1", "A foundational source."));
        paper.AddBibliographyEntry(new PdfBibliographyEntry("L1", "Lovelace, A.", "Notes", "Scientific Memoirs", "1843"));
        paper.EnableCitationLinks();

        PdfDocument document = paper.Build();

        string content = document.Pages[0].Canvas.ToPdfString();
        Assert.That(content, Does.Contain("(1 Analytical Institute) Tj"));
        Assert.That(content, Does.Contain("(Ada Lovelace [1]) Tj"));
        Assert.That(content, Does.Contain("(COMPACT PDF STUDY) Tj"));
        Assert.That(content, Does.Contain("(Background) Tj"));
        Assert.That(content, Does.Contain("(Methods) Tj"));
        Assert.That(content, Does.Contain("([1] Measured on the first page.) Tj"));
        Assert.That(content, Does.Contain("(Endnotes) Tj"));
        Assert.That(content, Does.Contain("([1] Supporting material is archived.) Tj"));
        Assert.That(document.GetPageLabel(0), Is.EqualTo("P-III"));
        Assert.That(document.GetCrossReferenceText("methods"), Is.EqualTo("Methods, page P-III"));
        Assert.That(document.GetCrossReferenceMarkerCount(), Is.EqualTo(2));
        Assert.That(document.Pages[0].InternalLinks.Count, Is.EqualTo(2));
    }

    [Test]
    public void PaperPublishingHelpersGenerateLinkedListsAndFlowTwoColumnSections()
    {
        PdfPaper paper = new PdfPaper("Publishing Paper");
        PdfImage image = paper.Document.AddImage(1, 1, new byte[] { 25, 50, 75 });
        PdfReportTable table = new PdfReportTable(new string[] { "Item", "Value" });
        table.AddRow(new string[] { "Count", "40" });
        paper.AddFigure(image, "Figure 1. Sample image.");
        paper.AddTable(table, "Table 1. Sample values.");

        string[] paragraphs = new string[40];
        for (int index = 0; index < paragraphs.Length; index++)
        {
            paragraphs[index] = "Column paragraph " + (index + 1) + ".";
        }

        paper.AddTwoColumnSection("Results", paragraphs, 18);
        paper.IncludeListOfFigures();
        paper.IncludeListOfTables();

        PdfDocument document = paper.Build();

        Assert.That(document.Pages.Count, Is.EqualTo(4));
        Assert.That(document.Pages[1].Canvas.ToPdfString(), Does.Contain("(Results) Tj"));
        Assert.That(document.Pages[1].Canvas.ToPdfString(), Does.Contain("(Column paragraph 40.) Tj"));
        Assert.That(document.Pages[2].Canvas.ToPdfString(), Does.Contain("(List of Figures) Tj"));
        Assert.That(document.Pages[3].Canvas.ToPdfString(), Does.Contain("(List of Tables) Tj"));
        Assert.That(document.GetPublishingMarkerCount(), Is.EqualTo(2));
        Assert.That(document.Pages[2].InternalLinks.Count, Is.EqualTo(1));
        Assert.That(document.Pages[3].InternalLinks.Count, Is.EqualTo(1));
    }

    [Test]
    public void PaperPublishingHelpersRejectInvalidConfiguration()
    {
        PdfPaper paper = new PdfPaper("Validation");
        PdfPaperAffiliation affiliation = new PdfPaperAffiliation("1", "Institute");
        paper.AddAffiliation(affiliation);

        Assert.Throws<ArgumentNullException>(() => new PdfPaperAffiliation(null!, "Institute"));
        Assert.Throws<ArgumentException>(() => new PdfPaperAffiliation("", "Institute"));
        Assert.Throws<ArgumentException>(() => paper.AddAffiliation(affiliation));
        Assert.Throws<ArgumentException>(() => paper.AddAuthorWithAffiliations("Ada", new string[] { "missing" }));
        Assert.Throws<ArgumentException>(() => paper.AddHeading("Heading", (PdfPaperHeadingLevel)99));
        Assert.Throws<ArgumentException>(() => paper.AddReferencedHeading("", "Heading", PdfPaperHeadingLevel.Level1));
        Assert.Throws<ArgumentNullException>(() => paper.AddCrossReference("target", null!));
        Assert.Throws<ArgumentNullException>(() => paper.AddFootnote(null!));
        Assert.Throws<ArgumentNullException>(() => paper.AddEndnote(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => paper.AddTwoColumnSection("Columns", new string[] { "Text" }, -1));
        Assert.Throws<ArgumentException>(() => paper.SetPageLabels((PdfPageLabelStyle)99, "", 1));
        Assert.Throws<ArgumentException>(() => paper.SetPageLabels(PdfPageLabelStyle.None, "", 1));
    }
}
