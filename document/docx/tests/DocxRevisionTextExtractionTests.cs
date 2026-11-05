using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxRevisionTextExtractionTests
{
    [Test]
    public void ParagraphGetText_DefaultIncludesInsertedAndMoveToButExcludesDeletedAndMoveFrom()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t>A</w:t></w:r><w:ins w:id=\"1\"><w:r><w:t>B</w:t></w:r></w:ins><w:del w:id=\"2\"><w:r><w:delText>C</w:delText></w:r></w:del><w:moveFrom w:id=\"3\"><w:r><w:t>D</w:t></w:r></w:moveFrom><w:moveTo w:id=\"4\"><w:r><w:t>E</w:t></w:r></w:moveTo><w:r><w:t>F</w:t></w:r></w:p>");

        Assert.That(paragraph.GetText(), Is.EqualTo("ABEF"));
    }

    [Test]
    public void ParagraphGetText_OptionsCanExcludeInsertedAndIncludeDeleted()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t>A</w:t></w:r><w:ins w:id=\"1\"><w:r><w:t>B</w:t></w:r></w:ins><w:del w:id=\"2\"><w:r><w:delText>C</w:delText></w:r></w:del><w:moveFrom w:id=\"3\"><w:p><w:r><w:t>D</w:t></w:r></w:p></w:moveFrom><w:moveTo w:id=\"4\"><w:p><w:r><w:t>E</w:t></w:r></w:p></w:moveTo><w:r><w:t>F</w:t></w:r></w:p>");
        global::docx.DocxTextOptions options = new global::docx.DocxTextOptions();

        options.IncludeInsertedText = false;
        options.IncludeDeletedText = true;

        Assert.That(paragraph.GetTextWithOptions(options), Is.EqualTo("ACDF"));
    }

    [Test]
    public void RunGetText_AppliesRevisionOptionsInChildOrder()
    {
        global::docx.DocxRun run = ParseRun("<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:t>A</w:t><w:ins w:id=\"1\"><w:r><w:t>B</w:t></w:r></w:ins><w:del w:id=\"2\"><w:r><w:delText>C</w:delText></w:r></w:del><w:t>D</w:t></w:r>");
        global::docx.DocxTextOptions options = new global::docx.DocxTextOptions();

        options.IncludeInsertedText = false;
        options.IncludeDeletedText = true;

        Assert.That(run.GetText(), Is.EqualTo("ABD"));
        Assert.That(run.GetTextWithOptions(options), Is.EqualTo("ACD"));
    }

    [Test]
    public void TableGetText_PropagatesRevisionOptionsToParagraphs()
    {
        global::docx.DocxTable table = ParseTable("<w:tbl xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tr><w:tc><w:p><w:r><w:t>A</w:t></w:r><w:ins w:id=\"1\"><w:r><w:t>B</w:t></w:r></w:ins><w:del w:id=\"2\"><w:r><w:delText>C</w:delText></w:r></w:del></w:p></w:tc></w:tr></w:tbl>");
        global::docx.DocxTextOptions options = new global::docx.DocxTextOptions();

        options.IncludeInsertedText = false;
        options.IncludeDeletedText = true;

        Assert.That(table.GetText(), Is.EqualTo("AB"));
        Assert.That(table.GetTextWithOptions(options), Is.EqualTo("AC"));
    }

    [Test]
    public void RevisionGetText_IgnoresFormattingChanges()
    {
        global::docx.DocxRevision revision = global::docx.DocxRevision.ParseXml("<w:rPrChange xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:id=\"1\"><w:rPr><w:b /></w:rPr></w:rPrChange>");

        Assert.That(revision.GetText(), Is.EqualTo(""));
    }

    [Test]
    public void RevisionGetText_ReadsParagraphAndTableChildrenWithOptions()
    {
        global::docx.DocxRevision revision = global::docx.DocxRevision.ParseXml("<w:ins xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:id=\"1\"><w:p><w:r><w:t>Intro</w:t></w:r></w:p><w:tbl><w:tr><w:tc><w:p><w:r><w:t>A</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t>B</w:t></w:r></w:p></w:tc></w:tr></w:tbl></w:ins>");
        global::docx.DocxTextOptions options = new global::docx.DocxTextOptions();

        options.ParagraphSeparator = "|";
        options.TableCellSeparator = ",";

        Assert.That(revision.BlockCount(), Is.EqualTo(2));
        Assert.That(revision.GetTextWithOptions(options), Is.EqualTo("Intro|A,B"));

        options.IncludeInsertedText = false;

        Assert.That(revision.GetTextWithOptions(options), Is.EqualTo(""));
    }

    [Test]
    public void RevisionAwareTextExtraction_RejectsInvalidArguments()
    {
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");
        global::docx.DocxRun run = new global::docx.DocxRun("");
        global::docx.DocxHyperlink hyperlink = new global::docx.DocxHyperlink("");
        global::docx.DocxField field = new global::docx.DocxField("");
        global::docx.DocxBlock block = global::docx.DocxBlock.FromParagraph(paragraph);
        global::docx.DocxRevision revision = global::docx.DocxRevision.ParseXml("<w:ins xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />");

        Assert.Throws<ArgumentNullException>(() => paragraph.GetTextWithOptions(null!));
        Assert.Throws<ArgumentNullException>(() => run.GetTextWithOptions(null!));
        Assert.Throws<ArgumentNullException>(() => hyperlink.GetTextWithOptions(null!));
        Assert.Throws<ArgumentNullException>(() => field.GetTextWithOptions(null!));
        Assert.Throws<ArgumentNullException>(() => block.GetTextWithOptions(null!));
        Assert.Throws<ArgumentNullException>(() => revision.GetTextWithOptions(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRun.IsTextExtractionElement(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRun.ReadTextExtractionElement(null!));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxRun.ReadTextExtractionElement(new global::docx.DocxXmlBuffer("<root />")));
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
}
