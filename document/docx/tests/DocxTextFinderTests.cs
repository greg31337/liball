using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxTextFinderTests
{
    [Test]
    public void FindParagraph_FindsTextAcrossAdjacentRuns()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t>Hel</w:t></w:r><w:r><w:t>lo </w:t></w:r><w:r><w:t>wor</w:t></w:r><w:r><w:t>ld</w:t></w:r></w:p>");

        List<global::docx.DocxTextMatch> matches = paragraph.FindText("lo wor");

        Assert.That(matches.Count, Is.EqualTo(1));
        Assert.That(global::docx.DocxTextFinder.Contains(paragraph, "world"), Is.True);
        Assert.That(matches[0].Paragraph, Is.SameAs(paragraph));
        Assert.That(matches[0].ParagraphIndex, Is.EqualTo(0));
        Assert.That(matches[0].MatchIndex, Is.EqualTo(3));
        Assert.That(matches[0].Length, Is.EqualTo(6));
        Assert.That(matches[0].StartRunIndex, Is.EqualTo(1));
        Assert.That(matches[0].StartRunOffset, Is.EqualTo(0));
        Assert.That(matches[0].EndRunIndex, Is.EqualTo(2));
        Assert.That(matches[0].EndRunOffset, Is.EqualTo(2));
        Assert.That(matches[0].Text, Is.EqualTo("lo wor"));
        Assert.That(matches[0].StartRun.GetText(), Is.EqualTo("lo "));
        Assert.That(matches[0].EndRun.GetText(), Is.EqualTo("wor"));
    }

    [Test]
    public void FindDocument_SearchesParagraphsInBodyTableAndContentControlOrder()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(CreatePackageWithBodyChildren(new string[]
        {
            CreateParagraphXml("Alpha"),
            "<w:tbl><w:tr><w:tc>" + CreateParagraphXml("table target") + "</w:tc></w:tr></w:tbl>",
            CreateContentControlXml("control target"),
            CreateParagraphXml("target last")
        }));

        List<global::docx.DocxTextMatch> matches = document.FindText("target");

        Assert.That(matches.Count, Is.EqualTo(3));
        Assert.That(document.Body.FindText("target").Count, Is.EqualTo(3));
        Assert.That(global::docx.DocxTextFinder.Find(document, "target").Count, Is.EqualTo(3));
        Assert.That(global::docx.DocxTextFinder.Contains(document.Body, "table target"), Is.True);
        Assert.That(matches[0].ParagraphIndex, Is.EqualTo(1));
        Assert.That(matches[0].MatchIndex, Is.EqualTo(6));
        Assert.That(matches[1].ParagraphIndex, Is.EqualTo(2));
        Assert.That(matches[1].MatchIndex, Is.EqualTo(8));
        Assert.That(matches[2].ParagraphIndex, Is.EqualTo(3));
        Assert.That(matches[2].MatchIndex, Is.EqualTo(0));
    }

    [Test]
    public void FindDocument_SearchesNestedTablesInCellOrder()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(CreatePackageWithBodyChildren(new string[]
        {
            CreateParagraphXml("Before target"),
            "<w:tbl><w:tr><w:tc>" + CreateParagraphXml("Outer target") + "<w:tbl><w:tr><w:tc>" + CreateParagraphXml("Nested target") + "</w:tc></w:tr></w:tbl></w:tc><w:tc>" + CreateParagraphXml("Second cell target") + "</w:tc></w:tr></w:tbl>",
            CreateParagraphXml("After")
        }));

        List<global::docx.DocxTextMatch> matches = document.FindText("target");

        Assert.That(matches.Count, Is.EqualTo(4));
        Assert.That(matches[0].Paragraph.GetText(), Is.EqualTo("Before target"));
        Assert.That(matches[1].Paragraph.GetText(), Is.EqualTo("Outer target"));
        Assert.That(matches[2].Paragraph.GetText(), Is.EqualTo("Nested target"));
        Assert.That(matches[3].Paragraph.GetText(), Is.EqualTo("Second cell target"));
        Assert.That(matches[3].ParagraphIndex, Is.EqualTo(3));
    }

    [Test]
    public void FindParagraph_RespectsFieldResultOption()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:fldSimple w:instr=\"MERGEFIELD Name\"><w:r><w:t>field target</w:t></w:r></w:fldSimple><w:r><w:t> visible target</w:t></w:r></w:p>");
        global::docx.DocxTextOptions options = new global::docx.DocxTextOptions();

        Assert.That(paragraph.FindText("target").Count, Is.EqualTo(2));

        options.IncludeFieldResults = false;

        List<global::docx.DocxTextMatch> matches = global::docx.DocxTextFinder.Find(paragraph, "target", options);

        Assert.That(matches.Count, Is.EqualTo(1));
        Assert.That(matches[0].MatchIndex, Is.EqualTo(9));
        Assert.That(matches[0].StartRun.GetText(), Is.EqualTo(" visible target"));
    }

    [Test]
    public void Find_RejectsInvalidArguments()
    {
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        global::docx.DocxBody body = global::docx.DocxBody.Create();
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");
        global::docx.DocxTextOptions options = new global::docx.DocxTextOptions();
        List<global::docx.DocxTextMatch> matches = new List<global::docx.DocxTextMatch>();
        List<global::docx.DocxRun> runs = new List<global::docx.DocxRun>();
        List<string> runTexts = new List<string>();
        List<int> runStartIndexes = new List<int>();
        global::docx.DocxRun run = new global::docx.DocxRun("");

        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextFinder.Find((global::docx.DocxDocument)null!, "x"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextFinder.Find(document, "x", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextFinder.Find((global::docx.DocxBody)null!, "x"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextFinder.Find(body, "x", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextFinder.Find((global::docx.DocxParagraph)null!, "x"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextFinder.Find(paragraph, "x", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextFinder.Find(paragraph, null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxTextFinder.Find(paragraph, ""));
        Assert.Throws<ArgumentNullException>(() => document.FindText(null!));
        Assert.Throws<ArgumentNullException>(() => body.FindText(null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.FindText(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextFinder.AddParagraphMatches(null!, paragraph, 0, "x", options));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextFinder.AddParagraphMatches(matches, null!, 0, "x", options));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextFinder.AddParagraphMatches(matches, paragraph, 0, null!, options));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextFinder.AddParagraphMatches(matches, paragraph, 0, "x", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextFinder.BuildParagraphSearchText(null!, options, runs, runTexts, runStartIndexes));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextFinder.BuildParagraphSearchText(paragraph, null!, runs, runTexts, runStartIndexes));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextFinder.BuildParagraphSearchText(paragraph, options, null!, runTexts, runStartIndexes));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextFinder.BuildParagraphSearchText(paragraph, options, runs, null!, runStartIndexes));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextFinder.BuildParagraphSearchText(paragraph, options, runs, runTexts, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextFinder.AddRunListText(runs, runTexts, runStartIndexes, null!, options, 0));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextFinder.AddRunText(null!, runTexts, runStartIndexes, run, options, 0));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextFinder.AddRunText(runs, null!, runStartIndexes, run, options, 0));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextFinder.AddRunText(runs, runTexts, null!, run, options, 0));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextFinder.AddRunText(runs, runTexts, runStartIndexes, null!, options, 0));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextFinder.AddRunText(runs, runTexts, runStartIndexes, run, null!, 0));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextFinder.FindRunIndex(null!, runTexts, 0));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextFinder.FindRunIndex(runStartIndexes, null!, 0));
        Assert.That(global::docx.DocxTextFinder.FindRunIndex(new List<int> { int.MaxValue }, new List<string> { "text" }, int.MaxValue), Is.EqualTo(0));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextFinder.GetParagraphs(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextFinder.AddBlockParagraphs(null!, global::docx.DocxBlock.FromParagraph(paragraph)));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextFinder.AddBlockParagraphs(new List<global::docx.DocxParagraph>(), null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextFinder.AddTableParagraphs(null!, new global::docx.DocxTable("")));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextFinder.AddTableParagraphs(new List<global::docx.DocxParagraph>(), null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextFinder.AddContentControlParagraphs(null!, new global::docx.DocxContentControl("")));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextFinder.AddContentControlParagraphs(new List<global::docx.DocxParagraph>(), null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextFinder.ValidateSearchText(null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxTextFinder.ValidateSearchText(""));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxTextMatch(null!, 0, 0, 1, 0, 0, 0, 0, "x", run, run));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxTextMatch(paragraph, 0, 0, 1, 0, 0, 0, 0, null!, run, run));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxTextMatch(paragraph, 0, 0, 1, 0, 0, 0, 0, "x", null!, run));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxTextMatch(paragraph, 0, 0, 1, 0, 0, 0, 0, "x", run, null!));
    }

    static global::docx.DocxPackage CreatePackageWithBodyChildren(string[] children)
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxRelationshipSet rootRelationships = global::docx.DocxPackage.CreateRootRelationshipSet("/word/document.xml");

        package.ContentTypes.SetDefault("xml", global::docx.DocxConstants.ContentTypeXml);
        package.AddXmlPartWithContent("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, CreateDocumentXml(children));
        package.AddXmlPartWithContent("_rels/.rels", global::docx.DocxConstants.ContentTypeRelationships, rootRelationships.WriteXml());

        return package;
    }

    static string CreateDocumentXml(string[] bodyChildren)
    {
        return "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body>" + string.Join("", bodyChildren) + "</w:body></w:document>";
    }

    static string CreateParagraphXml(string text)
    {
        return "<w:p><w:r><w:t>" + text + "</w:t></w:r></w:p>";
    }

    static string CreateContentControlXml(string text)
    {
        return "<w:sdt><w:sdtContent>" + CreateParagraphXml(text) + "</w:sdtContent></w:sdt>";
    }

    static global::docx.DocxParagraph ParseParagraph(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("p", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxParagraph.Parse(reader);
    }
}
