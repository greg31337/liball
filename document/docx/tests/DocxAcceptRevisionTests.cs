using System.Xml;
using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxAcceptRevisionTests
{
    [Test]
    public void ParagraphAcceptRevisions_UnwrapsInsertedAndMoveToAndRemovesDeletedMoveFromAndFormattingChanges()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:pPr><w:pStyle w:val=\"Body\" /><w:pPrChange w:id=\"5\"><w:pPr><w:jc w:val=\"center\" /></w:pPr></w:pPrChange></w:pPr><w:r><w:t>A</w:t></w:r><w:ins w:id=\"1\"><w:r><w:t>B</w:t></w:r></w:ins><w:del w:id=\"2\"><w:r><w:delText>C</w:delText></w:r></w:del><w:moveFrom w:id=\"3\"><w:r><w:t>D</w:t></w:r></w:moveFrom><w:moveTo w:id=\"4\"><w:r><w:t>E</w:t></w:r></w:moveTo><w:r><w:t>F</w:t></w:r></w:p>");

        int count = paragraph.AcceptRevisions();
        string xml = paragraph.WriteXml();

        Assert.That(count, Is.EqualTo(5));
        Assert.That(paragraph.GetText(), Is.EqualTo("ABEF"));
        Assert.That(paragraph.RevisionCount(), Is.EqualTo(0));
        Assert.That(paragraph.UnknownElementCount(), Is.EqualTo(0));
        Assert.That(paragraph.RunCount(), Is.EqualTo(4));
        Assert.That(xml, Does.Not.Contain("w:ins"));
        Assert.That(xml, Does.Not.Contain("w:del"));
        Assert.That(xml, Does.Not.Contain("w:moveFrom"));
        Assert.That(xml, Does.Not.Contain("w:moveTo"));
        Assert.That(xml, Does.Not.Contain("w:pPrChange"));
        Assert.That(xml, Does.Contain("w:pStyle"));
    }

    [Test]
    public void RunAcceptRevisions_UnwrapsAcceptedTextAndRemovesRunPropertyChange()
    {
        global::docx.DocxRun run = ParseRun("<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:rPr><w:b /><w:rPrChange w:id=\"3\"><w:rPr><w:i /></w:rPr></w:rPrChange></w:rPr><w:t>A</w:t><w:ins w:id=\"1\"><w:r><w:t>B</w:t></w:r></w:ins><w:del w:id=\"2\"><w:r><w:delText>C</w:delText></w:r></w:del></w:r>");

        int count = run.AcceptRevisions();
        string xml = run.WriteXml();

        Assert.That(count, Is.EqualTo(3));
        Assert.That(run.GetText(), Is.EqualTo("AB"));
        Assert.That(run.RevisionCount(), Is.EqualTo(0));
        Assert.That(run.TextCount(), Is.EqualTo(2));
        Assert.That(run.UnknownElementCount(), Is.EqualTo(0));
        Assert.That(xml, Does.Contain(">B</w:t>"));
        Assert.That(xml, Does.Not.Contain("w:rPrChange"));
        Assert.That(xml, Does.Not.Contain("w:delText"));
    }

    [Test]
    public void TableAcceptRevisions_AcceptsNestedCellContent()
    {
        global::docx.DocxTable table = ParseTable("<w:tbl xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tr><w:tc><w:p><w:r><w:t>A</w:t></w:r><w:ins w:id=\"1\"><w:r><w:t>B</w:t></w:r></w:ins><w:del w:id=\"2\"><w:r><w:delText>C</w:delText></w:r></w:del></w:p></w:tc></w:tr></w:tbl>");

        int count = table.AcceptRevisions();
        string xml = table.WriteXml();

        Assert.That(count, Is.EqualTo(2));
        Assert.That(table.GetText(), Is.EqualTo("AB"));
        Assert.That(table.GetRow(0).GetCell(0).GetText(), Is.EqualTo("AB"));
        Assert.That(xml, Does.Not.Contain("w:ins"));
        Assert.That(xml, Does.Not.Contain("w:del"));
    }

    [Test]
    public void RevisionAcceptXml_ReturnsAcceptedReplacementXml()
    {
        global::docx.DocxRevision inserted = global::docx.DocxRevision.ParseXml("<w:ins xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:id=\"1\"><w:r><w:t>A</w:t></w:r></w:ins>");
        global::docx.DocxRevision deleted = global::docx.DocxRevision.ParseXml("<w:del xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:id=\"2\"><w:r><w:delText>B</w:delText></w:r></w:del>");

        Assert.That(inserted.AcceptXml(), Does.Contain("<w:r"));
        Assert.That(inserted.AcceptXml(), Does.Not.Contain("w:ins"));
        Assert.That(deleted.AcceptXml(), Is.EqualTo(""));
    }

    [Test]
    public void BlockAndBodyAcceptRevisions_UpdateNestedContent()
    {
        global::docx.DocxBody body = ParseBody("<w:body xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p><w:r><w:t>A</w:t></w:r><w:ins w:id=\"1\"><w:r><w:t>B</w:t></w:r></w:ins></w:p></w:body>");
        global::docx.DocxBlock block = body.GetBlock(0);

        Assert.That(block.AcceptRevisions(), Is.EqualTo(1));
        Assert.That(block.GetText(), Is.EqualTo("AB"));

        body = ParseBody("<w:body xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p><w:r><w:t>A</w:t></w:r><w:del w:id=\"2\"><w:r><w:delText>B</w:delText></w:r></w:del></w:p></w:body>");

        Assert.That(body.AcceptRevisions(), Is.EqualTo(1));
        Assert.That(body.GetBlock(0).GetText(), Is.EqualTo("A"));
        Assert.That(body.WriteXml(), Does.Not.Contain("w:del"));
    }

    [Test]
    public void DocumentAcceptRevisions_UpdatesMainDocumentPartAndReparsesBody()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        package.ReplaceXmlPart("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body><w:p><w:r><w:t>A</w:t></w:r><w:ins w:id=\"1\"><w:r><w:t>B</w:t></w:r></w:ins><w:del w:id=\"2\"><w:r><w:delText>C</w:delText></w:r></w:del></w:p></w:body></w:document>");
        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);

        int count = document.AcceptRevisions();

        Assert.That(count, Is.EqualTo(2));
        Assert.That(document.Body.GetBlock(0).GetText(), Is.EqualTo("AB"));
        Assert.That(package.GetMainDocumentPart()!.GetText(), Does.Not.Contain("w:ins"));
        Assert.That(package.GetMainDocumentPart()!.GetText(), Does.Not.Contain("w:del"));
    }

    [Test]
    public void AcceptRevisions_RejectsInvalidArguments()
    {
        XmlDocument document = new XmlDocument();
        document.LoadXml("<root />");
        XmlWriter writer = XmlWriter.Create(new StringWriter());

        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRevision.AcceptRevisionXml(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRevision.CountRevisionElementsXml(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRevision.CountRevisionElementsNode(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRevision.WriteAcceptedNode(null!, writer));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRevision.WriteAcceptedNode(document.DocumentElement!, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRevision.WriteAcceptedElement(null!, writer));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRevision.WriteAcceptedElement(document.DocumentElement!, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRevision.IsRevisionElement(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRevision.ShouldKeepAcceptedRevisionChildren(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRevisions.AcceptRevisionXml(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRun.FlattenNestedRunXml(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRun.WriteFlattenedRunElement(null!, writer));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRun.WriteFlattenedRunElement(document.DocumentElement!, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRun.IsNestedRunElement(null!));
    }

    static global::docx.DocxParagraph ParseParagraph(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("p", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxParagraph.Parse(reader);
    }

    static global::docx.DocxRun ParseRun(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("r", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxRun.Parse(reader);
    }

    static global::docx.DocxTable ParseTable(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("tbl", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxTable.Parse(reader);
    }

    static global::docx.DocxBody ParseBody(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("body", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxBody.Parse(reader);
    }
}
