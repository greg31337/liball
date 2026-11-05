using System.Xml;
using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxRejectRevisionTests
{
    [Test]
    public void ParagraphRejectRevisions_RemovesInsertedMoveToAndRestoresDeletedMoveFromAndOldFormatting()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:pPr><w:pStyle w:val=\"Body\" /><w:pPrChange w:id=\"5\"><w:pPr><w:pStyle w:val=\"Old\" /></w:pPr></w:pPrChange></w:pPr><w:r><w:t>A</w:t></w:r><w:ins w:id=\"1\"><w:r><w:t>B</w:t></w:r></w:ins><w:del w:id=\"2\"><w:r><w:delText>C</w:delText></w:r></w:del><w:moveFrom w:id=\"3\"><w:r><w:t>D</w:t></w:r></w:moveFrom><w:moveTo w:id=\"4\"><w:r><w:t>E</w:t></w:r></w:moveTo><w:r><w:t>F</w:t></w:r></w:p>");

        int count = paragraph.RejectRevisions();
        string xml = paragraph.WriteXml();

        Assert.That(count, Is.EqualTo(5));
        Assert.That(paragraph.GetText(), Is.EqualTo("ACDF"));
        Assert.That(paragraph.RevisionCount(), Is.EqualTo(0));
        Assert.That(paragraph.UnknownElementCount(), Is.EqualTo(0));
        Assert.That(paragraph.RunCount(), Is.EqualTo(4));
        Assert.That(xml, Does.Not.Contain("w:ins"));
        Assert.That(xml, Does.Not.Contain("w:del"));
        Assert.That(xml, Does.Not.Contain("w:moveFrom"));
        Assert.That(xml, Does.Not.Contain("w:moveTo"));
        Assert.That(xml, Does.Not.Contain("w:pPrChange"));
        Assert.That(xml, Does.Not.Contain("w:delText"));
        Assert.That(xml, Does.Contain("Old"));
        Assert.That(xml, Does.Not.Contain("Body"));
    }

    [Test]
    public void RunRejectRevisions_RestoresDeletedTextAndOldRunProperties()
    {
        global::docx.DocxRun run = ParseRun("<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:rPr><w:b /><w:rPrChange w:id=\"3\"><w:rPr><w:i /></w:rPr></w:rPrChange></w:rPr><w:t>A</w:t><w:ins w:id=\"1\"><w:r><w:t>B</w:t></w:r></w:ins><w:del w:id=\"2\"><w:r><w:delText>C</w:delText></w:r></w:del></w:r>");

        int count = run.RejectRevisions();
        string xml = run.WriteXml();

        Assert.That(count, Is.EqualTo(3));
        Assert.That(run.GetText(), Is.EqualTo("AC"));
        Assert.That(run.RevisionCount(), Is.EqualTo(0));
        Assert.That(run.TextCount(), Is.EqualTo(2));
        Assert.That(run.UnknownElementCount(), Is.EqualTo(0));
        Assert.That(xml, Does.Contain(">C</w:t>"));
        Assert.That(xml, Does.Contain("w:i"));
        Assert.That(xml, Does.Not.Contain("w:b"));
        Assert.That(xml, Does.Not.Contain("w:rPrChange"));
        Assert.That(xml, Does.Not.Contain("w:delText"));
    }

    [Test]
    public void TableRejectRevisions_RestoresNestedCellContent()
    {
        global::docx.DocxTable table = ParseTable("<w:tbl xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tr><w:tc><w:p><w:r><w:t>A</w:t></w:r><w:ins w:id=\"1\"><w:r><w:t>B</w:t></w:r></w:ins><w:del w:id=\"2\"><w:r><w:delText>C</w:delText></w:r></w:del></w:p></w:tc></w:tr></w:tbl>");

        int count = table.RejectRevisions();
        string xml = table.WriteXml();

        Assert.That(count, Is.EqualTo(2));
        Assert.That(table.GetText(), Is.EqualTo("AC"));
        Assert.That(table.GetRow(0).GetCell(0).GetText(), Is.EqualTo("AC"));
        Assert.That(xml, Does.Not.Contain("w:ins"));
        Assert.That(xml, Does.Not.Contain("w:del"));
        Assert.That(xml, Does.Not.Contain("w:delText"));
    }

    [Test]
    public void RevisionRejectXml_ReturnsRejectedReplacementXml()
    {
        global::docx.DocxRevision inserted = global::docx.DocxRevision.ParseXml("<w:ins xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:id=\"1\"><w:r><w:t>A</w:t></w:r></w:ins>");
        global::docx.DocxRevision deleted = global::docx.DocxRevision.ParseXml("<w:del xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:id=\"2\"><w:r><w:delText>B</w:delText></w:r></w:del>");

        Assert.That(inserted.RejectXml(), Is.EqualTo(""));
        Assert.That(deleted.RejectXml(), Does.Contain("<w:r"));
        Assert.That(deleted.RejectXml(), Does.Contain(">B</w:t>"));
        Assert.That(deleted.RejectXml(), Does.Not.Contain("w:del"));
        Assert.That(deleted.RejectXml(), Does.Not.Contain("w:delText"));
    }

    [Test]
    public void BlockBodyAndDocumentRejectRevisions_UpdateNestedContent()
    {
        global::docx.DocxBody body = ParseBody("<w:body xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p><w:r><w:t>A</w:t></w:r><w:del w:id=\"1\"><w:r><w:delText>B</w:delText></w:r></w:del></w:p></w:body>");
        global::docx.DocxBlock block = body.GetBlock(0);

        Assert.That(block.RejectRevisions(), Is.EqualTo(1));
        Assert.That(block.GetText(), Is.EqualTo("AB"));

        body = ParseBody("<w:body xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p><w:r><w:t>A</w:t></w:r><w:ins w:id=\"2\"><w:r><w:t>B</w:t></w:r></w:ins></w:p></w:body>");

        Assert.That(body.RejectRevisions(), Is.EqualTo(1));
        Assert.That(body.GetBlock(0).GetText(), Is.EqualTo("A"));
        Assert.That(body.WriteXml(), Does.Not.Contain("w:ins"));

        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        package.ReplaceXmlPart("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body><w:p><w:r><w:t>A</w:t></w:r><w:del w:id=\"3\"><w:r><w:delText>B</w:delText></w:r></w:del></w:p></w:body></w:document>");
        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);

        Assert.That(document.RejectRevisions(), Is.EqualTo(1));
        Assert.That(document.Body.GetBlock(0).GetText(), Is.EqualTo("AB"));
        Assert.That(package.GetMainDocumentPart()!.GetText(), Does.Not.Contain("w:del"));
        Assert.That(package.GetMainDocumentPart()!.GetText(), Does.Not.Contain("w:delText"));
    }

    [Test]
    public void RejectRevisions_RejectsInvalidArguments()
    {
        XmlDocument document = new XmlDocument();
        document.LoadXml("<root />");
        XmlWriter writer = XmlWriter.Create(new StringWriter());

        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRevision.RejectRevisionXml(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRevision.WriteRejectedNode(null!, writer));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRevision.WriteRejectedNode(document.DocumentElement!, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRevision.WriteRejectedElement(null!, writer));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRevision.WriteRejectedElement(document.DocumentElement!, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRevision.IsDeletedTextElement(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRevision.WriteRejectedDeletedTextElement(null!, writer));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRevision.WriteRejectedDeletedTextElement(document.DocumentElement!, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRevision.HasRejectedPropertyChange(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRevision.FindRejectedPropertyElement(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRevision.WriteRejectedPropertyElement(null!, writer));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRevision.WriteRejectedPropertyElement(document.DocumentElement!, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRevision.ShouldKeepRejectedRevisionChildren(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRevision.IsFormattingChangeLocalName(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRevisions.RejectRevisionXml(null!));
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
