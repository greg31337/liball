using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxParagraphPublicApiTests
{
    [Test]
    public void Navigation_ClassifiesDirectChildrenInDocumentOrder()
    {
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph(
            "<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" xmlns:x=\"urn:test\">"
            + "<w:pPr><w:pStyle w:val=\"Body\" /></w:pPr>"
            + RunXml("Run")
            + "<w:hyperlink w:anchor=\"target\">" + RunXml("Link") + "</w:hyperlink>"
            + "<w:bookmarkStart w:id=\"1\" w:name=\"mark\" /><w:bookmarkEnd w:id=\"1\" />"
            + "<w:fldSimple w:instr=\"DATE\">" + RunXml("Result") + "</w:fldSimple>"
            + "<w:sdt><w:sdtContent>" + RunXml("Control") + "</w:sdtContent></w:sdt>"
            + "<w:proofErr w:type=\"spellStart\" />"
            + "<w:permStart w:id=\"2\" w:edGrp=\"everyone\" /><w:permEnd w:id=\"2\" />"
            + "<w:smartTag w:uri=\"urn:smart\" w:element=\"person\">" + RunXml("Smart") + "</w:smartTag>"
            + "<w:customXml w:uri=\"urn:custom\" w:element=\"item\">" + RunXml("Custom") + "</w:customXml>"
            + "<w:customXmlInsRangeEnd w:id=\"8\" />"
            + "<w:ins w:id=\"3\">" + RunXml("Inserted") + "</w:ins>"
            + "<x:preserved x:value=\"yes\" />"
            + "</w:p>");

        List<global::docx.DocxParagraphChild> children = paragraph.GetChildren();

        Assert.That(children.Count, Is.EqualTo(14));
        Assert.That(children[0].Kind, Is.EqualTo(global::docx.DocxParagraphChildKind.Run));
        Assert.That(children[0].Run, Is.Not.Null);
        Assert.That(children[1].Kind, Is.EqualTo(global::docx.DocxParagraphChildKind.Hyperlink));
        Assert.That(children[1].Hyperlink, Is.Not.Null);
        Assert.That(children[2].Kind, Is.EqualTo(global::docx.DocxParagraphChildKind.Bookmark));
        Assert.That(children[3].Kind, Is.EqualTo(global::docx.DocxParagraphChildKind.Bookmark));
        Assert.That(children[4].Kind, Is.EqualTo(global::docx.DocxParagraphChildKind.Field));
        Assert.That(children[5].Kind, Is.EqualTo(global::docx.DocxParagraphChildKind.ContentControl));
        Assert.That(children[6].Kind, Is.EqualTo(global::docx.DocxParagraphChildKind.ProofingMarker));
        Assert.That(children[7].Kind, Is.EqualTo(global::docx.DocxParagraphChildKind.PermissionRange));
        Assert.That(children[8].Kind, Is.EqualTo(global::docx.DocxParagraphChildKind.PermissionRange));
        Assert.That(children[9].Kind, Is.EqualTo(global::docx.DocxParagraphChildKind.SmartTag));
        Assert.That(children[10].Kind, Is.EqualTo(global::docx.DocxParagraphChildKind.CustomXml));
        Assert.That(children[11].Kind, Is.EqualTo(global::docx.DocxParagraphChildKind.CustomXml));
        Assert.That(children[12].Kind, Is.EqualTo(global::docx.DocxParagraphChildKind.Revision));
        Assert.That(children[13].Kind, Is.EqualTo(global::docx.DocxParagraphChildKind.PreservedUnknown));

        Assert.That(paragraph.RunCount(), Is.EqualTo(1));
        Assert.That(paragraph.HyperlinkCount(), Is.EqualTo(1));
        Assert.That(paragraph.BookmarkCount(), Is.EqualTo(2));
        Assert.That(paragraph.FieldCount(), Is.EqualTo(1));
        Assert.That(paragraph.ContentControlCount(), Is.EqualTo(1));
        Assert.That(paragraph.ProofingMarkerCount(), Is.EqualTo(1));
        Assert.That(paragraph.GetProofingMarker(0).Xml, Does.Contain("spellStart"));
        Assert.That(paragraph.PermissionRangeCount(), Is.EqualTo(2));
        Assert.That(paragraph.GetPermissionRange(0).Xml, Does.Contain("permStart"));
        Assert.That(paragraph.SmartTagCount(), Is.EqualTo(1));
        Assert.That(paragraph.GetSmartTag(0).Xml, Does.Contain("smartTag"));
        Assert.That(paragraph.CustomXmlMarkupCount(), Is.EqualTo(2));
        Assert.That(paragraph.GetCustomXmlMarkupChild(1).Xml, Does.Contain("customXmlInsRangeEnd"));
        Assert.That(paragraph.PreservedUnknownChildCount(), Is.EqualTo(1));
        Assert.That(paragraph.GetPreservedUnknownChild(0).Xml, Does.Contain("preserved"));

        children.Clear();
        paragraph.GetRuns().Clear();
        paragraph.GetHyperlinks().Clear();
        paragraph.GetBookmarks().Clear();
        paragraph.GetFields().Clear();
        paragraph.GetContentControls().Clear();

        Assert.That(paragraph.ChildCount(), Is.EqualTo(14));
        Assert.That(paragraph.GetRun(0).GetText(), Is.EqualTo("Run"));
        Assert.That(paragraph.GetHyperlink(0).GetText(), Is.EqualTo("Link"));
        Assert.That(paragraph.GetBookmark(0).Name, Is.EqualTo("mark"));
        Assert.That(paragraph.GetField(0).GetText(), Is.EqualTo("Result"));
        Assert.That(paragraph.GetContentControl(0).GetText(), Is.EqualTo("Control"));
    }

    [Test]
    public void GenericEditing_PreservesPropertiesAndUnknownMarkup()
    {
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph(
            "<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" xmlns:x=\"urn:test\">"
            + "<w:pPr><w:pStyle w:val=\"Body\" /></w:pPr>"
            + RunXml("Text")
            + "<x:preserved x:value=\"yes\" />"
            + "</w:p>");
        global::docx.DocxParagraphChild permission = Child("<w:permStart w:id=\"4\" />");
        global::docx.DocxParagraphChild proofing = Child("<w:proofErr w:type=\"spellStart\" />");
        global::docx.DocxParagraphChild smartTag = Child("<w:smartTag w:uri=\"urn:smart\" w:element=\"item\" />");

        paragraph.InsertParagraphChild(1, permission);
        paragraph.AddParagraphChild(proofing);
        paragraph.MoveParagraphChild(3, 0);
        global::docx.DocxParagraphChild replaced = paragraph.ReplaceParagraphChild(2, smartTag);
        global::docx.DocxParagraphChild removed = paragraph.RemoveParagraphChildAt(3);

        Assert.That(replaced.Kind, Is.EqualTo(global::docx.DocxParagraphChildKind.PermissionRange));
        Assert.That(removed.Kind, Is.EqualTo(global::docx.DocxParagraphChildKind.PreservedUnknown));
        Assert.That(paragraph.GetChild(0).Kind, Is.EqualTo(global::docx.DocxParagraphChildKind.ProofingMarker));
        Assert.That(paragraph.GetChild(1).Kind, Is.EqualTo(global::docx.DocxParagraphChildKind.Run));
        Assert.That(paragraph.GetChild(2).Kind, Is.EqualTo(global::docx.DocxParagraphChildKind.SmartTag));

        string xml = paragraph.WriteXml();
        global::docx.DocxParagraph reparsed = new global::docx.DocxParagraph(xml);

        Assert.That(xml, Does.Contain("pStyle"));
        Assert.That(xml, Does.Contain("spellStart"));
        Assert.That(xml, Does.Contain("smartTag"));
        Assert.That(xml, Does.Not.Contain("x:preserved"));
        Assert.That(reparsed.ChildCount(), Is.EqualTo(3));
        Assert.That(reparsed.GetText(), Is.EqualTo("Text"));
    }

    [Test]
    public void TypedEditing_InsertsAndReplacesEveryTypedChildKind()
    {
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />");
        global::docx.DocxRun run = new global::docx.DocxRun(RunXmlWithNamespace("Run"));
        global::docx.DocxHyperlink hyperlink = new global::docx.DocxHyperlink(HyperlinkXml("Link"));
        global::docx.DocxBookmark bookmark = new global::docx.DocxBookmark("", global::docx.DocxBookmark.KindStart, "5", "mark", "", "");
        global::docx.DocxField field = new global::docx.DocxField(FieldXml("Field"));
        global::docx.DocxContentControl control = new global::docx.DocxContentControl(ContentControlXml("Control"));

        paragraph.InsertRun(0, run);
        paragraph.InsertHyperlink(1, hyperlink);
        paragraph.InsertBookmark(2, bookmark);
        paragraph.InsertField(3, field);
        paragraph.InsertContentControl(4, control);

        Assert.That(paragraph.ReplaceChildWithHyperlink(0, new global::docx.DocxHyperlink(HyperlinkXml("H2"))).Kind, Is.EqualTo(global::docx.DocxParagraphChildKind.Run));
        Assert.That(paragraph.ReplaceChildWithBookmark(1, new global::docx.DocxBookmark("", global::docx.DocxBookmark.KindEnd, "5", "", "", "")).Kind, Is.EqualTo(global::docx.DocxParagraphChildKind.Hyperlink));
        Assert.That(paragraph.ReplaceChildWithField(2, new global::docx.DocxField(FieldXml("F2"))).Kind, Is.EqualTo(global::docx.DocxParagraphChildKind.Bookmark));
        Assert.That(paragraph.ReplaceChildWithContentControl(3, new global::docx.DocxContentControl(ContentControlXml("C2"))).Kind, Is.EqualTo(global::docx.DocxParagraphChildKind.Field));
        Assert.That(paragraph.ReplaceChildWithRun(4, new global::docx.DocxRun(RunXmlWithNamespace("R2"))).Kind, Is.EqualTo(global::docx.DocxParagraphChildKind.ContentControl));

        Assert.That(paragraph.GetChild(0).Kind, Is.EqualTo(global::docx.DocxParagraphChildKind.Hyperlink));
        Assert.That(paragraph.GetChild(1).Kind, Is.EqualTo(global::docx.DocxParagraphChildKind.Bookmark));
        Assert.That(paragraph.GetChild(2).Kind, Is.EqualTo(global::docx.DocxParagraphChildKind.Field));
        Assert.That(paragraph.GetChild(3).Kind, Is.EqualTo(global::docx.DocxParagraphChildKind.ContentControl));
        Assert.That(paragraph.GetChild(4).Kind, Is.EqualTo(global::docx.DocxParagraphChildKind.Run));
        Assert.That(paragraph.GetText(), Is.EqualTo("H2F2C2R2"));

        global::docx.DocxParagraph reparsed = new global::docx.DocxParagraph(paragraph.WriteXml());

        Assert.That(reparsed.ChildCount(), Is.EqualTo(5));
        Assert.That(reparsed.GetText(), Is.EqualTo("H2F2C2R2"));
    }

    [Test]
    public void Editing_RejectsNullValuesAndInvalidPositions()
    {
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\">" + RunXml("Only") + "</w:p>");

        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxParagraphChild(null!));
        Assert.Throws<ArgumentException>(() => new global::docx.DocxParagraphChild(""));
        Assert.Throws<ArgumentException>(() => new global::docx.DocxParagraphChild(" "));
        Assert.Throws<ArgumentNullException>(() => paragraph.AddParagraphChild(null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.InsertParagraphChild(0, null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.InsertRun(0, null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.InsertHyperlink(0, null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.InsertBookmark(0, null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.InsertField(0, null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.InsertContentControl(0, null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.ReplaceParagraphChild(0, null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.ReplaceChildWithRun(0, null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.ReplaceChildWithHyperlink(0, null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.ReplaceChildWithBookmark(0, null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.ReplaceChildWithField(0, null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.ReplaceChildWithContentControl(0, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => paragraph.InsertParagraphChild(2, Child("<w:proofErr w:type=\"spellEnd\" />")));
        Assert.Throws<ArgumentOutOfRangeException>(() => paragraph.MoveParagraphChild(0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => paragraph.MoveParagraphChild(1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => paragraph.ReplaceParagraphChild(1, Child("<w:proofErr w:type=\"spellEnd\" />")));
        Assert.Throws<ArgumentOutOfRangeException>(() => paragraph.RemoveParagraphChildAt(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => paragraph.GetChild(1));
    }

    static global::docx.DocxParagraphChild Child(string xml)
    {
        int attributeStart = xml.IndexOf(' ');
        string namespacedXml = xml.Substring(0, attributeStart)
            + " xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\""
            + xml.Substring(attributeStart);
        return new global::docx.DocxParagraphChild(namespacedXml);
    }

    static string RunXml(string text)
    {
        return "<w:r><w:t>" + text + "</w:t></w:r>";
    }

    static string RunXmlWithNamespace(string text)
    {
        return "<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:t>" + text + "</w:t></w:r>";
    }

    static string HyperlinkXml(string text)
    {
        return "<w:hyperlink xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:anchor=\"target\">" + RunXml(text) + "</w:hyperlink>";
    }

    static string FieldXml(string text)
    {
        return "<w:fldSimple xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:instr=\"DATE\">" + RunXml(text) + "</w:fldSimple>";
    }

    static string ContentControlXml(string text)
    {
        return "<w:sdt xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:sdtContent>" + RunXml(text) + "</w:sdtContent></w:sdt>";
    }
}
