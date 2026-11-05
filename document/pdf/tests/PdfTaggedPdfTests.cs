using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfTaggedPdfTests
{
    [Test]
    public void Save_WritesCompleteTaggedPdfStructureAndInteractiveMappings()
    {
        PdfDocument document = new PdfDocument();
        PdfPage firstPage = document.AddPage(300, 400);
        PdfPage secondPage = document.AddPage(300, 400);
        document.SetLanguage("en-US");
        document.EnableTaggedPdf();

        PdfAccessibilityNode root = document.AddAccessibilityNode(firstPage, "Document");
        PdfAccessibilityNode heading = document.AddStructureChild(root, "H1", "Heading");
        PdfAccessibilityNode paragraph = document.AddStructureChild(root, "P", "Paragraph");
        PdfAccessibilityNode figure = document.AddStructureChild(root, "Figure", "Diagram");
        PdfAccessibilityNode table = document.AddStructureChild(root, "Table", "Results");
        PdfAccessibilityNode row = document.AddStructureChild(table, "TR", string.Empty);
        PdfAccessibilityNode header = document.AddStructureChild(row, "TH", "Column heading");
        document.AddStructureChild(row, "TD", "Value");
        PdfAccessibilityNode list = document.AddStructureChildOnPage(root, secondPage, "L", "Steps");
        PdfAccessibilityNode item = document.AddStructureChild(list, "LI", string.Empty);
        document.AddStructureChild(item, "Lbl", "1");
        document.AddStructureChild(item, "LBody", "First step");
        PdfAccessibilityNode linkNode = document.AddStructureChild(root, "Link", "References");
        PdfAccessibilityNode annotationNode = document.AddStructureChild(root, "Annot", "Review note");
        PdfAccessibilityNode formNode = document.AddStructureChild(root, "Form", "Full name");

        document.SetStructureAlternateText(figure, "Quarterly trend diagram");
        document.SetStructureActualText(paragraph, "Normalized paragraph text");
        document.SetStructureLanguage(paragraph, "fr-CA");
        document.SetTableHeaderScope(header, PdfTableHeaderScope.Column);

        int headingMcid = firstPage.GetCanvas().BeginStructureContent(heading);
        firstPage.GetCanvas().DrawText("Quarterly report", 20, 360, 18);
        firstPage.GetCanvas().EndMarkedContent();
        int paragraphMcid = firstPage.GetCanvas().BeginStructureContent(paragraph);
        firstPage.GetCanvas().DrawText("Overview", 20, 330, 11);
        firstPage.GetCanvas().EndMarkedContent();
        firstPage.GetCanvas().BeginStructureContent(figure);
        firstPage.GetCanvas().SetFillColorValue(PdfColor.Gray(0.5));
        firstPage.GetCanvas().FillRectangle(20, 250, 100, 50);
        firstPage.GetCanvas().EndMarkedContent();
        firstPage.GetCanvas().BeginArtifact(PdfArtifactType.Layout);
        firstPage.GetCanvas().DrawText("Decoration", 20, 10, 8);
        firstPage.GetCanvas().EndMarkedContent();
        secondPage.GetCanvas().BeginStructureContent(list);
        secondPage.GetCanvas().DrawText("1. First step", 20, 360, 11);
        secondPage.GetCanvas().EndMarkedContent();
        secondPage.GetCanvas().BeginPaginationArtifact(PdfPaginationArtifactType.Footer);
        secondPage.GetCanvas().DrawText("Page 2", 140, 10, 8);
        secondPage.GetCanvas().EndMarkedContent();

        document.AddNamedDestination("top", firstPage, 0, 400);
        PdfInternalLink internalLink = firstPage.AddInternalLink("top", 20, 210, 60, 12);
        PdfExternalLink externalLink = firstPage.AddExternalLink("https://example.test", 20, 190, 80, 12);
        PdfAnnotation annotation = firstPage.AddTextAnnotation("Review", 120, 190, 16, 16);
        PdfAcroFormField field = document.AddTextField("name", firstPage, 20, 150, 120, 20);
        field.SetAccessibilityName("Full name");
        document.TagInternalLink(linkNode, internalLink);
        document.TagExternalLink(linkNode, externalLink);
        document.TagAnnotation(annotationNode, annotation);
        document.TagFormField(formNode, field);

        string pdf = Encoding.ASCII.GetString(document.ToBytes());

        Assert.That(headingMcid, Is.EqualTo(0));
        Assert.That(paragraphMcid, Is.EqualTo(1));
        Assert.That(pdf, Does.Contain("/StructTreeRoot"));
        Assert.That(pdf, Does.Contain("/MarkInfo << /Marked true >>"));
        Assert.That(pdf, Does.Contain("/ParentTree"));
        Assert.That(pdf, Does.Contain("/ParentTreeNextKey"));
        Assert.That(pdf, Does.Contain("/StructParents 0"));
        Assert.That(pdf, Does.Contain("/StructParents 1"));
        Assert.That(pdf, Does.Contain("/H1 <</MCID 0>> BDC"));
        Assert.That(pdf, Does.Contain("/P <</MCID 1>> BDC"));
        Assert.That(pdf, Does.Contain("/Artifact <</Type /Layout>> BDC"));
        Assert.That(pdf, Does.Contain("/Artifact <</Type /Pagination /Subtype /Footer>> BDC"));
        Assert.That(pdf, Does.Contain("/S /Table"));
        Assert.That(pdf, Does.Contain("/S /L"));
        Assert.That(pdf, Does.Contain("/S /H1"));
        Assert.That(pdf, Does.Contain("/Alt (Quarterly trend diagram)"));
        Assert.That(pdf, Does.Contain("/ActualText (Normalized paragraph text)"));
        Assert.That(pdf, Does.Contain("/Lang (fr-CA)"));
        Assert.That(pdf, Does.Contain("/A << /O /Table /Scope /Column >>"));
        Assert.That(Regex.Matches(pdf, "/Type /OBJR").Count, Is.EqualTo(4));
        Assert.That(Regex.Matches(pdf, "/StructParent [0-9]+").Count, Is.EqualTo(4));
        Assert.That(pdf, Does.Match(@"/Annots \[(?:[0-9]+ 0 R ){4}\]"));
    }

    [Test]
    public void StructureTreeApis_ControlRootAndSiblingReadingOrder()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(100, 100);
        PdfAccessibilityNode firstRoot = document.AddAccessibilityNode(page, "Document");
        PdfAccessibilityNode secondRoot = document.AddAccessibilityNode(page, "Part");
        PdfAccessibilityNode firstChild = document.AddStructureChild(firstRoot, "H1", string.Empty);
        PdfAccessibilityNode secondChild = document.AddStructureChild(firstRoot, "P", string.Empty);

        document.SetStructureReadingOrder(secondRoot, 0);
        document.SetStructureReadingOrder(secondChild, 0);

        Assert.That(document.GetStructureRootCount(), Is.EqualTo(2));
        Assert.That(document.GetStructureRoot(0), Is.SameAs(secondRoot));
        Assert.That(document.GetStructureRoot(1), Is.SameAs(firstRoot));
        Assert.That(document.GetStructureChildCount(firstRoot), Is.EqualTo(2));
        Assert.That(document.GetStructureChild(firstRoot, 0), Is.SameAs(secondChild));
        Assert.That(document.GetStructureChild(firstRoot, 1), Is.SameAs(firstChild));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.GetStructureRoot(2));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.GetStructureChild(firstRoot, 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => document.SetStructureReadingOrder(firstChild, 2));
    }

    [Test]
    public void TaggedPdfApis_RejectInvalidStructureAndIncompleteOutput()
    {
        PdfDocument missingLanguage = new PdfDocument();
        PdfPage missingLanguagePage = missingLanguage.AddPage(100, 100);
        missingLanguage.EnableTaggedPdf();
        missingLanguage.AddAccessibilityNode(missingLanguagePage, "Document");
        Assert.Throws<InvalidOperationException>(() => missingLanguage.ToBytes());

        PdfDocument missingRoot = new PdfDocument();
        missingRoot.AddPage(100, 100);
        missingRoot.SetLanguage("en");
        missingRoot.EnableTaggedPdf();
        Assert.Throws<InvalidOperationException>(() => missingRoot.ToBytes());

        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(100, 100);
        document.SetLanguage("en");
        document.EnableTaggedPdf();
        PdfAccessibilityNode root = document.AddAccessibilityNode(page, "Document");
        PdfAccessibilityNode paragraph = document.AddStructureChild(root, "P", string.Empty);
        PdfAccessibilityNode header = document.AddStructureChild(root, "TH", string.Empty);
        PdfAccessibilityNode foreign = new PdfAccessibilityNode(page, "P", string.Empty);

        Assert.Throws<ArgumentException>(() => document.AddStructureChild(root, "Sidebar", string.Empty));
        Assert.Throws<ArgumentException>(() => document.SetStructureAlternateText(foreign, "Text"));
        Assert.Throws<InvalidOperationException>(() => document.SetTableHeaderScope(paragraph, PdfTableHeaderScope.Row));
        Assert.Throws<ArgumentException>(() => document.SetTableHeaderScope(header, (PdfTableHeaderScope)99));
        Assert.Throws<InvalidOperationException>(() => page.GetCanvas().EndMarkedContent());

        page.GetCanvas().BeginStructureContent(paragraph);
        Assert.Throws<InvalidOperationException>(() => document.ToBytes());
        page.GetCanvas().EndMarkedContent();
        Assert.That(document.ToBytes(), Is.Not.Empty);

        PdfDocument legacy = new PdfDocument();
        PdfPage legacyPage = legacy.AddPage(100, 100);
        legacy.AddAccessibilityNode(legacyPage, "Sidebar");
        Assert.Throws<ArgumentException>(() => legacy.EnableTaggedPdf());
    }

    [Test]
    public void ImportPage_PreservesTaggedStructureMarkedContentAndObjectReferences()
    {
        PdfDocument source = new PdfDocument();
        PdfPage sourcePage = source.AddPage(100, 100);
        source.SetLanguage("en");
        source.EnableTaggedPdf();
        PdfAccessibilityNode root = source.AddAccessibilityNode(sourcePage, "Document");
        PdfAccessibilityNode linkNode = source.AddStructureChild(root, "Link", string.Empty);
        sourcePage.GetCanvas().BeginStructureContent(linkNode);
        sourcePage.GetCanvas().DrawText("site", 10, 50, 10);
        sourcePage.GetCanvas().EndMarkedContent();
        PdfExternalLink link = sourcePage.AddExternalLink("https://example.test", 10, 45, 30, 12);
        source.TagExternalLink(linkNode, link);

        PdfDocument target = new PdfDocument();
        target.ImportPage(source, 0);

        string pdf = Encoding.ASCII.GetString(target.ToBytes());

        Assert.That(target.IsTaggedPdfEnabled(), Is.True);
        Assert.That(target.GetStructureRootCount(), Is.EqualTo(1));
        Assert.That(target.GetStructureChildCount(target.GetStructureRoot(0)), Is.EqualTo(1));
        Assert.That(pdf, Does.Contain("/Link <</MCID 0>> BDC"));
        Assert.That(pdf, Does.Contain("/ParentTree"));
        Assert.That(pdf, Does.Contain("/Type /OBJR"));
        Assert.That(pdf, Does.Contain("/StructParent 1"));

        PdfDocument stampTarget = new PdfDocument();
        stampTarget.AddPage(100, 100);
        Assert.Throws<InvalidOperationException>(() => stampTarget.StampPageForeground(source, 0, 0));
    }
}
