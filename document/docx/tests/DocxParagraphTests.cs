using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxParagraphTests
{
    [Test]
    public void Parse_CapturesPropertiesAndDirectChildrenInOrder()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph(CreateParagraphXml(new string[]
        {
            "<w:pPr><w:pStyle w:val=\"Heading1\" /></w:pPr>",
            "<w:r marker=\"first\"><w:t>First</w:t></w:r>",
            "<w:hyperlink marker=\"link\"><w:r><w:t>Link</w:t></w:r></w:hyperlink>",
            "<w:r marker=\"second\"><w:t>Second</w:t></w:r>"
        }));

        Assert.That(paragraph.HasProperties(), Is.True);
        Assert.That(paragraph.Format.StyleId, Is.EqualTo("Heading1"));
        Assert.That(paragraph.GetPropertiesXml(), Does.Contain("pStyle"));
        Assert.That(paragraph.ChildCount(), Is.EqualTo(3));
        Assert.That(paragraph.GetChildXml(0), Does.Contain("first"));
        Assert.That(paragraph.GetChildXml(1), Does.Contain("link"));
        Assert.That(paragraph.GetChildXml(2), Does.Contain("second"));
        Assert.That(paragraph.RunElementCount(), Is.EqualTo(2));
        Assert.That(paragraph.RunCount(), Is.EqualTo(2));
        Assert.That(paragraph.CountChildElements("hyperlink", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.EqualTo(1));
        Assert.That(paragraph.HyperlinkElementCount(), Is.EqualTo(1));
        Assert.That(paragraph.HyperlinkCount(), Is.EqualTo(1));
    }

    [Test]
    public void Parse_HandlesParagraphWithoutProperties()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph(CreateParagraphXml(new string[]
        {
            "<w:r><w:t>Only</w:t></w:r>"
        }));

        Assert.That(paragraph.HasProperties(), Is.False);
        Assert.That(paragraph.GetPropertiesXml(), Is.EqualTo(""));
        Assert.That(paragraph.ChildCount(), Is.EqualTo(1));
        Assert.That(paragraph.RunElementCount(), Is.EqualTo(1));
        Assert.That(paragraph.RunCount(), Is.EqualTo(1));
    }

    [Test]
    public void Parse_CountsOnlyDirectChildElements()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph(CreateParagraphXml(new string[]
        {
            "<w:smartTag><w:r marker=\"nested\"><w:t>Nested</w:t></w:r></w:smartTag>",
            "<w:r marker=\"direct\"><w:t>Direct</w:t></w:r>"
        }));

        Assert.That(paragraph.ChildCount(), Is.EqualTo(2));
        Assert.That(paragraph.RunElementCount(), Is.EqualTo(1));
        Assert.That(paragraph.GetChildXml(0), Does.Contain("smartTag"));
        Assert.That(paragraph.GetChildXml(1), Does.Contain("direct"));
    }

    [Test]
    public void BodyParsing_ExposesParsedParagraphBlocks()
    {
        global::docx.DocxBody body = ParseBody("<w:body xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">" + CreateParagraphXml(new string[]
        {
            "<w:pPr><w:pStyle w:val=\"BodyText\" /></w:pPr>",
            "<w:r><w:t>Text</w:t></w:r>"
        }) + "</w:body>");

        global::docx.DocxParagraph? paragraph = body.GetBlock(0).Paragraph;

        Assert.That(paragraph, Is.Not.Null);
        Assert.That(paragraph!.HasProperties(), Is.True);
        Assert.That(paragraph.ChildCount(), Is.EqualTo(1));
        Assert.That(paragraph.RunElementCount(), Is.EqualTo(1));
    }

    [Test]
    public void WriteXml_WritesPropertiesAndChildrenInOrder()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph(CreateParagraphXml(new string[]
        {
            "<w:pPr><w:pStyle w:val=\"Heading1\" /></w:pPr>",
            "<w:r marker=\"first\"><w:t>First</w:t></w:r>",
            "<w:hyperlink marker=\"link\"><w:r><w:t>Link</w:t></w:r></w:hyperlink>",
            "<w:r marker=\"second\"><w:t>Second</w:t></w:r>"
        }));

        global::docx.DocxParagraph written = ParseParagraph(paragraph.WriteXml());

        Assert.That(written.HasProperties(), Is.True);
        Assert.That(written.GetPropertiesXml(), Does.Contain("Heading1"));
        Assert.That(written.ChildCount(), Is.EqualTo(3));
        Assert.That(written.GetChildXml(0), Does.Contain("first"));
        Assert.That(written.GetChildXml(1), Does.Contain("link"));
        Assert.That(written.GetChildXml(2), Does.Contain("second"));
        Assert.That(written.RunElementCount(), Is.EqualTo(2));
    }

    [Test]
    public void WriteXml_WritesEmptyParagraph()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />");

        Assert.That(paragraph.WriteXml(), Is.EqualTo("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />"));
    }

    [Test]
    public void WriteTo_WritesParagraphInsideExistingWriter()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph(CreateParagraphXml(new string[]
        {
            "<w:r><w:t>Inside</w:t></w:r>"
        }));
        global::docx.DocxXmlWriter writer = global::docx.DocxXmlWriter.Create();

        writer.StartElementWithoutPrefix("root", "");
        paragraph.WriteTo(writer);
        writer.EndElement();

        Assert.That(writer.GetXml(), Does.Contain("<w:p"));
        Assert.That(writer.GetXml(), Does.Contain("Inside"));
    }

    [Test]
    public void WriteXml_UsesUpdatedPropertiesAndChildren()
    {
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");

        paragraph.SetPropertiesXml("<w:pPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:pStyle w:val=\"Quote\" /></w:pPr>");
        paragraph.AddChild(new global::docx.DocxXmlBuffer("<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:t>Text</w:t></w:r>"));

        global::docx.DocxParagraph written = ParseParagraph(paragraph.WriteXml());

        Assert.That(written.HasProperties(), Is.True);
        Assert.That(written.GetPropertiesXml(), Does.Contain("Quote"));
        Assert.That(written.ChildCount(), Is.EqualTo(1));
        Assert.That(written.GetChildXml(0), Does.Contain("Text"));
    }

    [Test]
    public void GetText_ComposesRunsHyperlinksFieldsAndBreaksInChildOrder()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph(CreateParagraphXml(new string[]
        {
            "<w:r><w:t>A</w:t><w:tab /><w:t>B</w:t></w:r>",
            "<w:bookmarkStart w:id=\"4\" w:name=\"Place\" />",
            "<w:hyperlink w:anchor=\"Place\"><w:r><w:t>Link</w:t><w:br /><w:t>More</w:t></w:r></w:hyperlink>",
            "<w:bookmarkEnd w:id=\"4\" />",
            "<w:fldSimple w:instr=\"PAGE\"><w:r><w:t>9</w:t></w:r></w:fldSimple>",
            "<w:r><w:br w:type=\"page\" /><w:t>Done</w:t></w:r>"
        }));
        global::docx.DocxTextOptions options = new global::docx.DocxTextOptions();

        options.TabSeparator = "<tab>";
        options.LineBreakSeparator = "<line>";
        options.PageBreakSeparator = "<page>";

        Assert.That(paragraph.ChildCount(), Is.EqualTo(6));
        Assert.That(paragraph.RunCount(), Is.EqualTo(2));
        Assert.That(paragraph.HyperlinkCount(), Is.EqualTo(1));
        Assert.That(paragraph.BookmarkCount(), Is.EqualTo(2));
        Assert.That(paragraph.FieldCount(), Is.EqualTo(1));
        Assert.That(paragraph.GetTextWithOptions(options), Is.EqualTo("A<tab>BLink<line>More9<page>Done"));
    }

    [Test]
    public void ParagraphParsing_RejectsInvalidArguments()
    {
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />");

        Assert.Throws<ArgumentNullException>(() => paragraph.AddChild(null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.AddHyperlink(null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.AddBookmark(null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.AddField(null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.AddUnknownElement(null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.AddInlineImage(null!, 1, 1));
        Assert.Throws<ArgumentNullException>(() => paragraph.SetPropertiesXml(null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.CountChildElements(null!, ""));
        Assert.Throws<ArgumentNullException>(() => paragraph.CountChildElements("r", null!));
        Assert.Throws<ArgumentException>(() => paragraph.CountChildElements("", ""));
        Assert.Throws<ArgumentNullException>(() => paragraph.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.WritePropertiesTo(null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.WriteChildrenTo(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxParagraph.DocxHyperlinkFromBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxParagraph.DocxBookmarkFromBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxParagraph.DocxFieldFromBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxParagraph.DocxUnknownElementFromBuffer(null!));
    }

    static global::docx.DocxParagraph ParseParagraph(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("p", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxParagraph.Parse(reader);
    }

    static global::docx.DocxBody ParseBody(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("body", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxBody.Parse(reader);
    }

    static string CreateParagraphXml(string[] children)
    {
        return "<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">" + string.Join("", children) + "</w:p>";
    }
}
