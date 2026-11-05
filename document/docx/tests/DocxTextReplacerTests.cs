using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxTextReplacerTests
{
    [Test]
    public void ReplaceRun_ReplacesAllOccurrencesWithinTextNode()
    {
        global::docx.DocxRun run = ParseRun("<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:t>Hello target target</w:t></w:r>");

        int count = run.ReplaceText("target", "value");

        Assert.That(count, Is.EqualTo(2));
        Assert.That(run.GetText(), Is.EqualTo("Hello value value"));
        Assert.That(run.GetTextNode(0).GetText(), Is.EqualTo("Hello value value"));
        Assert.That(run.WriteXml(), Does.Contain("Hello value value"));
        Assert.That(run.WriteXml(), Does.Not.Contain("target"));
    }

    [Test]
    public void ReplaceRun_ReplacesMatchAcrossTextNodesInSameRun()
    {
        global::docx.DocxRun run = ParseRun("<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:t>AA{{</w:t><w:t>NAME}}</w:t><w:tab /><w:t>ZZ</w:t></w:r>");

        int count = global::docx.DocxTextReplacer.Replace(run, "{{NAME}}", "Bob");

        Assert.That(count, Is.EqualTo(1));
        Assert.That(run.GetText(), Is.EqualTo("AABob\tZZ"));
        Assert.That(run.GetTextNode(0).GetText(), Is.EqualTo("AABob"));
        Assert.That(run.GetTextNode(1).GetText(), Is.EqualTo(""));
        Assert.That(run.GetChildXml(2), Does.Contain("tab"));
        Assert.That(run.WriteXml(), Does.Contain("AABob"));
        Assert.That(run.WriteXml(), Does.Contain("tab"));
        Assert.That(run.WriteXml(), Does.Not.Contain("NAME"));
    }

    [Test]
    public void ReplaceRun_UpdatesXmlSpacePreservation()
    {
        global::docx.DocxRun run = ParseRun("<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:t>Token</w:t></w:r>");

        Assert.That(run.ReplaceText("Token", " padded "), Is.EqualTo(1));
        Assert.That(run.GetTextNode(0).PreserveSpace, Is.True);
        Assert.That(run.WriteXml(), Does.Contain("xml:space=\"preserve\""));

        Assert.That(run.ReplaceText(" padded ", "Trim"), Is.EqualTo(1));
        Assert.That(run.GetTextNode(0).PreserveSpace, Is.False);
        Assert.That(run.WriteXml(), Does.Not.Contain("xml:space=\"preserve\""));
    }

    [Test]
    public void ReplaceRun_ReturnsZeroWhenSearchTextIsAbsent()
    {
        global::docx.DocxRun run = ParseRun("<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:t>Text</w:t></w:r>");

        Assert.That(run.ReplaceText("missing", "value"), Is.EqualTo(0));
        Assert.That(run.GetText(), Is.EqualTo("Text"));
    }

    [Test]
    public void ReplaceParagraph_ReplacesTextAcrossAdjacentRunsAndPreservesRunFormatting()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:rPr><w:b /></w:rPr><w:t>Hello {{</w:t></w:r><w:r><w:rPr><w:i /></w:rPr><w:t>Name}}</w:t></w:r><w:r><w:t>!</w:t></w:r></w:p>");

        int count = paragraph.ReplaceText("{{Name}}", "Ada");

        Assert.That(count, Is.EqualTo(1));
        Assert.That(paragraph.GetText(), Is.EqualTo("Hello Ada!"));
        Assert.That(paragraph.GetRun(0).GetText(), Is.EqualTo("Hello Ada"));
        Assert.That(paragraph.GetRun(1).GetText(), Is.EqualTo(""));
        Assert.That(paragraph.GetRun(2).GetText(), Is.EqualTo("!"));
        Assert.That(paragraph.GetRun(0).WriteXml(), Does.Contain("w:b"));
        Assert.That(paragraph.GetRun(1).WriteXml(), Does.Contain("w:i"));
        Assert.That(paragraph.WriteXml(), Does.Contain("Hello Ada"));
        Assert.That(paragraph.WriteXml(), Does.Contain("w:b"));
        Assert.That(paragraph.WriteXml(), Does.Contain("w:i"));
        Assert.That(paragraph.WriteXml(), Does.Not.Contain("Name"));
    }

    [Test]
    public void ReplaceParagraph_PreservesSuffixInEndingRun()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t>A{{</w:t></w:r><w:r><w:t>X}}B</w:t></w:r></w:p>");

        int count = global::docx.DocxTextReplacer.Replace(paragraph, "{{X}}", "Y");

        Assert.That(count, Is.EqualTo(1));
        Assert.That(paragraph.GetText(), Is.EqualTo("AYB"));
        Assert.That(paragraph.GetRun(0).GetText(), Is.EqualTo("AY"));
        Assert.That(paragraph.GetRun(1).GetText(), Is.EqualTo("B"));
    }

    [Test]
    public void ReplaceParagraph_ReplacesMultipleCrossRunMatchesFromEnd()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t>A {{</w:t></w:r><w:r><w:t>X}} B {{</w:t></w:r><w:r><w:t>X}} C</w:t></w:r></w:p>");

        int count = paragraph.ReplaceText("{{X}}", "Y");

        Assert.That(count, Is.EqualTo(2));
        Assert.That(paragraph.GetText(), Is.EqualTo("A Y B Y C"));
        Assert.That(paragraph.GetRun(0).GetText(), Is.EqualTo("A Y"));
        Assert.That(paragraph.GetRun(1).GetText(), Is.EqualTo(" B Y"));
        Assert.That(paragraph.GetRun(2).GetText(), Is.EqualTo(" C"));
        Assert.That(paragraph.WriteXml(), Does.Not.Contain("{{X}}"));
    }

    [Test]
    public void ReplaceParagraph_UsesRunReplacementForSingleRunMatch()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t>Before token after</w:t></w:r></w:p>");

        Assert.That(paragraph.ReplaceText("token", "value"), Is.EqualTo(1));
        Assert.That(paragraph.GetText(), Is.EqualTo("Before value after"));
    }

    [Test]
    public void ReplaceRun_RejectsInvalidArguments()
    {
        global::docx.DocxRun run = new global::docx.DocxRun("");
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");
        global::docx.DocxText text = global::docx.DocxText.Create("text");
        List<int> textStartIndexes = new List<int>();
        List<string> runTexts = new List<string>();
        List<int> runStartIndexes = new List<int>();

        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextReplacer.Replace((global::docx.DocxParagraph)null!, "x", "y"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextReplacer.Replace(paragraph, null!, "y"));
        Assert.Throws<ArgumentException>(() => global::docx.DocxTextReplacer.Replace(paragraph, "", "y"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextReplacer.Replace(paragraph, "x", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextReplacer.Replace((global::docx.DocxRun)null!, "x", "y"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextReplacer.Replace(run, null!, "y"));
        Assert.Throws<ArgumentException>(() => global::docx.DocxTextReplacer.Replace(run, "", "y"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextReplacer.Replace(run, "x", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextReplacer.BuildParagraphRunText(null!, runTexts, runStartIndexes));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextReplacer.BuildParagraphRunText(paragraph, null!, runStartIndexes));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextReplacer.BuildParagraphRunText(paragraph, runTexts, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextReplacer.BuildRunText(null!, textStartIndexes));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextReplacer.BuildRunText(run, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextReplacer.FindMatchIndexes(null!, "x"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextReplacer.FindMatchIndexes("text", null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxTextReplacer.FindMatchIndexes("text", ""));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextReplacer.ReplaceParagraphMatch(null!, runTexts, runStartIndexes, 0, 1, "x"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextReplacer.ReplaceParagraphMatch(paragraph, null!, runStartIndexes, 0, 1, "x"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextReplacer.ReplaceParagraphMatch(paragraph, runTexts, null!, 0, 1, "x"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextReplacer.ReplaceParagraphMatch(paragraph, runTexts, runStartIndexes, 0, 1, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => global::docx.DocxTextReplacer.ReplaceParagraphMatch(paragraph, runTexts, runStartIndexes, -1, 1, "x"));
        Assert.Throws<ArgumentException>(() => global::docx.DocxTextReplacer.ReplaceParagraphMatch(paragraph, runTexts, runStartIndexes, 0, 0, "x"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextReplacer.ReplaceRunRange(null!, 0, 1, "x"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextReplacer.ReplaceRunRange(run, 0, 1, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => global::docx.DocxTextReplacer.ReplaceRunRange(run, -1, 1, "x"));
        Assert.Throws<ArgumentException>(() => global::docx.DocxTextReplacer.ReplaceRunRange(run, 0, -1, "x"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextReplacer.ReplaceMatch(null!, textStartIndexes, 0, 1, "x"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextReplacer.ReplaceMatch(run, null!, 0, 1, "x"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextReplacer.ReplaceMatch(run, textStartIndexes, 0, 1, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => global::docx.DocxTextReplacer.ReplaceMatch(run, textStartIndexes, -1, 1, "x"));
        Assert.Throws<ArgumentException>(() => global::docx.DocxTextReplacer.ReplaceMatch(run, textStartIndexes, 0, 0, "x"));
        Assert.DoesNotThrow(() => global::docx.DocxTextReplacer.ReplaceParagraphMatch(paragraph, runTexts, runStartIndexes, int.MaxValue, int.MaxValue, "x"));
        Assert.DoesNotThrow(() => global::docx.DocxTextReplacer.ReplaceMatch(run, textStartIndexes, int.MaxValue, int.MaxValue, "x"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextReplacer.FindTextNodeIndex(null!, textStartIndexes, 0));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextReplacer.FindTextNodeIndex(run, null!, 0));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextReplacer.FindRunIndex(null!, runTexts, 0));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextReplacer.FindRunIndex(runStartIndexes, null!, 0));
        run.AddText(text);
        Assert.That(global::docx.DocxTextReplacer.FindTextNodeIndex(run, new List<int> { int.MaxValue }, int.MaxValue), Is.EqualTo(0));
        Assert.That(global::docx.DocxTextReplacer.FindRunIndex(new List<int> { int.MaxValue }, new List<string> { "text" }, int.MaxValue), Is.EqualTo(0));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextReplacer.SetTextValue(null!, "x"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextReplacer.SetTextValue(text, null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextReplacer.RefreshRunTextChildren(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextReplacer.RefreshParagraphRunChildren(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTextReplacer.ValidateSearchText(null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxTextReplacer.ValidateSearchText(""));
        Assert.Throws<ArgumentNullException>(() => paragraph.ReplaceText(null!, "y"));
        Assert.Throws<ArgumentNullException>(() => paragraph.ReplaceText("x", null!));
        Assert.Throws<ArgumentNullException>(() => run.ReplaceText(null!, "y"));
        Assert.Throws<ArgumentNullException>(() => run.ReplaceText("x", null!));
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
}
