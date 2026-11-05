using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxRevisionTests
{
    [Test]
    public void ParagraphParsing_ExposesInsertedDeletedMovedAndFormattingRevisions()
    {
        global::docx.DocxParagraph paragraph = ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:ins w:id=\"1\" w:author=\"Alice\" w:date=\"2026-08-03T12:00:00Z\"><w:r><w:t>Inserted</w:t></w:r></w:ins><w:del w:id=\"2\" w:author=\"Bob\"><w:r><w:delText>Deleted</w:delText></w:r></w:del><w:moveFrom w:id=\"3\" w:author=\"Carol\"><w:p><w:r><w:t>Moved</w:t></w:r></w:p></w:moveFrom><w:pPrChange w:id=\"4\" w:author=\"Dana\"><w:pPr><w:jc w:val=\"center\" /></w:pPr></w:pPrChange></w:p>");

        Assert.That(paragraph.ChildCount(), Is.EqualTo(4));
        Assert.That(paragraph.RevisionCount(), Is.EqualTo(4));
        Assert.That(paragraph.UnknownElementCount(), Is.EqualTo(4));
        Assert.That(paragraph.CountRevisions(global::docx.DocxRevision.KindInserted), Is.EqualTo(1));
        Assert.That(paragraph.CountRevisions(global::docx.DocxRevision.KindDeleted), Is.EqualTo(1));
        Assert.That(paragraph.CountRevisions(global::docx.DocxRevision.KindMoveFrom), Is.EqualTo(1));
        Assert.That(paragraph.CountRevisions(global::docx.DocxRevision.KindFormatting), Is.EqualTo(1));

        global::docx.DocxRevision inserted = paragraph.GetRevision(0);
        Assert.That(inserted.IsInserted(), Is.True);
        Assert.That(inserted.Id, Is.EqualTo("1"));
        Assert.That(inserted.Author, Is.EqualTo("Alice"));
        Assert.That(inserted.Date, Is.EqualTo("2026-08-03T12:00:00Z"));
        Assert.That(inserted.RunCount(), Is.EqualTo(1));
        Assert.That(inserted.GetRun(0).GetText(), Is.EqualTo("Inserted"));

        global::docx.DocxRevision moved = paragraph.GetRevision(2);
        Assert.That(moved.IsMoveFrom(), Is.True);
        Assert.That(moved.BlockCount(), Is.EqualTo(1));
        Assert.That(moved.GetBlock(0).IsParagraph(), Is.True);
        Assert.That(moved.GetBlock(0).Paragraph!.GetText(), Is.EqualTo("Moved"));

        global::docx.DocxRevision formatting = paragraph.GetRevision(3);
        Assert.That(formatting.IsFormattingChange(), Is.True);
        Assert.That(formatting.UnknownChildCount(), Is.EqualTo(1));
        Assert.That(formatting.GetUnknownChildXml(0), Does.Contain("w:jc"));
    }

    [Test]
    public void RunParsing_ExposesRunLevelRevisions()
    {
        global::docx.DocxRun run = ParseRun("<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:ins w:id=\"5\" w:author=\"Eve\"><w:r><w:t>Inserted</w:t></w:r></w:ins><w:del w:id=\"6\" w:author=\"Frank\"><w:r><w:delText>Deleted</w:delText></w:r></w:del><w:rPrChange w:id=\"7\" w:author=\"Grace\"><w:rPr><w:b /></w:rPr></w:rPrChange></w:r>");

        Assert.That(run.ChildCount(), Is.EqualTo(3));
        Assert.That(run.RevisionCount(), Is.EqualTo(3));
        Assert.That(run.UnknownElementCount(), Is.EqualTo(3));
        Assert.That(run.CountRevisions(global::docx.DocxRevision.KindInserted), Is.EqualTo(1));
        Assert.That(run.CountRevisions(global::docx.DocxRevision.KindDeleted), Is.EqualTo(1));
        Assert.That(run.CountRevisions(global::docx.DocxRevision.KindFormatting), Is.EqualTo(1));
        Assert.That(run.GetRevision(0).RunCount(), Is.EqualTo(1));
        Assert.That(run.GetRevision(0).GetRun(0).GetText(), Is.EqualTo("Inserted"));
        Assert.That(run.GetRevision(2).UnknownChildCount(), Is.EqualTo(1));
        Assert.That(run.WriteXml(), Does.Contain("w:rPrChange"));
    }

    [Test]
    public void RevisionParsing_PreservesRawXmlAndChildren()
    {
        global::docx.DocxRevision revision = global::docx.DocxRevision.ParseXml("<w:moveTo xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:id=\"8\" w:author=\"Heidi\"><w:r><w:t>Moved here</w:t></w:r><w:tbl><w:tr><w:tc><w:p /></w:tc></w:tr></w:tbl></w:moveTo>");

        Assert.That(revision.IsMoveTo(), Is.True);
        Assert.That(revision.LocalName, Is.EqualTo("moveTo"));
        Assert.That(revision.Id, Is.EqualTo("8"));
        Assert.That(revision.Author, Is.EqualTo("Heidi"));
        Assert.That(revision.RunCount(), Is.EqualTo(1));
        Assert.That(revision.BlockCount(), Is.EqualTo(1));
        Assert.That(revision.GetBlock(0).IsTable(), Is.True);

        string xml = revision.WriteXml();

        Assert.That(xml, Does.Contain("w:moveTo"));
        Assert.That(xml, Does.Contain("Moved here"));
    }

    [Test]
    public void RevisionCollections_RejectInvalidArguments()
    {
        global::docx.DocxRevisions revisions = new global::docx.DocxRevisions();
        global::docx.DocxRevision revision = global::docx.DocxRevision.ParseXml("<w:ins xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:id=\"9\" />");
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");
        global::docx.DocxRun run = new global::docx.DocxRun("");

        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxRevision(null!, "w", "ins", global::docx.DocxConstants.NamespaceWordprocessingMl));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxRevision("", null!, "ins", global::docx.DocxConstants.NamespaceWordprocessingMl));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxRevision("", "w", null!, global::docx.DocxConstants.NamespaceWordprocessingMl));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxRevision("", "w", "ins", null!));
        Assert.Throws<ArgumentException>(() => new global::docx.DocxRevision("", "w", "", global::docx.DocxConstants.NamespaceWordprocessingMl));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRevision.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRevision.ParseXml(null!));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxRevision.ParseXml("<root />"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRevision.IsSupported(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRevision.IsRevisionLocalName(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRevision.KindFromLocalName(null!));
        Assert.Throws<ArgumentNullException>(() => revision.IsKind(null!));
        Assert.Throws<ArgumentException>(() => revision.IsKind(""));
        Assert.Throws<ArgumentNullException>(() => revision.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRevision.DocxRunFromBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRevision.DocxParagraphFromBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRevision.DocxTableFromBuffer(null!));

        Assert.Throws<ArgumentNullException>(() => revisions.AddRevision(null!));
        Assert.Throws<ArgumentNullException>(() => revisions.CountRevisions(null!));
        Assert.Throws<ArgumentException>(() => revisions.CountRevisions(""));
        Assert.Throws<ArgumentNullException>(() => revisions.FindRevisionById(null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.AddRevision(null!));
        Assert.Throws<ArgumentNullException>(() => paragraph.CountRevisions(null!));
        Assert.Throws<ArgumentException>(() => paragraph.CountRevisions(""));
        Assert.Throws<ArgumentNullException>(() => run.AddRevision(null!));
        Assert.Throws<ArgumentNullException>(() => run.CountRevisions(null!));
        Assert.Throws<ArgumentException>(() => run.CountRevisions(""));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxParagraph.DocxRevisionFromBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxRun.DocxRevisionFromBuffer(null!));
    }

    [Test]
    public void RevisionCollections_CountAndFindRevisions()
    {
        global::docx.DocxRevisions revisions = new global::docx.DocxRevisions();
        global::docx.DocxRevision inserted = global::docx.DocxRevision.ParseXml("<w:ins xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:id=\"10\" />");
        global::docx.DocxRevision deleted = global::docx.DocxRevision.ParseXml("<w:del xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:id=\"11\" />");

        revisions.AddRevision(inserted);
        revisions.AddRevision(deleted);

        Assert.That(revisions.RevisionCount(), Is.EqualTo(2));
        Assert.That(revisions.GetRevision(0), Is.SameAs(inserted));
        Assert.That(revisions.CountRevisions(global::docx.DocxRevision.KindInserted), Is.EqualTo(1));
        Assert.That(revisions.FindRevisionById("11"), Is.SameAs(deleted));
        Assert.That(revisions.FindRevisionById("missing"), Is.Null);
    }

    [Test]
    public void PublicRevisionManagement_TraversesKindsMetadataAndRevisionAwareText()
    {
        global::docx.DocxRevisionManagement management = new global::docx.DocxRevisionManagement();
        global::docx.DocxParagraph paragraph = ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:pPr><w:pPrChange w:id=\"5\" w:author=\"Eve\" w:date=\"2026-08-05T00:00:00Z\"><w:pPr><w:jc w:val=\"center\" /></w:pPr></w:pPrChange></w:pPr><w:ins w:id=\"1\" w:author=\"Alice\" w:date=\"2026-08-01T00:00:00Z\"><w:r><w:t>Inserted</w:t></w:r></w:ins><w:del w:id=\"2\" w:author=\"Bob\"><w:r><w:delText>Deleted</w:delText></w:r></w:del><w:moveFrom w:id=\"3\" w:author=\"Carol\"><w:r><w:t>MovedFrom</w:t></w:r></w:moveFrom><w:moveTo w:id=\"4\" w:author=\"Dana\"><w:r><w:t>MovedTo</w:t></w:r></w:moveTo></w:p>");

        List<global::docx.DocxRevisionInfo> revisions = management.GetParagraphRevisions(paragraph);

        Assert.That(revisions, Has.Count.EqualTo(5));
        Assert.That(revisions[0].Kind, Is.EqualTo(global::docx.DocxRevisionKind.FormattingChange));
        Assert.That(revisions[1].Kind, Is.EqualTo(global::docx.DocxRevisionKind.Inserted));
        Assert.That(revisions[1].LocalName, Is.EqualTo("ins"));
        Assert.That(revisions[1].Id, Is.EqualTo("1"));
        Assert.That(revisions[1].Author, Is.EqualTo("Alice"));
        Assert.That(revisions[1].Date, Is.EqualTo("2026-08-01T00:00:00Z"));
        Assert.That(revisions[1].Text, Is.EqualTo("Inserted"));
        Assert.That(revisions[1].Xml, Does.Contain("w:ins"));
        Assert.That(revisions[2].Kind, Is.EqualTo(global::docx.DocxRevisionKind.Deleted));
        Assert.That(revisions[2].Text, Is.EqualTo("Deleted"));
        Assert.That(revisions[3].Kind, Is.EqualTo(global::docx.DocxRevisionKind.MoveFrom));
        Assert.That(revisions[4].Kind, Is.EqualTo(global::docx.DocxRevisionKind.MoveTo));
        Assert.That(management.ExtractParagraphRevisionText(paragraph, true, false), Is.EqualTo("InsertedMovedTo"));
        Assert.That(management.ExtractParagraphRevisionText(paragraph, false, true), Is.EqualTo("DeletedMovedFrom"));

        global::docx.DocxRun run = ParseRun("<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:ins w:id=\"6\"><w:r><w:t>Nested</w:t></w:r></w:ins></w:r>");
        Assert.That(management.GetRunRevisions(run), Has.Count.EqualTo(1));
    }

    [Test]
    public void PublicRevisionManagement_AcceptsAndRejectsOnlySelectedParagraphChildren()
    {
        global::docx.DocxRevisionManagement management = new global::docx.DocxRevisionManagement();
        global::docx.DocxParagraph paragraph = ParseParagraph("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t>A</w:t></w:r><w:ins w:id=\"1\"><w:r><w:t>B</w:t></w:r></w:ins><w:del w:id=\"2\"><w:r><w:delText>C</w:delText></w:r></w:del><w:moveTo w:id=\"3\"><w:r><w:t>D</w:t></w:r></w:moveTo></w:p>");

        Assert.That(management.AcceptParagraphChildRange(paragraph, 1, 2), Is.EqualTo(1));
        Assert.That(paragraph.GetText(), Is.EqualTo("ABD"));
        Assert.That(management.GetParagraphRevisions(paragraph), Has.Count.EqualTo(2));
        Assert.That(management.GetParagraphRevisions(paragraph)[0].Id, Is.EqualTo("2"));

        Assert.That(management.RejectParagraphChildRange(paragraph, 2, 3), Is.EqualTo(1));
        Assert.That(paragraph.GetText(), Is.EqualTo("ABCD"));
        Assert.That(management.GetParagraphRevisions(paragraph), Has.Count.EqualTo(1));
        Assert.That(management.GetParagraphRevisions(paragraph)[0].Id, Is.EqualTo("3"));

        Assert.That(management.RejectParagraphChildRange(paragraph, 3, 4), Is.EqualTo(1));
        Assert.That(paragraph.GetText(), Is.EqualTo("ABC"));
        Assert.That(management.GetParagraphRevisions(paragraph), Is.Empty);
    }

    [Test]
    public void PublicRevisionManagement_TransformsEveryStoryAndPreservesTrackChanges()
    {
        global::docx.DocxRevisionManagement management = new global::docx.DocxRevisionManagement();
        global::docx.DocxDocument accepted = global::docx.DocxDocument.OpenPackage(CreateRevisionDocumentPackage());

        List<global::docx.DocxRevisionInfo> revisions = management.GetDocumentRevisions(accepted);

        Assert.That(revisions, Has.Count.EqualTo(3));
        Assert.That(revisions[0].PartName, Is.EqualTo("/word/document.xml"));
        Assert.That(management.GetDocumentRevisionsByKind(accepted, global::docx.DocxRevisionKind.Deleted), Has.Count.EqualTo(1));
        Assert.That(management.FindDocumentRevisionById(accepted, "2")!.Author, Is.EqualTo("Bob"));
        Assert.That(management.ExtractDocumentRevisionText(accepted, false, true), Does.Contain("HeaderDeleted"));
        Assert.That(management.ExtractDocumentRevisionText(accepted, false, true), Does.Not.Contain("MainInserted"));
        Assert.That(management.IsTrackChangesEnabled(accepted), Is.True);

        Assert.That(management.AcceptDocumentRevisions(accepted), Is.EqualTo(3));
        Assert.That(management.GetDocumentRevisions(accepted), Is.Empty);
        Assert.That(accepted.GetBody().GetText(), Is.EqualTo("MainInserted"));
        Assert.That(accepted.GetHeaders()[0].GetText(), Is.EqualTo(""));
        Assert.That(accepted.GetComments().GetText(), Is.EqualTo("CommentMoved"));
        Assert.That(management.IsTrackChangesEnabled(accepted), Is.True);

        management.SetTrackChangesEnabled(accepted, false);
        Assert.That(management.IsTrackChangesEnabled(accepted), Is.False);
        Assert.That(accepted.GetSettings().HasTrackRevisions, Is.True);

        global::docx.DocxDocument rejected = global::docx.DocxDocument.OpenPackage(CreateRevisionDocumentPackage());

        Assert.That(management.RejectDocumentRevisions(rejected), Is.EqualTo(3));
        Assert.That(management.GetDocumentRevisions(rejected), Is.Empty);
        Assert.That(rejected.GetBody().GetText(), Is.EqualTo(""));
        Assert.That(rejected.GetHeaders()[0].GetText(), Is.EqualTo("HeaderDeleted"));
        Assert.That(rejected.GetComments().GetText(), Is.EqualTo(""));
        Assert.That(management.IsTrackChangesEnabled(rejected), Is.True);
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

    static global::docx.DocxPackage CreateRevisionDocumentPackage()
    {
        global::docx.DocxPackage package = new global::docx.DocxPackage();
        global::docx.DocxRelationshipSet rootRelationships = global::docx.DocxPackage.CreateRootRelationshipSet("/word/document.xml");
        global::docx.DocxRelationshipSet documentRelationships = new global::docx.DocxRelationshipSet("/word/document.xml");

        documentRelationships.CreateRelationshipWithId("rHeader", global::docx.DocxConstants.RelationshipTypeHeader, "header1.xml", false);
        documentRelationships.CreateRelationshipWithId("rComments", global::docx.DocxConstants.RelationshipTypeComments, "comments.xml", false);
        documentRelationships.CreateRelationshipWithId("rSettings", global::docx.DocxConstants.RelationshipTypeSettings, "settings.xml", false);
        package.ContentTypes.SetDefault("rels", global::docx.DocxConstants.ContentTypeRelationships);
        package.ContentTypes.SetDefault("xml", global::docx.DocxConstants.ContentTypeXml);
        package.AddXmlPartWithContent("/word/document.xml", global::docx.DocxConstants.ContentTypeMainDocument, "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body><w:p><w:ins w:id=\"1\" w:author=\"Alice\"><w:r><w:t>MainInserted</w:t></w:r></w:ins></w:p></w:body></w:document>");
        package.AddXmlPartWithContent("/word/header1.xml", global::docx.DocxConstants.ContentTypeHeader, "<w:hdr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p><w:del w:id=\"2\" w:author=\"Bob\"><w:r><w:delText>HeaderDeleted</w:delText></w:r></w:del></w:p></w:hdr>");
        package.AddXmlPartWithContent("/word/comments.xml", global::docx.DocxConstants.ContentTypeComments, "<w:comments xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:comment w:id=\"7\" w:author=\"Reviewer\"><w:p><w:moveTo w:id=\"3\" w:author=\"Carol\"><w:r><w:t>CommentMoved</w:t></w:r></w:moveTo></w:p></w:comment></w:comments>");
        package.AddXmlPartWithContent("/word/settings.xml", global::docx.DocxConstants.ContentTypeSettings, "<w:settings xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:trackRevisions /></w:settings>");
        package.AddXmlPartWithContent("_rels/.rels", global::docx.DocxConstants.ContentTypeRelationships, rootRelationships.WriteXml());
        package.AddXmlPartWithContent("/word/_rels/document.xml.rels", global::docx.DocxConstants.ContentTypeRelationships, documentRelationships.WriteXml());
        return package;
    }
}
