using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxBodyPublicApiTests
{
    [Test]
    public void Navigation_ReturnsOrderedTypedSnapshots()
    {
        global::docx.DocxBody body = new global::docx.DocxBody(CreateBodyXml(new string[]
        {
            CreateParagraphXml("First"),
            CreateTableXml("Cell"),
            CreateContentControlXml("Controlled"),
            CreateParagraphXml("Last")
        }));

        List<global::docx.DocxBlock> blocks = body.GetBlocks();
        List<global::docx.DocxParagraph> paragraphs = body.GetParagraphs();
        List<global::docx.DocxTable> tables = body.GetTables();
        List<global::docx.DocxContentControl> controls = body.GetContentControls();

        Assert.That(blocks.Count, Is.EqualTo(4));
        Assert.That(blocks[0].IsParagraph(), Is.True);
        Assert.That(blocks[0].GetParagraph(), Is.SameAs(paragraphs[0]));
        Assert.That(blocks[0].GetTable(), Is.Null);
        Assert.That(blocks[1].IsTable(), Is.True);
        Assert.That(blocks[1].GetTable(), Is.SameAs(tables[0]));
        Assert.That(blocks[2].IsContentControl(), Is.True);
        Assert.That(blocks[2].GetContentControl(), Is.SameAs(controls[0]));
        Assert.That(body.GetParagraph(0).GetText(), Is.EqualTo("First"));
        Assert.That(body.GetParagraph(1).GetText(), Is.EqualTo("Last"));
        Assert.That(body.GetTable(0).GetText(), Is.EqualTo("Cell"));
        Assert.That(body.GetContentControl(0).GetText(), Is.EqualTo("Controlled"));

        blocks.Clear();
        paragraphs.Clear();
        tables.Clear();
        controls.Clear();

        Assert.That(body.BlockCount(), Is.EqualTo(4));
        Assert.That(body.ParagraphCount(), Is.EqualTo(2));
        Assert.That(body.TableCount(), Is.EqualTo(1));
        Assert.That(body.ContentControlCount(), Is.EqualTo(1));
    }

    [Test]
    public void Navigation_IncludesPreservedBlockLevelMarkupInDocumentOrder()
    {
        global::docx.DocxBody body = new global::docx.DocxBody(
            "<w:body xmlns:w=\"" + global::docx.DocxConstants.NamespaceWordprocessingMl
            + "\" xmlns:mc=\"" + global::docx.DocxConstants.NamespaceMarkupCompatibility
            + "\" xmlns:vendor=\"urn:vendor:navigation\">"
            + CreateParagraphXml("Before")
            + "<mc:AlternateContent><mc:Fallback><vendor:fallback /></mc:Fallback></mc:AlternateContent>"
            + "<vendor:unknown vendor:value=\"keep\" />"
            + CreateTableXml("Cell")
            + "<w:sectPr />"
            + "</w:body>");

        List<global::docx.DocxBlock> blocks = body.GetBlocks();

        Assert.That(blocks, Has.Count.EqualTo(4));
        Assert.That(blocks[0].IsParagraph(), Is.True);
        Assert.That(blocks[0].IsPreserved(), Is.False);
        Assert.That(blocks[0].GetPreservedXml(), Is.EqualTo(""));
        Assert.That(blocks[1].IsPreserved(), Is.True);
        Assert.That(blocks[1].GetPreservedXml(), Does.Contain("AlternateContent"));
        Assert.That(blocks[2].IsPreserved(), Is.True);
        Assert.That(blocks[2].GetPreservedXml(), Does.Contain("vendor:value=\"keep\""));
        Assert.That(blocks[3].IsTable(), Is.True);
        Assert.That(body.ParagraphCount(), Is.EqualTo(1));
        Assert.That(body.TableCount(), Is.EqualTo(1));
        Assert.That(body.GetText(), Is.EqualTo("Before\nCell"));

        global::docx.DocxBody reparsed = new global::docx.DocxBody(body.WriteXml());

        Assert.That(reparsed.BlockCount(), Is.EqualTo(4));
        Assert.That(reparsed.GetBlock(1).IsPreserved(), Is.True);
        Assert.That(reparsed.GetBlock(2).IsPreserved(), Is.True);
        Assert.That(reparsed.HasFinalSection(), Is.True);
    }

    [Test]
    public void Editing_InsertsReplacesMovesAndDeletesTypedBlocks()
    {
        global::docx.DocxBody body = new global::docx.DocxBody(CreateBodyXml(new string[0]));
        global::docx.DocxParagraph first = new global::docx.DocxParagraph(CreateParagraphXml("First"));
        global::docx.DocxTable table = new global::docx.DocxTable(CreateTableXml("Table"));
        global::docx.DocxContentControl control = new global::docx.DocxContentControl(CreateContentControlXml("Control"));

        body.InsertParagraph(0, first);
        body.InsertTable(1, table);
        body.InsertContentControl(2, control);

        global::docx.DocxTable replacementTable = new global::docx.DocxTable(CreateTableXml("Replacement table"));
        global::docx.DocxContentControl replacementControl = new global::docx.DocxContentControl(CreateContentControlXml("Replacement control"));
        global::docx.DocxParagraph replacementParagraph = new global::docx.DocxParagraph(CreateParagraphXml("Replacement paragraph"));

        Assert.That(body.ReplaceBlockWithTable(0, replacementTable).GetParagraph(), Is.SameAs(first));
        Assert.That(body.ReplaceBlockWithContentControl(1, replacementControl).GetTable(), Is.SameAs(table));
        Assert.That(body.ReplaceBlockWithParagraph(2, replacementParagraph).GetContentControl(), Is.SameAs(control));

        body.MoveBlock(2, 0);
        global::docx.DocxBlock removed = body.RemoveBlockAt(1);

        Assert.That(removed.GetTable(), Is.SameAs(replacementTable));
        Assert.That(body.BlockCount(), Is.EqualTo(2));
        Assert.That(body.GetBlock(0).GetParagraph(), Is.SameAs(replacementParagraph));
        Assert.That(body.GetBlock(1).GetContentControl(), Is.SameAs(replacementControl));

        global::docx.DocxBody reparsed = new global::docx.DocxBody(body.WriteXml());

        Assert.That(reparsed.BlockCount(), Is.EqualTo(2));
        Assert.That(reparsed.GetBlock(0).IsParagraph(), Is.True);
        Assert.That(reparsed.GetBlock(1).IsContentControl(), Is.True);
        Assert.That(reparsed.GetText(), Is.EqualTo("Replacement paragraph\nReplacement control"));
    }

    [Test]
    public void GenericBlockEditing_AppendsAndReplacesBlocks()
    {
        global::docx.DocxBody body = new global::docx.DocxBody(CreateBodyXml(new string[0]));
        global::docx.DocxBlock first = new global::docx.DocxBlock("preserved", "<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t>First</w:t></w:r></w:p>");
        global::docx.DocxBlock second = new global::docx.DocxBlock("preserved", "<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t>Second</w:t></w:r></w:p>");

        body.AddBlock(first);
        global::docx.DocxBlock replaced = body.ReplaceBlock(0, second);

        Assert.That(replaced, Is.SameAs(first));
        Assert.That(body.GetBlock(0), Is.SameAs(second));
        Assert.That(body.WriteXml(), Does.Contain("Second"));
        Assert.That(body.WriteXml(), Does.Not.Contain("First"));
    }

    [Test]
    public void FinalSectionEditing_KeepsOrderedSectionViewConsistent()
    {
        global::docx.DocxBody body = new global::docx.DocxBody(CreateBodyXml(new string[]
        {
            "<w:p><w:pPr><w:sectPr><w:type w:val=\"continuous\" /></w:sectPr></w:pPr></w:p>",
            "<w:sectPr><w:type w:val=\"nextPage\" /></w:sectPr>"
        }));
        global::docx.DocxSection replacement = new global::docx.DocxSection("<w:sectPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:type w:val=\"evenPage\" /></w:sectPr>");

        List<global::docx.DocxSection> initialSections = body.GetSections();

        Assert.That(initialSections.Count, Is.EqualTo(2));
        Assert.That(body.GetFinalSection(), Is.SameAs(initialSections[1]));

        initialSections.Clear();
        body.SetFinalSection(replacement);

        Assert.That(body.SectionCount(), Is.EqualTo(2));
        Assert.That(body.GetSection(1), Is.SameAs(replacement));
        Assert.That(body.GetFinalSection(), Is.SameAs(replacement));
        Assert.That(body.RemoveFinalSection(), Is.True);
        Assert.That(body.RemoveFinalSection(), Is.False);
        Assert.That(body.HasFinalSection(), Is.False);
        Assert.That(body.SectionCount(), Is.EqualTo(1));

        body.RemoveBlockAt(0);

        Assert.That(body.SectionCount(), Is.EqualTo(0));
        Assert.That(body.WriteXml(), Does.Not.Contain("sectPr"));
    }

    [Test]
    public void Editing_RejectsNullValuesAndInvalidPositions()
    {
        global::docx.DocxBody body = new global::docx.DocxBody(CreateBodyXml(new string[]
        {
            CreateParagraphXml("Only")
        }));

        Assert.Throws<ArgumentNullException>(() => body.InsertParagraph(0, null!));
        Assert.Throws<ArgumentNullException>(() => body.InsertTable(0, null!));
        Assert.Throws<ArgumentNullException>(() => body.InsertContentControl(0, null!));
        Assert.Throws<ArgumentNullException>(() => body.ReplaceBlock(0, null!));
        Assert.Throws<ArgumentNullException>(() => body.ReplaceBlockWithParagraph(0, null!));
        Assert.Throws<ArgumentNullException>(() => body.ReplaceBlockWithTable(0, null!));
        Assert.Throws<ArgumentNullException>(() => body.ReplaceBlockWithContentControl(0, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => body.MoveBlock(0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => body.MoveBlock(1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => body.RemoveBlockAt(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => body.GetParagraph(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => body.GetTable(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => body.GetContentControl(0));
    }

    static string CreateBodyXml(string[] children)
    {
        return "<w:body xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">" + string.Join("", children) + "</w:body>";
    }

    static string CreateParagraphXml(string text)
    {
        return "<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t>" + text + "</w:t></w:r></w:p>";
    }

    static string CreateTableXml(string text)
    {
        return "<w:tbl xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tr><w:tc>" + CreateParagraphXml(text) + "</w:tc></w:tr></w:tbl>";
    }

    static string CreateContentControlXml(string text)
    {
        return "<w:sdt xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:sdtContent>" + CreateParagraphXml(text) + "</w:sdtContent></w:sdt>";
    }
}
