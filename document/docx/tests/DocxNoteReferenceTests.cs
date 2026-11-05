using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxNoteReferenceTests
{
    [Test]
    public void AddFootnoteReference_AppendsReferenceRunToParagraph()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        global::docx.DocxFootnote footnote = document.CreateFootnote("Footnote body");
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");

        global::docx.DocxRun referenceRun = paragraph.AddFootnoteReference(footnote);
        global::docx.DocxParagraph written = ParseParagraph(paragraph.WriteXml());

        Assert.That(referenceRun.UnknownElementCount(), Is.EqualTo(1));
        Assert.That(referenceRun.CountUnknownElements("footnoteReference", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.EqualTo(1));
        Assert.That(paragraph.ChildCount(), Is.EqualTo(1));
        Assert.That(paragraph.RunCount(), Is.EqualTo(1));
        Assert.That(paragraph.RunElementCount(), Is.EqualTo(1));
        Assert.That(written.RunCount(), Is.EqualTo(1));
        Assert.That(written.GetRun(0).CountUnknownElements("footnoteReference", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.EqualTo(1));
        Assert.That(written.WriteXml(), Does.Contain("w:footnoteReference"));
        Assert.That(written.WriteXml(), Does.Contain("w:id=\"1\""));
    }

    [Test]
    public void AddEndnoteReference_AppendsReferenceRunToParagraph()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        global::docx.DocxEndnote endnote = document.CreateEndnote("Endnote body");
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");

        global::docx.DocxRun referenceRun = paragraph.AddEndnoteReference(endnote);
        global::docx.DocxParagraph written = ParseParagraph(paragraph.WriteXml());

        Assert.That(referenceRun.UnknownElementCount(), Is.EqualTo(1));
        Assert.That(referenceRun.CountUnknownElements("endnoteReference", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.EqualTo(1));
        Assert.That(paragraph.ChildCount(), Is.EqualTo(1));
        Assert.That(paragraph.RunCount(), Is.EqualTo(1));
        Assert.That(paragraph.RunElementCount(), Is.EqualTo(1));
        Assert.That(written.RunCount(), Is.EqualTo(1));
        Assert.That(written.GetRun(0).CountUnknownElements("endnoteReference", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.EqualTo(1));
        Assert.That(written.WriteXml(), Does.Contain("w:endnoteReference"));
        Assert.That(written.WriteXml(), Does.Contain("w:id=\"1\""));
    }

    [Test]
    public void NoteReferenceXml_WritesExpectedReferenceElements()
    {
        Assert.That(global::docx.DocxFootnote.CreateReferenceXml("12"), Does.Contain("<w:footnoteReference"));
        Assert.That(global::docx.DocxFootnote.CreateReferenceXml("12"), Does.Contain("w:id=\"12\""));
        Assert.That(global::docx.DocxEndnote.CreateReferenceXml("13"), Does.Contain("<w:endnoteReference"));
        Assert.That(global::docx.DocxEndnote.CreateReferenceXml("13"), Does.Contain("w:id=\"13\""));
    }

    [Test]
    public void NoteReferenceInsertion_RejectsInvalidArguments()
    {
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");
        global::docx.DocxXmlWriter writer = global::docx.DocxXmlWriter.Create();

        Assert.Throws<ArgumentNullException>(() => paragraph.AddFootnoteReference((global::docx.DocxFootnote)null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.AddFootnoteReferenceById((string)null!));
        Assert.Throws<ArgumentException>(() => paragraph.AddFootnoteReferenceById(""));
        Assert.Throws<ArgumentException>(() => paragraph.AddFootnoteReference(new global::docx.DocxFootnote("")));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxFootnote.CreateReferenceXml(null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxFootnote.CreateReferenceXml(""));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxFootnote.CreateReferenceRun(null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxFootnote.CreateReferenceRun(""));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxFootnote.WriteReferenceTo(null!, "1"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxFootnote.WriteReferenceTo(writer, null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxFootnote.WriteReferenceTo(writer, ""));

        Assert.Throws<ArgumentNullException>(() => paragraph.AddEndnoteReference((global::docx.DocxEndnote)null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.AddEndnoteReferenceById((string)null!));
        Assert.Throws<ArgumentException>(() => paragraph.AddEndnoteReferenceById(""));
        Assert.Throws<ArgumentException>(() => paragraph.AddEndnoteReference(new global::docx.DocxEndnote("")));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxEndnote.CreateReferenceXml(null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxEndnote.CreateReferenceXml(""));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxEndnote.CreateReferenceRun(null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxEndnote.CreateReferenceRun(""));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxEndnote.WriteReferenceTo(null!, "1"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxEndnote.WriteReferenceTo(writer, null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxEndnote.WriteReferenceTo(writer, ""));
    }

    static global::docx.DocxParagraph ParseParagraph(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("p", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxParagraph.Parse(reader);
    }
}
