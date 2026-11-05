using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxRunTests
{
    [Test]
    public void Parse_CapturesRunPropertiesAndDirectChildrenInOrder()
    {
        global::docx.DocxRun run = ParseRun(CreateRunXml(new string[]
        {
            "<w:rPr><w:b /></w:rPr>",
            "<w:t>First</w:t>",
            "<w:tab />",
            "<w:t>Second</w:t>"
        }));

        Assert.That(run.HasProperties(), Is.True);
        Assert.That(run.GetPropertiesXml(), Does.Contain("w:b"));
        Assert.That(run.ChildCount(), Is.EqualTo(3));
        Assert.That(run.GetChildXml(0), Does.Contain("First"));
        Assert.That(run.GetChildXml(1), Does.Contain("tab"));
        Assert.That(run.GetChildXml(2), Does.Contain("Second"));
        Assert.That(run.TextElementCount(), Is.EqualTo(2));
        Assert.That(run.Format.HasBold, Is.True);
    }

    [Test]
    public void Parse_HandlesRunWithoutProperties()
    {
        global::docx.DocxRun run = ParseRun(CreateRunXml(new string[]
        {
            "<w:t>Only</w:t>"
        }));

        Assert.That(run.HasProperties(), Is.False);
        Assert.That(run.GetPropertiesXml(), Is.EqualTo(""));
        Assert.That(run.ChildCount(), Is.EqualTo(1));
        Assert.That(run.TextElementCount(), Is.EqualTo(1));
    }

    [Test]
    public void Parse_CountsOnlyDirectTextChildren()
    {
        global::docx.DocxRun run = ParseRun(CreateRunXml(new string[]
        {
            "<w:drawing><w:t>Nested</w:t></w:drawing>",
            "<w:t>Direct</w:t>"
        }));

        Assert.That(run.ChildCount(), Is.EqualTo(2));
        Assert.That(run.TextElementCount(), Is.EqualTo(1));
        Assert.That(run.GetChildXml(0), Does.Contain("drawing"));
        Assert.That(run.GetChildXml(1), Does.Contain("Direct"));
    }

    [Test]
    public void ParagraphParsing_ExposesParsedDirectRuns()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r marker=\"first\"><w:t>First</w:t></w:r><w:hyperlink><w:r marker=\"nested\"><w:t>Nested</w:t></w:r></w:hyperlink><w:r marker=\"second\"><w:t>Second</w:t></w:r></w:p>");

        Assert.That(paragraph.RunCount(), Is.EqualTo(2));
        Assert.That(paragraph.GetRun(0).GetXml(), Does.Contain("first"));
        Assert.That(paragraph.GetRun(0).TextElementCount(), Is.EqualTo(1));
        Assert.That(paragraph.GetRun(1).GetXml(), Does.Contain("second"));
    }

    [Test]
    public void WriteXml_WritesPropertiesAndChildrenInOrder()
    {
        global::docx.DocxRun run = ParseRun(CreateRunXml(new string[]
        {
            "<w:rPr><w:b /></w:rPr>",
            "<w:t>First</w:t>",
            "<w:tab />",
            "<w:t>Second</w:t>"
        }));

        global::docx.DocxRun written = ParseRun(run.WriteXml());

        Assert.That(written.HasProperties(), Is.True);
        Assert.That(written.GetPropertiesXml(), Does.Contain("w:b"));
        Assert.That(written.ChildCount(), Is.EqualTo(3));
        Assert.That(written.GetChildXml(0), Does.Contain("First"));
        Assert.That(written.GetChildXml(1), Does.Contain("tab"));
        Assert.That(written.GetChildXml(2), Does.Contain("Second"));
        Assert.That(written.TextElementCount(), Is.EqualTo(2));
    }

    [Test]
    public void WriteXml_WritesEmptyRun()
    {
        global::docx.DocxRun run = ParseRun("<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />");

        Assert.That(run.WriteXml(), Is.EqualTo("<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />"));
    }

    [Test]
    public void WriteTo_WritesRunInsideExistingWriter()
    {
        global::docx.DocxRun run = ParseRun(CreateRunXml(new string[]
        {
            "<w:t>Inside</w:t>"
        }));
        global::docx.DocxXmlWriter writer = global::docx.DocxXmlWriter.Create();

        writer.StartElementWithoutPrefix("root", "");
        run.WriteTo(writer);
        writer.EndElement();

        Assert.That(writer.GetXml(), Does.Contain("<w:r"));
        Assert.That(writer.GetXml(), Does.Contain("Inside"));
    }

    [Test]
    public void WriteXml_UsesUpdatedPropertiesAndChildren()
    {
        global::docx.DocxRun run = new global::docx.DocxRun("");

        run.SetPropertiesXml("<w:rPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:i /></w:rPr>");
        run.AddChild(new global::docx.DocxXmlBuffer("<w:t xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">Text</w:t>"));

        global::docx.DocxRun written = ParseRun(run.WriteXml());

        Assert.That(written.HasProperties(), Is.True);
        Assert.That(written.GetPropertiesXml(), Does.Contain("w:i"));
        Assert.That(written.ChildCount(), Is.EqualTo(1));
        Assert.That(written.GetChildXml(0), Does.Contain("Text"));
    }

    [Test]
    public void GetText_AfterMutationUsesCurrentChildOrderAndBreakOptions()
    {
        global::docx.DocxRun run = new global::docx.DocxRun("");
        global::docx.DocxTextOptions options = new global::docx.DocxTextOptions();

        run.AddText(global::docx.DocxText.Create("A"));
        run.AddBreak(global::docx.DocxBreak.CreateTab());
        run.AddText(global::docx.DocxText.Create("B"));
        run.AddBreak(global::docx.DocxBreak.CreateColumnBreak());
        run.AddText(global::docx.DocxText.Create("C"));

        options.TabSeparator = "<tab>";
        options.ColumnBreakSeparator = "<column>";

        global::docx.DocxRun written = ParseRun(run.WriteXml());

        Assert.That(run.GetTextWithOptions(options), Is.EqualTo("A<tab>B<column>C"));
        Assert.That(written.ChildCount(), Is.EqualTo(5));
        Assert.That(written.TextElementCount(), Is.EqualTo(3));
        Assert.That(written.BreakCount(), Is.EqualTo(2));
        Assert.That(written.GetChildXml(1), Does.Contain("tab"));
        Assert.That(written.GetChildXml(3), Does.Contain("column"));
    }

    [Test]
    public void RunParsing_RejectsInvalidArguments()
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create("<root />");
        global::docx.DocxRun run = new global::docx.DocxRun("<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />");
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />");

        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxRun(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRun.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => run.AddChild(null!));
        Assert.Throws<ArgumentNullException>(() => run.AddText(null!));
        Assert.Throws<ArgumentNullException>(() => run.AddBreak(null!));
        Assert.Throws<ArgumentNullException>(() => run.AddUnknownElement(null!));
        Assert.Throws<ArgumentNullException>(() => run.SetPropertiesXml(null!));
        Assert.Throws<ArgumentNullException>(() => run.CountChildElements(null!, ""));
        Assert.Throws<ArgumentNullException>(() => run.CountChildElements("t", null!));
        Assert.Throws<ArgumentException>(() => run.CountChildElements("", ""));
        Assert.Throws<ArgumentNullException>(() => run.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => run.WritePropertiesTo(null!));
        Assert.Throws<ArgumentNullException>(() => run.WriteChildrenTo(null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.AddRun(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxParagraph.DocxRunFromBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRun.DocxUnknownElementFromBuffer(null!));

        Assert.That(reader.ReadToElement("root", ""), Is.True);
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxRun.Parse(reader));
    }

    static global::docx.DocxRun ParseRun(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("r", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxRun.Parse(reader);
    }

    static global::docx.DocxParagraph ParseParagraph(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("p", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxParagraph.Parse(reader);
    }

    static string CreateRunXml(string[] children)
    {
        return "<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">" + string.Join("", children) + "</w:r>";
    }
}
