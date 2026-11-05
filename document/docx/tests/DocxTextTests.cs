using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxTextTests
{
    [Test]
    public void Parse_ReadsTextValueAndPreserveSpace()
    {
        global::docx.DocxText text = ParseText("<w:t xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" xml:space=\"preserve\">  padded  </w:t>");

        Assert.That(text.GetText(), Is.EqualTo("  padded  "));
        Assert.That(text.PreserveSpace, Is.True);
        Assert.That(text.HasText(), Is.True);
        Assert.That(text.GetXml(), Does.Contain("xml:space=\"preserve\""));
    }

    [Test]
    public void Parse_ReadsTextWithoutPreserveSpace()
    {
        global::docx.DocxText text = ParseText("<w:t xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">Plain</w:t>");

        Assert.That(text.GetText(), Is.EqualTo("Plain"));
        Assert.That(text.PreserveSpace, Is.False);
        Assert.That(text.HasText(), Is.True);
    }

    [Test]
    public void Parse_ReadsEmptyText()
    {
        global::docx.DocxText text = ParseText("<w:t xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />");

        Assert.That(text.GetText(), Is.EqualTo(""));
        Assert.That(text.PreserveSpace, Is.False);
        Assert.That(text.HasText(), Is.False);
    }

    [Test]
    public void RunParsing_ExposesDirectTextNodesInOrderAndCombinedText()
    {
        global::docx.DocxRun run = ParseRun("<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:t>First</w:t><w:drawing><w:t>Nested</w:t></w:drawing><w:t xml:space=\"preserve\"> Second</w:t></w:r>");

        Assert.That(run.TextElementCount(), Is.EqualTo(2));
        Assert.That(run.TextCount(), Is.EqualTo(2));
        Assert.That(run.GetTextNode(0).GetText(), Is.EqualTo("First"));
        Assert.That(run.GetTextNode(1).GetText(), Is.EqualTo(" Second"));
        Assert.That(run.GetTextNode(1).PreserveSpace, Is.True);
        Assert.That(run.GetText(), Is.EqualTo("First Second"));
    }

    [Test]
    public void WriteXml_WritesTextAndEscapesXmlCharacters()
    {
        global::docx.DocxText text = new global::docx.DocxText("", "Hello <docx> & quotes", false);

        string xml = text.WriteXml();
        global::docx.DocxText parsed = ParseText(xml);

        Assert.That(xml, Does.Contain("&lt;docx&gt;"));
        Assert.That(xml, Does.Contain("&amp;"));
        Assert.That(parsed.GetText(), Is.EqualTo("Hello <docx> & quotes"));
        Assert.That(parsed.PreserveSpace, Is.False);
    }

    [Test]
    public void WriteXml_WritesPreserveSpaceAttributeWhenRequested()
    {
        global::docx.DocxText text = new global::docx.DocxText("", "  padded  ", true);

        string xml = text.WriteXml();
        global::docx.DocxText parsed = ParseText(xml);

        Assert.That(xml, Does.Contain("xml:space=\"preserve\""));
        Assert.That(parsed.GetText(), Is.EqualTo("  padded  "));
        Assert.That(parsed.PreserveSpace, Is.True);
    }

    [Test]
    public void Create_PreservesLeadingAndTrailingSpaces()
    {
        global::docx.DocxText leading = global::docx.DocxText.Create(" leading");
        global::docx.DocxText trailing = global::docx.DocxText.Create("trailing ");
        global::docx.DocxText plain = global::docx.DocxText.Create("plain");

        Assert.That(leading.PreserveSpace, Is.True);
        Assert.That(trailing.PreserveSpace, Is.True);
        Assert.That(plain.PreserveSpace, Is.False);
    }

    [Test]
    public void WriteTo_WritesTextInsideExistingWriter()
    {
        global::docx.DocxText text = new global::docx.DocxText("", "Inside", false);
        global::docx.DocxXmlWriter writer = global::docx.DocxXmlWriter.Create();

        writer.StartElementWithoutPrefix("root", "");
        text.WriteTo(writer);
        writer.EndElement();

        Assert.That(writer.GetXml(), Does.Contain("<w:t"));
        Assert.That(writer.GetXml(), Does.Contain("Inside"));
    }

    [Test]
    public void RunWriting_WritesAddedTextNode()
    {
        global::docx.DocxRun run = new global::docx.DocxRun("");

        run.AddText(global::docx.DocxText.Create("Generated "));
        run.AddText(new global::docx.DocxText("", "text", false));

        global::docx.DocxRun parsed = ParseRun(run.WriteXml());

        Assert.That(parsed.ChildCount(), Is.EqualTo(2));
        Assert.That(parsed.TextCount(), Is.EqualTo(2));
        Assert.That(parsed.GetText(), Is.EqualTo("Generated text"));
        Assert.That(parsed.GetTextNode(0).PreserveSpace, Is.True);
    }

    [Test]
    public void TextParsing_RejectsInvalidArguments()
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create("<root />");
        global::docx.DocxRun run = new global::docx.DocxRun("");
        global::docx.DocxText text = global::docx.DocxText.Create("text");

        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxText(null!, "", false));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxText("", null!, false));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxText.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxText.ParseXml(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxText.Create(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxText.ShouldPreserveSpace(null!));
        Assert.Throws<ArgumentNullException>(() => text.SetText(null!));
        Assert.Throws<ArgumentNullException>(() => text.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => run.AddText(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRun.DocxTextFromBuffer(null!));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxText.ParseXml("<root />"));

        Assert.That(reader.ReadToElement("root", ""), Is.True);
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxText.Parse(reader));
    }

    static global::docx.DocxText ParseText(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("t", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxText.Parse(reader);
    }

    static global::docx.DocxRun ParseRun(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("r", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxRun.Parse(reader);
    }
}
