using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxBodyBlockTests
{
    [Test]
    public void Body_ParseRecordsParagraphAndTableBlocksInSourceOrder()
    {
        global::docx.DocxBody body = ParseBody(CreateBodyXml(new string[]
        {
            "<w:p marker=\"first\" />",
            "<w:tbl marker=\"table\"><w:tr><w:tc><w:p marker=\"nested\" /></w:tc></w:tr></w:tbl>",
            "<w:p marker=\"second\" />"
        }));

        Assert.That(body.BlockCount(), Is.EqualTo(3));
        Assert.That(body.ParagraphCount(), Is.EqualTo(2));
        Assert.That(body.TableCount(), Is.EqualTo(1));
        Assert.That(body.GetBlock(0).IsParagraph(), Is.True);
        Assert.That(body.GetBlock(1).IsTable(), Is.True);
        Assert.That(body.GetBlock(2).IsParagraph(), Is.True);
        Assert.That(body.GetBlock(0).GetXml(), Does.Contain("first"));
        Assert.That(body.GetBlock(1).GetXml(), Does.Contain("table"));
        Assert.That(body.GetBlock(2).GetXml(), Does.Contain("second"));
    }

    [Test]
    public void Body_ParseIgnoresUnsupportedDirectChildrenAndNestedParagraphs()
    {
        global::docx.DocxBody body = ParseBody(CreateBodyXml(new string[]
        {
            "<w:sectPr><w:p marker=\"ignored\" /></w:sectPr>",
            "<w:tbl marker=\"table\"><w:tr><w:tc><w:p marker=\"nested\" /></w:tc></w:tr></w:tbl>",
            "<w:p marker=\"kept\" />"
        }));

        Assert.That(body.BlockCount(), Is.EqualTo(2));
        Assert.That(body.ParagraphCount(), Is.EqualTo(1));
        Assert.That(body.TableCount(), Is.EqualTo(1));
        Assert.That(body.GetBlock(0).IsTable(), Is.True);
        Assert.That(body.GetBlock(1).IsParagraph(), Is.True);
        Assert.That(body.GetBlock(1).GetXml(), Does.Contain("kept"));
    }

    [Test]
    public void BodySectionDiscovery_ReadsParagraphBreaksAndFinalBodySection()
    {
        global::docx.DocxBody body = ParseBody(CreateBodyXml(new string[]
        {
            "<w:p marker=\"first\"><w:pPr><w:sectPr><w:type w:val=\"continuous\" /><w:pgSz w:w=\"1000\" w:h=\"2000\" /></w:sectPr></w:pPr><w:r><w:t>One</w:t></w:r></w:p>",
            "<w:tbl marker=\"table\"><w:tr><w:tc><w:p /></w:tc></w:tr></w:tbl>",
            "<w:p marker=\"second\" />",
            "<w:sectPr><w:type w:val=\"nextPage\" /><w:pgMar w:top=\"720\" w:left=\"1440\" /></w:sectPr>"
        }));

        Assert.That(body.BlockCount(), Is.EqualTo(3));
        Assert.That(body.SectionCount(), Is.EqualTo(2));
        Assert.That(body.GetSection(0).Format.SectionType, Is.EqualTo("continuous"));
        Assert.That(body.GetSection(0).Format.PageWidth, Is.EqualTo("1000"));
        Assert.That(body.GetSection(0).Format.PageHeight, Is.EqualTo("2000"));
        Assert.That(body.GetSection(1).Format.SectionType, Is.EqualTo("nextPage"));
        Assert.That(body.GetSection(1).Format.MarginTop, Is.EqualTo("720"));
        Assert.That(body.GetSection(1).Format.MarginLeft, Is.EqualTo("1440"));
    }

    [Test]
    public void ParagraphAndTable_ParseCaptureRawXml()
    {
        global::docx.DocxXmlReader paragraphReader = global::docx.DocxXmlReader.Create("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" marker=\"p\" />");
        global::docx.DocxXmlReader tableReader = global::docx.DocxXmlReader.Create("<w:tbl xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" marker=\"tbl\" />");

        Assert.That(paragraphReader.ReadToElement("p", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);
        Assert.That(tableReader.ReadToElement("tbl", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        global::docx.DocxParagraph paragraph = global::docx.DocxParagraph.Parse(paragraphReader);
        global::docx.DocxTable table = global::docx.DocxTable.Parse(tableReader);

        Assert.That(paragraph.HasXml(), Is.True);
        Assert.That(table.HasXml(), Is.True);
        Assert.That(paragraph.GetXml(), Does.Contain("marker=\"p\""));
        Assert.That(table.GetXml(), Does.Contain("marker=\"tbl\""));
    }

    [Test]
    public void BlockFactories_CreateTypedBlocks()
    {
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />");
        global::docx.DocxTable table = new global::docx.DocxTable("<w:tbl xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />");

        global::docx.DocxBlock paragraphBlock = global::docx.DocxBlock.FromParagraph(paragraph);
        global::docx.DocxBlock tableBlock = global::docx.DocxBlock.FromTable(table);

        Assert.That(paragraphBlock.Kind, Is.EqualTo(global::docx.DocxBlock.KindParagraph));
        Assert.That(paragraphBlock.Paragraph, Is.SameAs(paragraph));
        Assert.That(paragraphBlock.Table, Is.Null);
        Assert.That(tableBlock.Kind, Is.EqualTo(global::docx.DocxBlock.KindTable));
        Assert.That(tableBlock.Table, Is.SameAs(table));
        Assert.That(tableBlock.Paragraph, Is.Null);
    }

    [Test]
    public void BodyTableInsertionHelpers_CreateTypedTableBlocks()
    {
        global::docx.DocxBody body = global::docx.DocxBody.Create();

        global::docx.DocxTable appended = body.CreateAndAddTable(2, 3);
        global::docx.DocxTable inserted = body.CreateAndInsertTable(0, 1, 1);

        Assert.That(body.BlockCount(), Is.EqualTo(2));
        Assert.That(body.TableCount(), Is.EqualTo(2));
        Assert.That(body.GetBlock(0).IsTable(), Is.True);
        Assert.That(body.GetBlock(0).Table, Is.SameAs(inserted));
        Assert.That(body.GetBlock(1).Table, Is.SameAs(appended));
        Assert.That(inserted.RowCount(), Is.EqualTo(1));
        Assert.That(inserted.GetRow(0).CellCount(), Is.EqualTo(1));
        Assert.That(appended.RowCount(), Is.EqualTo(2));
        Assert.That(appended.GetRow(0).CellCount(), Is.EqualTo(3));
        Assert.That(appended.GetRow(1).CellCount(), Is.EqualTo(3));
    }

    [Test]
    public void BodySectionInsertion_WritesParagraphBreakAndFinalSection()
    {
        global::docx.DocxBody body = global::docx.DocxBody.Create();
        global::docx.DocxSection breakSection = new global::docx.DocxSection("");
        global::docx.DocxSection finalSection = new global::docx.DocxSection("");

        breakSection.Format.SetSectionType("continuous");
        breakSection.Format.SetPageSize("1000", "2000", "", "");
        finalSection.Format.SetSectionType("nextPage");
        finalSection.Format.SetPageMargins("720", "", "", "1440", "", "", "");

        global::docx.DocxParagraph paragraph = body.AddSectionBreak(breakSection);
        body.SetFinalSection(finalSection);

        global::docx.DocxBody parsed = ParseBody(body.WriteXml());

        Assert.That(paragraph.HasSection(), Is.True);
        Assert.That(body.SectionCount(), Is.EqualTo(2));
        Assert.That(body.HasFinalSection(), Is.True);
        Assert.That(parsed.BlockCount(), Is.EqualTo(1));
        Assert.That(parsed.SectionCount(), Is.EqualTo(2));
        Assert.That(parsed.GetBlock(0).Paragraph, Is.Not.Null);
        Assert.That(parsed.GetBlock(0).Paragraph!.HasSection(), Is.True);
        Assert.That(parsed.GetSection(0).Format.SectionType, Is.EqualTo("continuous"));
        Assert.That(parsed.GetSection(0).Format.PageWidth, Is.EqualTo("1000"));
        Assert.That(parsed.GetSection(1).Format.SectionType, Is.EqualTo("nextPage"));
        Assert.That(parsed.GetSection(1).Format.MarginTop, Is.EqualTo("720"));
        Assert.That(parsed.GetSection(1).Format.MarginLeft, Is.EqualTo("1440"));
        Assert.That(parsed.HasFinalSection(), Is.True);
    }

    [Test]
    public void BlockParagraphAndTable_RejectInvalidArguments()
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create("<root />");
        global::docx.DocxBody body = global::docx.DocxBody.Create();
        global::docx.DocxBlock block = new global::docx.DocxBlock("raw", "<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />");
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");

        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxBlock(null!, ""));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxBlock("paragraph", null!));
        Assert.Throws<ArgumentException>(() => new global::docx.DocxBlock("", ""));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxBlock.FromParagraph(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxBlock.FromTable(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxParagraph(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxTable(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxParagraph.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTable.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => body.AddBlock(null!));
        Assert.Throws<ArgumentNullException>(() => body.AddParsedBlock(null!));
        Assert.Throws<ArgumentNullException>(() => body.InsertBlock(0, null!));
        Assert.Throws<ArgumentNullException>(() => body.AddSection(null!));
        Assert.Throws<ArgumentNullException>(() => body.AddParsedSection(null!));
        Assert.Throws<ArgumentNullException>(() => body.AddSectionsFromParagraphXml(null!));
        Assert.Throws<ArgumentNullException>(() => body.AddSectionBreak(null!));
        Assert.Throws<ArgumentNullException>(() => body.SetFinalSection(null!));
        Assert.Throws<ArgumentNullException>(() => body.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => body.WriteBlocksTo(null!));
        Assert.Throws<ArgumentNullException>(() => body.WriteFinalSectionTo(null!));
        Assert.Throws<ArgumentNullException>(() => block.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.SetSection(null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.WriteSectionTo(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxParagraph.DocxSectionFromPropertiesXml(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxBody.DocxSectionFromBuffer(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => body.CreateAndAddTable(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => body.CreateAndAddTable(1, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => body.CreateAndInsertTable(0, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => body.CreateAndInsertTable(0, 1, -1));

        Assert.That(reader.ReadToElement("root", ""), Is.True);
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxParagraph.Parse(reader));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxTable.Parse(reader));
    }

    static global::docx.DocxBody ParseBody(string bodyXml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(bodyXml);

        Assert.That(reader.ReadToElement("body", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxBody.Parse(reader);
    }

    static string CreateBodyXml(string[] children)
    {
        return "<w:body xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">" + string.Join("", children) + "</w:body>";
    }
}
