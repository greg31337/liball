using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxHyperlinkTests
{
    [Test]
    public void Parse_ReadsAttributesChildrenAndDirectRuns()
    {
        global::docx.DocxHyperlink hyperlink = ParseHyperlink("<w:hyperlink xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" r:id=\"rIdLink\" w:anchor=\"bookmark1\" w:history=\"1\" w:tooltip=\"Tip\"><w:r><w:t>Link</w:t></w:r><w:proofErr w:type=\"spellStart\" /><w:r><w:t> Text</w:t></w:r></w:hyperlink>");

        Assert.That(hyperlink.RelationshipId, Is.EqualTo("rIdLink"));
        Assert.That(hyperlink.Anchor, Is.EqualTo("bookmark1"));
        Assert.That(hyperlink.History, Is.EqualTo("1"));
        Assert.That(hyperlink.Tooltip, Is.EqualTo("Tip"));
        Assert.That(hyperlink.ChildCount(), Is.EqualTo(3));
        Assert.That(hyperlink.RunCount(), Is.EqualTo(2));
        Assert.That(hyperlink.GetRun(0).GetText(), Is.EqualTo("Link"));
        Assert.That(hyperlink.GetText(), Is.EqualTo("Link Text"));
        Assert.That(hyperlink.GetChildXml(1), Does.Contain("proofErr"));
    }

    [Test]
    public void ParagraphParsing_ExposesDirectHyperlinksAndKeepsDirectRunsSeparate()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><w:r><w:t>Before</w:t></w:r><w:hyperlink r:id=\"rIdLink\"><w:r><w:t>Link</w:t></w:r></w:hyperlink><w:smartTag><w:hyperlink r:id=\"rIdNested\" /></w:smartTag></w:p>");

        Assert.That(paragraph.ChildCount(), Is.EqualTo(3));
        Assert.That(paragraph.RunElementCount(), Is.EqualTo(1));
        Assert.That(paragraph.RunCount(), Is.EqualTo(1));
        Assert.That(paragraph.HyperlinkElementCount(), Is.EqualTo(1));
        Assert.That(paragraph.HyperlinkCount(), Is.EqualTo(1));
        Assert.That(paragraph.GetHyperlink(0).RelationshipId, Is.EqualTo("rIdLink"));
        Assert.That(paragraph.GetHyperlink(0).GetText(), Is.EqualTo("Link"));
    }

    [Test]
    public void ResolveRelationship_UsesExternalHyperlinkRelationship()
    {
        global::docx.DocxHyperlink hyperlink = ParseHyperlink("<w:hyperlink xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" r:id=\"rIdLink\" />");
        global::docx.DocxRelationshipSet relationships = CreateRelationships();

        Assert.That(hyperlink.ResolveRelationship(relationships), Is.True);
        Assert.That(hyperlink.Relationship, Is.SameAs(relationships.FindById("rIdLink")));
        Assert.That(hyperlink.Target, Is.EqualTo("https://example.com/path"));
        Assert.That(hyperlink.IsExternal, Is.True);
        Assert.That(hyperlink.TargetPartName, Is.EqualTo(""));
    }

    [Test]
    public void ResolveRelationship_ResolvesInternalHyperlinkTargetPartName()
    {
        global::docx.DocxHyperlink hyperlink = ParseHyperlink("<w:hyperlink xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" r:id=\"rIdInternal\" />");
        global::docx.DocxRelationshipSet relationships = CreateRelationships();

        Assert.That(hyperlink.ResolveRelationship(relationships), Is.True);
        Assert.That(hyperlink.Target, Is.EqualTo("../custom/item.xml"));
        Assert.That(hyperlink.IsExternal, Is.False);
        Assert.That(hyperlink.TargetPartName, Is.EqualTo("/custom/item.xml"));
    }

    [Test]
    public void ResolveHyperlinks_ResolvesOnlyPresentHyperlinkRelationships()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><w:hyperlink r:id=\"rIdLink\" /><w:hyperlink r:id=\"rIdImage\" /><w:hyperlink w:anchor=\"local\" /></w:p>");
        global::docx.DocxRelationshipSet relationships = CreateRelationships();

        Assert.That(paragraph.ResolveHyperlinks(relationships), Is.EqualTo(1));
        Assert.That(paragraph.GetHyperlink(0).Target, Is.EqualTo("https://example.com/path"));
        Assert.That(paragraph.GetHyperlink(1).Target, Is.EqualTo(""));
        Assert.That(paragraph.GetHyperlink(2).Target, Is.EqualTo(""));
    }

    [Test]
    public void CreateAnchor_WritesInternalAnchorHyperlink()
    {
        global::docx.DocxHyperlink hyperlink = global::docx.DocxHyperlink.CreateAnchor("bookmark1", "Jump");

        global::docx.DocxHyperlink parsed = ParseHyperlink(hyperlink.WriteXml());

        Assert.That(parsed.RelationshipId, Is.EqualTo(""));
        Assert.That(parsed.Anchor, Is.EqualTo("bookmark1"));
        Assert.That(parsed.RunCount(), Is.EqualTo(1));
        Assert.That(parsed.GetText(), Is.EqualTo("Jump"));
    }

    [Test]
    public void CreateExternal_CreatesRelationshipAndWritesRelationshipId()
    {
        global::docx.DocxRelationshipSet relationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        global::docx.DocxHyperlink hyperlink = global::docx.DocxHyperlink.CreateExternal(
            relationships,
            "https://example.com/",
            "Example");

        global::docx.DocxRelationship? relationship = relationships.FindById("rId1");
        global::docx.DocxHyperlink parsed = ParseHyperlink(hyperlink.WriteXml());

        Assert.That(relationship, Is.Not.Null);
        Assert.That(relationship!.Type, Is.EqualTo(global::docx.DocxConstants.RelationshipTypeHyperlink));
        Assert.That(relationship.Target, Is.EqualTo("https://example.com/"));
        Assert.That(relationship.TargetMode, Is.EqualTo("External"));
        Assert.That(parsed.RelationshipId, Is.EqualTo("rId1"));
        Assert.That(parsed.GetText(), Is.EqualTo("Example"));
        Assert.That(parsed.ResolveRelationship(relationships), Is.True);
        Assert.That(parsed.Target, Is.EqualTo("https://example.com/"));
    }

    [Test]
    public void WriteXml_WritesExistingRelationshipIdAndOptionalAttributes()
    {
        global::docx.DocxHyperlink hyperlink = global::docx.DocxHyperlink.CreateRelationshipLink("rId5", "Linked");

        hyperlink.SetAnchor("bookmark2");
        hyperlink.SetHistory("1");
        hyperlink.SetTooltip("Open item");

        string xml = hyperlink.WriteXml();
        global::docx.DocxHyperlink parsed = ParseHyperlink(xml);

        Assert.That(xml, Does.Contain("r:id=\"rId5\""));
        Assert.That(parsed.RelationshipId, Is.EqualTo("rId5"));
        Assert.That(parsed.Anchor, Is.EqualTo("bookmark2"));
        Assert.That(parsed.History, Is.EqualTo("1"));
        Assert.That(parsed.Tooltip, Is.EqualTo("Open item"));
        Assert.That(parsed.GetText(), Is.EqualTo("Linked"));
    }

    [Test]
    public void ParagraphWriting_WritesAddedHyperlink()
    {
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");

        paragraph.AddHyperlink(global::docx.DocxHyperlink.CreateAnchor("bookmark1", "Jump"));

        global::docx.DocxParagraph parsed = ParseParagraph(paragraph.WriteXml());

        Assert.That(parsed.ChildCount(), Is.EqualTo(1));
        Assert.That(parsed.HyperlinkCount(), Is.EqualTo(1));
        Assert.That(parsed.GetHyperlink(0).Anchor, Is.EqualTo("bookmark1"));
        Assert.That(parsed.GetHyperlink(0).GetText(), Is.EqualTo("Jump"));
    }

    [Test]
    public void ParagraphWriting_WritesExternalHyperlinkRelationshipForResolution()
    {
        global::docx.DocxRelationshipSet relationships = new global::docx.DocxRelationshipSet("/word/document.xml");
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");

        paragraph.AddHyperlink(global::docx.DocxHyperlink.CreateExternal(
            relationships,
            "https://example.com/report",
            "Report"));

        global::docx.DocxParagraph parsed = ParseParagraph(paragraph.WriteXml());

        Assert.That(relationships.Count(), Is.EqualTo(1));
        Assert.That(parsed.HyperlinkCount(), Is.EqualTo(1));
        Assert.That(parsed.GetHyperlink(0).RelationshipId, Is.EqualTo("rId1"));
        Assert.That(parsed.GetHyperlink(0).GetText(), Is.EqualTo("Report"));
        Assert.That(parsed.ResolveHyperlinks(relationships), Is.EqualTo(1));
        Assert.That(parsed.GetHyperlink(0).Target, Is.EqualTo("https://example.com/report"));
        Assert.That(parsed.GetText(), Is.EqualTo("Report"));
    }

    [Test]
    public void HyperlinkParsing_RejectsInvalidArguments()
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create("<root />");
        global::docx.DocxHyperlink hyperlink = new global::docx.DocxHyperlink("");
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");

        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxHyperlink(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxHyperlink.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxHyperlink.CreateAnchor(null!, "text"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxHyperlink.CreateAnchor("anchor", null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxHyperlink.CreateAnchor("", "text"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxHyperlink.CreateExternal(null!, "https://example.com/", "text"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxHyperlink.CreateExternal(CreateRelationships(), null!, "text"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxHyperlink.CreateExternal(CreateRelationships(), "https://example.com/", null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxHyperlink.CreateExternal(CreateRelationships(), "", "text"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxHyperlink.CreateRelationshipLink(null!, "text"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxHyperlink.CreateRelationshipLink("rId1", null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxHyperlink.CreateRelationshipLink("", "text"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxHyperlink.CreateTextRun(null!));
        Assert.Throws<ArgumentNullException>(() => hyperlink.AddChild(null!));
        Assert.Throws<ArgumentNullException>(() => hyperlink.AddRun(null!));
        Assert.Throws<ArgumentNullException>(() => hyperlink.SetRelationshipId(null!));
        Assert.Throws<ArgumentNullException>(() => hyperlink.SetAnchor(null!));
        Assert.Throws<ArgumentNullException>(() => hyperlink.SetHistory(null!));
        Assert.Throws<ArgumentNullException>(() => hyperlink.SetTooltip(null!));
        Assert.Throws<ArgumentNullException>(() => hyperlink.ResolveRelationship(null!));
        Assert.Throws<ArgumentNullException>(() => hyperlink.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => hyperlink.WriteChildrenTo(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxHyperlink.DocxRunFromBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.AddHyperlink(null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.ResolveHyperlinks(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxParagraph.DocxHyperlinkFromBuffer(null!));

        Assert.That(reader.ReadToElement("root", ""), Is.True);
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxHyperlink.Parse(reader));
    }

    static global::docx.DocxRelationshipSet CreateRelationships()
    {
        global::docx.DocxRelationshipSet relationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        relationships.CreateRelationshipWithId("rIdLink", global::docx.DocxConstants.RelationshipTypeHyperlink, "https://example.com/path", true);
        relationships.CreateRelationshipWithId("rIdInternal", global::docx.DocxConstants.RelationshipTypeHyperlink, "../custom/item.xml", false);
        relationships.CreateRelationshipWithId("rIdImage", global::docx.DocxConstants.RelationshipTypeImage, "media/image1.png", false);

        return relationships;
    }

    static global::docx.DocxHyperlink ParseHyperlink(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("hyperlink", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxHyperlink.Parse(reader);
    }

    static global::docx.DocxParagraph ParseParagraph(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("p", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxParagraph.Parse(reader);
    }
}
