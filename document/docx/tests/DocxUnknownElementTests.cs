using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxUnknownElementTests
{
    [Test]
    public void Parse_ReadsElementIdentityAndXml()
    {
        global::docx.DocxUnknownElement element = global::docx.DocxUnknownElement.ParseXml("<x:custom xmlns:x=\"urn:x\" x:data=\"1\"><x:child>value</x:child></x:custom>");

        Assert.That(element.Prefix, Is.EqualTo("x"));
        Assert.That(element.LocalName, Is.EqualTo("custom"));
        Assert.That(element.NamespaceUri, Is.EqualTo("urn:x"));
        Assert.That(element.IsElement("custom", "urn:x"), Is.True);
        Assert.That(element.GetXml(), Does.Contain("<x:child>value</x:child>"));
    }

    [Test]
    public void ParagraphParsing_ExposesOnlyDirectUnknownChildren()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" xmlns:x=\"urn:x\"><w:pPr><w:pStyle w:val=\"Body\" /></w:pPr><w:r><w:t>Text</w:t><x:nested /></w:r><w:hyperlink w:anchor=\"a\" /><w:bookmarkStart w:id=\"1\" w:name=\"Mark\" /><w:bookmarkEnd w:id=\"1\" /><w:fldSimple w:instr=\"PAGE\" /><x:custom x:data=\"1\"><x:child /></x:custom><w:smartTag><x:nested /></w:smartTag></w:p>");

        Assert.That(paragraph.ChildCount(), Is.EqualTo(7));
        Assert.That(paragraph.RunCount(), Is.EqualTo(1));
        Assert.That(paragraph.HyperlinkCount(), Is.EqualTo(1));
        Assert.That(paragraph.BookmarkCount(), Is.EqualTo(2));
        Assert.That(paragraph.FieldCount(), Is.EqualTo(1));
        Assert.That(paragraph.UnknownElementCount(), Is.EqualTo(2));
        Assert.That(paragraph.GetUnknownElement(0).IsElement("custom", "urn:x"), Is.True);
        Assert.That(paragraph.GetUnknownElement(1).IsElement("smartTag", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);
        Assert.That(paragraph.CountUnknownElements("custom", "urn:x"), Is.EqualTo(1));
    }

    [Test]
    public void ParagraphWriting_PreservesUnknownContent()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" xmlns:x=\"urn:x\"><x:custom x:data=\"1\"><x:child>value</x:child></x:custom><w:r><w:t>Text</w:t></w:r></w:p>");

        string xml = paragraph.WriteXml();
        global::docx.DocxParagraph written = ParseParagraph(xml);

        Assert.That(xml, Does.Contain("x:data=\"1\""));
        Assert.That(xml, Does.Contain("<x:child>value</x:child>"));
        Assert.That(written.UnknownElementCount(), Is.EqualTo(1));
        Assert.That(written.GetUnknownElement(0).LocalName, Is.EqualTo("custom"));
    }

    [Test]
    public void ParagraphWriting_WritesAddedUnknownElement()
    {
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");
        global::docx.DocxUnknownElement element = global::docx.DocxUnknownElement.ParseXml("<x:custom xmlns:x=\"urn:x\"><x:child /></x:custom>");

        paragraph.AddUnknownElement(element);

        global::docx.DocxParagraph written = ParseParagraph(paragraph.WriteXml());

        Assert.That(written.ChildCount(), Is.EqualTo(1));
        Assert.That(written.UnknownElementCount(), Is.EqualTo(1));
        Assert.That(written.GetUnknownElement(0).IsElement("custom", "urn:x"), Is.True);
    }

    [Test]
    public void RunParsing_ExposesOnlyDirectUnknownChildren()
    {
        global::docx.DocxRun run = ParseRun("<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" xmlns:x=\"urn:x\"><w:rPr><w:b /></w:rPr><w:t>Text</w:t><w:tab /><w:fldChar w:fldCharType=\"begin\" /><x:custom x:data=\"1\"><x:child /></x:custom><w:drawing><x:nested /></w:drawing></w:r>");

        Assert.That(run.ChildCount(), Is.EqualTo(5));
        Assert.That(run.TextCount(), Is.EqualTo(1));
        Assert.That(run.BreakCount(), Is.EqualTo(1));
        Assert.That(run.ComplexFieldElementCount(), Is.EqualTo(1));
        Assert.That(run.UnknownElementCount(), Is.EqualTo(2));
        Assert.That(run.GetUnknownElement(0).IsElement("custom", "urn:x"), Is.True);
        Assert.That(run.GetUnknownElement(1).IsElement("drawing", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);
        Assert.That(run.CountUnknownElements("custom", "urn:x"), Is.EqualTo(1));
    }

    [Test]
    public void RunWriting_PreservesUnknownContent()
    {
        global::docx.DocxRun run = ParseRun("<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" xmlns:x=\"urn:x\"><x:custom x:data=\"1\"><x:child>value</x:child></x:custom><w:t>Text</w:t></w:r>");

        string xml = run.WriteXml();
        global::docx.DocxRun written = ParseRun(xml);

        Assert.That(xml, Does.Contain("x:data=\"1\""));
        Assert.That(xml, Does.Contain("<x:child>value</x:child>"));
        Assert.That(written.UnknownElementCount(), Is.EqualTo(1));
        Assert.That(written.GetUnknownElement(0).LocalName, Is.EqualTo("custom"));
    }

    [Test]
    public void RunWriting_WritesAddedUnknownElement()
    {
        global::docx.DocxRun run = new global::docx.DocxRun("");
        global::docx.DocxUnknownElement element = global::docx.DocxUnknownElement.ParseXml("<x:custom xmlns:x=\"urn:x\"><x:child /></x:custom>");

        run.AddUnknownElement(element);

        global::docx.DocxRun written = ParseRun(run.WriteXml());

        Assert.That(written.ChildCount(), Is.EqualTo(1));
        Assert.That(written.UnknownElementCount(), Is.EqualTo(1));
        Assert.That(written.GetUnknownElement(0).IsElement("custom", "urn:x"), Is.True);
    }

    [Test]
    public void BodyWriting_PreservesUnknownParagraphAndRunMarkup()
    {
        global::docx.DocxBody body = new global::docx.DocxBody(CreateBodyWithUnknownMarkupXml());

        string xml = body.WriteXml();
        global::docx.DocxBody roundTripped = new global::docx.DocxBody(xml);
        global::docx.DocxParagraph paragraph = roundTripped.GetBlock(0).Paragraph!;
        global::docx.DocxRun run = paragraph.GetRun(0);

        Assert.That(xml, Does.Contain("x:pData=\"paragraph\""));
        Assert.That(xml, Does.Contain("x:rCustom"));
        Assert.That(xml, Does.Contain("x:rData=\"run\""));
        Assert.That(paragraph.UnknownElementCount(), Is.EqualTo(1));
        Assert.That(paragraph.GetUnknownElement(0).IsElement("pCustom", "urn:x"), Is.True);
        Assert.That(run.UnknownElementCount(), Is.EqualTo(1));
        Assert.That(run.GetUnknownElement(0).IsElement("rCustom", "urn:x"), Is.True);
        Assert.That(roundTripped.GetText(), Is.EqualTo("Text"));
    }

    [Test]
    public void UnknownElement_RejectsInvalidArguments()
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create("<root />");
        global::docx.DocxUnknownElement element = global::docx.DocxUnknownElement.ParseXml("<x:custom xmlns:x=\"urn:x\" />");
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");
        global::docx.DocxRun run = new global::docx.DocxRun("");

        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxUnknownElement(null!, "x", "custom", "urn:x"));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxUnknownElement("", null!, "custom", "urn:x"));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxUnknownElement("", "x", null!, "urn:x"));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxUnknownElement("", "x", "custom", null!));
        Assert.Throws<ArgumentException>(() => new global::docx.DocxUnknownElement("", "x", "", "urn:x"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxUnknownElement.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxUnknownElement.ParseXml(null!));
        Assert.Throws<ArgumentNullException>(() => element.IsElement(null!, "urn:x"));
        Assert.Throws<ArgumentNullException>(() => element.IsElement("custom", null!));
        Assert.Throws<ArgumentException>(() => element.IsElement("", "urn:x"));
        Assert.Throws<ArgumentNullException>(() => element.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.AddUnknownElement(null!));
        Assert.Throws<ArgumentNullException>(() => run.AddUnknownElement(null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.CountUnknownElements(null!, "urn:x"));
        Assert.Throws<ArgumentNullException>(() => paragraph.CountUnknownElements("custom", null!));
        Assert.Throws<ArgumentException>(() => paragraph.CountUnknownElements("", "urn:x"));
        Assert.Throws<ArgumentNullException>(() => run.CountUnknownElements(null!, "urn:x"));
        Assert.Throws<ArgumentNullException>(() => run.CountUnknownElements("custom", null!));
        Assert.Throws<ArgumentException>(() => run.CountUnknownElements("", "urn:x"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxParagraph.DocxUnknownElementFromBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRun.DocxUnknownElementFromBuffer(null!));

        Assert.Throws<InvalidOperationException>(() => global::docx.DocxUnknownElement.Parse(reader));
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

    static string CreateBodyWithUnknownMarkupXml()
    {
        return string.Join("", new string[]
        {
            "<w:body xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" xmlns:x=\"urn:x\">",
            "<w:p>",
            "<x:pCustom x:pData=\"paragraph\"><x:child>value</x:child></x:pCustom>",
            "<w:r><x:rCustom x:rData=\"run\" /><w:t>Text</w:t></w:r>",
            "</w:p>",
            "</w:body>"
        });
    }
}
